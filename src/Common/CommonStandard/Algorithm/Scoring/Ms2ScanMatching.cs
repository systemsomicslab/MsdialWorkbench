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
    }
}
