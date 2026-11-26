# notame preprocessing, quality metrics and MUVR selection for an MS-DIAL alignment export,
# with PCA and drift correction plots.
# Original workflow: RHaikonen, 2024-04-29.
# Ported to notame >= 1.0 (Bioconductor; objects are SummarizedExperiment, not MetaboSet).
#
# MS-DIAL sets these variables before sourcing this file:
#   path      - folder that holds the exported table; every output is written here
#   file_name - the wide (Height/Area) alignment table MS-DIAL exported for this run
#   ion_mod   - "pos", "neg" or "" (used as the Split of the feature IDs)

suppressPackageStartupMessages({
  library(notame)
  library(SummarizedExperiment)
  library(ggplot2)
  library(dplyr)
  library(doParallel)
})

if (utils::packageVersion("notame") < "0.99.0") {
  stop("This workflow needs notame 1.0 or later from Bioconductor (installed: ",
       utils::packageVersion("notame"), "). Install it with BiocManager::install('notame').",
       call. = FALSE)
}

path <- paste0(sub("/+$", "", normalizePath(path, winslash = "/", mustWork = TRUE)), "/")
if (!exists("ion_mod") || is.null(ion_mod) || is.na(ion_mod)) ion_mod <- ""
grouping_name <- "Class"
set.seed(1234567)

# notame::init_log() would also replace options(error=) of the whole (embedded) R session, so
# only the log destination is set here; it is set back to the console at the end.
futile.logger::flog.appender(futile.logger::appender.tee(paste0(path, "notame_log.txt")), name = "notame")
log_text(paste("notame", utils::packageVersion("notame"), "- input:", paste0(path, file_name)))

# Embedded R (MS-DIAL runs R in-process) is safest with serial BiocParallel; drift correction
# is fast enough without workers. The PSOCK cluster used for MUVR is created explicitly below.
BiocParallel::register(BiocParallel::SerialParam(), default = TRUE)
n_workers <- max(1L, min(8L, parallel::detectCores() - 1L), na.rm = TRUE)

# ---------------------------------------------------------------------------------------------
# Small helpers
# ---------------------------------------------------------------------------------------------

finite_vals <- function(x) x[is.finite(x)]
finite_mean <- function(x) { x <- finite_vals(x); if (length(x)) mean(x) else NA_real_ }
finite_sd <- function(x) { x <- finite_vals(x); if (length(x) > 1) stats::sd(x) else NA_real_ }
finite_median <- function(x) { x <- finite_vals(x); if (length(x)) stats::median(x) else NA_real_ }

# Opens a PNG device; Cairo gives the same output on every machine, png() is the fallback.
open_png <- function(file, width = 7, height = 7) {
  if (requireNamespace("Cairo", quietly = TRUE)) {
    Cairo::Cairo(file = file, unit = "in", dpi = 72, width = width, height = height,
                 type = "png", bg = "white")
  } else {
    grDevices::png(filename = file, units = "in", res = 72, width = width, height = height,
                   bg = "white")
  }
}

placeholder_plot <- function(title, message) {
  ggplot() + theme_void() + labs(title = title) +
    annotate("text", x = 0, y = 0, label = message)
}

# ---------------------------------------------------------------------------------------------
# Reading the MS-DIAL export
# ---------------------------------------------------------------------------------------------

find_mz_rt_cols <- function(feature_data) {
  mz_tags <- c("^average.?mz$", "mass", "m.?z$", "molecular.?weight", "average.?mz")
  rt_tags <- c("^average.?rt", "retention.?time", "^rt$", "(?=.*rt)(?=.*min)")
  pick <- function(tags) {
    for (tag in tags) {
      hits <- grepl(tag, tolower(colnames(feature_data)), perl = TRUE)
      if (any(hits)) return(colnames(feature_data)[which(hits)[1]])
    }
    NULL
  }
  mz_col <- pick(mz_tags)
  rt_col <- pick(rt_tags)
  if (is.null(mz_col)) stop("No m/z column found in the MS-DIAL export.", call. = FALSE)
  if (is.null(rt_col)) stop("No retention time column found in the MS-DIAL export.", call. = FALSE)
  list(mz_col = mz_col, rt_col = rt_col)
}

