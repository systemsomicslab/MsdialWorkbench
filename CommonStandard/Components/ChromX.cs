using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components
{
    public enum ChromXType { RT, RI, Drift, Mz }
    public enum ChromXUnit { Min, Sec, None, Mz }

    [Union(0, typeof(ChromXs))]
    [MessagePackObject]
    public abstract class ChromX
    {
        [Key(0)]
        public double Value { get; set; } = -1;
        [Key(1)]
        public ChromXType Type { get; set; }
        [Key(2)]
        public ChromXUnit Unit { get; set; }

        public ChromX() { }
        public ChromX(double val)
        {
            Value = val;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case ChromXType.RT:
                    return $"RT: {Value:F3} {GetUnitString()}";
                case ChromXType.RI:
                    return $"RI: {Value:F3} {GetUnitString()}";
                case ChromXType.Drift:
                    return $"Drift: {Value:F3} {GetUnitString()}";
                case ChromXType.Mz:
                    return $"Mz: {Value:F3} {GetUnitString()}";
                default:
                    return "";
            }
        }
        public string GetUnitString()
        {
            switch (Unit)
            {
                case ChromXUnit.Min:
                    return "min";
                case ChromXUnit.Sec:
                    return "sec";
                case ChromXUnit.None:
                    return "";
                case ChromXUnit.Mz:
                    return "m/z";
                default:
                    return "";
            }
        }
    }

    public class RetentionTime : ChromX
    {
        public RetentionTime() { }
        public RetentionTime(double retentionTime, ChromXUnit unit = ChromXUnit.Min) : base(retentionTime)
        {
            Type = ChromXType.RT;
            Unit = unit;
        }
    }

    public class RetentionIndex : ChromX
    {
        public RetentionIndex() { }
        public RetentionIndex(double retentionIndex, ChromXUnit unit = ChromXUnit.None) : base(retentionIndex)
        {
            Type = ChromXType.RI;
            Unit = unit;
        }
    }

    public class DriftTime : ChromX
    {
        public DriftTime() { }
        public DriftTime(double driftTime, ChromXUnit unit = ChromXUnit.None) : base(driftTime)
        {
            Type = ChromXType.Drift;
            Unit = unit;
        }

    }

    public class MzValue : ChromX {
        public MzValue() { }
        public MzValue(double mz, ChromXUnit unit = ChromXUnit.Mz) : base(mz) {
            Type = ChromXType.Mz;
            Unit = unit;
        }
    }
}
