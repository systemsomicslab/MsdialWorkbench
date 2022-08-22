using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Linq;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class EicModel : DisposableModelBase
    {
        public EicModel(IObservable<ChromatogramPeakFeatureModel> targetSource, IChromatogramLoader loader, string graphTitle, string horizontalTitle, string verticalTitle) {

            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            HorizontalProperty = nameof(PeakItem.Time);
            VerticalProperty = nameof(PeakItem.Intensity);

            var sources = targetSource.Select(t => Observable.FromAsync(token => loader.LoadChromatogramAsync(t, token))).Switch();
            var chromatogram_ = sources
                .ToReactiveProperty()
                .AddTo(Disposables);
            Chromatogram = chromatogram_;

            ItemLoaded = new[]
                {
                    targetSource.Select(_ => false),
                    chromatogram_.Delay(TimeSpan.FromSeconds(.05d)).Select(_ => true),
                }.Merge()
                .Throttle(TimeSpan.FromSeconds(.1d))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ChromRangeSource = Chromatogram.Select(chromatogram => chromatogram.GetTimeRange())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);
            AbundanceRangeSource = Chromatogram.Select(chromatogram => chromatogram.GetAbundanceRange())
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            Chromatogram.Subscribe(chromatogram => GraphTitle = chromatogram?.Description ?? string.Empty).AddTo(Disposables);
        }

        public EicModel(IObservable<ChromatogramPeakFeatureModel> targetSource, EicLoader loader)
            : this(targetSource, loader, string.Empty, string.Empty, string.Empty) {

        }

        public ReadOnlyReactivePropertySlim<bool> ItemLoaded { get; }

        public IReadOnlyReactiveProperty<Chromatogram> Chromatogram { get; }

        public IObservable<Range> ChromRangeSource { get; }
        public IObservable<Range> AbundanceRangeSource { get; }

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

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle;

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
    }
}
