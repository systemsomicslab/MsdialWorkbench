using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Windows.Media;
using System.Linq;

namespace CompMs.App.Msdial.ViewModel.Chart
{
    class MsSpectrumViewModel : ViewModelBase
    {
        static MsSpectrumViewModel() {
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

        public MsSpectrumViewModel(
            MsSpectrumModel model,
            IAxisManager<double> horizontalAxis = null,
            IAxisManager<double> upperVerticalAxis = null,
            IAxisManager<double> lowerVerticalAxis = null,
            IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrushSource = null,
            IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrushSource = null) {

            this.model = model ?? throw new ArgumentNullException(nameof(model));

            if (horizontalAxis is null) {
                horizontalAxis = this.model.HorizontalRangeSource
                    .ToReactiveAxisManager<double>(new ConstantMargin(40))
                    .AddTo(Disposables);
            }
            HorizontalAxis = horizontalAxis;

            if (upperVerticalAxis is null) {
                upperVerticalAxis = this.model.UpperVerticalRangeSource
                    .ToReactiveAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent)
                    .AddTo(Disposables);
            }
            UpperVerticalAxis = upperVerticalAxis;

            if (lowerVerticalAxis is null) {
                lowerVerticalAxis = this.model.LowerVerticalRangeSource
                    .ToReactiveAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent)
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
           
            if (upperSpectrumBrushSource is null) {
                upperSpectrumBrushSource = Observable.Return(new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, Brushes.Blue));
            }
            UpperSpectrumBrushSource = upperSpectrumBrushSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            if (lowerSpectrumBrushSource is null) {
                lowerSpectrumBrushSource = Observable.Return(new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, Brushes.Red));
            }
            LowerSpectrumBrushSource = lowerSpectrumBrushSource.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
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

        public ReadOnlyReactivePropertySlim<IBrushMapper<SpectrumComment>> UpperSpectrumBrushSource { get; }
        public ReadOnlyReactivePropertySlim<IBrushMapper<SpectrumComment>> LowerSpectrumBrushSource { get; }
    }
}
