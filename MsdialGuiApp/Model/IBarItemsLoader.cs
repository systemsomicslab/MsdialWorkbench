using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Mathematics.Basic;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model
{
    public interface IBarItemsLoader
    {
        List<BarItem> LoadBarItems(AlignmentSpotPropertyModel target);
        Task<List<BarItem>> LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token);
    }

    abstract class BaseBarItemsLoader : IBarItemsLoader
    {
        public BaseBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) {
            this.id2cls = id2cls;
        }

        protected readonly IReadOnlyDictionary<int, string> id2cls;

        public List<BarItem> LoadBarItems(AlignmentSpotPropertyModel target) {
            return LoadBarItemsCore(target);
        }

        public async Task<List<BarItem>> LoadBarItemsAsync(AlignmentSpotPropertyModel target, CancellationToken token) {
            if (target == null) {
                return new List<BarItem>();
            }
            return await Task.Run(() => LoadBarItemsCore(target), token).ConfigureAwait(false);
        }

        protected abstract List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target);
    }

    internal class HeightBarItemsLoader : BaseBarItemsLoader
    {
        public HeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            // TODO: Implement other features (PeakHeight, PeakArea, Normalized PeakHeight, Normalized PeakArea)
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakHeightTop), BasicMathematics.Stdev(pair.Select(peak => peak.PeakHeightTop).ToArray())))
                .ToList();
        }
    }

    internal class AreaAboveBaseLineBarItemsLoader : BaseBarItemsLoader
    {
        public AreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakAreaAboveBaseline), BasicMathematics.Stdev(pair.Select(peak => peak.PeakAreaAboveBaseline).ToArray())))
                .ToList();
        }
    }

    internal class AreaAboveZeroBarItemsLoader : BaseBarItemsLoader
    {
        public AreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.PeakAreaAboveZero), BasicMathematics.Stdev(pair.Select(peak => peak.PeakAreaAboveZero).ToArray())))
                .ToList();
        }
    }

    internal class NormalizedHeightBarItemsLoader : BaseBarItemsLoader
    {
        public NormalizedHeightBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakHeight), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakHeight).ToArray())))
                .ToList();
        }
    }

    internal class NormalizedAreaAboveBaseLineBarItemsLoader : BaseBarItemsLoader
    {
        public NormalizedAreaAboveBaseLineBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakAreaAboveBaseline), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakAreaAboveBaseline).ToArray())))
                .ToList();
        }
    }

    internal class NormalizedAreaAboveZeroBarItemsLoader : BaseBarItemsLoader
    {
        public NormalizedAreaAboveZeroBarItemsLoader(IReadOnlyDictionary<int, string> id2cls) : base(id2cls) {

        }

        protected override List<BarItem> LoadBarItemsCore(AlignmentSpotPropertyModel target) {
            return target.AlignedPeakProperties
                .GroupBy(peak => id2cls[peak.FileID])
                .Select(pair => new BarItem(pair.Key, pair.Average(peak => peak.NormalizedPeakAreaAboveZero), BasicMathematics.Stdev(pair.Select(peak => peak.NormalizedPeakAreaAboveZero).ToArray())))
                .ToList();
        }
    }
}
