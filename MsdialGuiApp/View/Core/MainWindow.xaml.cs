using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.View.Table;
using CompMs.App.Msdial.ViewModel;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM.WindowService;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
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

            var compoundSearchService = new DialogService<CompoundSearchWindow, CompoundSearchVM>(this);
            var peakSpotTableService = new DialogService<AlignmentSpotTable, PeakSpotTableViewModelBase>(this);
            var proteomicsTableService = new DialogService<ProteomicsSpotTable, PeakSpotTableViewModelBase>(this);
            var analysisFilePropertyResetService = new DialogService<AnalysisFilePropertyResettingWindow, AnalysisFilePropertySetViewModel>(this);
            var processSettingDialogService = new DialogService<ProjectSettingDialog, ProcessSettingViewModel>(this);
            DataContext = new MainWindowVM(
                compoundSearchService,
                peakSpotTableService,
                analysisFilePropertyResetService,
                proteomicsTableService,
                processSettingDialogService);

            broker = MessageBroker.Default;

            broker.ToObservable<ExperimentSpectrumViewModel>()
                .Subscribe(OpenExperimentSpectrumView);
            broker.ToObservable<SaveNistFileNameRequest>()
                .Subscribe(SelectSaveFileName);
        }

        private readonly IMessageBroker broker;

        public void CloseOwnedWindows() {
            foreach (var child in OwnedWindows.Cast<Window>()) {
                child.Close();
            }
        }

        private void OpenExperimentSpectrumView(ExperimentSpectrumViewModel viewmodel) {
            var dialog = new ExperimentSpectrumView() { Owner = this, DataContext = viewmodel, };
            dialog.Show();
        }

        private void SelectSaveFileName(SaveNistFileNameRequest request) {
            var sfd = new SaveFileDialog {
                Title = "Save spectra",
                Filter = "NIST format(*.msp)|*.msp",
                RestoreDirectory = true,
                AddExtension = true,
            };

            if (sfd.ShowDialog(this) == true) {
                var filename = sfd.FileName;
                broker.Publish(new SaveNistFileNameResponse(filename));
            }
        }
    }
}
