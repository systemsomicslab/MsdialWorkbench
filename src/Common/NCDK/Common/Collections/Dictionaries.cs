using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace NCDK.Common.Collections
{
    public static class Dictionaries
    {
        public static IReadOnlyDictionary<TKey, TValue> Empty<TKey, TValue>() => EmptyDictioary<TKey, TValue>.Value;
    }

    public class EmptyDictioary<TKey, TValue>
    {
        public static IReadOnlyDictionary<TKey, TValue> Value { get; } = new ReadOnlyDictionary<TKey, TValue>(new Dictionary<TKey, TValue>(0));
    }
}
