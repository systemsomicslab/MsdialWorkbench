using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace CompMs.Graphics.Template
{
    // from https://wpf.2000things.com/2014/02/19/1012-using-a-different-data-template-for-the-face-of-a-combobox/
    internal sealed class ComboBoxItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate SelectedTemplate { get; set; }
        public DataTemplate DropDownTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            return container is FrameworkElement fe && fe.TemplatedParent is ComboBox ? SelectedTemplate : DropDownTemplate;
        }
    }
}
