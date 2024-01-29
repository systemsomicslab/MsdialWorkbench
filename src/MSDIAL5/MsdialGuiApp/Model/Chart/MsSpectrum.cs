using CompMs.Common.Components;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Chart
{
    public sealed class MsSpectrum : BindableBase
    {
        private List<SpectrumPeak> _spectrum;
        
        public MsSpectrum(IReadOnlyList<SpectrumPeak> spectrum) {
            _spectrum = spectrum?.OrderBy(peak => peak.Mass).ToList() ?? new List<SpectrumPeak>(0);
        }

        public List<SpectrumPeak> Spectrum => _spectrum;

        public bool HasSpectrum => _spectrum.Count > 0;

        public (double, double) GetSpectrumRange(Func<SpectrumPeak, double> selector) {
            if (_spectrum.Count == 0) {
                return (0d, 1d);
            }
            return (_spectrum.Min(selector), _spectrum.Max(selector));
        }

        public MsSpectrum Difference(MsSpectrum? other, double tolerance) {
            if (other is null) {
                return new MsSpectrum(new List<SpectrumPeak>(0));
            }
            var result = new List<SpectrumPeak>();
            var j = 0;
            foreach (var peak in _spectrum) {
                while (j < other._spectrum.Count && other._spectrum[j].Mass < peak.Mass - tolerance) {
                    j++;
                }
                if (j < other._spectrum.Count && Math.Abs(other._spectrum[j].Mass - peak.Mass) <= tolerance) {
                    continue;
                }
                result.Add(peak);
            }
            return new MsSpectrum(result);
        }

        public MsSpectrum Product(MsSpectrum? other, double tolerance) {
            if (other is null) {
                return new MsSpectrum(new List<SpectrumPeak>(0));
            }
            var result = new List<SpectrumPeak>();
            var j = 0;
            foreach (var peak in _spectrum) {
                while (j < other._spectrum.Count && other._spectrum[j].Mass < peak.Mass - tolerance) {
                    j++;
                }
                if (j < other._spectrum.Count && Math.Abs(other._spectrum[j].Mass - peak.Mass) <= tolerance) {
                    result.Add(peak);
                }
            }
            return new MsSpectrum(result);
        }
    }
}
