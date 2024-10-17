using CompMs.Common.Algorithm.PeakPick;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components;

internal sealed class ChroChroChromatogram : IDisposable {
    private readonly Chromatogram _chromatogram;
    private ChromatogramGlobalProperty_temp2 _globalProperty;
    private readonly DifferencialCoefficients _differencialCoefficients;
    private readonly ChromatogramNoises _noises;

    internal ChroChroChromatogram(Chromatogram chromatogram, ChromatogramGlobalProperty_temp2 globalProperty, DifferencialCoefficients differencialCoefficients, ChromatogramNoises noises) {
        _chromatogram = chromatogram;
        _globalProperty = globalProperty;
        _differencialCoefficients = differencialCoefficients;
        _noises = noises;
    }

    public bool IsPeakStarted(int index, double slopeNoiseFoldCriteria) {
        return _differencialCoefficients.FirstDiffPeaklist[index] > _noises.SlopeNoise * slopeNoiseFoldCriteria &&
            _differencialCoefficients.FirstDiffPeaklist[index + 1] > _noises.SlopeNoise * slopeNoiseFoldCriteria;
    }

    internal int SearchRightEdgeCandidate(int i, double minimumDatapointCriteria, double slopeNoiseFoldCriteria) {
        var j = i;
        var foundPeakTop = false;
        var peaktopCheckPoint = j;
        while (j + 2 != _globalProperty.SmoothedChromatogram.Length - 1) {
            j++;

            // peak top check
            if (!foundPeakTop && (_differencialCoefficients.FirstDiffPeaklist[j - 1] > 0 && _differencialCoefficients.FirstDiffPeaklist[j] < 0) || (_differencialCoefficients.FirstDiffPeaklist[j - 1] > 0 && _differencialCoefficients.FirstDiffPeaklist[j + 1] < 0) && _differencialCoefficients.SecondDiffPeaklist[j] < -1 * (double)_noises.PeakTopNoise) {
                foundPeakTop = true; peaktopCheckPoint = j;
            }

            if (!foundPeakTop && _globalProperty.SmoothedChromatogram.IsLargePeakTop(j)) {
                foundPeakTop = true; peaktopCheckPoint = j;
            }

            // peak top check force
            if (!foundPeakTop && minimumDatapointCriteria < 1.5 && _globalProperty.SmoothedChromatogram.IsBroadPeakTop(j)) {
                foundPeakTop = true; peaktopCheckPoint = j;
            }


            var minimumPointFromTop = minimumDatapointCriteria <= 3 ? 1 : minimumDatapointCriteria * 0.5;
            if (foundPeakTop && peaktopCheckPoint + minimumPointFromTop <= j - 1) {
                if (_differencialCoefficients.FirstDiffPeaklist[j] > -1 * _noises.SlopeNoise * slopeNoiseFoldCriteria) break;

                if (_globalProperty.SmoothedChromatogram.IsFlat(j - 1, _noises.AmplitudeNoise)) break;

                if (_globalProperty.SmoothedChromatogram.IsLargeBottom(j)) break;

                // peak right check force
                if (minimumDatapointCriteria < 1.5 && _globalProperty.SmoothedChromatogram.IsBroadBottom(j)) {
                    foundPeakTop = true; peaktopCheckPoint = j;
                }
            }
        }
        return j;
    }

    internal bool IsNoise(double maxPeakHeight, double minPeakHeight, double _minimumAmplitudeCriteria, double amplitudeNoiseFoldCriteria, int start, int end) {
        return maxPeakHeight < _globalProperty.Noise || minPeakHeight < _minimumAmplitudeCriteria || minPeakHeight < (double)_noises.AmplitudeNoise * amplitudeNoiseFoldCriteria || (_globalProperty.IsHighBaseline && _chromatogram.HasBoundaryBelowThreshold(start, end, _globalProperty.BaselineMedian));
    }

