using CompMs.Common.Components;

namespace CompMs.App.Msdial.ViewModel
{
    public class SpectrumPeakWrapper
    {
        public double Intensity => innerModel.Intensity;
        public double Mass => innerModel.Mass;

        private SpectrumPeak innerModel;
        public SpectrumPeakWrapper(SpectrumPeak peak) {
            innerModel = peak;
        }
    }

}
