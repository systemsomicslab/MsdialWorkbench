using CompMs.MsdialCore.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using MsdialCoreTestHelper.DataProvider;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class RawSpectraTests
    {
        [TestMethod]
        public async Task GetMs1ChromatogramRtTest() {
            List<RawSpectrum> rawSpectra = new List<RawSpectrum>
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            };
            var provider = new StubDataProvider { Spectra = rawSpectra };
            var spectra = new RawSpectra(rawSpectra, IonMode.Positive, AcquisitionType.DDA, provider);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = (await spectra.GetMS1ExtractedChromatogramAsync(new MzRange(102, 2d), chromatogramRange, default)).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Length);
            Assert.AreEqual(1, chromatogram[0].Id);
            Assert.AreEqual(101d, chromatogram[0].Mz);
            Assert.AreEqual(1001d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].Time);
            Assert.AreEqual(2, chromatogram[1].Id);
            Assert.AreEqual(102d, chromatogram[1].Mz);
            Assert.AreEqual(1002d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].Time);
            Assert.AreEqual(3, chromatogram[2].Id);
            Assert.AreEqual(103d, chromatogram[2].Mz);
            Assert.AreEqual(1003d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].Time);
        }

        [TestMethod]
        public async Task GetMs1ChromatogramDtTest() {
            List<RawSpectrum> rawSpectra = new List<RawSpectrum>
            {
                new RawSpectrum { Index = 0, DriftTime = 1d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, } } },
                new RawSpectrum { Index = 1, DriftTime = 2d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, } } },
                new RawSpectrum { Index = 2, DriftTime = 3d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, } } },
                new RawSpectrum { Index = 3, DriftTime = 4d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, } } },
                new RawSpectrum { Index = 4, DriftTime = 5d, ScanPolarity = ScanPolarity.Negative, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, } } },
            };
            var provider = new StubDataProvider { Spectra = rawSpectra };
            var spectra = new RawSpectra(rawSpectra, IonMode.Negative, AcquisitionType.DDA, provider);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.Drift, ChromXUnit.Msec);
            var chromatogram = (await spectra.GetMS1ExtractedChromatogramAsync(new MzRange(102, 2d), chromatogramRange, default)).AsPeakArray();
            Assert.AreEqual(3, chromatogram.Length);
            Assert.AreEqual(1, chromatogram[0].Id);
            Assert.AreEqual(101d, chromatogram[0].Mz);
            Assert.AreEqual(1001d, chromatogram[0].Intensity);
            Assert.AreEqual(2d, chromatogram[0].Time);
            Assert.AreEqual(2, chromatogram[1].Id);
            Assert.AreEqual(102d, chromatogram[1].Mz);
            Assert.AreEqual(1002d, chromatogram[1].Intensity);
            Assert.AreEqual(3d, chromatogram[1].Time);
            Assert.AreEqual(3, chromatogram[2].Id);
            Assert.AreEqual(103d, chromatogram[2].Mz);
            Assert.AreEqual(1003d, chromatogram[2].Intensity);
            Assert.AreEqual(4d, chromatogram[2].Time);
        }

        [TestMethod()]
        public async Task GetMs1TotalIonChromatogramTest() {
            List<RawSpectrum> rawSpectra = new List<RawSpectrum>
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            };
            var provider = new StubDataProvider { Spectra = rawSpectra };
            var spectra = new RawSpectra(rawSpectra, IonMode.Positive, AcquisitionType.DDA, provider);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = (await spectra.GetMS1TotalIonChromatogramAsync(chromatogramRange, default).ConfigureAwait(false)).AsPeakArray();
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
        public async Task GetMs1BasePeakChromatogramTest() {
            List<RawSpectrum> rawSpectra = new List<RawSpectrum>
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, }, } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, }, } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, }, } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, }, } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, }, } },
            };
            var provider = new StubDataProvider { Spectra = rawSpectra };
            var spectra = new RawSpectra(rawSpectra, IonMode.Positive, AcquisitionType.DDA, provider);
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = (await spectra.GetMS1BasePeakChromatogramAsync(chromatogramRange, default)).AsPeakArray();
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

        [TestMethod()]
        public async Task GetMs1ExtractedChromatogramByHighestBasePeakMzTest() {
            List<RawSpectrum> rawSpectra = new List<RawSpectrum>
            {
                new RawSpectrum { Index = 0, ScanStartTime = 1d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 100d, Intensity = 1000d, }, new RawPeakElement{ Mz = 105d, Intensity = 1005d, },  } },
                new RawSpectrum { Index = 1, ScanStartTime = 2d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 101d, Intensity = 1001d, }, new RawPeakElement{ Mz = 106d, Intensity = 1006d, },  } },
                new RawSpectrum { Index = 2, ScanStartTime = 3d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 102d, Intensity = 1002d, }, new RawPeakElement{ Mz = 107d, Intensity = 1007d, },  } },
                new RawSpectrum { Index = 3, ScanStartTime = 4d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 103d, Intensity = 1003d, }, new RawPeakElement{ Mz = 108d, Intensity = 1008d, },  } },
                new RawSpectrum { Index = 4, ScanStartTime = 5d, ScanPolarity = ScanPolarity.Positive, MsLevel = 1, Spectrum = new[] { new RawPeakElement{ Mz = 104d, Intensity = 1004d, }, new RawPeakElement{ Mz = 109d, Intensity = 1009d, },  } },
            };
            var provider = new StubDataProvider() { Spectra = rawSpectra };

            var spectra = new RawSpectra(rawSpectra, IonMode.Positive, AcquisitionType.DDA, provider);
            var features = new[]
            {
                new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 103d, PeakHeightTop = 1000d, }),
                new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 102d, PeakHeightTop = 3000d, }),
                new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 104d, PeakHeightTop = 2000d, }),
                new ChromatogramPeakFeature(new BaseChromatogramPeakFeature { Mass = 101d, PeakHeightTop = 1000d, }),
            };
            var chromatogramRange = new ChromatogramRange(2d, 4d, ChromXType.RT, ChromXUnit.Min);
            var chromatogram = (await spectra.GetMS1ExtractedChromatogramByHighestBasePeakMzAsync(features, 2d, chromatogramRange, default)).AsPeakArray();
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
    }
}
