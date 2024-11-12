# RHaikonen
# 20240429
# notame preprocessing, quality metrics, and MUVR selection
# Additional plotting with PCA and drift correction

library(notame)
library(doParallel)
library(dplyr)
library(openxlsx)
library(pcaMethods)

# Functions to import Ms-dial export
name_features <- function (feature_data) 
{
  cols <- find_mz_rt_cols(feature_data)
  mz_col <- cols$mz_col
  rt_col <- cols$rt_col
  round_mz <- as.numeric(feature_data[, mz_col]) %>% as.character() %>% 
    gsub("[.]", "_", .)
  round_rt <- as.numeric(feature_data[, rt_col]) %>% as.character() %>% 
    gsub("[.]", "_", .)
  feature_data$Feature_ID <- paste0(feature_data$Split, "_", 
                                    round_mz, "a", round_rt)
  if (anyDuplicated(feature_data$Feature_ID)) {
    duplicates <- paste0(feature_data$Feature_ID[duplicated(feature_data$Feature_ID)], 
                         collapse = ", ")
    stop(paste0("Could not create unique feature names from m/z and retention time columns. Duplicated values: ", 
                duplicates))
  }
  feature_data
}

find_mz_rt_cols <- function (feature_data) 
{
  # Find mass and retention time columns
  mz_tags <- c("mass", "m.?z$", "molecular.?weight", "average mz", "average.mz", "molecularweight",
               "molecular weight", "average_mz")
  rt_tags <- c("retention.?time", "^rt$", "(?=.*rt)(?=.*min)")
  mz_col <- NULL
  for (tag in mz_tags) {
    hits <- grepl(tag, tolower(colnames(feature_data)), perl = T)
    if (any(hits)) {
      mz_col <- colnames(feature_data)[which(hits)[1]]
      break
    }
  }
  rt_col <- NULL
  for (tag in rt_tags) {
    hits <- grepl(tag, tolower(colnames(feature_data)), perl = T)
    if (any(hits)) {
      rt_col <- colnames(feature_data)[which(hits)[1]]
      break
    }
  }
  if (is.null(mz_col)) {
    stop(paste0("No mass to charge ratio column found - should match one of:\n", 
                paste(mz_tags, collapse = ", "), " (not case-sensitive)"))
  }
  if (is.null(rt_col)) {
    stop(paste0("No retention time column found - should match one of:\n", 
                paste(rt_tags, collapse = ", "), " (not case-sensitive)"))
  }
  return(list(mz_col = mz_col, rt_col = rt_col))
}

# helper fuctions:
best_class <- function(x) {
  x <- type.convert(as.character(x), as.is = TRUE)
  if (class(x) == "numeric") {
    x <- x
  } else if (length(unique(x)) < length(x) / 4) {
    x <- as.factor(x)
  } else if (is.integer(x)) {
    x <- as.numeric(x)
  } else {
    x <- as.character(x)
  }
  x
}
best_classes <- function (x)
{
  as.data.frame(lapply(x, best_class), stringsAsFactors = FALSE)
}
correct_drift_temp <- function (object, log_transform = TRUE, spar = NULL, spar_lower = 0.5, 
                                spar_upper = 1.5, check_quality = FALSE, condition = "RSD_r < 0 & D_ratio_r < 0", 
                                plotting = FALSE, n_plotting = nrow(object), file = NULL, width = 16, height = 8, color = "QC", 
                                shape = NULL, color_scale = getOption("notame.color_scale_dis"), 
                                shape_scale = scale_shape_manual(values = c(15, 16))) 
{
  corrected_list <- dc_cubic_spline(object, log_transform = log_transform, 
                                    spar = spar, spar_lower = spar_lower, spar_upper = spar_upper)
  corrected <- corrected_list$object
  inspected <- inspect_dc_temp(orig = object, dc = corrected, check_quality = check_quality, 
                               condition = condition)
  if (plotting) {
    if (is.null(file)) {
      stop("File must be specified")
    }
    save_dc_plots_temp(orig = object, dc = inspected, predicted = corrected_list$predicted, 
                       file = file, log_transform = log_transform, width = width, 
                       height = height, color = color, shape = shape, color_scale = color_scale, 
                       shape_scale = shape_scale, n_plotting = n_plotting)
  }
  inspected
}

################################################################################
################################################################################

