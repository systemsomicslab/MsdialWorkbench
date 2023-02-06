using CompMs.Common.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Tests
{
    [TestClass()]
    public class ImmsRepresentativeDataProviderTests
    {
        [TestMethod()]
        public void ImmsRepresentativeDataProviderTest() {
            var spectrums = new[]
            {
                CreateRawSpectrum(1, 1, 0, 0, 2.0, 100),
                CreateRawSpectrum(1, 1, 1, 1, 1.5, 200),
                CreateRawSpectrum(1, 1, 2, 2, 1.0, 150),
                CreateRawSpectrum(1, 1, 3, 3, 0.5, 200),
                CreateRawSpectrum(2, 2, 4, 4, 1.8),
                CreateRawSpectrum(2, 2, 5, 5, 1.6),
                CreateRawSpectrum(2, 2, 6, 6, 1.5),
                CreateRawSpectrum(2, 2, 7, 7, 1.2),
                CreateRawSpectrum(2, 2, 8, 8, 0.9),
                CreateRawSpectrum(2, 2, 9, 9, 0.8),
                CreateRawSpectrum(2, 2, 10, 10, 0.4),

                CreateRawSpectrum(3, 1, 11, 11, 2.0, 300),
                CreateRawSpectrum(3, 1, 12, 12, 1.5, 500),
                CreateRawSpectrum(3, 1, 13, 13, 1.0, 750),
                CreateRawSpectrum(3, 1, 14, 14, 0.5, 800),
                CreateRawSpectrum(4, 2, 15, 15, 1.8),
                CreateRawSpectrum(4, 2, 16, 16, 1.6),
                CreateRawSpectrum(4, 2, 17, 17, 1.4),
                CreateRawSpectrum(4, 2, 18, 18, 0.8),
                CreateRawSpectrum(4, 2, 19, 19, 0.3),
                CreateRawSpectrum(5, 2, 20, 20, 1.5),
                CreateRawSpectrum(5, 2, 21, 21, 1.3),
                CreateRawSpectrum(5, 2, 22, 22, 0.8),
                CreateRawSpectrum(5, 2, 23, 23, 0.8),
                CreateRawSpectrum(5, 2, 24, 24, 0.7),
                CreateRawSpectrum(5, 2, 25, 25, 0.3),

                CreateRawSpectrum(6, 1, 26, 26, 2.0, 200),
                CreateRawSpectrum(6, 1, 27, 27, 1.5, 400),
                CreateRawSpectrum(6, 1, 28, 28, 1.0, 850),
                CreateRawSpectrum(6, 1, 29, 29, 1.2, 700),
                CreateRawSpectrum(6, 1, 30, 30, 0.5, 300),
                CreateRawSpectrum(7, 2, 31, 31, 1.6),
                CreateRawSpectrum(7, 2, 32, 32, 1.4),
                CreateRawSpectrum(7, 2, 33, 33, 0.8),
                CreateRawSpectrum(7, 2, 34, 34, 0.7),
                CreateRawSpectrum(7, 2, 35, 35, 0.6),
                CreateRawSpectrum(7, 2, 36, 36, 0.4),
                CreateRawSpectrum(8, 2, 37, 37, 1.8),
                CreateRawSpectrum(8, 2, 38, 38, 1.8),
                CreateRawSpectrum(8, 2, 39, 39, 1.7),
                CreateRawSpectrum(8, 2, 40, 40, 1.3),
                CreateRawSpectrum(8, 2, 41, 41, 0.8),
                CreateRawSpectrum(8, 2, 42, 42, 0.7),
                CreateRawSpectrum(8, 2, 43, 43, 0.4),
                CreateRawSpectrum(8, 2, 44, 44, 0.3),

                CreateRawSpectrum(9, 1, 45, 45, 2.0, 100),
                CreateRawSpectrum(9, 1, 46, 46, 1.5, 150),
                CreateRawSpectrum(9, 1, 47, 47, 1.0, 150),
                CreateRawSpectrum(9, 1, 48, 48, 1.2, 100),
                CreateRawSpectrum(10, 2, 49, 49, 1.7),
                CreateRawSpectrum(10, 2, 50, 50, 1.3),
                CreateRawSpectrum(10, 2, 51, 51, 0.8),
                CreateRawSpectrum(10, 2, 52, 52, 0.7),
            };

            var provider = new ImmsRepresentativeDataProvider(spectrums);

            var expected = new[] { spectrums[26], spectrums[27], spectrums[28], spectrums[29], spectrums[30] };
            CollectionAssert.AreEquivalent(
                expected.Select(spec => (spec.DriftTime, spec.TotalIonCurrent)).ToArray(),
                provider.LoadMs1Spectrums().Select(spec => (spec.DriftTime, spec.TotalIonCurrent)).ToArray());

            CollectionAssert.AreEquivalent(
                spectrums.Where(spec => spec.MsLevel == 2).Select(spec => (spec.DriftTime, spec.OriginalIndex, spec.MsLevel)).ToArray(),
                provider.LoadMsNSpectrums(2).Select(spec => (spec.DriftTime, spec.OriginalIndex, spec.MsLevel)).ToArray());

            CollectionAssert.AreEquivalent(
                expected.Concat(spectrums.Where(spec => spec.MsLevel == 2)).Select(spec => (spec.DriftTime, spec.OriginalIndex, spec.MsLevel)).ToArray(),
                provider.LoadMsSpectrums().Select(spec => (spec.DriftTime, spec.OriginalIndex, spec.MsLevel)).ToArray());
        }

        private RawSpectrum CreateRawSpectrum(int scanNumber, int msLevel, int index, int originalIndex, double driftTime, int totalIonCurrent = 100) {
            return new RawSpectrum
            {
                ScanNumber = scanNumber,
                DriftTime = driftTime,
                MsLevel = msLevel,
                Index = index,
                OriginalIndex = originalIndex,
                TotalIonCurrent = totalIonCurrent
            };
        }
    }
}