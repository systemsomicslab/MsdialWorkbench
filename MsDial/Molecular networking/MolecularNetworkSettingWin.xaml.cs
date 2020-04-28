using Microsoft.Win32;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Algorithm.Clustering;
using Msdial.Lcms.Dataprocess.Utility;
using Newtonsoft.Json;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using System.Collections.ObjectModel;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for MolecularNetworkSettingWin.xaml
    /// </summary>
    public partial class MolecularNetworkSettingWin : Window {
        public MolecularNetworkSettingWin(bool isTargeted = false, bool isAlignedSpotPannel = false) {
            InitializeComponent();
            this.DataContext = new MolecularNetworkingSettingVM() { IsTargeted = isTargeted, IsAlignedSpotPannel = isAlignedSpotPannel };
        }
    }

    public class MolecularNetworkingSettingVM : ViewModelBase {

        private Ionization ionization;

        private AnalysisParametersBean paramLc;
        private AnalysisParamOfMsdialGcms paramGc;
        private MainWindow mainWindow;

        private bool isPeakSpots;
        private bool isAlignmentSpots;
        private bool isExportIonCorrelation;
        private bool isTargeted;
        private bool isAlignedSpotPannel;

        private double massTolerance;
        private double relativeAbundanceCutoff;
        private double similarityCutoff;
        private double ionCorrelationSimilarityCutoff;
        private double rtTolerance;

        /// <summary>
        /// Sets up the view model for the MolecularNetworkSettingWin window
        /// </summary>
        private DelegateCommand windowLoaded;
        public DelegateCommand WindowLoaded {
            get {
                return windowLoaded ?? (windowLoaded = new DelegateCommand(Window_Loaded, obj => { return true; }));
            }
        }

        private void Window_Loaded(object obj) {
            var view = (MolecularNetworkSettingWin)obj;
            this.mainWindow = (MainWindow)view.Owner;

            if (this.isTargeted == true) {
                if (this.isAlignedSpotPannel == true) {
                    this.IsAlignmentSpots = true;
                }
                else {
                    this.IsPeakSpots = true;
                }
                view.RadioButton_AlignmentSpots.IsEnabled = false;
                view.RadioButton_PeakSpots.IsEnabled = false;
                view.Label_Explanation.Content = "This networking is applied for the selected peak/alignment spot. \r\n" +
                    "If you want to apply this function for all of spots, please do it\r\nat Data visualization navigator.\r\n" +
                    "Importantly, please set 'Firefox' as the default html viewer\r\nfor faster loading of network";
            }
            else {
                if (this.mainWindow.TabItem_RtMzPairwisePlotAlignmentView.IsSelected)
                    this.IsAlignmentSpots = true;
                else
                    this.IsPeakSpots = true;
            }

            var project = this.mainWindow.ProjectProperty;
            if (project.Ionization == Ionization.ESI) {
                this.ionization = Ionization.ESI;
                this.paramLc = this.mainWindow.AnalysisParamForLC;

                if (this.paramLc.MnMassTolerance < 0.000001) {
                    this.MassTolerance = 0.025;
                    this.RelativeAbundanceCutoff = 1;
                    this.SimilarityCutoff = 90;
                    this.IsExportIonCorrelation = false;
                    this.IonCorrelationSimilarityCutoff = 95;
                    this.RtTolerance = 100;
                }
                else {
                    this.MassTolerance = this.paramLc.MnMassTolerance;
                    this.RelativeAbundanceCutoff = this.paramLc.MnRelativeAbundanceCutOff;
                    this.SimilarityCutoff = this.paramLc.MnSpectrumSimilarityCutOff;
                    this.IsExportIonCorrelation = this.paramLc.MnIsExportIonCorrelation;
                    this.IonCorrelationSimilarityCutoff = this.paramLc.MnIonCorrelationSimilarityCutOff;
                    this.RtTolerance = this.paramLc.MnRtTolerance;
                }
            }
            else {
                this.ionization = Ionization.EI;
                this.paramGc = this.mainWindow.AnalysisParamForGC;

                if (this.paramGc.MnMassTolerance < 0.0000001) {
                    this.MassTolerance = 0.5;
                    this.RelativeAbundanceCutoff = 1;
                    this.SimilarityCutoff = 90;
                    this.IsExportIonCorrelation = false;
                    this.IonCorrelationSimilarityCutoff = 95;
                    this.RtTolerance = 100;
                }
                else {
                    this.paramGc = this.mainWindow.AnalysisParamForGC;
                    this.MassTolerance = this.paramGc.MnMassTolerance;
                    this.RelativeAbundanceCutoff = this.paramGc.MnRelativeAbundanceCutOff;
                    this.SimilarityCutoff = this.paramGc.MnSpectrumSimilarityCutOff;
                    this.IsExportIonCorrelation = this.paramGc.MnIsExportIonCorrelation;
                    this.IonCorrelationSimilarityCutoff = this.paramGc.MnIonCorrelationSimilarityCutOff;
                    this.RtTolerance = this.paramGc.MnRtTolerance;
                }
            }

            OnPropertyChanged("MolecularNetwokringSetView");
        }

        /// <summary>
		/// Searching fragment queries
		/// </summary>
		private DelegateCommand run;
        public DelegateCommand Run {
            get {
                return run ?? (run = new DelegateCommand(winobj => {

                var view = (MolecularNetworkSettingWin)winobj;

                    if (this.ionization == Ionization.ESI) {
                        // parameter save
                        this.paramLc.MnMassTolerance = this.MassTolerance;
                        this.paramLc.MnRelativeAbundanceCutOff = this.RelativeAbundanceCutoff;
                        this.paramLc.MnSpectrumSimilarityCutOff = this.SimilarityCutoff;
                        this.paramLc.MnIsExportIonCorrelation = this.IsExportIonCorrelation;
                        this.paramLc.MnIonCorrelationSimilarityCutOff = this.IonCorrelationSimilarityCutoff;
                        this.paramLc.MnRtTolerance = this.RtTolerance;
                    }
                    else {
                        // parameter save
                        this.paramGc.MnMassTolerance = this.MassTolerance;
                        this.paramGc.MnRelativeAbundanceCutOff = this.RelativeAbundanceCutoff;
                        this.paramGc.MnSpectrumSimilarityCutOff = this.SimilarityCutoff;
                        this.paramGc.MnIsExportIonCorrelation = this.IsExportIonCorrelation;
                        this.paramGc.MnIonCorrelationSimilarityCutOff = this.IonCorrelationSimilarityCutoff;
                        this.paramGc.MnRtTolerance = this.RtTolerance;
                    }


                    if (this.isTargeted == false) {
                        if (this.ionization == Ionization.ESI) {
                            new MolecularNetworkingProcessForLC().Process(this.mainWindow, this.massTolerance,
                                this.relativeAbundanceCutoff, this.similarityCutoff, this.isExportIonCorrelation,
                                this.ionCorrelationSimilarityCutoff, this.rtTolerance, this.IsAlignmentSpots);
                            view.Close();
                        }
                        else {
                            new MolecularNetworkingProcessForGC().Process(this.mainWindow, this.massTolerance,
                                this.relativeAbundanceCutoff, this.similarityCutoff, this.isExportIonCorrelation,
                                this.ionCorrelationSimilarityCutoff, this.rtTolerance, this.IsAlignmentSpots);
                            view.Close();
                        }
                    }
                    else {
                        if (this.ionization == Ionization.ESI) {
                            new MolecularNetworkingProcessForLC().Process(this.mainWindow, this.massTolerance,
                                this.relativeAbundanceCutoff, this.similarityCutoff, this.isExportIonCorrelation,
                                this.ionCorrelationSimilarityCutoff, this.rtTolerance, this.IsAlignmentSpots, this.isTargeted);
                            view.Close();
                        }
                        else {
                            new MolecularNetworkingProcessForGC().Process(this.mainWindow, this.massTolerance,
                                this.relativeAbundanceCutoff, this.similarityCutoff, this.isExportIonCorrelation,
                                this.ionCorrelationSimilarityCutoff, this.rtTolerance, this.IsAlignmentSpots, this.isTargeted);
                            view.Close();
                        }
                    }
                }, CanRun));
            }
        }

        /// <summary>
        /// Checks whether the fragment search command can be executed or not
        /// </summary>
        /// <param name="obj"></param>
        /// <returns></returns>
        private bool CanRun(object obj) {

            if (this.mainWindow == null) return true;

            var isSelectedFile = this.mainWindow.FocusedFileID;
            var isSelectedAlignmentFile = this.mainWindow.FocusedAlignmentFileID;

            if (this.isPeakSpots && isSelectedFile < 0) return false;
            if (this.isAlignmentSpots && isSelectedAlignmentFile < 0) return false;

            return true;
        }


        /// <summary>
		/// Closes the window (on Cancel)
		/// </summary>
		private DelegateCommand cancel;
        public DelegateCommand Cancel {
            get {
                return cancel ?? (cancel = new DelegateCommand(obj => {
                    Window view = (Window)obj;
                    view.Close();
                }, obj => { return true; }));
            }
        }

        public bool IsPeakSpots {
            get {
                return isPeakSpots;
            }

            set {
                if (isPeakSpots == value) return;
                isPeakSpots = value;
                OnPropertyChanged("IsPeakSpots");
            }
        }

        public bool IsAlignmentSpots {
            get {
                return isAlignmentSpots;
            }

            set {
                if (isAlignmentSpots == value) return;
                isAlignmentSpots = value;
                OnPropertyChanged("IsAlignmentSpots");
            }
        }

        public double MassTolerance {
            get {
                return massTolerance;
            }

            set {
                if (massTolerance == value) return;
                massTolerance = value;
                OnPropertyChanged("MassTolerance");
            }
        }

        public double RelativeAbundanceCutoff {
            get {
                return relativeAbundanceCutoff;
            }

            set {
                if (relativeAbundanceCutoff == value) return;
                relativeAbundanceCutoff = value;
                OnPropertyChanged("RelativeAbundanceCutoff");
            }
        }

        public double SimilarityCutoff {
            get {
                return similarityCutoff;
            }

            set {
                if (similarityCutoff == value) return;
                similarityCutoff = value;
                OnPropertyChanged("SimilarityCutoff");
            }
        }

        public bool IsExportIonCorrelation {
            get {
                return isExportIonCorrelation;
            }

            set {
                if (IsExportIonCorrelation == value) return;
                isExportIonCorrelation = value;
                OnPropertyChanged("IsExportIonCorrelation");
            }
        }

        public double IonCorrelationSimilarityCutoff {
            get {
                return ionCorrelationSimilarityCutoff;
            }

            set {
                if (ionCorrelationSimilarityCutoff == value) return;
                ionCorrelationSimilarityCutoff = value;
                OnPropertyChanged("IonCorrelationSimilarityCutoff");
            }
        }

        public bool IsTargeted {
            get {
                return isTargeted;
            }

            set {
                isTargeted = value;
            }
        }

        public bool IsAlignedSpotPannel {
            get {
                return isAlignedSpotPannel;
            }

            set {
                isAlignedSpotPannel = value;
            }
        }

        public double RtTolerance {
            get {
                return rtTolerance;
            }

            set {
                rtTolerance = value;
                OnPropertyChanged("RtTolerance");
            }
        }
    }

    public class MolecularNetworkingProcessForLC {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "Progress: ";

        private bool isAlignedSpot;
        private bool isExportIonCorrelation;
        private bool isTargeted;

        private int focusedFileID;
        private int focusedPeakID;

        private int focusedAlignmentFileID;
        private int focusedAlignmentPeakID;

        private double massTolerance;
        private double relativeCutoff;
        private double similarityCutoff;
        private double ionCorrelationSimilarityCutOff;
        private double rtTolerance;
        #endregion

        #region // Processing method summary
        public void Process(MainWindow mainWindow, double massTolerance, double relativeCutoff, double similarityCutoff,
            bool isExportIonCorrelation, double ionCorrelationSimilarityCutOff, double rtTolerance,
            bool isAlignedSpot = false, bool isTargeted = false) {
            this.isAlignedSpot = isAlignedSpot;
            this.massTolerance = massTolerance;
            this.relativeCutoff = relativeCutoff;
            this.similarityCutoff = similarityCutoff;
            this.isExportIonCorrelation = isExportIonCorrelation;
            this.ionCorrelationSimilarityCutOff = ionCorrelationSimilarityCutOff;
            this.rtTolerance = rtTolerance;
            this.isTargeted = isTargeted;
            bgWorkerInitialize(mainWindow);

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, isAlignedSpot, isTargeted });
        }

        private void bgWorkerInitialize(MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.Title = "Generating nodes and edges for molecular network";
            this.pbw.ProgressBar_Label.Content = "0%";
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            this.focusedFileID = mainWindow.FocusedFileID;
            this.focusedPeakID = mainWindow.FocusedPeakID;

        }
        #endregion

        #region // background workers
        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var mainWindow = (MainWindow)arg[0];
            var isAlignedSpot = (bool)arg[1];
            var isTargeted = (bool)arg[2];
            var mspDB = mainWindow.MspDB;
            var param = mainWindow.AnalysisParamForLC;
            var projectProp = mainWindow.ProjectProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var colorBlushes = mainWindow.SolidColorBrushList;
            var barChartDisplayMode = mainWindow.BarChartDisplayMode;

            var mspQueries = new List<MspFormatCompoundInformationBean>();
            var rootObj = new RootObject();
            var cytoscapeNodes = new List<Node>();
            var cytoscapeEdges = new List<Edge>();

            var edgeSpecMax = 4000;
            var edgeCorrMax = 2000;

            var targetRt = -1.0F;

            MspFormatCompoundInformationBean targetQuery = null;

            if (isAlignedSpot == true) {
                #region
                var selectedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
                var alignmentResult = mainWindow.FocusedAlignmentResult;
                var fs = mainWindow.AlignViewDecFS;
                var seekpointList = mainWindow.AlignViewDecSeekPoints;
                var targetID = mainWindow.FocusedAlignmentPeakID;
                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;

                targetRt = alignedSpots[targetID].CentralRetentionTime;

                var minValue = Math.Log10(alignedSpots.Min(n => n.AverageValiable));
                var maxValue = Math.Log10(alignedSpots.Max(n => n.AverageValiable));

                foreach (var spot in alignedSpots) {
                    if (Math.Abs(targetRt - spot.CentralRetentionTime) > rtTolerance) continue;
                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, spot.AlignmentID);
                    var massSpectra = ms2Dec.MassSpectra;

                    if (massSpectra == null || massSpectra.Count == 0) continue;

                    var node = getAlignmentNodeInfoForLC(spot, mspDB, ms2Dec, massSpectra, maxValue, minValue);
                    var query = getNodeQueryAsMspObj(spot, node);
                    var chartJs = getChartJs(spot, analysisFiles, projectProp, barChartDisplayMode, isBoxPlot);

                    if (spot.AlignmentID == mainWindow.FocusedAlignmentPeakID) {
                        targetQuery = query;
                    }
                   
                    node.data.BarGraph = chartJs;

                    cytoscapeNodes.Add(node);
                    mspQueries.Add(query);
                }
                #endregion
            }
            else {
                #region
                var file = analysisFiles[this.focusedFileID];
                var fs = mainWindow.PeakViewDecFS;
                var seekpointList = mainWindow.PeakViewDecSeekPoints;

                //DataStorageLcUtility.SetPeakAreaBeanCollection(file, file.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                var peakSpots = file.PeakAreaBeanCollection;
                var specCollection = mainWindow.LcmsSpectrumCollection;

                var targetID = mainWindow.FocusedPeakID;
                targetRt = peakSpots[targetID].RtAtPeakTop;

                var minValue = Math.Log10(peakSpots.Min(n => n.IntensityAtPeakTop));
                var maxValue = Math.Log10(peakSpots.Max(n => n.IntensityAtPeakTop));

                foreach (var spot in peakSpots) {
                    if (spot.IsotopeWeightNumber != 0) continue;
                    if (spot.Ms2LevelDatapointNumber < 0) continue;
                    if (Math.Abs(targetRt - spot.RtAtPeakTop) > rtTolerance) continue;

                    var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(fs, seekpointList, spot.PeakID);
                    var massSpectra = ms2Dec.MassSpectra;

                    if (massSpectra == null || massSpectra.Count == 0) continue;
                    var node = getPeakSpotNodeForLC(spot, mspDB, ms2Dec, massSpectra, projectProp, specCollection, param, maxValue, minValue);
                    var query = getNodeQueryAsMspObj(spot, node);
                    var chartJs = getChartJs(file, spot);

                    if (spot.PeakID == mainWindow.FocusedPeakID) {
                        targetQuery = query;
                    }
                    
                    node.data.BarGraph = chartJs;

                    cytoscapeNodes.Add(node);
                    mspQueries.Add(query);
                }

                //DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(analysisFiles[this.focusedFileID]);
                #endregion
            }

            var edgeIds = new List<string>();

            var maxCoeff = isAlignedSpot == true && analysisFiles.Count >= 6 && isExportIonCorrelation == true ? 50.0 : 100.0;

            List<Msdial.Lcms.Dataprocess.Algorithm.Clustering.EdgeInformation> edges = null;

            if (isTargeted == false)
                edges = MsmsClustering.GetEdgeInformations(mspQueries, this.relativeCutoff, this.massTolerance, this.similarityCutoff, this.bgWorker, maxCoeff);
            else
                edges = MsmsClustering.GetEdgeInformations(targetQuery, mspQueries, this.relativeCutoff, this.massTolerance, this.similarityCutoff, this.bgWorker, maxCoeff);

            if (edges != null && edges.Count > 0) {
                foreach (var edge in edges.OrderByDescending(n => n.Score)) {
                    cytoscapeEdges.Add(new Edge() { data = new EdgeData() { source = int.Parse(edge.SourceComment), target = int.Parse(edge.TargetComment), linecolor = "red", score = edge.Score } });
                    if (!edgeIds.Contains(edge.SourceComment)) edgeIds.Add(edge.SourceComment);
                    if (!edgeIds.Contains(edge.TargetComment)) edgeIds.Add(edge.TargetComment);
                    if (cytoscapeEdges.Count > edgeSpecMax) break;
                }
            }

            if (maxCoeff == 0.5)
                this.bgWorker.ReportProgress(50);

            if (isAlignedSpot == true && analysisFiles.Count >= 6 && isExportIonCorrelation == true) {
                
                var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;
                edges = null;
                if (isTargeted == false)
                    edges = CorrelationClustering.GetEdgeInformations(alignedSpots.Where(n => n.MsmsIncluded == true).ToList(),
                        this.ionCorrelationSimilarityCutOff, this.rtTolerance, targetRt, this.bgWorker, maxCoeff);
                else
                    edges = CorrelationClustering.GetEdgeInformations(alignedSpots[mainWindow.FocusedAlignmentPeakID], 
                        alignedSpots.Where(n => n.MsmsIncluded == true).ToList(), 
                        this.ionCorrelationSimilarityCutOff, this.rtTolerance, targetRt,
                        this.bgWorker, maxCoeff);
                if (edges != null && edges.Count > 0) {
                    foreach (var edge in edges.OrderByDescending(n => n.Score)) {
                        cytoscapeEdges.Add(new Edge() { classes = "e_white", data = new EdgeData() { source = int.Parse(edge.SourceComment), target = int.Parse(edge.TargetComment), linecolor = "white", score = edge.Score } });
                        if (!edgeIds.Contains(edge.SourceComment)) edgeIds.Add(edge.SourceComment);
                        if (!edgeIds.Contains(edge.TargetComment)) edgeIds.Add(edge.TargetComment);
                        if (cytoscapeEdges.Count > edgeSpecMax + edgeCorrMax) break;
                    }
                }
            }

            //final node checker
            var cCytoscapeNodes = new List<Node>();
            foreach (var node in cytoscapeNodes) {
                if (edgeIds.Contains(node.data.id.ToString()))
                    cCytoscapeNodes.Add(node);
            }

            this.bgWorker.ReportProgress(100);

            rootObj.nodes = cCytoscapeNodes;
            rootObj.edges = cytoscapeEdges;
            e.Result = new object[] { mainWindow, rootObj };
        }

        private Chart getChartJs(AnalysisFileBean file, PeakAreaBean spot) {
            var chartJs = new Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            chartJs.data.labels.Add(file.AnalysisFilePropertyBean.AnalysisFileName);
            chartJsElement.data.Add(spot.IntensityAtPeakTop);
            chartJsElement.backgroundColor.Add("rgba(0, 0, 255, 0.8)");

            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }

        private MspFormatCompoundInformationBean getNodeQueryAsMspObj(PeakAreaBean spot, Node node) {
            var query = new MspFormatCompoundInformationBean() {
                Name = spot.MetaboliteName,
                RetentionTime = spot.RtAtPeakTop,
                PrecursorMz = spot.AccurateMass,
                Comment = spot.PeakID.ToString(),
                PeakNumber = node.data.MSMS.Count
            };

            query.MzIntensityCommentBeanList = new List<MzIntensityCommentBean>();
            foreach (var spec in node.data.MSMS) {
                query.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean() {
                    Mz = (float)spec[0],
                    Intensity = (float)spec[1]
                });
            }

            return query;
        }

        private Node getPeakSpotNodeForLC(PeakAreaBean spot, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2Dec, List<double[]> massSpectra, 
            ProjectPropertyBean projectProp, ObservableCollection<RawSpectrum> specCollection, AnalysisParametersBean param, double maxValue, double minValue) {
            var node = new Node() {
                //classes = "blue b_white hits",
                data = new NodeData() {
                    id = spot.PeakID,
                    Name = spot.MetaboliteName,
                    Rt = spot.RtAtPeakTop.ToString(),
                    Property = "RT " + Math.Round(spot.RtAtPeakTop, 3).ToString() + "_m/z " + Math.Round(spot.AccurateMass, 5).ToString(),
                    Mz = spot.AccurateMass.ToString(),
                    Adduct = spot.AdductIonName.ToString(),
                    IonMode = projectProp.IonMode.ToString(),
                    Formula = MspDataRetrieve.GetFormula(spot.LibraryID, mspDB),
                    InChiKey = MspDataRetrieve.GetInChIKey(spot.LibraryID, mspDB),
                    Ontology = MspDataRetrieve.GetOntology(spot.LibraryID, mspDB),
                    Smiles = MspDataRetrieve.GetSMILES(spot.LibraryID, mspDB),
                    Method = "MSMS",
                    Size = (int)((Math.Log10(spot.IntensityAtPeakTop) - minValue) / (maxValue - minValue) * 100 + 20),
                    bordercolor = "white"
                },
            };

            var backgroundcolor = "rgb(0,0,0)";
            if (spot.LibraryID >= 0 && MetaboliteColorCode.metabolite_colorcode.ContainsKey(node.data.Ontology))
                backgroundcolor = MetaboliteColorCode.metabolite_colorcode[node.data.Ontology];
            node.data.backgroundcolor = backgroundcolor;


            var ms1spec = new List<List<double>>();
            var ms2spec = new List<List<double>>();
            var ms1label = new List<string>();
            var ms2label = new List<string>();

            //var ms1Spectra = DataAccessLcUtility.GetCentroidMassSpectra(specCollection, projectProp.DataType, spot.Ms1LevelDatapointNumber, param.CentroidMs1Tolerance, true);
            //if (ms1Spectra != null && ms1Spectra.Count > 0) {
            //    var startId = DataAccessLcUtility.GetMs1StartIndex(spot.AccurateMass, 0.5F, ms1Spectra);
            //    for (int i = startId; i < ms1Spectra.Count; i++) {
            //        var mz = ms1Spectra[i][0];
            //        var intensity = ms1Spectra[i][1];

            //        if (mz < spot.AccurateMass - 0.5) continue;
            //        if (mz > spot.AccurateMass + 5.0) break;

            //        ms1spec.Add(new List<double>() { mz, intensity });
            //        ms1label.Add(Math.Round(mz, 4).ToString());
            //    }
            //}
            node.data.MS = ms1spec;
            if (ms1spec.Count > 0) node.data.MsMin = ms1spec[0][0];

            var maxIntensity = massSpectra.Max(n => n[1]);

            foreach (var spec in massSpectra) {
                if (spec[1] > maxIntensity * relativeCutoff * 0.01) {
                    ms2spec.Add(new List<double>() { spec[0], spec[1] });
                    ms2label.Add(Math.Round(spec[0], 4).ToString());
                }
            }
            node.data.MSMS = ms2spec;
            node.data.MsmsMin = ms2spec[0][0];
            node.data.MsLabel = ms1label;
            node.data.MsMsLabel = ms2label;

            return node;
        }

        private Chart getChartJs(AlignmentPropertyBean spot, ObservableCollection<AnalysisFileBean> analysisFiles,
            ProjectPropertyBean projectProp, BarChartDisplayMode barChartDisplayMode, bool isBoxPlot) {
            var barchartBean = MsDialStatistics.GetBarChartBean(spot, analysisFiles, projectProp, barChartDisplayMode, isBoxPlot);
            var chartJs = new Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            foreach (var chartElem in barchartBean.BarElements) {
                chartJs.data.labels.Add(chartElem.Legend);
                chartJsElement.data.Add(chartElem.Value);
                chartJsElement.backgroundColor.Add("rgba(" + chartElem.Brush.Color.R + "," + chartElem.Brush.Color.G + "," + chartElem.Brush.Color.B + ", 0.8)");
            }
            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }

        private MspFormatCompoundInformationBean getNodeQueryAsMspObj(AlignmentPropertyBean spot, Node node) {
            var query = new MspFormatCompoundInformationBean() {
                Name = spot.MetaboliteName,
                RetentionTime = spot.CentralRetentionTime,
                PrecursorMz = spot.CentralAccurateMass,
                Comment = spot.AlignmentID.ToString(),
                PeakNumber = node.data.MSMS.Count
            };

            query.MzIntensityCommentBeanList = new List<MzIntensityCommentBean>();
            foreach (var spec in node.data.MSMS) {
                query.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean() {
                    Mz = (float)spec[0],
                    Intensity = (float)spec[1]
                });
            }

            return query;
        }

        private Node getAlignmentNodeInfoForLC(AlignmentPropertyBean spot, List<MspFormatCompoundInformationBean> mspDB, MS2DecResult ms2Dec, List<double[]> massSpectra, double maxValue, double minValue) {

            var ionMode = AdductIonParcer.GetAdductIonBean(spot.AdductIonName).IonMode.ToString();
            if (ionMode == null || ionMode == string.Empty)
                ionMode = "Positive";

            var node = new Node() {
                //classes = "fuchsia b_pink hits",
                data = new NodeData() {
                    id = spot.AlignmentID,
                    Name = spot.MetaboliteName,
                    Rt = spot.CentralRetentionTime.ToString(),
                    Mz = spot.CentralAccurateMass.ToString(),
                    Adduct = spot.AdductIonName.ToString(),
                    IonMode = ionMode,
                    Method = "MSMS",
                    Property = "RT " + Math.Round(spot.CentralRetentionTime, 3).ToString() + "_m/z " + Math.Round(spot.CentralAccurateMass, 5).ToString(),
                    Formula = MspDataRetrieve.GetFormula(spot.LibraryID, mspDB),
                    InChiKey = MspDataRetrieve.GetInChIKey(spot.LibraryID, mspDB),
                    Ontology = MspDataRetrieve.GetOntology(spot.LibraryID, mspDB),
                    Smiles = MspDataRetrieve.GetSMILES(spot.LibraryID, mspDB),
                    Size = (int)((Math.Log10(spot.AverageValiable) - minValue) / (maxValue - minValue) * 100 + 20),
                    bordercolor = "white"
                },
            };

            var backgroundcolor = "rgb(0,0,0)";
            if (spot.LibraryID >= 0 && MetaboliteColorCode.metabolite_colorcode.ContainsKey(node.data.Ontology))
                backgroundcolor = MetaboliteColorCode.metabolite_colorcode[node.data.Ontology];
            node.data.backgroundcolor = backgroundcolor;

            var ms1spec = new List<List<double>>();
            var ms2spec = new List<List<double>>();
            var ms1label = new List<string>();
            var ms2label = new List<string>();

            ms1spec.Add(new List<double>() { ms2Dec.Ms1AccurateMass, ms2Dec.Ms1PeakHeight });
            ms1spec.Add(new List<double>() { ms2Dec.Ms1AccurateMass + 1.00335, ms2Dec.Ms1IsotopicIonM1PeakHeight });
            ms1spec.Add(new List<double>() { ms2Dec.Ms1AccurateMass + 2.00671, ms2Dec.Ms1IsotopicIonM2PeakHeight });

            ms1label.Add(Math.Round(ms2Dec.Ms1AccurateMass, 4).ToString());
            ms1label.Add(Math.Round(ms2Dec.Ms1AccurateMass + 1.00335, 4).ToString());
            ms1label.Add(Math.Round(ms2Dec.Ms1AccurateMass + 2.00671, 4).ToString());

            node.data.MS = ms1spec;
            node.data.MsMin = ms2Dec.Ms1AccurateMass;

            var maxIntensity = massSpectra.Max(n => n[1]);

            foreach (var spec in massSpectra) {
                if (spec[1] > maxIntensity * relativeCutoff * 0.01) {
                    ms2spec.Add(new List<double>() { spec[0], spec[1] });
                    ms2label.Add(Math.Round(spec[0], 4).ToString());
                }
            }
            node.data.MSMS = ms2spec;
            node.data.MsmsMin = ms2spec[0][0];
            node.data.MsLabel = ms1label;
            node.data.MsMsLabel = ms2label;

            return node;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            this.pbw.ProgressView.Value = e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + " " + e.ProgressPercentage + "%";
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();

            object[] arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];
            var rootObj = (RootObject)arg[1];

            var json = JsonConvert.SerializeObject(rootObj, Formatting.Indented);
            var curDir = System.AppDomain.CurrentDomain.BaseDirectory;
            var exportpath = curDir + "CytoscapeLocalBrowser/data/elements.js";
            using (StreamWriter sw = new StreamWriter(exportpath, false, Encoding.ASCII)) {
                sw.WriteLine("var dataElements =\r\n" + json.ToString() + "\r\n;");
            }

            var rKey = getDefaultExePath(@"http\shell\open\command");
            var url = curDir + "/CytoscapeLocalBrowser/MsdialCytoscapeViewer.html";
            var error = false;
            try {
                System.Diagnostics.Process.Start(url);
            } catch (Win32Exception ex) {
                error = true;
            }
            catch (ObjectDisposedException ex) {
                error = true;
            }
            catch (FileNotFoundException ex) {
                error = true;
            }
            if (focusedAlignmentFileID < 0) {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }
            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static string getDefaultExePath(string keyPath) {
            string path = "";

            RegistryKey rKey = Registry.ClassesRoot.OpenSubKey(keyPath);
            if (rKey != null) {
                string command = (string)rKey.GetValue(String.Empty);
                if (command == null) {
                    return path;
                }

                command = command.Trim();
                if (command.Length == 0) {
                    return path;
                }

                if (command[0] == '"') {
                    int endIndex = command.IndexOf('"', 1);
                    if (endIndex != -1) {
                        path = command.Substring(1, endIndex - 1);
                    }
                }
                else {
                    int endIndex = command.IndexOf(' ');
                    if (endIndex != -1) {
                        path = command.Substring(0, endIndex);
                    }
                    else {
                        path = command;
                    }
                }
            }
            return path;
        }


        #endregion
    }

    
    public class MolecularNetworkingProcessForGC
    {
        #region // members
        private BackgroundWorker bgWorker;
        private ProgressBarWin pbw;
        private string progressHeader = "Progress: ";

        private bool isAlignedSpot;
        private bool isExportIonCorrelation;
        private bool isTargeted;

        private int focusedFileID;
        private int focusedPeakID;

        private int focusedAlignmentFileID;
        private int focusedAlignmentPeakID;

        private double massTolerance;
        private double relativeCutoff;
        private double similarityCutoff;
        private double ionCorrelationSimilarityCutOff;
        private double rtTolerance;
        #endregion

        #region // Processing method summary
        public void Process(MainWindow mainWindow, double massTolerance, double relativeCutoff, double similarityCutoff,
            bool isExportIonCorrelation, double ionCorrelationSimilarityCutOff, double rtTolerance,
            bool isAlignedSpot = false, bool isTargeted = false) {
            this.isAlignedSpot = isAlignedSpot;
            this.massTolerance = massTolerance;
            this.relativeCutoff = relativeCutoff;
            this.similarityCutoff = similarityCutoff;
            this.isExportIonCorrelation = isExportIonCorrelation;
            this.ionCorrelationSimilarityCutOff = ionCorrelationSimilarityCutOff;
            this.isTargeted = isTargeted;
            this.rtTolerance = rtTolerance;
            bgWorkerInitialize(mainWindow);

            this.bgWorker.DoWork += new DoWorkEventHandler(bgWorker_Process_DoWork);
            this.bgWorker.RunWorkerAsync(new Object[] { mainWindow, isAlignedSpot, isTargeted });
        }

        private void bgWorkerInitialize(MainWindow mainWindow) {
            mainWindow.IsEnabled = false;
            Mouse.OverrideCursor = Cursors.Wait;

            this.pbw = new ProgressBarWin();
            this.pbw.Owner = mainWindow;
            this.pbw.Title = "Generating nodes and edges for molecular network";
            this.pbw.ProgressBar_Label.Content = "0%";
            this.pbw.ProgressView.Minimum = 0;
            this.pbw.ProgressView.Maximum = 100;
            this.pbw.ProgressView.Value = 0;
            this.pbw.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            this.pbw.Show();

            this.bgWorker = new BackgroundWorker();
            this.bgWorker.WorkerReportsProgress = true;
            this.bgWorker.WorkerSupportsCancellation = true;
            this.bgWorker.ProgressChanged += new ProgressChangedEventHandler(bgWorker_ProgressChanged);
            this.bgWorker.RunWorkerCompleted += new RunWorkerCompletedEventHandler(bgWorker_RunWorkerCompleted);

            this.focusedFileID = mainWindow.FocusedFileID;
            this.focusedPeakID = mainWindow.FocusedPeakID;
        }
        #endregion

        #region // background workers
        private void bgWorker_Process_DoWork(object sender, DoWorkEventArgs e) {
            object[] arg = (object[])e.Argument;

            var mainWindow = (MainWindow)arg[0];
            var isAlignedSpot = (bool)arg[1];
            var isTargeted = (bool)arg[2];
            var mspDB = mainWindow.MspDB;
            var param = mainWindow.AnalysisParamForGC;
            var projectProp = mainWindow.ProjectProperty;
            var analysisFiles = mainWindow.AnalysisFiles;
            var colorBlushes = mainWindow.SolidColorBrushList;
            var barChartDisplayMode = mainWindow.BarChartDisplayMode;
            var isBoxPlot = mainWindow.ProjectProperty.IsBoxPlotForAlignmentResult;

            var mspQueries = new List<MspFormatCompoundInformationBean>();
            var rootObj = new RootObject();
            var cytoscapeNodes = new List<Node>();
            var cytoscapeEdges = new List<Edge>();

            var edgeSpecMax = 4000;
            var edgeCorrMax = 2000;
            var targetRt = -1.0F;

            MspFormatCompoundInformationBean targetQuery = null;
            if (isAlignedSpot == true) {
                #region
                var selectedAlignmentFileID = mainWindow.FocusedAlignmentFileID;
                var alignmentResult = mainWindow.FocusedAlignmentResult;
                var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                var ms1DecResults = mainWindow.FocusedAlignmentMS1DecResults;
                var targetID = mainWindow.FocusedAlignmentPeakID;
                targetRt = alignedSpots[targetID].CentralRetentionTime;
                var minValue = Math.Log10(alignedSpots.Min(n => n.AverageValiable));
                var maxValue = Math.Log10(alignedSpots.Max(n => n.AverageValiable));

                foreach (var spot in alignedSpots) {
                    if (Math.Abs(targetRt - spot.CentralRetentionTime) > rtTolerance) continue;

                    var ms1Dec = ms1DecResults[spot.AlignmentID];
                    var massSpectra = ms1Dec.Spectrum;

                    if (massSpectra == null || massSpectra.Count == 0) continue;
                    var node = getNodeQuery(spot, mspDB, projectProp, maxValue, minValue, ms1Dec, massSpectra);
                    var query = getQueryAsMspObj(spot, node);
                    var chartJs = getChartJs(spot, analysisFiles, projectProp, barChartDisplayMode, isBoxPlot);

                    if (spot.AlignmentID == mainWindow.FocusedAlignmentPeakID) {
                        targetQuery = query;
                    }

                    node.data.BarGraph = chartJs;
                    cytoscapeNodes.Add(node);
                    mspQueries.Add(query);
                }
                #endregion
            }
            else {
                #region
                var file = analysisFiles[this.focusedFileID];
                var ms1DecResults = mainWindow.Ms1DecResults;
                var minValue = Math.Log10(ms1DecResults.Min(n => n.BasepeakHeight));
                var maxValue = Math.Log10(ms1DecResults.Max(n => n.BasepeakHeight));
                var targetID = mainWindow.FocusedMS1DecID;
                targetRt = ms1DecResults[targetID].RetentionTime;

                foreach (var result in ms1DecResults) {
                    if (Math.Abs(targetRt - result.RetentionTime) > rtTolerance) continue;
                    var massSpectra = result.Spectrum;
                    if (massSpectra == null || massSpectra.Count == 0) continue;

                    var node = getNodeQuery(result, mspDB, projectProp, massSpectra, maxValue, minValue);
                    var query = getQueryAsMspObj(node, result);
                    var chartJs = getChartJs(file, result);

                    if (result.Ms1DecID == mainWindow.FocusedMS1DecID) {
                        targetQuery = query;
                    }

                    node.data.BarGraph = chartJs;
                    cytoscapeNodes.Add(node);
                    mspQueries.Add(query);
                }
                #endregion
            }

            var edgeIds = new List<string>();

            var maxCoeff = isAlignedSpot == true && analysisFiles.Count >= 6 && isExportIonCorrelation == true ? 0.5 : 1.0;


            List<Msdial.Gcms.Dataprocess.Algorithm.EdgeInformation> edges = null;
            if (isTargeted == false)
                edges = EimsClustering.GetEdgeInformations(mspQueries, this.similarityCutoff, this.massTolerance, this.bgWorker, maxCoeff);
            else
                edges = EimsClustering.GetEdgeInformations(targetQuery, mspQueries, this.similarityCutoff, this.massTolerance, this.bgWorker, maxCoeff);

            foreach (var edge in edges.OrderByDescending(n => n.Score)) {
                cytoscapeEdges.Add(new Edge() { data = new EdgeData() { source = int.Parse(edge.Source), target = int.Parse(edge.Target), linecolor = "red", score = edge.Score } });
                if (!edgeIds.Contains(edge.Source)) edgeIds.Add(edge.Source);
                if (!edgeIds.Contains(edge.Target)) edgeIds.Add(edge.Target);
                if (cytoscapeEdges.Count > edgeSpecMax) break;
            }

            if (maxCoeff == 0.5)
                this.bgWorker.ReportProgress(50);

            if (isAlignedSpot == true && analysisFiles.Count >= 6 && isExportIonCorrelation == true) {

                var alignedSpots = mainWindow.FocusedAlignmentResult.AlignmentPropertyBeanCollection;

                List<Msdial.Lcms.Dataprocess.Algorithm.Clustering.EdgeInformation> edgelist = null;
                if (isTargeted == false)
                    edgelist = CorrelationClustering.GetEdgeInformations(alignedSpots.ToList(), 
                        this.ionCorrelationSimilarityCutOff, this.rtTolerance, targetRt, this.bgWorker, maxCoeff);
                else
                    edgelist = CorrelationClustering.GetEdgeInformations(alignedSpots[mainWindow.FocusedAlignmentPeakID], 
                        alignedSpots.ToList(), this.ionCorrelationSimilarityCutOff, this.rtTolerance, targetRt,
                        this.bgWorker, maxCoeff);

                foreach (var edge in edgelist.OrderByDescending(n => n.Score)) {
                    cytoscapeEdges.Add(new Edge() {
                        classes = "e_white",
                        data = new EdgeData() {
                            source = int.Parse(edge.SourceComment),
                            target = int.Parse(edge.TargetComment),
                            linecolor = "white",
                            score = edge.Score }
                    });
                    if (!edgeIds.Contains(edge.SourceComment)) edgeIds.Add(edge.SourceComment);
                    if (!edgeIds.Contains(edge.TargetComment)) edgeIds.Add(edge.TargetComment);
                    if (cytoscapeEdges.Count > edgeSpecMax + edgeCorrMax) break;
                }
            }

            //final node checker
            var cCytoscapeNodes = new List<Node>();
            foreach (var node in cytoscapeNodes) {
                if (edgeIds.Contains(node.data.id.ToString()))
                    cCytoscapeNodes.Add(node);
            }

            this.bgWorker.ReportProgress(100);

            rootObj.nodes = cCytoscapeNodes;
            rootObj.edges = cytoscapeEdges;
            e.Result = new object[] { mainWindow, rootObj };
        }

        private Chart getChartJs(AnalysisFileBean file, MS1DecResult result) {
            var chartJs = new Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            chartJs.data.labels.Add(file.AnalysisFilePropertyBean.AnalysisFileName);
            chartJsElement.data.Add(result.BasepeakHeight);
            chartJsElement.backgroundColor.Add("rgba(0, 0, 255, 0.8)");

            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }

        private MspFormatCompoundInformationBean getQueryAsMspObj(Node node, MS1DecResult result) {
            
            var query = new MspFormatCompoundInformationBean() {
                Name = node.data.Name,
                RetentionIndex = result.RetentionIndex,
                RetentionTime = result.RetentionTime,
                PrecursorMz = result.BasepeakMz,
                Comment = result.Ms1DecID.ToString(),
                PeakNumber = node.data.MS.Count
            };

            query.MzIntensityCommentBeanList = new List<MzIntensityCommentBean>();

            var ms1spec = new List<List<double>>();
            var ms1label = new List<string>();

            foreach (var spec in node.data.MS) {
                query.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean() {
                    Mz = (float)spec[0],
                    Intensity = (float)spec[1]
                });
            }
            return query;
        }

        private Node getNodeQuery(MS1DecResult result, List<MspFormatCompoundInformationBean> mspDB, 
            ProjectPropertyBean projectProp, List<Peak> massSpectra, double maxValue, double minValue) {

            var node = new Node() {
                //classes = "blue b_white hits",
                data = new NodeData() {
                    id = result.Ms1DecID,
                    Name = MspDataRetrieve.GetCompoundName(result.MspDbID, mspDB),
                    Rt = result.RetentionTime.ToString(),
                    Property = "RT " + Math.Round(result.RetentionTime, 3).ToString() + "_QuantMass " + Math.Round(result.BasepeakMz, 5).ToString(),
                    Mz = result.BasepeakMz.ToString(),
                    IonMode = projectProp.IonMode.ToString(),
                    Formula = MspDataRetrieve.GetFormula(result.MspDbID, mspDB),
                    InChiKey = MspDataRetrieve.GetInChIKey(result.MspDbID, mspDB),
                    Ontology = MspDataRetrieve.GetOntology(result.MspDbID, mspDB),
                    Smiles = MspDataRetrieve.GetSMILES(result.MspDbID, mspDB),
                    Method = "EIMS",
                    Size = (int)((Math.Log10(result.BasepeakHeight) - minValue) / (maxValue - minValue) * 100 + 20),
                    bordercolor = "white"
                },
            };

            var backgroundcolor = "rgb(0,0,0)";
            if (result.MspDbID >= 0 && MetaboliteColorCode.metabolite_colorcode.ContainsKey(node.data.Ontology))
                backgroundcolor = MetaboliteColorCode.metabolite_colorcode[node.data.Ontology];
            node.data.backgroundcolor = backgroundcolor;
            
            var ms1spec = new List<List<double>>();
            var ms1label = new List<string>();

            var maxIntensity = massSpectra.Max(n => n.Intensity);
            foreach (var spec in massSpectra) {
                if (spec.Intensity > maxIntensity * relativeCutoff * 0.01) {
                    ms1spec.Add(new List<double>() { spec.Mz, spec.Intensity });
                    ms1label.Add(Math.Round(spec.Mz, 4).ToString());
                }
            }

            node.data.MS = ms1spec;
            node.data.MsMin = massSpectra.Min(n => n.Mz);
            node.data.MsLabel = ms1label;

            return node;
        }


        private Chart getChartJs(AlignmentPropertyBean spot, ObservableCollection<AnalysisFileBean> analysisFiles, 
            ProjectPropertyBean projectProp, BarChartDisplayMode barChartDisplayMode, bool isBoxPlot) {
            var barchartBean = MsDialStatistics.GetBarChartBean(spot, analysisFiles, projectProp, barChartDisplayMode, isBoxPlot);
            var chartJs = new Chart() { type = "bar", data = new ChartData() };
            chartJs.data.labels = new List<string>();
            chartJs.data.datasets = new List<ChartElement>();
            var chartJsElement = new ChartElement() { label = "", data = new List<double>(), backgroundColor = new List<string>() };

            foreach (var chartElem in barchartBean.BarElements) {
                chartJs.data.labels.Add(chartElem.Legend);
                chartJsElement.data.Add(chartElem.Value);
                chartJsElement.backgroundColor.Add("rgba(" + chartElem.Brush.Color.R + "," + chartElem.Brush.Color.G + "," + chartElem.Brush.Color.B + ", 0.8)");
            }
            chartJs.data.datasets.Add(chartJsElement);

            return chartJs;
        }

        private MspFormatCompoundInformationBean getQueryAsMspObj(AlignmentPropertyBean spot, Node node) {
            var query = new MspFormatCompoundInformationBean() {
                Name = node.data.Name,
                RetentionIndex = spot.CentralRetentionIndex,
                RetentionTime = spot.CentralRetentionTime,
                PrecursorMz = spot.QuantMass,
                Comment = spot.AlignmentID.ToString(),
                PeakNumber = node.data.MS.Count
            };

            query.MzIntensityCommentBeanList = new List<MzIntensityCommentBean>();

            foreach (var spec in node.data.MS) {
                query.MzIntensityCommentBeanList.Add(new MzIntensityCommentBean() {
                    Mz = (float)spec[0],
                    Intensity = (float)spec[1]
                });
            }

            return query;
        }

        private Node getNodeQuery(AlignmentPropertyBean spot, List<MspFormatCompoundInformationBean> mspDB, ProjectPropertyBean projectProp,
            double maxValue, double minValue, MS1DecResult ms1Dec, List<Peak> massSpectra) {
            var node = new Node() {
                data = new NodeData() {
                    id = spot.AlignmentID,
                    Name = MspDataRetrieve.GetCompoundName(ms1Dec.MspDbID, mspDB),
                    Rt = spot.CentralRetentionTime.ToString(),
                    Ri = spot.CentralRetentionIndex.ToString(),
                    Mz = spot.QuantMass.ToString(),
                    IonMode = projectProp.IonMode.ToString(),
                    Property = "RT " + Math.Round(spot.CentralRetentionTime, 3).ToString() + "_QuantMass " + Math.Round(spot.QuantMass, 5).ToString(),
                    Formula = MspDataRetrieve.GetFormula(spot.LibraryID, mspDB),
                    InChiKey = MspDataRetrieve.GetInChIKey(spot.LibraryID, mspDB),
                    Ontology = MspDataRetrieve.GetOntology(spot.LibraryID, mspDB),
                    Smiles = MspDataRetrieve.GetSMILES(spot.LibraryID, mspDB),
                    Method = "EIMS",
                    Size = (int)((Math.Log10(spot.AverageValiable) - minValue) / (maxValue - minValue) * 100 + 20),
                    bordercolor = "white"
                },
            };

            var backgroundcolor = "rgb(0,0,0)";
            if (spot.LibraryID >= 0 && MetaboliteColorCode.metabolite_colorcode.ContainsKey(node.data.Ontology))
                backgroundcolor = MetaboliteColorCode.metabolite_colorcode[node.data.Ontology];
            node.data.backgroundcolor = backgroundcolor;
            
            var ms1spec = new List<List<double>>();
            var ms1label = new List<string>();
            var maxIntensity = massSpectra.Max(n => n.Intensity);

            foreach (var spec in massSpectra) {
                if (spec.Intensity > maxIntensity * relativeCutoff * 0.01) {
                    ms1spec.Add(new List<double>() { spec.Mz, spec.Intensity });
                    ms1label.Add(Math.Round(spec.Mz, 4).ToString());
                }
            }

            node.data.MS = ms1spec;
            node.data.MsMin = massSpectra.Min(n => n.Mz);
            node.data.MsLabel = ms1label;

            return node;
        }

        private void bgWorker_ProgressChanged(object sender, ProgressChangedEventArgs e) {

            this.pbw.ProgressView.Value = e.ProgressPercentage;
            this.pbw.ProgressBar_Label.Content = this.progressHeader + " " + e.ProgressPercentage + "%";
        }

        private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e) {
            this.pbw.Close();

            object[] arg = (object[])e.Result;

            var mainWindow = (MainWindow)arg[0];
            var rootObj = (RootObject)arg[1];

            var json = JsonConvert.SerializeObject(rootObj, Formatting.Indented);
            var curDir = System.AppDomain.CurrentDomain.BaseDirectory;

            var exportpath = curDir + "CytoscapeLocalBrowser/data/elements.js";
            using (StreamWriter sw = new StreamWriter(exportpath, false, Encoding.ASCII)) {
                sw.WriteLine("var dataElements =\r\n" + json.ToString() + "\r\n;");
            }

            var rKey = getDefaultExePath(@"http\shell\open\command");
            var url = curDir + "/CytoscapeLocalBrowser/MsdialCytoscapeViewer.html";
            var error = false;
            try {
                System.Diagnostics.Process.Start(url);
            }
            catch (Win32Exception ex) {
                error = true;
            }
            catch (ObjectDisposedException ex) {
                error = true;
            }
            catch (FileNotFoundException ex) {
                error = true;
            }

           
            if (focusedAlignmentFileID < 0) {
                Mouse.OverrideCursor = null;
                mainWindow.IsEnabled = true;
                return;
            }

            Mouse.OverrideCursor = null;
            mainWindow.IsEnabled = true;
        }

        private static string getDefaultExePath(string keyPath) {
            string path = "";

            RegistryKey rKey = Registry.ClassesRoot.OpenSubKey(keyPath);
            if (rKey != null) {
                string command = (string)rKey.GetValue(String.Empty);
                if (command == null) {
                    return path;
                }

                command = command.Trim();
                if (command.Length == 0) {
                    return path;
                }

                if (command[0] == '"') {
                    int endIndex = command.IndexOf('"', 1);
                    if (endIndex != -1) {
                        path = command.Substring(1, endIndex - 1);
                    }
                }
                else {
                    int endIndex = command.IndexOf(' ');
                    if (endIndex != -1) {
                        path = command.Substring(0, endIndex);
                    }
                    else {
                        path = command;
                    }
                }
            }
            return path;
        }


        #endregion
    }
}
