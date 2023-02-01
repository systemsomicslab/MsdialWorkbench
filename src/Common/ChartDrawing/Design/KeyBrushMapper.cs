using CompMs.Graphics.Base;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Media;

namespace CompMs.Graphics.Design
{
    public class KeyBrushMapper<T, U> : BrushMapper<T>
    {
        public KeyBrushMapper(IReadOnlyDictionary<U, Brush> mapper, Func<T, U> func, Brush defaultValue) {
            this.mapper = mapper;
            this.func = func;
            this.defaultValue = defaultValue;
        }

        public KeyBrushMapper(IReadOnlyDictionary<U, Brush> mapper, Func<T, U> func) : this(mapper, func, null) {

        }

        public KeyBrushMapper(IReadOnlyDictionary<U, Color> mapper, Func<T, U> func, Color defaultValue) {
            this.mapper = mapper.ToDictionary(kvp => kvp.Key, kvp => (Brush)new SolidColorBrush(kvp.Value));
            foreach (var value in this.mapper.Values) {
                value.Freeze();
            }
            this.func = func;
            this.defaultValue = new SolidColorBrush(defaultValue);
            this.defaultValue.Freeze();
        }

        public KeyBrushMapper(IReadOnlyDictionary<U, Color> mapper, Func<T, U> func) {
            this.mapper = mapper.ToDictionary(kvp => kvp.Key, kvp => (Brush)new SolidColorBrush(kvp.Value));
            foreach (var value in this.mapper.Values) {
                value.Freeze();
            }
            this.func = func;
        }

        private readonly Func<T, U> func;
        private readonly IReadOnlyDictionary<U, Brush> mapper;
        private readonly Brush defaultValue;

        public override Brush Map(T key) {

            if (mapper.TryGetValue(func(key), out var brush)) {
                return brush;
            }
            return defaultValue ?? throw new ArgumentException($"Unknown key({key}) passed.");
        }
    }

    public class KeyBrushMapper<T> : BrushMapper<T>
    {
        public KeyBrushMapper(IReadOnlyDictionary<T, Brush> mapper) {
            this.mapper = mapper;
        }

        public KeyBrushMapper(IReadOnlyDictionary<T, Color> mapper) {
            this.mapper = mapper.ToDictionary(kvp => kvp.Key, kvp => (Brush)new SolidColorBrush(kvp.Value));
            foreach (var value in this.mapper.Values) {
                value.Freeze();
            }
        }
        public KeyBrushMapper(IReadOnlyDictionary<T, Brush> mapper, Brush defaultValue) {
            this.mapper = mapper;
            this.defaultValue = defaultValue;
        }

        public KeyBrushMapper(IReadOnlyDictionary<T, Color> mapper, Color defaultValue) {
            this.mapper = mapper.ToDictionary(kvp => kvp.Key, kvp => (Brush)new SolidColorBrush(kvp.Value));
            foreach (var value in this.mapper.Values) {
                value.Freeze();
            }
            this.defaultValue = new SolidColorBrush(defaultValue);
            this.defaultValue.Freeze();
        }
        private IReadOnlyDictionary<T, Brush> mapper;
        private Brush defaultValue;

        public override Brush Map(T key) {
            if (mapper.ContainsKey(key)) {
                return mapper[key];
            }
            return defaultValue ?? throw new ArgumentException($"Unknown key({key}) passed.");
        }
    }
}
