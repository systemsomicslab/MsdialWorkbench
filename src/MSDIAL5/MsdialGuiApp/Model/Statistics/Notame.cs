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

using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Text;
using System.Windows.Forms;
using Newtonsoft.Json;

namespace CompMs.App.Msdial.Model.Statistics
{
    public class Notame : Form
    {
        private TextBox parameter1;
        private TextBox parameter2;
        private TextBox parameter3;
        private TextBox parameter4;
        private Label label1;
        private Label label2;
        private Label label3;
        private Label label4;

        public void Run()
        {
            Form form = new Form();
            Button button = new Button();
            button.Text = "Run";
            button.Location = new System.Drawing.Point(110, 150);
            button.Click += new EventHandler(SaveParameters);

            label1 = new Label();
            label1.Text = "Parameter 1";
            label1.Location = new Point(10, 10);
            form.Controls.Add(label1);

            parameter1 = new System.Windows.Forms.TextBox();
            parameter1.Location = new Point(120, 10);
            form.Controls.Add(parameter1);

            label2 = new Label();
            label2.Text = "Parameter 2";
            label2.Location = new Point(10, 40);
            form.Controls.Add(label2);

            parameter2 = new System.Windows.Forms.TextBox();
            parameter2.Location = new Point(120, 40);
            form.Controls.Add(parameter2);

            label3 = new Label();
            label3.Text = "Parameter 3";
            label3.Location = new Point(10, 70);
            form.Controls.Add(label3);

            parameter3 = new System.Windows.Forms.TextBox();
            parameter3.Location = new Point(120, 70);
            form.Controls.Add(parameter3);

            label4 = new Label();
            label4.Text = "Parameter 4";
            label4.Location = new Point(10, 100);
            form.Controls.Add(label4);

            parameter4 = new System.Windows.Forms.TextBox();
            parameter4.Location = new Point(120, 100);
            form.Controls.Add(parameter4);

            form.Text = "Insert Parameters";
            form.StartPosition = FormStartPosition.CenterParent;
            form.Controls.Add(button);
            form.MaximizeBox = false;
            form.MinimizeBox = false;
            form.ShowDialog();
        }

        private void SaveParameters(object sender, EventArgs e)
        {
            string parameter1Value = parameter1.Text;
            string parameter2Value = parameter2.Text;
            string parameter3Value = parameter3.Text;
            string parameter4Value = parameter4.Text;

            SaveParametersAsJson(parameter1Value, parameter2Value, parameter3Value, parameter4Value);
        }

        private void SaveParametersAsJson(string parameter1, string parameter2, string parameter3, string parameter4)
        {
            Dictionary<string, string> parameters = new Dictionary<string, string>();
            parameters.Add("Parameter1", parameter1);
            parameters.Add("Parameter2", parameter2);
            parameters.Add("Parameter3", parameter3);
            parameters.Add("Parameter4", parameter4);

            string jsonString = JsonConvert.SerializeObject(parameters, Formatting.Indented);
            string filePath = "D:/2023/11/Notame/NotameParameters.json";
            File.WriteAllText(filePath, jsonString);

            MessageBox.Show("Parameters saved to the JSON file.");
        }
    }
}