save_dc_plots_temp <- function (orig, dc, predicted, 
                                file, log_transform = TRUE, width = 16, height = 8, color = "QC", 
                                shape = color, color_scale = getOption("notame.color_scale_dis"), 
                                shape_scale = scale_shape_manual(values = c(15, 16)), n_plotting = n_plotting) 
{
  if (!requireNamespace("cowplot", quietly = TRUE)) {
    stop("Package \"cowplot\" needed for this function to work. Please install it.", 
         call. = FALSE)
  }
  dc_plot_helper <- function(data, fname, title = NULL) {
    tryCatch({
      p <- ggplot(mapping = aes_string(x = "Injection_order", 
                                       y = fname)) + theme_bw() +
        theme(panel.grid = element_blank(),
              legend.position = "bottom"
        ) + 
        color_scale + shape_scale + 
        labs(title = title)
      mean_qc <- finite_mean(data[data$QC == "QC", fname])
      sd_qc <- finite_sd(data[data$QC == "QC", fname])
      mean_sample <- finite_mean(data[data$QC != "QC", 
                                      fname])
      sd_sample <- finite_sd(data[data$QC != "QC", fname])
      y_intercepts <- sort(c(`-2 SD (Sample)` = mean_sample - 
                               2 * sd_sample, `-2 SD (QC)` = mean_qc - 2 * 
                               sd_qc, `+2 SD (QC)` = mean_qc + 2 * sd_qc, `+2 SD (Sample)` = mean_sample + 
                               2 * sd_sample))
      for (yint in y_intercepts) {
        p <- p + geom_hline(yintercept = yint, color = "grey", 
                            linetype = "dashed")
      }
      p + scale_y_continuous(sec.axis = sec_axis(~., breaks = y_intercepts, 
                                                 labels = names(y_intercepts))) + geom_point(data = data, 
                                                                                             mapping = aes_string(color = color, shape = shape))
    }, error = function(cond) {
      message(paste0("Problem with ", fname))
      message("Here's the original error message:")
      message(conditionMessage(cond))
      print(fname)
    })
  }
  # Discard ones with missing QC as they cant be plotted!
  orig <- orig[dc@featureData@data$DC_note != "Missing_QCS",]
  predicted <- predicted[dc@featureData@data$DC_note != "Missing_QCS",]
  dc <- dc[dc@featureData@data$DC_note != "Missing_QCS",]
  
  # also set some kind of threshold for abundance.
  temp <- apply(exprs(dc),1, finite_median)
  temp <- sort(temp, decreasing = T)
  temp <- temp[1:(length(temp)/3)]
  temp <- names(temp)
  
  orig <- orig[temp,]
  predicted <- predicted[temp,]
  dc <- dc[temp,]
  
  # select features based on n_plotting
  # half best and half worse corrections based on D_ratio
  
  temp <- fData(dc)
  # headilla ascendic
  sel_feat_1 <- head(temp[order(temp$DC_delta_D_ratio_r, decreasing = T),],
                     n = 1)$Feature_ID
  sel_feat <- head(temp[order(temp$DC_delta_D_ratio_r, decreasing = T),],
                   n = round(n_plotting/2))$Feature_ID
  # descendic
  sel_feat <- c(sel_feat, head(temp[order(temp$DC_delta_D_ratio_r, decreasing = F),],
                               n = round(n_plotting/2, digits = 0))$Feature_ID)
  orig <- orig[sel_feat,]
  predicted <- predicted[sel_feat,]
  dc <- dc[sel_feat,]
  
  orig_data_log <- combined_data(log(orig))
  dc_data_log <- combined_data(log(dc))
  orig_data <- combined_data(orig)
  dc_data <- combined_data(dc)
  predictions <- as.data.frame(t(predicted))
  predictions$Injection_order <- orig_data$Injection_order
  
  pdf(paste0(file, "Drift_correction_",format(Sys.time(), "%Y%m%d"), ".pdf"),
      width = width, height = height)
  for (fname in Biobase::featureNames(dc)) {
    p2 <- dc_plot_helper(data = dc_data, fname = fname, 
                         title = "After")
    if (log_transform) {
      p1 <- dc_plot_helper(data = orig_data, fname = fname, 
                           title = "Before")
      p3 <- dc_plot_helper(data = orig_data_log, fname = fname, 
                           title = "Drift correction in log space") + geom_line(data = predictions, 
                                                                                color = "grey")
      p4 <- dc_plot_helper(data = dc_data_log, fname = fname, 
                           title = "Corrected data in log space")
      tryCatch(expr = {
        p <- cowplot::plot_grid(p1, p3, p2, p4, nrow = 2)
      }, error = function(e) {
        message("Caught an error!")
        print(fname)
        print(e)
      })
    }
    else {
      p1 <- dc_plot_helper(data = orig_data, fname = fname, 
                           title = "Before (original values)") + geom_line(data = predictions, 
                                                                           color = "grey")
      tryCatch(expr = {
        p <- cowplot::plot_grid(p1, p2, nrow = 2)
      }, error = function(e) {
        message("Caught an error!")
        print(fname)
        print(e)
      })
    }
    plot(p) 
  }
  dev.off()
  
  Cairo::Cairo(file = paste0(file, "drift_cor_report.png"), unit = "in", dpi = 72, width = 7, 
               height = 7, type = "png", bg = "white")
  fname <- sel_feat_1
  p2 <- dc_plot_helper(data = dc_data, fname = fname, 
                       title = "After")
  if (log_transform) {
    p1 <- dc_plot_helper(data = orig_data, fname = fname, 
                         title = "Before")
    p3 <- dc_plot_helper(data = orig_data_log, fname = fname, 
                         title = "Drift correction in log space") + geom_line(data = predictions, 
                                                                              color = "grey")
    p4 <- dc_plot_helper(data = dc_data_log, fname = fname, 
                         title = "Corrected data in log space")
    tryCatch(expr = {
      p <- cowplot::plot_grid(p1, p3, p2, p4, nrow = 2)
    }, error = function(e) {
      message("Caught an error!")
      print(fname)
      print(e)
    })
  }
  else {
    p1 <- dc_plot_helper(data = orig_data, fname = fname, 
                         title = "Before (original values)") + geom_line(data = predictions, 
                                                                         color = "grey")
    tryCatch(expr = {
      p <- cowplot::plot_grid(p1, p2, nrow = 2)
    }, error = function(e) {
      message("Caught an error!")
      print(fname)
      print(e)
    })
  }
  plot(p) 
  dev.off()
  log_text(paste("\nSaved drift correction plots to:", file))
}