    internal PeakDetectionResult GetPeakDetectionResult(int peakTopId, int start, int end, double noiseFactor, double maxPeakHeight) {
        //1. Check HWHM criteria and calculate shapeness value, symmetry value, base peak value, ideal value, non ideal value
        if (end - start <= 3) return null;

        if (_chromatogram.PeakHeightFromBounds(start, end, peakTopId).MaxHeight < 0) return null;

        var idealSlopeValue = 0d;
        var nonIdealSlopeValue = 0d;
        var peakHalfDiff = double.MaxValue;
        var peakFivePercentDiff = double.MaxValue;
        var leftShapenessValue = double.MinValue;
        int leftPeakFivePercentId = -1;
        int leftPeakHalfId = -1;
        for (int j = peakTopId; j >= start; j--) {
            if (peakHalfDiff > Math.Abs((_chromatogram.IntensityDifference(peakTopId, start) / 2) - _chromatogram.IntensityDifference(j, start))) {
                peakHalfDiff = Math.Abs((_chromatogram.IntensityDifference(peakTopId, start) / 2) - _chromatogram.IntensityDifference(j, start));
                leftPeakHalfId = j;
            }

            if (peakFivePercentDiff > Math.Abs((_chromatogram.IntensityDifference(peakTopId, start) / 5) - _chromatogram.IntensityDifference(j, start))) {
                peakFivePercentDiff = Math.Abs((_chromatogram.IntensityDifference(peakTopId, start) / 5) - _chromatogram.IntensityDifference(j, start));
                leftPeakFivePercentId = j;
            }

            if (j == peakTopId) continue;

            if (leftShapenessValue < _chromatogram.IntensityDifference(peakTopId, j) / (peakTopId - j) / Math.Sqrt(_chromatogram.Intensity(peakTopId)))
                leftShapenessValue = _chromatogram.IntensityDifference(peakTopId, j) / (peakTopId - j) / Math.Sqrt(_chromatogram.Intensity(peakTopId));

            if (_chromatogram.IntensityDifference(j + 1, j) >= 0)
                idealSlopeValue += Math.Abs(_chromatogram.IntensityDifference(j + 1, j));
            else
                nonIdealSlopeValue += Math.Abs(_chromatogram.IntensityDifference(j + 1, j));
        }

        peakHalfDiff = double.MaxValue;
        peakFivePercentDiff = double.MaxValue;
        var rightShapenessValue = double.MinValue;
        int rightPeakHalfId = -1;
        int rightPeakFivePercentId = -1;
        for (int j = peakTopId; j < end; j++) {
            if (peakHalfDiff > Math.Abs((_chromatogram.IntensityDifference(peakTopId, end - 1)) / 2 - (_chromatogram.IntensityDifference(j, end - 1)))) {
                peakHalfDiff = Math.Abs((_chromatogram.IntensityDifference(peakTopId, end - 1)) / 2 - (_chromatogram.IntensityDifference(j, end - 1)));
                rightPeakHalfId = j;
            }

            if (peakFivePercentDiff > Math.Abs((_chromatogram.IntensityDifference(peakTopId, end - 1)) / 5 - (_chromatogram.IntensityDifference(j, end - 1)))) {
                peakFivePercentDiff = Math.Abs((_chromatogram.IntensityDifference(peakTopId, end - 1)) / 5 - (_chromatogram.IntensityDifference(j, end - 1)));
                rightPeakFivePercentId = j;
            }

            if (j == peakTopId) continue;

            if (rightShapenessValue < _chromatogram.IntensityDifference(peakTopId, j) / (j - peakTopId) / Math.Sqrt(_chromatogram.Intensity(peakTopId)))
                rightShapenessValue = _chromatogram.IntensityDifference(peakTopId, j) / (j - peakTopId) / Math.Sqrt(_chromatogram.Intensity(peakTopId));

            if (_chromatogram.IntensityDifference(j - 1, j) >= 0)
                idealSlopeValue += Math.Abs(_chromatogram.IntensityDifference(j - 1, j));
            else
                nonIdealSlopeValue += Math.Abs(_chromatogram.IntensityDifference(j - 1, j));
        }

        double gaussianNormalize;
        double basePeakValue;
        int peakHalfId;
        if (_chromatogram.IntensityDifference(start, end - 1) <= 0) {
            gaussianNormalize = _chromatogram.IntensityDifference(peakTopId, start);
            peakHalfId = leftPeakHalfId;
            basePeakValue = Math.Abs((_chromatogram.IntensityDifference(peakTopId, end - 1)) / (_chromatogram.IntensityDifference(peakTopId, start)));
        }
        else {
            gaussianNormalize = _chromatogram.IntensityDifference(peakTopId, end - 1);
            peakHalfId = rightPeakHalfId;
            basePeakValue = Math.Abs((_chromatogram.IntensityDifference(peakTopId, start)) / (_chromatogram.IntensityDifference(peakTopId, end - 1)));
        }

        double symmetryValue;
        if (Math.Abs(_chromatogram.TimeDifference(peakTopId, leftPeakFivePercentId)) <= Math.Abs(_chromatogram.TimeDifference(peakTopId, rightPeakFivePercentId)))
            symmetryValue = Math.Abs(_chromatogram.TimeDifference(peakTopId, leftPeakFivePercentId)) / Math.Abs(_chromatogram.TimeDifference(peakTopId, rightPeakFivePercentId));
        else
            symmetryValue = Math.Abs(_chromatogram.TimeDifference(peakTopId, rightPeakFivePercentId)) / Math.Abs(_chromatogram.TimeDifference(peakTopId, leftPeakFivePercentId));

        double peakHwhm = Math.Abs(_chromatogram.TimeDifference(peakHalfId, peakTopId));

        //2. Calculate peak pure value (from gaussian area and real area)
        double gaussianSigma = peakHwhm / Math.Sqrt(2 * Math.Log(2));
        double gaussianArea = gaussianNormalize * gaussianSigma * Math.Sqrt(2 * Math.PI) / 2;

        double realAreaAboveZero = 0;
        double leftPeakArea = 0;
        double rightPeakArea = 0;
        for (int j = start; j < end - 1; j++) {
            realAreaAboveZero += _chromatogram.CalculateArea(j + 1, j);
            if (j == peakTopId - 1)
                leftPeakArea = realAreaAboveZero;
            else if (j == end - 2)
                rightPeakArea = realAreaAboveZero - leftPeakArea;
        }


        double realAreaAboveBaseline = realAreaAboveZero - _chromatogram.CalculateArea(end - 1, start);

        if (_chromatogram.IntensityDifference(start, end - 1) <= 0) {
            leftPeakArea -= _chromatogram.Intensity(start) * (_chromatogram.TimeDifference(peakTopId, start));
            rightPeakArea -= _chromatogram.Intensity(start) * (_chromatogram.TimeDifference(end - 1, peakTopId));
        }
        else {
            leftPeakArea -= _chromatogram.Intensity(end - 1) * (_chromatogram.TimeDifference(peakTopId, start));
            rightPeakArea -= _chromatogram.Intensity(end - 1) * (_chromatogram.TimeDifference(end - 1, peakTopId));
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
        var estimatedNoise = Math.Max(1f, (float)(_globalProperty.Noise / noiseFactor));
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
            IntensityAtLeftPeakEdge = (float)_chromatogram.Intensity(start),
            IntensityAtPeakTop = (float)_chromatogram.Intensity(peakTopId),
            IntensityAtRightPeakEdge = (float)_chromatogram.Intensity(end - 1),
            PeakPureValue = (float)peakPureValue,
            ChromXAxisAtLeftPeakEdge = (float)_chromatogram.Time(start),
            ChromXAxisAtPeakTop = (float)_chromatogram.Time(peakTopId),
            ChromXAxisAtRightPeakEdge = (float)_chromatogram.Time(end - 1),
            ScanNumAtLeftPeakEdge = start,
            ScanNumAtPeakTop = peakTopId,
            ScanNumAtRightPeakEdge = end - 1,
            ShapnessValue = (float)((leftShapenessValue + rightShapenessValue) / 2),
            SymmetryValue = (float)symmetryValue,
            EstimatedNoise = estimatedNoise,
            SignalToNoise = (float)(maxPeakHeight / estimatedNoise),
        };
    }

