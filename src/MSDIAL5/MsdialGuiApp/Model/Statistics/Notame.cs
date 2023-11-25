//using RDotNet;
//using System.Windows;

//namespace CompMs.App.Msdial.Model.Statistics
//{
//    public sealed class Notame
//    {
//        public void Run()
//        {
//            REngine.SetEnvironmentVariables();
//            REngine.SetEnvironmentVariables("c:/program files/r/r-4.3.2/bin/x64", "c:/program files/r/r-4.3.2");
//            var engine = REngine.GetInstance();
//            //engine.Initialize();
//            engine.Evaluate("Sys.setenv(PATH = paste(\"C:/Program Files/R/R-4.3.2/bin/x64\", Sys.getenv(\"PATH\"), sep=\";\"))");
//            engine.Evaluate("library(stats)");
//            engine.Evaluate("source('c:/src/myscript.r')");

//            //engine.Evaluate("x <- c(1, 2, 3, 4, 5)");
//            //engine.Evaluate("y <- c(10, 15, 13, 18, 20)");
//            //engine.Evaluate("plot(x, y, type='l')");

//            //engine.Evaluate("dev.copy(png, 'graph.png')");
//            //engine.Evaluate("dev.off()");

//            MessageBox.Show("Graph generated and saved as 'graph.png'");
//        }
//    }
//}

using RDotNet;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Pipes;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CompMs.App.Msdial.Model.Statistics
{
    public class Notame : Form
    {
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
            _path = CreateTextBox(120, 10);

            _label2 = CreateLabel("File name: ", 10, 40);
            _fileName = CreateTextBox(120, 40);

            _label3 = CreateLabel("Ion mode: ", 10, 70);
            _ionMod = CreateTextBox(120, 70);

            _label4 = CreateLabel("Grouping name: ", 10, 100);
            _groupingName = CreateTextBox(120, 100);

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

        private TextBox CreateTextBox(int x, int y)
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

            SendParametersToNotame(jsonParameter);
        }

        private void SendParametersToNotame(string jsonParameter)
        {
            using (var pipeClient = new NamedPipeClientStream("notameParameters"))
            {
                pipeClient.Connect();

                using (var writer = new StreamWriter(pipeClient))
                {
                    writer.Write(jsonParameter);
                }
            }
            MessageBox.Show("Parameters sent to the Notame.");
        }
    }
}
