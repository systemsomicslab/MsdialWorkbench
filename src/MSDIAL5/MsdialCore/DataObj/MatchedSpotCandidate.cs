using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class MatchedSpotCandidate<T> where T: IAnnotatedObject, IChromatogramPeak
    {
        private readonly double _mzTolerance;
        private readonly double _mainChromXTolerance;

        public MatchedSpotCandidate(T spot, MoleculeMsReference reference, double mzTolerance, double mainChromXTolerance) {
            Spot = spot;
            Reference = reference;
            _mzTolerance = mzTolerance;
            _mainChromXTolerance = mainChromXTolerance;
        }

        public T Spot { get; }

        public MoleculeMsReference Reference { get; }

        public bool IsAnnotated {
            get {
                return Spot.MatchResults.MatchResults.Any(result => Reference.Name.Contains(result.Name));
            }
        }

        public bool IsSimilarWithMz {
            get {
                var mzTolerance = Math.Max(Reference.MassTolerance, _mzTolerance);
                return Math.Abs(Spot.Mass - Reference.PrecursorMz) < mzTolerance;
            }
        }

        public bool IsSimilarWithTime {
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
    }

    public static class MatchedSpotCandidate {
        public static MatchedSpotCandidate<T> IsMatchedWith<T>(this T spot, MoleculeMsReference reference, double mzTolerance, double mainChromXTolerance) where T: IAnnotatedObject, IChromatogramPeak {
            var candidate = new MatchedSpotCandidate<T>(spot, reference, mzTolerance, mainChromXTolerance);
            if (candidate.IsAnnotated || candidate.IsSimilarWithMz) {
                return candidate;
            }
            else {
                return null;
            }
        }
    }
}
