using System.Windows;

namespace CompMs.CommonMVVM
{
    // from https://thomaslevesque.com/2011/03/21/wpf-how-to-bind-to-data-when-the-datacontext-is-not-inherited/
    public sealed class BindingProxy : Freezable
    {
        protected override Freezable CreateInstanceCore() {
            return new BindingProxy();
        }

        public static readonly DependencyProperty DataProperty =
            DependencyProperty.Register(
                nameof(Data),
                typeof(object),
                typeof(BindingProxy),
                new PropertyMetadata(null));

        public object Data {
            get => GetValue(DataProperty);
            set => SetValue(DataProperty, value);
        }
    }
}
