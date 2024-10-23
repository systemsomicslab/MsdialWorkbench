using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AxisPropertySelectors<T>(AxisItemSelector<T> axisItemSelector)
    {
        private readonly Dictionary<Type, IPropertySelector<T>> _selectors = [];

        public AxisItemSelector<T> AxisItemSelector { get; } = axisItemSelector;

        public void Register<TSubject>(PropertySelector<TSubject, T> selector) {
            _selectors[typeof(TSubject)] = selector;
        }

        private void Register(Type subject, IPropertySelector<T> selector) {
            _selectors[subject] = selector;
        }

        public T? Select<TSubject>(TSubject subject) {
            return _selectors.TryGetValue(typeof(TSubject), out var selector) ? selector.Select(subject) : default;
        }

        public IPropertySelector<T>? GetSelector(Type subjectType) {
            return _selectors.TryGetValue(subjectType, out var selector) ? selector : null;
        }

        public IPropertySelector<T>? GetSelector<TSubject>() {
            return _selectors.TryGetValue(typeof(TSubject), out var selector) ? selector : null;
        }

        public static Builder CreateBuilder() {
            return new Builder();
        }

        public class Builder
        {
            private readonly Dictionary<Type, IPropertySelector<T>> _selectors = [];
            private readonly List<AxisItemModel<T>> _axisItems = [];

            public AxisPropertySelectors<T> Build() {
                var result = new AxisPropertySelectors<T>(new AxisItemSelector<T>(_axisItems.ToArray()));
                foreach (var kvp in _selectors) {
                    result.Register(kvp.Key, kvp.Value);
                }
                return result;
            }

            public Builder Add(AxisItemModel<T> axisItem) {
                _axisItems.Add(axisItem);
                return this;
            }

            public Builder Add(IAxisManager<T> axis, string label, string title) {
                _axisItems.Add(new AxisItemModel<T>(label, axis, title));
                return this;
            }

            public Builder Register<TSubject>(PropertySelector<TSubject, T> selector) {
                _selectors[typeof(TSubject)] = selector;
                return this;
            }
        }
    }
}
