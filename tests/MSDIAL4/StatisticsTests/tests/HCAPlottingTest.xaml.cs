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

namespace StatisticsTests.tests
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
            throw new NotImplementedException();
            var path = @"../../data/testmatrix.txt";
            var statObj = getStatObject(path);
            var result = StatisticsMathematics.PrincipalComponentAnalysis(statObj, MultivariateAnalysisOption.Pca);
            var bean = getPairwisePlotBean(result);
            var ui = new PairwisePlotUI(bean);
            this.Scoreplot.Content = ui;
            this.nMetabolites.Content = result.StatisticsObject.XLabels.Count;
            return;
        }

        private PairwisePlotBean getPairwisePlotBean(MultivariateAnalysisResult result)
        {
            var graphTitle = "PC test";
            var xTitle = "PC 1";
            var yTitle = "PC 2";
            var xValues = new ObservableCollection<double>(result.TPreds[0].ToList());
            var yValues = new ObservableCollection<double>(result.TPreds[1].ToList());
            var labels = result.StatisticsObject.YLabels;
            var brushes = new ObservableCollection<SolidColorBrush>();
            foreach (var _ in xValues)
                brushes.Add(Brushes.Black);
            var pairwiseplot = new PairwisePlotBean(graphTitle, xTitle, yTitle, xValues, yValues, labels, brushes, PairwisePlotDisplayLabel.None);
            return pairwiseplot;
        }

        private StatisticsObject getStatObject(string path)
        {
            var statobj = new StatisticsObject();
            using (var sr = new StreamReader(path, Encoding.ASCII, false))
            {
                for (int i = 1; i <= 9; i++)
                    sr.ReadLine();
                statobj.YLabels = new ObservableCollection<string>(sr.ReadLine().Split('\t').Skip(31).ToArray());
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
                    mat.Add(arr.Skip(31).Select(val => double.Parse(val)).ToArray());
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
