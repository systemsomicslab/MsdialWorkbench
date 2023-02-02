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
    /// ProgressBarMultiContainerWin.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressBarMultiContainerWin : Window
    {
        public ProgressBarMultiContainerWin() {
            InitializeComponent();
        }
        public void SetProgressBar(List<ProgressBarEach> pblist) {
            foreach (var pb in pblist) {
                var grid = new Grid() { Height = 20, Width = 680, HorizontalAlignment = HorizontalAlignment.Left };
                grid.Children.Add(pb);
                this.Panel.Children.Add(grid);
            }
            this.Panel.UpdateLayout();
        }
    }
}
