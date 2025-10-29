using CompMs.Graphics.Core.Base;

namespace CompMs.Graphics.Helper
{
    internal interface IPropertiesAccessor
    {
        string[] Properties { get; }

        object Apply(int depth, object item);
        object? Apply(object item);
        AxisValue ConvertToAxisValue(object item, IAxisManager axis);
    }
}