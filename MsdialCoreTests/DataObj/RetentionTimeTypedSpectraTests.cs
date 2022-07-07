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
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100f, Intensity = 1000f, } } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101f, Intensity = 1001f, } } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102f, Intensity = 1002f, } } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103f, Intensity = 1003f, } } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104f, Intensity = 1004f, } } },
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
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100f, Intensity = 1000f, }, new RawPeakElement{ Mz = 105f, Intensity = 1005f, }, } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101f, Intensity = 1001f, }, new RawPeakElement{ Mz = 106f, Intensity = 1006f, }, } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102f, Intensity = 1002f, }, new RawPeakElement{ Mz = 107f, Intensity = 1007f, }, } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103f, Intensity = 1003f, }, new RawPeakElement{ Mz = 108f, Intensity = 1008f, }, } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104f, Intensity = 1004f, }, new RawPeakElement{ Mz = 109f, Intensity = 1009f, }, } },
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
                new RawSpectrum { ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100f, Intensity = 1000f, }, new RawPeakElement{ Mz = 105f, Intensity = 1005f, }, } },
                new RawSpectrum { ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101f, Intensity = 1001f, }, new RawPeakElement{ Mz = 106f, Intensity = 1006f, }, } },
                new RawSpectrum { ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102f, Intensity = 1002f, }, new RawPeakElement{ Mz = 107f, Intensity = 1007f, }, } },
                new RawSpectrum { ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103f, Intensity = 1003f, }, new RawPeakElement{ Mz = 108f, Intensity = 1008f, }, } },
                new RawSpectrum { ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104f, Intensity = 1004f, }, new RawPeakElement{ Mz = 109f, Intensity = 1009f, }, } },
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
