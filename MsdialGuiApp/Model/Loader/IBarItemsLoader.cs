using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Mathematics.Basic;
using Reactive.Bindings.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reactive.Linq;

namespace CompMs.App.Msdial.Model.Loader
{
    public interface IBarItemsLoader
    {
        IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target);
    }

    class BarItemsLoader : IBarItemsLoader
    {
        public BarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2clsObservable, Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            loadBarItemsAsObservable = LoadBarItemsAsObserbleBySpot(id2clsObservable, expression);
        }

        private readonly Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> loadBarItemsAsObservable;

        public IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return loadBarItemsAsObservable(target);
        }

        protected static Func<AlignmentSpotPropertyModel, IObservable<List<BarItem>>> LoadBarItemsAsObserbleBySpot(
            IObservable<IReadOnlyDictionary<int, string>> id2clsObservable,
            Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            var selector = expression.Compile();
            IObservable<List<BarItem>> LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
                return id2clsObservable
                    .CombineLatest(target.AlignedPeakPropertiesModelAsObservable.Where(props => props != null),
                        (id2cls, properties) => properties
                            .GroupBy(peak =>
                                id2cls[peak.FileID],
                                (cls, peaks) => peaks
                                    .Select(peak => peak.ObserveProperty(expression))
                                    .CombineLatest()
                                    .Throttle(TimeSpan.FromMilliseconds(50))
                                    .Select(_ =>
                                        new BarItem(
                                            cls,
                                            peaks.Average(selector),
                                            BasicMathematics.Stdev(peaks.Select(selector).ToArray()))))
                        .CombineLatest())
                    .Switch()
                    .Select(items => items.ToList());
            }
            return LoadBarItemsAsObservable;
        }
    }

    internal sealed class HeightBarItemsLoader : BarItemsLoader
    {
        public HeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakHeightTop) {

        }
    }

    internal sealed class AreaAboveBaseLineBarItemsLoader : BarItemsLoader, IBarItemsLoader
    {
        public AreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakAreaAboveBaseline) {

        }
    }

    internal sealed class AreaAboveZeroBarItemsLoader : BarItemsLoader
    {
        public AreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakAreaAboveZero) {

        }
    }

    internal sealed class NormalizedHeightBarItemsLoader : BarItemsLoader
    {
        public NormalizedHeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakHeight) {

        }
    }

    internal sealed class NormalizedAreaAboveBaseLineBarItemsLoader : BarItemsLoader, IBarItemsLoader
    {
        public NormalizedAreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }

    internal sealed class NormalizedAreaAboveZeroBarItemsLoader : BarItemsLoader, IBarItemsLoader
    {
        public NormalizedAreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }
}
