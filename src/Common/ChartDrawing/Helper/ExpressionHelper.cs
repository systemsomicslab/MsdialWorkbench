using CompMs.Graphics.Core.Base;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CompMs.Graphics.Helper
{
    internal static class ExpressionHelper
    {
        public static Expression<Func<object, IAxisManager, AxisValue>> GetConvertToAxisValueExpression(Type type, string property) {
            var parameter = Expression.Parameter(typeof(object));
            var casted = Expression.Convert(parameter, type);
            var getter = Expression.Property(casted, property);
            var axis = Expression.Parameter(typeof(IAxisManager));

            var prop = Expression.Convert(getter, typeof(object));
            var axisvalue = Expression.Call(axis, typeof(IAxisManager).GetMethod(nameof(IAxisManager.TranslateToAxisValue)), prop);

            var axistype = typeof(IAxisManager<>).MakeGenericType(((PropertyInfo)getter.Member).PropertyType);
            var castedaxis = Expression.TypeAs(axis, axistype);
            var castedaxisvalue = Expression.Call(castedaxis, axistype.GetMethod(nameof(IAxisManager<object>.TranslateToAxisValue)), getter);

            var val = Expression.Condition(
                Expression.Equal(castedaxis, Expression.Constant(null)),
                axisvalue,
                castedaxisvalue);

            return Expression.Lambda<Func<object, IAxisManager, AxisValue>>(val, parameter, axis);
        }

        public static Expression<Func<object, object>> GetPropertyGetterExpression(Type type, string property) {
            var parameter = Expression.Parameter(typeof(object));
            var casted = Expression.Convert(parameter, type);
            var getter = Expression.Property(casted, property);
            var prop = Expression.Convert(getter, typeof(object));
            return Expression.Lambda<Func<object, object>>(prop, parameter);
        }
    }
}
