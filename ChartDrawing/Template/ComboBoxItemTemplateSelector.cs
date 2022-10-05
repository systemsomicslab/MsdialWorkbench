using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CompMs.Graphics.Template
{
    // from https://meleak.wordpress.com/2012/05/13/different-combobox-itemtemplate-for-dropdown/
    internal sealed class ComboBoxItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectedTemplate { get; set; }
        public DataTemplate DropDownTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            var ancestor = container;
            while (!(ancestor is null) && !(ancestor is ComboBox) && !(ancestor is ComboBoxItem)) {
                ancestor = VisualTreeHelper.GetParent(ancestor);
            }
            return ancestor is ComboBoxItem
                ? DropDownTemplate
                : SelectedTemplate;
        }
    }
}
