using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    internal sealed class ImmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        public ImmsAlignmentViewModel(
            ImmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IMessageBroker messageBroker,
            FocusControlManager focusControlManager) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            if (focusControlManager is null) {
                throw new ArgumentNullException(nameof(focusControlManager));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            _messageBroker = messageBroker;
            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = this.model.Brushes.AsReadOnly();
            SelectedBrush = this.model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            MassMin = this.model.MassMin;
            MassMax = this.model.MassMax;
            MassLower = new ReactiveProperty<double>(MassMin).AddTo(Disposables);
            MassUpper = new ReactiveProperty<double>(MassMax).AddTo(Disposables);
            MassLower.SetValidateNotifyError(v => v < MassMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassUpper.Value ? "Too large" : null);
            MassUpper.SetValidateNotifyError(v => v < MassLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > MassMax ? "Too large" : null);

            DriftMin = this.model.DriftMin;
            DriftMax = this.model.DriftMax;
            DriftLower = new ReactiveProperty<double>(DriftMin).AddTo(Disposables);
            DriftUpper = new ReactiveProperty<double>(DriftMax).AddTo(Disposables);
            DriftLower.SetValidateNotifyError(v => v < DriftMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftUpper.Value ? "Too large" : null);
            DriftUpper.SetValidateNotifyError(v => v < DriftLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > DriftMax ? "Too large" : null);

            MetaboliteFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);
            CommentFilterKeyword = new ReactivePropertySlim<string>(string.Empty).AddTo(Disposables);

            CommentFilterKeywords = CommentFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            MetaboliteFilterKeywords = MetaboliteFilterKeyword.Select(c => c.Split()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var DisplayFilters = this.ObserveProperty(m => m.DisplayFilters)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            new[]
            {
                MassLower.ToUnit(),
                MassUpper.ToUnit(),
                DriftLower.ToUnit(),
                DriftUpper.ToUnit(),
                CommentFilterKeyword.ToUnit(),
                MetaboliteFilterKeyword.ToUnit(),
                DisplayFilters.ToUnit(),
            }.Merge()
            .Throttle(TimeSpan.FromMilliseconds(500))
            .ObserveOnDispatcher()
            .Subscribe(_ => Ms1Spots.Refresh())
            .AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(this.model.Ms1Spots);

            var (focusAction, focused) = focusControlManager.Request();
            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(model.PlotModel, focusAction, focused).AddTo(Disposables);

            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            BarChartViewModel = new Chart.BarChartViewModel(model.BarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
            AlignmentSpotTableViewModel = new ImmsAlignmentSpotTableViewModel(
                model.AlignmentSpotTableModel,
                MassLower, MassUpper,
                DriftLower, DriftUpper,
                MetaboliteFilterKeyword, CommentFilterKeyword,
                Observable.Never<bool>().ToReactiveProperty())
                .AddTo(Disposables);

            SearchCompoundCommand = this.model.CanSearchCompound
                .ToReactiveCommand()
                .WithSubscribe(SearchCompound)
                .AddTo(Disposables);
        }

        private readonly ImmsAlignmentModel model;

        public Chart.AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private Chart.AlignmentPeakPlotViewModel plotViewModel;

        public Chart.MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private Chart.MsSpectrumViewModel ms2SpectrumViewModel;

        public Chart.BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            set => SetProperty(ref barChartViewModel, value);
        }
        private Chart.BarChartViewModel barChartViewModel;

        public Chart.AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            set => SetProperty(ref alignmentEicViewModel, value);
        }
        private Chart.AlignmentEicViewModel alignmentEicViewModel;

        public ImmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private ImmsAlignmentSpotTableViewModel alignmentSpotTableViewModel;

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set {
                var old = ms1Spots;
                if (SetProperty(ref ms1Spots, value)) {
                    if (old != null) old.Filter -= PeakFilter;
                    if (ms1Spots != null) ms1Spots.Filter += PeakFilter;
                }
            }
        }
        private ICollectionView ms1Spots;

        public ICollectionView PeakSpotsView => ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public ReactiveProperty<double> DriftLower { get; }
        public ReactiveProperty<double> DriftUpper { get; }

        public IReactiveProperty<string> CommentFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> CommentFilterKeywords { get; }
        public IReactiveProperty<string> MetaboliteFilterKeyword { get; }
        public ReadOnlyReactivePropertySlim<string[]> MetaboliteFilterKeywords { get; }

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

        public bool BlankFilterChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.Blank);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.Blank) != value) {
                    displayFilters.Write(DisplayFilter.Blank, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool UniqueIonsChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.UniqueIons);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.UniqueIons) != value) {
                    displayFilters.Write(DisplayFilter.UniqueIons, value);
                    OnPropertyChanged(nameof(DisplayFilters));
                }
            }
        }

        public bool CcsChecked {
            get {
                return ReadDisplayFilters(DisplayFilter.CcsMatched);
            }
            set {
                if (ReadDisplayFilters(DisplayFilter.CcsMatched) != value) {
                    displayFilters.Write(DisplayFilter.CcsMatched, value);
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
            protected internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = 0;

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && DriftFilter(spot)
                    && (!Ms2AcquiredChecked || spot.IsMsmsAssigned)
                    && (!MolecularIonChecked || spot.IsBaseIsotopeIon)
                    && (!BlankFilterChecked || spot.IsBlankFiltered)
                    && (!ManuallyModifiedChecked || spot.innerModel.IsManuallyModifiedForAnnotation)
                    && CommentFilter(spot, CommentFilterKeywords.Value)
                    && MetaboliteFilter(spot, MetaboliteFilterKeywords.Value);
            }
            return false;
        }

        bool AnnotationFilter(AlignmentSpotPropertyModel spot) {
            if (!ReadDisplayFilters(DisplayFilter.Annotates)) return true;
            return RefMatchedChecked && spot.IsRefMatched(model.MatchResultEvaluator)
                || SuggestedChecked && spot.IsSuggested(model.MatchResultEvaluator)
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower.Value <= spot.MassCenter
                && spot.MassCenter <= MassUpper.Value;
        }

        bool DriftFilter(AlignmentSpotPropertyModel spot) {
            return DriftLower.Value <= spot.innerModel.TimesCenter.Drift.Value
                && spot.innerModel.TimesCenter.Drift.Value <= DriftUpper.Value;
        }

        bool CommentFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Comment.Contains(keyword));
        }

        bool MetaboliteFilter(AlignmentSpotPropertyModel spot, IEnumerable<string> keywords) {
            return keywords.All(keyword => spot.Name.Contains(keyword));
        }

        public ReactiveCommand SearchCompoundCommand { get; }

        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IMessageBroker _messageBroker;

        private void SearchCompound() {
            using (var csm = model.CreateCompoundSearchModel()) {
                if (csm is null) {
                    return;
                }
                using (var vm = new ImmsCompoundSearchVM(csm)) {
                    compoundSearchService.ShowDialog(vm);
                }
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));
        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        private void SaveSpectra(Window owner) {
            var request = new SaveFileNameRequest(model.SaveSpectra)
            {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp|MassBank format(*.txt)|*.txt;|MASCOT format(*.mgf)|*.mgf|MSFINDER format(*.mat)|*.mat;|SIRIUS format(*.ms)|*.ms",
                RestoreDirectory = true,
                AddExtension = true,
            };
            _messageBroker.Publish(request);
        }

        private bool CanSaveSpectra(Window owner) {
            return this.model.CanSaveSpectra();
        }

        public void SaveProject() {
            model.SaveProject();
        }

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return DisplayFilters.Read(flags);
        }
    }
}
