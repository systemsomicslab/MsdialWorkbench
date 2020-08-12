using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(RangeTypeConverter))]
    public class Range
    {
        public double Minimum { get; set; }
        public double Maximum { get; set; }
    }

    public class RangeTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            var text = value as string;
            if (text == null) return null;

            var values = text.Split(',');

            if (!double.TryParse(values[0].Trim(), out var min)) throw new ArgumentException();
            if (!double.TryParse(values[1].Trim(), out var max)) throw new ArgumentException();

            return new Range { Minimum = min, Maximum = max };
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            var range = value as Range;
            return range == null ? null : $"{range.Minimum},{range.Maximum}";
        }
    }
}
