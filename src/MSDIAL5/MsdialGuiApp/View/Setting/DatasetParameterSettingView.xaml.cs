using CompMs.App.Msdial.Common;
using System;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for DatasetParameterSettingView.xaml
    /// </summary>
    public partial class DatasetParameterSettingView : UserControl
    {
        public DatasetParameterSettingView() {
            InitializeComponent();
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) {
            var fbd = new Graphics.Window.SelectFolderDialog
            {
                Title = "Choose a project folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                var dt = DateTime.Now;
                TextBox_ProjectFolderPath.Text = System.IO.Path.Combine(fbd.SelectedPath, $"{dt:yyyy_MM_dd_HH_mm_ss}.{SaveFileFormat.mtd}3");
            }
        }
    }
}
