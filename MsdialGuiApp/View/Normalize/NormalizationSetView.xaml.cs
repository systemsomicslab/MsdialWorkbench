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

namespace CompMs.App.Msdial.View.Normalize
{
    /// <summary>
    /// Interaction logic for NormalizationSetView.xaml
    /// </summary>
    public partial class NormalizationSetView : Window
    {
        public NormalizationSetView() {
            InitializeComponent();
            CommandBindings.Add(new CommandBinding(DoneCommand, (s, e) => { DialogResult = true; Close(); }));
        }

        public readonly static RoutedCommand DoneCommand = new RoutedCommand(nameof(DoneCommand), typeof(NormalizationSetView));
    }
}
