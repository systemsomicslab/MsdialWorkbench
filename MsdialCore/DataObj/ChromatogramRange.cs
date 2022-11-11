using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class ChromatogramRange
    {
        public ChromatogramRange(double begin, double end, ChromXType type, ChromXUnit unit) {
            Begin = begin;
            End = end;
            Type = type;
            Unit = unit;
        }

        public ChromatogramRange(IChromatogramPeakFeature peak, ChromXType type, ChromXUnit unit) {
            Begin = peak.ChromXsLeft.GetChromByType(type).Value;
            End = peak.ChromXsRight.GetChromByType(type).Value;
            Type = type;
            Unit = unit;
        }

        public double Begin { get; }
        public double End { get; }
        public ChromXType Type { get; }
        public ChromXUnit Unit { get; }

        public double Width => End - Begin;

        public ChromatogramRange Extend(double rate) {
            var extendWidth = Width * rate;
            return new ChromatogramRange(Begin - extendWidth, End + extendWidth, Type, Unit);
        }
    }
}
