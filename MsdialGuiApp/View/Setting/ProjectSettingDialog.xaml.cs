using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for ProjectSettingDialog.xaml
    /// </summary>
    public partial class ProjectSettingDialog : Window
    {
        public ProjectSettingDialog() {
            InitializeComponent();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) {
            Close();
        }
    }
}
