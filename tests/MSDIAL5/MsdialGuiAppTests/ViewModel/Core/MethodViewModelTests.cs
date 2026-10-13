using CompMs.App.Msdial.Model.Core;
using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.App.Msdial.ViewModel.Service;
using CompMs.Common.Enum;
using CompMs.CommonMVVM;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Reactive.Bindings.Extensions;
using Reactive.Bindings.Notifiers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.ViewModel.Core.Tests
{
    [TestClass()]
    public class MethodViewModelTests
    {
        private string _folder = string.Empty;
        private MessageBroker _broker = new MessageBroker();
        private List<ErrorMessageBoxRequest> _messages = new List<ErrorMessageBoxRequest>();

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-methodviewmodel-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
            _broker = new MessageBroker();
            _messages = new List<ErrorMessageBoxRequest>();
            _broker.Subscribe<ErrorMessageBoxRequest>(_messages.Add);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        // GC-MS disposes the sample on screen before it builds the next one, as the other method types do.
        [TestMethod()]
        public async Task ASampleThatFailsToOpenLeavesNoViewModelBoundToTheDisposedOne() {
            var files = new[] { Sample("a"), Sample("b"), };
            using var method = new FakeMethodModel(new AnalysisFileBeanModelCollection(files), Alignments(files), _broker);
            using var viewModel = MethodViewModel.ToResultViewModel(method.ObserveProperty(m => m.SelectedAnalysisModel), m => new FakeViewModel(m));
            await method.LoadAsync(default);
            var first = viewModel.Value;
            Assert.IsNotNull(first);
            method.FailOn.Add(files[1]);

            await method.LoadAnalysisFileAsync(files[1], default);

            Assert.IsTrue(first!.Model.IsDisposed);
            Assert.IsNull(viewModel.Value);
            Assert.IsTrue(first.IsDisposed);
            Assert.AreEqual("Sample could not be opened", _messages.Single().Caption);

            await method.LoadAnalysisFileAsync(files[0], default);
            Assert.IsNotNull(viewModel.Value);
            Assert.IsFalse(viewModel.Value!.Model.IsDisposed);
        }

        // DIMS and GC-MS alignment results.
        [TestMethod()]
        public async Task AnAlignmentResultThatFailsToOpenLeavesNoViewModelBoundToTheDisposedOne() {
            var files = new[] { Sample("a"), };
            var alignments = Alignments(files, "AlignmentResult1", "AlignmentResult2");
            using var method = new FakeMethodModel(new AnalysisFileBeanModelCollection(files), alignments, _broker);
            using var viewModel = MethodViewModel.ToResultViewModel(method.ObserveProperty(m => m.SelectedAlignmentModel), m => new FakeViewModel(m));
            await method.LoadAlignmentFileAsync(alignments.Files[0], default);
            var first = viewModel.Value;
            Assert.IsNotNull(first);
            method.FailOn.Add(alignments.Files[1]);

            await method.LoadAlignmentFileAsync(alignments.Files[1], default);

            Assert.IsTrue(first!.Model.IsDisposed);
            Assert.IsNull(viewModel.Value);
            Assert.IsTrue(first.IsDisposed);
            Assert.AreEqual("Alignment result could not be opened", _messages.Single().Caption);
        }

        private AnalysisFileBeanModel Sample(string name) {
            var file = AnalysisFileBeanModelTestHelper.Create();
            file.AnalysisFileName = name;
            file.AnalysisFileIncluded = true;
            file.AnalysisFilePath = Path.Combine(_folder, name + ".mzML");
            file.File.PeakAreaBeanInformationFilePath = Path.Combine(_folder, name + ".pai");
            File.WriteAllBytes(file.AnalysisFilePath, new byte[0]);
            File.WriteAllBytes(file.PeakListFilePath, new byte[0]);
            return file;
        }

        private static AlignmentFileBeanModelCollection Alignments(AnalysisFileBeanModel[] files, params string[] names) {
            return new AlignmentFileBeanModelCollection(names.Select(name => new AlignmentFileBean { FileName = name, }).ToList(), files.Select(f => f.File).ToList());
        }

        /// <summary>
        /// Builds its models as GC-MS does: the model on screen is disposed and taken away before the next one is built, which may fail.
        /// </summary>
        sealed class FakeMethodModel : MethodModelBase
        {
            public FakeMethodModel(AnalysisFileBeanModelCollection files, AlignmentFileBeanModelCollection alignments, IMessageBroker broker)
                : base(files, alignments, new FilePropertiesModel(new ProjectBaseParameter()), broker) {
            }

            public override BrowsingWithoutRawData BrowsingWithoutRawData => BrowsingWithoutRawData.Supported;

            public HashSet<object> FailOn { get; } = new HashSet<object>();

            public FakeModel? SelectedAnalysisModel {
                get => _selectedAnalysisModel;
                private set {
                    var old = _selectedAnalysisModel;
                    if (SetProperty(ref _selectedAnalysisModel, value)) {
                        old?.Dispose();
                    }
                }
            }
            private FakeModel? _selectedAnalysisModel;

            public FakeModel? SelectedAlignmentModel {
                get => _selectedAlignmentModel;
                private set {
                    var old = _selectedAlignmentModel;
                    if (SetProperty(ref _selectedAlignmentModel, value)) {
                        old?.Dispose();
                    }
                }
            }
            private FakeModel? _selectedAlignmentModel;

            protected override IAnalysisModel? LoadAnalysisFileCore(AnalysisFileBeanModel analysisFile) {
                SelectedAnalysisModel = null;
                if (FailOn.Contains(analysisFile)) {
                    throw new InvalidOperationException($"{analysisFile.AnalysisFileName} failed");
                }
                return SelectedAnalysisModel = new FakeModel();
            }

            protected override IAlignmentModel? LoadAlignmentFileCore(AlignmentFileBeanModel alignmentFileModel) {
                SelectedAlignmentModel = null;
                if (FailOn.Contains(alignmentFileModel)) {
                    throw new InvalidOperationException($"{alignmentFileModel.FileName} failed");
                }
                return SelectedAlignmentModel = new FakeModel();
            }

            public override Task RunAsync(ProcessOption option, CancellationToken token) => Task.CompletedTask;
        }

        sealed class FakeModel : BindableBase, IAnalysisModel, IAlignmentModel, IDisposable
        {
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;

            public Task SaveAsync(CancellationToken token) => Task.CompletedTask;
            public Task SaveAsync() => Task.CompletedTask;

            public AlignmentFileBeanModel AlignmentFile => throw new NotImplementedException();
            public AlignmentResultContainer AlignmentResult => throw new NotImplementedException();
            public AlignmentSpotSource AlignmentSpotSource => throw new NotImplementedException();
            public void SearchFragment() => throw new NotImplementedException();
            public void InvokeMsfinder() => throw new NotImplementedException();
            public void ExportMoleculerNetworkingData(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworking(MolecularSpectrumNetworkingBaseParameter parameter, bool useCurrentFiltering) => throw new NotImplementedException();
            public void InvokeMoleculerNetworkingForTargetSpot() => throw new NotImplementedException();
        }

        sealed class FakeViewModel : IDisposable
        {
            public FakeViewModel(FakeModel model) {
                Model = model;
            }

            public FakeModel Model { get; }
            public bool IsDisposed { get; private set; }

            public void Dispose() => IsDisposed = true;
        }
    }
}
