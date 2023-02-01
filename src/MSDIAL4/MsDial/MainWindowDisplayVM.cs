using Msdial.Gcms.Dataprocess.Algorithm;
using Msdial.Lcms.Dataprocess;
using Msdial.Lcms.Dataprocess.Utility;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Media.Imaging;

namespace Rfx.Riken.OsakaUniv
{
    public class MainWindowDisplayVM : ViewModelBase
    {
        MainWindow mainWindow;

        #region//member
        private PairwisePlotDisplayLabel displayLabel;
        private bool identifiedFilter;
        private bool annotatedFilter;
        private bool molecularIonFilter;
        private bool msmsFilter;
        private bool unknownFilter;
        private float ampSliderLowerValue;
        private float ampSliderUpperValue;
        private bool uniqueionFilter;
        private bool blankFilter;
        private bool ccsFilter;

        
        #endregion

        #region//property
        public PairwisePlotDisplayLabel DisplayLabel
        {
            get { return displayLabel; }
            set { displayLabel = value; }
        }

        public bool IdentifiedFilter
        {
            get { return identifiedFilter; }
            set { identifiedFilter = value; }
        }

        public bool AnnotatedFilter
        {
            get { return annotatedFilter; }
            set { annotatedFilter = value; }
        }

        public bool MolecularIonFilter
        {
            get { return molecularIonFilter; }
            set { molecularIonFilter = value; }
        }

        public bool MsmsFilter
        {
            get { return msmsFilter; }
            set { msmsFilter = value; }
        }

        public bool UnknownFilter
        {
            get { return unknownFilter; }
            set { unknownFilter = value; }
        }

        public float AmpSliderLowerValue
        {
            get { return ampSliderLowerValue; }
            set { ampSliderLowerValue = value; }
        }

        public float AmpSliderUpperValue
        {
            get { return ampSliderUpperValue; }
            set { ampSliderUpperValue = value; }
        }

        public bool UniqueionFilter {
            get {
                return uniqueionFilter;
            }

            set {
                uniqueionFilter = value;
            }
        }

        public bool BlankFilter {
            get {
                return blankFilter;
            }

            set {
                blankFilter = value;
            }
        }

        public bool CcsFilter {
            get { return ccsFilter; }
            set { ccsFilter = value; }
        }

       


        #endregion

        public MainWindowDisplayVM(MainWindow mainWindow)
        {
            this.mainWindow = mainWindow;
            this.displayLabel = PairwisePlotDisplayLabel.None;
            this.identifiedFilter = false;
            this.annotatedFilter = false;
            this.molecularIonFilter = false;
            this.msmsFilter = false;
            this.ccsFilter = false;
            this.unknownFilter = false;
            this.uniqueionFilter = false;
            this.ampSliderLowerValue = 0;
            this.ampSliderUpperValue = 100;
            this.blankFilter = false;
        }

        public void DriftViewRefresh() {
            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.AnalysisFiles == null) return;
                if (this.mainWindow.FocusedFileID < 0) return;
                if (this.mainWindow.DriftTimeMzPairwisePlotPeakViewUI.Content.GetType() != typeof(PairwisePlotPeakViewUI)) return;

                var mobilityContent = (PairwisePlotPeakViewUI)this.mainWindow.DriftTimeMzPairwisePlotPeakViewUI.Content;
                var mobilityPlot = mobilityContent.PairwisePlotBean;

                if (mobilityPlot.XAxisDatapointCollection == null || mobilityPlot.XAxisDatapointCollection.Count == 0) return;

                mobilityPlot.DisplayLabel = displayLabel;
                mobilityPlot.IdentifiedOnlyDisplayFilter = identifiedFilter;
                mobilityPlot.AnnotatedOnlyDisplayFilter = annotatedFilter;
                mobilityPlot.MolcularIonFilter = molecularIonFilter;
                mobilityPlot.MsmsOnlyDisplayFilter = msmsFilter;
                mobilityPlot.CcsFilter = ccsFilter;
                mobilityPlot.UnknownFilter = unknownFilter;
                mobilityPlot.UniqueionFilter = uniqueionFilter;
                mobilityPlot.AmplitudeDisplayLowerFilter = (float)ampSliderLowerValue;
                mobilityPlot.AmplitudeDisplayUpperFilter = (float)ampSliderUpperValue;
                mobilityPlot.BlankFilter = blankFilter;
                mobilityContent.RefreshUI();
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {
                if (this.mainWindow.AlignmentFiles == null) return;
                if (this.mainWindow.FocusedAlignmentFileID < 0) return;

                var mobilityContent = (PairwisePlotAlignmentViewUI)this.mainWindow.DriftTimeMzPairwiseAlignmentViewUI.Content;
                var mobilityPlot = mobilityContent.PairwisePlotBean;

                if (mobilityPlot.XAxisDatapointCollection == null || mobilityPlot.XAxisDatapointCollection.Count == 0) return;

                mobilityPlot.DisplayLabel = displayLabel;
                mobilityPlot.IdentifiedOnlyDisplayFilter = identifiedFilter;
                mobilityPlot.AnnotatedOnlyDisplayFilter = annotatedFilter;
                mobilityPlot.MolcularIonFilter = molecularIonFilter;
                mobilityPlot.MsmsOnlyDisplayFilter = msmsFilter;
                mobilityPlot.CcsFilter = ccsFilter;
                mobilityPlot.UnknownFilter = unknownFilter;
                mobilityPlot.UniqueionFilter = uniqueionFilter;
                mobilityPlot.AmplitudeDisplayLowerFilter = (float)ampSliderLowerValue;
                mobilityPlot.AmplitudeDisplayUpperFilter = (float)ampSliderUpperValue;
                mobilityPlot.BlankFilter = blankFilter;
                mobilityContent.RefreshUI();
            }
        }

        public void Refresh() // refresh pairwise plots
        {
            changeTableViewerSetting();
            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.AnalysisFiles == null) return;
                if (this.mainWindow.FocusedFileID < 0) return;

