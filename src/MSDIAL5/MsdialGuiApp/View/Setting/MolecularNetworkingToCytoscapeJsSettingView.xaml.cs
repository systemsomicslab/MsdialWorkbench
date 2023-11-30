using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting {
    /// <summary>
    /// Interaction logic for MolecularNetworkingToCytoscapeJsSettingView.xaml
    /// </summary>
    public partial class MolecularNetworkingToCytoscapeJsSettingView : Window {
        public MolecularNetworkingToCytoscapeJsSettingView() {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, Cancel_Click));
        }

        private void Run_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
