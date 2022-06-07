using CompMs.Common.Components;

namespace CompMs.MsdialCore.DataObj
{
    public class ChromatogramRange
    {
        public ChromatogramRange(double begin, double end, ChromXType type, ChromXUnit unit) {
            Begin = begin;
            End = end;
            Type = type;
            Unit = unit;
        }

        public double Begin { get; }
        public double End { get; }
        public ChromXType Type { get; }
        public ChromXUnit Unit { get; }
    }
}