###############################################################################
###############################################################################

inspect_dc_temp <- function (orig, dc, check_quality, condition = "RSD_r < 0 & D_ratio_r < 0") 
{
  if (is.null(quality(orig))) {
    orig <- assess_quality(orig)
  }
  if (is.null(quality(dc))) {
    dc <- assess_quality(dc)
  }
  orig_data <- exprs(orig)
  dc_data <- exprs(dc)
  fnames <- featureNames(orig)
  qdiff <- quality(dc)[2:5] - quality(orig)[2:5]
  log_text(paste("Inspecting drift correction results", Sys.time()))
  inspected <- foreach::foreach(i = seq_len(nrow(orig_data)), 
                                .combine = comb_temp, .export = c("%>%", "qdiff")) %dopar% 
    {
      data <- orig_data[i, ]
      if (all(is.na(dc_data[i, ]))) {
        dc_note <- "Missing_QCS"
      }
      else if (any(dc_data[i, ] < 0, na.rm = TRUE)) {
        dc_note <- "Negative_DC"
      }
      else if (check_quality) {
        pass <- paste0("qdiff[i, ] %>% dplyr::filter(", 
                       condition, ") %>% nrow() %>% as.logical()") %>% 
          parse(text = .) %>% eval()
        if (!pass) {
          dc_note <- "Low_quality"
        }
        else {
          data <- dc_data[i, ]
          dc_note <- "Drift_corrected"
        }
      }
      else {
        data <- dc_data[i, ]
        dc_note <- "Drift_corrected"
      }
      list(data = matrix(data, nrow = 1, dimnames = list(fnames[i], 
                                                         names(data))), dc_notes = data.frame(Feature_ID = fnames[i], 
                                                                                              DC_note = dc_note, stringsAsFactors = FALSE))
    }
  exprs(dc) <- inspected$data
  dc <- assess_quality(dc)
  dc <- join_fData(dc, inspected$dc_notes)
  
  # Join delta values
  qdiff <- cbind(rownames(qdiff), qdiff)
  colnames(qdiff) <- c("Feature_ID", "DC_delta_RSD", "DC_delta_RSD_r", "DC_delta_D_ratio", "DC_delta_D_ratio_r")
  dc <- join_fData(dc, qdiff)
  
  log_text(paste("Drift correction results inspected at", 
                 Sys.time()))
  dc_note <- inspected$dc_notes$DC_note
  note_counts <- table(dc_note) %>% unname()
  note_percentage <- note_counts/sum(note_counts)
  note_percentage <- scales::percent(as.numeric(note_percentage))
  note_labels <- table(dc_note) %>% names()
  report <- paste(note_labels, note_percentage, sep = ": ", 
                  collapse = ",  ")
  log_text(paste0("\nDrift correction results inspected, report:\n", 
                  report))
  dc
}

comb_temp <- function (x, ...) 
{
  mapply(rbind, x, ..., SIMPLIFY = FALSE)
}

##############################################################

