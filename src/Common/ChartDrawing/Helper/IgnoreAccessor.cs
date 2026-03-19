using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Helper;

internal sealed class IgnoreAccessor : IPropertiesAccessor
{
    public static IgnoreAccessor Instance { get; } = new IgnoreAccessor();

    public string[] Properties => [];

    public object Apply(int depth, object item) => null;

    public object? Apply(object item) => null;

    public AxisValue ConvertToAxisValue(object item, IAxisManager axis) => AxisValue.NaN;
}
