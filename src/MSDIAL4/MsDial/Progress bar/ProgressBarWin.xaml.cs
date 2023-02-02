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

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// PogressBarWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ProgressBarWin : Window
    {
        public ProgressBarWin()
        {
            InitializeComponent();
        }

        public void AddProgressValue(int i)
        {
            Dispatcher.BeginInvoke((Action)(() => this.ProgressView.Value += i));
        }

        public void SetProgressValue(int i)
        {
            Dispatcher.BeginInvoke((Action)(() => this.ProgressView.Value = i));
        }
    }
}
