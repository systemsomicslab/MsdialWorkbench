using System;
using System.IO;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

using Rfx.Riken.OsakaUniv;
using Msdial.Dendrogram;

namespace Msdial.DendrogramTest
{
    /// <summary>
    /// Interaction logic for HCAPlottingTest.xaml
    /// </summary>
    public partial class HCAPlottingTestWindow : Window
    {
        public HCAPlottingTestWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.HierarchicalClusterAnalysis(statObj);
            var bean = getDendrogramPlotBean(result);
            var ui = new DendrogramPlotUI(bean);
            this.DendrogramPlot.Content = ui;
            this.DataContext = bean;
            return;
        }

        private DendrogramPlotBean getDendrogramPlotBean(MultivariateAnalysisResult result)
        {
            var graphTitle = "Dendrogram test";
            var labels = result.StatisticsObject.YLabels;
            var dendrogramplot = new DendrogramPlotBean(graphTitle, labels, result.Dendrogram, result.Root);
            return dendrogramplot;
        }

        private StatisticsObject getStatObject(string path)
        {
            var statobj = new StatisticsObject();
            using (var sr = new StreamReader(path, Encoding.ASCII, false))
            {
                for (int i = 1; i <= 9; i++)
                    sr.ReadLine();
                // statobj.YLabels = new ObservableCollection<string>(sr.ReadLine().Split('\t').Skip(31).ToArray());
                var ylabel = sr.ReadLine().Split('\t').Skip(31).ToList();
                ylabel.RemoveAt(4);
                statobj.YLabels = new ObservableCollection<string>(ylabel);
                statobj.YVariables = Enumerable.Repeat(1, statobj.YLabels.Count).Select(e => (double)e).ToArray();
                var mat = new List<double[]>();
                var names = new List<string>();
                while (sr.EndOfStream == false){
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        continue;
                    var arr = line.Split('\t');
                    var id = arr[0];
                    var name = arr[3];
                    names.Add(id + name);
                    var vec = arr.Skip(31).Select(val => double.Parse(val)).ToList();
                    vec.RemoveAt(4);
                    mat.Add(vec.ToArray());
                }
                statobj.XLabels = new ObservableCollection<string>(names.ToArray());
                statobj.XDataMatrix = new double[statobj.YLabels.Count, statobj.XLabels.Count];
                for (int j = 0; j < statobj.XLabels.Count; ++j)
                    for (int i = 0; i < statobj.YLabels.Count; ++i)
                        statobj.XDataMatrix[i, j] = mat[j][i];
            }
            statobj.StatInitialization();
            return statobj;
        }
    }
}
