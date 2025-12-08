using System;

namespace CompMs.Graphics.Helper;

internal static class PropertiesAccessor
{
    public static IPropertiesAccessor Build(Type? type, string? properties) {
        if (properties is null) {
            return IgnoreAccessor.Instance;
        }
        if (properties == string.Empty) {
            return IdentityAccessor.Instance;
        }
        if (ExpressionHelper.ValidatePropertyString(type, properties)) {
            return new TypedPropertiesAccessor(properties, type);
        }
        return new DynamicPropertiesAccessor(properties);   
    }
}