read_MSDIAL <- function(tablefile, ion_mod = ""){
  tbl <- read.delim(tablefile, header = F, quote = "")
  
  corner_row <- which(tbl[, 1] != "")[1]
  corner_column <- which(tbl[1, ] != "")[1]
  
  # Featuredata
  peakmetadata_cols <- tbl %>%
    dplyr::select(1:(corner_column)) %>%
    dplyr::slice(corner_row:dplyr::n())
  
  colnames(peakmetadata_cols) <- as.character(unlist(peakmetadata_cols[1,]))
  row_tbl <- peakmetadata_cols[-1,]
  
  if (ion_mod != ""){
    row_tbl$Split <- ion_mod
  } else {
    row_tbl$Split <- "Unknown"
  }
  
  row_tbl <- name_features(as.data.frame(row_tbl))
  
  row_tbl <- row_tbl %>% dplyr::select(Feature_ID, 
                                       Split, dplyr::everything()) %>%
    best_classes() %>%
    dplyr::mutate_if(is.factor, as.character)
  
  rownames(row_tbl) <- row_tbl$Feature_ID
  fData <- as.data.frame(row_tbl)
  
  # extract the sample information (pData) and expression table
  quantval_cols <- tbl %>% dplyr::select((corner_column+1):ncol(tbl))
  # select(all_of(corner_column))
  
  # pData
  pData <- as.data.frame(t(quantval_cols[c(1:corner_row),]))
  
  # colnames for pData
  temp <- t(tbl[1:corner_row,corner_column])
  temp <- ifelse(temp == "Type", "QC", temp)
  temp <- ifelse(temp == "File type", "QC", temp)
  
  temp[grepl("batch",temp, ignore.case = T)] <- "Batch"
  
  colnames(pData) <- temp # c("Class", "FileType","InjectionOrder", "BatchId", "SampleId")
  pData <- as.data.frame(pData)
  colnames(pData) <- gsub(" ", "_", colnames(pData))
  
  pData$Sample_ID <- pData$Class
  
  # mark blanks
  pData$QC[grepl("blank",pData$Sample_ID, ignore.case = T)]<-"Blank"
  pData$Sample_ID[pData$Sample_ID == "Blank"] <- paste0("Blank_", seq_len(sum(pData$Sample_ID == "Blank", na.rm = T)))
  
  # mark QC
  pData$QC[grepl("QC",pData$Sample_ID, ignore.case = T)] <- "QC"
  subset <- pData$Sample_ID == "QC" & !is.na(pData$Sample_ID)
  pData$Sample_ID[subset] <- paste0("QC_", seq_len(sum(pData$Sample_ID == "QC", na.rm = T)))
  # pData$Sample_ID[grepl("QC",pData$Sample_ID, ignore.case = T)] <- "QC"
  
  # If averages and stdev still occure this will remove those
  pData <- pData[!(pData$Batch %in% c("Average", "Stdev")),]
  len_pdata <- nrow(pData)
  
  pData$Sample_ID <- paste0(make.unique(pData$Sample_ID, sep = "_"))
  
  # final pData
  rownames(pData) <- pData$Sample_ID
  
  # Expression part
  quantval_tbl <- as.matrix(quantval_cols[-c(1:corner_row),c(1:len_pdata)] %>%
                              dplyr::mutate(across(where(is.character), as.numeric)))
  
  rownames(quantval_tbl) <- rownames(fData)
  colnames(quantval_tbl) <- rownames(pData)
  
  return(list(exprs = quantval_tbl, pheno_data = pData, feature_data = fData))
}

# set up path
path <- file.path(path, "/")
grouping_name <- "Class"

set.seed(1234567)

if (exists("ion_mod")){
  data <- read_MSDIAL(paste0(path, file_name), ion_mod = ion_mod)
} else {
  data <- read_MSDIAL(paste0(path, file_name))
} 

# # if data have more modes
# if(split_col =! ""){
#   data <- notame::read_from_excel(file = paste0(path, ""), sheet = 1, split_by = split_col)
# }

