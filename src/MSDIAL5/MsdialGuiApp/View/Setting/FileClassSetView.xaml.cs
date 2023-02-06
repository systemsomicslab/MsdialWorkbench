using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for FileClassSetView.xaml
    /// </summary>
    public partial class FileClassSetView : UserControl
    {
        public FileClassSetView() {
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
