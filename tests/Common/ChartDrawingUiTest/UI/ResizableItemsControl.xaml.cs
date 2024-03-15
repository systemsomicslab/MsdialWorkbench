using System.Collections.ObjectModel;
using System.Linq;
using System.Windows.Controls;

namespace ChartDrawingUiTest.UI
{
    /// <summary>
    /// Interaction logic for ResizableItemsControl.xaml
    /// </summary>
    public partial class ResizableItemsControl : Page
    {
        private ObservableCollection<int> _items;

        public ResizableItemsControl() {
            InitializeComponent();

            DataContext = _items = [1, 2, 3, 4, 5];
        }

        private void Add_Click(object sender, System.Windows.RoutedEventArgs e) {
            var button = (Button)sender;
            var index = (int)button.Tag;
            _items.Insert(index, _items.Max() + 1);
        }

        private void Remove_Click(object sender, System.Windows.RoutedEventArgs e) {
            var button = (Button)sender;
            var index = (int)button.Tag;
            _items.RemoveAt(index);
        }
    }
}
