using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class DriftTimeTypedSpectraTests
    {
        [TestMethod]
        public void GetMs1ExtractedChromatogramTest() {
            var spectra = new DriftTimeTypedSpectra(new[]
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            },
            ChromXUnit.Msec,
            IonMode.Positive, AcquisitionType.DDA);
            var chromatogram = spectra.GetMs1ExtractedChromatogram(102, 2d, 2d, 4d).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(3, chromatogram[0].ID);
            Assert.AreEqual(103d, chromatogram[0].Mass);
            Assert.AreEqual(1003d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(102d, chromatogram[1].Mass);
            Assert.AreEqual(1002d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
            Assert.AreEqual(1, chromatogram[2].ID);
            Assert.AreEqual(101d, chromatogram[2].Mass);
            Assert.AreEqual(1001d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
        }

        [TestMethod()]
        public void GetMs1TotalIonChromatogramTest() {
            var spectra = new DriftTimeTypedSpectra(new[]
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            },
            ChromXUnit.Msec,
            IonMode.Positive, AcquisitionType.DDA);
            var chromatogram = spectra.GetMs1TotalIonChromatogram(2d, 4d).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(3, chromatogram[0].ID);
            Assert.AreEqual(108d, chromatogram[0].Mass);
            Assert.AreEqual(2011d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(107d, chromatogram[1].Mass);
            Assert.AreEqual(2009d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
            Assert.AreEqual(1, chromatogram[2].ID);
            Assert.AreEqual(106d, chromatogram[2].Mass);
            Assert.AreEqual(2007d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
        }

        [TestMethod()]
        public void GetMs1BasePeakChromatogramTest() {
            var spectra = new DriftTimeTypedSpectra(new[]
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, DriftTime = 5d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, DriftTime = 4d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, DriftTime = 2d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            },
            ChromXUnit.Msec,
            IonMode.Positive, AcquisitionType.DDA);
            var chromatogram = spectra.GetMs1BasePeakChromatogram(2d, 4d).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Count);
            Assert.AreEqual(3, chromatogram[0].ID);
            Assert.AreEqual(108d, chromatogram[0].Mass);
            Assert.AreEqual(1008d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].ChromXs.Drift.Value);
            Assert.AreEqual(2, chromatogram[1].ID);
            Assert.AreEqual(107d, chromatogram[1].Mass);
            Assert.AreEqual(1007d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].ChromXs.Drift.Value);
            Assert.AreEqual(1, chromatogram[2].ID);
            Assert.AreEqual(106d, chromatogram[2].Mass);
            Assert.AreEqual(1006d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].ChromXs.Drift.Value);
        }

        [TestMethod()]
        public void GetProductIonChromatogramTest() {
            var spectra = new DriftTimeTypedSpectra(new[]
            {
                new RawSpectrum { Index = 0, DriftTime = 1d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 1, DriftTime = 2d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 2, DriftTime = 3d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 3, DriftTime = 4d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 4, DriftTime = 5d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50.2d, Intensity = 2000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 5, DriftTime = 6d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 200d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 6, DriftTime = 7d, MsLevel = 1, ScanPolarity = ScanPolarity.Positive, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, } },
                new RawSpectrum { Index = 7, DriftTime = 8d, MsLevel = 2, ScanPolarity = ScanPolarity.Positive, Precursor = new RawPrecursorIon { SelectedIonMz = 100d, }, Spectrum = new[] { new RawPeakElement{ Mz = 50d, Intensity = 1000d, }, new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, } },
            },
            ChromXUnit.Msec,
            IonMode.Positive, AcquisitionType.DDA);
            var chromatogram = spectra.GetProductIonChromatogram(new MzRange(100d, .1d), new MzRange(50d, .5d), new ChromatogramRange(3d, 7d, ChromXType.Drift, ChromXUnit.Msec)).AsPeakArray();
            Assert.AreEqual(2, chromatogram.Count);
            Assert.AreEqual(3, chromatogram[0].ID);
            Assert.AreEqual(50d, chromatogram[0].Mass);
            Assert.AreEqual(1000d, chromatogram[0].Intensity);
            Assert.AreEqual(4d, chromatogram[0].ChromXs.Drift.Value);
            Assert.AreEqual(4, chromatogram[1].ID);
            Assert.AreEqual(50.2d, chromatogram[1].Mass);
            Assert.AreEqual(2000d, chromatogram[1].Intensity);
            Assert.AreEqual(5d, chromatogram[1].ChromXs.Drift.Value);
        }
    }
}
