using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;

namespace CompMs.App.Msdial.Model.Core.Tests
{
    [TestClass()]
    public class DatasetModelTests
    {
        private string _folder = string.Empty;
        private MessageBroker _broker = new MessageBroker();
        private List<ErrorMessageBoxRequest> _messages = new List<ErrorMessageBoxRequest>();
        private MessageBoxResult _answer = MessageBoxResult.None;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-datasetmodel-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            _broker = new MessageBroker();
            _messages = new List<ErrorMessageBoxRequest>();
            _answer = MessageBoxResult.None;
            _broker.Subscribe<ErrorMessageBoxRequest>(request => {
                _messages.Add(request);
                request.Result = _answer;
            });
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public async Task ADatasetWithAllItsRawDataOpensWithoutAsking() {
            var files = new AnalysisFileBeanModelCollection(new[] { Sample("a"), Sample("b"), });
            var method = CreateMethod(BrowsingWithoutRawData.Supported, files);

            Assert.AreSame(method, await DatasetModel.OpenMethodAsync(method, files, _broker));

            Assert.AreEqual(0, _messages.Count);
            CollectionAssert.AreEqual(new[] { files.AnalysisFiles[0] }, method.Opened);
            Assert.IsFalse(method.IsDisposed);
        }

        [TestMethod()]
        public async Task DecliningOpensNoSampleAndDisposesTheMethod() {
            var files = new AnalysisFileBeanModelCollection(new[] { Sample("a", raw: false), Sample("b"), });
            var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            _answer = MessageBoxResult.No;

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => DatasetModel.OpenMethodAsync(method, files, _broker));

            Assert.AreEqual(MessageBoxButton.YesNo, _messages.Single().ButtonType);
            StringAssert.StartsWith(_messages[0].Content, "Raw data are not available for 1 of 2 samples in this dataset");
            Assert.AreEqual(0, method.Opened.Count);
            Assert.IsTrue(method.IsDisposed);
        }

        [TestMethod()]
        public async Task AcceptingOpensTheMethodWithWhatTheProjectStores() {
            var files = new AnalysisFileBeanModelCollection(new[] { Sample("a", raw: false), Sample("b"), });
            var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            _answer = MessageBoxResult.Yes;

            Assert.AreSame(method, await DatasetModel.OpenMethodAsync(method, files, _broker));

            Assert.AreEqual(1, _messages.Count);
            CollectionAssert.AreEqual(new[] { files.AnalysisFiles[0] }, method.Opened);
            Assert.IsFalse(files.AnalysisFiles[0].IsRawDataAvailable);
            Assert.IsFalse(method.IsDisposed);
        }

        // Processing reads every sample, so an excluded sample whose raw data are missing is asked about too.
        [TestMethod()]
        public async Task AnExcludedSampleWithoutRawDataIsAskedAbout() {
            var excluded = Sample("a", raw: false);
            excluded.AnalysisFileIncluded = false;
            var files = new AnalysisFileBeanModelCollection(new[] { excluded, Sample("b"), });
            var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            _answer = MessageBoxResult.No;

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => DatasetModel.OpenMethodAsync(method, files, _broker));

            StringAssert.Contains(_messages.Single().Content, excluded.AnalysisFilePath);
            Assert.AreEqual(0, method.Opened.Count);
        }

        [TestMethod()]
        public async Task ImagingWithoutRawDataIsRefusedAndTheMethodDisposed() {
            var files = new AnalysisFileBeanModelCollection(new[] { Sample("a", raw: false), });
            var method = CreateMethod(BrowsingWithoutRawData.NotSupported, files);
            _answer = MessageBoxResult.OK;

            await Assert.ThrowsExactlyAsync<OperationCanceledException>(() => DatasetModel.OpenMethodAsync(method, files, _broker));

            Assert.AreEqual(MessageBoxButton.OK, _messages.Single().ButtonType);
            Assert.AreEqual(0, method.Opened.Count);
            Assert.IsTrue(method.IsDisposed);
        }

        [TestMethod()]
        public async Task AMethodThatFailsToOpenIsDisposed() {
            var files = new AnalysisFileBeanModelCollection(new[] { Sample("a"), });
            var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            method.LoadFails = new InvalidOperationException("The method failed to open.");

            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => DatasetModel.OpenMethodAsync(method, files, _broker));

            Assert.AreEqual(0, _messages.Count);
            Assert.IsTrue(method.IsDisposed);
        }

        private AnalysisFileBeanModel Sample(string name, bool raw = true) {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = name;
            file.AnalysisFileIncluded = true;
            file.AnalysisFilePath = Path.Combine(_folder, name + ".mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, name + ".pai");
            if (raw) {
                File.WriteAllBytes(file.AnalysisFilePath, new byte[0]);
            }
            File.WriteAllBytes(file.PeakListFilePath, new byte[0]);
            return file;
        }

        private FakeMethodModel CreateMethod(BrowsingWithoutRawData mode, AnalysisFileBeanModelCollection files) {
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), files.AnalysisFiles.Select(f => f.File).ToList());
            return new FakeMethodModel(mode, files, alignments, _broker);
        }

        sealed class FakeMethodModel : MethodModelBase
        {
            private readonly BrowsingWithoutRawData _mode;

            public FakeMethodModel(BrowsingWithoutRawData mode, AnalysisFileBeanModelCollection files, AlignmentFileBeanModelCollection alignments, IMessageBroker broker)
                : base(files, alignments, new FilePropertiesModel(new ProjectBaseParameter()), broker) {
                _mode = mode;
            }

            public override BrowsingWithoutRawData BrowsingWithoutRawData => _mode;

            public List<AnalysisFileBeanModel> Opened { get; } = new List<AnalysisFileBeanModel>();
            public Exception? LoadFails { get; set; }
            public bool IsDisposed { get; private set; }

            public override Task LoadAsync(CancellationToken token) {
                if (LoadFails is not null) {
                    return Task.FromException(LoadFails);
                }
                return base.LoadAsync(token);
            }

            protected override IAnalysisModel? LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
                Opened.Add(analysisFile);
                return new FakeAnalysisModel();
            }

            protected override IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) => throw new NotImplementedException();

            public override Task RunAsync(ProcessOption option, CancellationToken token) => Task.CompletedTask;

            protected override void Dispose(bool disposing) {
                IsDisposed = true;
                base.Dispose(disposing);
            }
        }

        sealed class FakeAnalysisModel : BindableBase, IAnalysisModel
        {
            public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
            public void SearchFragment() => throw new NotImplementedException();
            public void InvokeMsfinder() => throw new NotImplementedException();
            public void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworkingForTargetSpot() => throw new NotImplementedException();
        }
    }
}