# Feature IDs like "pos_301_14123a5_123"; spots that share m/z and RT get their alignment ID added.
name_features <- function(feature_data, split) {
  cols <- find_mz_rt_cols(feature_data)
  mz <- gsub("[.]", "_", as.character(suppressWarnings(as.numeric(feature_data[[cols$mz_col]]))))
  rt <- gsub("[.]", "_", as.character(suppressWarnings(as.numeric(feature_data[[cols$rt_col]]))))
  ids <- paste0(split, "_", mz, "a", rt)
  if (anyDuplicated(ids)) {
    dup <- ids %in% ids[duplicated(ids)]
    if ("Alignment ID" %in% colnames(feature_data)) {
      ids[dup] <- paste0(ids[dup], "_ID", feature_data[["Alignment ID"]][dup])
    }
    ids <- make.unique(ids, sep = "_")
    log_text(paste(sum(dup), "features share m/z and RT; their alignment IDs were added to the feature IDs"))
  }
  ids
}

# Reads the wide table written by MS-DIAL (AlignmentCSVExporter): a few file metadata rows
# (Class, Parameter1.., File type, Injection order, Batch ID) above the column header row, the
# metadata labels in the last peak-metadata column. Locating the "Class" cell (instead of the
# first non-empty cell) keeps this working when MS-DIAL writes an RT-axis note into the first cell.
read_msdial <- function(tablefile, split = "") {
  n_col <- max(utils::count.fields(tablefile, sep = "\t", quote = "", comment.char = "",
                                   blank.lines.skip = TRUE), na.rm = TRUE)
  tbl <- utils::read.delim(tablefile, header = FALSE, quote = "", comment.char = "",
                           colClasses = "character", na.strings = character(0), fill = TRUE,
                           col.names = paste0("V", seq_len(n_col)), check.names = FALSE,
                           strip.white = TRUE, encoding = "UTF-8")
  tbl[is.na(tbl)] <- ""
  top <- as.matrix(tbl[seq_len(min(nrow(tbl), 50)), , drop = FALSE])
  hit <- which(top == "Class", arr.ind = TRUE)
  if (!nrow(hit)) {
    stop("Could not find the 'Class' row of the MS-DIAL export. ",
         "Use the table that the Notame dialog exports (wide format).", call. = FALSE)
  }
  hit <- hit[order(hit[, 1], hit[, 2]), , drop = FALSE]
  class_row <- hit[1, 1]
  corner_col <- hit[1, 2]
  below <- which(tbl[(class_row + 1):nrow(tbl), 1] != "")
  if (!length(below)) stop("The MS-DIAL export has no column header row.", call. = FALSE)
  header_row <- class_row + below[1]
  meta_rows <- class_row:(header_row - 1)
  meta_labels <- tbl[meta_rows, corner_col]
  header <- unlist(tbl[header_row, ], use.names = FALSE)

  # Sample columns; per-class Average/Stdev columns, if any, carry those words in the last meta row.
  sample_cols <- (corner_col + 1):n_col
  last_meta <- unlist(tbl[header_row - 1, sample_cols], use.names = FALSE)
  sample_cols <- sample_cols[header[sample_cols] != "" & !last_meta %in% c("Average", "Stdev")]
  if (length(sample_cols) < 2) stop("The MS-DIAL export has fewer than two samples.", call. = FALSE)

  # Sample information
  pheno <- as.data.frame(t(as.matrix(tbl[meta_rows, sample_cols, drop = FALSE])),
                         stringsAsFactors = FALSE)
  rename <- c("File type" = "File_type", "Injection order" = "Injection_order", "Batch ID" = "Batch")
  colnames(pheno) <- ifelse(meta_labels %in% names(rename), rename[meta_labels],
                            gsub("[^A-Za-z0-9_]", "_", meta_labels))
  if (!"Class" %in% colnames(pheno)) stop("No Class row in the MS-DIAL export.", call. = FALSE)
  if (!"Injection_order" %in% colnames(pheno)) {
    stop("No 'Injection order' row in the MS-DIAL export.", call. = FALSE)
  }
  file_type <- if ("File_type" %in% colnames(pheno)) pheno$File_type else rep("Sample", nrow(pheno))
  qc <- ifelse(file_type %in% c("QC", "Blank", "Standard"), file_type, "Sample")
  qc[grepl("blank", pheno$Class, ignore.case = TRUE)] <- "Blank"
  qc[grepl("qc", pheno$Class, ignore.case = TRUE)] <- "QC"
  pheno$QC <- qc
  pheno$Injection_order <- suppressWarnings(as.numeric(pheno$Injection_order))
  if (anyNA(pheno$Injection_order)) {
    stop("Injection order is missing for some samples; set it in MS-DIAL's file property.", call. = FALSE)
  }
  if ("Batch" %in% colnames(pheno)) pheno$Batch <- suppressWarnings(as.numeric(pheno$Batch))
  pheno$Sample_ID <- make.unique(header[sample_cols], sep = "_")
  rownames(pheno) <- pheno$Sample_ID
  pheno <- pheno[, c("Sample_ID", setdiff(colnames(pheno), "Sample_ID"))]

  # Feature information
  data_rows <- (header_row + 1):nrow(tbl)
  data_rows <- data_rows[tbl[data_rows, 1] != ""]
  fdata <- tbl[data_rows, seq_len(corner_col), drop = FALSE]
  colnames(fdata) <- make.unique(ifelse(header[seq_len(corner_col)] == "",
                                        paste0("Column", seq_len(corner_col)),
                                        header[seq_len(corner_col)]))
  split <- if (nzchar(split)) split else "Unknown"
  fdata <- data.frame(Feature_ID = name_features(fdata, split), Split = split, fdata,
                      check.names = FALSE, stringsAsFactors = FALSE)
  rownames(fdata) <- fdata$Feature_ID

  # Abundances
  abund <- suppressWarnings(matrix(as.numeric(as.matrix(tbl[data_rows, sample_cols, drop = FALSE])),
                                   nrow = length(data_rows)))
  dimnames(abund) <- list(fdata$Feature_ID, pheno$Sample_ID)

  se <- SummarizedExperiment(assays = list(abundances = abund),
                             colData = S4Vectors::DataFrame(pheno, check.names = FALSE),
                             rowData = S4Vectors::DataFrame(fdata, check.names = FALSE))
  se <- notame::fix_object(se, assay.type = "abundances")
  # Keep sample labels as plain strings (factor levels would survive subsetting).
  for (col in intersect(c("Sample_ID", "Class", "Parameter1", "Parameter2", "QC", "File_type"),
                        colnames(colData(se)))) {
    colData(se)[[col]] <- as.character(colData(se)[[col]])
  }
  se
}

