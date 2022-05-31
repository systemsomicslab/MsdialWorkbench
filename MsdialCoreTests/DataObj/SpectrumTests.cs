using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass]
    public class SpectrumTests
    {
        [TestMethod]
        public void RetrieveBinTest() {
            var spectrum = new Spectrum(new[]{
                new RawPeakElement { Mz = 10, Intensity = 101, },
                new RawPeakElement { Mz = 11, Intensity = 102, },
                new RawPeakElement { Mz = 12, Intensity = 104, },
                new RawPeakElement { Mz = 14, Intensity = 103, },
                new RawPeakElement { Mz = 16, Intensity = 105, },
                new RawPeakElement { Mz = 20, Intensity = 106, },
            });

            var (baseMz, baseIntensity, summedIntensity) = spectrum.RetrieveBin(13, 2);
            Assert.AreEqual(12, baseMz);
            Assert.AreEqual(104, baseIntensity);
            Assert.AreEqual(309, summedIntensity);
        }

        [TestMethod()]
        public void RetrieveTotalIntensityTest() {
            var spectrum = new Spectrum(new[]{
                new RawPeakElement { Mz = 10, Intensity = 101, },
                new RawPeakElement { Mz = 11, Intensity = 102, },
                new RawPeakElement { Mz = 12, Intensity = 104, },
                new RawPeakElement { Mz = 14, Intensity = 103, },
                new RawPeakElement { Mz = 16, Intensity = 106, },
                new RawPeakElement { Mz = 20, Intensity = 105, },
            });

            var (baseMz, baseIntensity, summedIntensity) = spectrum.RetrieveTotalIntensity();
            Assert.AreEqual(16, baseMz);
            Assert.AreEqual(106, baseIntensity);
            Assert.AreEqual(621, summedIntensity);
        }
    }
}
