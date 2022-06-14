using Microsoft.Win32;
using System.Windows.Input;

namespace CompMs.Graphics.IO
{
    /// <summary>
    /// Interaction logic for SaveImageAsDialog.xaml
    /// </summary>
    public partial class SaveImageAsDialog : System.Windows.Window
    {
        public SaveImageAsDialog() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, Click_close));
            CommandBindings.Add(new CommandBinding(ApplicationCommands.SaveAs, Select_export_file));
        }

        private void Click_close(object sender, System.Windows.RoutedEventArgs e) {
            Close();
        }

        private void Select_export_file(object sender, System.Windows.RoutedEventArgs e) {
            var sfd = new SaveFileDialog
            {
                RestoreDirectory = true,
                DefaultExt = ".emf",
                Filter = string.Join("|", new[]
                {
                    "Extended Metafile Format(.emf)|*.emf",
                    "PNG image(.png)|*.png",
                }),
            };
            if (sfd.ShowDialog() == true) {
                ExportPath_TextBox.Text = sfd.FileName;
            }
        }
    }
}
