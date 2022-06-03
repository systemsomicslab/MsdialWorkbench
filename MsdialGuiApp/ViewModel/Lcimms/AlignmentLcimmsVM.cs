using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Search;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using CompMs.MsdialCore.DataObj;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class AlignmentLcimmsVM : AlignmentFileViewModel
    {
        public AlignmentLcimmsVM(
            LcimmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            FocusControlManager focusControlManager)
            : base(model) {
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

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = this.model.Brushes.AsReadOnly();
            SelectedBrush = this.model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);
            PeakFilterViewModel = PeakSpotNavigatorViewModel.PeakFilterViewModel;

            Ms1Spots = CollectionViewSource.GetDefaultView(this.model.Ms1Spots);

            var (peakPlotFocusAction, peakPlotFocused) = focusControlManager.Request();
            PlotViewModel = new AlignmentPeakPlotViewModel(model.PlotModel, peakPlotFocusAction, peakPlotFocused).AddTo(Disposables);

            Ms2SpectrumViewModel = new MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);
            BarChartViewModel = new BarChartViewModel(model.BarChartModel, null, Observable.Never<bool>()).AddTo(Disposables);
            AlignmentEicViewModel = new AlignmentEicViewModel(model.AlignmentEicModel).AddTo(Disposables);
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
            SearchCompoundCommand.Subscribe(SearchCompound).AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);
        }

        public AlignmentPeakPlotViewModel PlotViewModel {
            get => plotViewModel;
            set => SetProperty(ref plotViewModel, value);
        }
        private AlignmentPeakPlotViewModel plotViewModel;

        public MsSpectrumViewModel Ms2SpectrumViewModel {
            get => ms2SpectrumViewModel;
            set => SetProperty(ref ms2SpectrumViewModel, value);
        }
        private MsSpectrumViewModel ms2SpectrumViewModel;

        public BarChartViewModel BarChartViewModel {
            get => barChartViewModel;
            set => SetProperty(ref barChartViewModel, value);
        }
        private BarChartViewModel barChartViewModel;

        public AlignmentEicViewModel AlignmentEicViewModel {
            get => alignmentEicViewModel;
            set => SetProperty(ref alignmentEicViewModel, value);
        }
        private AlignmentEicViewModel alignmentEicViewModel;

        /*
        public LcimmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private LcimmsAlignmentSpotTableViewModel alignmentSpotTableViewModel;
        */

        public ICollectionView Ms1Spots {
            get => ms1Spots;
            set => SetProperty(ref ms1Spots, value);
        }
        private ICollectionView ms1Spots;

        public override ICollectionView PeakSpotsView => ms1Spots;

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>> SelectedBrush { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public PeakFilterViewModel PeakFilterViewModel { get; }
        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        private readonly LcimmsAlignmentModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

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