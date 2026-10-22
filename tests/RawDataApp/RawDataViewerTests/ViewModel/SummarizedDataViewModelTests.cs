using CompMs.App.RawDataViewer.Model;
using CompMs.App.RawDataViewer.Tests;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.ViewModel.Tests
{
    [TestClass]
    public class SummarizedDataViewModelTests
    {
        private string _directory;

        [ClassInitialize]
        public static void Listen(TestContext _) {
            UnobservedTaskExceptions.Listen();
        }

        [TestInitialize]
        public void Setup() {
            _directory = Path.Combine(Path.GetTempPath(), $"RawDataViewerTests_{Guid.NewGuid():N}");
            Directory.CreateDirectory(_directory);
        }

        [TestCleanup]
        public void Cleanup() {
            // Another test in the run may have made the application, whose dispatcher then does the disposing.
            TestApplication.WaitForBackgroundWork();
            Directory.Delete(_directory, recursive: true);
        }

        [TestMethod]
        public void EveryPanelReportsWhyARawFileCouldNotBeLoaded() {
            var file = UnreadableFile();
            var model = new SummarizedDataModel(new AnalysisDataModel(AnalysisFile(file), MachineCategory.LCMS, IonMode.Positive, useProfile: false));
            var viewModel = new SummarizedDataViewModel(model);
            try {
                Assert.IsTrue(SpinWait.SpinUntil(() =>
                    !(viewModel.MsSpectrumIntensityCheckPanel.Value is null)
                    && !(viewModel.MsPeakSpotsCheckPanel.Value is null)
                    && !(viewModel.RawMsSpectrumCheckPanel.Value is null), TimeSpan.FromSeconds(60)), "every panel stops loading");

                var expected = $"System.Exception: Loading {file} is failed.";
                Assert.AreEqual(expected, viewModel.MsSpectrumIntensityCheckPanel.Value.ErrorMessage);
                Assert.AreEqual(expected, viewModel.MsPeakSpotsCheckPanel.Value.ErrorMessage);
                Assert.AreEqual(expected, viewModel.RawMsSpectrumCheckPanel.Value.ErrorMessage);
                Assert.IsNull(viewModel.MsSpectrumIntensityCheckViewModel.Value);
                Assert.IsNull(viewModel.MsPeakSpotsCheckViewModel.Value);
                Assert.IsNull(viewModel.RawMsSpectrumCheckViewModel.Value);
            }
            finally {
                viewModel.Dispose();
                model.Dispose();
            }
        }

        [TestMethod]
        public void AViewModelMadeAfterItsModelHasLoadedShowsEveryPanelAndLinksAPeakToItsSpectrum() {
            var file = Path.Combine(_directory, "peak.mzML");
            SyntheticMzml.WriteMs1Peak(file, scanCount: 41, apexScan: 20);
            var model = new SummarizedDataModel(new AnalysisDataModel(AnalysisFile(file), MachineCategory.LCMS, IonMode.Positive, useProfile: false));
            SummarizedDataViewModel viewModel = null;
            try {
                // Every model task has ended before the view model subscribes to it, as for a second view model of a
                // file that has loaded: Task.ToObservable then emits each result during the subscription.
                Assert.IsTrue(Task.WhenAll(model.MsSpectrumIntensityCheckModelTask, model.MsPeakSpotsCheckModelTask, model.RawMsSpectrumCheckModelTask)
                    .Wait(TimeSpan.FromSeconds(60)), "the file loads");
                viewModel = new SummarizedDataViewModel(model);

                Assert.IsFalse(viewModel.MsSpectrumIntensityCheckPanel.Value.IsFailed, viewModel.MsSpectrumIntensityCheckPanel.Value.ErrorMessage);
                Assert.IsFalse(viewModel.MsPeakSpotsCheckPanel.Value.IsFailed, viewModel.MsPeakSpotsCheckPanel.Value.ErrorMessage);
                Assert.IsFalse(viewModel.RawMsSpectrumCheckPanel.Value.IsFailed, viewModel.RawMsSpectrumCheckPanel.Value.ErrorMessage);
                Assert.IsNotNull(viewModel.MsSpectrumIntensityCheckViewModel.Value, "the intensity histograms' view model is set");
                Assert.IsNotNull(viewModel.MsPeakSpotsCheckViewModel.Value, "the peak spots' view model is set");
                Assert.IsNotNull(viewModel.RawMsSpectrumCheckViewModel.Value, "the spectrum table's view model is set");
                Assert.AreSame(viewModel.MsSpectrumIntensityCheckPanel.Value.Value, viewModel.MsSpectrumIntensityCheckViewModel.Value);
                Assert.AreSame(viewModel.MsPeakSpotsCheckPanel.Value.Value, viewModel.MsPeakSpotsCheckViewModel.Value);
                Assert.AreSame(viewModel.RawMsSpectrumCheckPanel.Value.Value, viewModel.RawMsSpectrumCheckViewModel.Value);

                var peakSpots = viewModel.MsPeakSpotsCheckViewModel.Value;
                var spectra = viewModel.RawMsSpectrumCheckViewModel.Value;
                var peak = peakSpots.Distribution.Value.Peaks.Single(p => Math.Abs(p.PeakFeature.Mass - SyntheticMzml.PeakMz) < 0.01);
                Assert.AreNotEqual(peak.MS1RawSpectrumIdTop, spectra.SelectedSpectrum.Value.ScanNumber, "the spectrum of the peak top is not the one shown first");

                peakSpots.SelectedPeak.Value = peak;

                Assert.IsNotNull(spectra.SelectedSpectrum.Value, "a spectrum is selected");
                Assert.AreEqual(peak.MS1RawSpectrumIdTop, spectra.SelectedSpectrum.Value.ScanNumber, "selecting the peak selects the spectrum of its top");
            }
            finally {
                viewModel?.Dispose();
                model.Dispose();
            }
        }

        [TestMethod]
        public void AModelTaskThatFaultsAfterItsViewModelIsDisposedLeavesNoUnobservedTaskException() {
            // The file is removed from the list while it loads, which disposes its view model: no panel is left to read
            // the faults.
            var file = UnreadableFile();

            var ended = LoadAndDisposeTheViewModel(file);

            Assert.IsTrue(ended.Wait(TimeSpan.FromSeconds(60)), "every model task ends");
            UnobservedTaskExceptions.Collect();
            Assert.IsFalse(UnobservedTaskExceptions.AnyWith(file), "the faults are observed");
        }

        // Not inlined, so that nothing in the test keeps the model and its tasks from being collected once they have ended.
        [MethodImpl(MethodImplOptions.NoInlining)]
        private static CountdownEvent LoadAndDisposeTheViewModel(string file) {
            var model = new SummarizedDataModel(new AnalysisDataModel(AnalysisFile(file), MachineCategory.LCMS, IonMode.Positive, useProfile: false));
            new SummarizedDataViewModel(model).Dispose();
            TestApplication.WaitForBackgroundWork();

            var tasks = new Task[] { model.MsSpectrumIntensityCheckModelTask, model.MsPeakSpotsCheckModelTask, model.RawMsSpectrumCheckModelTask, };
            Assert.IsFalse(tasks.Any(task => task.IsCompleted), "the view model is disposed before the tasks end");
            var ended = new CountdownEvent(tasks.Length);
            foreach (var task in tasks) {
                // Does not read the task, which would observe its fault.
                task.ContinueWith(_ => ended.Signal(), CancellationToken.None, TaskContinuationOptions.ExecuteSynchronously, TaskScheduler.Default);
            }
            return ended;
        }

        // RawDataHandler reads no file of this extension, so AnalysisDataModel gives up after its five attempts, half a
        // second apart, and every model task of the summary window faults with the same exception.
        private string UnreadableFile() {
            var file = Path.Combine(_directory, "file.unsupported");
            File.WriteAllText(file, string.Empty);
            return file;
        }

        private static AnalysisFileBean AnalysisFile(string file) {
            return new AnalysisFileBean { AnalysisFilePath = file, AnalysisFileName = Path.GetFileName(file), AcquisitionType = AcquisitionType.DDA, };
        }
    }
}