    public List<PeakDetectionResult> DetectPeaks(double noiseFactor, int averagePeakWidth, double amplitudeNoiseFoldCriteria, double slopeNoiseFoldCriteria, double minimumDatapointCriteria, double minimumAmplitudeCriteria) {
        var results = new List<PeakDetectionResult>();
        var infinitLoopCheck = false;
        var infinitLoopID = 0;
        var margin = Math.Max((int)minimumDatapointCriteria, 2);
        for (int i = margin; i < _globalProperty.SmoothedChromatogram.Length - margin; i++) {
            if (IsPeakStarted(i, slopeNoiseFoldCriteria)) {
                var start = _globalProperty.SearchRealLeftEdge(i);
                var j = SearchRightEdgeCandidate(i, minimumDatapointCriteria, slopeNoiseFoldCriteria);
                j = _globalProperty.SearchRealRightEdge(j, ref infinitLoopCheck, ref infinitLoopID, out var isBreak);
                if (isBreak) {
                    break;
                }
                i = Math.Max(i, j);
                if (j - start + 1 < minimumDatapointCriteria) {
                    continue;
                }
                var (newStart, peakTopID, newEnd) = _chromatogram.ShrinkPeakRange(start, j + 1, averagePeakWidth);
                var (minPeakHeight, maxPeakHeight) = _chromatogram.PeakHeightFromBounds(newStart, newEnd, peakTopID);
                if (IsNoise(maxPeakHeight, minPeakHeight, minimumAmplitudeCriteria, amplitudeNoiseFoldCriteria, newStart, newEnd)) {
                    continue;
                }
                var result = GetPeakDetectionResult(peakTopID, newStart, newEnd, noiseFactor, maxPeakHeight);
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

    public void Dispose() {
        _globalProperty?.Dispose();
        _globalProperty = null;
    }

    internal static ChroChroChromatogram CreateFromChromatogram(Chromatogram chromatogram, NoiseEstimateParameter noiseParameter) {
        // 'chromatogram' properties
        var globalProperty = chromatogram.GetProperty(noiseParameter);

        // differential factors
        var differencialCoefficients = globalProperty.GenerateDifferencialCoefficients();

        // slope noises
        var noises = globalProperty.CalculateSlopeNoises(differencialCoefficients);

        return new ChroChroChromatogram(chromatogram, globalProperty, differencialCoefficients, noises);
    }
}

public sealed class ChromatogramGlobalProperty_temp2 : IDisposable
{
    private readonly static double[] FIRST_DIFF_COEFF = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
    private readonly static double[] SECOND_DIFF_COEFF = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };

    private Chromatogram _baselineChromatogram;
    private Chromatogram _baselineCorrectedChromatogram;

    internal ChromatogramGlobalProperty_temp2(double maxIntensity, double minIntensity, double baselineMedian, double noise, bool isHighBaseline,
        Chromatogram smoothedPeakList, Chromatogram baseline, Chromatogram baselineCorrectedPeakList) {
        MaxIntensity = maxIntensity;
        MinIntensity = minIntensity;
        BaselineMedian = baselineMedian;
        Noise = noise;
        IsHighBaseline = isHighBaseline;
        SmoothedChromatogram = smoothedPeakList;
        _baselineChromatogram = baseline;
        _baselineCorrectedChromatogram = baselineCorrectedPeakList;
    }

    public double MaxIntensity { get; }
    public double MinIntensity { get; }
    public double BaselineMedian { get; }
    public double Noise { get; }
    public bool IsHighBaseline { get; }
    internal Chromatogram SmoothedChromatogram { get; private set; }

    public DifferencialCoefficients GenerateDifferencialCoefficients() {

        var firstDiffPeaklist = new List<double>(SmoothedChromatogram.Length);
        var secondDiffPeaklist = new List<double>(SmoothedChromatogram.Length);

        var maxFirstDiff = double.MinValue;
        var maxSecondDiff = double.MinValue;
        var maxAmplitudeDiff = double.MinValue;

        double firstDiff, secondDiff;
        int halfDatapoint = FIRST_DIFF_COEFF.Length / 2;

        for (int i = 0; i < SmoothedChromatogram.Length; i++) {
            if (i < halfDatapoint) {
                firstDiffPeaklist.Add(0);
                secondDiffPeaklist.Add(0);
                continue;
            }
            if (i >= SmoothedChromatogram.Length - halfDatapoint) {
                firstDiffPeaklist.Add(0);
                secondDiffPeaklist.Add(0);
                continue;
            }

            firstDiff = secondDiff = 0;
            for (int j = 0; j < FIRST_DIFF_COEFF.Length; j++) {
                firstDiff += FIRST_DIFF_COEFF[j] * SmoothedChromatogram.Intensity(i + j - halfDatapoint);
                secondDiff += SECOND_DIFF_COEFF[j] * SmoothedChromatogram.Intensity(i + j - halfDatapoint);
            }
            firstDiffPeaklist.Add(firstDiff);
            secondDiffPeaklist.Add(secondDiff);

            if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
            if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
            if (Math.Abs(SmoothedChromatogram.IntensityDifference(i, i - 1)) > maxAmplitudeDiff)
                maxAmplitudeDiff = Math.Abs(SmoothedChromatogram.IntensityDifference(i, i - 1));
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
        for (int i = 2; i < SmoothedChromatogram.Length - 2; i++) {
            if (Math.Abs(SmoothedChromatogram.IntensityDifference(i + 1, i)) < amplitudeNoiseThresh &&
                Math.Abs(SmoothedChromatogram.IntensityDifference(i + 1, i)) > 0)
                amplitudeNoiseCandidate.Add(Math.Abs(SmoothedChromatogram.IntensityDifference(i + 1, i)));
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

    public int SearchRealLeftEdge(int i) {
        //search real left edge within 5 data points
        for (int j = 0; j <= 5; j++) {
            if (i - j - 1 < 0 || SmoothedChromatogram.IntensityDifference(i - j, i - j - 1) <= 0) {
                return i - j;
            }
        }
        return i - 6;
    }

    internal int SearchRealRightEdge(int i, ref bool infinitLoopCheck, ref int infinitLoopID, out bool isBreak) {
        //Search real right edge within 5 data points
        var isTooLongRightEdge = false;
        var trackcounter = 0;
        isBreak = false;

        //case: wrong edge is in right of real edge
        for (int j = 0; j <= 5; j++) {
            if (i - j - 1 < 0 || SmoothedChromatogram.IntensityDifference(i - j, i - j - 1) <= 0) {
                break;
            }
            isTooLongRightEdge = true;
            trackcounter++;
        }
        if (isTooLongRightEdge) {
            var k = i - trackcounter;
            if (infinitLoopCheck && k == infinitLoopID && k > SmoothedChromatogram.Length - 10) {
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
            if (i + j + 1 > SmoothedChromatogram.Length - 1) break;
            if (SmoothedChromatogram.IntensityDifference(i + j, i + j + 1) <= 0) {
                break;
            }

            trackcounter++;
        }
        return i + trackcounter;
    }

    public void Dispose() {
        _baselineChromatogram?.Dispose();
        _baselineCorrectedChromatogram?.Dispose();
        SmoothedChromatogram?.Dispose();

        _baselineChromatogram = null;
        _baselineCorrectedChromatogram = null;
        SmoothedChromatogram = null;
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
    internal ChromatogramNoises(double amplitudeNoise, double slopeNoise, double peakTopNoise) {
        AmplitudeNoise = amplitudeNoise;
        SlopeNoise = slopeNoise;
        PeakTopNoise = peakTopNoise;
    }

    public double AmplitudeNoise { get; }
    public double SlopeNoise { get; }
    public double PeakTopNoise { get; }
}
