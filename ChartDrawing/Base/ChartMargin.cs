using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(ChartMarginTypeConverter))]
    public class ChartMargin
    {
        public ChartMargin() { }

        public ChartMargin(double leftMargin, double rightMargin) {
            Left = leftMargin;
            Right = rightMargin;
        }

        public ChartMargin(double margin) {
            Left = margin;
            Right = margin;
        }

        public double Left { get; set; }
        public double Right { get; set; }

        public Range Add(Range range) {
            var d = range.Delta;
            return new Range(range.Minimum - d * Left, range.Maximum + d * Right);
        }

        public Range Remove(Range range) {
            var d = range.Delta / (1 + Left + Right);
            var x = range.Minimum + Left * d;
            return new Range(x, x + d);
        }
    }

    public class ChartMarginTypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            return sourceType == typeof(string);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            return destinationType == typeof(string);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (!(value is string text)) return null;

            var values = text.Split(',');
            double left, right;

            if (values.Length == 2) {
                if (!double.TryParse(values[0].Trim(), out left)) return null;
                if (!double.TryParse(values[1].Trim(), out right)) return null;
            }
            else if (values.Length == 1) {
                if (!double.TryParse(values[0].Trim(), out left)) return null;
                right = left;
            }
            else {
                return null;
            }


            return new ChartMargin { Left = left, Right = right };
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return (value is ChartMargin margin) ? $"{margin.Left},{margin.Right}" : null;
        }
    }
}