# ---------------------------------------------------------------------------------------------
# Plots
# ---------------------------------------------------------------------------------------------

# PCA of the unflagged features, unit variance scaled (NIPALS when values are missing).
pca_plot <- function(object, color, title) {
  tryCatch({
    obj <- drop_flagged(object)
    x <- t(assay(obj))
    keep <- apply(x, 2, function(v) sum(is.finite(v)) > 2 && isTRUE(finite_sd(v) > 0))
    x <- x[, keep, drop = FALSE]
    x[!is.finite(x)] <- NA
    if (ncol(x) < 2 || nrow(x) < 3) {
      return(placeholder_plot(title, "Too few features or samples for PCA"))
    }
    res <- suppressWarnings(pcaMethods::pca(x, nPcs = 2, scale = "uv", center = TRUE))
    scores <- as.data.frame(pcaMethods::scores(res))
    group <- colData(obj)[[color]]
    scores$group <- if (is.numeric(group) && color != "Batch") group else factor(group)
    r2 <- round(100 * res@R2, 1)
    ggplot(scores, aes(x = PC1, y = PC2, color = group)) +
      geom_point(size = 3) +
      theme_bw() +
      labs(title = title, color = color,
           x = paste0("PC1 (", r2[1], " %)"), y = paste0("PC2 (", r2[2], " %)"))
  }, error = function(e) {
    log_text(paste("PCA failed:", conditionMessage(e)))
    placeholder_plot(title, paste("PCA failed:", conditionMessage(e)))
  })
}

