using System;
using System.Collections.Generic;
using CompMs.Common.Components;

namespace CompMs.Common.Lipidomics
{
    public static class CompairTwoFeagmentsIntensity
    {
        public static bool isFragment1GreaterThanFragment2(List<SpectrumPeak> spectrum, double ms2Tolerance,
            double fragment1, double fragment2)
        {
            var frag1intensity = 0.0;
            var frag2intensity = 0.0;
            for (int i = 0; i < spectrum.Count; i++)
            {
                var mz = spectrum[i].Mass;
                var intensity = spectrum[i].Intensity; // should be normalized by max intensity to 100

                if (intensity > frag1intensity && Math.Abs(mz - fragment1) < ms2Tolerance)
                {
                    frag1intensity = intensity;
                }

                if (intensity > frag2intensity && Math.Abs(mz - fragment2) < ms2Tolerance)
                {
                    frag2intensity = intensity;
                }
            }
            if (frag1intensity > frag2intensity) return true;
            else return false;
        }

    }
}
