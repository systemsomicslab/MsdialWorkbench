using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Statistics
{
    /// <summary>
    /// Interaction logic for NormalizationInternalStandardSetView.xaml
    /// </summary>
    public partial class NormalizationInternalStandardSetView : UserControl
    {
        public NormalizationInternalStandardSetView() {
            InitializeComponent();
        }

        private void FinishClose(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                window.DialogResult = true;
            }
            window.Close();
        }

        private void CancelClose(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            if (System.Windows.Interop.ComponentDispatcher.IsThreadModal) {
                window.DialogResult = false;
            }
            window.Close();
        }
    }
}
