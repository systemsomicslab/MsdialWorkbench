# RHaikonen
# 20240429
# notame preprocessing and quality metrics
# Additional plotting with PCA and drift correction

# Load libraries
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
grouping_name <- "Class" # set the name of group that exist MS-dial

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
  
  # take the parameters and use them
  
  # num_params <- sum(stringr::str_count(string = colnames(data$pheno_data), pattern = "Parameter"))
  
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
  # remove the hashtags to enable them.
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
    # Needs missForest package for random forest imputation
    
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