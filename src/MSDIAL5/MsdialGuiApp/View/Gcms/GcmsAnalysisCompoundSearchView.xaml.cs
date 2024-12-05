using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Gcms
{
    /// <summary>
    /// Interaction logic for GcmsAnalysisCompoundSearchView.xaml
    /// </summary>
    public partial class GcmsAnalysisCompoundSearchView : UserControl
    {
        public GcmsAnalysisCompoundSearchView() {
            InitializeComponent();
        }

        private void Ok_Click(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            window.DialogResult = true;
            window.Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            var window = Window.GetWindow(this);
            window.DialogResult = false;
            window.Close();
        }
    }
}
