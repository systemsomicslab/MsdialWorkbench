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
    /// Interaction logic for LowessSpanSetWin.xaml
    /// </summary>
    public partial class LowessSpanSetWin : Window
    {
        MainWindow mainWindow;
        public LowessSpanSetWin(MainWindow mainWindow)
        {
            InitializeComponent();
            this.mainWindow = mainWindow;
            this.DataContext = new LowessSpanSetVM(mainWindow, this);
        }

        private void SpanOptButton_Click(object sender, RoutedEventArgs e)
        {
            var lowessSpan = ((LowessSpanSetVM)this.DataContext).MinOptSize;

            Mouse.OverrideCursor = Cursors.Wait;

            var optSpan = MsDialDataNormalization.LowessSpanTune(this.mainWindow, lowessSpan);

            Mouse.OverrideCursor = null;

            ((LowessSpanSetVM)this.DataContext).LowessSpan = optSpan;
        }
    }
}
