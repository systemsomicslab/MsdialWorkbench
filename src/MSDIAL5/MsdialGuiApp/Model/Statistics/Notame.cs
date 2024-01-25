using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.Model.Export;
using CompMs.CommonMVVM;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using RDotNet;
using Reactive.Bindings.Notifiers;
using System;
using System.Threading.Tasks;
using System.Threading;
using System.IO;

namespace CompMs.App.Msdial.Model.Statistics
{
    internal sealed class Notame : BindableBase
    {
        private readonly DataExportBaseParameter _dataExportParameter;

        public Notame(IAlignmentResultExportModel exportGroup, AlignmentFilesForExport alignmentFilesForExport, AlignmentPeakSpotSupplyer peakSpotSupplyer, DataExportBaseParameter dataExportParameter) {
            AlignmentFilesForExport = alignmentFilesForExport;
            PeakSpotSupplyer = peakSpotSupplyer ?? throw new ArgumentNullException(nameof(peakSpotSupplyer));
            _dataExportParameter = dataExportParameter;
            Group = exportGroup;
            ExportDirectory = dataExportParameter.ExportFolderPath;
        }

        public string ExportDirectory {
            get => _exportDirectory;
            set => SetProperty(ref _exportDirectory, value);
        }
        private string _exportDirectory;
        
        public AlignmentFilesForExport AlignmentFilesForExport { get; }
        public AlignmentPeakSpotSupplyer PeakSpotSupplyer { get; }
        public IAlignmentResultExportModel Group { get; }

        public Task ExportAlignmentResultAsync(IMessageBroker broker) {
            return Task.Run(() => {
                var publisher = new TaskProgressPublisher(broker, $"Exporting {AlignmentFilesForExport.SelectedFile.FileName}");
                using (publisher.Start()) {
                var numExportFile = (double)Group.CountExportFiles(AlignmentFilesForExport.SelectedFile);
                var count = 0;
                void notify(string file) {
                        publisher.Progress(Interlocked.Increment(ref count) / numExportFile, file);
                }
                Group.Export(AlignmentFilesForExport.SelectedFile, ExportDirectory, notify);
                _dataExportParameter.ExportFolderPath = ExportDirectory;
                }
            });
        }
        
        public string GetFileName(string fileName) {
            var files = new DirectoryInfo(ExportDirectory).GetFiles();
            DateTime lastUpdated = DateTime.MinValue;
            foreach (FileInfo file in files) {
                if (file.LastWriteTime > lastUpdated) {
                    lastUpdated = file.LastWriteTime;
                    fileName = file.Name;
                }
            }
            return fileName;
        }
        private string FileName;
        
        public string GetIonMode(string ionMode)
        {
            ionMode = IonMode.ToString();
            if (ionMode == "Positive")
            {
                ionMode = "pos";
            }
            else if (ionMode == "Negative")
            {
                ionMode = "neg";
            }
            return ionMode;
        }
        private IonMode IonMode { get; }
        private string NotameIonMode;

        public void Run()
        {
            FileName = GetFileName(FileName);
            NotameIonMode = GetIonMode(NotameIonMode);
            SendParametersToNotame();
        }

