# RHaikonen
# 20240429
# MUVR model to find most interesting ones
# Saves boxplot
# (possibly returning the information to ms-dial so
# user can look features in "browser")

## Load libraries
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
  if (!requireNamespace("MUVR", quietly = TRUE)) {
    stop("Package \"MUVR\" needed for this function to work. Please install it from\n         https://gitlab.com/CarlBrunius/MUVR", 
         call. = FALSE)
  }
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
# Reporting
if(report){
  setwd(ppath)
  usrName <- "By Hanhineva Lab"
  file.create("Notame_Report.Rnw")
  rnwFile <- file("Notame_Report.Rnw", "w")
  # fig.count <<- 0
  # table.count <<- 0
  header <- c("\\documentclass[a4paper]{article}", "\\usepackage[margin=1.0in]{geometry}", 
              "\\usepackage{longtable}", "\\usepackage{graphicx}", 
              "\\usepackage{grffile}", "<<echo=false>>=", "options(width=60);", 
              "@", "\\SweaveOpts{eps=FALSE,pdf=TRUE}", "\\title{Notame preprocessing report}",
              paste("\\author{", usrName, "}", sep = ""),
              "\\begin{document}", "\\parskip=.3cm", "\\maketitle")
  cat(header, file = rnwFile, sep = "\n", append = TRUE)
  
  # Background information
  
  descr <- c("\\section{About notame}\n", 
             "Notame (NOn-TArgeted MEtabolomics) is an R package designed to preprocess",
             "LC-MS metabolomics data and provide quality metrics, statistics, and",
             "results visualizations. The package was originally developed by Anton",
             "Klåvus at the Kati Hanhineva lab, University of Eastern Finland, and",
             "it utilizes several other R packages, such as MUVR, to perform the",
             "data processing. The notame publication, inlcuding a description of a",
             "complete LC-MS workflow, is freely available.",
             "\\footnote{Klåvus A, Kokla M, Noerman S, et al. \\textit{“Notame”: Workflow for Non-Targeted LC–MS Metabolic Profiling},", 
             "Metabolites. 2020; 10(4):135.}\n")
  cat(descr, file = rnwFile, append = TRUE)
  
  # Drift correction
  descr <- c("\\section{Drift correction}\n",
             "Signal drift is a typical issue in LC-MS metabolomics, affecting sequences",
             "with 30 to 50 injections or more. It causes a gradual change in the signal",
             "intensity of the molecular features during the LC-MS analysis, which should",
             "be corrected for optimal data quality. Notame utilizes information from the",
             "QC samples to correct the drift. The QC samples are pooled from the biological",
             "samples and injected before and after the samples as well as after every",
             "10 to 12 samples in the sequence. Notame performs drift correction by fitting",
             "a smoothed cubic spline to the signal intensities of the QC samples,",
             "separately for each molecular feature. The smoothing function prevents",
             "overfitting the curve in case there are single deviating QC samples.\n",
             "Please note that at least four QC samples are needed in the dataset to",
             "perform this step. If your data has less, it is advisable to check the",
             "PCA instead, where the QC samples should be tightly clustered.\n")
  
  cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  if(file.exists("drift_cor_report.png")){
    fig <- c("\\begin{figure}[htp]", "\\begin{center}", paste("\\includegraphics[width=1.0\\textwidth]{", 
                                                              "drift_cor_report.png", "}", sep = ""), "\\caption{Most signigicant features}", 
             "\\end{center}", paste("\\label{", "drift_cor_report.png", 
                                    "}", sep = ""), "\\end{figure}", "\\clearpage\n\n")
    cat(fig, file = rnwFile, append = TRUE, sep = "\n")
  } else{
    descr <- "\\textbf{Selected peak file has less than 3 QC samples. Thus the drift correction was not made.}"
    cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  }
  # Quality metrics
  descr <- c("\\section{Quality metrics}\n",
             "The quality of each molecular feature is determined by calculating",
             "descriptive values according to the recommendations by Broadhurst et al.",
             "\\footnote{Broadhurst D, Goodacre R, Reinke SN et al.",
             "\\textit{Guidelines and considerations for the use of system suitability and quality control samples in mass spectrometry assays applied in untargeted clinical metabolomic studies.},", 
             "Metabolomics 14, 72 (2018).}",
             ". The following quality criteria should be met to consider the molecular",
             "feature of good quality: the detection rate in the QC samples is atleast",
             "70%, relative standard deviation (RSD) in the QC samples is less than 0.2",
             "(20%), and the D-ratio is less than 0.4. D-ratio or dispersion ratio is",
             "the sample standard deviation of the QC samples divided by the sample",
             "standard deviation of the biological samples. The output data matrix from",
             "Notame will include a column titled Flag that indicates whether the",
             "molecular feature was flagged for bad quality and the reason (low QC detection",
             "or low quality).\n",
             "The flag for low QC detection or low quality is not intended for automatically",
             "discarding all such peaks but rather be used as a warning signal to pay close",
             "attention to the peak (e.g., peak shape, peak area integration, signal-to-noise",
             "ratio) and to decide yourself whether the feature has sufficient quality for",
             "reporting. For example, in highly variable sample sets, a true metabolite that",
             "has detectable levels only in a few samples may dilute below the detection limit",
             "in the pooled QC samples and result in a flag for low QC detection.\n\n")
  
  cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  
  # descr <- c("<<echo=false, results=tex>>=", "print(metaboset)", 
  #            "@", "\n\n")
  # cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  cat(desc_metaboset, file = rnwFile, append = TRUE, sep = "\n")
  
  # PCA
  descr <- c("\\section{Overview of the data}\n",
             "PCA can give brief overview of the data.")
  cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  
  fig <- c("\\begin{figure}[htp]", "\\begin{center}", paste("\\includegraphics[width=1.0\\textwidth]{", 
                                                            "PCA_for_report.png", "}", sep = ""), "\\caption{PCA plot of classes in data}", 
           "\\end{center}", paste("\\label{", "PCA_for_report.png", 
                                  "}", sep = ""), "\\end{figure}", "\\clearpage\n\n")
  cat(fig, file = rnwFile, append = TRUE, sep = "\n")
  
  
  
  # MUVR
  descr <- c("\\section{Selection of differential molecular features with MUVR}\n",
             "MUVR (Multivariate methods with Unbiased Variable selection in R) is an",
             "algorithm developed by Lin Shi and Carl Brunius at the Chalmers University",
             "of Technology",
             "\\footnote{Shi L, Westerhuis JA, Rosén J, et al.",
             "\\textit{Variable selection and validation in multivariate modelling.},", 
             "Bioinformatics. 2019 Mar 15;35(6):972-980.}",
             ". It chooses a limited number of molecular features, in this case maximum",
             "50, that differ between the study groups (given in the Class column in",
             "MS-DIAL project). The algorithm uses a recursive variable selection procedure",
             "with partial least squares (PLS) and random forest (RF) modelling.\n",
             "The molecular features chosen by MUVR are accompanied with box plots and",
             "can be used as a starting point for manual curation of the data and finding",
             "the relevant metabolites for your study question.")
  
  cat(descr, file = rnwFile, append = TRUE, sep = "\n")
  
  fig <- c("\\begin{figure}[htp]", "\\begin{center}", paste("\\includegraphics[width=1.0\\textwidth]{", 
                                                            "box_plotdpi72.png", "}", sep = ""), "\\caption{12 most signigicant features}", 
           "\\end{center}", paste("\\label{", "box_plotdpi72.png", 
                                  "}", sep = ""), "\\end{figure}", "\\clearpage\n\n")
  cat(fig, file = rnwFile, append = TRUE, sep = "\n")
  
  
  # Finally close the file
  
  end <- c("\\vspace{5 mm}\n\n--------------------------------\n\n", 
           "The report was generated on \\Sexpr{date()} with \\Sexpr{print(version$version.string)}, OS system:", 
           "\\Sexpr{Sys.info()['sysname']}, version: \\Sexpr{gsub('#[0-9]+', '', Sys.info()['version'])} .\n", 
           "\\end{document}\n\n")
  cat(end, file = rnwFile, append = TRUE)
  
  close(rnwFile)
  
  
  utils::Sweave(file = "Notame_Report.Rnw", encoding = "utf8")
  res <- try(tools::texi2dvi("Notame_Report.tex", pdf = TRUE,
                             quiet = TRUE))
  
  
}

# END