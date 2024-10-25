using CompMs.Graphics.Core.Base;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AxisPropertySelectors<T>(AxisItemSelector<T> axisItemSelector)
    {
        private readonly Dictionary<Type, PropertySelectorCollection<T>> _selectors = [];

        public AxisItemSelector<T> AxisItemSelector { get; } = axisItemSelector;

        public void Register<TSubject>(PropertySelector<TSubject, T> selector) {
            Register(typeof(TSubject), selector);
        }

        private void Register(Type subject, IPropertySelector<T> selector) {
            if (!_selectors.TryGetValue(subject, out var collection)) {
                _selectors[subject] = collection = new();
            }
            collection.Add(selector);
        }

        public T? Select<TSubject>(TSubject subject) {
            var selector = GetSelector<TSubject>();
            return selector is not null ? selector.Select(subject) : default;
        }

        public IPropertySelector<T>? GetSelector(Type subjectType) {
            if (_selectors.TryGetValue(subjectType, out var collection)) {
                return collection.SelectedSelector;
            }
            return default;
        }

        public IPropertySelector<T>? GetSelector<TSubject>() {
            return GetSelector(typeof(TSubject));
        }

        public static Builder CreateBuilder() {
            return new Builder();
        }

        public class Builder
        {
            private readonly List<(Type, IPropertySelector<T>)> _selectors = [];
            private readonly List<AxisItemModel<T>> _axisItems = [];

            public AxisPropertySelectors<T> Build() {
                var result = new AxisPropertySelectors<T>(new AxisItemSelector<T>(_axisItems.ToArray()));
                foreach (var (type, selector) in _selectors) {
                    result.Register(type, selector);
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
                _selectors.Add((typeof(TSubject), selector));
                return this;
            }
        }
    }
}
