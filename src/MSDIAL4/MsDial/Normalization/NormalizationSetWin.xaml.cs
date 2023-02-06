using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
    /// NormalizationSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class NormalizationSetWin : Window
    {
        private MainWindow mainWindow;
        public NormalizationSetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.DataContext = new NormalizationSetVM(mainWindow, this);
        }
    }
}
