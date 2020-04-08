using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Components
{
    public enum TimeType { RT, RI, Drift }
    public enum TimeUnit { Min, Sec, None }

    public abstract class Time
    {
        public double Value { get; set; }
        public TimeType Type { get; set; }
        public TimeUnit Unit { get; set; }

        public Time() { }
        public Time(double val)
        {
            Value = val;
        }

        public override string ToString()
        {
            var unit = Unit == TimeUnit.None ? "" : " " + Unit.ToString();
            switch (Type)
            {
                case TimeType.RT:
                    return "RT: " + Value + unit;
                case TimeType.RI:
                    return "RI: " + Value + unit;
                case TimeType.Drift:
                    return "Drift: " + Value + unit;
                default:
                    break;
            }
            return "RT: " + Value + " " + Unit;
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
