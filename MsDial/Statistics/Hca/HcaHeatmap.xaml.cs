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

namespace Rfx.Riken.OsakaUniv.Hca
{
    /// <summary>
    /// Interaction logic for HcaHeatmap.xaml
    /// </summary>
    public partial class HcaHeatmap : UserControl
    {
        public HcaHeatmap()
        {
            InitializeComponent();
        }

        private void MenuItem_Click(object sender, RoutedEventArgs e)
        {
            var window = new SaveImageAsWin(this);
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }
    }
}
