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

namespace CompMs.App.Msdial.View.Normalize
{
    /// <summary>
    /// Interaction logic for SplashSetView.xaml
    /// </summary>
    public partial class SplashSetView : UserControl
    {
        public SplashSetView() {
            InitializeComponent();
        }

        public void CloseCommand_Execute(object sender, ExecutedRoutedEventArgs e) {
            Window.GetWindow(this).Close();
        }

        public void CloseCommand_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
    }
}
