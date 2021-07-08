using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.Common.Parameter;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
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

namespace CompMs.App.Msdial.ViewModel.Dims
{
    class AnalysisDimsVM : AnalysisFileViewModel
    {
        public AnalysisDimsVM(
            DimsAnalysisModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

            Target = Model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

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

            var hAxis = Model.PlotModel
                .ObserveProperty(m => m.HorizontalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);
            var vAxis = Model.PlotModel
                .ObserveProperty(m => m.VerticalRange)
                .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                .AddTo(Disposables);

            PlotViewModel = new AnalysisPeakPlotViewModel(Model.PlotModel, brushSource: Observable.Return(Model.Brush), horizontalAxis: hAxis, verticalAxis: vAxis).AddTo(Disposables);
            EicViewModel = new EicViewModel(Model.EicModel, horizontalAxis: hAxis).AddTo(Disposables);
            RawDecSpectrumsViewModel = new RawDecSpectrumsViewModel(Model.Ms2SpectrumModel).AddTo(Disposables);
            PeakTableViewModel = new DimsAnalysisPeakTableViewModel(
                Model.PeakTableModel,
                Observable.Return(Model.EicLoader),
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
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public AnalysisPeakPlotViewModel PlotViewModel {
            get => plotViewModel2;
            set => SetProperty(ref plotViewModel2, value);
        }
        private AnalysisPeakPlotViewModel plotViewModel2;

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

        public override ICollectionView PeakSpots => ms1Peaks;

        public bool RefMatchedChecked {
            get => ReadDisplayFilters(DisplayFilter.RefMatched);
            set {
                if (ReadDisplayFilters(DisplayFilter.RefMatched) != value) {
                    displayFilters.Write(DisplayFilter.RefMatched, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool SuggestedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Suggested);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Suggested) != value) {
                    displayFilters.Write(DisplayFilter.Suggested, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool UnknownChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Unknown);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Unknown) != value) {
                    displayFilters.Write(DisplayFilter.Unknown, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool Ms2AcquiredChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Ms2Acquired) != value) {
                    displayFilters.Write(DisplayFilter.Ms2Acquired, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool MolecularIonChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.MolecularIon);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.MolecularIon) != value) {
                    displayFilters.Write(DisplayFilter.MolecularIon, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool ManuallyModifiedChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.ManuallyModified);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.ManuallyModified) != value) {
                    displayFilters.Write(DisplayFilter.ManuallyModified, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

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
                    Ms1Peaks?.Refresh();
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(PeakTableViewModel);
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
