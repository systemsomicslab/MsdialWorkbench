using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialCore.MSDec.Tests
{
    /// <summary>
    /// GC-MS MS1Dec keys every scan by its position in the spectrum list, which is <see cref="RawSpectrum.Index"/> and the
    /// key the peak features carry in <see cref="ChromatogramPeakFeature.MS1RawSpectrumIdTop"/>.
    /// <see cref="RawSpectrum.ScanNumber"/> need not equal that position (scan numbers that start at 1, scan numbers
    /// with gaps, product-ion scans between the MS1 scans), and it must not change the deconvolution.
    /// </summary>
    [TestClass]
    public class MSDecHandlerGcmsScanIndexTests
    {
        private const int Ms1ScanCount = 400;
        private const double ScanIntervalMin = 0.005;
        private const double Sigma = 5d;
        private const double BackgroundMz = 40d;
        private const double BackgroundIntensity = 2000d;

        // A well separated component and a coeluting pair (14 scans apart), each with three fragment ions.
        private static readonly (int TopScan, double Height, (double Mz, double Ratio)[] Ions)[] Components = new[] {
            (120, 100000d, new[] { (50d, 1d), (73d, 0.6), (91d, 0.3) }),
            (260, 80000d, new[] { (55d, 1d), (77d, 0.5), (105d, 0.25) }),
            (274, 60000d, new[] { (57d, 1d), (85d, 0.7), (117d, 0.2) }),
        };

        /// <summary>
        /// The synthetic run: <see cref="Ms1ScanCount"/> positive MS1 scans, Index = position in the returned list.
        /// With <paramref name="otherScanEvery"/> &gt; 0 another scan (<paramref name="otherMsLevel"/>,
        /// <paramref name="otherPolarity"/>) precedes every that many MS1 scans. Its peaks are at m/z values the MS1 scans
        /// do not have. <paramref name="scanNumberOf"/> gives each spectrum's ScanNumber from its position.
        /// </summary>
        private static List<RawSpectrum> BuildSpectra(Func<int, int> scanNumberOf, int otherScanEvery = 0, int otherMsLevel = 2, ScanPolarity otherPolarity = ScanPolarity.Positive) {
            var spectra = new List<RawSpectrum>();
            for (int scan = 0; scan < Ms1ScanCount; scan++) {
                var rt = scan * ScanIntervalMin;
                if (otherScanEvery > 0 && scan % otherScanEvery == 0) {
                    var other = new[] { new RawPeakElement { Mz = 60d, Intensity = 50000d }, new RawPeakElement { Mz = 150d, Intensity = 70000d } };
                    spectra.Add(new RawSpectrum {
                        Index = spectra.Count,
                        ScanNumber = scanNumberOf(spectra.Count),
                        MsLevel = otherMsLevel,
                        ScanPolarity = otherPolarity,
                        ScanStartTime = rt,
                        ScanStartTimeUnit = Units.Minute,
                        Spectrum = other,
                        DefaultArrayLength = other.Length,
                        TotalIonCurrent = other.Sum(p => p.Intensity),
                    });
                }

                var peaks = new List<RawPeakElement> { new RawPeakElement { Mz = BackgroundMz, Intensity = BackgroundIntensity } };
                foreach (var (topScan, height, ions) in Components) {
                    var shape = Math.Exp(-0.5 * Math.Pow((scan - topScan) / Sigma, 2));
                    foreach (var (mz, ratio) in ions) {
                        peaks.Add(new RawPeakElement { Mz = mz, Intensity = height * ratio * shape });
                    }
                }
                var array = peaks.OrderBy(p => p.Mz).ToArray();
                spectra.Add(new RawSpectrum {
                    Index = spectra.Count,
                    ScanNumber = scanNumberOf(spectra.Count),
                    MsLevel = 1,
                    ScanPolarity = ScanPolarity.Positive,
                    ScanStartTime = rt,
                    ScanStartTimeUnit = Units.Minute,
                    Spectrum = array,
                    DefaultArrayLength = array.Length,
                    LowestObservedMz = array.First().Mz,
                    HighestObservedMz = array.Last().Mz,
                    BasePeakMz = array.OrderByDescending(p => p.Intensity).First().Mz,
                    BasePeakIntensity = array.Max(p => p.Intensity),
                    TotalIonCurrent = array.Sum(p => p.Intensity),
                });
            }
            return spectra;
        }

        private static ParameterBase BuildParameter() {
            return new ParameterBase {
                IonMode = IonMode.Positive,
                MSDataType = MSDataType.Centroid,
                AccuracyType = AccuracyType.IsNominal,
                MassSliceWidth = 0.5f,
                CentroidMs1Tolerance = 0.5f,
                MassRangeBegin = 30f,
                MassRangeEnd = 200f,
                MinimumAmplitude = 1000d,
                MinimumDatapoints = 5d,
                SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage,
                SmoothingLevel = 3,
                AveragePeakWidth = 10f,
                SigmaWindowValue = 0.5f,
                AmplitudeCutoff = 0f,
            };
        }

        private static List<MSDecResult> Deconvolute(List<RawSpectrum> spectra) {
            var parameter = BuildParameter();
            var provider = new StandardDataProvider(spectra);
            var file = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA };
            var range = new ChromatogramRange(0d, Ms1ScanCount * ScanIntervalMin, ChromXType.RT, ChromXUnit.Min);
            var features = new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, provider, 1, CancellationToken.None, null, range);
            Assert.IsTrue(features.Count > 0, "the synthetic data must give peak features");
            foreach (var feature in features) {
                Assert.IsTrue(IsPositiveMs1(spectra[feature.MS1RawSpectrumIdTop]), "a feature's MS1RawSpectrumIdTop is the list position of a positive MS1 scan");
            }
            return MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));
        }

        /// <summary>
        /// Compares two deconvolutions of the same signal. Spectrum positions (RawSpectrumID, model chromatogram peak IDs)
        /// may differ between the two lists when one has product-ion scans in between; they must name the same scans.
        /// </summary>
        private static void AssertSameDeconvolution(
            List<MSDecResult> expected, List<RawSpectrum> expectedSpectra,
            List<MSDecResult> actual, List<RawSpectrum> actualSpectra) {
            Assert.IsTrue(expected.Count >= Components.Length, $"the reference run gives a result per component ({expected.Count})");
            Assert.AreEqual(expected.Count, actual.Count, "number of deconvolution results");
            for (int i = 0; i < expected.Count; i++) {
                var e = expected[i];
                var a = actual[i];
                Assert.AreEqual(e.ScanID, a.ScanID, $"result {i}: ScanID");
                Assert.AreEqual(e.ChromXs.RT.Value, a.ChromXs.RT.Value, $"result {i}: retention time");

                // RawSpectrumID is a list position: it points at the same MS1 scan in both runs.
                var eTop = expectedSpectra[e.RawSpectrumID];
                var aTop = actualSpectra[a.RawSpectrumID];
                Assert.IsTrue(IsPositiveMs1(aTop), $"result {i}: RawSpectrumID points at a positive MS1 scan");
                Assert.AreEqual(a.RawSpectrumID, aTop.Index, $"result {i}: RawSpectrumID is the peak-top spectrum's Index");
                Assert.AreEqual(eTop.ScanStartTime, aTop.ScanStartTime, $"result {i}: peak-top scan");
                Assert.AreEqual(a.ChromXs.RT.Value, aTop.ScanStartTime, 1e-6, $"result {i}: RawSpectrumID is the scan at the result's retention time");

                Assert.AreEqual(e.ModelPeakMz, a.ModelPeakMz, $"result {i}: model peak m/z");
                Assert.AreEqual(e.ModelPeakHeight, a.ModelPeakHeight, $"result {i}: model peak height");
                Assert.AreEqual(e.ModelPeakArea, a.ModelPeakArea, $"result {i}: model peak area");
                Assert.AreEqual(e.ModelPeakPurity, a.ModelPeakPurity, $"result {i}: model peak purity");
                Assert.AreEqual(e.AmplitudeScore, a.AmplitudeScore, $"result {i}: amplitude score");
                Assert.AreEqual(e.IntegratedHeight, a.IntegratedHeight, $"result {i}: integrated height");
                Assert.AreEqual(e.IntegratedArea, a.IntegratedArea, $"result {i}: integrated area");
                Assert.AreEqual(e.SignalNoiseRatio, a.SignalNoiseRatio, $"result {i}: S/N");
                CollectionAssert.AreEqual(e.ModelMasses, a.ModelMasses, $"result {i}: model masses");
                Assert.AreEqual(e.Splash, a.Splash, $"result {i}: splash");

                Assert.AreEqual(e.Spectrum.Count, a.Spectrum.Count, $"result {i}: deconvoluted spectrum size");
                for (int j = 0; j < e.Spectrum.Count; j++) {
                    Assert.AreEqual(e.Spectrum[j].Mass, a.Spectrum[j].Mass, $"result {i}, peak {j}: m/z");
                    Assert.AreEqual(e.Spectrum[j].Intensity, a.Spectrum[j].Intensity, $"result {i}, peak {j}: intensity");
                }

                // The model chromatogram's peak IDs are spectrum Indexes, as in the EICs of RawSpectra that
                // GetChromatogramQuantInformation matches them against.
                Assert.AreEqual(e.ModelPeakChromatogram.Count, a.ModelPeakChromatogram.Count, $"result {i}: model chromatogram length");
                for (int j = 0; j < e.ModelPeakChromatogram.Count; j++) {
                    var ep = e.ModelPeakChromatogram[j];
                    var ap = a.ModelPeakChromatogram[j];
                    Assert.AreEqual(ep.Intensity, ap.Intensity, $"result {i}, model point {j}: intensity");
                    Assert.AreEqual(ep.ChromXs.RT.Value, ap.ChromXs.RT.Value, $"result {i}, model point {j}: time");
                    Assert.AreEqual(expectedSpectra[ep.ID].ScanStartTime, actualSpectra[ap.ID].ScanStartTime, $"result {i}, model point {j}: scan");
                    Assert.IsTrue(IsPositiveMs1(actualSpectra[ap.ID]), $"result {i}, model point {j}: a positive MS1 scan");
                }
            }
        }

        private static bool IsPositiveMs1(RawSpectrum spectrum) => spectrum.MsLevel == 1 && spectrum.ScanPolarity == ScanPolarity.Positive;

        private static List<RawSpectrum> Reference() => BuildSpectra(position => position);

        [TestMethod]
        public void ReferenceRunHasScanNumberEqualToIndex() {
            var spectra = Reference();
            var results = Deconvolute(spectra);
            Assert.IsTrue(results.Count >= Components.Length);
            foreach (var (topScan, _, _) in Components) {
                Assert.IsTrue(results.Any(r => Math.Abs(r.RawSpectrumID - topScan) <= 1), $"a result at scan {topScan}");
            }
        }

        [TestMethod]
        public void ScanNumbersStartingAtOneGiveTheSameDeconvolution() {
            var expectedSpectra = Reference();
            var expected = Deconvolute(expectedSpectra);

            var spectra = BuildSpectra(position => position + 1);
            var results = Deconvolute(spectra);

            AssertSameDeconvolution(expected, expectedSpectra, results, spectra);
            for (int i = 0; i < results.Count; i++) {
                Assert.AreEqual(expected[i].RawSpectrumID, results[i].RawSpectrumID, $"result {i}: RawSpectrumID is the position, not the ScanNumber");
            }
        }

        [TestMethod]
        public void ScanNumbersWithGapsGiveTheSameDeconvolution() {
            var expectedSpectra = Reference();
            var expected = Deconvolute(expectedSpectra);

            var spectra = BuildSpectra(position => 1000 + 3 * position);
            var results = Deconvolute(spectra);

            AssertSameDeconvolution(expected, expectedSpectra, results, spectra);
            for (int i = 0; i < results.Count; i++) {
                Assert.AreEqual(expected[i].RawSpectrumID, results[i].RawSpectrumID, $"result {i}: RawSpectrumID is the position, not the ScanNumber");
            }
        }

        [TestMethod]
        public void ProductIonScansBetweenTheMs1ScansGiveTheSameDeconvolution() {
            var expectedSpectra = Reference();
            var expected = Deconvolute(expectedSpectra);

            // An MS2 scan before every third MS1 scan, with ScanNumber == Index == position as the raw data readers give.
            // The peak features' edges are then positions in the full list, not MS1 chromatogram scans.
            var spectra = BuildSpectra(position => position, otherScanEvery: 3);
            Assert.AreEqual(Ms1ScanCount + (Ms1ScanCount + 2) / 3, spectra.Count);
            var results = Deconvolute(spectra);

            AssertSameDeconvolution(expected, expectedSpectra, results, spectra);
        }

        [TestMethod]
        public void ProductIonScansAndScanNumbersFromOneGiveTheSameDeconvolution() {
            var expectedSpectra = Reference();
            var expected = Deconvolute(expectedSpectra);

            // An MS2 scan before every third MS1 scan, and scan numbers counted from 1 as in mzML "scan=" ids.
            var spectra = BuildSpectra(position => position + 1, otherScanEvery: 3);
            var results = Deconvolute(spectra);

            AssertSameDeconvolution(expected, expectedSpectra, results, spectra);
        }

        [TestMethod]
        public void OtherPolarityScansBetweenTheMs1ScansGiveTheSameDeconvolution() {
            var expectedSpectra = Reference();
            var expected = Deconvolute(expectedSpectra);

            // A negative-mode MS1 scan before every second positive MS1 scan; the run is processed in positive mode.
            var spectra = BuildSpectra(position => position, otherScanEvery: 2, otherMsLevel: 1, otherPolarity: ScanPolarity.Negative);
            var results = Deconvolute(spectra);

            AssertSameDeconvolution(expected, expectedSpectra, results, spectra);
        }
    }
}
