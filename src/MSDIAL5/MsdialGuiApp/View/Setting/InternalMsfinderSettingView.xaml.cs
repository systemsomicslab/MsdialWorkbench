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

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// InternalMsfinderSettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class InternalMsfinderSettingView : Window
    {
        public InternalMsfinderSettingView()
        {
            InitializeComponent();
        }
        private void Button_RtInChiKeyDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import your RT and InChIKey dictionary",
                Filter = "Text file(*.txt)|*.txt;",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true)
            {
                TextBox_RtInChIKeyDictionaryFilepath.Text = ofd.FileName;
            }
        }

        private void Button_RtSmilesDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import your RT and SMILES dictionary",
                Filter = "Text file(*.txt)|*.txt",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true)
            {
                TextBox_RtSmilesDictionaryFilepath.Text = ofd.FileName;
            }
        }

        private void Button_CcsAdductInChiKeyDictionaryFilepath_Browse_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog
            {
                Title = "Import your CCS, Adduct and InChIKey dictionary",
                Filter = "Text file(*.txt)|*.txt",
                RestoreDirectory = true,
                Multiselect = false,
            };

            if (ofd.ShowDialog() == true)
            {
                TextBox_CcsAdductInChIKeyDictionaryFilepath.Text = ofd.FileName;
            }
        }

        private void Click_Cancel(object sender, RoutedEventArgs e)
        {
            DialogResult = false;
            Close();
        }
    }
}
