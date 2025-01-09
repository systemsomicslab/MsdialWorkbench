using CompMs.Common.DataObj;
using CompMs.Raw.Abstractions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsdialCoreTestHelper.DataProvider;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Internal.Tests;

[TestClass()]
public class CachedDataProviderTests
{
    private IDataProvider provider;

    [TestInitialize]
    public void Setup() {
        List<RawSpectrum> rawSpectra =
        [
            new RawSpectrum { Index = 0, ScanStartTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 1, ScanStartTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 2, ScanStartTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 3, ScanStartTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 4, ScanStartTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50.2d, Intensity = 2000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 5, ScanStartTime = 6d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 6, ScanStartTime = 7d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
            new RawSpectrum { Index = 7, ScanStartTime = 8d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
        ];
        provider = new CachedDataProvider(new StubDataProvider() { Spectra = rawSpectra, });
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
}