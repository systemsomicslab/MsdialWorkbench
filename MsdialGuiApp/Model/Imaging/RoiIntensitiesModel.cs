using CompMs.App.Msdial.Common;
using CompMs.App.Msdial.Model.Chart;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiIntensitiesModel : BindableBase
    {
        public RoiIntensitiesModel(RawPixelFeatures features, List<MaldiFrameInfo> infos, RoiModel roi, Raw2DElement element, MaldiFrameLaserInfo laserInfo) {
            if (features is null) {
                throw new ArgumentNullException(nameof(features));
            }

            Roi = roi ?? throw new ArgumentNullException(nameof(roi));
            Mz = new MzValue(element.Mz);
            Drift = new DriftTime(element.Drift);
            Intensities = features.IntensityArray;

            var xmin = infos.Select(info => info.XIndexPos).DefaultIfEmpty(0).Min();
            var xmax = infos.Select(info => info.XIndexPos).DefaultIfEmpty(1).Max();
            var ymin = infos.Select(info => info.YIndexPos).DefaultIfEmpty(0).Min();
            var ymax = infos.Select(info => info.YIndexPos).DefaultIfEmpty(1).Max();
            var width = xmax - xmin + 1;
            var height = ymax - ymin + 1;
            var pf = PixelFormats.Indexed8;
            var stride = (pf.BitsPerPixel + 7) / 8;
            var image = new byte[width * stride * height];
            var zmin = features.IntensityArray.DefaultIfEmpty(0).Min();
            var zmax = features.IntensityArray.DefaultIfEmpty(255).Max();
            foreach (var (intensity, info) in features.IntensityArray.Zip(infos, (x, y) => (x, y))) {
                image[width * stride * (info.YIndexPos - ymin) + (info.XIndexPos - xmin) * stride] = (byte)Math.Max(1, (intensity - zmin) / (zmax - zmin) * 255);
            }
            BitmapImageModel = new BitmapImageModel(image, width, height, pf, Colormaps.Viridis, $"m/z {element.Mz}, Mobility {element.Drift} [1/K0]");
        }

        public RoiModel Roi { get; }
        public SampleImageModel SampleImageModel { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public double[] Intensities { get; }
        public double AccumulatedIntensity => Intensities.Average();

        public BitmapImageModel BitmapImageModel { get; }
    }
}
