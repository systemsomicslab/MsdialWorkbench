using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Lcimms;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Dims;
using CompMs.App.Msdial.ViewModel.Imms;
using CompMs.App.Msdial.ViewModel.Lcimms;
using CompMs.App.Msdial.ViewModel.Lcms;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM;
using CompMs.CommonMVVM.WindowService;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.ViewModel.Core
{
    public class DatasetViewModel : ViewModelBase
    {
        private readonly IWindowService<CompoundSearchVM> compoundSearchService;
        private readonly IWindowService<PeakSpotTableViewModelBase> peakSpotTableService;
        private readonly IWindowService<PeakSpotTableViewModelBase> proteomicsTableService;

        public DatasetViewModel(
            IDatasetModel model,
            IWindowService<CompoundSearchVM> compoundSearchService,
            IWindowService<PeakSpotTableViewModelBase> peakSpotTableService,
            IWindowService<PeakSpotTableViewModelBase> proteomicsTableService) {
            Model = model;
            this.compoundSearchService = compoundSearchService;
            this.peakSpotTableService = peakSpotTableService;
            this.proteomicsTableService = proteomicsTableService;
            MethodViewModel = model.ToReactivePropertySlimAsSynchronized(
                m => m.Method,
                m => ConvertToViewModel(m),
                vm => vm?.Model)
            .AddTo(Disposables);
        }

        public IDatasetModel Model { get; }

        public ReactivePropertySlim<MethodViewModel> MethodViewModel { get; }

        private MethodViewModel ConvertToViewModel(MethodModelBase model) {
            switch (model) {
                case LcmsMethodModel lc:
                    return new LcmsMethodVM(lc, compoundSearchService, peakSpotTableService, proteomicsTableService, Observable.Return(Model.Storage.Parameter));
                case ImmsMethodModel im:
                    return new ImmsMethodVM(im, compoundSearchService, peakSpotTableService);
                case DimsMethodModel di:
                    return new DimsMethodVM(di, compoundSearchService, peakSpotTableService);
                case LcimmsMethodModel lcim:
                    return new LcimmsMethodVM(lcim, compoundSearchService, peakSpotTableService);
                // case GcmsMethodModel _:
                default:
                    return null;
            }
        }
    }
}
