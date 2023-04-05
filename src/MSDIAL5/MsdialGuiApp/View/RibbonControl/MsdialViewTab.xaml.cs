using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.RibbonControl
{
    /// <summary>
    /// Interaction logic for MsdialViewTab.xaml
    /// </summary>
    public partial class MsdialViewTab : RibbonTab
    {
        public MsdialViewTab()
        {
            InitializeComponent();
        }

        private void ClickHandled(object sender, RoutedEventArgs e) {
            //e.Handled = true;
            //((sender as FrameworkElement)?.Tag as ICommand)?.Execute((sender as MenuItem).CommandParameter);
        }
    }
}
