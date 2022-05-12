using System;
using System.Collections.ObjectModel;

namespace CompMs.Common.Algorithm.PeakPick
{
    internal sealed class DataPointCollection : Collection<DataPoint>
    {
        private readonly Chromatogram _chromatogram;
        private readonly ChromatogramBaseline _baseline;

        public DataPointCollection(Chromatogram chromatogram, ChromatogramBaseline baseline) {
            _chromatogram = chromatogram;
            _baseline = baseline;
        }

        public void AddPoint(int i) {
            Add(_chromatogram.CreateDataPoint(i));
        }

        public void AddPoints(Peak peak) {
            for (int i = peak.LeftIndex; i <= peak.RightIndex; i++) {
                AddPoint(i);
            }
        }

        public PeakDetectionResult GetPeakDetectionResult(int peakTopId, int currentPeakId, double maxPeakHeight) {

            //1. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
            #region
            if (Count <= 3) return null;
            if (Items[peakTopId].IntensityDifference(Items[0]) < 0 && Items[peakTopId].IntensityDifference(Items[Count - 1]) < 0) return null;

            var idealSlopeValue = 0d;
            var nonIdealSlopeValue = 0d;

            var leftPeakHalfDiff = double.MaxValue;
            var leftPeakFivePercentDiff = double.MaxValue;
            var leftShapenessValue = double.MinValue;
            int leftPeakFivePercentId = -1, leftPeakHalfId = -1;
            for (int j = peakTopId; j >= 0; j--) {
                if (leftPeakHalfDiff > Math.Abs((Items[peakTopId].IntensityDifference(Items[0]) / 2) - Items[j].IntensityDifference(Items[0]))) {
                    leftPeakHalfDiff = Math.Abs((Items[peakTopId].IntensityDifference(Items[0]) / 2) - Items[j].IntensityDifference(Items[0]));
                    leftPeakHalfId = j;
                }

                if (leftPeakFivePercentDiff > Math.Abs((Items[peakTopId].IntensityDifference(Items[0]) / 5) - Items[j].IntensityDifference(Items[0]))) {
                    leftPeakFivePercentDiff = Math.Abs((Items[peakTopId].IntensityDifference(Items[0]) / 5) - Items[j].IntensityDifference(Items[0]));
                    leftPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;
                leftShapenessValue = Math.Max(leftShapenessValue, CalculateShapeness(peakTopId, j));

                var intensityDiff = Items[j + 1].IntensityDifference(Items[j]);
                if (intensityDiff >= 0)
                    idealSlopeValue += Math.Abs(intensityDiff);
                else
                    nonIdealSlopeValue += Math.Abs(intensityDiff);
            }

            var rightPeakHalfDiff = double.MaxValue;
            var rightPeakFivePercentDiff = double.MaxValue;
            var rightShapenessValue = double.MinValue;
            int rightPeakFivePercentId = -1, rightPeakHalfId = -1;
            for (int j = peakTopId; j <= Count - 1; j++) {
                if (rightPeakHalfDiff > Math.Abs((Items[peakTopId].IntensityDifference(Items[Count - 1]) / 2) - Items[j].IntensityDifference(Items[Count - 1]))) {
                    rightPeakHalfDiff = Math.Abs((Items[peakTopId].IntensityDifference(Items[Count - 1]) / 2) - Items[j].IntensityDifference(Items[Count - 1]));
                    rightPeakHalfId = j;
                }

                if (rightPeakFivePercentDiff > Math.Abs((Items[peakTopId].IntensityDifference(Items[Items.Count - 1]) / 5) - Items[j].IntensityDifference(Items[Items.Count - 1]))) {
                    rightPeakFivePercentDiff = Math.Abs((Items[peakTopId].IntensityDifference(Items[Items.Count - 1]) / 5) - Items[j].IntensityDifference(Items[Items.Count - 1]));
                    rightPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                rightShapenessValue = Math.Max(rightShapenessValue, CalculateShapeness(peakTopId, j));

                if (Items[j - 1].IntensityDifference(Items[j]) >= 0)
                    idealSlopeValue += Math.Abs(Items[j - 1].IntensityDifference(Items[j]));
                else
                    nonIdealSlopeValue += Math.Abs(Items[j - 1].IntensityDifference(Items[j]));
            }

            int peakHalfId;
            double gaussianNormalize, basePeakValue;
            if (Items[0].IntensityLessThanOrEqual(Items[Items.Count - 1])) {
                gaussianNormalize = Items[peakTopId].IntensityDifference(Items[0]);
                peakHalfId = leftPeakHalfId;
                basePeakValue = Math.Abs(Items[peakTopId].IntensityDifference(Items[Items.Count - 1]) / Items[peakTopId].IntensityDifference(Items[0]));
            }
            else {
                gaussianNormalize = Items[peakTopId].IntensityDifference(Items[Items.Count - 1]);
                peakHalfId = rightPeakHalfId;
                basePeakValue = Math.Abs(Items[peakTopId].IntensityDifference(Items[0]) / Items[peakTopId].IntensityDifference(Items[Items.Count - 1]));
            }

            double symmetryValue;
            if (Math.Abs(Items[peakTopId].ChromDifference(Items[leftPeakFivePercentId])) <= Math.Abs(Items[peakTopId].ChromDifference(Items[rightPeakFivePercentId])))
                symmetryValue = Math.Abs(Items[peakTopId].ChromDifference(Items[leftPeakFivePercentId])) / Math.Abs(Items[peakTopId].ChromDifference(Items[rightPeakFivePercentId]));
            else
                symmetryValue = Math.Abs(Items[peakTopId].ChromDifference(Items[rightPeakFivePercentId])) / Math.Abs(Items[peakTopId].ChromDifference(Items[leftPeakFivePercentId]));

            var peakHwhm = Math.Abs(Items[peakHalfId].ChromDifference(Items[peakTopId]));
            #endregion

            //2. Calculate peak pure value (from gaussian area and real area)
            #region
            var gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
            var gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

            var realAreaAboveZero = 0d;
            var leftPeakArea = 0d;
            var rightPeakArea = 0d;
            for (int j = 0; j < Items.Count - 1; j++) {
                realAreaAboveZero += Items[j + 1].CalculateChromatogramArea(Items[j]);
                if (j == peakTopId - 1)
                    leftPeakArea = realAreaAboveZero;
                else if (j == Items.Count - 2)
                    rightPeakArea = realAreaAboveZero - leftPeakArea;
            }

            var realAreaAboveBaseline = realAreaAboveZero - Items[Items.Count - 1].CalculateChromatogramArea(Items[0]);

            if (Items[0].IntensityLessThanOrEqual(Items[Items.Count - 1])) {
                leftPeakArea -= Items[0].Intensity * Items[peakTopId].ChromDifference(Items[0]);
                rightPeakArea -= Items[0].Intensity * Items[Items.Count - 1].ChromDifference(Items[peakTopId]);
            }
            else {
                leftPeakArea -= Items[Items.Count - 1].Intensity * Items[peakTopId].ChromDifference(Items[0]);
                rightPeakArea -= Items[Items.Count - 1].Intensity * Items[Items.Count - 1].ChromDifference(Items[peakTopId]);
            }

            double gaussianSimilarityLeftValue;
            if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
            else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

            double gaussianSimilarityRightValue;
            if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
            else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

            var gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;

            var idealSlopeRate = 1d - (nonIdealSlopeValue / idealSlopeValue);
            if (idealSlopeRate < 0) idealSlopeRate = 0;

            var peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue + idealSlopeRate) / 4;
            if (peakPureValue > 1) peakPureValue = 1;
            if (peakPureValue < 0) peakPureValue = 0;
            #endregion

            //3. Set area information
            #region
            var estimatedNoise = _baseline.EstimateNoise;
            var detectedPeakInformation = new PeakDetectionResult() {
                PeakID = currentPeakId,
                AmplitudeOrderValue = -1,
                AmplitudeScoreValue = -1,
                AreaAboveBaseline = (float)(realAreaAboveBaseline * 60),
                AreaAboveZero = (float)(realAreaAboveZero * 60),
                BasePeakValue = (float)basePeakValue,
                GaussianSimilarityValue = (float)gaussinaSimilarityValue,
                IdealSlopeValue = (float)idealSlopeRate,
                IntensityAtLeftPeakEdge = (float)Items[0].Intensity,
                IntensityAtPeakTop = (float)Items[peakTopId].Intensity,
                IntensityAtRightPeakEdge = (float)Items[Items.Count - 1].Intensity,
                PeakPureValue = (float)peakPureValue,
                ChromXAxisAtLeftPeakEdge = (float)Items[0].ChromValue,
                ChromXAxisAtPeakTop = (float)Items[peakTopId].ChromValue,
                ChromXAxisAtRightPeakEdge = (float)Items[Items.Count - 1].ChromValue,
                ScanNumAtLeftPeakEdge = (int)Items[0].Id,
                ScanNumAtPeakTop = (int)Items[peakTopId].Id,
                ScanNumAtRightPeakEdge = (int)Items[Items.Count - 1].Id,
                ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
                SymmetryValue = (float)symmetryValue,
                EstimatedNoise = estimatedNoise,
                SignalToNoise = (float)(maxPeakHeight / estimatedNoise),
            };
            if (detectedPeakInformation.EstimatedNoise < 1.0)
                detectedPeakInformation.EstimatedNoise = 1.0F;
            #endregion
            return detectedPeakInformation;
        }

        public double CalculateShapeness(int peakTopId, int anotherId) {
            return Items[peakTopId].IntensityDifference(Items[anotherId]) / Math.Abs(peakTopId - anotherId) / Math.Sqrt(Items[peakTopId].Intensity);
        }
    }
}
