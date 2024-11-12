using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Components;
#if NETFRAMEWORK || NETSTANDARD
using CompMs.Common.Extension;
#endif
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.PeakPick;

public class PeakDetectionResult {
    public int PeakID { get; set; } = -1;
    public int ScanNumAtPeakTop { get; set; } = -1;
    public int ScanNumAtRightPeakEdge { get; set; } = -1;
    public int ScanNumAtLeftPeakEdge { get; set; } = -1;
    public float IntensityAtPeakTop { get; set; } = -1.0F;
    public float IntensityAtRightPeakEdge { get; set; } = -1.0F;
    public float IntensityAtLeftPeakEdge { get; set; } = -1.0F;
    public float ChromXAxisAtPeakTop { get; set; } = -1.0F;
    public float ChromXAxisAtRightPeakEdge { get; set; } = -1.0F;
    public float ChromXAxisAtLeftPeakEdge { get; set; } = -1.0F;
    public float AmplitudeOrderValue { get; set; } = -1.0F;
    public float AmplitudeScoreValue { get; set; } = -1.0F;
    public float SymmetryValue { get; set; } = -1.0F;
    public float BasePeakValue { get; set; } = -1.0F;
    public float IdealSlopeValue { get; set; } = -1.0F;
    public float GaussianSimilarityValue { get; set; } = -1.0F;
    public float ShapnessValue { get; set; } = -1.0F;
    public float PeakPureValue { get; set; } = -1.0F;
    public float AreaAboveBaseline { get; set; } = -1.0F;
    public float AreaAboveZero { get; set; } = -1.0F;
    public float EstimatedNoise { get; set; } = -1.0F;
    public float SignalToNoise { get; set; } = -1.0F;

    private static readonly double INTENSITY_FOLDCHANGE_THREASHOLD = .1d;
    public bool IsWeakCompareTo(double maxIntensityAtPeaks) {
        var edgeIntensity = (IntensityAtLeftPeakEdge + IntensityAtRightPeakEdge) * 0.5;
        var peakheightFromEdge = IntensityAtPeakTop - edgeIntensity;
        return IntensityAtPeakTop <= 0 || peakheightFromEdge < maxIntensityAtPeaks * INTENSITY_FOLDCHANGE_THREASHOLD;
    }

    public BaseChromatogramPeakFeature ConvertToPeakFeature(ExtractedIonChromatogram chromatogram, double mz) {
        return new BaseChromatogramPeakFeature
        {
            ChromScanIdLeft = ScanNumAtLeftPeakEdge,
            ChromScanIdTop = ScanNumAtPeakTop,
            ChromScanIdRight = ScanNumAtRightPeakEdge,
            ChromXsLeft = new ChromXs(new RetentionTime(chromatogram.Time(ScanNumAtLeftPeakEdge), ChromXUnit.Min)),
            ChromXsTop = new ChromXs(new RetentionTime(chromatogram.Time(ScanNumAtPeakTop), ChromXUnit.Min)),
            ChromXsRight = new ChromXs(new RetentionTime(chromatogram.Time(ScanNumAtRightPeakEdge), ChromXUnit.Min)),
            PeakHeightLeft = IntensityAtLeftPeakEdge,
            PeakHeightTop = IntensityAtPeakTop,
            PeakHeightRight = IntensityAtRightPeakEdge,
            PeakAreaAboveZero = AreaAboveZero,
            PeakAreaAboveBaseline = AreaAboveBaseline,
            Mass = mz,
        };
    }
}

public sealed class PeakDetection {

    private readonly double _minimumDatapointCriteria;
    private readonly double _minimumAmplitudeCriteria;

    public PeakDetection(double minimumDatapointCriteria, double minimumAmplitudeCriteria) {
        _minimumDatapointCriteria = minimumDatapointCriteria;
        _minimumAmplitudeCriteria = minimumAmplitudeCriteria;
    }

    public List<PeakDetectionResult> PeakDetectionVS1(Chromatogram chromatogram) {
        using var chroChroChromatogram = ChroChroChromatogram.CreateFromChromatogram(chromatogram, NoiseEstimateParameter.GlobalParameter);

        var averagePeakWidth = 20;
        var amplitudeNoiseFoldCriteria = 4d;
        var slopeNoiseFoldCriteria = 2d;
        return chroChroChromatogram.DetectPeaks(NoiseEstimateParameter.GlobalParameter.NoiseFactor, averagePeakWidth, amplitudeNoiseFoldCriteria, slopeNoiseFoldCriteria, _minimumDatapointCriteria, _minimumAmplitudeCriteria);
    }

