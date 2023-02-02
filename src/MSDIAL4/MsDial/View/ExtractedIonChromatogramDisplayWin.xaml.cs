using System;
using System.Collections.Generic;
using System.IO;
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
    /// ExtractedIonChromatogramDisplayWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class ExtractedIonChromatogramDisplayWin : Window
    {
        public ExtractedIonChromatogramDisplayWin()
        {
            InitializeComponent();
        }

        public ExtractedIonChromatogramDisplayWin(ChromatogramTicEicViewModel chromatogramTicEicViewModel)
        {
            InitializeComponent();
            this.ChromatogramTicEicUI.Content = new ChromatogramTicEicUI(chromatogramTicEicViewModel);
        }

        private void contextMenu_SaveImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveImageAsWin window = new SaveImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_CopyImageAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            CopyImageAsWin window = new CopyImageAsWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }

        private void contextMenu_SaveChromatogramTableAs_Click(object sender, RoutedEventArgs e)
        {
            var target = ((e.Source as MenuItem).Parent as ContextMenu).PlacementTarget;

            SaveDataTableAsTextWin window = new SaveDataTableAsTextWin(target);
            window.Owner = this;
            window.WindowStartupLocation = System.Windows.WindowStartupLocation.CenterOwner;
            window.Show();
        }
    }
}
