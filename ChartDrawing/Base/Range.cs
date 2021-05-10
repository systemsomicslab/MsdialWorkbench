using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(RangeTypeConverter))]
    public class Range
    {
        public AxisValue Minimum { get; private set; }
        public AxisValue Maximum { get; private set; }

        public Range(AxisValue minimum, AxisValue maximum) {
            if (minimum <= maximum) {
                Minimum = minimum;
                Maximum = maximum;
            }
            else {
                Minimum = 0;
                Maximum = 0;
            }
        }

        public double Delta {
            get => Maximum.Value - Minimum.Value;
        }

        public bool Contains(AxisValue value)
        {
            return Minimum <= value && value <= Maximum;
        }

        public bool Contains(Range other)
        {
            return Contains(other.Minimum) && Contains(other.Maximum);
        }

        public Range Intersect(Range other) {
            if (other == null) {
                return this;
            }
            return new Range(Math.Max(Minimum.Value, other.Minimum.Value), Math.Min(Maximum.Value, other.Maximum.Value));
        }

        public Range Union(Range other) {
            if (other == null) {
                return this;
            }
            return new Range(Math.Min(Minimum.Value, other.Minimum.Value), Math.Max(Maximum.Value, other.Maximum.Value));
        }
    }

    public class RangeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (!(value is string text)) return null;

            var values = text.Split(',');

            if (values.Length != 2) return null;

            if (!double.TryParse(values[0].Trim(), out var min)) return null;
            if (!double.TryParse(values[1].Trim(), out var max)) return null;

            return new Range(new AxisValue(min), new AxisValue(max));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return (value is Range range) ? $"{range.Minimum.Value},{range.Maximum.Value}" : null;
        }
    }
}