if (exists("data")) {
  # Replace spaces with underscores if necessary
  rownames(data$exprs) <- gsub(" ", "_", rownames(data$exprs))
  rownames(data$feature_data) <- gsub(" ", "_", rownames(data$feature_data))
  data$feature_data$Feature_ID <- gsub(" ", "_", data$feature_data$Feature_ID)
  
  # Works with one or more modes
  # Construct MetaboSet objects
  # (note: assumption is that the dataset already contains group information)
  
  if ("Parameter1" %in% colnames(data$pheno_data) & "Parameter2" %in% colnames(data$pheno_data)){
    metaboset <- notame::construct_metabosets(exprs = data$exprs, pheno_data = data$pheno_data,
                                              feature_data = data$feature_data,
                                              group_col = "Parameter1", time_col = "Parameter2", split_data = F)
  } else{
    metaboset <- notame::construct_metabosets(exprs = data$exprs, pheno_data = data$pheno_data,
                                              feature_data = data$feature_data,
                                              group_col = "Class", split_data = F)
  }
  
  # Make sure that injection order is numeric
  metaboset$Injection_order <- as.numeric(metaboset$Injection_order)
  
  # Plot PCA at the beginning and then after preprocessing at the end
  metaboset_temp <- metaboset[,!metaboset$QC %in% "Blank"]
  # metaboset_temp <- metaboset_temp[, metaboset_temp$QC != "QC"]
  
  PCA_start <- plot_pca(metaboset_temp, color = "Class", title = "Before preprocessing")
  PCA_start_inj <- plot_pca(metaboset_temp, color = "Injection_order", title = "Before preprocessing")
  
  # Preprocessing of ONE mode
  # (NB: visualizations disabled here for saving computational time,
  # Also, create a folder called figures in the working folder if you create visualizations)
  
  # First, set all zero abundances to NA and asses the detection rate and flag based on that
  metaboset <- notame::mark_nas(metaboset, value = 0)
  metaboset <- notame::mark_nas(metaboset, value = 1)
  metaboset <- notame::flag_detection(metaboset)
  
  # Then check that there is QC samples.
  # The whole process is dependent on QC samples
  if("QC" %in% metaboset$QC ){
    # Take several cores to speed up processing
    num_cores <- parallel::detectCores() - 5
    cl <- parallel::makeCluster(num_cores)
    doParallel::registerDoParallel(cl)
    
    # If lause. Jos alle 4 QC:ta
    len_qc <- ncol(metaboset[, metaboset$QC == "QC"])
    
    if (len_qc > 3){
      # Correct the possible drift
      corrected <- correct_drift_temp(metaboset, check_quality = T, log_transform = T,
                                      plotting = T, file = path,
                                      n_plotting = 20)
    } else{
      log_text(paste("\nNot enough QC samples. Only", len_qc, "QC samples and 4 needed."))
      log_text(paste("\nPreprocessing continues without drift correction."))
      corrected <- metaboset
    }
    
    # flag based on quality
    corrected <- notame::flag_quality(corrected)
    # visualizations(corrected, prefix = paste0(path, "figures/", name, "_CLEANED"))
    PCA_after_drift <- plot_pca(corrected, color = "Class", title = "After drift correction")
    PCA_after_drift_inj <- plot_pca(corrected, color = "Injection_order", title = "After drift correction")
    
    # Remove the QCs for imputation
    merged_no_qc <- notame::drop_qcs(corrected)
    merged_no_qc <- merged_no_qc[,!merged_no_qc$QC %in% "Blank"]
    
    #visualizations(merged_no_qc, prefix = paste0(path, "figures/FULL_NO_QC"))
    
    # Imputation
    # (note: may not be necessary especially if gap filling by compulsion was used in MS-DIAL)
    
    # Set seed number for reproducibility
    set.seed(1234567)
    imputed <- notame::impute_rf(merged_no_qc)
    # Impute features with almost all NAs with 1
    imputed <- impute_simple(imputed, value = 1, na_limit = 0.8)
    # Impute bad quality features
    set.seed(1234567)
    imputed <- impute_rf(imputed, all_features = TRUE)
    
    #Stop using several cores (releases them for other use)
    parallel::stopCluster(cl)
    
    PCA_end <- plot_pca(imputed, color = "Class", title = "After preprocessing")
    
    # Plot the PCA for report
    Cairo::Cairo(file = paste0(path,"PCA_for_report.png"), unit = "in", dpi = 72, width = 7, 
                 height = 7, type = "png", bg = "white")
    if(exists("PCA_after_drift")){
      plot(PCA_after_drift)
    } else {
      plot(PCA_end)
    }
    
    dev.off()
    
    pdf(file = paste0(path, "PCA.pdf"), width = 16, height = 8)
    if (length(unique(metaboset_temp$Batch))>1){
      plot(plot_pca(metaboset_temp, color = "Batch", title = "Before preprocessing"))
    }
    plot(PCA_start)
    plot(PCA_start_inj)
    plot(PCA_after_drift)
    plot(PCA_after_drift_inj)
    if (length(unique(imputed$Batch))>1){
      plot(plot_pca(imputed, color = "Batch", title = "After preprocessing"))
    }
    plot(PCA_end)
    plot(plot_pca(imputed, color = "Injection_order", title = "After preprocessing"))
    dev.off()
    
    # Save the merged and processed data
    # options to save in rds format or to excel
    print("Saving imputed files")
    saveRDS(imputed, file = paste0(path, "full_data.RDS"))
    write_to_excel(imputed, file = paste0(path, "full_data.xlsx"))
  } else {
    print("No QC samples so no drift correction nor imputation was done")
    
    print("Still saving the RDS and excel for further analysis.")
    saveRDS(metaboset, file = paste0(path, "full_data.RDS"))
    write_to_excel(metaboset, file = paste0(path, "full_data.xlsx"))
  }
} else {
  stop("Something went wrong in data reading.")
}

#########################################################################

# MUVR model to find most interesting ones
# Saves boxplot

library(notame)
library(doParallel)
library(dplyr)
library(MUVR)
library(tidyr)

