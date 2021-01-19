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

namespace CompMs.Graphics.UI.ProgressBar
{
    /// <summary>
    /// Interaction logic for ProgressBarWindow.xaml
    /// </summary>
    public partial class ProgressBarWindow : System.Windows.Window
    {
        public ProgressBarWindow() {
            InitializeComponent();
        }
        public void AddProgressValue(int i)
        {
            Dispatcher.BeginInvoke((Action)(() => ProgressView.Value += i));
        }

        public void SetProgressValue(int i)
        {
            Dispatcher.BeginInvoke((Action)(() => ProgressView.Value = i));
        }
    }
}
