using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using System;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiIntensitiesModel : BindableBase
    {
        public RoiIntensitiesModel(RawPixelFeatures features, RoiModel roi, Raw2DElement element) {
            if (features is null) {
                throw new ArgumentNullException(nameof(features));
            }

            Roi = roi ?? throw new ArgumentNullException(nameof(roi));
            Mz = new MzValue(element.Mz);
            Drift = new DriftTime(element.Drift);
            Intensities = features.IntensityArray;
        }

        public RoiModel Roi { get; }
        public MzValue Mz { get; }
        public DriftTime Drift { get; }
        public double[] Intensities { get; }
        public double AccumulatedIntensity => Intensities.Average();
    }
}
