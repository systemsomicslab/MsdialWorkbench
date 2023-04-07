using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public sealed class MatchedSpectrumPeakPair
    {
        public MatchedSpectrumPeakPair(ISpectrumPeak experiment, SpectrumPeak reference) {
            Experiment = experiment;
            Reference = reference;
        }

        public ISpectrumPeak Experiment { get; }
        public SpectrumPeak Reference { get; }
    }
}
