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
    /// Interaction logic for MassqlSettingView.xaml
    /// </summary>
    public partial class MassqlSettingView : Window {
        public MassqlSettingView()
        {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
        }
    }
}
