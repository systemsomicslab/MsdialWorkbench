using Msdial.Lcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess.Scoring;
using Msdial.Lcms.Dataprocess.Utility;
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
    public class MsmsSearchVM : ViewModelBase
    {
        #region // members
        private Window window;

        private ObservableCollection<MsmsSearchMeasurmentVM> msmsSearchMeasurmentVMs;
        private ObservableCollection<MsmsSearchReferenceVM> msmsSearchReferenceVMs;

        private MS2DecResult ms2DecResult;

        private List<MspFormatCompoundInformationBean> mspDB;

        private int selectedFileId;
        private int selectedReferenceId;
        private int selectedReferenceViewModelId;

        private int focusedPeakId;
        private int focusedAlignmentId;

        private bool alignmentViewer;
        private bool peakViewer;

        private float retentionTimeTolerance;
        private float ms1Tolerance;
        private float ms2Tolerance;

        private float massBegin;
        private float massEnd;

        private bool isUseRT;

        private TargetOmics targetOmics;
        #endregion

        #region // constructor
        public MsmsSearchVM() { }

        public MsmsSearchVM(AlignmentResultBean alignmentResult, int focusedAlignmentId, AnalysisParametersBean param, MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics)
        {
            this.selectedFileId = alignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentId].RepresentativeFileID;
            this.selectedReferenceId = alignmentResult.AlignmentPropertyBeanCollection[focusedAlignmentId].LibraryID;

            this.focusedAlignmentId = focusedAlignmentId;

            this.alignmentViewer = true;
            this.peakViewer = false;

            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.ms1Tolerance = param.Ms1LibrarySearchTolerance;
            this.ms2Tolerance = param.Ms2LibrarySearchTolerance;

            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;

            this.targetOmics = targetOmics;
            this.isUseRT = param.IsUseRetentionInfoForIdentificationScoring;

            this.massBegin = param.Ms2MassRangeBegin;
            this.massEnd = param.Ms2MassRangeEnd;

            this.msmsSearchMeasurmentVMs = getCompoundSearchMeasurmentInformationViewModelCollection(alignmentResult, focusedAlignmentId);
            SetCompoundSearchReferenceViewModelCollection();
        }

        public MsmsSearchVM(AlignmentResultBean alignmentResult, AlignedDriftSpotPropertyBean driftSpot, AnalysisParametersBean param, MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics) {

            this.selectedFileId = driftSpot.RepresentativeFileID;
            this.selectedReferenceId = driftSpot.LibraryID;

            this.focusedAlignmentId = driftSpot.MasterID;

            this.alignmentViewer = true;
            this.peakViewer = false;

            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.ms1Tolerance = param.Ms1LibrarySearchTolerance;
            this.ms2Tolerance = param.Ms2LibrarySearchTolerance;

            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;

            this.targetOmics = targetOmics;
            this.isUseRT = param.IsUseRetentionInfoForIdentificationScoring;

            this.massBegin = param.Ms2MassRangeBegin;
            this.massEnd = param.Ms2MassRangeEnd;

            this.msmsSearchMeasurmentVMs = getCompoundSearchMeasurmentInformationViewModelCollection(alignmentResult, driftSpot);
            SetCompoundSearchReferenceViewModelCollection();
        }

        public MsmsSearchVM(AnalysisFileBean analysisFile, int focusedPeakId, AnalysisParametersBean param, 
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics)
        {
            this.selectedFileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            this.selectedReferenceId = analysisFile.PeakAreaBeanCollection[focusedPeakId].LibraryID;

            this.focusedPeakId = focusedPeakId;

            this.peakViewer = true;
            this.alignmentViewer = false;

            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.ms1Tolerance = param.Ms1LibrarySearchTolerance;
            this.ms2Tolerance = param.Ms2LibrarySearchTolerance;

            this.massBegin = param.Ms2MassRangeBegin;
            this.massEnd = param.Ms2MassRangeEnd;

            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;

            this.targetOmics = targetOmics;
            this.isUseRT = param.IsUseRetentionInfoForIdentificationScoring;

            this.msmsSearchMeasurmentVMs = getCompoundSearchMeasurmentInformationViewModelCollection(analysisFile, focusedPeakId);
            SetCompoundSearchReferenceViewModelCollection();
        }

        public MsmsSearchVM(AnalysisFileBean analysisFile, DriftSpotBean driftSpot, AnalysisParametersBean param,
           MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics) {
            this.selectedFileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            this.selectedReferenceId = driftSpot.LibraryID;

            this.focusedPeakId = driftSpot.MasterPeakID;

            this.peakViewer = true;
            this.alignmentViewer = false;

            this.retentionTimeTolerance = param.RetentionTimeLibrarySearchTolerance;
            this.ms1Tolerance = param.Ms1LibrarySearchTolerance;
            this.ms2Tolerance = param.Ms2LibrarySearchTolerance;

            this.massBegin = param.Ms2MassRangeBegin;
            this.massEnd = param.Ms2MassRangeEnd;

            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;

            this.targetOmics = targetOmics;
            this.isUseRT = param.IsUseRetentionInfoForIdentificationScoring;

            this.msmsSearchMeasurmentVMs = getCompoundSearchMeasurmentInformationViewModelCollection(analysisFile, driftSpot);
            SetCompoundSearchReferenceViewModelCollection();
        }


        #endregion

        #region // properties
        public ObservableCollection<MsmsSearchMeasurmentVM> CompoundSearchMeasurmentInformationViewModelCollection
        {
            get { return msmsSearchMeasurmentVMs; }
            set { if (msmsSearchMeasurmentVMs == value) return; msmsSearchMeasurmentVMs = value; OnPropertyChanged("CompoundSearchMeasurmentInformationViewModelCollection"); }
        }

        public ObservableCollection<MsmsSearchReferenceVM> CompoundSearchReferenceInformationViewModelCollection
        {
            get { return msmsSearchReferenceVMs; }
            set { if (msmsSearchReferenceVMs == value) return; msmsSearchReferenceVMs = value; OnPropertyChanged("CompoundSearchReferenceInformationViewModelCollection"); }
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

        public float RetentionTimeTolerance
        {
            get { return retentionTimeTolerance; }
            set { if (retentionTimeTolerance == value) return; retentionTimeTolerance = value; OnPropertyChanged("RetentionTimeTolerance"); }
        }

        public float Ms1Tolerance
        {
            get { return ms1Tolerance; }
            set { if (ms1Tolerance == value) return; ms1Tolerance = value; OnPropertyChanged("Ms1Tolerance"); }
        }

        public float Ms2Tolerance
        {
            get { return ms2Tolerance; }
            set { if (ms2Tolerance == value) return; ms2Tolerance = value; OnPropertyChanged("Ms2Tolerance"); }
        }

        public bool AlignmentViewer
        {
            get { return alignmentViewer; }
            set { alignmentViewer = value; }
        }

        public bool PeakViewer
        {
            get { return peakViewer; }
            set { peakViewer = value; }
        }
        #endregion

        #region // method

        protected override void executeCommand(object parameter)
        {
            base.executeCommand(parameter);
            this.window.Close();
        }

        private ObservableCollection<MsmsSearchMeasurmentVM> getCompoundSearchMeasurmentInformationViewModelCollection(AnalysisFileBean analysisFile, int focusedPeakId)
        {
            ObservableCollection<MsmsSearchMeasurmentVM> msmsSearchMeasurmentVMs = new ObservableCollection<MsmsSearchMeasurmentVM>();
            MsmsSearchMeasurmentVM msmsSearchMeasurmentVM = new MsmsSearchMeasurmentVM();

            PeakAreaBean peakAreaBean = analysisFile.PeakAreaBeanCollection[focusedPeakId];

            msmsSearchMeasurmentVM.FileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            msmsSearchMeasurmentVM.FileName = analysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            msmsSearchMeasurmentVM.FilePath = analysisFile.AnalysisFilePropertyBean.AnalysisFilePath;

            msmsSearchMeasurmentVM.RetentionTime = peakAreaBean.RtAtPeakTop;
            msmsSearchMeasurmentVM.AccurateMass = peakAreaBean.AccurateMass;
            msmsSearchMeasurmentVM.AdductIonName = peakAreaBean.AdductIonName;
            msmsSearchMeasurmentVM.LibraryId = peakAreaBean.LibraryID;
            msmsSearchMeasurmentVM.MetaboliteName = peakAreaBean.MetaboliteName;

            msmsSearchMeasurmentVMs.Add(msmsSearchMeasurmentVM);

            return msmsSearchMeasurmentVMs;
        }

        private ObservableCollection<MsmsSearchMeasurmentVM> getCompoundSearchMeasurmentInformationViewModelCollection(AnalysisFileBean analysisFile, DriftSpotBean driftSpot) {
            ObservableCollection<MsmsSearchMeasurmentVM> msmsSearchMeasurmentVMs = new ObservableCollection<MsmsSearchMeasurmentVM>();
            MsmsSearchMeasurmentVM msmsSearchMeasurmentVM = new MsmsSearchMeasurmentVM();

            msmsSearchMeasurmentVM.FileId = analysisFile.AnalysisFilePropertyBean.AnalysisFileId;
            msmsSearchMeasurmentVM.FileName = analysisFile.AnalysisFilePropertyBean.AnalysisFileName;
            msmsSearchMeasurmentVM.FilePath = analysisFile.AnalysisFilePropertyBean.AnalysisFilePath;

            msmsSearchMeasurmentVM.RetentionTime = driftSpot.DriftTimeAtPeakTop;
            msmsSearchMeasurmentVM.AccurateMass = driftSpot.AccurateMass;
            msmsSearchMeasurmentVM.AdductIonName = driftSpot.AdductIonName;
            msmsSearchMeasurmentVM.LibraryId = driftSpot.LibraryID;
            msmsSearchMeasurmentVM.MetaboliteName = driftSpot.MetaboliteName;

            msmsSearchMeasurmentVMs.Add(msmsSearchMeasurmentVM);

            return msmsSearchMeasurmentVMs;
        }

        private ObservableCollection<MsmsSearchMeasurmentVM> getCompoundSearchMeasurmentInformationViewModelCollection(AlignmentResultBean alignmentResultBean, int foucsedAlignmentPeakId)
        {
            ObservableCollection<MsmsSearchMeasurmentVM> msmsSearchMeasurmentVMs = new ObservableCollection<MsmsSearchMeasurmentVM>();
            MsmsSearchMeasurmentVM msmsSearchMeasurmentVM = new MsmsSearchMeasurmentVM();
            AlignmentPropertyBean alignmentProperty = alignmentResultBean.AlignmentPropertyBeanCollection[foucsedAlignmentPeakId];

            int repFileId = alignmentProperty.RepresentativeFileID;

            msmsSearchMeasurmentVM.FileId = alignmentProperty.RepresentativeFileID;
            msmsSearchMeasurmentVM.FileName = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].FileName;
            msmsSearchMeasurmentVM.RetentionTime = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].RetentionTime;
            msmsSearchMeasurmentVM.AccurateMass = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].AccurateMass;
            msmsSearchMeasurmentVM.AdductIonName = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].AdductIonName;
            msmsSearchMeasurmentVM.LibraryId = alignmentProperty.AlignedPeakPropertyBeanCollection[repFileId].LibraryID;
            msmsSearchMeasurmentVM.MetaboliteName = alignmentProperty.MetaboliteName;

            msmsSearchMeasurmentVMs.Add(msmsSearchMeasurmentVM);

            return msmsSearchMeasurmentVMs;

        }

        private ObservableCollection<MsmsSearchMeasurmentVM> getCompoundSearchMeasurmentInformationViewModelCollection(AlignmentResultBean alignmentResultBean, AlignedDriftSpotPropertyBean driftSpot) {
            ObservableCollection<MsmsSearchMeasurmentVM> msmsSearchMeasurmentVMs = new ObservableCollection<MsmsSearchMeasurmentVM>();
            MsmsSearchMeasurmentVM msmsSearchMeasurmentVM = new MsmsSearchMeasurmentVM();

            int repFileId = driftSpot.RepresentativeFileID;

            msmsSearchMeasurmentVM.FileId = driftSpot.RepresentativeFileID;
            msmsSearchMeasurmentVM.FileName = driftSpot.AlignedPeakPropertyBeanCollection[repFileId].FileName;
            msmsSearchMeasurmentVM.RetentionTime = driftSpot.AlignedPeakPropertyBeanCollection[repFileId].DriftTime;
            msmsSearchMeasurmentVM.AccurateMass = driftSpot.AlignedPeakPropertyBeanCollection[repFileId].AccurateMass;
            msmsSearchMeasurmentVM.AdductIonName = driftSpot.AlignedPeakPropertyBeanCollection[repFileId].AdductIonName;
            msmsSearchMeasurmentVM.LibraryId = driftSpot.AlignedPeakPropertyBeanCollection[repFileId].LibraryID;
            msmsSearchMeasurmentVM.MetaboliteName = driftSpot.MetaboliteName;

            msmsSearchMeasurmentVMs.Add(msmsSearchMeasurmentVM);

            return msmsSearchMeasurmentVMs;

        }

        public void SetCompoundSearchReferenceViewModelCollection()
        {
            this.CompoundSearchReferenceInformationViewModelCollection = new ObservableCollection<MsmsSearchReferenceVM>();
            List<MsmsSearchReferenceVM> msmsSearchReferenceVMs = new List<MsmsSearchReferenceVM>();
            MsmsSearchReferenceVM msmsSearchReferenceVM = new MsmsSearchReferenceVM();

            float retentionTime = this.ms2DecResult.PeakTopRetentionTime;
            float accurateMass = this.ms2DecResult.Ms1AccurateMass;

            this.mspDB = this.mspDB.OrderBy(n => n.PrecursorMz).ToList();
            if (this.ms2DecResult.MassSpectra.Count != 0) this.ms2DecResult.MassSpectra = this.ms2DecResult.MassSpectra.OrderBy(n => n[0]).ToList();

            int startIndex = DataAccessLcUtility.GetDatabaseStartIndex(accurateMass, this.ms1Tolerance, this.mspDB);
            double totalSimilarity = 0, spectraSimilarity = -1, reverseSearchSimilarity = -1, rtSimilarity = -1, accurateMassSimilarity = -1, presenseSimilarity = -1, isotopeSimilarity = -1;

            for (int i = startIndex; i < this.mspDB.Count; i++)
            {
                if (this.mspDB[i].PrecursorMz > accurateMass + this.ms1Tolerance) break;
                if (this.mspDB[i].PrecursorMz < accurateMass - this.ms1Tolerance) continue;

                rtSimilarity = -1; isotopeSimilarity = -1; spectraSimilarity = -1; reverseSearchSimilarity = -1; accurateMassSimilarity = -1; presenseSimilarity = -1;

                accurateMassSimilarity = Math.Exp(-0.5 * Math.Pow((accurateMass - this.mspDB[i].PrecursorMz) / this.ms1Tolerance, 2));
                if (this.mspDB[i].RetentionTime >= 0) rtSimilarity = Math.Exp(-0.5 * Math.Pow((retentionTime - this.mspDB[i].RetentionTime) / this.retentionTimeTolerance, 2));
                if (ms2DecResult.MassSpectra.Count != 0)
                {
                    spectraSimilarity = LcmsScoring.GetMassSpectraSimilarity(new ObservableCollection<double[]>(this.ms2DecResult.MassSpectra), this.mspDB[i].MzIntensityCommentBeanList, 
                        this.ms2Tolerance, this.massBegin, this.massEnd);
                    reverseSearchSimilarity = LcmsScoring.GetReverseSearchSimilarity(new ObservableCollection<double[]>(this.ms2DecResult.MassSpectra), this.mspDB[i].MzIntensityCommentBeanList, 
                        this.ms2Tolerance, this.massBegin, this.massEnd);
                    presenseSimilarity = LcmsScoring.GetPresenceSimilarity(new ObservableCollection<double[]>(this.ms2DecResult.MassSpectra), this.mspDB[i].MzIntensityCommentBeanList, 
                        this.ms2Tolerance, this.massBegin, this.massEnd);
                }

                var spectrumPenalty = false;
                if (this.mspDB[i].MzIntensityCommentBeanList != null && this.mspDB[i].MzIntensityCommentBeanList.Count <= 1) spectrumPenalty = true;

                totalSimilarity = LcmsScoring.GetTotalSimilarity(accurateMassSimilarity, rtSimilarity, isotopeSimilarity, 
                    spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, spectrumPenalty, this.targetOmics, this.isUseRT);

                msmsSearchReferenceVM = getCompoundSearchReferenceInformationViewModel(rtSimilarity, accurateMassSimilarity, spectraSimilarity, reverseSearchSimilarity, presenseSimilarity, totalSimilarity, this.mspDB[i]);
                msmsSearchReferenceVMs.Add(msmsSearchReferenceVM);
            }

            this.mspDB = this.mspDB.OrderBy(n => n.Id).ToList();

            if (msmsSearchReferenceVMs.Count == 0)
            {
                this.CompoundSearchReferenceInformationViewModelCollection = new ObservableCollection<MsmsSearchReferenceVM>();
            }
            else
            {
                msmsSearchReferenceVMs = msmsSearchReferenceVMs.OrderByDescending(n => n.TotalSimilarity).ToList();
                for (int i = 0; i < msmsSearchReferenceVMs.Count; i++) msmsSearchReferenceVMs[i].Id = i;
                
                this.CompoundSearchReferenceInformationViewModelCollection = new ObservableCollection<MsmsSearchReferenceVM>(msmsSearchReferenceVMs);
            }
        }

        private MsmsSearchReferenceVM getCompoundSearchReferenceInformationViewModel(double rtSimilarity, 
            double accurateMassSimilarity, double spectraSimilarity, double reverseSearchSimilarity, 
            double presenseSimilarity, double totalSimilarity, MspFormatCompoundInformationBean msp)
        {
            MsmsSearchReferenceVM msmsSearchReferenceVM = new MsmsSearchReferenceVM();
            msmsSearchReferenceVM.CompoundName = msp.Name;
            msmsSearchReferenceVM.AccurateMass = msp.PrecursorMz;
            msmsSearchReferenceVM.RetentionTime = msp.RetentionTime;
            msmsSearchReferenceVM.AccurateMassSimilarity = (float)accurateMassSimilarity;
            msmsSearchReferenceVM.RetentionTimeSimlarity = (float)rtSimilarity;
            msmsSearchReferenceVM.DotProduct = (float)spectraSimilarity;
            msmsSearchReferenceVM.ReverseDotProduct = (float)reverseSearchSimilarity;
            msmsSearchReferenceVM.PresenseSimilarity = (float)presenseSimilarity;
            msmsSearchReferenceVM.TotalSimilarity = (float)totalSimilarity;
            msmsSearchReferenceVM.LibraryId = msp.Id;
            msmsSearchReferenceVM.Instrument = msp.InstrumentType;
            msmsSearchReferenceVM.Comment = msp.Comment;
            msmsSearchReferenceVM.Adduct = msp.AdductIonBean != null && msp.AdductIonBean.AdductIonName != string.Empty ? msp.AdductIonBean.AdductIonName : "null";

            return msmsSearchReferenceVM;
        }
        #endregion

    }
}