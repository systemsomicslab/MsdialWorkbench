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
    /// The GC-MS MS1Dec model-peak purity is the deconvoluted spectrum's summed intensity over the positive ion current
    /// (the sum of the positive raw intensities) of the raw spectrum at the model peak top (RawSpectrumID), clamped to [0, 1].
    /// ScanID is only the result counter and must not select the spectrum, and the file's stored TotalIonCurrent is not
    /// the denominator: files with background-subtracted, signed intensities store a TIC that can be near zero or negative.
    /// </summary>
    [TestClass]
    public class MSDecHandlerGcmsPurityTests
    {
        private const int ScanCount = 400;
        private const double ScanIntervalMin = 0.005;
        private const double Sigma = 5d;
        private const double BackgroundMz = 40d;
        private const double BackgroundIntensity = 2000d;

        // Two well separated components, each with three fragment ions (m/z, relative abundance).
        private static readonly (int TopScan, double Height, (double Mz, double Ratio)[] Ions)[] Components = new[] {
            (120, 100000d, new[] { (50d, 1d), (73d, 0.6), (91d, 0.3) }),
            (260, 80000d, new[] { (55d, 1d), (77d, 0.5), (105d, 0.25) }),
        };

        // The same ions at a fifth of the height: weak enough that the negative noise below outweighs them in the signed TIC.
        private static readonly (int TopScan, double Height, (double Mz, double Ratio)[] Ions)[] WeakComponents = new[] {
            (120, 20000d, Components[0].Ions),
            (260, 16000d, Components[1].Ions),
        };

        // Background-subtracted noise stored as negative values in every scan, one value per nominal bin (as in the
        // Kovats demo ABF files): m/z 110-199, -300 to -500 each, about -36,000 per scan in total.
        private static IEnumerable<RawPeakElement> NegativeNoise(int scan) {
            for (int mz = 110; mz < 200; mz++) {
                yield return new RawPeakElement { Mz = mz, Intensity = -300d - 100d * ((scan + mz) % 3) };
            }
        }

        private static List<RawSpectrum> BuildSpectra() {
            return BuildSpectra(Components, _ => Enumerable.Empty<RawPeakElement>());
        }

        private static List<RawSpectrum> BuildSpectra(
            (int TopScan, double Height, (double Mz, double Ratio)[] Ions)[] components,
            Func<int, IEnumerable<RawPeakElement>> noise) {
            var spectra = new List<RawSpectrum>(ScanCount);
            for (int scan = 0; scan < ScanCount; scan++) {
                var peaks = new List<RawPeakElement> { new RawPeakElement { Mz = BackgroundMz, Intensity = BackgroundIntensity } };
                foreach (var (topScan, height, ions) in components) {
                    var shape = Math.Exp(-0.5 * Math.Pow((scan - topScan) / Sigma, 2));
                    foreach (var (mz, ratio) in ions) {
                        peaks.Add(new RawPeakElement { Mz = mz, Intensity = height * ratio * shape });
                    }
                }
                peaks.AddRange(noise(scan));
                var array = peaks.OrderBy(p => p.Mz).ToArray();
                spectra.Add(new RawSpectrum {
                    Index = scan,
                    ScanNumber = scan,
                    MsLevel = 1,
                    ScanPolarity = ScanPolarity.Positive,
                    ScanStartTime = scan * ScanIntervalMin,
                    ScanStartTimeUnit = Units.Minute,
                    Spectrum = array,
                    DefaultArrayLength = array.Length,
                    LowestObservedMz = array.First().Mz,
                    HighestObservedMz = array.Last().Mz,
                    BasePeakMz = array.OrderByDescending(p => p.Intensity).First().Mz,
                    BasePeakIntensity = array.Max(p => p.Intensity),
                    // Like the ABF reader, the stored TIC is the signed sum of the stored values.
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

        private static List<MSDecResult> Deconvolute(List<RawSpectrum> spectra, ParameterBase parameter) {
            var provider = new StandardDataProvider(spectra);
            var file = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA };
            var range = new ChromatogramRange(0d, ScanCount * ScanIntervalMin, ChromXType.RT, ChromXUnit.Min);
            var features = new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, provider, 1, CancellationToken.None, null, range);
            Assert.IsTrue(features.Count > 0, "the synthetic data must give peak features");
            return MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));
        }

        private static double PositiveIonCurrent(RawSpectrum spectrum) {
            return spectrum.Spectrum.Where(p => p.Intensity > 0d).Sum(p => p.Intensity);
        }

        [TestMethod]
        public void ModelPeakPurityUsesTheTicOfThePeakTopSpectrum() {
            var spectra = BuildSpectra();
            var results = Deconvolute(spectra, BuildParameter());

            Assert.AreEqual(Components.Length, results.Count, "one deconvolution result per component");
            for (int i = 0; i < results.Count; i++) {
                var result = results[i];
                Assert.AreEqual(i, result.ScanID, "ScanID is the result counter");
                Assert.AreEqual(Components[i].TopScan, result.RawSpectrumID, 1, "RawSpectrumID is the raw scan at the model peak top");

                // Without negative values the positive ion current is the stored TIC.
                var peakTopTic = spectra[result.RawSpectrumID].TotalIonCurrent;
                Assert.AreEqual(peakTopTic, PositiveIonCurrent(spectra[result.RawSpectrumID]));
                var eic = result.Spectrum.Sum(p => p.Intensity);
                Assert.AreEqual((float)(eic / peakTopTic), result.ModelPeakPurity, "purity = deconvoluted intensity / TIC at the peak top");
                Assert.IsTrue(result.ModelPeakPurity > 0f && result.ModelPeakPurity <= 1f, $"purity {result.ModelPeakPurity} is a fraction of the peak-top TIC");

                // The counter-indexed scan is baseline only; its TIC would give a purity far above 1.
                var counterTic = spectra[result.ScanID].TotalIonCurrent;
                Assert.AreNotEqual(peakTopTic, counterTic);
                Assert.IsTrue(eic / counterTic > 1d, "the counter-indexed scan would give a purity above 1");
            }
        }

        [TestMethod]
        public void ModelPeakPurityIgnoresNegativeStoredIntensities() {
            var spectra = BuildSpectra(WeakComponents, NegativeNoise);
            var results = Deconvolute(spectra, BuildParameter());

            Assert.AreEqual(WeakComponents.Length, results.Count, "one deconvolution result per component");
            var storedTicPurities = new List<double>();
            for (int i = 0; i < results.Count; i++) {
                var result = results[i];
                Assert.AreEqual(WeakComponents[i].TopScan, result.RawSpectrumID, 1, "RawSpectrumID is the raw scan at the model peak top");
                var peakTop = spectra[result.RawSpectrumID];
                var eic = result.Spectrum.Sum(p => p.Intensity);
                Assert.IsTrue(eic > 0d, "the deconvoluted spectrum carries signal");

                var positive = PositiveIonCurrent(peakTop);
                Assert.AreEqual((float)(eic / positive), result.ModelPeakPurity, "purity = deconvoluted intensity / positive ion current at the peak top");
                Assert.IsTrue(result.ModelPeakPurity > 0f && result.ModelPeakPurity <= 1f, $"purity {result.ModelPeakPurity} is a fraction in (0, 1]");
                storedTicPurities.Add(eic / peakTop.TotalIonCurrent);
            }

            // The signed stored TIC would have given a purity far above 1 for the first component and below 0 for the second.
            Assert.IsTrue(storedTicPurities[0] > 1d, $"stored-TIC purity {storedTicPurities[0]} of the first component");
            Assert.IsTrue(storedTicPurities[1] < 0d, $"stored-TIC purity {storedTicPurities[1]} of the second component");
        }

        private static RawSpectrum SpectrumOf(params double[] intensities) {
            var peaks = intensities.Select((intensity, i) => new RawPeakElement { Mz = 50d + i, Intensity = intensity }).ToArray();
            // The stored TIC is the signed sum; the purity must not depend on it.
            return new RawSpectrum { Spectrum = peaks, TotalIonCurrent = intensities.Sum() };
        }

        [TestMethod]
        public void PurityDenominatorIsThePositiveIonCurrentNotTheStoredTic() {
            var spectrum = SpectrumOf(100d, -50d, 300d, -400d); // stored TIC -50, positive ion current 400
            Assert.AreEqual(0.5f, MSDecHandler.CalculateModelPeakPurity(200d, spectrum));

            spectrum.TotalIonCurrent = 1e-3;
            Assert.AreEqual(0.5f, MSDecHandler.CalculateModelPeakPurity(200d, spectrum), "the stored TIC is not read");
        }

        [TestMethod]
        public void PurityWithoutNegativeValuesIsDeconvolutedOverSummedIntensities() {
            Assert.AreEqual(0.25f, MSDecHandler.CalculateModelPeakPurity(250d, SpectrumOf(100d, 300d, 600d)));
        }

        [TestMethod]
        public void PurityIsZeroWithoutPositiveIonCurrent() {
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(100d, SpectrumOf(-10d, -20d)));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(100d, SpectrumOf(0d, 0d)));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(100d, SpectrumOf()));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(100d, new RawSpectrum { Spectrum = null, TotalIonCurrent = 50d }));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(100d, null), "a missing peak-top spectrum");
        }

        [TestMethod]
        public void PurityIsClampedToTheUnitInterval() {
            Assert.AreEqual(1f, MSDecHandler.CalculateModelPeakPurity(500d, SpectrumOf(100d, 300d)));
            Assert.AreEqual(1f, MSDecHandler.CalculateModelPeakPurity(400d, SpectrumOf(100d, 300d)));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(-5d, SpectrumOf(100d, 300d)));
            Assert.AreEqual(0f, MSDecHandler.CalculateModelPeakPurity(double.NaN, SpectrumOf(100d, 300d)));
        }
    }
}