        private void SendParametersToNotame()
        {
            REngine.SetEnvironmentVariables();
            REngine.SetEnvironmentVariables("c:/program files/r/r-4.3.2/bin/x64", "c:/program files/r/r-4.3.2");
            var engine = REngine.GetInstance();
            engine.Evaluate("Sys.setenv(PATH = paste(\"C:/Program Files/R/R-4.3.2/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))");
            engine.Evaluate("library(notame)");
            engine.Evaluate("library(doParallel)");
            engine.Evaluate("library(dplyr)");
            engine.Evaluate("library(openxlsx)");
            engine.SetSymbol("path", engine.CreateCharacter(ExportDirectory));
            engine.SetSymbol("file_name", engine.CreateCharacter(FileName));
            engine.SetSymbol("ion_mod", engine.CreateCharacter(NotameIonMode));

            string rScript = @"
                # RHaikonen
                # 20231110
                # Overall view of notame preprocessing and quality metrics

                # library(notame)
                # library(doParallel)
                # library(dplyr)
                # library(openxlsx)

                # Functions to import Ms-dial export
                name_features <- function (feature_data, ion_mod) 
                {
                  cols <- find_mz_rt_cols(feature_data)
                  mz_col <- cols$mz_col
                  rt_col <- cols$rt_col
                  round_mz <- as.numeric(feature_data[, mz_col]) %>% as.character() %>% 
                    gsub(""[.]"", ""_"", .)
                  round_rt <- as.numeric(feature_data[, rt_col]) %>% as.character() %>% 
                    gsub(""[.]"", ""_"", .)
                  feature_data$Feature_ID <- paste0(ion_mod, ""_"", 
                                                    round_mz, ""a"", round_rt)
                  if (anyDuplicated(feature_data$Feature_ID)) {
                    duplicates <- paste0(feature_data$Feature_ID[duplicated(feature_data$Feature_ID)], 
                                         collapse = "", "")
                    stop(paste0(""Could not create unique feature names from m/z and retention time columns. Duplicated values: "", 
                                duplicates))
                  }
                  feature_data
                }
                find_mz_rt_cols <- function (feature_data) 
                {
                  mz_tags <- c(""mass"", ""average mz"", ""average.mz"", ""molecularweight"", 
                               ""molecular weight"", ""average_mz"")
                  rt_tags <- c(""retention time"", ""retentiontime"", ""average rt[(]min[)]"", 
                               ""average[_]rt[_]min[_]"", ""average[.]rt[.]min[.]"", ""^rt$"")
                  mz_col <- NULL
                  for (tag in mz_tags) {
                    hits <- grepl(tag, tolower(colnames(feature_data)))
                    if (any(hits)) {
                      mz_col <- colnames(feature_data)[which(hits)[1]]
                      break
                    }
                  }
                  rt_col <- NULL
                  for (tag in rt_tags) {
                    hits <- grepl(tag, tolower(colnames(feature_data)))
                    if (any(hits)) {
                      rt_col <- colnames(feature_data)[which(hits)[1]]
                      break
                    }
                  }
                  if (is.null(mz_col)) {
                    stop(paste0(""No mass to charge ratio column found - should match one of:\n"", 
                                paste(mz_tags, collapse = "", ""), "" (not case-sensitive)""))
                  }
                  if (is.null(rt_col)) {
                    stop(paste0(""No retention time column found - should match one of:\n"", 
                                paste(rt_tags, collapse = "", ""), "" (not case-sensitive)""))
                  }
                  return(list(mz_col = mz_col, rt_col = rt_col))
                }


                ##############################################################
                read_MSDIAL <- function(tablefile, ion_mod){
  
                  tbl <- readr::read_delim(tablefile, delim = ""\t"")
  
                  temp <- colnames(tbl)
                  temp <- sub(""\\.."", NA, temp)
                  tbl <- rbind(temp, tbl)
  
                  corner_row <- which(tbl[, 1] != """")[1]
                  corner_column <- which(tbl[1, ] != """")[1]
  
                  # Featuredata
                  peakmetadata_cols <- tbl %>%
                    dplyr::select(1:(corner_column)) %>%
                    dplyr::slice(corner_row:dplyr::n())
  
                  colnames(peakmetadata_cols) <- as.character(unlist(peakmetadata_cols[1,]))
                  row_tbl <- peakmetadata_cols[-1,]
  
                  row_tbl <- name_features(as.data.frame(row_tbl), ion_mod = ion_mod)
                  rownames(row_tbl) <- row_tbl$Feature_ID
                  row_tbl$Split <- ion_mod
                  fData <- as.data.frame(row_tbl)
  
                  # extract the sample information (pData) and expression table
                  quantval_cols <- tbl %>% dplyr::select((corner_column+1):ncol(tbl))
  
                  numof_sample_class <- length(na.omit(unique(unlist(quantval_cols[1,]))))
                  numof_avgstdev_cols <- numof_sample_class * 2
                  quantval_cols <- quantval_cols %>%
                    dplyr::select(1:(ncol(quantval_cols)-numof_avgstdev_cols))
  
                  # pData
                  pData <- as.data.frame(t(quantval_cols[c(1:corner_row),]))
  
                  # colnames for pData
                  temp <- t(tbl[1:corner_row,corner_column])
                  temp <- ifelse(temp == ""File type"", ""QC"", temp)
                  colnames(pData) <- temp # c(""Class"", ""FileType"",""InjectionOrder"", ""BatchId"", ""SampleId"")
  
                  pData$Sample_ID <- paste0(""Sample_"", pData$Class)
  
                  # final pData
                  pData <- as.data.frame(pData)
                  colnames(pData) <- gsub("" "", ""_"", colnames(pData))
                  rownames(pData) <- pData$Sample_ID
  
                  # Expression part
                  quantval_tbl <- as.matrix(quantval_cols[-c(1:corner_row),] %>%
                                              dplyr::mutate(across(where(is.character), as.numeric)))
  
                  rownames(quantval_tbl) <- rownames(fData)
                  colnames(quantval_tbl) <- rownames(pData)
  
                  return(list(exprs = quantval_tbl, pheno_data = pData, feature_data = fData))
                }
                
                path <- file.path(path, ""/"")
                grouping_name <- ""Class""
                # (note: as we use split_by here assumption is that the data contains signals from two or more modes)
                # One of split_by and name are needed. Depending of the number of modes.
                if(ion_mod != """"){
                  data <- read_MSDIAL(paste0(path, file_name), ion_mod = ion_mod)
                  # data <- notame::read_from_excel(file = paste0(path, file_name), sheet = 1, name = ion_mod)
                }

                # Some adaptations can be done
                # Replace spaces with underscores if necessary
                rownames(data$exprs) <- gsub("" "", ""_"", rownames(data$exprs))
                rownames(data$feature_data) <- gsub("" "", ""_"", rownames(data$feature_data))
                data$feature_data$Feature_ID <- gsub("" "", ""_"", data$feature_data$Feature_ID)

                # Works with one or more modes
                # Construct MetaboSet objects
                # (note: assumption is that the dataset already contains group information)
                metaboset <- notame::construct_metabosets(exprs = data$exprs, pheno_data = data$pheno_data,
                                                          feature_data = data$feature_data,
                                                          group_col = grouping_name, split_data = F)

                # first check that there is QC samples.
                # The who process is dependent on QC samples
                if(""QC"" %in% metaboset$QC ){
                  # Take several cores to speed up processing
                  num_cores <- parallel::detectCores() - 5
                  cl <- parallel::makeCluster(num_cores)
                  doParallel::registerDoParallel(cl)
  
                  # Set all zero abundances to NA and asses the detection rate and flag based on that
                  metaboset <- notame::mark_nas(metaboset, value = 0)
                  metaboset <- notame::flag_detection(metaboset)
  
                  # Set the name for visualization and do visualization
                  # Commented here!
                  # name <- """"
                  # visualizations(metaboset, prefix = paste0(path, ""figures/"", name, ""_ORIG""))
  
                  # Correct the possible drift
                  corrected <- notame::correct_drift(metaboset)
                  # visualizations(corrected, prefix = paste0(path, ""figures/"", name, ""_DRIFT""))
  
                  # flag based on quality
                  corrected <- notame::flag_quality(corrected)
                  # visualizations(corrected, prefix = paste0(path, ""figures/"", name, ""_CLEANED""))
  
                  # Remove the QCs for imputation
                  merged_no_qc <- notame::drop_qcs(corrected)
                  #visualizations(merged_no_qc, prefix = paste0(path, ""figures/FULL_NO_QC""))
  
                  # Imputation
                  # (note: may not be necessary especially if gap filling by compulsion was used in MS-DIAL)
                  # Needs missForest package for random forest imputation
  
                  # Set seed number for reproducibility
                  set.seed(1234567)
                  imputed <- notame::impute_rf(merged_no_qc)
                  imputed <- notame::impute_rf(imputed, all_features = TRUE)
  
                  #Stop using several cores (releases them for other use)
                  parallel::stopCluster(cl)
  
                  # Save the merged and processed data
                  # options to save in rds format or to excel
                  saveRDS(imputed, file = paste0(path, ""full_data.RDS""))
                  write_to_excel(imputed, file = paste0(path, ""full_data.xlsx""))
                } else{
                  saveRDS(metaboset, file = paste0(path, ""full_data.RDS""))
                  write_to_excel(metaboset, file = paste0(path, ""full_data.xlsx""))
                }
            ";
            engine.Evaluate(rScript);
        }
    }
}
