using CompMs.Common.MessagePack;
using Msdial.Lcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
	public class AnalysisParamSetForLcVM: ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        private ObservableCollection<AdductIonInformationVM> adductIonVMs;
        private ObservableCollection<ExcludeMassVM> excludedMassVMs;
        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;

        private string libraryFilePath;
        private string libraryFilePathCopy;

        private string postIdentificationLibraryFilePath;
        private string postIdentificationLibraryFilePathCopy;

        private string targetFormulaLibraryFilePath;
        private string targetFormulaLibraryFilePathCopy;

        private string alignmentResultFileName;

        // ion mobility
        private bool isTIMS;
        private bool isDTIMS;
        private bool isTWIMS;
        private string mobilityUnit;

        public AnalysisParamSetForLcVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.analysisFiles = mainWindow.AnalysisFiles;
            this.alignmentFiles = mainWindow.AlignmentFiles;

            this.libraryFilePath = ProjectProperty.LibraryFilePath;
            this.libraryFilePathCopy = ProjectProperty.LibraryFilePath;

            this.postIdentificationLibraryFilePath = ProjectProperty.PostIdentificationLibraryFilePath;
            this.postIdentificationLibraryFilePathCopy = ProjectProperty.PostIdentificationLibraryFilePath;

            this.targetFormulaLibraryFilePath = ProjectProperty.TargetFormulaLibraryFilePath;
            this.targetFormulaLibraryFilePathCopy = ProjectProperty.TargetFormulaLibraryFilePath;

            this.adductIonVMs = new ObservableCollection<AdductIonInformationVM>();
            if (Param.AdductIonInformationBeanList == null) {
                Param.AdductIonInformationBeanList = AdductResourceParser.GetAdductIonInformationList(ProjectProperty.IonMode);
            }

            if (Param.QcAtLeastFilter == true) {
                Param.QcAtLeastFilter = false;
            }

            for (int i = 0; i < Param.AdductIonInformationBeanList.Count; i++)
                this.adductIonVMs.Add(new AdductIonInformationVM() { AdductIonInformationBean = Param.AdductIonInformationBeanList[i] });
            
            this.excludedMassVMs = new ObservableCollection<ExcludeMassVM>();
            for (int i = 0; i < 200; i++)
                this.excludedMassVMs.Add(new ExcludeMassVM());

            if (Param.ExcludedMassList != null && Param.ExcludedMassList.Count != 0)
            {
                for (int i = 0; i < Param.ExcludedMassList.Count; i++)
                {
                    this.excludedMassVMs[i].ExcludedMass = Param.ExcludedMassList[i].ExcludedMass;
                    this.excludedMassVMs[i].MassTolerance = Param.ExcludedMassList[i].MassTolerance;
                }
            }
            if (Param.IonMobilityType == IonMobilityType.Tims) {
                this.IsTIMS = true; this.IsDTIMS = false; this.IsTWIMS = false; this.MobilityUnit = "1/k0";
            }
            else if (Param.IonMobilityType == IonMobilityType.Dtims) {
                this.IsTIMS = false; this.IsDTIMS = true; this.IsTWIMS = false; this.MobilityUnit = "Drift time (msec)";
            }
            else if (Param.IonMobilityType == IonMobilityType.Twims) {
                this.IsTIMS = false; this.IsDTIMS = false; this.IsTWIMS = true; this.MobilityUnit = "Drift time (msec)";
            }
            else {
                this.IsTIMS = true; this.IsDTIMS = false; this.IsTWIMS = false; this.MobilityUnit = "1/k0";
            }

            var dt = DateTime.Now;
            this.alignmentResultFileName = "alignmentResult" + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;
        }

        public void UpdateExcludedMassList(AnalysisParametersBean param)
        {
            if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0)
            {
                for (int i = 0; i < param.ExcludedMassList.Count; i++)
                {
                    this.excludedMassVMs[i].ExcludedMass = param.ExcludedMassList[i].ExcludedMass;
                    this.excludedMassVMs[i].MassTolerance = param.ExcludedMassList[i].MassTolerance;
                }
            }
        }

        public void UpdateAdductList(AnalysisParametersBean param)
        {
            if (param.AdductIonInformationBeanList != null && param.AdductIonInformationBeanList.Count != 0)
            {
                this.adductIonVMs = new ObservableCollection<AdductIonInformationVM>();
                for (int i = 0; i < param.AdductIonInformationBeanList.Count; i++)
                    this.adductIonVMs.Add(new AdductIonInformationVM() { AdductIonInformationBean = param.AdductIonInformationBeanList[i] });
            }
        }

        public void UpdateAdductListByUser(List<AdductIonInformationBean> updateAdductList)
        {
            var updateAdduct = updateAdductList[updateAdductList.Count - 1];

            var updateAdductCollection = new ObservableCollection<AdductIonInformationVM>();
            foreach (var adduct in this.adductIonVMs) updateAdductCollection.Add(adduct);

            updateAdductCollection.Add(new AdductIonInformationVM() { AdductIonInformationBean = updateAdduct });
            AdductIonInformationViewModelCollection = updateAdductCollection;
        }

        protected async override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            Mouse.OverrideCursor = Cursors.Wait;

            var message = new ShortMessageWindow() {
                Owner = this.window,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            if (this.ExcuteRtCorrection)
                message.Label_MessageTitle.Text = "RT correction viewer will be opened\r\nafter libraries are loaded.";
            else
                message.Label_MessageTitle.Text = "Loading libraries..";
            message.Show();

            if (await ClosingMethod() == true)
            {
                message.Close();
                window.DialogResult = true;
                window.Close();
            }
            else {
                message.Close();
            }
            Mouse.OverrideCursor = null;
        }

        private async Task<bool> ClosingMethod()
        {
            if (adductIonVMs[0].AdductIonInformationBean.Included == false)
            {
                MessageBox.Show("M + H or M - H must be included.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (Param.MaxChargeNumber <= 0) {
                Param.MaxChargeNumber = 2;
            }

            Param.ExcludedMassList = new List<ExcludeMassBean>();
            for (int i = 0; i < excludedMassVMs.Count; i++)
            {
                if (this.excludedMassVMs[i].ExcludedMass == null || this.excludedMassVMs[i].MassTolerance == null) continue;
                if (this.excludedMassVMs[i].ExcludedMass <= 0 || this.excludedMassVMs[i].MassTolerance <= 0) continue;
                Param.ExcludedMassList.Add(new ExcludeMassBean() { ExcludedMass = this.excludedMassVMs[i].ExcludedMass, MassTolerance = this.excludedMassVMs[i].MassTolerance });
            }

            Param.AdductIonInformationBeanList = new List<AdductIonInformationBean>();
            for (int i = 0; i < adductIonVMs.Count; i++)
                Param.AdductIonInformationBeanList.Add(adductIonVMs[i].AdductIonInformationBean);

            if (this.libraryFilePath != null && this.libraryFilePath != "" && this.libraryFilePath != this.libraryFilePathCopy &&
                ProjectProperty.TargetOmics == TargetOmics.Metablomics)
            {
                ProjectProperty.LibraryFilePath = this.libraryFilePath;
                if (Path.GetExtension(this.libraryFilePath) == ".msp")
                {
                    this.mainWindow.MspDB = DatabaseLcUtility.GetMspDbQueries(this.libraryFilePath, this.mainWindow.IupacReference);
                } else if(Path.GetExtension(this.libraryFilePath) == ".msp2")
                {
                    this.mainWindow.MspDB = MspMethods.LoadMspFromFile(this.libraryFilePath);
                }
                else
                {
                    ProjectProperty.LibraryFilePath = string.Empty;
                    this.mainWindow.MspDB = new List<MspFormatCompoundInformationBean>();
                }
            }
            else if ((this.libraryFilePath == null || this.libraryFilePath == "") && this.mainWindow.ProjectProperty.TargetOmics == TargetOmics.Metablomics)
            {
                ProjectProperty.LibraryFilePath = string.Empty;
                this.mainWindow.MspDB = new List<MspFormatCompoundInformationBean>();
            }
            else if (ProjectProperty.TargetOmics == TargetOmics.Lipidomics)
            {
                string mainDirectory = System.IO.Path.GetDirectoryName(System.Windows.Forms.Application.ExecutablePath);
                if (System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "*",
                    System.IO.SearchOption.TopDirectoryOnly).Length == 1)
                {
                    ProjectProperty.LibraryFilePath = System.IO.Directory.GetFiles(mainDirectory, "*." + SaveFileFormat.lbm + "*", System.IO.SearchOption.TopDirectoryOnly)[0];

                    //this.mainWindow.MspDB = DatabaseLcUtility.GetMspDbQueries(ProjectProperty.LibraryFilePath, this.mainWindow.IupacReference, this.mainWindow.AnalysisParamForLC.LipidQueryBean);
                    var source = await Task.Run(() =>
                        DatabaseLcUtility.GetMspDbQueries(ProjectProperty.LibraryFilePath, this.mainWindow.IupacReference, this.mainWindow.AnalysisParamForLC.LipidQueryBean)
                    );
                    this.mainWindow.MspDB = source;
                }
            }

            //if (this.postIdentificationLibraryFilePath != null && this.postIdentificationLibraryFilePath != string.Empty && this.postIdentificationLibraryFilePath != this.postIdentificationLibraryFilePathCopy)
            if (this.postIdentificationLibraryFilePath != null && this.postIdentificationLibraryFilePath != string.Empty) {

                if (!System.IO.File.Exists(this.postIdentificationLibraryFilePath)) {
                    MessageBox.Show(this.postIdentificationLibraryFilePath + " does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
                ProjectProperty.PostIdentificationLibraryFilePath = this.postIdentificationLibraryFilePath;
                this.mainWindow.PostIdentificationTxtDB = DatabaseLcUtility.GetTxtDbQueries(this.postIdentificationLibraryFilePath);

                if (this.mainWindow.PostIdentificationTxtDB == null || this.mainWindow.PostIdentificationTxtDB.Count == 0) return false;
            }
            else if (this.postIdentificationLibraryFilePath == null || this.postIdentificationLibraryFilePath == "")
            {
                ProjectProperty.PostIdentificationLibraryFilePath = string.Empty;
                this.mainWindow.PostIdentificationTxtDB = new List<PostIdentificatioinReferenceBean>();
            }

            if (Param.UseTargetFormulaLibrary == true)
            {
               // if (this.targetFormulaLibraryFilePath != null && this.targetFormulaLibraryFilePath != string.Empty && this.targetFormulaLibraryFilePath != this.targetFormulaLibraryFilePathCopy)
                if (this.targetFormulaLibraryFilePath != null && this.targetFormulaLibraryFilePath != string.Empty) {


                    if (!System.IO.File.Exists(this.targetFormulaLibraryFilePath)) {
                        MessageBox.Show(this.targetFormulaLibraryFilePath + " does not exist.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                        return false;
                    }
                    ProjectProperty.TargetFormulaLibraryFilePath = this.targetFormulaLibraryFilePath;

                    this.mainWindow.TargetFormulaLibrary = DatabaseLcUtility.GetTxtFormulaDbQueries(this.targetFormulaLibraryFilePath);

                    if (this.mainWindow.TargetFormulaLibrary == null || this.mainWindow.TargetFormulaLibrary.Count == 0) return false;
                }
                
            }

            if (Param.TrackingIsotopeLabels && Param.SetFullyLabeledReferenceFile) {
                var nonlabeledRefFileID = Param.NonLabeledReferenceID;
                var labeledRefFileID = Param.FullyLabeledReferenceID;
                if (nonlabeledRefFileID == labeledRefFileID) {
                    MessageBox.Show("The reference file of fully-labeled experimental data should be different from non-labeled reference file.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            Param.MsdialVersionNumber = Properties.Resources.VERSION;

            if (Param.IsIonMobility && Param.IonMobilityType != IonMobilityType.Tims && !Param.IsAllCalibrantDataImported) {
                var errorMessage = Param.IonMobilityType == IonMobilityType.Dtims
                    ? "For Agilent single fieled-based CCS calculation, you have to set the coefficients for all files. "
                    : "For Waters CCS calculation, you have to set the coefficients for all files. ";
                errorMessage += "Otherwise, the Mason–Schamp equation using gasweight=28.0134 and temperature=305.0 is used for CCS calculation for all data. ";
                errorMessage += "Do you perform the CCS parameter setting?";
                if (MessageBox.Show(errorMessage, "Error", MessageBoxButton.YesNo, MessageBoxImage.Error) == MessageBoxResult.Yes) {
                    return false;
                }
            }

            if (TogetherWithAlignment && this.analysisFiles.Count > 1)
            {
                if (Param.QcAtLeastFilter == true)
                    Param.QcAtLeastFilter = false;
                if (Param.AccumulatedRtRagne == 0.0) {
                    Param.DriftTimeAlignmentFactor = 0.5F;
                    Param.DriftTimeAlignmentTolerance = 0.025F;
                    Param.AccumulatedRtRagne = 0.2F;
                }
                
                if (Param.IsRemoveFeatureBasedOnPeakHeightFoldChange &&
                    analysisFiles.Count(n => n.AnalysisFilePropertyBean.AnalysisFileType == AnalysisFileType.Blank) == 0) {
                    if (MessageBox.Show("If you use blank sample filter, please set at least one file's type as Blank in file property setting. " +
                        "Do you continue this analysis without the filter option?",
                        "Messsage", MessageBoxButton.OKCancel, MessageBoxImage.Error) == MessageBoxResult.Cancel)
                        return false;
                }

                if (this.alignmentFiles == null || this.alignmentFiles.Count == 0)
                {
                    this.alignmentFiles.Add(
                        new AlignmentFileBean() {
                            FileID = 0,
                            FileName = this.alignmentResultFileName,
                            FilePath = ProjectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf,
                            EicFilePath = ProjectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + ".EIC.aef"
                        });
                }
                else
                {
                    this.alignmentFiles.Add(
                        new AlignmentFileBean() {
                            FileID = this.alignmentFiles.Count,
                            FileName = this.alignmentResultFileName,
                            FilePath = ProjectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf,
                            EicFilePath = ProjectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + ".EIC.aef"
                        });
                }
            }

            return true;
        }

        public void VmUpdate()
        {
            //data correction
            OnPropertyChanged("RetentionTimeBegin"); OnPropertyChanged("RetentionTimeEnd");
            OnPropertyChanged("MassRangeBegin"); OnPropertyChanged("MassRangeEnd"); OnPropertyChanged("Ms2MassRangeBegin"); OnPropertyChanged("Ms2MassRangeEnd");
            OnPropertyChanged("CentroidMs1Tolerance"); OnPropertyChanged("CentroidMs2Tolerance"); OnPropertyChanged("PeakDetectionBasedCentroid");
            OnPropertyChanged("MaxChargeNumber"); OnPropertyChanged("NumThreads");OnPropertyChanged("ExcuteRtCorrection");
            OnPropertyChanged("IsBrClConsideredForIsotopes");

            //peak detection
            OnPropertyChanged("SmoothingMethod"); OnPropertyChanged("MinimumAmplitude"); OnPropertyChanged("SmoothingLevel"); OnPropertyChanged("MassSliceWidth"); OnPropertyChanged("BackgroundSubtraction");
            OnPropertyChanged("AmplitudeNoiseFactor"); OnPropertyChanged("PeaktopNoiseFactor"); OnPropertyChanged("SlopeNoiseFactor"); OnPropertyChanged("MinimumDatapoints"); OnPropertyChanged("ExcludedMassViewModelCollection"); 


            //identification
            OnPropertyChanged("LibraryFilePath"); OnPropertyChanged("PostIdentificationLibraryFilePath");
            OnPropertyChanged("RetentionTimeLibrarySearchTolerance"); OnPropertyChanged("Ms1LibrarySearchTolerance");
            OnPropertyChanged("Ms2LibrarySearchTolerance"); OnPropertyChanged("IdentificationScoreCutOff"); OnPropertyChanged("RetentionTimeToleranceOfPostIdentification"); OnPropertyChanged("AccurateMassToleranceOfPostIdentification");
            OnPropertyChanged("PostIdentificationScoreCutOff"); OnPropertyChanged("OnlyReportTopHitForPostAnnotation"); OnPropertyChanged("RelativeAbundanceCutOff");
            OnPropertyChanged("IsIdentificationOnlyPerformedForAlignmentFile");
            OnPropertyChanged("IsUseRetentionInfoForIdentificationScoring");
            OnPropertyChanged("IsUseRetentionInfoForIdentificationFiltering");

            //adduct
            OnPropertyChanged("AdductIonInformationViewModelCollection"); OnPropertyChanged("AdductAndIsotopeMassTolerance");

            //ms2dec
            OnPropertyChanged("BandWidth"); OnPropertyChanged("SegmentNumber");
            OnPropertyChanged("SigmaWindowValue"); OnPropertyChanged("DeconvolutionType");
            OnPropertyChanged("AmplitudeCutoff"); OnPropertyChanged("RemoveAfterPrecursor");
            OnPropertyChanged("KeptIsotopeRange"); OnPropertyChanged("KeepOriginalPrecursorIsotopes");

            //alignment
            OnPropertyChanged("AlignmentResultFileName"); OnPropertyChanged("RetentionTimeAlignmentTolerance"); OnPropertyChanged("Ms1AlignmentTolerance");
            OnPropertyChanged("Ms2SimilarityAlignmentTolerance"); OnPropertyChanged("RetentionTimeAlignmentTolerance"); OnPropertyChanged("Ms1AlignmentTolerance"); OnPropertyChanged("AlignmentScoreCutOff");
            OnPropertyChanged("AlignmentReferenceFileID"); OnPropertyChanged("PeakCountFilter"); OnPropertyChanged("NPercentDetectedInOneGroup");
            OnPropertyChanged("QcAtLeastFilter"); OnPropertyChanged("GapFillingOption"); OnPropertyChanged("TogetherWithAlignment");

            //alignment filtering
            OnPropertyChanged("IsRemoveFeatureBasedOnPeakHeightFoldChange"); OnPropertyChanged("SampleMaxOverBlankAverage"); OnPropertyChanged("SampleAverageOverBlankAverage");
            OnPropertyChanged("IsKeepRemovableFeaturesAndAssignedTagForChecking"); OnPropertyChanged("IsKeepIdentifiedMetaboliteFeatures"); OnPropertyChanged("IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples");
            OnPropertyChanged("BlankFiltering"); OnPropertyChanged("IsKeepAnnotatedMetaboliteFeatures"); OnPropertyChanged("FoldChangeForBlankFiltering");
            OnPropertyChanged("IsForceInsertForGapFilling");

            //ion mobility
            OnPropertyChanged("IsTIMS"); OnPropertyChanged("IsDTIMS"); OnPropertyChanged("IsTWIMS");
            OnPropertyChanged("AccumulatedRtRagne"); OnPropertyChanged("CcsSearchTolerance");
            OnPropertyChanged("MobilityUnit"); OnPropertyChanged("IsUseCcsForIdentificationFiltering"); OnPropertyChanged("IsUseCcsForIdentificationScoring");
            OnPropertyChanged("DriftTimeAlignmentTolerance");

            //tracking of isotope labels
            OnPropertyChanged("TrackingIsotopeLabels"); OnPropertyChanged("UseTargetFormulaLibrary");
            OnPropertyChanged("SetFullyLabeledReferenceFile"); OnPropertyChanged("FullyLabeledReferenceID");
        }

        #region // Property
        
        public AnalysisParametersBean Param
        {
            get { return this.mainWindow.AnalysisParamForLC; }
            set { this.mainWindow.AnalysisParamForLC = value; }
        }

        public RdamPropertyBean RdamProperty
        {
            get { return this.mainWindow.RdamProperty; }
            set { this.mainWindow.RdamProperty = value; }
        }

        public ProjectPropertyBean ProjectProperty
        {
            get { return this.mainWindow.ProjectProperty; }
            set { this.mainWindow.ProjectProperty = value; }
        }

        #region peak detection
        public SmoothingMethod SmoothingMethod
        {
            get { return Param.SmoothingMethod; }
            set { if (Param.SmoothingMethod == value) return; Param.SmoothingMethod = value; OnPropertyChanged("SmoothingMethod"); }
        }

        public double MinimumAmplitude
        {
            get { return Param.MinimumAmplitude; }
            set { if (Param.MinimumAmplitude == value) return; Param.MinimumAmplitude = value; OnPropertyChanged("MinimumAmplitude"); }
        }

        public int SmoothingLevel
        {
            get { return Param.SmoothingLevel; }
            set { if (Param.SmoothingLevel == value) return; Param.SmoothingLevel = value; OnPropertyChanged("SmoothingLevel"); }
        }

        public float MassSliceWidth
        {
            get { return Param.MassSliceWidth; }
            set { if (Param.MassSliceWidth == value) return; Param.MassSliceWidth = value; OnPropertyChanged("MassSliceWidth"); }
        }

        public bool BackgroundSubtraction
        {
            get { return Param.BackgroundSubtraction; }
            set { if (Param.BackgroundSubtraction == value) return; Param.BackgroundSubtraction = value; OnPropertyChanged("BackgroundSubtraction"); }
        }

        public double AmplitudeNoiseFactor
        {
            get { return Param.AmplitudeNoiseFactor; }
            set { if (Param.AmplitudeNoiseFactor == value) return; Param.AmplitudeNoiseFactor = value; OnPropertyChanged("AmplitudeNoiseFactor"); }
        }

        public double PeaktopNoiseFactor
        {
            get { return Param.PeaktopNoiseFactor; }
            set { if (Param.PeaktopNoiseFactor == value) return; Param.PeaktopNoiseFactor = value; OnPropertyChanged("PeaktopNoiseFactor"); }
        }

        public double SlopeNoiseFactor
        {
            get { return Param.SlopeNoiseFactor; }
            set { if (Param.SlopeNoiseFactor == value) return; Param.SlopeNoiseFactor = value; OnPropertyChanged("SlopeNoiseFactor"); }
        }

        public double MinimumDatapoints
        {
            get { return Param.MinimumDatapoints; }
            set { if (Param.MinimumDatapoints == value) return; Param.MinimumDatapoints = value; OnPropertyChanged("MinimumDatapoints"); }
        }

        public ObservableCollection<ExcludeMassVM> ExcludedMassViewModelCollection
        {
            get { return excludedMassVMs; }
            set { if (excludedMassVMs == value) return; excludedMassVMs = value; OnPropertyChanged("ExcludedMassViewModelCollection"); }
        }

        public int MaxChargeNumber {
            get { return Param.MaxChargeNumber; }
            set { if (Param.MaxChargeNumber == value) return; Param.MaxChargeNumber = value; OnPropertyChanged("MaxChargeNumber"); }
        }

        public bool IsBrClConsideredForIsotopes {
            get { return Param.IsBrClConsideredForIsotopes; }
            set { if (Param.IsBrClConsideredForIsotopes == value) return; Param.IsBrClConsideredForIsotopes = value; OnPropertyChanged("IsBrClConsideredForIsotopes"); }
        }

        public int NumThreads {
            get { return Param.NumThreads; }
            set { if (Param.NumThreads == value) return; Param.NumThreads = value; OnPropertyChanged("NumThreads"); }
        }
        #endregion

        #region data correction
        public float RetentionTimeBegin
        {
            get { return Param.RetentionTimeBegin; }
            set { if (Param.RetentionTimeBegin == value) return; Param.RetentionTimeBegin = value; OnPropertyChanged("RetentionTimeBegin"); }
        }

        public float RetentionTimeEnd
        {
            get { return Param.RetentionTimeEnd; }
            set { if (Param.RetentionTimeEnd == value) return; Param.RetentionTimeEnd = value; OnPropertyChanged("RetentionTimeEnd"); }
        }

        public float MassRangeBegin
        {
            get { return Param.MassRangeBegin; }
            set { if (Param.MassRangeBegin == value) return; Param.MassRangeBegin = value; OnPropertyChanged("MassRangeBegin"); }
        }

        public float MassRangeEnd
        {
            get { return Param.MassRangeEnd; }
            set { if (Param.MassRangeEnd == value) return; Param.MassRangeEnd = value; OnPropertyChanged("MassRangeEnd"); }
        }

        public float Ms2MassRangeBegin {
            get { return Param.Ms2MassRangeBegin; }
            set { if (Param.Ms2MassRangeBegin == value) return; Param.Ms2MassRangeBegin = value; OnPropertyChanged("Ms2MassRangeBegin"); }
        }

        public float Ms2MassRangeEnd {
            get { return Param.Ms2MassRangeEnd; }
            set { if (Param.Ms2MassRangeEnd == value) return; Param.Ms2MassRangeEnd = value; OnPropertyChanged("Ms2MassRangeEnd"); }
        }


        public float CentroidMs1Tolerance
        {
            get { return Param.CentroidMs1Tolerance; }
            set { if (Param.CentroidMs1Tolerance == value) return; Param.CentroidMs1Tolerance = value; OnPropertyChanged("CentroidMs1Tolerance"); }
        }

        public float CentroidMs2Tolerance
        {
            get { return Param.CentroidMs2Tolerance; }
            set { if (Param.CentroidMs2Tolerance == value) return; Param.CentroidMs2Tolerance = value; OnPropertyChanged("CentroidMs2Tolerance"); }
        }

        public bool PeakDetectionBasedCentroid
        {
            get { return Param.PeakDetectionBasedCentroid; }
            set { if (Param.PeakDetectionBasedCentroid == value) return; Param.PeakDetectionBasedCentroid = value; OnPropertyChanged("PeakDetectionBasedCentroid"); }
        }

        public bool ExcuteRtCorrection {
            get { return Param.RetentionTimeCorrectionCommon?.RetentionTimeCorrectionParam?.ExcuteRtCorrection ?? false; }
            set { if(Param.RetentionTimeCorrectionCommon == null) { Param.RetentionTimeCorrectionCommon = new RetentionTimeCorrection.RetentionTimeCorrectionCommon(); }
                if (Param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection == value) return;
                Param.RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection = value; OnPropertyChanged("ExcuteRtCorrection");
            }
        }
        #endregion

        #region identification
        public string LibraryFilePath
        {
            get { return libraryFilePath; }
            set { if (libraryFilePath == value) return; libraryFilePath = value; OnPropertyChanged("LibraryFilePath"); }
        }

        public string PostIdentificationLibraryFilePath
        {
            get { return postIdentificationLibraryFilePath; }
            set { if (postIdentificationLibraryFilePath == value) return; postIdentificationLibraryFilePath = value; OnPropertyChanged("PostIdentificationLibraryFilePath"); }
        }

        public float RetentionTimeLibrarySearchTolerance
        {
            get { return Param.RetentionTimeLibrarySearchTolerance; }
            set { if (Param.RetentionTimeLibrarySearchTolerance == value) return; Param.RetentionTimeLibrarySearchTolerance = value; OnPropertyChanged("RetentionTimeLibrarySearchTolerance"); }
        }

        public float Ms1LibrarySearchTolerance
        {
            get { return Param.Ms1LibrarySearchTolerance; }
            set { if (Param.Ms1LibrarySearchTolerance == value) return; Param.Ms1LibrarySearchTolerance = value; OnPropertyChanged("Ms1LibrarySearchTolerance"); }
        }

        public float Ms2LibrarySearchTolerance
        {
            get { return Param.Ms2LibrarySearchTolerance; }
            set { if (Param.Ms2LibrarySearchTolerance == value) return; Param.Ms2LibrarySearchTolerance = value; OnPropertyChanged("Ms2LibrarySearchTolerance"); }
        }

        public float IdentificationScoreCutOff
        {
            get { return Param.IdentificationScoreCutOff; }
            set { if (Param.IdentificationScoreCutOff == value) return; Param.IdentificationScoreCutOff = value; OnPropertyChanged("IdentificationScoreCutOff"); }
        }

        public float RetentionTimeToleranceOfPostIdentification
        {
            get { return Param.RetentionTimeToleranceOfPostIdentification; }
            set { if (Param.RetentionTimeToleranceOfPostIdentification == value) return; Param.RetentionTimeToleranceOfPostIdentification = value; OnPropertyChanged("RetentionTimeToleranceOfPostIdentification"); }
        }

        public float AccurateMassToleranceOfPostIdentification
        {
            get { return Param.AccurateMassToleranceOfPostIdentification; }
            set { if (Param.AccurateMassToleranceOfPostIdentification == value) return; Param.AccurateMassToleranceOfPostIdentification = value; OnPropertyChanged("AccurateMassToleranceOfPostIdentification"); }
        }

        public float PostIdentificationScoreCutOff
        {
            get { return Param.PostIdentificationScoreCutOff; }
            set { if (Param.PostIdentificationScoreCutOff == value) return; Param.PostIdentificationScoreCutOff = value; OnPropertyChanged("PostIdentificationScoreCutOff"); }
        }

        public bool OnlyReportTopHitForPostAnnotation
        {
            get { return Param.OnlyReportTopHitForPostAnnotation; }
            set { if (Param.OnlyReportTopHitForPostAnnotation == value) return; Param.OnlyReportTopHitForPostAnnotation = value; OnPropertyChanged("OnlyReportTopHitForPostAnnotation"); }
        }

        public float RelativeAbundanceCutOff
        {
            get { return Param.RelativeAbundanceCutOff; }
            set { if (Param.RelativeAbundanceCutOff == value) return; Param.RelativeAbundanceCutOff = value; OnPropertyChanged("RelativeAbundanceCutOff"); }
        }

        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get { return Param.IsIdentificationOnlyPerformedForAlignmentFile; }
            set {
                if (Param.IsIdentificationOnlyPerformedForAlignmentFile == value)
                    return;
                Param.IsIdentificationOnlyPerformedForAlignmentFile = value;
                OnPropertyChanged("IsIdentificationOnlyPerformedForAlignmentFile");
            }
        }

        public bool IsUseRetentionInfoForIdentificationScoring {
            get { return Param.IsUseRetentionInfoForIdentificationScoring; }
            set {
                if (Param.IsUseRetentionInfoForIdentificationScoring == value) return;
                Param.IsUseRetentionInfoForIdentificationScoring = value;
                OnPropertyChanged("IsUseRetentionInfoForIdentificationScoring");
            }
        }

        public bool IsUseRetentionInfoForIdentificationFiltering {
            get { return Param.IsUseRetentionInfoForIdentificationFiltering; }
            set {
                if (Param.IsUseRetentionInfoForIdentificationFiltering == value) return;
                Param.IsUseRetentionInfoForIdentificationFiltering = value;
                OnPropertyChanged("IsUseRetentionInfoForIdentificationFiltering");
            }
        }
        #endregion

        #region ms2dec
        public int BandWidth
        {
            get { return Param.BandWidth; }
            set { if (Param.BandWidth == value) return; Param.BandWidth = value; OnPropertyChanged("BandWidth"); }
        }

        public int SegmentNumber
        {
            get { return Param.SegmentNumber; }
            set { if (Param.SegmentNumber == value) return; Param.SegmentNumber = value; OnPropertyChanged("SegmentNumber"); }
        }

        public float SigmaWindowValue
        {
            get { return Param.SigmaWindowValue; }
            set { if (Param.SigmaWindowValue == value) return; Param.SigmaWindowValue = value; OnPropertyChanged("SigmaWindowValue"); }
        }

        public DeconvolutionType DeconvolutionType
        {
            get { return Param.DeconvolutionType; }
            set { if (Param.DeconvolutionType == value) return; Param.DeconvolutionType = value; OnPropertyChanged("DeconvolutionType"); }
        }

        public float AmplitudeCutoff
        {
            get { return Param.AmplitudeCutoff; }
            set { if (Param.AmplitudeCutoff == value) return; Param.AmplitudeCutoff = value; OnPropertyChanged("AmplitudeCutoff"); }
        }

        public bool RemoveAfterPrecursor
        {
            get { return Param.RemoveAfterPrecursor; }
            set { if (Param.RemoveAfterPrecursor == value) return; Param.RemoveAfterPrecursor = value; OnPropertyChanged("RemoveAfterPrecursor"); }
        }

        public float KeptIsotopeRange {
            get { return Param.KeptIsotopeRange; }
            set { if (Param.KeptIsotopeRange == value) return; Param.KeptIsotopeRange = value; OnPropertyChanged("KeptIsotopeRange"); }
        }

        public bool KeepOriginalPrecursorIsotopes {
            get { return Param.KeepOriginalPrecursorIsotopes; }
            set { if (Param.KeepOriginalPrecursorIsotopes == value) return; Param.KeepOriginalPrecursorIsotopes = value; OnPropertyChanged("KeepOriginalPrecursorIsotopes"); }
        }

        #endregion

        #region adduct

        public float AdductAndIsotopeMassTolerance
        {
            get { return Param.AdductAndIsotopeMassTolerance; }
            set { if (Param.AdductAndIsotopeMassTolerance == value) return; Param.AdductAndIsotopeMassTolerance = value; OnPropertyChanged("AdductAndIsotopeMassTolerance"); }
        }

        public ObservableCollection<AdductIonInformationVM> AdductIonInformationViewModelCollection
        {
            get { return adductIonVMs; }
            set { adductIonVMs = value; OnPropertyChanged("AdductIonInformationViewModelCollection"); }
        }

        #endregion

        #region alignment
        [Required(ErrorMessage = "Decide a file name")]
        public string AlignmentResultFileName
        {
            get { return alignmentResultFileName; }
            set { if (alignmentResultFileName == value) return; alignmentResultFileName = value; OnPropertyChanged("AlignmentResultFileName"); }
        }

        public int AlignmentReferenceFileID
        {
            get { return Param.AlignmentReferenceFileID; }
            set { if (Param.AlignmentReferenceFileID == value) return; Param.AlignmentReferenceFileID = value; OnPropertyChanged("AlignmentReferenceFileID"); }
        }

        public float PeakCountFilter
        {
            get { return Param.PeakCountFilter; }
            set { if (Param.PeakCountFilter == value) return; Param.PeakCountFilter = value; OnPropertyChanged("PeakCountFilter"); }
        }

        public bool QcAtLeastFilter
        {
            get { return Param.QcAtLeastFilter; }
            set { if (Param.QcAtLeastFilter == value) return; Param.QcAtLeastFilter = value; OnPropertyChanged("QcAtLeastFilter"); }
        }

        public bool GapFillingOption
        {
            get { return Param.GapFillingOption; }
            set { if (Param.GapFillingOption == value) return; Param.GapFillingOption = value; OnPropertyChanged("GapFillingOption"); }
        }

        public bool TogetherWithAlignment
        {
            get { return Param.TogetherWithAlignment; }
            set { if (Param.TogetherWithAlignment == value) return; Param.TogetherWithAlignment = value; OnPropertyChanged("TogetherWithAlignment"); }
        }

        public float RetentionTimeAlignmentFactor
        {
            get { return Param.RetentionTimeAlignmentFactor; }
            set { if (RetentionTimeAlignmentFactor == value) return; Param.RetentionTimeAlignmentFactor = value; OnPropertyChanged("RetentionTimeAlignmentTolerance"); }
        }

        public float Ms1AlignmentFactor
        {
            get { return Param.Ms1AlignmentFactor; }
            set { if (Param.Ms1AlignmentFactor == value) return; Param.Ms1AlignmentFactor = value; OnPropertyChanged("Ms1AlignmentTolerance"); }
        }

        public float Ms2SimilarityAlignmentFactor
        {
            get { return Param.Ms2SimilarityAlignmentFactor; }
            set { if (Param.Ms2SimilarityAlignmentFactor == value) return; Param.Ms2SimilarityAlignmentFactor = value; OnPropertyChanged("Ms2SimilarityAlignmentTolerance"); }
        }

        public float RetentionTimeAlignmentTolerance
        {
            get { return Param.RetentionTimeAlignmentTolerance; }
            set { if (Param.RetentionTimeAlignmentTolerance == value) return; Param.RetentionTimeAlignmentTolerance = value; OnPropertyChanged("RetentionTimeAlignmentTolerance"); }
        }

        public float Ms1AlignmentTolerance
        {
            get { return Param.Ms1AlignmentTolerance; }
            set { if (Param.Ms1AlignmentTolerance == value) return; Param.Ms1AlignmentTolerance = value; OnPropertyChanged("Ms1AlignmentTolerance"); }
        }

        public float AlignmentScoreCutOff
        {
            get { return Param.AlignmentScoreCutOff; }
            set { if (Param.AlignmentScoreCutOff == value) return; Param.AlignmentScoreCutOff = value; OnPropertyChanged("AlignmentScoreCutOff"); }
        }

        public float NPercentDetectedInOneGroup {
            get { return Param.NPercentDetectedInOneGroup; }
            set { if (Param.NPercentDetectedInOneGroup == value) return; Param.NPercentDetectedInOneGroup = value; OnPropertyChanged("NPercentDetectedInOneGroup"); }
        }

        public bool IsRemoveFeatureBasedOnPeakHeightFoldChange {
            get { return Param.IsRemoveFeatureBasedOnPeakHeightFoldChange; }
            set { if (Param.IsRemoveFeatureBasedOnPeakHeightFoldChange == value) return; Param.IsRemoveFeatureBasedOnPeakHeightFoldChange = value; OnPropertyChanged("IsRemoveFeatureBasedOnPeakHeightFoldChange"); }
        }

        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get { return Param.IsKeepRemovableFeaturesAndAssignedTagForChecking; }
            set { if (Param.IsKeepRemovableFeaturesAndAssignedTagForChecking == value) return; Param.IsKeepRemovableFeaturesAndAssignedTagForChecking = value; OnPropertyChanged("IsKeepRemovableFeaturesAndAssignedTagForChecking"); }
        }

        public bool IsKeepIdentifiedMetaboliteFeatures {
            get { return Param.IsKeepIdentifiedMetaboliteFeatures; }
            set { if (Param.IsKeepIdentifiedMetaboliteFeatures == value) return; Param.IsKeepIdentifiedMetaboliteFeatures = value; OnPropertyChanged("IsKeepIdentifiedMetaboliteFeatures"); }
        }

        public bool IsKeepAnnotatedMetaboliteFeatures {
            get { return Param.IsKeepAnnotatedMetaboliteFeatures; }
            set { if (Param.IsKeepAnnotatedMetaboliteFeatures == value) return; Param.IsKeepAnnotatedMetaboliteFeatures = value; OnPropertyChanged("IsKeepAnnotatedMetaboliteFeatures"); }
        }

        public BlankFiltering BlankFiltering {
            get { return Param.BlankFiltering; }
            set { if (Param.BlankFiltering == value) return; Param.BlankFiltering = value; OnPropertyChanged("BlankFiltering"); }
        }

        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get { return Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples; }
            set { if (Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples == value) return; Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value; OnPropertyChanged("IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples"); }
        }

        public float SampleMaxOverBlankAverage {
            get { return Param.SampleMaxOverBlankAverage; }
            set { if (Param.SampleMaxOverBlankAverage == value) return; Param.SampleMaxOverBlankAverage = value; OnPropertyChanged("SampleMaxOverBlankAverage"); }
        }

        public float SampleAverageOverBlankAverage {
            get { return Param.SampleAverageOverBlankAverage; }
            set { if (Param.SampleAverageOverBlankAverage == value) return; Param.SampleAverageOverBlankAverage = value; OnPropertyChanged("SampleAverageOverBlankAverage"); }
        }

        public float FoldChangeForBlankFiltering {
            get { return Param.FoldChangeForBlankFiltering; }
            set { if (Param.FoldChangeForBlankFiltering == value) return; Param.FoldChangeForBlankFiltering = value; OnPropertyChanged("FoldChangeForBlankFiltering"); }
        }

        public bool IsForceInsertForGapFilling {
            get { return Param.IsForceInsertForGapFilling; }
            set { if (Param.IsForceInsertForGapFilling == value) return; Param.IsForceInsertForGapFilling = value; OnPropertyChanged("IsForceInsertForGapFilling"); }
        }

        #endregion

        #region mobility

        public bool IsTIMS {
            get { return isTIMS; }
            set { if (isTIMS == value) return;
                isTIMS = value;
                if (isTIMS == true) {
                    isDTIMS = false; isTWIMS = false; 
                }
                changeMobilityUnit();
                OnPropertyChanged("IsTIMS"); }
        }

        
        public bool IsDTIMS {
            get { return isDTIMS; }
            set { if (isDTIMS == value) return;
                isDTIMS = value;
                if (isDTIMS == true) {
                    isTIMS = false; isTWIMS = false; 
                }
                changeMobilityUnit();
                OnPropertyChanged("IsDTIMS"); }
        }

        public bool IsTWIMS {
            get { return isTWIMS; }
            set { if (isTWIMS == value) return;
                isTWIMS = value;
                if (isTWIMS == true) {
                    isTIMS = false; isDTIMS = false;
                }
                changeMobilityUnit();
                OnPropertyChanged("IsTWIMS"); }
        }

        private void changeMobilityUnit() {
            if (isTIMS) {
                MobilityUnit = "1/k0";
                Param.IonMobilityType = IonMobilityType.Tims;
            }
            else if (isDTIMS) {
                MobilityUnit = "msec";
                Param.IonMobilityType = IonMobilityType.Dtims;
            }
            else if (isTWIMS) {
                MobilityUnit = "msec";
                Param.IonMobilityType = IonMobilityType.Twims;
            }
            else {
                MobilityUnit = "1/k0";
                Param.IonMobilityType = IonMobilityType.Tims;
            }
        }

        public float AccumulatedRtRagne {
            get { return Param.AccumulatedRtRagne; }
            set { if (Param.AccumulatedRtRagne == value) return; Param.AccumulatedRtRagne = value; OnPropertyChanged("AccumulatedRtRagne"); }
        }

        public string MobilityUnit {
            get { return mobilityUnit; }
            set { if (mobilityUnit == value) return; mobilityUnit = value; OnPropertyChanged("MobilityUnit"); }
        }

        public float CcsSearchTolerance {
            get { return Param.CcsSearchTolerance; }
            set { if (Param.CcsSearchTolerance == value) return; Param.CcsSearchTolerance = value; OnPropertyChanged("CcsSearchTolerance"); }
        }

        public bool IsUseCcsForIdentificationFiltering {
            get { return Param.IsUseCcsForIdentificationFiltering; }
            set { if (Param.IsUseCcsForIdentificationFiltering == value) return;
                Param.IsUseCcsForIdentificationFiltering = value;
                OnPropertyChanged("IsUseCcsForIdentificationFiltering");
            }
        }

        public bool IsUseCcsForIdentificationScoring {
            get { return Param.IsUseCcsForIdentificationScoring; }
            set {
                if (Param.IsUseCcsForIdentificationScoring == value) return;
                Param.IsUseCcsForIdentificationScoring = value;
                OnPropertyChanged("IsUseCcsForIdentificationScoring");
            }
        }

        public float DriftTimeAlignmentTolerance {
            get { return Param.DriftTimeAlignmentTolerance; }
            set { if (Param.DriftTimeAlignmentTolerance == value) return;
                Param.DriftTimeAlignmentTolerance = value;
                OnPropertyChanged("DriftTimeAlignmentTolerance"); }
        }
        #endregion

        #region tracking of isotope labels
        public bool TrackingIsotopeLabels
        {
            get { return Param.TrackingIsotopeLabels; }
            set { if (Param.TrackingIsotopeLabels == value) return; Param.TrackingIsotopeLabels = value; OnPropertyChanged("TrackingIsotopeLabels"); }
        }

        public bool UseTargetFormulaLibrary
        {
            get { return Param.UseTargetFormulaLibrary; }
            set { if (Param.UseTargetFormulaLibrary == value) return; Param.UseTargetFormulaLibrary = value; OnPropertyChanged("UseTargetFormulaLibrary"); }
        }

        public IsotopeTrackingDictionary IsotopeTrackingDictionary
        {
            get { return Param.IsotopeTrackingDictionary; }
            set { if (Param.IsotopeTrackingDictionary == value) return; Param.IsotopeTrackingDictionary = value; OnPropertyChanged("IsotopeTrackingDictionary"); }
        }

        public int NonLabeledReferenceID
        {
            get { return Param.NonLabeledReferenceID; }
            set { if (Param.NonLabeledReferenceID == value) return; Param.NonLabeledReferenceID = value; OnPropertyChanged("NonLabeledReferenceID"); }
        }

        public bool SetFullyLabeledReferenceFile {
            get { return Param.SetFullyLabeledReferenceFile; }
            set { if (Param.SetFullyLabeledReferenceFile == value) return; Param.SetFullyLabeledReferenceFile = value; OnPropertyChanged("SetFullyLabeledReferenceFile"); }
        }

        public int FullyLabeledReferenceID {
            get { return Param.FullyLabeledReferenceID; }
            set { if (Param.FullyLabeledReferenceID == value) return; Param.FullyLabeledReferenceID = value; OnPropertyChanged("FullyLabeledReferenceID"); }
        }

        public string TargetFormulaLibraryFilePath
        {
            get { return targetFormulaLibraryFilePath; }
            set { if (targetFormulaLibraryFilePath == value) return; targetFormulaLibraryFilePath = value; OnPropertyChanged("TargetFormulaLibraryFilePath"); }
        }

        #endregion
       
        #endregion

    }
}
