using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class IntensityImageModel : BindableBase
    {
        private readonly MaldiFrames _frameInfos;
        private readonly RawIntensityOnPixelsLoader _intensitiesLoader;
        private readonly int _peakIndex;

        public IntensityImageModel(MaldiFrames frameInfos, ChromatogramPeakFeatureModel peak, MaldiFrameLaserInfo laserInfo, RawIntensityOnPixelsLoader intensitiesLoader, int peakIndex) {
            _frameInfos = frameInfos;
            Peak = peak;
            _intensitiesLoader = intensitiesLoader;
            _peakIndex = peakIndex;
            Mz = new MzValue(peak.Mass);
            Drift = peak.Drift;
        }

        public ChromatogramPeakFeatureModel Peak { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public BitmapImageModel BitmapImageModel {
            get {
                var xmin = BitmapImageModel.WithMarginToPoint(_frameInfos.XIndexPosMin);
                var ymin = BitmapImageModel.WithMarginToPoint(_frameInfos.YIndexPosMin);
                var width = BitmapImageModel.WithMarginToLength(_frameInfos.XIndexWidth);
                var height = BitmapImageModel.WithMarginToLength(_frameInfos.YIndexHeight);
                var pf = PixelFormats.Indexed8;
                var stride = (pf.BitsPerPixel + 7) / 8;

                var factory = () => {
                    var image = new byte[width * stride * height];
                    var pixels = _intensitiesLoader.Load(_peakIndex);
                    var features = pixels.PixelPeakFeaturesList[0];
                    var sorted = features.IntensityArray.OrderBy(x => x).ToArray();
                    var length = sorted.Length;
                    var zmin = sorted.DefaultIfEmpty(0d).First();
                    var zmax = sorted.DefaultIfEmpty(255).ElementAt((int)(length * .99));
                    if (zmin == zmax) {
                        zmax = zmin + 1;
                    }
                    foreach (var (intensity, info) in features.IntensityArray.Zip(_frameInfos.Infos, (x, y) => (x, y))) {
                        image[width * stride * (info.YIndexPos - ymin) + (info.XIndexPos - xmin) * stride] = (byte)Math.Max(1, Math.Min((intensity - zmin) / (zmax - zmin), 1d) * 255);
                    }
                    return image;
                };
                return BitmapImageModel.Create(factory, width, height, pf, Colormaps.Viridis, $"m/z {Mz.Value}, Mobility {Drift.Value} [1/K0]");
            }
        }

        public async Task SaveAsync(Stream stream) {
            if (string.IsNullOrEmpty(Peak.Name)) {
                return;
            }
            var pixels = _intensitiesLoader.Load(_peakIndex);
            var row = string.Format("{0},{1},{2},{3},", Peak.MasterPeakID, Peak.Name, Mz.Value, Drift.Value) + string.Join(",", pixels.PixelPeakFeaturesList[0].IntensityArray);
            var encoded = UTF8Encoding.Default.GetBytes(row + "\n");
            await stream.WriteAsync(encoded, 0, encoded.Length).ConfigureAwait(false);
        }
    }
}
