using CompMs.App.Msdial.ViewModel.Setting;
using System.Windows.Controls;

namespace CompMs.App.Msdial.View.Setting
{
    /// <summary>
    /// Interaction logic for DataCollectionSettingView.xaml
    /// </summary>
    public partial class DataCollectionSettingView : UserControl
    {
        public DataCollectionSettingView() {
            InitializeComponent();
        }

        private void Run_RtCorrection(object sender, System.Windows.RoutedEventArgs e) {
            if (DataContext is not DataCollectionSettingViewModel vm) {
                return;
            }
            var (analysisFiles, parameter) = vm.Model.GetAnalysisFileAndParameterToShowRetentionTimeCorrectionDialog();
            var dialog = new RetentionTimeCorrectionWinLegacy(analysisFiles, parameter, false);
            dialog.ShowDialog();
        }
    }
}
