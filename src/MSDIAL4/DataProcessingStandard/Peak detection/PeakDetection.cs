using Accord.Statistics.Kernels;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    public sealed class PeakDetection
    {
        private PeakDetection(){}

        // below is a global peak detection method for gcms/lcms data preprocessing
        public static ObservableCollection<PeakDetectionResult> PeakDetectionVS1(
            double minimumDatapointCriteria, double minimumAmplitudeCriteria, List<double[]> peaklist) {

            var results = new ObservableCollection<PeakDetectionResult>();
            #region

            // global parameter
            var averagePeakWidth = 20.0;
            var amplitudeNoiseFoldCriteria = 4.0;
            var slopeNoiseFoldCriteria = 2.0;
            var peaktopNoiseFoldCriteria = 2.0;

            var smoother = 1;
            var baselineLevel = 30;
            var noiseEstimateBin = 50;
            var minNoiseWindowSize = 10;
            var minNoiseLevel = 50.0;
            var noiseFactor = 3.0;

            // 'chromatogram' properties
            double maxChromIntensity, minChromIntensity, baselineMedian, noise;
            List<double[]> ssPeaklist, baseline, baselineCorrectedPeaklist;
            bool isHighBaseline;
            findChromatogramGlobalProperties(peaklist, smoother, baselineLevel, 
                noiseEstimateBin, minNoiseWindowSize, minNoiseLevel, noiseFactor,
                out maxChromIntensity, out minChromIntensity, out baselineMedian, out noise, out isHighBaseline,
                out ssPeaklist, out baseline, out baselineCorrectedPeaklist);

            // differential factors
            List<double> firstDiffPeaklist, secondDiffPeaklist;
            double maxFirstDiff, maxSecondDiff, maxAmplitudeDiff;
            generateDifferencialCoefficients(ssPeaklist, out firstDiffPeaklist, out secondDiffPeaklist, out maxAmplitudeDiff, out maxFirstDiff, out maxSecondDiff);

            // slope noises
            double amplitudeNoise, slopeNoise, peaktopNoise;
            calculateSlopeNoises(ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, maxAmplitudeDiff, maxFirstDiff, maxSecondDiff, 
                out amplitudeNoise, out slopeNoise, out peaktopNoise);

            var datapoints = new List<double[]>();
            var infinitLoopCheck = false;
            var infinitLoopID = 0;
            var nextPeakCheck = false;
            var nextPeakCheckReminder = 0;
            var margin = 5;
            if (minimumDatapointCriteria > margin) margin = (int)minimumDatapointCriteria;


            var peakCounter = 0;
            for (int i = margin; i < ssPeaklist.Count - margin; i++) {
                if (i > nextPeakCheckReminder + 2) nextPeakCheck = false;
                if (isPeakStarted(i, ssPeaklist, firstDiffPeaklist, slopeNoise, slopeNoiseFoldCriteria, results, nextPeakCheck)) {

                    //if (Math.Abs(ssPeaklist[i][1] - 1.967) < 0.05 && Math.Abs(ssPeaklist[i][2] - 609.14703) < 0.1) {
                    //    Console.WriteLine("rt {0}, m/z {1}, intensity {2}", ssPeaklist[i][1], ssPeaklist[i][2], ssPeaklist[i][3]);
                    //}


                    datapoints = new List<double[]>();
                    datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3],
                        firstDiffPeaklist[i], secondDiffPeaklist[i] });
                    searchRealLeftEdge(i, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist);
                    i = searchRightEdgeCandidate(i, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, 
                        slopeNoise, slopeNoiseFoldCriteria, amplitudeNoise, peaktopNoise, minimumDatapointCriteria);
                    var isBreak = false;
                    i = searchRealRightEdge(i, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, ref infinitLoopCheck, ref infinitLoopID, out isBreak);
                    if (isBreak) break;
                    if (datapoints.Count < minimumDatapointCriteria) continue;
                    var peaktopID = 0;
                    curateDatapoints(datapoints, averagePeakWidth, out peaktopID);

                    var maxPeakHeight = 0.0;
                    var minPeakHeight = 0.0;
                    peakHeightFromBaseline(datapoints, peaktopID, out maxPeakHeight, out minPeakHeight);
                    if (maxPeakHeight < noise) continue;
                    if (minPeakHeight < minimumAmplitudeCriteria || minPeakHeight < amplitudeNoise * amplitudeNoiseFoldCriteria) continue;
                    if (isHighBaseline && Math.Min(datapoints[0][3], datapoints[datapoints.Count - 1][3]) < baselineMedian) continue;

                    var result = GetPeakDetectionResult(datapoints, peaktopID);
                    if (result == null) continue;
                    result.PeakID = peakCounter;
                    result.EstimatedNoise = (float)(noise / noiseFactor);
                    if (result.EstimatedNoise < 1.0) result.EstimatedNoise = 1.0F;
                    result.SignalToNoise = (float)(maxPeakHeight / result.EstimatedNoise);
                    
                    results.Add(result);

                    peakCounter++;
                }
            }
            #endregion
            if (results.Count == 0) return null;
            return finalizePeakDetectionResults(results);
        }

        #region methods

        private static ObservableCollection<PeakDetectionResult> finalizePeakDetectionResults(ObservableCollection<PeakDetectionResult> results) {

            var sResults = results.OrderByDescending(n => n.IntensityAtPeakTop).ToList();
            float maxIntensity = sResults[0].IntensityAtPeakTop;
            for (int i = 0; i < sResults.Count; i++) {
                sResults[i].AmplitudeScoreValue = sResults[i].IntensityAtPeakTop / maxIntensity;
                sResults[i].AmplitudeOrderValue = i + 1;
            }
            sResults = sResults.OrderBy(n => n.PeakID).ToList();
            return new ObservableCollection<PeakDetectionResult>(sResults);
        }

        private static void peakHeightFromBaseline(List<double[]> datapoints, int peaktopID, out double maxPeakHeight, out double minPeakHeight) {
            var peaktopInt = datapoints[peaktopID][3];
            var peakleftInt = datapoints[0][3];
            var peakrightInt = datapoints[datapoints.Count - 1][3];


            maxPeakHeight = Math.Max(peaktopInt - peakleftInt, peaktopInt - peakrightInt);
            minPeakHeight = Math.Min(peaktopInt - peakleftInt, peaktopInt - peakrightInt);
        }

        private static void curateDatapoints(List<double[]> datapoints, double averagePeakWidth, out int peakTopId) {
            peakTopId = -1;

            var peakTopIntensity = double.MinValue;
            var excludedLeftCutPoint = 0;
            var excludedRightCutPoint = 0;

            for (int j = 0; j < datapoints.Count; j++) {
                if (peakTopIntensity < datapoints[j][3]) {
                    peakTopIntensity = datapoints[j][3];
                    peakTopId = j;
                }
            }
            if (peakTopId > averagePeakWidth) {
                excludedLeftCutPoint = 0;
                for (int j = peakTopId - (int)averagePeakWidth; j >= 0; j--) {
                    if (j - 1 <= 0) break;
                    if (datapoints[j][3] <= datapoints[j - 1][3]) {
                        excludedLeftCutPoint = j;
                        break;
                    }
                }
                if (excludedLeftCutPoint > 0) {
                    for (int j = 0; j < excludedLeftCutPoint; j++)
                        datapoints.RemoveAt(0);
                    peakTopId = peakTopId - excludedLeftCutPoint;
                }
            }
            if (datapoints.Count - 1 > peakTopId + averagePeakWidth) {
                excludedRightCutPoint = 0;
                for (int j = peakTopId + (int)averagePeakWidth; j < datapoints.Count; j++) {
                    if (j + 1 > datapoints.Count - 1) break;
                    if (datapoints[j][3] <= datapoints[j + 1][3]) { excludedRightCutPoint = datapoints.Count - 1 - j; break; }
                }
                if (excludedRightCutPoint > 0)
                    for (int j = 0; j < excludedRightCutPoint; j++)
                        datapoints.RemoveAt(datapoints.Count - 1);
            }
        }

        private static int searchRealRightEdge(int i, List<double[]> datapoints, List<double[]> peaklist, List<double[]> ssPeaklist, 
            List<double> firstDiffPeaklist, List<double> secondDiffPeaklist, ref bool infinitLoopCheck, ref int infinitLoopID, out bool isBreak) {
            //Search real right edge within 5 data points
            var rightCheck = false;
            var trackcounter = 0;
            isBreak = false;
            
            //case: wrong edge is in right of real edge
            if (rightCheck == false) {
                for (int j = 0; j <= 5; j++) {
                    if (i - j - 1 < 0) break;
                    if (ssPeaklist[i - j][3] <= ssPeaklist[i - j - 1][3]) break;
                    if (ssPeaklist[i - j][3] > ssPeaklist[i - j - 1][3]) {
                        datapoints.RemoveAt(datapoints.Count - 1);
                        rightCheck = true;
                        trackcounter++;
                    }
                }
                if (trackcounter > 0) {
                    i -= trackcounter;
                    if (infinitLoopCheck == true && i == infinitLoopID && i > ssPeaklist.Count - 10) {
                        isBreak = true;
                        return i;
                    };
                    infinitLoopCheck = true; infinitLoopID = i;
                }
            }

            //case: wrong edge is in left of real edge
            if (rightCheck == false) {
                for (int j = 0; j <= 5; j++) {
                    if (i + j + 1 > ssPeaklist.Count - 1) break;
                    if (ssPeaklist[i + j][3] <= ssPeaklist[i + j + 1][3]) break;
                    if (ssPeaklist[i + j][3] > ssPeaklist[i + j + 1][3]) {
                        datapoints.Add(new double[] { peaklist[i + j + 1][0], peaklist[i + j + 1][1], peaklist[i + j + 1][2],
                                    peaklist[i + j + 1][3], firstDiffPeaklist[i + j + 1], secondDiffPeaklist[i + j + 1] });
                        rightCheck = true;
                        trackcounter++;
                    }
                }
                if (trackcounter > 0) i += trackcounter;
            }
            return i;
        }
        private static int searchRightEdgeCandidate(int i, List<double[]> datapoints, List<double[]> peaklist, List<double[]> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist, 
            double slopeNoise, double slopeNoiseFoldCriteria, double amplitudeNoise, double peaktopNoise, double minimumDatapointCriteria) {
            var peaktopCheck = false;
            var peaktopCheckPoint = i;
            while (true) {
                if (i + 2 == ssPeaklist.Count - 1) break;

                i++;
                datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3],
                            firstDiffPeaklist[i], secondDiffPeaklist[i] });
                
                // peak top check
                if (peaktopCheck == false &&
                    (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0) || (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i + 1] < 0) &&
                    secondDiffPeaklist[i] < -1 * peaktopNoise) {
                    peaktopCheck = true; peaktopCheckPoint = i;
                }
                
                if (peaktopCheck == false && 
                    (ssPeaklist[i - 2][3] <= ssPeaklist[i - 1][3]) &&
                    (ssPeaklist[i - 1][3] <= ssPeaklist[i][3]) &&
                    (ssPeaklist[i][3] >= ssPeaklist[i + 1][3]) &&
                    (ssPeaklist[i + 1][3] >= ssPeaklist[i + 2][3])) {
                    peaktopCheck = true; peaktopCheckPoint = i;
                }

                // peak top check force
                if (peaktopCheck == false && minimumDatapointCriteria < 1.5 &&
                    ((ssPeaklist[i - 2][3] <= ssPeaklist[i - 1][3]) &&
                    (ssPeaklist[i - 1][3] <= ssPeaklist[i][3]) &&
                    (ssPeaklist[i][3] >= ssPeaklist[i + 1][3])) || 
                    ((ssPeaklist[i - 1][3] <= ssPeaklist[i][3]) &&
                    (ssPeaklist[i][3] >= ssPeaklist[i + 1][3]) &&
                    (ssPeaklist[i + 1][3] >= ssPeaklist[i + 2][3]))) {
                    peaktopCheck = true; peaktopCheckPoint = i;
                }

                var minimumPointFromTop = minimumDatapointCriteria <= 3 ? 1 : minimumDatapointCriteria * 0.5;
                if (peaktopCheck == true && peaktopCheckPoint + minimumPointFromTop <= i - 1) {
                    if (firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
                    if (Math.Abs(ssPeaklist[i - 2][3] - ssPeaklist[i - 1][3]) < amplitudeNoise &&
                          Math.Abs(ssPeaklist[i - 1][3] - ssPeaklist[i][3]) < amplitudeNoise) break;
                    
                    if ((ssPeaklist[i - 2][3] >= ssPeaklist[i - 1][3]) &&
                        (ssPeaklist[i - 1][3] >= ssPeaklist[i][3]) &&
                        (ssPeaklist[i][3] <= ssPeaklist[i + 1][3]) &&
                        (ssPeaklist[i + 1][3] <= ssPeaklist[i + 2][3])) break;

                    // peak right check force
                    if (minimumDatapointCriteria < 1.5 &&
                        ((ssPeaklist[i - 2][3] >= ssPeaklist[i - 1][3]) &&
                        (ssPeaklist[i - 1][3] >= ssPeaklist[i][3]) &&
                        (ssPeaklist[i][3] <= ssPeaklist[i + 1][3])) ||
                        ((ssPeaklist[i - 1][3] >= ssPeaklist[i][3]) &&
                        (ssPeaklist[i][3] <= ssPeaklist[i + 1][3]) &&
                        (ssPeaklist[i + 1][3] <= ssPeaklist[i + 2][3]))) {
                        peaktopCheck = true; peaktopCheckPoint = i;
                    }
                }
            }
            return i;
        }
        private static void searchRealLeftEdge(int i, List<double[]> datapoints, List<double[]> peaklist, List<double[]> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist) {
            //search real left edge within 5 data points
            for (int j = 0; j <= 5; j++) {
                if (i - j - 1 < 0) break;
                if (ssPeaklist[i - j][3] <= ssPeaklist[i - j - 1][3]) break;
                if (ssPeaklist[i - j][3] > ssPeaklist[i - j - 1][3])
                    datapoints.Insert(0, new double[] { peaklist[i - j - 1][0], peaklist[i - j - 1][1],
                                peaklist[i - j - 1][2], peaklist[i - j - 1][3], firstDiffPeaklist[i - j - 1], secondDiffPeaklist[i - j - 1] });
            }
        }

        private static bool isPeakStarted(int index, List<double[]> ssPeaklist, List<double> firstDiffPeaklist, 
            double slopeNoise, double slopeNoiseFoldCriteria, ObservableCollection<PeakDetectionResult> peakDetectionResults, bool nextPeakCheck) {

            if (firstDiffPeaklist[index] > slopeNoise * slopeNoiseFoldCriteria && 
                firstDiffPeaklist[index + 1] > slopeNoise * slopeNoiseFoldCriteria ||
                (nextPeakCheck &&
                peakDetectionResults[peakDetectionResults.Count - 1].IntensityAtRightPeakEdge < ssPeaklist[index][3] &&
                ssPeaklist[index][3] < ssPeaklist[index + 1][3] && ssPeaklist[index + 1][3] < ssPeaklist[index + 2][3])) {
                return true;
            }
            else {
                return false;
            }
        }

        private static void calculateSlopeNoises(List<double[]> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist,
            double maxAmplitudeDiff, double maxFirstDiff, double maxSecondDiff, out double amplitudeNoise, out double slopeNoise, out double peaktopNoise) {

            var amplitudeNoiseCandidate = new List<double>();
            var slopeNoiseCandidate = new List<double>();
            var peaktopNoiseCandidate = new List<double>();
            double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
            for (int i = 2; i < ssPeaklist.Count - 2; i++) {
                if (Math.Abs(ssPeaklist[i + 1][3] - ssPeaklist[i][3]) < amplitudeNoiseThresh &&
                    Math.Abs(ssPeaklist[i + 1][3] - ssPeaklist[i][3]) > 0)
                    amplitudeNoiseCandidate.Add(Math.Abs(ssPeaklist[i + 1][3] - ssPeaklist[i][3]));
                if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0)
                    slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
                if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh &&
                    Math.Abs(secondDiffPeaklist[i]) > 0)
                    peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
            }
            if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
            if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
            if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
        }

        private static void generateDifferencialCoefficients(List<double[]> ssPeaklist, out List<double> firstDiffPeaklist, out List<double> secondDiffPeaklist,
            out double maxAmplitudeDiff, out double maxFirstDiff, out double maxSecondDiff) {

            firstDiffPeaklist = new List<double>();
            secondDiffPeaklist = new List<double>();

            maxFirstDiff = double.MinValue;
            maxSecondDiff = double.MinValue;
            maxAmplitudeDiff = double.MinValue;

            var firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
            var secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
            double firstDiff, secondDiff;
            int halfDatapoint = (int)(firstDiffCoeff.Length / 2);

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
                for (int j = 0; j < firstDiffCoeff.Length; j++) {
                    firstDiff += firstDiffCoeff[j] * ssPeaklist[i + j - halfDatapoint][3];
                    secondDiff += secondDiffCoeff[j] * ssPeaklist[i + j - halfDatapoint][3];
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(ssPeaklist[i][3] - ssPeaklist[i - 1][3]) > maxAmplitudeDiff)
                    maxAmplitudeDiff = Math.Abs(ssPeaklist[i][3] - ssPeaklist[i - 1][3]);
            }
        }

        private static void findChromatogramGlobalProperties(List<double[]> peaklist, int smoother, int baselineLevel, 
            int noiseEstimateBin, int minNoiseWindowSize, double minNoiseLevel, double noiseFactor,
            out double maxChromIntensity, out double minChromIntensity, out double baselineMedian, out double noise, out bool isHighBaseline,
            out List<double[]> ssPeaklist, out List<double[]> baseline, out List<double[]> baselineCorrectedPeaklist) {

            // checking chromatogram properties
            maxChromIntensity = double.MinValue;
            minChromIntensity = double.MaxValue;
            ssPeaklist = Smoothing.LinearWeightedMovingAverage(Smoothing.LinearWeightedMovingAverage(peaklist, 1), 1);
            baseline = Smoothing.SimpleMovingAverage(Smoothing.SimpleMovingAverage(peaklist, 30), 30);
            baselineCorrectedPeaklist = new List<double[]>();

            var amplitudeDiffs = new List<double>();
            var counter = noiseEstimateBin;
            var ampMax = double.MinValue;
            var ampMin = double.MaxValue;

            for (int i = 0; i < peaklist.Count; i++) {
                var intensity = ssPeaklist[i][3] - baseline[i][3];
                if (intensity < 0) intensity = 0;
                baselineCorrectedPeaklist.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], intensity });

                if (peaklist[i][3] > maxChromIntensity) maxChromIntensity = peaklist[i][3];
                if (peaklist[i][3] < minChromIntensity) minChromIntensity = peaklist[i][3];

                if (counter < i) {
                    if (ampMax > ampMin) {
                        amplitudeDiffs.Add(ampMax - ampMin);
                    }

                    counter += noiseEstimateBin;
                    ampMax = double.MinValue;
                    ampMin = double.MaxValue;
                }
                else {
                    if (ampMax < intensity) ampMax = intensity;
                    if (ampMin > intensity) ampMin = intensity;
                }
            }
            baselineMedian = BasicMathematics.Median(baseline, 3);
            isHighBaseline = baselineMedian > (maxChromIntensity - minChromIntensity) * 0.5 ? true : false;

            if (amplitudeDiffs.Count >= minNoiseWindowSize) {
                minNoiseLevel = BasicMathematics.Median(amplitudeDiffs.ToArray());
            }

            noise = minNoiseLevel * noiseFactor;
        }
        #endregion method

        ///// <summary>
        ///// This is the peak detection method from a chromatogram used in all programs, MS-DIAL, MRM-PROBS, and MRM-DIFF programs.
        ///// This method returns the collection of PeakDetectionResult from a EIC or BIC or TIC chromatogram.
        ///// </summary>
        ///// <param name="minimumDatapointCriteria">
        ///// User-defined: the peaks having less than this param data points will be removed from the final result.
        ///// </param>
        ///// <param name="minimumAmplitudeCriteria">
        ///// User-defined: the peaks having less than this param from the baseline will be removed from the final result.
        ///// </param>
        ///// <param name="amplitudeNoiseFoldCriteria">
        ///// 2 (default: now users cannot set this parameter)
        ///// </param>
        ///// <param name="slopeNoiseFoldCriteria">
        ///// 2 (default: now users cannot set this parameter)
        ///// </param>
        ///// <param name="peaktopNoiseFoldCriteria">
        ///// 4 (default: now users cannot set this parameter)
        ///// </param>
        ///// <param name="peaklist">
        ///// List of double array (chromatogram). [0]scan number [1]retention time [2]m/z [3]intensity
        ///// </param>
        ///// <returns>
        ///// Collection of PeakDetectionResult.cs bean
        ///// </returns>
        public static ObservableCollection<PeakDetectionResult> GetDetectedPeakInformationCollectionFromDifferentialBasedPeakDetectionAlgorithm(
            double minimumDatapointCriteria, double minimumAmplitudeCriteria, double amplitudeNoiseFoldCriteria,
            double slopeNoiseFoldCriteria, double peaktopNoiseFoldCriteria, List<double[]> peaklist) {
            var peakDetectionResults = new List<PeakDetectionResult>();

            //Differential calculation
            #region
            var firstDiffPeaklist = new List<double>();
            var secondDiffPeaklist = new List<double>();
            var firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
            var secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
            double firstDiff, secondDiff, maxFirstDiff = double.MinValue, maxSecondDiff = double.MinValue, maxAmplitudeDiff = double.MinValue;
            int halfDatapoint = (int)(firstDiffCoeff.Length / 2), peakID = 0;

            var noiseEstimateWidth = 50; // test
            var amplitudeDiffs = new List<double>();
            var counter = noiseEstimateWidth;
            var ampMax = double.MinValue;
            var ampMin = double.MaxValue;

            for (int i = 0; i < peaklist.Count; i++) {
                if (i < halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }
                if (i >= peaklist.Count - halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }

                firstDiff = secondDiff = 0;
                for (int j = 0; j < firstDiffCoeff.Length; j++) {
                    firstDiff += firstDiffCoeff[j] * peaklist[i + j - halfDatapoint][3];
                    secondDiff += secondDiffCoeff[j] * peaklist[i + j - halfDatapoint][3];
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(peaklist[i][3] - peaklist[i - 1][3]) > maxAmplitudeDiff) maxAmplitudeDiff = Math.Abs(peaklist[i][3] - peaklist[i - 1][3]);

                if (counter < i) {
                    if (ampMax > ampMin) {
                        amplitudeDiffs.Add(ampMax - ampMin);
                    }

                    counter += noiseEstimateWidth;
                    ampMax = double.MinValue;
                    ampMin = double.MaxValue;
                }
                else {
                    if (ampMax < peaklist[i][3]) ampMax = peaklist[i][3];
                    if (ampMin > peaklist[i][3]) ampMin = peaklist[i][3];
                }
            }
            #endregion

            //Noise estimate
            #region
            List<double> amplitudeNoiseCandidate = new List<double>();
            List<double> slopeNoiseCandidate = new List<double>();
            List<double> peaktopNoiseCandidate = new List<double>();
            double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
            double amplitudeNoise, slopeNoise, peaktopNoise;
            for (int i = 2; i < peaklist.Count - 2; i++) {
                if (Math.Abs(peaklist[i + 1][3] - peaklist[i][3]) < amplitudeNoiseThresh && Math.Abs(peaklist[i + 1][3] - peaklist[i][3]) > 0)
                    amplitudeNoiseCandidate.Add(Math.Abs(peaklist[i + 1][3] - peaklist[i][3]));
                if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0) slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
                if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh && Math.Abs(secondDiffPeaklist[i]) > 0) peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
            }
            if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
            if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
            if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());

            var ampNoise2 = 50.0; // ad hoc
            if (amplitudeDiffs.Count >= 10) {
                amplitudeDiffs = amplitudeDiffs.OrderByDescending(n => n).ToList();
                ampNoise2 = amplitudeDiffs[(int)(0.8 * amplitudeDiffs.Count)];
            }


            #endregion

            //Search peaks
            #region
            List<double[]> datapoints;
            double peakTopIntensity, peakHwhm, peakHalfDiff, peakFivePercentDiff, leftShapenessValue, rightShapenessValue
                , gaussianSigma, gaussianNormalize, gaussianArea, gaussinaSimilarityValue, gaussianSimilarityLeftValue, gaussianSimilarityRightValue
                , realAreaAboveZero, realAreaAboveBaseline, leftPeakArea, rightPeakArea, idealSlopeValue, nonIdealSlopeValue, symmetryValue, basePeakValue, peakPureValue;
            int peaktopCheckPoint, peakTopId = -1, peakHalfId = -1, leftPeakFivePercentId = -1, rightPeakFivePercentId = -1, leftPeakHalfId = -1, rightPeakHalfId = -1;

            var peaktopCheck = false;
            var infinitLoopCheck = false;
            var infinitLoopID = 0.0;
            var nextPeakCheck = false;
            var nextPeakCheckReminder = 0;

            for (int i = 0; i < peaklist.Count; i++) {
                if (i >= peaklist.Count - 1 - minimumDatapointCriteria) break;
                if (i >= peaklist.Count - 5) break;
                if (i > nextPeakCheckReminder + 2) nextPeakCheck = false;
                //1. Left edge criteria
                if (firstDiffPeaklist[i] > slopeNoise * slopeNoiseFoldCriteria && firstDiffPeaklist[i + 1] > slopeNoise * slopeNoiseFoldCriteria ||
                    (nextPeakCheck &&
                    peakDetectionResults[peakDetectionResults.Count - 1].IntensityAtRightPeakEdge < peaklist[i][3] &&
                    peaklist[i][3] < peaklist[i + 1][3] && peaklist[i + 1][3] < peaklist[i + 2][3])) {
                    datapoints = new List<double[]>();
                    datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3], firstDiffPeaklist[i], secondDiffPeaklist[i] });

                    //search real left edge within 5 data points
                    for (int j = 0; j <= 5; j++) {
                        if (i - j - 1 < 0) break;
                        if (peaklist[i - j][3] <= peaklist[i - j - 1][3]) break;
                        if (peaklist[i - j][3] > peaklist[i - j - 1][3]) datapoints.Insert(0, new double[] { peaklist[i - j - 1][0], peaklist[i - j - 1][1], peaklist[i - j - 1][2], peaklist[i - j - 1][3], firstDiffPeaklist[i - j - 1], secondDiffPeaklist[i - j - 1] });
                    }

                    //2. Right edge criteria
                    #region
                    peaktopCheck = false;
                    peaktopCheckPoint = i;
                    while (true) {
                        if (i + 1 == peaklist.Count - 1) break;

                        i++;
                        datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3], firstDiffPeaklist[i], secondDiffPeaklist[i] });
                        if (peaktopCheck == false &&
                            (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0) || (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i + 1] < 0) &&
                            secondDiffPeaklist[i] < -1 * peaktopNoise) {
                            peaktopCheck = true; peaktopCheckPoint = i;
                        }
                        if (peaktopCheck == true && peaktopCheckPoint + 3 <= i - 1) {
                            if (firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
                            if (Math.Abs(peaklist[i - 2][3] - peaklist[i - 1][3]) < amplitudeNoise &&
                                  Math.Abs(peaklist[i - 1][3] - peaklist[i][3]) < amplitudeNoise) break;
                        }
                    }

                    //Search real right edge within 5 data points
                    var rightCheck = false;
                    var trackcounter = 0;
                    //case: wrong edge is in right of real edge
                    if (rightCheck == false) {
                        for (int j = 0; j <= 5; j++) {
                            if (i - j - 1 < 0) break;
                            if (peaklist[i - j][3] <= peaklist[i - j - 1][3]) break;
                            if (peaklist[i - j][3] > peaklist[i - j - 1][3]) {
                                datapoints.RemoveAt(datapoints.Count - 1);
                                rightCheck = true;
                                trackcounter++;
                            }
                        }
                        if (trackcounter > 0) {
                            i -= trackcounter;
                            if (infinitLoopCheck == true && i == infinitLoopID && i > peaklist.Count - 10) break;
                            infinitLoopCheck = true; infinitLoopID = i;
                        }
                    }

                    //case: wrong edge is in left of real edge
                    if (rightCheck == false) {
                        for (int j = 0; j <= 5; j++) {
                            if (i + j + 1 > peaklist.Count - 1) break;
                            if (peaklist[i + j][3] <= peaklist[i + j + 1][3]) break;
                            if (peaklist[i + j][3] > peaklist[i + j + 1][3]) {
                                datapoints.Add(new double[] { peaklist[i + j + 1][0], peaklist[i + j + 1][1], peaklist[i + j + 1][2], peaklist[i + j + 1][3], firstDiffPeaklist[i + j + 1], secondDiffPeaklist[i + j + 1] });
                                rightCheck = true;
                                trackcounter++;
                            }
                        }
                        if (trackcounter > 0) i += trackcounter;
                    }

                    #endregion

                    //3. Check minimum datapoint criteria
                    #region
                    if (datapoints.Count < minimumDatapointCriteria) continue;
                    #endregion

                    //4. Check peak criteria
                    #region
                    peakTopIntensity = double.MinValue;
                    peakTopId = -1;
                    for (int j = 0; j < datapoints.Count; j++) {
                        if (peakTopIntensity < datapoints[j][3]) {
                            peakTopIntensity = datapoints[j][3];
                            peakTopId = j;
                        }
                    }

                    var minEdgeIntensity = Math.Min(datapoints[0][3], datapoints[datapoints.Count - 1][3]);
                    var peakheighFromBaseline = datapoints[peakTopId][3] - minEdgeIntensity;

                    if ((datapoints[peakTopId][3] - datapoints[0][3] < minimumAmplitudeCriteria && datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < minimumAmplitudeCriteria)
                        || (datapoints[peakTopId][3] - datapoints[0][3] < amplitudeNoise * amplitudeNoiseFoldCriteria
                        && datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < amplitudeNoise * amplitudeNoiseFoldCriteria)) continue;
                    if (peakheighFromBaseline < ampNoise2) continue;
                    #endregion

                    //5. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
                    #region
                    idealSlopeValue = 0;
                    nonIdealSlopeValue = 0;
                    peakHalfDiff = double.MaxValue;
                    peakFivePercentDiff = double.MaxValue;
                    leftShapenessValue = double.MinValue;
                    for (int j = peakTopId; j >= 0; j--) {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]))) {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]));
                            leftPeakHalfId = j;
                        }

                        if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3]))) {
                            peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3]));
                            leftPeakFivePercentId = j;
                        }

                        if (j == peakTopId) continue;

                        if (leftShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]))
                            leftShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]);

                        //if (datapoints[j + 1][3] - datapoints[j][3] >= 0)
                        //    idealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
                        //else
                        //    nonIdealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);

                        if (datapoints[j][4] > 0)
                            idealSlopeValue += Math.Abs(datapoints[j][4]);
                        else
                            nonIdealSlopeValue += Math.Abs(datapoints[j][4]);
                    }

                    peakHalfDiff = double.MaxValue;
                    peakFivePercentDiff = double.MaxValue;
                    rightShapenessValue = double.MinValue;
                    for (int j = peakTopId; j <= datapoints.Count - 1; j++) {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]))) {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                            rightPeakHalfId = j;
                        }

                        if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]))) {
                            peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                            rightPeakFivePercentId = j;
                        }

                        if (j == peakTopId) continue;

                        if (rightShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]))
                            rightShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]);

                        //if (datapoints[j - 1][3] - datapoints[j][3] >= 0)
                        //    idealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);
                        //else
                        //    nonIdealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);

                        if (datapoints[j][4] < 0)
                            idealSlopeValue += Math.Abs(datapoints[j][4]);
                        else
                            nonIdealSlopeValue += Math.Abs(datapoints[j][4]);
                    }

                    if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3]) {
                        gaussianNormalize = datapoints[peakTopId][3] - datapoints[0][3];
                        peakHalfId = leftPeakHalfId;
                        basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / (datapoints[peakTopId][3] - datapoints[0][3]));
                    }
                    else {
                        gaussianNormalize = datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3];
                        peakHalfId = rightPeakHalfId;
                        basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / (datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]));
                    }

                    if (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) <= Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]))
                        symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]);
                    else
                        symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]);

                    peakHwhm = Math.Abs(datapoints[peakHalfId][1] - datapoints[peakTopId][1]);
                    #endregion

                    //6. calculate peak pure value (from gaussian area and real area)
                    #region
                    gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
                    gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

                    realAreaAboveZero = 0;
                    leftPeakArea = 0;
                    rightPeakArea = 0;
                    for (int j = 0; j < datapoints.Count - 1; j++) {
                        realAreaAboveZero += (datapoints[j][3] + datapoints[j + 1][3]) * (datapoints[j + 1][1] - datapoints[j][1]) * 0.5;
                        if (j == peakTopId - 1)
                            leftPeakArea = realAreaAboveZero;
                        else if (j == datapoints.Count - 2)
                            rightPeakArea = realAreaAboveZero - leftPeakArea;
                    }
                    realAreaAboveBaseline = realAreaAboveZero - (datapoints[0][3] + datapoints[datapoints.Count - 1][3]) * (datapoints[datapoints.Count - 1][1] - datapoints[0][1]) / 2;

                    if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3]) {
                        leftPeakArea = leftPeakArea - datapoints[0][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                        rightPeakArea = rightPeakArea - datapoints[0][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
                    }
                    else {
                        leftPeakArea = leftPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                        rightPeakArea = rightPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
                    }

                    if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
                    else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

                    if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
                    else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

                    gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;
                    idealSlopeValue = (idealSlopeValue - nonIdealSlopeValue) / idealSlopeValue;

                    if (idealSlopeValue < 0) idealSlopeValue = 0;

                    peakPureValue = (basePeakValue + symmetryValue + gaussinaSimilarityValue) / 3;

                    if (peakPureValue > 1) peakPureValue = 1;
                    if (peakPureValue < 0) peakPureValue = 0;
                    #endregion

                    //7. Set peakInforamtion
                    #region
                    var result = new PeakDetectionResult() {
                        PeakID = peakID,
                        AmplitudeOrderValue = -1,
                        AmplitudeScoreValue = -1,
                        AreaAboveBaseline = (float)(realAreaAboveBaseline * 60),
                        AreaAboveZero = (float)(realAreaAboveZero * 60),
                        BasePeakValue = (float)basePeakValue,
                        GaussianSimilarityValue = (float)gaussinaSimilarityValue,
                        IdealSlopeValue = (float)idealSlopeValue,
                        IntensityAtLeftPeakEdge = (float)datapoints[0][3],
                        IntensityAtPeakTop = (float)datapoints[peakTopId][3],
                        IntensityAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][3],
                        PeakPureValue = (float)peakPureValue,
                        RtAtLeftPeakEdge = (float)datapoints[0][1],
                        RtAtPeakTop = (float)datapoints[peakTopId][1],
                        RtAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][1],
                        ScanNumAtLeftPeakEdge = (int)datapoints[0][0],
                        ScanNumAtPeakTop = (int)datapoints[peakTopId][0],
                        ScanNumAtRightPeakEdge = (int)datapoints[datapoints.Count - 1][0],
                        ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
                        SymmetryValue = (float)symmetryValue
                    };

                    peakDetectionResults.Add(result);
                    peakID++;
                    nextPeakCheck = true;
                    nextPeakCheckReminder = i;
                    #endregion
                }
            }
            #endregion

            if (peakDetectionResults.Count == 0) return null;

            peakDetectionResults = peakDetectionResults.OrderByDescending(n => n.IntensityAtPeakTop).ToList();
            float maxIntensity = peakDetectionResults[0].IntensityAtPeakTop;
            for (int i = 0; i < peakDetectionResults.Count; i++) {
                peakDetectionResults[i].AmplitudeScoreValue = peakDetectionResults[i].IntensityAtPeakTop / maxIntensity;
                peakDetectionResults[i].AmplitudeOrderValue = i + 1;
            }
            peakDetectionResults = peakDetectionResults.OrderBy(n => n.PeakID).ToList();

            return new ObservableCollection<PeakDetectionResult>(peakDetectionResults);
        }

        /// <summary>
        /// This is the same algorithm of the above method but just returns the results as List.
        /// </summary>
        /// <param name="minimumDatapointCriteria">
        /// User-defined: the peaks having less than this param data points will be removed from the final result.
        /// </param>
        /// <param name="minimumAmplitudeCriteria">
        /// User-defined: the peaks having less than this param from the baseline will be removed from the final result.
        /// </param>
        /// <param name="amplitudeNoiseFoldCriteria">
        /// 2 (default: now users cannot set this parameter)
        /// </param>
        /// <param name="slopeNoiseFoldCriteria">
        /// 2 (default: now users cannot set this parameter)
        /// </param>
        /// <param name="peaktopNoiseFoldCriteria">
        /// 4 (default: now users cannot set this parameter)
        /// </param>
        /// <param name="peaklist">
        /// List of double array (chromatogram). [0]scan number [1]retention time [2]m/z [3]intensity
        /// </param>
        /// <returns>
        /// List of PeakDetectionResult.cs bean
        /// </returns>
        public static List<PeakDetectionResult> GetDetectedPeakInformationListFromDifferentialBasedPeakDetectionAlgorithm(
            double minimumDatapointCriteria, double minimumAmplitudeCriteria, double amplitudeNoiseFoldCriteria, 
            double slopeNoiseFoldCriteria, double peaktopNoiseFoldCriteria, int averagePeakWidth, List<double[]> peaklist)
        {
            List<PeakDetectionResult> detectedPeakInformationList = new List<PeakDetectionResult>();
            PeakDetectionResult detectedPeakInformation;

            //Differential calculation
            #region
            List<double> firstDiffPeaklist = new List<double>();
            List<double> secondDiffPeaklist = new List<double>();
            double[] firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
            double[] secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };
            double firstDiff, secondDiff, maxFirstDiff = double.MinValue, maxSecondDiff = double.MinValue, maxAmplitudeDiff = double.MinValue;
            int halfDatapoint = (int)(firstDiffCoeff.Length / 2), peakID = 0;
            for (int i = 0; i < peaklist.Count; i++)
            {
                if (i < halfDatapoint)
                {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }
                if (i >= peaklist.Count - halfDatapoint)
                {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }

                firstDiff = secondDiff = 0;
                for (int j = 0; j < firstDiffCoeff.Length; j++)
                {
                    firstDiff += firstDiffCoeff[j] * peaklist[i + j - halfDatapoint][3];
                    secondDiff += secondDiffCoeff[j] * peaklist[i + j - halfDatapoint][3];
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(peaklist[i][3] - peaklist[i - 1][3]) > maxAmplitudeDiff) maxAmplitudeDiff = Math.Abs(peaklist[i][3] - peaklist[i - 1][3]);
            }
            #endregion

            //Noise estimate
            #region
            List<double> amplitudeNoiseCandidate = new List<double>();
            List<double> slopeNoiseCandidate = new List<double>();
            List<double> peaktopNoiseCandidate = new List<double>();
            double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
            double amplitudeNoise, slopeNoise, peaktopNoise;
            for (int i = 2; i < peaklist.Count - 2; i++)
            {
                if (Math.Abs(peaklist[i + 1][3] - peaklist[i][3]) < amplitudeNoiseThresh && Math.Abs(peaklist[i + 1][3] - peaklist[i][3]) > 0) amplitudeNoiseCandidate.Add(Math.Abs(peaklist[i + 1][3] - peaklist[i][3]));
                if (Math.Abs(firstDiffPeaklist[i]) < slopeNoiseThresh && Math.Abs(firstDiffPeaklist[i]) > 0) slopeNoiseCandidate.Add(Math.Abs(firstDiffPeaklist[i]));
                if (secondDiffPeaklist[i] < 0 && Math.Abs(secondDiffPeaklist[i]) < peaktopNoiseThresh && Math.Abs(secondDiffPeaklist[i]) > 0) peaktopNoiseCandidate.Add(Math.Abs(secondDiffPeaklist[i]));
            }
            if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
            if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
            if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
            #endregion

            //Search peaks
            #region
            List<double[]> datapoints;
            double peakTopIntensity, peakHwhm, peakHalfDiff, peakFivePercentDiff, leftShapenessValue, rightShapenessValue
                , gaussianSigma, gaussianNormalize, gaussianArea, gaussinaSimilarityValue, gaussianSimilarityLeftValue, gaussianSimilarityRightValue
                , realAreaAboveZero, realAreaAboveBaseline, leftPeakArea, rightPeakArea, idealSlopeValue, nonIdealSlopeValue, symmetryValue, basePeakValue, peakPureValue;
            int peaktopCheckPoint, peakTopId = -1, peakHalfId = -1, leftPeakFivePercentId = -1, rightPeakFivePercentId = -1, leftPeakHalfId = -1, rightPeakHalfId = -1;
            int excludedLeftCutPoint = 0, excludedRightCutPoint = 0;
            bool peaktopCheck = false;
            for (int i = 0; i < peaklist.Count; i++)
            {
                if (i >= peaklist.Count - 1 - minimumDatapointCriteria) break;

                //1. Left edge criteria
                if (firstDiffPeaklist[i] > slopeNoise * slopeNoiseFoldCriteria && firstDiffPeaklist[i + 1] > slopeNoise * slopeNoiseFoldCriteria)
                {
                    datapoints = new List<double[]>();
                    datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3], firstDiffPeaklist[i], secondDiffPeaklist[i] });

                    //search real left edge within 5 data points
                    for (int j = 0; j <= 5; j++)
                    {
                        if (i - j - 1 < 0) break;
                        if (peaklist[i - j][3] <= peaklist[i - j - 1][3]) break;
                        if (peaklist[i - j][3] > peaklist[i - j - 1][3]) datapoints.Insert(0, new double[] { peaklist[i - j - 1][0], peaklist[i - j - 1][1], peaklist[i - j - 1][2], peaklist[i - j - 1][3], firstDiffPeaklist[i - j - 1], secondDiffPeaklist[i - j - 1] });
                    }

                    //2. Right edge criteria
                    #region
                    peaktopCheck = false;
                    peaktopCheckPoint = i;
                    while (true)
                    {
                        if (i + 1 == peaklist.Count - 1) break;

                        i++;
                        datapoints.Add(new double[] { peaklist[i][0], peaklist[i][1], peaklist[i][2], peaklist[i][3], firstDiffPeaklist[i], secondDiffPeaklist[i] });
                        if (peaktopCheck == false && firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0 && secondDiffPeaklist[i] < -1 * peaktopNoise * peaktopNoiseFoldCriteria) { peaktopCheck = true; peaktopCheckPoint = i; }
                        if (peaktopCheck == true && peaktopCheckPoint + 2 + (int)(minimumDatapointCriteria / 2) <= i - 1 && firstDiffPeaklist[i - 1] > -1 * slopeNoise * slopeNoiseFoldCriteria && firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
                    }

                    //Search real right edge within 5 data points
                    //case: wrong edge is in left of real edge
                    for (int j = 0; j <= 5; j++)
                    {
                        if (i + j + 1 > peaklist.Count - 1) break;
                        if (peaklist[i + j][3] <= peaklist[i + j + 1][3]) break;
                        if (peaklist[i + j][3] > peaklist[i + j + 1][3])
                            datapoints.Add(new double[] { peaklist[i + j + 1][0], peaklist[i + j + 1][1], peaklist[i + j + 1][2], peaklist[i + j + 1][3], firstDiffPeaklist[i + j + 1], secondDiffPeaklist[i + j + 1] });
                    }
                    //case: wrong edge is in right of real edge
                    for (int j = 0; j <= 5; j++)
                    {
                        if (i - j - 1 < 0) break;
                        if (peaklist[i - j][3] <= peaklist[i - j - 1][3]) break;
                        if (peaklist[i - j][3] > peaklist[i - j - 1][3]) datapoints.RemoveAt(datapoints.Count - 1);
                    }
                    #endregion

                    //3. Check minimum datapoint criteria
                    #region
                    if (datapoints.Count < minimumDatapointCriteria) continue;
                    #endregion

                    //4. Check peak half height at half width
                    #region
                    peakTopIntensity = double.MinValue;
                    peakTopId = -1;
                    for (int j = 0; j < datapoints.Count; j++)
                    {
                        if (peakTopIntensity < datapoints[j][3])
                        {
                            peakTopIntensity = datapoints[j][3];
                            peakTopId = j;
                        }
                    }
                    if (datapoints[peakTopId][3] - datapoints[0][3] < minimumAmplitudeCriteria || 
                        datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < minimumAmplitudeCriteria || 
                        datapoints[peakTopId][3] - datapoints[0][3] < amplitudeNoise * amplitudeNoiseFoldCriteria || 
                        datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < amplitudeNoise * amplitudeNoiseFoldCriteria) continue;
                    if (peakTopId > averagePeakWidth)
                    {
                        excludedLeftCutPoint = 0;
                        for (int j = peakTopId - averagePeakWidth; j >= 0; j--)
                        {
                            if (j - 1 <= 0) break; 
                            if (datapoints[j][3] <= datapoints[j - 1][3]) { excludedLeftCutPoint = j; break; }
                        }
                        if (excludedLeftCutPoint > 0)
                        {
                            for (int j = 0; j < excludedLeftCutPoint; j++) datapoints.RemoveAt(0);
                            peakTopId = peakTopId - excludedLeftCutPoint;
                        }
                    }
                    if (datapoints.Count - 1 > peakTopId + averagePeakWidth)
                    {
                        excludedRightCutPoint = 0;
                        for (int j = peakTopId + averagePeakWidth; j < datapoints.Count; j++)
                        {
                            if (j + 1 > datapoints.Count - 1) break;
                            if (datapoints[j][3] <= datapoints[j + 1][3]) { excludedRightCutPoint = datapoints.Count - 1 - j; break; }
                        }
                        if (excludedRightCutPoint > 0)
                            for (int j = 0; j < excludedRightCutPoint; j++) datapoints.RemoveAt(datapoints.Count - 1);
                    }
                    #endregion

                    //5. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
                    #region
                    idealSlopeValue = 0;
                    nonIdealSlopeValue = 0;
                    peakHalfDiff = double.MaxValue;
                    peakFivePercentDiff = double.MaxValue;
                    leftShapenessValue = double.MinValue;
                    for (int j = peakTopId; j >= 0; j--)
                    {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3])))
                        {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]));
                            leftPeakHalfId = j;
                        }

                        if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3])))
                        {
                            peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3]));
                            leftPeakFivePercentId = j;
                        }

                        if (j == peakTopId) continue;

                        if (leftShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]))
                            leftShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]);

                        if (datapoints[j + 1][3] - datapoints[j][3] >= 0)
                            idealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
                        else
                            nonIdealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
                    }

                    peakHalfDiff = double.MaxValue;
                    peakFivePercentDiff = double.MaxValue;
                    rightShapenessValue = double.MinValue;
                    for (int j = peakTopId; j <= datapoints.Count - 1; j++)
                    {
                        if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3])))
                        {
                            peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                            rightPeakHalfId = j;
                        }

                        if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3])))
                        {
                            peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                            rightPeakFivePercentId = j;
                        }

                        if (j == peakTopId) continue;

                        if (rightShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]))
                            rightShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]);

                        if (datapoints[j - 1][3] - datapoints[j][3] >= 0)
                            idealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);
                        else
                            nonIdealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);
                    }

                    if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3])
                    {
                        gaussianNormalize = datapoints[peakTopId][3] - datapoints[0][3];
                        peakHalfId = leftPeakHalfId;
                        basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / (datapoints[peakTopId][3] - datapoints[0][3]));
                    }
                    else
                    {
                        gaussianNormalize = datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3];
                        peakHalfId = rightPeakHalfId;
                        basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / (datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]));
                    }

                    if (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) <= Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]))
                        symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]);
                    else
                        symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]);

                    peakHwhm = Math.Abs(datapoints[peakHalfId][1] - datapoints[peakTopId][1]);
                    #endregion

                    //6. calculate peak pure value (from gaussian area and real area)
                    #region
                    gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
                    gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

                    realAreaAboveZero = 0;
                    leftPeakArea = 0;
                    rightPeakArea = 0;
                    for (int j = 0; j < datapoints.Count - 1; j++)
                    {
                        realAreaAboveZero += (datapoints[j][3] + datapoints[j + 1][3]) * (datapoints[j + 1][1] - datapoints[j][1]) * 0.5;
                        if (j == peakTopId - 1)
                            leftPeakArea = realAreaAboveZero;
                        else if (j == datapoints.Count - 2)
                            rightPeakArea = realAreaAboveZero - leftPeakArea;
                    }
                    realAreaAboveBaseline = realAreaAboveZero - (datapoints[0][3] + datapoints[datapoints.Count - 1][3]) * (datapoints[datapoints.Count - 1][1] - datapoints[0][1]) / 2;

                    if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3])
                    {
                        leftPeakArea = leftPeakArea - datapoints[0][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                        rightPeakArea = rightPeakArea - datapoints[0][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
                    }
                    else
                    {
                        leftPeakArea = leftPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                        rightPeakArea = rightPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
                    }

                    if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
                    else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

                    if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
                    else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

                    gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;
                    idealSlopeValue = (idealSlopeValue - nonIdealSlopeValue) / idealSlopeValue;

                    if (idealSlopeValue < 0) idealSlopeValue = 0;

                    peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue + idealSlopeValue) / 4;
                    if (peakPureValue > 1) peakPureValue = 1;
                    if (peakPureValue < 0) peakPureValue = 0;
                    #endregion

                    //7. Set peakInforamtion
                    #region
                    detectedPeakInformation = new PeakDetectionResult()
                    {
                        PeakID = peakID,
                        AmplitudeOrderValue = -1,
                        AmplitudeScoreValue = -1,
                        AreaAboveBaseline = (float)(realAreaAboveBaseline * 60),
                        AreaAboveZero = (float)(realAreaAboveZero * 60),
                        BasePeakValue = (float)basePeakValue,
                        GaussianSimilarityValue = (float)gaussinaSimilarityValue,
                        IdealSlopeValue = (float)idealSlopeValue,
                        IntensityAtLeftPeakEdge = (float)datapoints[0][3],
                        IntensityAtPeakTop = (float)datapoints[peakTopId][3],
                        IntensityAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][3],
                        PeakPureValue = (float)peakPureValue,
                        RtAtLeftPeakEdge = (float)datapoints[0][1],
                        RtAtPeakTop = (float)datapoints[peakTopId][1],
                        RtAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][1],
                        ScanNumAtLeftPeakEdge = (int)datapoints[0][0],
                        ScanNumAtPeakTop = (int)datapoints[peakTopId][0],
                        ScanNumAtRightPeakEdge = (int)datapoints[datapoints.Count - 1][0],
                        ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
                        SymmetryValue = (float)symmetryValue,
                    };
                    detectedPeakInformationList.Add(detectedPeakInformation);
                    peakID++;
                    #endregion
                }
            }
            #endregion

            //finalize
            #region
            if (detectedPeakInformationList.Count == 0) return null;

            detectedPeakInformationList = detectedPeakInformationList.OrderByDescending(n => n.IntensityAtPeakTop).ToList();
            float maxIntensity = detectedPeakInformationList[0].IntensityAtPeakTop;
            for (int i = 0; i < detectedPeakInformationList.Count; i++)
            {
                detectedPeakInformationList[i].AmplitudeScoreValue = detectedPeakInformationList[i].IntensityAtPeakTop / maxIntensity;
                detectedPeakInformationList[i].AmplitudeOrderValue = i + 1;
            }
            detectedPeakInformationList = detectedPeakInformationList.OrderBy(n => n.PeakID).ToList();
            #endregion
            
            return detectedPeakInformationList;
        }

        /// <summary>
        /// This method is used in MRM-DIFF program.
        /// </summary>
        /// <param name="referenceDetectedPeakInformationBeanCollection"></param>
        /// <param name="peaklist"></param>
        /// <returns></returns>
        public static ObservableCollection<PeakDetectionResult> DataDependendPeakDetection(ObservableCollection<PeakDetectionResult> referenceDetectedPeakInformationBeanCollection, List<double[]> peaklist)
        {
            if (referenceDetectedPeakInformationBeanCollection == null || referenceDetectedPeakInformationBeanCollection.Count == 0) return null;

            List<PeakDetectionResult> sampleDetectedPeakInformationList = new List<PeakDetectionResult>();
            PeakDetectionResult sampleDetectedPeakInformation;
            List<double[]> datapoints;
            int peaktopID;
            double maxPeakIntensity;
            for (int i = 0; i < referenceDetectedPeakInformationBeanCollection.Count; i++)
            {
                sampleDetectedPeakInformation = new PeakDetectionResult();
                datapoints = new List<double[]>();
                peaktopID = 0;
                maxPeakIntensity = double.MinValue;
                for (int j = referenceDetectedPeakInformationBeanCollection[i].ScanNumAtLeftPeakEdge; j <= referenceDetectedPeakInformationBeanCollection[i].ScanNumAtRightPeakEdge; j++)
                {
                    datapoints.Add(peaklist[j]);
                    if (maxPeakIntensity < peaklist[j][3]) { maxPeakIntensity = peaklist[j][3]; peaktopID = j; }
                }
                peaktopID = peaktopID - referenceDetectedPeakInformationBeanCollection[i].ScanNumAtLeftPeakEdge;
                sampleDetectedPeakInformationList.Add(GetPeakDetectionResult(datapoints, peaktopID));
            }

            if (sampleDetectedPeakInformationList.Count == 0) return null;

            sampleDetectedPeakInformationList = sampleDetectedPeakInformationList.OrderByDescending(n => n.IntensityAtPeakTop).ToList();
            float maxIntensity = sampleDetectedPeakInformationList[0].IntensityAtPeakTop;
            for (int i = 0; i < sampleDetectedPeakInformationList.Count; i++)
            {
                sampleDetectedPeakInformationList[i].AmplitudeScoreValue = sampleDetectedPeakInformationList[i].IntensityAtPeakTop / maxIntensity;
                sampleDetectedPeakInformationList[i].AmplitudeOrderValue = i + 1;
            }
            sampleDetectedPeakInformationList = sampleDetectedPeakInformationList.OrderBy(n => n.RtAtPeakTop).ToList();
            for (int i = 0; i < sampleDetectedPeakInformationList.Count; i++) { sampleDetectedPeakInformationList[i].PeakID = i; }

            return new ObservableCollection<PeakDetectionResult>(sampleDetectedPeakInformationList);
        }

        /// <summary>
        /// This method is now used in this class only from the above methods.
        /// </summary>
        /// <param name="datapoints"></param>
        /// <param name="peakTopId"></param>
        /// <returns></returns>
        public static PeakDetectionResult GetPeakDetectionResult(List<double[]> datapoints, int peakTopId)
        {
            PeakDetectionResult detectedPeakInformation;
            double peakHwhm, peakHalfDiff, peakFivePercentDiff, leftShapenessValue, rightShapenessValue
                , gaussianSigma, gaussianNormalize, gaussianArea, gaussinaSimilarityValue, gaussianSimilarityLeftValue, gaussianSimilarityRightValue
                , realAreaAboveZero, realAreaAboveBaseline, leftPeakArea, rightPeakArea, idealSlopeValue, nonIdealSlopeValue, symmetryValue, basePeakValue, peakPureValue;
            int peakHalfId = -1, leftPeakFivePercentId = -1, rightPeakFivePercentId = -1, leftPeakHalfId = -1, rightPeakHalfId = -1;

            //1. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
            #region
            if (datapoints.Count <= 3) return null;
            if (datapoints[peakTopId][3] - datapoints[0][3] < 0 && datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3] < 0) return null;
            idealSlopeValue = 0;
            nonIdealSlopeValue = 0;
            peakHalfDiff = double.MaxValue;
            peakFivePercentDiff = double.MaxValue;
            leftShapenessValue = double.MinValue;

            for (int j = peakTopId; j >= 0; j--)
            {
                if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]))) {
                    peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 2 - (datapoints[j][3] - datapoints[0][3]));
                    leftPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3]))) {
                    peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / 5 - (datapoints[j][3] - datapoints[0][3]));
                    leftPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (leftShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]))
                    leftShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (peakTopId - j) / Math.Sqrt(datapoints[peakTopId][3]);

                if (datapoints[j + 1][3] - datapoints[j][3] >= 0)
                    idealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
                else
                    nonIdealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
            }
            peakHalfDiff = double.MaxValue;
            peakFivePercentDiff = double.MaxValue;
            rightShapenessValue = double.MinValue;
            for (int j = peakTopId; j <= datapoints.Count - 1; j++)
            {
                if (peakHalfDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]))) {
                    peakHalfDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 2 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                    rightPeakHalfId = j;
                }

                if (peakFivePercentDiff > Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]))) {
                    peakFivePercentDiff = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / 5 - (datapoints[j][3] - datapoints[datapoints.Count - 1][3]));
                    rightPeakFivePercentId = j;
                }

                if (j == peakTopId) continue;

                if (rightShapenessValue < (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]))
                    rightShapenessValue = (datapoints[peakTopId][3] - datapoints[j][3]) / (j - peakTopId) / Math.Sqrt(datapoints[peakTopId][3]);

                if (datapoints[j - 1][3] - datapoints[j][3] >= 0)
                    idealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);
                else
                    nonIdealSlopeValue += Math.Abs(datapoints[j - 1][3] - datapoints[j][3]);
            }


            if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3]) {
                gaussianNormalize = datapoints[peakTopId][3] - datapoints[0][3];
                peakHalfId = leftPeakHalfId;
                basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]) / (datapoints[peakTopId][3] - datapoints[0][3]));
            }
            else {
                gaussianNormalize = datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3];
                peakHalfId = rightPeakHalfId;
                basePeakValue = Math.Abs((datapoints[peakTopId][3] - datapoints[0][3]) / (datapoints[peakTopId][3] - datapoints[datapoints.Count - 1][3]));
            }

            if (Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) <= Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]))
                symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]);
            else
                symmetryValue = Math.Abs(datapoints[peakTopId][1] - datapoints[rightPeakFivePercentId][1]) / Math.Abs(datapoints[peakTopId][1] - datapoints[leftPeakFivePercentId][1]);

            peakHwhm = Math.Abs(datapoints[peakHalfId][1] - datapoints[peakTopId][1]);
            #endregion

            //2. Calculate peak pure value (from gaussian area and real area)
            #region
            gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
            gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

            realAreaAboveZero = 0;
            leftPeakArea = 0;
            rightPeakArea = 0;
            for (int j = 0; j < datapoints.Count - 1; j++)
            {
                realAreaAboveZero += (datapoints[j][3] + datapoints[j + 1][3]) * (datapoints[j + 1][1] - datapoints[j][1]) * 0.5;
                if (j == peakTopId - 1)
                    leftPeakArea = realAreaAboveZero;
                else if (j == datapoints.Count - 2)
                    rightPeakArea = realAreaAboveZero - leftPeakArea;
            }

            realAreaAboveBaseline = realAreaAboveZero - (datapoints[0][3] + datapoints[datapoints.Count - 1][3]) * (datapoints[datapoints.Count - 1][1] - datapoints[0][1]) / 2;

            if (datapoints[0][3] <= datapoints[datapoints.Count - 1][3]) {
                leftPeakArea = leftPeakArea - datapoints[0][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                rightPeakArea = rightPeakArea - datapoints[0][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
            }
            else {
                leftPeakArea = leftPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[peakTopId][1] - datapoints[0][1]);
                rightPeakArea = rightPeakArea - datapoints[datapoints.Count - 1][3] * (datapoints[datapoints.Count - 1][1] - datapoints[peakTopId][1]);
            }

            if (gaussianArea >= leftPeakArea) gaussianSimilarityLeftValue = leftPeakArea / gaussianArea;
            else gaussianSimilarityLeftValue = gaussianArea / leftPeakArea;

            if (gaussianArea >= rightPeakArea) gaussianSimilarityRightValue = rightPeakArea / gaussianArea;
            else gaussianSimilarityRightValue = gaussianArea / rightPeakArea;

            gaussinaSimilarityValue = (gaussianSimilarityLeftValue + gaussianSimilarityRightValue) / 2;
            idealSlopeValue = (idealSlopeValue - nonIdealSlopeValue) / idealSlopeValue;

            if (idealSlopeValue < 0) idealSlopeValue = 0;

            peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue + idealSlopeValue) / 4;
            if (peakPureValue > 1) peakPureValue = 1;
            if (peakPureValue < 0) peakPureValue = 0;
            #endregion

            //3. Set area information
            #region
            detectedPeakInformation = new PeakDetectionResult()
            {
                PeakID = -1,
                AmplitudeOrderValue = -1,
                AmplitudeScoreValue = -1,
                AreaAboveBaseline = (float)(realAreaAboveBaseline * 60),
                AreaAboveZero = (float)(realAreaAboveZero * 60),
                BasePeakValue = (float)basePeakValue,
                GaussianSimilarityValue = (float)gaussinaSimilarityValue,
                IdealSlopeValue = (float)idealSlopeValue,
                IntensityAtLeftPeakEdge = (float)datapoints[0][3],
                IntensityAtPeakTop = (float)datapoints[peakTopId][3],
                IntensityAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][3],
                PeakPureValue = (float)peakPureValue,
                RtAtLeftPeakEdge = (float)datapoints[0][1],
                RtAtPeakTop = (float)datapoints[peakTopId][1],
                RtAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][1],
                ScanNumAtLeftPeakEdge = (int)datapoints[0][0],
                ScanNumAtPeakTop = (int)datapoints[peakTopId][0],
                ScanNumAtRightPeakEdge = (int)datapoints[datapoints.Count - 1][0],
                ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
                SymmetryValue = (float)symmetryValue
            };
            #endregion
            return detectedPeakInformation;
        }
    }
}
