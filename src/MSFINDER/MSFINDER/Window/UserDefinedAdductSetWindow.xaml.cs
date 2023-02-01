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
    /// Interaction logic for UserDefinedAdductSetWindow.xaml
    /// </summary>
    public partial class UserDefinedAdductSetWindow : Window
    {
        private MainWindow mainWindow;
        private MainWindowVM mainWindowVM;

        public UserDefinedAdductSetWindow(MainWindow mainWindow, MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.mainWindowVM = mainWindowVM;

            this.DataContext = new UserDefinedAdductVM(this, this.mainWindowVM, this.mainWindow);
        }
    }
}
