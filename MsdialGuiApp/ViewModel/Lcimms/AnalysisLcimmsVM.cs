using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    public class AnalysisLcimmsVM : AnalysisFileVM
    {

        public AnalysisLcimmsVM(LcimmsAnalysisModel model) {
            this.model = model;

            AmplitudeOrderMin = model.Ms1Peaks.DefaultIfEmpty().Min(peak => peak?.AmplitudeOrderValue) ?? 0;
            AmplitudeOrderMax = model.Ms1Peaks.DefaultIfEmpty().Max(peak => peak?.AmplitudeOrderValue) ?? 0;
            Ms1Peaks = CollectionViewSource.GetDefaultView(model.Ms1Peaks);

            PropertyChanged += OnFilterChanged;
        }

        private readonly LcimmsAnalysisModel model;
        
        public ICollectionView Ms1Peaks {
            get => ms1Peaks;
            set {
                var old = ms1Peaks;
                if (SetProperty(ref ms1Peaks, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Peaks != null) ms1Peaks.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Peaks;

        public string DisplayLabel {
            get => displayLabel;
            set => SetProperty(ref displayLabel, value);
        }
        private string displayLabel;

        public bool RefMatchedChecked => ReadDisplayFilters(DisplayFilter.RefMatched);
        public bool SuggestedChecked => ReadDisplayFilters(DisplayFilter.Suggested);
        public bool UnknownChecked => ReadDisplayFilters(DisplayFilter.Unknown);
        public bool Ms2AcquiredChecked => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
        public bool MolecularIonChecked => ReadDisplayFilters(DisplayFilter.MolecularIon);
        public bool CcsChecked => ReadDisplayFilters(DisplayFilter.CcsMatched);
        public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        internal DisplayFilter DisplayFilters {
            get => displayFilters;
            set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = DisplayFilter.Unset;

        internal bool ReadDisplayFilters(DisplayFilter flag) {
            return displayFilters.Read(flag);
        }

        public double AmplitudeLowerValue {
            get => amplitudeLowerValue;
            set => SetProperty(ref amplitudeLowerValue, value);
        }

        public double AmplitudeUpperValue {
            get => amplitudeUpperValue;
            set => SetProperty(ref amplitudeUpperValue, value);
        }
        private double amplitudeLowerValue = 0d, amplitudeUpperValue = 1d;

        public double AmplitudeOrderMin { get; }
        public double AmplitudeOrderMax { get; }

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureVM peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureVM peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureVM peak) {
            return AmplitudeLowerValue * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        void OnFilterChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(DisplayFilters)
                || e.PropertyName == nameof(AmplitudeLowerValue)
                || e.PropertyName == nameof(AmplitudeUpperValue))
                Ms1Peaks?.Refresh();
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            model.FocusByID(axis);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;
        private void FocusByMz(IAxisManager axis) {
            model.FocusByMz(axis);
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        public void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM<ChromatogramPeakFeature>(model.AnalysisFile, model.Target.InnerModel, model.MsdecResult, null, model.MspAnnotator);
            var window = new View.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            window.ShowDialog();
        }
    }
}