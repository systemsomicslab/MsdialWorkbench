using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class EicViewModel : ViewModelBase
    {
        public EicViewModel(
            EicModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {

            Eic = model.ObserveProperty(m => m.Eic)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            EicPeak = model.ObserveProperty(m => m.EicPeak)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            EicFocused = model.ObserveProperty(m => m.EicFocused)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            if (horizontalAxis == null) {
                var chromRange = model.ObserveProperty(m => m.ChromRange);
                horizontalAxis = new ReactiveAxisManager<double>(chromRange).AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis == null) {
                var abundanceRange = model.ObserveProperty(m => m.AbundanceRange);
                verticalAxis = new ReactiveAxisManager<double>(abundanceRange, new ChartMargin(0, 0.1), new Range(0d, 0d)).AddTo(Disposables);
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

        public ReadOnlyReactivePropertySlim<List<ChromatogramPeakWrapper>> Eic { get; }

        public ReadOnlyReactivePropertySlim<List<ChromatogramPeakWrapper>> EicPeak { get; }

        public ReadOnlyReactivePropertySlim<List<ChromatogramPeakWrapper>> EicFocused { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }
    }
}
