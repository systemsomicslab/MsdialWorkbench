namespace CompMs.Common.Components
{
    public sealed class MatchedSpectrumPeakPair
    {
        public MatchedSpectrumPeakPair(SpectrumPeak experiment, SpectrumPeak reference) {
            Experiment = experiment;
            Reference = reference;
        }

        public SpectrumPeak Experiment { get; }
        public SpectrumPeak Reference { get; }
    }
}
