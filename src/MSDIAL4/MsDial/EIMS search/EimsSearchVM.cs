using Msdial.Gcms.Dataprocess.Algorithm;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Windows;

namespace Rfx.Riken.OsakaUniv
{
    public class EimsSearchVM : ViewModelBase
    {
        #region // members
        private Window window;

        private ObservableCollection<EimsSearchMeasurmentVM> eimsSearchMeasurmentVMs;
        private ObservableCollection<EimsSearchReferenceVM> eimsSearchReferenceVMs;

        private MS1DecResult ms1DecResult;
        private List<MspFormatCompoundInformationBean> mspDB;

        private AnalysisParamOfMsdialGcms param;

        private int selectedFileId;
        private int selectedReferenceId;
        private int selectedReferenceViewModelId;
        private int focusedMs1DecId;
        private int focusedAlignmentId;
        private int maxMspDisplayNumber;

        private bool alignmentViewer;
        private bool peakViewer;

        private float retentionTimeTolerance;
        private float retentionIndexTolerance;
        private float massTolerance;
        #endregion

        #region // properties
        public ObservableCollection<EimsSearchMeasurmentVM> EimsSearchMeasurmentVMs
        {
            get { return eimsSearchMeasurmentVMs; }
            set { if (eimsSearchMeasurmentVMs == value) return; eimsSearchMeasurmentVMs = value; OnPropertyChanged("EimsSearchMeasurmentVMs"); }
        }

        public ObservableCollection<EimsSearchReferenceVM> EimsSearchReferenceVMs
        {
            get { return eimsSearchReferenceVMs; }
            set { if (eimsSearchReferenceVMs == value) return; eimsSearchReferenceVMs = value; OnPropertyChanged("EimsSearchReferenceVMs"); }
        }

        public MS1DecResult Ms1DecResult
        {
            get { return ms1DecResult; }
            set { if (ms1DecResult == value) return; ms1DecResult = value; OnPropertyChanged("Ms1DecResult"); }
        }

        public List<MspFormatCompoundInformationBean> MspDB
        {
            get { return mspDB; }
            set { if (mspDB == value) return; mspDB = value; OnPropertyChanged("MspDB"); }
        }

        public int SelectedFileId
        {
            get { return selectedFileId; }
            set { if (selectedFileId == value) return; selectedFileId = value; OnPropertyChanged("SelectedFileId"); }
        }

        public int SelectedReferenceId
        {
            get { return selectedReferenceId; }
            set { if (selectedReferenceId == value) return; selectedReferenceId = value; OnPropertyChanged("SelectedReferenceId"); }
        }

        public int SelectedReferenceViewModelId
        {
            get { return selectedReferenceViewModelId; }
            set { if (selectedReferenceViewModelId == value) return; selectedReferenceViewModelId = value; OnPropertyChanged("SelectedReferenceViewModelId"); }
        }

        public int FocusedMs1DecId
        {
            get { return focusedMs1DecId; }
            set { if (focusedMs1DecId == value) return; focusedMs1DecId = value; OnPropertyChanged("FocusedMs1DecId"); }
        }

        public int FocusedAlignmentId
        {
            get { return focusedAlignmentId; }
            set { if (focusedAlignmentId == value) return; focusedAlignmentId = value; OnPropertyChanged("FocusedAlignmentId"); }
        }

        public bool AlignmentViewer
        {
            get { return alignmentViewer; }
            set { if (alignmentViewer == value) return; alignmentViewer = value; OnPropertyChanged("AlignmentViewer"); }
        }

        public bool PeakViewer
        {
            get { return peakViewer; }
            set { if (peakViewer == value) return; peakViewer = value; OnPropertyChanged("PeakViewer"); }
        }

        public float RetentionTimeTolerance
        {
            get { return retentionTimeTolerance; }
            set { if (retentionTimeTolerance == value) return; retentionTimeTolerance = value; OnPropertyChanged("RetentionTimeTolerance"); }
        }

        public float RetentionIndexTolerance
        {
            get { return retentionIndexTolerance; }
            set { if (retentionIndexTolerance == value) return; retentionIndexTolerance = value; OnPropertyChanged("RetentionIndexTolerance"); }
        }

        public float MassTolerance
        {
            get { return massTolerance; }
            set { if (massTolerance == value) return; massTolerance = value; OnPropertyChanged("MassTolerance"); }
        }

        public int MaxMspDisplayNumber
        {
            get { return maxMspDisplayNumber; }
            set { maxMspDisplayNumber = value; }
        }

        #endregion

        #region // constructer
        public EimsSearchVM() { }

