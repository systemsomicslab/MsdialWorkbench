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
    /// A GC-MS file in which no peak passes the detection settings has nothing to deconvolute. MS1Dec must return no result
    /// for it instead of throwing, because one such file (a blank, or a minimum peak height above every signal) used to
    /// abort the whole GC-MS run.
    /// </summary>
    [TestClass]
    public class MSDecHandlerGcmsNoPeakTests
    {
        private const int ScanCount = 200;
        private const double ScanIntervalMin = 0.005;

        private static List<RawSpectrum> BuildSpectra(ScanPolarity polarity) {
            var spectra = new List<RawSpectrum>(ScanCount);
            for (int scan = 0; scan < ScanCount; scan++) {
                var shape = Math.Exp(-0.5 * Math.Pow((scan - 100) / 5d, 2));
                var array = new[] {
                    new RawPeakElement { Mz = 40d, Intensity = 2000d },
                    new RawPeakElement { Mz = 73d, Intensity = 100000d * shape },
                    new RawPeakElement { Mz = 91d, Intensity = 50000d * shape },
                };
                spectra.Add(new RawSpectrum {
                    Index = scan,
                    ScanNumber = scan,
                    MsLevel = 1,
                    ScanPolarity = polarity,
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

        private static ParameterBase BuildParameter(double minimumAmplitude) {
            return new ParameterBase {
                IonMode = IonMode.Positive,
                MSDataType = MSDataType.Centroid,
                AccuracyType = AccuracyType.IsNominal,
                MassSliceWidth = 0.5f,
                CentroidMs1Tolerance = 0.5f,
                MassRangeBegin = 30f,
                MassRangeEnd = 200f,
                MinimumAmplitude = minimumAmplitude,
                MinimumDatapoints = 5d,
                SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage,
                SmoothingLevel = 3,
                AveragePeakWidth = 10f,
                SigmaWindowValue = 0.5f,
                AmplitudeCutoff = 0f,
            };
        }

        private static List<ChromatogramPeakFeature> DetectPeaks(List<RawSpectrum> spectra, ParameterBase parameter) {
            var provider = new StandardDataProvider(spectra);
            var file = new AnalysisFileBean { AcquisitionType = AcquisitionType.DDA };
            var range = new ChromatogramRange(0d, ScanCount * ScanIntervalMin, ChromXType.RT, ChromXUnit.Min);
            return new PeakSpottingCore(parameter).Execute3DFeatureDetection(file, provider, 1, CancellationToken.None, null, range);
        }

        [TestMethod]
        public void NoResultWhenNoPeakPassesTheMinimumPeakHeight() {
            var spectra = BuildSpectra(ScanPolarity.Positive);
            var parameter = BuildParameter(minimumAmplitude: 1e9);
            var features = DetectPeaks(spectra, parameter);
            Assert.AreEqual(0, features.Count, "no peak reaches a minimum peak height of 1e9");

            var results = MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void NoResultWhenNoScanHasTheProjectPolarity() {
            // An ANDI-MS (.cdf) file without test_ionization_polarity is read with undefined polarity, so a positive-mode
            // project finds no scan and no peak in it.
            var spectra = BuildSpectra(ScanPolarity.Undefined);
            var parameter = BuildParameter(minimumAmplitude: 1000d);
            var features = DetectPeaks(spectra, parameter);

            var results = MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));

            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void TheSameDataWithAReachableHeightStillGivesItsComponent() {
            var spectra = BuildSpectra(ScanPolarity.Positive);
            var parameter = BuildParameter(minimumAmplitude: 1000d);
            var features = DetectPeaks(spectra, parameter);
            Assert.IsTrue(features.Count > 0, "the synthetic data must give peak features");

            var results = MSDecHandler.GetMSDecResults(spectra, features, parameter, new ReportProgress(null, 0d, 100d));

            Assert.AreEqual(1, results.Count, "one deconvolution result for the one component");
            Assert.AreEqual(100, results[0].RawSpectrumID, 1);
        }
    }
}
