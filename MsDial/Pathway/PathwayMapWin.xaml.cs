using Common.BarChart;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Msdial.Pathway;
using Riken.Metabolomics.Pathwaymap;
using System;
using System.Collections.Generic;
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

namespace Riken.PathwayView {
    /// <summary>
    /// Interaction logic for PathwayView.xaml
    /// </summary>
    public partial class PathwayMapWin : Window {
        
        public PathwayMapWin(MainWindow mainWindow, PathwayMapObj pathwayObj, List<Metabolomics.Pathwaymap.Node> allNodes) {
            InitializeComponent();
            this.DataContext = new PathwayVM(this, mainWindow, pathwayObj, allNodes);
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            var window = new SaveImageAsWin(target, "1000", "1000");
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }
    }

    public class PathwayVM : ViewModelBase {
        public MainWindow MainWindow { get; set; }
        public PathwayMapObj PathwayObj { get; set; }
        public PathwayMapUI GlobalView { get; set; }

        public PathwayMapObj PathwaySubObj { get; set; }
        public PathwayMapUI FocusedView { get; set; }
        public PathwayMapWin PathwayMapWin { get; set; }
        public List<Metabolomics.Pathwaymap.Node> AllNodes { get; set; }

        public PathwayVM(PathwayMapWin pathwayMapWin, MainWindow mainWindow, 
            PathwayMapObj pathwayObj, List<Metabolomics.Pathwaymap.Node> allNodes) {
            this.MainWindow = mainWindow;
            this.PathwayObj = pathwayObj;
            this.PathwayMapWin = pathwayMapWin;
            this.AllNodes = allNodes;
            this.PathwayMapWin.GrobalMap.Content = new PathwayMapUI(pathwayObj);

            var project = mainWindow.ProjectProperty;
            this.PathwayObj.PropertyChanged += PathwayObj_PropertyChanged;
            
            // initialize
            foreach (var node in this.PathwayObj.Nodes) {
                if (node.BarChart != null) {
                    this.PathwayObj.SelectedPlotID = node.ID;
                    break;
                }
            }
        }

        private void PathwayObj_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "SelectedPlotID") {
                var project = this.MainWindow.ProjectProperty;
                var node = this.PathwayObj.Id2Node[this.PathwayObj.SelectedPlotID];
                List<Metabolomics.Pathwaymap.Node> nodes = null;
                if (node.Label == "All others") {
                    nodes = MappingToPathways.GetNodeObjects(this.AllNodes,
                          (float)this.PathwayMapWin.Grid_FocusedMap.ActualWidth, (float)this.PathwayMapWin.Grid_FocusedMap.ActualHeight,
                          AllNodes[0].Width, AllNodes[0].Height, 20, "All others");
                }
                else {
                    if (node.IsMappedByInChIKey == false) { // meaning lipid pathway's node
                        var lipidname = node.Key;
                        nodes = MappingToPathways.GetNodeObjects(this.AllNodes,
                            (float)this.PathwayMapWin.Grid_FocusedMap.ActualWidth, (float)this.PathwayMapWin.Grid_FocusedMap.ActualHeight,
                            AllNodes[0].Width, AllNodes[0].Height, 20, node.Label);

                    }
                    else {

                    }
                }

                if (nodes != null) {
                    this.PathwaySubObj = new PathwayMapObj(nodes);
                    this.PathwayMapWin.FocusedMap.Content = new PathwayMapUI(this.PathwaySubObj);
                    this.PathwaySubObj.PropertyChanged += PathwayObjSub_PropertyChanged;
                    // initialize
                    foreach (var subNode in this.PathwaySubObj.Nodes) {
                        if (subNode.BarChart != null) {
                            this.PathwaySubObj.SelectedPlotID = subNode.ID;
                            break;
                        }
                    }
                }

                var mainBarChart = getBarchartCopy(node.BarChart);
                if (mainBarChart == null) return;
                this.PathwayMapWin.Barchart_Main.Content = new BarChartUI(mainBarChart);
            }
        }

       
        private void PathwayObjSub_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "SelectedPlotID") {
                var project = this.MainWindow.ProjectProperty;
                var node = this.PathwaySubObj.Id2Node[this.PathwaySubObj.SelectedPlotID];
                var subBarChart = getBarchartCopy(node.BarChart);
                if (subBarChart == null) return;
                this.PathwayMapWin.Barchart_Sub.Content = new BarChartUI(subBarChart);
            }
        }

        private BarChartBean getBarchartCopy(BarChartBean barChart) {
            if (barChart == null) return null;
            var xAxisTitle = "Class";
            var yAxisTitle = barChart.YAxisTitle;
            var mainTitle = barChart.MainTitle;
            var barElements = new List<BarElement>();
            foreach (var element in barChart.BarElements) {
                barElements.Add(element);
            }
            var newBarchart = new BarChartBean(barElements, mainTitle, xAxisTitle, yAxisTitle);
            return newBarchart;
        }

    }
}
