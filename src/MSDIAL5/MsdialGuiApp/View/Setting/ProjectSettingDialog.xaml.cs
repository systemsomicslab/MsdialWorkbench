using System.Collections;
using System.Windows;
using System.Windows.Data;
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
            var collections = (IList)Resources["SettingViewModels"];
            var vms = CollectionViewSource.GetDefaultView(collections);
            vms.MoveCurrentToFirst();
        }

        private void Close_Executed(object sender, ExecutedRoutedEventArgs e) {
            DialogResult = false;
            Close();
        }

        private void RunButton_Click(object sender, RoutedEventArgs e) {
            DialogResult = true;
            Close();
        }
    }
}
