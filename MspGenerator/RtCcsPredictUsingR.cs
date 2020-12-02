using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NCDK;
using NCDK.IO;
using NCDK.IO.Listener;
using NCDK.QSAR.Descriptors.Moleculars;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;
using RDotNet;

namespace CompMs.MspGenerator
{
    public class RtCcsPredictOnR
    {

         public static void runRTPredict(string workingDirectry, string rPath, string rScriptAvdModelPath, string rtModelingRdsFile)
        {
            var outputFolderPath = workingDirectry;

            rScriptAvdModelPath = rScriptAvdModelPath.Replace("\\", "/");
            outputFolderPath = outputFolderPath.Replace("\\", "/");
            rtModelingRdsFile = rtModelingRdsFile.Replace("\\", "/");

            REngine.SetEnvironmentVariables(rPath);


            using (REngine r = REngine.GetInstance())
            {
                r.Initialize();

                //Set the working directory
                r.Evaluate("setwd(\"" + rScriptAvdModelPath + "\")");

                // from Xgb_RTprediction
                //#Load libraries
                //r.Evaluate("library(Retip)");
                r.Evaluate("library(tidyverse)");
                r.Evaluate("library(xgboost)");
                //r.Evaluate("prep.wizard()");
                //# import training set table
                r.Evaluate("library(readxl)");
                r.Evaluate("lipidomics_edit <- read_csv(\"masterRT.csv\")");
                //# Clean dataset from NA and low variance value
                r.Evaluate("db_rt_padel <- data.frame(lipidomics_edit[,-c(1:3)])");

                r.Evaluate("xgb_padel <- readRDS(\"" + rtModelingRdsFile + "\")");

                //# Predict over LIPIDOMICS DATABASE
                //# import your database
                r.Evaluate("setwd(\"" + outputFolderPath + "\")");
                r.Evaluate("files <- list.files(pattern=\"RTpred*\", recursive=FALSE)");
                r.Evaluate(@"
                    rtPredict <- function(x) {

                    lipidomics_db <- read.csv(x)

                    target <- lipidomics_db

                    target_desc_f <- target[names(target) %in% names(db_rt_padel)]

                    target_db <- data.frame(target[, c(1)], target_desc_f)

                    target_db <- target_db[stats::complete.cases(target_db), ]

                    target_head <- target_db[, c(1)]

                    target_desc <- target_db[, -c(1)]

                    ncol1 <- ncol(target_desc)

                    pred1_target <- stats::predict(xgb_padel, target_desc[, 1:ncol1])

                    pred2_target <- data.frame(pred1_target)

                    pred_target <- data.frame(target_head, round((pred2_target), 2))

                    colnames(pred_target)[1] <- ""RTP""

                    exportName <- paste(substr(basename(x), 1, nchar(basename(x)) - 4), ""txt"", sep = ""."")

                    write.table(pred_target, exportName, quote = F, col.names = F, append = F)

                    }
                ");

                r.Evaluate("lapply(files, function(x) { rtPredict(x) })");

            }

        }
        public static void runCcsPredict(string workingDirectry, string rPath, string rScriptAvdModelPath, string cssModelingRdsFile)
        {
            var outputFolderPath = workingDirectry;

            rScriptAvdModelPath = rScriptAvdModelPath.Replace("\\", "/");
            outputFolderPath = outputFolderPath.Replace("\\", "/");
            cssModelingRdsFile = cssModelingRdsFile.Replace("\\", "/");

            REngine.SetEnvironmentVariables(rPath);

            using (REngine r = REngine.GetInstance())
            
                {
                    r.Initialize();

                //Set the working directory
                r.Evaluate("setwd(\"" + rScriptAvdModelPath + "\")");

                ////#Load libraries
                //r.Evaluate("library(Retip)");
                r.Evaluate("library(tidyverse)");

                r.Evaluate("library(xgboost)");
                //r.Evaluate("prep.wizard()");
                //# import training set table
                r.Evaluate("library(readxl)");

                r.Evaluate("CCS_All <- read_csv(\"masterCCS.csv\")");
                r.Evaluate("db_CCS <- data.frame(CCS_All[,-c(1:3)])");

                // load RDS
                r.Evaluate("xgb_ccs_final <- readRDS(\"" + cssModelingRdsFile + "\")");

                //# you need to import files without column CCS
                r.Evaluate("setwd(\"" + outputFolderPath + "\")");
                //# RT.spell.ccs
                r.Evaluate("model <- xgb_ccs_final");

                r.Evaluate("files <- list.files(pattern=\"CCSpred*\", recursive=FALSE)");
                r.Evaluate("files2 <- subset(files,grepl(\"csv\",files))");


                r.Evaluate(@"
                  ccsPredict <- function(x) {
                  target <- read.csv(x)
                  target_desc_f <- target[names(target) %in% names(db_CCS)]
                  target_db <- data.frame(target[,c(1)],target_desc_f)
                  target_db <- target_db [stats::complete.cases(target_db), ]
                  target_head <- target_db[,c(1)]
                  target_desc <- target_db[,-c(1)]
                  ncol1 <- ncol(target_desc)
                  pred1_target <- stats::predict(model, target_desc[,1:ncol1])
                  pred2_target <- data.frame(pred1_target)
                  pred_target <- data.frame(target_head,round((pred2_target),2))
                  colnames(pred_target)[1] <- ""CCSP""

                  exportName <- paste(substr(basename(x), 1, nchar(basename(x)) - 4),""txt"",sep = ""."")  
                  write.table(pred_target, exportName, quote = F, col.names = F, append = F)
                }

                    ");

                r.Evaluate("lapply(files2, function(x) { ccsPredict(x) })");

            }
        }

        public static void runPredict(string workingDirectry, string rPath, string rScriptAvdModelPath, string rtModelingRdsFile, string cssModelingRdsFile)
        {
            var outputFolderPath = workingDirectry;

            rScriptAvdModelPath = rScriptAvdModelPath.Replace("\\", "/");
            outputFolderPath = outputFolderPath.Replace("\\", "/");
            rtModelingRdsFile = rtModelingRdsFile.Replace("\\", "/");
            cssModelingRdsFile = cssModelingRdsFile.Replace("\\", "/");
            //xgbRTprediction = rScriptAvdModelPath + xgbRTprediction;

            REngine.SetEnvironmentVariables(rPath);

            using (REngine r = GetInitiazedREngine())
            {
                r.Initialize();

                //Set the working directory
                r.Evaluate("setwd(\"" + rScriptAvdModelPath + "\")");

                // from Xgb_RTprediction
                //#Load libraries
                //r.Evaluate("library(Retip)");
                r.Evaluate("library(tidyverse)");
                r.Evaluate("library(xgboost)");
                //r.Evaluate("prep.wizard()");
                //# import training set table
                r.Evaluate("library(readxl)");
                r.Evaluate("lipidomics_edit <- read_csv(\"masterRT.csv\")");
                //# Clean dataset from NA and low variance value
                r.Evaluate("db_rt_padel <- data.frame(lipidomics_edit[,-c(1:3)])");

                r.Evaluate("xgb_padel <- readRDS(\"" + rtModelingRdsFile + "\")");

                //# Predict over LIPIDOMICS DATABASE
                //# import your database
                r.Evaluate("setwd(\"" + outputFolderPath + "\")");
                r.Evaluate("files <- list.files(pattern=\"RTpred*\", recursive=FALSE)");
                r.Evaluate("files2 <- subset(files,grepl(\"csv\",files))");
                r.Evaluate(@"
                    rtPredict <- function(x) {
                    print(x)
                    lipidomics_db <- read.csv(x)
                    target <- lipidomics_db
                    target_desc_f <- target[names(target) %in% names(db_rt_padel)]
                    target_db <- data.frame(target[, c(1)], target_desc_f)
                    target_db <- target_db[stats::complete.cases(target_db), ]
                    target_head <- target_db[, c(1)]
                    target_desc <- target_db[, -c(1)]
                    ncol1 <- ncol(target_desc)
                    pred1_target <- stats::predict(xgb_padel, target_desc[, 1:ncol1])
                    pred2_target <- data.frame(pred1_target)
                    pred_target <- data.frame(target_head, round((pred2_target), 2))
                    colnames(pred_target)[1] <- ""RTP""
                    exportName <- paste(substr(basename(x), 1, nchar(basename(x)) - 4), ""txt"", sep = ""."")
                    write.table(pred_target, exportName, quote = F, col.names = F, append = F)
                    }
                ");

                r.Evaluate("lapply(files2, function(x) { rtPredict(x) })");
                r.Initialize();

                //Set the working directory
                r.Evaluate("setwd(\"" + rScriptAvdModelPath + "\")");

                //r.Evaluate("source(\"fuctions_import.R\")");
                ////#Load libraries
                //r.Evaluate("library(Retip)");
                r.Evaluate("library(xgboost)");
                //r.Evaluate("prep.wizard()");
                //# import training set table
                r.Evaluate("library(readxl)");

                r.Evaluate("CCS_All <- read_csv(\"masterCCS.csv\")");
                r.Evaluate("db_CCS <- data.frame(CCS_All[,-c(1:3)])");

                // load RDS
                r.Evaluate("xgb_ccs_final <- readRDS(\"" + cssModelingRdsFile + "\")");

                //# you need to import files without column CCS
                r.Evaluate("setwd(\"" + outputFolderPath + "\")");
                //# RT.spell.ccs
                r.Evaluate("model <- xgb_ccs_final");

                r.Evaluate("files <- list.files(pattern=\"CCSpred*\", recursive=FALSE)");
                r.Evaluate("files2 <- subset(files,grepl(\"csv\",files))");

                r.Evaluate(@"
                  ccsPredict <- function(x) {
                  target <- read.csv(x)
                  print(x)
                  target_desc_f <- target[names(target) %in% names(db_CCS)]
                  target_db <- data.frame(target[,c(1)],target_desc_f)
                  target_db <- target_db [stats::complete.cases(target_db), ]
                  target_head <- target_db[,c(1)]
                  target_desc <- target_db[,-c(1)]
                  ncol1 <- ncol(target_desc)
                  pred1_target <- stats::predict(model, target_desc[,1:ncol1])
                  pred2_target <- data.frame(pred1_target)
                  pred_target <- data.frame(target_head,round((pred2_target),2))
                  colnames(pred_target)[1] <- ""CCSP""

                  exportName <- paste(substr(basename(x), 1, nchar(basename(x)) - 4),""txt"",sep = ""."")  
                  write.table(pred_target, exportName, quote = F, col.names = F, append = F)
                }

                    ");

                r.Evaluate("lapply(files2, function(x) { ccsPredict(x) })");

            }
        }


        public static void generatePredictModel(string workingDirectry, string rPath, string rScriptAvdModelPath)
        {
            var outputFolderPath = workingDirectry + @"\result\";

            rScriptAvdModelPath = rScriptAvdModelPath.Replace("\\", "/");
            outputFolderPath = outputFolderPath.Replace("\\", "/");
            var rPath1 = rPath.Replace("\\", "/");

            REngine.SetEnvironmentVariables(rPath1);

            using (REngine r = REngine.GetInstance())
            {
                r.Initialize();

                //Set the working directory
                r.Evaluate("setwd(\"" + rScriptAvdModelPath + "\")");

                /// from Xgb_RTprediction
                //#Load libraries
                r.Evaluate("library(Retip)");
                r.Evaluate("library(tidyverse)");
                r.Evaluate("library(xgboost)");
                r.Evaluate("prep.wizard()");
                //# import training set table
                r.Evaluate("library(readxl)");
                r.Evaluate("lipidomics_edit <- read_csv(\"masterRT.csv\")");
                //# Clean dataset from NA and low variance value
                r.Evaluate("db_rt_padel <- data.frame(lipidomics_edit[,-c(1:3)])");
                ////# Train Models
                r.Evaluate("xgb_padel <- fit.xgboost(db_rt_padel)");
                ////#Save RDS
                r.Evaluate("currentdate <- Sys.Date()");
                r.Evaluate("saveRDS(xgb_padel, paste(\"./xgb_padel_evaluation_RT_\", currentdate, \".rds\", sep=\"\"))");

                /// CCSprediction
                //# import training set table
                //r.Evaluate("source(\"fuctions_import.R\")"); //not use
                r.Evaluate(@"
                        fit.xgboost.ccs <- function(x){

                        cv.ctrl <- caret::trainControl(method = ""cv"",number = 10)

                        xgb.grid <- base::expand.grid(nrounds = c(100, 200, 300, 400, 500, 600, 700),
                                  max_depth = c(5),
                                  eta = c(0.025, 0.05),
                                  gamma = c(0.01),
                                  colsample_bytree = c(0.75),
                                  subsample = c(0.50),
                                  min_child_weight = c(0))

                        print(""Computing model Xgboost... Please wait..."")

                          set.seed(101)
                          model_xgb <-caret::train(CCS ~.,
                            data=x,
                            method=""xgbTree"",
                            metric = ""RMSE"",
                            trControl=cv.ctrl,
                            tuneGrid=xgb.grid,
                            tuneLength = 14)
                              print(""End training"")
                                      return (model_xgb)
                        }
                ");//"model_xgboost_ccs.R"

                r.Evaluate(@"
                    p.model.ccs <- function(t,m,title=""title""){
 
                      t1 <- t
                      ncolt1 <- ncol(t1)
  
                      # CCS prediction on test dataframe
                      prd <- stats::predict(m,t1[,2:ncolt1])
                      prd <- data.frame(prd)
                      names(prd) <- c(""CCSP"")
  
  
                      x <- t$CCS
                      y <- prd$CCSP
                      res_rf_fh <- data.frame(round((caret::postResample(x, y)),2))
                      names(res_rf_fh) <- c(""Stats"")
                      # plot the line graphic
                      graphics::plot(t$CCS, prd$CCSP,
                                     xlab=""Observed CCS"", ylab=""Predicted CCS"",
                                     pch=19, xlim=c(100, 400), ylim=c(100, 400), main =paste0(""Predicted CCS vs Real - "", title))
                      graphics::abline(0,1, col='red')
                      graphics::legend('topleft',inset=.05, text.font=4,ncol = 2, title = 'Stats',legend = c(""RMSE"", ""R2"", ""MAE"",res_rf_fh$Stats))
  
                      # plotting histograms
                      df <- data.frame(t$CCS - prd$CCSP)
                      names(df) <- c(""CCS_ERR"")
                      ggplot2::ggplot(df, ggplot2::aes(df$CCS_ERR)) +
                        ggplot2::geom_histogram(ggplot2::aes(y =..density..),
                                                breaks = seq(-20, 20, by = 1),
                                                colour = ""black"",
                                                fill = ""white"")+
                        ggplot2::labs(title = paste0(""Error distribution - "", title),x=""CCS_ERR"")+
                        ggplot2::scale_color_manual(""Legend"", values = c(""blue"",""red""))+
                        ggplot2::stat_function(fun = stats::pnorm, ggplot2::aes(colour = ""pnorm""), args = list(mean = mean(df$CCS_ERR), sd = stats::sd(df$CCS_ERR)))+
                        ggplot2::stat_function(fun = stats::dnorm, ggplot2::aes(colour = ""dnorm""), args = list(mean = mean(df$CCS_ERR), sd = stats::sd(df$CCS_ERR)))
  
                      }

                ");//"plot_model_ccs.R"

                r.Evaluate("CCS_All <- read_csv(\"masterCCS.csv\")");
                r.Evaluate("db_CCS <- data.frame(CCS_All[,-c(1:3)])");
                //# save or load model all except keras #
                r.Evaluate("xgb_ccs_final <- fit.xgboost.ccs(db_CCS)");
                r.Evaluate("p.model.ccs(db_CCS, m = xgb_ccs_final, title = \"RIKEN LIPIDOMICS-XGB\")");
                //# Save RDS
                r.Evaluate("saveRDS(xgb_ccs_final, paste(\"./xgb_padel_evaluation_CCS_\", currentdate, \".rds\", sep=\"\"))");

            }
        }
        public static REngine r = null;
        public static REngine GetInitiazedREngine()
        {
            if (r == null)
            {
                REngine.SetEnvironmentVariables();
                r = REngine.GetInstance();
                r.Initialize();
            }
            return r;
        }

    }
}