    // below is a global peak detection method for gcms/lcms data preprocessing
    public static List<PeakDetectionResult> PeakDetectionVS1(IReadOnlyList<ChromatogramPeak> peaklist, double minimumDatapointCriteria, double minimumAmplitudeCriteria) {
        var results = new List<PeakDetectionResult>();
        #region
        // global parameter
        var noiseEstimateBin = NoiseEstimateParameter.GlobalParameter.NoiseEstimateBin;
        var minNoiseWindowSize = NoiseEstimateParameter.GlobalParameter.MinimumNoiseWindowSize;
        var minNoiseLevel = NoiseEstimateParameter.GlobalParameter.MinimumNoiseLevel;
        var noiseFactor = NoiseEstimateParameter.GlobalParameter.NoiseFactor;

        // 'chromatogram' properties
        var globalProperty = FindChromatogramGlobalProperties(peaklist, noiseEstimateBin, minNoiseWindowSize, minNoiseLevel, noiseFactor);
        var baselineMedian = globalProperty.BaselineMedian;
        var noise = globalProperty.Noise;
        var isHighBaseline = globalProperty.IsHighBaseline;
        var ssPeaklist = globalProperty.SmoothedPeakList;

        // differential factors
        generateDifferencialCoefficients(ssPeaklist, out List<double> firstDiffPeaklist, out List<double> secondDiffPeaklist, out double maxAmplitudeDiff, out double maxFirstDiff, out double maxSecondDiff);

        // slope noises
        calculateSlopeNoises(ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, maxAmplitudeDiff, maxFirstDiff, maxSecondDiff, out double amplitudeNoise, out double slopeNoise, out double peaktopNoise);

        var infinitLoopCheck = false;
        var infinitLoopID = 0;
        var margin = Math.Max((int)minimumDatapointCriteria, 5);

        var averagePeakWidth = 20.0;
        var amplitudeNoiseFoldCriteria = 4.0;
        var slopeNoiseFoldCriteria = 2.0;
        var peakCounter = 0;
        for (int i = margin; i < ssPeaklist.Count - margin; i++) {
            if (IsPeakStarted(i, firstDiffPeaklist, slopeNoise, slopeNoiseFoldCriteria)) {
                var datapoints = new List<double[]>();
                datapoints.Add(new double[] { i, peaklist[i].ChromXs.Value, peaklist[i].Mass, peaklist[i].Intensity, firstDiffPeaklist[i], secondDiffPeaklist[i] });
                searchRealLeftEdge(i, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist);
                var j = searchRightEdgeCandidate(i, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, slopeNoise, slopeNoiseFoldCriteria, amplitudeNoise, peaktopNoise, minimumDatapointCriteria);
                j = searchRealRightEdge(j, datapoints, peaklist, ssPeaklist, firstDiffPeaklist, secondDiffPeaklist, ref infinitLoopCheck, ref infinitLoopID, out var isBreak);
                i = Math.Max(i, j);
                if (isBreak) {
                    break;
                }
                if (datapoints.Count < minimumDatapointCriteria) {
                    continue;
                }
                curateDatapoints(datapoints, averagePeakWidth, out var peaktopID);

                peakHeightFromBaseline(datapoints, peaktopID, out var maxPeakHeight, out var minPeakHeight);
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
        if (results.Count != 0) {
            FinalizePeakDetectionResults(results);
        }
        return results;
    }

    #region methods

    private static void FinalizePeakDetectionResults(List<PeakDetectionResult> results) {
        var sResults = results.OrderByDescending(n => n.IntensityAtPeakTop).ToList();
        float maxIntensity = sResults[0].IntensityAtPeakTop;
        for (int i = 0; i < sResults.Count; i++) {
            sResults[i].AmplitudeScoreValue = sResults[i].IntensityAtPeakTop / maxIntensity;
            sResults[i].AmplitudeOrderValue = i + 1;
        }
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

    private static int searchRealRightEdge(int i, List<double[]> datapoints, IReadOnlyList<ChromatogramPeak> peaklist, List<ChromatogramPeak> ssPeaklist,
        List<double> firstDiffPeaklist, List<double> secondDiffPeaklist, ref bool infinitLoopCheck, ref int infinitLoopID, out bool isBreak) {
        //Search real right edge within 5 data points
        var rightCheck = false;
        var trackcounter = 0;
        isBreak = false;

        //case: wrong edge is in right of real edge
        if (rightCheck == false) {
            for (int j = 0; j <= 5; j++) {
                if (i - j - 1 < 0) break;
                if (ssPeaklist[i - j].Intensity <= ssPeaklist[i - j - 1].Intensity) break;
                if (ssPeaklist[i - j].Intensity > ssPeaklist[i - j - 1].Intensity) {
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
                if (ssPeaklist[i + j].Intensity <= ssPeaklist[i + j + 1].Intensity) break;
                if (ssPeaklist[i + j].Intensity > ssPeaklist[i + j + 1].Intensity) {
                    datapoints.Add(new double[] { i + j + 1, peaklist[i + j + 1].ChromXs.Value, peaklist[i + j + 1].Mass,
                                peaklist[i + j + 1].Intensity, firstDiffPeaklist[i + j + 1], secondDiffPeaklist[i + j + 1] });
                    rightCheck = true;
                    trackcounter++;
                }
            }
            if (trackcounter > 0) i += trackcounter;
        }
        return i;
    }

    private static int searchRightEdgeCandidate(int i, List<double[]> datapoints,
        IReadOnlyList<ChromatogramPeak> peaklist, List<ChromatogramPeak> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist,
        double slopeNoise, double slopeNoiseFoldCriteria, double amplitudeNoise, double peaktopNoise, double minimumDatapointCriteria) {
        var peaktopCheck = false;
        var peaktopCheckPoint = i;
        while (true) {
            if (i + 2 == ssPeaklist.Count - 1) break;

            i++;
            datapoints.Add(new double[] { i, peaklist[i].ChromXs.Value, peaklist[i].Mass, peaklist[i].Intensity,
                        firstDiffPeaklist[i], secondDiffPeaklist[i] });

            // peak top check
            if (peaktopCheck == false &&
                (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i] < 0) || (firstDiffPeaklist[i - 1] > 0 && firstDiffPeaklist[i + 1] < 0) &&
                secondDiffPeaklist[i] < -1 * peaktopNoise) {
                peaktopCheck = true; peaktopCheckPoint = i;
            }

            if (peaktopCheck == false &&
               (ssPeaklist[i - 2].Intensity <= ssPeaklist[i - 1].Intensity) &&
               (ssPeaklist[i - 1].Intensity <= ssPeaklist[i].Intensity) &&
               (ssPeaklist[i].Intensity >= ssPeaklist[i + 1].Intensity) &&
               (ssPeaklist[i + 1].Intensity >= ssPeaklist[i + 2].Intensity)) {
                peaktopCheck = true; peaktopCheckPoint = i;
            }

            // peak top check force
            if (peaktopCheck == false && minimumDatapointCriteria < 1.5 &&
                ((ssPeaklist[i - 2].Intensity <= ssPeaklist[i - 1].Intensity) &&
                (ssPeaklist[i - 1].Intensity <= ssPeaklist[i].Intensity) &&
                (ssPeaklist[i].Intensity >= ssPeaklist[i + 1].Intensity)) ||
                ((ssPeaklist[i - 1].Intensity <= ssPeaklist[i].Intensity) &&
                (ssPeaklist[i].Intensity >= ssPeaklist[i + 1].Intensity) &&
                (ssPeaklist[i + 1].Intensity >= ssPeaklist[i + 2].Intensity))) {
                peaktopCheck = true; peaktopCheckPoint = i;
            }


            var minimumPointFromTop = minimumDatapointCriteria <= 3 ? 1 : minimumDatapointCriteria * 0.5;
            if (peaktopCheck == true && peaktopCheckPoint + minimumPointFromTop <= i - 1) {
                if (firstDiffPeaklist[i] > -1 * slopeNoise * slopeNoiseFoldCriteria) break;
                if (Math.Abs(ssPeaklist[i - 2].Intensity - ssPeaklist[i - 1].Intensity) < amplitudeNoise &&
                      Math.Abs(ssPeaklist[i - 1].Intensity - ssPeaklist[i].Intensity) < amplitudeNoise) break;

                if ((ssPeaklist[i - 2].Intensity >= ssPeaklist[i - 1].Intensity) &&
                    (ssPeaklist[i - 1].Intensity >= ssPeaklist[i].Intensity) &&
                    (ssPeaklist[i].Intensity <= ssPeaklist[i + 1].Intensity) &&
                    (ssPeaklist[i + 1].Intensity <= ssPeaklist[i + 2].Intensity)) break;

                // peak right check force
                if (minimumDatapointCriteria < 1.5 &&
                    ((ssPeaklist[i - 2].Intensity >= ssPeaklist[i - 1].Intensity) &&
                    (ssPeaklist[i - 1].Intensity >= ssPeaklist[i].Intensity) &&
                    (ssPeaklist[i].Intensity <= ssPeaklist[i + 1].Intensity)) ||
                    ((ssPeaklist[i - 1].Intensity >= ssPeaklist[i].Intensity) &&
                    (ssPeaklist[i].Intensity <= ssPeaklist[i + 1].Intensity) &&
                    (ssPeaklist[i + 1].Intensity <= ssPeaklist[i + 2].Intensity))) {
                    peaktopCheck = true; peaktopCheckPoint = i;
                }
            }
        }
        return i;
    }

    private static void searchRealLeftEdge(int i, List<double[]> datapoints, 
        IReadOnlyList<ChromatogramPeak> peaklist, List<ChromatogramPeak> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist) {
        //search real left edge within 5 data points
        for (int j = 0; j <= 5; j++) {
            if (i - j - 1 < 0) break;
            if (ssPeaklist[i - j].Intensity <= ssPeaklist[i - j - 1].Intensity) break;
            if (ssPeaklist[i - j].Intensity > ssPeaklist[i - j - 1].Intensity)
                datapoints.Insert(0, new double[] { i - j - 1, peaklist[i - j - 1].ChromXs.Value,
                            peaklist[i - j - 1].Mass, peaklist[i - j - 1].Intensity, firstDiffPeaklist[i - j - 1], secondDiffPeaklist[i - j - 1] });
        }
    }

    private static bool IsPeakStarted(int index, List<double> firstDiffPeaklist, double slopeNoise, double slopeNoiseFoldCriteria)
    {
        return firstDiffPeaklist[index] > slopeNoise * slopeNoiseFoldCriteria &&
               firstDiffPeaklist[index + 1] > slopeNoise * slopeNoiseFoldCriteria;
    }

    private static void calculateSlopeNoises(List<ChromatogramPeak> ssPeaklist, List<double> firstDiffPeaklist, List<double> secondDiffPeaklist,
        double maxAmplitudeDiff, double maxFirstDiff, double maxSecondDiff, out double amplitudeNoise, out double slopeNoise, out double peaktopNoise) {

        var amplitudeNoiseCandidate = new List<double>();
        var slopeNoiseCandidate = new List<double>();
        var peaktopNoiseCandidate = new List<double>();
        double amplitudeNoiseThresh = maxAmplitudeDiff * 0.05, slopeNoiseThresh = maxFirstDiff * 0.05, peaktopNoiseThresh = maxSecondDiff * 0.05;
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
        if (amplitudeNoiseCandidate.Count == 0) amplitudeNoise = 0.0001; else amplitudeNoise = BasicMathematics.Median(amplitudeNoiseCandidate.ToArray());
        if (slopeNoiseCandidate.Count == 0) slopeNoise = 0.0001; else slopeNoise = BasicMathematics.Median(slopeNoiseCandidate.ToArray());
        if (peaktopNoiseCandidate.Count == 0) peaktopNoise = 0.0001; else peaktopNoise = BasicMathematics.Median(peaktopNoiseCandidate.ToArray());
    }
    private static void generateDifferencialCoefficients(List<ChromatogramPeak> ssPeaklist, out List<double> firstDiffPeaklist, out List<double> secondDiffPeaklist,
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
                firstDiff += firstDiffCoeff[j] * ssPeaklist[i + j - halfDatapoint].Intensity;
                secondDiff += secondDiffCoeff[j] * ssPeaklist[i + j - halfDatapoint].Intensity;
            }
            firstDiffPeaklist.Add(firstDiff);
            secondDiffPeaklist.Add(secondDiff);

            if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
            if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
            if (Math.Abs(ssPeaklist[i].Intensity - ssPeaklist[i - 1].Intensity) > maxAmplitudeDiff)
                maxAmplitudeDiff = Math.Abs(ssPeaklist[i].Intensity - ssPeaklist[i - 1].Intensity);
        }
    }

    private static ChromatogramGlobalProperty FindChromatogramGlobalProperties(
        IReadOnlyList<ChromatogramPeak> peaklist, 
        int noiseEstimateBin, 
        int minNoiseWindowSize,
        double minNoiseLevel, 
        double noiseFactor)
    {
        // checking chromatogram properties
        var baseIntensites = peaklist.Select(peak => peak.Intensity).ToList();
        var baselineMedian = BasicMathematics.Median(baseIntensites);
        var maxChromIntensity = peaklist.DefaultIfEmpty().Max(peak => peak?.Intensity) ?? double.MinValue;
        var minChromIntensity = peaklist.DefaultIfEmpty().Min(peak => peak?.Intensity) ?? double.MaxValue;
        var isHighBaseline = baselineMedian > (maxChromIntensity + minChromIntensity) * 0.5;

        var ssPeaklist = Smoothing.LinearWeightedMovingAverage(Smoothing.LinearWeightedMovingAverage(peaklist, 1), 1);
        var baseline = Smoothing.SimpleMovingAverage(Smoothing.SimpleMovingAverage(peaklist, 10), 10);
        var baselineCorrectedPeaklist = Enumerable.Range(0, peaklist.Count)
            .Select(i => new ChromatogramPeak(peaklist[i].ID, peaklist[i].Mass, Math.Max(0, ssPeaklist[i].Intensity - baseline[i].Intensity), peaklist[i].ChromXs))
            .ToList();

        var amplitudeDiffs = baselineCorrectedPeaklist
            .Chunk(noiseEstimateBin)
            .Where(bin => bin.Length >= 1)
            .Select(bin => bin.Max(peak => peak.Intensity) - bin.Min(peak => peak.Intensity))
            .Where(diff => diff > 0)
            .ToList();
        if (amplitudeDiffs.Count() >= minNoiseWindowSize) {
            minNoiseLevel = BasicMathematics.Median(amplitudeDiffs);
        }
        var noise = minNoiseLevel * noiseFactor;

        return new ChromatogramGlobalProperty(maxChromIntensity, minChromIntensity, baselineMedian, noise, isHighBaseline, ssPeaklist, baseline, baselineCorrectedPeaklist);
    }

    class ChromatogramGlobalProperty
    {
        public ChromatogramGlobalProperty(double maxIntensity, double minIntensity, double baselineMedian, double noise, bool isHighBaseline, List<ChromatogramPeak> smoothedPeakList, List<ChromatogramPeak> baseline, List<ChromatogramPeak> baselineCorrectedPeakList)
        {
            MaxIntensity = maxIntensity;
            MinIntensity = minIntensity;
            BaselineMedian = baselineMedian;
            Noise = noise;
            IsHighBaseline = isHighBaseline;
            SmoothedPeakList = smoothedPeakList;
            Baseline = baseline;
            BaselineCorrectedPeakList = baselineCorrectedPeakList;
        }

        public double MaxIntensity { get; }
        public double MinIntensity { get; }
        public double BaselineMedian { get; }
        public double Noise { get; }
        public bool IsHighBaseline { get; }
        public List<ChromatogramPeak> SmoothedPeakList { get; }
        public List<ChromatogramPeak> Baseline { get; }
        public List<ChromatogramPeak> BaselineCorrectedPeakList { get; }
    }

    public static PeakDetectionResult GetPeakDetectionResult(List<double[]> datapoints, int peakTopId) {
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

            if (datapoints[j + 1][3] - datapoints[j][3] >= 0)
                idealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
            else
                nonIdealSlopeValue += Math.Abs(datapoints[j + 1][3] - datapoints[j][3]);
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

        peakPureValue = (gaussinaSimilarityValue + 1.2 * basePeakValue + 0.8 * symmetryValue + idealSlopeValue) / 4;
        if (peakPureValue > 1) peakPureValue = 1;
        if (peakPureValue < 0) peakPureValue = 0;
        #endregion

        //3. Set area information
        #region
        detectedPeakInformation = new PeakDetectionResult() {
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
            ChromXAxisAtLeftPeakEdge = (float)datapoints[0][1],
            ChromXAxisAtPeakTop = (float)datapoints[peakTopId][1],
            ChromXAxisAtRightPeakEdge = (float)datapoints[datapoints.Count - 1][1],
            ScanNumAtLeftPeakEdge = (int)datapoints[0][0],
            ScanNumAtPeakTop = (int)datapoints[peakTopId][0],
            ScanNumAtRightPeakEdge = (int)datapoints[datapoints.Count - 1][0],
            ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
            SymmetryValue = (float)symmetryValue
        };
        #endregion
        return detectedPeakInformation;
    }

    #endregion method
}
