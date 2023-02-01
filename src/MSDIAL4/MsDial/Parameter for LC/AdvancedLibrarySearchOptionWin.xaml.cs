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
    /// AdvancedLibrarySearchOptionWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class AdvancedLibrarySearchOptionWin : Window
    {
        private MainWindow mainWindow;

        public AdvancedLibrarySearchOptionWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new AdvancedLibraryOptionSettingVM(this.mainWindow, this);
        }

        private void CheckBox_OnlyReportTopHitForPostAnnotation_Unchecked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AdvancedLibraryOptionSettingVM)this.DataContext).OnlyReportTopHitForPostAnnotation = false;
        }

        private void CheckBox_OnlyReportTopHitForPostAnnotation_Checked(object sender, RoutedEventArgs e)
        {
            if (this.DataContext == null) return;
            ((AdvancedLibraryOptionSettingVM)this.DataContext).OnlyReportTopHitForPostAnnotation = true;
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
