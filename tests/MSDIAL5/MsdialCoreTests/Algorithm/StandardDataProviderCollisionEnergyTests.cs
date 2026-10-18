using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Tests;

[TestClass()]
public class StandardDataProviderCollisionEnergyTests
{
    // A conforming mzML records the collision energy under precursor/activation only, and the
    // mzML reader leaves RawSpectrum.CollisionEnergy at 0 for such a scan.
    [TestMethod()]
    public void ProductIonScanWithoutSpectrumLevelEnergyTakesItsPrecursorEnergy() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 1, ScanStartTime = 1d, },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 0d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, CollisionEnergy = 35d, }, },
        });

        var spectra = provider.LoadMsSpectrums();

        Assert.AreEqual(0d, spectra[0].CollisionEnergy);
        Assert.AreEqual(35d, spectra[1].CollisionEnergy);
        CollectionAssert.AreEqual(new[] { 35d }, provider.LoadCollisionEnergyTargets().ToArray());
    }

    [TestMethod()]
    public void SpectrumLevelEnergyIsKept() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 20d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, CollisionEnergy = 35d, }, },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 0d, Precursor = new RawPrecursorIon { SelectedIonMz = 335d, CollisionEnergy = 0d, }, },
            new RawSpectrum { Index = 2, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 0d, },
        });

        var spectra = provider.LoadMsSpectrums();

        Assert.AreEqual(20d, spectra[0].CollisionEnergy);
        Assert.AreEqual(0d, spectra[1].CollisionEnergy);
        Assert.AreEqual(0d, spectra[2].CollisionEnergy);
    }

    [TestMethod()]
    public void MsLevelZeroIsStillReadAsMs1() {
        var provider = new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 0, ScanStartTime = 1d, Precursor = new RawPrecursorIon { CollisionEnergy = 35d, }, },
        });

        var spectrum = provider.LoadMsSpectrums()[0];

        Assert.AreEqual(1, spectrum.MsLevel);
        Assert.AreEqual(0d, spectrum.CollisionEnergy);
    }
}
