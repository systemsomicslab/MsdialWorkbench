using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Core
{
    public class ProjectViewModel : ViewModelBase
    {
        public ProjectViewModel(
            IProjectModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<AnalysisFilePropertySetViewModel> analysisFilePropertyResetService) {
            CurrentDataset = model.ToReactivePropertySlimAsSynchronized(m => m.CurrentDataset).AddTo(Disposables);
            Datasets = model.Datasets.ToReadOnlyReactiveCollection().AddTo(Disposables);

            CurrentDatasetViewModel = model.ToReactivePropertySlimAsSynchronized(
                m => m.CurrentDataset,
                m => m is null ? null : new DatasetViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, analysisFilePropertyResetService),
                vm => vm?.Model
            ).AddTo(Disposables);
        }

        public ReactivePropertySlim<IDatasetModel> CurrentDataset { get; }
        public ReadOnlyReactiveCollection<IDatasetModel> Datasets { get; }

        public ReactivePropertySlim<DatasetViewModel> CurrentDatasetViewModel { get; }
    }
}
