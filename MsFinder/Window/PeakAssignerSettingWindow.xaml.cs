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
    /// Interaction logic for PeakAssignSettingWindow.xaml
    /// </summary>
    public partial class PeakAssignerSettingWindow : Window
    {
        MainWindow mainWindow;
        MainWindowVM mainWindowVM;

        public PeakAssignerSettingWindow(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.mainWindowVM = mainWindowVM;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new PeakAssignerSettingVM(this, this.mainWindow, this.mainWindowVM);
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            this.Close();
        }
    }
}
