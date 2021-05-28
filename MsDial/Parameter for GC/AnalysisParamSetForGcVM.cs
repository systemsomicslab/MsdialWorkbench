using Msdial.Gcms.Dataprocess.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisParamSetForGcVM : ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;
        private AnalysisParamOfMsdialGcms param;

        private ProjectPropertyBean projectProperty;

        private ObservableCollection<AnalysisFileBean> analysisFiles;
        private ObservableCollection<AlignmentFileBean> alignmentFiles;
        private ObservableCollection<ExcludeMassVM> excludedMassVMs;

        private string mspFilePath;
        private string mspFilePathCopy;

        private string riFilePath;
        private string riFilePathCopy;

        private string alignmentResultFileName;

        private bool isAccurateMs;

        public AnalysisParamSetForGcVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;
            this.projectProperty = mainWindow.ProjectProperty;
            this.analysisFiles = mainWindow.AnalysisFiles;
            this.alignmentFiles = mainWindow.AlignmentFiles;
            this.param = mainWindow.AnalysisParamForGC;
            this.param.DataType = this.projectProperty.DataType;
            this.param.IonMode = this.projectProperty.IonMode;

            this.mspFilePath = this.param.MspFilePath;
            this.mspFilePathCopy = this.param.MspFilePath;
            this.excludedMassVMs = new ObservableCollection<ExcludeMassVM>();
            for (int i = 0; i < 200; i++)
                this.excludedMassVMs.Add(new ExcludeMassVM());
            if (this.param.ExcludedMassList != null && this.param.ExcludedMassList.Count != 0) {
                for (int i = 0; i < this.param.ExcludedMassList.Count; i++) {
                    this.excludedMassVMs[i].ExcludedMass = this.param.ExcludedMassList[i].ExcludedMass;
                    this.excludedMassVMs[i].MassTolerance = this.param.ExcludedMassList[i].MassTolerance;
                }
            }

            if (this.param.AccuracyType == AccuracyType.IsAccurate) this.isAccurateMs = true;
            else this.isAccurateMs = false;
            if (this.param.QcAtLeastFilter == true) {
                this.param.QcAtLeastFilter = false;
            }
            
            var dt = DateTime.Now;
            this.alignmentResultFileName = "alignmentResult" + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;
        }

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);

            Mouse.OverrideCursor = Cursors.Wait;
            if (ClosingMethod() == true)
            {
                window.DialogResult = true;
                window.Close();
            }
            Mouse.OverrideCursor = null;
        }

        private bool ClosingMethod()
        {
            if (this.mspFilePath != null && this.mspFilePath != "")
            {
                if (this.mspFilePath != this.mspFilePathCopy) {
                    this.param.MspFilePath = this.mspFilePath;
                    this.mainWindow.MspDB = DatabaseGcUtility.GetMspDbQueries(this.param.MspFilePath);
                }
            }
            else if (this.param.RiCompoundType == RiCompoundType.Fames) {
                var fileUri = new Uri("/Resources/Fiehn GCMS MSP.txt", UriKind.Relative);
                var info = Application.GetResourceStream(fileUri);
                this.param.MspFilePath = "Internal resource";
                this.mainWindow.MspDB = MspFileParcer.FiehnGcmsMspReader(info.Stream);
            }
            else if ((this.mspFilePath == null || this.mspFilePath == "") && this.param.RiCompoundType == RiCompoundType.Alkanes)
            {
                this.param.MspFilePath = string.Empty;
                this.mainWindow.MspDB = new List<MspFormatCompoundInformationBean>();
            }

            
            if (this.param.RetentionType == RetentionType.RI && (this.param.FileIdRiInfoDictionary == null || this.param.FileIdRiInfoDictionary.Count != this.analysisFiles.Count))
            {
                MessageBox.Show("If you choose the retention index type for compound identifications, please select your RI dictionary file. \r\n" + 
                "Note that please select 'retention time' option if you want to skip the identification procedure.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            if (this.param.RetentionType == RetentionType.RI && this.param.RiCompoundType == RiCompoundType.Fames)
            {
                var flg = false;
                foreach (var pair in this.param.FileIdRiInfoDictionary)
                {
                    var key = pair.Key;
                    var value = pair.Value;
                    if (!isFamesContanesMatch(value.RiDictionary)) flg = true;
                }
                if (flg == true)
                {
                    var text = "If you use the FAMEs RI, you have to decide the retention times as minute for \r\n"
                            + "C8, C9, C10, C12, C14, C16, C18, C20, C22, C24, C26, C28, C30.";

                    MessageBox.Show(text, "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                    return false;
                }
            }

            this.param.ExcludedMassList = new List<ExcludeMassBean>();
            for (int i = 0; i < excludedMassVMs.Count; i++) {
                if (this.excludedMassVMs[i].ExcludedMass == null || this.excludedMassVMs[i].MassTolerance == null) continue;
                if (this.excludedMassVMs[i].ExcludedMass <= 0 || this.excludedMassVMs[i].MassTolerance <= 0) continue;
                this.param.ExcludedMassList.Add(new ExcludeMassBean() { ExcludedMass = this.excludedMassVMs[i].ExcludedMass, MassTolerance = this.excludedMassVMs[i].MassTolerance });
            }

            if (this.param.AlignmentIndexType == AlignmentIndexType.RI && (this.param.FileIdRiInfoDictionary == null || this.param.FileIdRiInfoDictionary.Count != this.analysisFiles.Count)) {
                MessageBox.Show("If you choose the retention index for peak alignment, please select your RI dictionary file at identification tab. \r\n" +
                "Note that please select 'retention time' option if you want to align your data by RT (min).", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }


            if (TogetherWithAlignment && this.analysisFiles.Count > 1)
            {
                if (Param.QcAtLeastFilter == true)
                    Param.QcAtLeastFilter = false;

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
                            FilePath = this.projectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf,
                            EicFilePath = this.projectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + ".EIC.aef"
                        });
                }
                else
                {
                    this.alignmentFiles.Add(
                        new AlignmentFileBean() {
                            FileID = this.alignmentFiles.Count,
                            FileName = this.alignmentResultFileName,
                            FilePath = this.projectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf,
                            EicFilePath = this.projectProperty.ProjectFolderPath + "\\" + this.alignmentResultFileName + ".EIC.aef"
                        });
                }
            }

            if (this.param.AccuracyType == AccuracyType.IsNominal)
            {
                this.param.MassSliceWidth = 0.5F;
                this.param.MassAccuracy = 0.5F;
            }

            this.param.MsdialVersionNumber = Properties.Resources.VERSION;
            this.mainWindow.AnalysisParamForGC = this.param;

            return true;
        }

        private bool isFamesContanesMatch(Dictionary<int, float> riDictionary)
        {
            var fiehnFamesDictionary = MspFileParcer.GetFiehnFamesDictionary();

            if (fiehnFamesDictionary.Count != riDictionary.Count) return false;
            foreach (var fFame in fiehnFamesDictionary)
            {
                var fiehnCnumber = fFame.Key;
                var flg = false;
                foreach (var dict in riDictionary)
                {
                    if (fiehnCnumber == dict.Key)
                    {
                        flg = true;
                        break;
                    }
                }
                if (flg == false) return false;
            }
            return true;
        }

        public void VmUpdate()
        {
            //data correction
            OnPropertyChanged("RetentionTimeBegin"); OnPropertyChanged("RetentionTimeEnd");
            OnPropertyChanged("MassRangeBegin"); OnPropertyChanged("MassRangeEnd"); OnPropertyChanged("NumThreads");

            //peak detection
            OnPropertyChanged("SmoothingMethod"); OnPropertyChanged("SmoothingLevel"); 
            OnPropertyChanged("MassSliceWidth"); OnPropertyChanged("AveragePeakWidth");
            OnPropertyChanged("MinimumAmplitude"); OnPropertyChanged("MassAccuracy");
            OnPropertyChanged("ExcludedMassViewModelCollection");

            //identification
            OnPropertyChanged("MspFilePath"); OnPropertyChanged("RiDictionaryFilePath");
            OnPropertyChanged("RetentionTimeLibrarySearchTolerance"); OnPropertyChanged("RetentionIndexLibrarySearchTolerance");
            OnPropertyChanged("EiSimilarityLibrarySearchCutOff"); OnPropertyChanged("IdentificationScoreCutOff");
            OnPropertyChanged("MzLibrarySearchTolerance"); OnPropertyChanged("IsIdentificationOnlyPerformedForAlignmentFile");
            OnPropertyChanged("IsUseRetentionInfoForIdentificationScoring");
            OnPropertyChanged("IsReplaceQuantmassByUserDefinedValue");
            OnPropertyChanged("IsUseRetentionInfoForIdentificationFiltering");
            OnPropertyChanged("IsOnlyTopHitReport");

            //ms1dec
            OnPropertyChanged("SigmaWindowValue"); OnPropertyChanged("AmplitudeCutoff"); 

            //alignment
            OnPropertyChanged("AlignmentResultFileName"); OnPropertyChanged("RetentionTimeAlignmentTolerance"); 
            OnPropertyChanged("EiSimilarityAlignmentFactor"); OnPropertyChanged("RetentionTimeAlignmentTolerance"); 
            OnPropertyChanged("EiSimilarityAlignmentCutOff"); OnPropertyChanged("AlignmentScoreCutOff");
            OnPropertyChanged("AlignmentReferenceFileID");OnPropertyChanged("TogetherWithAlignment"); OnPropertyChanged("RetentionIndexAlignmentTolerance");
            OnPropertyChanged("IsForceInsertForGapFilling"); OnPropertyChanged("IsRepresentativeQuantMassBasedOnBasePeakMz");

            //alignment filtering
            OnPropertyChanged("PeakCountFilter");
            OnPropertyChanged("QcAtLeastFilter"); OnPropertyChanged("NPercentDetectedInOneGroup");
            OnPropertyChanged("IsRemoveFeatureBasedOnPeakHeightFoldChange"); OnPropertyChanged("SampleMaxOverBlankAverage"); OnPropertyChanged("SampleAverageOverBlankAverage");
            OnPropertyChanged("IsKeepRemovableFeaturesAndAssignedTagForChecking"); OnPropertyChanged("IsKeepIdentifiedMetaboliteFeatures"); OnPropertyChanged("IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples");
            OnPropertyChanged("BlankFiltering"); OnPropertyChanged("IsKeepAnnotatedMetaboliteFeatures"); OnPropertyChanged("FoldChangeForBlankFiltering");

        }

        public void UpdateExcludedMassList(AnalysisParamOfMsdialGcms param) {
            if (param.ExcludedMassList != null && param.ExcludedMassList.Count != 0) {
                for (int i = 0; i < param.ExcludedMassList.Count; i++) {
                    this.excludedMassVMs[i].ExcludedMass = param.ExcludedMassList[i].ExcludedMass;
                    this.excludedMassVMs[i].MassTolerance = param.ExcludedMassList[i].MassTolerance;
                }
            }
        }

        public AnalysisParamOfMsdialGcms Param
        {
            get { return param; }
            set { param = value; }
        }

        #region data correction
        public float RetentionTimeBegin
        {
            get { return this.param.RetentionTimeBegin; }
            set { if (this.param.RetentionTimeBegin == value) return; this.param.RetentionTimeBegin = value; OnPropertyChanged("RetentionTimeBegin"); }
        }

        public float RetentionTimeEnd
        {
            get { return this.param.RetentionTimeEnd; }
            set { if (this.param.RetentionTimeEnd == value) return; this.param.RetentionTimeEnd = value; OnPropertyChanged("RetentionTimeEnd"); }
        }

        public float MassRangeBegin
        {
            get { return this.param.MassRangeBegin; }
            set { if (this.param.MassRangeBegin == value) return; this.param.MassRangeBegin = value; OnPropertyChanged("MassRangeBegin"); }
        }

        public float MassRangeEnd
        {
            get { return this.param.MassRangeEnd; }
            set { if (this.param.MassRangeEnd == value) return; this.param.MassRangeEnd = value; OnPropertyChanged("MassRangeEnd"); }
        }

        public int NumThreads {
            get { return this.param.NumThreads; }
            set { if (this.param.NumThreads == value) return; this.param.NumThreads = value; OnPropertyChanged("NumThreads"); }
        }
        #endregion

        #region peak detection
        public SmoothingMethod SmoothingMethod
        {
            get { return this.param.SmoothingMethod; }
            set { if (this.param.SmoothingMethod == value) return; this.param.SmoothingMethod = value; OnPropertyChanged("SmoothingMethod"); }
        }

        public int SmoothingLevel
        {
            get { return this.param.SmoothingLevel; }
            set { if (this.param.SmoothingLevel == value) return; this.param.SmoothingLevel = value; OnPropertyChanged("SmoothingLevel"); }
        }

        public int AveragePeakWidth
        {
            get { return this.param.AveragePeakWidth; }
            set { if (this.param.AveragePeakWidth == value) return; this.param.AveragePeakWidth = value; this.param.MinimumDatapoints = value * 0.5; OnPropertyChanged("AveragePeakWidth"); }
        }

        public float MassSliceWidth
        {
            get { return this.param.MassSliceWidth; }
            set { if (this.param.MassSliceWidth == value) return; this.param.MassSliceWidth = value; OnPropertyChanged("MassSliceWidth"); }
        }

        public double MinimumAmplitude
        {
            get { return this.param.MinimumAmplitude; }
            set { if (this.param.MinimumAmplitude == value) return; this.param.MinimumAmplitude = value; OnPropertyChanged("MinimumAmplitude"); }
        }

        public float MassAccuracy
        {
            get { return this.param.MassAccuracy; }
            set { if (this.param.MassAccuracy == value) return; this.param.MassAccuracy = value; OnPropertyChanged("MassAccuracy"); }
        }

        public bool IsAccurateMs
        {
            get { return isAccurateMs; }
            set 
            { 
                if (isAccurateMs == value) return;
                isAccurateMs = value;
                if (value == true)
                    this.param.AccuracyType = AccuracyType.IsAccurate;
                else
                    this.param.AccuracyType = AccuracyType.IsNominal;
                OnPropertyChanged("IsAccurateMs");
            }
        }

        public ObservableCollection<ExcludeMassVM> ExcludedMassViewModelCollection {
            get { return excludedMassVMs; }
            set { if (excludedMassVMs == value) return; excludedMassVMs = value; OnPropertyChanged("ExcludedMassViewModelCollection"); }
        }

        #endregion

        #region ms1dec
        [Range(0.001, 1.0)]
        public float SigmaWindowValue
        {
            get { return this.param.SigmaWindowValue; }
            set { if (this.param.SigmaWindowValue == value) return; this.param.SigmaWindowValue = value; OnPropertyChanged("SigmaWindowValue"); }
        }

        public float AmplitudeCutoff
        {
            get { return this.param.AmplitudeCutoff; }
            set { if (this.param.AmplitudeCutoff == value) return; this.param.AmplitudeCutoff = value; OnPropertyChanged("AmplitudeCutoff"); }
        }
        #endregion

        #region identification
        public string MspFilePath
        {
            get { return this.mspFilePath; }
            set { if (this.mspFilePath == value) return; this.mspFilePath = value; OnPropertyChanged("MspFilePath"); }
        }

        public string RiDictionaryFilePath
        {
            get { return this.riFilePath; }
            set { if (this.riFilePath == value) return; this.riFilePath = value; OnPropertyChanged("RiDictionaryFilePath"); }
        }

        public float RetentionTimeLibrarySearchTolerance
        {
            get { return this.param.RetentionTimeLibrarySearchTolerance; }
            set { if (this.param.RetentionTimeLibrarySearchTolerance == value) return; this.param.RetentionTimeLibrarySearchTolerance = value; OnPropertyChanged("RetentionTimeLibrarySearchTolerance"); }
        }

        public float RetentionIndexLibrarySearchTolerance
        {
            get { return this.param.RetentionIndexLibrarySearchTolerance; }
            set { if (this.param.RetentionIndexLibrarySearchTolerance == value) return; this.param.RetentionIndexLibrarySearchTolerance = value; OnPropertyChanged("RetentionIndexLibrarySearchTolerance"); }
        }

        public float EiSimilarityLibrarySearchCutOff
        {
            get { return this.param.EiSimilarityLibrarySearchCutOff; }
            set { if (this.param.EiSimilarityLibrarySearchCutOff == value) return; this.param.EiSimilarityLibrarySearchCutOff = value; OnPropertyChanged("EiSimilarityLibrarySearchCutOff"); }
        }

        public float MzLibrarySearchTolerance
        {
            get { return this.param.MzLibrarySearchTolerance; }
            set { if (this.param.MzLibrarySearchTolerance == value) return; this.param.MzLibrarySearchTolerance = value; OnPropertyChanged("MzLibrarySearchTolerance"); }
        }

        public float IdentificationScoreCutOff
        {
            get { return this.param.IdentificationScoreCutOff; }
            set { if (this.param.IdentificationScoreCutOff == value) return; this.param.IdentificationScoreCutOff = value; OnPropertyChanged("IdentificationScoreCutOff"); }
        }

        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get { return this.param.IsIdentificationOnlyPerformedForAlignmentFile; }
            set { if (this.param.IsIdentificationOnlyPerformedForAlignmentFile == value) return;
                this.param.IsIdentificationOnlyPerformedForAlignmentFile = value;
                OnPropertyChanged("IsIdentificationOnlyPerformedForAlignmentFile"); }
        }

        public bool IsUseRetentionInfoForIdentificationScoring {
            get { return this.param.IsUseRetentionInfoForIdentificationScoring; }
            set {
                if (this.param.IsUseRetentionInfoForIdentificationScoring == value) return;
                this.param.IsUseRetentionInfoForIdentificationScoring = value;
                OnPropertyChanged("IsUseRetentionInfoForIdentificationScoring");
            }
        }

        public bool IsReplaceQuantmassByUserDefinedValue {
            get { return this.param.IsReplaceQuantmassByUserDefinedValue; }
            set {
                if (this.param.IsReplaceQuantmassByUserDefinedValue == value) return;
                this.param.IsReplaceQuantmassByUserDefinedValue = value;
                OnPropertyChanged("IsReplaceQuantmassByUserDefinedValue");
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

        public bool IsOnlyTopHitReport {
            get { return Param.IsOnlyTopHitReport; }
            set {
                if (Param.IsOnlyTopHitReport == value) return;
                Param.IsOnlyTopHitReport = value;
                OnPropertyChanged("IsOnlyTopHitReport");
            }
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
            get { return this.param.AlignmentReferenceFileID; }
            set { if (this.param.AlignmentReferenceFileID == value) return; this.param.AlignmentReferenceFileID = value; OnPropertyChanged("AlignmentReferenceFileID"); }
        }

    

        public bool TogetherWithAlignment
        {
            get { return this.param.TogetherWithAlignment; }
            set { if (this.param.TogetherWithAlignment == value) return; this.param.TogetherWithAlignment = value; OnPropertyChanged("TogetherWithAlignment"); }
        }

        public float RetentionTimeAlignmentFactor
        {
            get { return this.param.RetentionTimeAlignmentFactor; }
            set { if (this.param.RetentionTimeAlignmentFactor == value) return; this.param.RetentionTimeAlignmentFactor = value; OnPropertyChanged("RetentionTimeAlignmentFactor"); }
        }

        public float EiSimilarityAlignmentFactor
        {
            get { return this.param.EiSimilarityAlignmentFactor; }
            set { if (this.param.EiSimilarityAlignmentFactor == value) return; this.param.EiSimilarityAlignmentFactor = value; OnPropertyChanged("EiSimilarityAlignmentFactor"); }
        }

        public float EiSimilarityAlignmentCutOff
        {
            get { return this.param.EiSimilarityAlignmentCutOff; }
            set { if (this.param.EiSimilarityAlignmentCutOff == value) return; this.param.EiSimilarityAlignmentCutOff = value; OnPropertyChanged("EiSimilarityAlignmentCutOff"); }
        }

        public float RetentionTimeAlignmentTolerance
        {
            get { return this.param.RetentionTimeAlignmentTolerance; }
            set { if (this.param.RetentionTimeAlignmentTolerance == value) return; this.param.RetentionTimeAlignmentTolerance = value; OnPropertyChanged("RetentionTimeAlignmentTolerance"); }
        }

        public float RetentionIndexAlignmentTolerance
        {
            get { return this.param.RetentionIndexAlignmentTolerance; }
            set { if (this.param.RetentionIndexAlignmentTolerance == value) return; this.param.RetentionIndexAlignmentTolerance = value; OnPropertyChanged("RetentionIndexAlignmentTolerance"); }
        }

        public float AlignmentScoreCutOff
        {
            get { return this.param.AlignmentScoreCutOff; }
            set { if (this.param.AlignmentScoreCutOff == value) return; this.param.AlignmentScoreCutOff = value; OnPropertyChanged("AlignmentScoreCutOff"); }
        }

        public bool IsForceInsertForGapFilling {
            get { return this.param.IsForceInsertForGapFilling; }
            set { if (this.param.IsForceInsertForGapFilling == value) return; this.param.IsForceInsertForGapFilling = value; OnPropertyChanged("IsForceInsertForGapFilling"); }
        }

        public bool IsRepresentativeQuantMassBasedOnBasePeakMz {
            get { return this.param.IsRepresentativeQuantMassBasedOnBasePeakMz; }
            set { if (this.param.IsRepresentativeQuantMassBasedOnBasePeakMz == value) return; this.param.IsRepresentativeQuantMassBasedOnBasePeakMz = value; OnPropertyChanged("IsRepresentativeQuantMassBasedOnBasePeakMz"); }
        }

        #endregion

        #region filtering
        public float PeakCountFilter {
            get { return this.param.PeakCountFilter; }
            set { if (this.param.PeakCountFilter == value) return; this.param.PeakCountFilter = value; OnPropertyChanged("PeakCountFilter"); }
        }

        public bool QcAtLeastFilter {
            get { return this.param.QcAtLeastFilter; }
            set { if (this.param.QcAtLeastFilter == value) return; this.param.QcAtLeastFilter = value; OnPropertyChanged("QcAtLeastFilter"); }
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

        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get { return Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples; }
            set { if (Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples == value) return; Param.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value; OnPropertyChanged("IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples"); }
        }

        public bool IsKeepAnnotatedMetaboliteFeatures {
            get { return Param.IsKeepAnnotatedMetaboliteFeatures; }
            set { if (Param.IsKeepAnnotatedMetaboliteFeatures == value) return; Param.IsKeepAnnotatedMetaboliteFeatures = value; OnPropertyChanged("IsKeepAnnotatedMetaboliteFeatures"); }
        }

        public BlankFiltering BlankFiltering {
            get { return Param.BlankFiltering; }
            set { if (Param.BlankFiltering == value) return; Param.BlankFiltering = value; OnPropertyChanged("BlankFiltering"); }
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

        #endregion
    }
}
