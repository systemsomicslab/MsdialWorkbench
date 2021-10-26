using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class SurveyScanViewModel : ViewModelBase
    {
        public SurveyScanViewModel(
            SurveyScanModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> verticalAxis = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            this.model = model;

            if (horizontalAxis is null) {
                horizontalAxis = this.model.HorizontalRangeSource
                    .ToReactiveAxisManager<double>(new RelativeMargin(0.05))
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (verticalAxis is null) {
                verticalAxis = this.model.VerticalRangeSource
                    .ToReactiveAxisManager<double>(new ConstantMargin(0, 40), new Range(0d, 0d), LabelType.Order)
                    .AddTo(Disposables);
            }
            VerticalAxis = verticalAxis;

            Spectrum = this.model.SpectrumSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            GraphTitle = this.model.Elements.ObserveProperty(m => m.GraphTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            HorizontalTitle = this.model.Elements.ObserveProperty(m => m.HorizontalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            VerticalTitle = this.model.Elements.ObserveProperty(m => m.VerticalTitle)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            HorizontalProperty = this.model.Elements.ObserveProperty(m => m.HorizontalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            VerticalProperty = this.model.Elements.ObserveProperty(m => m.VerticalProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
        }

        private readonly SurveyScanModel model;

        public ReadOnlyReactivePropertySlim<List<SpectrumPeakWrapper>> Spectrum { get; }

        public IAxisManager<double> HorizontalAxis { get; }

        public IAxisManager<double> VerticalAxis { get; }

        public ReadOnlyReactivePropertySlim<string> GraphTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalTitle { get; }

        public ReadOnlyReactivePropertySlim<string> HorizontalProperty { get; }

        public ReadOnlyReactivePropertySlim<string> VerticalProperty { get; }

        public ReadOnlyReactivePropertySlim<double> MaxIntensity { get; }
    }
}
