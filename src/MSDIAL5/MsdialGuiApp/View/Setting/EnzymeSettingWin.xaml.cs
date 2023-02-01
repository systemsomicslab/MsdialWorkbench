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
    /// Interaction logic for EnzymeSettingWin.xaml
    /// </summary>
    public partial class EnzymeSettingWin : Window {
        public EnzymeSettingWin() {
            InitializeComponent();
        }

        private void Button_Cancel_Click(object sender, RoutedEventArgs e) {
            if (this.DataContext == null) return;
            this.Close();
        }
    }
}