        public EimsSearchVM(AnalysisFileBean analysisFile, int focusedPeakId, AnalysisParamOfMsdialGcms param, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB)
        {
            this.selectedFileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            this.selectedReferenceId = ms1DecResult.MspDbID;

            this.focusedMs1DecId = focusedPeakId;

            this.peakViewer = true;
            this.alignmentViewer = false;

            this.param = param;
            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.retentionIndexTolerance = param.RetentionIndexLibrarySearchTolerance;
            this.massTolerance = param.MzLibrarySearchTolerance;
            this.maxMspDisplayNumber = param.MaxMspDisplayNumber;

            this.ms1DecResult = ms1DecResult;
            this.mspDB = mspDB;

            this.eimsSearchMeasurmentVMs = getEimsSearchMeasurmentVMs(analysisFile, ms1DecResult);
            SetCompoundSearchReferenceViewModelCollection();
        }

        public EimsSearchVM(AlignmentResultBean alignmentResult, int focusedAlignmentId, AnalysisParamOfMsdialGcms param, MS1DecResult ms1DecResult, List<MspFormatCompoundInformationBean> mspDB)
        {
            this.selectedFileId = alignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentId].RepresentativeFileID;
            this.selectedReferenceId = alignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentId].LibraryID;

            this.focusedAlignmentId = focusedAlignmentId;

            this.alignmentViewer = true;
            this.peakViewer = false;

            this.param = param;
            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.retentionIndexTolerance = param.RetentionIndexLibrarySearchTolerance;
            this.massTolerance = param.MzLibrarySearchTolerance;
            this.maxMspDisplayNumber = param.MaxMspDisplayNumber;

            this.ms1DecResult = ms1DecResult;
            this.mspDB = mspDB;

