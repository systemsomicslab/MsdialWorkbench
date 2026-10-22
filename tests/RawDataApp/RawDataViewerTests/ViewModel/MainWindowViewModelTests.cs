using CompMs.App.RawDataViewer.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;

namespace CompMs.App.RawDataViewer.ViewModel.Tests
{
    [TestClass]
    public class MainWindowViewModelTests
    {
        private string _file;

        [TestInitialize]
        public void Setup() {
            TestApplication.Ensure();
            // The path only has to exist to pass validation. RawDataHandler reads no file of this extension,
            // so loading fails quickly and nothing is written beside it.
            _file = Path.Combine(Path.GetTempPath(), $"RawDataViewerTests_{Guid.NewGuid():N}.unsupported");
            File.WriteAllText(_file, string.Empty);
        }

        [TestCleanup]
        public void Cleanup() {
            File.Delete(_file);
        }

        [TestMethod]
        public void LoadSelectsTheNewEntryAndAsksForItsSummaryWindow() {
            var published = new List<SummarizedDataViewModel>();
            using var subscription = MessageBroker.Default.Subscribe<SummarizedDataViewModel>(published.Add);
            SummarizedDataViewModel selected = null, entry = null;
            MainWindowViewModel viewModel = null;

            // The window builds its view model on the UI thread, in XAML, and the Load button runs the command there.
            Application.Current.Dispatcher.Invoke(() => {
                viewModel = new MainWindowViewModel();
                viewModel.FilePath.Value = _file;
                viewModel.LoadAnalysisDataCommand.Execute();
                selected = viewModel.SelectedSummarizedDataViewModel.Value;
                entry = viewModel.SummarizedDataViewModels.Count == 1 ? viewModel.SummarizedDataViewModels[0] : null;
            });

            try {
                Assert.IsNotNull(entry, "the loaded file is listed as soon as Load returns");
                Assert.AreSame(entry, selected, "the loaded file is selected");
                Assert.AreEqual(1, published.Count, "MainWindow is asked to show one summary window");
                Assert.AreSame(entry, published[0]);
            }
            finally {
                Application.Current.Dispatcher.Invoke(() => viewModel.Dispose());
                TestApplication.WaitForBackgroundWork();
            }
        }
    }
}
