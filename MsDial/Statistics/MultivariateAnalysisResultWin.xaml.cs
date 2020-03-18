using Common.BarChart;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
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

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for PlsResultWin.xaml
    /// </summary>
    public partial class MultivariateAnalysisResultWin : Window {

        public MultivariateAnalysisResultWin(MultivariateAnalysisResult result) {
            InitializeComponent();
            var vm = new MultivariateAnalysisResultVM(result);
            vm.Initializer();
            this.DataContext = vm;
            buttonInitialize(result.MultivariateAnalysisOption);
        }

        private void buttonInitialize(MultivariateAnalysisOption option) {
            if (option == MultivariateAnalysisOption.Pca) {
                this.Button_PredictedVsExperiment.IsEnabled = false;
                this.Button_VIP.IsEnabled = false;
                this.Button_Splot.IsEnabled = false;
                this.Button_Coefficient.IsEnabled = false;
            }
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr) {
                this.Button_Splot.IsEnabled = false;
            }
        }

        
    }

    public class MultivariateAnalysisResultVM : ViewModelBase {

        public Window MAResultWin { get; set; }
        public MultivariateAnalysisResult MAResult { get; set; }
        private bool isImported;

        public ObservableCollection<int> XComponents { get; set; }
        private int selectedXComponent;
        public int SelectedXComponent {
            get { return selectedXComponent; }
            set {
                if (selectedXComponent != value) {
                    selectedXComponent = value;
                    OnPropertyChanged("SelectedXComponent");
                    Debug.WriteLine(selectedXComponent);
                    UpdatePlots();
                }
            }
        }

        
        public ObservableCollection<int> YComponents { get; set; }
        private int selectedYComponent;
        public int SelectedYComponent {
            get { return selectedYComponent; }
            set {
                if (selectedYComponent != value) {
                    selectedYComponent = value;
                    OnPropertyChanged("SelectedYComponent");
                    Debug.WriteLine(selectedYComponent);
                    UpdatePlots();
                }
            }
        }

        public ObservableCollection<string> SampleLabels { get; set; }
        private int selectedSampleLabel;
        public int SelectedSampleLabel {
            get { return selectedSampleLabel; }
            set {
                if (selectedSampleLabel != value) {
                    selectedSampleLabel = value;
                    OnPropertyChanged("SelectedSampleLabel");
                    Debug.WriteLine(selectedSampleLabel);
                    UpdatePlots();
                }
            }
        }
        public ObservableCollection<string> MetaboliteLabels { get; set; }
        private int selectedMetaboliteLabel;
        public int SelectedMetaboliteLabel {
            get { return selectedMetaboliteLabel; }
            set {
                if (selectedMetaboliteLabel != value) {
                    selectedMetaboliteLabel = value;
                    OnPropertyChanged("SelectedMetaboliteLabel");
                    Debug.WriteLine(selectedMetaboliteLabel);
                    UpdatePlots();
                }
            }
        }

        public ObservableCollection<int> SamplePlotSizes { get; set; }
        private int selectedSamplePlotSize;
        public int SelectedSamplePlotSize {
            get { return selectedSamplePlotSize; }
            set {
                if (selectedSamplePlotSize != value) {
                    selectedSamplePlotSize = value;
                    OnPropertyChanged("SelectedSamplePlotSize");
                    Debug.WriteLine(selectedSamplePlotSize);
                    UpdatePlots();
                }
            }
        }

        public ObservableCollection<int> MetabolitePlotSizes { get; set; }
        private int selectedMetabolitePlotSize;
        public int SelectedMetabolitePlotSize {
            get { return selectedMetabolitePlotSize; }
            set {
                if (selectedMetabolitePlotSize != value) {
                    selectedMetabolitePlotSize = value;
                    OnPropertyChanged("SelectedMetabolitePlotSize");
                    Debug.WriteLine(selectedMetabolitePlotSize);
                    UpdatePlots();
                }
            }
        }

        public PairwisePlotUI ScoreplotUI { get; set; }
        public PairwisePlotUI LoadingplotUI { get; set; }

        public BarChartUI Chart1 { get; set; }
        public BarChartUI Chart2 { get; set; }

        public MultivariateAnalysisResultVM(MultivariateAnalysisResult result) {
            this.MAResult = result;
        }

        /// <summary>
        /// Sets up the view model for the pls result window in InvokeCommandAction
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }
        
        private DelegateCommand showPredVsExp;
        public DelegateCommand ShowPredVsExp {
            get {
                return showPredVsExp ?? (showPredVsExp = new DelegateCommand(Show_PredVsExp, obj => { return true; }));
            }
        }

        private DelegateCommand showContributionPlot;
        public DelegateCommand ShowContributionPlot {
            get {
                return showContributionPlot ?? (showContributionPlot = new DelegateCommand(Show_ContributionPlot, obj => { return true; }));
            }
        }

        private DelegateCommand showSPlot;
        public DelegateCommand ShowSPlot {
            get {
                return showSPlot ?? (showSPlot = new DelegateCommand(Show_SPlot, obj => { return true; }));
            }
        }

        private DelegateCommand showVIPs;
        public DelegateCommand ShowVIPs {
            get {
                return showVIPs ?? (showVIPs = new DelegateCommand(Show_VIPs, obj => { return true; }));
            }
        }

        private DelegateCommand showCoefficients;
        public DelegateCommand ShowCoefficients {
            get {
                return showCoefficients ?? (showCoefficients = new DelegateCommand(Show_Coefficients, obj => { return true; }));
            }
        }


        private DelegateCommand savePlotProperties;
        public DelegateCommand SavePlotProperties {
            get {
                return savePlotProperties ?? (savePlotProperties = new DelegateCommand(Show_PlotProperties, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj) {
            this.MAResultWin = (MultivariateAnalysisResultWin)obj;
        }


        private void Show_PlotProperties(object obj) {
            var window = new SaveDataTableAsTextWin(this.MAResult);
            window.Owner = this.MAResultWin;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void Show_PredVsExp(object obj) {

            var xAxisTitle = "Experiment values";
            var yAxisTitle = "Predicted values";

            var xAxisValues = new ObservableCollection<double>(this.MAResult.StatisticsObject.YVariables);
            var yAxisValues = new ObservableCollection<double>(this.MAResult.PredictedYs);

            var brushes = convertRgbaToBrush(this.MAResult.StatisticsObject.YColors);

            var plotBean = new PairwisePlotBean("Predicted vs Experiment plot", xAxisTitle, yAxisTitle,
                xAxisValues, yAxisValues, this.MAResult.StatisticsObject.YLabels,
                brushes, PairwisePlotDisplayLabel.None);
            var pairwiseUI = new PairwisePlotUI(plotBean);
            showPairwiseWin(pairwiseUI);
        }

        private ObservableCollection<SolidColorBrush> convertRgbaToBrush(ObservableCollection<byte[]> bytes) {
            if (bytes == null) return null;
            var brushes = new ObservableCollection<SolidColorBrush>();
            foreach (var colorBytes in bytes) {
                var colorprop = new Color() { R = colorBytes[0], G = colorBytes[1], B = colorBytes[2], A = colorBytes[3] };
                var brush = new SolidColorBrush(colorprop);
                brushes.Add(brush);
            }
            return brushes;
        }

        private void Show_ContributionPlot(object obj) {

            if (this.MAResult.MultivariateAnalysisOption == MultivariateAnalysisOption.Pca) {
                var contributions = this.MAResult.Contributions;
                var indexes = new ObservableCollection<int>();
                var labels = new ObservableCollection<string>();
                for (int i = 0; i < contributions.Count; i++) {
                    indexes.Add(i); labels.Add("Component " + (i + 1));
                }

                var chartUI = GetBarChartUI(indexes, labels, contributions, 
                    "Contribution plot", "Component", "Contribution", true);
                showBarChartWin(chartUI);
            }
            else if (this.MAResult.MultivariateAnalysisOption == MultivariateAnalysisOption.Oplsda || this.MAResult.MultivariateAnalysisOption == MultivariateAnalysisOption.Oplsr) {
                var q2values = this.MAResult.Q2Cums;
                var indexes = new ObservableCollection<int>();
                var labels = new ObservableCollection<string>();
                for (int i = 0; i < q2values.Count; i++) {
                    indexes.Add(i); labels.Add("To" + (i));
                }

                var chartUI = GetBarChartUI(indexes, labels, q2values, "Q2 plot", "Latent variables", "Contribution", true);
                showBarChartWin(chartUI);
            }
            else {
                var q2values = this.MAResult.Q2Cums;
                var indexes = new ObservableCollection<int>();
                var labels = new ObservableCollection<string>();
                for (int i = 0; i < q2values.Count; i++) {
                    indexes.Add(i); labels.Add("LV" + (i + 1));
                }

                var chartUI = GetBarChartUI(indexes, labels, q2values, "Q2 plot", "Latent variables", "Contribution", true);
                showBarChartWin(chartUI);
            }
        }

        private void showBarChartWin(BarChartUI chartUI) {
            var win = new StatisticsBarchartWin(chartUI);
            win.Owner = this.MAResultWin;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.Show();
        }

        private void Show_SPlot(object obj) {

            var xAxisLoadings = new ObservableCollection<double>(this.MAResult.PPreds[0]);
            var yAxisLoadings = new ObservableCollection<double>(this.MAResult.PPredCoeffs[0]);

            var brushes = convertRgbaToBrush(this.MAResult.StatisticsObject.XColors);
            var plotBean = new PairwisePlotBean("S-plot", "P loading", "P correlation",
                xAxisLoadings, yAxisLoadings, this.MAResult.StatisticsObject.XLabels,
                brushes, PairwisePlotDisplayLabel.None);

            var pairwiseUI = new PairwisePlotUI(plotBean);
            showPairwiseWin(pairwiseUI);

        }

        private void showPairwiseWin(PairwisePlotUI pairwiseUI) {
            var win = new StatisticsPairwisePlotWin(pairwiseUI);
            win.Owner = this.MAResultWin;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.Show();
        }

        private void Show_VIPs(object obj) {
            var chartUI = GetBarChartUI(this.MAResult.StatisticsObject.XIndexes, this.MAResult.StatisticsObject.XLabels, this.MAResult.Vips,
                "Variable importance for prediction (VIP)", "Metabolite name", "Value");
            showBarChartWin(chartUI);
        }


        private void Show_Coefficients(object obj) {
            var chartUI = GetBarChartUI(this.MAResult.StatisticsObject.XIndexes, this.MAResult.StatisticsObject.XLabels, this.MAResult.Coefficients,
                "Coefficients", "Metabolite name", "Value");
            showBarChartWin(chartUI);
        }

        private void UpdatePlots() {
            ScoreplotUpdate(this.selectedXComponent, this.selectedYComponent, this.MAResult.MultivariateAnalysisOption);
            LoadingplotUpdate(this.selectedXComponent, this.selectedYComponent, this.MAResult.MultivariateAnalysisOption);

            OnPropertyChanged("ScoreplotUI");
            OnPropertyChanged("LoadingplotUI");
        }

        private void ScoreplotUpdate(int xComp, int yComp, MultivariateAnalysisOption option) {
            var tPredsX = new ObservableCollection<double>(this.MAResult.TPreds[xComp]);
            var tPredsY = option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr
                ? new ObservableCollection<double>(this.MAResult.ToPreds[yComp]) 
                : new ObservableCollection<double>(this.MAResult.TPreds[yComp]);
            var tLabels = this.MAResult.StatisticsObject.YLabels;
            var tIndexes = this.MAResult.StatisticsObject.YIndexes;
            var tColors = this.MAResult.StatisticsObject.YColors;

            var title = string.Empty;
            if (option == MultivariateAnalysisOption.Pca)
                title = "PCA score plot";
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr)
                title = "PLS score plot";
            else if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr)
                title = "OPLS score plot";

            var xAxisHeader = string.Empty;
            var yAxisHeader = string.Empty;
            if (option == MultivariateAnalysisOption.Pca) {
                xAxisHeader = "Principal component";
                yAxisHeader = "Principal component";
            }
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr) {
                xAxisHeader = "Latent variable";
                yAxisHeader = "Latent variable";
            }
            else if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr) {
                xAxisHeader = "Biological score";
                yAxisHeader = "Orthogonal score";
            }

            var label = PairwisePlotDisplayLabel.Label;
            if (this.selectedSampleLabel == 1) label = PairwisePlotDisplayLabel.X;
            else if (this.selectedSampleLabel == 2) label = PairwisePlotDisplayLabel.Y;
            else if (this.selectedSampleLabel == 3) label = PairwisePlotDisplayLabel.None;
            var plotsize = this.SamplePlotSizes[this.selectedSamplePlotSize];

            this.ScoreplotUI = GetPairwisePlotUI(tPredsX, tPredsY, tIndexes, 
                tLabels, tColors, title, xAxisHeader + (xComp + 1).ToString(), 
                yAxisHeader + (yComp + 1).ToString(), label, plotsize);
        }

        private void LoadingplotUpdate(int xComp, int yComp, MultivariateAnalysisOption option) {
            var pPredsX = new ObservableCollection<double>(this.MAResult.PPreds[xComp]);
            var pPredsY = option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr
                ? new ObservableCollection<double>(this.MAResult.PoPreds[yComp]) 
                : new ObservableCollection<double>(this.MAResult.PPreds[yComp]);
            var pLabels = this.MAResult.StatisticsObject.XLabels;
            var pIndexes = this.MAResult.StatisticsObject.XIndexes;
            var pColors = this.MAResult.StatisticsObject.XColors;

            var title = string.Empty;
            if (option == MultivariateAnalysisOption.Pca)
                title = "PCA loading plot";
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr)
                title = "PLS loading plot";
            else if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr)
                title = "OPLS loading plot";

            var xAxisHeader = string.Empty;
            var yAxisHeader = string.Empty;
            if (option == MultivariateAnalysisOption.Pca) {
                xAxisHeader = "Principal component";
                yAxisHeader = "Principal component";
            }
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr) {
                xAxisHeader = "Latent variable";
                yAxisHeader = "Latent variable";
            }
            else if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr) {
                xAxisHeader = "Biological loading";
                yAxisHeader = "Orthogonal loading";
            }

            var label = PairwisePlotDisplayLabel.Label;
            if (this.selectedMetaboliteLabel == 1) label = PairwisePlotDisplayLabel.X;
            else if (this.selectedMetaboliteLabel == 2) label = PairwisePlotDisplayLabel.Y;
            else if (this.selectedMetaboliteLabel == 3) label = PairwisePlotDisplayLabel.None;
            var plotsize = this.MetabolitePlotSizes[this.selectedMetabolitePlotSize];

            this.LoadingplotUI = GetPairwisePlotUI(pPredsX, pPredsY, pIndexes, pLabels, pColors, 
                title, xAxisHeader + (xComp + 1).ToString(), yAxisHeader + (yComp + 1).ToString(),
                label, plotsize);

            this.Chart1 = GetBarChartUI(pIndexes, pLabels, pPredsX, title, xAxisHeader + (xComp + 1).ToString(), "Value");
            this.Chart2 = GetBarChartUI(pIndexes, pLabels, pPredsY, title, yAxisHeader + (yComp + 1).ToString(), "Value");
        }


        // utility only used in this window
        // collections must be equal length
        public BarChartUI GetBarChartUI(ObservableCollection<int> indexes,
            ObservableCollection<string> labeles,
            ObservableCollection<double> values,
            string title, string xTitle, string yTitle, bool isOrderKept = false) {

            var elements = new List<BarElement>();
            for (int i = 0; i < indexes.Count; i++) {
                var elem = new BarElement() {
                    Id = indexes[i],
                    Value = Math.Abs(values[i]),
                    Error = 0.0,
                    Legend = labeles[i],
                    Brush = values[i] > 0 ? Brushes.Red : Brushes.Blue
                };
                elements.Add(elem);
            }
            if (!isOrderKept)
                elements = elements.OrderByDescending(n => n.Value).ToList();

            var chartBean = new BarChartBean(elements, title, xTitle, yTitle);
            return new BarChartUI(chartBean);
        }

        public PairwisePlotUI GetPairwisePlotUI(ObservableCollection<double> xValues, ObservableCollection<double> yValues, 
            ObservableCollection<int> indexes, ObservableCollection<string> labels, ObservableCollection<byte[]> colorByteList,
            string title, string xTitle, string yTitle, 
            PairwisePlotDisplayLabel display, double plotSize) {
            var brushes = convertRgbaToBrush(colorByteList);
            var plotBean = new PairwisePlotBean(title, xTitle, yTitle,
                   xValues, yValues, labels, brushes, indexes, display);

            return new PairwisePlotUI(plotBean) {
                PlotSize = plotSize
            };
        }

        public void Initializer() {

            // set global info
            this.SampleLabels = new ObservableCollection<string>() { "Label", "X value", "Y value", "None" };
            this.MetaboliteLabels = new ObservableCollection<string>() { "Label", "X value", "Y value", "None" };
            this.SamplePlotSizes = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24 };
            this.MetabolitePlotSizes = new ObservableCollection<int> { 1, 2, 3, 4, 5, 6, 8, 10, 12, 14, 16, 20, 24 };

            this.SelectedSampleLabel = 0;
            this.SelectedMetaboliteLabel = 0;
            this.SelectedSamplePlotSize = 5;
            this.SelectedMetabolitePlotSize = 5;

            var option = this.MAResult.MultivariateAnalysisOption;
            if (option == MultivariateAnalysisOption.Oplsda || option == MultivariateAnalysisOption.Oplsr) {

                var biofactor = this.MAResult.OptimizedFactor;
                var orthofactor = this.MAResult.OptimizedOrthoFactor;
                var bioComponents = new ObservableCollection<int>();
                for (int i = 0; i < biofactor; i++) {
                    bioComponents.Add(i + 1);
                }
                var orthoComponents = new ObservableCollection<int>();
                for (int i = 0; i < orthofactor; i++) {
                    orthoComponents.Add(i + 1);
                }
                
                this.XComponents = bioComponents;
                this.YComponents = orthoComponents;

                this.SelectedXComponent = 0;
                this.SelectedYComponent = 0;
            }
            else if (option == MultivariateAnalysisOption.Plsda || option == MultivariateAnalysisOption.Plsr) {

                var optfactor = this.MAResult.OptimizedFactor;
                var components = new ObservableCollection<int>();
                for (int i = 0; i < optfactor; i++) {
                    components.Add(i + 1);
                }
                
                this.XComponents = components;
                this.YComponents = components;

                this.SelectedXComponent = 0;
                this.SelectedYComponent = 1;
            }
            else if (option == MultivariateAnalysisOption.Pca) {

                var optfactor = this.MAResult.Contributions.Count();
                var components = new ObservableCollection<int>();
                for (int i = 0; i < optfactor; i++) {
                    components.Add(i + 1);
                }

                this.XComponents = components;
                this.YComponents = components;

                this.SelectedXComponent = 0;
                this.SelectedYComponent = 1;
            }
            this.isImported = true;
            UpdatePlots();
        }

    }
}
