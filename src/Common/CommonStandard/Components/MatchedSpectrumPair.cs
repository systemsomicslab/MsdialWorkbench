using System.Collections.Generic;

namespace CompMs.Common.Components
{
    public sealed class MatchedSpectrumPair
    {
        private readonly List<MatchedSpectrumPeakPair> _pairs;

        public MatchedSpectrumPair(List<MatchedSpectrumPeakPair> pairs) {
            _pairs = pairs;
        }
    }
}
