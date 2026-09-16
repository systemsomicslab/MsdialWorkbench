using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Helper;

internal sealed class IdentityAccessor : IPropertiesAccessor
{
    public static IdentityAccessor Instance { get; } = new IdentityAccessor();

    public string[] Properties { get; } = [];

    public object Apply(int depth, object item) {
        return item;
    }

    public object? Apply(object item) {
        return item;
    }

    public AxisValue ConvertToAxisValue(object item, IAxisManager axis) {
        return axis.TranslateToAxisValue(item);
    }
}
