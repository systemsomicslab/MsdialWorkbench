using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components
{
    public sealed class ChroChroChromatogram {
        public ChroChroChromatogram(Chromatogram_temp2 chromatogram, ChromatogramGlobalProperty_temp2 globalProperty, DifferencialCoefficients differencialCoefficients, ChromatogramNoises noises) {
            Chromatogram = chromatogram;
            GlobalProperty = globalProperty;
            DifferencialCoefficients = differencialCoefficients;
            Noises = noises;
        }

        public Chromatogram_temp2 Chromatogram { get; }
        public ChromatogramGlobalProperty_temp2 GlobalProperty { get; }
        public DifferencialCoefficients DifferencialCoefficients { get; }
        public ChromatogramNoises Noises { get; }

        public bool IsPeakStarted(int index, double slopeNoiseFoldCriteria) {
            return DifferencialCoefficients.FirstDiffPeaklist[index] > Noises.SlopeNoise * slopeNoiseFoldCriteria &&
                DifferencialCoefficients.FirstDiffPeaklist[index + 1] > Noises.SlopeNoise * slopeNoiseFoldCriteria;
        }

        internal int SearchRealLeftEdge2(int i) {
            var ssPeaklist = GlobalProperty.SmoothedChromatogram.Peaks;
            //search real left edge within 5 data points
            for (int j = 0; j <= 5; j++) {
                if (i - j - 1 < 0 || ssPeaklist[i - j].Intensity <= ssPeaklist[i - j - 1].Intensity) {
                    return i - j;
                }
            }
            return i - 6;
        }

        internal int SearchRightEdgeCandidate2(int i, double minimumDatapointCriteria, double slopeNoiseFoldCriteria) {
            var j = i;
            var peaktopCheck = false;
            var peaktopCheckPoint = j;
            while (true) {
                if (j + 2 == GlobalProperty.SmoothedChromatogram.Peaks.Count - 1) break;
                j++;

                // peak top check
                if (peaktopCheck == false &&
                    (DifferencialCoefficients.FirstDiffPeaklist[j - 1] > 0 && DifferencialCoefficients.FirstDiffPeaklist[j] < 0) || (DifferencialCoefficients.FirstDiffPeaklist[j - 1] > 0 && DifferencialCoefficients.FirstDiffPeaklist[j + 1] < 0) && DifferencialCoefficients.SecondDiffPeaklist[j] < -1 * (double)Noises.PeakTopNoise) {
                    peaktopCheck = true; peaktopCheckPoint = j;
                }

                if (peaktopCheck == false &&
                    GlobalProperty.SmoothedChromatogram.Peaks[j - 2].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity &&
                    GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                    GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity &&
                    GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j + 2].Intensity) {
                    peaktopCheck = true; peaktopCheckPoint = j;
                }

                // peak top check force
                if (peaktopCheck == false && minimumDatapointCriteria < 1.5 &&
                    (GlobalProperty.SmoothedChromatogram.Peaks[j - 2].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity &&
                     GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                     GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity) ||
                    (GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                     GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity &&
                     GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j + 2].Intensity)) {
                    peaktopCheck = true; peaktopCheckPoint = j;
                }


                var minimumPointFromTop = minimumDatapointCriteria <= 3 ? 1 : minimumDatapointCriteria * 0.5;
                if (peaktopCheck == true && peaktopCheckPoint + minimumPointFromTop <= j - 1) {
                    if (DifferencialCoefficients.FirstDiffPeaklist[j] > -1 * (double)Noises.SlopeNoise * slopeNoiseFoldCriteria) break;
                    if (Math.Abs(GlobalProperty.SmoothedChromatogram.Peaks[j - 2].Intensity - GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity) < (double)Noises.AmplitudeNoise &&
                          Math.Abs(GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity - GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity) < (double)Noises.AmplitudeNoise) break;

                    if (GlobalProperty.SmoothedChromatogram.Peaks[j - 2].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity &&
                        GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                        GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity &&
                        GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j + 2].Intensity) break;

                    // peak right check force
                    if (minimumDatapointCriteria < 1.5 &&
                        (GlobalProperty.SmoothedChromatogram.Peaks[j - 2].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity &&
                         GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                         GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity) ||
                        (GlobalProperty.SmoothedChromatogram.Peaks[j - 1].Intensity >= GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity &&
                         GlobalProperty.SmoothedChromatogram.Peaks[j].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity &&
                         GlobalProperty.SmoothedChromatogram.Peaks[j + 1].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[j + 2].Intensity)) {
                        peaktopCheck = true; peaktopCheckPoint = j;
                    }
                }
            }
            return j;
        }

        internal int SearchRealRightEdge2(int i, ref bool infinitLoopCheck, ref int infinitLoopID, out bool isBreak) {
            //Search real right edge within 5 data points
            var isTooLongRightEdge = false;
            var trackcounter = 0;
            isBreak = false;

            //case: wrong edge is in right of real edge
            for (int j = 0; j <= 5; j++) {
                if (i - j - 1 < 0 || GlobalProperty.SmoothedChromatogram.Peaks[i - j].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[i - j - 1].Intensity) {
                    break;
                }
                isTooLongRightEdge = true;
                trackcounter++;
            }
            if (isTooLongRightEdge) {
                var k = i - trackcounter;
                if (infinitLoopCheck && k == infinitLoopID && k > GlobalProperty.SmoothedChromatogram.Peaks.Count - 10) {
                    isBreak = true;
                }
                else {
                    infinitLoopCheck = true;
                    infinitLoopID = k;
                }
                return k;
            }

            //case: wrong edge is in left of real edge
            for (int j = 0; j <= 5; j++) {
                if (i + j + 1 > GlobalProperty.SmoothedChromatogram.Peaks.Count - 1) break;
                if (GlobalProperty.SmoothedChromatogram.Peaks[i + j].Intensity <= GlobalProperty.SmoothedChromatogram.Peaks[i + j + 1].Intensity) break;
                if (GlobalProperty.SmoothedChromatogram.Peaks[i + j].Intensity > GlobalProperty.SmoothedChromatogram.Peaks[i + j + 1].Intensity) {
                    trackcounter++;
                }
            }
            return i + trackcounter;
        }

        internal bool IsNoise(double maxPeakHeight, double minPeakHeight, double _minimumAmplitudeCriteria, double amplitudeNoiseFoldCriteria, int start, int end) {
            return maxPeakHeight < GlobalProperty.Noise || minPeakHeight < _minimumAmplitudeCriteria || minPeakHeight < (double)Noises.AmplitudeNoise * amplitudeNoiseFoldCriteria || (GlobalProperty.IsHighBaseline && Math.Min(Chromatogram.Peaks[start].Intensity, Chromatogram.Peaks[end - 1].Intensity) < (double)GlobalProperty.BaselineMedian);
        }

        internal int GetPeakTop(int start, int end) {
            var peakTopIntensity = double.MinValue;
            var peakTopId = start;
            for (int i = start; i < end; i++) {
                if (peakTopIntensity < Chromatogram.Peaks[i].Intensity) {
                    peakTopIntensity = Chromatogram.Peaks[i].Intensity;
                    peakTopId = i;
                }
            }
            return peakTopId;
        }

        internal (int, int, int) CuratPeakRange(int start, int end, double averagePeakWidth) {
            var peakTopId = GetPeakTop(start, end);

            var newStart = start;
            for (int j = peakTopId - (int)averagePeakWidth; j >= start; j--) {
                if (j - 1 < start) {
                    break;
                }
                if (Chromatogram.Peaks[j - 1].Intensity >= Chromatogram.Peaks[j].Intensity) {
                    newStart = j;
                    break;
                }
            }

            var newEnd = end;
            for (int j = peakTopId + (int)averagePeakWidth; j < end; j++) {
                if (j + 1 >= end) {
                    break;
                }
                if (Chromatogram.Peaks[j].Intensity <= Chromatogram.Peaks[j + 1].Intensity) {
                    newEnd = j + 1;
                    break;
                }
            }

            return (newStart, peakTopId, newEnd);
        }

        internal (double, double) PeakHeightFromBounds(int start, int end, int top) {
            var topIntensity = Chromatogram.Peaks[top].Intensity;
            var leftIntensity = Chromatogram.Peaks[start].Intensity;
            var rightIntensity = Chromatogram.Peaks[end - 1].Intensity;
            return (topIntensity - Math.Max(leftIntensity, rightIntensity), topIntensity - Math.Min(leftIntensity, rightIntensity));
        }

        internal PeakDetectionResult GetPeakDetectionResult(int peakTopId, int start, int end, double noiseFactor, double maxPeakHeight) {
            //1. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
            if (end - start <= 3) return null;

            var peaks = Chromatogram.Peaks;
            if (peaks[peakTopId].Intensity - peaks[start].Intensity < 0 && peaks[peakTopId].Intensity - peaks[end - 1].Intensity < 0) return null;

            var idealSlopeValue = 0d;
            var nonIdealSlopeValue = 0d;
            var peakHalfDiff = double.MaxValue;
            var peakFivePercentDiff = double.MaxValue;
            var leftShapenessValue = double.MinValue;
            int leftPeakFivePercentId = -1;
            int leftPeakHalfId = -1;
            for (int j = peakTopId; j >= start; j--) {
                if (peakHalfDiff > Math.Abs((peaks[peakTopId].Intensity - peaks[start].Intensity) / 2 - (peaks[j].Intensity - peaks[start].Intensity))) {
                    peakHalfDiff = Math.Abs((peaks[peakTopId].Intensity - peaks[start].Intensity) / 2 - (peaks[j].Intensity - peaks[start].Intensity));
                    leftPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((peaks[peakTopId].Intensity - peaks[start].Intensity) / 5 - (peaks[j].Intensity - peaks[start].Intensity))) {
                    peakFivePercentDiff = Math.Abs((peaks[peakTopId].Intensity - peaks[start].Intensity) / 5 - (peaks[j].Intensity - peaks[start].Intensity));
                    leftPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (leftShapenessValue < (peaks[peakTopId].Intensity - peaks[j].Intensity) / (peakTopId - j) / Math.Sqrt(peaks[peakTopId].Intensity))
                    leftShapenessValue = (peaks[peakTopId].Intensity - peaks[j].Intensity) / (peakTopId - j) / Math.Sqrt(peaks[peakTopId].Intensity);

                if (peaks[j + 1].Intensity - peaks[j].Intensity >= 0)
                    idealSlopeValue += Math.Abs(peaks[j + 1].Intensity - peaks[j].Intensity);
                else
                    nonIdealSlopeValue += Math.Abs(peaks[j + 1].Intensity - peaks[j].Intensity);
            }

            peakHalfDiff = double.MaxValue;
            peakFivePercentDiff = double.MaxValue;
            var rightShapenessValue = double.MinValue;
            int rightPeakHalfId = -1;
            int rightPeakFivePercentId = -1;
            for (int j = peakTopId; j < end; j++) {
                if (peakHalfDiff > Math.Abs((peaks[peakTopId].Intensity - peaks[end - 1].Intensity) / 2 - (peaks[j].Intensity - peaks[end - 1].Intensity))) {
                    peakHalfDiff = Math.Abs((peaks[peakTopId].Intensity - peaks[end - 1].Intensity) / 2 - (peaks[j].Intensity - peaks[end - 1].Intensity));
                    rightPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((peaks[peakTopId].Intensity - peaks[end - 1].Intensity) / 5 - (peaks[j].Intensity - peaks[end - 1].Intensity))) {
                    peakFivePercentDiff = Math.Abs((peaks[peakTopId].Intensity - peaks[end - 1].Intensity) / 5 - (peaks[j].Intensity - peaks[end - 1].Intensity));
                    rightPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (rightShapenessValue < (peaks[peakTopId].Intensity - peaks[j].Intensity) / (j - peakTopId) / Math.Sqrt(peaks[peakTopId].Intensity))
                    rightShapenessValue = (peaks[peakTopId].Intensity - peaks[j].Intensity) / (j - peakTopId) / Math.Sqrt(peaks[peakTopId].Intensity);

                if (peaks[j - 1].Intensity - peaks[j].Intensity >= 0)
                    idealSlopeValue += Math.Abs(peaks[j - 1].Intensity - peaks[j].Intensity);
                else
                    nonIdealSlopeValue += Math.Abs(peaks[j - 1].Intensity - peaks[j].Intensity);
            }

            double gaussianNormalize;
            double basePeakValue;
            int peakHalfId = -1;
            if (peaks[start].Intensity <= peaks[end - 1].Intensity) {
                gaussianNormalize = peaks[peakTopId].Intensity - peaks[start].Intensity;
                peakHalfId = leftPeakHalfId;
                basePeakValue = Math.Abs((peaks[peakTopId].Intensity - peaks[end - 1].Intensity) / (peaks[peakTopId].Intensity - peaks[start].Intensity));
            }
            else {
                gaussianNormalize = peaks[peakTopId].Intensity - peaks[end - 1].Intensity;
                peakHalfId = rightPeakHalfId;
                basePeakValue = Math.Abs((peaks[peakTopId].Intensity - peaks[start].Intensity) / (peaks[peakTopId].Intensity - peaks[end - 1].Intensity));
            }

            double symmetryValue;
            if (Math.Abs(peaks[peakTopId].Time - peaks[leftPeakFivePercentId].Time) <= Math.Abs(peaks[peakTopId].Time - peaks[rightPeakFivePercentId].Time))
                symmetryValue = Math.Abs(peaks[peakTopId].Time - peaks[leftPeakFivePercentId].Time) / Math.Abs(peaks[peakTopId].Time - peaks[rightPeakFivePercentId].Time);
            else
                symmetryValue = Math.Abs(peaks[peakTopId].Time - peaks[rightPeakFivePercentId].Time) / Math.Abs(peaks[peakTopId].Time - peaks[leftPeakFivePercentId].Time);

            double peakHwhm = Math.Abs(peaks[peakHalfId].Time - peaks[peakTopId].Time);

            //2. Calculate peak pure value (from gaussian area and real area)
            double gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
            double gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

            double realAreaAboveZero = 0;
            double leftPeakArea = 0;
            double rightPeakArea = 0;
            for (int j = start; j < end - 1; j++) {
                realAreaAboveZero += (peaks[j].Intensity + peaks[j + 1].Intensity) * (peaks[j + 1].Time - peaks[j].Time) * 0.5;
                if (j == peakTopId - 1)
                    leftPeakArea = realAreaAboveZero;
                else if (j == end - 2)
                    rightPeakArea = realAreaAboveZero - leftPeakArea;
            }


            double realAreaAboveBaseline = realAreaAboveZero - (peaks[start].Intensity + peaks[end - 1].Intensity) * (peaks[end - 1].Time - peaks[start].Time) / 2;

            if (peaks[start].Intensity <= peaks[end - 1].Intensity) {
                leftPeakArea = leftPeakArea - peaks[start].Intensity * (peaks[peakTopId].Time - peaks[start].Time);
                rightPeakArea = rightPeakArea - peaks[start].Intensity * (peaks[end - 1].Time - peaks[peakTopId].Time);
            }
            else {
                leftPeakArea = leftPeakArea - peaks[end - 1].Intensity * (peaks[peakTopId].Time - peaks[start].Time);
                rightPeakArea = rightPeakArea - peaks[end - 1].Intensity * (peaks[end - 1].Time - peaks[peakTopId].Time);
            }

            double gaussianSimilarityLeftValue;
            if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
            else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

            double gaussianSimilarityRightValue;
            if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
            else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

            double gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;
            idealSlopeValue = (idealSlopeValue - nonIdealSlopeValue) / idealSlopeValue;

            if (idealSlopeValue < 0) idealSlopeValue = 0;

            double peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue + idealSlopeValue) / 4;
            if (peakPureValue > 1) peakPureValue = 1;
            if (peakPureValue < 0) peakPureValue = 0;

            //3. Set area information
            var estimatedNoise = Math.Max(1f, (float)(GlobalProperty.Noise / noiseFactor));
            return new PeakDetectionResult
            {
                PeakID = -1,
                AmplitudeOrderValue = -1,
                AmplitudeScoreValue = -1,
                AreaAboveBaseline = (float)(realAreaAboveBaseline * 60),
                AreaAboveZero = (float)(realAreaAboveZero * 60),
                BasePeakValue = (float)basePeakValue,
                GaussianSimilarityValue = (float)gaussinaSimilarityValue,
                IdealSlopeValue = (float)idealSlopeValue,
                IntensityAtLeftPeakEdge = (float)peaks[start].Intensity,
                IntensityAtPeakTop = (float)peaks[peakTopId].Intensity,
                IntensityAtRightPeakEdge = (float)peaks[end - 1].Intensity,
                PeakPureValue = (float)peakPureValue,
                ChromXAxisAtLeftPeakEdge = (float)peaks[start].Time,
                ChromXAxisAtPeakTop = (float)peaks[peakTopId].Time,
                ChromXAxisAtRightPeakEdge = (float)peaks[end - 1].Time,
                ScanNumAtLeftPeakEdge = start,
                ScanNumAtPeakTop = peakTopId,
                ScanNumAtRightPeakEdge = end - 1,
                ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
                SymmetryValue = (float)symmetryValue,
                EstimatedNoise = estimatedNoise,
                SignalToNoise = (float)(maxPeakHeight / estimatedNoise),
            };
        }

        public List<PeakDetectionResult> DetectPeaks(double noiseFactor, double averagePeakWidth, double amplitudeNoiseFoldCriteria, double slopeNoiseFoldCriteria, double minimumDatapointCriteria, double minimumAmplitudeCriteria) {
            var results = new List<PeakDetectionResult>();
            var infinitLoopCheck = false;
            var infinitLoopID = 0;
            var margin = Math.Max((int)minimumDatapointCriteria, 5);
            for (int i = margin; i < this.GlobalProperty.SmoothedChromatogram.Peaks.Count - margin; i++) {
                if (this.IsPeakStarted(i, slopeNoiseFoldCriteria)) {
                    var start = this.SearchRealLeftEdge2(i);
                    var j = this.SearchRightEdgeCandidate2(i, minimumDatapointCriteria, slopeNoiseFoldCriteria);
                    j = this.SearchRealRightEdge2(j, ref infinitLoopCheck, ref infinitLoopID, out var isBreak);
                    if (isBreak) {
                        break;
                    }
                    i = Math.Max(i, j);
                    if (j - start + 1 < minimumDatapointCriteria) {
                        continue;
                    }
                    var (newStart, peakTopID, newEnd) = this.CuratPeakRange(start, j + 1, averagePeakWidth);
                    var (minPeakHeight, maxPeakHeight) = this.PeakHeightFromBounds(newStart, newEnd, peakTopID);
                    if (this.IsNoise(maxPeakHeight, minPeakHeight, minimumAmplitudeCriteria, amplitudeNoiseFoldCriteria, newStart, newEnd)) {
                        continue;
                    }
                    var result = this.GetPeakDetectionResult(peakTopID, newStart, newEnd, noiseFactor, maxPeakHeight);
                    if (result is null) continue;
                    result.PeakID = results.Count;
                    results.Add(result);
                }
            }
            if (results.Count != 0) {
                var sResults = results.OrderByDescending(result => result.IntensityAtPeakTop).ToArray();
                float maxIntensity = sResults[0].IntensityAtPeakTop;
                for (int i = 0; i < sResults.Length; i++) {
                    sResults[i].AmplitudeScoreValue = sResults[i].IntensityAtPeakTop / maxIntensity;
                    sResults[i].AmplitudeOrderValue = i + 1;
                }
            }
            return results;
        }
    }

    public sealed class ChromatogramGlobalProperty_temp2
    {
        private readonly static double[] FIRST_DIFF_COEFF = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
        private readonly static double[] SECOND_DIFF_COEFF = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };

        public ChromatogramGlobalProperty_temp2(double maxIntensity, double minIntensity, double baselineMedian, double noise, bool isHighBaseline,
            Chromatogram_temp2 smoothedPeakList, Chromatogram_temp2 baseline, Chromatogram_temp2 baselineCorrectedPeakList) {
            MaxIntensity = maxIntensity;
            MinIntensity = minIntensity;
            BaselineMedian = baselineMedian;
            Noise = noise;
            IsHighBaseline = isHighBaseline;
            SmoothedChromatogram = smoothedPeakList;
            BaselineChromatogram = baseline;
            BaselineCorrectedChromatogram = baselineCorrectedPeakList;
        }

        public double MaxIntensity { get; }
        public double MinIntensity { get; }
        public double BaselineMedian { get; }
        public double Noise { get; }
        public bool IsHighBaseline { get; }
        public IReadOnlyList<ValuePeak> SmoothedPeakList => SmoothedChromatogram.Peaks;
        public IReadOnlyList<ValuePeak> Baseline => BaselineChromatogram.Peaks;
        public IReadOnlyList<ValuePeak> BaselineCorrectedPeakList => BaselineCorrectedChromatogram.Peaks;
        public Chromatogram_temp2 SmoothedChromatogram { get; }
        public Chromatogram_temp2 BaselineChromatogram { get; }
        public Chromatogram_temp2 BaselineCorrectedChromatogram { get; }

        public DifferencialCoefficients GenerateDifferencialCoefficients() {
            var ssPeaklist = SmoothedChromatogram.Peaks;

            var firstDiffPeaklist = new List<double>(ssPeaklist.Count);
            var secondDiffPeaklist = new List<double>(ssPeaklist.Count);

            var maxFirstDiff = double.MinValue;
            var maxSecondDiff = double.MinValue;
            var maxAmplitudeDiff = double.MinValue;

            double firstDiff, secondDiff;
            int halfDatapoint = (int)(FIRST_DIFF_COEFF.Length / 2);

            for (int i = 0; i < ssPeaklist.Count; i++) {
                if (i < halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }
                if (i >= ssPeaklist.Count - halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }

                firstDiff = secondDiff = 0;
                for (int j = 0; j < FIRST_DIFF_COEFF.Length; j++) {
                    firstDiff += FIRST_DIFF_COEFF[j] * ssPeaklist[i + j - halfDatapoint].Intensity;
                    secondDiff += SECOND_DIFF_COEFF[j] * ssPeaklist[i + j - halfDatapoint].Intensity;
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(ssPeaklist[i].Intensity - ssPeaklist[i - 1].Intensity) > maxAmplitudeDiff)
                    maxAmplitudeDiff = Math.Abs(ssPeaklist[i].Intensity - ssPeaklist[i - 1].Intensity);
            }
            return new DifferencialCoefficients(firstDiffPeaklist, secondDiffPeaklist, maxAmplitudeDiff, maxFirstDiff, maxSecondDiff);
        }

        public ChromatogramNoises CalculateSlopeNoises(DifferencialCoefficients differencialCoefficients) {
            var amplitudeNoiseCandidate = new List<double>();
            var slopeNoiseCandidate = new List<double>();
            var peaktopNoiseCandidate = new List<double>();
            double amplitudeNoiseThresh = differencialCoefficients.MaxAmplitudeDiff * 0.05, slopeNoiseThresh = differencialCoefficients.MaxFirstDiff * 0.05, peaktopNoiseThresh = differencialCoefficients.MaxSecondDiff * 0.05;
            var firstDiffPeaklist = differencialCoefficients.FirstDiffPeaklist;
            var secondDiffPeaklist = differencialCoefficients.SecondDiffPeaklist;
            var ssPeaklist = SmoothedChromatogram.Peaks;
            for (int i = 2; i < ssPeaklist.Count - 2; i++) {
                if (Math.Abs(ssPeaklist[i + 1].Intensity - ssPeaklist[i].Intensity) < amplitudeNoiseThresh &&
                    Math.Abs(ssPeaklist[i + 1].Intensity - ssPeaklist[i].Intensity) > 0)
                    amplitudeNoiseCandidate.Add(Math.Abs(ssPeaklist[i + 1].Intensity - ssPeaklist[i].Intensity));
                if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0)
                    slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
                if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh &&
                    Math.Abs(secondDiffPeaklist[i]) > 0)
                    peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
            }
            var amplitudeNoise = amplitudeNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(amplitudeNoiseCandidate);
            var slopeNoise = slopeNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(slopeNoiseCandidate);
            var peaktopNoise = peaktopNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(peaktopNoiseCandidate);
            return new ChromatogramNoises(amplitudeNoise, slopeNoise, peaktopNoise); ;
        }
    }

    public sealed class DifferencialCoefficients {
        internal DifferencialCoefficients(List<double> firstDiffPeaklist, List<double> secondDiffPeaklist, double maxAmplitudeDiff, double maxFirstDiff, double maxSecondDiff) {
            FirstDiffPeaklist = firstDiffPeaklist;
            SecondDiffPeaklist = secondDiffPeaklist;
            MaxAmplitudeDiff = maxAmplitudeDiff;
            MaxFirstDiff = maxFirstDiff;
            MaxSecondDiff = maxSecondDiff;
        }

        public List<double> FirstDiffPeaklist { get; }
        public List<double> SecondDiffPeaklist { get; }
        public double MaxAmplitudeDiff { get; }
        public double MaxFirstDiff { get; }
        public double MaxSecondDiff { get; }
    }

    public sealed class ChromatogramNoises {
        public ChromatogramNoises(double amplitudeNoise, double slopeNoise, double peakTopNoise) {
            AmplitudeNoise = amplitudeNoise;
            SlopeNoise = slopeNoise;
            PeakTopNoise = peakTopNoise;
        }

        public double AmplitudeNoise { get; }
        public double SlopeNoise { get; }
        public double PeakTopNoise { get; }
    }
}
