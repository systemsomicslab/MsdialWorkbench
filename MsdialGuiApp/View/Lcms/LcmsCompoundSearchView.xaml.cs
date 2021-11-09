using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

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
