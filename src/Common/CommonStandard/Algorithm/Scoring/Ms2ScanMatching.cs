using CompMs.Common.Components;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.Scoring
{
    public sealed class Ms2ScanMatching
    {
        private readonly MsRefSearchParameterBase _searchParameter;

        public Ms2ScanMatching(MsRefSearchParameterBase searchParameter) {
            _searchParameter = searchParameter ?? throw new ArgumentNullException(nameof(searchParameter));
        }

        public MatchedSpectrumPair GetMatchedSpectrum(List<SpectrumPeak> experiment, List<SpectrumPeak> reference) {
            var matched = MsScanMatching.GetMachedSpectralPeaks(experiment, reference, _searchParameter.Ms2Tolerance, _searchParameter.MassRangeBegin, _searchParameter.MassRangeEnd);
            return new MatchedSpectrumPair(matched.Select(peak => new MatchedSpectrumPeakPair(new SpectrumPeak(peak.Mass, peak.Resolution), peak)).ToList());
        }

        public (List<SpectrumPeak> reference, double[,] inteisities) GetMatchedSpectraMatrix(List<SpectrumPeak> reference, IReadOnlyList<List<SpectrumPeak>> scans) {
            var reference_ = ShrinkPeaks(reference);
            var bin = _searchParameter.Ms2Tolerance;
            var result = new double[reference_.Count, scans.Count];
            var idx = new int[scans.Count];
            for (int i = 0; i < reference_.Count; i++) {
                var refPeak = reference_[i];
                for (int j = 0; j < scans.Count; j++) {
                    ref int idc = ref idx[j];
                    List<SpectrumPeak> spectrum = scans[j];
                    while (idc < spectrum.Count && spectrum[idc].Mass < refPeak.Mass - bin) {
                        idc++;
                    }
                    ref var intensity = ref result[i, j];
                    while (idc < spectrum.Count && spectrum[idc].Mass <= refPeak.Mass + bin) {
                        intensity += spectrum[idc++].Intensity;
                    }
                }
            }
            return (reference_, result);
        }

        private List<SpectrumPeak> ShrinkPeaks(List<SpectrumPeak> reference) {
            var result = new List<SpectrumPeak>(reference.Count);
            foreach (var peak in reference) {
                if (result.Any() && peak.Mass - result[result.Count - 1].Mass <= _searchParameter.Ms2Tolerance) {
                    result[result.Count - 1].Intensity += peak.Intensity;
                    result[result.Count - 1].SpectrumComment |= peak.SpectrumComment;
                    if (string.IsNullOrEmpty(result[result.Count - 1].Comment)) {
                        result[result.Count - 1].Comment = peak.Comment;
                    }
                    else {
                        result[result.Count - 1].Comment += ", " + peak.Comment;
                    }
                }
                else {
                    result.Add(peak.Clone());
                }
            }
            return result;
        }
    }
}
