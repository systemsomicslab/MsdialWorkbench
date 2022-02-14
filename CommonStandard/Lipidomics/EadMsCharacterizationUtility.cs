using CompMs.Common.Components;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Lipidomics {
    public static class EadMsCharacterizationUtility {
        public static bool IsDiagnosticFragmentExist(List<SpectrumPeak> spectrum, 
            double mzTolerance,
            double diagnosticMz, 
            double threshold) {
            for (int i = 0; i < spectrum.Count; i++) {
                var mz = spectrum[i].Mass;
                var intensity = spectrum[i].Intensity; // should be normalized by max intensity to 100

                if (intensity > threshold && Math.Abs(mz - diagnosticMz) < mzTolerance) {
                    return true;
                }
            }
            return false;
        }

        public static int CountDetectedIons(
            List<SpectrumPeak> exp_spectrum,
            List<SpectrumPeak> ref_spectrum,
            double tolerance) {
            var ionDetectedCounter = 0;
            foreach (var ion in ref_spectrum) {
                if (EadMsCharacterizationUtility.IsDiagnosticFragmentExist(exp_spectrum, tolerance, ion.Mass, ion.Intensity * 0.01)) {
                    ionDetectedCounter++;
                }
            }
            return ionDetectedCounter;
        }
    }
}
