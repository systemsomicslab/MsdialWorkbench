using CompMs.App.Msdial.Model.DataObj;
using CompMs.App.Msdial.Model.DataObj.Tests;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.Msdial.Model.Loader.Tests
{
    [TestClass()]
    public class RawDataProviderFactoryTests
    {
        private string _folder = string.Empty;

        [TestInitialize()]
        public void Setup() {
            _folder = Path.Combine(Path.GetTempPath(), $"msdial-providerfactory-{Guid.NewGuid():N}");
            Directory.CreateDirectory(_folder);
        }

        [TestCleanup()]
        public void Cleanup() {
            Directory.Delete(_folder, recursive: true);
        }

        [TestMethod()]
        public void BrowsingAMissingFileGetsTheUnavailableProviderWithoutAskingTheInnerFactory() {
            var inner = new CountingFactory();
            var factory = new BrowsingDataProviderFactory(inner);

            var provider = factory.Create(CreateModel(Path.Combine(_folder, "missing.mzML")));

            Assert.AreSame(RawDataUnavailableProvider.Instance, provider);
            Assert.AreEqual(0, inner.Calls);
        }

        [TestMethod()]
        public void BrowsingAPresentFileAsksTheInnerFactoryOnce() {
            var inner = new CountingFactory();
            var factory = new BrowsingDataProviderFactory(inner);

            var provider = factory.Create(CreateModel(CreatePresentFile()));

            Assert.AreSame(inner.Provider, provider);
            Assert.AreEqual(1, inner.Calls);
        }

        [TestMethod()]
        public void RequiringAMissingFileThrowsWithoutAskingTheInnerFactory() {
            var inner = new CountingFactory();
            var factory = new RawDataRequiredProviderFactory(inner);
            var file = new AnalysisFileBean { AnalysisFileName = "sample", AnalysisFilePath = Path.Combine(_folder, "missing.mzML"), };

            var ex = Assert.ThrowsException<RawDataNotAvailableException>(() => factory.Create(file));

            Assert.AreEqual(file.AnalysisFilePath, ex.FileName);
            Assert.AreEqual("sample", ex.AnalysisFileName);
            Assert.AreEqual(0, inner.Calls);
        }

        [TestMethod()]
        public void RequiringAPresentFileAsksTheInnerFactory() {
            var inner = new CountingFactory();
            var factory = new RawDataRequiredProviderFactory(inner);
            var file = new AnalysisFileBean { AnalysisFileName = "sample", AnalysisFilePath = CreatePresentFile(), };

            Assert.AreSame(inner.Provider, factory.Create(file));
            Assert.AreEqual(1, inner.Calls);
        }

        [TestMethod()]
        public void AnOptionalMissingFileGetsTheUnavailableProvider() {
            var inner = new CountingFactory();
            var factory = new RawDataOptionalProviderFactory(inner);

            var missing = new AnalysisFileBean { AnalysisFileName = "missing", AnalysisFilePath = Path.Combine(_folder, "missing.mzML"), };
            var present = new AnalysisFileBean { AnalysisFileName = "present", AnalysisFilePath = CreatePresentFile(), };

            Assert.AreSame(RawDataUnavailableProvider.Instance, factory.Create(missing));
            Assert.AreEqual(0, inner.Calls);
            Assert.AreSame(inner.Provider, factory.Create(present));
            Assert.AreEqual(1, inner.Calls);
        }

        private string CreatePresentFile() {
            var path = Path.Combine(_folder, "present.mzML");
            File.WriteAllBytes(path, new byte[0]);
            return path;
        }

        private static AnalysisFileBeanModel CreateModel(string path) {
            var model = AnalysisFileBeanModelTestHelper.Create();
            model.AnalysisFilePath = path;
            return model;
        }

        sealed class CountingFactory : IDataProviderFactory<AnalysisFileBean>
        {
            public int Calls { get; private set; }

            public IDataProvider Provider { get; } = new StubProvider();

            public IDataProvider Create(AnalysisFileBean source) {
                Calls++;
                return Provider;
            }
        }

        sealed class StubProvider : IDataProvider
        {
            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => throw new NotImplementedException();
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => throw new NotImplementedException();
            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => throw new NotImplementedException();
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => throw new NotImplementedException();
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => throw new NotImplementedException();
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => throw new NotImplementedException();
        }
    }
}
