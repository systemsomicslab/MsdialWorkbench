using System;
using System.Collections;
using System.Collections.Generic;

namespace CompMs.Common.StructureFinder.DiagnosticRules.Json
{
    /// <summary>The JSON types of the rule-file subset. Integers and floats are separate, as in Python's json.</summary>
    public enum JsonKind
    {
        Null,
        Boolean,
        Integer,
        Float,
        String,
        Array,
        Object,
    }

    /// <summary>
    /// An immutable JSON value as <see cref="StrictJsonReader"/> reads it: objects keep their document key order
    /// (canonical output sorts them), integers are <see cref="long"/> and floats keep their canonical text.
    /// </summary>
    public abstract class JsonValue
    {
        internal JsonValue() { }

        public abstract JsonKind Kind { get; }

        public static JsonValue Null => JsonNull.Instance;

        public static JsonValue Of(bool value) => value ? JsonBoolean.True : JsonBoolean.False;

        public static JsonValue Of(long value) => new JsonInteger(value);

        public static JsonValue Of(string value) => new JsonString(value);

        /// <summary>A string, or <see cref="Null"/> for a null reference.</summary>
        public static JsonValue OfNullable(string? value) => value is null ? JsonNull.Instance : new JsonString(value);
    }

    public sealed class JsonNull : JsonValue
    {
        public static readonly JsonNull Instance = new JsonNull();

        private JsonNull() { }

        public override JsonKind Kind => JsonKind.Null;
    }

    public sealed class JsonBoolean : JsonValue
    {
        public static readonly JsonBoolean True = new JsonBoolean(true);
        public static readonly JsonBoolean False = new JsonBoolean(false);

        private JsonBoolean(bool value) {
            Value = value;
        }

        public bool Value { get; }

        public override JsonKind Kind => JsonKind.Boolean;
    }

    /// <summary>An integer lexeme: |value| &lt;= 2^53 - 1 when read from a file.</summary>
    public sealed class JsonInteger : JsonValue
    {
        public JsonInteger(long value) {
            Value = value;
        }

        public long Value { get; }

        public override JsonKind Kind => JsonKind.Integer;
    }

    /// <summary>
    /// A float lexeme of the number subset (design 3.1). <see cref="Canonical"/> is the lexeme with trailing
    /// fraction zeros stripped (".0" kept), which equals Python's repr of the value. The value is
    /// <see cref="Mantissa"/> / 10^<see cref="Scale"/>, converted by one correctly rounded IEEE division.
    /// </summary>
    public sealed class JsonFloat : JsonValue
    {
        internal JsonFloat(double value, string canonical, long mantissa, int scale) {
            Value = value;
            Canonical = canonical;
            Mantissa = mantissa;
            Scale = scale;
        }

        public double Value { get; }

        public string Canonical { get; }

        /// <summary>The signed digit integer of the stripped lexeme.</summary>
        public long Mantissa { get; }

        /// <summary>The number of fraction digits of the stripped lexeme.</summary>
        public int Scale { get; }

        public override JsonKind Kind => JsonKind.Float;

        /// <summary>Parses a float lexeme of the subset; throws <see cref="StrictJsonException"/> with its code.</summary>
        public static JsonFloat Parse(string lexeme) => DecimalLexeme.ParseFloat(lexeme);
    }

    public sealed class JsonString : JsonValue
    {
        public JsonString(string value) {
            Value = value ?? throw new ArgumentNullException(nameof(value));
        }

        public string Value { get; }

        public override JsonKind Kind => JsonKind.String;
    }

    public sealed class JsonArray : JsonValue, IReadOnlyList<JsonValue>
    {
        private readonly JsonValue[] _items;

        public JsonArray(IEnumerable<JsonValue> items) {
            var list = new List<JsonValue>();
            foreach (var item in items) {
                list.Add(item ?? throw new ArgumentException("null item", nameof(items)));
            }
            _items = list.ToArray();
        }

        public static readonly JsonArray Empty = new JsonArray(new JsonValue[0]);

        public override JsonKind Kind => JsonKind.Array;

        public int Count => _items.Length;

        public JsonValue this[int index] => _items[index];

        public IEnumerator<JsonValue> GetEnumerator() => ((IEnumerable<JsonValue>)_items).GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => _items.GetEnumerator();
    }

    /// <summary>An object with unique keys in document order.</summary>
    public sealed class JsonObject : JsonValue
    {
        private readonly KeyValuePair<string, JsonValue>[] _members;
        private readonly Dictionary<string, JsonValue> _byKey;

        public JsonObject(IEnumerable<KeyValuePair<string, JsonValue>> members) {
            var list = new List<KeyValuePair<string, JsonValue>>();
            _byKey = new Dictionary<string, JsonValue>(StringComparer.Ordinal);
            foreach (var m in members) {
                if (m.Key is null || m.Value is null) {
                    throw new ArgumentException("null key or value", nameof(members));
                }
                if (_byKey.ContainsKey(m.Key)) {
                    throw new ArgumentException("duplicate key", nameof(members));
                }
                _byKey.Add(m.Key, m.Value);
                list.Add(m);
            }
            _members = list.ToArray();
        }

        public static readonly JsonObject Empty = new JsonObject(new KeyValuePair<string, JsonValue>[0]);

        public override JsonKind Kind => JsonKind.Object;

        /// <summary>Members in document (insertion) order.</summary>
        public IReadOnlyList<KeyValuePair<string, JsonValue>> Members => _members;

        public int Count => _members.Length;

        public IEnumerable<string> Keys {
            get {
                foreach (var m in _members) {
                    yield return m.Key;
                }
            }
        }

        public bool ContainsKey(string key) => _byKey.ContainsKey(key);

        public bool TryGetValue(string key, out JsonValue value) => _byKey.TryGetValue(key, out value);

        /// <summary>The value of a key, or null (a C# null, not <see cref="JsonNull"/>) when the key is absent.</summary>
        public JsonValue? Get(string key) => _byKey.TryGetValue(key, out var v) ? v : null;

        public JsonValue this[string key] => _byKey[key];

        /// <summary>A copy without the given keys, in the same order.</summary>
        public JsonObject Without(params string[] keys) {
            var drop = new HashSet<string>(keys, StringComparer.Ordinal);
            var kept = new List<KeyValuePair<string, JsonValue>>();
            foreach (var m in _members) {
                if (!drop.Contains(m.Key)) {
                    kept.Add(m);
                }
            }
            return new JsonObject(kept);
        }
    }
}
