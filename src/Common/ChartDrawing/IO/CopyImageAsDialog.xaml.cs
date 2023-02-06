using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    /// <summary>
    /// Interaction logic for CopyImageAsDialog.xaml
    /// </summary>
    public partial class CopyImageAsDialog : System.Windows.Window
    {
        public CopyImageAsDialog() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, Click_close));
        }

        private void Click_close(object sender, System.Windows.RoutedEventArgs e) {
            Close();
        }
    }
}
