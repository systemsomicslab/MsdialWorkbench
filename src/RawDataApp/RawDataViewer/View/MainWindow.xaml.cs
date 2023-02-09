using CompMs.App.RawDataViewer.ViewModel;
using Microsoft.Win32;
using Reactive.Bindings.Notifiers;
using System;
using System.Linq;
using System.Reactive.Linq;
using System.Windows;
using System.Windows.Input;

namespace CompMs.App.RawDataViewer.View
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow() {
            InitializeComponent();

            CommandBindings.Add(new CommandBinding(ApplicationCommands.Open, OpenRawFile));

            MessageBroker.Default.ToObservable<SummarizedDataViewModel>()
                .Where(vm => !(vm is null))
                .Subscribe(ShowSummarizedDataView);

            var drop = Observable.FromEvent<DragEventHandler, DragEventArgs>(
                h => (s, e) => h(e),
                h => Drop += h,
                h => Drop -= h);
            drop.Subscribe(DragDropFile);
        }

        private void OpenRawFile(object sender, ExecutedRoutedEventArgs e) {
            var ofd = new OpenFileDialog
            {
                Title = "Please select raw data file.",
                RestoreDirectory = true,
            };
            if (ofd.ShowDialog() == true) {
                FileNameInputBox.Text = ofd.FileName;
            }
        }

        private void DragDropFile(DragEventArgs e) { 
            if (e.Data.GetDataPresent(DataFormats.FileDrop)) {
                var datas = e.Data.GetData(DataFormats.FileDrop) as string[];
                FileNameInputBox.Text = datas.FirstOrDefault();
            }
            if (e.Data.GetDataPresent(DataFormats.Text)) {
                var data = e.Data.GetData(DataFormats.Text) as string;
                FileNameInputBox.Text = data;
            }
        }

        private void ShowSummarizedDataView(SummarizedDataViewModel vm) {
            var dialog = new Window
            {
                Content = new SummarizedDataView(),
                DataContext = vm,
                Owner = this,
                Title = vm.AnalysisDataModel.AnalysisFile.AnalysisFileName,
                Width = 1200,
                Height = 800,
            };
            dialog.Show();
        }

        private void ShowMsSpectrumIntensityCheckView(MsSpectrumIntensityCheckViewModel vm) {
            var dialog = new Window
            {
                Content = new MsSpectrumIntensityCheckView(),
                DataContext = vm,
                Owner = this,
                Title = vm.AnalysisFile.Value.AnalysisFileName,
                Width = 1200,
                Height = 800,
            };
            dialog.Show();
        }

        private void ShowMsPeakSpotsCheckView(MsPeakSpotsCheckViewModel vm) {
            var dialog = new Window
            {
                Content = new MsPeakSpotsCheckView(),
                DataContext = vm,
                Owner = this,
                Title = vm.AnalysisFile.Value.AnalysisFileName,
                Width = 1200,
                Height = 800,
            };
            dialog.Show();
        }
    }
}
