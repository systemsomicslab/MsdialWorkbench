using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Lcimms
{
    /// <summary>
    /// Interaction logic for LcimmsCompoundSearchView.xaml
    /// </summary>
    public partial class LcimmsCompoundSearchView : UserControl
    {
        public LcimmsCompoundSearchView() {
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
