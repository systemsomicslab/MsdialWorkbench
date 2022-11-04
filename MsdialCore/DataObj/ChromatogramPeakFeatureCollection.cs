using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
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

        public void SetRawMs1Id(IReadOnlyList<ValuePeak> peaklist) {
            foreach (var item in _items) {
                var peakFeature = item.PeakFeature;
                item.MS1RawSpectrumIdLeft = peaklist[peakFeature.ChromScanIdLeft].Id;
                item.MS1RawSpectrumIdTop = peaklist[peakFeature.ChromScanIdTop].Id;
                item.MS1RawSpectrumIdRight = peaklist[peakFeature.ChromScanIdRight].Id;
            }
        }

        public void ResetAmplitudeScore() {
            if (_items.Count > 1) {
                var rank = 0;
                var base_ = (float)(_items.Count - 1);
                var ordered = _items.OrderBy(item => item.PeakFeature.PeakHeightTop);
                foreach (var item in ordered) {
                    item.PeakShape.AmplitudeScoreValue = rank / base_;
                    item.PeakShape.AmplitudeOrderValue = rank++;
                }
            }
        }

        public void ResetPeakID() {
            var ordered = _items.OrderBy(item => item.ChromXs.Value).ThenBy(item => item.PeakFeature.Mass);
            var masterPeakId = 0;
            var peakId = 0;
            foreach (var peakSpot in ordered) {
                peakSpot.PeakID = peakId++;
                peakSpot.MasterPeakID = masterPeakId++;
                if (peakSpot.DriftChromFeatures != null) {
                    for (int j = 0; j < peakSpot.DriftChromFeatures.Count; j++) {
                        var driftSpot = peakSpot.DriftChromFeatures[j];
                        driftSpot.MasterPeakID = masterPeakId++;
                        driftSpot.PeakID = j;
                        driftSpot.ParentPeakID = peakSpot.PeakID;
                    }
                }
            }
        }

        public Task SerializeAsync(AnalysisFileBean file, CancellationToken token = default) {
            return SerializeAsync(file.PeakAreaBeanInformationFilePath, token);
        }

        public Task SerializeAsync(string outputFile, CancellationToken token = default) {
            return Task.Run(() => MsdialPeakSerializer.SaveChromatogramPeakFeatures(outputFile, _items), token);
        }

        public static async Task<ChromatogramPeakFeatureCollection> LoadAsync(string inputFile, CancellationToken token = default) {
            var peaks = await Task.Run(() => MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(inputFile), token).ConfigureAwait(false);
            return new ChromatogramPeakFeatureCollection(peaks);
        }
    }
}
