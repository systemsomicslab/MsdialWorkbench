using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Loader;
using CompMs.App.Msdial.Utility;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using Reactive.Bindings;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class EicModel : DisposableModelBase
    {
        private EicModel(IReadOnlyReactiveProperty<PeakChromatogram?> chromatogram_, ReadOnlyReactivePropertySlim<bool> itemLoaded, string graphTitle, string horizontalTitle, string verticalTitle) {
            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;
            HorizontalProperty = nameof(PeakItem.Time);
            VerticalProperty = nameof(PeakItem.Intensity);

            Chromatogram = chromatogram_;
            ItemLoaded = itemLoaded;
            ChromRangeSource = chromatogram_.Select(chromatogram => chromatogram?.GetTimeRange() ?? new AxisRange(0d, 1d))
                .ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d))
                .AddTo(Disposables);
            AbundanceRangeSource = chromatogram_.Select(chromatogram => chromatogram?.GetAbundanceRange() ?? new AxisRange(0d, 1d))
                .ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d))
                .AddTo(Disposables);
            chromatogram_.Subscribe(chromatogram => GraphTitle = chromatogram?.Description ?? string.Empty).AddTo(Disposables);
        }

        public EicModel(IObservable<ChromatogramPeakFeatureModel?> targetSource, IChromatogramLoader<ChromatogramPeakFeatureModel> loader, string graphTitle, string horizontalTitle, string verticalTitle) {

            GraphTitle = graphTitle;
            HorizontalTitle = horizontalTitle;
            VerticalTitle = verticalTitle;

            HorizontalProperty = nameof(PeakItem.Time);
            VerticalProperty = nameof(PeakItem.Intensity);

            var sources = targetSource.DefaultIfNull(t => Observable.FromAsync(token => loader.LoadChromatogramAsync(t, token)), Observable.Never<PeakChromatogram>()).Switch();
            ReactiveProperty<PeakChromatogram> chromatogram_ = sources
                .ToReactiveProperty(loader.EmptyChromatogram)
                .AddTo(Disposables);
            Chromatogram = chromatogram_;

            ItemLoaded = new[]
                {
                    targetSource.ToConstant(false),
                    chromatogram_.Delay(TimeSpan.FromSeconds(.05d)).ToConstant(true),
                }.Merge()
                .Throttle(TimeSpan.FromSeconds(.1d))
                .ToReadOnlyReactivePropertySlim()
                .AddTo(Disposables);

            ChromRangeSource = Chromatogram.Select(chromatogram => chromatogram?.GetTimeRange() ?? new AxisRange(0d, 1d))
                .ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d))
                .AddTo(Disposables);
            AbundanceRangeSource = Chromatogram.Select(chromatogram => chromatogram?.GetAbundanceRange() ?? new AxisRange(0d, 1d))
                .ToReadOnlyReactivePropertySlim(new AxisRange(0d, 1d))
                .AddTo(Disposables);

            Chromatogram.Subscribe(chromatogram => GraphTitle = chromatogram?.Description ?? string.Empty).AddTo(Disposables);
        }

        public EicModel(IObservable<ChromatogramPeakFeatureModel?> targetSource, IChromatogramLoader<ChromatogramPeakFeatureModel> loader)
            : this(targetSource, loader, string.Empty, string.Empty, string.Empty) {

        }

        public ReadOnlyReactivePropertySlim<bool> ItemLoaded { get; }

        public IReadOnlyReactiveProperty<PeakChromatogram?> Chromatogram { get; }

        public IObservable<AxisRange> ChromRangeSource { get; }
        public IObservable<AxisRange> AbundanceRangeSource { get; }

        public string HorizontalTitle {
            get => horizontalTitle;
            set => SetProperty(ref horizontalTitle, value);
        }
        private string horizontalTitle = string.Empty;

        public string VerticalTitle {
            get => verticalTitle;
            set => SetProperty(ref verticalTitle, value);
        }
        private string verticalTitle = string.Empty;

        public string GraphTitle {
            get => graphTitle;
            set => SetProperty(ref graphTitle, value);
        }
        private string graphTitle = string.Empty;

        public string HorizontalProperty {
            get => horizontalProperty;
            set => SetProperty(ref horizontalProperty, value);
        }
        private string horizontalProperty = string.Empty;

        public string VerticalProperty {
            get => verticalProperty;
            set => SetProperty(ref verticalProperty, value);
        }
        private string verticalProperty = string.Empty;

        public static Builder CreateBuilder(string graphTitle, string horizontalTitle, string verticalTitle) {
            return new Builder(graphTitle, horizontalTitle, verticalTitle);
        }

        internal class Builder {
            private readonly string _graphTitle, _horizontalTitle, _verticalTitle;
            private readonly List<IConnectableObservable<(PeakChromatogram? Chromatogram, bool Loaded)>> _sources; 

            public Builder(string graphTitle, string horizontalTitle, string verticalTitle)
            {
                _graphTitle = graphTitle;
                _horizontalTitle = horizontalTitle;
                _verticalTitle = verticalTitle;
                _sources = new List<IConnectableObservable<(PeakChromatogram?, bool)>>();
            }

            public Builder Append<T>(IObservable<T> targetSource, IChromatogramLoader<T> loader) {
                var source = targetSource.SelectSwitch(t => Observable.FromAsync(token => loader.LoadChromatogramAsync(t, token)).Select(c => ((PeakChromatogram?)c, true)).StartWith((null, false))).Publish();
                _sources.Add(source);
                return this;
            }

            public EicModel Build() {
                var source = _sources.Merge();
                var chromatogram = source.Select(p => p.Chromatogram).ToReactiveProperty();
                var itemLoaded = source.Select(p => p.Loaded).ToReadOnlyReactivePropertySlim();
                var result = new EicModel(chromatogram, itemLoaded, _graphTitle, _horizontalTitle, _verticalTitle);
                for (int i = 0; i < _sources.Count; i++) {
                    result.Disposables.Add(_sources[i].Connect());
                }
                result.Disposables.Add(chromatogram);
                result.Disposables.Add(itemLoaded);
                _sources.Clear();
                return result;
            }
        }
    }
}