notame_repeated_boxes <- function(file_path, file_name = "MUVR_plot.pdf", separate = group_col(object),
                                  face = NULL, object, comp = T, name_col = NULL){
  pdf(file = paste0(file_path, file_name))
  
  for (i in seq_len(nrow(object))) {
    if (i%%500 == 0) {
      cat(paste0("Iteration ", i, "/", nrow(object), "\n"))
    }
    
    feat_name <- featureNames(object)[i]
    p <- notame_boxplot(object = object, feat_name = feat_name, separate = separate,
                        face = face, comp = comp, name_col = name_col)
    print(p)
  }
  dev.off()
}
# functions to produce the figures
notame_boxplot <- function(object, feat_name, name_col = NULL,
                           separate = group_col(object),
                           face = NULL, comp = NULL, free_y = T){
  metabolite <- as.data.frame(t(exprs(object[fData(object)$Feature_ID == feat_name])))
  metabolite <- cbind(metabolite, pData(object)[,separate])
  
  # Add the group to facet
  if(!is.null(face)){
    metabolite <- cbind(metabolite, pData(object)[,face])
  }
  
  colnames(metabolite) <- c("metabol", "sep")
  # sep needs to be factor
  metabolite$sep <- as.factor(metabolite$sep)
  
  # give the name for facet group
  if(!is.null(face)){
    colnames(metabolite)[3] <- "face"
  }
  # add the title based on identification
  if (!is.null(name_col)){
    if (!is.na(fData(object)[feat_name, name_col])){
      otsikko <- fData(object)[feat_name, name_col]
    }
    else{
      otsikko <- feat_name
    }
  } else{
    if (object@featureData@data[feat_name,"Metabolite.name"] != "Unknown"){
      otsikko <- paste0(object@featureData@data[feat_name,"Metabolite.name"], "\nFeat: ", feat_name)
    }
    else{
      otsikko <- feat_name
    }
    
  }
  
  library(ggpubr)
  
  nutri_color <- c("#009FB8", "#BB0A21", "#9063CD", "#F6AE2D", "#3BC62F", "#DC7FA1", "#09715E",
                   "#747CC9", "#CB613F", "#65A74F", "#B1953F", "#B65CBF", "#48B1A7", "#C8577B")
  Tissue_plot1 <- ggpubr::ggboxplot(metabolite, x = "sep", y = "metabol",
                                    title = otsikko, fill = "sep", palette = nutri_color) +
    theme_bw() + 
    theme(axis.text.x = element_text(angle = 90),
          axis.title = element_blank(),
          plot.title = element_text(hjust = 0.5))
  
  if(!is.null(comp)){
    if(is.list(comp)){
      Tissue_plot1 <- Tissue_plot1 +
        geom_signif(comparisons = comp,
                    map_signif_level=sigFunc,
                    margin_top = seq(-0.1, 0.1*length(comp), length.out = length(comp))) +
        scale_y_continuous(expand = c(0.2,0))
    }
    else{
      comp <- t(combn(levels(metabolite$sep),2))
      comp <- split(comp, 1:nrow(comp))
      Tissue_plot1 <- Tissue_plot1 + 
        geom_signif(comparisons = comp,
                    map_signif_level=sigFunc,
                    margin_top = seq(-0.1, 0.1*length(comp), length.out = length(comp)),
                    vjust = 0.6)
    }
  }
  if(!is.null(face)){
    if(free_y){
      Tissue_plot1 + facet_wrap(.~face, scales = "free_y")
    }
    else{
      Tissue_plot1 + facet_wrap(.~face)
    }
    
  }
  else{
    Tissue_plot1
  }
}
# Help function to geom_signif
sigFunc = function(x){
  # browser()
  if(x < 0.001){"***"} 
  else if(x < 0.01){"**"}
  else if(x < 0.05){"*"}
  else{NA}}

plot_boxes_notame <- function(metaboset_figure, separate = "Class", ppath){
  temp <- combined_data(metaboset_figure)
  feat_names <- featureNames(metaboset_figure)
  # Make temp long
  # - data: Data object
  # - key: Name of new key column (made from names of data columns)
  # - value: Name of new value column
  data_long <- gather(data = temp, key = feature, value = abund, feat_names, factor_key=TRUE)
  
  comp <- t(combn(unique(metaboset_figure[[separate]]),2))
  comp <- split(comp, 1:nrow(comp))
  
  # metabolite in all metabolites
  p1 <- ggboxplot(data_long, x = separate, y = "abund", ggtheme = theme_bw()) +
    theme(axis.text.x = element_text(angle = 45, vjust = 1, hjust = 1))
  p2 <- facet(p = p1, facet.by = "feature", scales = "free") + 
    geom_signif(comparisons = comp,
                map_signif_level=sigFunc,
                margin_top = seq(-0.1, 0.1*length(comp), length.out = length(comp)),
                vjust = 0.6)
  
  dpi <- 72
  format <- "png"
  imgName <- "box_plot"
  imgName = paste0(ppath, imgName, "dpi", dpi, ".", format)
  Cairo::Cairo(file = imgName, unit = "in", dpi = 72, width = 9,
               height = 9, type = "png", bg = "white")
  plot(p2)
  dev.off()
}

