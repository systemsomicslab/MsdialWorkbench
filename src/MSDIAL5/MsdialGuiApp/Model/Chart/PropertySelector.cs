using System;
using System.Linq.Expressions;
using System.Reflection;

namespace CompMs.App.Msdial.Model.Chart
{
    public class PropertySelector<TSubject, TProperty>
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
    }
}
