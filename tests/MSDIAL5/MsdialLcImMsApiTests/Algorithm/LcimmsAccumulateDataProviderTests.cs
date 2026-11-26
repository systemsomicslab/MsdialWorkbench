using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialLcImMsApi.Algorithm.Tests;

[TestClass]
public class LcimmsAccumulateDataProviderTests
{
    [TestMethod]
    public void LoadMs1Spectrums_GroupsDriftSpectraByRetentionTimeEvenWhenEveryScanNumberDiffers() {
        // The mzML reader gives every spectrum its own ScanNumber, so frames can only be told apart by retention time.
        var spectra = new List<RawSpectrum>
        {
            Spectrum(scanNumber: 0, rt: 1.0, msLevel: 1, driftTime: 1, intensity: 10),
            Spectrum(scanNumber: 1, rt: 1.0, msLevel: 1, driftTime: 2, intensity: 20),
            Spectrum(scanNumber: 2, rt: 1.05, msLevel: 2, driftTime: 1, intensity: 1000),
            Spectrum(scanNumber: 3, rt: 1.1, msLevel: 1, driftTime: 1, intensity: 30),
            Spectrum(scanNumber: 4, rt: 1.1, msLevel: 1, driftTime: 2, intensity: 40),
        };
        var provider = new LcimmsAccumulateDataProvider(new RawMeasurement { SpectrumList = spectra });

        var actual = provider.LoadMs1Spectrums();

        Assert.AreEqual(2, actual.Count);
        Assert.AreEqual(1.0, actual[0].ScanStartTime);
        Assert.AreEqual(30d, actual[0].TotalIonCurrent);
        Assert.AreEqual(1.1, actual[1].ScanStartTime);
        Assert.AreEqual(70d, actual[1].TotalIonCurrent);
    }

    private static RawSpectrum Spectrum(int scanNumber, double rt, int msLevel, double driftTime, double intensity) {
        return new RawSpectrum
        {
            Index = scanNumber,
            OriginalIndex = scanNumber,
            ScanNumber = scanNumber,
            ScanStartTime = rt,
            MsLevel = msLevel,
            ScanPolarity = ScanPolarity.Positive,
            DriftTime = driftTime,
            Spectrum = [new RawPeakElement { Mz = 100d, Intensity = intensity, }],
        };
    }
}
