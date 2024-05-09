using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal.Tests;

[TestClass()]
public class CachedDataProviderTests
{
    private IDataProvider provider;

    [TestInitialize]
    public void Setup() {
        provider = new CachedDataProvider(new StubDataProvider());
    }

    [TestMethod()]
    public void LoadMs1SpectrumsTest() {
        var actual1 = provider.LoadMs1Spectrums();
        var actual2 = provider.LoadMs1Spectrums();
        Assert.AreEqual(actual1, actual2);
    }

    [TestMethod()]
    public void LoadMs1SpectrumsAsyncTest() {
        var actual1 = provider.LoadMs1SpectrumsAsync(default).Result;
        var actual2 = provider.LoadMs1SpectrumsAsync(default).Result;
        Assert.AreEqual(actual1, actual2);
    }

    [TestMethod()]
    public void LoadMsNSpectrumsTest() {
        var actual1 = provider.LoadMsNSpectrums(1);
        var actual2 = provider.LoadMsNSpectrums(1);
        var actual3 = provider.LoadMsNSpectrums(2);
        Assert.AreEqual(actual1, actual2);
        Assert.AreNotEqual(actual1, actual3);
    }

    [TestMethod()]
    public void LoadMsNSpectrumsAsyncTest() {
        var actual1 = provider.LoadMsNSpectrumsAsync(1, default).Result;
        var actual2 = provider.LoadMsNSpectrumsAsync(1, default).Result;
        var actual3 = provider.LoadMsNSpectrumsAsync(2, default).Result;
        Assert.AreEqual(actual1, actual2);
        Assert.AreNotEqual(actual1, actual3);
    }

    [TestMethod()]
    public void LoadMsSpectrumsTest() {
        var actual1 = provider.LoadMsSpectrums();
        var actual2 = provider.LoadMsSpectrums();
        Assert.AreEqual(actual1, actual2);
    }

    [TestMethod()]
    public void LoadMsSpectrumsAsyncTest() {
        var actual1 = provider.LoadMsSpectrumsAsync(default).Result;
        var actual2 = provider.LoadMsSpectrumsAsync(default).Result;
        Assert.AreEqual(actual1, actual2);
    }

    class StubDataProvider : IDataProvider
    {
        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() {
            return new List<RawSpectrum>(0).AsReadOnly();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) {
            return Task.FromResult(new List<RawSpectrum>(0).AsReadOnly());
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) {
            return new List<RawSpectrum>(0).AsReadOnly();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) {
            return Task.FromResult(new List<RawSpectrum>(0).AsReadOnly());
        }

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() {
            return new List<RawSpectrum>(0).AsReadOnly();
        }

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) {
            return Task.FromResult(new List<RawSpectrum>(0).AsReadOnly());
        }
    }
}