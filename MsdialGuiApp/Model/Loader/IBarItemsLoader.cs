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
        BarItemCollection LoadBarItemsAsObservable(AlignmentSpotPropertyModel target);
    }

    internal class BarItemsLoader : IBarItemsLoader
    {
        private readonly Func<AlignmentSpotPropertyModel, BarItemCollection> _loadBarItemCollection;

        public BarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2clsObservable, Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            _loadBarItemCollection = LoadBarItemCollectionBySpot(id2clsObservable, expression);
        }

        public BarItemCollection LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return _loadBarItemCollection(target);
        }

        protected static Func<AlignmentSpotPropertyModel, BarItemCollection> LoadBarItemCollectionBySpot(
            IObservable<IReadOnlyDictionary<int, string>> id2clsObservable,
            Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            var selector = expression.Compile();
            BarItemCollection LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
                var loading = target.AlignedPeakPropertiesModelAsObservable.Select(props => props is null);
                var propertiesAsObserable = new[]
                {
                    target.AlignedPeakPropertiesModelAsObservable.Where(props => props is null).Select(_ => Enumerable.Empty<AlignmentChromPeakFeatureModel>()),
                    target.AlignedPeakPropertiesModelAsObservable.Where(props => !(props is null)),
                }.Merge();

                var barItems = Observable.CombineLatest(
                    id2clsObservable,
                    propertiesAsObserable,
                    (id2cls, properties) =>
                        properties.GroupBy(
                            peak => id2cls[peak.FileID],
                            (cls, peaks) =>
                                peaks.Select(peak => peak.ObserveProperty(expression))
                                    .CombineLatest()
                                    .Select(_ =>
                                        new BarItem(
                                            cls,
                                            peaks.Average(selector),
                                            BasicMathematics.Stdev(peaks.Select(selector).ToArray()))))
                        .CombineLatest().StartWith(new List<BarItem>(0)))
                    .Switch()
                    .Select(items => items.ToList());
                return new BarItemCollection(barItems, loading);
            }

            return LoadBarItemsAsObservable;
        }
    }

    internal sealed class HeightBarItemsLoader : BarItemsLoader
    {
        public HeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakHeightTop) {

        }

        public HeightBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, p => p.PeakHeightTop) {

        }
    }

    internal sealed class AreaAboveBaseLineBarItemsLoader : BarItemsLoader
    {
        public AreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakAreaAboveBaseline) {

        }

        public AreaAboveBaseLineBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, p => p.PeakAreaAboveBaseline) {

        }
    }

    internal sealed class AreaAboveZeroBarItemsLoader : BarItemsLoader
    {
        public AreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), p => p.PeakAreaAboveZero) {

        }

        public AreaAboveZeroBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, p => p.PeakAreaAboveZero) {

        }
    }

    internal sealed class NormalizedHeightBarItemsLoader : BarItemsLoader
    {
        public NormalizedHeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakHeight) {

        }

        public NormalizedHeightBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, peak => peak.NormalizedPeakHeight) {

        }
    }

    internal sealed class NormalizedAreaAboveBaseLineBarItemsLoader : BarItemsLoader
    {
        public NormalizedAreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakAreaAboveBaseline) {

        }

        public NormalizedAreaAboveBaseLineBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }

    internal sealed class NormalizedAreaAboveZeroBarItemsLoader : BarItemsLoader
    {
        public NormalizedAreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(Observable.Return(id2cls), peak => peak.NormalizedPeakAreaAboveBaseline) {

        }

        public NormalizedAreaAboveZeroBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls) : base(id2cls, peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }
}