            this.eimsSearchMeasurmentVMs = getEimsSearchMeasurmentVMs(alignmentResult, focusedAlignmentId);
            SetCompoundSearchReferenceViewModelCollection();
        }
        #endregion

        #region // methods
        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            this.window.Close();
        }

        public string GetCompoundName(int id)
        {
            if (this.mspDB == null || this.mspDB.Count - 1 < id || id < 0) return "Unknown";
            else return this.mspDB[id].Name;
        }

        private ObservableCollection<EimsSearchMeasurmentVM> getEimsSearchMeasurmentVMs(AnalysisFileBean analysisFile, MS1DecResult ms1DecResult)
        {
            var eimsSearchMeasurmentVMs = new ObservableCollection<EimsSearchMeasurmentVM>();
            var eimsSearchMeasurmentVM = new EimsSearchMeasurmentVM();

            eimsSearchMeasurmentVM.FileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            eimsSearchMeasurmentVM.FileName = analysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            eimsSearchMeasurmentVM.FilePath = analysisFile.AnalysisFilePropertyBean.AnalysisFilePath;

            eimsSearchMeasurmentVM.QuantMass = ms1DecResult.BasepeakMz;
            eimsSearchMeasurmentVM.RetentionTime = ms1DecResult.RetentionTime;
            eimsSearchMeasurmentVM.RetentionIndex = ms1DecResult.RetentionIndex;
            eimsSearchMeasurmentVM.MspID = ms1DecResult.MspDbID;
            eimsSearchMeasurmentVM.MetaboliteName = GetCompoundName(ms1DecResult.MspDbID);

            eimsSearchMeasurmentVMs.Add(eimsSearchMeasurmentVM);

            return eimsSearchMeasurmentVMs;
        }

        private ObservableCollection<EimsSearchMeasurmentVM> getEimsSearchMeasurmentVMs(AlignmentResultBean alignmentResult, int foucsedAlignmentPeakId)
        {
            var eimsSearchMeasurmentVMs = new ObservableCollection<EimsSearchMeasurmentVM>();
            var eimsSearchMeasurmentVM = new EimsSearchMeasurmentVM();
            AlignmentPropertyBean alignmentProperty = alignmentResult.AlignmentPropertyBeanCollection[foucsedAlignmentPeakId];

            int repFileId = alignmentProperty.RepresentativeFileID;

            eimsSearchMeasurmentVM.FileId = alignmentProperty.RepresentativeFileID;
            eimsSearchMeasurmentVM.FileName = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].FileName;

            eimsSearchMeasurmentVM.QuantMass = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].QuantMass;
            eimsSearchMeasurmentVM.RetentionTime = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].RetentionTime;
            eimsSearchMeasurmentVM.RetentionIndex = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].RetentionIndex;
            eimsSearchMeasurmentVM.MspID = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].LibraryID;
            eimsSearchMeasurmentVM.MetaboliteName = alignmentProperty.MetaboliteName;

            eimsSearchMeasurmentVMs.Add(eimsSearchMeasurmentVM);

            return eimsSearchMeasurmentVMs;

        }

        public void SetCompoundSearchReferenceViewModelCollection()
        {
            this.EimsSearchReferenceVMs = new ObservableCollection<EimsSearchReferenceVM>();
            var eimsSearchReferenceVMs = new List<EimsSearchReferenceVM>();
            var eimsSearchReferenceVM = new EimsSearchReferenceVM();

            float retentionTime = this.ms1DecResult.RetentionTime;
            float retentionIndex = this.ms1DecResult.RetentionIndex;

            if (this.ms1DecResult.Spectrum.Count != 0) this.ms1DecResult.Spectrum = this.ms1DecResult.Spectrum.OrderBy(n => n.Mz).ToList();

            double totalSimilarity = 0, eiSimilarity = -1, dotProduct = -1, revDotProduct = -1, rtSimilarity = -1, riSimilarity = -1, presensePercent = -1;

            for (int i = 0; i < this.mspDB.Count; i++)
            {
                rtSimilarity = -1; eiSimilarity = -1; dotProduct = -1; revDotProduct = -1; riSimilarity = -1; presensePercent = -1;
                if (this.mspDB[i].RetentionTime >= 0) rtSimilarity = GcmsScoring.GetGaussianSimilarity(retentionTime, this.mspDB[i].RetentionTime, this.retentionTimeTolerance);
                if (this.mspDB[i].RetentionIndex >= 0) riSimilarity = GcmsScoring.GetGaussianSimilarity(retentionIndex, this.mspDB[i].RetentionIndex, this.retentionIndexTolerance);
                if (ms1DecResult.Spectrum.Count != 0)
                {
                    dotProduct = GcmsScoring.GetDotProduct(ms1DecResult.Spectrum, this.mspDB[i].MzIntensityCommentBeanList, this.massTolerance, this.param.MassRangeBegin, this.param.MassRangeEnd);
                    revDotProduct = GcmsScoring.GetReverseDotProduct(ms1DecResult.Spectrum, this.mspDB[i].MzIntensityCommentBeanList, this.massTolerance, this.param.MassRangeBegin, this.param.MassRangeEnd);
                    presensePercent = GcmsScoring.GetPresencePercentage(ms1DecResult.Spectrum, this.mspDB[i].MzIntensityCommentBeanList, this.massTolerance, this.param.MassRangeBegin, this.param.MassRangeEnd);
                    eiSimilarity = GcmsScoring.GetEiSpectraSimilarity(dotProduct, revDotProduct, presensePercent);
                }

                if (this.param.RetentionType == RetentionType.RI)
                    totalSimilarity = GcmsScoring.GetTotalSimilarity(riSimilarity, eiSimilarity, param.IsUseRetentionInfoForIdentificationScoring);
                else
                    totalSimilarity = GcmsScoring.GetTotalSimilarity(rtSimilarity, eiSimilarity, param.IsUseRetentionInfoForIdentificationScoring);

                eimsSearchReferenceVM = getEimsSearchReferenceVM(rtSimilarity, riSimilarity, eiSimilarity, dotProduct, revDotProduct, presensePercent, totalSimilarity, this.mspDB[i]);
                eimsSearchReferenceVMs.Add(eimsSearchReferenceVM);
            }

            this.mspDB = this.mspDB.OrderBy(n => n.Id).ToList();

            if (eimsSearchReferenceVMs.Count == 0)
            {
                this.EimsSearchReferenceVMs = new ObservableCollection<EimsSearchReferenceVM>();
            }
            else
            {
                eimsSearchReferenceVMs = eimsSearchReferenceVMs.OrderByDescending(n => n.TotalSimilarity).ToList();
                for (int i = 0; i < eimsSearchReferenceVMs.Count; i++)
                {
                    if (i >= this.maxMspDisplayNumber) break;
                    eimsSearchReferenceVMs[i].Id = i;
                }
                this.EimsSearchReferenceVMs = new ObservableCollection<EimsSearchReferenceVM>(eimsSearchReferenceVMs);
            }
        }

        private EimsSearchReferenceVM getEimsSearchReferenceVM(double rtSimilarity, double riSimilarity, double eiSimilarity, double dotProduct, double revDotProduct, double presensePercent, double totalSimilarity, MspFormatCompoundInformationBean msp)
        {
            var eimsSearchReferenceVM = new EimsSearchReferenceVM();
            eimsSearchReferenceVM.CompoundName = msp.Name;
            eimsSearchReferenceVM.RetentionIndex = msp.RetentionIndex;
            eimsSearchReferenceVM.RetentionTime = msp.RetentionTime;
            eimsSearchReferenceVM.RetentionIndexSimilarity = (float)riSimilarity;
            eimsSearchReferenceVM.RetentionTimeSimlarity = (float)rtSimilarity;
            eimsSearchReferenceVM.EiSpectraSimilarity = (float)eiSimilarity;
            eimsSearchReferenceVM.DotProduct = (float)dotProduct;
            eimsSearchReferenceVM.ReverseDotProduct = (float)revDotProduct;
            eimsSearchReferenceVM.PresenseSimilarity = (float)presensePercent;
            eimsSearchReferenceVM.TotalSimilarity = (float)totalSimilarity;
            eimsSearchReferenceVM.MspID = msp.Id;

            return eimsSearchReferenceVM;
        }
        #endregion
    }
}
