using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.RawDataHandler.Core;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.Utility {
    public sealed class DataAccess {
        private DataAccess() { }

        // raw data access
        public static List<RawSpectrum> GetAllSpectra(string filepath) {
            List<RawSpectrum> rawSpectra = null;
            using (var rawDataAccess = new RawDataAccess(filepath, 0, false)) {
                var measurment = GetRawDataMeasurement(rawDataAccess);
                rawSpectra = measurment.SpectrumList;
            }
            return rawSpectra;
        }

        public static RawMeasurement GetRawDataMeasurement(RawDataAccess rawDataAccess) {
            var mes = rawDataAccess.GetMeasurement();
            return mes;
        }

        public static List<RawSpectrum> GetAllSpectra(RawDataAccess rawDataAccess) {
            var mes = rawDataAccess.GetMeasurement();
            if (mes == null) return null;
            return mes.SpectrumList;
        }

        // smoother
        public static List<ChromatogramPeak> GetSmoothedPeaklist(List<ChromatogramPeak> peaklist, SmoothingMethod smoothingMethod, int smoothingLevel) {
            var smoothedPeaklist = new List<ChromatogramPeak>();

            switch (smoothingMethod) {
                case SmoothingMethod.SimpleMovingAverage:
                    smoothedPeaklist = Smoothing.SimpleMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LinearWeightedMovingAverage:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.SavitzkyGolayFilter:
                    smoothedPeaklist = Smoothing.SavitxkyGolayFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.BinomialFilter:
                    smoothedPeaklist = Smoothing.BinomialFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LowessFilter:
                    smoothedPeaklist = Smoothing.LowessFilter(peaklist, smoothingLevel);
                    break;
                case SmoothingMethod.LoessFilter:
                    smoothedPeaklist = Smoothing.LoessFilter(peaklist, smoothingLevel);
                    break;
                default:
                    smoothedPeaklist = Smoothing.LinearWeightedMovingAverage(peaklist, smoothingLevel);
                    break;
            }

            return smoothedPeaklist;
        }

        // converter
        public static ChromatogramPeakFeature GetChromatogramPeakFeature(PeakDetectionResult result, ChromXType type, ChromXUnit unit) {
            if (result == null) return null;

            var peakFeature = new ChromatogramPeakFeature() {

                PeakID = result.PeakID,
               
                ChromScanIdLeft = result.ScanNumAtLeftPeakEdge,
                ChromScanIdTop = result.ScanNumAtPeakTop,
                ChromScanIdRight = result.ScanNumAtRightPeakEdge,

                ChromXsLeft = ChromMsConverter.GetChromXsObj(result.ChromXAxisAtLeftPeakEdge, type, unit),
                ChromXsTop = ChromMsConverter.GetChromXsObj(result.ChromXAxisAtPeakTop, type, unit),
                ChromXsRight = ChromMsConverter.GetChromXsObj(result.ChromXAxisAtRightPeakEdge, type, unit),

                ChromXs = ChromMsConverter.GetChromXsObj(result.ChromXAxisAtPeakTop, type, unit),

                PeakHeightLeft = result.IntensityAtLeftPeakEdge,
                PeakHeightTop = result.IntensityAtPeakTop,
                PeakHeightRight = result.IntensityAtRightPeakEdge,

                PeakAreaAboveZero = result.AreaAboveZero,
                PeakAreaAboveBaseline = result.AreaAboveBaseline,

                PeakShape = new ChromatogramPeakShape() {
                    SignalToNoise = result.SignalToNoise,
                    EstimatedNoise = result.EstimatedNoise,
                    BasePeakValue = result.BasePeakValue,
                    GaussianSimilarityValue = result.GaussianSimilarityValue,
                    IdealSlopeValue = result.IdealSlopeValue,
                    PeakPureValue = result.PeakPureValue,
                    ShapenessValue = result.ShapnessValue,
                    SymmetryValue = result.SymmetryValue,
                    AmplitudeOrderValue = result.AmplitudeOrderValue,
                    AmplitudeScoreValue = result.AmplitudeScoreValue
                }
            };

            return peakFeature;
        }

        // data access
        public static int GetScanStartIndexByMz(float targetMass, List<ChromatogramPeakFeature> features) {
            int startIndex = 0, endIndex = features.Count - 1;

            int counter = 0;
            while (counter < 5) {
                if (features[startIndex].PrecursorMz <= targetMass &&
                    targetMass < features[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (features[(startIndex + endIndex) / 2].PrecursorMz <= targetMass &&
                    targetMass < features[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        public static int GetDatabaseStartIndex(double precursorMz, double ms1Tolerance, List<MoleculeMsReference> mspDB) {
            double targetMass = precursorMz - ms1Tolerance;
            int startIndex = 0, endIndex = mspDB.Count - 1;
            if (targetMass > mspDB[endIndex].PrecursorMz) return endIndex;

            int counter = 0;
            while (counter < 10) {
                if (mspDB[startIndex].PrecursorMz <= targetMass && targetMass < mspDB[(startIndex + endIndex) / 2].PrecursorMz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (mspDB[(startIndex + endIndex) / 2].PrecursorMz <= targetMass && targetMass < mspDB[endIndex].PrecursorMz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }
    }
}
