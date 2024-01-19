using CompMs.CommonMVVM;
using Newtonsoft.Json;
using RDotNet;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows;

namespace CompMs.App.Msdial.Model.Statistics
{
    public sealed class Notame : BindableBase
    {
        public string Path {
            get => _path;
            set => SetProperty(ref _path, value);
        }
        private string _path = string.Empty;

        public string FileName {
            get => _fileName;
            set => SetProperty(ref _fileName, value);
        }
        private string _fileName = string.Empty;

        public string IonMode {
            get => _ionMode;
            set => SetProperty(ref _ionMode, value);
        }
        private string _ionMode = string.Empty;

        public string GroupingName {
            get => _groupingName;
            set => SetProperty(ref _groupingName, value);
        }
        private string _groupingName = string.Empty;

        public void Run()
        {
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
            engine.SetSymbol("path", engine.CreateCharacter(Path));
            engine.SetSymbol("file_name", engine.CreateCharacter(FileName));
            engine.SetSymbol("ion_mod", engine.CreateCharacter(IonMode));
            engine.SetSymbol("grouping_name", engine.CreateCharacter(GroupingName));

            string rScript = @"
                data <- notame::read_from_excel(file = paste0(path, file_name), sheet = 1, name = ion_mod)

                rownames(data$exprs) <- gsub("" "", ""_"", rownames(data$exprs))
                rownames(data$feature_data) <- gsub("" "", ""_"", rownames(data$feature_data))
                data$feature_data$Feature_ID <- gsub("" "", ""_"", data$feature_data$Feature_ID)
                data$feature_data$Split <- gsub("" "", ""_"", data$feature_data$Split)

                modes <- notame::construct_metabosets(exprs = data$exprs, pheno_data = data$pheno_data,
                                             feature_data = data$feature_data,
                                             group_col = grouping_name)

                metaboset <- notame::merge_metabosets(modes)

                num_cores <- parallel::detectCores() - 5
                cl <- parallel::makeCluster(num_cores)
                doParallel::registerDoParallel(cl)

                metaboset <- notame::mark_nas(metaboset, value = 0)
                metaboset <- notame::flag_detection(metaboset)

                corrected <- notame::correct_drift(metaboset)

                corrected <- notame::flag_quality(corrected)

                merged_no_qc <- notame::drop_qcs(corrected)

                set.seed(1234567)
                imputed <- notame::impute_rf(merged_no_qc)
                imputed <- notame::impute_rf(imputed, all_features = TRUE)

                parallel::stopCluster(cl)

                saveRDS(imputed, file = paste0(path, ""full_data.RDS""))
                write_to_excel(imputed, file = paste0(path, ""full_data.xlsx""))
            ";
            engine.Evaluate(rScript);
        }
    }
}
