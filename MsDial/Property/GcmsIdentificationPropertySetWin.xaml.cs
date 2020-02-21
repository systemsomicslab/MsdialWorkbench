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
    /// Interaction logic for GcmsIdentificationPropertySetWin.xaml
    /// </summary>
    public partial class GcmsIdentificationPropertySetWin : Window
    {
        private MainWindow mainWindow;

        public GcmsIdentificationPropertySetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            this.DataContext = new GcmsIdentificationPropertySettingVM(this.mainWindow, this);
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.Close();
        }

        private void Datagrid_AlignmentProperty_CurrentCellChanged(object sender, EventArgs e)
        {
            var cell = DataGridHelper.GetCell((DataGridCellInfo)this.Datagrid_AlignmentProperty.CurrentCell);
            if (cell == null) return;
            cell.Focus();
            this.Datagrid_AlignmentProperty.BeginEdit();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            mainWindow.WindowOpened = false;
        }
    }
}
