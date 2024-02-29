using CompMs.Common.Components;
using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Utility.Tests
{
    [TestClass()]
    public class DataAccessTests
    {
        [TestMethod()]
        public void GetAverageSpectrumTest() {
            var spectra = new[]
            {
                new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 100, }] },
                new RawSpectrum { Spectrum = [new() { Mz = 200d, Intensity = 80, }] },
                new RawSpectrum { Spectrum = [new() { Mz = 300d, Intensity = 100, }] },
                new RawSpectrum { Spectrum = [new() { Mz = 100d, Intensity = 20, }, new() { Mz = 200d, Intensity = 70d }] },
            };

            var actual = DataAccess.GetAverageSpectrum(spectra, [0, 1, 3,], .01);
            var expected = new[]
            {
                new SpectrumPeak { Mass = 100d, Intensity = 40, },
                new SpectrumPeak { Mass = 200d, Intensity = 50, },
            };

            Assert.AreEqual(expected.Length, actual.Count);
            for (int i = 0; i < expected.Length; i++) {
                Assert.AreEqual(expected[i].Mass, actual[i].Mass);
                Assert.AreEqual(expected[i].Intensity, actual[i].Intensity);
            }
        }
    }
}