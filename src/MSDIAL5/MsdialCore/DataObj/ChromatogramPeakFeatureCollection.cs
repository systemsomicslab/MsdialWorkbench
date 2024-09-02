using CompMs.Common.Components;
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

        public void ClearMatchResultProperties() {
            foreach (var item in _items) {
                item.ClearMatchResultProperty();
            }
        }

        public void SetRawMs1Id(ExtractedIonChromatogram chromatogram) {
            foreach (var item in _items) {
                var peakFeature = item.PeakFeature;
                item.MS1RawSpectrumIdLeft = chromatogram.Id(peakFeature.ChromScanIdLeft);
                item.MS1RawSpectrumIdTop = chromatogram.Id(peakFeature.ChromScanIdTop);
                item.MS1RawSpectrumIdRight = chromatogram.Id(peakFeature.ChromScanIdRight);
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
            var ordered = _items;
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

        public ChromatogramPeakFeatureCollection Flatten()
        {

            var flatten = _items.SelectMany(item => item.DriftChromFeatures?.Any() ?? false ? item.DriftChromFeatures : [item]).ToList();
            return new ChromatogramPeakFeatureCollection(flatten);
        }

        public Task SerializeAsync(AnalysisFileBean file, CancellationToken token = default) {
            return SerializeAsync(file.PeakAreaBeanInformationFilePath, token);
        }

        public Task SerializeAsync(string outputFile, CancellationToken token = default) {
            return Task.Run(() => MsdialPeakSerializer.SaveChromatogramPeakFeatures(outputFile, _items), token);
        }

        public static async Task<ChromatogramPeakFeatureCollection> LoadAsync(string inputFile, CancellationToken token = default) {
            var peaks = await Task.Run(() => MsdialPeakSerializer.LoadChromatogramPeakFeatures(inputFile), token).ConfigureAwait(false);
            return new ChromatogramPeakFeatureCollection(peaks);
        }
    }
}
