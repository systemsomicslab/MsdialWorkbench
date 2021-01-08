using CompMs.Common.Components;

namespace CompMs.App.Msdial.ViewModel
{
    public class ChromatogramPeakWrapper
    {
        public double Intensity => innerModel.Intensity;
        public double? ChromXValue => innerModel.ChromXs?.Value;

        private ChromatogramPeak innerModel;
        public ChromatogramPeakWrapper(ChromatogramPeak peak) {
            innerModel = peak;
        }
    }

}
