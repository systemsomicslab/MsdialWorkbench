using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

namespace CompMs.App.SpectrumViewer.View
{
    public class AddItemTemplateSelector : DataTemplateSelector
    {
        public DataTemplate ContentTemplate { get; set; }
        public DataTemplate NewItemTemplate { get; set; }

        public override DataTemplate SelectTemplate(object item, DependencyObject container) {
            if (item == CollectionView.NewItemPlaceholder) {
                return NewItemTemplate;
            }
            else {
                return ContentTemplate;
            }
        }
    }
}
