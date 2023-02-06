using Riken.Metabolomics.BinVestigate;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// Interaction logic for BinVestigateStatisticsBrowserWin.xaml
    /// </summary>
    public partial class BinVestigateStatisticsBrowserWin : Window
    {
        public BinVestigateStatisticsBrowserWin()
        {
            InitializeComponent();
            WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
        }

        private void TreeView_BinBaseQuantTree_SelectedItemChanged(object sender, RoutedPropertyChangedEventArgs<object> e)
        {
            if (this.DataContext == null) return;

            var selectedItem = (BinBaseQuantTree)this.TreeView_BinBaseQuantTree.SelectedItem;
            ((BinVestigateStatisticsBrowserVM)this.DataContext).SelectedQuantTree = selectedItem;
        }
    }
}
