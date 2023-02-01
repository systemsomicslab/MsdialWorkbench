using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.DataObj;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiPeakSummaryModel : BindableBase
    {
        private readonly RoiModel _roi;
        private readonly RawPixelFeatures _pixelFeatures;

        public RoiPeakSummaryModel(RoiModel roi, RawPixelFeatures pixelFeatures, ChromatogramPeakFeatureModel peak) {
            _roi = roi ?? throw new System.ArgumentNullException(nameof(roi));
            _pixelFeatures = pixelFeatures ?? throw new System.ArgumentNullException(nameof(pixelFeatures));
            Peak = peak;
        }

        public ChromatogramPeakFeatureModel Peak { get; }
        public double AccumulatedIntensity => (_accumulatedIntensity ?? (_accumulatedIntensity = _pixelFeatures.IntensityArray.Average())).Value;
        private double? _accumulatedIntensity = null;
    }
}