# One drift correction panel: points coloured by sample type, dashed lines at mean +- 2 SD of
# the QCs and of the other samples, and optionally the fitted QC spline.
dc_panel <- function(df, title, fit = NULL) {
  qc <- df$QC == "QC"
  yints <- c(`-2 SD (Sample)` = finite_mean(df$value[!qc]) - 2 * finite_sd(df$value[!qc]),
             `-2 SD (QC)` = finite_mean(df$value[qc]) - 2 * finite_sd(df$value[qc]),
             `+2 SD (QC)` = finite_mean(df$value[qc]) + 2 * finite_sd(df$value[qc]),
             `+2 SD (Sample)` = finite_mean(df$value[!qc]) + 2 * finite_sd(df$value[!qc]))
  yints <- sort(yints[is.finite(yints)])
  p <- ggplot(df, aes(x = Injection_order, y = value)) +
    theme_bw() +
    theme(panel.grid = element_blank(), legend.position = "bottom") +
    labs(title = title, x = "Injection order", y = NULL)
  if (length(yints)) {
    p <- p + geom_hline(yintercept = yints, color = "grey", linetype = "dashed") +
      scale_y_continuous(sec.axis = sec_axis(~., breaks = yints, labels = names(yints)))
  }
  if (!is.null(fit)) {
    p <- p + geom_line(data = fit, aes(x = Injection_order, y = value), color = "grey40")
  }
  p + geom_point(aes(color = QC, shape = QC), na.rm = TRUE)
}

dc_feature_plot <- function(orig, dc, fname, spar_lower = 0.5, spar_upper = 1.5) {
  io <- orig$Injection_order
  before <- as.numeric(assay(orig)[fname, ])
  after <- as.numeric(assay(dc)[fname, ])
  base <- data.frame(Injection_order = io, QC = orig$QC)
  log_before <- log(before)
  qc_detected <- orig$QC == "QC" & is.finite(log_before)
  fit <- NULL
  if (sum(qc_detected) >= 4) {
    spline <- stats::smooth.spline(x = io[qc_detected], y = log_before[qc_detected], all.knots = TRUE,
                                   control.spar = list(low = spar_lower, high = spar_upper))
    grid <- sort(unique(io))
    fit <- data.frame(Injection_order = grid, value = stats::predict(spline, grid)$y)
  }
  note <- rowData(dc)[fname, "DC_note"]
  p1 <- dc_panel(transform(base, value = before), paste("Before:", fname))
  p2 <- dc_panel(transform(base, value = after), paste0("After (", note, ")"))
  p3 <- dc_panel(transform(base, value = log_before), "Drift correction in log space", fit)
  p4 <- dc_panel(transform(base, value = log(after)), "Corrected data in log space")
  cowplot::plot_grid(p1, p3, p2, p4, nrow = 2)
}

quality_cols <- c("RSD", "RSD_r", "D_ratio", "D_ratio_r")

# Change of a quality metric by the drift correction (negative = better).
quality_delta <- function(orig, dc, metric) {
  q_orig <- quality(orig)
  q_dc <- quality(dc)
  delta <- q_dc[[metric]][match(rownames(dc), q_dc$Feature_ID)] -
    q_orig[[metric]][match(rownames(dc), q_orig$Feature_ID)]
  names(delta) <- rownames(dc)
  delta
}

