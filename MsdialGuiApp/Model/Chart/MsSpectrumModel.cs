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

            var upperMsSpectrum = upperSpectrum.Select(spectrum => new MsSpectrum(spectrum));
            var upperVerticalRangeProperty = upperMsSpectrum
                .Select(msSpectrum => msSpectrum.GetSpectrumRange(verticalPropertySelector.Selector));
            UpperVerticalRangeSource = upperVerticalRangeProperty;
            var upperContinuousVerticalAxis = upperVerticalRangeProperty
                .ToReactiveContinuousAxisManager<double>(new ConstantMargin(0, 30), new Range(0d, 0d), LabelType.Percent)
                .AddTo(Disposables);
            var upperLogVerticalAxis = upperVerticalRangeProperty
                .Select(range => (range.Minimum.Value, range.Maximum.Value))
                .ToReactiveLogScaleAxisManager(new ConstantMargin(0, 30), 1d, 1d)
                .AddTo(Disposables);
            UpperVerticalAxisItemCollection = new ObservableCollection<AxisItemModel>(new[]
            {
                new AxisItemModel(upperContinuousVerticalAxis, "Normal"),
                new AxisItemModel(upperLogVerticalAxis, "Log10"),
            });

            var lowerMsSpectrum = lowerSpectrum.Select(spectrum => new MsSpectrum(spectrum));
            var lowerVerticalRangeSource = lowerMsSpectrum
                .Select(msSpectrum => msSpectrum.GetSpectrumRange(verticalPropertySelector.Selector));

            var horizontalRangeSource = new[]
            {
                upperMsSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(horizontalPropertySelector.Selector)),
                lowerMsSpectrum.Select(msSpectrum => msSpectrum.GetSpectrumRange(horizontalPropertySelector.Selector)),
            }.CombineLatest(xs => xs.Aggregate((x, y) => x.Union(y)));

            var horizontalAxis = horizontalRangeSource.ToReactiveContinuousAxisManager<double>(new ConstantMargin(40)).AddTo(Disposables);
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
            ReferenceHasSpectrumInfomation = lowerSpectrum.Select(spectrum => spectrum?.Any() ?? false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);
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

            var productMsSpectrum = upperMsSpectrum.CombineLatest(lowerMsSpectrum, (upper, lower) => upper.Product(lower, 0.05d));
            var upperProductSpectrumModel = new SingleSpectrumModel(
                productMsSpectrum.Select(msSpectrum => msSpectrum.Spectrum),
                horizontalAxisObservable, horizontalPropertySelector,
                UpperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty,
                graphLabels,
                upperSpectraExporter).AddTo(Disposables);
            upperProductSpectrumModel.IsVisible.Value = false;
            upperProductSpectrumModel.LineThickness.Value = 3d;
            UpperProductSpectrumModel = upperProductSpectrumModel;
            var differenceMsSpectrum = upperMsSpectrum.CombineLatest(lowerMsSpectrum, (upper, lower) => upper.Difference(lower, 0.05d));
            var upperDifferenceSpectrumModel = new SingleSpectrumModel(
                differenceMsSpectrum.Select(msSpectrum => msSpectrum.Spectrum),
                horizontalAxisObservable, horizontalPropertySelector,
                UpperVerticalAxis, verticalPropertySelector,
                upperSpectrumBrush, hueProperty,
                graphLabels,
                upperSpectraExporter).AddTo(Disposables);
            upperDifferenceSpectrumModel.IsVisible.Value = false;
            upperDifferenceSpectrumModel.LineThickness.Value = 1d;
            UpperDifferenceSpectrumModel = upperDifferenceSpectrumModel;
            UpperSpectraModel = new ReactiveCollection<SingleSpectrumModel>
            {
                UpperSpectrumModel,
                upperProductSpectrumModel,
                upperDifferenceSpectrumModel,
            };
        }

        public ReactiveCollection<SingleSpectrumModel> UpperSpectraModel { get; }
        public SingleSpectrumModel UpperSpectrumModel { get; }
        public SingleSpectrumModel UpperProductSpectrumModel { get; }
        public SingleSpectrumModel UpperDifferenceSpectrumModel { get; }

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

        public void SwitchViewToAllSpectrum() {
            UpperSpectrumModel.IsVisible.Value = true;
            UpperDifferenceSpectrumModel.IsVisible.Value = false;
            UpperProductSpectrumModel.IsVisible.Value = false;
        }

        public void SwitchViewToCompareSpectrum() {
            UpperSpectrumModel.IsVisible.Value = false;
            UpperDifferenceSpectrumModel.IsVisible.Value = true;
            UpperProductSpectrumModel.IsVisible.Value = true;
        }

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
                source.Select(src => upperLoader.LoadSpectrumAsObservable(src)).Switch(),
                source.Select(src => lowerLoader.LoadSpectrumAsObservable(src)).Switch(),
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
