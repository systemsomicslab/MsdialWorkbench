using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>
    /// The strict JSON reader of rule files (design 3.1). It accepts exactly what the Python reference
    /// (<c>rule_format.load_strict</c>: CPython's json with the number, pair and constant hooks) accepts, and
    /// reports each refusal with the same code.
    /// </summary>
    /// <remarks>
    /// <para>The input is UTF-8 without a byte-order mark. The grammar is RFC 8259 as CPython's scanner applies it:
    /// whitespace is SP, HT, LF and CR only, a raw control character in a string is a syntax error, and the
    /// number lexeme is delimited exactly as CPython delimits it (an exponent without digits is backtracked).</para>
    /// <para>Errors are raised in document order, as CPython raises them: a number is checked when its lexeme ends,
    /// NaN and Infinity when they are read, duplicate keys when their object closes, and a syntax error where it
    /// is found. Nesting, the top-level type and printable ASCII are checked after the parse, in that order. The
    /// parser keeps its own stack, so no depth can overflow the call stack; CPython refuses very deep input with
    /// a RecursionError, which the reference also reports as nesting_depth. Precedence between two defects in one
    /// document is defined only where CPython's order is (design 3.1).</para>
    /// </remarks>
    public static class StrictJsonReader
    {
        public const int MaxDepth = 128;

        /// <summary>Reads a rule file: the top level must be an object.</summary>
        public static JsonObject Read(byte[] bytes) => (JsonObject)ReadValue(bytes, allowArray: false);

        /// <summary>The same reader, also accepting a top-level array: draft staging files only, which are never
        /// hashed (the reference's <c>load_strict_staging</c>).</summary>
        internal static JsonValue ReadStaging(byte[] bytes) => ReadValue(bytes, allowArray: true);

        private static JsonValue ReadValue(byte[] bytes, bool allowArray) {
            if (bytes is null) {
                throw new System.ArgumentNullException(nameof(bytes));
            }
            if (bytes.Length >= 3 && bytes[0] == 0xEF && bytes[1] == 0xBB && bytes[2] == 0xBF) {
                throw new StrictJsonException(RuleErrorCodes.Bom);
            }
            var text = DecodeUtf8(bytes);
            var value = new Parser(text).ParseDocument();
            if (!(value is JsonObject || (allowArray && value is JsonArray))) {
                throw new StrictJsonException(RuleErrorCodes.TopLevelNotObject);
            }
            if (Depth(value) > MaxDepth) {
                throw new StrictJsonException(RuleErrorCodes.NestingDepth);
            }
            CheckPrintableAscii(value);
            return value;
        }

        /// <summary>Strict UTF-8 as Python's codec decodes it: no overlong forms, surrogates or code points above U+10FFFF.</summary>
        internal static string DecodeUtf8(byte[] bytes) {
            var sb = new StringBuilder(bytes.Length);
            var i = 0;
            while (i < bytes.Length) {
                int b0 = bytes[i];
                if (b0 < 0x80) {
                    sb.Append((char)b0);
                    i++;
                    continue;
                }
                int need, cp, min;
                if (b0 >= 0xC2 && b0 <= 0xDF) {
                    need = 1; cp = b0 & 0x1F; min = 0x80;
                }
                else if (b0 >= 0xE0 && b0 <= 0xEF) {
                    need = 2; cp = b0 & 0x0F; min = 0x800;
                }
                else if (b0 >= 0xF0 && b0 <= 0xF4) {
                    need = 3; cp = b0 & 0x07; min = 0x10000;
                }
                else {
                    throw new StrictJsonException(RuleErrorCodes.InvalidUtf8);
                }
                if (i + need >= bytes.Length) {
                    throw new StrictJsonException(RuleErrorCodes.InvalidUtf8); // truncated sequence
                }
                for (var j = 1; j <= need; j++) {
                    int b = bytes[i + j];
                    if ((b & 0xC0) != 0x80) {
                        throw new StrictJsonException(RuleErrorCodes.InvalidUtf8);
                    }
                    cp = (cp << 6) | (b & 0x3F);
                }
                if (cp < min || cp > 0x10FFFF || (cp >= 0xD800 && cp <= 0xDFFF)) {
                    throw new StrictJsonException(RuleErrorCodes.InvalidUtf8);
                }
                if (cp >= 0x10000) {
                    cp -= 0x10000;
                    sb.Append((char)(0xD800 + (cp >> 10)));
                    sb.Append((char)(0xDC00 + (cp & 0x3FF)));
                }
                else {
                    sb.Append((char)cp);
                }
                i += need + 1;
            }
            return sb.ToString();
        }

        /// <summary>The nesting depth of arrays and objects (a scalar is 0, an empty object 1).</summary>
        internal static int Depth(JsonValue root) {
            var best = 0;
            var stack = new Stack<KeyValuePair<JsonValue, int>>();
            stack.Push(new KeyValuePair<JsonValue, int>(root, 1));
            while (stack.Count > 0) {
                var top = stack.Pop();
                if (top.Key is JsonObject o) {
                    if (top.Value > best) {
                        best = top.Value;
                    }
                    foreach (var m in o.Members) {
                        stack.Push(new KeyValuePair<JsonValue, int>(m.Value, top.Value + 1));
                    }
                }
                else if (top.Key is JsonArray a) {
                    if (top.Value > best) {
                        best = top.Value;
                    }
                    foreach (var item in a) {
                        stack.Push(new KeyValuePair<JsonValue, int>(item, top.Value + 1));
                    }
                }
            }
            return best;
        }

        /// <summary>
        /// Every key and string is printable ASCII (0x20-0x7E): depth first, in document order, each key before its
        /// value; the first offending code unit decides (below 0x20 or 0x7F: control_char; above: non_ascii).
        /// </summary>
        internal static void CheckPrintableAscii(JsonValue root) {
            // Explicit stack, pushed in reverse so that pops follow document order.
            var stack = new Stack<object>();
            stack.Push(root);
            while (stack.Count > 0) {
                var top = stack.Pop();
                if (top is string s) {
                    CheckString(s);
                }
                else if (top is JsonString js) {
                    CheckString(js.Value);
                }
                else if (top is JsonObject o) {
                    for (var i = o.Members.Count - 1; i >= 0; i--) {
                        stack.Push(o.Members[i].Value);
                        stack.Push(o.Members[i].Key);
                    }
                }
                else if (top is JsonArray a) {
                    for (var i = a.Count - 1; i >= 0; i--) {
                        stack.Push(a[i]);
                    }
                }
            }
        }

        private static void CheckString(string s) {
            foreach (var ch in s) {
                if (ch < 0x20 || ch == 0x7F) {
                    throw new StrictJsonException(RuleErrorCodes.ControlChar);
                }
                if (ch > 0x7F) {
                    throw new StrictJsonException(RuleErrorCodes.NonAscii);
                }
            }
        }

        private sealed class Frame
        {
            public Frame(bool isObject) {
                IsObject = isObject;
            }

            public bool IsObject { get; }
            public List<KeyValuePair<string, JsonValue>> Members { get; } = new List<KeyValuePair<string, JsonValue>>();
            public List<JsonValue> Items { get; } = new List<JsonValue>();
            public string? PendingKey { get; set; }
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) {
                _s = s;
            }

            private static StrictJsonException Syntax() => new StrictJsonException(RuleErrorCodes.Syntax);

            private bool AtEnd => _i >= _s.Length;

            private char Peek() => _i < _s.Length ? _s[_i] : '\0';

            private void SkipWhitespace() {
                while (_i < _s.Length) {
                    var c = _s[_i];
                    if (c == ' ' || c == '\t' || c == '\n' || c == '\r') {
                        _i++;
                    }
                    else {
                        break;
                    }
                }
            }

            public JsonValue ParseDocument() {
                SkipWhitespace();
                var value = ParseValue();
                SkipWhitespace();
                if (!AtEnd) {
                    throw Syntax(); // extra data
                }
                return value;
            }

            private JsonValue ParseValue() {
                var stack = new Stack<Frame>();
                while (true) {
                    // A value starts at _i (whitespace already skipped).
                    JsonValue value;
                    if (AtEnd) {
                        throw Syntax();
                    }
                    var c = _s[_i];
                    if (c == '{') {
                        _i++;
                        SkipWhitespace();
                        if (Peek() == '}' && !AtEnd) {
                            _i++;
                            value = JsonObject.Empty;
                        }
                        else {
                            var frame = new Frame(true);
                            stack.Push(frame);
                            ReadKeyAndColon(frame);
                            SkipWhitespace();
                            continue;
                        }
                    }
                    else if (c == '[') {
                        _i++;
                        SkipWhitespace();
                        if (Peek() == ']' && !AtEnd) {
                            _i++;
                            value = JsonArray.Empty;
                        }
                        else {
                            stack.Push(new Frame(false));
                            continue;
                        }
                    }
                    else {
                        value = ParseScalar();
                    }

                    // Attach the finished value to its container, closing containers as their ends are read.
                    while (true) {
                        if (stack.Count == 0) {
                            return value;
                        }
                        var frame = stack.Peek();
                        if (frame.IsObject) {
                            frame.Members.Add(new KeyValuePair<string, JsonValue>(frame.PendingKey!, value));
                        }
                        else {
                            frame.Items.Add(value);
                        }
                        SkipWhitespace();
                        if (AtEnd) {
                            throw Syntax();
                        }
                        var d = _s[_i];
                        if (d == ',') {
                            _i++;
                            SkipWhitespace();
                            if (frame.IsObject) {
                                ReadKeyAndColon(frame);
                                SkipWhitespace();
                            }
                            else if (Peek() == ']' && !AtEnd) {
                                throw Syntax(); // trailing comma
                            }
                            break; // parse the next value
                        }
                        if (frame.IsObject ? d == '}' : d == ']') {
                            _i++;
                            stack.Pop();
                            value = frame.IsObject ? CloseObject(frame) : new JsonArray(frame.Items);
                            continue;
                        }
                        throw Syntax();
                    }
                }
            }

            private static JsonObject CloseObject(Frame frame) {
                // CPython's object_pairs_hook runs when the object closes.
                var seen = new HashSet<string>(System.StringComparer.Ordinal);
                foreach (var m in frame.Members) {
                    if (!seen.Add(m.Key)) {
                        throw new StrictJsonException(RuleErrorCodes.DuplicateKey);
                    }
                }
                return new JsonObject(frame.Members);
            }

            private void ReadKeyAndColon(Frame frame) {
                if (AtEnd || _s[_i] != '"') {
                    throw Syntax(); // expecting a property name (this also refuses a trailing comma)
                }
                frame.PendingKey = ParseString();
                SkipWhitespace();
                if (AtEnd || _s[_i] != ':') {
                    throw Syntax();
                }
                _i++;
                SkipWhitespace();
            }

            private JsonValue ParseScalar() {
                var c = _s[_i];
                switch (c) {
                    case '"':
                        return new JsonString(ParseString());
                    case 'n':
                        if (Matches("null")) {
                            _i += 4;
                            return JsonNull.Instance;
                        }
                        break;
                    case 't':
                        if (Matches("true")) {
                            _i += 4;
                            return JsonBoolean.True;
                        }
                        break;
                    case 'f':
                        if (Matches("false")) {
                            _i += 5;
                            return JsonBoolean.False;
                        }
                        break;
                    case 'N':
                        if (Matches("NaN")) {
                            throw new StrictJsonException(RuleErrorCodes.Nonfinite);
                        }
                        break;
                    case 'I':
                        if (Matches("Infinity")) {
                            throw new StrictJsonException(RuleErrorCodes.Nonfinite);
                        }
                        break;
                    case '-':
                        if (Matches("-Infinity")) {
                            throw new StrictJsonException(RuleErrorCodes.Nonfinite);
                        }
                        break;
                }
                return ParseNumber();
            }

            private bool Matches(string literal) =>
                _i + literal.Length <= _s.Length && string.CompareOrdinal(_s, _i, literal, 0, literal.Length) == 0;

            private JsonValue ParseNumber() {
                // CPython's _match_number_unicode.
                var start = _i;
                var idx = _i;
                var end = _s.Length - 1;
                if (_s[idx] == '-') {
                    idx++;
                    if (idx > end) {
                        throw Syntax();
                    }
                }
                if (_s[idx] >= '1' && _s[idx] <= '9') {
                    idx++;
                    while (idx <= end && _s[idx] >= '0' && _s[idx] <= '9') {
                        idx++;
                    }
                }
                else if (_s[idx] == '0') {
                    idx++;
                }
                else {
                    throw Syntax();
                }
                var isFloat = false;
                if (idx < end && _s[idx] == '.' && _s[idx + 1] >= '0' && _s[idx + 1] <= '9') {
                    isFloat = true;
                    idx += 2;
                    while (idx <= end && _s[idx] >= '0' && _s[idx] <= '9') {
                        idx++;
                    }
                }
                if (idx < end && (_s[idx] == 'e' || _s[idx] == 'E')) {
                    var eStart = idx;
                    idx++;
                    if (idx < end && (_s[idx] == '-' || _s[idx] == '+')) {
                        idx++;
                    }
                    while (idx <= end && _s[idx] >= '0' && _s[idx] <= '9') {
                        idx++;
                    }
                    if (_s[idx - 1] >= '0' && _s[idx - 1] <= '9') {
                        isFloat = true;
                    }
                    else {
                        idx = eStart;
                    }
                }
                var lexeme = _s.Substring(start, idx - start);
                _i = idx;
                if (isFloat) {
                    return DecimalLexeme.ParseFloat(lexeme);
                }
                return new JsonInteger(DecimalLexeme.ParseInteger(lexeme));
            }

            private string ParseString() {
                // CPython's scanstring_unicode with strict=True; _i is at the opening quote.
                _i++;
                var sb = new StringBuilder();
                while (true) {
                    if (AtEnd) {
                        throw Syntax(); // unterminated string
                    }
                    var c = _s[_i];
                    if (c == '"') {
                        _i++;
                        return sb.ToString();
                    }
                    if (c == '\\') {
                        _i++;
                        if (AtEnd) {
                            throw Syntax();
                        }
                        var e = _s[_i];
                        switch (e) {
                            case '"': sb.Append('"'); _i++; break;
                            case '\\': sb.Append('\\'); _i++; break;
                            case '/': sb.Append('/'); _i++; break;
                            case 'b': sb.Append('\b'); _i++; break;
                            case 'f': sb.Append('\f'); _i++; break;
                            case 'n': sb.Append('\n'); _i++; break;
                            case 'r': sb.Append('\r'); _i++; break;
                            case 't': sb.Append('\t'); _i++; break;
                            case 'u':
                                _i++;
                                sb.Append((char)ReadHex4());
                                break;
                            default:
                                throw Syntax(); // invalid escape
                        }
                        continue;
                    }
                    if (c < 0x20) {
                        throw Syntax(); // invalid control character
                    }
                    sb.Append(c);
                    _i++;
                }
            }

            private int ReadHex4() {
                if (_i + 4 > _s.Length) {
                    throw Syntax();
                }
                var v = 0;
                for (var k = 0; k < 4; k++) {
                    var h = _s[_i + k];
                    int d;
                    if (h >= '0' && h <= '9') {
                        d = h - '0';
                    }
                    else if (h >= 'a' && h <= 'f') {
                        d = h - 'a' + 10;
                    }
                    else if (h >= 'A' && h <= 'F') {
                        d = h - 'A' + 10;
                    }
                    else {
                        throw Syntax();
                    }
                    v = v * 16 + d;
                }
                _i += 4;
                return v;
            }
        }
    }
}
