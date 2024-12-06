using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// InternalMsfinderSettingView.xaml の相互作用ロジック
    /// </summary>
    public partial class InternalMsfinderSettingView : UserControl
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
    }
}
