using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Utility {
    public sealed class ComponentsConverter {
        private ComponentsConverter() { }

        public static string GetSpectrumString(RawPeakElement[] spectrum, char mz_abs_separator = ':', char peaks_separater = ' ') {
            if (spectrum == null || spectrum.Length == 0)
                return string.Empty;

            var specString = string.Empty;
            for (int i = 0; i < spectrum.Length; i++) {
                var spec = spectrum[i];
                var mz = Math.Round(spec.Mz, 5);
                var intensity = Math.Round(spec.Intensity, 0);
                var sString = mz + mz_abs_separator + intensity;

                if (i == spectrum.Length - 1)
                    specString += sString;
                else
                    specString += sString + peaks_separater;
            }

            return specString;
        }

       
        
        //public static ChromXs GetChromXsObj(double value, ChromXType type, ChromXUnit unit) {
        //    switch (type) {
        //        case ChromXType.RT: return new ChromXs() { RT = new RetentionTime(value, unit) };
        //        case ChromXType.RI: return new ChromXs() { RI = new RetentionIndex(value, unit) };
        //        case ChromXType.Drift: return new ChromXs() { Drift = new DriftTime(value, unit) };
        //        case ChromXType.Mz: return new ChromXs() { Mz = new MzValue(value, unit) };
        //        default: return new ChromXs() { RT = new RetentionTime(value) };
        //    }
        //}
    }
}
