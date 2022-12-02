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
    internal sealed class RoiIntensitiesModel : BindableBase
    {
        public RoiIntensitiesModel(RawPixelFeatures features, MaldiFrames frameInfos, ChromatogramPeakFeatureModel peak, MaldiFrameLaserInfo laserInfo) {
            if (features is null) {
                throw new ArgumentNullException(nameof(features));
            }

            Peak = peak;
            Mz = peak.Mz;
            Drift = peak.Drift;

            var xmin = frameInfos.XIndexPosMin - 1;
            var ymin = frameInfos.YIndexPosMin - 1;
            var width = frameInfos.XIndexWidth + 2;
            var height = frameInfos.YIndexHeight + 2;
            var pf = PixelFormats.Indexed8;
            var stride = (pf.BitsPerPixel + 7) / 8;
            var image = new byte[width * stride * height];
            var zmin = features.IntensityArray.DefaultIfEmpty(0).Min();
            var zmax = features.IntensityArray.DefaultIfEmpty(255).Max();
            foreach (var (intensity, info) in features.IntensityArray.Zip(frameInfos.Infos, (x, y) => (x, y))) {
                image[width * stride * (info.YIndexPos - ymin) + (info.XIndexPos - xmin) * stride] = (byte)Math.Max(1, (intensity - zmin) / (zmax - zmin) * 255);
            }
            BitmapImageModel = BitmapImageModel.Create(image, width, height, pf, Colormaps.Viridis, $"m/z {Mz.Value}, Mobility {Drift.Value} [1/K0]");
        }

        public ChromatogramPeakFeatureModel Peak { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public BitmapImageModel BitmapImageModel { get; }
    }
}
