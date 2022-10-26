using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    internal sealed class ProjectViewModel : ViewModelBase
    {
        public ProjectViewModel(
            IProjectModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService,
            IWindowService<AnalysisFilePropertyResetViewModel> analysisFilePropertyResetService,
            IMessageBroker broker) {
            CurrentDataset = model.ToReactivePropertySlimAsSynchronized(m => m.CurrentDataset).AddTo(Disposables);
            Datasets = model.Datasets.ToReadOnlyReactiveCollection().AddTo(Disposables);

            CurrentDatasetViewModel = model.ObserveProperty(m => m.CurrentDataset)
                .Select(m => m is null ? null : new DatasetViewModel(m, compoundSearchService, peakSpotTableService, proteomicsTableService, analysisFilePropertyResetService, broker))
                .DisposePreviousValue()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ReactivePropertySlim<IDatasetModel> CurrentDataset { get; }
        public ReadOnlyReactiveCollection<IDatasetModel> Datasets { get; }

        public ReadOnlyReactivePropertySlim<DatasetViewModel> CurrentDatasetViewModel { get; }
    }
}
