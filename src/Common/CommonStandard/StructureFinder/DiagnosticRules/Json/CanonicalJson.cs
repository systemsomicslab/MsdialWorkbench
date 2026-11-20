using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// Canonical JSON (design 3.2): Python's <c>json.dumps(o, sort_keys=True, separators=(",", ":"),
    /// ensure_ascii=True, allow_nan=False)</c>, byte for byte.
    /// </summary>
    /// <remarks>
    /// Keys sort by Unicode code point, which is Python's string order (and ordinal order for the ASCII keys of
    /// rule files). Strings are escaped as Python's ensure_ascii does, per UTF-16 code unit: <c>\"</c>,
    /// <c>\\</c>, the five short escapes, and <c>\u</c> with four lowercase hex digits for every other unit
    /// below 0x20 or from 0x7F up, so a non-BMP character is written as its surrogate pair. Integers are written
    /// with the invariant culture and floats from their canonical lexeme.
    /// </remarks>
    public static class CanonicalJson
    {
        /// <summary>Compares strings by Unicode code point (Python's str order).</summary>
        public static readonly IComparer<string> CodePointOrder = new CodePointComparer();

        public static byte[] ToBytes(JsonValue value) => Encoding.ASCII.GetBytes(ToText(value));

        public static string ToText(JsonValue value) {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        public static void Write(StringBuilder sb, JsonValue value) {
            switch (value) {
                case JsonNull _:
                    sb.Append("null");
                    break;
                case JsonBoolean b:
                    sb.Append(b.Value ? "true" : "false");
                    break;
                case JsonInteger i:
                    sb.Append(i.Value.ToString(CultureInfo.InvariantCulture));
                    break;
                case JsonFloat f:
                    sb.Append(f.Canonical);
                    break;
                case JsonString s:
                    WriteString(sb, s.Value);
                    break;
                case JsonArray a:
                    sb.Append('[');
                    for (var k = 0; k < a.Count; k++) {
                        if (k > 0) {
                            sb.Append(',');
                        }
                        Write(sb, a[k]);
                    }
                    sb.Append(']');
                    break;
                case JsonObject o:
                    sb.Append('{');
                    var first = true;
                    foreach (var m in SortedMembers(o)) {
                        if (!first) {
                            sb.Append(',');
                        }
                        first = false;
                        WriteString(sb, m.Key);
                        sb.Append(':');
                        Write(sb, m.Value);
                    }
                    sb.Append('}');
                    break;
                default:
                    throw new ArgumentException("unknown JSON value", nameof(value));
            }
        }

        /// <summary>A string literal as Python's <c>json.dumps(s, ensure_ascii=True)</c> writes it, quotes included.</summary>
        public static string EscapeString(string s) {
            var sb = new StringBuilder(s.Length + 2);
            WriteString(sb, s);
            return sb.ToString();
        }

        public static void WriteString(StringBuilder sb, string s) {
            sb.Append('"');
            foreach (var c in s) {
                switch (c) {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default:
                        if (c < 0x20 || c > 0x7E) {
                            sb.Append("\\u");
                            AppendHex4(sb, c);
                        }
                        else {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        internal static IEnumerable<KeyValuePair<string, JsonValue>> SortedMembers(JsonObject o) {
            var members = new List<KeyValuePair<string, JsonValue>>(o.Members);
            members.Sort((x, y) => CodePointOrder.Compare(x.Key, y.Key));
            return members;
        }

        private static void AppendHex4(StringBuilder sb, char c) {
            const string hex = "0123456789abcdef";
            sb.Append(hex[(c >> 12) & 0xF]);
            sb.Append(hex[(c >> 8) & 0xF]);
            sb.Append(hex[(c >> 4) & 0xF]);
            sb.Append(hex[c & 0xF]);
        }

        private sealed class CodePointComparer : IComparer<string>
        {
            public int Compare(string? x, string? y) {
                if (ReferenceEquals(x, y)) {
                    return 0;
                }
                if (x is null) {
                    return -1;
                }
                if (y is null) {
                    return 1;
                }
                var i = 0;
                var j = 0;
                while (i < x.Length && j < y.Length) {
                    var a = CodePointAt(x, ref i);
                    var b = CodePointAt(y, ref j);
                    if (a != b) {
                        return a < b ? -1 : 1;
                    }
                }
                if (i < x.Length) {
                    return 1;
                }
                return j < y.Length ? -1 : 0;
            }

            private static int CodePointAt(string s, ref int i) {
                var c = s[i];
                if (c >= 0xD800 && c <= 0xDBFF && i + 1 < s.Length && s[i + 1] >= 0xDC00 && s[i + 1] <= 0xDFFF) {
                    var cp = 0x10000 + ((c - 0xD800) << 10) + (s[i + 1] - 0xDC00);
                    i += 2;
                    return cp;
                }
                i++;
                return c;
            }
        }
    }
}
