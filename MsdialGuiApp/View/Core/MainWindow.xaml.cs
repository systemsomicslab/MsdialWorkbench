using CompMs.App.Msdial.StartUp;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.View.Table;
using CompMs.App.Msdial.ViewModel;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM.WindowService;
using System.Linq;
using System.Windows;
using System.Windows.Controls.Ribbon;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow() {
            InitializeComponent();

            var startUpService = new DialogService<StartUpWindow, StartUpWindowVM>(this);
            var analysisFilePropertySetService = new DialogService<AnalysisFilePropertySetWindow, AnalysisFilePropertySetViewModel>(this);
            var compoundSearchService = new DialogService<CompoundSearchWindow, CompoundSearchVM>(this);
            var peakSpotTableService = new DialogService<AlignmentSpotTable, PeakSpotTableViewModelBase>(this);
            var analysisFilePropertyResetService = new DialogService<AnalysisFilePropertyResettingWindow, AnalysisFilePropertySetViewModel>(this);
            DataContext = new MainWindowVM(
                startUpService,
                analysisFilePropertySetService,
                compoundSearchService,
                peakSpotTableService,
                analysisFilePropertyResetService);
        }

        public void CloseOwnedWindows() {
            foreach (var child in OwnedWindows.Cast<Window>()) {
                child.Close();
            }
        }
    }
}
