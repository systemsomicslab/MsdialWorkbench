using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    class RawPurifiedSpectrumsModel : DisposableModelBase {
        
        public RawPurifiedSpectrumsModel(
            IObservable<ChromatogramPeakFeatureModel> targetSource,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> rawLoader,
            IMsSpectrumLoader<ChromatogramPeakFeatureModel> decLoader,
            Func<SpectrumPeak, double> horizontalSelector,
            Func<SpectrumPeak, double> verticalSelector) {
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
                targetSource.Select(_ => false),
                UpperSpectrumSource.Delay(TimeSpan.FromSeconds(.05d)).Select(_ => true),
            }.Merge()
            .Throttle(TimeSpan.FromSeconds(.1d))
            .ToReadOnlyReactivePropertySlim()
            .AddTo(Disposables);
            LowerSpectrumLoaded = new[]
            {
                targetSource.Select(_ => false),
                LowerSpectrumSource.Delay(TimeSpan.FromSeconds(.05d)).Select(_ => true),
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
                .Merge(upperEmpty.Select(_ => new Range(0, 1)));
            var lowerEmpty = lowerSpectrum.Where(spec => spec is null || !spec.Any());
            var lowerAny = lowerSpectrum.Where(spec => spec?.Any() ?? false);
            LowerVerticalRangeSource = lowerAny
                .WithLatestFrom(Observable.Return(verticalSelector),
                    (spec, selector) => new Range(spec.Min(selector), spec.Max(selector)))
                .Merge(lowerEmpty.Select(_ => new Range(0, 1)));

            HorizontalRangeSource = new[]
            {
                upperSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
                lowerSpectrum.Where(spec => !(spec is null)).StartWith(new List<SpectrumPeak>(0)),
            }.CombineLatest(specs => specs.SelectMany(spec => spec))
            .Where(spec => spec.Any())
            .WithLatestFrom(Observable.Return(horizontalSelector),
                (spec, selector) => new Range(spec.Min(horizontalSelector), spec.Max(horizontalSelector)));
        }

        public IObservable<List<SpectrumPeak>> UpperSpectrumSource { get; }

        public IObservable<List<SpectrumPeak>> LowerSpectrumSource { get; }
        public ReadOnlyReactivePropertySlim<bool> UpperSpectrumLoaded { get; }
        public ReadOnlyReactivePropertySlim<bool> LowerSpectrumLoaded { get; }
        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

        public string HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string horizontalTitle;

        public string VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string verticalTitle;

        public string HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string horizontalProperty;

        public string VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string verticalProperty;

        public string LabelProperty {
            get => labelProperty;
            set => SetProperty(ref labelProperty, value);
        }
        private string labelProperty;

        public string OrderingProperty {
            get => orderingProperty;
            set => SetProperty(ref orderingProperty, value);
        }
        private string orderingProperty;

        public Func<SpectrumPeak, double> HorizontalSelector { get; }
        public Func<SpectrumPeak, double> VerticalSelector { get; }

        public IObservable<Range> HorizontalRangeSource { get; }

        public IObservable<Range> UpperVerticalRangeSource { get; }

        public IObservable<Range> LowerVerticalRangeSource { get; }
    }
}
