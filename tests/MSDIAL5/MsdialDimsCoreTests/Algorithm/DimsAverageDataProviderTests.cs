using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using MsdialCoreTestHelper.DataProvider;

namespace CompMs.MsdialDimsCore.Algorithm.Tests;

[TestClass()]
public class DimsAverageDataProviderTests
{
    [TestMethod()]
    public void DimsAverageDataProviderTest() {
        var stubprovider = new StubDataProvider();
        stubprovider.SetSpectra([
            new() { MsLevel = 1, Spectrum = [Peak(300d, 30d), Peak(600.005d, 10d), Peak(800d, 20d), ], },
            new() { MsLevel = 1, Spectrum = [Peak(300.008d, 30d), Peak(600d, 10d), Peak(800.005d, 20d), ], },
            new() { MsLevel = 1, Spectrum = [Peak(600d, 10d), Peak(800d, 20d), ], },
        ]);
        var provider = new DimsAverageDataProvider(stubprovider, 0.01, 0d, 100d);

        var spectra = provider.LoadMs1Spectrums();
        Assert.AreEqual(1, spectra.Count);

        var spectrum = spectra[0];
        Assert.AreEqual(3, spectrum.Spectrum.Length);
        Assert.AreEqual(300d, spectrum.Spectrum[0].Mz, .02);
        Assert.AreEqual(20d, spectrum.Spectrum[0].Intensity);
        Assert.AreEqual(600d, spectrum.Spectrum[1].Mz, .02);
        Assert.AreEqual(10d, spectrum.Spectrum[1].Intensity);
        Assert.AreEqual(800d, spectrum.Spectrum[2].Mz, .02);
        Assert.AreEqual(20d, spectrum.Spectrum[2].Intensity);
    }

    private RawPeakElement Peak(double mz, double intensity) => new RawPeakElement() { Mz = mz, Intensity = intensity, };
}