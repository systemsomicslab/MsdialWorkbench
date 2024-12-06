using CompMs.App.Msdial.Model.Notification;
using CompMs.App.Msdial.Model.Service;
using CompMs.App.Msdial.Utility;
using CompMs.App.Msdial.View.Chart;
using CompMs.App.Msdial.View.Export;
using CompMs.App.Msdial.View.MsResult;
using CompMs.App.Msdial.View.PeakCuration;
using CompMs.App.Msdial.View.Search;
using CompMs.App.Msdial.View.Setting;
using CompMs.App.Msdial.View.Statistics;
using CompMs.App.Msdial.View.Table;
using CompMs.App.Msdial.ViewModel.Chart;
using CompMs.App.Msdial.ViewModel.Core;
using CompMs.App.Msdial.ViewModel.Export;
using CompMs.App.Msdial.ViewModel.MsResult;
using CompMs.App.Msdial.ViewModel.PeakCuration;
using CompMs.App.Msdial.ViewModel.Search;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.App.Msdial.ViewModel.Statistics;
using CompMs.App.Msdial.ViewModel.Table;
using CompMs.CommonMVVM.Common;
using CompMs.CommonMVVM.WindowService;
using CompMs.Graphics.Behavior;
using CompMs.Graphics.UI;
using CompMs.Graphics.UI.Message;
using CompMs.Graphics.UI.ProgressBar;
using CompMs.Graphics.Window;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.ComponentModel;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Controls.Ribbon;
using System.Windows.Input;

