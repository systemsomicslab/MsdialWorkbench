using System.Globalization;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>RFC 6901 JSON pointers, as the Python reference's <c>jp</c> builds them.</summary>
    public static class Pointers
    {
        public static string Child(string pointer, string key) =>
            pointer + "/" + key.Replace("~", "~0").Replace("/", "~1");

        public static string Child(string pointer, int index) =>
            pointer + "/" + index.ToString(CultureInfo.InvariantCulture);

        public static string Child(string pointer, string key, int index) => Child(Child(pointer, key), index);

        public static string Child(string pointer, string key1, string key2) => Child(Child(pointer, key1), key2);
    }
}
