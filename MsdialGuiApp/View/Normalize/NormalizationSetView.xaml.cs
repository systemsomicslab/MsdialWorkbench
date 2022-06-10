using System.Windows;
using System.Windows.Input;

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
            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
        }

        public readonly static RoutedCommand DoneCommand = new RoutedCommand(nameof(DoneCommand), typeof(NormalizationSetView));
    }
}
