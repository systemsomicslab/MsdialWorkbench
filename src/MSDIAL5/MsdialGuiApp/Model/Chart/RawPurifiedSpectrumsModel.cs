using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Base;
using CompMs.Graphics.Core.Base;
using CompMs.MsdialCore.Export;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class RawPurifiedSpectrumsModel : DisposableModelBase {
        
        public RawPurifiedSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector,
            IObservable<IBrushMapper<SpectrumComment>> upperSpectrumBrush = null,
            IObservable<IBrushMapper<SpectrumComment>> lowerSpectrumBrush = null) {
            if (targetSource is null) {
                throw new ArgumentNullException(nameof(targetSource));
            }

            if (rawLoader is null) {
                throw new ArgumentNullException(nameof(rawLoader));
            }

            if (decLoader is null) {
                throw new ArgumentNullException(nameof(decLoader));
            }

            var upperSpectrum = targetSource
                .WithLatestFrom(Observable.Return(rawLoader),
                    (target, loader) => loader.LoadSpectrumAsObservable(target))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            var lowerSpectrum = targetSource
                .WithLatestFrom(Observable.Return(decLoader),
                    (target, loader) => loader.LoadSpectrumAsObservable(target))
                .Switch()
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            UpperSpectrumSource = upperSpectrum;
            LowerSpectrumSource = lowerSpectrum;

            UpperSpectrumLoaded = new[]
            {
                targetSource.ToConstant(false),
                upperSpectrum.Delay(TimeSpan.FromSeconds(.05d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.1d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            LowerSpectrumLoaded = new[]
            {
                targetSource.ToConstant(false),
                LowerSpectrumSource.Delay(TimeSpan.FromSeconds(.05d)).ToConstant(true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.1d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);

            HorizontalSelector = horizontalSelector ?? throw new ArgumentNullException(nameof(horizontalSelector));
            VerticalSelector = verticalSelector ?? throw new ArgumentNullException(nameof(verticalSelector));
            var upperEmpty = upperSpectrum.Where(spec => spec is null || !spec.Any());
            var upperAny = upperSpectrum.Where(spec => spec?.Any() ?? false);
            UpperVerticalRangeSource = upperAny
                .Select(spec => new Range(spec.Min(verticalSelector), spec.Max(verticalSelector)))
                .Merge(upperEmpty.ToConstant(new Range(0, 1)));
            var lowerEmpty = lowerSpectrum.Where(spec => spec is null || !spec.Any());
            var lowerAny = lowerSpectrum.Where(spec => spec?.Any() ?? false);
            LowerVerticalRangeSource = lowerAny
                .WithLatestFrom(Observable.Return(verticalSelector),
                    (spec, selector) => new Range(spec.Min(selector), spec.Max(selector)))
                .Merge(lowerEmpty.ToConstant(new Range(0, 1)));

            HorizontalRangeSource = new[]
            {
                upperSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
                lowerSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
            }.CombineLatest(specs => specs.SelectMany(spec => spec))
            .Where(spec => spec.Any())
            .WithLatestFrom(Observable.Return(horizontalSelector),
                (spec, selector) => new Range(spec.Min(horizontalSelector), spec.Max(horizontalSelector)));

            UpperSpectrumBrush = upperSpectrumBrush;
            LowerSpectrumBrush = lowerSpectrumBrush;


            // temporary
            GraphTitle = "Raw vs. Purified spectrum";
            HorizontalTitle = "m/z";
            VerticalTitle = "Absolute abundance";
            HorizontalProperty = nameof(SpectrumPeak.Mass);
            VerticalProperty = nameof(SpectrumPeak.Intensity);
            LabelProperty = nameof(SpectrumPeak.Mass);
            OrderingProperty = nameof(SpectrumPeak.Intensity);

            var rawMsSpectrum = targetSource.SelectSwitch(rawLoader.LoadMsSpectrumAsObservable).Publish();
            SingleSpectrumModel rawSpectrumModel = new SingleSpectrumModel(
                rawMsSpectrum,
                new PropertySelector<SpectrumPeak, double>(HorizontalProperty, horizontalSelector),
                new PropertySelector<SpectrumPeak, double>(VerticalProperty, verticalSelector),
                upperSpectrumBrush, nameof(SpectrumPeak.SpectrumComment),
                new GraphLabels("Raw EI spectrum", HorizontalTitle, VerticalTitle, LabelProperty, OrderingProperty),
                Observable.Return((ISpectraExporter)null),
                UpperSpectrumLoaded).AddTo(Disposables);
            Disposables.Add(rawMsSpectrum.Connect());
            RawSpectrumModel = rawSpectrumModel;

            var decMsSpectrum = targetSource.SelectSwitch(decLoader.LoadMsSpectrumAsObservable).Publish();
            SingleSpectrumModel decSpectrumModel = new SingleSpectrumModel(
                decMsSpectrum,
                new PropertySelector<SpectrumPeak, double>(HorizontalProperty, horizontalSelector),
                new PropertySelector<SpectrumPeak, double>(VerticalProperty, verticalSelector),
                lowerSpectrumBrush, nameof(SpectrumPeak.SpectrumComment),
                new GraphLabels("Deconvoluted EI spectrum", HorizontalTitle, VerticalTitle, LabelProperty, OrderingProperty),
                Observable.Return((ISpectraExporter)null),
                LowerSpectrumLoaded).AddTo(Disposables);
            Disposables.Add(decMsSpectrum.Connect());
            DecSpectrumModel = decSpectrumModel;
        }

        public SingleSpectrumModel RawSpectrumModel { get; }
        public SingleSpectrumModel DecSpectrumModel { get; }

        public IObservable<List<SpectrumPeak>> UpperSpectrumSource { get; }
        public IObservable<List<SpectrumPeak>> LowerSpectrumSource { get; }
        public ReadOnlyReactivePropertySlim<bool> UpperSpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> LowerSpectrumLoaded { get; }
        public string GraphTitle {
            get => _graphTitle;
            set => SetProperty(ref _graphTitle, value);
        }
        private string _graphTitle;

        public string HorizontalTitle {
            get => _horizontalTitle;
            set => SetProperty(ref _horizontalTitle, value);
        }
        private string _horizontalTitle;

        public string VerticalTitle {
            get => _verticalTitle;
            set => SetProperty(ref _verticalTitle, value);
        }
        private string _verticalTitle;

        public string HorizontalProperty {
            get => _horizontalProperty;
            set => SetProperty(ref _horizontalProperty, value);
        }
        private string _horizontalProperty;

        public string VerticalProperty {
            get => _verticalProperty;
            set => SetProperty(ref _verticalProperty, value);
        }
        private string _verticalProperty;

        public string LabelProperty {
            get => _labelProperty;
            set => SetProperty(ref _labelProperty, value);
        }
        private string _labelProperty;

        public string OrderingProperty {
            get => _orderingProperty;
            set => SetProperty(ref _orderingProperty, value);
        }
        private string _orderingProperty;

        public Func<SpectrumPeak, double> HorizontalSelector { get; }
        public Func<SpectrumPeak, double> VerticalSelector { get; }
        public IObservable<IBrushMapper<SpectrumComment>> UpperSpectrumBrush { get; }
        public IObservable<IBrushMapper<SpectrumComment>> LowerSpectrumBrush { get; }
        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> UpperVerticalRangeSource { get; }

        public IObservable<Range> LowerVerticalRangeSource { get; }
    }
}
