using System.Windows.Controls;

namespace ChartDrawingUiTest.UI
{
    /// <summary>
    /// Interaction logic for ResizableItemsControl.xaml
    /// </summary>
    public partial class ResizableItemsControl : Page
    {
        public ResizableItemsControl() {
            InitializeComponent();

            DataContext = new[] {1, 2, 3, 4, 5 };
        }
    }
}
