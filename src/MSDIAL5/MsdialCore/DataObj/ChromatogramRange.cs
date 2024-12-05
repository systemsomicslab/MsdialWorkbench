using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using System;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class ChromatogramRange
    {
        public ChromatogramRange(double begin, double end, ChromXType type, ChromXUnit unit) {
            if (begin > end) {
                throw new ArgumentException($"begin argument should be smaller than end argument.");
            }
            Begin = begin;
            End = end;
            Type = type;
            Unit = unit;
        }

        public ChromatogramRange(IChromatogramPeakFeature peak, ChromXType type, ChromXUnit unit) : this(peak.ChromXsLeft.GetChromByType(type).Value, peak.ChromXsRight.GetChromByType(type).Value, type, unit) {

        }

        public double Begin { get; }
        public double End { get; }
        public ChromXType Type { get; }
        public ChromXUnit Unit { get; }

        public double Width => End - Begin;

        public ChromatogramRange ExtendRelative(double rate) {
            if (rate < -.5d) {
                throw new ArgumentException("rate argument should be larger than `-0.5`.");
            }
            var extendWidth = Width * rate;
            return new ChromatogramRange(Begin - extendWidth, End + extendWidth, Type, Unit);
        }

        public ChromatogramRange ExtendWith(double value) {
            if (value < - Width / 2d) {
                throw new ArgumentException("value argument should be larger than `- Width / 2`.");
            }
            return new ChromatogramRange(Begin - value, End + value, Type, Unit);
        }

        public ChromatogramRange RestrictBy(double limitLow, double limitHigh) {
            if (limitLow > limitHigh) {
                throw new ArgumentException("limitHigh argument should be larger than limitLow argument.");
            }
            if (limitLow > End) {
                return new ChromatogramRange(End, End, Type, Unit);
            }
            if (limitHigh < Begin) {
                return new ChromatogramRange(Begin, Begin, Type, Unit);
            }
            return new ChromatogramRange(Math.Max(limitLow, Begin), Math.Min(limitHigh, End), Type, Unit);
        }

        public static ChromatogramRange FromTimes<T>(T begin, T end) where T: IChromX {
            return new ChromatogramRange(begin.Value, end.Value, begin.Type, begin.Unit);
        }
    }
}
