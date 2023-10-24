using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Chart
{
    internal sealed class AxisPropertySelectors<T>
    {
        private readonly Dictionary<Type, IPropertySelector<T>> _selectors;

        public AxisPropertySelectors(AxisItemSelector<T> axisItemSelector) {
            _selectors = new Dictionary<Type, IPropertySelector<T>>();
            AxisItemSelector = axisItemSelector;
        }

        public AxisItemSelector<T> AxisItemSelector { get; }

        public void Register<TSubject>(PropertySelector<TSubject, T> selector) {
            _selectors[typeof(TSubject)] = selector;
        }

        public T? Select<TSubject>(TSubject subject) {
            return _selectors.TryGetValue(typeof(TSubject), out var selector) ? selector.Select(subject) : default;
        }

        public IPropertySelector<T>? GetSelector(Type subjectType) {
            return _selectors.TryGetValue(subjectType, out var selector) ? selector : null;
        }
    }
}
