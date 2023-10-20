using CompMs.CommonSourceGenerator.MVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Setting
{
    [BufferedBindableType(typeof(PeakFeatureSearchValue))]
    internal partial class PeakFeatureSearchValueModel
    {
        public void ClearChromatogramSearchQuery() {
            Title = string.Empty;
            Mass = 0d;
            MassTolerance = 0d;
        }

        public void ClearPeakSearchQuery() {
            Mass = 0d;
            MassTolerance = 0d;
            RelativeIntensityCutoff = 0d;
            PeakFeatureSearchType = PeakFeatureSearchType.ProductIon;
        }
    }
}