                var content = (PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content;
                var pairwisePlotBean = content.PairwisePlotBean;

                if (pairwisePlotBean.XAxisDatapointCollection == null || pairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                pairwisePlotBean.DisplayLabel = displayLabel;
                pairwisePlotBean.IdentifiedOnlyDisplayFilter = identifiedFilter;
                pairwisePlotBean.AnnotatedOnlyDisplayFilter = annotatedFilter;
                pairwisePlotBean.MolcularIonFilter = molecularIonFilter;
                pairwisePlotBean.MsmsOnlyDisplayFilter = msmsFilter;
                pairwisePlotBean.CcsFilter = ccsFilter;
                pairwisePlotBean.UnknownFilter = unknownFilter;
                pairwisePlotBean.UniqueionFilter = uniqueionFilter;
                pairwisePlotBean.AmplitudeDisplayLowerFilter = (float)ampSliderLowerValue;
                pairwisePlotBean.AmplitudeDisplayUpperFilter = (float)ampSliderUpperValue;
                pairwisePlotBean.BlankFilter = blankFilter;
                content.RefreshUI();

                var spotCount = countDisplayedSpots(pairwisePlotBean, this.mainWindow.PairwisePlotFocus, this.mainWindow.ProjectProperty.Ionization);
                this.mainWindow.Label_DisplayPeakNum.Content = "Num. " + spotCount;

                if (this.mainWindow.AnalysisParamForLC.IsIonMobility == true) {
                    DriftViewRefresh();
                }
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {
                if (this.mainWindow.AlignmentFiles == null) return;
                if (this.mainWindow.FocusedAlignmentFileID < 0) return;

                var content = (PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content;
                var pairwisePlotBean = ((PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content).PairwisePlotBean;

                if (pairwisePlotBean.XAxisDatapointCollection == null || pairwisePlotBean.XAxisDatapointCollection.Count == 0) return;

                pairwisePlotBean.DisplayLabel = displayLabel;
                pairwisePlotBean.IdentifiedOnlyDisplayFilter = identifiedFilter;
                pairwisePlotBean.AnnotatedOnlyDisplayFilter = annotatedFilter;
                pairwisePlotBean.MolcularIonFilter = molecularIonFilter;
                pairwisePlotBean.MsmsOnlyDisplayFilter = msmsFilter;
                pairwisePlotBean.CcsFilter = ccsFilter;
                pairwisePlotBean.UnknownFilter = unknownFilter;
                pairwisePlotBean.UniqueionFilter = uniqueionFilter;
                pairwisePlotBean.AmplitudeDisplayLowerFilter = (float)ampSliderLowerValue;
                pairwisePlotBean.AmplitudeDisplayUpperFilter = (float)ampSliderUpperValue;
                pairwisePlotBean.BlankFilter = blankFilter;
                content.RefreshUI();

                var spotCount = countDisplayedSpots(pairwisePlotBean, this.mainWindow.PairwisePlotFocus, this.mainWindow.ProjectProperty.Ionization);
                this.mainWindow.Label_DisplayPeakNum.Content = "Num. " + spotCount;

                if (this.mainWindow.AnalysisParamForLC.IsIonMobility == true) {
                    DriftViewRefresh();
                }
            }

            MetaDataLabelRefresh();
        }

        public void MetaDataLabelRefresh()
        {
            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var ms1DecID = this.mainWindow.FocusedMS1DecID;
                    var ms1DecResults = this.mainWindow.Ms1DecResults;

                    if (ms1DecResults != null && ms1DecID >= 0 && ms1DecID < ms1DecResults.Count) {

                        var ms1DecResult = ms1DecResults[ms1DecID];

                        labelUiSetting(ms1DecResult);

                        this.mainWindow.DisplayFocusId = ms1DecID;
                        this.mainWindow.DisplayFocusRt = Math.Round(ms1DecResult.RetentionTime, 3);
                        this.mainWindow.DisplayFocusMz = Math.Round(ms1DecResult.BasepeakMz, 5);
                    }
                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {
                    
                    var fileID = this.mainWindow.FocusedFileID;
                    var files = this.mainWindow.AnalysisFiles;
                    var param = this.mainWindow.AnalysisParamForLC;

                    if (files != null && fileID >= 0) {
                        var peakAreas = files[fileID].PeakAreaBeanCollection;
                        var peakID = this.mainWindow.FocusedPeakID;

                        if (peakAreas != null && peakID >= 0 && peakID < peakAreas.Count) {

                            if (this.mainWindow.AnalysisParamForLC.IsIonMobility) {
                                var peakSpot = peakAreas[peakID];
                                var driftSpot = this.mainWindow.SelectedPeakViewDriftSpot;
                                if (driftSpot == null) return;

                                labelUiSetting(peakSpot, driftSpot);

                                this.mainWindow.DisplayFocusId = driftSpot.MasterPeakID;
                                this.mainWindow.DisplayFocusRt = Math.Round(driftSpot.DriftTimeAtPeakTop, 3);
                                this.mainWindow.DisplayFocusMz = Math.Round(driftSpot.AccurateMass, 5);
                            }
                            else {
                                var peak = peakAreas[peakID];

                                labelUiSetting(peak);

                                this.mainWindow.DisplayFocusId = peakID;
                                this.mainWindow.DisplayFocusRt = Math.Round(peak.RtAtPeakTop, 3);
                                this.mainWindow.DisplayFocusMz = Math.Round(peak.AccurateMass, 5);
                            }
                        }
                    }
                }
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {
                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var alignmentResult = this.mainWindow.FocusedAlignmentResult;
                    if (alignmentResult != null) {

                        var peakID = this.mainWindow.FocusedAlignmentPeakID;
                        var ms1DecID = this.mainWindow.FocusedAlignmentMs1DecID;

                        var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                        var alignedMs1Decs = this.mainWindow.FocusedAlignmentMS1DecResults;

                        if (alignedSpots != null && alignedMs1Decs != null && 
                            peakID >= 0 && ms1DecID >= 0 && peakID < alignedSpots.Count && ms1DecID < alignedMs1Decs.Count) {

                            labelUiSetting(alignedSpots[peakID], alignedMs1Decs[ms1DecID]);

                            this.mainWindow.DisplayFocusId = peakID;
                            this.mainWindow.DisplayFocusMz = Math.Round(alignedSpots[peakID].QuantMass, 5);

                            if (this.mainWindow.AnalysisParamForGC.AlignmentIndexType == AlignmentIndexType.RT)
                                this.mainWindow.DisplayFocusRt = Math.Round(alignedSpots[peakID].CentralRetentionTime, 3);
                            else
                                this.mainWindow.DisplayFocusRt = Math.Round(alignedSpots[peakID].CentralRetentionIndex, 1);
                        }
                    }
                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                    var alignmentResult = this.mainWindow.FocusedAlignmentResult;
                    if (alignmentResult != null) {
                        var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                        var peakID = this.mainWindow.FocusedAlignmentPeakID;

                        if (alignedSpots != null && peakID >= 0 && peakID < alignedSpots.Count) {


                            if (this.mainWindow.AnalysisParamForLC.IsIonMobility) {
                                var peakSpot = alignedSpots[peakID];
                                var driftSpot = this.mainWindow.SelectedAlignmentViewDriftSpot;
                                if (driftSpot == null) return;

                                labelUiSetting(peakSpot, driftSpot);

                                this.mainWindow.DisplayFocusId = driftSpot.MasterID;
                                this.mainWindow.DisplayFocusRt = Math.Round(driftSpot.CentralDriftTime, 3);
                                this.mainWindow.DisplayFocusMz = Math.Round(driftSpot.CentralAccurateMass, 5);
                            }
                            else {
                                labelUiSetting(alignedSpots[peakID]);

                                this.mainWindow.DisplayFocusId = peakID;
                                this.mainWindow.DisplayFocusRt = Math.Round(alignedSpots[peakID].CentralRetentionTime, 3);
                                this.mainWindow.DisplayFocusMz = Math.Round(alignedSpots[peakID].CentralAccurateMass, 5);
                            }
                        }
                    }
                }
            }
        }

        public void FocusNext() {
            var rtTol = 0.5F;
            var imTol = 0.5F;
            var mzTol = 20F;

            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.FocusedFileID < 0)
                    return;

                var content = (PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                #region
                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var ms1DecResults = this.mainWindow.Ms1DecResults;
                    var peakID = this.mainWindow.FocusedMS1DecID + 1;
                    refreshPeakViewEims(content, peakID, ms1DecResults, rtTol, mzTol);

                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                    var fileID = this.mainWindow.FocusedFileID;
                    var files = this.mainWindow.AnalysisFiles;
                    var param = this.mainWindow.AnalysisParamForLC;

                    if (files != null && fileID >= 0) {
                        if (param.IsIonMobility) {

                            var contentIM = (PairwisePlotPeakViewUI)this.mainWindow.DriftTimeMzPairwisePlotPeakViewUI.Content;
                            if (contentIM.PairwisePlotBean.XAxisDatapointCollection == null ||
                                contentIM.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                                return;
                            var peakAreas = files[fileID].PeakAreaBeanCollection;
                            var peakSpotID = this.mainWindow.FocusedPeakID;
                            var driftSpots = this.mainWindow.DriftSpotBeanList;
                            var driftSpotID = this.mainWindow.FocusedDriftSpotID + 1;

                            refreshPeakViewImEsiMs(content, contentIM, peakSpotID, peakAreas, driftSpotID, driftSpots, rtTol, mzTol, imTol);
                        }
                        else {
                            var peakAreas = files[fileID].PeakAreaBeanCollection;
                            var peakID = this.mainWindow.FocusedPeakID + 1;
                            refreshPeakViewEsiMs(content, peakID, peakAreas, rtTol, mzTol);
                        }
                    }
                }
                #endregion
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {

                if (this.mainWindow.FocusedAlignmentFileID < 0)
                    return;

                var content = (PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                var alignmentResult = this.mainWindow.FocusedAlignmentResult;
                if (alignmentResult == null) return;

                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var peakID = this.mainWindow.DisplayFocusId + 1;
                    var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;

                    refreshAlignmentViewEims(content, peakID, alignedSpots, rtTol, mzTol);
                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                    var param = this.mainWindow.AnalysisParamForLC;
                    if (param.IsIonMobility) {
                        var contentIM = (PairwisePlotAlignmentViewUI)this.mainWindow.DriftTimeMzPairwiseAlignmentViewUI.Content;
                        if (contentIM.PairwisePlotBean.XAxisDatapointCollection == null ||
                            contentIM.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                            return;
                        var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                        var alignedSpotID = this.mainWindow.FocusedAlignmentPeakID;
                        var driftSpots = this.mainWindow.AlignedDriftSpotBeanList;
                        var driftSpotID = this.mainWindow.FocusedAlignmentDriftID + 1;

                        refreshPeakViewImEsiMs(content, contentIM, alignedSpotID, alignedSpots, driftSpotID, driftSpots, rtTol, mzTol, imTol);
                    }
                    else {
                        var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                        var peakID = this.mainWindow.DisplayFocusId + 1;

                        refreshAlignmentViewEsiMs(content, peakID, alignedSpots, rtTol, mzTol);
                    }
                }
            }
        }

      
        public void FocusBack() {
            var rtTol = 0.5F;
            var mzTol = 20F;
            var imTol = 0.5F;

            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.FocusedFileID < 0)
                    return;

                var content = (PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                #region
                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var ms1DecResults = this.mainWindow.Ms1DecResults;
                    var peakID = this.mainWindow.FocusedMS1DecID - 1;
                    refreshPeakViewEims(content, peakID, ms1DecResults, rtTol, mzTol);

                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                    var fileID = this.mainWindow.FocusedFileID;
                    var files = this.mainWindow.AnalysisFiles;
                    var param = this.mainWindow.AnalysisParamForLC;

                    if (files != null && fileID >= 0) {

                        if (param.IsIonMobility) {
                            var contentIM = (PairwisePlotPeakViewUI)this.mainWindow.DriftTimeMzPairwisePlotPeakViewUI.Content;
                            if (contentIM.PairwisePlotBean.XAxisDatapointCollection == null ||
                                contentIM.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                                return;
                            var peakAreas = files[fileID].PeakAreaBeanCollection;
                            var peakSpotID = this.mainWindow.FocusedPeakID;
                            var driftSpots = this.mainWindow.DriftSpotBeanList;
                            var driftSpotID = this.mainWindow.FocusedDriftSpotID - 1;

                            refreshPeakViewImEsiMs(content, contentIM, peakSpotID, peakAreas, driftSpotID, driftSpots, rtTol, mzTol, imTol);
                        }
                        else {
                            var peakAreas = files[fileID].PeakAreaBeanCollection;
                            var peakID = this.mainWindow.FocusedPeakID - 1;
                            refreshPeakViewEsiMs(content, peakID, peakAreas, rtTol, mzTol);
                        }
                    }
                }
                #endregion
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {

                if (this.mainWindow.FocusedAlignmentFileID < 0)
                    return;

                var content = (PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                var alignmentResult = this.mainWindow.FocusedAlignmentResult;
                if (alignmentResult == null) return;

                if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                    var peakID = this.mainWindow.DisplayFocusId - 1;
                    var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;

                    refreshAlignmentViewEims(content, peakID, alignedSpots, rtTol, mzTol);
                }
                else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                    var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                    var peakID = this.mainWindow.DisplayFocusId - 1;

                    refreshAlignmentViewEsiMs(content, peakID, alignedSpots, rtTol, mzTol);
                }
            }
        }

        private void refreshAlignmentViewEsiMs(PairwisePlotAlignmentViewUI content, int peakID, ObservableCollection<AlignmentPropertyBean> alignedSpots, float rtTol, float mzTol) {
            if (alignedSpots != null && peakID >= 0 && peakID < alignedSpots.Count) {
                var mz = alignedSpots[peakID].CentralAccurateMass;
                var rt = alignedSpots[peakID].CentralRetentionTime;

                content.PairwisePlotBean.SelectedPlotId = peakID;
                content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                content.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                content.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                content.RefreshUI();
            }
        }

        private void refreshAlignmentViewEims(PairwisePlotAlignmentViewUI content, int peakID, ObservableCollection<AlignmentPropertyBean> alignedSpots, 
            float rtTol, float mzTol) {
            if (alignedSpots != null && peakID >= 0 && peakID < alignedSpots.Count) {
                var rt = 0.0F;
                var mz = alignedSpots[peakID].QuantMass;
                if (this.mainWindow.AnalysisParamForGC.AlignmentIndexType == AlignmentIndexType.RT)
                    rt = alignedSpots[peakID].CentralRetentionTime;
                else {
                    rt = alignedSpots[peakID].CentralRetentionIndex;
                    if (this.mainWindow.AnalysisParamForGC.RiCompoundType == RiCompoundType.Alkanes) {
                        rtTol = 10;
                    }
                    else {
                        rtTol = 2000;
                    }
                }

                content.PairwisePlotBean.SelectedPlotId = peakID;
                content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                content.PairwisePlotBean.DisplayRangeMaxY = mz + 5;
                content.PairwisePlotBean.DisplayRangeMinY = mz - 5;
                content.RefreshUI();
            }
        }

        private void refreshPeakViewEsiMs(PairwisePlotPeakViewUI content, int peakID, ObservableCollection<PeakAreaBean> peakAreas, float rtTol, float mzTol) {
            if (peakAreas != null && peakID >= 0 && peakID < peakAreas.Count) {

                var peak = peakAreas[peakID];
                var rt = peak.RtAtPeakTop;
                var mz = peak.AccurateMass;

                content.PairwisePlotBean.SelectedPlotId = peakID;
                content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                content.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                content.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                content.RefreshUI();
            }
        }


        private void refreshPeakViewImEsiMs(PairwisePlotPeakViewUI contentRT, PairwisePlotPeakViewUI contentIM, int masterID,
            ObservableCollection<PeakAreaBean> peakAreas, float rtTol, float mzTol, float imTol) {

            var closestPeakID = DataAccessLcUtility.GetPeakAreaBeanIdMostlyMatchedWithMasterID(peakAreas, masterID);
            var peakSpot = peakAreas[closestPeakID];
            var peakID = peakSpot.PeakID;
            var rt = peakSpot.RtAtPeakTop;
            var mz = peakSpot.AccurateMass;

            contentRT.PairwisePlotBean.SelectedPlotId = peakID;
            contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
            contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
            contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
            contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
            contentRT.RefreshUI();

            if (peakSpot.MasterPeakID == masterID) return;

            // now, there should be drift spot having the master id
            var driftSpots = this.mainWindow.DriftSpotBeanList;
            foreach (var spot in driftSpots) {
                if (spot.MasterPeakID == masterID) {
                    if (this.mainWindow.SelectedPeakViewDriftSpot.DisplayedSpotID == spot.DisplayedSpotID) return;
                    var driftID = spot.DisplayedSpotID;
                    var dt = spot.DriftTimeAtPeakTop;

                    contentIM.PairwisePlotBean.SelectedPlotId = driftID;
                    contentIM.PairwisePlotBean.DisplayRangeMaxX = dt + imTol;
                    contentIM.PairwisePlotBean.DisplayRangeMinX = dt - imTol;
                    contentIM.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                    contentIM.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                    contentIM.RefreshUI();
                    return;
                }
            }
        }


        private void refreshPeakViewImEsiMs(PairwisePlotAlignmentViewUI contentRT, PairwisePlotAlignmentViewUI contentIM, 
            int masterID, ObservableCollection<AlignmentPropertyBean> alignedSpots, float rtTol, float mzTol, float imTol) {

            var closestPeakID = DataAccessLcUtility.GetAlignmentSpotIdMostlyMatchedWithMasterID(alignedSpots, masterID);
            var peakSpot = alignedSpots[closestPeakID];
            var peakID = peakSpot.AlignmentID;
            var rt = peakSpot.CentralRetentionTime;
            var mz = peakSpot.CentralAccurateMass;

            contentRT.PairwisePlotBean.SelectedPlotId = peakID;
            contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
            contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
            contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
            contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
            contentRT.RefreshUI();

            if (peakSpot.MasterID == masterID) return;

            // now, there should be drift spot having the master id
            var driftSpots = this.mainWindow.AlignedDriftSpotBeanList;
            foreach (var spot in driftSpots) {
                if (spot.MasterID == masterID) {
                    if (this.mainWindow.FocusedAlignmentDriftID == spot.DisplayedSpotID) return;
                    var driftID = spot.DisplayedSpotID;
                    var dt = spot.CentralDriftTime;

                    contentIM.PairwisePlotBean.SelectedPlotId = driftID;
                    contentIM.PairwisePlotBean.DisplayRangeMaxX = dt + imTol;
                    contentIM.PairwisePlotBean.DisplayRangeMinX = dt - imTol;
                    contentIM.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                    contentIM.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                    contentIM.RefreshUI();
                    return;
                }
            }
        }


        private void refreshPeakViewImEsiMs(PairwisePlotPeakViewUI contentRT, PairwisePlotPeakViewUI contentIM, int peakSpotID,
           ObservableCollection<PeakAreaBean> peakAreas, int driftSpotID, ObservableCollection<DriftSpotBean> driftSpots,
           float rtTol, float mzTol, float imTol) {

            if (driftSpotID < 0) { // meaning, we have to use the previous peak id on rt/mz dimension
                var startDriftSpot = driftSpots[0];
                var parentPeakID = startDriftSpot.PeakAreaBeanID;
                var parentPeakSpot = peakAreas[parentPeakID];
                if (parentPeakSpot.MasterPeakID == 0 || parentPeakID == 0) return;

                var previousPeakSpot = peakAreas[parentPeakID - 1];
                var peakID = previousPeakSpot.PeakID;
                var rt = previousPeakSpot.RtAtPeakTop;
                var mz = previousPeakSpot.AccurateMass;

                contentRT.PairwisePlotBean.SelectedPlotId = peakID;
                contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentRT.RefreshUI();
            } 
            else if (driftSpotID > driftSpots.Count - 1) { // meaning, we have to use the next peak id on rt/mz dimension
                var endDriftSpot = driftSpots[driftSpots.Count - 1];
                var parentPeakID = endDriftSpot.PeakAreaBeanID;
                var parentPeakSpot = peakAreas[parentPeakID];
                if (peakAreas.Count - 1 == parentPeakID) return;

                var nextPeakSpot = peakAreas[parentPeakID + 1];
                var peakID = nextPeakSpot.PeakID;
                var rt = nextPeakSpot.RtAtPeakTop;
                var mz = nextPeakSpot.AccurateMass;

                contentRT.PairwisePlotBean.SelectedPlotId = peakID;
                contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentRT.RefreshUI();
            }
            else {
                var driftSpot = driftSpots[driftSpotID];
                var dt = driftSpot.DriftTimeAtPeakTop;
                var mz = driftSpot.AccurateMass;

                contentIM.PairwisePlotBean.SelectedPlotId = driftSpotID;
                contentIM.PairwisePlotBean.DisplayRangeMaxX = dt + imTol;
                contentIM.PairwisePlotBean.DisplayRangeMinX = dt - imTol;
                contentIM.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentIM.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentIM.RefreshUI();
            }
        }

        private void refreshPeakViewImEsiMs(PairwisePlotAlignmentViewUI contentRT, PairwisePlotAlignmentViewUI contentIM, 
            int alignedSpotID, ObservableCollection<AlignmentPropertyBean> alignedSpots, int driftSpotID,
            ObservableCollection<AlignedDriftSpotPropertyBean> driftSpots, float rtTol, float mzTol, float imTol) {

            if (driftSpotID < 0) { // meaning, we have to use the previous peak id on rt/mz dimension
                var startDriftSpot = driftSpots[0];
                var parentPeakID = startDriftSpot.AlignmentSpotID;
                var parentPeakSpot = alignedSpots[parentPeakID];
                if (parentPeakSpot.MasterID == 0 || parentPeakID == 0) return;

                var previousPeakSpot = alignedSpots[parentPeakID - 1];
                var peakID = previousPeakSpot.AlignmentID;
                var rt = previousPeakSpot.CentralRetentionTime;
                var mz = previousPeakSpot.CentralAccurateMass;

                contentRT.PairwisePlotBean.SelectedPlotId = peakID;
                contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentRT.RefreshUI();
            }
            else if (driftSpotID > driftSpots.Count - 1) { // meaning, we have to use the next peak id on rt/mz dimension
                var endDriftSpot = driftSpots[driftSpots.Count - 1];
                var parentPeakID = endDriftSpot.AlignmentSpotID;
                var parentPeakSpot = alignedSpots[parentPeakID];
                if (alignedSpots.Count - 1 == parentPeakID) return;

                var nextPeakSpot = alignedSpots[parentPeakID + 1];
                var peakID = nextPeakSpot.AlignmentID;
                var rt = nextPeakSpot.CentralRetentionTime;
                var mz = nextPeakSpot.CentralAccurateMass;

                contentRT.PairwisePlotBean.SelectedPlotId = peakID;
                contentRT.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                contentRT.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentRT.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentRT.RefreshUI();
            }
            else {
                var driftSpot = driftSpots[driftSpotID];
                var dt = driftSpot.CentralCcs;
                var mz = driftSpot.CentralAccurateMass;

                contentIM.PairwisePlotBean.SelectedPlotId = driftSpotID;
                contentIM.PairwisePlotBean.DisplayRangeMaxX = dt + imTol;
                contentIM.PairwisePlotBean.DisplayRangeMinX = dt - imTol;
                contentIM.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                contentIM.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                contentIM.RefreshUI();
            }
        }


        private void refreshPeakViewEims(PairwisePlotPeakViewUI content, int peakID, List<MS1DecResult> ms1DecResults, float rtTol, float mzTol) {
            if (ms1DecResults == null)
                return;
            if (peakID >= 0 && peakID < ms1DecResults.Count) {

                var peak = ms1DecResults[peakID];
                var rt = peak.RetentionTime;
                var mz = peak.BasepeakMz;

                content.PairwisePlotBean.SelectedMs1DecID = peakID;
                content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                content.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                content.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                content.RefreshUI();
            }
        }

        

        public void DisplayFocus(string key) {

            var rt = -1.0F;
            var mz = -1.0F;
            var rtTol = 0.5F;
            var mzTol = 20F;
            var imTol = 0.5F;

            if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.peakView) {
                if (this.mainWindow.FocusedFileID < 0)
                    return;

                var content = (PairwisePlotPeakViewUI)this.mainWindow.RtMzPairwisePlotPeakViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                var maxRt = content.PairwisePlotBean.MaxX;
                var minRt = content.PairwisePlotBean.MinX;
                var maxMz = content.PairwisePlotBean.MaxY;
                var minMz = content.PairwisePlotBean.MinY;


                #region
                if (key == "RT") {
                    rt = (float)this.mainWindow.DisplayFocusRt;

                    if (rt + rtTol >= maxRt) {
                        rt = maxRt - rtTol;
                        this.mainWindow.DisplayFocusRt = Math.Round(rt, 2);
                    }
                    else if (rt - rtTol <= minRt) {
                        rt = minRt + rtTol;
                        this.mainWindow.DisplayFocusRt = Math.Round(rt, 2);
                    }

                    content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                    content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                    content.RefreshUI();
                }
                else if (key == "MZ") {
                    mz = (float)this.mainWindow.DisplayFocusMz;

                    if (mz + mzTol >= maxMz) {
                        mz = maxMz - mzTol;
                        this.mainWindow.DisplayFocusMz = Math.Round(mz, 5);
                    }
                    else if (mz - mzTol <= minMz) {
                        mz = minMz + mzTol;
                        this.mainWindow.DisplayFocusMz = Math.Round(mz, 5);
                    }

                    content.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                    content.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                    content.RefreshUI();
                }
                else if (key == "ID") {

                    if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                        var ms1DecResults = this.mainWindow.Ms1DecResults;
                        var peakID = this.mainWindow.DisplayFocusId;

                        refreshPeakViewEims(content, peakID, ms1DecResults, rtTol, mzTol);
                    }
                    else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                        var fileID = this.mainWindow.FocusedFileID;
                        var files = this.mainWindow.AnalysisFiles;
                        var param = this.mainWindow.AnalysisParamForLC;
                        if (files != null && fileID >= 0) {

                            if (param.IsIonMobility) {
                                var contentIM = (PairwisePlotPeakViewUI)this.mainWindow.DriftTimeMzPairwisePlotPeakViewUI.Content;
                                if (contentIM.PairwisePlotBean.XAxisDatapointCollection == null ||
                                    contentIM.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                                    return;
                                var peakAreas = files[fileID].PeakAreaBeanCollection;
                                var targetID = this.mainWindow.DisplayFocusId;

                                refreshPeakViewImEsiMs(content, contentIM, targetID, peakAreas, rtTol, mzTol, imTol);
                            }
                            else {
                                var peakAreas = files[fileID].PeakAreaBeanCollection;
                                var peakID = this.mainWindow.DisplayFocusId;

                                refreshPeakViewEsiMs(content, peakID, peakAreas, rtTol, mzTol);
                            }
                        }
                    }
                }
                #endregion
            }
            else if (this.mainWindow.PairwisePlotFocus == PairwisePlotFocus.alignmentView) {

                if (this.mainWindow.FocusedAlignmentFileID < 0)
                    return;

                var content = (PairwisePlotAlignmentViewUI)this.mainWindow.RtMzPairwisePlotAlignmentViewUI.Content;
                if (content.PairwisePlotBean.XAxisDatapointCollection == null ||
                    content.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                    return;

                var maxRt = content.PairwisePlotBean.MaxX;
                var minRt = content.PairwisePlotBean.MinX;
                var maxMz = content.PairwisePlotBean.MaxY;
                var minMz = content.PairwisePlotBean.MinY;

                if (key == "RT") {

                    rt = (float)this.mainWindow.DisplayFocusRt;

                    if (rt + rtTol >= maxRt) {
                        rt = maxRt - rtTol;
                        this.mainWindow.DisplayFocusRt = Math.Round(rt, 2);
                    }
                    else if (rt - rtTol <= minRt) {
                        rt = minRt + rtTol;
                        this.mainWindow.DisplayFocusRt = Math.Round(rt, 2);
                    }

                    content.PairwisePlotBean.DisplayRangeMaxX = rt + rtTol;
                    content.PairwisePlotBean.DisplayRangeMinX = rt - rtTol;
                    content.RefreshUI();
                }
                else if (key == "MZ") {

                    mz = (float)this.mainWindow.DisplayFocusMz;

                    if (mz + mzTol >= maxMz) {
                        mz = maxMz - mzTol;
                        this.mainWindow.DisplayFocusMz = Math.Round(mz, 5);
                    }
                    else if (mz - mzTol <= minMz) {
                        mz = minMz + mzTol;
                        this.mainWindow.DisplayFocusMz = Math.Round(mz, 5);
                    }

                    content.PairwisePlotBean.DisplayRangeMaxY = mz + mzTol;
                    content.PairwisePlotBean.DisplayRangeMinY = mz - mzTol;
                    content.RefreshUI();
                }
                else if (key == "ID") {

                    var alignmentResult = this.mainWindow.FocusedAlignmentResult;
                    if (alignmentResult == null) return;
                    

                    if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {

                        var peakID = this.mainWindow.DisplayFocusId;
                        var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;

                        refreshAlignmentViewEims(content, peakID, alignedSpots, rtTol, mzTol);
                    }
                    else if (this.mainWindow.ProjectProperty.Ionization == Ionization.ESI) {

                        var param = this.mainWindow.AnalysisParamForLC;
                        if (param.IsIonMobility) {
                            var contentIM = (PairwisePlotAlignmentViewUI)this.mainWindow.DriftTimeMzPairwiseAlignmentViewUI.Content;
                            if (contentIM.PairwisePlotBean.XAxisDatapointCollection == null ||
                                contentIM.PairwisePlotBean.XAxisDatapointCollection.Count == 0)
                                return;
                            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                            var targetID = this.mainWindow.DisplayFocusId;

                            refreshPeakViewImEsiMs(content, contentIM, targetID, alignedSpots, rtTol, mzTol, imTol);
                        }
                        else {
                            var alignedSpots = alignmentResult.AlignmentPropertyBeanCollection;
                            var peakID = this.mainWindow.DisplayFocusId;

                            refreshAlignmentViewEsiMs(content, peakID, alignedSpots, rtTol, mzTol);
                        }
                    }
                }
            }
        }

        private void labelUiSetting(PeakAreaBean peakAreaBean)
        {
            var peakHeight = Math.Round(peakAreaBean.IntensityAtPeakTop, 0);
            var peakArea = Math.Round(peakAreaBean.AreaAboveZero, 0);
            var rt = Math.Round(peakAreaBean.RtAtPeakTop, 3);
            var mz = Math.Round(peakAreaBean.AccurateMass, 5);

            var refRt = -1.0;
            var refMz = -1.0;
            var rtDiff = -1.0;
            var mzDiff = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = peakAreaBean.Comment;

            var annotation = "Unknown";
            if (peakAreaBean.MetaboliteName != string.Empty)
                annotation = peakAreaBean.MetaboliteName;
            var adduct = peakAreaBean.AdductIonName;

            if (peakAreaBean.LibraryID >= 0 && this.mainWindow.MspDB != null && this.mainWindow.MspDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.MspDB[peakAreaBean.LibraryID].RetentionTime, 3);
                refMz = Math.Round(this.mainWindow.MspDB[peakAreaBean.LibraryID].PrecursorMz, 5);
                specSimilarity = Math.Round(peakAreaBean.MassSpectraSimilarityValue, 0);
                revSpecSimilarity = Math.Round(peakAreaBean.ReverseSearchSimilarityValue, 0);
                rtSimilarity = Math.Round(peakAreaBean.RtSimilarityValue, 0);
                mzSimilarity = Math.Round(peakAreaBean.AccurateMassSimilarity, 0);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                var libInChIKey = this.mainWindow.MspDB[peakAreaBean.LibraryID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[peakAreaBean.LibraryID].Smiles;
                var libOntology = this.mainWindow.MspDB[peakAreaBean.LibraryID].Ontology;
                var libFormula = this.mainWindow.MspDB[peakAreaBean.LibraryID].Formula;
                if (libOntology == null || libOntology == string.Empty) {
                    libOntology = this.mainWindow.MspDB[peakAreaBean.LibraryID].CompoundClass;
                }

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }
            else if (peakAreaBean.PostIdentificationLibraryId >= 0 && this.mainWindow.PostIdentificationTxtDB != null && this.mainWindow.PostIdentificationTxtDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].RetentionTime, 3);
                refMz = Math.Round(this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].AccurateMass, 5);
                specSimilarity = Math.Round(peakAreaBean.MassSpectraSimilarityValue, 0);
                revSpecSimilarity = Math.Round(peakAreaBean.ReverseSearchSimilarityValue, 0);
                rtSimilarity = Math.Round(peakAreaBean.RtSimilarityValue, 0);
                mzSimilarity = Math.Round(peakAreaBean.AccurateMassSimilarity, 0);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                var libInChIKey = this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].Inchikey;
                var libSmiles = this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].Smiles;
                var libOntology = this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].Ontology;
                var libFormulaObj = this.mainWindow.PostIdentificationTxtDB[peakAreaBean.PostIdentificationLibraryId].Formula;
                var libFormula = libFormulaObj != null ? libFormulaObj.FormulaString : string.Empty;

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var mzString = refMz < 0 ? mz.ToString() : mz.ToString() + "|ref=" + refMz.ToString() + "|diff(mDa)=" + mzDiff.ToString();
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var mzSimilarityString = mzSimilarity < 0 ? "NA" : mzSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = adduct;
            this.mainWindow.DisplayedMzInfo = mzString;
            this.mainWindow.DisplayedPeakQuantInfo = peakHeight.ToString() + "|" + peakArea.ToString();
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = mzSimilarityString;
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = string.Empty;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        private void labelUiSetting(PeakAreaBean peakSpot, DriftSpotBean driftSpot) {
            if (driftSpot == null) return;

            var intensityOnRt = Math.Round(peakSpot.IntensityAtPeakTop, 0);
            var intensityOnDt = Math.Round(driftSpot.IntensityAtPeakTop, 0);
            var areaOnRt = Math.Round(peakSpot.AreaAboveZero, 0);
            var areaOnDt = Math.Round(driftSpot.AreaAboveZero, 0);
            var rt = Math.Round(peakSpot.RtAtPeakTop, 3);
            var dt = Math.Round(driftSpot.DriftTimeAtPeakTop, 3);
            var ccs = Math.Round(driftSpot.Ccs, 3);
            var mz = Math.Round(peakSpot.AccurateMass, 5);
            var param = this.mainWindow.AnalysisParamForLC;
            var fileid = this.mainWindow.FocusedFileID;
            var calinfo = param.FileidToCcsCalibrantData[fileid];

            var refRt = -1.0;
            var refCcs = -1.0;
            var refDt = -1.0;
            var refMz = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var ccsSimilarity = -1.0;
            var rtDiff = -1.0;
            var mzDiff = -1.0;
            var ccsDiff = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = string.Empty;

            var annotation = "Unknown";
            if (driftSpot.MetaboliteName != string.Empty)
                annotation = driftSpot.MetaboliteName;
            annotation += "|" + driftSpot.AdductIonName;

            //this.mainWindow.TextBox_PeakInformation_PeakIntensity.Text = intensityOnRt + "/" + intensityOnDt;
            //this.mainWindow.TextBox_PeakInformation_RetentionTime.Text = rt + "/" + dt + "/" + ccs;
            //this.mainWindow.TextBox_PeakInformation_AccurateMass.Text = Math.Round(driftSpot.AccurateMass, 5).ToString();

            if (driftSpot.LibraryID >= 0 && this.mainWindow.MspDB != null && this.mainWindow.MspDB.Count != 0) {
                refRt = Math.Round(this.mainWindow.MspDB[driftSpot.LibraryID].RetentionTime, 2);
                refCcs = this.mainWindow.MspDB[driftSpot.LibraryID].CollisionCrossSection;
                var charge = this.mainWindow.MspDB[driftSpot.LibraryID].AdductIonBean.ChargeNumber;
                refMz = Math.Round(this.mainWindow.MspDB[driftSpot.LibraryID].PrecursorMz, 5);
                var refK0 = IonMobilityUtility.CrossSectionToMobility(param.IonMobilityType, refCcs, charge, refMz, calinfo, param.IsAllCalibrantDataImported);
                refDt = refK0 > 0 ? Math.Round(refK0, 3) : -1.0;

                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refCcs > 0) ccsDiff = Math.Round(Math.Abs(refCcs - ccs), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                specSimilarity = Math.Round(driftSpot.MassSpectraSimilarityValue, 0);
                revSpecSimilarity = Math.Round(driftSpot.ReverseSearchSimilarityValue, 0);
                rtSimilarity = Math.Round(peakSpot.RtSimilarityValue, 0);
                ccsSimilarity = Math.Round(driftSpot.CcsSimilarity, 0);

                var libInChIKey = this.mainWindow.MspDB[driftSpot.LibraryID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[driftSpot.LibraryID].Smiles;
                var libOntology = this.mainWindow.MspDB[driftSpot.LibraryID].Ontology;
                var libFormula = this.mainWindow.MspDB[driftSpot.LibraryID].Formula;
                if (libOntology == null || libOntology == string.Empty) {
                    libOntology = this.mainWindow.MspDB[driftSpot.LibraryID].CompoundClass;
                }

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }
            else if (driftSpot.PostIdentificationLibraryId >= 0 && this.mainWindow.PostIdentificationTxtDB != null && this.mainWindow.PostIdentificationTxtDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].RetentionTime, 3);
                refCcs = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].Ccs;

                var adduct = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].AdductIon;
                var charge = peakSpot.ChargeNumber;
                if (adduct != null && adduct.ChargeNumber != 0) {
                    charge = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].AdductIon.ChargeNumber;
                }
                refMz = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].AccurateMass;
                var refK0 = IonMobilityUtility.CrossSectionToMobility(param.IonMobilityType, refCcs, charge, refMz, calinfo, param.IsAllCalibrantDataImported);
                refDt = refK0 > 0 ? Math.Round(refK0, 3) : -1.0;

                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refCcs > 0) ccsDiff = Math.Round(Math.Abs(refCcs - ccs), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                rtSimilarity = Math.Round(peakSpot.RtSimilarityValue, 0);
                ccsSimilarity = Math.Round(driftSpot.CcsSimilarity, 0);

                var libInChIKey = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].Inchikey;
                var libSmiles = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].Smiles;
                var libOntology = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].Ontology;
                var libFormulaObj = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryId].Formula;
                var libFormula = libFormulaObj != null ? libFormulaObj.FormulaString : string.Empty;

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var mzString = refMz < 0 ? mz.ToString() : mz.ToString() + "|ref=" + refMz.ToString() + "|diff(mDa)=" + mzDiff.ToString();
            var ccsString = refCcs < 0 ? ccs.ToString() : ccs.ToString() + "|ref=" + refCcs.ToString() + "|diff=" + ccsDiff.ToString(); 
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var mzSimilarityString = mzSimilarity < 0 ? "NA" : mzSimilarity.ToString();
            var ccsSimilarityString = ccsSimilarity < 0 ? "NA" : ccsSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = ccsString;
            this.mainWindow.DisplayedMzInfo = mzString;
            this.mainWindow.DisplayedPeakQuantInfo = intensityOnDt + "|" + areaOnDt + " on DT; " + intensityOnRt.ToString() + "|" + areaOnRt.ToString() + " on RT";
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = mzSimilarityString;
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = ccsSimilarityString;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        private void labelUiSetting(MS1DecResult ms1DecResult)
        {
            var peakHeight = Math.Round(ms1DecResult.BasepeakHeight, 0);
            var peakArea = Math.Round(ms1DecResult.BasepeakArea, 0);
            var rt = Math.Round(ms1DecResult.RetentionTime, 3);
            var ri = Math.Round(ms1DecResult.RetentionIndex, 1);
            var mz = Math.Round(ms1DecResult.BasepeakMz, 5);

            var refRt = -1.0;
            var refRi = -1.0;
            var rtDiff = -1.0;
            var riDiff = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var riSimilarity = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = "no available";

            var annotation = GetCompoundName(ms1DecResult.MspDbID);


            if (ms1DecResult.MspDbID >= 0 && this.mainWindow.MspDB != null && this.mainWindow.MspDB.Count != 0) {
                refRt = Math.Round(this.mainWindow.MspDB[ms1DecResult.MspDbID].RetentionTime, 3);
                refRi = Math.Round(this.mainWindow.MspDB[ms1DecResult.MspDbID].RetentionIndex, 1);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refRi > 0) riDiff = Math.Round(Math.Abs(refRi - ri), 1);

                specSimilarity = Math.Round(ms1DecResult.DotProduct, 0);
                revSpecSimilarity = Math.Round(ms1DecResult.ReverseDotProduct, 0);
                riSimilarity = Math.Round(ms1DecResult.RetentionIndexSimilarity, 0);

                var libInChIKey = this.mainWindow.MspDB[ms1DecResult.MspDbID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[ms1DecResult.MspDbID].Smiles;
                var libOntology = this.mainWindow.MspDB[ms1DecResult.MspDbID].Ontology;
                var libFormula = this.mainWindow.MspDB[ms1DecResult.MspDbID].Formula;

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var riString = refRi < 0 ? ri.ToString() : ri.ToString() + "|ref=" + refRi.ToString() + "|diff=" + riDiff.ToString();
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var riSimilarityString = riSimilarity < 0 ? "NA" : riSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = riString;
            this.mainWindow.DisplayedMzInfo = mz.ToString();
            this.mainWindow.DisplayedPeakQuantInfo = peakHeight.ToString() + "|" + peakArea.ToString();
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = "no available";
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = riSimilarityString;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        private void labelUiSetting(AlignmentPropertyBean alignmentPropertyBean)
        {
            var peakHeight = Math.Round(alignmentPropertyBean.AverageValiable, 0);
            var rt = Math.Round(alignmentPropertyBean.CentralRetentionTime, 3);
            var mz = Math.Round(alignmentPropertyBean.CentralAccurateMass, 5);

            var refRt = -1.0;
            var refMz = -1.0;
            var rtDiff = -1.0;
            var mzDiff = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = alignmentPropertyBean.Comment;

            var annotation = "Unknown";
            if (alignmentPropertyBean.MetaboliteName != string.Empty)
                annotation = alignmentPropertyBean.MetaboliteName;
            var adduct = alignmentPropertyBean.AdductIonName;

            if (alignmentPropertyBean.LibraryID >= 0 && this.mainWindow.MspDB != null && 
                this.mainWindow.MspDB.Count != 0 && alignmentPropertyBean.LibraryID < this.mainWindow.MspDB.Count) {

                refRt = Math.Round(this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].RetentionTime, 3);
                refMz = Math.Round(this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].PrecursorMz, 5);
                specSimilarity = Math.Round(alignmentPropertyBean.MassSpectraSimilarity, 0);
                revSpecSimilarity = Math.Round(alignmentPropertyBean.ReverseSimilarity, 0);
                rtSimilarity = Math.Round(alignmentPropertyBean.RetentionTimeSimilarity, 0);
                mzSimilarity = Math.Round(alignmentPropertyBean.AccurateMassSimilarity, 0);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                var libInChIKey = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Smiles;
                var libOntology = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Ontology;
                var libFormula = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Formula;
                if (libOntology == null || libOntology == string.Empty) {
                    libOntology = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].CompoundClass;
                }
                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }
            else if (alignmentPropertyBean.PostIdentificationLibraryID >= 0 && this.mainWindow.PostIdentificationTxtDB != null && this.mainWindow.PostIdentificationTxtDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].RetentionTime, 3);
                refMz = Math.Round(this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].AccurateMass, 5);
                specSimilarity = Math.Round(alignmentPropertyBean.MassSpectraSimilarity, 0);
                revSpecSimilarity = Math.Round(alignmentPropertyBean.ReverseSimilarity, 0);
                rtSimilarity = Math.Round(alignmentPropertyBean.RetentionTimeSimilarity, 0);
                mzSimilarity = Math.Round(alignmentPropertyBean.AccurateMassSimilarity, 0);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                var libInChIKey = this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].Inchikey;
                var libSmiles = this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].Smiles;
                var libOntology = this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].Ontology;
                var libFormulaObj = this.mainWindow.PostIdentificationTxtDB[alignmentPropertyBean.PostIdentificationLibraryID].Formula;
                var libFormula = libFormulaObj != null ? libFormulaObj.FormulaString : string.Empty;

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var mzString = refMz < 0 ? mz.ToString() : mz.ToString() + "|ref=" + refMz.ToString() + "|diff(mDa)=" + mzDiff.ToString();
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var mzSimilarityString = mzSimilarity < 0 ? "NA" : mzSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = adduct;
            this.mainWindow.DisplayedMzInfo = mzString;
            this.mainWindow.DisplayedPeakQuantInfo = peakHeight.ToString() + " (height average in samples)";
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = mzSimilarityString;
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = string.Empty;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        private void labelUiSetting(AlignmentPropertyBean peakSpot, AlignedDriftSpotPropertyBean driftSpot) {
            if (driftSpot == null) return;

            var intensityOnRt = Math.Round(peakSpot.AverageValiable, 0);
            var intensityOnDt = Math.Round(driftSpot.AverageValiable, 0);
            var rt = Math.Round(peakSpot.CentralRetentionTime, 3);
            var dt = Math.Round(driftSpot.CentralDriftTime, 3);
            var ccs = Math.Round(driftSpot.CentralCcs, 3);
            var mz = Math.Round(peakSpot.CentralAccurateMass, 5);
            var param = this.mainWindow.AnalysisParamForLC;

            var refRt = -1.0;
            var refCcs = -1.0;
            var refDt = -1.0;
            var refMz = -1.0;
            var rtDiff = -1.0;
            var mzDiff = -1.0;
            var ccsDiff = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var ccsSimilarity = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = string.Empty;

            var annotation = "Unknown";
            if (driftSpot.MetaboliteName != string.Empty)
                annotation = driftSpot.MetaboliteName;
            annotation += "|" + driftSpot.AdductIonName;

            var repfileid = driftSpot.RepresentativeFileID;
            var calinfo = param.FileidToCcsCalibrantData[repfileid];

            if (driftSpot.LibraryID >= 0 && this.mainWindow.MspDB != null && this.mainWindow.MspDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.MspDB[driftSpot.LibraryID].RetentionTime, 2);
                refCcs = this.mainWindow.MspDB[driftSpot.LibraryID].CollisionCrossSection;
                var charge = this.mainWindow.MspDB[driftSpot.LibraryID].AdductIonBean.ChargeNumber;
                refMz = Math.Round(this.mainWindow.MspDB[driftSpot.LibraryID].PrecursorMz, 5);
                var refK0 = IonMobilityUtility.CrossSectionToMobility(param.IonMobilityType, refCcs, charge, refMz, calinfo, param.IsAllCalibrantDataImported);
                refDt = refK0 > 0 ? Math.Round(refK0, 3) : -1.0;

                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refCcs > 0) ccsDiff = Math.Round(Math.Abs(refCcs - ccs), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                specSimilarity = Math.Round(driftSpot.MassSpectraSimilarity, 0);
                revSpecSimilarity = Math.Round(driftSpot.ReverseSimilarity, 0);
                rtSimilarity = Math.Round(peakSpot.RetentionTimeSimilarity, 0);
                ccsSimilarity = Math.Round(driftSpot.CcsSimilarity, 0);

                var libInChIKey = this.mainWindow.MspDB[driftSpot.LibraryID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[driftSpot.LibraryID].Smiles;
                var libOntology = this.mainWindow.MspDB[driftSpot.LibraryID].Ontology;
                var libFormula = this.mainWindow.MspDB[driftSpot.LibraryID].Formula;
                if (libOntology == null || libOntology == string.Empty) {
                    libOntology = this.mainWindow.MspDB[driftSpot.LibraryID].CompoundClass;
                }

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }
            else if (driftSpot.PostIdentificationLibraryID >= 0 && this.mainWindow.PostIdentificationTxtDB != null && this.mainWindow.PostIdentificationTxtDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].RetentionTime, 3);
                refCcs = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].Ccs;

                var adduct = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].AdductIon;
                var charge = peakSpot.ChargeNumber;
                if (adduct != null && adduct.ChargeNumber != 0) {
                    charge = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].AdductIon.ChargeNumber;
                }
                refMz = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].AccurateMass;
                var refK0 = IonMobilityUtility.CrossSectionToMobility(param.IonMobilityType, refCcs, charge, refMz, calinfo, param.IsAllCalibrantDataImported);
                refDt = refK0 > 0 ? Math.Round(refK0, 3) : -1.0;

                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refCcs > 0) ccsDiff = Math.Round(Math.Abs(refCcs - ccs), 3);
                mzDiff = Math.Round(Math.Abs(refMz - mz) * 1000, 2);

                rtSimilarity = Math.Round(peakSpot.RetentionTimeSimilarity, 0);
                ccsSimilarity = Math.Round(driftSpot.CcsSimilarity, 0);

                var libInChIKey = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].Inchikey;
                var libSmiles = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].Smiles;
                var libOntology = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].Ontology;
                var libFormulaObj = this.mainWindow.PostIdentificationTxtDB[driftSpot.PostIdentificationLibraryID].Formula;
                var libFormula = libFormulaObj != null ? libFormulaObj.FormulaString : string.Empty;

                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var mzString = refMz < 0 ? mz.ToString() : mz.ToString() + "|ref=" + refMz.ToString() + "|diff(mDa)=" + mzDiff.ToString();
            var ccsString = refCcs < 0 ? ccs.ToString() : ccs.ToString() + "|ref=" + refCcs.ToString() + "|diff=" + ccsDiff.ToString();
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var mzSimilarityString = mzSimilarity < 0 ? "NA" : mzSimilarity.ToString();
            var ccsSimilarityString = ccsSimilarity < 0 ? "NA" : ccsSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = ccsString;
            this.mainWindow.DisplayedMzInfo = mzString;
            this.mainWindow.DisplayedPeakQuantInfo = "(DT)" + intensityOnDt + "|(RT)" + intensityOnRt.ToString() + " (average height)";
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = mzSimilarityString;
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = ccsSimilarityString;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        private void labelUiSetting(AlignmentPropertyBean alignmentPropertyBean, MS1DecResult ms1DecResult)
        {

            var peakHeight = Math.Round(alignmentPropertyBean.AverageValiable, 0);
            var rt = Math.Round(alignmentPropertyBean.CentralRetentionTime, 3);
            var ri = Math.Round(alignmentPropertyBean.CentralRetentionIndex, 1);
            var mz = Math.Round(alignmentPropertyBean.QuantMass, 5);

            var refRt = -1.0;
            var refRi = -1.0;
            var refMz = -1.0;
            var rtDiff = -1.0;
            var riDiff = -1.0;
            var specSimilarity = -1.0;
            var revSpecSimilarity = -1.0;
            var rtSimilarity = -1.0;
            var mzSimilarity = -1.0;
            var riSimilarity = -1.0;
            var inchikey = "NA";
            var formula = "NA";
            var ontology = "NA";
            var smiles = "NA";
            var comment = alignmentPropertyBean.Comment;
            var annotation = "Unknown";
            if (alignmentPropertyBean.MetaboliteName != string.Empty) annotation = alignmentPropertyBean.MetaboliteName;
            //var annotation = GetCompoundName(alignmentPropertyBean.LibraryID);

            if (alignmentPropertyBean.LibraryID >= 0 && this.mainWindow.MspDB != null && this.mainWindow.MspDB.Count != 0) {

                refRt = Math.Round(this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].RetentionTime, 3);
                refRi = Math.Round(this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].RetentionIndex, 1);
                if (refRt > 0) rtDiff = Math.Round(Math.Abs(refRt - rt), 3);
                if (refRi > 0) riDiff = Math.Round(Math.Abs(refRi - ri), 1);
                specSimilarity = Math.Round(alignmentPropertyBean.MassSpectraSimilarity, 0);
                revSpecSimilarity = Math.Round(alignmentPropertyBean.ReverseSimilarity, 0);
                riSimilarity = Math.Round(alignmentPropertyBean.RetentionIndexSimilarity, 0);

                var libInChIKey = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].InchiKey;
                var libSmiles = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Smiles;
                var libOntology = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Ontology;
                var libFormula = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].Formula;
                if (libOntology == null || libOntology == string.Empty) {
                    libOntology = this.mainWindow.MspDB[alignmentPropertyBean.LibraryID].CompoundClass;
                }
                inchikey = libInChIKey != null && libInChIKey != string.Empty ? libInChIKey : "NA";
                smiles = libSmiles != null && libSmiles != string.Empty ? libSmiles : "NA";
                ontology = libOntology != null && libOntology != string.Empty ? libOntology : "NA";
                formula = libFormula != null && libFormula != string.Empty ? libFormula : "NA";
            }

            var rtString = refRt < 0 ? rt.ToString() : rt.ToString() + "|ref=" + refRt.ToString() + "|diff=" + rtDiff.ToString();
            var riString = refRi < 0 ? ri.ToString() : ri.ToString() + "|ref=" + refRi.ToString() + "|diff=" + riDiff.ToString();
            var specSimilarityString = specSimilarity < 0 ? "NA" : specSimilarity.ToString();
            var revSpecSimilarityString = revSpecSimilarity < 0 ? "NA" : revSpecSimilarity.ToString();
            var rtSimilarityString = rtSimilarity < 0 ? "NA" : rtSimilarity.ToString();
            var riSimilarityString = riSimilarity < 0 ? "NA" : riSimilarity.ToString();

            this.mainWindow.DisplayedAnnotatedNameInfo = annotation;
            this.mainWindow.DisplayedRetentionInfo = rtString;
            this.mainWindow.DisplayedOtherDiagnosticsInfo = riString;
            this.mainWindow.DisplayedMzInfo = mz.ToString();
            this.mainWindow.DisplayedPeakQuantInfo = peakHeight.ToString() + " (average height)";
            this.mainWindow.DisplayedFormulaOntologyInfo = formula + "|" + ontology;
            this.mainWindow.DisplayedInChIKeyInfo = inchikey;
            this.mainWindow.DisplayedCommentInfo = comment;
            this.mainWindow.DisplayedRtSimilarityInfo = rtSimilarityString;
            this.mainWindow.DisplayedMzSimilarityInfo = "no available";
            this.mainWindow.DisplayedSpectrumMatchInfo = "(Dot)" + specSimilarityString + " | (Rev)" + revSpecSimilarityString;
            this.mainWindow.DisplayedCcsSimilarityInfo = riSimilarityString;
            this.mainWindow.DisplayedSmilesInfo = smiles;
            if (this.mainWindow.TabItem_StructureImage.IsSelected == true)
                this.mainWindow.Image_Structure.Source = GetSmilesAsImage(smiles, this.mainWindow.TabControl_PeakCharacter.ActualWidth, this.mainWindow.TabControl_PeakCharacter.ActualHeight);
        }

        public string GetCompoundName(int id)
        {
            if (this.mainWindow.MspDB == null || this.mainWindow.MspDB.Count - 1 < id || id < 0) return "Unknown";
            else return this.mainWindow.MspDB[id].Name;
        }

        private int countDisplayedSpots(PairwisePlotBean pairwisePlotBean, PairwisePlotFocus pairwisePlotFocus, Ionization ionization) 
        {
            var count = -1;
            if (pairwisePlotFocus == PairwisePlotFocus.peakView) { // case in peak viewer focused
                if (ionization == Ionization.ESI) {
                    var peakAreas = pairwisePlotBean.PeakAreaBeanCollection;
                    count = countInPeakAreBeans(peakAreas);
                }
                else { // case in alignment viewer focused
                    var ms1DecResults = pairwisePlotBean.Ms1DecResults;
                    count = countInMS1DecResults(ms1DecResults);
                }
            }
            else {
                var alignedSpots = pairwisePlotBean.AlignmentPropertyBeanCollection;
                count = countInAlignedSpots(alignedSpots);
            }
            return count;
        }

        private int countInAlignedSpots(ObservableCollection<AlignmentPropertyBean> alignedSpots)
        {
            var total = alignedSpots.Count;
            foreach (var spot in alignedSpots) {
                if (spot.RelativeAmplitudeValue < ampSliderLowerValue * 0.01) {
                    total--;
                    continue;
                }

                if (spot.RelativeAmplitudeValue > ampSliderUpperValue * 0.01) {
                    total--;
                    continue;
                }

                if ((molecularIonFilter && spot.IsotopeTrackingWeightNumber > 0) || 
                    (molecularIonFilter && spot.PostDefinedIsotopeWeightNumber > 0)) {
                    total--;
                    continue;
                }

                //filter by identified, annotated, unknowns, ccs
                var identified = false; var annotated = false; var ccsMatched = false;
                if (spot.MetaboliteName != null && spot.MetaboliteName.IndexOf("w/o MS2:", 0) >= 0) annotated = true;
                if (spot.MetaboliteName != null && (spot.LibraryID >= 0 || spot.PostIdentificationLibraryID >= 0) && spot.MetaboliteName.IndexOf("w/o MS2:", 0) < 0) identified = true;
                if (spot.IsCcsMatch) ccsMatched = true;
                //if (spot.MetaboliteName.Contains("PC") && !spot.MetaboliteName.Contains("w/o")) {
                //    Console.WriteLine();
                //}

                var annotationChecker = true;
                if (!identifiedFilter && !annotatedFilter && !UnknownFilter && !CcsFilter) {
                    annotationChecker = false;
                }
                
                if(identifiedFilter && identified) annotationChecker = false;
                if (annotatedFilter && annotated) annotationChecker = false;
                if (ccsFilter && ccsMatched) annotationChecker = false;
                if (UnknownFilter && !identified && !annotated && !ccsMatched) annotationChecker = false;

                if (annotationChecker) {
                    total--;
                    continue;
                }

                //msms
                if (mainWindow.AnalysisParamForLC.IsIonMobility) {
                    if (msmsFilter && !spot.AlignedDriftSpots.Any(drift => drift.MsmsIncluded)) {
                        total--;
                        continue;
                    }
                }
                else {
                    if (msmsFilter && !spot.MsmsIncluded) {
                        total--;
                        continue;
                    }
                }

                //unique fragment
                if (uniqueionFilter && !spot.IsFragmentQueryExist) {
                    total--;
                    continue;
                }

                //blank
                if(blankFilter && spot.IsBlankFiltered) {
                    total--;
                    continue;
                }
            }
            return total;
        }

        private int countInMS1DecResults(List<MS1DecResult> ms1DecResults)
        {
            var total = ms1DecResults.Count;
            foreach (var result in ms1DecResults) {
                if (result.AmplitudeScore < ampSliderLowerValue * 0.01) {
                    total--;
                    continue;
                }

                if (result.AmplitudeScore > ampSliderUpperValue * 0.01) {
                    total--;
                    continue;
                }

                var identified = result.MspDbID >= 0 ? true : false;
                var annotationChecker = true;
                if (!identifiedFilter && !annotatedFilter && !UnknownFilter) annotationChecker = false;
                if (identifiedFilter && identified) annotationChecker = false;
                if (annotatedFilter && identified) annotationChecker = false;
                if (UnknownFilter && !identified) annotationChecker = false;

                if (annotationChecker) {
                    total--;
                    continue;
                }
            }

            return total;
        }

        private int countInPeakAreBeans(ObservableCollection<PeakAreaBean> peakAreas)
        {
            var total = peakAreas.Count;
            foreach (var peak in peakAreas) {
                if (peak.AmplitudeScoreValue < ampSliderLowerValue * 0.01) {
                    total--;
                    continue;
                }

                if (peak.AmplitudeScoreValue > ampSliderUpperValue * 0.01) {
                    total--;
                    continue;
                }

                if (molecularIonFilter && peak.IsotopeWeightNumber > 0) {
                    total--;
                    continue;
                }

                //filter by identified, annotated, unknowns
                var identified = false; var annotated = false; var ccsMatched = false;
                if (peak.MetaboliteName.IndexOf("w/o MS2:", 0) >= 0) annotated = true;
                if ((peak.LibraryID >= 0 || peak.PostIdentificationLibraryId >= 0) && peak.MetaboliteName.IndexOf("w/o MS2:", 0) < 0) identified = true;
                if (peak.IsCcsMatch) ccsMatched = true;

                var annotationChecker = true;
                if (!identifiedFilter && !annotatedFilter && !UnknownFilter && !CcsFilter) {
                    annotationChecker = false;
                }

                if (identifiedFilter && identified) annotationChecker = false;
                if (annotatedFilter && annotated) annotationChecker = false;
                if (ccsFilter && ccsMatched) annotationChecker = false;
                if (UnknownFilter && !identified && !annotated && !ccsMatched) annotationChecker = false;

                if (annotationChecker) {
                    total--;
                    continue;
                }

                //msms
                if (mainWindow.AnalysisParamForLC.IsIonMobility) {
                    if (msmsFilter && peak.DriftSpots.All(spot => spot.Ms2LevelDatapointNumber < 0)) {
                        total--;
                        continue;
                    }
                }
                else {
                    if (msmsFilter && peak.Ms2LevelDatapointNumber < 0) {
                        total--;
                        continue;
                    }
                }

                //unique fragment
                if (uniqueionFilter && !peak.IsFragmentQueryExist) {
                    total--;
                    continue;
                }
            }
            return total;
        }

        private void changeTableViewerSetting() {
            if(this.mainWindow.PeakSpotTableViewer != null) {
                var setting = this.mainWindow.PeakSpotTableViewer.PeakSpotTableViewerVM.Settings;
                if (setting.IdentifiedFilter != identifiedFilter) setting.IdentifiedFilter = identifiedFilter;
                if (setting.AnnotatedFilter != annotatedFilter) setting.AnnotatedFilter = annotatedFilter;
                if (setting.MolecularIonFilter != molecularIonFilter) setting.MolecularIonFilter = molecularIonFilter;
                if (setting.MsmsFilter != msmsFilter) setting.MsmsFilter = msmsFilter;
                if (setting.CcsFilter != ccsFilter) setting.CcsFilter = ccsFilter;
                if (setting.UnknownFilter != unknownFilter) setting.UnknownFilter = unknownFilter;
                if (setting.UniqueionFilter != uniqueionFilter) setting.UniqueionFilter = uniqueionFilter;
                if (setting.AmpSliderLowerValue != (float)ampSliderLowerValue) setting.AmpSliderLowerValue = (float)ampSliderLowerValue;
                if (setting.AmpSliderUpperValue != (float)ampSliderUpperValue) setting.AmpSliderUpperValue = (float)ampSliderUpperValue;
            }
            if (this.mainWindow.AlignmentSpotTableViewer != null) {
                var setting = this.mainWindow.AlignmentSpotTableViewer.AlignmentSpotTableViewerVM.Settings;
                if (setting.IdentifiedFilter != identifiedFilter) setting.IdentifiedFilter = identifiedFilter;
                if (setting.AnnotatedFilter != annotatedFilter) setting.AnnotatedFilter = annotatedFilter;
                if (setting.MolecularIonFilter != molecularIonFilter) setting.MolecularIonFilter = molecularIonFilter;
                if (setting.MsmsFilter != msmsFilter) setting.MsmsFilter = msmsFilter;
                if (setting.CcsFilter != ccsFilter) setting.CcsFilter = ccsFilter;
                if (setting.UnknownFilter != unknownFilter) setting.UnknownFilter = unknownFilter;
                if (setting.UniqueionFilter != uniqueionFilter) setting.UniqueionFilter = uniqueionFilter;
                if (setting.BlankFilter != blankFilter) setting.BlankFilter = blankFilter;
                if (setting.AmpSliderLowerValue != (float)ampSliderLowerValue) setting.AmpSliderLowerValue = (float)ampSliderLowerValue;
                if (setting.AmpSliderUpperValue != (float)ampSliderUpperValue) setting.AmpSliderUpperValue = (float)ampSliderUpperValue;
            }
            if (this.mainWindow.QuantmassBrowser != null) {
                var setting = this.mainWindow.QuantmassBrowser.QuantmassBrowserVM.Settings;
                if (setting.IdentifiedFilter != identifiedFilter) setting.IdentifiedFilter = identifiedFilter;
                if (setting.AnnotatedFilter != annotatedFilter) setting.AnnotatedFilter = annotatedFilter;
                if (setting.MolecularIonFilter != molecularIonFilter) setting.MolecularIonFilter = molecularIonFilter;
                if (setting.MsmsFilter != msmsFilter) setting.MsmsFilter = msmsFilter;
                if (setting.CcsFilter != ccsFilter) setting.CcsFilter = ccsFilter;
                if (setting.UnknownFilter != unknownFilter) setting.UnknownFilter = unknownFilter;
                if (setting.UniqueionFilter != uniqueionFilter) setting.UniqueionFilter = uniqueionFilter;
                if (setting.BlankFilter != blankFilter) setting.BlankFilter = blankFilter;
                if (setting.AmpSliderLowerValue != (float)ampSliderLowerValue) setting.AmpSliderLowerValue = (float)ampSliderLowerValue;
                if (setting.AmpSliderUpperValue != (float)ampSliderUpperValue) setting.AmpSliderUpperValue = (float)ampSliderUpperValue;
            }
        }

        public BitmapImage GetSmilesAsImage(string smiles, double width, double height) {
            if (width <= 0 || height <= 0) return null;
            if (smiles == string.Empty || smiles == "NA") return null;
            return MoleculeImage.SmilesToMediaImageSource(smiles, (int)width, (int)height);
        }
    }
}
