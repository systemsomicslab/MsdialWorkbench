using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    internal sealed class EicViewModel : ViewModelBase
    {
        public EicViewModel(
            EicModel model,
            IAxisManager<double>? horizontalAxis = null,
            IAxisManager<double>? verticalAxis = null) {

            ItemLoaded = model.ItemLoaded;

            Chromatogram = model.Chromatogram
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            if (horizontalAxis is null) {
                horizontalAxis = model.ChromRangeSource
                    .ToReactiveContinuousAxisManager<double>()
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis is null) {
                verticalAxis = model.AbundanceRangeSource
                    .ToReactiveContinuousAxisManager<double>(new RelativeMargin(0, 0.1), new AxisRange(0d, 0d), LabelType.Order)
                    .AddTo(Disposables);
            }
            VerticalAxis = verticalAxis;

            GraphTitle = model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            HorizontalTitle = model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            VerticalTitle = model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            VerticalProperty = model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        public ReadOnlyReactivePropertySlim<bool> ItemLoaded { get; }

        public ReadOnlyReactivePropertySlim<PeakChromatogram?> Chromatogram { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string?> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string?> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string?> VerticalProperty { get; }
    }
}
