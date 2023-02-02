using System;
using System.Collections.Generic;
using System.Diagnostics;
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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Rfx.Riken.OsakaUniv.Hca
{
    /// <summary>
    /// Interaction logic for HcaHeatmap.xaml
    /// </summary>
    public partial class HcaHeatmap : UserControl
    {
        public HcaHeatmap()
        {
            InitializeComponent();
            MouseRightButtonDown += HandleContextMenuOnMouseRightButtonDown;
            MouseLeave += HandleContextMenuOnMouseLeave;
            ContextMenuOpening += HandleContextMenuOnContextMenuOpning;
            sw = new Stopwatch();
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var window = new SaveImageAsWin(this);
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var window = new CopyImageAsWin(this);
            window.Owner = Window.GetWindow(this);
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        Stopwatch sw;
        void HandleContextMenuOnMouseRightButtonDown(object sender, MouseButtonEventArgs e)
        {
            sw.Start();
        }
        void HandleContextMenuOnContextMenuOpning(object sender, ContextMenuEventArgs e)
        {
            sw.Stop();
            if (sw.ElapsedMilliseconds > 200)
                e.Handled = true;
            sw.Reset();
        }
        void HandleContextMenuOnMouseLeave(object sender, MouseEventArgs e)
        {
            sw.Stop();
            sw.Reset();
        }
    }
}
