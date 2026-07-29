using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class IntensityImageModel : BindableBase
    {
        private readonly RawIntensityOnPixelsLoader _intensitiesLoader;
        internal readonly int _peakIndex;

        public IntensityImageModel(ChromatogramPeakFeatureModel peak, RawIntensityOnPixelsLoader intensitiesLoader, int peakIndex) {
            Peak = peak;
            _intensitiesLoader = intensitiesLoader;
            _peakIndex = peakIndex;
            Mz = new MzValue(peak.Mass);
            Drift = peak.Drift;
        }

        public ChromatogramPeakFeatureModel Peak { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }

        public async Task SaveAsync(Stream stream, bool skipUnknownPeaks = true, System.Threading.CancellationToken token = default) {
            if (skipUnknownPeaks && string.IsNullOrEmpty(Peak.Name)) {
                return;
            }
            var pixels = await _intensitiesLoader.LoadAsync(_peakIndex, token).ConfigureAwait(false);
            var row = string.Format("{0},{1},{2},{3},", Peak.MasterPeakID, Peak.Name, Mz.Value, Drift.Value) + string.Join(",", pixels.PixelPeakFeaturesList[0].IntensityArray);
            var encoded = UTF8Encoding.Default.GetBytes(row + "\n");
            await stream.WriteAsync(encoded, 0, encoded.Length, token).ConfigureAwait(false);
        }
    }
}
