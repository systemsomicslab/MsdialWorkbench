using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(AxisValueTypeConverter))]
    public struct AxisValue : IComparable<AxisValue>
    {
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

        public int CompareTo(AxisValue other) {
            return Value.CompareTo(other.Value);
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
