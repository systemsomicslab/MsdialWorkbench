using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(ChartMarginTypeConverter))]
    public class ChartMargin : IChartMargin
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

        public AxisRange Add(AxisRange range) {
            var d = range.Delta;
            return new AxisRange(range.Minimum - d * Left, range.Maximum + d * Right);
        }

        public AxisRange Remove(AxisRange range) {
            var d = range.Delta / (1 + Left + Right);
            var x = range.Minimum + Left * d;
            return new AxisRange(x, x + d);
        }

        public (double, double) Add(double lower, double upper) {
            var d = upper - lower;
            return (lower - d * Left, upper + d * Right);
        }

        public (double, double) Remove(double lower, double upper) {
            var d = (upper - lower) / (1 + Left + Right);
            var x = lower + Left * d;
            return (x, x + d);
        }

        public override string ToString() {
            return $"{Left},{Right}";
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

    [TypeConverter(typeof(ChartMargin2TypeConverter))]
    public interface IChartMargin
    {
        (double, double) Add(double lower, double upper);
        (double, double) Remove(double lower, double upper);
    }

    public class RelativeMargin : IChartMargin
    {
        public RelativeMargin(double lower, double upper) {
            Lower = lower;
            Upper = upper;
        }

        public RelativeMargin(double margin) {
            Lower = Upper = margin;
        }

        public double Lower { get; }
        public double Upper { get; }

        public (double, double) Add(double lower, double upper) {
            if (lower > upper) {
                return (lower, upper);
            }
            var delta = upper - lower;
            return (lower - delta * Lower, upper + delta * Upper);
        }

        public (double, double) Remove(double lower, double upper) {
            if (lower > upper) {
                return (lower, upper);
            }
            var delta = (upper - lower) / (1 + Upper + Lower);
            return (lower + Lower * delta, upper - Upper * delta);
        }

        public override string ToString() {
            return $"{Lower}*,{Upper}*";
        }
    }

    public class ConstantMargin : IChartMargin
    {
        public ConstantMargin(double lower, double upper) {
            Lower = lower;
            Upper = upper;
        }

        public ConstantMargin(double margin) {
            Lower = Upper = margin;
        }

        public double Lower { get; }
        public double Upper { get; }

        public (double, double) Add(double lower, double upper) {
            return (lower - Lower, upper + Upper);
        }

        public (double, double) Remove(double lower, double upper) {
            return (lower + Lower, upper - Upper);
        }

        public override string ToString() {
            return $"{Lower}+,{Upper}+";
        }
    }

    public class ChartMargin2TypeConverter : TypeConverter
    {
        public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
            if (sourceType == typeof(string)) {
                return true;
            }
            return base.CanConvertFrom(context, sourceType);
        }

        public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
            if (destinationType == typeof(string)) {
                return true;
            }
            return base.CanConvertTo(context, destinationType);
        }

        public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
            if (value is string text) {

                var values = text.Split(',');
                if (values.Length == 0 || values.Length >= 3) {
                    return null;
                }

                if (values.All(v => v.EndsWith("+"))) {
                    if (TryParseMargin(values.Select(v => v.TrimEnd('+')).ToArray(), out var left, out var right)) {
                        return new ConstantMargin(left, right);
                    }
                }
                else if (values.All(v => v.EndsWith("*"))) {
                    if (TryParseMargin(values.Select(v => v.TrimEnd('*')).ToArray(), out var left, out var right)) {
                        return new RelativeMargin(left, right);
                    }
                }
                else {
                    if (TryParseMargin(values, out var left, out var right)) {
                        return new RelativeMargin(left, right);
                    }
                }
            }
            return base.ConvertFrom(context, culture, value);
        }

        private bool TryParseMargin(IReadOnlyList<string> values, out double left, out double right) {
            left = right = 0;
            if (values.Count == 0 || values.Count >= 3) {
                return false;
            }
            if (double.TryParse(values[0].Trim(), out left)) {
                if (values.Count == 1) {
                    right = left;
                    return true;
                }
                if (double.TryParse(values[1].Trim(), out right)) {
                    return true;
                }
            }
            return false;
        }

        public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
            return value.ToString();
        }
    }
}
