using CompMs.Common.Extension;
using CompMs.CommonMVVM;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Drawing;
using RDotNet;
using System;

namespace CompMs.App.Msdial.Model.Statistics
{
    public class Notame
    {
        public void GenerateGraph()
        {
            REngine.SetEnvironmentVariables();
            var engine = REngine.GetInstance();
            engine.Initialize();
            engine.Evaluate("x <- c(1, 2, 3, 4, 5)");
            engine.Evaluate("y <- c(10, 15, 13, 18, 20)");
            engine.Evaluate("plot(x, y, type='l')");

            engine.Evaluate("dev.copy(png, 'graph.png')");
            engine.Evaluate("dev.off()");

            MessageBox.Show("Graph generated and saved as 'graph.png'");
        }

        private void ButtonExit(object sender, RoutedEventArgs e)
        {
            REngine.SetEnvironmentVariables();
            var engine = REngine.GetInstance();
            engine.Initialize();
            engine.Dispose();
            Application.Current.Shutdown();
        }

        public interface IGenerateGraph
        {
            void GenerateGraph();
        }
    }
}