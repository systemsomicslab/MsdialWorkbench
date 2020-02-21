using System;
using System.Collections.Generic;
using System.Diagnostics;
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

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for LipoqualitySpectrumSearchWin.xaml
    /// </summary>
    public partial class LipoqualitySpectrumSearchWin : Window {
        public LipoqualitySpectrumSearchWin() {
            InitializeComponent();
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            var window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e) {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            var window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void GotoBinvestigate(object sender, RequestNavigateEventArgs e) {
            Process.Start(new ProcessStartInfo(e.Uri.AbsoluteUri));
            e.Handled = true;
        }
    }
}
