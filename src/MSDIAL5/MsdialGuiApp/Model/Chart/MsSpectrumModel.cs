using CompMs.App.Msdial.Common;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
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
        private static readonly IReadOnlyDictionary<SpectrumComment, Brush> SpectrumBrushes;
        private readonly ReadOnlyReactivePropertySlim<MsSpectrum> _upperSpectrum;
        private readonly ReadOnlyReactivePropertySlim<MsSpectrum> _lowerSpectrum;
        private readonly ReadOnlyReactivePropertySlim<Ms2ScanMatching> _ms2ScanMatching;

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

        public MsSpectrumModel(
            IObservable<MsSpectrum> upperMsSpectrum,
            IObservable<MsSpectrum> lowerMsSpectrum,
            PropertySelector<SpectrumPeak, double> horizontalPropertySelector,
            PropertySelector<SpectrumPeak, double> verticalPropertySelector,
            GraphLabels graphLabels,
            string hueProperty,
            IObservable<IBrushMapper> upperSpectrumBrush,
            IObservable<IBrushMapper> lowerSpectrumBrush,
            IObservable<ISpectraExporter> upperSpectraExporter,
            IObservable<ISpectraExporter> lowerSpectraExporter,
            ReadOnlyReactivePropertySlim<bool> spectrumLoaded,
            IObservable<Ms2ScanMatching> ms2ScanMatching) {
            if (upperMsSpectrum is null) {
                throw new ArgumentNullException(nameof(upperMsSpectrum));
            }

            if (lowerMsSpectrum is null) {
                throw new ArgumentNullException(nameof(lowerMsSpectrum));
            }

            UpperSpectrumModel = new SingleSpectrumModel(upperMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, upperSpectraExporter).AddTo(Disposables);
            UpperVerticalAxisItem = UpperSpectrumModel.VerticalAxisItemSelector.ToReactivePropertySlimAsSynchronized(selector => selector.SelectedAxisItem).AddTo(Disposables);
            UpperVerticalAxis = UpperSpectrumModel.VerticalAxis;

            LowerSpectrumModel = new SingleSpectrumModel(lowerMsSpectrum, horizontalPropertySelector, verticalPropertySelector, lowerSpectrumBrush, hueProperty, graphLabels, lowerSpectraExporter).AddTo(Disposables);
            LowerVerticalAxisItem = LowerSpectrumModel.VerticalAxisItemSelector.ToReactivePropertySlimAsSynchronized(selector => selector.SelectedAxisItem).AddTo(Disposables);
            LowerVerticalAxis = LowerSpectrumModel.VerticalAxis;

            var productMsSpectrum = upperMsSpectrum.CombineLatest(lowerMsSpectrum, (upper, lower) => upper?.Product(lower, 0.05d)).Publish();
            var upperProductSpectrumModel = new SingleSpectrumModel(productMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, upperSpectraExporter).AddTo(Disposables);
            upperProductSpectrumModel.IsVisible.Value = false;
            upperProductSpectrumModel.LineThickness.Value = 3d;
            UpperProductSpectrumModel = upperProductSpectrumModel;
            Disposables.Add(productMsSpectrum.Connect());

            var differenceMsSpectrum = upperMsSpectrum.CombineLatest(lowerMsSpectrum, (upper, lower) => upper?.Difference(lower, 0.05d)).Publish();
            var upperDifferenceSpectrumModel = new SingleSpectrumModel(differenceMsSpectrum, horizontalPropertySelector, verticalPropertySelector, upperSpectrumBrush, hueProperty, graphLabels, upperSpectraExporter).AddTo(Disposables);
            upperDifferenceSpectrumModel.IsVisible.Value = false;
            upperDifferenceSpectrumModel.LineThickness.Value = 1d;
            UpperDifferenceSpectrumModel = upperDifferenceSpectrumModel;
            Disposables.Add(differenceMsSpectrum.Connect());

            UpperSpectraModel = new ReactiveCollection<SingleSpectrumModel>
            {
                UpperSpectrumModel,
                upperProductSpectrumModel,
                upperDifferenceSpectrumModel,
            };

            var horizontalRangeSource = new[]
            {
                upperMsSpectrum.Select(msSpectrum => msSpectrum?.GetSpectrumRange(spec => horizontalPropertySelector.Selector(spec)) ?? (0d, 1d)),
                lowerMsSpectrum.Select(msSpectrum => msSpectrum?.GetSpectrumRange(spec => horizontalPropertySelector.Selector(spec)) ?? (0d, 1d)),
            }.CombineLatest(xs => xs.Aggregate((x, y) => (Math.Min(x.Item1, y.Item1), Math.Max(x.Item2, x.Item2))));
            var horizontalAxis = horizontalRangeSource.ToReactiveContinuousAxisManager(new ConstantMargin(40)).AddTo(Disposables);
            HorizontalAxis = Observable.Return(horizontalAxis);
            VerticalPropertySelector = verticalPropertySelector;
            HorizontalPropertySelector = horizontalPropertySelector;
            GraphLabels = graphLabels;
            SpectrumLoaded = spectrumLoaded ?? new ReadOnlyReactivePropertySlim<bool>(Observable.Return(true));
            _ms2ScanMatching = ms2ScanMatching?.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            ReferenceHasSpectrumInfomation = LowerSpectrumModel.Spectrum.Select(spectrum => spectrum?.Any() ?? false).ToReadOnlyReactivePropertySlim().AddTo(Disposables);

            _upperSpectrum = upperMsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            _lowerSpectrum = lowerMsSpectrum.ToReadOnlyReactivePropertySlim().AddTo(Disposables);
            var canSaveMatchedSpectrum = new[]{
                ms2ScanMatching?.Select(s => s != null) ?? Observable.Return(false),
                _upperSpectrum.Select(s => s != null),
                _lowerSpectrum.Select(s => s != null),
            }.CombineLatestValuesAreAllTrue().Publish();
            Disposables.Add(canSaveMatchedSpectrum.Connect());
            CanSaveMatchedSpectrum = canSaveMatchedSpectrum;
        }

        public ReactiveCollection<SingleSpectrumModel> UpperSpectraModel { get; }
        public SingleSpectrumModel UpperSpectrumModel { get; }
        public SingleSpectrumModel UpperProductSpectrumModel { get; }
        public SingleSpectrumModel UpperDifferenceSpectrumModel { get; }
        public SingleSpectrumModel LowerSpectrumModel { get; }

        public IObservable<IAxisManager<double>> HorizontalAxis { get; }

        public IObservable<IAxisManager<double>> LowerVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> LowerVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel<double>> LowerVerticalAxisItemCollection => LowerSpectrumModel.VerticalAxisItemSelector.AxisItems;

        public IObservable<IAxisManager<double>> UpperVerticalAxis { get; }
        public ReactivePropertySlim<AxisItemModel<double>> UpperVerticalAxisItem { get; }
        public ObservableCollection<AxisItemModel<double>> UpperVerticalAxisItemCollection => UpperSpectrumModel.VerticalAxisItemSelector.AxisItems;

        public GraphLabels GraphLabels { get; }
        public ReadOnlyReactivePropertySlim<bool> SpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> ReferenceHasSpectrumInfomation { get; }
        public PropertySelector<SpectrumPeak, double> HorizontalPropertySelector { get; }
        public PropertySelector<SpectrumPeak, double> VerticalPropertySelector { get; }

        public IObservable<bool> CanSaveMatchedSpectrum { get; }

        public void SaveMatchedSpectrum(Stream stream) {
            if (_ms2ScanMatching?.Value is Ms2ScanMatching scorer) {
                var pairs = scorer.GetMatchedSpectrum(_upperSpectrum.Value.Spectrum, _lowerSpectrum.Value.Spectrum);
                pairs.Save(stream);
            }
        }

        public IObservable<bool> CanSaveUpperSpectrum => UpperSpectrumModel.CanSave;

        public void SaveUpperSpectrum(Stream stream) {
            UpperSpectrumModel.Save(stream);
        }

        public IObservable<bool> CanSaveLowerSpectrum => LowerSpectrumModel.CanSave;

        public void SaveLowerSpectrum(Stream stream) {
            LowerSpectrumModel.Save(stream);
        }

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

        public static IBrushMapper<SpectrumComment> GetBrush(Brush defaultBrush) {
            return new KeyBrushMapper<SpectrumComment>(SpectrumBrushes, defaultBrush);
        }
    }
}
