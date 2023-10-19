using CompMs.CommonSourceGenerator.MVVM;
using CompMs.MsdialCore.DataObj;

namespace CompMs.App.Msdial.Model.Setting
{
    [WrapToBindableType(typeof(PeakFeatureSearchValue))]
    internal partial class PeakFeatureSearchValueModel
    {
        public void ClearTitle() {
            Title = string.Empty;
        }

        public void ClearMassInformation() {
            Mass = 0d;
            MassTolerance = 0d;
        }
    }
}
