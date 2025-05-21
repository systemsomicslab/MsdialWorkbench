using CompMs.CommonMVVM;
using System;
using System.ComponentModel;
using System.Globalization;

namespace CompMs.Graphics.Base;

[TypeConverter(typeof(AxisTitlesTypeConverter))]
public sealed class AxisTitles : BindableBase
{
    public static readonly AxisTitles Empty = new AxisTitles { Titles = [] };

    public string[] Titles {
        get => _titles;
        set => SetProperty(ref _titles, value);
    }
    private string[] _titles = [];

    public override string ToString() {
        return string.Join(";", _titles);
    }
}

public sealed class AxisTitlesTypeConverter : TypeConverter {
    public override bool CanConvertFrom(ITypeDescriptorContext context, Type sourceType) {
        if (sourceType == typeof(string)) {
            return true;
        }
        if (typeof(System.Collections.Generic.IEnumerable<string>).IsAssignableFrom(sourceType)) {
            return true;
        }
        return base.CanConvertFrom(context, sourceType);
    }

    public override bool CanConvertTo(ITypeDescriptorContext context, Type destinationType) {
        if (destinationType == typeof(string)) {
            return true;
        }
        if (destinationType.IsAssignableFrom(typeof(string[]))) {
            return true;
        }
        return base.CanConvertTo(context, destinationType);
    }

    public override object ConvertFrom(ITypeDescriptorContext context, CultureInfo culture, object value) {
        if (value is string title) {
            return new AxisTitles { Titles = [title] };
        }
        if (value is string[] titles) {
            return new AxisTitles { Titles = titles };
        }
        if (value is System.Collections.Generic.IList<string> titles_) {
            return new AxisTitles { Titles = [.. titles_] };
        }
        return base.ConvertFrom(context, culture, value);
    }

    public override object ConvertTo(ITypeDescriptorContext context, CultureInfo culture, object value, Type destinationType) {
        if (destinationType == typeof(string)) {
            return value.ToString();
        }
        if (destinationType == typeof(string[]) || destinationType.IsAssignableFrom(typeof(string[]))) {
            return ((AxisTitles)value).Titles;
        }
        return base.ConvertTo(context, culture, value, destinationType);
    }
}