# Add citations
add_cit <- function (name, ref) 
{
  cites <- getOption("notame.citations")
  if (!name %in% names(cites)) {
    cites[[name]] <- ref
    options(notame.citations = cites)
  }
}

# is numeric
looks_num <- function (x) 
{
  stopifnot(is.atomic(x) || is.list(x))
  num_nas <- sum(is.na(x))
  num_nas_new <- suppressWarnings(sum(is.na(as.numeric(x))))
  return(num_nas_new == num_nas)
}

# MUVR function
MUVR_run <- function (object, y = NULL, id = NULL, multi_level = FALSE, 
                      multi_level_var = NULL, covariates = NULL, static_covariates = NULL, 
                      all_features = FALSE, nRep = 5, nOuter = 6, nInner = nOuter - 
                        1, varRatio = 0.75, method = c("PLS", "RF"), ...) 
{
  add_cit("MUVR package was used to fit multivariate models with variable selection:", 
          citation("MUVR"))
  classes <- sapply(pData(object)[, c(covariates, static_covariates)], 
                    class)
  if (length(classes) && any(classes != "numeric")) {
    stop("MUVR can only deal with numeric inputs, please transform all covariates to numeric", 
         call. = FALSE)
  }
  object <- drop_flagged(object, all_features = all_features)
  if (any(!sapply(pData(object)[, covariates], looks_num))) {
    stop("All covariates should be convertable to numeric")
  }
  pData(object)[covariates] <- lapply(pData(object)[covariates], 
                                      as.numeric)
  if (!multi_level) {
    if (is.null(y)) {
      stop("y variable needs to be defined unless doing multi-level modeling")
    }
    predictors <- combined_data(object)[, c(featureNames(object), 
                                            covariates)]
    outcome <- pData(object)[, y]
    if (is.null(id)) {
      muvr_model <- MUVR::MUVR(X = predictors, Y = outcome, 
                               nRep = nRep, nOuter = nOuter, nInner = nInner, 
                               varRatio = varRatio, method = method, ...)
    }
    else {
      ID <- pData(object)[, id]
      muvr_model <- MUVR::MUVR(X = predictors, Y = outcome, 
                               ID = ID, nRep = nRep, nOuter = nOuter, nInner = nInner, 
                               varRatio = varRatio, method = method, ...)
    }
  }
  else {
    if (is.null(id) || is.null(multi_level_var)) {
      stop("id and multi_level_var needed for multi-level modeling")
    }
    ml_var <- pData(object)[, multi_level_var] <- as.factor(pData(object)[, 
                                                                          multi_level_var])
    if (length(levels(ml_var)) != 2) {
      stop("The multilevel variable should have exactly 2 unique values")
    }
    else {
      cat(paste("Computing effect matrix according to", 
                multi_level_var, ":", levels(ml_var)[2], "-", 
                levels(ml_var)[1]))
    }
    cd <- combined_data(object)
    cd <- cd[order(cd[, id]), ]
    x1 <- cd[cd[, multi_level_var] == levels(ml_var)[1], 
             c(featureNames(object), covariates)]
    x2 <- cd[cd[, multi_level_var] == levels(ml_var)[2], 
             c(featureNames(object), covariates)]
    predictors <- x2 - x1
    predictors[, static_covariates] <- cd[cd[, multi_level_var] == 
                                            levels(ml_var)[1], static_covariates]
    rownames(predictors) <- unique(cd[, id])
    if (!is.null(y)) {
      outcome <- cd[cd[, multi_level_var] == levels(ml_var)[1], 
                    y]
      muvr_model <- MUVR::MUVR(X = predictors, Y = outcome, 
                               nRep = nRep, nOuter = nOuter, nInner = nInner, 
                               varRatio = varRatio, method = method, ...)
    }
    else {
      muvr_model <- MUVR::MUVR(X = predictors, ML = TRUE, 
                               nRep = nRep, nOuter = nOuter, nInner = nInner, 
                               varRatio = varRatio, method = method, ...)
    }
  }
  muvr_model
}

###############################################
###############################################

ppath <- file.path(path, "/")
fig_comp <- TRUE
saveresults <- TRUE
report <- TRUE

set.seed(1234567)

# read the data
metaboset <- readRDS(file = paste0(ppath, "full_data.RDS"))

# Take several cores to speed up processing
num_cores <- parallel::detectCores() - 5
cl <- parallel::makeCluster(num_cores)
doParallel::registerDoParallel(cl)

if (min(table(metaboset$Class)) < 2) {
  num_outer <- 1
} else if (min(table(metaboset$Class)) < 6){
  num_outer <- min(table(metaboset$Class))
} else {
  num_outer <- 6
}

