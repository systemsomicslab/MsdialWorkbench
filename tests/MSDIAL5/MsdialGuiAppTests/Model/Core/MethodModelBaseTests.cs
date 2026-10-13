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

namespace CompMs.App.Msdial.Model.Core.Tests
{
    [TestClass()]
    public class MethodModelBaseTests
    {
        private string _folder = string.Empty;
        private MessageBroker _broker = new MessageBroker();
        private List<ErrorMessageBoxRequest> _messages = new List<ErrorMessageBoxRequest>();

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-methodmodel-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            _broker = new MessageBroker();
            _messages = new List<ErrorMessageBoxRequest>();
            _broker.Subscribe<ErrorMessageBoxRequest>(_messages.Add);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public async Task AFirstSampleThatFailsLeavesTheProjectOpenWithNoSample() {
            var files = new[] { Sample("a"), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            method.FailOn.Add(files[0]);

            await method.LoadAsync(default);

            Assert.IsNull(method.AnalysisFileModel);
            Assert.IsNull(method.AnalysisModelBase);
            Assert.AreEqual(1, _messages.Count);
            Assert.AreEqual("Sample could not be opened", _messages[0].Caption);
            StringAssert.Contains(_messages[0].Content, "InvalidOperationException: a failed");
        }

        [TestMethod()]
        public async Task AlignmentsOnlyOpensTheFirstSampleWithRawData() {
            var files = new[] { Sample("a", raw: false), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.AlignmentsOnly, files);

            await method.LoadAsync(default);

            CollectionAssert.AreEqual(new[] { files[1] }, method.Opened);
            Assert.AreSame(files[1], method.AnalysisFileModel);
            Assert.AreEqual(0, _messages.Count);
        }

        [TestMethod()]
        public async Task SupportedOpensTheFirstSampleWithoutRawData() {
            var files = new[] { Sample("a", raw: false), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.Supported, files);

            await method.LoadAsync(default);

            CollectionAssert.AreEqual(new[] { files[0] }, method.Opened);
            Assert.AreEqual(0, _messages.Count);
        }

        [TestMethod()]
        public async Task ASampleWithoutItsPeakListIsSkippedAndRefused() {
            var files = new[] { Sample("a", peakList: false), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.Supported, files);

            await method.LoadAsync(default);
            CollectionAssert.AreEqual(new[] { files[1] }, method.Opened);
            Assert.AreEqual(0, _messages.Count);

            await method.LoadAnalysisFileAsync(files[0], default);

            CollectionAssert.AreEqual(new[] { files[1] }, method.Opened);
            Assert.AreSame(files[1], method.AnalysisFileModel);
            Assert.AreEqual("Saved results not found", _messages.Single().Caption);
            StringAssert.Contains(_messages[0].Content, files[0].PeakListFilePath);
        }

        [TestMethod()]
        public async Task AlignmentsOnlyRefusesASampleWithoutRawDataAndKeepsTheCurrentOne() {
            var files = new[] { Sample("a"), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.AlignmentsOnly, files);
            FindRawDataAsTheDatasetDoes(files);
            await method.LoadAsync(default);
            Assert.AreSame(files[0], method.AnalysisFileModel);
            var current = method.AnalysisModelBase;
            // Removed after the project opened: opening the sample by hand checks the disk again.
            File.Delete(files[1].AnalysisFilePath);
            Assert.IsTrue(files[1].IsRawDataAvailable);

            await method.LoadAnalysisFileAsync(files[1], default);

            CollectionAssert.AreEqual(new[] { files[0] }, method.Opened);
            Assert.AreSame(files[0], method.AnalysisFileModel);
            Assert.AreSame(current, method.AnalysisModelBase);
            Assert.AreEqual(0, ((FakeAnalysisModel)current!).Saves, "The current sample is not saved again when the next one is refused.");
            Assert.AreEqual("Raw data not available", _messages.Single().Caption);
        }

        [TestMethod()]
        public async Task AlignmentsOnlyFirstOpensWhatTheDatasetFoundWithoutCheckingTheDiskAgain() {
            var files = new[] { Sample("a"), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.AlignmentsOnly, files);
            FindRawDataAsTheDatasetDoes(files);
            // Gone between the dataset's check and the first open, which uses what the dataset found a moment before.
            File.Delete(files[0].AnalysisFilePath);

            await method.LoadAsync(default);

            CollectionAssert.AreEqual(new[] { files[0] }, method.Opened);
            Assert.IsTrue(files[0].IsRawDataAvailable);
            Assert.AreEqual(0, _messages.Count);
        }

        [TestMethod()]
        public async Task ASwitchThatFailsResetsTheSelectionInsteadOfThrowing() {
            var files = new[] { Sample("a"), Sample("b"), };
            using var method = CreateMethod(BrowsingWithoutRawData.Supported, files);
            await method.LoadAsync(default);
            method.FailOn.Add(files[1]);

            await method.LoadAnalysisFileAsync(files[1], default);

            Assert.IsNull(method.AnalysisFileModel);
            Assert.IsNull(method.AnalysisModelBase);
            Assert.AreEqual("Sample could not be opened", _messages.Single().Caption);

            await method.LoadAnalysisFileAsync(files[0], default);
            Assert.AreSame(files[0], method.AnalysisFileModel);
        }

        [TestMethod()]
        public async Task ImagingChecksNothingBeforeOpening() {
            var files = new[] { Sample("a", raw: false, peakList: false), };
            using var method = CreateMethod(BrowsingWithoutRawData.NotSupported, files);

            await method.LoadAsync(default);

            CollectionAssert.AreEqual(new[] { files[0] }, method.Opened);
            Assert.AreEqual(0, _messages.Count);
        }

        [TestMethod()]
        public async Task AnAlignmentResultThatFailsLeavesTheProjectOpen() {
            var files = new[] { Sample("a"), };
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean> { new AlignmentFileBean { FileName = "AlignmentResult", }, }, files.Select(f => f.File).ToList());
            using var method = new FakeMethodModel(BrowsingWithoutRawData.Supported, new AnalysisFileBeanModelCollection(files), alignments, _broker);

            await method.LoadAlignmentFileAsync(alignments.Files[0], default);

            Assert.IsNull(method.AlignmentFile);
            Assert.IsNull(method.AlignmentModelBase);
            Assert.AreEqual("Alignment result could not be opened", _messages.Single().Caption);
        }

