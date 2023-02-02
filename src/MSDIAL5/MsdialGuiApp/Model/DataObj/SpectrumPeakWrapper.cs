using CompMs.Common.Components;
using CompMs.Common.Extension;
using NSSplash;
using NSSplash.impl;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.DataObj
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

    static class SpectrumPeakWrapperExtension
    {
        public static string CalculateSplashKey(this IReadOnlyCollection<SpectrumPeakWrapper> spectrum) {
            if (spectrum.IsEmptyOrNull() || spectrum.Count <= 2 && spectrum.All(peak => peak.Intensity == 0))
                return "N/A";
            var msspectrum = new MSSpectrum(string.Join(" ", spectrum.Select(peak => $"{peak.Mass}:{peak.Intensity}").ToArray()));
            return new Splash().splashIt(msspectrum);
        }
    }

}
