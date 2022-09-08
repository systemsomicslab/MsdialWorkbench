using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class ChromatogramPeakFeatureCollection
    {
        private readonly List<ChromatogramPeakFeature> _items;

        public ChromatogramPeakFeatureCollection(List<ChromatogramPeakFeature> items) {
            _items = items ?? throw new System.ArgumentNullException(nameof(items));
            Items = _items.AsReadOnly();
        }

        public ReadOnlyCollection<ChromatogramPeakFeature> Items { get; }

        public Task SerializeAsync(string outputFile, CancellationToken token = default) {
            return Task.Run(() => MsdialPeakSerializer.SaveChromatogramPeakFeatures(outputFile, _items), token);
        }

        public static async Task<ChromatogramPeakFeatureCollection> LoadAsync(string inputFile, CancellationToken token = default) {
            var peaks = await Task.Run(() => MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(inputFile), token).ConfigureAwait(false);
            return new ChromatogramPeakFeatureCollection(peaks);
        }
    }
}
