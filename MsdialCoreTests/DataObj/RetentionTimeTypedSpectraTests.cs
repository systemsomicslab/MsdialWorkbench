using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class RetentionTimeTypedSpectraTests
    {
        [TestMethod]
        public void GetMs1ExtractedChromatogramTest() {
            var spectra = new RetentionTimeTypedSpectra(new[]
            {
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            },
            ChromXUnit.Min,
            IonMode.Positive);
            var chromatogram = spectra.GetMs1ExtractedChromatogram(102, 2d, 2d, 4d).Peaks;
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

        [TestMethod()]
        public void GetMs1TotalIonChromatogramTest() {
            var spectra = new RetentionTimeTypedSpectra(new[]
            {
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            },
            ChromXUnit.Min,
            IonMode.Positive);
            var chromatogram = spectra.GetMs1TotalIonChromatogram(2d, 4d).Peaks;
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(1, chromatogram[0].ID);
            Assert.AreEqual(106d, chromatogram[0].Mass);
            Assert.AreEqual(2007d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(107d, chromatogram[1].Mass);
            Assert.AreEqual(2009d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
            Assert.AreEqual(3, chromatogram[2].ID);
            Assert.AreEqual(108d, chromatogram[2].Mass);
            Assert.AreEqual(2011d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
        }

        [TestMethod()]
        public void GetMs1BasePeakChromatogramTest() {
            var spectra = new RetentionTimeTypedSpectra(new[]
            {
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            },
            ChromXUnit.Min,
            IonMode.Positive);
            var chromatogram = spectra.GetMs1BasePeakChromatogram(2d, 4d).Peaks;
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(1, chromatogram[0].ID);
            Assert.AreEqual(106d, chromatogram[0].Mass);
            Assert.AreEqual(1006d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.RT.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(107d, chromatogram[1].Mass);
            Assert.AreEqual(1007d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.RT.Value);
            Assert.AreEqual(3, chromatogram[2].ID);
            Assert.AreEqual(108d, chromatogram[2].Mass);
            Assert.AreEqual(1008d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.RT.Value);
        }
    }
}
