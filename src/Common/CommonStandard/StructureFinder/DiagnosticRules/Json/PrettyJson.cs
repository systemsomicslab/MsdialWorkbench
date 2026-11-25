using System;
using System.Globalization;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// The storage form of rule files (design 3.3): Python's <c>json.dumps(o, sort_keys=True, indent=1,
    /// ensure_ascii=True) + "\n"</c>, which is what <c>rule_format.py</c> writes. It is a lint target, not a load rule.
    /// </summary>
    public static class PrettyJson
    {
        public static byte[] ToBytes(JsonValue value) => Encoding.ASCII.GetBytes(ToText(value));

        public static string ToText(JsonValue value) {
            var sb = new StringBuilder();
            Write(sb, value, 0);
            sb.Append('\n');
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, JsonValue value, int level) {
            switch (value) {
                case JsonArray a:
                    if (a.Count == 0) {
                        sb.Append("[]");
                        return;
                    }
                    sb.Append('[');
                    for (var k = 0; k < a.Count; k++) {
                        if (k > 0) {
                            sb.Append(',');
                        }
                        NewLine(sb, level + 1);
                        Write(sb, a[k], level + 1);
                    }
                    NewLine(sb, level);
                    sb.Append(']');
                    return;
                case JsonObject o:
                    if (o.Count == 0) {
                        sb.Append("{}");
                        return;
                    }
                    sb.Append('{');
                    var first = true;
                    foreach (var m in CanonicalJson.SortedMembers(o)) {
                        if (!first) {
                            sb.Append(',');
                        }
                        first = false;
                        NewLine(sb, level + 1);
                        CanonicalJson.WriteString(sb, m.Key);
                        sb.Append(": ");
                        Write(sb, m.Value, level + 1);
                    }
                    NewLine(sb, level);
                    sb.Append('}');
                    return;
                default:
                    CanonicalJson.Write(sb, value);
                    return;
            }
        }

        private static void NewLine(StringBuilder sb, int level) {
            sb.Append('\n');
            sb.Append(' ', level);
        }
    }
}
