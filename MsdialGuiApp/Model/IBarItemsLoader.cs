using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Mathematics.Basic;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public interface IBarItemsLoader
    {
        List<BarItem> LoadBarItems(AlignmentSpotPropertyModel target);
        Task<List<BarItem>> LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token);
        IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target);
    }

    abstract class BaseBarItemsLoader
    {
        public BaseBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) {
            this.id2cls = id2cls;
        }

        protected readonly IReadOnlyDictionary<int, string> id2cls;

        public List<BarItem> LoadBarItems(AlignmentSpotPropertyModel target) {
            return LoadBarItemsCore(target);
        }

        public Task<List<BarItem>> LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            if (target is null) {
                return Task.FromResult(new List<BarItem>());
            }
            return Task.Run(() => LoadBarItemsCore(target), token);
        }

        protected abstract List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target);

        protected Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> LoadBarItemsAsObserbleBySpot(System.Linq.Expressions.Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            var selector = expression.Compile();
            IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
                return target
                    .AlignedPeakPropertiesModel
                    .GroupBy(
                        peak => id2cls[peak.FileID],
                        (cls, peaks) =>
                            peaks.Select(peak => peak.ObserveProperty(expression))
                                .CombineLatest()
                                .Throttle(TimeSpan.FromMilliseconds(50))
                                .Select(_ => 
                                    new BarItem(
                                        cls,
                                        peaks.Average(selector),
                                        BasicMathematics.Stdev(peaks.Select(selector).ToArray()))))
                    .CombineLatest()
                    .Select(items => items.ToList());
            }
            return LoadBarItemsAsObservable;
        }
    }

    internal class HeightBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public HeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(p => p.PeakHeightTop);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakHeightTop), BasicMathematics.Stdev(pair.Select(peak => peak.PeakHeightTop).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }

    internal class AreaAboveBaseLineBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public AreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(p => p.PeakAreaAboveBaseline);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakAreaAboveBaseline), BasicMathematics.Stdev(pair.Select(peak => peak.PeakAreaAboveBaseline).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }

    internal class AreaAboveZeroBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public AreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(p => p.PeakAreaAboveZero);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakAreaAboveZero), BasicMathematics.Stdev(pair.Select(peak => peak.PeakAreaAboveZero).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }

    internal class NormalizedHeightBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public NormalizedHeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(peak => peak.NormalizedPeakHeight);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakHeight), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakHeight).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }

    internal class NormalizedAreaAboveBaseLineBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public NormalizedAreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(peak => peak.NormalizedPeakAreaAboveBaseline);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakAreaAboveBaseline), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakAreaAboveBaseline).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }

    internal class NormalizedAreaAboveZeroBarItemsLoader : BaseBarItemsLoader, IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadAsObservable;

        public NormalizedAreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {
            loadAsObservable = LoadBarItemsAsObserbleBySpot(peak => peak.NormalizedPeakAreaAboveBaseline);
        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakAreaAboveZero), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakAreaAboveZero).ToArray())))
                .ToList();
        }

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadAsObservable(target);
        }
    }
}
