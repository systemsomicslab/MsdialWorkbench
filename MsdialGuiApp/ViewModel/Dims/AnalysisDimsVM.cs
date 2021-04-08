using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.ViewModel.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    public class AnalysisDimsVM : AnalysisFileVM
    {
        public AnalysisDimsVM(DimsAnalysisModel model) {
            this.model = model;
            Ms1Peaks = CollectionViewSource.GetDefaultView(model.Ms1Peaks);

            AmplitudeOrderMin = model.Ms1Peaks.Min(peak => peak.AmplitudeOrderValue);
            AmplitudeOrderMax = model.Ms1Peaks.Max(peak => peak.AmplitudeOrderValue);
            PropertyChanged += OnFilterChanged;

            PlotViewModel = new AnalysisPeakPlotVM(model.PlotModel, "Mass", "KMD", string.Empty, "m/z", "Kendrick mass defect");
            WeakEventManager<AnalysisPeakPlotModel, PropertyChangedEventArgs>.AddHandler(model.PlotModel, "PropertyChanged", UpdateGraphTitleOnTargetChanged);
        }

        private readonly DimsAnalysisModel model;

        public AnalysisPeakPlotVM PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private AnalysisPeakPlotVM plotViewModel;

        private void UpdateGraphTitleOnTargetChanged(object sender, PropertyChangedEventArgs e) {
            if (e.PropertyName == nameof(model.PlotModel.Target)) {
                var peak = model.PlotModel.Target.InnerModel;
                PlotViewModel.GraphTitle = $"Spot ID: {peak.MasterPeakID} Scan: {peak.MS1RawSpectrumIdTop} Mass m/z: {peak.Mass:N5}";
            }
        }

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
        // public bool BlankFilterChecked => ReadDisplayFilters(DisplayFilter.Blank);
        // public bool UniqueIonsChecked => ReadDisplayFilters(DisplayFilter.UniqueIons);
        public bool ManuallyModifiedChecked => ReadDisplayFilters(DisplayFilter.ManuallyModified);

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = 0;


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

        public string CommentFilterKeyword {
            get => commentFilterKeyword;
            set {
                if (SetProperty(ref commentFilterKeyword, value)){
                    if (!string.IsNullOrEmpty(commentFilterKeyword)) {
                        commentFilterKeywords = commentFilterKeyword.Split().ToList();
                    }
                    else {
                        commentFilterKeywords = new List<string>(0);
                    }
                    Ms1Peaks?.Refresh();
                }
            }
        }
        private string commentFilterKeyword;
        private List<string> commentFilterKeywords = new List<string>(0);

        public string MetaboliteFilterKeyword {
            get => metaboliteFilterKeyword;
            set {
                if (SetProperty(ref metaboliteFilterKeyword, value)) {
                    if (!string.IsNullOrEmpty(metaboliteFilterKeyword)) {
                        metaboliteFilterKeywords = metaboliteFilterKeyword.Split().ToList();
                    }
                    else {
                        metaboliteFilterKeywords = new List<string>(0);
                    }
                    Ms1Peaks?.Refresh();
                }
            }
        }
        private string metaboliteFilterKeyword;
        private List<string> metaboliteFilterKeywords = new List<string>(0);

        public int FocusID {
            get => focusID;
            set => SetProperty(ref focusID, value);
        }
        private int focusID;

        public double FocusMz {
            get => focusMz;
            set => SetProperty(ref focusMz, value);
        }
        private double focusMz;

        bool PeakFilter(object obj) {
            if (obj is ChromatogramPeakFeatureVM peak) {
                return AnnotationFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && MetaboliteFilter(peak, metaboliteFilterKeywords)
                    && CommentFilter(peak, commentFilterKeywords)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation);
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

        bool CommentFilter(ChromatogramPeakFeatureVM peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(ChromatogramPeakFeatureVM peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
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
            model.FocusById(axis, FocusID);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            model.FocusByMz(axis, FocusMz);
        }

        public DelegateCommand<Window> SearchCompoundCommand => searchCompoundCommand ?? (searchCompoundCommand = new DelegateCommand<Window>(SearchCompound));
        private DelegateCommand<Window> searchCompoundCommand;

        private void SearchCompound(Window owner) {
            var vm = new CompoundSearchVM<ChromatogramPeakFeature>(model.AnalysisFile, model.Target.InnerModel, model.MsdecResult, null, model.MspAnnotator, model.Parameter.MspSearchParam);
            var window = new View.CompoundSearchWindow
            {
                DataContext = vm,
                Owner = owner,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };

            if (window.ShowDialog() == true) {
                model.Target.RaisePropertyChanged();
                _ = model.OnTargetChangedAsync(model.Target);
                Ms1Peaks?.Refresh();
            }
        }

        public DelegateCommand<Window> SaveMs2SpectrumCommand => saveMs2SpectrumCommand ?? (saveMs2SpectrumCommand = new DelegateCommand<Window>(SaveSpectra, CanSaveSpectra));
        private DelegateCommand<Window> saveMs2SpectrumCommand;

        private void SaveSpectra(Window owner)
        {
            var sfd = new SaveFileDialog
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp", // MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf;
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(owner) == true)
            {
                var filename = sfd.FileName;
                model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner)
        {
            return model.CanSaveSpectra();
        }


        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
