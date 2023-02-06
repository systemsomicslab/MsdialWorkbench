using CompMs.Graphics.Window;
using System.Windows.Controls;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for ProjectParameterSettingView.xaml
    /// </summary>
    public partial class ProjectParameterSettingView : UserControl
    {
        public ProjectParameterSettingView() {
            InitializeComponent();
        }

        private void Open_Executed(object sender, ExecutedRoutedEventArgs e) {
            var sfd = new SelectFolderDialog
            {
                Title = "Choose a project folder.",
            };

            if (sfd.ShowDialog() == DialogResult.OK) {
                TextBox_ProjectFolderPath.Text = sfd.SelectedPath;
            }
        }

        private void Open_CanExecute(object sender, CanExecuteRoutedEventArgs e) {
            e.CanExecute = true;
        }
    }
}
