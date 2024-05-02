# RHaikonen
# 20240429
# MUVR model to find most interesting ones

## Load libraries
library(notame)
library(doParallel)
library(dplyr)
library(MUVR)

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
    otsikko <- feat_name
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

ppath <- file.path(path, "/")
fig_comp <- TRUE
saveresults <- TRUE

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
  MUVR_res <- notame::muvr_analysis(object = metaboset, y = "Class",
                            method = "RF", nOuter = num_outer)
  parallel::stopCluster(cl)
  
  MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "max")
  
  if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "mid")
  } else if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR::getVIP(MVObj = MUVR_res, model = "min")
  } else if (nrow(MUVR_vip) > 20) {
    MUVR_vip <- MUVR_vip[1:20,]
  }
  
  metaboset_figure <- metaboset[MUVR_vip$name,]
  
  colnames <- colnames(pData(metaboset))
  if ("Parameter1" %in% colnames & "Parameter2" %in% colnames){
    notame_repeated_boxes(file_path = ppath, object = metaboset_figure,
                          comp = fig_comp, separate = "Parameter1", face = "Parameter2")
  } else{
    notame_repeated_boxes(file_path = ppath, object = metaboset_figure, comp = fig_comp)
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
# END