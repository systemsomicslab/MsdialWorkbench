using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(RangeTypeConverter))]
    public class Range
    {
        public AxisValue Minimum { get; set; }
        public AxisValue Maximum { get; set; }

        public Range(AxisValue minimum, AxisValue maximum) {
            Minimum = minimum;
            Maximum = maximum;
        }
    }

    public class RangeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (!(value is string text)) throw new ArgumentException();

            var values = text.Split(',');

            if (values.Length != 2) throw new ArgumentException();

            if (!double.TryParse(values[0].Trim(), out var min)) throw new ArgumentException();
            if (!double.TryParse(values[1].Trim(), out var max)) throw new ArgumentException();

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
