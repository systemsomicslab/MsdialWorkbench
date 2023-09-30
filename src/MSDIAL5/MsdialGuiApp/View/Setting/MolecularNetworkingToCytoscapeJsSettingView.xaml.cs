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

namespace CompMs.App.Msdial.View.Setting {
    /// <summary>
    /// Interaction logic for MolecularNetworkingToCytoscapeJsSettingView.xaml
    /// </summary>
    public partial class MolecularNetworkingToCytoscapeJsSettingView : Window {
        public MolecularNetworkingToCytoscapeJsSettingView() {
            InitializeComponent();
        }

        private void Run_Click(object sender, RoutedEventArgs e) {
            Close();
        }

        private void Cancel_Click(object sender, RoutedEventArgs e) {
            Close();
        }
    }
}
