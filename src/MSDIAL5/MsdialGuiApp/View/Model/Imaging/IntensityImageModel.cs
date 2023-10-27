using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class IntensityImageModel : BindableBase
    {
        public IntensityImageModel(RawPixelFeatures features, MaldiFrames frameInfos, ChromatogramPeakFeatureModel peak, MaldiFrameLaserInfo laserInfo) {
            if (features is null) {
                throw new ArgumentNullException(nameof(features));
            }

            Peak = peak;
            Mz = peak.Mz;
            Drift = peak.Drift;

            var xmin = BitmapImageModel.WithMarginToPoint(frameInfos.XIndexPosMin);
            var ymin = BitmapImageModel.WithMarginToPoint(frameInfos.YIndexPosMin);
            var width = BitmapImageModel.WithMarginToLength(frameInfos.XIndexWidth);
            var height = BitmapImageModel.WithMarginToLength(frameInfos.YIndexHeight);
            var pf = PixelFormats.Indexed8;
            var stride = (pf.BitsPerPixel + 7) / 8;
            var image = new byte[width * stride * height];
            var sorted = features.IntensityArray.OrderBy(x => x).ToArray();
            var length = sorted.Length;
            var zmin = sorted.DefaultIfEmpty(0d).First();
            var zmax = sorted.DefaultIfEmpty(255).ElementAt((int)(length * .99));
            if (zmin == zmax) {
                zmax = zmin + 1;
            }
            foreach (var (intensity, info) in features.IntensityArray.Zip(frameInfos.Infos, (x, y) => (x, y))) {
                image[width * stride * (info.YIndexPos - ymin) + (info.XIndexPos - xmin) * stride] = (byte)Math.Max(1, Math.Min((intensity - zmin) / (zmax - zmin), 1d) * 255);
            }
            BitmapImageModel = BitmapImageModel.Create(image, width, height, pf, Colormaps.Viridis, $"m/z {Mz.Value}, Mobility {Drift.Value} [1/K0]");
        }

        public ChromatogramPeakFeatureModel Peak { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public BitmapImageModel BitmapImageModel { get; }
    }
}
