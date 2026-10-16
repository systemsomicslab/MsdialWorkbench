using CompMs.App.Msdial.ViewModel.Setting;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for LipidRtCheckSettingView.xaml
    /// </summary>
    public partial class LipidRtCheckSettingView : Window
    {
        public LipidRtCheckSettingView() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Close, (s, e) => Close()));
            // The check writes the spots' tags while it runs; the window stays until it has finished and saved.
            Closing += (s, e) => e.Cancel = DataContext is LipidRtCheckSettingViewModel vm && vm.IsRunning.Value;
        }
    }
}
