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

    /// <summary>
    /// Creates a lambda expression that converts a given object to an axis value using an <see cref="IAxisManager"/>, based on a specified getter expression.
    /// </summary>
    /// <remarks>
    /// This method dynamically generates a lambda expression capable of invoking a specific <c>TranslateToAxisValue</c> method on the <see cref="IAxisManager"/> interface. It incorporates type checking and conditional logic to ensure that the correct translation method is called, whether generic or non-generic, depending on the runtime type of the <see cref="IAxisManager"/> and the return type of the <paramref name="getter"/> expression.
    /// If the <paramref name="getter"/>'s return type matches the type parameter of a generic <see cref="IAxisManager{T}"/> implementation provided at runtime, this specific implementation's <c>TranslateToAxisValue</c> method is invoked. Otherwise, the method falls back to the non-generic <c>TranslateToAxisValue</c> method.
    /// If the conversion is not possible due to a type mismatch, the method returns <see cref="AxisValue.NaN"/>.
    /// </remarks>
    /// <param name="getter">A lambda expression representing a getter for the value to be converted. The expression should take a single parameter of any type and return a value. This value is then converted to an axis value by the provided <see cref="IAxisManager"/>.</param>
    /// <returns>A lambda expression of type <see cref="Func{Object, IAxisManager, AxisValue}"/>. This expression takes an object and an <see cref="IAxisManager"/> as inputs and returns an <see cref="AxisValue"/> representing the axis value obtained by translating the input object's value as determined by the <paramref name="getter"/> expression.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="getter"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the <paramref name="getter"/> does not represent a valid lambda expression with exactly one input parameter.</exception>
    public static Expression<Func<object, IAxisManager, AxisValue>> GetConvertToAxisValueExpression(LambdaExpression getter) {
        if (getter is null) {
            throw new ArgumentNullException(nameof(getter));
        }
        if (getter.Parameters is { Count: not 1}) {
            throw new ArgumentException(nameof(getter));
        }

        var parameter = Expression.Parameter(typeof(object));
        var converted = Expression.Convert(parameter, getter.Parameters[0].Type);
        var returnValue = Expression.Invoke(getter, converted);

        var axis = Expression.Parameter(typeof(IAxisManager));

        var value = Expression.Convert(returnValue, typeof(object));
        var axisvalue = Expression.Call(axis, typeof(IAxisManager).GetMethod(nameof(IAxisManager.TranslateToAxisValue)), value);

        var axistype = typeof(IAxisManager<>).MakeGenericType(getter.ReturnType);
        var castedaxis = Expression.TypeAs(axis, axistype);
        var castedaxisvalue = Expression.Call(castedaxis, axistype.GetMethod(nameof(IAxisManager<object>.TranslateToAxisValue)), returnValue);

        var val = Expression.Condition(
            Expression.TypeIs(parameter, converted.Type),
            Expression.Condition(
                Expression.TypeIs(axis, axistype),
                castedaxisvalue,
                axisvalue),
            Expression.Constant(AxisValue.NaN)
        );
        return Expression.Lambda<Func<object, IAxisManager, AxisValue>>(val, parameter, axis);
    }

    /// <summary>
    /// Creates a lambda expression representing an identity function for a specified type.
    /// </summary>
    /// <remarks>
    /// An identity function is a function that returns its input. This method generates a lambda expression
    /// that takes a single parameter of the specified <paramref name="type"/> and returns it directly.
    /// This can be useful in scenarios where a generic lambda expression is needed for passing through values
    /// of a specific type without modification.
    /// </remarks>
    /// <param name="type">The type of the parameter and return value of the lambda expression. This type must not be null.</param>
    /// <returns>A <see cref="LambdaExpression"/> that represents an identity function for the specified type.
    /// The expression takes a single parameter of the specified <paramref name="type"/> and returns it.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="type"/> is null.</exception>
    public static LambdaExpression Identity(Type type) {
        var parameter = Expression.Parameter(type);
        return Expression.Lambda(parameter, parameter);
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
