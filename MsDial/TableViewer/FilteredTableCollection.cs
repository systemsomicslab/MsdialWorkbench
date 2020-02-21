using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.ComponentModel;

namespace Rfx.Riken.OsakaUniv.TableViewer
{
    public class FilteredTable : ViewModelBase
    {
        public ICollectionView View { get; private set; }

        public FilteredTable(System.Collections.IList list) {
            View = System.Windows.Data.CollectionViewSource.GetDefaultView(list);
        }
    }

    public class FilterSettings : ViewModelBase
    {
        #region member and variables

        private bool identifiedFilter = false;
        private bool annotatedFilter = false;
        private bool molecularIonFilter = false;
        private bool msmsFilter = false;
        private bool unknownFilter = false;
        private bool blankFilter = false;
        private bool ccsFilter = false;
        private float ampSliderLowerValue = 0f;
        private float ampSliderUpperValue = 100f;
        private float mzSliderLowerValue = 0f;
        private float mzSliderUpperValue = 1000f;
        private float rtSliderLowerValue = 0f;
        private float rtSliderUpperValue = 100f;
        private bool uniqueionFilter = false;
        private string _metaboliteNameFilter = "";
        private string _commentFilter = "";
        private string _shortInchiKey = "";

        private bool isAccurateMassSimilarityCutOff = false;
        private bool isRetentionTimeSimilarityCutOff = false;
        private bool isSimpleDotProductSimilarityCutOff = false;
        private bool isDotProductSimilarityCutOff = false;
        private bool isReverseDotProductSimilarityCutOff = false;
        private bool isFragmentPresenceSimilarityCutOff = false;

        private float accurateMassSimilarityCutOffMin = -1;
        private float retentionTimeSimilarityCutOffMin = -1;
        private float simpleDotProductSimilarityCutOffMin = -1;
        private float dotProductSimilarityCutOffMin = -1;
        private float reverseDotProductSimilarityCutOffMin = -1;
        private float fragmentPresenceSimilarityCutOffMin = -1;
        
        private float accurateMassSimilarityCutOffMax = 1000;
        private float retentionTimeSimilarityCutOffMax = 1000;
        private float simpleDotProductSimilarityCutOffMax = 1000;
        private float dotProductSimilarityCutOffMax = 1000;
        private float reverseDotProductSimilarityCutOffMax = 1000;
        private float fragmentPresenceSimilarityCutOffMax = 1000;



        public enum ViewerType { Peak, Alignment, Compound };
        public ViewerType ViewType { get; set; }
        public int NumRows {
            get {
                if (ViewType == ViewerType.Peak) { return this.view.Cast<PeakSpotRow>().Count(); }
                else if (ViewType == ViewerType.Alignment) { return this.view.Cast<AlignmentSpotRow>().Count(); }
                else if (ViewType == ViewerType.Compound) { return this.view.Cast<CompoundGroup>().Count(); }
                else { return 0; }
            }
        }

        private ICollectionView view;

        public bool IdentifiedFilter {
            get { return identifiedFilter; }
            set {
                if (identifiedFilter == value) return;
                identifiedFilter = value;
                Update();
                OnPropertyChanged("IdentifiedFilter");
            }
        }

        public bool AnnotatedFilter {
            get { return annotatedFilter; }
            set {
                if (annotatedFilter == value) return;
                annotatedFilter = value;
                Update();
                OnPropertyChanged("AnnotatedFilter");
            }
        }
        public bool MolecularIonFilter {
            get { return molecularIonFilter; }
            set {
                if (molecularIonFilter == value) return;
                molecularIonFilter = value;
                Update();
                OnPropertyChanged("MolecularIonFilter");
            }
        }

        public bool MsmsFilter {
            get { return msmsFilter; }
            set {
                if (msmsFilter == value) return;
                msmsFilter = value;
                Update();
                OnPropertyChanged("MsmsFilter");
            }
        }
        public bool UnknownFilter {
            get { return unknownFilter; }
            set {
                if (unknownFilter == value) return;
                unknownFilter = value;
                Update();
                OnPropertyChanged("UnknownFilter");
            }
        }

