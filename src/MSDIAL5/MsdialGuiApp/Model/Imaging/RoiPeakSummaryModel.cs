using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Linq;

namespace CompMs.App.Msdial.Model.Imaging
{
    internal sealed class RoiPeakSummaryModel : BindableBase
    {
        private readonly RoiAccess _access;
        private readonly RawIntensityOnPixelsLoader _intensitiesLoader;
        private readonly int _peakIndex;

        public RoiPeakSummaryModel(RoiAccess access, ChromatogramPeakFeatureModel peak, RawIntensityOnPixelsLoader intensitiesLoader, int peakIndex) {
            _access = access ?? throw new System.ArgumentNullException(nameof(access));
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
            _accumulatedIntensity = _access.Access(_intensitiesLoader.Load(_peakIndex).PixelPeakFeaturesList[0].IntensityArray).Average();
        }
    }
}
