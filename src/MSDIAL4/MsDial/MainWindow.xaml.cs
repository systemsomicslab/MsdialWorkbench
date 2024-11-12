using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using BCDev.XamlToys; // This comes from http://xamltoys.codeplex.com/. Unfortunately the repo no longer exists. We gratefully use+modify it here.
using Microsoft.Win32;
using edu.ucdavis.fiehnlab.MonaExport.Windows;
using edu.ucdavis.fiehnlab.MonaExport.DataObjects;
using Msdial.Lcms.Dataprocess.Utility;
using Msdial.Gcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv.Help;
using Common.BarChart;
using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Algorithm;
using System.Diagnostics;
using System.Linq;
using edu.ucdavis.fiehnlab.MonaRestApi.model;
using System.Text;
using edu.ucdavis.fiehnlab.MonaExport.ViewModels;
using NSSplash.impl;
using System.Threading.Tasks;
using Msdial.Lcms.Dataprocess.Test;
using System.ComponentModel.DataAnnotations;
using CompMs.RawDataHandler.Core;
using Riken.Metabolomics.Lipoquality;
using Riken.Metabolomics.Msdial.Pathway;
using CompMs.Common.MessagePack;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv
{

    /*
         * Summary for MS-DIAL programing
         *
         * List<double[]> peaklist: shows data point list of chromatogram; double[] { scan number, retention time [min], base peak m/z, base peak intensity }
         * List<double[]> masslist: shows data point list of mass spectrum; double[] { m/z, intensity }
         *
         * .abf (binary) includes raw data. How to use => look at "private void bgWorker_AllProcess_DoWork(object sender, DoWorkEventArgs e)" of AnalysisFileProcess.cs
         *
         * .mtb (serialized) stores SavePropertyBean. How to use => look at "private void bgWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)" of AnalysisFileProcess.cs
         *
         * .med (serialized) stores data processing parameters. How to use => look at "private void saveParametersMenu_Click(object sender, RoutedEventArgs e)" of MainWindow.xaml.cs
         *
         * .pai (serialized) stores the peak detection result, i.e. ObservableCollection<PeakAreaBean>. How to use => look at "private void bgWorker_AllProcess_DoWork(object sender, DoWorkEventArgs e)" of AnalysisFileProcess.cs
         *
         * .arf (serialized) stores the alignment result, i.e. ObservableCollection<AlignmentPropertyBean>. How to use => look at "private void bgWorker_DoWork(object sender, DoWorkEventArgs e)" of JointAlignerProcess.cs
         *
         * .lbm (ascii) stores the LipidBlast MS/MS spectrum as the MSP format. How to use => "public LipidDbSetVM(MainWindow mainWindow, Window window)" of LipidDbSetVM.cs
         *
         * MS-DIAL utilizes some resources including .lbm and others prepared in 'Resources' folder. These resources will be retrieved when MS-DIAL is opened or when the project will be started.
         * How to use. => follow "private void newProjectMenu_Click(object sender, RoutedEventArgs e)" of MainWindow.xaml.cs
         *
         * .dcl (binary) stores the deconvolution result as described below. How to use => look at "private static void writeMS2DecResult" of SpectralDeconvolution.cs
         *
         * The main algorithm is mainly written in "Dataprocessing" assembly and IdaModeDataProcessing.cs and SwathDataAnalyzerDataprocessing.
         *
         * The data property is mainly stored in the C# class library of "Bean" folder of "MSDIAL" assembly and "Common" assembly.
         *
         * The graphical user interface is mainly constructed by MVVM consisting of "Bean", "ViewModel", and "Window" folders.
         *
         * <Deconvolution file (.dcl)>
         * [first header] Ver. Number
         * [second header] number of PeakAreaBean's collection
         * [third header] seekpointer list
         * (peak 1: meta data -> spectra -> datapoint detail)
         * (float)peak top retention time
         * (float)ms1 accurate mass
         * (float)unique mass
         * (float)ms1 peak height
         * (float)ms1 Isotopic ion [M+1] peak height
         * (float)ms1 Isotopic ion [M+2] peak height
         * (float)deconvoluted ions total area
         * [(int)spectra number:SN]
         * [(int)data point number:DN]
         * [(float)mz1], [(int)intensity1](spectrum1)
         * [(float)mz2], [(int)intensity2](spectrum2)
         * [(float)mz3], [(int)intensity3](spectrum3)
         * *********************************************
         * [(float)mzSN], [(int)intensitySN](spectrumSN)
         * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint1)
         * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint2)
         * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapoint3)
         * ********************************************************************************************
         * [(int)scan num.], [(float)rt], [(float)BasePeakMz], [(int)BasePeakIntensity](datapointDN)
         *
         *  (peak 2: unique mass -> spectra -> datapoint detail)
         * </Deconvolution>
         *
         */


    /// <summary>
    /// Enum properties to deal with MS-DIAL project
    /// </summary>
    public enum SaveFileFormat { abf, mtd, dcl, arf, med, pai, mat, lbm, dcm, ldp, ldn, ms }
    //public enum ExportSpectraFileFormat { mgf, msp, txt, mat }
    //public enum ExportspectraType { profile, centroid, deconvoluted }
    public enum MatExportOption { OnlyFocusedPeak, UnknownPeaks, UnknownPeaksWithoutIsotope, AllPeaks, IdentifiedPeaks, MsmsPeaks, MonoisotopicAndMsmsPeaks }
    public enum ReversibleMassSpectraView { raw, component }
    public enum MrmChromatogramView { raw, component, both }
    public enum PairwisePlotFocus { peakView, alignmentView, scoreplotView, loadingplotView }
    public enum MsMsDisplayFocus { ActVsRef, Ms2Chrom, RawVsDeco, RepVsRef }
    public enum BarChartDisplayMode { OriginalHeight, NormalizedHeight, OriginalArea, NormalizedArea }

    /// <summary>
    /// MainWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MainWindow : Window, INotifyPropertyChanged, IDataErrorInfo {

        #region // member variables
        //Window property
        private static MainWindow mainWindow;
        private MainWindowDisplayVM mainWindowDisplayVM;
        public string MainWindowTitle { get; set; }

        //Save property
        private SavePropertyBean saveProperty;

        //Rdam property
        private RawDataAccess rawDataAccess;
        private RawMeasurement rawMeasurement;
        private ObservableCollection<RawSpectrum> lcmsSpectrumCollection;
        private ObservableCollection<RawSpectrum> accumulatedMs1Specra;
        private List<RawSpectrum> gcmsSpectrumList;

        //Deconvolution file stream property
        private FileStream peakViewDecFS;
        private List<long> peakViewDecSeekPoints;
        private MS2DecResult peakViewMS2DecResult;

        private FileStream alignViewDecFS;
        private List<long> alignViewDecSeekPoints;
        private MS2DecResult alignViewMS2DecResult;

        // alignedEIC
        private FileStream alignEicFS;
        private List<long> alignEicSeekPoints;
        private AlignedData alignEicResult;
        private AlignedData alignEicResultOnDrift;

        //GCMS file properties
        private List<PeakAreaBean> gcmsPeakAreaList;
        private List<MS1DecResult> ms1DecResults;

        //Project folder path
        private ProjectPropertyBean projectProperty;

        //Analysis file property list
        private ObservableCollection<AnalysisFileBean> analysisFiles;

        //Alignment file property list
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private AlignmentResultBean focusedAlignmentResult;
        private List<MS1DecResult> focusedAlignmentMS1DecResults;

        //Ion mobility property list
        private ObservableCollection<DriftSpotBean> driftSpotBeanList;
        private ObservableCollection<AlignedDriftSpotPropertyBean> alignedDriftSpotBeanList;

        //Analysis parameters property
        private AnalysisParametersBean analysisParamForLC;
        private AnalysisParamOfMsdialGcms analysisParamForGC;

        //Rdam property
        private RdamPropertyBean rdamProperty;

        //Database property
        private List<MspFormatCompoundInformationBean> mspDB;
        private List<PostIdentificatioinReferenceBean> postIdentificationTxtDB;
        private List<PostIdentificatioinReferenceBean> targetFormulaLibrary;
        //IUPAC property
        private IupacReferenceBean iupacReference;

        //PCA bean
        //private PrincipalComponentAnalysisResult pcaBean;
        //private PcaResultWin pcaResultWin;

        //PLS result
        private MultivariateAnalysisResult multivariateAnalysisResult;
        private MultivariateAnalysisResultWin multivariateAnalysisResultWin;

        //HCA result
        private HcaResultWin hcaResultWin;

        //Focus
        private int focusedFileID;
        private int focusedPeakID;
        private int focusedMS1DecID;
        private int focusedDriftSpotID;
        private int focusedMasterID;
        private DriftSpotBean selectedPeakViewDriftSpot;
        private AlignedDriftSpotPropertyBean selectedAlignmentViewDriftSpot;

        private int focusedAlignmentFileID;
        private int focusedAlignmentPeakID;
        private int focusedAlignmentMs1DecID;
        private int focusedAlignmentDriftID;
        private int focusedAlignmentMasterID;

        private int displayFocusId;
        private double displayFocusRt;
        private double displayFocusMz;

        private PairwisePlotFocus pairwisePlotFocus;
        private MsMsDisplayFocus msmsDisplayFocus;
        private bool propertyCheck;
        private bool windowOpened;

        //Toggle buttons
        private ReversibleMassSpectraView reversiblaMassSpectraViewEnum;
        private MrmChromatogramView mrmChromatogramViewEnum;

        //Menu items enables
        private bool analysisProcessed;
        private bool statisticsProcessed;
        private bool isAlignedEicFileExist;

        //INI field
        private MsDialIniField msdialIniField;

        //Solid color
        private List<SolidColorBrush> solidColorBrushList = new List<SolidColorBrush>() { Brushes.Blue, Brushes.Red, Brushes.Green
            , Brushes.DarkBlue, Brushes.DarkRed, Brushes.DarkGreen, Brushes.DeepPink, Brushes.OrangeRed
            , Brushes.Purple, Brushes.Crimson, Brushes.DarkGoldenrod, Brushes.Black, Brushes.BlanchedAlmond
            , Brushes.BlueViolet, Brushes.Brown, Brushes.BurlyWood, Brushes.CadetBlue, Brushes.Aquamarine
            , Brushes.Yellow, Brushes.Crimson, Brushes.Chartreuse, Brushes.Chocolate, Brushes.Coral
            , Brushes.CornflowerBlue, Brushes.Cornsilk, Brushes.Crimson, Brushes.Cyan, Brushes.DarkCyan
            , Brushes.DarkKhaki, Brushes.DarkMagenta, Brushes.DarkOliveGreen, Brushes.DarkOrange, Brushes.DarkOrchid
            , Brushes.DarkSalmon, Brushes.DarkSeaGreen, Brushes.DarkSlateBlue, Brushes.DarkSlateGray
            , Brushes.DarkTurquoise, Brushes.DeepSkyBlue, Brushes.DodgerBlue, Brushes.Firebrick, Brushes.FloralWhite
            , Brushes.ForestGreen, Brushes.Fuchsia, Brushes.Gainsboro, Brushes.GhostWhite, Brushes.Gold
            , Brushes.Goldenrod, Brushes.Gray, Brushes.Navy, Brushes.DarkGreen, Brushes.Lime
            , Brushes.MediumBlue };

        //Mona Vars
        private List<MonaSpectrum> markedFileIDs;
        private MonaProjectData monaProjectData;

        // table viewer
        private PeakSpotTableViewer peakSpotTableViewer;
        private bool peakSpotTableViewerFlag = true;
        private bool peakSpotTableViewerFlag2 = true;
        private AlignmentSpotTableViewer alignmentSpotTableViewer;
        private bool alignmentSpotTableViewerFlag = true;
        private bool alignmentSpotTableViewerFlag2 = true;
        private bool isClickOnRtMzViewer = false;
        private bool isClickOnDtMzViewer = false;

        // quant mass manager
        private QuantmassBrowser quantmassBrowser;
        private bool quantmassBrowserFlag = true;
        private bool quantmassBrowserFlag2 = true;

        // additional viewers
        private SampleTableViewerInAlignment sampleTableViewer;
        private ManualPeakMod.AlignedChromatogramModificationWin alignedChromatogramModificationWin;

        // aif viewer
        private AifViewerControl aifViewerController;

        // lipoquality database
        private List<AlignmentPropertyBean> lipoqualitySpotProperties;
        private List<AnalysisFileBean> lipoqualityFileProperties;
        private List<RawData> lipoqualitySpectrumQueries;

        // labels
        private string displayedAnnotatedNameInfo;
        private string displayedRetentionInfo;
        private string displayedOtherDiagnosticsInfo;
        private string displayedMzInfo;
        private string displayedPeakQuantInfo;
        private string displayedFormulaOntologyInfo;
        private string displayedInChIKeyInfo;
        private string displayedCommentInfo;
        private string displayedRtSimilarityInfo;
        private string displayedMzSimilarityInfo;
        private string displayedSpectrumMatchInfo;
        private string displayedCcsSimilarityInfo;
        private string displayedSmilesInfo;


        #endregion

        # region// Properties
        public static MainWindow MainWindowInstance
        {
            get { return MainWindow.mainWindow; }
            set { MainWindow.mainWindow = value; }
        }

        public MainWindowDisplayVM MainWindowDisplayVM
        {
            get { return mainWindowDisplayVM; }
            set { mainWindowDisplayVM = value; }
        }

        public ProjectPropertyBean ProjectProperty
        {
            get { return projectProperty; }
            set { projectProperty = value; }
        }

        public SavePropertyBean SaveProperty
        {
            get { return saveProperty; }
            set { saveProperty = value; }
        }

        public AnalysisParametersBean AnalysisParamForLC
        {
            get { return analysisParamForLC; }
            set { analysisParamForLC = value; }
        }

        public AnalysisParamOfMsdialGcms AnalysisParamForGC
        {
            get { return analysisParamForGC; }
            set { analysisParamForGC = value; }
        }

        public ObservableCollection<AnalysisFileBean> AnalysisFiles
        {
            get { return analysisFiles; }
            set { analysisFiles = value; }
        }

        public ObservableCollection<RawSpectrum> LcmsSpectrumCollection
        {
            get { return lcmsSpectrumCollection; }
            set { lcmsSpectrumCollection = value; }
        }

        public List<RawSpectrum> GcmsSpectrumList
        {
            get { return gcmsSpectrumList; }
            set { gcmsSpectrumList = value; }
        }

        public RdamPropertyBean RdamProperty
        {
            get { return rdamProperty; }
            set { rdamProperty = value; }
        }

        public List<MspFormatCompoundInformationBean> MspDB
        {
            get { return mspDB; }
            set { mspDB = value; }
        }

        public List<PostIdentificatioinReferenceBean> PostIdentificationTxtDB
        {
            get { return postIdentificationTxtDB; }
            set { postIdentificationTxtDB = value; }
        }

        public List<PostIdentificatioinReferenceBean> TargetFormulaLibrary
        {
            get { return targetFormulaLibrary; }
            set { targetFormulaLibrary = value; }
        }

        public MS2DecResult PeakViewMS2DecResult
        {
            get { return peakViewMS2DecResult; }
            set { peakViewMS2DecResult = value; }
        }

        public MS2DecResult AlignViewMS2DecResult
        {
            get { return alignViewMS2DecResult; }
            set { alignViewMS2DecResult = value; }
        }

        public int FocusedFileID
        {
            get { return focusedFileID; }
            set { focusedFileID = value; }
        }

        public int FocusedPeakID
        {
            get { return focusedPeakID; }
            set { focusedPeakID = value; }
        }

        public int FocusedMS1DecID
        {
            get { return focusedMS1DecID; }
            set { focusedMS1DecID = value; }
        }

		public int FocusedAlignmentFileID
        {
            get { return focusedAlignmentFileID; }
            set { focusedAlignmentFileID = value; }
        }

        public int FocusedAlignmentPeakID
        {
            get { return focusedAlignmentPeakID; }
            set { focusedAlignmentPeakID = value; }
        }

        public int FocusedAlignmentMs1DecID
        {
            get { return focusedAlignmentMs1DecID; }
            set { focusedAlignmentMs1DecID = value; }
        }

        public int DisplayFocusId {
            get { return displayFocusId; }
            set { if (displayFocusId == value) return; displayFocusId = value; OnPropertyChanged("DisplayFocusId"); }
        }

        public double DisplayFocusRt {
            get { return displayFocusRt; }
            set { if (displayFocusRt == value) return; displayFocusRt = value; OnPropertyChanged("DisplayFocusRt"); }
        }

        public double DisplayFocusMz {
            get { return displayFocusMz; }
            set { if (displayFocusMz == value) return; displayFocusMz = value; OnPropertyChanged("DisplayFocusMz"); }
        }

        public bool WindowOpened
        {
            get { return windowOpened; }
            set { windowOpened = value; }
        }

        public FileStream PeakViewDecFS
        {
            get { return peakViewDecFS; }
            set { peakViewDecFS = value; }
        }

        public List<long> PeakViewDecSeekPoints
        {
            get { return peakViewDecSeekPoints; }
            set { peakViewDecSeekPoints = value; }
        }

        public FileStream AlignViewDecFS
        {
            get { return alignViewDecFS; }
            set { alignViewDecFS = value; }
        }

        public List<long> AlignViewDecSeekPoints
        {
            get { return alignViewDecSeekPoints; }
            set { alignViewDecSeekPoints = value; }
        }

        public IupacReferenceBean IupacReference
        {
            get { return iupacReference; }
            set { iupacReference = value; }
        }

        //public PrincipalComponentAnalysisResult PcaBean
        //{
        //    get { return pcaBean; }
        //    set { pcaBean = value; }
        //}

        public MultivariateAnalysisResult MultivariateAnalysisResult {
            get { return multivariateAnalysisResult; }
            set { multivariateAnalysisResult = value; }
        }

        public ObservableCollection<AlignmentFileBean> AlignmentFiles
        {
            get { return alignmentFiles; }
            set { alignmentFiles = value; }
        }

        public AlignmentResultBean FocusedAlignmentResult
        {
            get { return focusedAlignmentResult; }
            set { focusedAlignmentResult = value; }
        }

        public List<PeakAreaBean> GcmsPeakAreaList {
            get { return gcmsPeakAreaList; }
            set { gcmsPeakAreaList = value; }
        }

        public List<MS1DecResult> Ms1DecResults
        {
            get { return ms1DecResults; }
            set { ms1DecResults = value; }
        }

        public List<MS1DecResult> FocusedAlignmentMS1DecResults
        {
            get { return focusedAlignmentMS1DecResults; }
            set { focusedAlignmentMS1DecResults = value; }
        }

        public List<SolidColorBrush> SolidColorBrushList
        {
            get { return solidColorBrushList; }
            set { solidColorBrushList = value; }
        }

        public PairwisePlotFocus PairwisePlotFocus
        {
            get { return pairwisePlotFocus; }
            set { pairwisePlotFocus = value; }
        }

        public MsDialIniField MsdialIniField
        {
            get { return msdialIniField; }
            set { msdialIniField = value; }
        }

        public PeakSpotTableViewer PeakSpotTableViewer {
            get { return peakSpotTableViewer; }
            set { peakSpotTableViewer = value; }
        }

        public AlignmentSpotTableViewer AlignmentSpotTableViewer {
            get { return alignmentSpotTableViewer; }
            set { alignmentSpotTableViewer = value; }
        }

        public QuantmassBrowser QuantmassBrowser {
            get { return quantmassBrowser; }
            set { quantmassBrowser = value; }
        }

        public BarChartDisplayMode BarChartDisplayMode { get; set; } = BarChartDisplayMode.OriginalHeight;
        public bool isNormalized { get; set; }

        public string DisplayedAnnotatedNameInfo {
            get {
                return displayedAnnotatedNameInfo;
            }

            set {
                if (displayedAnnotatedNameInfo == value) return;
                displayedAnnotatedNameInfo = value;
                OnPropertyChanged("DisplayedAnnotatedNameInfo");
            }
        }

        public string DisplayedRetentionInfo {
            get {
                return displayedRetentionInfo;
            }

            set {
                if (displayedRetentionInfo == value) return;
                displayedRetentionInfo = value;
                OnPropertyChanged("DisplayedRetentionInfo");
            }
        }

        public string DisplayedOtherDiagnosticsInfo {
            get {
                return displayedOtherDiagnosticsInfo;
            }

            set {
                if (displayedOtherDiagnosticsInfo == value) return;
                displayedOtherDiagnosticsInfo = value;
                OnPropertyChanged("DisplayedOtherDiagnosticsInfo");
            }
        }

        public string DisplayedMzInfo {
            get {
                return displayedMzInfo;
            }

            set {
                if (displayedMzInfo == value) return;
                displayedMzInfo = value;
                OnPropertyChanged("DisplayedMzInfo");
            }
        }

        public string DisplayedPeakQuantInfo {
            get {
                return displayedPeakQuantInfo;
            }

            set {
                if (displayedPeakQuantInfo == value) return;
                displayedPeakQuantInfo = value;
                OnPropertyChanged("DisplayedPeakQuantInfo");
            }
        }

        public string DisplayedFormulaOntologyInfo {
            get {
                return displayedFormulaOntologyInfo;
            }

            set {
                if (displayedFormulaOntologyInfo == value) return;
                displayedFormulaOntologyInfo = value;
                OnPropertyChanged("DisplayedFormulaOntologyInfo");
            }
        }

        public string DisplayedInChIKeyInfo {
            get {
                return displayedInChIKeyInfo;
            }

            set {
                if (displayedInChIKeyInfo == value) return;
                displayedInChIKeyInfo = value;
                OnPropertyChanged("DisplayedInChIKeyInfo");
            }
        }

        public string DisplayedCommentInfo {
            get {
                return displayedCommentInfo;
            }

            set {
                if (displayedCommentInfo == value) return;
                displayedCommentInfo = value;
                OnPropertyChanged("DisplayedCommentInfo");
            }
        }

        public string DisplayedRtSimilarityInfo {
            get {
                return displayedRtSimilarityInfo;
            }

            set {
                if (displayedRtSimilarityInfo == value) return;
                displayedRtSimilarityInfo = value;
                OnPropertyChanged("DisplayedRtSimilarityInfo");
            }
        }

        public string DisplayedMzSimilarityInfo {
            get {
                return displayedMzSimilarityInfo;
            }

            set {
                if (displayedMzSimilarityInfo == value) return;
                displayedMzSimilarityInfo = value;
                OnPropertyChanged("DisplayedMzSimilarityInfo");
            }
        }

        public string DisplayedSpectrumMatchInfo {
            get {
                return displayedSpectrumMatchInfo;
            }

            set {
                if (displayedSpectrumMatchInfo == value) return;
                displayedSpectrumMatchInfo = value;
                OnPropertyChanged("DisplayedSpectrumMatchInfo");
            }
        }

        public string DisplayedCcsSimilarityInfo {
            get {
                return displayedCcsSimilarityInfo;
            }

            set {
                if (displayedCcsSimilarityInfo == value) return;
                displayedCcsSimilarityInfo = value;
                OnPropertyChanged("DisplayedCcsSimilarityInfo");
            }
        }

        public string DisplayedSmilesInfo {
            get {
                return displayedSmilesInfo;
            }

            set {
                if (displayedSmilesInfo == value) return;
                displayedSmilesInfo = value;
                OnPropertyChanged("DisplayedSmilesInfo");
            }
        }
        #endregion

        #region // constractor and initialization
        public MainWindow()
        {
            InitializeComponent();
            this.Title = Properties.Resources.VERSION;
            this.mainWindowDisplayVM = new MainWindowDisplayVM(this);
            this.MainWindowTitle = this.Title;

            this.doubleSlider_AmplitudeFilter.LowerSlider.ValueChanged += AmplitudeLowerSlider_ValueChanged;
            this.doubleSlider_AmplitudeFilter.UpperSlider.ValueChanged += AmplitudeUpperSlider_ValueChanged;

            this.DataContext = this;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            projectInitializing();
            //RunDevelopedVersion();
        }

        #region Developmental Codes
        private void RunDevelopedVersion()
        {
           method_Tada_Test01();

        }

        private void method_Tada_Test01()
        {

            //var f = @"G:\ETYPE\171206_B1_CorrelationTest\2018_3_7_20_3_31_added_comments.mtd";
            //           var f = @"G:\ETYPE\171206_B1_CorrelationTest\2018_3_7_20_3_31.mtd";
            //var f = @"C:\Users\tipputa\Desktop\TestRtCorrection\2018_9_26_21_14_43.mtd2";
            var f = @"D:\2_study\ms-dial-demo\ETYPE_test\2019_12_11_14_34_20.mtd2";
            SetProjects(f, 0, 8);
        }

        private void MenuItem_DevMethod_MS2Chromatogram_Click(object sender, RoutedEventArgs e)
        {
            //var rawMs2Dv = UiAccessLcUtility.GetMs2ChromatogramDrawVisual(this.LcmsSpectrumCollection, this.DriftSpotBeanList[this.FocusedDriftSpotID],
            //    analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.FocusedPeakID],
            //    null, projectProperty, analysisParamForLC);

            //var accumulatedMs2Dv = UiAccessLcUtility.GetMs2ChromatogramDrawVisual(this.LcmsSpectrumCollection, this.DriftSpotBeanList[this.FocusedDriftSpotID],
            //    analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.FocusedPeakID],
            //    this.PeakViewMS2DecResult, projectProperty, analysisParamForLC);


            //var win = new GuiTest.Msdial.Chart.TwoRows.TwoRowsWindow(new ChartDrawing.DefaultUC(rawMs2Dv), new ChartDrawing.DefaultUC(accumulatedMs2Dv));
            //win.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            //win.Show();
        }

        private void SetProjects(string f, int peakFileId, int alignmentFileId)
        {
            this.saveProperty = MessagePackHandler.LoadFromFile<SavePropertyBean>(f);
            openProjectMenuPropertySetting(f);
            FileNavigatorUserControlsRefresh(this.analysisFiles);

            if (peakFileId >= 0)
                PeakViewerForLcRefresh(peakFileId);
            if(alignmentFileId >= 0)
                AlignmentViewerForLcRefresh(alignmentFileId);

            this.analysisProcessed = true;
            menuItemRefresh();
        }
        #endregion


        private void Window_ContentRendered(object sender, EventArgs e)
        {
            if (this.MainWindowTitle.Contains("-tada") || this.MainWindowTitle.Contains("-beta") || this.MainWindowTitle.Contains("-dev")) return;
            Mouse.OverrideCursor = Cursors.Wait;
            var window = new ShortMessageWindow() {
                Owner = mainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.Label_MessageTitle.Text = "Checking for updates..";
            window.Show();

            VersionUpdateNotificationService.CheckForUpdates();
            window.Close();

            Mouse.OverrideCursor = null;
        }

        public void projectInitializing()
        {
            mainWindow = this;
            this.Title = this.MainWindowTitle;

            this.focusedPeakID = -1;
            this.focusedFileID = -1;
            this.focusedAlignmentFileID = -1;
            this.focusedAlignmentPeakID = -1;
            this.focusedMS1DecID = -1;
            this.focusedAlignmentMs1DecID = -1;

            this.DisplayFocusId = -1;
            this.DisplayFocusRt = -1;
            this.DisplayFocusMz = -1;

            this.ComboBox_DisplayLabel.ItemsSource = new string[] { "None", "Retention time", "Mass", "Metabolite", "Isotope", "Adduct" };
            this.ComboBox_DisplayLabel.SelectedIndex = 0;
            this.ToggleButton_MsMsChromatogramRaw.IsChecked = true;
            this.ToggleButton_MeasurmentReferenceSpectraDeconvoluted.IsChecked = true;
            this.analysisProcessed = false;
            this.statisticsProcessed = false;

            this.projectProperty = new ProjectPropertyBean();
            this.analysisFiles = new ObservableCollection<AnalysisFileBean>();

            this.analysisParamForLC = new AnalysisParametersBean();
            this.analysisParamForGC = new AnalysisParamOfMsdialGcms();

            this.gcmsPeakAreaList = new List<PeakAreaBean>();
            this.ms1DecResults = new List<MS1DecResult>();
            this.focusedAlignmentMS1DecResults = new List<MS1DecResult>();

            this.mspDB = new List<MspFormatCompoundInformationBean>();
            this.postIdentificationTxtDB = new List<PostIdentificatioinReferenceBean>();
            this.targetFormulaLibrary = new List<PostIdentificatioinReferenceBean>();
            this.alignmentFiles = new ObservableCollection<AlignmentFileBean>();

            this.lcmsSpectrumCollection = new ObservableCollection<RawSpectrum>();
            this.rdamProperty = new RdamPropertyBean();
            this.gcmsSpectrumList = new List<RawSpectrum>();

            //this.pcaBean = new PrincipalComponentAnalysisResult();
            this.multivariateAnalysisResult = new MultivariateAnalysisResult();
            this.iupacReference = new IupacReferenceBean();
            this.msdialIniField = MsDialIniParcer.Read();

            this.lipoqualityFileProperties = new List<AnalysisFileBean>();
            this.lipoqualitySpectrumQueries = new List<RawData>();
            this.lipoqualitySpotProperties = new List<AlignmentPropertyBean>();

            menuItemRefresh();
        }

        private void Window_Closing(object sender, CancelEventArgs e)
        {
            if (!this.analysisProcessed) return;

            MessageBoxResult result = MessageBox.Show(this, "Did you already save the project file?\r\nCan you close this project file?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK) {
                if (this.aifViewerController != null) this.aifViewerController.Close();
            }
            else e.Cancel = true;
        }

        public void PeakSpotTableViewerClose(object sender, EventArgs e) {
            this.PeakSpotTableViewer = null;
            this.peakSpotTableViewerFlag = true;
            this.peakSpotTableViewerFlag2 = true;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void AlignmentSpotTableViewerClose(object sender, EventArgs e) {
            this.AlignmentSpotTableViewer = null;
            this.alignmentSpotTableViewerFlag = true;
            this.alignmentSpotTableViewerFlag2 = true;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        public void QuantmassBrowserClose(object sender, EventArgs e)
        {
            this.quantmassBrowser = null;
            this.quantmassBrowserFlag = true;
            this.quantmassBrowserFlag2 = true;
            GC.Collect();
            GC.WaitForPendingFinalizers();
        }

        private void aifViewerControllerClose(object sender, EventArgs e) {
            this.aifViewerController = null;
        }

        public void menuItemRefresh()
        {
            if (this.analysisProcessed)
            {
                this.MenuItem_SaveAsProject.IsEnabled = true;
                this.MenuItem_SaveProject.IsEnabled = true;
                this.MenuItem_Dataprocessing.IsEnabled = true;
                this.MenuItem_Postprocessing.IsEnabled = true;
                this.MenuItem_StatisticalAnalysis.IsEnabled = true;
                //this.MenuItem_PartialLeastSquares.IsEnabled = true;
                //this.MenuItem_Identification.IsEnabled = true;
                this.MenuItem_Search.IsEnabled = true;
                this.MenuItem_View.IsEnabled = true;
                this.MenuItem_Option.IsEnabled = true;
                this.MenuItem_Export.IsEnabled = true;
                this.MenuItem_SaveParameters.IsEnabled = true;
                this.MenuItem_CorrDec.IsEnabled = true;

                this.Title = this.MainWindowTitle + " " + this.projectProperty.ProjectFilePath;

                ContextMenu menuPeakViewer = this.FindResource("menuPeakViewer") as ContextMenu;

				if (this.projectProperty.Ionization == Ionization.ESI)
                {
                    this.CheckBox_PlotAnnotatedOnly.IsEnabled = true;
                    this.CheckBox_PlotMsMsOnly.IsEnabled = true;
                    this.CheckBox_PlotMolcularIonOnly.IsEnabled = true;
                    this.CheckBox_SearchedFragment.IsEnabled = true;

                    this.Label_UiMass.Content = "m/z:";
                    this.Label_ReferenceMass.Content = "m/z similarity:";
                    this.TabItem_MS2Chromatogram.Header = "MS2 Chrom.";

                    if (this.analysisParamForLC.IsIonMobility) {
                        this.Label_RetentionTime.Content = "RT[min]|Mobility: ";
                        this.Label_Ccs.Content = "CCS:";
                        this.Label_CcsSimilarity.Content = "CCS similarity:";
                        this.CheckBox_CcsMatchedOnly.IsEnabled = true;
                    }
                    else {
                        this.Label_RetentionTime.Content = "RT[min]: ";
                        this.Label_Ccs.Content = "Adduct type";
                        this.Label_CcsSimilarity.Content = "(no available)";
                        this.CheckBox_CcsMatchedOnly.IsEnabled = false;
                    }

                    this.MenuItem_MsmsFragmentSearch.IsEnabled = true;
                    this.MenuItem_QuantMassUpdate.IsEnabled = false;
                    this.MenuItem_PosNegAmalgamator.IsEnabled = true;
                    if (this.projectProperty.CheckAIF) {
                        //this.MenuItem_CorrDec.IsEnabled = true;
                       // this.MenuItem_ParticularAlignmentSpotExport.IsEnabled = true;
                    }
                }
                else
                {
                    this.CheckBox_PlotAnnotatedOnly.IsEnabled = false;
                    this.CheckBox_PlotMsMsOnly.IsEnabled = false;
                    this.CheckBox_PlotMolcularIonOnly.IsEnabled = false;
                    this.CheckBox_SearchedFragment.IsEnabled = false;
                    this.CheckBox_CcsMatchedOnly.IsEnabled = false;

                    this.Label_UiMass.Content = "Quant mass";
                    this.Label_RetentionTime.Content = "RT [min]:";
                    this.Label_ReferenceMass.Content = "RI similarity:";
                    this.TabItem_MS2Chromatogram.Header = "EI Chrom.";
                    this.Label_Ccs.Content = "Retention index";
                    this.Label_CcsSimilarity.Content = "(no available)";

                    this.MenuItem_MsmsFragmentSearch.IsEnabled = false;
                    this.MenuItem_QuantMassUpdate.IsEnabled = true;
                    this.MenuItem_PosNegAmalgamator.IsEnabled = false;
                    this.MenuItem_CorrDec.IsEnabled = false;
                }
            }
            else
            {
                this.MenuItem_SaveAsProject.IsEnabled = false;
                this.MenuItem_SaveProject.IsEnabled = false;
                this.MenuItem_Dataprocessing.IsEnabled = false;
                this.MenuItem_Postprocessing.IsEnabled = false;
                this.MenuItem_StatisticalAnalysis.IsEnabled = false;
                this.MenuItem_PartialLeastSquares.IsEnabled = false;
                //this.MenuItem_Identification.IsEnabled = false;
                this.MenuItem_View.IsEnabled = false;
                this.MenuItem_Option.IsEnabled = false;
                this.MenuItem_Search.IsEnabled = false;
                this.MenuItem_Export.IsEnabled = false;
                this.MenuItem_SaveParameters.IsEnabled = false;

                ContextMenu menuPeakViewer = this.FindResource("menuPeakViewer") as ContextMenu;
			}

			if (this.focusedAlignmentResult != null)
			{
                if (this.focusedAlignmentResult.Normalized)
				{
                    this.MenuItem_PrincipalComponentAnalysis.IsEnabled = true;
                    this.MenuItem_PartialLeastSquares.IsEnabled = true;
                    this.MenuItem_HierarchicalClusteringAnalysis.IsEnabled = true;
                    this.isNormalized = true;
                    OnPropertyChanged("isNormalized");
                    BarChartDisplayMode = BarChartDisplayMode.NormalizedHeight;
                }
				else
				{
                    this.MenuItem_PrincipalComponentAnalysis.IsEnabled = false;
                    this.MenuItem_PartialLeastSquares.IsEnabled = false;
                    this.MenuItem_HierarchicalClusteringAnalysis.IsEnabled = false;

                    this.isNormalized = false;
                    OnPropertyChanged("isNormalized");
                    BarChartDisplayMode = BarChartDisplayMode.OriginalHeight;
                }
                this.MenuItem_PathwayMapper.IsEnabled = true;
            }
            else
			{
                this.MenuItem_PrincipalComponentAnalysis.IsEnabled = false;
                this.MenuItem_PartialLeastSquares.IsEnabled = false;
                this.MenuItem_HierarchicalClusteringAnalysis.IsEnabled = false;
                this.isNormalized = false;
                OnPropertyChanged("isNormalized");
                this.MenuItem_PathwayMapper.IsEnabled = false;
                BarChartDisplayMode = BarChartDisplayMode.OriginalHeight;
            }

            if (this.statisticsProcessed)
			{
			}
            else
			{
			}

        }
        #endregion

        #region // method for menu items
        private async void newProjectMenu_Click(object sender, RoutedEventArgs e)
        {
            if (!reStartProjectReminder()) return;

            var suw = new StartUpWindow(this);
            suw.Owner = this;
            suw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (suw.ShowDialog() == true)
            {
                var afpsw = new AnalysisFilePropertySetWin(this);
                afpsw.Owner = this;
                afpsw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (afpsw.ShowDialog() == true)
                {
                    if (this.projectProperty.Ionization == Ionization.ESI)
                    {
                        var apsw = new AnalysisParamSetForLcWin(mainWindow);
                        apsw.Owner = this;
                        apsw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        if (apsw.ShowDialog() == true)
                        {
                            if (await new ProcessManagerLC().RunAllProcess(this) == true) {
                                this.analysisProcessed = true;
                                menuItemRefresh();
                            }
                        }
                    }
                    else
                    {
                        var apsw = new AnalysisParamSetForGcWin(mainWindow);
                        apsw.Owner = this;
                        apsw.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                        if (apsw.ShowDialog() == true)
                        {
                            await new AnalysisFileProcessForGCMulti().Process(this);
                            this.analysisProcessed = true;
                            menuItemRefresh();
                        }
                    }
                }
            }
        }

        private async void openProjectMenu_Click(object sender, RoutedEventArgs e) {
            if (!reStartProjectReminder()) return;

            var ofd = new OpenFileDialog();
            ofd.Filter = "MTD file(*.mtd, *mtd2)|*.mtd?|MTD2 file(*.mtd2)|*mtd2|All(*)|*";
            ofd.Title = "Import a project file";
            ofd.RestoreDirectory = true;

            if (ofd.ShowDialog() == true) {
                Mouse.OverrideCursor = Cursors.Wait;

                var window = new ShortMessageWindow() {
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Show();

                //var errorString = string.Empty;
                await Task.Run(() =>
                {
                    this.saveProperty = MessagePackHandler.LoadFromFile<SavePropertyBean>(ofd.FileName);
                    if (this.SaveProperty == null) {
                        //errorString = this.Title + " cannot open the project: \n" + ofd.FileName;
                        return;
                    }
                    openProjectMenuPropertySetting(ofd.FileName);
                });
                if (this.SaveProperty == null) {
                    MessageBox.Show(this.Title + " cannot open the project: \n" + ofd.FileName, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    window.Close();
                    Mouse.OverrideCursor = null;
                    return;
                }

                FileNavigatorUserControlsRefresh(this.analysisFiles);
                if (this.projectProperty.Ionization == Ionization.ESI)
                    PeakViewerForLcRefresh(0);
                else
                    PeakViewerForGcRefresh(0);

                window.Close();
                Mouse.OverrideCursor = null;
                this.analysisProcessed = true;
                menuItemRefresh();

            }
        }

        private void exportFDLipidmicsResult_Click(object sender, RoutedEventArgs e) {
            var exportFolder = @"D:\fd_collection\Analysis\alignment_results";
            var filepaths = new List<(string, string)> {
                #region
                /*
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Actinidiaceae_Collection4_Negative\2020_8_12_12_12_24.mtd2", "Actinidiaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Actinidiaceae_Collection4_Positive\2020_8_12_12_18_26.mtd2", "Actinidiaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Actinidiaceae_Collection5_Negative\2020_8_12_12_31_0.mtd2", "Actinidiaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Actinidiaceae_Collection5_Positive\2020_8_12_13_10_3.mtd2", "Actinidiaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Amaryllidaceae_Collection5_Negative\2020_8_12_14_5_31.mtd2", "Amaryllidaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Amaryllidaceae_Collection5_Positive\2020_8_12_14_13_35.mtd2", "Amaryllidaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Apiaceae_Collection5_Negative\2020_8_12_14_31_13.mtd2", "Apiaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Apiaceae_Collection5_Positive\2020_8_12_14_33_48.mtd2", "Apiaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Asteraceae_Collection5_Negative\2020_8_12_14_45_47.mtd2", "Asteraceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Asteraceae_Collection5_Positive\2020_8_12_14_50_14.mtd2", "Asteraceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Basellaceae_Collection5_Negative\2020_8_12_15_0_10.mtd2", "Basellaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Basellaceae_Collection5_Positive\2020_8_12_15_6_36.mtd2", "Basellaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Brassicaceae_Collection4_Negative\2020_8_12_15_15_52.mtd2", "Brassicaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Brassicaceae_Collection4_Positive\2020_8_12_15_17_25.mtd2", "Brassicaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Brassicaceae_Collection5_Negative\2020_8_12_15_25_39.mtd2", "Brassicaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Brassicaceae_Collection5_Positive\2020_8_12_15_28_44.mtd2", "Brassicaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Bromeliaceae_Collection5_Negative\2020_8_12_15_36_46.mtd2", "Bromeliaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Bromeliaceae_Collection5_Positive\2020_8_12_15_39_25.mtd2", "Bromeliaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Caprifoliaceae_Collection5_Negative\2020_8_12_15_46_9.mtd2", "Caprifoliaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Caprifoliaceae_Collection5_Positive\2020_8_12_15_49_2.mtd2", "Caprifoliaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Cucurbitaceae_Collection4_Negative\2020_8_12_15_55_35.mtd2", "Cucurbitaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Cucurbitaceae_Collection4_Positive\2020_8_12_15_59_18.mtd2", "Cucurbitaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Cucurbitaceae_Collection5_Negative\2020_8_12_16_7_49.mtd2", "Cucurbitaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Cucurbitaceae_Collection5_Positive\2020_8_12_16_11_8.mtd2", "Cucurbitaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Dioscoreaceae_Collection4_Negative\2020_8_12_16_15_17.mtd2", "Dioscoreaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Dioscoreaceae_Collection4_Positive\2020_8_12_16_17_58.mtd2", "Dioscoreaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Dioscoreaceae_Collection5_Negative\2020_8_12_16_19_54.mtd2", "Dioscoreaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Dioscoreaceae_Collection5_Positive\2020_8_12_16_21_54.mtd2", "Dioscoreaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Ebenaceae_Collection5_Negative\2020_8_12_16_23_50.mtd2", "Ebenaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Ebenaceae_Collection5_Positive\2020_8_12_16_26_3.mtd2", "Ebenaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Equisetaceae_Collection4_Negative\2020_8_12_16_27_59.mtd2", "Equisetaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Equisetaceae_Collection4_Positive\2020_8_12_16_30_31.mtd2", "Equisetaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Equisetaceae_Collection5_Negative\2020_8_12_16_32_12.mtd2", "Equisetaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Equisetaceae_Collection5_Positive\2020_8_12_16_34_27.mtd2", "Equisetaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Euphorbiaceae_Collection4_Negative\2020_8_12_16_37_19.mtd2", "Euphorbiaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Euphorbiaceae_Collection4_Positive\2020_8_12_16_38_53.mtd2", "Euphorbiaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Euphorbiaceae_Collection5_Negative\2020_8_12_16_40_32.mtd2", "Euphorbiaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Euphorbiaceae_Collection5_Positive\2020_8_12_16_42_17.mtd2", "Euphorbiaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Fabaceae_Collection4_Negative\2020_8_12_16_43_58.mtd2", "Fabaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Fabaceae_Collection4_Positive\2020_8_12_16_46_27.mtd2", "Fabaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Fabaceae_Collection5_Negative\2020_8_12_16_48_34.mtd2", "Fabaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Fabaceae_Collection5_Positive\2020_8_12_16_50_24.mtd2", "Fabaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Lauraceae_Collection4_Negative\2020_8_12_16_51_52.mtd2", "Lauraceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Lauraceae_Collection4_Positive\2020_8_12_16_53_46.mtd2", "Lauraceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Lauraceae_Collection5_Negative\2020_8_12_16_55_58.mtd2", "Lauraceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Lauraceae_Collection5_Positive\2020_8_12_16_58_43.mtd2", "Lauraceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Malvaceae_Collection5_Negative\2020_8_12_17_1_6.mtd2", "Malvaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Malvaceae_Collection5_Positive\2020_8_12_17_2_58.mtd2", "Malvaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Musaceae_Collection4_Negative\2020_8_12_17_4_55.mtd2", "Musaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Musaceae_Collection4_Positive\2020_8_12_17_7_3.mtd2", "Musaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Musaceae_Collection5_Negative\2020_8_12_17_9_1.mtd2", "Musaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Musaceae_Collection5_Positive\2020_8_12_17_10_52.mtd2", "Musaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Pedaliaceae_Collection4_Negative\2020_8_12_17_13_15.mtd2", "Pedaliaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Pedaliaceae_Collection4_Positive\2020_8_12_17_15_10.mtd2", "Pedaliaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Pedaliaceae_Collection5_Negative\2020_8_12_17_17_5.mtd2", "Pedaliaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Pedaliaceae_Collection5_Positive\2020_8_12_17_19_11.mtd2", "Pedaliaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Poaceae_Collection4_Negative\2020_8_12_17_21_13.mtd2", "Poaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Poaceae_Collection4_Positive\2020_8_12_17_24_50.mtd2", "Poaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Poaceae_Collection5_Negative\2020_8_12_17_32_7.mtd2", "Poaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Poaceae_Collection5_Positive\2020_8_12_17_37_8.mtd2", "Poaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Polygonaceae_Collection4_Negative\2020_8_12_17_38_44.mtd2", "Polygonaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Polygonaceae_Collection4_Positive\2020_8_12_17_40_48.mtd2", "Polygonaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rosaceae_Collection4_Negative\2020_8_12_17_42_46.mtd2", "Rosaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rosaceae_Collection4_Positive\2020_8_12_17_46_32.mtd2", "Rosaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rosaceae_Collection5_Negative\2020_8_12_17_48_58.mtd2", "Rosaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rosaceae_Collection5_Positive\2020_8_12_17_52_57.mtd2", "Rosaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rutaceae_Collection5_Negative\2020_8_12_18_3_50.mtd2", "Rutaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Rutaceae_Collection5_Positive\2020_8_12_18_6_50.mtd2", "Rutaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Saururaceae_Collection4_Negative\2020_8_12_18_8_40.mtd2", "Saururaceae_Collection4_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Saururaceae_Collection4_Positive\2020_8_12_18_10_22.mtd2", "Saururaceae_Collection4_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Saururaceae_Collection5_Negative\2020_8_12_18_13_8.mtd2", "Saururaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Saururaceae_Collection5_Positive\2020_8_12_18_15_4.mtd2", "Saururaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Solanaceae_Collection5_Negative\2020_8_12_18_17_13.mtd2", "Solanaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Solanaceae_Collection5_Positive\2020_8_12_18_19_16.mtd2", "Solanaceae_Collection5_Positive"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Zingiberaceae_Collection5_Negative\2020_8_12_18_21_21.mtd2", "Zingiberaceae_Collection5_Negative"),
                (@"\\mtbdt\Mtb_info\data\FD_Collection\Projects\Zingiberaceae_Collection5_Positive\2020_8_12_18_23_32.mtd2", "Zingiberaceae_Collection5_Positive"),
                */
                #endregion
            };

            foreach ((var filepath, var outdir) in filepaths) {
                this.saveProperty = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                openProjectMenuPropertySetting(filepath);
                FocusedFileID = 0;

                var alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(this.AlignmentFiles[this.AlignmentFiles.Count - 1].FilePath);
                var analysisFiles = mainWindow.AnalysisFiles;

                var output = Path.Combine(exportFolder, outdir);
                ProjectProperty.ExportFolderPath = output;

                ProjectProperty.RawDatamatrix = true;
                ProjectProperty.SnMatrixExport = true;
                DataExportLcUtility.AlignmentResultExport(this, AlignmentFiles.Count - 1, -1);
            }
        }

        //this is my private source code for a specific project
        private void exportPlantMetabolomeResult_Click(object sender, RoutedEventArgs e) {
            var exportFolder = @"D:\PROJECT_Plant Specialized Metabolites Annotations\All plant tissues\MSDIAL-Test\C grouping result";
            //var exportFolder = @"C:\Users\Hiroshi Tsugawa\Desktop";
            var filepathDictionary = new Dictionary<string, string>() {
                #region
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Leaf and stem\Negative\Project-2017831125.mtd", "AT_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Leaf and stem\Positive\Project-2017831126.mtd", "AT_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Root\Negative\Project-2017831129.mtd", "AT_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\1 Arabidopsis thaliana\Root\Positive\Project-20178311210.mtd", "AT_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Leaf and stem\Negative\Project-20178311212.mtd", "NT_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Leaf and stem\Positive\Project-20178311213.mtd", "NT_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Root\Negative\Project-20178311215.mtd", "NT_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\2 Nicotiana tabacum\Root\Positive\Project-20178311216.mtd", "NT_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Bulb\Negative\Project-20178311222.mtd", "AC_Bulb_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Bulb\Positive\Project-20178311223.mtd", "AC_Bulb_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Leaf\Negative\Project-20178311225.mtd", "AC_Leaf_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Leaf\Positive\Project-20178311226.mtd", "AC_Leaf_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Root\Negative\Project-20178311219.mtd", "AC_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\3 Allium cepa\Root\Positive\Project-20178311220.mtd", "AC_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Leaf and stem\Negative\Project-20178311229.mtd", "GM_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Leaf and stem\Positive\Project-20178311230.mtd", "GM_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Root\Negative\Project-20178311232.mtd", "GM_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\4 Glycine max\Root\Positive\Project-20178311233.mtd", "GM_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Leaf and stem\Negative\Project-20178311235.mtd", "GG_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Leaf and stem\Positive\Project-20178311236.mtd", "GG_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Root\Negative\Project-20178311239.mtd", "GG_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\5 Glycyrrhiza glabra\Root\Positive\Project-20178311240.mtd", "GG_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Leaf and stem\Negative\Project-20178311242.mtd", "GU_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Leaf and stem\Positive\Project-20178311243.mtd", "GU_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Root\Negative\Project-20178311245.mtd", "GU_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\6 Glycyrrhiza urahensis\Root\Positive\Project-20178311246.mtd", "GU_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Fruit green\Negative\Project-20178311255.mtd", "LE_FruitGreen_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Fruit green\Positive\Project-20178311256.mtd", "LE_FruitGreen_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Leaf and stem\Negative\Project-20178311252.mtd", "LE_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Leaf and stem\Positive\Project-20178311253.mtd", "LE_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Ripe\Negative\Project-20178311249.mtd", "LE_Ripe_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\7 Lycopersicon esculentum\Ripe\Positive\Project-20178311250.mtd", "LE_Ripe_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Flower\Negative\Project-2017831135.mtd", "MT_Flower_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Flower\Positive\Project-2017831136.mtd", "MT_Flower_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Leaf and stem\Negative\Project-20178311258.mtd", "MT_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Leaf and stem\Positive\Project-20178311259.mtd", "MT_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Ripe pod\Negative\Project-20178311311.mtd", "MT_RipePod_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Ripe pod\Positive\Project-20178311312.mtd", "MT_RipePod_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Root\Negative\Project-2017831132.mtd", "MT_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Root\Positive\Project-2017831133.mtd", "MT_Root_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Seed\Negative\Project-2017831138.mtd", "MT_Seed_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\8 Medicago truncatula\Seed\Positive\Project-2017831139.mtd", "MT_Seed_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Leaf and stem\Negative\Project-20178311314.mtd", "OS_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Leaf and stem\Positive\Project-20178311315.mtd", "OS_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Root\Negative\Project-20178311318.mtd", "OS_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\9 Oryza sativa\Root\Positive\Project-20178311319.mtd", "OS_Root_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Leaf and stem\Negative\Project-20178311321.mtd", "ST_LeafStem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Leaf and stem\Positive\Project-20178311322.mtd", "ST_LeafStem_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Root\Negative\Project-20178311324.mtd", "ST_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Root\Positive\Project-20178311325.mtd", "ST_Root_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Tuber\Negative\Project-20178311328.mtd", "ST_Tuber_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\10 Solanum tuberosum\Tuber\Positive\Project-20178311329.mtd", "ST_Tuber_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Leaf\Negative\Project-20178311334.mtd", "ZM_Leaf_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Leaf\Positive\Project-20178311335.mtd", "ZM_Leaf_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Root\Negative\Project-20178311331.mtd", "ZM_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Root\Positive\Project-20178311332.mtd", "ZM_Root_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Seed\Negative\Project-20178311337.mtd", "ZM_Seed_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Seed\Positive\Project-20178311338.mtd", "ZM_Seed_Pos_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Stem\Negative\Project-20178311340.mtd", "ZM_Stem_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\11 Zea mays\Stem\Positive\Project-20178311341.mtd", "ZM_Stem_Pos_vs1.txt" },

                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\12 Ophiorrhiza pumila\Results\Negative_mode\2018_1_3_16_23_35.mtd", "OP_Root_Neg_vs1.txt" },
                { @"D:\PROJECT_Plant Specialized Metabolites Annotations\12 Ophiorrhiza pumila\Results\Positive_mode\Pos_7_11_2017_12_22_16_53_26.mtd", "OP_Root_Pos_vs1.txt" }
                #endregion
            };

            foreach (var pair in filepathDictionary) {
                var filepath = pair.Key;
                var output = exportFolder + "\\" + pair.Value;

                //                this.saveProperty = (SavePropertyBean)DataStorageMsdialUtility.LoadFromXmlFile(filepath, typeof(SavePropertyBean));
                this.saveProperty = MessagePackHandler.LoadFromFile<SavePropertyBean>(filepath);
                openProjectMenuPropertySetting(filepath);

                //i changed 0 to 1 for OP!!
                var alignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(this.AlignmentFiles[0].FilePath);
                //var alignmentResult = (AlignmentResultBean)DataStorageLcUtility.LoadFromXmlFile(this.AlignmentFiles[0].FilePath, typeof(AlignmentResultBean));
                var analysisFiles = mainWindow.AnalysisFiles;
                DataExportLcUtility.ExportMsdialIsotopeGroupingResult_PrivateMethod(this, 0, output, alignmentResult,
                    this.mspDB, this.analysisFiles, 1);


                //var fs = File.Open(this.AlignmentFiles[0].SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
                //var seekpointList = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);

                #region
                //using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                //    //Header
                //    ResultExportLcUtility.WriteDataMatrixHeader(sw, analysisFiles);

                //    var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;

                //    var level1Spots = new ObservableCollection<AlignmentPropertyBean>();
                //    foreach (var spot in alignedSpots.Where(n => n.LibraryID >= 0 && !n.MetaboliteName.Contains("w/o") && n.RetentionTimeSimilarity > 0)) {
                //        spot.MetaboliteName = "Level 1_" + spot.MetaboliteName;
                //        level1Spots.Add(spot);
                //    }

                //    var level2Spots = new ObservableCollection<AlignmentPropertyBean>();
                //    foreach (var spot in alignedSpots.Where(n => n.LibraryID >= 0 && !n.MetaboliteName.Contains("w/o") && n.RetentionTimeSimilarity <= 0)) {
                //        spot.MetaboliteName = "Level 2_" + spot.MetaboliteName;
                //        level1Spots.Add(spot);
                //    }

                //    var level3Spots = new ObservableCollection<AlignmentPropertyBean>();
                //    foreach (var spot in alignedSpots.Where(n => n.MsmsIncluded)) {
                //        if (spot.LibraryID >= 0 && !spot.MetaboliteName.Contains("w/o")) continue;

                //        spot.MetaboliteName = "Unknown";
                //        spot.LibraryID = -1;
                //        level3Spots.Add(spot);
                //    }

                //    var level4Spots = new ObservableCollection<AlignmentPropertyBean>();
                //    foreach (var spot in alignedSpots.Where(n => !n.MsmsIncluded)) {
                //        if (spot.LibraryID >= 0 && !spot.MetaboliteName.Contains("w/o")) continue;

                //        spot.MetaboliteName = "Unknown";
                //        spot.LibraryID = -1;
                //        level4Spots.Add(spot);
                //    }

                //    //From the second
                //    for (int i = 0; i < level1Spots.Count; i++) {

                //        var nonlabelInt = level1Spots[i].AlignedPeakPropertyBeanCollection[0].Variable;
                //        var labelInt = level1Spots[i].AlignedPeakPropertyBeanCollection[1].Variable;
                //        if (nonlabelInt <= labelInt * 2) continue;

                //        ResultExportLcUtility.WriteDataMatrixMetaData(sw, level1Spots[i], alignedSpots, mspDB, fs, seekpointList);

                //        for (int j = 0; j < level1Spots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                //            var value = Math.Round(level1Spots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                //            if (j == level1Spots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                //                sw.WriteLine(value);
                //            else
                //                sw.Write(value + "\t");
                //        }
                //    }

                //    for (int i = 0; i < level2Spots.Count; i++) {

                //        var nonlabelInt = level2Spots[i].AlignedPeakPropertyBeanCollection[0].Variable;
                //        var labelInt = level2Spots[i].AlignedPeakPropertyBeanCollection[1].Variable;
                //        if (nonlabelInt <= labelInt * 2) continue;

                //        ResultExportLcUtility.WriteDataMatrixMetaData(sw, level2Spots[i], alignedSpots, mspDB, fs, seekpointList);

                //        for (int j = 0; j < level2Spots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                //            var value = Math.Round(level2Spots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                //            if (j == level2Spots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                //                sw.WriteLine(value);
                //            else
                //                sw.Write(value + "\t");
                //        }
                //    }

                //    for (int i = 0; i < level3Spots.Count; i++) {

                //        var nonlabelInt = level3Spots[i].AlignedPeakPropertyBeanCollection[0].Variable;
                //        var labelInt = level3Spots[i].AlignedPeakPropertyBeanCollection[1].Variable;
                //        if (nonlabelInt <= labelInt * 2) continue;


                //        ResultExportLcUtility.WriteDataMatrixMetaData(sw, level3Spots[i], alignedSpots, mspDB, fs, seekpointList);

                //        for (int j = 0; j < level3Spots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                //            var value = Math.Round(level3Spots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                //            if (j == level3Spots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                //                sw.WriteLine(value);
                //            else
                //                sw.Write(value + "\t");
                //        }
                //    }

                //    for (int i = 0; i < level4Spots.Count; i++) {

                //        var nonlabelInt = level4Spots[i].AlignedPeakPropertyBeanCollection[0].Variable;
                //        var labelInt = level4Spots[i].AlignedPeakPropertyBeanCollection[1].Variable;
                //        if (nonlabelInt <= labelInt * 2) continue;


                //        ResultExportLcUtility.WriteDataMatrixMetaData(sw, level4Spots[i], alignedSpots, mspDB, fs, seekpointList);

                //        for (int j = 0; j < level4Spots[i].AlignedPeakPropertyBeanCollection.Count; j++) {
                //            var value = Math.Round(level4Spots[i].AlignedPeakPropertyBeanCollection[j].Variable, 0);
                //            if (j == level4Spots[i].AlignedPeakPropertyBeanCollection.Count - 1)
                //                sw.WriteLine(value);
                //            else
                //                sw.Write(value + "\t");
                //        }
                //    }
                //}
                #endregion
                //fs.Dispose();
                //fs.Close();
            }
        }

		private bool reStartProjectReminder()
        {
            if (!this.analysisProcessed) return true;

            MessageBoxResult result = MessageBox.Show(this, "Did you already save the project file?\r\nDo you want to close this project and to re-start new project?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question);
            if (result == MessageBoxResult.OK)
            {
                projectInitializing();
                PeakViewDataAccessRefresh();
                AlignmentViewDataAccessRefresh();
                FileNavigatorUserControlsRefresh(null);
                return true;
            }
            else
            {
                return false;
            }
        }

        private void openProjectMenuPropertySetting(string filePath)
        {
            string projectFolderPath = Path.GetDirectoryName(filePath);

            //check lab use only
            if (this.MainWindowTitle.Contains("-dev2")) {
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersion = true;
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersionTada = true;
            }
            else if (this.MainWindowTitle.Contains("-tada")) {
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersionTada = true;
            }
            else if (this.MainWindowTitle.Contains("-dev")) {
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersion = true;
            }

            else {
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersion = false;
                this.saveProperty.ProjectPropertyBean.IsLabPrivateVersionTada = false;
            }
            // overwrite project property
            this.saveProperty.ProjectPropertyBean.ProjectFolderPath = projectFolderPath;
            this.saveProperty.ProjectPropertyBean.ProjectFilePath = filePath;
            if (this.saveProperty.ProjectPropertyBean.Ionization == Ionization.ESI && this.saveProperty.TargetFormulaLibrary == null)
                this.saveProperty.ProjectPropertyBean.DataTypeMS2 = this.saveProperty.ProjectPropertyBean.DataType;



            // overwrite rdam properties
            this.saveProperty.RdamPropertyBean.RdamFileID_RdamFilePath = new Dictionary<int, string>();
            this.saveProperty.RdamPropertyBean.RdamFilePath_RdamFileID = new Dictionary<string, int>();
            for (int i = 0; i < this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection.Count; i++)
            {
                var extension = System.IO.Path.GetExtension(this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFilePath);

                this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFilePath =
                    projectFolderPath + "\\" + this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFileName + extension;
                this.saveProperty.RdamPropertyBean.RdamFileID_RdamFilePath[this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFileID] =
                    this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFilePath;
                this.saveProperty.RdamPropertyBean.RdamFilePath_RdamFileID[this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFilePath] =
                    this.saveProperty.RdamPropertyBean.RdamFileContentBeanCollection[i].RdamFileID;
            }

            // overwrite analysis file properties
            for (int i = 0; i < this.saveProperty.AnalysisFileBeanCollection.Count; i++)
            {
                var extension = System.IO.Path.GetExtension(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath);

                this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath =
                    projectFolderPath + "\\" + Path.GetFileNameWithoutExtension(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.AnalysisFilePath) + extension;
                this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePath =
                    projectFolderPath + "\\" + Path.GetFileName(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePath);
                if (this.saveProperty.ProjectPropertyBean.CheckAIF) {
                    for(var j = 0; j < this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePathList.Count; j++) {
                        this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[j] =
                            projectFolderPath + "\\" + Path.GetFileName(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePathList[j]);
                    }
                }
                if (this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath != null)
                    this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath =
                        projectFolderPath + "\\" + Path.GetFileName(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                else
                    this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath =
                        projectFolderPath + "\\" + Path.GetFileNameWithoutExtension(this.saveProperty.AnalysisFileBeanCollection[i].AnalysisFilePropertyBean.DeconvolutionFilePath) + "." + SaveFileFormat.pai;

                if (this.saveProperty.AnalysisFileBeanCollection[i].RetentionTimeCorrectionBean == null)
                    this.saveProperty.AnalysisFileBeanCollection[i].RetentionTimeCorrectionBean = new RetentionTimeCorrection.RetentionTimeCorrectionBean();
            }

            // overwrite alignment file properties
            if (this.saveProperty.AlignmentFileBeanCollection != null)
            {
                for (int i = 0; i < this.saveProperty.AlignmentFileBeanCollection.Count; i++)
                {
                    this.saveProperty.AlignmentFileBeanCollection[i].FilePath = projectFolderPath + "\\" + this.saveProperty.AlignmentFileBeanCollection[i].FileName + "." + SaveFileFormat.arf;
                    this.saveProperty.AlignmentFileBeanCollection[i].EicFilePath = projectFolderPath + "\\" + this.saveProperty.AlignmentFileBeanCollection[i].FileName + ".EIC.aef";
                    this.saveProperty.AlignmentFileBeanCollection[i].SpectraFilePath = projectFolderPath + "\\" + System.IO.Path.GetFileName(this.saveProperty.AlignmentFileBeanCollection[i].SpectraFilePath);
                }
            }

            // overwrite parameters
            if (this.saveProperty.ProjectPropertyBean.Ionization == Ionization.ESI)
            {
                this.saveProperty.AnalysisParametersBean.LipidQueryBean =
                    DataStorageLcUtility.LipidQueryRetrieve(this.saveProperty.AnalysisParametersBean.LipidQueryBean,
                    this.saveProperty.ProjectPropertyBean);
                if (this.saveProperty.AnalysisParametersBean.IsotopeTrackingDictionary == null)
                    this.saveProperty.AnalysisParametersBean.IsotopeTrackingDictionary = new IsotopeTrackingDictionary();

                var newAdducts = AdductResourceParser.GetAdductIonInformationList(this.saveProperty.ProjectPropertyBean.IonMode);
                var oldAdducts = this.saveProperty.AnalysisParametersBean.AdductIonInformationBeanList;

                foreach (var nAdduct in newAdducts) {
                    var flg = false;
                    foreach (var oAdduct in oldAdducts) {
                        if (oAdduct.AdductName == nAdduct.AdductName) {
                            flg = true;
                            break;
                        }
                    }
                    if (flg == false) {
                        oldAdducts.Add(nAdduct);
                    }
                }
                //adduct ionmode recalculation (because I noticed there is a problem in the previous version, then, I needed to add this function for the compatibility)
                foreach (var adduct in oldAdducts) {
                    var nAdduct = AdductIonParcer.GetAdductIonBean(adduct.AdductName);
                    adduct.IonMode = nAdduct.IonMode;
                }

                this.saveProperty.AnalysisParametersBean.AdductIonInformationBeanList = oldAdducts;
                //--until here

                //if (this.saveProperty.ProjectPropertyBean.FinalSavedDate == null || this.saveProperty.ProjectPropertyBean.FinalSavedDate < DateTime.Now) {
                //    this.saveProperty.AnalysisParametersBean.IsUseRetentionInfoForIdentificationScoring = true;
                //}

                if (this.saveProperty.AnalysisParametersBean.NumThreads <= 0)
                    this.saveProperty.AnalysisParametersBean.NumThreads = 1;

                if (this.saveProperty.AnalysisParametersBean.MaxChargeNumber <= 0)
                    this.saveProperty.AnalysisParametersBean.MaxChargeNumber = 1;

                //for last update in 20200205
                if (this.saveProperty.AnalysisParametersBean.Ms2MassRangeBegin < 1.0 && this.saveProperty.AnalysisParametersBean.Ms2MassRangeEnd < 1.0) {
                    this.saveProperty.AnalysisParametersBean.Ms2MassRangeBegin = 0;
                    this.saveProperty.AnalysisParametersBean.Ms2MassRangeEnd = 2000;
                }
            }
            else if (this.saveProperty.ProjectPropertyBean.Ionization == Ionization.EI) {
                if (this.saveProperty.AnalysisParamForGC.FileIdRiInfoDictionary == null) this.saveProperty.AnalysisParamForGC.FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
                if (this.saveProperty.AnalysisParamForGC.RiDictionary != null && this.saveProperty.AnalysisParamForGC.RiDictionary.Count != 0 && this.saveProperty.AnalysisParamForGC.FileIdRiInfoDictionary.Count == 0) {
                    foreach (var file in this.saveProperty.AnalysisFileBeanCollection) {
                        file.AnalysisFilePropertyBean.RiDictionaryFilePath = this.saveProperty.AnalysisParamForGC.RiDictionaryFilePath;
                        this.saveProperty.AnalysisParamForGC.FileIdRiInfoDictionary[file.AnalysisFilePropertyBean.AnalysisFileId] = new RiDictionaryInfo() {
                             DictionaryFilePath = this.saveProperty.AnalysisParamForGC.RiDictionaryFilePath, RiDictionary = this.saveProperty.AnalysisParamForGC.RiDictionary
                        };
                    }
                    this.saveProperty.AnalysisParamForGC.RiDictionary = null;
                }
                if (this.saveProperty.ProjectPropertyBean.FinalSavedDate == null || this.saveProperty.ProjectPropertyBean.FinalSavedDate < DateTime.Now) {
                    this.saveProperty.AnalysisParamForGC.IsUseRetentionInfoForIdentificationScoring = true;
                }
                if (this.saveProperty.AnalysisParamForGC.NumThreads <= 0)
                    this.saveProperty.AnalysisParamForGC.NumThreads = 1;
            }

            //for latest update 190727
            if (this.saveProperty.ProjectPropertyBean.ClassnameToColorBytes == null || this.saveProperty.ProjectPropertyBean.ClassnameToColorBytes.Count == 0) {
                MsDialStatistics.ClassColorDictionaryInitialization(this.saveProperty.AnalysisFileBeanCollection, this.saveProperty.ProjectPropertyBean, this.SolidColorBrushList);
            }

            //for last update 191023
            if (this.saveProperty.ProjectPropertyBean.SeparationType == SeparationType.IonMobility || (this.saveProperty.AnalysisParametersBean != null && this.saveProperty.AnalysisParametersBean.IsIonMobility)) {
                this.SaveProperty.ProjectPropertyBean.SeparationType = SeparationType.IonMobility;
                if (this.saveProperty.AnalysisParametersBean.FileidToCcsCalibrantData == null) {
                    var files = this.saveProperty.AnalysisFileBeanCollection;
                    var param = this.saveProperty.AnalysisParametersBean;
                    param.FileidToCcsCalibrantData = new Dictionary<int, CoefficientsForCcsCalculation>();
                    foreach(var file in files) {
                        var fileid = file.AnalysisFilePropertyBean.AnalysisFileId;
                        param.FileidToCcsCalibrantData[fileid] = new CoefficientsForCcsCalculation();
                    }
                    param.IsAllCalibrantDataImported = false;
                }
            }



            // set
            this.rdamProperty = this.saveProperty.RdamPropertyBean;
            this.iupacReference = this.saveProperty.IupacReferenceBean;
            this.mspDB = this.saveProperty.MspFormatCompoundInformationBeanList;
            this.postIdentificationTxtDB = this.saveProperty.PostIdentificationReferenceBeanList;
            this.targetFormulaLibrary = this.saveProperty.TargetFormulaLibrary;
            this.analysisFiles = this.saveProperty.AnalysisFileBeanCollection;
            this.alignmentFiles = this.saveProperty.AlignmentFileBeanCollection;
            this.analysisParamForGC = this.saveProperty.AnalysisParamForGC;
            this.analysisParamForLC = this.saveProperty.AnalysisParametersBean;
            this.projectProperty = this.saveProperty.ProjectPropertyBean;
        }

        private void saveParametersMenu_Click(object sender, RoutedEventArgs e)
        {
            if (this.analysisFiles == null) return;

            SaveFileDialog sfd = new SaveFileDialog();
            sfd.FileName = "*." + SaveFileFormat.med;
            sfd.InitialDirectory = this.projectProperty.ProjectFolderPath;
            sfd.Filter = "MED format(*.med)|*.med";
            sfd.Title = "Save file dialog";

            if (sfd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var window = new ShortMessageWindow() {
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Label_MessageTitle.Text = "Saving the parameter..";
                window.Show();

                if (this.projectProperty.Ionization == Ionization.ESI)
                    MessagePackHandler.SaveToFile<AnalysisParametersBean>(this.analysisParamForLC, sfd.FileName);
                //DataStorageMsdialUtility.SaveToXmlFile(this.analysisParamForLC, sfd.FileName, typeof(AnalysisParametersBean));
                else
                    MessagePackHandler.SaveToFile<AnalysisParamOfMsdialGcms>(this.analysisParamForGC, sfd.FileName);
                //DataStorageMsdialUtility.SaveToXmlFile(this.analysisParamForGC, sfd.FileName, typeof(AnalysisParamOfMsdialGcms));

                window.Close();
                Mouse.OverrideCursor = null;
            }
        }

        private void saveAsProjectMenu_Click(object sender, RoutedEventArgs e)
        {
            if (this.analysisFiles == null) return;

            var sfd = new SaveFileDialog();
            sfd.FileName = "*." + SaveFileFormat.mtd;
            sfd.InitialDirectory = this.projectProperty.ProjectFolderPath;
            sfd.Filter = "MTD format(*.mtd)|*.mtd";
            sfd.Title = "Save file dialog";

            if (sfd.ShowDialog() == true)
            {
                if (Path.GetDirectoryName(sfd.FileName) != this.projectProperty.ProjectFolderPath) {
					MessageBox.Show("Save folder should be the same folder as analysis files.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
					return;
				}

                this.projectProperty.ProjectFilePath = sfd.FileName;

                Mouse.OverrideCursor = Cursors.Wait;

                var window = new ShortMessageWindow() {
                    Owner = mainWindow,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Label_MessageTitle.Text = "Saving the project..";
                window.Show();

                if (this.projectProperty.Ionization == Ionization.ESI)
                {
                    this.saveProperty = DataStorageLcUtility.GetSavePropertyBean(this.projectProperty, this.rdamProperty, this.mspDB
                    , this.iupacReference, this.analysisParamForLC, this.analysisFiles, this.alignmentFiles
                    , this.postIdentificationTxtDB, this.targetFormulaLibrary);
                }
                else
                {
                    this.saveProperty = DataStorageGcUtility.GetSavePropertyBean(this.projectProperty, this.rdamProperty, this.mspDB
                    , this.iupacReference, this.analysisParamForGC, this.analysisFiles, this.alignmentFiles);
                }


                if (this.focusedFileID >= 0) {
                    if (this.projectProperty.Ionization == Ionization.ESI)
                        MessagePackHandler.SaveToFile<ObservableCollection<PeakAreaBean>>(this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection, this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                    //DataStorageMsdialUtility.SaveToXmlFile(this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection, this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath, typeof(ObservableCollection<PeakAreaBean>));
                    else
                        DataStorageGcUtility.WriteMs1DecResults(this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.DeconvolutionFilePath, this.ms1DecResults);
                }
                DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(this.analysisFiles[this.focusedFileID]);

                if (this.focusedAlignmentFileID >= 0)
                    MessagePackHandler.SaveToFile<AlignmentResultBean>(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath);
                //DataStorageMsdialUtility.SaveToXmlFile(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath, typeof(AlignmentResultBean));

                MessagePackHandler.SaveToFile<SavePropertyBean>(this.saveProperty, this.projectProperty.ProjectFilePath);
                //DataStorageMsdialUtility.SaveToXmlFile(this.saveProperty, this.projectProperty.ProjectFilePath, typeof(SavePropertyBean));
                DataStorageLcUtility.SetPeakAreaBeanCollection(this.analysisFiles[this.focusedFileID], this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);


                window.Close();
                Mouse.OverrideCursor = null;
            }
        }

        private void saveProjectMenu_Click(object sender, RoutedEventArgs e)
        {
            if (this.analysisFiles == null) return;

            Mouse.OverrideCursor = Cursors.Wait;
            var window = new ShortMessageWindow() {
                Owner = mainWindow,
                WindowStartupLocation = WindowStartupLocation.CenterOwner
            };
            window.Label_MessageTitle.Text = "Saving the project..";
            window.Show();

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                this.saveProperty = DataStorageLcUtility.GetSavePropertyBean(this.projectProperty, this.rdamProperty, this.mspDB
                , this.iupacReference, this.analysisParamForLC, this.analysisFiles, this.alignmentFiles
                , this.postIdentificationTxtDB, this.targetFormulaLibrary);
            }
            else
            {
                this.saveProperty = DataStorageGcUtility.GetSavePropertyBean(this.projectProperty, this.rdamProperty, this.mspDB
                , this.iupacReference, this.analysisParamForGC, this.analysisFiles, this.alignmentFiles);
            }

            if (this.focusedFileID >= 0) {
                if (this.projectProperty.Ionization == Ionization.ESI)
                    MessagePackHandler.SaveToFile<ObservableCollection<PeakAreaBean>>(this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection, this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
                //DataStorageMsdialUtility.SaveToXmlFile(this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection, this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath, typeof(ObservableCollection<PeakAreaBean>));
                else
                    DataStorageGcUtility.WriteMs1DecResults(this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.DeconvolutionFilePath, this.ms1DecResults);
            }
            DataRefreshLcUtility.PeakInformationCollectionMemoryRefresh(this.analysisFiles[this.focusedFileID]);

            if (this.focusedAlignmentFileID >= 0)
                MessagePackHandler.SaveToFile<AlignmentResultBean>(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath);
            //DataStorageMsdialUtility.SaveToXmlFile(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath, typeof(AlignmentResultBean));

            MessagePackHandler.SaveToFile<SavePropertyBean>(this.saveProperty, this.projectProperty.ProjectFilePath);
            //DataStorageMsdialUtility.SaveToXmlFile(this.saveProperty, this.projectProperty.ProjectFilePath, typeof(SavePropertyBean));
            DataStorageLcUtility.SetPeakAreaBeanCollection(this.analysisFiles[this.focusedFileID], this.analysisFiles[this.focusedFileID].AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);

            window.Close();

            Mouse.OverrideCursor = null;
        }

        private void downloadOnlineMsMsMenu_Click(object sender, RoutedEventArgs e)
        {
            var window = new DownloadOnlineMsMsRecordWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private async void allProcessing_Click(object sender, RoutedEventArgs e)
        {

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                mainWindow.AnalysisParamForLC.ProcessOption = ProcessOption.All;
                var window = new AnalysisParamSetForLcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();
                    var isValid = await new ProcessManagerLC(true).RunAllProcess(this);
                }
            }
            else
            {
                mainWindow.AnalysisParamForGC.ProcessOption = ProcessOption.All;
                var window = new AnalysisParamSetForGcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();
                    await new AnalysisFileProcessForGCMulti().Process(this);
                }
            }
        }


        private async void identificationProcessing_Click(object sender, RoutedEventArgs e)
        {

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                mainWindow.AnalysisParamForLC.ProcessOption = ProcessOption.IdentificationPlusAlignment;


                var window = new AnalysisParamSetForLcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();
                    var isValid = await new ProcessManagerLC(true).RunAllProcess(this);
                }
            }


            else
            {

                mainWindow.AnalysisParamForGC.ProcessOption = ProcessOption.IdentificationPlusAlignment;
                var window = new AnalysisParamSetForGcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();



                    await new AnalysisFileProcessForGCMulti().Process(this);
                }
            }


        }

        private async void alignmetProcessing_Click(object sender, RoutedEventArgs e)
        {
            if (this.AnalysisFiles.Count <= 1) return;
            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                mainWindow.AnalysisParamForLC.ProcessOption = ProcessOption.Alignment;

                var window = new AnalysisParamSetForLcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();
                    var isValid = await new ProcessManagerLC(true).RunAlignmentProcess(this);
                }
            }
            else
            {
                mainWindow.AnalysisParamForGC.ProcessOption = ProcessOption.Alignment;

                var window = new AnalysisParamSetForGcWin(mainWindow);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

                if (window.ShowDialog() == true)
                {
                    PeakViewDataAccessRefresh();
                    await new JointAlignerProcessGC().Execute(this);
                }
            }
        }

        private void normalizationMethod_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var window = new NormalizationSetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Title = "Normalization for " + this.alignmentFiles[this.focusedAlignmentFileID].FileName;

            if (window.ShowDialog() == true)
            {
                this.focusedAlignmentResult.Normalized = true;
                MessagePackHandler.SaveToFile<AlignmentResultBean>(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath);
                //DataStorageMsdialUtility.SaveToXmlFile(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath, typeof(AlignmentResultBean));
                BarChartDisplayMode = BarChartDisplayMode.NormalizedHeight;
                menuItemRefresh();
                Update_BarChart();
                Reset_AlignmentTableViewer();
            }
        }

        private void principalComponentAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var window = new PcaSetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            if (window.ShowDialog() == true)
            {
                this.multivariateAnalysisResultWin = new MultivariateAnalysisResultWin(
                   this.multivariateAnalysisResult);
                this.multivariateAnalysisResultWin.Owner = this;
                this.multivariateAnalysisResultWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                this.multivariateAnalysisResultWin.Show();
            }
        }

        private void partialLeastSquares_Click(object sender, RoutedEventArgs e) {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var window = new PlsSetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true) {

                this.multivariateAnalysisResultWin = new MultivariateAnalysisResultWin(
                    this.multivariateAnalysisResult);
                this.multivariateAnalysisResultWin.Owner = this;
                this.multivariateAnalysisResultWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                this.multivariateAnalysisResultWin.Show();
            }
        }

        private void hierarchicalclusteringanalysis_Click(object sender, RoutedEventArgs e) {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            var window = new HcaSetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            if (window.ShowDialog() == true) {

                Mouse.OverrideCursor = Cursors.Wait;
                this.hcaResultWin = new HcaResultWin(this.multivariateAnalysisResult);
                this.hcaResultWin.Owner = this;
                this.hcaResultWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                this.hcaResultWin.Show();

                Mouse.OverrideCursor = null;
            }
        }

        private void MenuItem_MsmsMolecularNetwork_Click(object sender, RoutedEventArgs e) {
            if (this.FocusedFileID < 0) {
                return;
            }

            var window = new MolecularNetworkSettingWin();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void identificationSetting_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }


            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                var window = new LcmsIdentificationPropertySetWin(this);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Title = "Identification property setting for " + this.alignmentFiles[this.focusedAlignmentFileID].FileName;
                window.Show();
            }
            else
            {
                var window = new GcmsIdentificationPropertySetWin(this);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Title = "Identification property setting for " + this.alignmentFiles[this.focusedAlignmentFileID].FileName;
                window.Show();
            }


            this.windowOpened = true;
        }

        private void filePropertySetting_Click(object sender, RoutedEventArgs e)
        {
            var window = new FilePropertySetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void classPropertySetting_Click(object sender, RoutedEventArgs e) {
            var window = new FileClassSettingWin(this.projectProperty);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void ProjectPropertySetting_Click(object sender, RoutedEventArgs e)
        {
            var window = new ProjectPropertySetWin();
            window.Owner = this;
            var plvm = new ProjectPropertySetVM();
            window.DataContext = plvm;

            window.Show();
        }

        private void alignmentResultPropertySetting_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }


            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                var window = new LcmsNormalizationPropertySetWin(this);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Title = "Normalization property setting for " + this.alignmentFiles[this.focusedAlignmentFileID].FileName;
                window.Show();
            }
            else
            {
                var window = new GcmsNormalizationPropertySetWin(this);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.Title = "Normalization property setting for " + this.alignmentFiles[this.focusedAlignmentFileID].FileName;
                window.Show();
            }
            this.windowOpened = true;
        }

        private void menuItem_PeaklistExport_Click(object sender, RoutedEventArgs e)
        {
            var window = new PeaklistExportWin();
            window.Owner = this;
            var plvm = new PeaklistExportVM();
            window.DataContext = plvm;

            window.Show();
        }

        private void menuItem_AlignmentResultExport_Click(object sender, RoutedEventArgs e)
        {

            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0)
            {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var window = new AlignmentResultExportWin(mainWindow);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void menuItem_NormalizationResultExport_Click(object sender, RoutedEventArgs e)
        {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0)
            {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            //var filePath = @"C:\Users\tipputa\Desktop\test.pdf";

            var sfd = new SaveFileDialog();
            sfd.FileName = "NormalizationPlots.pdf";
            sfd.InitialDirectory = this.projectProperty.ProjectFolderPath;
            sfd.Filter = "PDF file (*.pdf)|*.pdf";
            sfd.Title = "Save file dialog";

            if (sfd.ShowDialog() == true)
            {
                Mouse.OverrideCursor = Cursors.Wait;
                var filePath = sfd.FileName;
                Msdial.Common.Export.DataExportAsPdf.ExportNormalizationResults(filePath, new List<AlignmentPropertyBean>(this.FocusedAlignmentResult.AlignmentPropertyBeanCollection), this.AnalysisFiles);
                Mouse.OverrideCursor = null;
            }
        }

        private void menuItem_HelpAbout_Click(object sender, RoutedEventArgs e)
        {
            var window = new HelpAboutWindow();
            window.DataContext = new HelpAboutWindowVM();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void menuItem_ParameterExport_Click(object sender, RoutedEventArgs e)
        {

            if (this.analysisParamForLC == null)
            {
                MessageBox.Show("The parameter is not set yet.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            var sfd = new SaveFileDialog();
            sfd.FileName = "*.txt";
            sfd.InitialDirectory = this.projectProperty.ProjectFolderPath;
            sfd.Filter = "Text format(*.txt)|*.txt";
            sfd.Title = "Save file dialog";

            if (sfd.ShowDialog() == true)
            {
                var filePath = sfd.FileName;



                var files = this.analysisFiles;
                var project = this.projectProperty;

                if (project.Ionization == Ionization.ESI)
                {
                    var param = this.analysisParamForLC;
                    DataExportLcUtility.ParameterExport(files, param, project, filePath);
                }
                else
                {
                    var param = this.analysisParamForGC;
                    DataExportGcUtility.ParameterExport(files, param, filePath);
                }
            }
        }

        private void MenuItem_DrawTicChromatogram_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;


            ChromatogramTicEicViewModel chromatogramTicEicViewModel = null;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                if (this.analysisParamForLC.IsIonMobility) {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramTicViewModel(this.accumulatedMs1Specra, this.analysisFiles[this.focusedFileID], this.projectProperty, this.analysisParamForLC);
                }
                else {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramTicViewModel(this.lcmsSpectrumCollection, this.analysisFiles[this.focusedFileID], this.projectProperty, this.analysisParamForLC);
                }
            }
            else
                chromatogramTicEicViewModel = UiAccessGcUtility.GetChromatogramTicViewModel(this.gcmsSpectrumList, this.analysisFiles[this.focusedFileID], this.analysisParamForGC);

            var window = new ExtractedIonChromatogramDisplayWin(chromatogramTicEicViewModel);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void MenuItem_DrawBpcChromatogram_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;

            ChromatogramTicEicViewModel chromatogramTicEicViewModel = null;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                if (this.analysisParamForLC.IsIonMobility) {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramBpcViewModel(
                    this.projectProperty, this.analysisFiles[this.focusedFileID],
                    this.analysisParamForLC, this.accumulatedMs1Specra);
                }
                else {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramBpcViewModel(
                       this.projectProperty, this.analysisFiles[this.focusedFileID],
                       this.analysisParamForLC, this.lcmsSpectrumCollection);
                }
            }
            else
                chromatogramTicEicViewModel = UiAccessGcUtility.GetChromatogramBpcViewModel(
                    this.analysisFiles[this.focusedFileID], this.analysisParamForGC, this.gcmsSpectrumList);

            var window = new ExtractedIonChromatogramDisplayWin(chromatogramTicEicViewModel);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void MenuItem_DrawTicBpcHighestEicChromatogram_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;

            ChromatogramTicEicViewModel chromatogramTicEicViewModel = null;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                if (this.analysisParamForLC.IsIonMobility) {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramTicBpcHighestEicViewModel(
                       this.projectProperty, this.analysisFiles[this.focusedFileID],
                       this.analysisParamForLC, this.accumulatedMs1Specra);
                }
                else {
                    chromatogramTicEicViewModel = UiAccessLcUtility.GetChromatogramTicBpcHighestEicViewModel(
                       this.projectProperty, this.analysisFiles[this.focusedFileID],
                       this.analysisParamForLC, this.lcmsSpectrumCollection);
                }
            }
            else
                chromatogramTicEicViewModel = UiAccessGcUtility.GetChromatogramTicBpcHighestEicViewModel(
                    this.analysisFiles[this.focusedFileID], this.analysisParamForGC, this.gcmsPeakAreaList, this.gcmsSpectrumList);

            var window = new ExtractedIonChromatogramDisplayWin(chromatogramTicEicViewModel);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }



        private void MenuItem_DrawEicChromatogram_Click(object sender, RoutedEventArgs e)
        {
            var window = new ExtractedIonChromatogramDisplaySetWin(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void MenuItem_ShowRtCorrection_Click(object sender, RoutedEventArgs e) {
            if (this.AnalysisParamForLC == null) return;
            if (this.AnalysisParamForLC.RetentionTimeCorrectionCommon == null) return;
        //    var filePath = projectProperty.ProjectFolderPath + "\\" + Path.GetFileNameWithoutExtension(projectProperty.ProjectFilePath);
        //    var detectedStdCommonList = Msdial.ChartHandler.Utility.RtCorrection.MakeCommonStdList(analysisFiles, AnalysisParamForLC.RetentionTimeCorrectionCommon.StandardLibrary);

        //    Msdial.DataExport.DataExportAsPdf.Export_RetentionTimeCorrection_All(filePath, new List<AnalysisFileBean>(analysisFiles), analysisParamForLC, analysisParamForLC.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam, detectedStdCommonList);


            mainWindow.AnalysisFiles = new ObservableCollection<AnalysisFileBean>(mainWindow.AnalysisFiles.
                OrderBy(x => x.AnalysisFilePropertyBean.AnalysisBatch).
                ThenBy(x => x.AnalysisFilePropertyBean.AnalysisFileAnalyticalOrder));
            var rtCorrectionWin = new RetentionTimeCorrection.RetentionTimeCorrectionWin(mainWindow, true);
            rtCorrectionWin.Owner = mainWindow;
            rtCorrectionWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            rtCorrectionWin.ShowDialog();
            mainWindow.AnalysisFiles = new ObservableCollection<AnalysisFileBean>(mainWindow.AnalysisFiles.OrderBy(x => x.AnalysisFilePropertyBean.AnalysisFileId));
        }

        public void MenuItem_ShowAlignmentSampleTable_Click(object sender, RoutedEventArgs e) {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            if (this.alignedDriftSpotBeanList != null && this.focusedAlignmentDriftID >= 0) {

                var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
                var uiName = ((ChromatogramAlignedEicUI)target).Name;
                //Debug.WriteLine(((ChromatogramAlignedEicUI)target).Name);
                //Debug.WriteLine(sender.ToString());

                if (uiName == "AlignedEicUIOnDrift") {
                    Show_AlignmentSampleTableOnIonMobility();
                }
                else {
                    Show_AlignmentSampleTable();
                }
            }
            else {
                Show_AlignmentSampleTable();
            }
        }

        private void MenuItem_AlignedChromatogramModification_Click(object sender, RoutedEventArgs e) {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            if (this.alignedDriftSpotBeanList != null && this.focusedAlignmentDriftID >= 0) {
                var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
                var uiName = ((ChromatogramAlignedEicUI)target).Name;
                //Debug.WriteLine(((ChromatogramAlignedEicUI)target).Name);
                //Debug.WriteLine(sender.ToString());

                if (uiName == "AlignedEicUIOnDrift") {
                    Show_AlignedChromatogramModificationWin(true);
                }
                else {
                    Show_AlignedChromatogramModificationWin();
                }
            }
            else {
                Show_AlignedChromatogramModificationWin();
            }
        }

        private void MenuItem_PeakSpotTableViewer_Click(object sender, RoutedEventArgs e) {
            if (this.analysisFiles == null || this.analysisFiles.Count == 0) return;
            // new TableViewerHandler().StartUpPeakSpotTableViewer(this);
            // don't need to await;
            _ = new TableViewerTaskHandler().StartUpPeakSpotTableViewer(this);
        }

        private void MenuItem_AlignmentSpotTableViewer_Click(object sender, RoutedEventArgs e) {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            //new TableViewerHandler().StartUpAlignmentSpotTableViewer(this);
            // don't need to await;
            _ = new TableViewerTaskHandler().StartUpAlignmentSpotTableViewer(this);

        }

        private void MenuItem_CorrelationBasedDeconvolution_ForAIF_SettingAndRun_Click(object sender, RoutedEventArgs e) {

            if (this.focusedAlignmentFileID < 0) {
                MessageBox.Show("Choose an alignment file from the file navigator.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            if (this.AnalysisFiles.Count <= 3) {
                MessageBox.Show("Sorry, it requires >3 samples", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            PeakViewDataAccessRefresh();
            if (AnalysisParamForLC.AnalysisParamOfMsdialCorrDec == null)
                AnalysisParamForLC.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();


            var window = new ForAIF.CorrDecSetting(AnalysisParamForLC.AnalysisParamOfMsdialCorrDec, this, FocusedAlignmentResult);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;

            window.Show();
        }

        private async void MenuItem_CorrelationBasedDeconvolution_ForAIF_Identification_Click(object sender, RoutedEventArgs e)
        {

            if (this.focusedAlignmentFileID < 0)
            {
                MessageBox.Show("Choose an alignment file from the file navigator.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            if (this.AnalysisFiles.Count <= 3)
            {
                MessageBox.Show("Sorry, it requires >3 samples", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            var filePath = projectProperty.ProjectFolderPath + "\\" + alignmentFiles[focusedAlignmentFileID].FileName + "_CorrelationBasedDecRes_Raw_0.cbd";
            if (!System.IO.File.Exists(filePath))
            {
                MessageBox.Show("Please run CorrDec program before identification process", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            PeakViewDataAccessRefresh();
            await new CorrDecIdentificationProcess().Process(this, FocusedAlignmentResult);
        }

        private async void MenuItem_CorrelationBasedDeconvolution_SingleSpot_Click(object sender, RoutedEventArgs e)
        {

            if (this.focusedAlignmentFileID < 0)
            {
                MessageBox.Show("Choose an alignment file from the file navigator.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            if (this.AnalysisFiles.Count <= 3)
            {
                MessageBox.Show("Sorry, it requires >3 samples", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            PeakViewDataAccessRefresh();
            if (AnalysisParamForLC.AnalysisParamOfMsdialCorrDec == null)
                AnalysisParamForLC.AnalysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();


            var window = new ForAIF.CorrDecSetting(AnalysisParamForLC.AnalysisParamOfMsdialCorrDec, this, FocusedAlignmentResult, true);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }
        private void MenuItem_CorrelationBasedDeconvolution_ShowResult_Click(object sender, RoutedEventArgs e)
        {

            if (this.focusedAlignmentFileID < 0)
            {
                MessageBox.Show("Choose an alignment file from the file navigator.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            if (this.AnalysisFiles.Count <= 3)
            {
                MessageBox.Show("Sorry, it requires >3 samples", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            SetAifViewerControllerForCorrDec();
            aifViewerController.AifViewControlForPeakVM.AlignmentID = this.focusedAlignmentPeakID;
            aifViewerController.AifViewControlForPeakVM.ShowCorrelDecRes();
        }

        private void SetAifViewerControllerForCorrDec() {
            if (ProjectProperty.CheckAIF) {
                if (aifViewerController == null) {
                    aifViewerController = new AifViewerControl(new AifViewControlCommonProperties(ProjectProperty, AnalysisFiles, AnalysisParamForLC, LcmsSpectrumCollection, MsdialIniField, analysisFiles[focusedFileID], mspDB), peakViewDecFS, peakViewDecSeekPoints, 0);
                    aifViewerController.Closed += aifViewerControllerClose;
                    aifViewerController.AifViewControlForPeakVM.Checker.PropertyChanged += aifViewerController_propertyChanged;
                    aifViewerController.Owner = this;
                    aifViewerController.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    aifViewerController.Show();
                    SetAlignmentFileForAifViewerController();
                }
            } else {
                if (aifViewerController != null) {
                    aifViewerController.Close();
                }
                aifViewerController = new AifViewerControl(new AifViewControlCommonProperties(ProjectProperty, AnalysisFiles, AnalysisParamForLC, LcmsSpectrumCollection, MsdialIniField, analysisFiles[focusedFileID], mspDB), peakViewDecFS, peakViewDecSeekPoints, 0);
                aifViewerController.AifViewControlForPeakVM.Checker.PropertyChanged += aifViewerController_propertyChanged;
                SetAlignmentFileForAifViewerController(force: true);
            }
        }

        private void MenuItem_MsViewerForAIF2_Click(object sender, RoutedEventArgs e) {

            var outputs = new List<CorrDecResult>();
            var alignmentFile = alignmentFiles[focusedAlignmentFileID];
            //for (var numDec = 0; numDec < projectProperty.Ms2LevelIdList.Count; numDec++) {
            var numDec = 0;
                var output = new CorrDecResult();
                var decFilePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_MsGrouping_Raw_" + numDec + ".mfg";
                var filePath = projectProperty.ProjectFolderPath + "\\" + alignmentFile.FileName + "_CorrelationBasedDecRes_Raw_" + numDec + ".cbd";
                using (var fs = File.Open(decFilePath, FileMode.Open, FileAccess.ReadWrite))
                {
                    var sp = CreateMzIntGroupListFile.ReadSeekPointsOfMsGroup(fs);
                    var msGroupRes = CreateMzIntGroupListFile.ReadMsGroupingResults(fs, sp, this.FocusedAlignmentPeakID);
                      var Ms2Dict = CorrDecHandler.CreateMsGroupDictionary(msGroupRes, this.AnalysisFiles.Count, 200);
                    var targetInt = this.FocusedAlignmentResult.AlignmentPropertyBeanCollection[this.FocusedAlignmentPeakID].AlignedPeakPropertyBeanCollection.Select(x => (double)x.Variable).ToArray();
                // CorrelDecHandler.CopyRawData(Ms2Dict, targetInt);
                // output.PeakMatrix = CorrelDecHandler.SingleSpotCorrelationCalculation(msGroupRes, this.AnalysisFiles.Count, this.FocusedAlignmentPeakID, this.FocusedAlignmentResult.AlignmentPropertyBeanCollection, AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.MinCorr_MS1, AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.CorrDiff_MS1, AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.CorrDiff_MS2, AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.MinCorr_MS2, AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.MinNumberOfSample, (int)(AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.MinDetectedPercentToVisualize * AnalysisFiles.Count), AnalysisParamForLC.AnalysisParamOfMsdialCorrDec.RemoveAfterPrecursor);
                //   outputs.Add(output);
            }
            var correlDecResMsViewer = new MsViewer.CorrelationDecResMsViewer(new AifViewControlCommonProperties(this.ProjectProperty, analysisFiles, this.AnalysisParamForLC, this.lcmsSpectrumCollection, MsdialIniField, this.AnalysisFiles[this.FocusedFileID], mspDB), this.FocusedAlignmentResult, outputs, this.ProjectProperty.ExperimentID_AnalystExperimentInformationBean.Values.Select(x => x.Name).ToList(), this.FocusedAlignmentPeakID);
            correlDecResMsViewer.Owner = this;
            correlDecResMsViewer.Title = "Correlation-based deconvolution";
            correlDecResMsViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            correlDecResMsViewer.Show();
            /*
            for (var numDec = 0; numDec < projectProperty.Ms2LevelIdList.Count; numDec++) {
                var decFilePath = projectProperty.ProjectFolderPath + "\\MsGrouping_Raw_" + 0 + ".mfg";
                var filePath = projectProperty.ProjectFolderPath + "\\CorrelationBasedDecRes_Raw_" + 0 + ".cbd";
                var correlMaxDif = 0.05f; var correlThreshold = 0.75f; var MinCorrelPrecursors = 0.9f;
                var peaks = CorrelDecHandler.SingleSpotCorrelationCalculation(decFilePath, this.AnalysisFiles.Count, this.FocusedAlignmentPeakID,
                         this.FocusedAlignmentResult.AlignmentPropertyBeanCollection, MinCorrelPrecursors, correlMaxDif, correlThreshold);
                CorrelDecHandler.WriteCorrelationDecRes(projectProperty, this.FocusedAlignmentResult.AlignmentPropertyBeanCollection, analysisFiles.Count, filePath, decFilePath);
                foreach (var peak in peaks) { foreach (var p in peak) { System.Diagnostics.Debug.Write(p + " "); } Debug.WriteLine(""); }
            }
            */
        }

        private void resetIonMobilityDataBrowser() {
            //check the current tab number
            if (this.analysisParamForLC.IsIonMobility) {
                this.Grid_EicViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_EicViewer.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                this.Grid_PeakSpotViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_PeakSpotViewer.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                this.Grid_AlignmentSpotViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_AlignmentSpotViewer.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                this.Grid_BarChart.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_BarChart.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);

                this.Grid_AlignedEIC.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_AlignedEIC.ColumnDefinitions[1].Width = new GridLength(1, GridUnitType.Star);
            }
            else {
                this.Grid_EicViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_EicViewer.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);

                this.Grid_PeakSpotViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_PeakSpotViewer.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);

                this.Grid_AlignmentSpotViewer.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_AlignmentSpotViewer.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);

                this.Grid_BarChart.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_BarChart.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);

                this.Grid_AlignedEIC.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
                this.Grid_AlignedEIC.ColumnDefinitions[1].Width = new GridLength(0, GridUnitType.Pixel);
            }
        }


        private void MenuItem_ParticularAlignmentSpotExport_Click(object sender, RoutedEventArgs e) {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            var window = new ForAIF.ParticularAlignmentSpotExporterWindow(this);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void MenuItem_ExperimentConfig_Click(object sender, RoutedEventArgs e) {
            var window = new ExperimentConfigViewer(this.projectProperty);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void menuItem_CopyScreenshotToClipboard_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var target = this;

                Drawing drawing = Utility.GetDrawingFromXaml((MainWindow)this);

                MemoryStream wmfStream = new MemoryStream();
                using (var graphics = Utility.CreateEmf(wmfStream, drawing.Bounds))
                    Utility.RenderDrawingToGraphics(drawing, graphics);
                wmfStream.Position = 0;
                System.Drawing.Imaging.Metafile metafile = new System.Drawing.Imaging.Metafile(wmfStream);
                IntPtr hEMF, hEMF2;
                hEMF = metafile.GetHenhmetafile(); // invalidates mf

                if (!hEMF.Equals(new IntPtr(0)))
                {
                    hEMF2 = ExtensionMethods.CopyEnhMetaFile(hEMF, new IntPtr(0));
                    if (!hEMF2.Equals(new IntPtr(0)))
                    {
                        if (ExtensionMethods.OpenClipboard(((IWin32Window)this.OwnerAsWin32()).Handle))
                        {
                            if (ExtensionMethods.EmptyClipboard())
                            {
                                ExtensionMethods.SetClipboardData(14 /*CF_ENHMETAFILE*/, hEMF2);
                                ExtensionMethods.CloseClipboard();
                            }
                        }
                    }
                    ExtensionMethods.DeleteEnhMetaFile(hEMF);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message);
            }
        }

        private void fragmentQuerySetting_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;

            var window = new FragmentQuerySetWin();
            window.Owner = this;

            var mpvm = new FragmentQuerySetVM();
            window.DataContext = mpvm;

            window.Show();
        }

        private void menuItem_MolecularSpectrumNetworkingExport_Click(object sender, RoutedEventArgs e) {

            if (this.focusedFileID < 0) return;

            var window = new MolecularNetworkExportWin();
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void PosNegAmalgamator_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;

            var window = new PosNegAmalgamatorSetWin();
            window.Owner = this;

            var mpvm = new PosNegAmalgamatorSetVM();
            window.DataContext = mpvm;

            window.Show();
        }

        #endregion

        #region // method for display range reset
        private void buttonClick_DisplayRangeAll_Reset(object sender, RoutedEventArgs e)
        {
            if (this.pairwisePlotFocus == PairwisePlotFocus.peakView)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMaxX = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MaxX;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMinX = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MinX;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMaxY = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MaxY;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMinY = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MinY;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
            }
            else if (this.pairwisePlotFocus == PairwisePlotFocus.alignmentView)
            {
                if (this.focusedAlignmentFileID < 0) return;
                if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMaxX = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MaxX;
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMinX = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MinX;
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMaxY = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MaxY;
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMinY = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MinY;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
            }
        }

        private void buttonClick_DisplayRangeRT_Reset(object sender, RoutedEventArgs e)
        {
            if (this.pairwisePlotFocus == PairwisePlotFocus.peakView)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMaxX = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MaxX;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMinX = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MinX;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
            }
            else if (this.pairwisePlotFocus == PairwisePlotFocus.alignmentView)
            {
                if (this.focusedAlignmentFileID < 0) return;
                if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMaxX = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MaxX;
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMinX = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MinX;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
            }
        }

        private void buttonClick_DisplayRangeMS1_Reset(object sender, RoutedEventArgs e)
        {
            if (this.pairwisePlotFocus == PairwisePlotFocus.peakView)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMaxY = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MaxY;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DisplayRangeMinY = ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.MinY;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
            }
            else if (this.pairwisePlotFocus == PairwisePlotFocus.alignmentView)
            {
                if (this.focusedAlignmentFileID < 0) return;
                if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMaxY = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MaxY;
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.DisplayRangeMinY = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.MinY;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
            }
        }

        private void buttonClick_DisplayRangeMS2_Reset(object sender, RoutedEventArgs e)
        {
            if (this.msmsDisplayFocus == MsMsDisplayFocus.ActVsRef)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMax = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MaxIntensity;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMin = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MinIntensity;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MaxMass;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MinMass;

                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMaxReference = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MaxIntensityReference;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMinReference = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MinIntensityReference;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMaxReference = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MaxMassReference;
                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMinReference = ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel.MinMassReference;

                ((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).RefreshUI();
            }
            else if (this.msmsDisplayFocus == MsMsDisplayFocus.Ms2Chrom)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((ChromatogramMrmUI)this.MS2ChromatogramUI.Content).ChromatogramMrmFE.ResetGraphDisplayRange();
            }
            else if (this.msmsDisplayFocus == MsMsDisplayFocus.RawVsDeco)
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramFE.ResetGraphDisplayRange();
                ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramFE.ResetGraphDisplayRange();
            }
            else if (this.msmsDisplayFocus == MsMsDisplayFocus.RepVsRef)
            {
                if (this.focusedAlignmentFileID < 0) return;
                if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMax = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MaxIntensity;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMin = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MinIntensity;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MaxMass;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MinMass;

                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMaxReference = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MaxIntensityReference;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeIntensityMinReference = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MinIntensityReference;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMaxReference = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MaxMassReference;
                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMinReference = ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).MassSpectrogramViewModel.MinMassReference;

                ((MassSpectrogramWithReferenceUI)this.RepAndRefMassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void ToggleButton_MeasurmentReferenceSpectraRaw_Checked(object sender, RoutedEventArgs e)
        {
            this.ToggleButton_MeasurmentReferenceSpectraDeconvoluted.IsChecked = false;
            this.reversiblaMassSpectraViewEnum = ReversibleMassSpectraView.raw;

            showReversibleSpectra();
        }

        private void ToggleButton_MeasurmentReferenceSpectraDeconvoluted_Checked(object sender, RoutedEventArgs e)
        {
            this.ToggleButton_MeasurmentReferenceSpectraRaw.IsChecked = false;
            this.reversiblaMassSpectraViewEnum = ReversibleMassSpectraView.component;

            showReversibleSpectra();
        }

        private void showReversibleSpectra()
        {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                if (this.projectProperty.MethodType == MethodType.ddMSMS) return;
                if (((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel == null) return;
                if (this.analysisParamForLC.IsIonMobility)
                {
                    var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(this.lcmsSpectrumCollection,
                        this.DriftSpotBeanList[this.FocusedDriftSpotID],
                        this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID],
                        this.analysisParamForLC, this.ProjectProperty, this.peakViewMS2DecResult,
                        this.reversiblaMassSpectraViewEnum, this.mspDB);
                    this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

                }
                else
                {
                    var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(this.lcmsSpectrumCollection,
                        this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID],
                        this.analysisParamForLC, this.peakViewMS2DecResult,
                        this.reversiblaMassSpectraViewEnum, this.projectProperty, this.mspDB);
                    this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
                }
            }
            else
            {
                if (((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel == null) return;
                var mass2SpectrogramViewModel = UiAccessGcUtility.GetReversibleSpectrumVM(this.gcmsSpectrumList, this.ms1DecResults[this.focusedMS1DecID], this.analysisParamForGC, this.reversiblaMassSpectraViewEnum, this.mspDB);
                this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
            }
        }

        private void ToggleButton_MsMsChromatogramRaw_Checked(object sender, RoutedEventArgs e)
        {
            this.ToggleButton_MsMsChromatogramDeconvoluted.IsChecked = false;
            this.ToggleButton_MsMsChromatogramRawAndDeconvoluted.IsChecked = false;

            this.mrmChromatogramViewEnum = MrmChromatogramView.raw;

            showDeconvolutedChromatograms();
        }

        private void ToggleButton_MsMsChromatogramDeconvoluted_Checked(object sender, RoutedEventArgs e)
        {
            this.ToggleButton_MsMsChromatogramRawAndDeconvoluted.IsChecked = false;
            this.ToggleButton_MsMsChromatogramRaw.IsChecked = false;

            this.mrmChromatogramViewEnum = MrmChromatogramView.component;

            showDeconvolutedChromatograms();
        }

        private void ToggleButton_MsMsChromatogramRawAndDeconvoluted_Checked(object sender, RoutedEventArgs e)
        {
            this.ToggleButton_MsMsChromatogramDeconvoluted.IsChecked = false;
            this.ToggleButton_MsMsChromatogramRaw.IsChecked = false;

            this.mrmChromatogramViewEnum = MrmChromatogramView.both;

            showDeconvolutedChromatograms();
        }

        private void showDeconvolutedChromatograms()
        {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                if (this.projectProperty.MethodType == MethodType.ddMSMS) return;
                if (((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel == null) return;


                ChromatogramMrmViewModel chromatogramMrmViewModel = null;
                if (this.AnalysisParamForLC.IsIonMobility)
                {
                    chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramIonMobilityViewModel(this.lcmsSpectrumCollection, this.ProjectProperty, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this.DriftSpotBeanList[this.FocusedDriftSpotID], this.analysisParamForLC, this.PeakViewMS2DecResult, this.mrmChromatogramViewEnum, this.solidColorBrushList);
                }
                else
                {
                    chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramViewModel(this.lcmsSpectrumCollection, this.ProjectProperty, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this.analysisParamForLC, this.projectProperty.ExperimentID_AnalystExperimentInformationBean, this.peakViewMS2DecResult, this.mrmChromatogramViewEnum, this.solidColorBrushList);
                }
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmViewModel);
            }
            else if (this.projectProperty.Ionization == Ionization.EI)
            {
                if (((MassSpectrogramWithReferenceUI)this.Ms2MassSpectrogramUI.Content).MassSpectrogramViewModel == null) return;

                var chromatogramMrmViewModel = UiAccessGcUtility.GetDeconvolutedChromatogramVM(this.gcmsSpectrumList, this.ms1DecResults[this.focusedMS1DecID], this.analysisParamForGC, this.mrmChromatogramViewEnum, this.solidColorBrushList);
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmViewModel);
            }
        }

        #endregion

        #region // method for context menu items
        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SaveSpectraTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
            SaveSpectraTableAsWin window = null;

            if (this.projectProperty.Ionization == Ionization.ESI)
                window = new SaveSpectraTableAsWin(target, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this);
            else
                window = new SaveSpectraTableAsWin(target, this.ms1DecResults[this.focusedMS1DecID], this);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SaveRepresentativeSpectraTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveSpectraTableAsWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI)
                window = new SaveSpectraTableAsWin(target, this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID], this);
            else
                window = new SaveSpectraTableAsWin(target, this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID], this);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopySpectraTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopySpectraTableAsWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI)
                window = new CopySpectraTableAsWin(target, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this);
            else
                window = new CopySpectraTableAsWin(target, this.ms1DecResults[this.focusedMS1DecID], this);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyRepresentativeSpectraTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopySpectraTableAsWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI)
                window = new CopySpectraTableAsWin(target, this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID], this);
            else
                window = new CopySpectraTableAsWin(target, this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID], this);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SaveSpectraAsUserSettingFormat_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
            if (this.projectProperty.Ionization == Ionization.ESI)
                MS2ExporterAsUserDefinedStyle.particular_settings_tada(target, this);
            //  else
            //       window = new SaveSpectraTableAsWin(target, this.ms1DecResults[this.focusedMS1DecID], this);
        }

        private void contextMenu_SaveSpectraAsUserSettingFormat_Alignment_Click(object sender, RoutedEventArgs e) {
            if (this.focusedAlignmentFileID  < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MS2ExporterAsUserDefinedStyle.particular_settings_tada(target, this, true);
            //  else
            //       window = new SaveSpectraTableAsWin(target, this.ms1DecResults[this.focusedMS1DecID], this);
        }


        private void contextMenu_SaveChromatogramTableAs_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveDataTableAsTextWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI)
                window = new SaveDataTableAsTextWin(target, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID]);
            else
                window = new SaveDataTableAsTextWin(target, this.ms1DecResults[this.focusedMS1DecID]);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyVariablesToClipboard_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;
            if (this.focusedAlignmentFileID < 0) return;
            if (this.focusedAlignmentPeakID < 0) return;

            var variables = this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID].AlignedPeakPropertyBeanCollection;
            var samplesString = String.Join("\t", variables.Select(n => n.FileName));
            var variavlesString = String.Join("\t", variables.Select(n => n.Variable));

            Clipboard.SetDataObject(samplesString + "\r\n" + variavlesString, true);
        }

        private void contextMenu_AddComponentToSearchList_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            StoreMsAnnotationTagsWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                var peakid = this.analysisParamForLC.IsIonMobility ? this.focusedMasterID : this.focusedPeakID;
                window = new StoreMsAnnotationTagsWin(this, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection, peakid);
            }
            else
                window = new StoreMsAnnotationTagsWin(this, this.ms1DecResults, this.focusedMS1DecID);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void contextMenu_AddComponentToSearchListAtAlignmentViewer_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;

            StoreMsAnnotationTagsWin window = null;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                var peakid = this.analysisParamForLC.IsIonMobility ? this.focusedAlignmentMasterID : this.focusedAlignmentPeakID;
                window = new StoreMsAnnotationTagsWin(this, this.focusedAlignmentResult, peakid);
            }
            else
                window = new StoreMsAnnotationTagsWin(this, this.focusedAlignmentMS1DecResults, this.focusedAlignmentMs1DecID);

            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        private void contextMenu_GoToMsFinderAsCentroid_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MsDialToExternalApps.SendToMsFinderProgram(this, ExportspectraType.centroid);
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.ms1DecResults[this.focusedMS1DecID]);
        }

        private void contextMenu_GoToMsFinderAsProfile_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MsDialToExternalApps.SendToMsFinderProgram(this, ExportspectraType.profile);
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.ms1DecResults[this.focusedMS1DecID]);
        }

        private void contextMenu_GoToMsFinderAsDeconvoluted_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MsDialToExternalApps.SendToMsFinderProgram(this, ExportspectraType.deconvoluted);
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.ms1DecResults[this.focusedMS1DecID]);
        }

        private void contextMenu_GoToMsFinderAsAlignmentResult_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MsDialToExternalApps.SendToMsFinderProgram(this);
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID]);
        }

        private void contextMenu_ViewAsOverlayedChromatograms_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;
            if (this.focusedAlignmentPeakID < 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            ChromatogramTicEicViewModel chromatogramTicEicVM;

            if (this.projectProperty.Ionization == Ionization.ESI){
                chromatogramTicEicVM = UiAccessLcUtility.GetMultiFilesEicsOfTargetPeak(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID],
                    this.analysisFiles, this.focusedFileID, this.lcmsSpectrumCollection, this.projectProperty, this.rdamProperty, this.analysisParamForLC);
            }
            else {
                chromatogramTicEicVM = UiAccessGcUtility.GetMultiFilesEicsOfTargetPeak(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID],
                    this.analysisFiles, this.focusedFileID, this.gcmsSpectrumList, this.rdamProperty, this.analysisParamForGC, this.projectProperty);
            }

            var displayWindow = new ExtractedIonChromatogramDisplayWin(chromatogramTicEicVM);
            displayWindow.Owner = this;
            displayWindow.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            displayWindow.Show();

            Mouse.OverrideCursor = null;
        }

        private void QuantMassUpdater_Click(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;
            if (this.focusedAlignmentFileID < 0) {
                MessageBox.Show("Please load an alignment result in your GC-MS project.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }
            if (this.projectProperty.Ionization == Ionization.ESI) {
                MessageBox.Show("This option is available at GC-MS project.", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            this.quantmassBrowser = new QuantmassBrowser(this, this.alignmentFiles[this.FocusedAlignmentFileID],
                this.focusedAlignmentResult, this.focusedAlignmentMs1DecID, this.mspDB, this.analysisParamForGC);
            this.quantmassBrowser.QuantmassBrowserVM.PropertyChanged -= mainWindow.QuantmassBrowser_propertyChanged;
            this.quantmassBrowser.QuantmassBrowserVM.PropertyChanged += mainWindow.QuantmassBrowser_propertyChanged;
            this.quantmassBrowser.Closed += mainWindow.QuantmassBrowserClose;

            this.quantmassBrowser.Owner = this;
            this.quantmassBrowser.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            this.quantmassBrowser.Show();

            this.MainWindowDisplayVM.Refresh();
            this.quantmassBrowser.UpdateLayout();
        }

        public void QuantmassBrowser_propertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;
            if (e.PropertyName == "SelectedData") {
                if (quantmassBrowserFlag) {
                    quantmassBrowserFlag2 = false;
                    this.focusedAlignmentPeakID = ((QuantmassBrowserVM)sender).SelectedData.AlignmentPropertyBean.AlignmentID;
                    var pairWisePlot = (PairwisePlotBean)((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean;
                    pairWisePlot.SelectedPlotId = this.focusedAlignmentPeakID;
                    var alignedSpot = this.FocusedAlignmentResult.AlignmentPropertyBeanCollection[this.FocusedAlignmentPeakID];
                    var rt = alignedSpot.CentralRetentionTime;
                    if (this.ProjectProperty.Ionization == Ionization.EI &&
                        this.analysisParamForGC.AlignmentIndexType == AlignmentIndexType.RI)
                        rt = alignedSpot.CentralRetentionIndex;
                    var mz = this.ProjectProperty.Ionization == Ionization.ESI ?
                        alignedSpot.CentralAccurateMass :
                        alignedSpot.QuantMass;

                    var rtTol = (pairWisePlot.DisplayRangeMaxX - pairWisePlot.DisplayRangeMinX) / 2;
                    var mzTol = (pairWisePlot.DisplayRangeMaxY - pairWisePlot.DisplayRangeMinY) / 2;
                    Debug.WriteLine("r:" + rt + ", mz:" + mz + ", rtol: " + rtTol + ", mtol: " + mzTol);
                    pairWisePlot.DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)pairWisePlot.MaxX);
                    pairWisePlot.DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)pairWisePlot.MinX);
                    pairWisePlot.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)pairWisePlot.MaxY);
                    pairWisePlot.DisplayRangeMinY = Math.Max((float)(mz -
                        mzTol), (float)pairWisePlot.MinY);
                    ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
                }
                else
                    quantmassBrowserFlag = true;
            }
        }

        private void contextMenu_BrowseSmootherBaselineCorrectionResult_Click(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (this.focusedPeakID < 0) return;
            if (this.ChromatogramXicUI.Content == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel == null) return;

            var chromVM = ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel;
            var chromBean = chromVM.ChromatogramBean;
            var chrom = chromBean.ChromatogramDataPointCollection;
            var peaklist = new List<double[]>(chrom);
            var ssPeaklist = Smoothing.LinearWeightedMovingAverage(Smoothing.LinearWeightedMovingAverage(peaklist, 1), 1);

            var baseline = Smoothing.SimpleMovingAverage(peaklist, 30);
            for (int i = 0; i < 1; i++) {
                baseline = Smoothing.SimpleMovingAverage(baseline, 30);
            }

            var bcorrectPeaklist = new List<double[]>();
            for (int i = 0; i < peaklist.Count; i++) {
                var intensity = ssPeaklist[i][3] - baseline[i][3];
                if (intensity < 0) intensity = 0;
                bcorrectPeaklist.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], intensity });
            }

            var noiseEstimateWidth = 50; // test
            var amplitudeDiffs = new List<double>();
            var counter = noiseEstimateWidth;
            var ampMax = double.MinValue;
            var ampMin = double.MaxValue;

            for (int i = 0; i < ssPeaklist.Count; i++) {
                if (counter < i) {
                    if (ampMax > ampMin) {
                        amplitudeDiffs.Add(ampMax - ampMin);
                    }

                    counter += noiseEstimateWidth;
                    ampMax = double.MinValue;
                    ampMin = double.MaxValue;
                }
                else {
                    if (ampMax < bcorrectPeaklist[i][3]) ampMax = bcorrectPeaklist[i][3];
                    if (ampMin > bcorrectPeaklist[i][3]) ampMin = bcorrectPeaklist[i][3];
                }
            }
            var ampNoise2 = 50.0; // ad hoc
            if (amplitudeDiffs.Count >= 10) {
                ampNoise2 = BasicMathematics.Median(amplitudeDiffs.ToArray());
            }

            var ampList = new List<double[]>();
            for (int i = 0; i < peaklist.Count; i++) {
                ampList.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], ampNoise2 * 3 });
            }

            var chromatogramBeanCollection = new ObservableCollection<ChromatogramBean>();
            var targetMz = chromBean.Mz;

            var originChrom = new ChromatogramBean(true, Brushes.Blue, 1.5,
                "Original",
                chromVM.Mass, chromVM.MassTolerance, chromVM.TargetRt,
                chromVM.TargetLeftRt, chromVM.TargetRightRt,
                true,
                new ObservableCollection<double[]>(peaklist));

            var ssChrom = new ChromatogramBean(true, Brushes.Red, 1.5,
               "Smoothed",
               chromVM.Mass, chromVM.MassTolerance, chromVM.TargetRt,
               chromVM.TargetLeftRt, chromVM.TargetRightRt,
               true,
               new ObservableCollection<double[]>(ssPeaklist));

            var baselineChrom = new ChromatogramBean(true, Brushes.Green, 1.5,
              "Baseline",
              chromVM.Mass, chromVM.MassTolerance, chromVM.TargetRt,
              chromVM.TargetLeftRt, chromVM.TargetRightRt,
              true,
              new ObservableCollection<double[]>(baseline));

            var baseCorrectedChrom = new ChromatogramBean(true, Brushes.Black, 1.5,
              "Corrected",
              chromVM.Mass, chromVM.MassTolerance, chromVM.TargetRt,
              chromVM.TargetLeftRt, chromVM.TargetRightRt,
              true,
              new ObservableCollection<double[]>(bcorrectPeaklist));

            var noiseChrom = new ChromatogramBean(true, Brushes.Pink, 1.5,
              "Noise",
              chromVM.Mass, chromVM.MassTolerance, chromVM.TargetRt,
              chromVM.TargetLeftRt, chromVM.TargetRightRt,
              true,
              new ObservableCollection<double[]>(ampList));

            chromatogramBeanCollection.Add(originChrom);
            chromatogramBeanCollection.Add(ssChrom);
            chromatogramBeanCollection.Add(baselineChrom);
            chromatogramBeanCollection.Add(baseCorrectedChrom);
            chromatogramBeanCollection.Add(noiseChrom);

            var chromUI = new ChromatogramAlignedEicUI(new ChromatogramTicEicViewModel(chromatogramBeanCollection, ChromatogramEditMode.Display, ChromatogramDisplayLabel.AnnotatedMetabolite,
                ChromatogramQuantitativeMode.Height, ChromatogramIntensityMode.Absolute,
                "Browse smoother and baseline correction", -1, "Selected files", "Selected files", "Selected files", -1));

            var win = new BrowseSmootherAndBaselineCorrectionWin(chromUI);
            win.Owner = this;
            win.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            win.Show();
        }
        #endregion

        #region // method for peak viewer
        public void FileNavigatorUserControlsRefresh(ObservableCollection<AnalysisFileBean> analysisFileBeanCollection)
        {
            this.ListBox_FileName.MouseDoubleClick -= listBox_FileName_MouseDoubleClick;
            this.ListBox_FileName.MouseDoubleClick += listBox_FileName_MouseDoubleClick;
            this.ListBox_FileName.DisplayMemberPath = "AnalysisFilePropertyBean.AnalysisFileName";
            this.ListBox_FileName.ItemsSource = analysisFileBeanCollection;

			this.ListBox_AlignedFiles.MouseDoubleClick -= listBox_AlignedFiles_MouseDoubleClick;
			this.ListBox_AlignedFiles.MouseDoubleClick += listBox_AlignedFiles_MouseDoubleClick;
            this.ListBox_AlignedFiles.DisplayMemberPath = "FileName";
            this.ListBox_AlignedFiles.ItemsSource = this.alignmentFiles;

            this.reversiblaMassSpectraViewEnum = ReversibleMassSpectraView.component;
            this.mrmChromatogramViewEnum = MrmChromatogramView.raw;

            resetIonMobilityDataBrowser();
        }

        public void PeakViewerForLcRefresh(int fileID)
        {
            var analysisFileBean = this.analysisFiles[fileID];

            this.focusedFileID = fileID;
            var rdamFileID = this.rdamProperty.RdamFilePath_RdamFileID[analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath];
            var measurementID = this.rdamProperty.RdamFileContentBeanCollection[rdamFileID].FileID_MeasurementID[analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId];

            if (!File.Exists(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath) && !Directory.Exists(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath)) {
                MessageBox.Show("The raw file cannot be found. Please put your all data in the same directory.",
                    "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            DataStorageLcUtility.SetPeakAreaBeanCollection(analysisFileBean, analysisFileBean.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
            if (analysisFileBean.PeakAreaBeanCollection == null || analysisFileBean.PeakAreaBeanCollection.Count == 0)
            {
                MessageBox.Show("There is no peak information in " + analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName +
                    ". So please select other files or check ion mode for it and re-analyze it with the ion mode setting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return;
            }
            this.rawDataAccess = new RawDataAccess(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true, analysisFileBean.RetentionTimeCorrectionBean.PredictedRt);
            this.rawMeasurement = DataAccessLcUtility.GetRawDataMeasurement(this.rawDataAccess);
            this.lcmsSpectrumCollection = DataAccessLcUtility.GetAllSpectrumCollection(this.rawMeasurement);
            this.accumulatedMs1Specra = DataAccessLcUtility.GetAccumulatedMs1SpectrumCollection(this.rawMeasurement);

            this.peakViewDecFS = File.Open(analysisFileBean.AnalysisFilePropertyBean.DeconvolutionFilePath, FileMode.Open, FileAccess.ReadWrite);
            this.peakViewDecSeekPoints = new List<long>();
            this.peakViewDecSeekPoints = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(this.peakViewDecFS);

            var isColorByCompoundClass = false; if (this.projectProperty.TargetOmics == TargetOmics.Lipidomics) isColorByCompoundClass = true;
            var pairwisePlotBean = UiAccessLcUtility.GetRtMzPairwisePlotPeakViewBean(analysisFileBean, isColorByCompoundClass, this.mspDB);
			ScanNumberFilter.Maximum = pairwisePlotBean.PeakAreaBeanCollection.Max(it => it.ScanNumberAtPeakTop);

			this.RtMzPairwisePlotPeakViewUI.Content = new PairwisePlotPeakViewUI(pairwisePlotBean);
            var content = (PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content;

            if (this.ProjectProperty.SeparationType == SeparationType.IonMobility)
            {
                content.PairwisePlotBean.PropertyChanged -= peakViewerOnDataDependentAcquisition_PropertyChanged;
                content.PairwisePlotBean.PropertyChanged += peakViewerOnDataDependentAcquisition_PropertyChanged;
            }
            else
            {
                if (this.projectProperty.MethodType == MethodType.diMSMS)
                {
                    content.PairwisePlotBean.PropertyChanged -= peakViewerOnDataIndependentAcquisition_PropertyChanged;
                    content.PairwisePlotBean.PropertyChanged += peakViewerOnDataIndependentAcquisition_PropertyChanged;
                }
                else if (this.projectProperty.MethodType == MethodType.ddMSMS)
                {
                    content.PairwisePlotBean.PropertyChanged -= peakViewerOnDataDependentAcquisition_PropertyChanged;
                    content.PairwisePlotBean.PropertyChanged += peakViewerOnDataDependentAcquisition_PropertyChanged;
                }
            }
            if (this.AnalysisParamForLC.IsIonMobility) {
                refreshDriftSpotViewer(analysisFileBean, 0);
            }

            if (projectProperty.CheckAIF && aifViewerController == null) {
                if(projectProperty.CollisionEnergyList == null || projectProperty.CollisionEnergyList.Count == 0) {
                    Mouse.OverrideCursor = null;
                    var window = new ExperimentConfigViewer(this.projectProperty);
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                    Mouse.OverrideCursor = Cursors.Wait;
                }
                aifViewerController = new AifViewerControl(new AifViewControlCommonProperties(ProjectProperty, AnalysisFiles, AnalysisParamForLC, LcmsSpectrumCollection, MsdialIniField, analysisFileBean, mspDB), peakViewDecFS, peakViewDecSeekPoints, 0);
                aifViewerController.Closed += aifViewerControllerClose;
                aifViewerController.AifViewControlForPeakVM.Checker.PropertyChanged += aifViewerController_propertyChanged;
                aifViewerController.Owner = this;
                aifViewerController.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                aifViewerController.Show();
            }
            else if (projectProperty.CheckAIF){
                aifViewerController.PeakFileChange(new AifViewControlCommonProperties(ProjectProperty, AnalysisFiles, AnalysisParamForLC, LcmsSpectrumCollection, MsdialIniField, analysisFileBean, mspDB), peakViewDecFS, peakViewDecSeekPoints, 0);
            }

            if (this.peakSpotTableViewer != null) {
                _ = new TableViewerTaskHandler().FileChangePeakSpotTableViewer(this);
            }

            content.PairwisePlotBean.SelectedPlotId = 0;
            this.mainWindowDisplayVM.Refresh();
        }

        private void refreshDriftSpotViewer(AnalysisFileBean file, int spotID) {
            var repSpotID = 0;
            var isLipidomics = this.projectProperty.TargetOmics == TargetOmics.Lipidomics ? true : false;
            var param = this.analysisParamForLC;
            if (param == null) return;
            var driftPairwisePlotBean = UiAccessLcUtility.GetDriftTimeMzPairwisePlotPeakViewBean(file, spotID, this.analysisParamForLC.IonMobilityType,
                param, out repSpotID, isLipidomics, this.mspDB);

            this.DriftSpotBeanList = driftPairwisePlotBean.DriftSpots;
            if (driftPairwisePlotBean == null) return;
            this.DriftTimeMzPairwisePlotPeakViewUI.Content = new PairwisePlotPeakViewUI(driftPairwisePlotBean);
            var driftContent = (PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content;

            var peakSpot = file.PeakAreaBeanCollection[spotID];
            float[] rectangleRange = getRectangleRange(peakSpot, param.AccumulatedRtRagne);

            ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeXmin = rectangleRange[0];
            ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeXmax = rectangleRange[1];
            ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeYmin = rectangleRange[2];
            ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeYmax = rectangleRange[3];

            driftContent.PairwisePlotBean.PropertyChanged -= driftSpotViewerOnDataDependentAcquisition_PropertyChanged;
            driftContent.PairwisePlotBean.PropertyChanged += driftSpotViewerOnDataDependentAcquisition_PropertyChanged;

            driftContent.PairwisePlotBean.SelectedPlotId = repSpotID;
            driftContent.RefreshUI();
            this.mainWindowDisplayVM.DriftViewRefresh();
        }

        public void PeakViewerForGcRefresh(int fileID)
        {
            AnalysisFileBean analysisFileBean = this.analysisFiles[fileID];

            this.focusedFileID = fileID;
            int rdamFileID = this.rdamProperty.RdamFilePath_RdamFileID[analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath];
            int measurementID = this.rdamProperty.RdamFileContentBeanCollection[rdamFileID].FileID_MeasurementID[analysisFileBean.AnalysisFilePropertyBean.AnalysisFileId];

            if (!File.Exists(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath) && !Directory.Exists(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath)) {
				MessageBox.Show("The raw file cannot be found. Please put all your data in the same directory.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}

            this.gcmsPeakAreaList = DataStorageGcUtility.GetPeakAreaList(analysisFileBean.AnalysisFilePropertyBean.PeakAreaBeanInformationFilePath);
            if (this.gcmsPeakAreaList == null || this.gcmsPeakAreaList.Count == 0) {
				MessageBox.Show("There is no peak information in " + analysisFileBean.AnalysisFilePropertyBean.AnalysisFileName + ". So please select other files or check ion mode for it and re-analyze it with the ion mode setting.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
				return;
			}
            this.rawDataAccess = new RawDataAccess(analysisFileBean.AnalysisFilePropertyBean.AnalysisFilePath, measurementID, false, false, true);
            this.gcmsSpectrumList = DataAccessGcUtility.GetRdamSpectrumList(this.rawDataAccess);

            this.ms1DecResults = DataStorageGcUtility.ReadMS1DecResults(analysisFileBean.AnalysisFilePropertyBean.DeconvolutionFilePath);

            var pairwisePlotBean = UiAccessGcUtility.GetRtMzPairwisePlotPeakViewBean(analysisFileBean, this.gcmsPeakAreaList, this.ms1DecResults);
            this.RtMzPairwisePlotPeakViewUI.Content = new PairwisePlotPeakViewUI(pairwisePlotBean);
            var content = (PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content;
            content.PairwisePlotBean.PropertyChanged -= peakViewerOnGcmsAcquisition_PropertyChanged;
            content.PairwisePlotBean.PropertyChanged += peakViewerOnGcmsAcquisition_PropertyChanged;
            content.PairwisePlotBean.SelectedPlotId = 0;
            content.PairwisePlotBean.SelectedMs1DecID = 0;

            if (this.peakSpotTableViewer != null) {
                _ = new TableViewerTaskHandler().FileChangePeakSpotTableViewer(this);
            }

            this.mainWindowDisplayVM.Refresh();
        }


        public void PeakTableViewer_propertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "SelectedData")
            {
                if (this.focusedFileID < 0) return;
                if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;
                if (peakSpotTableViewerFlag)
                {
                    peakSpotTableViewerFlag2 = false;

                    //Debug.WriteLine(((PeakSpotTableViewerVM)sender).SelectedData.PeakID);

                    var isIonMobility = this.projectProperty.Ionization == Ionization.ESI && this.analysisParamForLC.IsIonMobility;
                    float rt, mz;

                    if (isIonMobility)
                    {
                        var selectedData = ((PeakSpotTableViewerVM)sender).SelectedData;
                        this.focusedMasterID = selectedData.PeakID;

                        var rtmzPairwise = (PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean;
                        var dtmzPairwise = (PairwisePlotBean)((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).PairwisePlotBean;
                        var file = this.analysisFiles[this.focusedFileID];

                        if (selectedData.Mobility <= 0)
                        { // it measn that users selected a peak on rt/mz plot.
                            var peakID = selectedData.PeakAreaBean.PeakID;
                            this.focusedPeakID = peakID;

                            rtmzPairwise.SelectedPlotId = this.focusedPeakID;
                            var peak = file.PeakAreaBeanCollection[this.focusedPeakID];
                            rt = peak.RtAtPeakTop;
                            mz = peak.AccurateMass;

                            updateRtMzPairwisePlot(rt, mz, rtmzPairwise);
                        }
                        else
                        {

                            var driftID = selectedData.DriftSpotBean.PeakID;
                            var isSpotExist = false;
                            var focusedDriftSpotID = -1;
                            foreach (var drift in this.driftSpotBeanList)
                            {
                                if (drift.MasterPeakID == this.focusedMasterID)
                                {
                                    isSpotExist = true;
                                    focusedDriftSpotID = drift.DisplayedSpotID;
                                    break;
                                }
                            }
                            if (isSpotExist)
                            {
                                this.focusedDriftSpotID = focusedDriftSpotID;
                                dtmzPairwise.SelectedPlotId = this.focusedDriftSpotID;
                                var peak = this.driftSpotBeanList[this.focusedDriftSpotID];
                                rt = peak.DriftTimeAtPeakTop;
                                mz = peak.AccurateMass;

                                updateDtMzPairwisePlot(rt, mz, dtmzPairwise);
                            }
                            else
                            { // in case there is no drift spot in the current drifts, new drift spots are generated by selected the parent peakarea spot.
                                var peakID = selectedData.DriftSpotBean.PeakAreaBeanID;
                                this.focusedPeakID = peakID;

                                rtmzPairwise.SelectedPlotId = this.focusedPeakID;
                                var peak = file.PeakAreaBeanCollection[this.focusedPeakID];
                                rt = peak.RtAtPeakTop;
                                mz = peak.AccurateMass;

                                updateRtMzPairwisePlot(rt, mz, rtmzPairwise);
                            }
                        }
                    }
                    else
                    {
                        this.focusedPeakID = ((PeakSpotTableViewerVM)sender).SelectedData.PeakID;
                        var pairwisePlot = (PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean;
                        var file = this.analysisFiles[this.focusedFileID];
                        if (this.ProjectProperty.Ionization == Ionization.ESI)
                        {
                            pairwisePlot.SelectedPlotId = this.focusedPeakID;
                            var peak = file.PeakAreaBeanCollection[this.focusedPeakID];
                            rt = peak.RtAtPeakTop;
                            mz = peak.AccurateMass;

                        }
                        else
                        {
                            pairwisePlot.SelectedMs1DecID = this.focusedPeakID;
                            var peak = this.ms1DecResults[this.focusedPeakID];
                            rt = peak.RetentionTime;
                            mz = peak.BasepeakMz;
                        }
                        updateRtMzPairwisePlot(rt, mz, pairwisePlot);
                        //var rtTol = (pairwisePlot.DisplayRangeMaxX - pairwisePlot.DisplayRangeMinX) / 2;
                        //var mzTol = (pairwisePlot.DisplayRangeMaxY - pairwisePlot.DisplayRangeMinY) / 2;
                        //((PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean).DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)pairwisePlot.MaxX);
                        //((PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean).DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)pairwisePlot.MinX);
                        //((PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean).DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)pairwisePlot.MaxY);
                        //((PairwisePlotBean)((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean).DisplayRangeMinY = Math.Max((float)(mz - mzTol), (float)pairwisePlot.MinY);
                        //((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
                    }
                }
                else
                    peakSpotTableViewerFlag = true;
            }
            else if (e.PropertyName == "ExportPeakSpotsToMsFinder")
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "Please select export folder.";

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath.Length > 200)
                    {
                        MessageBox.Show("The folder is too nested, please chenge folder path (less than 200 length).\nCurrent folder path length: " + fbd.SelectedPath.Length, "Error", MessageBoxButton.OK);
                        return;
                    }
                    var target = PeakSpotTableViewer.PeakSpotTableViewerVM.Source.Where(x => x.Checked).Select(x => x.PeakAreaBean);
                    MsDialToExternalApps.SendToMsFinderProgramSelectedPeakSpots(this, fbd.SelectedPath, target);
                }
            }
            else if (e.PropertyName == "ExportPeakSpotsAsMsp")
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "Please select export folder.";

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if(fbd.SelectedPath.Length > 220)
                    {
                        MessageBox.Show("The folder is too nested, please chenge folder path (less than 230 length).\nCurrent folder path length: " + fbd.SelectedPath.Length, "Error", MessageBoxButton.OK);
                        return;
                    }
                    var target = PeakSpotTableViewer.PeakSpotTableViewerVM.Source.Where(x => x.Checked).Select(x => x.PeakAreaBean);
                    foreach (var spot in target)
                    {
                        var ms2Dec = SpectralDeconvolution.ReadMS2DecResult(this.peakViewDecFS, this.peakViewDecSeekPoints, spot.PeakID);
                        var filePath = fbd.SelectedPath + "\\ID" + spot.PeakID.ToString("00000") + "_" + Math.Round(spot.RtAtPeakTop, 2).ToString() + "_" + Math.Round(spot.AccurateMass, 2).ToString() + ".msp";
                        if (filePath.Length < 260)
                        {
                            using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.ASCII))
                            {
                                Msdial.Common.Export.ExportMassSpectrum.WriteSpectrumFromPeakSpot(sw, mainWindow.ProjectProperty, mainWindow.MspDB, mainWindow.AnalysisParamForLC, ms2Dec, spot, LcmsSpectrumCollection, mainWindow.AnalysisFiles[FocusedFileID], true);
                            }
                        }
                    }
                    MessageBox.Show("Exported.", "Notice", MessageBoxButton.OK);
                }
            }
        }
        private void updateDtMzPairwisePlot(float rt, float mz, PairwisePlotBean dtmzPairwise) {
            var rtTol = (dtmzPairwise.DisplayRangeMaxX - dtmzPairwise.DisplayRangeMinX) / 2;
            var mzTol = (dtmzPairwise.DisplayRangeMaxY - dtmzPairwise.DisplayRangeMinY) / 2;
            dtmzPairwise.DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)dtmzPairwise.MaxX);
            dtmzPairwise.DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)dtmzPairwise.MinX);
            dtmzPairwise.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)dtmzPairwise.MaxY);
            dtmzPairwise.DisplayRangeMinY = Math.Max((float)(mz - mzTol), (float)dtmzPairwise.MinY);
            ((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).RefreshUI();
        }

        private void updateRtMzPairwisePlot(float rt, float mz, PairwisePlotBean rtmzPairwise) {
            var rtTol = (rtmzPairwise.DisplayRangeMaxX - rtmzPairwise.DisplayRangeMinX) / 2;
            var mzTol = (rtmzPairwise.DisplayRangeMaxY - rtmzPairwise.DisplayRangeMinY) / 2;
            rtmzPairwise.DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)rtmzPairwise.MaxX);
            rtmzPairwise.DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)rtmzPairwise.MinX);
            rtmzPairwise.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)rtmzPairwise.MaxY);
            rtmzPairwise.DisplayRangeMinY = Math.Max((float)(mz - mzTol), (float)rtmzPairwise.MinY);
            ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
        }

        private void peakViewerOnDataDependentAcquisition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId")
            {
                Mouse.OverrideCursor = Cursors.Wait;

                this.focusedPeakID = ((PairwisePlotBean)sender).SelectedPlotId;
                if (this.focusedPeakID < 0) return;

                var file = this.analysisFiles[this.focusedFileID];
                var peak = file.PeakAreaBeanCollection[this.focusedPeakID];
                var masterID = peak.MasterPeakID;
                this.focusedMasterID = masterID;

                var targetPeakID = this.analysisParamForLC.IsIonMobility ? masterID : peak.PeakID;
                if (this.PeakSpotTableViewer != null && peakSpotTableViewerFlag2) {
                    peakSpotTableViewerFlag = false;
                    this.PeakSpotTableViewer.PeakSpotTableViewerVM.ChangeSelectedData(targetPeakID);
                }
                peakSpotTableViewerFlag2 = true;

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.GraphTitle =
                    file.AnalysisFilePropertyBean.AnalysisFileName + "  " + "Spot ID: " + targetPeakID + "  " + "Scan: " + peak.ScanNumberAtPeakTop + "  "
                    + "RT: " + Math.Round(peak.RtAtPeakTop, 2).ToString() + " min  " + "Mass: m/z " + Math.Round(peak.AccurateMass, 5).ToString();

                this.peakViewMS2DecResult = SpectralDeconvolution.ReadMS2DecResult(this.peakViewDecFS, this.peakViewDecSeekPoints, targetPeakID);

                var chromatogramXicViewModel = UiAccessLcUtility.GetChromatogramXicViewModel(this.accumulatedMs1Specra, this.lcmsSpectrumCollection, peak, this.analysisParamForLC, file.AnalysisFilePropertyBean, this.projectProperty);
                var mass1SpectrogramViewModel = UiAccessLcUtility.GetMs1MassSpectrogramViewModel(this.accumulatedMs1Specra, this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.projectProperty);
                var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(peak, peakViewMS2DecResult, this.mspDB);
                var chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramViewModel(peak, peakViewMS2DecResult, this.solidColorBrushList);
                var mass2RawSpectrogramViewModel = UiAccessLcUtility.GetMs2RawMassspectrogramViewModel(this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.projectProperty);
                var mass2DeconvolutedSpectrogramViewModel = UiAccessLcUtility.GetMs2DeconvolutedMassspectrogramViewModel(peak, this.peakViewMS2DecResult);

                this.ChromatogramXicUI.Content = new ChromatogramXicUI(chromatogramXicViewModel);
                this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI(mass1SpectrogramViewModel);
                this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmViewModel);
                this.RawMassSpectrogramUI.Content = new MassSpectrogramUI(mass2RawSpectrogramViewModel);
                this.DeconvolutedMassSpectrogramUI.Content = new MassSpectrogramUI(mass2DeconvolutedSpectrogramViewModel);

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();

                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
                }

                if (((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= deconvolutedMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += deconvolutedMassSpectrogramViewModel_PropertyChanged;
                }

                if (this.analysisParamForLC.IsIonMobility) {
                    refreshDriftSpotViewer(file, this.focusedPeakID);
                }

                this.mainWindowDisplayVM.MetaDataLabelRefresh();

                Mouse.OverrideCursor = null;
            }
            else if (e.PropertyName == "DisplayRangeMinX" || e.PropertyName == "DisplayRangeMaxX")
            {
                if (((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();
            }
            else if (e.PropertyName == "DisplayRangeMinY" || e.PropertyName == "DisplayRangeMaxY")
            {
                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void peakViewerOnDataIndependentAcquisition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId")
            {
                Mouse.OverrideCursor = Cursors.Wait;
                this.focusedPeakID = ((PairwisePlotBean)sender).SelectedPlotId;
                var file = this.analysisFiles[this.focusedFileID];
                var peak = file.PeakAreaBeanCollection[this.focusedPeakID];
                if (this.PeakSpotTableViewer != null && peakSpotTableViewerFlag2) {
                    System.Diagnostics.Debug.WriteLine("changed for peak spot table");
                    peakSpotTableViewerFlag = false;
                    this.PeakSpotTableViewer.PeakSpotTableViewerVM.ChangeSelectedData(this.focusedPeakID);
                }
                peakSpotTableViewerFlag2 = true;
                if (focusedPeakID < 0) return;

                if (projectProperty.CheckAIF && aifViewerController != null) {
                    aifViewerController.AifViewControlForPeakVM.ChangePeakSpotId(this.focusedPeakID);
                }
                float[] rectangleRange = getRectangleRange(this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this.projectProperty.ExperimentID_AnalystExperimentInformationBean);

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.GraphTitle = file.AnalysisFilePropertyBean.AnalysisFileName + "  " + "ID: " + this.focusedPeakID + "  "
                    + "Scan: " + peak.ScanNumberAtPeakTop + "  " + "RT: " + Math.Round(peak.RtAtPeakTop, 2).ToString() + " min  " + "Mass: m/z " + Math.Round(peak.AccurateMass, 5).ToString();

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeXmin = rectangleRange[0];
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeXmax = rectangleRange[1];
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeYmin = rectangleRange[2];
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.RectangleRangeYmax = rectangleRange[3];

                this.peakViewMS2DecResult = SpectralDeconvolution.ReadMS2DecResult(this.peakViewDecFS, this.peakViewDecSeekPoints, this.focusedPeakID);

                ChromatogramXicViewModel chromatogramXicViewModel = UiAccessLcUtility.GetChromatogramXicViewModel(this.accumulatedMs1Specra, this.lcmsSpectrumCollection, peak, this.analysisParamForLC, file.AnalysisFilePropertyBean, this.projectProperty);
                MassSpectrogramViewModel mass1SpectrogramViewModel = UiAccessLcUtility.GetMs1MassSpectrogramViewModel(this.accumulatedMs1Specra, this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.projectProperty);
                MassSpectrogramViewModel mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.peakViewMS2DecResult, this.reversiblaMassSpectraViewEnum, this.projectProperty, this.mspDB);
                ChromatogramMrmViewModel chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramViewModel(this.lcmsSpectrumCollection, this.ProjectProperty, peak, this.analysisParamForLC, this.projectProperty.ExperimentID_AnalystExperimentInformationBean, this.peakViewMS2DecResult, this.mrmChromatogramViewEnum, this.solidColorBrushList);
                MassSpectrogramViewModel mass2RawSpectrogramViewModel = UiAccessLcUtility.GetMs2RawMassspectrogramViewModel(this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.projectProperty);
                MassSpectrogramViewModel mass2DeconvolutedSpectrogramViewModel = UiAccessLcUtility.GetMs2DeconvolutedMassspectrogramViewModel(peak, this.peakViewMS2DecResult);

                this.ChromatogramXicUI.Content = new ChromatogramXicUI(chromatogramXicViewModel);
                this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI(mass1SpectrogramViewModel);
                this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmViewModel);
                this.RawMassSpectrogramUI.Content = new MassSpectrogramUI(mass2RawSpectrogramViewModel);
                this.DeconvolutedMassSpectrogramUI.Content = new MassSpectrogramUI(mass2DeconvolutedSpectrogramViewModel);

                if (((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= deconvolutedMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += deconvolutedMassSpectrogramViewModel_PropertyChanged;
                }

                this.propertyCheck = false;

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();

                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();

                this.mainWindowDisplayVM.MetaDataLabelRefresh();

                Mouse.OverrideCursor = null;
            }
            else if (e.PropertyName == "DisplayRangeMinX" || e.PropertyName == "DisplayRangeMaxX")
            {
                if (((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();
            }
            else if (e.PropertyName == "DisplayRangeMinY" || e.PropertyName == "DisplayRangeMaxY")
            {
                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void peakViewerOnGcmsAcquisition_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (this.focusedFileID < 0) return;
            if (((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId") {
                #region
                Mouse.OverrideCursor = Cursors.Wait;

                this.focusedPeakID = ((PairwisePlotBean)sender).SelectedPlotId;
                if (this.focusedPeakID < 0) return;

                var file = this.analysisFiles[this.focusedFileID];
                var fileProp = file.AnalysisFilePropertyBean;
                var peak = this.gcmsPeakAreaList[this.focusedPeakID];
                var mass = Math.Round(peak.AccurateMass, 5).ToString(); if (this.analysisParamForGC.AccuracyType == AccuracyType.IsNominal) mass = Math.Round(peak.AccurateMass, 1).ToString();
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.GraphTitle = fileProp.AnalysisFileName + "  " + "ID: " + this.focusedPeakID + "  "
                    + "Scan: " + peak.ScanNumberAtPeakTop + "  " + "RT: " + Math.Round(peak.RtAtPeakTop, 3).ToString() + " min  " + "Mass: m/z " + mass;

                var chromatogramXicVM = UiAccessGcUtility.GetChromatogramXicVM(this.gcmsSpectrumList, peak.AccurateMass, peak.RtAtPeakTop, this.analysisParamForGC, "Extracted Ion");
                var rawSpectrumOnLeftVM = UiAccessGcUtility.GetRawMs1MassSpectrogramVM(this.gcmsSpectrumList, peak.AccurateMass, peak.RtAtPeakTop, peak.Ms1LevelDatapointNumber, this.analysisParamForGC);

                this.ChromatogramXicUI.Content = new ChromatogramXicUI(chromatogramXicVM);
                this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI(rawSpectrumOnLeftVM);

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();

                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
                }

                Mouse.OverrideCursor = null;
                #endregion
            } else if (e.PropertyName == "SelectedMs1DecID") {
                #region
                Mouse.OverrideCursor = Cursors.Wait;
                this.focusedMS1DecID = ((PairwisePlotBean)sender).SelectedMs1DecID;
                if (this.PeakSpotTableViewer != null && peakSpotTableViewerFlag2) {
                    peakSpotTableViewerFlag = false;
                    this.PeakSpotTableViewer.PeakSpotTableViewerVM.ChangeSelectedData(this.focusedMS1DecID);
                }
                peakSpotTableViewerFlag2 = true;

                if (this.focusedMS1DecID < 0) return;

                var file = this.analysisFiles[this.focusedFileID];
                var fileProp = file.AnalysisFilePropertyBean;
                var ms1dec = this.ms1DecResults[this.focusedMS1DecID];
                var mass = Math.Round(ms1dec.BasepeakMz, 4).ToString(); if (this.analysisParamForGC.AccuracyType == AccuracyType.IsNominal) mass = Math.Round(ms1dec.BasepeakMz, 1).ToString();

                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.GraphTitle = fileProp.AnalysisFileName + "  " + "ID: " + this.focusedMS1DecID + "  " + "Scan: " + ms1dec.ScanNumber + "  "
                    + "RT: " + Math.Round(ms1dec.RetentionTime, 3).ToString() + " min  " + "RI: " + Math.Round(ms1dec.RetentionIndex, 0).ToString() + "  " + "Quant mass: m/z " + mass;

                var chromatogramXicVM = UiAccessGcUtility.GetChromatogramXicVM(this.gcmsSpectrumList, ms1dec.BasepeakMz, ms1dec.RetentionTime, this.analysisParamForGC, "Model ion");
                var rawSpectrumOnLeftVM = UiAccessGcUtility.GetRawMs1MassSpectrogramVM(this.gcmsSpectrumList, ms1dec.BasepeakMz, ms1dec.RetentionTime, ms1dec.ScanNumber, this.analysisParamForGC);
                var reversibleSpectrumVM = UiAccessGcUtility.GetReversibleSpectrumVM(this.gcmsSpectrumList, ms1dec, this.analysisParamForGC, this.reversiblaMassSpectraViewEnum, this.mspDB);
                var chromatogramMrmVM = UiAccessGcUtility.GetDeconvolutedChromatogramVM(this.gcmsSpectrumList, ms1dec, this.analysisParamForGC, this.mrmChromatogramViewEnum, this.solidColorBrushList);
                var rawSpectrumOnRightVM = UiAccessGcUtility.GetRawMs1MassSpectrogramVM(this.gcmsSpectrumList, ms1dec, this.analysisParamForGC);
                var mass2DeconvolutedSpectrogramViewModel = UiAccessGcUtility.GetMs2DeconvolutedMassSpectrogramVM(ms1dec);

                this.ChromatogramXicUI.Content = new ChromatogramXicUI(chromatogramXicVM);
                this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI(rawSpectrumOnLeftVM);
                this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(reversibleSpectrumVM);
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmVM);
                this.RawMassSpectrogramUI.Content = new MassSpectrogramUI(rawSpectrumOnRightVM);
                this.DeconvolutedMassSpectrogramUI.Content = new MassSpectrogramUI(mass2DeconvolutedSpectrogramViewModel);

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();

                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
                }

                if (((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel != null)
                {
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= deconvolutedMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += deconvolutedMassSpectrogramViewModel_PropertyChanged;
                }

                this.mainWindowDisplayVM.MetaDataLabelRefresh();

                Mouse.OverrideCursor = null;
                #endregion
            } else if (e.PropertyName == "DisplayRangeMinX" || e.PropertyName == "DisplayRangeMaxX") {
                if (((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection == null || ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;

                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.ChromatogramXicUI.Content).RefreshUI();
            }
            else if (e.PropertyName == "DisplayRangeMinY" || e.PropertyName == "DisplayRangeMaxY")
            {
                if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null || ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void driftSpotViewerOnDataDependentAcquisition_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (this.focusedFileID < 0) return;
            if (this.focusedPeakID < 0) return;
            if (((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId") {
                Mouse.OverrideCursor = Cursors.Wait;
                this.FocusedDriftSpotID = ((PairwisePlotBean)sender).SelectedPlotId;
                if (this.FocusedDriftSpotID < 0) return;

                var file = this.analysisFiles[this.focusedFileID];
                var peakSpots = file.PeakAreaBeanCollection;
                var driftSpot = this.DriftSpotBeanList[this.FocusedDriftSpotID];
                this.selectedPeakViewDriftSpot = driftSpot;
                this.focusedMasterID = driftSpot.MasterPeakID;

                if (this.PeakSpotTableViewer != null && peakSpotTableViewerFlag2) {
                    peakSpotTableViewerFlag = false;
                    this.PeakSpotTableViewer.PeakSpotTableViewerVM.ChangeSelectedData(this.focusedMasterID);
                }
                peakSpotTableViewerFlag2 = true;

                ((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.GraphTitle =
                    file.AnalysisFilePropertyBean.AnalysisFileName + "  " + "Spot ID: " + driftSpot.MasterPeakID + "  "
                    + "Parent scan ID: " + driftSpot.PeakAreaBeanID + "  "
                    + "Mobility: " + Math.Round(driftSpot.DriftTimeAtPeakTop, 2).ToString() + " millisecond  "
                    + "Mass: m/z " + Math.Round(driftSpot.AccurateMass, 5).ToString();

                this.peakViewMS2DecResult = SpectralDeconvolution.ReadMS2DecResult(this.peakViewDecFS, this.peakViewDecSeekPoints, driftSpot.MasterPeakID);

                var chromatogramXicVM =
                    UiAccessLcUtility.GetChromatogramXicViewModel(this.lcmsSpectrumCollection, driftSpot, peakSpots, this.analysisParamForLC, file.AnalysisFilePropertyBean, this.projectProperty);

                //MassSpectrogramViewModel mass1SpectrogramViewModel = UiAccessLcUtility.GetMs1MassSpectrogramViewModel(this.lcmsSpectrumCollection, peak, this.analysisParamForLC, this.projectProperty);
                //MassSpectrogramViewModel mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(this.lcmsSpectrumCollection, driftSpot, this.analysisParamForLC,
                //peakViewMS2DecResult, ReversibleMassSpectraView.raw, this.projectProperty, this.mspDB);
                // ChromatogramMrmViewModel chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramViewModel(peakSpots[focusedPeakID], peakViewMS2DecResult, this.solidColorBrushList);
                ChromatogramMrmViewModel chromatogramMrmViewModel = UiAccessLcUtility.GetMs2ChromatogramIonMobilityViewModel(this.lcmsSpectrumCollection, this.ProjectProperty, this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this.DriftSpotBeanList[this.FocusedDriftSpotID], this.analysisParamForLC, this.PeakViewMS2DecResult, this.mrmChromatogramViewEnum, this.solidColorBrushList);

                var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(this.lcmsSpectrumCollection,
                    driftSpot,
                    peakSpots[driftSpot.PeakAreaBeanID],
                    this.analysisParamForLC, this.ProjectProperty, this.peakViewMS2DecResult,
                    this.reversiblaMassSpectraViewEnum, this.mspDB);


                MassSpectrogramViewModel mass2RawSpectrogramViewModel = null;
                if (this.projectProperty.MethodType == MethodType.ddMSMS)
                    mass2RawSpectrogramViewModel = UiAccessLcUtility.GetMs2RawMassspectrogramViewModel(this.lcmsSpectrumCollection, driftSpot, this.analysisParamForLC, this.projectProperty);
                else
                    mass2RawSpectrogramViewModel = UiAccessLcUtility.GetMs2RawMassspectrogramWithAccumulateViewModel(this.lcmsSpectrumCollection, driftSpot, peakSpots[driftSpot.PeakAreaBeanID], this.analysisParamForLC, projectProperty);

                var mass2DeconvolutedSpectrogramViewModel = UiAccessLcUtility.GetMs2DeconvolutedMassspectrogramViewModel(driftSpot, this.peakViewMS2DecResult);

                this.DriftChromatogramXicUI.Content = new ChromatogramXicUI(chromatogramXicVM);
                //this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI(mass1SpectrogramViewModel);
                this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
                this.MS2ChromatogramUI.Content = new ChromatogramMrmUI(chromatogramMrmViewModel);
                this.RawMassSpectrogramUI.Content = new MassSpectrogramUI(mass2RawSpectrogramViewModel);
                this.DeconvolutedMassSpectrogramUI.Content = new MassSpectrogramUI(mass2DeconvolutedSpectrogramViewModel);

                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).RefreshUI();

                //if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel != null) {
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
                //}

                if (((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel != null) {
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= rawMassSpectrogramViewModel_PropertyChanged;
                    ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += rawMassSpectrogramViewModel_PropertyChanged;
                  //  ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged -= deconvolutedMassSpectrogramViewModel_PropertyChanged;
                  //  ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.PropertyChanged += deconvolutedMassSpectrogramViewModel_PropertyChanged;
                }

                //if (this.analysisParamForLC.IsIonMobility) {
                //    refreshDriftSpotViewer(file, this.focusedPeakID);
                //}

                this.mainWindowDisplayVM.MetaDataLabelRefresh();

                Mouse.OverrideCursor = null;
            }
            else if (e.PropertyName == "DisplayRangeMinX" || e.PropertyName == "DisplayRangeMaxX") {
                if (((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean == null ||
                    ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection == null ||
                    ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.ChromatogramBean.ChromatogramDataPointCollection.Count == 0) return;

                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMax = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).ChromatogramXicViewModel.DisplayRangeRtMin = ((PairwisePlotBean)sender).DisplayRangeMinX;
                ((ChromatogramXicUI)this.DriftChromatogramXicUI.Content).RefreshUI();
            }
            else if (e.PropertyName == "DisplayRangeMinY" || e.PropertyName == "DisplayRangeMaxY") {
                //if (((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel == null ||
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean == null ||
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection == null ||
                //    ((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.MeasuredMassSpectrogramBean.MassSpectraCollection.Count == 0) return;
                //((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((PairwisePlotBean)sender).DisplayRangeMaxY;
                //((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((PairwisePlotBean)sender).DisplayRangeMinY;
                //((MassSpectrogramLeftRotateUI)this.Ms1MassSpectrogramUI.Content).RefreshUI();
            }
        }


        private void rawMassSpectrogramViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayRangeMassMin")
            {
                if (this.propertyCheck == true) { this.propertyCheck = false; return; }
                this.propertyCheck = true;

                ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax;
                ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin;
                ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).RefreshUI();
            }
        }

        private void deconvolutedMassSpectrogramViewModel_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (e.PropertyName == "DisplayRangeMassMin")
            {
                if (this.propertyCheck == true) { this.propertyCheck = false; return; }
                this.propertyCheck = true;

                ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax = ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMax;
                ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin = ((MassSpectrogramUI)this.DeconvolutedMassSpectrogramUI.Content).MassSpectrogramViewModel.DisplayRangeMassMin;
                ((MassSpectrogramUI)this.RawMassSpectrogramUI.Content).RefreshUI();
            }
        }
        #endregion

        # region // method for alignment viewer
        public void AlignmentViewerForLcRefresh(int alignmentFileID)
        {
            this.focusedAlignmentFileID = alignmentFileID;
            var alignmentFile = this.alignmentFiles[this.focusedAlignmentFileID];

            if (!File.Exists(alignmentFile.EicFilePath)) {
                MessageBox.Show("The aligned EIC file (" + alignmentFile.FileName + ".EIC.aef) cannot be found. Please execute peak alignment for the use of aligned EIC viewer.",
                    "Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Asterisk);
                this.isAlignedEicFileExist = false;
            }
            else {
                var errorString = string.Empty;
                if (ErrorHandler.IsFileLocked(alignmentFile.EicFilePath, out errorString)) {
                    MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else {
                    this.isAlignedEicFileExist = true;
                    this.alignEicFS = File.Open(alignmentFile.EicFilePath, FileMode.Open, FileAccess.ReadWrite);
                    this.alignEicSeekPoints = AlignedEic.ReadSeekPointsOfAlignedEic(this.alignEicFS);
                }
            }
            this.focusedAlignmentPeakID = 0;
            this.focusedAlignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(alignmentFile.FilePath);

            this.alignViewDecFS = File.Open(alignmentFile.SpectraFilePath, FileMode.Open, FileAccess.ReadWrite);
            this.alignViewDecSeekPoints = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(this.alignViewDecFS);

            SetAlignmentFileForAifViewerController();
            var isColorByCompoundClass = false; if (this.projectProperty.TargetOmics == TargetOmics.Lipidomics) isColorByCompoundClass = true;
            menuItemRefresh();

            var pairwisePlotBean = UiAccessLcUtility.GetRtMzPairwisePlotAlignmentViewBean(alignmentFile, focusedAlignmentResult, isColorByCompoundClass, this.mspDB);
            this.RtMzPairwisePlotAlignmentViewUI.Content = new PairwisePlotAlignmentViewUI(pairwisePlotBean);

            if (this.analysisParamForLC.IsIonMobility) {
                refreshDriftSpotViewer(alignmentFile, focusedAlignmentResult, isColorByCompoundClass, this.mspDB, this.focusedAlignmentPeakID);
            }

            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.PropertyChanged -= alignViewRtMzForLcPairwisePlotBean_PropertyChanged;
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.PropertyChanged += alignViewRtMzForLcPairwisePlotBean_PropertyChanged;
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = this.focusedAlignmentPeakID;

            TableViewerUtility.CalcStatisticsForAlignmentProperty(this.focusedAlignmentResult.AlignmentPropertyBeanCollection, this.analysisFiles, this.analysisParamForLC.IsIonMobility);
        }

        public void SetAlignmentFileForAifViewerController(bool force = false)
        {
            if (force || (ProjectProperty.CheckAIF && this.aifViewerController != null))
            {
                this.aifViewerController.AifViewControlForPeakVM.ReadAlignmentFileFileStream(this.focusedAlignmentResult, this.alignmentFiles[this.focusedAlignmentFileID].FilePath, this.alignViewDecFS, this.alignViewDecSeekPoints);
                this.aifViewerController.AifViewControlForPeakVM.InitializeAlignedData(this.alignEicFS, this.alignEicSeekPoints);
            }
        }

        private void refreshDriftSpotViewer(AlignmentFileBean alignmentFile, AlignmentResultBean alignmentResultBean,
            bool isColorByCompoundClass, List<MspFormatCompoundInformationBean> mspDB, int spotID) {
            var repSpotID = 0;
            var isLipidomics = this.projectProperty.TargetOmics == TargetOmics.Lipidomics ? true : false;
            var driftPairwisePlotBean = UiAccessLcUtility.GetDriftTimeMzPairwisePlotAlignmentViewBean(alignmentFile, alignmentResultBean, spotID,
                this.analysisParamForLC.IonMobilityType, out repSpotID, isLipidomics, this.mspDB);
            if (driftPairwisePlotBean == null) return;
            this.AlignedDriftSpotBeanList = driftPairwisePlotBean.AlignmentDriftSpotBean;

            this.DriftTimeMzPairwiseAlignmentViewUI.Content = new PairwisePlotAlignmentViewUI(driftPairwisePlotBean);
            var driftContent = (PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content;

            var param = this.analysisParamForLC;
            if (param == null) return;
            var alignedSpots = alignmentResultBean.AlignmentPropertyBeanCollection;
            var alignedSpot = alignedSpots[spotID];
            float[] rectangleRange = getRectangleRange(alignedSpot, param.AccumulatedRtRagne);

            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.RectangleRangeXmin = rectangleRange[0];
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.RectangleRangeXmax = rectangleRange[1];
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.RectangleRangeYmin = rectangleRange[2];
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.RectangleRangeYmax = rectangleRange[3];

            driftContent.PairwisePlotBean.PropertyChanged -= driftSpotAlignmentViewerOnDataDependentAcquisition_PropertyChanged;
            driftContent.PairwisePlotBean.PropertyChanged += driftSpotAlignmentViewerOnDataDependentAcquisition_PropertyChanged;

            driftContent.PairwisePlotBean.SelectedPlotId = repSpotID;
            driftContent.RefreshUI();
            this.mainWindowDisplayVM.DriftViewRefresh();
        }

        private void driftSpotAlignmentViewerOnDataDependentAcquisition_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (((PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId") {
                Mouse.OverrideCursor = Cursors.Wait;
                if (!this.isClickOnRtMzViewer) {
                    this.isClickOnDtMzViewer = true;
                }
                else {
                    this.isClickOnDtMzViewer = false;
                }

                this.focusedAlignmentDriftID = ((PairwisePlotBean)sender).SelectedPlotId;
                this.selectedAlignmentViewDriftSpot = this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID];
                this.focusedAlignmentMasterID = this.selectedAlignmentViewDriftSpot.MasterID;

                if (this.alignmentSpotTableViewer != null && alignmentSpotTableViewerFlag2 && isClickOnDtMzViewer) {
                    alignmentSpotTableViewerFlag = false;
                    Debug.WriteLine("Changed to False");

                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.ChangeSelectedData(this.focusedAlignmentMasterID);
                    //Debug.WriteLine("Dt vs Mz ok");
                }
                if (!alignmentSpotTableViewerFlag) alignmentSpotTableViewerFlag = true;
                alignmentSpotTableViewerFlag2 = true;

                if (this.isAlignedEicFileExist) {
                    this.alignEicResultOnDrift = AlignedEic.ReadAlignedEicResult(this.alignEicFS, this.alignEicSeekPoints, this.focusedAlignmentMasterID);
                    drawAlignedEicOnDrift();
                }
                this.alignViewMS2DecResult = SpectralDeconvolution.ReadMS2DecResult(this.alignViewDecFS, this.alignViewDecSeekPoints, this.focusedAlignmentMasterID);

                var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(
                    this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID],
                    this.alignViewMS2DecResult, this.mspDB);
                this.RepAndRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

                Update_BarChartOnDrift();
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
                if (this.isClickOnDtMzViewer) {
                    Update_SampleTableViewerOnDrift();
                    Update_AlignedChromatogramModificationWinOnDrift();
                }
                this.isClickOnDtMzViewer = false;
                Mouse.OverrideCursor = null;
            }
        }

        public void AlignmentViewerForGcRefresh(int alignmentFileID)
        {
            this.focusedAlignmentFileID = alignmentFileID;
            var alignmentFile = this.alignmentFiles[this.focusedAlignmentFileID];

            if (!File.Exists(alignmentFile.EicFilePath)) {
                MessageBox.Show("The aligned EIC file (" + alignmentFile.FileName + ".EIC.aef) cannot be found. Please execute peak alignment for the use of aligned EIC viewer.",
                    "Notice",
                    MessageBoxButton.OK,
                    MessageBoxImage.Asterisk);
                this.isAlignedEicFileExist = false;
            }
            else {
                var errorString = string.Empty;
                if (ErrorHandler.IsFileLocked(alignmentFile.EicFilePath, out errorString)) {
                    MessageBox.Show(errorString, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return;
                }
                else {
                    this.isAlignedEicFileExist = true;
                    this.alignEicFS = File.Open(alignmentFile.EicFilePath, FileMode.Open, FileAccess.ReadWrite);
                    this.alignEicSeekPoints = AlignedEic.ReadSeekPointsOfAlignedEic(this.alignEicFS);
                }
            }

            this.focusedAlignmentResult = MessagePackHandler.LoadFromFile<AlignmentResultBean>(alignmentFile.FilePath);
            //this.focusedAlignmentResult = (AlignmentResultBean)DataStorageGcUtility.LoadFromXmlFile(alignmentFile.FilePath, typeof(AlignmentResultBean));

            this.focusedAlignmentMS1DecResults = DataStorageGcUtility.ReadMS1DecResults(alignmentFile.SpectraFilePath);
            this.focusedAlignmentPeakID = 0;

            menuItemRefresh();

            var pairwisePlotBean = UiAccessGcUtility.GetRtMzPairwisePlotAlignmentViewBean(alignmentFile, focusedAlignmentResult, focusedAlignmentMS1DecResults);
            this.RtMzPairwisePlotAlignmentViewUI.Content = new PairwisePlotAlignmentViewUI(pairwisePlotBean);

            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.PropertyChanged -= alignViewRtMzForGcPairwisePlotBean_PropertyChanged;
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.PropertyChanged += alignViewRtMzForGcPairwisePlotBean_PropertyChanged;
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.SelectedPlotId = this.focusedAlignmentPeakID;

            TableViewerUtility.CalcStatisticsForAlignmentProperty(this.focusedAlignmentResult.AlignmentPropertyBeanCollection, this.analysisFiles, false);

        }

        public void AlignmentSpotTableView_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;
            if (e.PropertyName == "SelectedData")
            {
                if (alignmentSpotTableViewerFlag)
                {
                    alignmentSpotTableViewerFlag2 = false;
                    Debug.WriteLine("SelectedData ok");

                    var isIonMobility = this.projectProperty.Ionization == Ionization.ESI && this.analysisParamForLC.IsIonMobility;
                    if (isIonMobility)
                    {
                        Debug.WriteLine("Is ion mobility selected ok");
                        var selectedData = ((AlignmentSpotTableViewerVM)sender).SelectedData;
                        this.focusedAlignmentMasterID = selectedData.MasterID;

                        var rtmzPairwise = (PairwisePlotBean)((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean;
                        var dtmzPairwise = (PairwisePlotBean)((PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content).PairwisePlotBean;

                        if (selectedData.Mobility <= 0)
                        {
                            this.isClickOnRtMzViewer = true;
                            this.isClickOnDtMzViewer = false;
                            var peakID = selectedData.AlignmentPropertyBean.AlignmentID;
                            this.focusedAlignmentPeakID = peakID;
                            if (rtmzPairwise.SelectedPlotId == this.focusedAlignmentPeakID) {
                                Update_SampleTableViewer();
                                Update_AlignedChromatogramModificationWin();
                            }
                            else {
                                rtmzPairwise.SelectedPlotId = this.focusedAlignmentPeakID;
                            }

                            var alignedSpot = this.FocusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID];
                            var rt = alignedSpot.CentralRetentionTime;
                            var mz = alignedSpot.CentralAccurateMass;

                            updateRtMzAlignmentPairwisePlot(rtmzPairwise, rt, mz);
                        }
                        else
                        {
                            this.isClickOnRtMzViewer = false;
                            this.isClickOnDtMzViewer = true;
                            var driftID = selectedData.AlignedDriftSpotPropertyBean.AlignmentID;
                            var isSpotExist = false;
                            var focusedDriftSpotID = -1;
                            foreach (var drift in this.alignedDriftSpotBeanList)
                            {
                                if (drift.MasterID == this.focusedAlignmentMasterID)
                                {
                                    isSpotExist = true;
                                    focusedDriftSpotID = drift.DisplayedSpotID;
                                    break;
                                }
                            }
                            if (isSpotExist)
                            {
                                this.focusedAlignmentDriftID = focusedDriftSpotID;
                                if (dtmzPairwise.SelectedPlotId == this.focusedAlignmentDriftID) {
                                    Update_SampleTableViewerOnDrift();
                                    Update_AlignedChromatogramModificationWinOnDrift();
                                }
                                else {
                                    dtmzPairwise.SelectedPlotId = this.focusedAlignmentDriftID;
                                }

                                var peak = this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID];
                                var rt = peak.CentralDriftTime;
                                var mz = peak.CentralAccurateMass;

                                updateDtMzAlignmentPairwisePlot(dtmzPairwise, rt, mz);
                            }
                            else
                            { // in case there is no drift spot in the current drifts, new drift spots are generated by selected the parent peakarea spot.
                                var peakID = selectedData.AlignedDriftSpotPropertyBean.AlignmentSpotID;
                                this.focusedAlignmentPeakID = peakID;

                                rtmzPairwise.SelectedPlotId = this.focusedAlignmentPeakID;
                                var alignedSpot = this.FocusedAlignmentResult.AlignmentPropertyBeanCollection[this.FocusedAlignmentPeakID];
                                var rt = alignedSpot.CentralRetentionTime;
                                var mz = alignedSpot.CentralAccurateMass;

                                updateRtMzAlignmentPairwisePlot(rtmzPairwise, rt, mz);
                            }
                        }

                    }
                    else
                    {
                        this.focusedAlignmentPeakID = ((AlignmentSpotTableViewerVM)sender).SelectedData.AlignmentPropertyBean.AlignmentID;
                        var pairWisePlot = (PairwisePlotBean)((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean;
                        pairWisePlot.SelectedPlotId = this.focusedAlignmentPeakID;
                        var alignedSpot = this.FocusedAlignmentResult.AlignmentPropertyBeanCollection[this.FocusedAlignmentPeakID];
                        var rt = alignedSpot.CentralRetentionTime;
                        if (this.ProjectProperty.Ionization == Ionization.EI &&
                            this.analysisParamForGC.AlignmentIndexType == AlignmentIndexType.RI)
                            rt = alignedSpot.CentralRetentionIndex;
                        var mz = this.ProjectProperty.Ionization == Ionization.ESI ?
                            alignedSpot.CentralAccurateMass :
                            alignedSpot.QuantMass;

                        updateRtMzAlignmentPairwisePlot(pairWisePlot, rt, mz);

                        //var rtTol = (pairWisePlot.DisplayRangeMaxX - pairWisePlot.DisplayRangeMinX) / 2;
                        //var mzTol = (pairWisePlot.DisplayRangeMaxY - pairWisePlot.DisplayRangeMinY) / 2;
                        //Debug.WriteLine("r:" + rt + ", mz:" + mz + ", rtol: " + rtTol + ", mtol: " + mzTol);
                        //pairWisePlot.DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)pairWisePlot.MaxX);
                        //pairWisePlot.DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)pairWisePlot.MinX);
                        //pairWisePlot.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)pairWisePlot.MaxY);
                        //pairWisePlot.DisplayRangeMinY = Math.Max((float)(mz -
                        //    mzTol), (float)pairWisePlot.MinY);
                        //((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
                    }

                }
                else {
                    alignmentSpotTableViewerFlag = true;
                    Debug.WriteLine("Changed to True");
                }
            }
            else if (e.PropertyName == "ExportAlignmentSpotsToMsFinder")
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "Please select export folder.";

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath.Length > 220)
                    {
                        MessageBox.Show("The folder is too nested, please chenge folder path (less than 230 length).\nCurrent folder path length: " + fbd.SelectedPath.Length, "Error", MessageBoxButton.OK);
                        return;
                    }
                    var filePath = projectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(this.alignmentFiles[focusedAlignmentFileID].FilePath) + "_CorrelationBasedDecRes_Raw_0.cbd";
                    var target = AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source.Where(x => x.Checked).Select(x => x.AlignmentPropertyBean);
                    if (File.Exists(filePath)) {
                        Action ms2decAct = () => {
                            //Debug.Print("MS2Dec is selected");
                            MsDialToExternalApps.SendToMsFinderProgramSelectedAlignmentSpots(this, fbd.SelectedPath, target);
                        };
                        Action corrDecAct = () => {
                            //Debug.Print("CorrDec is selected");
                            SetAifViewerControllerForCorrDec();
                            aifViewerController.AifViewControlForPeakVM.BulkExportToMsFinder(AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source, fbd.SelectedPath, alignmentSpotTableViewer);
                        };
                        var w = new Rfx.Riken.OsakaUniv.ForAIF.CorrDec.AskDecMethodWindow(ms2decAct, corrDecAct);
                        w.Owner = this.AlignmentSpotTableViewer;
                        w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        w.ShowDialog();
                    } else {
                        MsDialToExternalApps.SendToMsFinderProgramSelectedAlignmentSpots(this, fbd.SelectedPath, target);
                    }
                }
            }
            else if (e.PropertyName == "ExportAlignmentSpotsAsMspFormat")
            {
                System.Windows.Forms.FolderBrowserDialog fbd = new System.Windows.Forms.FolderBrowserDialog();
                fbd.Description = "Please select export folder.";

                if (fbd.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    if (fbd.SelectedPath.Length > 220)
                    {
                        MessageBox.Show("The folder is too nested, please chenge folder path (less than 230 length).\nCurrent folder path length: " + fbd.SelectedPath.Length, "Error", MessageBoxButton.OK);
                        return;
                    }

                    var target = AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source.Where(x => x.Checked).Select(x => x.AlignmentPropertyBean);
                    var corrDecFilePath = projectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(this.alignmentFiles[focusedAlignmentFileID].FilePath) + "_CorrelationBasedDecRes_Raw_0.cbd";
                    if (File.Exists(corrDecFilePath)) {
                        Action ms2decAct = () => {
                            foreach (var spot in target) {
                                var ms2Dec = Msdial.Lcms.Dataprocess.Algorithm.SpectralDeconvolution.ReadMS2DecResult(AlignViewDecFS, AlignViewDecSeekPoints, spot.AlignmentID);
                                var filePath = fbd.SelectedPath + "\\ID" + spot.AlignmentID.ToString("00000") + "_" + Math.Round(spot.CentralRetentionTime, 2).ToString() + "_" + Math.Round(spot.CentralAccurateMass, 2).ToString() + ".msp";
                                if (filePath.Length < 260) {
                                    using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.ASCII)) {
                                        Msdial.Common.Export.ExportMassSpectrum.WriteSpectrumFromAlignment(sw, mainWindow.ProjectProperty, mainWindow.MspDB, ms2Dec, spot, mainWindow.AnalysisFiles, true);
                                    }
                                }
                            }
                        };
                        Action corrDecAct = () => {
                            SetAifViewerControllerForCorrDec();
                            aifViewerController.AifViewControlForPeakVM.MultipleSaveAsMsp(AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source, fbd.SelectedPath, alignmentSpotTableViewer);
                        };
                        var w = new Rfx.Riken.OsakaUniv.ForAIF.CorrDec.AskDecMethodWindow(ms2decAct, corrDecAct);
                        w.Owner = this.AlignmentSpotTableViewer;
                        w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                        w.ShowDialog();
                    } else {
                        foreach (var spot in target) {
                            var ms2Dec = Msdial.Lcms.Dataprocess.Algorithm.SpectralDeconvolution.ReadMS2DecResult(AlignViewDecFS, AlignViewDecSeekPoints, spot.AlignmentID);
                            var filePath = fbd.SelectedPath + "\\ID" + spot.AlignmentID.ToString("00000") + "_" + Math.Round(spot.CentralRetentionTime, 2).ToString() + "_" + Math.Round(spot.CentralAccurateMass, 2).ToString() + ".msp";
                            if (filePath.Length < 260) {
                                using (var sw = new System.IO.StreamWriter(filePath, false, Encoding.ASCII)) {
                                    Msdial.Common.Export.ExportMassSpectrum.WriteSpectrumFromAlignment(sw, mainWindow.ProjectProperty, mainWindow.MspDB, ms2Dec, spot, mainWindow.AnalysisFiles, true);
                                }
                            }
                        }
                    }
                }
                MessageBox.Show("Exported.", "Notice", MessageBoxButton.OK);
            }
            else if (e.PropertyName == "CtrlD")
            {
                CtrlD_KeyUp();
            }

        }

        private void updateDtMzAlignmentPairwisePlot(PairwisePlotBean dtmzPairwise, float dt, float mz) {
            var rtTol = (dtmzPairwise.DisplayRangeMaxX - dtmzPairwise.DisplayRangeMinX) / 2;
            var mzTol = (dtmzPairwise.DisplayRangeMaxY - dtmzPairwise.DisplayRangeMinY) / 2;
            Debug.WriteLine("r:" + dt + ", mz:" + mz + ", rtol: " + rtTol + ", mtol: " + mzTol);
            dtmzPairwise.DisplayRangeMaxX = Math.Min((float)(dt + rtTol), (float)dtmzPairwise.MaxX);
            dtmzPairwise.DisplayRangeMinX = Math.Max((float)(dt - rtTol), (float)dtmzPairwise.MinX);
            dtmzPairwise.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)dtmzPairwise.MaxY);
            dtmzPairwise.DisplayRangeMinY = Math.Max((float)(mz - mzTol), (float)dtmzPairwise.MinY);

            // temp 190809
            //dtmzPairwise.DisplayRangeMaxX = (float)dtmzPairwise.MaxX;
            //dtmzPairwise.DisplayRangeMinX = (float)dtmzPairwise.MinX;
            //dtmzPairwise.DisplayRangeMaxY = (float)dtmzPairwise.MaxY;
            //dtmzPairwise.DisplayRangeMinY = (float)dtmzPairwise.MinY;


            ((PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content).RefreshUI();
        }

        private void updateRtMzAlignmentPairwisePlot(PairwisePlotBean rtmzPairwise, float rt, float mz) {
            var rtTol = (rtmzPairwise.DisplayRangeMaxX - rtmzPairwise.DisplayRangeMinX) / 2;
            var mzTol = (rtmzPairwise.DisplayRangeMaxY - rtmzPairwise.DisplayRangeMinY) / 2;
            Debug.WriteLine("r:" + rt + ", mz:" + mz + ", rtol: " + rtTol + ", mtol: " + mzTol);
            rtmzPairwise.DisplayRangeMaxX = Math.Min((float)(rt + rtTol), (float)rtmzPairwise.MaxX);
            rtmzPairwise.DisplayRangeMinX = Math.Max((float)(rt - rtTol), (float)rtmzPairwise.MinX);
            rtmzPairwise.DisplayRangeMaxY = Math.Min((float)(mz + mzTol), (float)rtmzPairwise.MaxY);
            rtmzPairwise.DisplayRangeMinY = Math.Max((float)(mz -
                mzTol), (float)rtmzPairwise.MinY);
            ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
        }

        private void drawAlignedEic() {

            ChromatogramTicEicViewModel alignedEicVM = null;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                alignedEicVM = UiAccessLcUtility.GetAlignedEicChromatogram(
                    this.alignEicResult,
                    this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID].AlignedPeakPropertyBeanCollection,
                    this.AnalysisFiles, this.projectProperty,
                    this.analysisParamForLC, false);
            }
            else if (this.projectProperty.Ionization == Ionization.EI) {
                alignedEicVM = UiAccessGcUtility.GetAlignedEicChromatogram(
                    this.alignEicResult,
                    this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID].AlignedPeakPropertyBeanCollection,
                    this.AnalysisFiles, this.projectProperty,
                    this.focusedAlignmentResult.AnalysisParamForGC);
            }

            this.AlignedEicUI.Content = new ChromatogramAlignedEicUI(alignedEicVM);
        }

        private void drawAlignedEicOnDrift() {

            ChromatogramTicEicViewModel alignedEicVM = null;
            if (this.selectedAlignmentViewDriftSpot == null) return;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                alignedEicVM = UiAccessLcUtility.GetAlignedEicChromatogram(
                    this.alignEicResultOnDrift,
                    this.selectedAlignmentViewDriftSpot.AlignedPeakPropertyBeanCollection,
                    this.AnalysisFiles, this.projectProperty,
                    this.analysisParamForLC, true);
            }

            this.AlignedEicUIOnDrift.Content = new ChromatogramAlignedEicUI(alignedEicVM);
        }

        private void alignViewRtMzForLcPairwisePlotBean_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null ||
                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId") {
                Mouse.OverrideCursor = Cursors.Wait;
                if (!this.isClickOnDtMzViewer) {
                    this.isClickOnRtMzViewer = true;
                }
                else {
                    this.isClickOnRtMzViewer = false;
                }

                this.focusedAlignmentPeakID = ((PairwisePlotBean)sender).SelectedPlotId;
                var alignmentFile = this.alignmentFiles[this.focusedAlignmentFileID];
                var isColorByCompoundClass = false; if (this.projectProperty.TargetOmics == TargetOmics.Lipidomics) isColorByCompoundClass = true;

                var alignedSpots = ((PairwisePlotBean)sender).AlignmentPropertyBeanCollection;
                var targetAlignmentID
                    = this.projectProperty.Ionization == Ionization.ESI && this.analysisParamForLC.IsIonMobility
                    ? alignedSpots[this.focusedAlignmentPeakID].MasterID
                    : this.focusedAlignmentPeakID;

                if (this.alignmentSpotTableViewer != null && alignmentSpotTableViewerFlag2 && this.isClickOnRtMzViewer) {
                    alignmentSpotTableViewerFlag = false;
                    Debug.WriteLine("Changed to False");
                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.ChangeSelectedData(targetAlignmentID);
                    Debug.WriteLine("Rt vs Mz ok");
                    Debug.WriteLine(alignmentSpotTableViewerFlag.ToString());
                }
                if (!alignmentSpotTableViewerFlag) alignmentSpotTableViewerFlag = true;
                alignmentSpotTableViewerFlag2 = true;

                if (aifViewerController != null) {
                    aifViewerController.AifViewControlForPeakVM.ChangeAlignmentSpotId(targetAlignmentID);
                }

                if (this.isAlignedEicFileExist) {
                    this.alignEicResult = AlignedEic.ReadAlignedEicResult(this.alignEicFS, this.alignEicSeekPoints, targetAlignmentID);
                    drawAlignedEic();
                }
                this.alignViewMS2DecResult = SpectralDeconvolution.ReadMS2DecResult(this.alignViewDecFS, this.alignViewDecSeekPoints, targetAlignmentID);

                var mass2SpectrogramViewModel = UiAccessLcUtility.GetMs2MassspectrogramViewModel(
                    this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID],
                    this.alignViewMS2DecResult, this.mspDB);
                this.RepAndRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

                Update_BarChart();
                this.mainWindowDisplayVM.MetaDataLabelRefresh();

                if (this.isClickOnRtMzViewer) {
                    Update_SampleTableViewer();
                    Update_AlignedChromatogramModificationWin();
                }

                if (this.analysisParamForLC.IsIonMobility) {
                    refreshDriftSpotViewer(alignmentFile, focusedAlignmentResult, isColorByCompoundClass, this.mspDB, this.focusedAlignmentPeakID);
                }
                this.isClickOnRtMzViewer = false;
                Mouse.OverrideCursor = null;
            }
            else if (e.PropertyName == "DisplayRangeMinX" || e.PropertyName == "DisplayRangeMaxX") {
                if (this.isAlignedEicFileExist == false) return;
                if (((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.ChromatogramBeanCollection == null || ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.ChromatogramBeanCollection.Count == 0) return;

                var pairwisePlotDisplayMaxX = ((PairwisePlotBean)sender).DisplayRangeMaxX;
                var pairwisePlotDisplayMinX = ((PairwisePlotBean)sender).DisplayRangeMinX;

                var alignedChromDisplayRtMax = ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMax;
                var alignedChromDisplayRtMin = ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMin;

                var alignedChromRtMax = ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.MaxRt;
                var alignedChromRtMin = ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.MinRt;

                if (pairwisePlotDisplayMaxX > alignedChromRtMax || pairwisePlotDisplayMinX > alignedChromRtMin) {
                    ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMax = alignedChromRtMax;
                    ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMin = alignedChromRtMin;
                }
                else {
                    ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMax = pairwisePlotDisplayMaxX;
                    ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).ChromatogramTicEicViewModel.DisplayRangeRtMin = pairwisePlotDisplayMinX;
                }

                ((ChromatogramAlignedEicUI)this.AlignedEicUI.Content).RefreshUI();
            }
        }

        private void alignViewRtMzForGcPairwisePlotBean_PropertyChanged(object sender, PropertyChangedEventArgs e)
        {
            if (((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection == null || ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

            if (e.PropertyName == "SelectedPlotId")
            {
                Mouse.OverrideCursor = Cursors.Wait;

                this.focusedAlignmentPeakID = ((PairwisePlotBean)sender).SelectedPlotId;
                this.focusedAlignmentMs1DecID = this.focusedAlignmentPeakID;

                if (this.alignmentSpotTableViewer != null && alignmentSpotTableViewerFlag2) {
                    alignmentSpotTableViewerFlag = false;
                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.ChangeSelectedData(this.focusedAlignmentPeakID);
                }
                alignmentSpotTableViewerFlag2 = true;

                if (this.quantmassBrowser != null && quantmassBrowserFlag2) {
                    quantmassBrowserFlag = false;
                    this.quantmassBrowser.QuantmassBrowserVM.ChangeSelectedData(this.focusedAlignmentPeakID);
                }
                quantmassBrowserFlag2 = true;

                if (this.isAlignedEicFileExist) {
                    this.alignEicResult = AlignedEic.ReadAlignedEicResult(this.alignEicFS, this.alignEicSeekPoints, this.focusedAlignmentPeakID);
                    var alignedEicVM = UiAccessGcUtility.GetAlignedEicChromatogram(this.alignEicResult,
                        this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID].AlignedPeakPropertyBeanCollection,
                        this.AnalysisFiles, this.projectProperty, this.FocusedAlignmentResult.AnalysisParamForGC);
                    this.AlignedEicUI.Content = new ChromatogramAlignedEicUI(alignedEicVM);
                }
                var mass2SpectrogramViewModel = UiAccessGcUtility.GetReversibleSpectrumVM(this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID],
                    this.analysisParamForGC, this.reversiblaMassSpectraViewEnum, this.mspDB);
                this.RepAndRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

                Update_BarChart();

                //var barChartBean = MsDialStatistics.GetBarChartBean(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID],
                //    this.analysisFiles, this.solidColorBrushList, this.BarChartDisplayMode);
                //this.BarChartUI.Content = new BarChartUI(barChartBean);

                this.mainWindowDisplayVM.MetaDataLabelRefresh();
                Update_SampleTableViewer();
                Update_AlignedChromatogramModificationWin();

                Mouse.OverrideCursor = null;
            }
        }

        #endregion

        #region manual chromatogram modification

        #region AlignmentSampleTable
        public void Show_AlignmentSampleTable() {
            if (sampleTableViewer != null) { MessageBox.Show("The window already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            ObservableCollection<SampleTableRow> source = null;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID],
                            this.alignEicResult, analysisFiles, projectProperty, AnalysisParamForLC, solidColorBrushList);
            }
            else if (this.projectProperty.Ionization == Ionization.EI) {
                source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID],
                            this.alignEicResult, analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForGC, solidColorBrushList);
            }
            else {
                return;
            }

            this.sampleTableViewer = new SampleTableViewerInAlignment(source, AnalysisParamForGC);
            sampleTableViewer.Owner = this;
            sampleTableViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            sampleTableViewer.SampleTableViewerInAlignmentVM.PropertyChanged += SampleTableViewerInAlignmentVM_PropertyChanged;
            sampleTableViewer.Closed += SampleTableViewer_Closed;
            sampleTableViewer.Show();
        }

        private void SampleTableViewerInAlignmentVM_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "IsPropertyChanged") {
                //Update_SampleTableViewer();
                Update_BarChart();
                Update_AlignmentSpotTableViewer();

                this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID].IsManuallyModified = true;

                if (this.analysisFiles.Count > 100) return;
                Update_AlignedEicUI((SampleTableViewerInAlignmentVM)sender);
            }
        }


        public void SampleTableViewer_Closed(object sender, EventArgs e) {
            this.sampleTableViewer = null;
        }

        public void Update_SampleTableViewer() {
            if (sampleTableViewer == null) return;
            ObservableCollection<SampleTableRow> source = null;
            if (this.projectProperty.Ionization == Ionization.ESI) {

                source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID],
                            this.alignEicResult, analysisFiles, projectProperty, AnalysisParamForLC, solidColorBrushList);
            }
            else if (this.projectProperty.Ionization == Ionization.EI) {
                source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID],
                            this.alignEicResult, analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForGC, solidColorBrushList);
            }
            else {
                return;
            }

            sampleTableViewer.ChangeSource(source);
            sampleTableViewer.SampleTableViewerInAlignmentVM.PropertyChanged += SampleTableViewerInAlignmentVM_PropertyChanged;
        }

        #region sample table viewer on ion mobility
        public void Show_AlignmentSampleTableOnIonMobility() {
            if (sampleTableViewer != null) { MessageBox.Show("The window already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            ObservableCollection<SampleTableRow> source = null;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID],
                            this.alignEicResultOnDrift, analysisFiles, projectProperty, AnalysisParamForLC, solidColorBrushList);
            }
            else {
                return;
            }

            this.sampleTableViewer = new SampleTableViewerInAlignment(source, AnalysisParamForGC);
            sampleTableViewer.Owner = this;
            sampleTableViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
            sampleTableViewer.SampleTableViewerInAlignmentVM.PropertyChanged += SampleTableViewerInAlignmentVMOnDrift_PropertyChanged;
            sampleTableViewer.Closed += SampleTableViewer_Closed;
            sampleTableViewer.Show();
        }

        private void SampleTableViewerInAlignmentVMOnDrift_PropertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "IsPropertyChanged") {
                //Update_SampleTableViewer();
                Update_BarChartOnDrift();
                Update_AlignmentSpotTableViewer();

                this.focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID].IsManuallyModified = true;

                if (this.analysisFiles.Count > 100) return;
                Update_AlignedEicUIOnDrift((SampleTableViewerInAlignmentVM)sender);
            }
        }

        public void Update_SampleTableViewerOnDrift() {
            if (sampleTableViewer == null) return;
            ObservableCollection<SampleTableRow> source = null;
            source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID],
                            this.alignEicResultOnDrift, analysisFiles, projectProperty, AnalysisParamForLC, solidColorBrushList);

            sampleTableViewer.ChangeSource(source);
            sampleTableViewer.SampleTableViewerInAlignmentVM.PropertyChanged += SampleTableViewerInAlignmentVM_PropertyChanged;
        }
        #endregion


        #endregion

        #region AlignedChromatogramModification
        public void Show_AlignedChromatogramModificationWin(bool isOnDrift = false) {
            if (alignedChromatogramModificationWin != null) { MessageBox.Show("The window already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }

            if (this.projectProperty.Ionization == Ionization.ESI) {

                //var isOnDrift = this.alignedDriftSpotBeanList != null && this.alignedDriftSpotBeanList.Count > 0 && this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID].MasterID == this.focusedAlignmentMasterID;
                if (isOnDrift) {
                    alignedChromatogramModificationWin = new ManualPeakMod.AlignedChromatogramModificationWin(alignEicResultOnDrift,
                       this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID], analysisFiles,
                       projectProperty, FocusedAlignmentResult.AnalysisParamForLC, solidColorBrushList);
                    alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWinOnDrift_propertyChanged;
                }
                else {
                    alignedChromatogramModificationWin = new ManualPeakMod.AlignedChromatogramModificationWin(alignEicResult,
                       focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID], analysisFiles,
                       projectProperty, FocusedAlignmentResult.AnalysisParamForLC);
                    alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWin_propertyChanged;
                }
            }
            else {
                alignedChromatogramModificationWin = new ManualPeakMod.AlignedChromatogramModificationWin(alignEicResult,
                    focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID], analysisFiles,
                    projectProperty, FocusedAlignmentResult.AnalysisParamForGC);
                alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWin_propertyChanged;
            }

            //alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWin_propertyChanged;
            alignedChromatogramModificationWin.Owner = this;
            alignedChromatogramModificationWin.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            alignedChromatogramModificationWin.Show();
            alignedChromatogramModificationWin.Closed += AlignedChromatogramModificationWin_Closed;
        }

        public void AlignedChromatogramModificationWin_Closed(object sender, EventArgs e) {
            this.alignedChromatogramModificationWin = null;
        }

        public void AlignedChromatogramModificationWin_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "UpdateTrigger") {

                //var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
                //var uiName = ((ChromatogramAlignedEicUI)target).Name;
                drawAlignedEic();
                Update_SampleTableViewer();
                Update_BarChart();
                Update_AlignmentSpotTableViewer();
                //var isOnDrift = this.alignedDriftSpotBeanList != null && this.alignedDriftSpotBeanList.Count > 0 && this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID].MasterID == this.focusedAlignmentMasterID;
                //if (isOnDrift) {
                //    drawAlignedEicOnDrift();
                //    Update_SampleTableViewerOnDrift();
                //    Update_BarChartOnDrift();
                //    Update_AlignmentSpotTableViewer();
                //}
                //else {
                //    drawAlignedEic();
                //    Update_SampleTableViewer();
                //    Update_BarChart();
                //    Update_AlignmentSpotTableViewer();
                //}
            }
        }

        public void AlignedChromatogramModificationWinOnDrift_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "UpdateTrigger") {

                //var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
                //var uiName = ((ChromatogramAlignedEicUI)target).Name;
                drawAlignedEicOnDrift();
                Update_SampleTableViewerOnDrift();
                Update_BarChartOnDrift();
                Update_AlignmentSpotTableViewer();
                //var isOnDrift = this.alignedDriftSpotBeanList != null && this.alignedDriftSpotBeanList.Count > 0 && this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID].MasterID == this.focusedAlignmentMasterID;
                //if (isOnDrift) {
                //    drawAlignedEicOnDrift();
                //    Update_SampleTableViewerOnDrift();
                //    Update_BarChartOnDrift();
                //    Update_AlignmentSpotTableViewer();
                //}
                //else {
                //    drawAlignedEic();
                //    Update_SampleTableViewer();
                //    Update_BarChart();
                //    Update_AlignmentSpotTableViewer();
                //}
            }
        }

        public void Update_AlignedChromatogramModificationWin() {
            if (alignedChromatogramModificationWin == null) return;

            //var isOnDrift = this.alignedDriftSpotBeanList != null && this.alignedDriftSpotBeanList.Count > 0 &&
            //    this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID].MasterID == this.focusedAlignmentMasterID;
            //if (isOnDrift) {
            //    alignedChromatogramModificationWin.ChangeVM(alignEicResultOnDrift, this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID], analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForLC, solidColorBrushList);
            //}
            //else {
            //    alignedChromatogramModificationWin.ChangeVM(alignEicResult, focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID], analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForLC, solidColorBrushList);
            //}
            alignedChromatogramModificationWin.ChangeVM(alignEicResult, focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID],
                analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForLC);
            alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWin_propertyChanged;
        }

        public void Update_AlignedChromatogramModificationWinOnDrift() {
            if (alignedChromatogramModificationWin == null) return;

            //var isOnDrift = this.alignedDriftSpotBeanList != null && this.alignedDriftSpotBeanList.Count > 0 &&
            //    this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID].MasterID == this.focusedAlignmentMasterID;
            //if (isOnDrift) {
            //}
            //else {
            //    alignedChromatogramModificationWin.ChangeVM(alignEicResult, focusedAlignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentPeakID], analysisFiles, projectProperty, FocusedAlignmentResult.AnalysisParamForLC, solidColorBrushList);
            //}
            alignedChromatogramModificationWin.ChangeVM(alignEicResultOnDrift, this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID], analysisFiles,
                projectProperty, FocusedAlignmentResult.AnalysisParamForLC);
            alignedChromatogramModificationWin.VM.PropertyChanged += AlignedChromatogramModificationWinOnDrift_propertyChanged;
        }
        #endregion

        #endregion

        #region update method by manual modification
        private void Update_AlignedEicUI(SampleTableViewerInAlignmentVM vm) {
            var prop = vm.SelectedData.AlignedPeakPropertyBeanCollection;
            var fileid = prop.FileID;
            var chromUI = (ChromatogramAlignedEicUI)this.AlignedEicUI.Content;
            var chrom = chromUI.ChromatogramTicEicViewModel.ChromatogramBeanCollection[fileid];

            chrom.RtPeakLeft = prop.RetentionTimeLeft;
            chrom.RtPeakRight = prop.RetentionTimeRight;
            chrom.RtPeakTop = prop.RetentionTime;

            chromUI.RefreshUI();
        }

        private void Update_AlignedEicUIOnDrift(SampleTableViewerInAlignmentVM vm) {
            var prop = vm.SelectedData.AlignedPeakPropertyBeanCollection;
            var fileid = prop.FileID;
            var chromUI = (ChromatogramAlignedEicUI)this.AlignedEicUIOnDrift.Content;
            var chrom = chromUI.ChromatogramTicEicViewModel.ChromatogramBeanCollection[fileid];

            chrom.RtPeakLeft = prop.DriftTimeLeft;
            chrom.RtPeakRight = prop.DriftTimeRight;
            chrom.RtPeakTop = prop.DriftTime;

            chromUI.RefreshUI();
        }

        public void Update_BarChart() {

            if (this.focusedAlignmentResult == null ||
                this.focusedAlignmentResult.AlignmentPropertyBeanCollection == null ||
                this.focusedAlignmentResult.AlignmentPropertyBeanCollection.Count == 0) return;
            var barChartBean = MsDialStatistics.GetBarChartBean(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID],
                  this.analysisFiles, this.projectProperty, this.BarChartDisplayMode, this.projectProperty.IsBoxPlotForAlignmentResult);

            this.BarChartUI.Content = new BarChartUI(barChartBean);
            var titleString = "Bar chart of aligned spot";
            var chartString = " (OH)";
            if (this.BarChartDisplayMode == BarChartDisplayMode.OriginalArea)
                chartString = " (OA)";
            else if (this.BarChartDisplayMode == BarChartDisplayMode.NormalizedHeight)
                chartString = " (NH)";
            else if (this.BarChartDisplayMode == BarChartDisplayMode.NormalizedArea)
                chartString = " (NA)";

            titleString += chartString;
            this.TabItem_BarChartViewer.Header = titleString;
        }

        public void Update_BarChartOnDrift() {

            if (this.focusedAlignmentResult == null ||
                this.focusedAlignmentResult.AlignmentPropertyBeanCollection == null ||
                this.focusedAlignmentResult.AlignmentPropertyBeanCollection.Count == 0) return;
            if (this.focusedAlignmentDriftID < 0) return;
            var barChartBean = MsDialStatistics.GetBarChartBean(this.AlignedDriftSpotBeanList[this.focusedAlignmentDriftID],
                  this.analysisFiles, this.projectProperty, this.BarChartDisplayMode, this.projectProperty.IsBoxPlotForAlignmentResult); ;

            this.BarChartUIOnDrift.Content = new BarChartUI(barChartBean);
            var titleString = "Bar chart of aligned spot";
            var chartString = " (OH)";
            if (this.BarChartDisplayMode == BarChartDisplayMode.OriginalArea)
                chartString = " (OA)";
            else if (this.BarChartDisplayMode == BarChartDisplayMode.NormalizedHeight)
                chartString = " (NH)";
            else if (this.BarChartDisplayMode == BarChartDisplayMode.NormalizedArea)
                chartString = " (NA)";

            titleString += chartString;
            this.TabItem_BarChartViewer.Header = titleString;
        }

        private void Update_AlignmentSpotTableViewer() {
            if (alignmentSpotTableViewer == null) return;
            alignmentSpotTableViewer.AlignmentSpotTableViewerVM.UpdateSelectedData(analysisFiles,
                this.projectProperty, this.BarChartDisplayMode, this.projectProperty.IsBoxPlotForAlignmentResult);
        }

        #endregion

        #region // method for spectral DB search, MS-FINDER search, BinVestigate search, Lipoquality search, Molecular-spectrum networking
        private void buttonClick_CompoundSearchPeakViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            if (this.mspDB == null || this.mspDB.Count == 0)
            {
                MessageBox.Show("Library information should be imported.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }


            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                MsmsSearchWin window = null;
                if (this.analysisParamForLC.IsIonMobility) {

                    var displayedDriftSpots = ((PairwisePlotPeakViewUI)this.DriftTimeMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.DriftSpots;
                    var driftSpot = displayedDriftSpots[this.FocusedDriftSpotID];

                    window = new MsmsSearchWin(this.analysisFiles[this.focusedFileID], this.FocusedDriftSpotID,
                        displayedDriftSpots, driftSpot,
                        this.analysisParamForLC, this.peakViewMS2DecResult, this.mspDB, this.projectProperty.TargetOmics);
                }
                else {
                    window = new MsmsSearchWin(this.analysisFiles[this.focusedFileID], this.focusedPeakID, this.analysisParamForLC,
                            this.peakViewMS2DecResult, this.mspDB, this.projectProperty.TargetOmics);
                }


                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
            }
            else
            {
                var window = new EimsSearchWin(this.analysisFiles[this.focusedFileID], this.focusedMS1DecID, this.analysisParamForGC, this.ms1DecResults[this.focusedMS1DecID], this.mspDB);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
            }
        }

        private void buttonClick_CompoundSearchAlignmentViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;
            if (this.focusedAlignmentResult == null) return;


            if (this.mspDB == null || this.mspDB.Count == 0)
            {
                MessageBox.Show("Library information should be imported.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            if (this.projectProperty.Ionization == Ionization.ESI)
            {
                if (this.AnalysisParamForLC.IsIonMobility) {
                    var displayedDriftSpots = ((PairwisePlotAlignmentViewUI)this.DriftTimeMzPairwiseAlignmentViewUI.Content).PairwisePlotBean.AlignmentDriftSpotBean;
                    var window = new MsmsSearchWin(this.focusedAlignmentResult, this.focusedAlignmentDriftID, displayedDriftSpots, this.analysisParamForLC, this.alignViewMS2DecResult, this.mspDB, this.projectProperty.TargetOmics);
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();

                    ionmobilityTableViewerUpdate();
                    this.mainWindowDisplayVM.MetaDataLabelRefresh();
                }
                else {
                    var window = new MsmsSearchWin(this.focusedAlignmentResult, this.focusedAlignmentPeakID, this.analysisParamForLC, this.alignViewMS2DecResult, this.mspDB, this.projectProperty.TargetOmics);
                    window.Owner = this;
                    window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                    window.ShowDialog();
                    this.mainWindowDisplayVM.MetaDataLabelRefresh();
                }
            }
            else
            {
                var window = new EimsSearchWin(this.focusedAlignmentResult, this.focusedAlignmentPeakID, this.analysisParamForGC, this.focusedAlignmentMS1DecResults[this.focusedAlignmentPeakID], this.mspDB);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
            }
        }

        private void ionmobilityTableViewerUpdate() {
            var targetedDriftID = this.focusedAlignmentDriftID;
            var targetedDriftSpot = this.alignedDriftSpotBeanList[targetedDriftID];

            var driftMaster = targetedDriftSpot.MasterID;
            var peakSpotID = targetedDriftSpot.AlignmentSpotID;
            var peakSpot = this.focusedAlignmentResult.AlignmentPropertyBeanCollection[peakSpotID];
            var peakSpotMaster = peakSpot.MasterID;

            if (this.alignmentSpotTableViewer != null) {
                this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[driftMaster].MetaboliteName = targetedDriftSpot.MetaboliteName;
                this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[driftMaster].UpdateMetaboliteNameOnTable(true);

                this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[peakSpotMaster].MetaboliteName = peakSpot.MetaboliteName;
                this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[peakSpotMaster].UpdateMetaboliteNameOnTable(false);

                this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.SourceView.Refresh();
            }
        }

        private void buttonClick_MsFinderSearchAlignmentViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI)
                MsDialToExternalApps.SendToMsFinderProgram(this);
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID]);

        }

        private void buttonClick_BinVestigateSearchAlignmentViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedAlignmentFileID < 0) return;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                MessageBox.Show("BinVestigate is available in GC-MS project", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new BinVestigateSpectrumSearchWin();
            window.Owner = this;

            this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID].MetaboliteName
                = this.mainWindowDisplayVM.GetCompoundName(this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID].MspDbID);
            var mpvm = new BinVestigateSpectrumSearchVM(this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID]);
            window.DataContext = mpvm;

            window.Show();
        }

        private void buttonClick_MsFinderSearchPeakViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;

            if (this.projectProperty.Ionization == Ionization.ESI) {
                if (this.ToggleButton_MeasurmentReferenceSpectraDeconvoluted.IsChecked == true)
                    MsDialToExternalApps.SendToMsFinderProgram(this, ExportspectraType.deconvoluted);
                else
                    MsDialToExternalApps.SendToMsFinderProgram(this, ExportspectraType.centroid);
            }
            else
                MsDialToExternalApps.SendToMsFinderProgram(this, this.ms1DecResults[this.focusedMS1DecID]);
        }

        private void buttonClick_BinVestigateSearchPeakViewer(object sender, RoutedEventArgs e)
        {
            if (this.focusedFileID < 0) return;
            if (this.projectProperty.Ionization == Ionization.ESI) {
                MessageBox.Show("BinVestigate is available in GC-MS project", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var window = new BinVestigateSpectrumSearchWin();
            window.Owner = this;

            this.ms1DecResults[this.focusedMS1DecID].MetaboliteName = this.mainWindowDisplayVM.GetCompoundName(this.ms1DecResults[this.focusedMS1DecID].MspDbID);
            var mpvm = new BinVestigateSpectrumSearchVM(this.ms1DecResults[this.focusedMS1DecID]);
            window.DataContext = mpvm;

            window.Show();
        }

        private void buttonClick_LipoqualityDbSearchPeakViewer(object sender, RoutedEventArgs e) {
            if (this.focusedFileID < 0) return;
            if (this.projectProperty.Ionization != Ionization.ESI || this.projectProperty.TargetOmics != TargetOmics.Lipidomics) {
                MessageBox.Show("Lipoquality search is available in LC-MS lipidomics project", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (this.mspDB == null || this.mspDB.Count == 0) {
                MessageBox.Show("No library query", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var spot = this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID];
            if (lipoqualityFileProperties == null || lipoqualityFileProperties.Count == 0) {
                var files = new List<AnalysisFileBean>();
                var properties = new List<AlignmentPropertyBean>();
                var queries = new List<RawData>();

                Mouse.OverrideCursor = Cursors.Wait;
                string mainDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                if (this.projectProperty.IonMode == IonMode.Positive) {
                    if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldp + "*",
                    System.IO.SearchOption.TopDirectoryOnly).Length >= 1) {
                        var lqfile = System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldp + "*", System.IO.SearchOption.TopDirectoryOnly)[0];
                        DatabaseLcUtility.GetLipoqlualityDatabase(lqfile, out files, out properties, out queries);
                    }
                }
                else {
                    if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldn + "*",
                   System.IO.SearchOption.TopDirectoryOnly).Length >= 1) {
                        var lqfile = System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldn + "*", System.IO.SearchOption.TopDirectoryOnly)[0];
                        DatabaseLcUtility.GetLipoqlualityDatabase(lqfile, out files, out properties, out queries);
                    }
                }
                if (files.Count > 0) {
                    this.lipoqualityFileProperties = files;
                    this.lipoqualitySpectrumQueries = queries;
                    this.lipoqualitySpotProperties = properties;
                }
                Mouse.OverrideCursor = null;
            }

            if (this.lipoqualitySpotProperties == null || this.lipoqualitySpotProperties.Count == 0) {
                MessageBox.Show("No library query", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //var lqDbLink = "No link";
            //if (spot.LibraryID > 0 && this.mspDB.Count - 1 > spot.LibraryID) {
            //    lqDbLink = DatabaseLcUtility.GetLipoqualityDatabaseURL(this.mspDB[spot.LibraryID]);
            //}

            var targetSpot = this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID];

            var window = new LipoqualitySpectrumSearchWin();
            window.Owner = this;

            var mpvm = new LipoqualitySpectrumSearchVM(targetSpot, this.peakViewMS2DecResult, this.lipoqualitySpotProperties, this.lipoqualitySpectrumQueries, this.lipoqualityFileProperties);
            window.DataContext = mpvm;

            window.Show();

            //if (spot.LibraryID < 0) {
            //    MessageBox.Show("The selected peak is not identified", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}

            //if (spot.LibraryID > 0 && this.mspDB.Count - 1 > spot.LibraryID) {
            //    LipoqualityRest.GoToLQDB(mspDB[spot.LibraryID], spot.MetaboliteName);
            //}
        }

        private void buttonClick_LipoqualityDbSearchAlignmentViewer(object sender, RoutedEventArgs e) {
            if (this.focusedAlignmentFileID < 0) return;
            if (this.projectProperty.Ionization != Ionization.ESI || this.projectProperty.TargetOmics != TargetOmics.Lipidomics) {
                MessageBox.Show("Lipoquality search is available in LC-MS lipidomics project", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (this.mspDB == null || this.mspDB.Count == 0) {
                MessageBox.Show("No library query", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var spot = this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID];
            if (lipoqualityFileProperties == null || lipoqualityFileProperties.Count == 0) {
                var files = new List<AnalysisFileBean>();
                var properties = new List<AlignmentPropertyBean>();
                var queries = new List<RawData>();
                Mouse.OverrideCursor = Cursors.Wait;
                string mainDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                if (this.projectProperty.IonMode == IonMode.Positive) {
                    if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldp + "*",
                    System.IO.SearchOption.TopDirectoryOnly).Length >= 1) {
                        var lqfile = System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldp + "*", System.IO.SearchOption.TopDirectoryOnly)[0];
                        DatabaseLcUtility.GetLipoqlualityDatabase(lqfile, out files, out properties, out queries);
                    }
                }
                else {
                    if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldn + "*",
                   System.IO.SearchOption.TopDirectoryOnly).Length >= 1) {
                        var lqfile = System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.ldn + "*", System.IO.SearchOption.TopDirectoryOnly)[0];
                        DatabaseLcUtility.GetLipoqlualityDatabase(lqfile, out files, out properties, out queries);
                    }
                }
                if (files.Count > 0) {
                    this.lipoqualityFileProperties = files;
                    this.lipoqualitySpectrumQueries = queries;
                    this.lipoqualitySpotProperties = properties;
                }
                Mouse.OverrideCursor = null;
            }

            var targetSpot = this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID];

            if (this.lipoqualitySpotProperties == null || this.lipoqualitySpotProperties.Count == 0) {
                MessageBox.Show("No library query", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            //var lqDbLink = "No link";
            //if (spot.LibraryID > 0 && this.mspDB.Count - 1 > spot.LibraryID) {
            //    lqDbLink = DatabaseLcUtility.GetLipoqualityDatabaseURL(this.mspDB[spot.LibraryID]);
            //}

            var window = new LipoqualitySpectrumSearchWin();
            window.Owner = this;

            var mpvm = new LipoqualitySpectrumSearchVM(targetSpot, this.alignViewMS2DecResult, this.lipoqualitySpotProperties, this.lipoqualitySpectrumQueries, this.lipoqualityFileProperties);
            window.DataContext = mpvm;

            window.Show();

            //if (spot.LibraryID < 0) {
            //    MessageBox.Show("The selected peak is not identified", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}

            //if (spot.LibraryID > 0 && this.mspDB.Count - 1 > spot.LibraryID) {
            //    LipoqualityRest.GoToLQDB(mspDB[spot.LibraryID], spot.MetaboliteName);
            //}
        }

        private void buttonClick_MolecularNetworkPeakViewer(object sender, RoutedEventArgs e) {
            if (this.FocusedFileID < 0) {
                return;
            }

            var window = new MolecularNetworkSettingWin(true, false);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }


        private void buttonClick_MolecularNetworkAlignmentViewer(object sender, RoutedEventArgs e) {
            if (this.FocusedFileID < 0) {
                return;
            }

            var window = new MolecularNetworkSettingWin(true, true);
            window.Owner = this;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.ShowDialog();
        }

        /*        private void refresh_PeakTableViewer() {
                    if (this.PeakSpotTableViewer != null) {
                    }
                }
                private void refresh_AlignmentTableViewer() {
                    if (this.AlignmentSpotTableViewer != null) {
                        this.AlignmentSpotTableViewer.UpdateLayout();
                    }
                }
                */
        #endregion

        #region // method for AIF
        private void aifViewerController_propertyChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == "LibSearchPeak") {
                this.MainWindowDisplayVM.MetaDataLabelRefresh();
            }
            if (e.PropertyName == "LibSearchAlignment") {
                this.MainWindowDisplayVM.MetaDataLabelRefresh();
            }
            if (e.PropertyName == "PeakExportToMsFinder") {
                if (this.PeakSpotTableViewer == null) {
                    var w = new ShortMessageWindow("Please open peak spot table viewer (click \"Show ion table\")");
                    w.Owner = this;
                    w.Width = 500;
                    w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    w.Show();
                } else {
                    PeakSpotTableViewer.PeakSpotTableViewerVM.ChangeToMsFinderExporterView();
                    //new ForAIF.MsFinderMultipleExporter(this.PeakSpotTableViewer, this.aifViewerController);
                }
            }
            if (e.PropertyName == "AlignmentExportToMsFinder") {
                if (this.AlignmentSpotTableViewer == null) {
                    var w = new ShortMessageWindow("Please open alignment spot table viewer (click \"Show ion table\")");
                    w.Owner = this;
                    w.Width = 500;
                    w.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    w.Show();
                } else {
                    this.AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.ChangeToMsFinderExporterView();
                //   new ForAIF.MsFinderMultipleExporter(this.AlignmentSpotTableViewer, this.aifViewerController);
                }
            }
        }
        #endregion

        #region // commonly used method
        private float[] getRectangleRange(PeakAreaBean peakAreaBean, Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean)
        {
            float accurateMass = peakAreaBean.AccurateMass;
            float precursorStartMz = 0, precursorEndMz = 0;
            if (projectProperty.CheckAIF) {
                var target = experimentID_AnalystExperimentInformationBean[projectProperty.Ms2LevelIdList[0]];
                precursorStartMz = target.StartMz;
                precursorEndMz = target.EndMz;
            }
            foreach (var value in experimentID_AnalystExperimentInformationBean) { if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < accurateMass && accurateMass <= value.Value.EndMz) { precursorStartMz = value.Value.StartMz; precursorEndMz = value.Value.EndMz; break; } }

            //[0]xmin[1]xmax[2]ymin[3]ymax
            return new float[] { peakAreaBean.RtAtPeakTop - (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F, peakAreaBean.RtAtPeakTop + (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 1.5F, precursorStartMz, precursorEndMz };
        }

        private float[] getRectangleRange(PeakAreaBean peakAreaBean, float rtWidth) {
            float rt = peakAreaBean.RtAtPeakTop;
            //float rtWidthHarf = (peakAreaBean.RtAtRightPeakEdge - peakAreaBean.RtAtLeftPeakEdge) * 0.5F;
            float accurateMass = peakAreaBean.AccurateMass;

            //[0]xmin[1]xmax[2]ymin[3]ymax
            return new float[] {
                rt - rtWidth * 0.5F, rt + rtWidth * 0.5F, 0, 2000 };
        }

        private float[] getRectangleRange(AlignmentPropertyBean alignedSpot, float rtWidth) {
            float rt = alignedSpot.CentralRetentionTime;
            //float rtWidthHarf = alignedSpot.AveragePeakWidth * 0.5F;
            float accurateMass = alignedSpot.CentralAccurateMass;

            //[0]xmin[1]xmax[2]ymin[3]ymax
            return new float[] {
                rt - rtWidth * 0.5F, rt + rtWidth * 0.5F, 0, 2000 };
        }

        private void listBox_AlignedFiles_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.windowOpened == true)
            {
                MessageBox.Show("Close the property setting window.", "Message", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            if (this.focusedAlignmentFileID >= 0)
                if (MessageBox.Show("Continue without saving processed files?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) return;

           // if (this.pcaResultWin != null) { this.pcaResultWin.Close(); this.pcaBean = null; }

            AlignmentViewDataAccessRefresh();

            int alignmentFileID = ((ListBox)sender).SelectedIndex;
            if (alignmentFileID < 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            if (this.projectProperty.Ionization == Ionization.ESI)
                AlignmentViewerForLcRefresh(alignmentFileID);
            else
                AlignmentViewerForGcRefresh(alignmentFileID);

            this.TabItem_RtMzPairwisePlotAlignmentView.IsSelected = true;

            Mouse.OverrideCursor = null;
        }

        private void listBox_FileName_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            if (this.focusedFileID < 0 || e.OriginalSource.GetType().Name != "TextBlock") return;
            if (this.focusedFileID >= 0)
                if (MessageBox.Show("Continue without saving processed files?", "Question", MessageBoxButton.OKCancel, MessageBoxImage.Question) == MessageBoxResult.Cancel) return;

            PeakViewDataAccessRefresh(false);

            int fileID = ((ListBox)sender).SelectedIndex;
            if (fileID < 0) return;

            Mouse.OverrideCursor = Cursors.Wait;

            if (this.projectProperty.Ionization == Ionization.ESI)
                PeakViewerForLcRefresh(fileID);
            else
                PeakViewerForGcRefresh(fileID);

            this.TabItem_RtMzPairwisePlotPeakView.IsSelected = true;

            Mouse.OverrideCursor = null;
		}

        public void PeakViewDataAccessRefresh(bool refreshWithViewer = true)
        {
            if (this.rawDataAccess != null)
            {
                this.rawDataAccess.Dispose();
            }

            if (this.peakViewDecFS != null)
            {
                this.peakViewDecFS.Dispose();
                this.peakViewDecFS.Close();
            }


            if (this.peakViewDecSeekPoints != null)
            {
                this.peakViewDecSeekPoints = new List<long>();
            }


            if (this.peakViewMS2DecResult != null)
            {
                this.peakViewMS2DecResult = null;
            }

            if (this.PeakSpotTableViewer != null) {
                if (!refreshWithViewer) {
                    this.PeakSpotTableViewer.IsEnabled = false;
                }
                else {
                    this.PeakSpotTableViewer.Close();
                    this.PeakSpotTableViewer = null;
                    this.peakSpotTableViewerFlag = true;
                    this.peakSpotTableViewerFlag2 = true;
                }
            }

            if (this.aifViewerController != null) {
                if (refreshWithViewer) {
                    this.aifViewerController.Close();
                    this.aifViewerController = null;
                }
            }

            peakViewerGuiRefresh();
        }

        public void AlignmentViewDataAccessRefresh()
        {
            if (this.focusedAlignmentResult != null)
            {
                this.focusedAlignmentResult = null;
            }

            if (this.alignViewDecFS != null)
            {
                this.alignViewDecFS.Dispose();
                this.alignViewDecFS.Close();
            }

            if (this.alignViewDecSeekPoints != null)
            {
                this.alignViewDecSeekPoints = new List<long>();
            }

            if (this.alignViewMS2DecResult != null)
            {
                this.alignViewMS2DecResult = null;
            }

            if (this.alignmentSpotTableViewer != null) {
                this.alignmentSpotTableViewer.Close();
                this.alignmentSpotTableViewer = null;
                this.alignmentSpotTableViewerFlag = true;
                this.alignmentSpotTableViewerFlag2 = true;
            }

            if (this.quantmassBrowser != null) {
                this.quantmassBrowser.Close();
                this.quantmassBrowser = null;
                this.quantmassBrowserFlag = true;
                this.quantmassBrowserFlag2 = true;
            }


            if (this.alignEicFS != null) {
                this.alignEicFS.Dispose();
                this.alignEicFS.Close();
            }

            if (this.alignEicSeekPoints != null) {
                this.alignEicSeekPoints = new List<long>();
            }

            if (this.alignEicResult != null) {
                this.alignEicResult = null;
            }
            if (this.alignEicResultOnDrift != null) {
                this.alignEicResultOnDrift = null;
            }

            alignmentViewerGuiRefresh();
        }

        private void peakViewerGuiRefresh()
        {
			this.RtMzPairwisePlotPeakViewUI.Content = new PairwisePlotPeakViewUI();
            this.ChromatogramXicUI.Content = new ChromatogramXicUI();
            this.Ms1MassSpectrogramUI.Content = new MassSpectrogramLeftRotateUI();
            this.Ms2MassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI();
            this.MS2ChromatogramUI.Content = new ChromatogramMrmUI();
            this.RawMassSpectrogramUI.Content = new MassSpectrogramUI();
            this.DeconvolutedMassSpectrogramUI.Content = new MassSpectrogramUI();

            this.DriftTimeMzPairwisePlotPeakViewUI.Content = new PairwisePlotPeakViewUI();
            this.DriftChromatogramXicUI.Content = new ChromatogramXicUI();
        }

        private void alignmentViewerGuiRefresh()
        {
            this.RtMzPairwisePlotAlignmentViewUI.Content = new PairwisePlotAlignmentViewUI();
            this.BarChartUI.Content = new BarChartUI();
            this.AlignedEicUI.Content = new ChromatogramAlignedEicUI();
            this.RepAndRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI();

            this.DriftTimeMzPairwiseAlignmentViewUI.Content = new PairwisePlotAlignmentViewUI();
            this.BarChartUIOnDrift.Content = new BarChartUI();
            this.AlignedEicUIOnDrift.Content = new ChromatogramAlignedEicUI();
        }

        private void TabControl_PairwisePlotViewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == 0) {
                this.pairwisePlotFocus = PairwisePlotFocus.peakView;
                this.TabItem_EicViewer.IsSelected = true;
                this.TabItem_MeasurementVsReference.IsSelected = true;
                this.Label_FocusSpotRt.Content = "RT(min): ";
                Button_TableViewer.ToolTip = new ToolTip().Content = "Peak Spot Viewer";
                Button_TableViewer.Click -= MenuItem_AlignmentSpotTableViewer_Click;
                Button_TableViewer.Click += MenuItem_PeakSpotTableViewer_Click;
            }
            else {
                this.pairwisePlotFocus = PairwisePlotFocus.alignmentView;
                this.TabItem_BarChartViewer.IsSelected = true;
                this.TabItem_RepresentativeVsReference.IsSelected = true;
                Button_TableViewer.ToolTip = new ToolTip().Content = "Alignment Spot Viewer";
                Button_TableViewer.Click -= MenuItem_PeakSpotTableViewer_Click;
                Button_TableViewer.Click += MenuItem_AlignmentSpotTableViewer_Click;

                if (this.projectProperty != null && this.projectProperty.Ionization == Ionization.EI &&
                    this.analysisParamForGC != null && this.analysisParamForGC.AlignmentIndexType == AlignmentIndexType.RI) {
                    this.Label_FocusSpotRt.Content = "RI value: ";
                }
                else {
                    this.Label_FocusSpotRt.Content = "RT(min): ";
                }
            }

            this.mainWindowDisplayVM.Refresh();
        }

        private void TabControl_MS2Viewer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (((TabControl)sender).SelectedIndex == 0) this.msmsDisplayFocus = MsMsDisplayFocus.ActVsRef;
            else if (((TabControl)sender).SelectedIndex == 1) this.msmsDisplayFocus = MsMsDisplayFocus.Ms2Chrom;
            else if (((TabControl)sender).SelectedIndex == 2) this.msmsDisplayFocus = MsMsDisplayFocus.RawVsDeco;
            else if (((TabControl)sender).SelectedIndex == 3) this.msmsDisplayFocus = MsMsDisplayFocus.RepVsRef;
        }

        #endregion

        #region // method for display filter
        private void doubleSlider_AmplitudeFilter_Loaded(object sender, RoutedEventArgs e)
        {
            this.doubleSlider_AmplitudeFilter.LowerSlider.ValueChanged += AmplitudeLowerSlider_ValueChanged;
            this.doubleSlider_AmplitudeFilter.UpperSlider.ValueChanged += AmplitudeUpperSlider_ValueChanged;
        }

        private void AmplitudeLowerSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.mainWindowDisplayVM.AmpSliderLowerValue = (float)this.doubleSlider_AmplitudeFilter.LowerSlider.Value;
            this.mainWindowDisplayVM.Refresh();
        }

        private void AmplitudeUpperSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
        {
            this.mainWindowDisplayVM.AmpSliderUpperValue = (float)this.doubleSlider_AmplitudeFilter.UpperSlider.Value;
            this.mainWindowDisplayVM.Refresh();
        }

        private void ComboBox_DisplayLabel_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            this.mainWindowDisplayVM.DisplayLabel = (PairwisePlotDisplayLabel)((ComboBox)sender).SelectedIndex;
            this.mainWindowDisplayVM.Refresh();
        }

		private void ScanNumberFilter_TextChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
			if (!analysisProcessed) return;
            if(ScanNumberFilter.Value == null) { ScanNumberFilter.Value = -1; }

			if (pairwisePlotFocus == PairwisePlotFocus.peakView) {  // LCMS || LCMS/MS
                var ppbfe = (PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content;
                //ppbfe.PairwisePlotBean.SelectedPlotId = -1;
                if (ppbfe.PairwisePlotBean.XAxisDatapointCollection == null || ppbfe.PairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                if (ProjectProperty.Ionization == Ionization.ESI) {
                    if (ScanNumberFilter.Value > ppbfe.PairwisePlotBean.PeakAreaBeanCollection.Max(it => it.ScanNumberAtPeakTop)) {
                        ScanNumberFilter.Value = ppbfe.PairwisePlotBean.PeakAreaBeanCollection.Max(it => it.ScanNumberAtPeakTop);
                    }

                } else if(ProjectProperty.Ionization == Ionization.EI) {    // GCMS
                    if (ScanNumberFilter.Value > ppbfe.PairwisePlotBean.Ms1DecResults.Max(it => it.ScanNumber)) {
                        ScanNumberFilter.Value = ppbfe.PairwisePlotBean.Ms1DecResults.Max(it => it.ScanNumber);
                    }
                }

                ppbfe.PairwisePlotBean.ScanNumber = (int)ScanNumberFilter.Value;
                ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).RefreshUI();
			} else if (pairwisePlotFocus == PairwisePlotFocus.alignmentView) {
				var ppav = ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean;

                if (ppav.XAxisDatapointCollection == null || ppav.XAxisDatapointCollection.Count == 0) return;
				ppav.ScanNumber = (int)ScanNumberFilter.Value;

                ((PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content).RefreshUI();
			}
		}

        private void checkBox_PlotIdentifiedOnly_Checked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.IdentifiedFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotIdentifiedOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.IdentifiedFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotAnnotatedOnly_Checked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.AnnotatedFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotAnnotatedOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.AnnotatedFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotMsMsOnly_Checked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.MsmsFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotMsMsOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.MsmsFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotMolcularIonOnly_Checked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.MolecularIonFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotMolcularIonOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.MolecularIonFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }


        private void checkBox_PlotUnknownOnly_Checked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.UnknownFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_PlotUnknownOnly_Unchecked(object sender, RoutedEventArgs e)
        {
            this.mainWindowDisplayVM.UnknownFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_SearchedFragment_Checked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.UniqueionFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_SearchedFragment_Unchecked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.UniqueionFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_IsBlankPassed_Checked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.BlankFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_IsBlankPassed_Unchecked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.BlankFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_MatchedOnly_Unchecked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.CcsFilter = false;
            this.mainWindowDisplayVM.Refresh();
        }

        private void checkBox_MatchedOnly_Checked(object sender, RoutedEventArgs e) {
            this.mainWindowDisplayVM.CcsFilter = true;
            this.mainWindowDisplayVM.Refresh();
        }
        #endregion

        #region // Mona Export Functions

        private void fillInfoBox()
        {
			if (markedFileIDs.Count > 0) {
				informationBox.Text = markedFileIDs.Count.ToString() + " spectra marked for upload.";
			} else {
				informationBox.Clear();
			}
        }

        private void contextMenu_MarkForExportMona_Click(object sender, RoutedEventArgs e)
        {
			if(monaProjectData == null) {
				monaProjectData = new MonaProjectData();
			}

			if (markedFileIDs == null) {
				markedFileIDs = new List<MonaSpectrum>();
			}

			try {
				MonaSpectrum toMark;
				if (projectProperty.Ionization == Ionization.EI) {          // if project is GCMS
					toMark = createMonaSpectrum(FocusedMS1DecID, projectProperty.Ionization);
				} else {                                                    // if project is LCMS or GCMS/MS
					toMark = createMonaSpectrum(FocusedPeakID, projectProperty.Ionization);
				}

				if (markedFileIDs.FindAll(sp => sp.splash.splash == toMark.splash.splash).Count > 0) {
					return;
				} else {
					markedFileIDs.Add(toMark);
				}
			} catch (ArgumentException ex) {
				MessageBox.Show("This peak is not fully identified yet, so it can't be uploded to MoNA.", ex.Message, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}

			fillInfoBox();
        }

        private void contextMenu_exportToMonaMultiple_Click(object sender, RoutedEventArgs e)
        {
			if (markedFileIDs == null || markedFileIDs.Count == 0) {
				MessageBox.Show("There are no spectra marked for upload.", "Info", MessageBoxButton.OK, MessageBoxImage.Exclamation);
				return;
			}

			if (monaProjectData == null) {
				monaProjectData = new MonaProjectData();
			}

            try {
                markedFileIDs.ForEach(spec => monaProjectData.Spectra.Add(spec));
                Debug.WriteLine("about to export: " + monaProjectData.Spectra.Count + " spectrum/a");

                var exportVM = new MonaExportWindowVM(monaProjectData);
                MonaExportWindow window = new MonaExportWindow(exportVM);
                window.Owner = this;
                window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
                window.ShowDialog();

                markedFileIDs.Clear();
            } catch (Exception ex) {
                MessageBox.Show(string.Format("There was an error opening the MonaExport window. Please inform the developers.\nError: {0}"), ex.Message);
            }

            monaProjectData = new MonaProjectData();
			informationBox.Clear();
		}

		private void contextMenu_exportToMonaSingle_Click(object sender, RoutedEventArgs e)
        {
            var entryList = new List<MonaSpectrum>();
			if (monaProjectData == null) {
				monaProjectData = new MonaProjectData();
			}

			monaProjectData.Spectra = new ObservableCollection<MonaSpectrum>();
			try {
				monaProjectData.Spectra.Add(createMonaSpectrum(focusedPeakID, projectProperty.Ionization));

				var exportVM = new MonaExportWindowVM(monaProjectData);
				MonaExportWindow window = new MonaExportWindow(exportVM);

				window.Owner = this;
				window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
				window.ShowDialog();
			} catch(ArgumentException ex) {
				MessageBox.Show("This peak is not fully identified yet, so it can't be uploded to MoNA.\n" + ex.Message, ex.GetType().Name, MessageBoxButton.OK, MessageBoxImage.Exclamation);
			}

			monaProjectData = new MonaProjectData();
			informationBox.Clear();
		}

		private void contextMenu_ClearMarkedPeaksMona_Click(object sender, RoutedEventArgs e)
        {
            if (markedFileIDs != null) markedFileIDs.Clear();
            fillInfoBox();
        }

		private MonaSpectrum createMonaSpectrum(int peakId, Ionization ionizationType) {
			MonaSpectrum spectrum = new MonaSpectrum() { metaData = new ObservableCollection<MetaData>(), compounds = new ObservableCollection<Compound>(), tags = new ObservableCollection<Tag>() };
			MassSpectrogramViewModel ms2decVM;
			PeakAreaBean peak;
			MspFormatCompoundInformationBean libraryCmp;
			var metaDataList = new ObservableCollection<MetaData>();

			if (ionizationType == Ionization.ESI) {
				peak = analysisFiles[FocusedFileID].PeakAreaBeanCollection[peakId];
				ms2decVM = UiAccessLcUtility.GetMs2DeconvolutedMassspectrogramViewModel(peak, peakViewMS2DecResult);

				if (peak.LibraryID < 0) {
					throw new ArgumentException("Peak is not identified.");
				}
				if(string.IsNullOrWhiteSpace(MspDB[peak.LibraryID].InchiKey)) {
					throw new ArgumentException("Peak is missing InChIKey.");
				}

				libraryCmp = MspDB[peak.LibraryID]; // identified compound for current peak

				//splash
				string[] blocks = ms2decVM.MeasuredMassSpectrogramBean.SplashKey.Split('-');
				var splash = new Splash() { splash = ms2decVM.MeasuredMassSpectrogramBean.SplashKey, block1 = blocks[0], block2 = blocks[1], block3 = blocks[2], block4 = blocks[3] };
				spectrum.splash = splash;

                // metadata list
                metaDataList.Add(new MetaData() { Name = "ms level", Value = "MS2" });
				metaDataList.Add(new MetaData() { Name = "accurate mass", Value = peak.AccurateMass.ToString() });
				metaDataList.Add(new MetaData() { Name = "Adduct Ion Accurate Mass", Value = peak.AdductIonAccurateMass.ToString() });
				metaDataList.Add(new MetaData() { Name = "Adduct Ion Name", Value = peak.AdductIonName });
				metaDataList.Add(new MetaData() { Name = "Retention Time", Value = peak.RtAtPeakTop.ToString() });
				metaDataList.Add(new MetaData() { Name = "Precursor MZ", Value = libraryCmp.PrecursorMz.ToString() });
				metaDataList.Add(new MetaData() { Name = "Ion Mode", Value = libraryCmp.IonMode.ToString() });
				metaDataList.Add(new MetaData() { Name = "Sample file", Value = analysisFiles[FocusedFileID].AnalysisFilePropertyBean.AnalysisFilePath });
				metaDataList.Add(new MetaData() { Name = "Sample type", Value = analysisFiles[FocusedFileID].AnalysisFilePropertyBean.AnalysisFileType.ToString() });
			} else {
				peak = gcmsPeakAreaList[peakId];
				var deconvolutedPeak = ms1DecResults.Single(it => it.Ms1DecID == focusedMS1DecID);
				ms2decVM = UiAccessGcUtility.GetMs2DeconvolutedMassSpectrogramVM(deconvolutedPeak);
				libraryCmp = MspDB[deconvolutedPeak.MspDbID]; // identified compound for current peak

				//splash
				var splasher = new NSSplash.Splash();
				var ions = new List<Ion>();
				deconvolutedPeak.Spectrum.ForEach(p => ions.Add(new Ion(p.Mz, p.Intensity)));

				var splash = splasher.splashIt(new MSSpectrum(ions));
				string[] blocks = splash.Split('-');
				spectrum.splash = new Splash {splash=splash, block1=blocks[0], block2 = blocks[1], block3 = blocks[2], block4 = blocks[3] };

                // metadata list
                metaDataList.Add(new MetaData() { Name = "ms level", Value = "MS1" });
                metaDataList.Add(new MetaData() { Name = "accurate mass", Value = peak.AccurateMass.ToString() });
				metaDataList.Add(new MetaData() { Name = "retention time", Value = deconvolutedPeak.RetentionTime.ToString() });
                metaDataList.Add(new MetaData() { Name = "retention index", Value = deconvolutedPeak.RetentionIndex.ToString() });
                if (libraryCmp.PrecursorMz > 0) { metaDataList.Add(new MetaData() { Name = "precursor mz", Value = libraryCmp.PrecursorMz.ToString() }); }
				metaDataList.Add(new MetaData() { Name = "ion mode", Value = libraryCmp.IonMode.ToString() });
				metaDataList.Add(new MetaData() { Name = "sample file", Value = analysisFiles[FocusedFileID].AnalysisFilePropertyBean.AnalysisFilePath });
				metaDataList.Add(new MetaData() { Name = "sample type", Value = analysisFiles[FocusedFileID].AnalysisFilePropertyBean.AnalysisFileType.ToString() });
			}


			// compound list
			var compoundList = new ObservableCollection<Compound>();

			var cmp = new Compound() { names = new ObservableCollection<Name>() };
			cmp.inchiKey = libraryCmp.InchiKey.Replace("InChIKey=", "");

			Name name = new Name() { source = "MS-Dial Identification" };
			if (libraryCmp.Name.Contains(";")) {
				name.name = libraryCmp.Name.Split(';').First();
			} else {
				name.name = libraryCmp.Name;
			}
			cmp.names.Add(name);

			compoundList.Add(cmp);

			// build spectrum object
			spectrum.metaData = metaDataList;
			spectrum.compounds = compoundList;
			spectrum.tags = new ObservableCollection<Tag>();

			var spec = new StringBuilder();
			foreach (double[] ion in ms2decVM.MeasuredMassSpectrogramBean.MassSpectraCollection) {
				spec.AppendFormat("{0}:{1} ", ion[0], ion[1]);
			}
			spectrum.spectrum = spec.ToString().Trim();

			return spectrum;
		}

        private void contextMenu_MarkForExportMona_HotKey(object sender, ExecutedRoutedEventArgs e)
        {
            if (focusedPeakID >= 0) contextMenu_MarkForExportMona_Click(sender, e);
        }

        private void contextMenu_ExportMultiple_Hotkey(object sender, ExecutedRoutedEventArgs e)
        {
            if (focusedPeakID >= 0) contextMenu_exportToMonaMultiple_Click(sender, e);
        }

        private void contextMenu_ExportSingle_Hotkey(object sender, ExecutedRoutedEventArgs e)
        {
            if (focusedPeakID >= 0) contextMenu_exportToMonaSingle_Click(sender, e);
        }

        // Bar chart settings
        private void contextMenu_BarChartDisplayMode_OriginalHeight_Click(object sender, RoutedEventArgs e) {
            if (BarChartDisplayMode == BarChartDisplayMode.OriginalHeight) return;
            BarChartDisplayMode = BarChartDisplayMode.OriginalHeight;
            Update_BarChart();
            if (this.analysisParamForLC != null && this.analysisParamForLC.IsIonMobility) {
                Update_BarChartOnDrift();
            }
            Reset_AlignmentTableViewer();
        }
        private void contextMenu_BarChartDisplayMode_NormalizedHeight_Click(object sender, RoutedEventArgs e) {
            if (BarChartDisplayMode == BarChartDisplayMode.NormalizedHeight) return;
            BarChartDisplayMode = BarChartDisplayMode.NormalizedHeight;
            Update_BarChart();
            if (this.analysisParamForLC != null && this.analysisParamForLC.IsIonMobility) {
                Update_BarChartOnDrift();
            }
            Reset_AlignmentTableViewer();

        }
        private void contextMenu_BarChartDisplayMode_OriginalArea_Click(object sender, RoutedEventArgs e) {
            if (BarChartDisplayMode == BarChartDisplayMode.OriginalArea) return;
            BarChartDisplayMode = BarChartDisplayMode.OriginalArea;
            Update_BarChart();
            if (this.analysisParamForLC != null && this.analysisParamForLC.IsIonMobility) {
                Update_BarChartOnDrift();
            }
            Reset_AlignmentTableViewer();
        }
        private void contextMenu_BarChartType_BoxPlot_Click(object sender, RoutedEventArgs e) {
            if (this.projectProperty.IsBoxPlotForAlignmentResult == true) return;
            this.projectProperty.IsBoxPlotForAlignmentResult = true;
            Update_BarChart();
            if (this.analysisParamForLC != null && this.analysisParamForLC.IsIonMobility) {
                Update_BarChartOnDrift();
            }

            Reset_AlignmentTableViewer();
        }
        private void contextMenu_BarChartType_BarChart_Click(object sender, RoutedEventArgs e) {
            if (this.projectProperty.IsBoxPlotForAlignmentResult == false) return;
            this.projectProperty.IsBoxPlotForAlignmentResult = false;
            Update_BarChart();
            if (this.analysisParamForLC != null && this.analysisParamForLC.IsIonMobility) {
                Update_BarChartOnDrift();
            }
            Reset_AlignmentTableViewer();
        }

        // it takes too long time, and sometimes failed.
        public void Update_AlignmentTableViewer_AllFigures() {
            if(this.AlignmentSpotTableViewer != null)
                this.AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.UpdateAllImages(analysisFiles,
                    this.projectProperty, BarChartDisplayMode, this.projectProperty.IsBoxPlotForAlignmentResult);
        }

        public void Reset_AlignmentTableViewer() {
            if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
                MessageBox.Show("You don't have any aligmnet result file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }
            if (this.focusedAlignmentFileID < 0) { MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error); return; }
            if (this.alignmentSpotTableViewer != null) {
                this.alignmentSpotTableViewer.Close();
                this.alignmentSpotTableViewer = null;
                this.alignmentSpotTableViewerFlag = true;
                this.alignmentSpotTableViewerFlag2 = true;
                _ = new TableViewerTaskHandler().StartUpAlignmentSpotTableViewer(this);
            }
        }

        #endregion

        #region // event on pairwise plots
        private void RtMzPairwisePlotPeakViewUI_MouseRightButtonUp(object sender, MouseButtonEventArgs e) {
            if (e.Source == null || e.Source.ToString() != "PairwisePlotPeakViewUI") return;
            var ppui = (PairwisePlotPeakViewUI)e.Source;
			var ppfe = (PairwisePlotPeakViewFE)e.OriginalSource;
			var ppb = ppfe.PairwisePlotBean;

			var mousePos = Mouse.GetPosition(ppui);

			var dataPoint = ppfe.GetDataPositionOnMousePoint(mousePos);

			//const float FACTOR = 0.1F;

			//if (ms1DecResults.Count > 0) {
			//	foreach (var peak in ms1DecResults) {
			//		if ((dataPoint.X > peak.RetentionTime - FACTOR && dataPoint.X < peak.RetentionTime + FACTOR)) {
			//			Debug.WriteLine("peak.X: " + peak.RetentionTime + "   --   mouse.x: " + dataPoint.X);
			//		}
			//		if (dataPoint.Y > peak.BasepeakMz - 1 && dataPoint.Y < peak.BasepeakMz + 1) {
			//			Debug.WriteLine("peak.Y: " + peak.BasepeakMz + "   --   mouse.y: " + dataPoint.Y);
			//		}
			//	}
			//} else {
			//	Debug.WriteLine("no peakAreaBeanCollection");
			//}
			//Debug.WriteLine("range X: " + ppui.PairwisePlotBean.DisplayRangeMaxX + " - " + ppui.PairwisePlotBean.MaxX);
			//Debug.WriteLine("range Y: " + ppui.PairwisePlotBean.DisplayRangeMaxY + " - " + ppui.PairwisePlotBean.MaxY);

			var ctxMenu = FindResource("menuPeakViewer") as ContextMenu;
			var monaMenu = ctxMenu.Items[3] as MenuItem;
            var mrmprobsMenu = ctxMenu.Items[4] as MenuItem;

			if (FocusedMS1DecID >= 0 || focusedPeakID >= 0) {
				//enable mona menu
				monaMenu.IsEnabled = true;
				foreach (MenuItem item in monaMenu.Items) {
					item.IsEnabled = true;
				}

                //enable mrmprobs export menu
                mrmprobsMenu.IsEnabled = true;
                foreach (MenuItem item in mrmprobsMenu.Items) {
                    item.IsEnabled = true;
                }
			} else {
				//disable mona menu
				monaMenu.IsEnabled = false;
				foreach (MenuItem item in monaMenu.Items) {
					item.IsEnabled = false;
				}

                //disable mrmprobs export menu
                mrmprobsMenu.IsEnabled = false;
                foreach (MenuItem item in mrmprobsMenu.Items) {
                    item.IsEnabled = false;
                }
			}
		}

        private void RtMzPairwisePlotAlignmentViewUI_MouseRightButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (e.Source == null || e.Source.ToString() != "PairwisePlotAlignmentViewUI") return;
            var ppui = (PairwisePlotAlignmentViewUI)e.Source;
            var ppfe = (PairwisePlotAlignmentViewFE)e.OriginalSource;

            var mousePos = Mouse.GetPosition(ppui);

            var dataPoint = ppfe.GetDataPositionOnMousePoint(mousePos);

            var ctxMenu = FindResource("menuAlignmentViewer") as ContextMenu;
            var mrmprobsMenu = ctxMenu.Items[3] as MenuItem;

            if (FocusedAlignmentMs1DecID >= 0 || FocusedAlignmentPeakID >= 0) {
                //enable mrmprobs export menu
                mrmprobsMenu.IsEnabled = true;
                foreach (MenuItem item in mrmprobsMenu.Items) {
                    item.IsEnabled = true;
                }
            }
            else {
                //disable mrmprobs export menu
                mrmprobsMenu.IsEnabled = false;
                foreach (MenuItem item in mrmprobsMenu.Items) {
                    item.IsEnabled = false;
                }
            }
        }
        #endregion

        #region // export as mrmprobs format
        private void contextMenu_MrmprobsRefExportSaveAs_Click(object sender, RoutedEventArgs e)
        {
            //Console.WriteLine((e.Source as MenuItem).Parent.ToString());

            //var target = ((((e.Source as MenuItem).Parent as MenuItem)).Parent as ContextMenu).PlacementTarget;

            //if (target.GetType() == typeof(PairwisePlotPeakViewUI) && this.focusedFileID < 0) return;
            //if (target.GetType() == typeof(PairwisePlotAlignmentViewUI) && this.focusedAlignmentFileID < 0) return;

            var isAlignmentView = this.pairwisePlotFocus == PairwisePlotFocus.alignmentView ? true : false;

            var window = new MrmprobsExportWin();
            window.Owner = this;

            var mpvm = new MrmprobsExportVM(isAlignmentView);
            window.DataContext = mpvm;

            window.Show();
        }

        private void contextMenu_MrmprobsRefExportCopyAs_Click(object sender, RoutedEventArgs e)
        {
            //var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;
            //var target = ((((e.Source as MenuItem).Parent as MenuItem)).Parent as ContextMenu).PlacementTarget;

            //if (target.GetType() == typeof(PairwisePlotPeakViewUI) && this.focusedFileID < 0) return;
            //if (target.GetType() == typeof(PairwisePlotAlignmentViewUI) && this.focusedAlignmentFileID < 0) return;

            var isAlignmentView = this.pairwisePlotFocus == PairwisePlotFocus.alignmentView ? true : false;

            if (this.projectProperty.Ionization == Ionization.ESI) {

                if (isAlignmentView) {
                    DataExportLcUtility.CopyToClipboardMrmprobsRef(this.alignViewMS2DecResult,
                       this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID], this.mspDB,
                       this.analysisParamForLC, this.projectProperty);
                }
                else {
                    DataExportLcUtility.CopyToClipboardMrmprobsRef(this.peakViewMS2DecResult,
                        this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID], this.mspDB,
                        this.analysisParamForLC, this.projectProperty);
                }

            }
            else {
                if (isAlignmentView) {
                    DataExportGcUtility.CopyToClipboardMrmprobsRef(this.focusedAlignmentMS1DecResults[this.focusedAlignmentMs1DecID], this.mspDB, this.analysisParamForGC);
                }
                else {
                    DataExportGcUtility.CopyToClipboardMrmprobsRef(this.ms1DecResults[this.focusedMS1DecID], this.mspDB, this.analysisParamForGC);
                }
            }
        }

        #endregion

        #region // export as lipoquality database format
        private void menuItem_LipoqualityDatabaseFormatExport_Click(object sender, RoutedEventArgs e)
        {
            //var library = @"D:\20170130-Mori-Plant related standard spectra\Negative ion library for MAT file creation-CE10.txt";
            //var outputFolder = @"D:\20170130-Mori-Plant related standard spectra\Mat files-Pos-20170801";

            //var ofd = new OpenFileDialog();
            //ofd.Filter = "Text file(*.txt)|*.txt";
            //ofd.Title = "Import a library file";
            //ofd.RestoreDirectory = true;

            //if (ofd.ShowDialog() == true) {
            //    library = ofd.FileName;
            //    int focusedFileID = mainWindow.FocusedFileID;
            //    int focusedPeakID = mainWindow.FocusedPeakID;

            //    this.PeakViewDataAccessRefresh();

            //    this.IsEnabled = false;
            //    Mouse.OverrideCursor = Cursors.Wait;

            //    var matmetadataList = ExtractMatFiles.GetMatMetaDataList(library);
            //    var libraryname = System.IO.Path.GetFileNameWithoutExtension(library);
            //    ExtractMatFiles.ExportMatFiles(outputFolder, libraryname, matmetadataList,
            //        this.analysisFiles, this.rdamProperty, this.projectProperty, this.analysisParamForLC);

            //    this.PeakViewerForLcRefresh(focusedFileID);
            //    ((PairwisePlotPeakViewUI)this.RtMzPairwisePlotPeakViewUI.Content).PairwisePlotBean.SelectedPlotId = focusedPeakID;

            //    Mouse.OverrideCursor = null;
            //    this.IsEnabled = true;
            //}

            //if (this.alignmentFiles == null || this.alignmentFiles.Count == 0) {
            //    MessageBox.Show("There is no alignment result in this project", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
            //    return;
            //}

            var window = new LipoqualityDatabaseFormatExportWin();
            window.Owner = this;

            var mpvm = new LipoqualityDatabaseFormatExportVM();
            window.DataContext = mpvm;

            window.Show();
        }
        #endregion

        #region // display utility
        private void Button_FocusRegionByID_Click(object sender, RoutedEventArgs e) {
            if (this.projectProperty == null)
                return;
            this.mainWindowDisplayVM.DisplayFocus("ID");
        }

        private void TextBox_FocusSpotID_KeyDown(object sender, KeyEventArgs e) {
            if (this.projectProperty == null)
                return;
            if (e.Key == Key.Enter) {
                this.mainWindowDisplayVM.DisplayFocus("ID");
            }
        }

        private void CtrlD_KeyUp()
        {
            if (this.pairwisePlotFocus == PairwisePlotFocus.peakView)
            {
                if (this.AnalysisParamForLC != null)
                {
                    if (this.AnalysisParamForLC.IsIonMobility)
                    {
                        var driftSpot = this.driftSpotBeanList[this.focusedDriftSpotID];
                        driftSpot.LibraryID = -1;
                        driftSpot.MetaboliteName = string.Empty;
                        driftSpot.RtSimilarityValue = -1;
                        driftSpot.AccurateMassSimilarity = -1;
                        driftSpot.ReverseSearchSimilarityValue = -1;
                        driftSpot.MassSpectraSimilarityValue = -1;
                        driftSpot.PresenseSimilarityValue = -1;
                        driftSpot.TotalScore = -1;
                        driftSpot.PostIdentificationLibraryId = -1;
                        driftSpot.IsMs1Match = false;
                        driftSpot.IsMs2Match = false;
                    }
                    else
                    {
                        var peakSpot = this.analysisFiles[this.focusedFileID].PeakAreaBeanCollection[this.focusedPeakID];
                        peakSpot.LibraryID = -1;
                        peakSpot.MetaboliteName = string.Empty;
                        peakSpot.RtSimilarityValue = -1;
                        peakSpot.AccurateMassSimilarity = -1;
                        peakSpot.ReverseSearchSimilarityValue = -1;
                        peakSpot.MassSpectraSimilarityValue = -1;
                        peakSpot.PresenseSimilarityValue = -1;
                        peakSpot.TotalScore = -1;
                        peakSpot.PostIdentificationLibraryId = -1;
                        peakSpot.IsMs1Match = false;
                        peakSpot.IsMs2Match = false;
                        if (peakSpot.LibraryIDList != null)
                            for (var i = 0; i < peakSpot.LibraryIDList.Count; i++) peakSpot.LibraryIDList[i] = -1;
                    }
                }
                else if (this.AnalysisParamForGC != null)
                {
                    var ms1decId = this.focusedMS1DecID;
                    var ms1DecResult = this.ms1DecResults[ms1decId];
                    ms1DecResult.MspDbID = -1;
                    ms1DecResult.EiSpectrumSimilarity = -1;
                    ms1DecResult.DotProduct = -1;
                    ms1DecResult.ReverseDotProduct = -1;
                    ms1DecResult.PresencePersentage = -1;
                    ms1DecResult.RetentionTimeSimilarity = -1;
                    ms1DecResult.RetentionIndexSimilarity = -1;
                    ms1DecResult.TotalSimilarity = -1;
                }
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
                this.mainWindowDisplayVM.Refresh();
                if (this.peakSpotTableViewer != null)
                    this.peakSpotTableViewer.PeakSpotTableViewerVM.SourceView.Refresh();
            }
            else
            {
                if (this.focusedAlignmentFileID < 0) return;
                if (this.AnalysisParamForLC != null && this.AnalysisParamForLC.IsIonMobility)
                {
                    var driftSpot = this.alignedDriftSpotBeanList[this.focusedAlignmentDriftID];
                    driftSpot.LibraryID = -1;
                    driftSpot.MetaboliteName = string.Empty;
                    driftSpot.ReverseSimilarity = -1;
                    driftSpot.MassSpectraSimilarity = -1;
                    driftSpot.FragmentPresencePercentage = -1;
                    driftSpot.RetentionTimeSimilarity = -1;
                    driftSpot.AccurateMassSimilarity = -1;
                    driftSpot.TotalSimilairty = -1;
                    driftSpot.IsMs1Match = false;
                    driftSpot.IsMs2Match = false;
                    driftSpot.IsCcsMatch = false;
                    driftSpot.IsRtMatch = false;
                    driftSpot.IsManuallyModifiedForAnnotation = true;
                    driftSpot.PostIdentificationLibraryID = -1;
                    if (this.alignmentSpotTableViewer != null)
                    {
                        this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[driftSpot.MasterID].MetaboliteName = driftSpot.MetaboliteName;
                        this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[driftSpot.MasterID].UpdateMetaboliteNameOnTable(true);
                    }
                    updateAlignmentSpotMasterAsUnknown(this.focusedAlignmentResult.AlignmentPropertyBeanCollection[driftSpot.AlignmentSpotID]);
                }
                else
                {
                    var alignedSpot = this.focusedAlignmentResult.AlignmentPropertyBeanCollection[this.focusedAlignmentPeakID];
                    alignedSpot.LibraryID = -1;
                    alignedSpot.MetaboliteName = string.Empty;
                    alignedSpot.ReverseSimilarity = -1;
                    alignedSpot.MassSpectraSimilarity = -1;
                    alignedSpot.FragmentPresencePercentage = -1;
                    alignedSpot.RetentionTimeSimilarity = -1;
                    alignedSpot.AccurateMassSimilarity = -1;
                    alignedSpot.TotalSimilairty = -1;
                    alignedSpot.PostIdentificationLibraryID = -1;
                    alignedSpot.IsMs1Match = false;
                    alignedSpot.IsMs2Match = false;
                    alignedSpot.IsManuallyModifiedForAnnotation = true;
                    if (alignedSpot.LibraryIdList != null)
                        for (var i = 0; i < alignedSpot.LibraryIdList.Count; i++) alignedSpot.LibraryIdList[i] = -1;
                    if (alignedSpot.CorrelBasedlibraryIdList != null)
                        for (var i = 0; i < alignedSpot.CorrelBasedlibraryIdList.Count; i++) alignedSpot.CorrelBasedlibraryIdList[i] = -1;

                }
                this.mainWindowDisplayVM.MetaDataLabelRefresh();
                this.mainWindowDisplayVM.Refresh();
                if (this.alignmentSpotTableViewer != null)
                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.SourceView.Refresh();
            }
        }

        private void RtMzPairwisePlotPeakViewUI_KeyUp(object sender, KeyEventArgs e) {
            if (this.projectProperty == null)
                return;
            if (e.Key == Key.D && Keyboard.Modifiers == ModifierKeys.Control)
            {
                CtrlD_KeyUp();
            }
            else if (e.Key == Key.Right)
            {
                this.mainWindowDisplayVM.FocusNext();
            }
            else if (e.Key == Key.Left)
            {
                this.mainWindowDisplayVM.FocusBack();
            }
            #region my private code
            //var alignResult = this.focusedAlignmentResult;
            //if (alignResult == null) return;

            //var currentID = this.focusedAlignmentPeakID;
            //if (currentID < 0) return;
            //var spots = alignResult.AlignmentPropertyBeanCollection;

            //if (e.Key == Key.Right) {
            //    for (int i = 0; i < spots.Count; i++) {
            //        var spot = spots[i];
            //        if (currentID < spot.AlignmentID && spot.IsotopeTrackingWeightNumber == 0) {
            //            if (drawNextSpot(spot, spots))
            //                break;
            //        }
            //    }
            //}
            //else if (e.Key == Key.Left) {
            //    for (int i = spots.Count - 1; i >= 0; i--) {
            //        var spot = spots[i];
            //        if (currentID > spot.AlignmentID && spot.IsotopeTrackingWeightNumber == 0) {
            //            if (drawNextSpot(spot, spots))
            //                break;
            //        }
            //    }
            //}
            #endregion

        }

        private void updateAlignmentSpotMasterAsUnknown(AlignmentPropertyBean spot) {
            var isAllUnknown = true;
            foreach (var drift in spot.AlignedDriftSpots) {
                if (drift.LibraryID >= 0 && drift.IsMs2Match) {
                    isAllUnknown = false;
                    break;
                }
            }
            if (isAllUnknown == true) {
                spot.LibraryID = -1;
                spot.MetaboliteName = string.Empty;
                spot.ReverseSimilarity = -1;
                spot.MassSpectraSimilarity = -1;
                spot.FragmentPresencePercentage = -1;
                spot.RetentionTimeSimilarity = -1;
                spot.CcsSimilarity = -1;
                spot.AccurateMassSimilarity = -1;
                spot.TotalSimilairty = -1;
                spot.PostIdentificationLibraryID = -1;
                spot.IsMs1Match = false;
                spot.IsMs2Match = false;
                spot.IsManuallyModifiedForAnnotation = true;
                if (this.alignmentSpotTableViewer != null) {
                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[spot.MasterID].MetaboliteName = spot.MetaboliteName;
                    this.alignmentSpotTableViewer.AlignmentSpotTableViewerVM.Source[spot.MasterID].UpdateMetaboliteNameOnTable(false);
                }
            }
        }

        private bool drawNextSpot(AlignmentPropertyBean spot, ObservableCollection<AlignmentPropertyBean> spots) {
            var nextSpots = spots.Where(n => n.IsotopeTrackingParentID == spot.IsotopeTrackingParentID).ToList();
            if (nextSpots.Count >= 2) {

                var beginSpot = nextSpots[0];
                var lastSpot = nextSpots[nextSpots.Count - 1];

                var nonLabelID = this.AnalysisParamForLC.NonLabeledReferenceID;
                var labeledID = this.analysisParamForLC.FullyLabeledReferenceID;
                if (beginSpot.AlignedPeakPropertyBeanCollection[nonLabelID].Variable >
                    beginSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable * 3.0 &&
                    lastSpot.AlignedPeakPropertyBeanCollection[labeledID].Variable >
                    lastSpot.AlignedPeakPropertyBeanCollection[nonLabelID].Variable * 3.0) {

                    this.focusedAlignmentPeakID = spot.AlignmentID;
                    this.TextBox_FocusSpotID.Text = spot.AlignmentID.ToString();

                    var content = (PairwisePlotAlignmentViewUI)this.RtMzPairwisePlotAlignmentViewUI.Content;
                    var mz = beginSpot.CentralAccurateMass;
                    var rt = beginSpot.CentralRetentionTime;

                    content.PairwisePlotBean.SelectedPlotId = spot.AlignmentID;
                    content.PairwisePlotBean.DisplayRangeMaxX = rt + 0.5F;
                    content.PairwisePlotBean.DisplayRangeMinX = rt - 0.5F;
                    content.PairwisePlotBean.DisplayRangeMaxY = lastSpot.CentralAccurateMass + 10;
                    content.PairwisePlotBean.DisplayRangeMinY = beginSpot.CentralAccurateMass - 10;
                    content.RefreshUI();
                    return true;
                }
            }
            return false;
        }

        private void Button_FocusRegionByRt_Click(object sender, RoutedEventArgs e) {
            if (this.projectProperty == null)
                return;
            this.mainWindowDisplayVM.DisplayFocus("RT");
        }

        private void TextBox_FocusSpotRT_KeyDown(object sender, KeyEventArgs e) {
            if (this.projectProperty == null)
                return;
            if (e.Key == Key.Enter) {
                this.mainWindowDisplayVM.DisplayFocus("RT");
            }
        }

        private void Button_FocusRegionByMz_Click(object sender, RoutedEventArgs e) {
            if (this.projectProperty == null)
                return;
            this.mainWindowDisplayVM.DisplayFocus("MZ");
        }

        private void TextBox_FocusSpotMZ_KeyDown(object sender, KeyEventArgs e) {
            if (this.projectProperty == null)
                return;
            if (e.Key == Key.Enter) {
                this.mainWindowDisplayVM.DisplayFocus("MZ");
            }
        }

        private void TabControl_PeakCharacter_SelectionChanged(object sender, SelectionChangedEventArgs e) {
            if (this.projectProperty == null) return;

            if (this.TabItem_StructureImage.IsSelected == true) {
                this.Image_Structure.Source = this.mainWindowDisplayVM.GetSmilesAsImage(this.DisplayedSmilesInfo, this.TabControl_PeakCharacter.ActualWidth, this.TabControl_PeakCharacter.ActualHeight);
            }
        }

        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName) {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged

        #region // Required Method for IDataErrorInfo
        private Dictionary<string, string> errors = new Dictionary<string, string>();
        public string Error { get { return null; } }

        public string this[string columnName] {
            get {
                try {
                    var result = new List<System.ComponentModel.DataAnnotations.ValidationResult>();
                    if (Validator.TryValidateProperty(GetType().GetProperty(columnName).GetValue(this, null), new ValidationContext(this, null, null) { MemberName = columnName }, result)) {
                        if (ValidationResultException(columnName))
                            this.ClearError(columnName);
                    }
                    else {
                        if (ValidationResultException(columnName))
                            this.SetError(columnName, result.First().ErrorMessage);
                    }

                    if (errors.ContainsKey(columnName)) {
                        return errors[columnName];
                    }
                    return null;
                }
                finally { CommandManager.InvalidateRequerySuggested(); }
            }
        }

        //Register the exception properties
        private bool ValidationResultException(string columnName) {
            return true;
        }

        protected void SetError(string propertyName, string errorMessage) {
            this.errors[propertyName] = errorMessage;
            this.OnPropertyChanged("HasError");
        }

        protected void ClearError(string propertyName) {
            if (this.errors.ContainsKey(propertyName)) {
                this.errors.Remove(propertyName);
                this.OnPropertyChanged("HasError");
            }
        }

        protected void ClearErrors() {
            this.errors.Clear();
            this.OnPropertyChanged("HasError");
        }

        private bool hasViewError;

        public bool HasViewError {
            get { return hasViewError; }
            set {
                if (EqualityComparer<bool>.Default.Equals(hasViewError, value))
                    return;
                hasViewError = value;
                OnPropertyChanged("HasViewError");
                OnPropertyChanged("HasError");
            }
        }

        public bool HasError {
            get {
                return this.errors.Count != 0 || HasViewError;
            }
        }

        public int FocusedDriftSpotID {
            get {
                return focusedDriftSpotID;
            }

            set {
                focusedDriftSpotID = value;
            }
        }

        public ObservableCollection<RawSpectrum> AccumulatedMs1Specra {
            get {
                return accumulatedMs1Specra;
            }

            set {
                accumulatedMs1Specra = value;
            }
        }

        public RawMeasurement RawMeasurement {
            get {
                return rawMeasurement;
            }

            set {
                rawMeasurement = value;
            }
        }

        public DriftSpotBean SelectedPeakViewDriftSpot {
            get {
                return selectedPeakViewDriftSpot;
            }

            set {
                selectedPeakViewDriftSpot = value;
            }
        }

        public int FocusedAlignmentDriftID {
            get {
                return focusedAlignmentDriftID;
            }

            set {
                focusedAlignmentDriftID = value;
            }
        }

        public int FocusedAlignmentMasterID {
            get {
                return focusedAlignmentMasterID;
            }

            set {
                focusedAlignmentMasterID = value;
            }
        }

        public int FocusedMasterID {
            get {
                return focusedMasterID;
            }

            set {
                focusedMasterID = value;
            }
        }

        public ObservableCollection<DriftSpotBean> DriftSpotBeanList {
            get {
                return driftSpotBeanList;
            }

            set {
                driftSpotBeanList = value;
            }
        }

        public ObservableCollection<AlignedDriftSpotPropertyBean> AlignedDriftSpotBeanList {
            get {
                return alignedDriftSpotBeanList;
            }

            set {
                alignedDriftSpotBeanList = value;
            }
        }

        public AlignedDriftSpotPropertyBean SelectedAlignmentViewDriftSpot {
            get {
                return selectedAlignmentViewDriftSpot;
            }

            set {
                selectedAlignmentViewDriftSpot = value;
            }
        }

        public AlignedData AlignEicResultOnDrift {
            get {
                return alignEicResultOnDrift;
            }

            set {
                alignEicResultOnDrift = value;
            }
        }

        public List<AlignmentPropertyBean> LipoqualitySpotProperties {
            get {
                return lipoqualitySpotProperties;
            }

            set {
                lipoqualitySpotProperties = value;
            }
        }

        public List<AnalysisFileBean> LipoqualityFileProperties {
            get {
                return lipoqualityFileProperties;
            }

            set {
                lipoqualityFileProperties = value;
            }
        }

        public List<RawData> LipoqualitySpectrumQueries {
            get {
                return lipoqualitySpectrumQueries;
            }

            set {
                lipoqualitySpectrumQueries = value;
            }
        }






        #endregion

        private void MenuItem_MappingToLipidPathway_Click(object sender, RoutedEventArgs e) {
            if (this.focusedAlignmentFileID < 0) return;
            //if (this.projectProperty.TargetOmics == TargetOmics.Metablomics) {
            //    MessageBox.Show("Pathway mapping is now available at lipidomics project. Metabolomics pathway mapping will be activated when it's ready.", "Information", MessageBoxButton.OK, MessageBoxImage.Asterisk);
            //    return;
            //}
            var window = new MetabolicPathwaySetWin(this);
            window.Owner = mainWindow;
            window.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            window.Show();
            //Mouse.OverrideCursor = Cursors.Wait;
            //MappingToPathways.MappingToLipidPathway(@"C:\Users\hiros\Dropbox\Vanted mapper\Lipid_Pathway_summary_ver2.graphml", this);
            //Mouse.OverrideCursor = null;
            //if (this.projectProperty.Ionization != Ionization.ESI || this.projectProperty.TargetOmics != TargetOmics.Lipidomics) {
            //    MessageBox.Show("Lipoquality search is available in LC-MS lipidomics project", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}

            //if (this.mspDB == null || this.mspDB.Count == 0) {
            //    MessageBox.Show("No library query", "Information", MessageBoxButton.OK, MessageBoxImage.Information);
            //    return;
            //}

            //Mouse.OverrideCursor = Cursors.Wait;
            //LipidProfileMapper.LipidProfileProjectionToCytoscape(this);
            //Mouse.OverrideCursor = null;
        }

        
    }
}
