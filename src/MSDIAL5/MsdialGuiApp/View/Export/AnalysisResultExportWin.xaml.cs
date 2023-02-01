using System.Windows;

namespace CompMs.App.Msdial.View.Export
{
    /// <summary>
    /// Interaction logic for AnalysisResultExportWin.xaml
    /// </summary>
    public partial class AnalysisResultExportWin : Window
    {
        public AnalysisResultExportWin() {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void ExportButton_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
