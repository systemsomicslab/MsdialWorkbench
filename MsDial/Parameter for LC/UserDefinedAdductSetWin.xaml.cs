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
    /// Interaction logic for UserDefinedAdductSW.xaml
    /// </summary>
    public partial class UserDefinedAdductSetWin : Window
    {
        private Window window;
        private MainWindow mainWindow;
        public UserDefinedAdductSetWin(Window window, MainWindow mainWindow)
        {
            InitializeComponent();
            this.window = window;
            this.mainWindow = mainWindow;

            this.DataContext = new UserDefinedAdductVM(this, this.mainWindow);
        }
    }
}
