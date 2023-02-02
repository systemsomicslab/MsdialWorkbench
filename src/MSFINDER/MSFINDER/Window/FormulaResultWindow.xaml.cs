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
    /// FormulaResultWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class FormulaResultWindow : Window
    {
        private MainWindowVM mainWindowVM;

        public FormulaResultWindow(MainWindowVM mainWindowVM)
        {
            InitializeComponent();
            this.mainWindowVM = mainWindowVM;
            this.DataContext = this.mainWindowVM.SelectedFormulaVM;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
        }
    }
}
