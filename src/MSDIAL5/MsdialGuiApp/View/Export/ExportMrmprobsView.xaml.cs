using Microsoft.Win32;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Export
{
    /// <summary>
    /// Interaction logic for ExportMrmprobsView.xaml
    /// </summary>
    public partial class ExportMrmprobsView : UserControl
    {
        public ExportMrmprobsView() {
            InitializeComponent();
        }

        private void BrowseFile(object sender, System.Windows.RoutedEventArgs e) {
            var sfd = new SaveFileDialog
            {
                AddExtension = true,
                DefaultExt = "txt",
                RestoreDirectory = true,
                Filter = "text file|*.txt",
            };
            if (sfd.ShowDialog() == true) {
                ExportFilePath.Text = sfd.FileName;
            }
        }
    }
}
