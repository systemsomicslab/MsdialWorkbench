using CompMs.Graphics.Core.Base;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CompMs.Graphics.Helper;

internal sealed class TypedPropertiesAccessor : IPropertiesAccessor
{
    private readonly LambdaExpression[] _expressions;
    private readonly Expression<Func<object, IAxisManager, AxisValue>> _getAxisValueExpression;

    public TypedPropertiesAccessor(string property, Type type) {
        Properties = ExpressionHelper.GetProperties(property);
        _expressions = ExpressionHelper.GetPropertyGetterFromSourceExpressions(type, property);
        _getAxisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(_expressions.LastOrDefault() ?? ExpressionHelper.Identity(type));
    }

    public string[] Properties { get; }

    private Delegate[] Delegates => _delegates ??= _expressions.Select(exp => exp.Compile()).ToArray();
    private Delegate[]? _delegates;

    public object? Apply(object item) {
        return Delegates.LastOrDefault()?.DynamicInvoke(item);
    }

    public object Apply(int depth, object item) {
        if (Properties.Length <= depth) {
            throw new ArgumentOutOfRangeException(nameof(depth));
        }
        return Delegates[depth].DynamicInvoke(item);
    }

    private Func<object, IAxisManager, AxisValue> GetAxisValue => _getAxisValue ??= _getAxisValueExpression.Compile();
    private Func<object, IAxisManager, AxisValue>? _getAxisValue;

    public AxisValue ConvertToAxisValue(object item, IAxisManager axis) {
        return GetAxisValue(item, axis);
    }
}
