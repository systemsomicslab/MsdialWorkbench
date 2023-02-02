using System;
using System.Collections.Generic;
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
    /// Interaction logic for LipidDbSetWin.xaml
    /// </summary>
    public partial class LipidDbSetWin : Window
    {
        public LipidDbSetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.DataContext = new LipidDbSetVM(mainWindow, this);

            var ionMode = mainWindow.ProjectProperty.IonMode;
            if (ionMode == IonMode.Positive) { this.TabItem_NegativeSet.IsEnabled = false; this.TabControl.SelectedIndex = 0; }
            else if (ionMode == IonMode.Negative) { this.TabItem_PositiveSet.IsEnabled = false; this.TabControl.SelectedIndex = 1; }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Click_CheckAll(object sender, RoutedEventArgs e) {
            var posQueries = ((LipidDbSetVM)this.DataContext).PositiveQueryVMs;
            var negQueries = ((LipidDbSetVM)this.DataContext).NegativeQueryVMs;
            var ionMode = ((LipidDbSetVM)this.DataContext).IonMode;

            if (ionMode == IonMode.Positive) {
                foreach (var query in posQueries) {
                    query.IsSelected = true;
                }
            }
            else {
                foreach (var query in negQueries) {
                    query.IsSelected = true;
                }
            }
        }

        private void Click_RemoveAll(object sender, RoutedEventArgs e) {
            var posQueries = ((LipidDbSetVM)this.DataContext).PositiveQueryVMs;
            var negQueries = ((LipidDbSetVM)this.DataContext).NegativeQueryVMs;
            var ionMode = ((LipidDbSetVM)this.DataContext).IonMode;

            if (ionMode == IonMode.Positive) {
                foreach (var query in posQueries) {
                    query.IsSelected = false;
                }
            }
            else {
                foreach (var query in negQueries) {
                    query.IsSelected = false;
                }
            }
        }
    }
}
