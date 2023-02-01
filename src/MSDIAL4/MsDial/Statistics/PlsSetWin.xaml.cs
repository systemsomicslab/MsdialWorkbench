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
    /// Interaction logic for PlsSetWin.xaml
    /// </summary>
    public partial class PlsSetWin : Window {
        private MainWindow mainWindow;
        public PlsSetWin(MainWindow mainWindow) {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e) {
            this.DataContext = new PlsSettingVM(this.mainWindow, this);
            if (this.mainWindow.ProjectProperty.Ionization == Ionization.EI) {
                this.CheckBox_Annotated.IsEnabled = false;
                //this.CheckBox_IsIdentifiedImportedInStatistics.Content = "Identified";
            }
        }
    }
}