        public bool BlankFilter {
            get { return blankFilter; }
            set {
                if (blankFilter == value) return;
                blankFilter = value;
                Update();
                OnPropertyChanged("BlankFilter");
            }
        }
        public float AmpSliderLowerValue {
            get { return ampSliderLowerValue; }
            set {
                if (ampSliderLowerValue == value) return;
                ampSliderLowerValue = value;
                Update();
                OnPropertyChanged("AmpSliderLowerValue");
            }
        }

        public float AmpSliderUpperValue {
            get { return ampSliderUpperValue; }
            set {
                if (ampSliderUpperValue == value) return;
                ampSliderUpperValue = value;
                Update();
                OnPropertyChanged("AmpSliderUpperValue");
            }
        }

        public float MzSliderUpperValue {
            get { return mzSliderUpperValue; }
            set {
                if (mzSliderUpperValue == value) return;
                mzSliderUpperValue = value;
                Update();
                OnPropertyChanged("MzSliderUpperValue");
            }
        }
        public float MzSliderLowerValue {
            get { return mzSliderLowerValue; }
            set {
                if (mzSliderLowerValue == value) return;
                mzSliderLowerValue = value;
                Update();
                OnPropertyChanged("MzSliderLowerValue");
            }
        }
        public float RtSliderUpperValue {
            get { return rtSliderUpperValue; }
            set {
                if (rtSliderUpperValue == value) return;
                rtSliderUpperValue = value;
                Update();
                OnPropertyChanged("RtSliderUpperValue");
            }
        }
        public float RtSliderLowerValue {
            get { return rtSliderLowerValue; }
            set {
                if (rtSliderLowerValue == value) return;
                rtSliderLowerValue = value;
                Update();
                OnPropertyChanged("RtSliderLowerValue");
            }
        }


        public bool UniqueionFilter {
            get {
                return uniqueionFilter;
            }

            set {
                if (uniqueionFilter == value) return;
                uniqueionFilter = value;
                Update();
                OnPropertyChanged("UniqueionFilter");
            }
        }

        public string MetaboliteNameFilter {
            get { return _metaboliteNameFilter; }
            set { if (_metaboliteNameFilter == value) return; _metaboliteNameFilter = value; Update(); OnPropertyChanged("MetaboliteNameFilter"); }
        }

        public string ShortInchiKeyFilter {
            get { return _shortInchiKey; }
            set { if (_shortInchiKey == value) return; _shortInchiKey = value; Update(); OnPropertyChanged("ShortInchiKeyFilter"); }
        }

        public string CommentFilter {
            get { return _commentFilter; }
            set { if (_commentFilter == value) return; _commentFilter = value; Update(); OnPropertyChanged("CommentFilter"); }
        }

        public bool CcsFilter {
            get { return ccsFilter; }
            set {
                if (ccsFilter == value) return;
                ccsFilter = value;
                Update();
                OnPropertyChanged("CcsFilter");
            }
        }

        public float AccurateMassSimilarityCutOffMin {
            get { return accurateMassSimilarityCutOffMin; }
            set {
                if (accurateMassSimilarityCutOffMin == value) return;
                accurateMassSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(AccurateMassSimilarityCutOffMin));
            }
        }

