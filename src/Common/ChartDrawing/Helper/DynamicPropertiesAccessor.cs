using CompMs.Graphics.Core.Base;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CompMs.Graphics.Helper;

internal sealed class DynamicPropertiesAccessor(string property) : IPropertiesAccessor
{
    private Type? _currentType;
    private LambdaExpression[]? _expressions;
    private Expression<Func<object, IAxisManager, AxisValue>>? _getAxisValueExpression;

    public string[] Properties { get; } = ExpressionHelper.GetProperties(property);

    private Delegate[] Delegates => _delegates ??= _expressions.Select(exp => exp.Compile()).ToArray();
    private Delegate[]? _delegates;

    private Func<object, IAxisManager, AxisValue> GetAxisValue => _getAxisValue ??= _getAxisValueExpression.Compile();
    private Func<object, IAxisManager, AxisValue>? _getAxisValue;

    public object Apply(int depth, object item) {
        var type = item.GetType();
        if (_currentType != type) {
            Init(type);
        }
        return Delegates[depth].DynamicInvoke(item);
    }

    public object? Apply(object item) {
        var type = item.GetType();
        if (_currentType != type) {
            Init(type);
        }
        return Delegates.LastOrDefault()?.DynamicInvoke(item);
    }

    public AxisValue ConvertToAxisValue(object item, IAxisManager axis) {
        var type = item.GetType();
        if (_currentType != type) {
            Init(type);
        }
        return GetAxisValue(item, axis);
    }

    private void Init(Type type) {
        _expressions = ExpressionHelper.GetPropertyGetterFromSourceExpressions(type, property);
        _getAxisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(_expressions.LastOrDefault() ?? ExpressionHelper.Identity(type));
        _currentType = type;
        _delegates = null;
        _getAxisValue = null;
    }
}