# Cubic spline drift correction (log space) that keeps a feature's correction only when it lowers
# both RSD_r and D_ratio_r; other features keep their original values (DC_note "Low_quality").
# This is what correct_drift(check_quality = TRUE) is meant to do, done here because that option
# fails in notame 1.0.x ("invalid argument type" in .help_inspect_dc). `orig` must carry quality
# metrics (assess_quality).
correct_drift_checked <- function(orig) {
  dc <- correct_drift(orig, check_quality = FALSE, log_transform = TRUE)
  improved <- quality_delta(orig, dc, "RSD_r") < 0 & quality_delta(orig, dc, "D_ratio_r") < 0
  low <- rowData(dc)$DC_note == "Drift_corrected" & !(improved %in% TRUE)
  if (any(low)) {
    assay(dc)[low, ] <- assay(orig)[low, ]
    rowData(dc)$DC_note[low] <- "Low_quality"
    rowData(dc)[low, quality_cols] <- rowData(orig)[low, quality_cols]
  }
  counts <- table(rowData(dc)$DC_note)
  log_text(paste0("Drift correction: ", paste(names(counts), counts, sep = " ", collapse = ", ")))
  dc
}

# Plots the drift correction of the most abundant third of the corrected features: half with the
# largest improvement of the robust D-ratio, half with the smallest.
save_dc_plots <- function(orig, dc, file_prefix, n_plotting = 20) {
  notes <- rowData(dc)$DC_note
  delta <- quality_delta(orig, dc, "D_ratio_r")
  candidates <- rownames(dc)[notes == "Drift_corrected" & is.finite(delta)]
  if (!length(candidates)) {
    log_text("No drift corrected features to plot")
    return(invisible(NULL))
  }
  abundance <- apply(assay(dc)[candidates, , drop = FALSE], 1, finite_median)
  candidates <- names(sort(abundance, decreasing = TRUE))[seq_len(max(1, ceiling(length(candidates) / 3)))]
  ranked <- candidates[order(delta[candidates])]
  half <- ceiling(n_plotting / 2)
  selected <- unique(c(head(ranked, half), tail(ranked, half)))

  grDevices::pdf(paste0(file_prefix, "Drift_correction_", format(Sys.time(), "%Y%m%d"), ".pdf"),
                 width = 16, height = 8)
  for (fname in selected) {
    tryCatch(print(dc_feature_plot(orig, dc, fname)),
             error = function(e) log_text(paste("Drift plot failed for", fname, ":", conditionMessage(e))))
  }
  grDevices::dev.off()

  open_png(paste0(file_prefix, "drift_cor_report.png"))
  tryCatch(print(dc_feature_plot(orig, dc, ranked[1])),
           error = function(e) log_text(paste("Drift report plot failed:", conditionMessage(e))))
  grDevices::dev.off()
  log_text(paste("Saved drift correction plots to", file_prefix))
}

# ---------------------------------------------------------------------------------------------
# Preprocessing
# ---------------------------------------------------------------------------------------------

non_samples <- c("Blank", "Standard")

se <- read_msdial(paste0(path, file_name), split = ion_mod)
log_text(paste("Read", nrow(se), "features and", ncol(se), "samples;",
               sum(se$QC == "QC"), "QC samples"))

# With "Class = Group_Time" names MS-DIAL also writes Parameter1/Parameter2; use them as group
# and facet when the second part actually varies.
group_col <- grouping_name
face_col <- NULL
if (all(c("Parameter1", "Parameter2") %in% colnames(colData(se)))) {
  p2 <- se$Parameter2[se$QC == "Sample"]
  if (length(unique(p2[!is.na(p2) & p2 != "NA"])) > 1) {
    group_col <- "Parameter1"
    face_col <- "Parameter2"
  }
}

