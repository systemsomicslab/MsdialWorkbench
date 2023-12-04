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
using System.Windows.Forms;

namespace CompMs.App.Msdial.Model.Statistics
{
    public sealed class Notame : BindableBase
    {
        public string Path {
            get => __path;
            set => SetProperty(ref __path, value);
        }
        private string __path = string.Empty;

        public string FileName {
            get => __fileName;
            set => SetProperty(ref __fileName, value);
        }
        private string __fileName = string.Empty;

        public string IonMode {
            get => __ionMode;
            set => SetProperty(ref __ionMode, value);
        }
        private string __ionMode = string.Empty;

        public string GroupingName {
            get => __groupingName;
            set => SetProperty(ref __groupingName, value);
        }
        private string __groupingName = string.Empty;

        private TextBox _path;
        private TextBox _fileName;
        private TextBox _ionMod;
        private TextBox _groupingName;
        private Label _label1;
        private Label _label2;
        private Label _label3;
        private Label _label4;

        public void Run()
        {
            Form form = CreateForm();
            form.ShowDialog();
        }

        private Form CreateForm()
        {
            Form form = new Form();
            Button button = CreateButton("Run");
            button.Click += SaveParameters;

            _label1 = CreateLabel("File path: ", 10, 10);
            _path = CreateTextBox(120, 10, "Sample data file path (For example: c:/src/)");

            _label2 = CreateLabel("File name: ", 10, 40);
            _fileName = CreateTextBox(120, 40, "File name (For example: Hilic_pos.xlsx)");

            _label3 = CreateLabel("Ion mode: ", 10, 70);
            _ionMod = CreateTextBox(120, 70, "Ion Mode (For example: HILIC_pos)");

            _label4 = CreateLabel("Grouping name: ", 10, 100);
            _groupingName = CreateTextBox(120, 100, "Grouping name (For example: Group");

            form.Text = "Insert Parameters: ";
            form.StartPosition = FormStartPosition.CenterParent;
            form.Controls.AddRange(new Control[] { button, _label1, _path, _label2, _fileName, _label3, _ionMod, _label4, _groupingName });
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            return form;
        }

        private Button CreateButton(string text)
        {
            Button button = new Button();
            button.Text = text;
            button.Location = new System.Drawing.Point(110, 150);
            return button;
        }

        private Label CreateLabel(string text, int x, int y)
        {
            Label label = new Label();
            label.Text = text;
            label.Location = new Point(x, y);
            return label;
        }

        private TextBox CreateTextBox(int x, int y, string description)
        {
            TextBox textBox = new System.Windows.Forms.TextBox();
            textBox.Location = new Point(x, y);
            return textBox;
        }

        private void SaveParameters(object sender, EventArgs e)
        {
            string pathValue = _path.Text;
            string fileNameValue = _fileName.Text;
            string ionModValue = _ionMod.Text;
            string groupingNameValue = _groupingName.Text;

            SaveParametersAsJson(pathValue, fileNameValue, ionModValue, groupingNameValue);
            SendParametersToNotame(pathValue, fileNameValue, ionModValue, groupingNameValue);
        }

        private void SaveParametersAsJson(string path, string fileName, string ionMod, string groupingName)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>
            {
                { "path", path },
                { "file_name", fileName },
                { "ion_mod", ionMod },
                { "grouping_name", groupingName }
            };

            string jsonParameter = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            string filePath = "C:/src/NotameParameters.json";
            File.WriteAllText(filePath, jsonParameter);
            MessageBox.Show("Parameters saved to the JSON file.");
        }

        private void SendParametersToNotame(string path, string fileName, string ionMod, string groupingName)
        {
            REngine.SetEnvironmentVariables();
            REngine.SetEnvironmentVariables("c:/program files/r/r-4.3.2/bin/x64", "c:/program files/r/r-4.3.2");
            var engine = REngine.GetInstance();
            engine.Evaluate("Sys.setenv(PATH = paste(\"C:/Program Files/R/R-4.3.2/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))");
            engine.Evaluate("library(notame)");
            engine.Evaluate("library(doParallel)");
            engine.Evaluate("library(dplyr)");
            engine.Evaluate("library(openxlsx)");
            engine.SetSymbol("path", engine.CreateCharacter(path));
            engine.SetSymbol("file_name", engine.CreateCharacter(fileName));
            engine.SetSymbol("ion_mod", engine.CreateCharacter(ionMod));
            engine.SetSymbol("grouping_name", engine.CreateCharacter(groupingName));

            //File path = Sample data file path (For example: )
            //File name =

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
