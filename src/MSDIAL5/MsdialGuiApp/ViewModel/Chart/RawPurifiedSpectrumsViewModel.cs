using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class RawPurifiedSpectrumsViewModel : ViewModelBase {
        public RawPurifiedSpectrumsViewModel(
           RawPurifiedSpectrumsModel model,
           IAxisManager<double> horizontalAxis = null,
           IAxisManager<double> upperVerticalAxis = null,
           IAxisManager<double> lowerVerticalAxis = null,
           IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrushSource = null,
           IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrushSource = null) {
            if (model is null) {
                throw new ArgumentNullException(nameof(model));
            }

            if (horizontalAxis is null) {
                horizontalAxis = model.HorizontalRangeSource
                    .ToReactiveContinuousAxisManager<double>(new ConstantMargin(40))
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (upperVerticalAxis is null) {
                upperVerticalAxis = model.UpperVerticalRangeSource
                    .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Standard)
                    .AddTo(Disposables);
            }
            UpperVerticalAxis = upperVerticalAxis;

            if (lowerVerticalAxis is null) {
                lowerVerticalAxis = model.LowerVerticalRangeSource
                    .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Standard)
                    .AddTo(Disposables);
            }
            LowerVerticalAxis = lowerVerticalAxis;

            UpperSpectrum = model.UpperSpectrumSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            LowerSpectrum = model.LowerSpectrumSource
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            UpperSpectrumLoaded = model.UpperSpectrumLoaded;
            LowerSpectrumLoaded = model.LowerSpectrumLoaded;

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

            LabelProperty = model.ObserveProperty(m => m.LabelProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            OrderingProperty = model.ObserveProperty(m => m.OrderingProperty)
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            if (upperSpectrumBrushSource is null) {
                upperSpectrumBrushSource = model.UpperSpectrumBrush ?? Observable.Return(new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, Brushes.Blue));
            }
            UpperSpectrumBrushSource = upperSpectrumBrushSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            if (lowerSpectrumBrushSource is null) {
                lowerSpectrumBrushSource = model.LowerSpectrumBrush ?? Observable.Return(new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, Brushes.Red));
            }
            LowerSpectrumBrushSource = lowerSpectrumBrushSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
        }

        static RawPurifiedSpectrumsViewModel() {
            SpectrumBrushes = Enum.GetValues(typeof(SpectrumComment))
                .Cast<SpectrumComment>()
                .Where(comment => comment != SpectrumComment.none)
                .Zip(ChartBrushes.SolidColorBrushList, (comment, brush) => (comment, brush))
                .ToDictionary(
                    kvp => kvp.comment,
                    kvp => (Brush)kvp.brush
                );
        }

        private static readonly IReadOnlyDictionary<SpectrumComment, Brush> SpectrumBrushes;


        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> UpperSpectrum { get; }

        public ReadOnlyReactivePropertySlim<List<SpectrumPeak>> LowerSpectrum { get; }
        public ReadOnlyReactivePropertySlim<bool> UpperSpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> LowerSpectrumLoaded { get; }
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

        public ReadOnlyReactivePropertySlim<IBrushMapper<SpectrumComment>> UpperSpectrumBrushSource { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<SpectrumComment>> LowerSpectrumBrushSource { get; }

        public List<object> Items {
            get {
                return new List<object>
                {
                    new
                    {
                        Spectrum = UpperSpectrum,
                        IsLoaded = UpperSpectrumLoaded,
                        VerticalAxis = UpperVerticalAxis,
                        HorizontalProperty = HorizontalProperty,
                        VerticalProperty = VerticalProperty,
                        LabelProperty = LabelProperty,
                        OrderingProperty = OrderingProperty,
                        BrushSource = UpperSpectrumBrushSource,
                    },
                    new
                    {
                        Spectrum = LowerSpectrum,
                        IsLoaded = LowerSpectrumLoaded,
                        VerticalAxis = LowerVerticalAxis,
                        HorizontalProperty = HorizontalProperty,
                        VerticalProperty = VerticalProperty,
                        LabelProperty = LabelProperty,
                        OrderingProperty = OrderingProperty,
                        BrushSource = LowerSpectrumBrushSource,
                    }
                };
            }
        }
    }
}
