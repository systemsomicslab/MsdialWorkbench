using CompMs.App.Msdial.Common;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.AxisManager;
using CompMs.Graphics.AxisManager.Generic;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.Graphics.Design;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Chart
{
    public class MsSpectrumModel : DisposableModelBase {
        static MsSpectrumModel() {
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

        public MsSpectrumModel(
            IObservable<List<SpectrumPeak>> upperSpectrum,
            IObservable<List<SpectrumPeak>> lowerSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> upperSpectraExporter,
            IObservable<ISpectraExporter> lowerSpectraExporter,
            ReadOnlyReactivePropertySlim<bool> spectrumLoaded) {

            if (upperSpectrum is null) {
                throw new ArgumentNullException(nameof(upperSpectrum));
            }

            if (lowerSpectrum is null) {
                throw new ArgumentNullException(nameof(lowerSpectrum));
            }

            var verticalSelector = verticalPropertySelector.Selector;
            var upperEmpty = upperSpectrum.Where(spec => spec is null || !spec.Any());
            var upperAny = upperSpectrum.Where(spec => spec?.Any() ?? false);
            var upperVerticalRangeSource = upperAny
                .Select(spec => new Range(spec.Min(verticalSelector), spec.Max(verticalSelector)))
                .Merge(upperEmpty.Select(_ => new Range(0, 1)));
            UpperVerticalRangeSource = upperVerticalRangeSource;
            var upperContinuousVerticalAxis = upperVerticalRangeSource
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent);
            var upperLogVerticalAxis = upperVerticalRangeSource
                .Select(range => (range.Minimum.Value, range.Maximum.Value))
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d);
            UpperVerticalAxisItemCollection = new ObservableCollection<AxisItemModel>(new[]
            {
                new AxisItemModel(upperContinuousVerticalAxis, "Normal"),
                new AxisItemModel(upperLogVerticalAxis, "Log10"),
            });

            var lowerEmpty = lowerSpectrum.Where(spec => spec is null || !spec.Any());
            var lowerAny = lowerSpectrum.Where(spec => spec?.Any() ?? false);
            var lowerVerticalRangeSource = lowerAny
                .Select(spec => new Range(spec.Min(verticalSelector), spec.Max(verticalSelector)))
                .Merge(lowerEmpty.Select(_ => new Range(0, 1)));

            var horizontalSelector = horizontalPropertySelector.Selector;
            var horizontalRangeSource = new[]
            {
                upperSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
                lowerSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
            }.CombineLatest(specs => specs.SelectMany(spec => spec))
            .Where(spec => spec.Any())
            .Select(spec => new Range(spec.Min(horizontalSelector), spec.Max(horizontalSelector)));

            var horizontalAxis = horizontalRangeSource.ToReactiveContinuousAxisManager<double>(new ConstantMargin(40));
            var horizontalAxisObservable = Observable.Return(horizontalAxis);
            var upperVerticalAxisObservable = new ReactivePropertySlim<AxisItemModel>(UpperVerticalAxisItemCollection[0]).AddTo(Disposables);
            var lowerVerticalAxis = lowerVerticalRangeSource.ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent);
            var lowerVerticalAxisObservable = Observable.Return(lowerVerticalAxis);
            if (upperSpectrumBrush is null)
                upperSpectrumBrush = Observable.Return(GetBrush(Brushes.Blue));
            if (lowerSpectrumBrush is null)
                lowerSpectrumBrush = Observable.Return(GetBrush(Brushes.Red));
            if (string.IsNullOrEmpty(hueProperty))
                hueProperty = nameof(SpectrumPeak.SpectrumComment);

            HorizontalAxis = horizontalAxisObservable;
            UpperVerticalAxisItem = upperVerticalAxisObservable;
            UpperVerticalAxis = UpperVerticalAxisItem.Select(item => item.AxisManager);
            LowerVerticalAxis = lowerVerticalAxisObservable;
            VerticalPropertySelector = verticalPropertySelector;
            HorizontalPropertySelector = horizontalPropertySelector;
            GraphLabels = graphLabels;
            SpectrumLoaded = spectrumLoaded ?? new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
            ReferenceHasSpectrumInfomation = lowerSpectrum.Select(spectrum => spectrum.Any()).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            UpperSpectrumModel = new SingleSpectrumModel(
                upperSpectrum,
                horizontalAxisObservable, horizontalPropertySelector,
                UpperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty,
                graphLabels,
                upperSpectraExporter).AddTo(Disposables);
            LowerSpectrumModel = new SingleSpectrumModel(
                lowerSpectrum,
                horizontalAxisObservable, horizontalPropertySelector,
                LowerVerticalAxis, verticalPropertySelector,
                lowerSpectrumBrush, hueProperty,
                graphLabels,
                lowerSpectraExporter).AddTo(Disposables);
        }

        public SingleSpectrumModel UpperSpectrumModel { get; }
        public SingleSpectrumModel LowerSpectrumModel { get; }

        public IObservable<IAxisManager<double>> HorizontalAxis { get; }
        public IObservable<IAxisManager<double>> UpperVerticalAxis { get; }
        public IObservable<IAxisManager<double>> LowerVerticalAxis { get; }

        public ReactivePropertySlim<AxisItemModel> UpperVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel> UpperVerticalAxisItemCollection { get; }

        public GraphLabels GraphLabels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInfomation { get; }
        public PropertySelector<SpectrumPeak, double> HorizontalPropertySelector { get; }
        public PropertySelector<SpectrumPeak, double> VerticalPropertySelector { get; }

        public IObservable<bool> CanSaveUpperSpectrum => UpperSpectrumModel.CanSave;

        public void SaveUpperSpectrum(Stream stream) {
            UpperSpectrumModel.Save(stream);
        }

        public IObservable<bool> CanSaveLowerSpectrum => LowerSpectrumModel.CanSave;

        public void SaveLowerSpectrum(Stream stream) {
            LowerSpectrumModel.Save(stream);
        }

        public IObservable<Range> UpperVerticalRangeSource { get; }

        public static MsSpectrumModel Create<T, U, V>(
            IObservable<T> source,
            IMsSpectrumLoader<U> upperLoader,
            IMsSpectrumLoader<V> lowerLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector,
            string graphTitle,
            string horizontalTitle,
            string verticalTitle,
            string horizontalProperty,
            string verticalProperty,
            string labelProperty,
            string orderingProperty,
            string hueProperty,
            IObservable<IBrushMapper> upperBrush,
            IObservable<IBrushMapper> lowerBrush)
            where T: U, V {

            return new MsSpectrumModel(
                source.Select(src => Observable.FromAsync(token => upperLoader.LoadSpectrumAsync(src, token))).Switch(),
                source.Select(src => Observable.FromAsync(token => lowerLoader.LoadSpectrumAsync(src, token))).Switch(),
                new PropertySelector<SpectrumPeak, double>(horizontalProperty, horizontalSelector),
                new PropertySelector<SpectrumPeak, double>(verticalProperty, verticalSelector),
                new GraphLabels(graphTitle, horizontalTitle, verticalTitle, labelProperty, orderingProperty),
                hueProperty,
                upperBrush,
                lowerBrush,
                Observable.Return((ISpectraExporter)null),
                Observable.Return((ISpectraExporter)null),
                null);
        }

        public static IBrushMapper<SpectrumComment> GetBrush(Brush defaultBrush) {
            return new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, defaultBrush);
        }
    }
}
