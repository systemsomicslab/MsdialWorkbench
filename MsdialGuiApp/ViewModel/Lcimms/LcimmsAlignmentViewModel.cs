using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Information;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Reactive.Linq;
using System.Windows.Data;
using System.Windows.Input;

namespace CompMs.App.Msdial.ViewModel.Lcimms
{
    internal sealed class LcimmsAlignmentViewModel : ViewModelBase, IAlignmentResultViewModel
    {
        public LcimmsAlignmentViewModel(
            LcimmsAlignmentModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
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

            Target = this.model.Target.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            Brushes = this.model.Brushes.AsReadOnly();
            SelectedBrush = this.model.ToReactivePropertySlimAsSynchronized(m => m.SelectedBrush).AddTo(Disposables);

            PeakSpotNavigatorViewModel = new PeakSpotNavigatorViewModel(model.PeakSpotNavigatorModel).AddTo(Disposables);

            Ms1Spots = CollectionViewSource.GetDefaultView(this.model.Ms1Spots);

            var (peakPlotFocusAction, peakPlotFocused) = focusControlManager.Request();
            RtMzPlotViewModel = new AlignmentPeakPlotViewModel(model.RtMzPlotModel, peakPlotFocusAction, peakPlotFocused).AddTo(Disposables);
            DtMzPlotViewModel = new AlignmentPeakPlotViewModel(model.DtMzPlotModel, peakPlotFocusAction, peakPlotFocused).AddTo(Disposables);

            Ms2SpectrumViewModel = new MsSpectrumViewModel(model.Ms2SpectrumModel).AddTo(Disposables);

            var (barChartViewFocusAction, barChartViewFocused) = focusControlManager.Request();
            RtBarChartViewModel = new BarChartViewModel(model.RtBarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            DtBarChartViewModel = new BarChartViewModel(model.DtBarChartModel, barChartViewFocusAction, barChartViewFocused).AddTo(Disposables);
            BarChartViewModels = new MultiBarChartViewModel(RtBarChartViewModel, DtBarChartViewModel).AddTo(Disposables);

            RtAlignmentEicViewModel = new AlignmentEicViewModel(model.RtAlignmentEicModel).AddTo(Disposables);
            DtAlignmentEicViewModel = new AlignmentEicViewModel(model.DtAlignmentEicModel).AddTo(Disposables);
            AlignmentEicViewModels = new MultiAlignmentEicViewModel(RtAlignmentEicViewModel, DtAlignmentEicViewModel).AddTo(Disposables);
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

            SearchCompoundCommand = new[]{
                model.Target.Select(t => t?.innerModel is null),
                model.MsdecResult.Select(r => r is null),
                model.CompoundSearchModel.Select(m => m is null),
            }.CombineLatestValuesAreAllFalse()
            .ToReactiveCommand()
            .WithSubscribe(() => {
                using (var vm = new LcimmsCompoundSearchViewModel(model.CompoundSearchModel.Value)) {
                    compoundSearchService.ShowDialog(vm);
                }
            })
            .AddTo(Disposables);

            FocusNavigatorViewModel = new FocusNavigatorViewModel(model.FocusNavigatorModel).AddTo(Disposables);

            PeakInformationViewModel = new PeakInformationViewModel(model.PeakInformationModel).AddTo(Disposables);
            CompoundDetailViewModel = new CompoundDetailViewModel(model.CompoundDetailModel).AddTo(Disposables);
            PeakDetailViewModels = new ViewModelBase[] { PeakInformationViewModel, CompoundDetailViewModel, };
        }

        public AlignmentPeakPlotViewModel RtMzPlotViewModel { get; }
        public AlignmentPeakPlotViewModel DtMzPlotViewModel { get; }
        public MsSpectrumViewModel Ms2SpectrumViewModel { get; }
        public BarChartViewModel RtBarChartViewModel { get; }
        public BarChartViewModel DtBarChartViewModel { get; }
        public MultiBarChartViewModel BarChartViewModels { get; }

        BarChartViewModel IAlignmentResultViewModel.BarChartViewModel => DtBarChartViewModel;
        public AlignmentEicViewModel RtAlignmentEicViewModel { get; }
        public AlignmentEicViewModel DtAlignmentEicViewModel { get; }
        public MultiAlignmentEicViewModel AlignmentEicViewModels { get; }

        /*
        public LcimmsAlignmentSpotTableViewModel AlignmentSpotTableViewModel {
            get => alignmentSpotTableViewModel;
            set => SetProperty(ref alignmentSpotTableViewModel, value);
        }
        private LcimmsAlignmentSpotTableViewModel alignmentSpotTableViewModel;
        */

        public ICollectionView Ms1Spots { get; }

        public ReadOnlyReactivePropertySlim<AlignmentSpotPropertyModel> Target { get; }

        public ReactivePropertySlim<BrushMapData<AlignmentSpotPropertyModel>> SelectedBrush { get; }
        public PeakSpotNavigatorViewModel PeakSpotNavigatorViewModel { get; }
        public ReadOnlyCollection<BrushMapData<AlignmentSpotPropertyModel>> Brushes { get; }

        private readonly LcimmsAlignmentModel model;
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;

        public FocusNavigatorViewModel FocusNavigatorViewModel { get; }
        public PeakInformationViewModel PeakInformationViewModel { get; }
        public CompoundDetailViewModel CompoundDetailViewModel { get; }
        public ViewModelBase[] PeakDetailViewModels { get; }

        public ReactiveCommand SearchCompoundCommand { get; }

        public ICommand ShowIonTableCommand => showIonTableCommand ?? (showIonTableCommand = new DelegateCommand(ShowIonTable));

        private DelegateCommand showIonTableCommand;

        private void ShowIonTable() {
            // peakSpotTableService.Show(AlignmentSpotTableViewModel);
        }

        public void SaveProject() {
            model.SaveProject();
        }
    }
}