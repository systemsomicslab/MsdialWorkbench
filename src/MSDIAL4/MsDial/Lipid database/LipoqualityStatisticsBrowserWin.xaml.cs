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
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv {
    /// <summary>
    /// Interaction logic for LipoqualityStatisticsBrowserWin.xaml
    /// </summary>
    public partial class LipoqualityStatisticsBrowserWin : Window {
        public LipoqualityStatisticsBrowserWin() {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }

        private void TreeView_LipoqualityQuantTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e) {
            if (this.DataContext == null) return;

            var selectedItem = (LipoqualityQuantTree)this.TreeView_LipoqualityQuantTree.SelectedItem;
            ((LipoqualityStatisticsBrowserVM)this.DataContext).SelectedQuantTree = selectedItem;
        }
    }
}
