using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class MsSpectrumViewModel : ViewModelBase
    {
        public MsSpectrumViewModel(
            MsSpectrumModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> upperVerticalAxis = null,
            IAxisManager<double> lowerVerticalAxis = null) {

            this.model = model ?? throw new ArgumentNullException(nameof(model));

            if (horizontalAxis is null) {
                horizontalAxis = this.model.ObserveProperty(m => m.HorizontalRange)
                    .ToReactiveAxisManager<double>(new ChartMargin(0.05))
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (upperVerticalAxis is null) {
                upperVerticalAxis = this.model.ObserveProperty(m => m.UpperVerticalRange)
                    .ToReactiveAxisManager<double>(new ChartMargin(0, 0.05), new Range(0d, 0d))
                    .AddTo(Disposables);
            }
            UpperVerticalAxis = upperVerticalAxis;

            if (lowerVerticalAxis is null) {
                lowerVerticalAxis = this.model.ObserveProperty(m => m.LowerVerticalRange)
                    .ToReactiveAxisManager<double>(new ChartMargin(0, 0.05), new Range(0d, 0d))
                    .AddTo(Disposables);
            }
            LowerVerticalAxis = lowerVerticalAxis;

            UpperSpectrum = this.model.ObserveProperty(m => m.UpperSpectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrum = this.model.ObserveProperty(m => m.LowerSpectrum)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            GraphTitle = this.model.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalTitle = this.model.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalTitle = this.model.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            HorizontalProperty = this.model.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            VerticalProperty = this.model.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LabelProperty = this.model.ObserveProperty(m => m.LabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            OrderingProperty = this.model.ObserveProperty(m => m.OrderingProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        private readonly MsSpectrumModel model;

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> UpperSpectrum { get; }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> LowerSpectrum { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> UpperVerticalAxis { get; }

        public IAxisManager<double> LowerVerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> LabelProperty { get; }

        public ReadOnlyReactivePropertySlim<string> OrderingProperty { get; }
    }
}