se_samples <- se[, !se$QC %in% non_samples]
PCA_start <- pca_plot(se_samples, color = "Class", title = "Before preprocessing")
PCA_start_inj <- pca_plot(se_samples, color = "Injection_order", title = "Before preprocessing")
PCA_start_batch <- if ("Batch" %in% colnames(colData(se_samples)) &&
                       length(unique(se_samples$Batch)) > 1) {
  pca_plot(se_samples, color = "Batch", title = "Before preprocessing")
}

# Zero (and 1) abundances are missing values; flag features with a low detection rate.
se <- mark_nas(se, value = 0)
se <- mark_nas(se, value = 1)
colData(se)[[group_col]] <- factor(colData(se)[[group_col]])  # flag_detection wants a factor
se <- flag_detection(se, group = group_col)
colData(se)[[group_col]] <- as.character(colData(se)[[group_col]])

n_qc <- sum(se$QC == "QC")
PCA_after_drift <- NULL
PCA_after_drift_inj <- NULL
if (n_qc > 0) {
  if (n_qc >= 4) {
    orig <- assess_quality(se)
    se <- correct_drift_checked(orig)
    save_dc_plots(orig, se, file_prefix = path, n_plotting = 20)
    rm(orig)
  } else {
    log_text(paste("Only", n_qc, "QC samples (4 needed); preprocessing continues without drift correction."))
  }
  se <- flag_quality(se)
  se_no_blank <- se[, !se$QC %in% non_samples]
  PCA_after_drift <- pca_plot(se_no_blank, color = "Class", title = "After drift correction")
  PCA_after_drift_inj <- pca_plot(se_no_blank, color = "Injection_order", title = "After drift correction")
} else {
  log_text("No QC samples: drift correction and quality flagging were skipped.")
}

# Imputation of the biological samples (QCs, blanks and standards removed).
samples <- drop_qcs(se)
samples <- samples[, !samples$QC %in% non_samples]
set.seed(1234567)
if (any(is.na(flag(samples)))) {
  imputed <- impute_rf(samples)
} else {
  imputed <- samples
}
imputed <- impute_simple(imputed, value = 1, na_limit = 0.8)
set.seed(1234567)
imputed <- impute_rf(imputed, all_features = TRUE)

PCA_end <- pca_plot(imputed, color = "Class", title = "After preprocessing")

open_png(paste0(path, "PCA_for_report.png"))
print(if (!is.null(PCA_after_drift)) PCA_after_drift else PCA_end)
grDevices::dev.off()

grDevices::pdf(file = paste0(path, "PCA.pdf"), width = 16, height = 8)
if (!is.null(PCA_start_batch)) print(PCA_start_batch)
print(PCA_start)
print(PCA_start_inj)
if (!is.null(PCA_after_drift)) print(PCA_after_drift)
if (!is.null(PCA_after_drift_inj)) print(PCA_after_drift_inj)
if ("Batch" %in% colnames(colData(imputed)) && length(unique(imputed$Batch)) > 1) {
  print(pca_plot(imputed, color = "Batch", title = "After preprocessing"))
}
print(PCA_end)
print(pca_plot(imputed, color = "Injection_order", title = "After preprocessing"))
grDevices::dev.off()

log_text("Saving the preprocessed data")
saveRDS(imputed, file = paste0(path, "full_data.RDS"))
write_to_excel(imputed, file = paste0(path, "full_data.xlsx"))

# ---------------------------------------------------------------------------------------------
# MUVR: features that best separate the classes, with box plots
# ---------------------------------------------------------------------------------------------

sigFunc <- function(x) {
  if (is.na(x)) NA
  else if (x < 0.001) "***"
  else if (x < 0.01) "**"
  else if (x < 0.05) "*"
  else NA
}

nutri_color <- c("#009FB8", "#BB0A21", "#9063CD", "#F6AE2D", "#3BC62F", "#DC7FA1", "#09715E",
                 "#747CC9", "#CB613F", "#65A74F", "#B1953F", "#B65CBF", "#48B1A7", "#C8577B")

