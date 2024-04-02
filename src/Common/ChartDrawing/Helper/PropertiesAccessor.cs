using CompMs.Graphics.Core.Base;
using System;
using System.Linq;
using System.Linq.Expressions;

namespace CompMs.Graphics.Helper;

internal sealed class PropertiesAccessor
{
    private readonly LambdaExpression[] _expressions;
    private readonly Expression<Func<object, IAxisManager, AxisValue>> _getAxisValueExpression;

    public PropertiesAccessor(string property, Type type)
    {
        Properties = ExpressionHelper.GetProperties(property);
        _expressions = ExpressionHelper.GetPropertyGetterFromSourceExpressions(type, property);
        _getAxisValueExpression = ExpressionHelper.GetConvertToAxisValueExpression(_expressions.LastOrDefault() ?? ExpressionHelper.Identity(type));
    }

    public string[] Properties { get; }

    public Delegate[] Delegates => _delegates ??= _expressions.Select(exp => exp.Compile()).ToArray();
    private Delegate[]? _delegates;

    public Func<object, IAxisManager, AxisValue> GetAxisValue => _getAxisValue ??= _getAxisValueExpression.Compile();
    private Func<object, IAxisManager, AxisValue>? _getAxisValue;
}
