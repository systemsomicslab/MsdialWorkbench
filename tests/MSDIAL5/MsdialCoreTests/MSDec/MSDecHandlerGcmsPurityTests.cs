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
    /// The GC-MS MS1Dec model-peak purity is the deconvoluted spectrum's summed intensity over the TIC of the raw spectrum
    /// at the model peak top (RawSpectrumID), as in MS-DIAL 4. ScanID is only the result counter and must not select the TIC.
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

        private static List<RawSpectrum> BuildSpectra() {
            var spectra = new List<RawSpectrum>(ScanCount);
            for (int scan = 0; scan < ScanCount; scan++) {
                var peaks = new List<RawPeakElement> { new RawPeakElement { Mz = BackgroundMz, Intensity = BackgroundIntensity } };
                foreach (var (topScan, height, ions) in Components) {
                    var shape = Math.Exp(-0.5 * Math.Pow((scan - topScan) / Sigma, 2));
                    foreach (var (mz, ratio) in ions) {
                        peaks.Add(new RawPeakElement { Mz = mz, Intensity = height * ratio * shape });
                    }
                }
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

        [TestMethod]
        public void ModelPeakPurityUsesTheTicOfThePeakTopSpectrum() {
            var spectra = BuildSpectra();
            var parameter = BuildParameter();
            var provider = new StandardDataProvider(spectra);
            var file = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA };
            var range = new ChromatogramRange(0d, ScanCount * ScanIntervalMin, ChromXType.RT, ChromXUnit.Min);
            var features = new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, provider, 1, CancellationToken.None, null, range);
            Assert.IsTrue(features.Count > 0, "the synthetic data must give peak features");

            var results = MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));

            Assert.AreEqual(Components.Length, results.Count, "one deconvolution result per component");
            for (int i = 0; i < results.Count; i++) {
                var result = results[i];
                Assert.AreEqual(i, result.ScanID, "ScanID is the result counter");
                Assert.AreEqual(Components[i].TopScan, result.RawSpectrumID, 1, "RawSpectrumID is the raw scan at the model peak top");

                var peakTopTic = spectra[result.RawSpectrumID].TotalIonCurrent;
                var eic = result.Spectrum.Sum(p => p.Intensity);
                Assert.AreEqual((float)(eic / peakTopTic), result.ModelPeakPurity, "purity = deconvoluted intensity / TIC at the peak top");
                Assert.IsTrue(result.ModelPeakPurity > 0f && result.ModelPeakPurity <= 1f, $"purity {result.ModelPeakPurity} is a fraction of the peak-top TIC");

                // The counter-indexed scan is baseline only; its TIC would give a purity far above 1.
                var counterTic = spectra[result.ScanID].TotalIonCurrent;
                Assert.AreNotEqual(peakTopTic, counterTic);
                Assert.IsTrue(eic / counterTic > 1d, "the counter-indexed scan would give a purity above 1");
            }
        }
    }
}
