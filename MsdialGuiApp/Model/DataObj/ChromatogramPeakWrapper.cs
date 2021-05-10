using CompMs.Common.Components;

namespace CompMs.App.Msdial.Model.DataObj
{
    public class ChromatogramPeakWrapper
    {
        public double Intensity => innerModel.Intensity;
        public double? ChromXValue => innerModel.ChromXs?.Value;

        private readonly ChromatogramPeak innerModel;
        public ChromatogramPeakWrapper(ChromatogramPeak peak) {
            innerModel = peak;
        }
    }

}