# if () # modify it so that it gives warnings that there is no enough classes!
#   # alternatively use parameters1 or two to do the MUVR

if (min(table(metaboset$Class)) < 2){
  log_text("\nNumber of Class is too low! MUVR can not be performed!")
} else {
  # Run the MUVR algorithm to take most interesting ones
  MUVR_res <- MUVR_run(object = metaboset, y = "Class",
                       method = "RF", nOuter = num_outer)
  parallel::stopCluster(cl)
  
  MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "max")
  
  if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "mid")
  }
  if (nrow(MUVR_vip) > 20 & nrow(getVIP(MVObj = MUVR_res, model = "min")) > 10 ) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "min")
  }
  if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR_vip[1:20,]
  }
  
  metaboset_figure <- metaboset[MUVR_vip$name,]
  
  colnames <- colnames(pData(metaboset))
  if ("Parameter1" %in% colnames & "Parameter2" %in% colnames){
    notame_repeated_boxes(file_path = ppath, object = metaboset_figure,
                          comp = fig_comp, separate = "Parameter1", face = "Parameter2")
    if(nrow(MUVR_vip) > 12){
      MUVR_vip <- MUVR_vip[1:12,]
      metaboset_figure <- metaboset[MUVR_vip$name,]
    }
    
    plot_boxes_notame(metaboset_figure, separate = "Parameter1", ppath = ppath)
    
  } else{
    notame_repeated_boxes(file_path = ppath, object = metaboset_figure, comp = fig_comp)
    if(nrow(MUVR_vip) > 12){
      MUVR_vip <- MUVR_vip[1:12,]
      metaboset_figure <- metaboset[MUVR_vip$name,]
    }
    plot_boxes_notame(metaboset_figure, ppath = ppath)
  }
  # Extract all VIP values and add to object
  # MUVR_vip_all <- MUVR::getVIP(MVObj = MUVR_res, model = "max")
  
  nVar <- drop_flagged(metaboset)
  nVar <- nrow(nVar)
  VIPs = sort(MUVR_res$VIP[, "max"])[1:nVar]
  VIPs = data.frame(order = 1:nVar, name = names(VIPs), rank = VIPs)
  VIPs$name = as.character(VIPs$name)
  
  VIPs <- VIPs[,c(2,3)]
  colnames(VIPs) <- c("Feature_ID", "VIP_score_MUVR")
  metaboset <- notame::join_fData(metaboset, VIPs)
  if (saveresults){
    saveRDS(metaboset, file = paste0(ppath, "MUVR_full_data.RDS"))
    write_to_excel(metaboset, file = paste0(ppath, "MUVR_full_data.xlsx"))
  }
}

#####
# functions for reporting
metaboset_printer_temp <- function(x) {
  temp <- paste("MetaboSet object with", nrow(x), "features and", ncol(x), "samples.\n")
  temp <- paste(temp, sum(x$QC == "QC"), "QC samples included\n")
  temp <- paste(temp,
                sum(is.na(flag(x))), "non-flagged features,",
                sum(!is.na(flag(x))), "flagged features.\n\n")
  
  if (!is.na(group_col(x))) {
    temp <- paste0(temp, group_col(x), ":\n")
    temp <- paste0(temp, print_levels_temp(pData(x)[, group_col(x)]))
  }
  if (!is.na(time_col(x))) {
    temp <- paste0(temp, time_col(x), ":\n")
    temp <- paste0(temp, print_levels_temp(pData(x)[, time_col(x)]))
  }
  if (!is.na(subject_col(x))) {
    temp <- paste0(subject_col(x), ":\n")
    subject <- as.character(pData(x)[, subject_col(x)])
    subject <- subject[!grepl("QC", subject)]
    temp <- paste0(temp,
                   "  ", length(unique(subject)), " distinct subjects\n  min:",
                   min(table(subject)), ", max:", max(table(subject)),
                   " observations per subject.\n"
    )
  }
  
  temp <- paste0(temp,"\nThe object has the following parts (splits):\n")
  splits <- unique(fData(x)$Split)
  t_splits <- table(fData(x)$Split)
  for (split in splits) {
    temp <- paste0(temp, "  ", split, ": ", t_splits[split], " features\n")
  }
  temp <- paste0(temp, "\n")
  return(temp)
}
print_levels_temp <- function(v) {
  t_groups <- table(v)
  if (is.factor(v)) {
    groups <- levels(v)
  } else {
    groups <- unique(v)
  }
  output <- sapply(groups, function(y) {
    obs <- t_groups[y]
    paste0(y, ": ", obs)
  })
  output <- paste(output, collapse = ", ")
  return(paste("  ", output, "\n"))
}
desc_metaboset <- metaboset_printer_temp(metaboset)

#####################################################