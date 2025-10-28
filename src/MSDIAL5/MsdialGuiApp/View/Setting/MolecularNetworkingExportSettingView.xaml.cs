using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for MolecularNetworkingSettingView.xaml
    /// </summary>
    public partial class MolecularNetworkingExportSettingView : Window
    {
        public MolecularNetworkingExportSettingView()
        {
            InitializeComponent();
            CommandBindings.AddRange(new[] {
                new CommandBinding(ApplicationCommands.Open, Browse_Click),
                new CommandBinding(ApplicationCommands.Close, Cancel_Click),
            });
        }

        private void Browse_Click(object sender, RoutedEventArgs e) {
            var fbd = new Graphics.Window.SelectFolderDialog {
                Title = "Choose a export folder.",
            };

            if (fbd.ShowDialog() == Graphics.Window.DialogResult.OK) {
                TextBox_ExportFolderPath.Text = fbd.SelectedPath;
            }
        }

        private void Run_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