namespace CompMs.App.Msdial.View.Core
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : RibbonWindow
    {
        public MainWindow() {
            InitializeComponent();

            var peakSpotTableService = new DialogService<AlignmentSpotTable, PeakSpotTableViewModelBase>(this);
            DataContext = new MainWindowVM(peakSpotTableService);

            var broker = MessageBroker.Default;

            broker.ToObservable<ProgressBarMultiContainerRequest>()
                .Subscribe(ShowMultiProgressBarWindow);
            broker.ToObservable<ProgressBarRequest>()
                .Subscribe(ShowProgressBarWindow);
            broker.ToObservable<ShortMessageRequest>()
                .Subscribe(ShowShortMessageDialog);
            broker.ToObservable<ProcessMessageRequest>()
                .Subscribe(ShowProcessMessageDialog);
            broker.ToObservable<FileClassSetViewModel>()
                .Subscribe(ShowChildSettingDialog<FileClassSetView>("Class property setting", height: 450, width: 400));
            broker.ToObservable<SaveFileNameRequest>()
                .Subscribe(GetSaveFilePath);
            broker.ToObservable<SelectFolderRequest>()
                .Subscribe(SelectFolderPath);
            broker.ToObservable<OpenFileRequest>()
                .Subscribe(OpenFileDialog);
            broker.ToObservable<ErrorMessageBoxRequest>()
                .Subscribe(ShowErrorConfirmationMessage);
            broker.ToObservable<RiDictionarySettingViewModel>()
                .Subscribe(ShowRiDictionarySettingDialog);
            broker.ToObservable<AlignedChromatogramModificationViewModelLegacy>()
                .Subscribe(CreateAlignedChromatogramModificationDialog);
            broker.ToObservable<SampleTableViewerInAlignmentViewModelLegacy>()
                .Subscribe(CreateSampleTableViewerDialog);
            broker.ToObservable<InternalStandardSetViewModel>()
                .Subscribe(ShowChildSettingDialog<InternalStandardSetView>("Internal standard settting", height: 600, width: 800));
            broker.ToObservable<PCAPLSResultViewModel>()
                .Subscribe(OpenPCAPLSResultView);
            broker.ToObservable<ExperimentSpectrumViewModel>()
                .Subscribe(ShowChildView<ExperimentSpectrumView>);
            broker.ToObservable<ProteinGroupTableViewModel>()
                .Subscribe(ShowChildView<ProteinGroupTable>);
            broker.ToObservable<ChromatogramsViewModel>()
                .Subscribe(ShowChildView<DisplayChromatogramsView>);
            broker.ToObservable<CheckChromatogramsViewModel>()
                .Subscribe(ShowChildContent<CheckChromatogramsView>(height: 400, width: 1000, needDispose: true));
            broker.ToObservable<NormalizationSetViewModel>()
                .Subscribe(ShowChildDialog<NormalizationSetView>);
            broker.ToObservable<MultivariateAnalysisSettingViewModel>()
                .Subscribe(ShowChildView<MultivariateAnalysisSettingView>);
            broker.ToObservable<AnalysisResultExportViewModel>()
                .Subscribe(ShowChildDialog<AnalysisResultExportWin>);
            broker.ToObservable<AlignmentResultExportViewModel>()
                .Subscribe(ShowChildDialog<AlignmentResultExportWin>);
            broker.ToObservable<TargetCompoundLibrarySettingViewModel>()
                .Subscribe(ShowChildSettingDialog<TargetCompoundsLibrarySettingView>("Select library file", height: 600, width: 400));
            broker.ToObservable<FindTargetCompoundsSpotViewModel>()
                .Subscribe(ShowChildSettingDialog<FindTargetCompoundsSpotView>("Find match peak spots", height: 800, width: 400));
            broker.ToObservable<MolecularNetworkingExportSettingViewModel>()
                .Subscribe(ShowChildView<MolecularNetworkingExportSettingView>);
            broker.ToObservable<MolecularNetworkingSendingToCytoscapeJsSettingViewModel>()
                .Subscribe(ShowChildView<MolecularNetworkingToCytoscapeJsSettingView>);
            broker.ToObservable<AnalysisPeakTableViewModelBase>()
                .Subscribe(ShowChildView<AlignmentSpotTable>);
            broker.ToObservable<AlignmentSpotTableViewModelBase>()
                .Subscribe(ShowChildView<AlignmentSpotTable>);
            broker.ToObservable<AnalysisFilePropertyResetViewModel>()
                .Subscribe(ShowChildSettingDialog<AnalysisFilePropertyResettingView>("Analysis property setting", height: 700, width: 1000));
            broker.ToObservable<ProcessSettingViewModel>()
                .Subscribe(ShowChildDialog<ProjectSettingDialog>);
            broker.ToObservable<ProjectPropertySettingViewModel>()
                .Subscribe(ShowChildSettingDialog<ProjectPropertySettingView>("Project property setting", height: 400, width: 400));
            broker.ToObservable<ICompoundSearchViewModel>()
                .Subscribe(ShowChildDialog<CompoundSearchWindow>);
            broker.ToObservable<ExportMrmprobsViewModel>()
                .Subscribe(ShowChildSettingDialog<ExportMrmprobsView>("MRMPROBS reference library export", height: 560, width: 560, finishCommandContent: "Export"));
            broker.ToObservable<AccumulatedMs1SpectrumViewModel>()
                .Subscribe(ShowChildContent<AccumulatedMs1SpectrumView>(height: 600, width: 800));
            broker.ToObservable<AccumulatedMs2SpectrumViewModel>()
                .Subscribe(ShowChildContent<AccumulatedMs2SpectrumView>(height: 600, width: 800));
            broker.ToObservable<AccumulatedExtractedMs2SpectrumViewModel>()
                .Subscribe(ShowChildContent<AccumulatedExtractedMs2SpectrumView>(height: 600, width: 800));
            broker.ToObservable<AccumulatedSpecificExperimentMS2SpectrumViewModel>()
                .Subscribe(ShowChildContent<AccumulatedSpecificExperimentMS2SpectrumView>(height: 600, width: 800));
            broker.ToObservable<FormulaFinderAdductIonSettingViewModel>()
                .Subscribe(ShowChildDialog<FormulaFinderAdductIonSettingView>);
            broker.ToObservable<InternalMsfinderBatchSettingViewModel>()
                .Subscribe(ShowChildSettingDialog<InternalMsfinderBatchSettingView>("MS-FINDER  batch processing setting", height: 800, width: 800, finishCommandContent: "Run", needDispose: true));
            broker.ToObservable<InternalMsFinderViewModel>()
                .Subscribe(ShowChildContent<InternalMsFinderView>("MS-FINDER", height: 1000, width: 1500));
            broker.ToObservable<InternalMsfinderSettingViewModel>()
                .Subscribe(ShowChildSettingDialog<InternalMsfinderSettingView>("MS-FINDER setting", height: 600, width: 800, finishCommandContent: "OK", needDispose: true));
            broker.ToObservable<InternalMsFinderSingleSpotViewModel>()
                .Subscribe(ShowChildContent<InternalMsFinderSingleSpotView>("MS-FINDER", height: 1000, width: 1500, needDispose: true));
            broker.ToObservable<InternalMsfinderSubstructure>()
                .Subscribe(ShowChildContent<SubstructureView>("Substructure Viewer", height: 600, width: 1000));
            broker.ToObservable<FseaResultViewModel>()
                .Subscribe(ShowChildContent<FseaResultView>("FSEA Result Viewer", height: 600, width: 1000));
            broker.ToObservable<NotameViewModel>()
                .Subscribe(ShowChildSettingDialog<NotameView>("Notame preprocessing", height: 500, width: 450, finishCommandContent: "Run"));
            /*
            broker.ToObservable<PeakSpotTableViewModelBase>()
                .Subscribe(ShowChildView<AlignmentSpotTable>);
            broker.ToObservable<PeakSpotTableViewModelBase>()
                .Subscribe(ShowChildView<ProteomicsSpotTable>);
            */
#if RELEASE
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Critical;
#elif DEBUG
            System.Diagnostics.PresentationTraceSources.DataBindingSource.Switch.Level = System.Diagnostics.SourceLevels.Warning;
#endif
        }

        public void CloseOwnedWindows() {
            Dispatcher.Invoke(() => {
                foreach (var child in OwnedWindows.OfType<Window>()) {
                    if (child.IsLoaded) {
                        child.Close();
                    }
                }
            });
        }

        private void ShowChildView<TView>(object viewmodel) where TView : Window, new() {
            var view = new TView() {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            view.Show();
        }

        private Action<object> ShowChildContent<TView>(string? title = null, double? height = null, double? width = null, bool needDispose = false) where TView: FrameworkElement, new() {
            void InnerShowDialog(object viewmodel) {
                var view = new Window
                {
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    DataContext = viewmodel,
                    Content = new TView(),
                };
                if ((title ?? (viewmodel as IDialogPropertiesViewModel)?.Title) is string t) {
                    view.Title = t;
                }
                if ((height ?? (viewmodel as IDialogPropertiesViewModel)?.Height) is double h) {
                    view.Height = h;
                }
                if ((width ?? (viewmodel as IDialogPropertiesViewModel)?.Width) is double w) {
                    view.Width = w;
                }
                if (needDispose) {
                    DataContextCleanupBehavior.SetIsEnabled(view, true);
                }
                view.Show();
            }
            return InnerShowDialog;
        }

        private void ShowChildViewWithDispose<TView>(object viewmodel) where TView : Window, new() {
            Window view = new TView()
            {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            DataContextCleanupBehavior.SetIsEnabled(view, true);
            view.Show();
        }

        private void ShowChildDialog<TView>(object viewmodel) where TView : Window, new() {
            var view = new TView() {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            view.ShowDialog();
        }

        private Action<object> ShowChildSettingDialog<TView>(string title, double height, double width, object? finishCommandContent = null, bool needDispose = false)
            where TView: FrameworkElement, new() {
            void InnerShowDialog(object viewmodel) {
                var dialog = new SettingDialog() {
                    Height = height, Width = width,
                    Title = title,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    DataContext = viewmodel,
                    Content = new TView(),
                };
                if (finishCommandContent is not null) {
                    dialog.FinishCommandContent = finishCommandContent;
                }
                dialog.ShowDialog();
                if (needDispose)
                {
                    (viewmodel as IDisposable)?.Dispose();
                }
            }
            return InnerShowDialog;
        }

        private void ShowMultiProgressBarWindow(ProgressBarMultiContainerRequest request) {
            using (var viewmodel = new ProgressBarMultiContainerVM(request)) {
                var dialog = new ProgressBarMultiContainerWindow() {
                    DataContext = viewmodel,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                dialog.Loaded += async (s, e) => {
                    await viewmodel.RunAsync().ConfigureAwait(false);
                    request.Result = true;
                    dialog.Dispatcher.Invoke(dialog.Close);
                };
                dialog.ShowDialog();
            }
        }

        private void ShowProgressBarWindow(ProgressBarRequest request) {
            using (var viewmodel = new ProgressBarVM(request)) {
                var dialog = new ProgressBarWindow() {
                    DataContext = viewmodel,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                dialog.Loaded += async (s, e) => {
                    await request.AsyncAction.Invoke(viewmodel).ConfigureAwait(false);
                    request.Result = true;
                    dialog.Dispatcher.Invoke(dialog.Close);
                };
                dialog.ShowDialog();
            }
        }

        private void ShowShortMessageDialog(ShortMessageRequest request) {
            Dispatcher.Invoke(() => {
                var dialog = new ShortMessageWindow() {
                    DataContext = request.Content,
                    Text = request.Content,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                request.Result = dialog.ShowDialog();
            });
        }

        private void ShowProcessMessageDialog(ProcessMessageRequest request) {
            var dialog = new ShortMessageWindow() {
                DataContext = request.Content,
                Text = request.Content,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
            };
            dialog.Loaded += async (s, e) => {
                await request.AsyncAction();
                dialog.Dispatcher.Invoke(() => {
                    dialog.DialogResult = true;
                    dialog.Close();
                });
            };
            request.Result = dialog.ShowDialog();
        }

        private void OpenPCAPLSResultView(PCAPLSResultViewModel viewmodel) {
            var dialog = new Window() {
                DataContext = viewmodel,
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Content = new MultivariateAnalysisResultView(),
            };
            dialog.Show();
        }

        private void GetSaveFilePath(SaveFileNameRequest request) {
            var sfd = new SaveFileDialog() {
                Title = request.Title,
                Filter = request.Filter,
                RestoreDirectory = request.RestoreDirectory,
                AddExtension = request.AddExtension,
            };

            request.Result = sfd.ShowDialog(this);
            if (request.Result == true) {
                request.Run(sfd.FileName);
            }
        }

        private void OpenFileDialog(OpenFileRequest request) {
            var ofd = new OpenFileDialog() {
                Title = request.Title,
                Filter = request.Filter,
            };

            if (ofd.ShowDialog(this) == true) {
                request.Run(ofd.FileName);
            }
        }

        private void SelectFolderPath(SelectFolderRequest request) {
            var sfd = new SelectFolderDialog() {
                Title = request.Title,
                SelectedPath = request.SelectedPath,
            };

            if (sfd.ShowDialog(this) == Graphics.Window.DialogResult.OK) {
                request.Run(sfd.SelectedPath!);
            }
        }

        private void ShowErrorConfirmationMessage(ErrorMessageBoxRequest request) {
            Dispatcher.Invoke(() => {
                var result = MessageBox.Show(request.Content, request.Caption, request.ButtonType, MessageBoxImage.Error);
                request.Result = result;
            });
        }

        private void ShowRiDictionarySettingDialog(RiDictionarySettingViewModel viewmodel) {
            Dispatcher.Invoke(() => {
                var dialog = new SettingDialog() {
                    DataContext = viewmodel,
                    Height = 600, Width = 800,
                    Title = "Retention index dictionary setting",
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                    Content = new RetentionIndexDictionarySettingView(),
                    ApplyCommand = viewmodel.ApplyCommand,
                    FinishCommand = viewmodel.ApplyCommand,
                };
                dialog.ShowDialog();
            });
        }

        private void CreateAlignedChromatogramModificationDialog(AlignedChromatogramModificationViewModelLegacy vm) {
            Dispatcher.Invoke(() => {
                var window = new AlignedPeakCorrectionWinLegacy() {
                    DataContext = vm,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Closed += (s, e) => vm.Dispose();
                window.Show();
            });
        }

        private void CreateSampleTableViewerDialog(SampleTableViewerInAlignmentViewModelLegacy vm) {
            Dispatcher.Invoke(() => {
                var window = new SampleTableViewerInAlignmentLegacy() {
                    DataContext = vm,
                    Owner = this,
                    WindowStartupLocation = WindowStartupLocation.CenterOwner,
                };
                window.Closed += (s, e) => vm.Dispose();
                window.Show();
            });
        }

        protected override void OnContentRendered(EventArgs e) {
            base.OnContentRendered(e);

            if (GlobalResources.Instance.IsLabPrivate) {
                return;
            }
            Mouse.OverrideCursor = Cursors.Wait;
            var window = new ShortMessageWindow() {
                Owner = this,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Text = "Checking for updates.."
            };
            window.Show();
            VersionUpdateNotificationService.CheckForUpdates();
            window.Close();

            Mouse.OverrideCursor = null;
        }

        protected override void OnClosing(CancelEventArgs e) {
            base.OnClosing(e);

            if (DataContext is MainWindowVM vm && vm.TaskProgressCollection.Any()) {
                var result = MessageBox.Show(
                    "A process is running in the background.\n" +
                    "If the application is terminated, the project may be corrupted.\n" +
                    "Do you want to close the application?",
                    "Warning",
                    MessageBoxButton.OKCancel);
                if (result != MessageBoxResult.OK) {
                    e.Cancel = true;
                }
            }
        }

        private void Hyperlink_RequestNavigate(object sender, System.Windows.Navigation.RequestNavigateEventArgs e) {
            System.Diagnostics.Process.Start(e.Uri.ToString());
        }
    }
}
