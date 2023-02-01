using System.Runtime.Serialization;

namespace NCDK.Common.Serialization
{
    internal static class SerializationTool
    {
        public static void AddNullableValue<T>(this SerializationInfo info, string name, T? value) where T : struct
        {
            if (value != null)
                info.AddValue(name, value);
        }

        public static T? GetNullable<T>(this SerializationInfo info, string name) where T : struct
        {
            T v;
            try
            {
                v = (T)info.GetValue(nameof(name), typeof(T));
            }
            catch (SerializationException)
            {
                return null;
            }
            return v;
        }
    }
}
