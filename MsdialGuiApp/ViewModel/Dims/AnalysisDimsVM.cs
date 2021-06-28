using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.ChemView;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.DataObj;
using Microsoft.Win32;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class AnalysisDimsVM : AnalysisFileVM
    {
        public AnalysisDimsVM(
            DimsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService) {

            Model = model;
            this.compoundSearchService = compoundSearchService;

            Target = Model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            Target.Subscribe(UpdateGraphTitleOnTargetChanged);

            MassMin = Model.MassMin;
            MassMax = Model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty);
            CommentFilterKeywords = CommentFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            var AmplitudeLowerValue = this.ObserveProperty(m => m.AmplitudeLowerValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var AmplitudeUpperValue = this.ObserveProperty(m => m.AmplitudeUpperValue)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AmplitudeOrderMin = Model.Ms1Peaks.Min(peak => peak.AmplitudeOrderValue);
            AmplitudeOrderMax = Model.Ms1Peaks.Max(peak => peak.AmplitudeOrderValue);

            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
                AmplitudeLowerValue.ToUnit(),
                AmplitudeUpperValue.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Peaks.Refresh())
            .AddTo(Disposables);

            Ms1Peaks = CollectionViewSource.GetDefaultView(Model.Ms1Peaks);

            var hAxis = Model.PlotModel2
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = Model.PlotModel2
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);

            PlotViewModel = new AnalysisPeakPlotViewModel(Model.PlotModel2, brushSource: Observable.Return(Model.Brush), horizontalAxis: hAxis, verticalAxis: vAxis).AddTo(Disposables);
            EicViewModel = new EicViewModel(Model.EicModel2, horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(Model.Ms2SpectrumModel2).AddTo(Disposables);
            PeakTableViewModel = new DimsAnalysisPeakTableViewModel(
                Model.PeakTableModel,
                MassLower,
                MassUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);

            SearchCompoundCommand = new[]
            {
                Model.Target.Select(t => t?.InnerModel != null),
                Model.MsdecResult.Select(r => r != null),
            }.CombineLatestValuesAreAllTrue()
            .ToReactiveCommand()
            .WithSubscribe(SearchCompound)
            .AddTo(Disposables);
        }

        public DimsAnalysisModel Model { get; }

        private readonly IWindowService<CompoundSearchVM> compoundSearchService;

        public AnalysisPeakPlotViewModel PlotViewModel {
            get => plotViewModel2;
            set => SetProperty(ref plotViewModel2, value);
        }
        private AnalysisPeakPlotViewModel plotViewModel2;

        private void UpdateGraphTitleOnTargetChanged(ChromatogramPeakFeatureModel t) {
            if (t == null) {
                Model.PlotModel2.GraphTitle = string.Empty;
                Model.EicModel2.GraphTitle = string.Empty;
            }
            else {
                Model.PlotModel2.GraphTitle = $"Spot ID: {t.MasterPeakID} Scan: {t.MS1RawSpectrumIdTop} Mass m/z: {t.Mass:N5}";
                Model.EicModel2.GraphTitle = $"{t.Mass:N4}[Da]  Max intensity: {Model.EicModel2.MaxIntensity:F0}";
            }
        }

        public EicViewModel EicViewModel {
            get => eicViewModel2;
            set => SetProperty(ref eicViewModel2, value);
        }
        private EicViewModel eicViewModel2;

        public RawDecSpectrumsViewModel RawDecSpectrumsViewModel {
            get => rawDecSpectrumsViewModel;
            set => SetProperty(ref rawDecSpectrumsViewModel, value);
        }
        private RawDecSpectrumsViewModel rawDecSpectrumsViewModel;

        public DimsAnalysisPeakTableViewModel PeakTableViewModel {
            get => peakTableViewModel;
            set => SetProperty(ref peakTableViewModel, value);
        }
        private DimsAnalysisPeakTableViewModel peakTableViewModel;

        public ReadOnlyReactivePropertySlim<ChromatogramPeakFeatureModel> Target { get; }

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

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public ReactivePropertySlim<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }
        public ReactivePropertySlim<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }

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
            if (obj is ChromatogramPeakFeatureModel peak) {
                return AnnotationFilter(peak)
                    && MzFilter(peak)
                    && AmplitudeFilter(peak)
                    && (!Ms2AcquiredChecked || peak.IsMsmsContained)
                    && (!MolecularIonChecked || peak.IsotopeWeightNumber == 0)
                    && (!ManuallyModifiedChecked || peak.InnerModel.IsManuallyModifiedForAnnotation)
                    && MetaboliteFilter(peak, MetaboliteFilterKeywords.Value)
                    && CommentFilter(peak, CommentFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(ChromatogramPeakFeatureModel peak) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && peak.IsRefMatched
                || SuggestedChecked && peak.IsSuggested
                || UnknownChecked && peak.IsUnknown;
        }

        bool AmplitudeFilter(ChromatogramPeakFeatureModel peak) {
            return AmplitudeLowerValue * (AmplitudeOrderMax - AmplitudeOrderMin) <= peak.AmplitudeOrderValue - AmplitudeOrderMin
                && peak.AmplitudeScore - AmplitudeOrderMin <= AmplitudeUpperValue * (AmplitudeOrderMax - AmplitudeOrderMin);
        }

        bool MzFilter(ChromatogramPeakFeatureModel peak) {
            return MassLower.Value <= peak.Mass && peak.Mass <= MassUpper.Value;
        }

        bool CommentFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => string.IsNullOrEmpty(keyword) || (peak.Comment?.Contains(keyword) ?? false));
        }

        bool MetaboliteFilter(ChromatogramPeakFeatureModel peak, IEnumerable<string> keywords) {
            return keywords.All(keyword => peak.Name.Contains(keyword));
        }

        public DelegateCommand<IAxisManager> FocusByIDCommand => focusByIDCommand ?? (focusByIDCommand = new DelegateCommand<IAxisManager>(FocusByID));
        private DelegateCommand<IAxisManager> focusByIDCommand;

        private void FocusByID(IAxisManager axis) {
            Model.FocusById(axis, FocusID);
        }

        public DelegateCommand<IAxisManager> FocusByMzCommand => focusByMzCommand ?? (focusByMzCommand = new DelegateCommand<IAxisManager>(FocusByMz));
        private DelegateCommand<IAxisManager> focusByMzCommand;

        private static readonly double MzTol = 20;
        private void FocusByMz(IAxisManager axis) {
            Model.FocusByMz(axis, FocusMz);
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private void SearchCompound() {
            using (var model = new CompoundSearchModel<ChromatogramPeakFeature>(
                Model.AnalysisFile,
                Model.Target.Value.InnerModel,
                Model.MsdecResult.Value,
                null,
                Model.MspAnnotator,
                new MsRefSearchParameterBase(Model.Parameter.MspSearchParam)))
            using (var vm = new CompoundSearchVM(model)) {
                if (compoundSearchService.ShowDialog(vm) == true) {
                    Model.Target.Value.RaisePropertyChanged();
                    _ = Model.OnTargetChangedAsync(Model.Target.Value);
                    Ms1Peaks?.Refresh();
                }
            }
        }

        public DelegateCommand<Window> ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand<Window>(ShowIonTable));
        private DelegateCommand<Window> showIonTableCommand;

        private void ShowIonTable(Window owner) {
            var window = new View.Table.AlignmentSpotTable
            {
                DataContext = PeakTableViewModel,
                WindowStartupLocation = WindowStartupLocation.CenterScreen,
                Owner = owner,
            };

            window.Show();
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
                Model.SaveSpectra(filename);
            }
        }

        private bool CanSaveSpectra(Window owner)
        {
            return Model.CanSaveSpectra();
        }


        private bool ReadDisplayFilters(DisplayFilter flags) {
            return (flags & DisplayFilters) != 0;
        }
    }
}
