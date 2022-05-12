using CompMs.Common.Components;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.PeakPick
{

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
    }

    internal sealed class DifferencialCoefficients
    {
        private readonly double _maxAmplitudeDiff;
        private readonly double _maxFirstDiff;
        private readonly double _maxSecondDiff;

        private DifferencialCoefficients(List<double> firstDiffPeakList, List<double> secondDiffPeakList, double maxAmplitudeDiff, double maxFirstDiff, double maxSecondDiff) {
            FirstDiffPeakList = firstDiffPeakList;
            SecondDiffPeakList = secondDiffPeakList;
            _maxAmplitudeDiff = maxAmplitudeDiff;
            _maxFirstDiff = maxFirstDiff;
            _maxSecondDiff = maxSecondDiff;
        }

        public List<double> FirstDiffPeakList { get; }

        public List<double> SecondDiffPeakList { get; }

        public bool IsPeakStarted(int index, Noise noise, double slopeNoiseFoldCriteria)
        {
            return noise.IsSlopeNoise(FirstDiffPeakList[index], slopeNoiseFoldCriteria) && noise.IsSlopeNoise(FirstDiffPeakList[index + 1], slopeNoiseFoldCriteria);
        }

        public bool IsPeaktop(int i, Noise noise) {
            return (FirstDiffPeakList[i - 1] > 0 )
                && ((FirstDiffPeakList[i] < 0) || (FirstDiffPeakList[i + 1] < 0))
                && noise.IsNotPeaktopNoise(-1 * SecondDiffPeakList[i]);
        }

        public bool MaybeAmplitudeNoise(double peakDifference) {
            return 0 < Math.Abs(peakDifference) && Math.Abs(peakDifference) < (_maxAmplitudeDiff * 0.05);
        }

        public bool MaybeSlopeNoise(double firstDifference) {
            return 0 < Math.Abs(firstDifference) && Math.Abs(firstDifference) < (_maxFirstDiff * 0.05);
        }

        public bool MaybePeaktopNoise(double secondDifference) {
            return secondDifference < 0 && 0 < Math.Abs(secondDifference) && Math.Abs(secondDifference) < (_maxSecondDiff * 0.05);
        }

        private static readonly double[] firstDiffCoeff = new double[] { -0.2, -0.1, 0, 0.1, 0.2 };
        private static readonly double[] secondDiffCoeff = new double[] { 0.14285714, -0.07142857, -0.1428571, -0.07142857, 0.14285714 };

        public static DifferencialCoefficients Calculate(IReadOnlyList<ChromatogramPeak> _smoothedPeakList) {
            var firstDiffPeaklist = new List<double>();
            var secondDiffPeaklist = new List<double>();
            var maxAmplitudeDiff = double.MinValue;
            var maxFirstDiff = double.MinValue;
            var maxSecondDiff = double.MinValue;

            double firstDiff, secondDiff;
            int halfDatapoint = firstDiffCoeff.Length / 2;

            for (int i = 0; i < _smoothedPeakList.Count; i++) {
                if (i < halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }
                if (i >= _smoothedPeakList.Count - halfDatapoint) {
                    firstDiffPeaklist.Add(0);
                    secondDiffPeaklist.Add(0);
                    continue;
                }

                firstDiff = secondDiff = 0;
                for (int j = 0; j < firstDiffCoeff.Length; j++) {
                    firstDiff += firstDiffCoeff[j] * _smoothedPeakList[i + j - halfDatapoint].Intensity;
                    secondDiff += secondDiffCoeff[j] * _smoothedPeakList[i + j - halfDatapoint].Intensity;
                }
                firstDiffPeaklist.Add(firstDiff);
                secondDiffPeaklist.Add(secondDiff);

                if (Math.Abs(firstDiff) > maxFirstDiff) maxFirstDiff = Math.Abs(firstDiff);
                if (secondDiff < 0 && maxSecondDiff < -1 * secondDiff) maxSecondDiff = -1 * secondDiff;
                if (Math.Abs(_smoothedPeakList[i].Intensity - _smoothedPeakList[i - 1].Intensity) > maxAmplitudeDiff)
                    maxAmplitudeDiff = Math.Abs(_smoothedPeakList[i].Intensity - _smoothedPeakList[i - 1].Intensity);
            }

            return new DifferencialCoefficients(firstDiffPeaklist, secondDiffPeaklist, maxAmplitudeDiff, maxFirstDiff, maxSecondDiff);
        }
    }

    internal sealed class Noise
    {
        private readonly double _amplitudeNoise;
        private readonly double _slopeNoise;
        private readonly double _peaktopNoise;

        public Noise(double amplitudeNoise, double slopeNoise, double peaktopNoise) {
            _amplitudeNoise = amplitudeNoise;
            _slopeNoise = slopeNoise;
            _peaktopNoise = peaktopNoise;
        }

        public bool IsAmplitudeNoise(double intensity, double foldCriteria) {
            return intensity < _amplitudeNoise * foldCriteria;
        }

        public bool IsSlopeNoise(double intensity, double foldCriteria) {
            return intensity > _slopeNoise * foldCriteria;
        }

        public bool IsNotPeaktopNoise(double diffIntensity) {
            return diffIntensity > _peaktopNoise;
        }

        public static Noise Calculate(IReadOnlyList<ChromatogramPeak> smoothedPeakList, DifferencialCoefficients diffCoef) {
            var amplitudeNoiseCandidate = new List<double>();
            var slopeNoiseCandidate = new List<double>();
            var peaktopNoiseCandidate = new List<double>();
            for (int i = 2; i < smoothedPeakList.Count - 2; i++) {
                var amplitudeDiff = smoothedPeakList[i + 1].Intensity - smoothedPeakList[i].Intensity;
                if (diffCoef.MaybeAmplitudeNoise(amplitudeDiff)) {
                    amplitudeNoiseCandidate.Add(Math.Abs(amplitudeDiff));
                }
                var firstDiff = diffCoef.FirstDiffPeakList[i];
                if (diffCoef.MaybeSlopeNoise(firstDiff)) {
                    slopeNoiseCandidate.Add(Math.Abs(firstDiff));
                }
                var secondDiff = diffCoef.SecondDiffPeakList[i];
                if (diffCoef.MaybePeaktopNoise(secondDiff)) {
                    peaktopNoiseCandidate.Add(Math.Abs(secondDiff));
                }
            }
            var amplitudeNoise = amplitudeNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(amplitudeNoiseCandidate);
            var slopeNoise = slopeNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(slopeNoiseCandidate);
            var peaktopNoise = peaktopNoiseCandidate.Count == 0 ? 0.0001 : BasicMathematics.Median(peaktopNoiseCandidate);
            return new Noise(amplitudeNoise, slopeNoise, peaktopNoise);
        }
    }

    internal sealed class DataPoint
    {
        public DataPoint(int id, double chromValue, double intensity) {
            Id = id;
            ChromValue = chromValue;
            Intensity = intensity;
        }

        public int Id { get; }
        public double ChromValue { get; }
        public double Intensity { get; }

        public double ChromDifference(DataPoint another) {
            return ChromValue - another.ChromValue;
        }

        public bool IntensityLessThanOrEqual(DataPoint another) {
            return Intensity <= another.Intensity;
        }

        public double IntensityDifference(DataPoint another) {
            return Intensity - another.Intensity;
        }

        public double CalculateChromatogramArea(DataPoint another) {
            return (Intensity + another.Intensity) * ChromDifference(another) * 0.5;
        }
    }

    internal sealed class InfiniteLoopDetector
    {
        public InfiniteLoopDetector() {
            _checkStarted = false;
            _loopCandidateId = 0;
        }

        bool _checkStarted;
        int _loopCandidateId;

        public bool IsLooped(int id) {
            return _checkStarted && _loopCandidateId == id;
        }

        public void Update(int id) {
            _checkStarted = true;
            _loopCandidateId = id;
        }
    }

    internal sealed class Peak
    {
        public Peak(int leftIndex, int rightIndex) {
            if (leftIndex > rightIndex) {
                throw new ArgumentException($"leftIndex should be less or equal to rightIndex.\nleftIndex: {leftIndex}, rightIndex: {rightIndex}");
            }
            LeftIndex = leftIndex;
            RightIndex = rightIndex;
        }

        public int LeftIndex { get; }
        public int RightIndex { get; }
        public int Length => RightIndex - LeftIndex + 1;
    }

    public static class PeakDetection {
        // below is a global peak detection method for gcms/lcms data preprocessing
        public static List<PeakDetectionResult> PeakDetectionVS1(IReadOnlyList<ChromatogramPeak> peaklist, double minimumDatapointCriteria, double minimumAmplitudeCriteria) {
            var chromatogram = new Chromatogram(peaklist);

            // 'chromatogram' properties
            var baseline = chromatogram.FindChromatogramBaseline(noiseEstimateBin: 50, minimumNoiseBinCount: 10, minimumNoiseLevel: 50d, noiseFactor: 3d);

            var loopDetector = new InfiniteLoopDetector();
            var margin = Math.Max((int)minimumDatapointCriteria, 5);

            var amplitudeNoiseFoldCriteria = 4.0;
            var slopeNoiseFoldCriteria = 2.0;

            var currentPeakId = 0;
            var results = new List<PeakDetectionResult>();
            for (int i = margin; i < chromatogram.SmoothedChromatogramLength - margin; i++) {
                if (!chromatogram.IsPeakStarted(i, slopeNoiseFoldCriteria)) {
                    continue;
                }
                var (isLooped, peak) = chromatogram.FindPeak(i, slopeNoiseFoldCriteria, minimumDatapointCriteria, loopDetector);
                if (isLooped) {
                    break;
                }
                
                i = peak.RightIndex;
                if (peak.Length < minimumDatapointCriteria) {
                    continue;
                }

                var (peaktopIndex, curatedPeak) = chromatogram.CuratePeakRange(peak, averagePeakWidth: 20d);
                var (maxPeakHeight, minPeakHeight) = chromatogram.DifferenceInPeakHeightFromEachEnd(curatedPeak, peaktopIndex);
                if (baseline.IsNoise(maxPeakHeight)
                    || minPeakHeight < minimumAmplitudeCriteria
                    || chromatogram.IsAmplitudeNoise(minPeakHeight, amplitudeNoiseFoldCriteria)
                    || baseline.IsNoiseIfHighBaseline(chromatogram.IntensityOfLowerEnd(curatedPeak))) {
                    continue;
                }

                var datapoints = new DataPointCollection(chromatogram, baseline);
                datapoints.AddPoints(curatedPeak);
                var result = datapoints.GetPeakDetectionResult(peaktopIndex - curatedPeak.LeftIndex, currentPeakId, maxPeakHeight);
                if (result is null) continue;
                currentPeakId++;
                results.Add(result);
            }
            if (results.Count != 0) {
                SetAmplitudeScore(results);
            }
            return results;
        }

        private static void SetAmplitudeScore(List<PeakDetectionResult> results) {
            var sResults = results.OrderByDescending(result => result.IntensityAtPeakTop).ToList();
            float maxIntensity = sResults[0].IntensityAtPeakTop;
            for (int i = 0; i < sResults.Count; i++) {
                sResults[i].AmplitudeScoreValue = sResults[i].IntensityAtPeakTop / maxIntensity;
                sResults[i].AmplitudeOrderValue = i + 1;
            }
        }
    }
}
