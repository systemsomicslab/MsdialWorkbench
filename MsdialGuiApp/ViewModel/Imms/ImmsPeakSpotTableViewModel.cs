using CompMs.App.Msdial.Model;
using CompMs.App.Msdial.Model.Imms;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.ViewModel.Table;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;

namespace CompMs.App.Msdial.ViewModel.Imms
{
    abstract class ImmsPeakSpotTableViewModel : PeakSpotTableViewModelBase
    {
        protected ImmsPeakSpotTableViewModel(
            IImmsPeakSpotTableModel model,
            IReactiveProperty<double> massLower, IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower, IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(model, metaboliteFilterKeyword, commentFilterKeyword) {
            if (massLower is null) {
                throw new ArgumentNullException(nameof(massLower));
            }

            if (massUpper is null) {
                throw new ArgumentNullException(nameof(massUpper));
            }

            if (driftLower is null) {
                throw new ArgumentNullException(nameof(driftLower));
            }

            if (driftUpper is null) {
                throw new ArgumentNullException(nameof(driftUpper));
            }

            MassMin = model.MassMin;
            MassMax = model.MassMax;
            MassLower = massLower;
            MassUpper = massUpper;

            DriftMin = model.DriftMin;
            DriftMax = model.DriftMax;
            DriftLower = driftLower;
            DriftUpper = driftUpper;
        }

        public double MassMin { get; }
        public double MassMax { get; }
        public IReactiveProperty<double> MassLower { get; }
        public IReactiveProperty<double> MassUpper { get; }

        public double DriftMin { get; }
        public double DriftMax { get; }
        public IReactiveProperty<double> DriftLower { get; }
        public IReactiveProperty<double> DriftUpper { get; }
    }

    sealed class ImmsAnalysisPeakTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAnalysisPeakTableViewModel(
            ImmsAnalysisPeakTableModel model,
            IObservable<EicLoader> eicLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower,
            IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword, IReactiveProperty<string> commentFilterKeyword)
            : base(model, massLower, massUpper, driftLower, driftUpper, metaboliteFilterKeyword, commentFilterKeyword) {
            if (eicLoader is null) {
                throw new ArgumentNullException(nameof(eicLoader));
            }

            EicLoader = eicLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<EicLoader> EicLoader { get; }
    }

    sealed class ImmsAlignmentSpotTableViewModel : ImmsPeakSpotTableViewModel
    {
        public ImmsAlignmentSpotTableViewModel(
            ImmsAlignmentSpotTableModel model,
            IObservable<IBarItemsLoader> barItemsLoader,
            IReactiveProperty<double> massLower,
            IReactiveProperty<double> massUpper,
            IReactiveProperty<double> driftLower,
            IReactiveProperty<double> driftUpper,
            IReactiveProperty<string> metaboliteFilterKeyword,
            IReactiveProperty<string> commentFilterKeyword)
            : base(
                  model,
                  massLower,
                  massUpper,
                  driftLower,
                  driftUpper,
                  metaboliteFilterKeyword,
                  commentFilterKeyword) {
            if (barItemsLoader is null) {
                throw new ArgumentNullException(nameof(barItemsLoader));
            }

            BarItemsLoader = barItemsLoader.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<IBarItemsLoader> BarItemsLoader { get; }
    }
}
