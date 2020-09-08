using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Graphics.Core.Base
{
    [TypeConverter(typeof(AxisValueTypeConverter))]
    public struct AxisValue
    {
        public double Value { get; }
        public AxisValue(double val) {
            Value = val;
        }

        public static implicit operator double(AxisValue val) {
            return val.Value;
        }

        public static implicit operator AxisValue(double val) {
            return new AxisValue(val);
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
