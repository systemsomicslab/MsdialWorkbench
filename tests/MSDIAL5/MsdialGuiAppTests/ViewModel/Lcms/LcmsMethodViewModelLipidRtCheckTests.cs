using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.Lcms;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.App.Msdial.ViewModel.Setting;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System.Collections.Generic;
using System.Threading;
using System.Windows;

namespace CompMs.App.Msdial.ViewModel.Lcms.Tests
{
    /// <summary>
    /// "Re-run lipid RT check" leaves its windows to the view: the view model publishes the error box and the setting window's
    /// view model on the broker, and MainWindow shows them.
    /// </summary>
    [TestClass]
    public class LcmsMethodViewModelLipidRtCheckTests
    {
        [TestInitialize]
        public void Setup() {
            EnsureApplication();
        }

        // The method view model observes on the UI dispatcher, so it is built where the application's UI thread would build it:
        // on an application whose dispatcher runs on its own STA thread, as in LipidRtCheckViewTests.
        private static void EnsureApplication() {
            if (Application.Current is not null) {
                return;
            }
            using var ready = new ManualResetEventSlim();
            var thread = new Thread(() => {
                _ = new Application { ShutdownMode = ShutdownMode.OnExplicitShutdown, };
                ready.Set();
                System.Windows.Threading.Dispatcher.Run();
            })
            {
                IsBackground = true,
            };
            thread.SetApartmentState(ApartmentState.STA);
            thread.Start();
            ready.Wait();
        }

        [TestMethod]
        public void WithoutAnAlignmentResultTheViewIsAskedForTheErrorBoxShownBefore() {
            var broker = new MessageBroker();
            var errors = new List<ErrorMessageBoxRequest>();
            var windows = new List<LipidRtCheckSettingViewModel>();
            broker.Subscribe<ErrorMessageBoxRequest>(errors.Add);
            broker.Subscribe<LipidRtCheckSettingViewModel>(windows.Add);

            Application.Current.Dispatcher.Invoke(() => {
                using var method = CreateMethod(broker);
                using var viewModel = LcmsMethodViewModel.Create(method, broker);

                // The ribbon button passes no command parameter.
                Assert.IsTrue(viewModel.ShowLipidRtCheckSettingCommand.CanExecute(null));
                viewModel.ShowLipidRtCheckSettingCommand.Execute(null);
            });

            Assert.AreEqual(1, errors.Count);
            Assert.AreEqual("Please select an alignment result file.", errors[0].Content);
            Assert.AreEqual("Error", errors[0].Caption);
            Assert.AreEqual(MessageBoxButton.OK, errors[0].ButtonType);
            Assert.AreEqual(0, windows.Count, "no setting window without an alignment result");
        }

        // An LC-MS method over an empty project: nothing is loaded, so no result is selected.
        private static LcmsMethodModel CreateMethod(IMessageBroker broker) {
            var parameter = new MsdialLcmsParameter();
            var storage = new MsdialLcmsDataStorage
            {
                MsdialLcmsParameter = parameter,
                DataBases = DataBaseStorage.CreateEmpty(),
                DataBaseMapper = new DataBaseMapper(),
            };
            var analysisFiles = new AnalysisFileBeanModelCollection();
            var alignmentFiles = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), new List<AnalysisFileBean>());
            return new LcmsMethodModel(analysisFiles, alignmentFiles, storage, new NoRawData(), new FilePropertiesModel(parameter.ProjectParam), new StudyContextModel(parameter.ProjectParam), broker);
        }

        private sealed class NoRawData : IDataProviderFactory<AnalysisFileBean>
        {
            public IDataProvider Create(AnalysisFileBean source) => throw new AssertFailedException("The raw data were asked for.");
        }
    }
}
