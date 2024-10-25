using CompMs.CommonMVVM;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Chart;

public interface IPropertySelector<TProperty> {
    string Property { get; }

    TProperty? Select<T>(T subject);
}

public class PropertySelector<TSubject, TProperty> : IPropertySelector<TProperty>
{
    public PropertySelector(string property, Func<TSubject, TProperty> selector) {
        Property = property;
        Selector = selector;
    }

    public PropertySelector(Expression<Func<TSubject, TProperty>> propertySelector) {
        Property = GetPropertyName(propertySelector);
        Selector = propertySelector.Compile();
    }

    public string Property { get; }

    public Func<TSubject, TProperty> Selector { get; } 

    private string GetPropertyName(Expression<Func<TSubject, TProperty>> propertySelector) {
        if (propertySelector.Body is MemberExpression member) {
            if (member.Member is MemberInfo info) {
                return info.Name;
            }
        }
        throw new ArgumentException(nameof(propertySelector));
    }

    TProperty? IPropertySelector<TProperty>.Select<T>(T subject) {
        return subject is TSubject typed ? Selector(typed) : default;
    }

    public static implicit operator PropertySelector<TSubject, TProperty>(Expression<Func<TSubject, TProperty>> propertySelector) {
        return new PropertySelector<TSubject, TProperty>(propertySelector);
    }
}

public sealed class PropertySelectorCollection<TProperty> : BindableBase {
    private readonly ObservableCollection<IPropertySelector<TProperty>> _selectors = [];

    public PropertySelectorCollection()
    {
        Selectors = new ReadOnlyObservableCollection<IPropertySelector<TProperty>>(_selectors);
    }

    public ReadOnlyObservableCollection<IPropertySelector<TProperty>> Selectors { get; }

    public IPropertySelector<TProperty>? SelectedSelector {
        get => _selectedSelector;
        set => SetProperty(ref _selectedSelector, value);
    }
    private IPropertySelector<TProperty>? _selectedSelector;

    public void Add(IPropertySelector<TProperty> selector) {
        _selectors.Add(selector);
        SelectedSelector ??= selector;
    }

    public bool Remove(IPropertySelector<TProperty> selector) {
        var result = _selectors.Remove(selector);
        if (result && SelectedSelector == selector) {
            SelectedSelector = _selectors.FirstOrDefault();
        }
        return result;
    }
}
