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
            SampleImageModel = new SampleImageModel(
                features.IntensityArray.Zip(infos, (intensity, info) => new ImagePixel(info.MotorPositionX, info.MotorPositionY, intensity)),
                $"m/z {element.Mz}, Mobility {element.Drift} [1/K0]",
                new GradientStopCollection
                {
                    // new GradientStop(Colors.Blue, 0d),
                    // new GradientStop(Colors.White, .5d),
                    // new GradientStop(Colors.Red, 1d),
                    new GradientStop(Color.FromRgb( 68,   1,  84), 0d),
                    new GradientStop(Color.FromRgb( 59,  82, 139), .25d),
                    new GradientStop(Color.FromRgb( 33, 145, 140), .5d),
                    new GradientStop(Color.FromRgb( 94, 201,  98), .75d),
                    new GradientStop(Color.FromRgb(253, 231,  37), 1d),
                },
                laserInfo.BeamScanSizeX, laserInfo.BeamScanSizeY);
        }

        public RoiModel Roi { get; }
        public SampleImageModel SampleImageModel { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public double[] Intensities { get; }
        public double AccumulatedIntensity => Intensities.Average();
    }
}
