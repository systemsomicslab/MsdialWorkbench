using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components
{
    public enum TimeType { RT, RI, Drift }
    public enum TimeUnit { Min, Sec, None }

    public abstract class Time
    {
        public double Value { get; set; } = -1;
        public TimeType Type { get; set; }
        public TimeUnit Unit { get; set; }

        public Time() { }
        public Time(double val)
        {
            Value = val;
        }

        public override string ToString()
        {
            switch (Type)
            {
                case TimeType.RT:
                    return $"RT: {Value:F3} {GetUnitString()}";
                case TimeType.RI:
                    return $"RI: {Value:F3} {GetUnitString()}";
                case TimeType.Drift:
                    return $"Drift: {Value:F3} {GetUnitString()}";
                default:
                    return "";
            }
        }
        public string GetUnitString()
        {
            switch (Unit)
            {
                case TimeUnit.Min:
                    return "min";
                case TimeUnit.Sec:
                    return "sec";
                case TimeUnit.None:
                    return "";
                default:
                    return "";
            }
        }
    }

    public class RetentionTime : Time
    {
        public RetentionTime() { }
        public RetentionTime(double retentionTime, TimeUnit unit = TimeUnit.Min) : base(retentionTime)
        {
            Type = TimeType.RT;
            Unit = unit;
        }
    }

    public class RetentionIndex : Time
    {
        public RetentionIndex() { }
        public RetentionIndex(double retentionIndex, TimeUnit unit = TimeUnit.None) : base(retentionIndex)
        {
            Type = TimeType.RI;
            Unit = unit;
        }
    }

    public class DriftTime : Time
    {
        public DriftTime() { }
        public DriftTime(double driftTime, TimeUnit unit = TimeUnit.None) : base(driftTime)
        {
            Type = TimeType.Drift;
            Unit = unit;
        }

    }
}