        public float RetentionTimeSimilarityCutOffMin {
            get { return retentionTimeSimilarityCutOffMin; }
            set {
                if (retentionTimeSimilarityCutOffMin == value) return;
                retentionTimeSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(RetentionTimeSimilarityCutOffMin));
            }
        }

        public float DotProductSimilarityCutOffMin {
            get { return dotProductSimilarityCutOffMin; }
            set {
                if (dotProductSimilarityCutOffMin == value) return;
                dotProductSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(DotProductSimilarityCutOffMin));
            }
        }

        public float ReverseDotProductSimilarityCutOffMin {
            get { return reverseDotProductSimilarityCutOffMin; }
            set {
                if (reverseDotProductSimilarityCutOffMin == value) return;
                reverseDotProductSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(ReverseDotProductSimilarityCutOffMin));
            }
        }

        public float FragmentPresenceSimilarityCutOffMin {
            get { return fragmentPresenceSimilarityCutOffMin; }
            set {
                if (fragmentPresenceSimilarityCutOffMin == value) return;
                fragmentPresenceSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(FragmentPresenceSimilarityCutOffMin));
            }
        }

        public float SimpleDotProductSimilarityCutOffMin {
            get { return simpleDotProductSimilarityCutOffMin; }
            set {
                if (simpleDotProductSimilarityCutOffMin == value) return;
                simpleDotProductSimilarityCutOffMin = value;
                Update();
                OnPropertyChanged(nameof(SimpleDotProductSimilarityCutOffMin));
            }
        }


        public float AccurateMassSimilarityCutOffMax {
            get { return accurateMassSimilarityCutOffMax; }
            set {
                if (accurateMassSimilarityCutOffMax == value) return;
                accurateMassSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(AccurateMassSimilarityCutOffMax));
            }
        }

        public float RetentionTimeSimilarityCutOffMax {
            get { return retentionTimeSimilarityCutOffMax; }
            set {
                if (retentionTimeSimilarityCutOffMax == value) return;
                retentionTimeSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(RetentionTimeSimilarityCutOffMax));
            }
        }

        public float DotProductSimilarityCutOffMax {
            get { return dotProductSimilarityCutOffMax; }
            set {
                if (dotProductSimilarityCutOffMax == value) return;
                dotProductSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(DotProductSimilarityCutOffMax));
            }
        }

        public float ReverseDotProductSimilarityCutOffMax {
            get { return reverseDotProductSimilarityCutOffMax; }
            set {
                if (reverseDotProductSimilarityCutOffMax == value) return;
                reverseDotProductSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(ReverseDotProductSimilarityCutOffMax));
            }
        }

        public float FragmentPresenceSimilarityCutOffMax {
            get { return fragmentPresenceSimilarityCutOffMax; }
            set {
                if (fragmentPresenceSimilarityCutOffMax == value) return;
                fragmentPresenceSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(FragmentPresenceSimilarityCutOffMax));
            }
        }

        public float SimpleDotProductSimilarityCutOffMax {
            get { return simpleDotProductSimilarityCutOffMax; }
            set {
                if (simpleDotProductSimilarityCutOffMax == value) return;
                simpleDotProductSimilarityCutOffMax = value;
                Update();
                OnPropertyChanged(nameof(SimpleDotProductSimilarityCutOffMax));
            }
        }


        public bool IsAccurateMassSimilarityCutOff { get {
                return isAccurateMassSimilarityCutOff;
            }
            set {
                if (isAccurateMassSimilarityCutOff == value) return;
                isAccurateMassSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsAccurateMassSimilarityCutOff));
            }
        }

        public bool IsRetentionTimeSimilarityCutOff {
            get {
                return isRetentionTimeSimilarityCutOff;
            }
            set {
                if (isRetentionTimeSimilarityCutOff == value) return;
                isRetentionTimeSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsRetentionTimeSimilarityCutOff));
            }
        }
        public bool IsSimpleDotProductSimilarityCutOff {
            get {
                return isSimpleDotProductSimilarityCutOff;
            }
            set {
                if (isSimpleDotProductSimilarityCutOff == value) return;
                isSimpleDotProductSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsSimpleDotProductSimilarityCutOff));
            }
        }
        public bool IsDotProductSimilarityCutOff {
            get {
                return isDotProductSimilarityCutOff;
            }
            set {
                if (isDotProductSimilarityCutOff == value) return;
                isDotProductSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsDotProductSimilarityCutOff));
            }
        }
        public bool IsReverseDotProductSimilarityCutOff {
            get {
                return isReverseDotProductSimilarityCutOff;
            }
            set {
                if (isReverseDotProductSimilarityCutOff == value) return;
                isReverseDotProductSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsReverseDotProductSimilarityCutOff));
            }
        }
        public bool IsFragmentPresenceSimilarityCutOff {
            get {
                return isFragmentPresenceSimilarityCutOff;
            }
            set {
                if (isFragmentPresenceSimilarityCutOff == value) return;
                isFragmentPresenceSimilarityCutOff = value;
                Update();
                OnPropertyChanged(nameof(IsFragmentPresenceSimilarityCutOff));
            }
        }

        public void Update() {
            try
            {
                this.view.Refresh();
                OnPropertyChanged("NumRows");
            }
            catch {
                System.Windows.MessageBox.Show("Please finish the change in table viewer", "Error", System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Error);
            }
        }



        #endregion

        public FilterSettings(ICollectionView view, ViewerType type) {
            this.view = view;
            this.ViewType = type;
        }

        public bool SpotFilter(object sender) {
            if (sender.GetType() == typeof(PeakSpotRow)) {
                return peakSpotFilter((PeakSpotRow)sender);
            }
            else if (sender.GetType() == typeof(AlignmentSpotRow)) {
                return alignmentSpotFilter((AlignmentSpotRow)sender);
            }
            else if (sender.GetType() == typeof(TableViewer.CompoundGroup)) {
                return compoundGroupFilter((TableViewer.CompoundGroup)sender);
            }
            return false;
        }

        private bool peakSpotFilter(PeakSpotRow row) {
            // GC
            if (row.Ms1DecRes != null) {
                var ms1dec = row.Ms1DecRes;
                if (ms1dec.AmplitudeScore < this.AmpSliderLowerValue * 0.01) return false;
                if (ms1dec.AmplitudeScore > this.AmpSliderUpperValue * 0.01) return false;
                if (ms1dec.RetentionTime < this.RtSliderLowerValue) return false;
                if (ms1dec.RetentionTime > this.RtSliderUpperValue) return false;
                if (ms1dec.BasepeakMz < this.MzSliderLowerValue) return false;
                if (ms1dec.BasepeakMz > this.MzSliderUpperValue) return false;

                else if (this.UnknownFilter) {
                    if (ms1dec.MspDbID >= 0) return false;
                }
                if (this.MetaboliteNameFilter != string.Empty && !ms1dec.MetaboliteName.ToLower().Contains(this.MetaboliteNameFilter.ToLower())) return false;

                var isDisplayed = false;
                if (!this.AnnotatedFilter && !this.IdentifiedFilter && !this.UnknownFilter)
                    isDisplayed = true;

                if (this.AnnotatedFilter || this.IdentifiedFilter) {
                    if (ms1dec.MspDbID >= 0) isDisplayed = true;
                }

                if (this.UnknownFilter) {
                    if (ms1dec.MspDbID < 0) isDisplayed = true;
                }
                return isDisplayed;
            }

            //LC
            else {
                var peakAreaBean = row.PeakAreaBean;
                if (this.MetaboliteNameFilter != string.Empty && !peakAreaBean.MetaboliteName.ToLower().Contains(this.MetaboliteNameFilter.ToLower())) return false;
                if (this.CommentFilter != string.Empty && !peakAreaBean.Comment.ToLower().Contains(this.CommentFilter.ToLower())) return false;
                if (peakAreaBean.AmplitudeScoreValue < this.AmpSliderLowerValue * 0.01) return false;
                if (peakAreaBean.AmplitudeScoreValue > this.AmpSliderUpperValue * 0.01) return false;
                if (peakAreaBean.RtAtPeakTop < this.RtSliderLowerValue) return false;
                if (peakAreaBean.RtAtPeakTop > this.RtSliderUpperValue) return false;
                if (peakAreaBean.AccurateMass < this.MzSliderLowerValue) return false;
                if (peakAreaBean.AccurateMass > this.MzSliderUpperValue) return false;

                if (this.MolecularIonFilter && peakAreaBean.IsotopeWeightNumber > 0) return false;

                var metabolite = peakAreaBean.MetaboliteName;
                var isDisplayed = false;

                if (!this.AnnotatedFilter && !this.IdentifiedFilter && !this.UnknownFilter)
                    isDisplayed = true;

                if (this.IdentifiedFilter &&
                    metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                    isDisplayed = true;
                if (this.AnnotatedFilter &&
                    metabolite != string.Empty && metabolite.Contains("w/o"))
                    isDisplayed = true;
                if (this.UnknownFilter &&
                    (metabolite == string.Empty || metabolite.Contains("Unknown")))
                    isDisplayed = true;

                if (this.UniqueionFilter &&
                    !peakAreaBean.IsFragmentQueryExist)
                    isDisplayed = false;

                if (this.MsmsFilter &&
                    peakAreaBean.Ms2LevelDatapointNumber < 0)
                    isDisplayed = false;

                return isDisplayed;
            }
        }

        private bool alignmentSpotFilter(AlignmentSpotRow row) {
            var isDisplayed = false;

            if (row.Mobility > 0) {
                var alignedSpot = row.AlignmentPropertyBean;
                var driftSpot = row.AlignedDriftSpotPropertyBean;
                if (alignedSpot.RelativeAmplitudeValue < this.AmpSliderLowerValue * 0.01) return false;
                if (alignedSpot.RelativeAmplitudeValue > this.AmpSliderUpperValue * 0.01) return false;
                if (this.MolecularIonFilter && alignedSpot.IsotopeTrackingWeightNumber > 0) return false;
                if (this.MolecularIonFilter && alignedSpot.PostDefinedIsotopeWeightNumber > 0) return false;

                if (this.MetaboliteNameFilter != string.Empty && !driftSpot.MetaboliteName.ToLower().Contains(this.MetaboliteNameFilter.ToLower())) return false;
                if (this.CommentFilter != string.Empty && !driftSpot.Comment.ToLower().Contains(this.CommentFilter.ToLower())) return false;
                if (alignedSpot.CentralRetentionTime < this.RtSliderLowerValue) return false;
                if (alignedSpot.CentralRetentionTime > this.RtSliderUpperValue) return false;

                if (this.UniqueionFilter && !driftSpot.IsFragmentQueryExist) return false;

                var metabolite = driftSpot.MetaboliteName;

                if (!this.AnnotatedFilter && !this.IdentifiedFilter && !this.UnknownFilter)
                    isDisplayed = true;

                if (this.IdentifiedFilter &&
                    metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                    isDisplayed = true;
                if (this.AnnotatedFilter &&
                    metabolite != string.Empty && metabolite.Contains("w/o"))
                    isDisplayed = true;
                if (this.UnknownFilter &&
                    (metabolite == string.Empty || metabolite.Contains("Unknown")))
                    isDisplayed = true;

                if (this.UniqueionFilter &&
                    !driftSpot.IsFragmentQueryExist)
                    isDisplayed = false;

                if (this.BlankFilter &&
                    driftSpot.IsBlankFiltered)
                    isDisplayed = false;

                //LC
                else {
                    if (this.ShortInchiKeyFilter != string.Empty && !row.ShortInchiKey.ToLower().Contains(this.ShortInchiKeyFilter.ToLower())) return false;
                    if (driftSpot.CentralAccurateMass < this.MzSliderLowerValue) return false;
                    if (driftSpot.CentralAccurateMass > this.MzSliderUpperValue) return false;
                    if (this.MsmsFilter && !driftSpot.MsmsIncluded) return false;
                }
            }
            else {
                var spot = row.AlignmentPropertyBean;
                if (spot.RelativeAmplitudeValue < this.AmpSliderLowerValue * 0.01) return false;
                if (spot.RelativeAmplitudeValue > this.AmpSliderUpperValue * 0.01) return false;
                if (this.MolecularIonFilter && spot.IsotopeTrackingWeightNumber > 0) return false;
                if (this.MolecularIonFilter && spot.PostDefinedIsotopeWeightNumber > 0) return false;

                if (this.MetaboliteNameFilter != string.Empty && !spot.MetaboliteName.ToLower().Contains(this.MetaboliteNameFilter.ToLower())) return false;
                if (this.CommentFilter != string.Empty && !spot.Comment.ToLower().Contains(this.CommentFilter.ToLower())) return false;
                if (spot.CentralRetentionTime < this.RtSliderLowerValue) return false;
                if (spot.CentralRetentionTime > this.RtSliderUpperValue) return false;

                if (this.UniqueionFilter && !spot.IsFragmentQueryExist) return false;

                var metabolite = spot.MetaboliteName;

                if (!this.AnnotatedFilter && !this.IdentifiedFilter && !this.UnknownFilter)
                    isDisplayed = true;

                if (this.IdentifiedFilter &&
                    metabolite != string.Empty && !metabolite.Contains("w/o") && !metabolite.Contains("Unknown"))
                    isDisplayed = true;
                if (this.AnnotatedFilter &&
                    metabolite != string.Empty && metabolite.Contains("w/o"))
                    isDisplayed = true;
                if (this.UnknownFilter &&
                    (metabolite == string.Empty || metabolite.Contains("Unknown")))
                    isDisplayed = true;

                if (this.UniqueionFilter &&
                    !spot.IsFragmentQueryExist)
                    isDisplayed = false;

                if (this.BlankFilter &&
                    spot.IsBlankFiltered)
                    isDisplayed = false;


                // GC
                if (spot.QuantMass > 0) {
                    if (spot.QuantMass < this.MzSliderLowerValue) return false;
                    if (spot.QuantMass > this.MzSliderUpperValue) return false;
                    if (this.MsmsFilter && !spot.MsmsIncluded) return false;
                }

                //LC
                else {
                    if (this.ShortInchiKeyFilter != string.Empty && !row.ShortInchiKey.ToLower().Contains(this.ShortInchiKeyFilter.ToLower())) return false;
                    if (spot.CentralAccurateMass < this.MzSliderLowerValue) return false;
                    if (spot.CentralAccurateMass > this.MzSliderUpperValue) return false;
                    if (this.MsmsFilter && !spot.MsmsIncluded) return false;

                    if (IsSimpleDotProductSimilarityCutOff && 
                        (row.AlignmentPropertyBean.SimpleDotProductSimilarity < SimpleDotProductSimilarityCutOffMin ||
                        row.AlignmentPropertyBean.SimpleDotProductSimilarity > SimpleDotProductSimilarityCutOffMax)) return false;
                    if (IsAccurateMassSimilarityCutOff && 
                        (row.AlignmentPropertyBean.AccurateMassSimilarity < AccurateMassSimilarityCutOffMin ||
                        row.AlignmentPropertyBean.AccurateMassSimilarity > AccurateMassSimilarityCutOffMax)) return false;
                    if (IsRetentionTimeSimilarityCutOff && 
                        (row.AlignmentPropertyBean.RetentionTimeSimilarity < RetentionTimeSimilarityCutOffMin || 
                        row.AlignmentPropertyBean.RetentionTimeSimilarity > RetentionTimeSimilarityCutOffMax)) return false;
                    if (IsDotProductSimilarityCutOff &&
                       (row.AlignmentPropertyBean.MassSpectraSimilarity < DotProductSimilarityCutOffMin ||
                        row.AlignmentPropertyBean.MassSpectraSimilarity > DotProductSimilarityCutOffMax)) return false;
                    if (IsReverseDotProductSimilarityCutOff &&
                       (row.AlignmentPropertyBean.ReverseSimilarity < ReverseDotProductSimilarityCutOffMin ||
                        row.AlignmentPropertyBean.ReverseSimilarity > ReverseDotProductSimilarityCutOffMax)) return false;
                    if (IsFragmentPresenceSimilarityCutOff &&
                       (row.AlignmentPropertyBean.FragmentPresencePercentage < FragmentPresenceSimilarityCutOffMin ||
                        row.AlignmentPropertyBean.FragmentPresencePercentage > FragmentPresenceSimilarityCutOffMax)) return false;
                }
            }

            return isDisplayed;
        }

        private bool compoundGroupFilter(CompoundGroup row) {

            if (this.MetaboliteNameFilter != string.Empty && !row.Names.ToLower().Contains(this.MetaboliteNameFilter.ToLower())) return false;
            return true;
        }
    }

}
