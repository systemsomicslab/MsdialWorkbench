using CompMs.Graphics.Core.Base;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CompMs.Graphics.Helper
{
    internal static class ExpressionHelper
    {
        public static Expression<Func<object, IAxisManager, AxisValue>> GetConvertToAxisValueExpression(Type type, string property) {
            var properties = property == string.Empty ? [] : property.Split('.');
            var parameter = Expression.Parameter(typeof(object));
            var curType = type;
            Expression curValue = Expression.Convert(parameter, curType);
            foreach (var p in properties) {
                var m = Expression.Property(curValue, p);
                curType = ((PropertyInfo)m.Member).PropertyType;
                curValue = m;
            }
            var axis = Expression.Parameter(typeof(IAxisManager));
            Expression prop = Expression.Convert(curValue, typeof(object));
            if (curValue is UnaryExpression { NodeType: ExpressionType.Convert } unary && unary.Operand == parameter) {
                prop = parameter;
            }
            var axisvalue = Expression.Call(axis, typeof(IAxisManager).GetMethod(nameof(IAxisManager.TranslateToAxisValue)), prop);

            var axistype = typeof(IAxisManager<>).MakeGenericType(curType);
            var castedaxis = Expression.TypeAs(axis, axistype);
            var castedaxisvalue = Expression.Call(castedaxis, axistype.GetMethod(nameof(IAxisManager<object>.TranslateToAxisValue)), curValue);

            var val = Expression.Condition(
                Expression.Equal(castedaxis, Expression.Constant(null)),
                axisvalue,
                castedaxisvalue);
            return Expression.Lambda<Func<object, IAxisManager, AxisValue>>(val, parameter, axis);
        }

        public static Expression<Func<object, object>> GetPropertyGetterExpression(Type type, string property) {
            var properties = property == string.Empty ? [] : property.Split('.');
            var parameter = Expression.Parameter(typeof(object));
            var curType = type;
            Expression curValue = parameter;
            foreach (var p in properties) {
                var v = Expression.Convert(curValue, curType);
                var m = Expression.Property(v, p);
                curType = ((PropertyInfo)m.Member).PropertyType;
                curValue = m;
            }
            var prop = Expression.Convert(curValue, typeof(object));
            return Expression.Lambda<Func<object, object>>(prop, parameter);
        }

        public static bool ValidatePropertyString(Type type, string? property) {
            if (property is null) {
                return false;
            }
            if (property == string.Empty) {
                return true;
            }
            var properties = property.Split('.');
            foreach (var p in properties) {
                var prop = type?.GetProperty(p);
                if (prop is null) {
                    return false;
                }
                type = prop.PropertyType;
            }
            return true;
        }
    }
}
