using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class MatchedSpotCandidate<T> where T: IAnnotatedObject, IChromatogramPeak
    {
        private readonly double _mzTolerance;
        private readonly double _mainChromXTolerance;
        private readonly double _amplitudeThreshold;

        public MatchedSpotCandidate(T spot, MoleculeMsReference reference, double mzTolerance, double mainChromXTolerance, double amplitudeThreshold) {
            Spot = spot;
            Reference = reference;
            _mzTolerance = mzTolerance;
            _mainChromXTolerance = mainChromXTolerance;
            _amplitudeThreshold = amplitudeThreshold;
        }

        public T Spot { get; }

        public MoleculeMsReference Reference { get; }

        public bool IsAnnotated {
            get {
                return Reference.Name.Contains(Spot.MatchResults.Representative.Name);
            }
        }

        public bool IsSimilarByMz {
            get {
                var mzTolerance = Math.Max(Reference.MassTolerance, _mzTolerance);
                return Math.Abs(Spot.Mass - Reference.PrecursorMz) < mzTolerance;
            }
        }

        public bool IsSimilarByTime {
            get {
                var type = Spot.ChromXs.MainType;
                var t = Spot.ChromXs.GetChromByType(type);
                var s = Reference.ChromXs.GetChromByType(type);
                var mainChromXTolerance = Math.Max(Reference.RetentionTimeTolerance, _mainChromXTolerance);
                switch (type) {
                    case ChromXType.RT:
                        return Math.Abs(t.Value - s.Value) < mainChromXTolerance;
                    case ChromXType.RI:
                        return Math.Abs(t.Value - s.Value) < mainChromXTolerance;
                    case ChromXType.Drift:
                        return Math.Abs(t.Value - s.Value) < mainChromXTolerance;
                    case ChromXType.Mz:
                        return Math.Abs(t.Value - s.Value) < mainChromXTolerance;
                    default:
                        throw new NotImplementedException();
                }
            }
        }

        public bool IsStrongerThanThreshold {
            get {
                return Spot.Intensity >= _amplitudeThreshold;
            }
        }
    }

    public sealed class MatchedSpotCandidateCalculator {
        private readonly double _mzTolerance;
        private readonly double _mainChromXTolerance;
        private readonly double _amplitudeThreshold;

        public MatchedSpotCandidateCalculator(double mzTolerance, double mainChromXTolerance, double amplitudeThreshold) {
            _mzTolerance = mzTolerance;
            _mainChromXTolerance = mainChromXTolerance;
            _amplitudeThreshold = amplitudeThreshold;
        }

        public MatchedSpotCandidate<T> Score<T>(T spot, MoleculeMsReference reference) where T: IAnnotatedObject, IChromatogramPeak {
            return new MatchedSpotCandidate<T>(spot, reference, _mzTolerance, _mainChromXTolerance, _amplitudeThreshold);
        }

        public MatchedSpotCandidate<T> Match<T>(T spot, MoleculeMsReference reference) where T: IAnnotatedObject, IChromatogramPeak {
            var candidate = Score(spot, reference);
            if (candidate.IsSimilarByMz) {
                return candidate;
            }
            else {
                return null;
            }
        }
    }

    public static class MatchedSpotCandidate {
        public static MatchedSpotCandidate<T> IsMatchedWith<T>(this T spot, MoleculeMsReference reference, double mzTolerance, double mainChromXTolerance, double amplitudeThreshold) where T: IAnnotatedObject, IChromatogramPeak {
            var calculator = new MatchedSpotCandidateCalculator(mzTolerance, mainChromXTolerance, amplitudeThreshold);
            return calculator.Match(spot, reference);
        }
    }
}
