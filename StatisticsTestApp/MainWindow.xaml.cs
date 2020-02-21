using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace StatisticsTestApp {
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window {
        public MainWindow() {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            var path = @"C:\Users\ADMIN\Desktop\new 1.txt";
            var statObj = getTestStatisticsObject(path);
            //var result = StatisticsMathematics.PrincipalComponentAnalysis(statObj, MultivariateAnalysisOption.Pca);

            //var pairwisePlot = getPairwisePlotBean(result);
            var pairwisePlot = new PairwisePlotBean();
            var pairwisePlotUI = new PairwisePlotUI(pairwisePlot);
            this.PairWise.Content = pairwisePlotUI;

        }

        private PairwisePlotBean getPairwisePlotBean(MultivariateAnalysisResult result) {
            var graphTitle = "PCA";
            var xTitle = "PC 1";
            var yTitle = "PC 2";
            var xValues = new ObservableCollection<double>(result.TPreds[0].ToList());
            var yValues = new ObservableCollection<double>(result.TPreds[1].ToList());
            var brushes = new ObservableCollection<SolidColorBrush>();
            foreach (var value in xValues) brushes.Add(Brushes.Black);
            var labels = result.StatisticsObject.XLabels;
            var indexes = result.StatisticsObject.XIndexes;
            //brushes = result.StatisticsObject.XColors;
            var pairwiseplot = new PairwisePlotBean(graphTitle, xTitle, yTitle, xValues, yValues, labels, brushes, indexes, PairwisePlotDisplayLabel.None);
            return pairwiseplot;
        }

        private StatisticsObject getTestStatisticsObject(string path) {
            var statObj = new StatisticsObject();
            return statObj;
            var metabolites = new List<string>();
            using (var sr = new StreamReader(path, Encoding.ASCII, false)) {
                for (int i = 0; i < 9; i++) sr.ReadLine();
                var files = sr.ReadLine().Split('\t');
                statObj.XLabels = new ObservableCollection<string>(files.ToList());
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    var id = lineArray[0];
                    var metabolitename = lineArray[3];
                    metabolites.Add(metabolitename);
                }
            }
            statObj.YLabels = new ObservableCollection<string>(metabolites);
            return statObj;
        }
    }

    public class MultivariateAnalysisResultVM : ViewModelBase {

    }

}
