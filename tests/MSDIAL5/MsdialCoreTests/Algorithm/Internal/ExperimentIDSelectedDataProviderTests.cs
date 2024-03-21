using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Algorithm.Internal.Tests;

[TestClass()]
public class ExperimentIDSelectedDataProviderTests
{

    [TestMethod]
    public void LoadMs1Spectrums_WithMatchingExperimentID_ReturnsFilteredSpectra()
    {
        var dataProvider = new StubDataProvider();
        var experimentID = 1;
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = provider.LoadMs1Spectrums();

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID));
    }

    [TestMethod]
    public async Task LoadMs1SpectrumsAsync_WithMatchingExperimentID_ReturnsFilteredSpectra()
    {
        var dataProvider = new StubDataProvider();
        var experimentID = 1;
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = await provider.LoadMs1SpectrumsAsync(CancellationToken.None);

        Assert.AreEqual(1, result.Count);
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID));
    }

    [DataTestMethod]
    [DataRow(2, 1)]
    [DataRow(3, 2)]
    public void LoadMsNSpectrums_WithMatchingExperimentIDAndLevel_ReturnsFilteredSpectra(int experimentID, int expectedCount)
    {
        var dataProvider = new StubDataProvider();
        var msLevel = 2;
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = provider.LoadMsNSpectrums(msLevel);

        Assert.AreEqual(expectedCount, result.Count, $"Filtered spectra count should be {expectedCount}.");
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID), "All spectra should have the matching experiment ID.");
    }

    [DataTestMethod]
    [DataRow(2, 1)]
    [DataRow(3, 2)]
    public async Task LoadMsNSpectrumsAsync_WithMatchingExperimentIDAndLevel_ReturnsFilteredSpectra(int experimentID, int expectedCount)
    {
        var dataProvider = new StubDataProvider();
        var msLevel = 2; // 仮のMSレベル値
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = await provider.LoadMsNSpectrumsAsync(msLevel, CancellationToken.None);

        Assert.AreEqual(expectedCount, result.Count, $"Filtered spectra count should be {expectedCount}.");
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID), "All spectra should have the matching experiment ID.");
    }

    [TestMethod]
    public void LoadMsSpectrums_WithMatchingExperimentID_ReturnsFilteredSpectra()
    {
        var dataProvider = new StubDataProvider();
        var experimentID = 1;
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = provider.LoadMsSpectrums();

        Assert.AreEqual(1, result.Count, "Filtered spectra count should be 1.");
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID), "All returned spectra should have the matching experiment ID.");
    }

    [TestMethod]
    public async Task LoadMsSpectrumsAsync_WithMatchingExperimentID_ReturnsFilteredSpectra()
    {
        var dataProvider = new StubDataProvider();
        var experimentID = 1;
        var provider = new ExperimentIDSelectedDataProvider(dataProvider, experimentID);

        var result = await provider.LoadMsSpectrumsAsync(CancellationToken.None);

        Assert.IsTrue(result.Count > 0, "Expected non-empty collection of spectra.");
        Assert.IsTrue(result.All(s => s.ExperimentID == experimentID), "All returned spectra should have the matching experiment ID.");
    }

    private class StubDataProvider : IDataProvider
    {
        public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() =>
            LoadMsNSpectrums(1);

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) =>
            Task.FromResult(LoadMs1Spectrums());

        public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) =>
            LoadMsSpectrums().Where(s => s.MsLevel == level).ToList().AsReadOnly();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) =>
            Task.FromResult(LoadMsNSpectrums(level));

        public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() =>
            new List<RawSpectrum>
            {
                new() { ExperimentID = 1, MsLevel = 1, },
                new() { ExperimentID = 2, MsLevel = 2, },
                new() { ExperimentID = 3, MsLevel = 2, },
                new() { ExperimentID = 3, MsLevel = 2, },
            }.AsReadOnly();

        public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) =>
            Task.FromResult(LoadMsSpectrums());
    }
}
