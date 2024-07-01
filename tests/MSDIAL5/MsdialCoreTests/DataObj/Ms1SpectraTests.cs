using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class Ms1SpectraTests
    {
        [TestMethod]
        public void GetMs1ChromatogramRtTest() {
            var spectra = new Ms1Spectra(new[]
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            },
            IonMode.Positive, AcquisitionType.DDA);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = spectra.GetMs1ExtractedChromatogram(102, 2d, chromatogramRange).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(1, chromatogram[0].ID);
            Assert.AreEqual(101d, chromatogram[0].Mass);
            Assert.AreEqual(1001d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(102d, chromatogram[1].Mass);
            Assert.AreEqual(1002d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
            Assert.AreEqual(3, chromatogram[2].ID);
            Assert.AreEqual(103d, chromatogram[2].Mass);
            Assert.AreEqual(1003d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
        }

        [TestMethod]
        public void GetMs1ChromatogramDtTest() {
            var spectra = new Ms1Spectra(new[]
            {
                new RawSpectrum { Index = 0, DriftTime = 1d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { Index = 1, DriftTime = 2d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { Index = 2, DriftTime = 3d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { Index = 3, DriftTime = 4d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { Index = 4, DriftTime = 5d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            },
            IonMode.Negative, AcquisitionType.DDA);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.Drift, ChromXUnit.Msec);
            var chromatogram = spectra.GetMs1ExtractedChromatogram(102, 2d, chromatogramRange).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(1, chromatogram[0].ID);
            Assert.AreEqual(101d, chromatogram[0].Mass);
            Assert.AreEqual(1001d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(102d, chromatogram[1].Mass);
            Assert.AreEqual(1002d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
            Assert.AreEqual(3, chromatogram[2].ID);
            Assert.AreEqual(103d, chromatogram[2].Mass);
            Assert.AreEqual(1003d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
        }
    }
}
