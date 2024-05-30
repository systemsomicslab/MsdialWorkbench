using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(AxisRangeTypeConverter))]
    public sealed class AxisRange : IEquatable<AxisRange>
    {
        public AxisValue Minimum { get; private set; }
        public AxisValue Maximum { get; private set; }

        public AxisRange(AxisValue minimum, AxisValue maximum) {
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

        public bool Contains(AxisRange other)
        {
            return Contains(other.Minimum) && Contains(other.Maximum);
        }

        public AxisRange Intersect(AxisRange other) {
            if (other == null) {
                return this;
            }
            return new AxisRange(Math.Max(Minimum.Value, other.Minimum.Value), Math.Min(Maximum.Value, other.Maximum.Value));
        }

        public AxisRange Union(AxisRange? other) {
            if (other == null) {
                return this;
            }
            return new AxisRange(Math.Min(Minimum.Value, other.Minimum.Value), Math.Max(Maximum.Value, other.Maximum.Value));
        }

        public override string ToString() {
            return $"{Minimum.Value}-{Maximum.Value}";
        }

        public bool Equals(AxisRange other) {
            return !(other is null) && Maximum == other.Maximum && Minimum == other.Minimum;
        }
    }

    public sealed class AxisRangeTypeConverter : TypeConverter
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

            return new AxisRange(new AxisValue(min), new AxisValue(max));
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return (value is AxisRange range) ? $"{range.Minimum.Value},{range.Maximum.Value}" : null;
        }
    }
}
