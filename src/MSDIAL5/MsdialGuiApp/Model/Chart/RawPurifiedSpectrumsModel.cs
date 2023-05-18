using CompMs.App.Msdial.Model.Loader;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawPurifiedSpectrumsModel : DisposableModelBase {

        public RawPurifiedSpectrumsModel(SingleSpectrumModel rawSpectrumModel, SingleSpectrumModel decSpectrumModel)
        {
            RawSpectrumModel = rawSpectrumModel ?? throw new ArgumentNullException(nameof(rawSpectrumModel));
            DecSpectrumModel = decSpectrumModel ?? throw new ArgumentNullException(nameof(decSpectrumModel));

            HorizontalAxis = new[]
            {
                rawSpectrumModel.GetHorizontalRange(),
                decSpectrumModel.GetHorizontalRange(),
            }.CombineLatest(ranges => (ranges.Min(range => range.Item1), ranges.Max(range => range.Item2)))
            .ToReactiveContinuousAxisManager(new ConstantMargin(40))
            .AddTo(Disposables);
        }

        public SingleSpectrumModel RawSpectrumModel { get; }
        public SingleSpectrumModel DecSpectrumModel { get; }
        public ReactiveAxisManager<double> HorizontalAxis { get; }

        public static RawPurifiedSpectrumsModel Create<T>(
            IObservable<T> targetSource,
            IMsSpectrumLoader<T> rawLoader,
            IMsSpectrumLoader<T> decLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector,
            IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrush,
            IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrush) {
            if (targetSource is null) {
                throw new ArgumentNullException(nameof(targetSource));
            }

            if (rawLoader is null) {
                throw new ArgumentNullException(nameof(rawLoader));
            }

            if (decLoader is null) {
                throw new ArgumentNullException(nameof(decLoader));
            }

            // temporary
            var horizontalTitle = "m/z";
            var verticalTitle = "Absolute abundance";
            var horizontalProperty = nameof(SpectrumPeak.Mass);
            var verticalProperty = nameof(SpectrumPeak.Intensity);
            var labelProperty = nameof(SpectrumPeak.Mass);
            var orderingProperty = nameof(SpectrumPeak.Intensity);

            SingleSpectrumModel rawSpectrumModel = SingleSpectrumModel.Create(
                targetSource,
                rawLoader,
                new PropertySelector<SpectrumPeak, double>(horizontalProperty, horizontalSelector),
                new PropertySelector<SpectrumPeak, double>(verticalProperty, verticalSelector),
                upperSpectrumBrush, nameof(SpectrumPeak.SpectrumComment),
                new GraphLabels("Raw EI spectrum", horizontalTitle, verticalTitle, labelProperty, orderingProperty),
                Observable.Return((ISpectraExporter)null));

            SingleSpectrumModel decSpectrumModel = SingleSpectrumModel.Create(
                targetSource,
                decLoader,
                new PropertySelector<SpectrumPeak, double>(horizontalProperty, horizontalSelector),
                new PropertySelector<SpectrumPeak, double>(verticalProperty, verticalSelector),
                lowerSpectrumBrush, nameof(SpectrumPeak.SpectrumComment),
                new GraphLabels("Deconvoluted EI spectrum", horizontalTitle, verticalTitle, labelProperty, orderingProperty),
                Observable.Return((ISpectraExporter)null));

            var result = new RawPurifiedSpectrumsModel(rawSpectrumModel, decSpectrumModel);
            result.Disposables.Add(rawSpectrumModel);
            result.Disposables.Add(decSpectrumModel);
            return result;
        }
    }
}
