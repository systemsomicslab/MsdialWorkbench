using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiPeakSummaryModel : BindableBase
    {
        private readonly RoiModel _roi;
        private readonly RawIntensityOnPixelsLoader _intensitiesLoader;
        private readonly int _peakIndex;

        public RoiPeakSummaryModel(RoiModel roi, ChromatogramPeakFeatureModel peak, RawIntensityOnPixelsLoader intensitiesLoader, int peakIndex) {
            _roi = roi ?? throw new System.ArgumentNullException(nameof(roi));
            Peak = peak;
            _intensitiesLoader = intensitiesLoader;
            _peakIndex = peakIndex;
        }

        public ChromatogramPeakFeatureModel Peak { get; }

        public double AccumulatedIntensity {
            get {
                if (_accumulatedIntensity is null) {
                    CalculateAccumulatedIntensity();
                }
                return _accumulatedIntensity ?? 0d;
            }
        }
        private double? _accumulatedIntensity = null;

        private void  CalculateAccumulatedIntensity() {
            _accumulatedIntensity = _intensitiesLoader.Load(_peakIndex).PixelPeakFeaturesList[0].IntensityArray.Average();
        }
    }
}
