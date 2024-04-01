using CompMs.Graphics.Core.Base;
using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CompMs.Graphics.Helper;

    internal static class ExpressionHelper
    {
        public static Expression<Func<object, IAxisManager, AxisValue>> GetConvertToAxisValueExpression(Type type, string property) {
            var properties = property.Split('.');
            var parameter = Expression.Parameter(typeof(object));
            var curType = type;
            Expression curValue = parameter;
            foreach (var p in properties) {
                var v = Expression.Convert(curValue, curType);
                var m = Expression.Property(v, p);
                curType = ((PropertyInfo)m.Member).PropertyType;
                curValue = m;
            }
            var axis = Expression.Parameter(typeof(IAxisManager));
            var prop = Expression.Convert(curValue, typeof(object));
            var axisvalue = Expression.Call(axis, typeof(IAxisManager).GetMethod(nameof(IAxisManager.TranslateToAxisValue)), prop);

            var axistype = typeof(IAxisManager<>).MakeGenericType(((PropertyInfo)((MemberExpression)curValue).Member).PropertyType);
            var castedaxis = Expression.TypeAs(axis, axistype);
            var castedaxisvalue = Expression.Call(castedaxis, axistype.GetMethod(nameof(IAxisManager<object>.TranslateToAxisValue)), curValue);

            var val = Expression.Condition(
                Expression.Equal(castedaxis, Expression.Constant(null)),
                axisvalue,
                castedaxisvalue);
            return Expression.Lambda<Func<object, IAxisManager, AxisValue>>(val, parameter, axis);
        }

        public static Expression<Func<object, object>> GetPropertyGetterExpression(Type type, string property) {
            var properties = property.Split('.');
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

    /// <summary>
    /// Generates an array of <see cref="LambdaExpression"/> objects representing getter expressions for the specified property or nested properties of a given type.
    /// </summary>
    /// <remarks>
    /// This method allows for accessing nested properties by specifying a dot-separated string of property names (e.g., "Property.SubProperty").
    /// Each <see cref="LambdaExpression"/> in the resulting array corresponds to a getter for the respective property in the chain, starting from the root type.
    /// </remarks>
    /// <param name="type">The root type from which the property getters should be created. This type must not be null.</param>
    /// <param name="property">A dot-separated string representing the property or nested properties to access. Each property in the chain must exist and be accessible from the previous one.</param>
    /// <returns>An array of <see cref="LambdaExpression"/> objects, each representing a getter for the specified property or one of the nested properties. The length of the array matches the number of properties specified.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="property"/> is null, empty, or if any property in the chain does not exist on the expected type.</exception>
    public static LambdaExpression[] GetPropertyGetterExpressions(Type type, string property) {
        if (type is null) {
            throw new ArgumentNullException(nameof(type));
        }

        var properties = property.Split('.');
        var result = new LambdaExpression[properties.Length];
        var parameter = Expression.Parameter(type);
        Expression curValue = parameter;
        for (int i = 0; i < properties.Length; i++) {
            curValue = Expression.Property(curValue, properties[i]);
            result[i] = Expression.Lambda(curValue, parameter);
        }
        return result;
    }

        public static bool ValidatePropertyString(Type type, string property) {
            if (string.IsNullOrEmpty(property)) {
                return false;
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
