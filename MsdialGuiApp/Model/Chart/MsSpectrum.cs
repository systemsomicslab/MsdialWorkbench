using CompMs.Common.Components;
using CompMs.CommonMVVM;
using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class MsSpectrum : BindableBase
    {
        public MsSpectrum(List<SpectrumPeak> spectrum) {
            Spectrum = spectrum?.OrderBy(peak => peak.Mass).ToList() ?? new List<SpectrumPeak>(0);
        }

        public List<SpectrumPeak> Spectrum { get; }

        public Range GetSpectrumRange(Func<SpectrumPeak, double> selector) {
            if (Spectrum.Count == 0) {
                return new Range(0, 1);
            }
            return new Range(Spectrum.Min(selector), Spectrum.Max(selector));
        }

        public MsSpectrum Difference(MsSpectrum other, double tolerance) {
            var result = new List<SpectrumPeak>();
            var j = 0;
            foreach (var peak in Spectrum) {
                while (j < other.Spectrum.Count && other.Spectrum[j].Mass < peak.Mass - tolerance) {
                    j++;
                }
                if (j < other.Spectrum.Count && Math.Abs(other.Spectrum[j].Mass - peak.Mass) <= tolerance) {
                    continue;
                }
                result.Add(peak);
            }
            return new MsSpectrum(result);
        }

        public MsSpectrum Product(MsSpectrum other, double tolerance) {
            var result = new List<SpectrumPeak>();
            var j = 0;
            foreach (var peak in Spectrum) {
                while (j < other.Spectrum.Count && other.Spectrum[j].Mass < peak.Mass - tolerance) {
                    j++;
                }
                if (j < other.Spectrum.Count && Math.Abs(other.Spectrum[j].Mass - peak.Mass) <= tolerance) {
                    result.Add(peak);
                }
            }
            return new MsSpectrum(result);
        }
    }
}
