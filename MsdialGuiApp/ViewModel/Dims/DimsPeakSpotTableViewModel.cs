using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Dims;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Dims
{
    abstract class DimsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected DimsPeakSpotTableViewModel(
            IDimsPeakSpotTableModel model,
            IReactiveProperty<double> massLower, IReactiveProperty<double> massUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(model, metaboliteFilterKeyword, commentFilterKeyword) {
            if (massLower is null) {
                throw new ArgumentNullException(nameof(massLower));
            }

            if (massUpper is null) {
                throw new ArgumentNullException(nameof(massUpper));
            }

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            MassLower = massLower;
            MassUpper = massUpper;
        }

        public double MassMin { get; }

        public double MassMax { get; }

        public IReactiveProperty<double> MassLower { get; }

        public IReactiveProperty<double> MassUpper { get; }
    }

    sealed class DimsAnalysisPeakTableViewModel : DimsPeakSpotTableViewModel
    {
        public DimsAnalysisPeakTableViewModel(
            DimsPeakSpotTableModel<ChromatogramPeakFeatureModel> model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower, IReactiveProperty<double> massUpper,
            IReactiveProperty<string> metaboliteFilterKeyword, IReactiveProperty<string> commentFilterKeyword)
            : base(model, massLower, massUpper, metaboliteFilterKeyword, commentFilterKeyword) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }

            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    sealed class DimsAlignmentSpotTableViewModel : DimsPeakSpotTableViewModel
    {
        public DimsAlignmentSpotTableViewModel(
            DimsPeakSpotTableModel<AlignmentSpotPropertyModel> model,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper, IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(model, massLower, massUpper, metaboliteFilterKeyword, commentFilterKeyword) {

        }
    }
}