feature_title <- function(object, feat) {
  rd <- rowData(object)
  name_col <- grep("^metabolite[ _.]?name$", colnames(rd), ignore.case = TRUE, value = TRUE)[1]
  nm <- if (!is.na(name_col)) as.character(rd[feat, name_col]) else NA
  if (!is.na(nm) && nzchar(nm) && !tolower(nm) %in% c("unknown", "null")) {
    paste0(nm, "\nFeat: ", feat)
  } else {
    feat
  }
}

pairwise <- function(levels) {
  if (length(levels) < 2) return(NULL)
  comp <- t(utils::combn(levels, 2))
  split(comp, seq_len(nrow(comp)))
}

add_signif <- function(p, comp) {
  if (is.null(comp)) return(p)
  p + ggsignif::geom_signif(comparisons = comp, map_signif_level = sigFunc,
                            margin_top = seq(-0.1, 0.1 * length(comp), length.out = length(comp)),
                            vjust = 0.6, na.rm = TRUE)
}

notame_boxplot <- function(object, feat, separate, face = NULL) {
  df <- data.frame(value = as.numeric(assay(object)[feat, ]),
                   sep = factor(colData(object)[[separate]]))
  if (!is.null(face)) df$face <- colData(object)[[face]]
  p <- ggplot(df, aes(x = sep, y = value, fill = sep)) +
    geom_boxplot(outlier.shape = NA) +
    geom_jitter(width = 0.15, size = 1, show.legend = FALSE) +
    theme_bw() +
    theme(axis.text.x = element_text(angle = 90), axis.title = element_blank(),
          plot.title = element_text(hjust = 0.5)) +
    labs(title = feature_title(object, feat), fill = separate)
  if (nlevels(df$sep) <= length(nutri_color)) p <- p + scale_fill_manual(values = nutri_color)
  p <- add_signif(p, pairwise(levels(df$sep)))
  if (!is.null(face)) p <- p + facet_wrap(~face, scales = "free_y")
  p
}

notame_repeated_boxes <- function(file_path, object, separate, face = NULL,
                                  file_name = "MUVR_plot.pdf") {
  grDevices::pdf(file = paste0(file_path, file_name))
  for (feat in rownames(object)) {
    tryCatch(print(notame_boxplot(object, feat, separate, face)),
             error = function(e) log_text(paste("Box plot failed for", feat, ":", conditionMessage(e))))
  }
  grDevices::dev.off()
}

plot_boxes_notame <- function(object, separate, ppath) {
  feats <- rownames(object)
  long <- data.frame(feature = factor(rep(feats, times = ncol(object)), levels = feats),
                     value = as.vector(assay(object)[feats, , drop = FALSE]),
                     sep = factor(rep(colData(object)[[separate]], each = length(feats))))
  p <- ggplot(long, aes(x = sep, y = value)) +
    geom_boxplot() +
    facet_wrap(~feature, scales = "free") +
    theme_bw() +
    theme(axis.text.x = element_text(angle = 45, vjust = 1, hjust = 1), axis.title = element_blank())
  p <- add_signif(p, pairwise(levels(long$sep)))
  open_png(paste0(ppath, "box_plotdpi72.png"), width = 9, height = 9)
  print(p)
  grDevices::dev.off()
}

MUVR_run <- function(object, y, nRep = 5, nOuter = 6, nInner = nOuter - 1, varRatio = 0.75,
                     method = "RF", ...) {
  object <- drop_flagged(object)
  predictors <- as.data.frame(t(assay(object)))
  outcome <- factor(colData(object)[[y]])
  MUVR::MUVR(X = predictors, Y = outcome, nRep = nRep, nOuter = nOuter, nInner = nInner,
             varRatio = varRatio, method = method, ...)
}

ppath <- path
fig_comp <- TRUE
saveresults <- TRUE
report <- TRUE
set.seed(1234567)

