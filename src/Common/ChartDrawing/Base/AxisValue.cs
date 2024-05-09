using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(AxisValueTypeConverter))]
    public struct AxisValue : IComparable<AxisValue>
    {
        private static double eps = 1e-9;

        public double Value { get; }
        public static readonly AxisValue NaN = new AxisValue(double.NaN);

        public AxisValue(double val) {
            Value = val;
        }

        public static implicit operator double(AxisValue val) {
            return val.Value;
        }

        public static implicit operator AxisValue(double val) {
            return new AxisValue(val);
        }

        public static bool operator <(AxisValue lhs, AxisValue rhs) => lhs.CompareTo(rhs) < 0;
        public static bool operator >(AxisValue lhs, AxisValue rhs) => lhs.CompareTo(rhs) > 0;
        public static bool operator <=(AxisValue lhs, AxisValue rhs) => lhs.CompareTo(rhs) <= 0;
        public static bool operator >=(AxisValue lhs, AxisValue rhs) => lhs.CompareTo(rhs) >= 0;

        public int CompareTo(AxisValue other) {
            if (double.IsNaN(other.Value)) {
                return 1;
            }
            if (double.IsNaN(Value)) {
                return -1;
            }
            if (Value - other.Value >= eps) {
                return 1;
            }
            if (other.Value - Value >= eps) {
                return -1;
            }
            return 0;
        }

        public override bool Equals(object obj) {
            return (obj is AxisValue other) && other.Value == Value;
        }

        public override string ToString() {
            return Value.ToString();
        }

        public override int GetHashCode() {
            return Value.GetHashCode();
        }

        public bool IsNaN() {
            return double.IsNaN(Value);
        }
    }

    public class AxisValueTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string text) {
                if (!double.TryParse(text, out var v)) {
                    return new AxisValue(v);
                }
            }
            return null;
        }
    }
}
