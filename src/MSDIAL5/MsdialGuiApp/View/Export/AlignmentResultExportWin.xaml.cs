using System.Windows;

namespace CompMs.App.Msdial.View.Export
{
    /// <summary>
    /// Interaction logic for AlignmentResultExportWin.xaml
    /// </summary>
    public partial class AlignmentResultExportWin : Window
    {
        public AlignmentResultExportWin() {
            InitializeComponent();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void CancelButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
