using CompMs.App.Msdial.Model.DataObj;
using CompMs.CommonMVVM;
using System.Linq;
using System.Threading.Tasks;

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

        public double? AccumulatedIntensity {
            get {
                if (_accumulatedIntensity is null && !_isAccumulatedIntensityLoading) {
                    _ = EnsureCalculateAccumulatedIntensityAsync();
                }
                return _accumulatedIntensity;
            }

            private set => SetProperty(ref _accumulatedIntensity, value);

        }
        private double? _accumulatedIntensity = null;

        public bool IsAccumulatedIntensityLoading {
            get => _isAccumulatedIntensityLoading;
            private set => SetProperty(ref _isAccumulatedIntensityLoading, value);
        }
        private bool _isAccumulatedIntensityLoading = false;

        private async Task EnsureCalculateAccumulatedIntensityAsync() {
            IsAccumulatedIntensityLoading = true;
            var pixelsTask = _intensitiesLoader.LoadAsync(_peakIndex);
            var pixels = await pixelsTask.ConfigureAwait(false);
            AccumulatedIntensity = _access.Access(pixels.PixelPeakFeaturesList[0].IntensityArray).Average();
            IsAccumulatedIntensityLoading = false;
        }
    }
}
