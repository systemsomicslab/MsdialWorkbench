using CompMs.Common.DataObj;
using CompMs.MsdialCore.Algorithm;
using CompMs.RawDataHandler.Core;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.App.RawDataViewer.Model.Tests
{
    [TestClass]
    public class MsSpectrumIntensitySummarizerTests
    {
        [TestMethod]
        public void IntensitiesBelowOneAreCountedInNegativeBins() {
            // 0.0481 is the smallest MS1 intensity in the reporter's QC_02.mzML, written by Agilent's own centroiding.
            var spectra = new[]
            {
                Spectrum(1, 0.0481, 0.5, 0.75, 1d, 3d),
            };

            var (ms1, ms2) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);

            CollectionAssert.AreEqual(new[] { -5, -1, 0, 1, }, ms1.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, 1, 1, }, ms1.Values.ToArray());
            Assert.AreEqual(0, ms2.Count);
        }

        [TestMethod]
        public void BinsFromOneUpEqualRawDataHandlers() {
            var random = new Random(20261022);
            var spectra = new List<RawSpectrum>();
            foreach (var level in new[] { 0, 1, 2, 3, }) {
                var intensities = new List<double>();
                for (int i = 0; i < 2000; i++) {
                    // Log-uniform over [1, 2^41), so that every bin RawDataHandler keeps is visited.
                    intensities.Add(Math.Pow(2, random.NextDouble() * 41));
                }
                for (int k = 0; k <= 40; k++) {
                    // Each power of two, and the values on either side of it, where flooring a logarithm is most fragile.
                    var p = Math.Pow(2, k);
                    intensities.Add(p);
                    intensities.Add(BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(p) + 1));
                    if (k > 0) {
                        intensities.Add(BitConverter.Int64BitsToDouble(BitConverter.DoubleToInt64Bits(p) - 1));
                    }
                    // Agilent and others store intensities as 32-bit floats.
                    intensities.Add((float)(p * 1.37));
                }
                intensities.Add(Math.Pow(2, 41) - 1);
                spectra.Add(Spectrum(level, intensities.ToArray()));
            }

            var (ms1, ms2) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);
            var (expectedMs1, expectedMs2) = new RawDataAccess().ParseRawdataForStatistics(spectra);

            AssertSameNonEmptyBins(expectedMs1, ms1);
            AssertSameNonEmptyBins(expectedMs2, ms2);
            // Per level: the random values, three values for each of 2^0 to 2^40, the value below each of 2^1 to 2^40, and 2^41 - 1.
            Assert.AreEqual(2 * (2000 + 41 * 3 + 40 + 1), ms1.Values.Sum(), "every intensity of levels 0 and 1 is counted once");
        }

        [TestMethod]
        public void IntensitiesAtOrAbove2To41KeepTheirOwnBins() {
            // RawDataHandler folds these into the 2^40 bin, which the chart labels 2^40 <= I < 2^41.
            var spectra = new[] { Spectrum(1, Math.Pow(2, 41), Math.Pow(2, 45) * 1.5, double.MaxValue), };

            var (ms1, _) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);
            var (rdhMs1, _) = new RawDataAccess().ParseRawdataForStatistics(spectra);

            // Math.Log(double.MaxValue, 2) rounds to 1024, but double.MaxValue is below 2^1024, which is not a finite double.
            CollectionAssert.AreEqual(new[] { 41, 45, 1023, }, ms1.Keys.ToArray());
            Assert.AreEqual(3, rdhMs1[40]);
        }

        [TestMethod]
        public void ZeroNegativeNaNAndInfiniteIntensitiesAreSkipped() {
            var spectra = new[]
            {
                Spectrum(1, 0d, -0d, -1d, double.NaN, double.PositiveInfinity, double.NegativeInfinity, double.Epsilon, 5d),
                Spectrum(2, 0d, -3d, double.NaN, double.PositiveInfinity, double.NegativeInfinity),
            };

            var (ms1, ms2) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);

            // double.Epsilon, the smallest positive double, is a finite intensity above 0 like any other.
            CollectionAssert.AreEqual(new[] { -1074, 2, }, ms1.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 1, }, ms1.Values.ToArray());
            Assert.AreEqual(0, ms2.Count);
        }

        [TestMethod]
        public void LevelsBelowTwoAreMs1AndTheRestAreMs2() {
            var spectra = new[]
            {
                Spectrum(0, 2d),
                Spectrum(1, 4d, 4d),
                Spectrum(2, 8d),
                Spectrum(3, 16d, 16d, 16d),
            };

            var (ms1, ms2) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);

            CollectionAssert.AreEqual(new[] { 1, 2, }, ms1.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 2, }, ms1.Values.ToArray());
            CollectionAssert.AreEqual(new[] { 3, 4, }, ms2.Keys.ToArray());
            CollectionAssert.AreEqual(new[] { 1, 3, }, ms2.Values.ToArray());
        }

        [TestMethod]
        public void MissingSpectraAndPeakArraysAreSkipped() {
            var spectra = new[] { null, new RawSpectrum { MsLevel = 1, Spectrum = null, }, Spectrum(1, 2d), };

            var (ms1, ms2) = MsSpectrumIntensitySummarizer.CountIntensityBins(spectra);
            var (emptyMs1, emptyMs2) = MsSpectrumIntensitySummarizer.CountIntensityBins(null);

            Assert.AreEqual(1, ms1.Single().Value);
            Assert.AreEqual(0, ms2.Count + emptyMs1.Count + emptyMs2.Count);
        }

        [TestMethod]
        public async Task SummarizeAsyncBuildsBothHistogramsFromIntensitiesBelowOne() {
            var provider = new StubDataProvider(Spectrum(1, 0.0481, 100d), Spectrum(2, 0d, 50d));

            var summaries = await new MsSpectrumIntensitySummarizer().SummarizeAsync(Task.FromResult<IDataProvider>(provider));

            Assert.AreEqual(2, summaries.Length);
            CollectionAssert.AreEqual(new[] { Math.Pow(2, -5), Math.Pow(2, 6), }, summaries[0].IntensityHistogram.Select(p => p.X).ToArray());
            CollectionAssert.AreEqual(new[] { 1d, 1d, }, summaries[0].IntensityHistogram.Select(p => p.Y).ToArray());
            CollectionAssert.AreEqual(new[] { Math.Pow(2, 5), }, summaries[1].IntensityHistogram.Select(p => p.X).ToArray());
        }

        private static void AssertSameNonEmptyBins(IDictionary<int, int> expected, IDictionary<int, int> actual) {
            var expectedNonEmpty = expected.Where(kvp => kvp.Value > 0).OrderBy(kvp => kvp.Key).ToArray();
            var actualNonEmpty = actual.Where(kvp => kvp.Value > 0).OrderBy(kvp => kvp.Key).ToArray();
            CollectionAssert.AreEqual(expectedNonEmpty, actualNonEmpty);
        }

        internal static RawSpectrum Spectrum(int msLevel, params double[] intensities) {
            return new RawSpectrum
            {
                MsLevel = msLevel,
                Spectrum = intensities.Select((intensity, i) => new RawPeakElement { Mz = 100d + i, Intensity = intensity, }).ToArray(),
            };
        }

        internal sealed class StubDataProvider : IDataProvider
        {
            private readonly ReadOnlyCollection<RawSpectrum> _spectra;

            public StubDataProvider(params RawSpectrum[] spectra) {
                _spectra = spectra.ToList().AsReadOnly();
            }

            public ReadOnlyCollection<RawSpectrum> LoadMsSpectrums() => _spectra;
            public ReadOnlyCollection<RawSpectrum> LoadMs1Spectrums() => _spectra.Where(s => s.MsLevel == 1).ToList().AsReadOnly();
            public ReadOnlyCollection<RawSpectrum> LoadMsNSpectrums(int level) => _spectra.Where(s => s.MsLevel == level).ToList().AsReadOnly();
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsSpectrumsAsync(CancellationToken token) => Task.FromResult(LoadMsSpectrums());
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMs1SpectrumsAsync(CancellationToken token) => Task.FromResult(LoadMs1Spectrums());
            public Task<ReadOnlyCollection<RawSpectrum>> LoadMsNSpectrumsAsync(int level, CancellationToken token) => Task.FromResult(LoadMsNSpectrums(level));
        }
    }
}
