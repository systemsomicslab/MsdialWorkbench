using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;
using System.Linq;

namespace CompMs.Common.Lipidomics.Characterization
{
    internal interface ILipidScoring<in TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        LipidScore Score(TLipidCandidate lipid, IMSScanProperty scan);
    }

    internal sealed class LipidScoring<TLipidCandidate> : ILipidScoring<TLipidCandidate> where TLipidCandidate : ILipidCandidate
    {
        private readonly Func<TLipidCandidate, (double mz, double intensity)[]> _diagnosticIonsFactory;
        private readonly double _ms2Tolerance;

        public LipidScoring(Func<TLipidCandidate, (double mz, double intensity)[]> diagnosticIonsFactory, double ms2Tolerance) {
            _diagnosticIonsFactory = diagnosticIonsFactory;
            _ms2Tolerance = ms2Tolerance;
        }

        public LipidScore Score(TLipidCandidate lipid, IMSScanProperty scan) {
            var query = _diagnosticIonsFactory.Invoke(lipid).Select(pair => new SpectrumPeak { Mass = pair.mz, Intensity = pair.intensity }).ToList();
            LipidMsmsCharacterizationUtility.countFragmentExistence(scan.Spectrum, query, _ms2Tolerance, out var foundCount, out var averageIntensity);
            return new LipidScore(averageIntensity, foundCount);
        }
    }

    internal sealed class LipidScore {
        public LipidScore(double score, int count) {
            Score = score;
            Count = count;
        }

        public double Score { get; }
        public int Count { get; }
    }
}
