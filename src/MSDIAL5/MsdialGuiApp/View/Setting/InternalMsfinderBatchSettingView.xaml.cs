using Microsoft.Win32;
using System.Windows;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Setting {
    public partial class InternalMsfinderBatchSettingView : UserControl {
        public InternalMsfinderBatchSettingView() {
            InitializeComponent();
        }
        private void Button_ExistProjectPath_Browse_Click(object sender, RoutedEventArgs e) {
            var fbd = new Graphics.Window.SelectFolderDialog {
                Title = "Choose a folder which contains MS-FINDER project.",
            };
            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                TextBox_ExistProjectPath.Text = fbd.SelectedPath;
            }
        }
    }
}
