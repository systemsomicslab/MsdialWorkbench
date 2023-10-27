using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Utility;
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

        public BarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2clsObservable, AnalysisFileBeanModelCollection files, Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            _loadBarItemCollection = LoadBarItemCollectionBySpot(id2clsObservable, files, expression);
        }

        public BarItemCollection LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
            return _loadBarItemCollection(target);
        }

        private static Func<AlignmentSpotPropertyModel, BarItemCollection> LoadBarItemCollectionBySpot(
            IObservable<IReadOnlyDictionary<int, string>> id2clsObservable,
            AnalysisFileBeanModelCollection files,
            Expression<Func<AlignmentChromPeakFeatureModel, double>> expression) {
            var selector = expression.Compile();
            var includes = files.AnalysisFiles.Select(file => file.ObserveProperty(f => f.AnalysisFileIncluded)).CombineLatest();
            BarItemCollection LoadBarItemsAsObservable(AlignmentSpotPropertyModel target) {
                var loading = target.AlignedPeakPropertiesModelProperty.Select(props => props is null);
                var propertiesAsObserable = new[]
                {
                    target.AlignedPeakPropertiesModelProperty.TakeNull().Select(_ => Enumerable.Empty<AlignmentChromPeakFeatureModel>()),
                    target.AlignedPeakPropertiesModelProperty.SkipNull().CombineLatest(includes, (props, includes_) => props.Zip(includes_, (prop, include) => (prop, include)).Where(p => p.include).Select(p => p.prop)),
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
        public HeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls, AnalysisFileBeanModelCollection files) : base(Observable.Return(id2cls), files, p => p.PeakHeightTop) {

        }

        public HeightBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, p => p.PeakHeightTop) {

        }
    }

    internal sealed class AreaAboveBaseLineBarItemsLoader : BarItemsLoader
    {
        public AreaAboveBaseLineBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, p => p.PeakAreaAboveBaseline) {

        }
    }

    internal sealed class AreaAboveZeroBarItemsLoader : BarItemsLoader
    {
        public AreaAboveZeroBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, p => p.PeakAreaAboveZero) {

        }
    }

    internal sealed class NormalizedHeightBarItemsLoader : BarItemsLoader
    {
        public NormalizedHeightBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, peak => peak.NormalizedPeakHeight) {

        }
    }

    internal sealed class NormalizedAreaAboveBaseLineBarItemsLoader : BarItemsLoader
    {
        public NormalizedAreaAboveBaseLineBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }

    internal sealed class NormalizedAreaAboveZeroBarItemsLoader : BarItemsLoader
    {
        public NormalizedAreaAboveZeroBarItemsLoader(IObservable<IReadOnlyDictionary<int, string>> id2cls, AnalysisFileBeanModelCollection files) : base(id2cls, files, peak => peak.NormalizedPeakAreaAboveBaseline) {

        }
    }
}