        private AnalysisFileBeanModel Sample(string name, bool raw = true, bool peakList = true) {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = name;
            file.AnalysisFileIncluded = true;
            file.AnalysisFilePath = Path.Combine(_folder, name + ".mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, name + ".pai");
            if (raw) {
                File.WriteAllBytes(file.AnalysisFilePath, new byte[0]);
            }
            if (peakList) {
                File.WriteAllBytes(file.PeakListFilePath, new byte[0]);
            }
            return file;
        }

        // DatasetModel.LoadAsync checks every sample's raw data before the method opens one, and the result is remembered.
        private static void FindRawDataAsTheDatasetDoes(AnalysisFileBeanModel[] files) {
            foreach (var file in files) {
                Assert.IsTrue(file.RefreshRawDataAvailability());
            }
        }

        private FakeMethodModel CreateMethod(BrowsingWithoutRawData mode, AnalysisFileBeanModel[] files) {
            var alignments = new AlignmentFileBeanModelCollection(new List<AlignmentFileBean>(), files.Select(f => f.File).ToList());
            return new FakeMethodModel(mode, new AnalysisFileBeanModelCollection(files), alignments, _broker);
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
            public HashSet<AnalysisFileBeanModel> FailOn { get; } = new HashSet<AnalysisFileBeanModel>();

            protected override IAnalysisModel? LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
                if (FailOn.Contains(analysisFile)) {
                    throw new AggregateException(new InvalidOperationException($"{analysisFile.AnalysisFileName} failed"));
                }
                Opened.Add(analysisFile);
                return new FakeAnalysisModel();
            }

            protected override IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) {
                throw new NullReferenceException("No aligned spot information.");
            }

            public override Task RunAsync(ProcessOption option, CancellationToken token) => Task.CompletedTask;
        }

        sealed class FakeAnalysisModel : BindableBase, IAnalysisModel
        {
            public int Saves { get; private set; }

            public Task SaveAsync(CancellationToken token) {
                Saves++;
                return Task.CompletedTask;
            }

            public void SearchFragment() => throw new NotImplementedException();
            public void InvokeMsfinder() => throw new NotImplementedException();
            public void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworkingForTargetSpot() => throw new NotImplementedException();
        }
    }
}
