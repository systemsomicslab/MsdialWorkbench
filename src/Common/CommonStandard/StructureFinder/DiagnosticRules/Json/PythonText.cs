using System.Globalization;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// Python's <c>str()</c> and <c>repr()</c> of a parsed JSON value. The Python reference detects repeated list
    /// entries with <c>len(set(map(str, values)))</c>, so the loader compares the same texts (for example 1 and
    /// "1" collide there). Strings of rule files are printable ASCII, so only the quote and backslash rules of
    /// repr matter.
    /// </summary>
    internal static class PythonText
    {
        public static string Str(JsonValue value) => value is JsonString s ? s.Value : Repr(value);

        public static string Repr(JsonValue value) {
            var sb = new StringBuilder();
            WriteRepr(sb, value);
            return sb.ToString();
        }

        private static void WriteRepr(StringBuilder sb, JsonValue value) {
            switch (value) {
                case JsonNull _:
                    sb.Append("None");
                    break;
                case JsonBoolean b:
                    sb.Append(b.Value ? "True" : "False");
                    break;
                case JsonInteger i:
                    sb.Append(i.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case JsonFloat f:
                    sb.Append(f.Canonical);
                    break;
                case JsonString s:
                    WriteStringRepr(sb, s.Value);
                    break;
                case JsonArray a:
                    sb.Append('[');
                    for (var k = 0; k < a.Count; k++) {
                        if (k > 0) {
                            sb.Append(", ");
                        }
                        WriteRepr(sb, a[k]);
                    }
                    sb.Append(']');
                    break;
                case JsonObject o:
                    sb.Append('{');
                    for (var k = 0; k < o.Members.Count; k++) {
                        if (k > 0) {
                            sb.Append(", ");
                        }
                        WriteStringRepr(sb, o.Members[k].Key);
                        sb.Append(": ");
                        WriteRepr(sb, o.Members[k].Value);
                    }
                    sb.Append('}');
                    break;
            }
        }

        private static void WriteStringRepr(StringBuilder sb, string s) {
            var quote = s.IndexOf('\'') >= 0 && s.IndexOf('"') < 0 ? '"' : '\'';
            sb.Append(quote);
            foreach (var c in s) {
                if (c == '\\' || c == quote) {
                    sb.Append('\\');
                }
                sb.Append(c);
            }
            sb.Append(quote);
        }
    }
}
