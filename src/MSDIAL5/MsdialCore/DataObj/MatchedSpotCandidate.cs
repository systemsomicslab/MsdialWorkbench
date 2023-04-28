using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;
using System.Linq;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class MatchedSpotCandidate<T> where T: IAnnotatedObject, IChromatogramPeak
    {
        private static readonly double MASS_TOLERANCE = .01d; // TODO: temporary set
        private static readonly double RT_TOLERANCE = 1d; // TODO: temporary set

        public MatchedSpotCandidate(T spot, MoleculeMsReference reference) {
            Spot = spot;
            Reference = reference;
        }

        public T Spot { get; }

        public MoleculeMsReference Reference { get; }

        public bool IsAnnotated {
            get {
                return Spot.MatchResults.MatchResults.Any(result => result.Name == Reference.Name);
            }
        }

        public bool IsSimilarWithMz {
            get {
                var tolerance = Math.Max(Reference.MassTolerance, MASS_TOLERANCE);
                return Math.Abs(Spot.Mass - Reference.PrecursorMz) < tolerance;
            }
        }

        public bool IsSimilarWithTime {
            get {
                var type = Spot.ChromXs.MainType;
                var t = Spot.ChromXs.GetChromByType(type);
                var s = Reference.ChromXs.GetChromByType(type);
                switch (type) {
                    case ChromXType.RT:
                        var tolerance = Math.Max(Reference.RetentionTimeTolerance, RT_TOLERANCE);
                        return Math.Abs(t.Value - s.Value) < tolerance;
                    case ChromXType.RI:
                    //    return Math.Abs(t.Value - s.Value) < tolerance;
                    case ChromXType.Drift:
                    //    return Math.Abs(t.Value - s.Value) < tolerance;
                    case ChromXType.Mz:
                    //    return Math.Abs(t.Value - s.Value) < tolerance;
                    default:
                        throw new NotImplementedException();
                }
            }
        }
    }

    public static class MatchedSpotCandidate {
        public static MatchedSpotCandidate<T> IsMatchedWith<T>(this T spot, MoleculeMsReference reference) where T: IAnnotatedObject, IChromatogramPeak {
            var candidate = new MatchedSpotCandidate<T>(spot, reference);
            if (candidate.IsAnnotated || candidate.IsSimilarWithTime || candidate.IsSimilarWithMz) {
                return candidate;
            }
            else {
                return null;
            }
        }
    }
}
