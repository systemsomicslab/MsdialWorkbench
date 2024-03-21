using System.Windows.Controls;

namespace ChartDrawingUiTest.UI
{
    /// <summary>
    /// Interaction logic for DockItemsControl.xaml
    /// </summary>
    public partial class DockItemsControl : Page
    {
        public DockItemsControl() {
            InitializeComponent();

            DataContext = new[] { 1, 2, 3, 4, 5, };
        }
    }
}
