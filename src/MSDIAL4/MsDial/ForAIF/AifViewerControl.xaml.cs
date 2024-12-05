using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using Msdial.Lcms.Dataprocess.Algorithm;
using Rfx.Riken.OsakaUniv.MsViewer;
using CompMs.RawDataHandler.Core;
using CompMs.Common.DataObj;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// AifViewerControl.xaml の相互作用ロジック
    /// </summary>
    public partial class AifViewerControl : Window
    {

        public AifViewControlForPeakVM AifViewControlForPeakVM { get; set; }


        public AifViewerControl() {
            InitializeComponent();
        }

        public AifViewerControl(AifViewControlCommonProperties commonProp, FileStream mainDecFS, List<long> mainSeekPoints, int peakID) {
            InitializeComponent();
            this.AifViewControlForPeakVM = new AifViewControlForPeakVM(this, commonProp, mainDecFS, mainSeekPoints, peakID);
        }

        public void PeakFileChange(AifViewControlCommonProperties commonProp, FileStream mainDecFS, List<long> mainSeekPoints, int peakID) {
            this.AifViewControlForPeakVM.PeakFileChange(commonProp, mainDecFS, mainSeekPoints, peakID);

        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e) {
            this.AifViewControlForPeakVM.ClearAll();
        }


        #region Buttons
        private void Button_MsViewerWithRef_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowMsViewer();
        }

        private void Button_Ms2ChromViewer_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowMs2ChromViewer();
        }

        private void Button_PeakLibrarySearch_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowPeakLibrarySearchViewer();
        }

        private void Button_MsFinder_Peak_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowMsFinderExporter();
        }


        private void Button_AlignmentMsViewer_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowAlignmentMsViewer();
        }

        private void Button_AlignmentMs1ChromViewer_Click(object sender, RoutedEventArgs e) {
            this.AifViewControlForPeakVM.ShowAlignmentMs1ChromViewer();
        }
        private void Button_SampleViewer_Click(object sender, RoutedEventArgs e) {
            this.AifViewControlForPeakVM.ShowAlignedSampleTableViewer();
        }

        private void Button_AlignmentLibrarySearch_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowAlignmentLibrarySearchViewer();
        }

        private void Button_CorrelDecViewer_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowCorrelDecRes();
        }

        private void Button_MsFinder_Alignment_Click(object sender, RoutedEventArgs e) {
            AifViewControlForPeakVM.ShowMsFinderExporterForAlignment();
        }

        #endregion

    }

    #region CommonProperties
    public class AifViewControlCommonProperties
    {
        #region member
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

        #endregion
        public ProjectPropertyBean ProjectProperty { get; set; }
        public AnalysisParametersBean Param { get; set; }
        public ObservableCollection<RawSpectrum> Spectrum { get; set; }
        public MsDialIniField MsDialIniField { get; set; }
        public ObservableCollection<AnalysisFileBean> AnalysisFiles { get; set; }
        public AnalysisFileBean AnalysisFile { get; set; }
        public int NumDec { get; set; }
        public List<SolidColorBrush> SolidColorBrushList { get { return solidColorBrushList; } }
        public List<MspFormatCompoundInformationBean> MspDB { get; set; }

        public AifViewControlCommonProperties(ProjectPropertyBean projectProperty, ObservableCollection<AnalysisFileBean> analysisFiles, AnalysisParametersBean param, ObservableCollection<RawSpectrum> spectrum, MsDialIniField msFinderFilePath,
            AnalysisFileBean analysisFile, List<MspFormatCompoundInformationBean> mspDB) {
            ProjectProperty = projectProperty;
            Param = param;
            Spectrum = spectrum;
            MsDialIniField = msFinderFilePath;
            AnalysisFiles = analysisFiles;
            AnalysisFile = analysisFile;
            MspDB = mspDB;
            NumDec = projectProperty.Ms2LevelIdList.Count;
            if(NumDec == 0)
            {
                NumDec = 1;
            }
        }
    }
    #endregion

    public class AifViewControlForPeakTrigger : ViewModelBase
    {
        private bool _trigger;
        private MspFormatCompoundInformationBean _msp;
        private MspFormatCompoundInformationBean _mspAignment;
        public MspFormatCompoundInformationBean LibraryPeak { get { return _msp; } set { _msp = value; OnPropertyChanged("LibraryPeak"); } }
        public MspFormatCompoundInformationBean LibraryAlignment { get { return _mspAignment; } set { _mspAignment = value; OnPropertyChanged("LibraryAlignment"); } }
        public bool LibSearchAlignment { set { _trigger = value; OnPropertyChanged("LibSearchAlignment"); } }
        public bool LibSearchPeak { set { _trigger = value; OnPropertyChanged("LibSearchPeak"); } }
        public bool PeakExportToMsFinder { set { _trigger = value; OnPropertyChanged("PeakExportToMsFinder"); } }
        public bool AlignmentExportToMsFinder { set { _trigger = value; OnPropertyChanged("AlignmentExportToMsFinder"); } }
        public AifViewControlForPeakTrigger() { }
    }

    public class AifViewControlForPeakVM : ViewModelBase
    {

        #region member variables and properties
        private AifViewerControl aifViewerControl;

        private List<FileStream> files;
        private List<List<long>> seekpoints;
        private List<FileStream> filesAlignment;
        private List<List<long>> seekpointsAlignment;
        private List<FileStream> filesCorrel;
        private List<List<long>> seekpointsCorrel;
        private FileStream fileForAlignedData;
        private List<long> seekpointForAlignedData;
        private AlignmentResultBean alignmentResultBean;
        private string alignmentFilePath;
        private int peakID;
        private int alignmentID;

        // viewer
        private MsViewerWithReferenceForAIF msViewerWithReferenceForAIF;
        private Ms2ChromatogramForAIF ms2ChromatogramForAIF;
        private MsmsSearchForAIF msmsSearchForAIF;
        private MassSpectrogramAlignmentForAif massSpectrogramAlignmentForAif;
        private ScrollMs1ChromatogramForAif scrollMs1ChromatogramForAif;
        private CorrelationDecResMsViewer correlDecResMsViewer;
        private SampleTableViewerInAlignment sampleTableViewerInAlignment;
        private MsmsSearchForAIF msmsSearchAlignmentForAIF;

        public AifViewControlForPeakTrigger Checker;

        public AifViewControlCommonProperties CommonProp { get; set; }
        public List<string> NameList { get; set; }
        public List<MS2DecResult> MS2DecResList { get; set; }
        public List<MS2DecResult> MS2DecResListAlignment { get; set; }
        public List<CorrDecResult> CorrelDecResList { get; set; }
        public AlignedData AlignedData { get; set; }
        public int PeakID { get { return peakID; } set { if (peakID == value) return; peakID = value; OnPropertyChanged("PeakID"); } }
        public int AlignmentID { get { return alignmentID; } set { if (alignmentID == value) return; alignmentID = value; OnPropertyChanged("AlignmentID"); } }


        #endregion

        #region constructor
        public AifViewControlForPeakVM(AifViewerControl control, AifViewControlCommonProperties commonProp, FileStream mainDecFS, List<long> mainSeekPoints, int peakID) {
            this.aifViewerControl = control;
            CommonProp = commonProp;
            PeakID = peakID;
            ClearAll();
            NameList = getNameList(CommonProp.ProjectProperty);
            Checker = new AifViewControlForPeakTrigger();
            Checker.PropertyChanged -= checker_propertyChanged;
            Checker.PropertyChanged += checker_propertyChanged;

            initialize(mainDecFS, mainSeekPoints);
            ReadPeakDecRes();
        }
        #endregion

        #region propertyChanged
        private void checker_propertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e) {
            if (e.PropertyName == "LibraryPeak") {
                if (this.msViewerWithReferenceForAIF != null) {
                    this.msViewerWithReferenceForAIF.RefreshReference(this.Checker.LibraryPeak);
                }
            }
            if (e.PropertyName == "LibraryAlignment") {
                if (this.massSpectrogramAlignmentForAif != null) {
                    this.massSpectrogramAlignmentForAif.RefreshReference(this.Checker.LibraryAlignment);
                }
                if (this.correlDecResMsViewer != null) {
                    this.correlDecResMsViewer.RefreshReference(this.Checker.LibraryAlignment);
                }
            }
        }
        #endregion

        #region initialize
        private void initialize(FileStream mainDecFS, List<long> mainSeekPoints) {
            for (var i = 0; i < CommonProp.NumDec; i++) {
                if (i == 0) {
                    files = new List<FileStream>();
                    seekpoints = new List<List<long>>();
                    files.Add(mainDecFS);
                    seekpoints.Add(mainSeekPoints);
                }
                else {
                    var seekpoint = new List<long>();
                    var fs = File.Open(CommonProp.AnalysisFile.AnalysisFilePropertyBean.DeconvolutionFilePathList[i], FileMode.Open, FileAccess.ReadWrite);
                    seekpoint = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                    files.Add(fs);
                    seekpoints.Add(seekpoint);
                }
            }
        }

        // initialize in alignment
        public void ReadAlignmentFileFileStream(AlignmentResultBean alignmentResultBean, string alignmentFilePath, FileStream mainDecFS, List<long> mainSeekPoints) {
            clearAlignmentFiles();
            this.alignmentResultBean = alignmentResultBean;
            this.alignmentFilePath = alignmentFilePath;
            for (var i = 0; i < CommonProp.NumDec; i++) {
                if (i == 0) {
                    filesAlignment = new List<FileStream>();
                    seekpointsAlignment = new List<List<long>>();
                    filesAlignment.Add(mainDecFS);
                    seekpointsAlignment.Add(mainSeekPoints);
                }
                else {
                    var specFilePath = System.IO.Path.GetDirectoryName(alignmentFilePath) + "\\" + System.IO.Path.GetFileNameWithoutExtension(alignmentFilePath) + "." + i + "." + SaveFileFormat.dcl;
                    var seekpoint = new List<long>();
                    var fs = File.Open(specFilePath, FileMode.Open, FileAccess.Read);
                    seekpoint = SpectralDeconvolution.ReadSeekPointsOfMS2DecResultFile(fs);
                    filesAlignment.Add(fs);
                    seekpointsAlignment.Add(seekpoint);
                }
            }
            this.AlignmentID = 0;
            ReadAlignmentDecRes();
            AlignmentFileChangeViewers();
        }

        private void initializeCorrelDec() {
            clearCorrelFiles();
            filesCorrel = new List<FileStream>();
            seekpointsCorrel = new List<List<long>>();

            // for normal correl dec
            for (var i = 0; i < CommonProp.NumDec; i++) {
                var filePath = CommonProp.ProjectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(alignmentFilePath) + "_CorrelationBasedDecRes_Raw_" + i + ".cbd";
                //      var filePath = CommonProp.ProjectProperty.ProjectFolderPath + "\\CorrelationBasedDecRes_Dec_" + i + ".cbd";
                var seekpoint = new List<long>();
                var fs = File.Open(filePath, FileMode.Open);
                seekpoint = CorrDecHandler.ReadSeekPointsOfCorrelDec(fs);
                filesCorrel.Add(fs);
                seekpointsCorrel.Add(seekpoint);
            }
        }

        public void InitializeAlignedData(FileStream fs, List<long> sp) {
            clearAlignedEic();
            this.fileForAlignedData = fs;
            this.seekpointForAlignedData = sp;
            ReadAlignedEic();
        }

        private List<string> getNameList(ProjectPropertyBean projectProperty) {
            var nameList = new List<string>();
            foreach (var id in projectProperty.Ms2LevelIdList) {
                nameList.Add(projectProperty.ExperimentID_AnalystExperimentInformationBean[id].Name);
            }
            if(projectProperty.Ms2LevelIdList.Count == 0)
            {
                nameList.Add("No name");
            }
            return nameList;
        }
        #endregion

        #region change file, peakID, alignmentID
        public void PeakFileChange(AifViewControlCommonProperties commonProp, FileStream mainDecFS, List<long> mainSeekPoints, int peakID) {
            CommonProp = commonProp;
            PeakID = peakID;

            clearPeakFiles();
            initialize(mainDecFS, mainSeekPoints);
            ReadPeakDecRes();
            FileChangeViewers();
        }

        public void ChangePeakSpotId(int id) {
            this.PeakID = id;
            ReadPeakDecRes();
            RefreshPeakViewers();
        }

        public void ChangeAlignmentSpotId(int id) {
            this.AlignmentID = id;
            ReadAlignmentDecRes();
            ReadCorrelDecRes();
            ReadAlignedEic();
            RefreshAlignmentViewers();
        }
        #endregion

        #region read binary by changing peak id
        private void ReadPeakDecRes() {
            if (files == null || files.Count == 0 || seekpoints == null || seekpoints.Count == 0) return;
            MS2DecResList = new List<MS2DecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++) {
                MS2DecResList.Add(SpectralDeconvolution.ReadMS2DecResult(files[i], seekpoints[i], PeakID));
            }
        }

        private void ReadPeakDecRes(int peakId) {
            if (files == null || files.Count == 0 || seekpoints == null || seekpoints.Count == 0) return;
            MS2DecResList = new List<MS2DecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++) {
                MS2DecResList.Add(SpectralDeconvolution.ReadMS2DecResult(files[i], seekpoints[i], peakId));
            }
        }
        private void ReadAlignmentDecRes(int peakId) {
            if (alignmentResultBean == null || filesAlignment == null || filesAlignment.Count == 0 || seekpointsAlignment == null || seekpointsAlignment.Count == 0) return;
            MS2DecResListAlignment = new List<MS2DecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++) {
                MS2DecResListAlignment.Add(SpectralDeconvolution.ReadMS2DecResult(filesAlignment[i], seekpointsAlignment[i], peakId));
            }
        }

        private void ReadCorrelDecRes(int alignmentId)
        {
            if (alignmentResultBean == null || filesCorrel == null || filesCorrel.Count == 0 || seekpointsCorrel == null || seekpointsCorrel.Count == 0) return;
            CorrelDecResList = new List<CorrDecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++)
            {
                CorrelDecResList.Add(CorrDecHandler.ReadCorrelDecResult(filesCorrel[i], seekpointsCorrel[i], alignmentId));
            }
        }


        private void ReadAlignmentDecRes() {
            if (alignmentResultBean == null || filesAlignment == null || filesAlignment.Count == 0 || seekpointsAlignment == null || seekpointsAlignment.Count == 0) return;
            MS2DecResListAlignment = new List<MS2DecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++) {
                MS2DecResListAlignment.Add(SpectralDeconvolution.ReadMS2DecResult(filesAlignment[i], seekpointsAlignment[i], AlignmentID));
            }
        }
        private void ReadCorrelDecRes() {
            if (alignmentResultBean == null || filesCorrel == null || filesCorrel.Count == 0 || seekpointsCorrel == null || seekpointsCorrel.Count == 0) return;
            CorrelDecResList = new List<CorrDecResult>();
            for (var i = 0; i < CommonProp.NumDec; i++) {
                CorrelDecResList.Add(CorrDecHandler.ReadCorrelDecResult(filesCorrel[i], seekpointsCorrel[i], AlignmentID));
            }
        }

        private void ReadAlignedEic() {
            if (alignmentResultBean == null || fileForAlignedData == null || seekpointForAlignedData == null) return;
            AlignedData = AlignedEic.ReadAlignedEicResult(fileForAlignedData, seekpointForAlignedData, alignmentID);
        }
        #endregion

        #region // show viewer
        public void ShowMsViewer() {
            if (msViewerWithReferenceForAIF == null) {
                msViewerWithReferenceForAIF = new MsViewerWithReferenceForAIF(CommonProp, this.MS2DecResList, this.NameList, PeakID);
                msViewerWithReferenceForAIF.Owner = this.aifViewerControl;
                msViewerWithReferenceForAIF.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                msViewerWithReferenceForAIF.Show();
                msViewerWithReferenceForAIF.Closed += closedMethod_msViewer;
            }
            else
                message_exist("MS/MS Viewer");
        }

        public void ShowMs2ChromViewer() {
            if (ms2ChromatogramForAIF == null) {
                ms2ChromatogramForAIF = new Ms2ChromatogramForAIF(CommonProp, this.MS2DecResList, this.NameList, PeakID);
                ms2ChromatogramForAIF.Owner = this.aifViewerControl;
                ms2ChromatogramForAIF.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                ms2ChromatogramForAIF.Show();
                ms2ChromatogramForAIF.Closed += closedMethod_chromViewer;
            }
            else
                message_exist("MS2 Chromatogram Viewer");
        }

        public void ShowPeakLibrarySearchViewer() {
            if (msmsSearchForAIF == null) {
                msmsSearchForAIF = new MsmsSearchForAIF(CommonProp.AnalysisFile, this.PeakID, CommonProp.Param, this.MS2DecResList, CommonProp.MspDB, this.Checker);
                msmsSearchForAIF.Owner = this.aifViewerControl;
                msmsSearchForAIF.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                msmsSearchForAIF.Show();
                msmsSearchForAIF.Closed += closedMethod_msmsSearchForAIF;
            }
            else
                message_exist("Library search viewer in peak spot");
        }

        public void ShowMsFinderExporter() {
            Checker.PeakExportToMsFinder = true;
        }

        public void ShowMsFinderExporterForAlignment() {
            Checker.AlignmentExportToMsFinder = true;
        }


        // alignment
        public void ShowAlignmentMsViewer() {
            if (massSpectrogramAlignmentForAif == null) {
                if (this.alignmentResultBean != null) {
                    massSpectrogramAlignmentForAif = new MassSpectrogramAlignmentForAif(CommonProp, this.alignmentResultBean, this.MS2DecResListAlignment, this.NameList, AlignmentID);
                    massSpectrogramAlignmentForAif.Owner = this.aifViewerControl;
                    massSpectrogramAlignmentForAif.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    massSpectrogramAlignmentForAif.Show();
                    massSpectrogramAlignmentForAif.Closed += closedMethod_alignmentMsViewer;
                }
                else {
                    message_requestAlignment();
                }
            }
            else
                message_exist("Alignment MS/MS Viewer");
        }

        public void ShowAlignmentMs1ChromViewer() {
            if (scrollMs1ChromatogramForAif == null) {
                if (this.alignmentResultBean != null && this.AlignedData != null) {
                    scrollMs1ChromatogramForAif = new ScrollMs1ChromatogramForAif(this.CommonProp, this.AlignedData);
                    scrollMs1ChromatogramForAif.Owner = this.aifViewerControl;
                    scrollMs1ChromatogramForAif.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    scrollMs1ChromatogramForAif.Show();
                    scrollMs1ChromatogramForAif.Closed += closedMethod_alignedEicViewer;
                }
                else {
                    message_requestAlignment();
                }
            }
            else
                message_exist("Alignment Ms1 chromatogram viewer");
        }

        public void ShowAlignmentLibrarySearchViewer() {
            if (msmsSearchAlignmentForAIF == null) {
                if (this.alignmentResultBean != null) {
                    msmsSearchAlignmentForAIF = new MsmsSearchForAIF(this.alignmentResultBean, this.AlignmentID, CommonProp.Param, this.MS2DecResListAlignment, CommonProp.MspDB, this.Checker);
                    msmsSearchAlignmentForAIF.Owner = this.aifViewerControl;
                    msmsSearchAlignmentForAIF.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    msmsSearchAlignmentForAIF.Show();
                    msmsSearchAlignmentForAIF.Closed += closedMethod_msmsSearchAlignmentForAIF;
                }
                else {
                    message_requestAlignment();
                }
            }
            else
                message_exist("Library search viewer in alignment spot");
        }

        public void ShowAlignedSampleTableViewer() {
            if (sampleTableViewerInAlignment == null) {
                if (this.alignmentResultBean != null) {
                    var source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.alignmentResultBean.AlignmentPropertyBeanCollection[AlignmentID],
                        this.AlignedData, CommonProp.AnalysisFiles, CommonProp.ProjectProperty, CommonProp.Param, CommonProp.SolidColorBrushList);
                    sampleTableViewerInAlignment = new SampleTableViewerInAlignment(source);
                    sampleTableViewerInAlignment.Owner = this.aifViewerControl;
                    sampleTableViewerInAlignment.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                    sampleTableViewerInAlignment.Show();
                    sampleTableViewerInAlignment.Closed += closedMethod_alignedSampleTableViewer;
                }
                else {
                    message_requestAlignment();
                }
            }
            else
                message_exist("Aligned sample table viewer");
        }


        public void ShowCorrelDecRes() {
            if (correlDecResMsViewer == null) {
                if (this.alignmentResultBean != null) {
                    var filePath = CommonProp.ProjectProperty.ProjectFolderPath + "\\" + System.IO.Path.GetFileNameWithoutExtension(alignmentFilePath) + "_CorrelationBasedDecRes_Raw_0.cbd";
                    if (File.Exists(filePath)) {
                        initializeCorrelDec();
                        ReadCorrelDecRes();
                        correlDecResMsViewer = new CorrelationDecResMsViewer(CommonProp, this.alignmentResultBean, this.CorrelDecResList, this.NameList, AlignmentID);
                        if(this.aifViewerControl.IsActive)
                            correlDecResMsViewer.Owner = this.aifViewerControl;
                        correlDecResMsViewer.Title = "Correlation-based deconvolution";
                        correlDecResMsViewer.WindowStartupLocation = WindowStartupLocation.CenterScreen;
                        correlDecResMsViewer.Show();
                        correlDecResMsViewer.Closed += closedMethod_correlDecResMsViewer;
                    }
                    else {
                        if(this.CommonProp.AnalysisFiles.Count > 3)
                            MessageBox.Show("Please run correlation based deconvolution. \n(Menu -> Data Processing -> Correlation based deconvolution)", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        else {
                            MessageBox.Show("Sorry, it requires >3 samples", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        }
                    }
                }
                else {
                    message_requestAlignment();
                }
            }
            else
                message_exist("CorrelDecRes Viewer");

        }

        #endregion

        #region // Refresh
        private void FileChangeViewers() {
            if (msViewerWithReferenceForAIF != null) {
                msViewerWithReferenceForAIF.FileChange(CommonProp, MS2DecResList, PeakID);
            }
            if (ms2ChromatogramForAIF != null) {
                ms2ChromatogramForAIF.FileChange(CommonProp, MS2DecResList, PeakID);
            }
        }

        private void AlignmentFileChangeViewers() {
            if (massSpectrogramAlignmentForAif != null) {
                massSpectrogramAlignmentForAif.FileChange(CommonProp, this.alignmentResultBean, MS2DecResListAlignment, AlignmentID);
            }
            if (correlDecResMsViewer != null) {
                correlDecResMsViewer.FileChange(CommonProp, CorrelDecResList, AlignmentID);
            }
            if (scrollMs1ChromatogramForAif != null) {
                scrollMs1ChromatogramForAif.FileChange(CommonProp, AlignedData, AlignmentID);
            }
            if (sampleTableViewerInAlignment != null) {
                var source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.alignmentResultBean.AlignmentPropertyBeanCollection[AlignmentID],
                    this.AlignedData, CommonProp.AnalysisFiles, CommonProp.ProjectProperty, CommonProp.Param, CommonProp.SolidColorBrushList);
                sampleTableViewerInAlignment.ChangeSource(source);

            }

        }

        private void RefreshPeakViewers() {
            if (msViewerWithReferenceForAIF != null) {
                msViewerWithReferenceForAIF.Refresh(MS2DecResList, PeakID);
            }
            if (ms2ChromatogramForAIF != null) {
                ms2ChromatogramForAIF.Refresh(MS2DecResList, PeakID);
            }
            if (msmsSearchForAIF != null) {
                System.Diagnostics.Debug.WriteLine("control: " + PeakID);
                msmsSearchForAIF.Refresh(MS2DecResList, PeakID);
            }
        }

        private void RefreshAlignmentViewers() {
            if (massSpectrogramAlignmentForAif != null) {
                massSpectrogramAlignmentForAif.Refresh(MS2DecResListAlignment, AlignmentID);
            }
            if (scrollMs1ChromatogramForAif != null) {
                scrollMs1ChromatogramForAif.Refresh(AlignedData, AlignmentID);
            }
            if (msmsSearchAlignmentForAIF != null) {
                System.Diagnostics.Debug.WriteLine("control: " + PeakID);
                msmsSearchAlignmentForAIF.Refresh(MS2DecResListAlignment, AlignmentID);
            }

            if (sampleTableViewerInAlignment != null) {
                var source = UtilityForAIF.GetSourceOfAlignedSampleTableViewer(this.alignmentResultBean.AlignmentPropertyBeanCollection[AlignmentID],
                    this.AlignedData, CommonProp.AnalysisFiles, CommonProp.ProjectProperty, CommonProp.Param, CommonProp.SolidColorBrushList);
                sampleTableViewerInAlignment.ChangeSource(source);
            }
            if (correlDecResMsViewer != null) {
                correlDecResMsViewer.Refresh(CorrelDecResList, AlignmentID);
            }

        }
        #endregion

        #region Clear
        public void ClearAll() {
            clearPeakFiles();
            clearAlignmentFiles();
            clearCorrelFiles();
            clearAlignedEic();

            // peak spot
            if (msViewerWithReferenceForAIF != null)
                msViewerWithReferenceForAIF.Close();
            if (ms2ChromatogramForAIF != null)
                ms2ChromatogramForAIF.Close();
            if (msmsSearchForAIF != null)
                msmsSearchForAIF.Close();

            // alignment
            if (massSpectrogramAlignmentForAif != null)
                massSpectrogramAlignmentForAif.Close();
            if (scrollMs1ChromatogramForAif != null)
                scrollMs1ChromatogramForAif.Close();
            if (correlDecResMsViewer != null)
                correlDecResMsViewer.Close();
            if (msmsSearchAlignmentForAIF != null)
                msmsSearchAlignmentForAIF.Close();
            if (sampleTableViewerInAlignment != null)
                sampleTableViewerInAlignment.Close();
        }

        private void clearPeakFiles() {
            if (files == null || files.Count == 0 || seekpoints == null || seekpoints.Count == 0) return;
            for (var i = 1; i < CommonProp.NumDec; i++) {
                if (files[i] != null) {
                    files[i].Close(); files[i].Dispose();
                }
                seekpoints[i] = new List<long>();
            }
            files = new List<FileStream>();
            seekpoints = new List<List<long>>();
            MS2DecResList = new List<MS2DecResult>();

        }

        private void clearAlignmentFiles() {
            if (filesAlignment == null || filesAlignment.Count == 0 || seekpointsAlignment == null || seekpointsAlignment.Count == 0) return;
            for (var i = 1; i < CommonProp.NumDec; i++) {
                if (filesAlignment[i] != null) {
                    filesAlignment[i].Close(); filesAlignment[i].Dispose();
                }
                seekpointsAlignment[i] = new List<long>();
            }
            filesAlignment = new List<FileStream>();
            seekpointsAlignment = new List<List<long>>();
            MS2DecResListAlignment = new List<MS2DecResult>();
        }

        private void clearCorrelFiles() {
            if (filesCorrel == null || filesCorrel.Count == 0 || seekpointsCorrel == null || seekpointsCorrel.Count == 0) return;
            for (var i = 0; i < CommonProp.NumDec; i++) {
                if (filesCorrel[i] != null) {
                    filesCorrel[i].Dispose(); filesCorrel[i].Close();
                    seekpointsCorrel[i] = new List<long>();
                }
            }
            filesCorrel = new List<FileStream>();
            seekpointsCorrel = new List<List<long>>();
            CorrelDecResList = new List<CorrDecResult>();
        }

        private void clearAlignedEic() {
            fileForAlignedData = null;
            seekpointForAlignedData = new List<long>();
            AlignedData = null;
        }
        #endregion

        #region ClosedMethods
        private void closedMethod_msViewer(object sender, EventArgs e) {
            this.msViewerWithReferenceForAIF = null;
        }

        private void closedMethod_chromViewer(object sender, EventArgs e) {
            this.ms2ChromatogramForAIF = null;
        }
        private void closedMethod_msmsSearchForAIF(object sender, EventArgs e) {
            this.msmsSearchForAIF = null;
        }


        private void closedMethod_alignmentMsViewer(object sender, EventArgs e) {
            this.massSpectrogramAlignmentForAif = null;
        }
        private void closedMethod_msmsSearchAlignmentForAIF(object sender, EventArgs e) {
            this.msmsSearchAlignmentForAIF = null;
        }
        private void closedMethod_alignedEicViewer(object sender, EventArgs e) {
            this.scrollMs1ChromatogramForAif = null;
        }
        private void closedMethod_alignedSampleTableViewer(object sender, EventArgs e) {
            this.sampleTableViewerInAlignment = null;
        }
        private void closedMethod_correlDecResMsViewer(object sender, EventArgs e) {
            this.correlDecResMsViewer = null;
        }

        #endregion

        #region public methods

        #region multiple export to MsFinder
        public void BulkExportToMsFinder(ObservableCollection<PeakSpotRow> source, string exportDir, PeakSpotTableViewer viewer) {
            var w = new ShortMessageWindow("Exporting...,\nit takes time depending on the number of target peaks");
            w.Width = 500;
            w.Owner = viewer;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
            Mouse.OverrideCursor = Cursors.Wait;
            viewer.IsEnabled = false;
            this.aifViewerControl.IsEnabled = false;


            var iniField = CommonProp.MsDialIniField;
            if (!UtilityForAIF.CheckFilePath(iniField)) return;
            var peakId = this.PeakID;
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);
            foreach (var row in source) {
                if (row.Checked) {
                    ReadPeakDecRes(row.PeakID);
                    var peakArea = row.PeakAreaBean;
                    var fileString = CommonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
                    for (var i = 0; i < CommonProp.NumDec; i++) {
                        var filePath = exportDir + "\\" + "Dec" + "_" + Math.Round(peakArea.RtAtPeakTop, 2).ToString("00.00") + "_" + Math.Round(peakArea.AccurateMass, 2).ToString("000.000") +
                            "_" + NameList[i] + "_" + fileString + "_" + timeString + "." + SaveFileFormat.mat;
                        UtilityForAIF.SendToMsFinderProgram(this.CommonProp, this.MS2DecResList[i], peakArea, i, filePath);
                    }
                }
            }
            ReadPeakDecRes(peakId);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = CommonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = exportDir;
            process.Start();

            viewer.IsEnabled = true;
            this.aifViewerControl.IsEnabled = true;
            w.Close();
            Mouse.OverrideCursor = null;
        }

        public void BulkExportToMsFinder(ObservableCollection<AlignmentSpotRow> source, string exportDir, AlignmentSpotTableViewer viewer) {
            var w = new ShortMessageWindow("Exporting...,\nit takes time depending on the number of target peaks");
            w.Width = 500;
            w.Owner = viewer;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
            Mouse.OverrideCursor = Cursors.Wait;
            viewer.IsEnabled = false;
            this.aifViewerControl.IsEnabled = false;


            var iniField = CommonProp.MsDialIniField;
            if (!UtilityForAIF.CheckFilePath(iniField)) return;
            var alignmentId = this.AlignmentID;
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            initializeCorrelDec();

            foreach (var row in source) {
                if (row.Checked) {
                    ReadAlignmentDecRes(row.AlignmentPropertyBean.AlignmentID);
                    ReadCorrelDecRes(row.AlignmentPropertyBean.AlignmentID);
                    for (var i = 0; i < CommonProp.NumDec; i++)
                    {
                        var fileString = CommonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
                        var filePath = exportDir + "\\" + "Alignment" + "_" + Math.Round(row.AlignmentPropertyBean.CentralRetentionTime, 2).ToString("00.00") + "_" +
                            Math.Round(row.AlignmentPropertyBean.CentralAccurateMass, 2).ToString("000.000") + "_" + NameList[i] + "_" + fileString + "_" + timeString + "." + SaveFileFormat.mat;

                        //UtilityForAIF.SendToMsFinderProgram(this.CommonProp, this.MS2DecResListAlignment[i], row.AlignmentPropertyBean, i, filePath);
                        if (CorrelDecResList.Count > 0 && CorrelDecResList[i] != null)
                        {
                            UtilityForAIF.SendToMsFinderProgram(this.CommonProp, this.CorrelDecResList[i], row.AlignmentPropertyBean, i, filePath);
                        }
                    }
                }
            }
            ReadAlignmentDecRes(alignmentId);
            var process = new System.Diagnostics.Process();
            process.StartInfo.FileName = CommonProp.MsDialIniField.MsfinderFilePath;
            process.StartInfo.Arguments = exportDir;
            process.Start();

            viewer.IsEnabled = true;
            this.aifViewerControl.IsEnabled = true;
            w.Close();
            Mouse.OverrideCursor = null;
        }

        #endregion

        #region multiple save as msp format
        public void MultipleSaveAsMsp(ObservableCollection<PeakSpotRow> source, string exportDir, PeakSpotTableViewer viewer) {
            var w = new ShortMessageWindow("Exporting...,\nit takes time depending on the number of target peaks");
            w.Width = 500;
            w.Owner = viewer;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
            Mouse.OverrideCursor = Cursors.Wait;
            viewer.IsEnabled = false;
            this.aifViewerControl.IsEnabled = false;

            var peakId = this.PeakID;
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);
            var filePath = exportDir + "\\" + "Dec" +  "_" + timeString + "_" + CommonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName  + ".msp";
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {

                foreach (var row in source) {
                    if (row.Checked) {
                        ReadPeakDecRes(row.PeakID);
                        var peakArea = row.PeakAreaBean;
                        var fileString = CommonProp.AnalysisFile.AnalysisFilePropertyBean.AnalysisFileName;
                        for (var i = 0; i < CommonProp.NumDec; i++) {
                            if (MS2DecResList[i].MassSpectra.Count == 0) continue;
                            if (MS2DecResList[i].MassSpectra.Count(x => x[1] >= 0.5) == 0) continue;
                            UtilityForAIF.writeMsAnnotationTag(sw, this.CommonProp.ProjectProperty, this.CommonProp.MspDB, this.MS2DecResList[i], peakArea, i, CommonProp.AnalysisFile, true);
                            sw.WriteLine("");
                        }
                    }
                }
            }
            ReadPeakDecRes(peakId);

            viewer.IsEnabled = true;
            this.aifViewerControl.IsEnabled = true;
            w.Close();
            Mouse.OverrideCursor = null;
        }

        public void MultipleSaveAsMsp(ObservableCollection<AlignmentSpotRow> source, string exportDir, AlignmentSpotTableViewer viewer) {
            var w = new ShortMessageWindow("Exporting...,\nit takes time depending on the number of target peaks");
            w.Width = 500;
            w.Owner = viewer;
            w.WindowStartupLocation = WindowStartupLocation.CenterOwner;
            w.Show();
            Mouse.OverrideCursor = Cursors.Wait;
            viewer.IsEnabled = false;
            this.aifViewerControl.IsEnabled = false;

            if (!Directory.Exists(exportDir)) Directory.CreateDirectory(exportDir);

            var alignmentId = this.AlignmentID;
            var timeString = DateTime.Now.ToString("yy_MM_dd_HH_mm");
            var filePath = exportDir + "\\" + "Alignment" + "_" + timeString  + "_" + System.IO.Path.GetFileNameWithoutExtension(alignmentFilePath) + "_" + ".msp";
            initializeCorrelDec();
            using (StreamWriter sw = new StreamWriter(filePath, false, Encoding.ASCII)) {
                foreach (var row in source) {
                    if (row.Checked) {
                        ReadAlignmentDecRes(row.AlignmentPropertyBean.AlignmentID);
                        ReadCorrelDecRes(row.AlignmentPropertyBean.AlignmentID);
                        for (var i = 0; i < CommonProp.NumDec; i++) {
                            if (MS2DecResList[i].MassSpectra.Count == 0) continue;
                            if (MS2DecResList[i].MassSpectra.Count(x => x[1] >= 0.5) == 0) continue;
                            var masslist = CorrDecHandler.GetCorrDecSpectrumWithComment(this.CorrelDecResList[i], CommonProp.Param.AnalysisParamOfMsdialCorrDec, row.AlignmentPropertyBean.CentralAccurateMass, (int)(row.AlignmentPropertyBean.AlignedPeakPropertyBeanCollection.Count * row.AlignmentPropertyBean.FillParcentage));
                            if (masslist.Count == 0) continue;
                            UtilityForAIF.writeMsAnnotationTag(sw, CommonProp.ProjectProperty, this.CommonProp.MspDB, masslist, row.AlignmentPropertyBean, CommonProp.AnalysisFiles, i, true);
                            sw.WriteLine("");
                        }
                    }
                }
            }
            ReadAlignmentDecRes(alignmentId);

            viewer.IsEnabled = true;
            this.aifViewerControl.IsEnabled = true;
            w.Close();
            Mouse.OverrideCursor = null;
        }

        #endregion
        // export MS/MS similarity scores from mainWindow
        public void ExportMSMSScoresFromMainWindow(ObservableCollection<AlignmentPropertyBean> collections) {
            using (var sw = new StreamWriter(@"D:\1_study\ETYPE\181010_exportRes_B1.txt", false, Encoding.UTF8)) {
                foreach (var bean in collections) {
                    if (bean.Comment.Contains("B1")) {
                        this.AlignmentID = bean.AlignmentID;
                        initializeCorrelDec();
                        ReadAlignmentDecRes();
                        ReadCorrelDecRes();
                        var meta = bean.MetaboliteName + "\t" + bean.CentralAccurateMass + "\t" + bean.CentralRetentionTime + "\t" + bean.IdentificationRank + "\t" + bean.CorrelDecIdentificationRank + "\t" + bean.Comment;
                        for (var i = 0; i < this.CommonProp.NumDec; i++) {
                            var ms2dec = this.MS2DecResListAlignment[i].MassSpectra;
                            var scores1 = new List<float>();
                            if (bean.LibraryIdList[i] < 0) {
                                scores1 = nullScores();
                                var s1 = string.Join("\t", scores1);
                                sw.WriteLine("\tMS2Dec\t" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "\tNoHit\t" + ms2dec.Count + "\t-1\t" + s1);
                            }
                            else {
                                scores1 = UtilityForAIF.GetScores(this.CommonProp.Param, this.CommonProp.MspDB[bean.LibraryIdList[i]], new ObservableCollection<double[]>(ms2dec), bean.CentralAccurateMass, bean.CentralRetentionTime);
                                var s1 = string.Join("\t", scores1);
                                sw.Write(meta + "\tMS2Dec\t" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "\t" + this.CommonProp.MspDB[bean.LibraryIdList[i]].Name + "\t" + ms2dec.Count + "\t" + this.CommonProp.MspDB[bean.LibraryIdList[i]].PeakNumber + "\t" + s1);
                            }

                            var peakMatrix = this.CorrelDecResList[i].PeakMatrix;
                            var ms2peaks = new ObservableCollection<double[]>();
                            foreach (var p in peakMatrix) {
                                if (p.Score > 2 && p.Intensity > 0.5 && p.Count > 0.5 * bean.AlignedPeakPropertyBeanCollection.Count * bean.FillParcentage) {
                                    ms2peaks.Add(new double[] { p.Mz, p.Intensity });
                                }
                            }
                            var scores2 = new List<float>();
                            if (bean.CorrelBasedlibraryIdList[i] < 0) {
                                scores2 = nullScores();
                                var s2 = string.Join("\t", scores2);
                                sw.WriteLine("\tCbD\t" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "\tNoHit\t" + ms2peaks.Count + "\t-1\t" + s2);
                            }
                            else {
                                scores2 = UtilityForAIF.GetScores(this.CommonProp.Param, this.CommonProp.MspDB[bean.CorrelBasedlibraryIdList[i]], new ObservableCollection<double[]>(ms2peaks), bean.CentralAccurateMass, bean.CentralRetentionTime);
                                var s2 = string.Join("\t", scores2);
                                sw.WriteLine("\tCbD\t" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "\t" + this.CommonProp.MspDB[bean.CorrelBasedlibraryIdList[i]].Name + "\t" + ms2peaks.Count + "\t" + this.CommonProp.MspDB[bean.CorrelBasedlibraryIdList[i]].PeakNumber + "\t" + s2);

                            }
                        }
                    }
                }
            }
        }

        // export all figures from alignment spots with annotation
        /*
         * public void ExportFiguresFromMainWindow(ObservableCollection<AlignmentPropertyBean> collections) {
            if (!System.IO.Directory.Exists(CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp"))
                System.IO.Directory.CreateDirectory(CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp");

            foreach (var bean in collections) {
                this.AlignmentID = bean.AlignmentID;
                initializeCorrelDec();
                ReadAlignmentDecRes();
                ReadCorrelDecRes();
                for (var i = 0; i < this.CommonProp.NumDec; i++) {
                    var ms2dec = MS2ExporterAsUserDefinedStyle.GetMsListFromDecRes(this.MS2DecResListAlignment[i]);
                    var ref1 = new List<float[]>();
                    if (bean.LibraryIdList[i] < 0) {
                        ref1.Add(new float[] { 0, 0 });
                    }
                    else {
                        foreach (var peak in this.CommonProp.MspDB[bean.LibraryIdList[i]].MzIntensityCommentBeanList) {
                            ref1.Add(new float[] { peak.Mz, peak.Intensity });
                        }
                    }

                    var dv = MS2ExporterAsUserDefinedStyle.ExportMS2WithRef(ms2dec, ref1);
                    MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_MS2Dec.emf");
                    MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_MS2Dec.png");

                    var peakMatrix = this.CorrelDecResList[i].PeakMatrix;
                    var ms2peaks = new List<float[]>();
                    foreach (var p in peakMatrix) {
                        if (p.Score > 2 && p.Intensity > 0.5 && p.Count > 0.5 * bean.AlignedPeakPropertyBeanCollection.Count * bean.FillParcentage) {
                            ms2peaks.Add(new float[] { (float)p.Mz, (float)p.Intensity });
                        }
                    }
                    var ref2 = new List<float[]>();
                    if (bean.CorrelBasedlibraryIdList[i] < 0) {
                        ref2.Add(new float[] { 0, 0 });
                    }
                    else {
                        foreach (var peak in this.CommonProp.MspDB[bean.CorrelBasedlibraryIdList[i]].MzIntensityCommentBeanList) {
                            ref2.Add(new float[] { peak.Mz, peak.Intensity });
                        }
                    }
                    var dv2 = MS2ExporterAsUserDefinedStyle.ExportMS2WithRef(ms2peaks, ref2);
                    MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv2, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "CorrDec.emf");
                    MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv2, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "CorrDec.png");

                }
            }
        }

        // Export particular peak info
        public void ExportFiguresFromMainWindow_particularPeakSpot(ObservableCollection<AlignmentPropertyBean> collections) {
            if (!System.IO.Directory.Exists(CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp"))
                System.IO.Directory.CreateDirectory(CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp");

            var targetID = 6;
            var minX = 40;
            var maxX = 200;
            foreach (var bean in collections) {
                this.AlignmentID = bean.AlignmentID;
                if (this.AlignmentID != targetID) continue;
                initializeCorrelDec();
                ReadAlignmentDecRes();
                ReadCorrelDecRes();
                for (var i = 0; i < this.CommonProp.NumDec; i++) {
                    var ms2dec = MS2ExporterAsUserDefinedStyle.GetMsListFromDecRes(this.MS2DecResListAlignment[i]);
                    var ref1 = new List<float[]>();
                    if (bean.LibraryIdList[i] < 0) {
                        ref1.Add(new float[] { 0, 0 });
                    }
                    else {
                        foreach (var peak in this.CommonProp.MspDB[bean.LibraryIdList[i]].MzIntensityCommentBeanList) {
                            ref1.Add(new float[] { peak.Mz, peak.Intensity });
                        }
                    }

                    var dv = MS2ExporterAsUserDefinedStyle.ExportMS2WithRef(ms2dec, ref1);
                    dv.MinX = minX;
                    dv.MaxX = maxX;
                    MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_MS2Dec.emf");
                    MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_MS2Dec.png");

                    var peakMatrix = this.CorrelDecResList[i].PeakMatrix;
                    var ms2peaks = new List<float[]>();
                    foreach (var p in peakMatrix) {
                        Console.WriteLine("pcoount: " + p.Count);
                        if (p.Score > 2 && p.Intensity > 0.5 && p.Count > 0.5 * bean.AlignedPeakPropertyBeanCollection.Count * bean.FillParcentage) {
                            ms2peaks.Add(new float[] { (float)p.Mz, (float)p.Intensity });
                        }
                    }
                    var ref2 = new List<float[]>();
                    if (bean.CorrelBasedlibraryIdList[i] < 0) {
                        ref2.Add(new float[] { 0, 0 });
                    }
                    else {
                        foreach (var peak in this.CommonProp.MspDB[bean.CorrelBasedlibraryIdList[i]].MzIntensityCommentBeanList) {
                            ref2.Add(new float[] { peak.Mz, peak.Intensity });
                        }
                    }
                    var dv2 = MS2ExporterAsUserDefinedStyle.ExportMS2WithRef(ms2peaks, ref2);
                    dv2.MinX = minX;
                    dv2.MaxX = maxX;
                    MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv2, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "CorrDec.emf");
                    MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv2, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "CorrDec.png");

                    var ms2peaks2 = new List<float[]>();
                    foreach (var p in peakMatrix) {
                        ms2peaks2.Add(new float[] { (float)p.Mz, (float)p.Intensity });
                    }
                    var dv3 = MS2ExporterAsUserDefinedStyle.ExportMS2(ms2peaks2);
                    dv3.MinX = minX;
                    dv3.MaxX = maxX;
                    MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv3, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_combined_CorrDec.emf");
                    MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv3, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_combined_CorrDec.png", 500, 150);


                }
            }
            var peakID = 7;
            for (var i = 0; i < this.CommonProp.NumDec; i++) {

                var datapoint = CommonProp.AnalysisFile.PeakAreaBeanCollection[peakID].Ms2LevelDatapointNumberList[i];
                var spectrum = CommonProp.Spectrum[datapoint];
                var ms2peaks = new List<float[]>();
                foreach (var p in spectrum.Spectrum) {
                    if (p.Mz < maxX)
                        ms2peaks.Add(new float[] { (float)p.Mz, (float)p.Intensity });
                }
                var dv3 = MS2ExporterAsUserDefinedStyle.ExportMS2(ms2peaks);
                dv3.MinX = minX;
                dv3.MaxX = maxX;
                MS2ExporterAsUserDefinedStyle.SaveChartAsEmf(dv3, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_raw.emf");
                MS2ExporterAsUserDefinedStyle.SaveChartAsPng(dv3, CommonProp.ProjectProperty.ProjectFolderPath + "\\tmp\\ID" + this.AlignmentID + "_CE" + CommonProp.ProjectProperty.CollisionEnergyList[i] + "_raw.png", 500, 150);

            }

        }
        */
        private static List<float> nullScores() {
            var res = new List<float>();
            for (var i = 0; i < 8; i++) {
                res.Add(-1);
            }
            return res;
        }

        #endregion



        #region private methods
        private void message_exist(string txt) {
            MessageBox.Show(txt + " already exists.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void message_requestAlignment() {
            MessageBox.Show("Choose an alignment file from the file navigator.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        private void message_willBeAvailable() {
            MessageBox.Show("This button will be available soon.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
        }

        #endregion
    }
}
