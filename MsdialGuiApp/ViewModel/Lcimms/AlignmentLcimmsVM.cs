using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Base;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Data;


namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    class AlignmentLcimmsVM : AlignmentFileViewModel
    {
        public AlignmentLcimmsVM(
            LcimmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService)
            : base(model) {
            if (compoundSearchService is null) {
                throw new ArgumentNullException(nameof(compoundSearchService));
            }
            if (peakSpotTableService is null) {
                throw new ArgumentNullException(nameof(peakSpotTableService));
            }

            this.model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;

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

            RtMin = this.model.RtMin;
            RtMax = this.model.RtMax;
            RtLower = new ReactiveProperty<double>(RtMin).AddTo(Disposables);
            RtUpper = new ReactiveProperty<double>(RtMax).AddTo(Disposables);
            RtLower.SetValidateNotifyError(v => v < RtMin ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtUpper.Value ? "Too large" : null);
            RtUpper.SetValidateNotifyError(v => v < RtLower.Value ? "Too small" : null)
                .SetValidateNotifyError(v => v > RtMax ? "Too large" : null);

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

            PlotViewModel = new Chart.AlignmentPeakPlotViewModel(model.PlotModel, SelectedBrush).AddTo(Disposables);
            Ms2SpectrumViewModel = new Chart.MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            BarChartViewModel = new Chart.BarChartViewModel(model.BarChartModel, brushSource: null).AddTo(Disposables);
            AlignmentEicViewModel = new Chart.AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
            /*
            AlignmentSpotTableViewModel = new LcimmsAlignmentSpotTableViewModel(
                model.AlignmentSpotTableModel,
                Observable.Return(model.BarItemsLoader),
                MassLower, MassUpper,
                DriftLower, DriftUpper,
                MetaboliteFilterKeyword,
                CommentFilterKeyword)
                .AddTo(Disposables);
            */

            SearchCompoundCommand = this.model.Target
                .CombineLatest(this.model.MsdecResult, (t, r) => t?.innerModel != null && r != null)
                .ToReactiveCommand()
                .AddTo(Disposables);
            SearchCompoundCommand.Subscribe(SearchCompound);
        }

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

        /*
        public LcimmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private LcimmsAlignmentSpotTableViewModel alignmentSpotTableViewModel;
        */

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

        public override ICollectionView PeakSpotsView => ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<IBrushMapper<AlignmentSpotPropertyModel>> SelectedBrush { get; }

        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        public double MassMin { get; }
        public double MassMax { get; }
        public ReactiveProperty<double> MassLower { get; }
        public ReactiveProperty<double> MassUpper { get; }

        public double RtMin { get; }
        public double RtMax { get; }
        public ReactiveProperty<double> RtLower { get; }
        public ReactiveProperty<double> RtUpper { get; }

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
            set => SetDisplayFilters(DisplayFilter.RefMatched, value);
        }

        public bool SuggestedChecked {
            get => ReadDisplayFilters(DisplayFilter.Suggested);
            set => SetDisplayFilters(DisplayFilter.Suggested, value);
        }

        public bool UnknownChecked {
            get => ReadDisplayFilters(DisplayFilter.Unknown);
            set => SetDisplayFilters(DisplayFilter.Unknown, value);
        }

        public bool Ms2AcquiredChecked {
            get => ReadDisplayFilters(DisplayFilter.Ms2Acquired);
            set => SetDisplayFilters(DisplayFilter.Ms2Acquired, value);
        }

        public bool MolecularIonChecked {
            get => ReadDisplayFilters(DisplayFilter.MolecularIon);
            set => SetDisplayFilters(DisplayFilter.MolecularIon, value);
        }

        public bool BlankFilterChecked {
            get => ReadDisplayFilters(DisplayFilter.Blank);
            set => SetDisplayFilters(DisplayFilter.Blank, value);
        }

        public bool UniqueIonsChecked {
            get => ReadDisplayFilters(DisplayFilter.UniqueIons);
            set => SetDisplayFilters(DisplayFilter.UniqueIons, value);
        }

        public bool CcsChecked {
            get => ReadDisplayFilters(DisplayFilter.CcsMatched);
            set => SetDisplayFilters(DisplayFilter.CcsMatched, value);
        }

        public bool ManuallyModifiedChecked {
            get => ReadDisplayFilters(DisplayFilter.ManuallyModified);
            set => SetDisplayFilters(DisplayFilter.ManuallyModified, value);
        }

        public DisplayFilter DisplayFilters {
            get => displayFilters;
            protected internal set => SetProperty(ref displayFilters, value);
        }
        private DisplayFilter displayFilters = 0;

        private bool ReadDisplayFilters(DisplayFilter flags) {
            return DisplayFilters.Read(flags);
        }

        protected void WriteDisplayFilters(DisplayFilter flag, bool value) {
            displayFilters.Write(flag, value);
        }

        protected bool SetDisplayFilters(DisplayFilter flag, bool value) {
            if (ReadDisplayFilters(flag) != value) {
                WriteDisplayFilters(flag, value);
                OnPropertyChanged(nameof(DisplayFilters));
                return true;
            }
            return false;
        }

        bool PeakFilter(object obj) {
            if (obj is AlignmentSpotPropertyModel spot) {
                return AnnotationFilter(spot)
                    && MzFilter(spot)
                    && RtFilter(spot)
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
            return RefMatchedChecked && spot.IsRefMatched(model.DataBaseMapper)
                || SuggestedChecked && spot.IsSuggested(model.DataBaseMapper)
                || UnknownChecked && spot.IsUnknown;
        }

        bool MzFilter(AlignmentSpotPropertyModel spot) {
            return MassLower.Value <= spot.MassCenter
                && spot.MassCenter <= MassUpper.Value;
        }

        bool RtFilter(AlignmentSpotPropertyModel spot) {
            return RtLower.Value <= spot.innerModel.TimesCenter.RT.Value
                && spot.innerModel.TimesCenter.RT.Value <= RtUpper.Value;
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

        private readonly LcimmsAlignmentModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        private void SearchCompound() {
            if (model.Target.Value?.innerModel == null || model.MsdecResult.Value == null)
                return;

            using (var model = new CompoundSearchModel<AlignmentSpotProperty>(
                this.model.AlignmentFile,
                Target.Value.innerModel,
                this.model.MsdecResult.Value,
                null,
                null))
            using (var vm = new CompoundSearchVM(model)) {
                compoundSearchService.ShowDialog(vm);
            }
        }

        public DelegateCommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));

        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public void SaveProject() {
            model.SaveProject();
        }
    }
}