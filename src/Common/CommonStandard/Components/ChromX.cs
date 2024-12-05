using MessagePack;
using System;
using System.ComponentModel;

namespace CompMs.Common.Components
{
    public enum ChromXType { RT, RI, Drift, Mz }
    public enum ChromXUnit { Min, Sec, Msec, Mz, None, K0, OneOverK0 }

    [Union(0, typeof(ChromXs))]
    [Union(1, typeof(RetentionTime))]
    [Union(2, typeof(RetentionIndex))]
    [Union(3, typeof(MzValue))]
    [Union(4, typeof(DriftTime))]
    public interface IChromX {
        double Value { get; }
        ChromXType Type { get; } 
        ChromXUnit Unit { get; }
        IChromX Add(double delta);
    }

    [MessagePackObject]
    public sealed class RetentionTime : IChromX, IComparable<RetentionTime>
    {
        [Key(0)]
        public double Value { get; }
        [Key(1)]
        public ChromXType Type { get; }
        [Key(2)]
        public ChromXUnit Unit { get; }

        public RetentionTime(double retentionTime, ChromXUnit unit = ChromXUnit.Min)
        {
            Value = retentionTime;
            Type = ChromXType.RT;
            Unit = unit;
        }

        /// <summary>
        /// Only MessagePack for C# use this constructor. Use ctor(double, ChromXUnit) instead.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_type"></param>
        /// <param name="unit"></param>
        [SerializationConstructor]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RetentionTime(double value, ChromXType _type, ChromXUnit unit) {
            Value = value;
            Type = ChromXType.RT;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"RT: {Value:F3} {GetUnitString()}";
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
                case ChromXUnit.Msec:
                    return "msec";
                default:
                    return "";
            }
        }

        public int CompareTo(RetentionTime other) {
            return Value.CompareTo(other.Value);
        }

        IChromX IChromX.Add(double delta) {
            return this + delta;
        }

        public static RetentionTime operator+(RetentionTime left, double delta) {
            return new RetentionTime(left.Value + delta, left.Unit);
        }

        public static RetentionTime operator-(RetentionTime left, double delta) {
            return new RetentionTime(left.Value - delta, left.Unit);
        }

        internal static RetentionTime Default { get; } = new RetentionTime(-1, ChromXUnit.Min);
    }

    [MessagePackObject]
    public sealed class RetentionIndex : IChromX, IComparable<RetentionIndex>
    {
        [Key(0)]
        public double Value { get; }
        [Key(1)]
        public ChromXType Type { get; }
        [Key(2)]
        public ChromXUnit Unit { get; }

        public RetentionIndex(double retentionIndex, ChromXUnit unit = ChromXUnit.None)
        {
            Value = retentionIndex;
            Type = ChromXType.RI;
            Unit = unit;
        }

        /// <summary>
        /// Only MessagePack for C# use this constructor. Use ctor(double, ChromXUnit) instead.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_type"></param>
        /// <param name="unit"></param>
        [SerializationConstructor]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public RetentionIndex(double value, ChromXType _type, ChromXUnit unit) {
            Value = value;
            Type = ChromXType.RI;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"RI: {Value:F3} {GetUnitString()}";
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
                case ChromXUnit.Msec:
                    return "msec";
                default:
                    return "";
            }
        }

        public int CompareTo(RetentionIndex other) {
            return Value.CompareTo(other.Value);
        }

        IChromX IChromX.Add(double delta) {
            return this + delta;
        }

        public static RetentionIndex operator+(RetentionIndex left, double delta) {
            return new RetentionIndex(left.Value + delta, left.Unit);
        }

        public static RetentionIndex operator-(RetentionIndex left, double delta) {
            return new RetentionIndex(left.Value - delta, left.Unit);
        }

        internal static RetentionIndex Default { get; } = new RetentionIndex(-1, ChromXUnit.None);
    }

    [MessagePackObject]
    public sealed class DriftTime : IChromX, IComparable<DriftTime>
    {
        [Key(0)]
        public double Value { get; }
        [Key(1)]
        public ChromXType Type { get; }
        [Key(2)]
        public ChromXUnit Unit { get; }

        public DriftTime(double driftTime, ChromXUnit unit = ChromXUnit.Msec)
        {
            Value = driftTime;
            Type = ChromXType.Drift;
            Unit = unit;
        }

        /// <summary>
        /// Only MessagePack for C# use this constructor. Use ctor(double, ChromXUnit) instead.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_type"></param>
        /// <param name="unit"></param>
        [SerializationConstructor]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public DriftTime(double value, ChromXType _type, ChromXUnit unit) {
            Value = value;
            Type = ChromXType.Drift;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"Drift: {Value:F3} {GetUnitString()}";
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
                case ChromXUnit.Msec:
                    return "msec";
                default:
                    return "";
            }
        }

        public int CompareTo(DriftTime other) {
            return Value.CompareTo(other.Value);
        }

        IChromX IChromX.Add(double delta) {
            return this + delta;
        }

        public static DriftTime operator+(DriftTime left, double delta) {
            return new DriftTime(left.Value + delta, left.Unit);
        }

        public static DriftTime operator-(DriftTime left, double delta) {
            return new DriftTime(left.Value - delta, left.Unit);
        }

        internal static DriftTime Default { get; } = new DriftTime(-1, ChromXUnit.Msec);
    }

    [MessagePackObject]
    public sealed class MzValue : IChromX, IComparable<MzValue> {
        [Key(0)]
        public double Value { get; }
        [Key(1)]
        public ChromXType Type { get; }
        [Key(2)]
        public ChromXUnit Unit { get; }

        public MzValue(double mz, ChromXUnit unit = ChromXUnit.Mz) {
            Value = mz;
            Type = ChromXType.Mz;
            Unit = unit;
        }

        /// <summary>
        /// Only MessagePack for C# use this constructor. Use ctor(double, ChromXUnit) instead.
        /// </summary>
        /// <param name="value"></param>
        /// <param name="_type"></param>
        /// <param name="unit"></param>
        [SerializationConstructor]
        [EditorBrowsable(EditorBrowsableState.Never)]
        public MzValue(double value, ChromXType _type, ChromXUnit unit) {
            Value = value;
            Type = ChromXType.Mz;
            Unit = unit;
        }

        public override string ToString()
        {
            return $"Mz: {Value:F3} {GetUnitString()}";
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
                case ChromXUnit.Msec:
                    return "msec";
                default:
                    return "";
            }
        }

        public int CompareTo(MzValue other) {
            return Value.CompareTo(other.Value);
        }

        IChromX IChromX.Add(double delta) {
            return this + delta;
        }

        public static MzValue operator+(MzValue left, double delta) {
            return new MzValue(left.Value + delta, left.Unit);
        }

        public static MzValue operator-(MzValue left, double delta) {
            return new MzValue(left.Value - delta, left.Unit);
        }

        internal static MzValue Default { get; } = new MzValue(-1, ChromXUnit.Mz);
    }

    public static class ChromX {
        public static IChromX Convert(double value, ChromXType type, ChromXUnit unit) {
            switch (type) {
                case ChromXType.RT:
                    return new RetentionTime(value, unit);
                case ChromXType.RI:
                    return new RetentionIndex(value, unit);
                case ChromXType.Drift:
                    return new DriftTime(value, unit);
                case ChromXType.Mz:
                    return new MzValue(value, unit);
                default:
                    throw new NotSupportedException(nameof(type));
            }
        }
    }
}
