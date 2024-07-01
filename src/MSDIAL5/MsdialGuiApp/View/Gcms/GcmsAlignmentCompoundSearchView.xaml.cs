using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Gcms
{
    /// <summary>
    /// Interaction logic for GcmsAlignmentCompoundSearchView.xaml
    /// </summary>
    public partial class GcmsAlignmentCompoundSearchView : UserControl
    {
        public GcmsAlignmentCompoundSearchView() {
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
