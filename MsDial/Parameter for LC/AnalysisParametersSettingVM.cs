using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace Rfx.Riken.OsakaUniv
{
    public class AnalysisParametersSettingVM: ViewModelBase
    {
        private MainWindow mainWindow;
        private Window window;

        private ProjectPropertyBean projectPropertyBean;
        private AnalysisParametersBean analysisParametersBean;
        private RdamPropertyBean rdamPropertyBean;
        private ObservableCollection<AdductIonInformationVM> adductIonInformationViewModelCollection;
        private ObservableCollection<ExcludeMassVM> excludedMassViewModelCollection;
        private ObservableCollection<AnalysisFileBean> analysisFileBeanCollection;
        private ObservableCollection<AlignmentFileBean> alignmentFileBeanCollection;

        private string libraryFilePath;
        private string alignmentResultFileName;
        private string postIdentificationLibraryFilePath;

        public AnalysisParametersSettingVM(MainWindow mainWindow, Window window)
        {
            this.mainWindow = mainWindow;
            this.window = window;

            this.projectPropertyBean = mainWindow.ProjectPropertyBean;
            this.analysisParametersBean = mainWindow.AnalysisParametersBean;
            this.rdamPropertyBean = mainWindow.RdamPropertyBean;
            this.analysisFileBeanCollection = mainWindow.AnalysisFileBeanCollection;
            this.alignmentFileBeanCollection = mainWindow.AlignmentFileBeanCollection;

            this.adductIonInformationViewModelCollection = new ObservableCollection<AdductIonInformationVM>();
            for (int i = 0; i < this.analysisParametersBean.AdductIonInformationBeanList.Count; i++)
                this.adductIonInformationViewModelCollection.Add(new AdductIonInformationVM() { AdductIonInformationBean = analysisParametersBean.AdductIonInformationBeanList[i] });
            
            this.excludedMassViewModelCollection = new ObservableCollection<ExcludeMassVM>();
            for (int i = 0; i < 100; i++)
                this.excludedMassViewModelCollection.Add(new ExcludeMassVM());
            
            DateTime dt = DateTime.Now;
            this.alignmentResultFileName = "alignmentResult" + "_" + dt.Year + "_" + dt.Month + "_" + dt.Day + "_" + dt.Hour + "_" + dt.Minute + "_" + dt.Second;
        }


        public void UpdateAdductList(List<AdductIonInformationBean> updateAdductList)
        {
            var updateAdduct = updateAdductList[updateAdductList.Count - 1];

            var updateAdductCollection = new ObservableCollection<AdductIonInformationVM>();
            foreach (var adduct in this.adductIonInformationViewModelCollection) updateAdductCollection.Add(adduct);

            updateAdductCollection.Add(new AdductIonInformationVM() { AdductIonInformationBean = updateAdduct });
            AdductIonInformationViewModelCollection = updateAdductCollection;
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
            if (adductIonInformationViewModelCollection[0].AdductIonInformationBean.Included == false)
            {
                MessageBox.Show("M + H or M - H must be included.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return false;
            }

            this.analysisParametersBean.ExcludedMassList = new List<ExcludeMassBean>();
            for (int i = 0; i < excludedMassViewModelCollection.Count; i++)
            {
                if (this.excludedMassViewModelCollection[i].ExcludedMass == null || this.excludedMassViewModelCollection[i].MassTolerance == null) continue;
                if (this.excludedMassViewModelCollection[i].ExcludedMass <= 0 || this.excludedMassViewModelCollection[i].MassTolerance <= 0) continue;
                this.analysisParametersBean.ExcludedMassList.Add(new ExcludeMassBean() { ExcludedMass = this.excludedMassViewModelCollection[i].ExcludedMass, MassTolerance = this.excludedMassViewModelCollection[i].MassTolerance });
            }

            this.analysisParametersBean.AdductIonInformationBeanList = new List<AdductIonInformationBean>();
            for (int i = 0; i < adductIonInformationViewModelCollection.Count; i++)
                this.analysisParametersBean.AdductIonInformationBeanList.Add(adductIonInformationViewModelCollection[i].AdductIonInformationBean);

            if (this.libraryFilePath != null && this.libraryFilePath != "")
            {
                this.projectPropertyBean.LibraryFilePath = this.libraryFilePath;

                DatabaseProcessing dp = new DatabaseProcessing();
                this.mainWindow.MspFormatCompoundInformationBeanList = dp.SetMspFormatCompoundInformationBeanList(this.libraryFilePath, mainWindow.IupacReferenceBean);
            }

            if (this.postIdentificationLibraryFilePath != null && this.postIdentificationLibraryFilePath != string.Empty)
            {
                this.projectPropertyBean.PostIdentificationLibraryFilePath = this.postIdentificationLibraryFilePath;

                DatabaseProcessing dp = new DatabaseProcessing();
                this.mainWindow.PostIdentificationReferenceBeanList = dp.SetTextFormatoCompoundInformationBeanList(this.postIdentificationLibraryFilePath);

                if (this.mainWindow.PostIdentificationReferenceBeanList == null || this.mainWindow.PostIdentificationReferenceBeanList.Count == 0) return false;
            }

            if (TogetherWithAlignment && this.analysisFileBeanCollection.Count > 1)
            {
                if (this.alignmentFileBeanCollection == null || this.alignmentFileBeanCollection.Count == 0)
                {
                    this.alignmentFileBeanCollection.Add(new AlignmentFileBean() { FileID = 0, FileName = this.alignmentResultFileName, FilePath = this.projectPropertyBean.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf });
                }
                else
                {
                    this.alignmentFileBeanCollection.Add(new AlignmentFileBean() { FileID = this.alignmentFileBeanCollection.Count, FileName = this.alignmentResultFileName, FilePath = this.projectPropertyBean.ProjectFolderPath + "\\" + this.alignmentResultFileName + "." + SaveFileFormat.arf });
                }
            }

            return true;
        }

        #region // Property
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

        public AnalysisParametersBean AnalysisParametersBean
        {
            get { return analysisParametersBean; }
            set { if (analysisParametersBean == value) return; analysisParametersBean = value; OnPropertyChanged("AnalysisParametersBean"); }
        }

        public ObservableCollection<AdductIonInformationVM> AdductIonInformationViewModelCollection
        {
            get { return adductIonInformationViewModelCollection; }
            set { adductIonInformationViewModelCollection = value; OnPropertyChanged("AdductIonInformationViewModelCollection"); }
        }

        public RdamPropertyBean RdamPropertyBean
        {
            get { return rdamPropertyBean; }
            set { if (rdamPropertyBean == value) return; rdamPropertyBean = value; OnPropertyChanged("RdamPropertyBean"); }
        }

        [Required(ErrorMessage = "Decide a file name")]
        public string AlignmentResultFileName
        {
            get { return alignmentResultFileName; }
            set { if (alignmentResultFileName == value) return; alignmentResultFileName = value; OnPropertyChanged("AlignmentResultFileName"); }
        }


        public SmoothingMethod SmoothingMethod
        {
            get { return AnalysisParametersBean.SmoothingMethod; }
            set { if (AnalysisParametersBean.SmoothingMethod == value) return; AnalysisParametersBean.SmoothingMethod = value; OnPropertyChanged("SmoothingMethod"); }
        }

        public double MinimumAmplitude
        {
            get { return AnalysisParametersBean.MinimumAmplitude; }
            set { if (AnalysisParametersBean.MinimumAmplitude == value) return; AnalysisParametersBean.MinimumAmplitude = value; OnPropertyChanged("MinimumAmplitude"); }
        }

        public int SmoothingLevel
        {
            get { return AnalysisParametersBean.SmoothingLevel; }
            set { if (AnalysisParametersBean.SmoothingLevel == value) return; AnalysisParametersBean.SmoothingLevel = value; OnPropertyChanged("SmoothingLevel"); }
        }

        public float MassSliceWidth
        {
            get { return AnalysisParametersBean.MassSliceWidth; }
            set { if (AnalysisParametersBean.MassSliceWidth == value) return; AnalysisParametersBean.MassSliceWidth = value; OnPropertyChanged("MassSliceWidth"); }
        }

        public double AmplitudeNoiseFactor
        {
            get { return AnalysisParametersBean.AmplitudeNoiseFactor; }
            set { if (AnalysisParametersBean.AmplitudeNoiseFactor == value) return; AnalysisParametersBean.AmplitudeNoiseFactor = value; OnPropertyChanged("AmplitudeNoiseFactor"); }
        }

        public double PeaktopNoiseFactor
        {
            get { return AnalysisParametersBean.PeaktopNoiseFactor; }
            set { if (AnalysisParametersBean.PeaktopNoiseFactor == value) return; AnalysisParametersBean.PeaktopNoiseFactor = value; OnPropertyChanged("PeaktopNoiseFactor"); }
        }

        public double SlopeNoiseFactor
        {
            get { return AnalysisParametersBean.SlopeNoiseFactor; }
            set { if (AnalysisParametersBean.SlopeNoiseFactor == value) return; AnalysisParametersBean.SlopeNoiseFactor = value; OnPropertyChanged("SlopeNoiseFactor"); }
        }

        public double MinimumDatapoints
        {
            get { return AnalysisParametersBean.MinimumDatapoints; }
            set { if (AnalysisParametersBean.MinimumDatapoints == value) return; AnalysisParametersBean.MinimumDatapoints = value; OnPropertyChanged("MinimumDatapoints"); }
        }

        public ObservableCollection<ExcludeMassVM> ExcludedMassViewModelCollection
        {
            get { return excludedMassViewModelCollection; }
            set { if (excludedMassViewModelCollection == value) return; excludedMassViewModelCollection = value; OnPropertyChanged("ExcludedMassViewModelCollection"); }
        }

        public float RetentionTimeBegin
        {
            get { return AnalysisParametersBean.RetentionTimeBegin; }
            set { if (AnalysisParametersBean.RetentionTimeBegin == value) return; AnalysisParametersBean.RetentionTimeBegin = value; OnPropertyChanged("RetentionTimeBegin"); }
        }

        public float RetentionTimeEnd
        {
            get { return AnalysisParametersBean.RetentionTimeEnd; }
            set { if (AnalysisParametersBean.RetentionTimeEnd == value) return; AnalysisParametersBean.RetentionTimeEnd = value; OnPropertyChanged("RetentionTimeEnd"); }
        }

        public float MassRangeBegin
        {
            get { return AnalysisParametersBean.MassRangeBegin; }
            set { if (AnalysisParametersBean.MassRangeBegin == value) return; AnalysisParametersBean.MassRangeBegin = value; OnPropertyChanged("MassRangeBegin"); }
        }

        public float MassRangeEnd
        {
            get { return AnalysisParametersBean.MassRangeEnd; }
            set { if (AnalysisParametersBean.MassRangeEnd == value) return; AnalysisParametersBean.MassRangeEnd = value; OnPropertyChanged("MassRangeEnd"); }
        }

        public float CentroidMs1Tolerance
        {
            get { return AnalysisParametersBean.CentroidMs1Tolerance; }
            set { if (AnalysisParametersBean.CentroidMs1Tolerance == value) return; AnalysisParametersBean.CentroidMs1Tolerance = value; OnPropertyChanged("CentroidMs1Tolerance"); }
        }

        public float CentroidMs2Tolerance
        {
            get { return AnalysisParametersBean.CentroidMs2Tolerance; }
            set { if (AnalysisParametersBean.CentroidMs2Tolerance == value) return; AnalysisParametersBean.CentroidMs2Tolerance = value; OnPropertyChanged("CentroidMs2Tolerance"); }
        }

        public bool PeakDetectionBasedCentroid
        {
            get { return AnalysisParametersBean.PeakDetectionBasedCentroid; }
            set { if (AnalysisParametersBean.PeakDetectionBasedCentroid == value) return; AnalysisParametersBean.PeakDetectionBasedCentroid = value; OnPropertyChanged("PeakDetectionBasedCentroid"); }
        }

        public float RetentionTimeAlignmentFactor
        {
            get { return AnalysisParametersBean.RetentionTimeAlignmentFactor; }
            set { if (RetentionTimeAlignmentFactor == value) return; AnalysisParametersBean.RetentionTimeAlignmentFactor = value; OnPropertyChanged("RetentionTimeAlignmentTolerance"); }
        }

        public float Ms1AlignmentFactor
        {
            get { return AnalysisParametersBean.Ms1AlignmentFactor; }
            set { if (AnalysisParametersBean.Ms1AlignmentFactor == value) return; AnalysisParametersBean.Ms1AlignmentFactor = value; OnPropertyChanged("Ms1AlignmentTolerance"); }
        }

        public float Ms2SimilarityAlignmentFactor
        {
            get { return AnalysisParametersBean.Ms2SimilarityAlignmentFactor; }
            set { if (AnalysisParametersBean.Ms2SimilarityAlignmentFactor == value) return; AnalysisParametersBean.Ms2SimilarityAlignmentFactor = value; OnPropertyChanged("Ms2SimilarityAlignmentTolerance"); }
        }

        public float RetentionTimeAlignmentTolerance
        {
            get { return AnalysisParametersBean.RetentionTimeAlignmentTolerance; }
            set { if (AnalysisParametersBean.RetentionTimeAlignmentTolerance == value) return; AnalysisParametersBean.RetentionTimeAlignmentTolerance = value; OnPropertyChanged("RetentionTimeAlignmentTolerance"); }
        }

        public float Ms1AlignmentTolerance
        {
            get { return AnalysisParametersBean.Ms1AlignmentTolerance; }
            set { if (AnalysisParametersBean.Ms1AlignmentTolerance == value) return; AnalysisParametersBean.Ms1AlignmentTolerance = value; OnPropertyChanged("Ms1AlignmentTolerance"); }
        }

        public float AlignmentScoreCutOff
        {
            get { return AnalysisParametersBean.AlignmentScoreCutOff; }
            set { if (AnalysisParametersBean.AlignmentScoreCutOff == value) return; AnalysisParametersBean.AlignmentScoreCutOff = value; OnPropertyChanged("AlignmentScoreCutOff"); }
        }

        public float RetentionTimeLibrarySearchTolerance
        {
            get { return AnalysisParametersBean.RetentionTimeLibrarySearchTolerance; }
            set { if (AnalysisParametersBean.RetentionTimeLibrarySearchTolerance == value) return; AnalysisParametersBean.RetentionTimeLibrarySearchTolerance = value; OnPropertyChanged("RetentionTimeLibrarySearchTolerance"); }
        }

        public float Ms1LibrarySearchTolerance
        {
            get { return AnalysisParametersBean.Ms1LibrarySearchTolerance; }
            set { if (AnalysisParametersBean.Ms1LibrarySearchTolerance == value) return; AnalysisParametersBean.Ms1LibrarySearchTolerance = value; OnPropertyChanged("Ms1LibrarySearchTolerance"); }
        }

        public float Ms2LibrarySearchTolerance
        {
            get { return AnalysisParametersBean.Ms2LibrarySearchTolerance; }
            set { if (AnalysisParametersBean.Ms2LibrarySearchTolerance == value) return; AnalysisParametersBean.Ms2LibrarySearchTolerance = value; OnPropertyChanged("Ms2LibrarySearchTolerance"); }
        }

        public float IdentificationScoreCutOff
        {
            get { return AnalysisParametersBean.IdentificationScoreCutOff; }
            set { if (AnalysisParametersBean.IdentificationScoreCutOff == value) return; AnalysisParametersBean.IdentificationScoreCutOff = value; OnPropertyChanged("IdentificationScoreCutOff"); }
        }

        public float RetentionTimeToleranceOfPostIdentification
        {
            get { return AnalysisParametersBean.RetentionTimeToleranceOfPostIdentification; }
            set { if (AnalysisParametersBean.RetentionTimeToleranceOfPostIdentification == value) return; AnalysisParametersBean.RetentionTimeToleranceOfPostIdentification = value; OnPropertyChanged("RetentionTimeToleranceOfPostIdentification"); }
        }

        public float AccurateMassToleranceOfPostIdentification
        {
            get { return AnalysisParametersBean.AccurateMassToleranceOfPostIdentification; }
            set { if (AnalysisParametersBean.AccurateMassToleranceOfPostIdentification == value) return; AnalysisParametersBean.AccurateMassToleranceOfPostIdentification = value; OnPropertyChanged("AccurateMassToleranceOfPostIdentification"); }
        }

        public float PostIdentificationScoreCutOff
        {
            get { return AnalysisParametersBean.PostIdentificationScoreCutOff; }
            set { if (AnalysisParametersBean.PostIdentificationScoreCutOff == value) return; AnalysisParametersBean.PostIdentificationScoreCutOff = value; OnPropertyChanged("PostIdentificationScoreCutOff"); }
        }

        public bool OnlyReportTopHitForPostAnnotation
        {
            get { return AnalysisParametersBean.OnlyReportTopHitForPostAnnotation; }
            set { if (AnalysisParametersBean.OnlyReportTopHitForPostAnnotation == value) return; AnalysisParametersBean.OnlyReportTopHitForPostAnnotation = value; OnPropertyChanged("OnlyReportTopHitForPostAnnotation"); }
        }

        public float RelativeAbundanceCutOff
        {
            get { return AnalysisParametersBean.RelativeAbundanceCutOff; }
            set { if (AnalysisParametersBean.RelativeAbundanceCutOff == value) return; AnalysisParametersBean.RelativeAbundanceCutOff = value; OnPropertyChanged("RelativeAbundanceCutOff"); }
        }

        public int BandWidth
        {
            get { return AnalysisParametersBean.BandWidth; }
            set { if (AnalysisParametersBean.BandWidth == value) return; AnalysisParametersBean.BandWidth = value; OnPropertyChanged("BandWidth"); }
        }

        public int SegmentNumber
        {
            get { return AnalysisParametersBean.SegmentNumber; }
            set { if (AnalysisParametersBean.SegmentNumber == value) return; AnalysisParametersBean.SegmentNumber = value; OnPropertyChanged("SegmentNumber"); }
        }

        public float SigmaWindowValue
        {
            get { return AnalysisParametersBean.SigmaWindowValue; }
            set { if (AnalysisParametersBean.SigmaWindowValue == value) return; AnalysisParametersBean.SigmaWindowValue = value; OnPropertyChanged("SigmaWindowValue"); }
        }

        public DeconvolutionType DeconvolutionType
        {
            get { return AnalysisParametersBean.DeconvolutionType; }
            set { if (AnalysisParametersBean.DeconvolutionType == value) return; AnalysisParametersBean.DeconvolutionType = value; OnPropertyChanged("DeconvolutionType"); }
        }

        public float AmplitudeCutoff
        {
            get { return AnalysisParametersBean.AmplitudeCutoff; }
            set { if (AnalysisParametersBean.AmplitudeCutoff == value) return; AnalysisParametersBean.AmplitudeCutoff = value; OnPropertyChanged("AmplitudeCutoff"); }
        }

        public bool RemoveAfterPrecursor
        {
            get { return AnalysisParametersBean.RemoveAfterPrecursor; }
            set { if (AnalysisParametersBean.RemoveAfterPrecursor == value) return; AnalysisParametersBean.RemoveAfterPrecursor = value; OnPropertyChanged("RemoveAfterPrecursor"); }
        }

        public float AdductAndIsotopeMassTolerance
        {
            get { return AnalysisParametersBean.AdductAndIsotopeMassTolerance; }
            set { if (AnalysisParametersBean.AdductAndIsotopeMassTolerance == value) return; AnalysisParametersBean.AdductAndIsotopeMassTolerance = value; OnPropertyChanged("AdductAndIsotopeMassTolerance"); }
        }

        public int AlignmentReferenceFileID
        {
            get { return AnalysisParametersBean.AlignmentReferenceFileID; }
            set { if (AnalysisParametersBean.AlignmentReferenceFileID == value) return; AnalysisParametersBean.AlignmentReferenceFileID = value; OnPropertyChanged("AlignmentReferenceFileID"); }
        }

        public float PeakCountFilter
        {
            get { return AnalysisParametersBean.PeakCountFilter; }
            set { if (AnalysisParametersBean.PeakCountFilter == value) return; AnalysisParametersBean.PeakCountFilter = value; OnPropertyChanged("PeakCountFilter"); }
        }

        public bool QcAtLeastFilter
        {
            get { return AnalysisParametersBean.QcAtLeastFilter; }
            set { if (AnalysisParametersBean.QcAtLeastFilter == value) return; AnalysisParametersBean.QcAtLeastFilter = value; OnPropertyChanged("QcAtLeastFilter"); }
        }

        public bool GapFillingOption
        {
            get { return AnalysisParametersBean.GapFillingOption; }
            set { if (AnalysisParametersBean.GapFillingOption == value) return; AnalysisParametersBean.GapFillingOption = value; OnPropertyChanged("GapFillingOption"); }
        }

        public bool TogetherWithAlignment
        {
            get { return AnalysisParametersBean.TogetherWithAlignment; }
            set { if (AnalysisParametersBean.TogetherWithAlignment == value) return; AnalysisParametersBean.TogetherWithAlignment = value; OnPropertyChanged("TogetherWithAlignment"); }
        }

        #endregion

    }
}