# Runs MUVR on the classes, writes the box plots and MUVR_full_data.*, and returns the object with
# the VIP ranks added.
run_muvr <- function(metaboset) {
  num_outer <- min(6, min(table(metaboset$Class)))
  cl <- parallel::makeCluster(n_workers)
  doParallel::registerDoParallel(cl)
  MUVR_res <- tryCatch(
    MUVR_run(object = metaboset, y = "Class", method = "RF", nOuter = num_outer,
             nInner = num_outer - 1),
    finally = {
      parallel::stopCluster(cl)
      foreach::registerDoSEQ()
    })

  MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "max")
  if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "mid")
  }
  if (nrow(MUVR_vip) > 20 && nrow(MUVR::getVIP(MVObj = MUVR_res, model = "min")) > 10) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "min")
  }
  if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR_vip[1:20, ]
  }
  selected <- as.character(MUVR_vip$name)
  metaboset_figure <- metaboset[selected, ]
  box_separate <- if (!is.null(face_col)) group_col else "Class"
  notame_repeated_boxes(file_path = ppath, object = metaboset_figure,
                        separate = box_separate, face = face_col)
  plot_boxes_notame(metaboset[head(selected, 12), ], separate = box_separate, ppath = ppath)

  vip <- sort(MUVR_res$VIP[, "max"])
  VIPs <- data.frame(Feature_ID = names(vip), VIP_score_MUVR = unname(vip), stringsAsFactors = FALSE)
  metaboset <- notame::join_rowData(metaboset, VIPs)
  if (saveresults) {
    saveRDS(metaboset, file = paste0(ppath, "MUVR_full_data.RDS"))
    write_to_excel(metaboset, file = paste0(ppath, "MUVR_full_data.xlsx"))
  }
  metaboset
}

metaboset <- readRDS(file = paste0(ppath, "full_data.RDS"))
class_counts <- table(metaboset$Class)
n_unflagged <- sum(is.na(flag(metaboset)))

# MUVR's nested cross-validation needs at least three samples in every class.
if (length(class_counts) < 2 || min(class_counts) < 3) {
  log_text("MUVR was not performed: it needs two or more classes with at least three samples each.")
} else if (n_unflagged < 2) {
  log_text("MUVR was not performed: fewer than two features passed the quality flags.")
} else {
  # The preprocessed data is already saved; a failing MUVR only leaves out its outputs.
  metaboset <- tryCatch(run_muvr(metaboset), error = function(e) {
    log_text(paste("MUVR failed:", conditionMessage(e)))
    metaboset
  })
}

# ---------------------------------------------------------------------------------------------
# Summary text for the report
# ---------------------------------------------------------------------------------------------

print_levels_temp <- function(v) {
  t_groups <- table(v)
  paste("  ", paste0(names(t_groups), ": ", as.integer(t_groups), collapse = ", "), "\n")
}

metaboset_printer_temp <- function(x) {
  temp <- paste("The preprocessed data has", nrow(x), "features and", ncol(x), "samples.\n")
  temp <- paste(temp, sum(is.na(flag(x))), "non-flagged features,",
                sum(!is.na(flag(x))), "flagged features.\n\n")
  temp <- paste0(temp, "Class:\n", print_levels_temp(x$Class))
  if (!is.null(face_col)) {
    temp <- paste0(temp, group_col, ":\n", print_levels_temp(colData(x)[[group_col]]))
    temp <- paste0(temp, face_col, ":\n", print_levels_temp(colData(x)[[face_col]]))
  }
  temp <- paste0(temp, "\nThe object has the following parts (splits):\n")
  t_splits <- table(rowData(x)$Split)
  for (split in names(t_splits)) {
    temp <- paste0(temp, "  ", split, ": ", t_splits[[split]], " features\n")
  }
  paste0(temp, "\n")
}
desc_metaboset <- metaboset_printer_temp(metaboset)

log_text("notame workflow finished")
invisible(futile.logger::flog.appender(futile.logger::appender.console(), name = "notame"))
