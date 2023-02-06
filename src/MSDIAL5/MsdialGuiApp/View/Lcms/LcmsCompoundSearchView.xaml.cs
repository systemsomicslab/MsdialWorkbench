using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Lcms
{
    /// <summary>
    /// Interaction logic for LcmsCompoundSearchView.xaml
    /// </summary>
    public partial class LcmsCompoundSearchView : UserControl
    {
        public LcmsCompoundSearchView() {
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
