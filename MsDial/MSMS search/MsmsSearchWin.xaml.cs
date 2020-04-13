using Msdial.Lcms.Dataprocess.Algorithm;
using Riken.Metabolomics.Lipidomics;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// CompoundSearchWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class MsmsSearchWin : Window
    {
        private AnalysisFileBean analysisFile;
        private AlignmentResultBean alignmentResult;

        private List<MspFormatCompoundInformationBean> mspDB;
        private MS2DecResult ms2DecResult;
        private AnalysisParametersBean param;

        private int focusedPeakId;
        private int parentSpotId;
        private DriftSpotBean driftSpot;
        private AlignedDriftSpotPropertyBean alignmentDriftSpot;
        private TargetOmics targetOmics;

        public MsmsSearchWin()
        {
            InitializeComponent();
        }

        public MsmsSearchWin(AlignmentResultBean alignmentResult, int focusedDriftSpotId, 
            ObservableCollection<AlignedDriftSpotPropertyBean> alignedDriftSpots, AnalysisParametersBean param, 
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics)
        {
            InitializeComponent();
            
            this.alignmentResult = alignmentResult;
            this.alignmentDriftSpot = alignedDriftSpots[focusedDriftSpotId];
            this.focusedPeakId = alignmentDriftSpot.MasterID;
            this.parentSpotId = alignmentDriftSpot.AlignmentSpotID;
            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.targetOmics = targetOmics;
            this.DataContext = new MsmsSearchVM(alignmentResult, this.alignmentDriftSpot, param, ms2DecResult, mspDB, targetOmics);

            MassSpectrogramViewModel mass2SpectrogramViewModel = GetMs2MassspectrogramViewModel(this.ms2DecResult, ((MsmsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
            
            labelRefresh();
        }

        public MsmsSearchWin(AlignmentResultBean alignmentResult, int focusedAlignmentPeakId, AnalysisParametersBean param,
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics) {
            InitializeComponent();

            this.alignmentResult = alignmentResult;
            this.focusedPeakId = focusedAlignmentPeakId;
            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.targetOmics = targetOmics;
            this.DataContext = new MsmsSearchVM(alignmentResult, focusedAlignmentPeakId, param, ms2DecResult, mspDB, targetOmics);

            MassSpectrogramViewModel mass2SpectrogramViewModel = GetMs2MassspectrogramViewModel(this.ms2DecResult, ((MsmsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

            labelRefresh();
        }

        public MsmsSearchWin(AnalysisFileBean analysisFile, int focusedPeakId, AnalysisParametersBean param, 
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics)
        {
            InitializeComponent();
            
            this.analysisFile = analysisFile;
            this.focusedPeakId = focusedPeakId;
            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.targetOmics = targetOmics;
            this.DataContext = new MsmsSearchVM(analysisFile, focusedPeakId, param, ms2DecResult, mspDB, targetOmics);

            MassSpectrogramViewModel mass2SpectrogramViewModel = GetMs2MassspectrogramViewModel(this.ms2DecResult, ((MsmsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);
            
            labelRefresh();
        }

        public MsmsSearchWin(AnalysisFileBean analysisFile, int focusedDriftSpotId,
            ObservableCollection<DriftSpotBean> driftSpots, DriftSpotBean driftSpot,
            AnalysisParametersBean param,
            MS2DecResult ms2DecResult, List<MspFormatCompoundInformationBean> mspDB, TargetOmics targetOmics) {
            InitializeComponent();

            this.analysisFile = analysisFile;
            this.focusedPeakId = focusedDriftSpotId;
            this.parentSpotId = driftSpot.PeakAreaBeanID;
            this.ms2DecResult = ms2DecResult;
            this.mspDB = mspDB;
            this.param = param;
            this.driftSpot = driftSpot;
            this.targetOmics = targetOmics;
            this.DataContext = new MsmsSearchVM(analysisFile, driftSpot, param, ms2DecResult, mspDB, targetOmics);

            MassSpectrogramViewModel mass2SpectrogramViewModel = GetMs2MassspectrogramViewModel(this.ms2DecResult, ((MsmsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

            labelRefresh();
        }

        private void Button_ReAnalysis_Click(object sender, RoutedEventArgs e)
        {
            if (this.TextBox_RetentionTimeTolerance.Text == string.Empty || this.TextBox_Ms1Tolerance.Text == string.Empty || this.TextBox_Ms2Tolerance.Text == string.Empty)
            {
                MessageBox.Show("The search tolerance should be included in textbox.", "Error", MessageBoxButton.OK, MessageBoxImage.Error);
                return;
            }

            ((MsmsSearchVM)this.DataContext).SetCompoundSearchReferenceViewModelCollection();
        }

        private void Button_Confidence_Click(object sender, RoutedEventArgs e)
        {
            int libraryID = ((MsmsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchVM)this.DataContext).SelectedReferenceViewModelId;
            var queries = ((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection;
            if (queries == null || queries.Count == 0) return;
            if (((MsmsSearchVM)this.DataContext).PeakViewer)
            {
                if (this.param.IsIonMobility) {
                    this.driftSpot.LibraryID = libraryID;
                    this.driftSpot.MetaboliteName = getMetaboliteName(this.mspDB[libraryID]);
                    this.driftSpot.RtSimilarityValue = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                    this.driftSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                    this.driftSpot.ReverseSearchSimilarityValue = queries[selectedRowId].ReverseDotProduct * 1000;
                    this.driftSpot.MassSpectraSimilarityValue = queries[selectedRowId].DotProduct * 1000;
                    this.driftSpot.PresenseSimilarityValue = queries[selectedRowId].PresenseSimilarity * 1000;
                    this.driftSpot.TotalScore = queries[selectedRowId].TotalSimilarity * 1000;
                    this.driftSpot.PostIdentificationLibraryId = -1;
                    this.driftSpot.IsMs1Match = true;
                    this.driftSpot.IsMs2Match = true;
                    Identification.SetAdductIonInformation(this.driftSpot, this.mspDB[libraryID]);
                }
                else {
                    var peakSpot = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId];
                    peakSpot.LibraryID = libraryID;
                    peakSpot.MetaboliteName = getMetaboliteName(this.mspDB[libraryID]);
                    peakSpot.RtSimilarityValue = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                    peakSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                    peakSpot.ReverseSearchSimilarityValue = queries[selectedRowId].ReverseDotProduct * 1000;
                    peakSpot.MassSpectraSimilarityValue = queries[selectedRowId].DotProduct * 1000;
                    peakSpot.PresenseSimilarityValue = queries[selectedRowId].PresenseSimilarity * 1000;
                    peakSpot.TotalScore = queries[selectedRowId].TotalSimilarity * 1000;
                    peakSpot.PostIdentificationLibraryId = -1;
                    peakSpot.IsMs1Match = true;
                    peakSpot.IsMs2Match = true;
                    Identification.SetAdductIonInformation(peakSpot, this.mspDB[libraryID]);
                }
            }
            else
            {
                if (this.param.IsIonMobility) {
                    this.alignmentDriftSpot.LibraryID = libraryID;
                    this.alignmentDriftSpot.MetaboliteName = getMetaboliteName(this.mspDB[libraryID]);
                    this.alignmentDriftSpot.CcsSimilarity = queries[selectedRowId].CcsSimilarity * 1000;
                    this.alignmentDriftSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                    this.alignmentDriftSpot.ReverseSimilarity = queries[selectedRowId].ReverseDotProduct * 1000;
                    this.alignmentDriftSpot.MassSpectraSimilarity = queries[selectedRowId].DotProduct * 1000;
                    this.alignmentDriftSpot.AccurateMassSimilarity = queries[selectedRowId].PresenseSimilarity * 1000;
                    this.alignmentDriftSpot.TotalSimilairty = queries[selectedRowId].TotalSimilarity * 1000;
                    this.alignmentDriftSpot.IsMs1Match = true;
                    this.alignmentDriftSpot.IsMs2Match = true;
                    this.alignmentDriftSpot.PostIdentificationLibraryID = -1;
                    this.alignmentDriftSpot.IsManuallyModifiedForAnnotation = true;
                    PeakAlignment.SetAdductIonInformation(this.alignmentDriftSpot, this.mspDB[libraryID]);

                    updateAlignmentSpotMasterAsAnnotated(this.alignmentResult.AlignmentPropertyBeanCollection[this.parentSpotId]);
                }
                else {

                    var alignmentSpot = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId];
                    alignmentSpot.LibraryID = libraryID;
                    alignmentSpot.MetaboliteName = getMetaboliteName(this.mspDB[libraryID]);
                    alignmentSpot.ReverseSimilarity = queries[selectedRowId].ReverseDotProduct * 1000;
                    alignmentSpot.MassSpectraSimilarity = queries[selectedRowId].DotProduct * 1000;
                    alignmentSpot.FragmentPresencePercentage = queries[selectedRowId].PresenseSimilarity * 1000;
                    alignmentSpot.RetentionTimeSimilarity = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                    alignmentSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                    alignmentSpot.TotalSimilairty = queries[selectedRowId].TotalSimilarity * 1000;
                    alignmentSpot.PostIdentificationLibraryID = -1;
                    alignmentSpot.IsMs1Match = true;
                    alignmentSpot.IsMs2Match = true;
                    alignmentSpot.IsManuallyModifiedForAnnotation = true;
                    PeakAlignment.SetAdductIonInformation(alignmentSpot, this.mspDB[libraryID]);
                }
            }
            
            this.Close();
        }

        private string getMetaboliteName(MspFormatCompoundInformationBean query) {
            if (this.targetOmics == TargetOmics.Lipidomics) {
                var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(query);
                if (molecule == null || molecule.SublevelLipidName == null || molecule.LipidName == null) {
                    return query.Name; // for others and splash etc in compoundclass
                }
                if (molecule.SublevelLipidName == molecule.LipidName) {
                    return molecule.SublevelLipidName;
                }
                else {
                    return molecule.SublevelLipidName + "|" + molecule.LipidName;
                }
            }
            else {
                return query.Name;
            }

        }

        private void Button_Unsettled_Click(object sender, RoutedEventArgs e)
        {
            int libraryID = ((MsmsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchVM)this.DataContext).SelectedReferenceViewModelId;
            var queries = ((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection;
            if (((MsmsSearchVM)this.DataContext).PeakViewer)
            {
                if (this.param.IsIonMobility) {
                    if (queries != null && queries.Count > 0) {
                        this.driftSpot.LibraryID = libraryID;
                        this.driftSpot.MetaboliteName = "Unsettled: " + queries[selectedRowId].CompoundName;
                        this.driftSpot.RtSimilarityValue = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                        this.driftSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                        this.driftSpot.ReverseSearchSimilarityValue = queries[selectedRowId].ReverseDotProduct * 1000;
                        this.driftSpot.MassSpectraSimilarityValue = queries[selectedRowId].DotProduct * 1000;
                        this.driftSpot.PresenseSimilarityValue = queries[selectedRowId].PresenseSimilarity * 1000;
                        this.driftSpot.TotalScore = queries[selectedRowId].TotalSimilarity * 1000;
                        this.driftSpot.PostIdentificationLibraryId = -1;
                        Identification.SetAdductIonInformation(this.driftSpot, this.mspDB[libraryID]);
                    }
                    else {
                        this.driftSpot.MetaboliteName = "Unsettled: " + this.driftSpot.MetaboliteName;
                        this.driftSpot.PostIdentificationLibraryId = -1;
                    }
                }
                else {
                    var peakSpot = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId];
                    if (queries != null && queries.Count > 0) {
                        peakSpot.LibraryID = libraryID;
                        peakSpot.MetaboliteName = "Unsettled: " + queries[selectedRowId].CompoundName;
                        peakSpot.RtSimilarityValue = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                        peakSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                        peakSpot.ReverseSearchSimilarityValue = queries[selectedRowId].ReverseDotProduct * 1000;
                        peakSpot.MassSpectraSimilarityValue = queries[selectedRowId].DotProduct * 1000;
                        peakSpot.PresenseSimilarityValue = queries[selectedRowId].PresenseSimilarity * 1000;
                        peakSpot.TotalScore = queries[selectedRowId].TotalSimilarity * 1000;
                        peakSpot.PostIdentificationLibraryId = -1;
                        Identification.SetAdductIonInformation(peakSpot, this.mspDB[libraryID]);
                    }
                    else {
                        peakSpot.MetaboliteName = "Unsettled: " + peakSpot.MetaboliteName;
                        peakSpot.PostIdentificationLibraryId = -1;
                    }
                }
            }
            else
            {
                if (this.param.IsIonMobility) {
                    if (queries != null && queries.Count > 0) {
                        this.alignmentDriftSpot.LibraryID = libraryID;
                        this.alignmentDriftSpot.MetaboliteName = "Unsettled: " + queries[selectedRowId].CompoundName;
                        this.alignmentDriftSpot.CcsSimilarity = queries[selectedRowId].CcsSimilarity * 1000;
                        this.alignmentDriftSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                        this.alignmentDriftSpot.ReverseSimilarity = queries[selectedRowId].ReverseDotProduct * 1000;
                        this.alignmentDriftSpot.MassSpectraSimilarity = queries[selectedRowId].DotProduct * 1000;
                        this.alignmentDriftSpot.AccurateMassSimilarity = queries[selectedRowId].PresenseSimilarity * 1000;
                        this.alignmentDriftSpot.TotalSimilairty = queries[selectedRowId].TotalSimilarity * 1000;
                        this.alignmentDriftSpot.PostIdentificationLibraryID = -1;
                        PeakAlignment.SetAdductIonInformation(this.alignmentDriftSpot, this.mspDB[libraryID]);
                    }
                    else {
                        this.alignmentDriftSpot.MetaboliteName = "Unsettled: " + this.alignmentDriftSpot.MetaboliteName;
                        this.alignmentDriftSpot.PostIdentificationLibraryID = -1;
                    }
                    this.alignmentDriftSpot.IsManuallyModifiedForAnnotation = true;
                }
                else {
                    var alignmentSpot = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId];
                    if (queries != null && queries.Count > 0) {
                        alignmentSpot.LibraryID = libraryID;
                        alignmentSpot.MetaboliteName = "Unsettled: " + queries[selectedRowId].CompoundName;
                        alignmentSpot.ReverseSimilarity = queries[selectedRowId].ReverseDotProduct * 1000;
                        alignmentSpot.MassSpectraSimilarity = queries[selectedRowId].DotProduct * 1000;
                        alignmentSpot.FragmentPresencePercentage = queries[selectedRowId].PresenseSimilarity * 1000;
                        alignmentSpot.RetentionTimeSimilarity = queries[selectedRowId].RetentionTimeSimlarity * 1000;
                        alignmentSpot.AccurateMassSimilarity = queries[selectedRowId].AccurateMassSimilarity * 1000;
                        alignmentSpot.TotalSimilairty = queries[selectedRowId].TotalSimilarity * 1000;
                        alignmentSpot.PostIdentificationLibraryID = -1;
                        PeakAlignment.SetAdductIonInformation(alignmentSpot, this.mspDB[libraryID]);
                    }
                    else {
                        alignmentSpot.MetaboliteName = "Unsettled: " + alignmentSpot.MetaboliteName;
                        alignmentSpot.PostIdentificationLibraryID = -1;
                    }
                    alignmentSpot.IsManuallyModifiedForAnnotation = true;
                }
            }
           
            this.Close();

        }

        private void Button_Unknown_Click(object sender, RoutedEventArgs e)
        {
            int libraryID = ((MsmsSearchVM)this.DataContext).SelectedReferenceId;
            int selectedRowId = ((MsmsSearchVM)this.DataContext).SelectedReferenceViewModelId;

            if (((MsmsSearchVM)this.DataContext).PeakViewer)
            {
                if (this.param.IsIonMobility) {
                    this.driftSpot.LibraryID = -1;
                    this.driftSpot.MetaboliteName = string.Empty;
                    this.driftSpot.RtSimilarityValue = -1;
                    this.driftSpot.AccurateMassSimilarity = -1;
                    this.driftSpot.ReverseSearchSimilarityValue = -1;
                    this.driftSpot.MassSpectraSimilarityValue = -1;
                    this.driftSpot.PresenseSimilarityValue = -1;
                    this.driftSpot.TotalScore = -1;
                    this.driftSpot.PostIdentificationLibraryId = -1;
                    this.driftSpot.IsMs1Match = false;
                    this.driftSpot.IsMs2Match = false;
                }
                else {
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].LibraryID = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MetaboliteName = string.Empty;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].RtSimilarityValue = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].AccurateMassSimilarity = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].ReverseSearchSimilarityValue = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MassSpectraSimilarityValue = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].PresenseSimilarityValue = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].TotalScore = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].PostIdentificationLibraryId = -1;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].IsMs1Match = false;
                    this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].IsMs2Match = false;
                }
            }
            else
            {
                if (this.param.IsIonMobility) {

                    this.alignmentDriftSpot.LibraryID = -1;
                    this.alignmentDriftSpot.MetaboliteName = string.Empty;
                    this.alignmentDriftSpot.ReverseSimilarity = -1;
                    this.alignmentDriftSpot.MassSpectraSimilarity = -1;
                    this.alignmentDriftSpot.FragmentPresencePercentage = -1;
                    this.alignmentDriftSpot.RetentionTimeSimilarity = -1;
                    this.alignmentDriftSpot.AccurateMassSimilarity = -1;
                    this.alignmentDriftSpot.TotalSimilairty = -1;
                    this.alignmentDriftSpot.IsMs1Match = false;
                    this.alignmentDriftSpot.IsMs2Match = false;
                    this.alignmentDriftSpot.IsCcsMatch = false;
                    this.alignmentDriftSpot.IsRtMatch = false;
                    this.alignmentDriftSpot.PostIdentificationLibraryID = -1;
                    this.alignmentDriftSpot.IsManuallyModifiedForAnnotation = true;

                    updateAlignmentSpotMasterAsUnknown(this.alignmentResult.AlignmentPropertyBeanCollection[this.parentSpotId]);
                }
                else {
                    var alignmentSpot = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId];
                    alignmentSpot.LibraryID = -1;
                    alignmentSpot.MetaboliteName = string.Empty;
                    alignmentSpot.ReverseSimilarity = -1;
                    alignmentSpot.MassSpectraSimilarity = -1;
                    alignmentSpot.FragmentPresencePercentage = -1;
                    alignmentSpot.RetentionTimeSimilarity = -1;
                    alignmentSpot.AccurateMassSimilarity = -1;
                    alignmentSpot.TotalSimilairty = -1;
                    alignmentSpot.PostIdentificationLibraryID = -1;
                    alignmentSpot.IsMs1Match = false;
                    alignmentSpot.IsMs2Match = false;
                    alignmentSpot.IsManuallyModifiedForAnnotation = true;
                }
            }
            
            this.Close();
        }

        private void updateAlignmentSpotMasterAsAnnotated(AlignmentPropertyBean spot) {
            var maxScoreID = -1;
            var maxScore = double.MinValue;
            for (int i = 0; i < spot.AlignedDriftSpots.Count; i++) {
                var drift = spot.AlignedDriftSpots[i];
                if (drift.LibraryID >= 0 && drift.IsMs2Match) {
                    if (maxScore < drift.TotalSimilairty) {
                        maxScore = drift.TotalSimilairty;
                        maxScoreID = i;
                    }
                }
            }
            if (maxScoreID >= 0) {
                var drift = spot.AlignedDriftSpots[maxScoreID];
                spot.LibraryID = drift.LibraryID;
                spot.MetaboliteName = drift.MetaboliteName;
                spot.ReverseSimilarity = drift.ReverseSimilarity;
                spot.MassSpectraSimilarity = drift.MassSpectraSimilarity;
                spot.FragmentPresencePercentage = drift.FragmentPresencePercentage;
                spot.RetentionTimeSimilarity = drift.RetentionTimeSimilarity;
                spot.CcsSimilarity = drift.CcsSimilarity;
                spot.AccurateMassSimilarity = drift.AccurateMassSimilarity;
                spot.TotalSimilairty = drift.TotalSimilairty;
                spot.PostIdentificationLibraryID = -1;
                spot.IsMs1Match = true;
                spot.IsMs2Match = true;
                spot.IsManuallyModifiedForAnnotation = true;

                PeakAlignment.SetAdductIonInformation(spot, this.mspDB[spot.LibraryID]);
            }
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
            }
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void labelRefresh()
        {
            this.Label_SelectedLibraryID.Content = ((MsmsSearchVM)this.DataContext).SelectedReferenceId;

            if (((MsmsSearchVM)this.DataContext).PeakViewer)
            {
                if (this.param.IsIonMobility) {
                    this.Label_PeakInformation_AnnotatedMetabolite.Text = this.driftSpot.MetaboliteName;
                    this.Label_PeakInformation_RtSimilarity.Text = this.driftSpot.CcsSimilarity.ToString();
                    this.Label_PeakInformation_Ms1Similarity.Text = this.driftSpot.AccurateMassSimilarity.ToString();
                    this.Label_PeakInformation_DotProduct.Text = this.driftSpot.MassSpectraSimilarityValue.ToString();
                    this.Label_PeakInformation_ReverseDotProduct.Text = this.driftSpot.ReverseSearchSimilarityValue.ToString();
                    this.Label_PeakInformation_TotalScore.Text = this.driftSpot.TotalScore.ToString();
                }
                else {
                    this.Label_PeakInformation_AnnotatedMetabolite.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MetaboliteName;
                    this.Label_PeakInformation_RtSimilarity.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].RtSimilarityValue.ToString();
                    this.Label_PeakInformation_Ms1Similarity.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].AccurateMassSimilarity.ToString();
                    this.Label_PeakInformation_DotProduct.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].MassSpectraSimilarityValue.ToString();
                    this.Label_PeakInformation_ReverseDotProduct.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].ReverseSearchSimilarityValue.ToString();
                    this.Label_PeakInformation_TotalScore.Text = this.analysisFile.PeakAreaBeanCollection[this.focusedPeakId].TotalScore.ToString();
                }
            }
            else
            {
                if (this.param.IsIonMobility) {
                    this.Label_PeakInformation_AnnotatedMetabolite.Text = this.alignmentDriftSpot.MetaboliteName;
                    this.Label_PeakInformation_RtSimilarity.Text = this.alignmentDriftSpot.RetentionTimeSimilarity.ToString();
                    this.Label_PeakInformation_Ms1Similarity.Text = this.alignmentDriftSpot.AccurateMassSimilarity.ToString();
                    this.Label_PeakInformation_DotProduct.Text = this.alignmentDriftSpot.MassSpectraSimilarity.ToString();
                    this.Label_PeakInformation_ReverseDotProduct.Text = this.alignmentDriftSpot.ReverseSimilarity.ToString();
                    this.Label_PeakInformation_TotalScore.Text = this.alignmentDriftSpot.TotalSimilairty.ToString();
                }
                else {
                    this.Label_PeakInformation_AnnotatedMetabolite.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MetaboliteName;
                    this.Label_PeakInformation_RtSimilarity.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].RetentionTimeSimilarity.ToString();
                    this.Label_PeakInformation_Ms1Similarity.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].AccurateMassSimilarity.ToString();
                    this.Label_PeakInformation_DotProduct.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].MassSpectraSimilarity.ToString();
                    this.Label_PeakInformation_ReverseDotProduct.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].ReverseSimilarity.ToString();
                    this.Label_PeakInformation_TotalScore.Text = this.alignmentResult.AlignmentPropertyBeanCollection[this.focusedPeakId].TotalSimilairty.ToString();
                }
               
            }
        }

        private void labelRefresh(int selectedRowId)
        {
            this.Label_SelectedLibraryID.Content = ((MsmsSearchVM)this.DataContext).SelectedReferenceId;

            this.Label_PeakInformation_AnnotatedMetabolite.Text = ((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].CompoundName;
            this.Label_PeakInformation_RtSimilarity.Text = (((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].RetentionTimeSimlarity * 1000).ToString();
            this.Label_PeakInformation_Ms1Similarity.Text = (((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].AccurateMassSimilarity * 1000).ToString();
            this.Label_PeakInformation_DotProduct.Text = (((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].DotProduct * 1000).ToString();
            this.Label_PeakInformation_ReverseDotProduct.Text = (((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].ReverseDotProduct * 1000).ToString();
            this.Label_PeakInformation_TotalScore.Text = (((MsmsSearchVM)this.DataContext).CompoundSearchReferenceInformationViewModelCollection[selectedRowId].TotalSimilarity * 1000).ToString();
        }

        private MassSpectrogramViewModel GetMs2MassspectrogramViewModel(MS2DecResult deconvolutionResultBean, int libraryId)
        {
            float targetRt = deconvolutionResultBean.PeakTopRetentionTime;
            MassSpectrogramBean referenceSpectraBean;
            MassSpectrogramBean massSpectrogramBean = getMassSpectrogramBean(deconvolutionResultBean);

            string graphTitle = "MS2 spectra " + "Precursor: " + Math.Round(deconvolutionResultBean.Ms1AccurateMass, 5).ToString();

            if (this.mspDB != null && this.mspDB.Count != 0)
                referenceSpectraBean = getReferenceSpectra(this.mspDB, libraryId);
            else
                referenceSpectraBean = new MassSpectrogramBean(Brushes.Red, 2.0, null);

            return new MassSpectrogramViewModel(massSpectrogramBean, referenceSpectraBean, MassSpectrogramIntensityMode.Relative, 0, targetRt, graphTitle);
        }

        private MassSpectrogramBean getMassSpectrogramBean(MS2DecResult deconvolutionResultBean)
        {
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectraDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();
            List<double[]> masslist = new List<double[]>();

            if (deconvolutionResultBean.MassSpectra.Count == 0)
            {
                masslist.Add(new double[] { 0, 0 });
            }
            else
            {
                for (int i = 0; i < deconvolutionResultBean.MassSpectra.Count; i++)
                    masslist.Add(new double[] { deconvolutionResultBean.MassSpectra[i][0], deconvolutionResultBean.MassSpectra[i][1] });
            }

            masslist = masslist.OrderBy(n => n[0]).ToList();

            for (int i = 0; i < masslist.Count; i++)
                massSpectraDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = masslist[i][0], Intensity = masslist[i][1], Label = Math.Round(masslist[i][0], 4).ToString() });

            return new MassSpectrogramBean(Brushes.Black, 2.0, new ObservableCollection<double[]>(masslist), massSpectraDisplayLabelCollection);
        }

        private MassSpectrogramBean getReferenceSpectra(List<MspFormatCompoundInformationBean> mspFormatCompoundInformationBeanList, int libraryID)
        {
            ObservableCollection<double[]> masslist = new ObservableCollection<double[]>();
            ObservableCollection<MassSpectrogramDisplayLabel> massSpectrogramDisplayLabelCollection = new ObservableCollection<MassSpectrogramDisplayLabel>();

            if (libraryID < 0) return new MassSpectrogramBean(Brushes.Red, 1.0, null);

            for (int i = 0; i < mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList.Count; i++)
            {
                masslist.Add(new double[] { (double)mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList[i].Mz, (double)mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList[i].Intensity });
                massSpectrogramDisplayLabelCollection.Add(new MassSpectrogramDisplayLabel() { Mass = (double)mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList[i].Mz, Intensity = (double)mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList[i].Intensity, Label = mspFormatCompoundInformationBeanList[libraryID].MzIntensityCommentBeanList[i].Comment });
            }


            return new MassSpectrogramBean(Brushes.Red, 2.0, masslist, massSpectrogramDisplayLabelCollection);
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void DataGrid_LibraryInformation_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (this.DataGrid_LibraryInformation.SelectedItem == null) return;

            ((MsmsSearchVM)this.DataContext).SelectedReferenceId = ((MsmsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).LibraryId;
            ((MsmsSearchVM)this.DataContext).SelectedReferenceViewModelId = ((MsmsSearchReferenceVM)this.DataGrid_LibraryInformation.SelectedItem).Id;

            MassSpectrogramViewModel mass2SpectrogramViewModel = GetMs2MassspectrogramViewModel(this.ms2DecResult, ((MsmsSearchVM)this.DataContext).SelectedReferenceId);
            this.MeasVsRefMassSpectrogramUI.Content = new MassSpectrogramWithReferenceUI(mass2SpectrogramViewModel);

            labelRefresh(((MsmsSearchVM)this.DataContext).SelectedReferenceViewModelId);
        }
       
    }
}
