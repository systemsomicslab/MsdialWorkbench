using CompMs.Common.Algorithm.ChromSmoothing;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Algorithm.PeakPick
{
    internal sealed class Chromatogram
    {
        private readonly IReadOnlyList<ChromatogramPeak> _peakList;
        private readonly IReadOnlyList<ChromatogramPeak> _smoothedPeakList;
        private readonly DifferencialCoefficients _differencialCoefficients;
        private readonly Noise _noise;

        public Chromatogram(IReadOnlyList<ChromatogramPeak> peakList) {
            _peakList = peakList;
            _smoothedPeakList = Smoothing.LinearWeightedMovingAverage(Smoothing.LinearWeightedMovingAverage(peakList, 1), 1);

            // differential factors
            _differencialCoefficients = DifferencialCoefficients.Calculate(_smoothedPeakList);

            // slope noises
            _noise = Noise.Calculate(_smoothedPeakList, _differencialCoefficients);
        }

        public DataPoint CreateDataPoint(int i) {
            var peak = _peakList[i];
            return new DataPoint(peak.ID, peak.ChromXs.Value, peak.Intensity);
        }

        public int SmoothedChromatogramLength => _smoothedPeakList.Count;

        public double SmoothedPeakIntensityDifference(int i, int j) {
            return Math.Abs(_smoothedPeakList[i].Intensity - _smoothedPeakList[j].Intensity);
        }

        public bool SmoothedPeakIntensityLessThanOrEqual(int i, int j) {
            return _smoothedPeakList[i].Intensity <= _smoothedPeakList[j].Intensity;
        }

        public int FindLeftEdge(int i) {
            int j = i;
            //search real left edge within 5 data points
            for (; j >= i - 5; j--) {
                if (j - 1 < 0 || _smoothedPeakList[j].Intensity <= _smoothedPeakList[j - 1].Intensity) {
                    break;
                }
            }
            return j;
        }

        public (bool isLooped, Peak) FindPeak(int start, double slopeNoiseFoldCriteria, double minimumDatapointCriteria, InfiniteLoopDetector loopDetector) {
            var left = FindLeftEdge(start);
            var peakTop = FindPeaktop(start, minimumDatapointCriteria);
            var rightCandidate = FindPeakRightCandidate(peakTop, minimumDatapointCriteria, slopeNoiseFoldCriteria);
            (var isLooped, var right) = FindRightEdge(rightCandidate, loopDetector);
            return (isLooped, new Peak(left, right));
        }

        public int FindPeaktop(int start, double minimumDatapointCriteria) {
            var i = start;
            while (i + 2 < _smoothedPeakList.Count - 1) {
                i++;

                // peak top check
                if (IsPeaktop(i, minimumDatapointCriteria)) {
                    break;
                }
            }
            return i;
        }

        public int FindPeakRightCandidate(int peaktop, double minimumDatapointCriteria, double slopeNoiseFoldCriteria) {
            var minimumPointFromTop = minimumDatapointCriteria <= 3 ? 1 : minimumDatapointCriteria * 0.5;
            var i = peaktop;
            while (i + 2 < SmoothedChromatogramLength - 1) {
                i++;

                if (SearchPeakRight(i, slopeNoiseFoldCriteria, minimumDatapointCriteria, peaktop, minimumPointFromTop)) {
                    break;
                }
            }
            return i;
        }

        public bool IsPeaktop(int i, double minimumDatapointCriteria) {
            return _differencialCoefficients.IsPeaktop(i, _noise) || IsIntensityPeaktop(i) || (minimumDatapointCriteria < 1.5 && IsIntensityOneSideMissingPeaktop(i));
        }

        public bool IsIntensityPeaktop(int i) {
            if (i - 2 < 0 || i + 2 >= _smoothedPeakList.Count) {
                return false;
            }
            return _smoothedPeakList[i - 2].Intensity <= _smoothedPeakList[i - 1].Intensity
                && _smoothedPeakList[i - 1].Intensity <= _smoothedPeakList[i].Intensity
                && _smoothedPeakList[i].Intensity >= _smoothedPeakList[i + 1].Intensity
                && _smoothedPeakList[i + 1].Intensity >= _smoothedPeakList[i + 2].Intensity;
        }

        public bool IsIntensityOneSideMissingPeaktop(int i) {
            if (i - 1 < 0 || i + 1 >= _smoothedPeakList.Count) {
                return false;
            }
            return _smoothedPeakList[i - 1].Intensity <= _smoothedPeakList[i].Intensity
                && _smoothedPeakList[i].Intensity >= _smoothedPeakList[i + 1].Intensity
                && (i - 2 >= 0 && _smoothedPeakList[i - 2].Intensity <= _smoothedPeakList[i - 1].Intensity
                    || i + 2 < _smoothedPeakList.Count && _smoothedPeakList[i + 1].Intensity >= _smoothedPeakList[i + 2].Intensity);
        }

        public bool SearchPeakRight(int i, double slopeNoiseFoldCriteria, double minimumDatapointCriteria, int peaktopCheckPoint, double minimumPointFromTop) {
            if (peaktopCheckPoint + minimumPointFromTop > i - 1) {
                return false;
            }

            if (_noise.IsSlopeNoise(_differencialCoefficients.FirstDiffPeakList[i], -1 * slopeNoiseFoldCriteria)) {
                return true;
            }
            if (_noise.IsAmplitudeNoise(SmoothedPeakIntensityDifference(i - 2, i - 1), 1d)
                && _noise.IsAmplitudeNoise(SmoothedPeakIntensityDifference(i - 1, i), 1d)) {
                return true;
            }

            if (SmoothedPeakIntensityLessThanOrEqual(i - 1, i - 2)
                && SmoothedPeakIntensityLessThanOrEqual(i, i - 1)
                && SmoothedPeakIntensityLessThanOrEqual(i, i + 1)
                && SmoothedPeakIntensityLessThanOrEqual(i + 1, i + 2)) {
                return true;
            }

            // peak right check force
            if (minimumDatapointCriteria < 1.5
                && SmoothedPeakIntensityLessThanOrEqual(i, i - 1)
                && SmoothedPeakIntensityLessThanOrEqual(i, i + 1)
                && (SmoothedPeakIntensityLessThanOrEqual(i - 1, i - 2) || SmoothedPeakIntensityLessThanOrEqual(i + 1, i + 2))) {
                return true;
            }
            return false;
        }

        public (bool isLooped, int rightId) FindRightEdge(int right, InfiniteLoopDetector loopDetector) {
            //Search real right edge within 5 data points

            //case: wrong edge is in right of real edge
            var i = right;
            for (int k = right; k >= right - 5; k--) {
                if (k - 1 < 0) break;
                if (SmoothedPeakIntensityLessThanOrEqual(k, k - 1)) {
                    break;
                }
                i--;
            }
            if (right > i) {
                if (loopDetector.IsLooped(i) && i > SmoothedChromatogramLength - 10) {
                    return (true, i);
                }
                else {
                    loopDetector.Update(i);
                    return (false, i);
                }
            }

            //case: wrong edge is in left of real edge
            var j = right;
            for (int k = right; k <= right + 5; k++) {
                if (k + 1 >= SmoothedChromatogramLength) break;
                if (SmoothedPeakIntensityLessThanOrEqual(k, k + 1)) {
                    break;
                }
                j++;
            }
            return (false, j);
        }

        public (int, Peak) CuratePeakRange(Peak peak, double averagePeakWidth) {
            var peakTopChromatogramIndex = GetPeaktopIndex(peak);
            var curatedLeft = peak.LeftIndex;
            if (peak.LeftIndex < peakTopChromatogramIndex - averagePeakWidth) {
                int leftCandidate = Enumerable.Range(0, peakTopChromatogramIndex - (int)averagePeakWidth - peak.LeftIndex) // [0, peakTopChromatogramIndex - (int)averagePeakWidth - left)
                    .Select(i => peakTopChromatogramIndex - (int)averagePeakWidth - i) // (left, peakTopChromatogramIndex - (int)averagePeakWidth] 
                    .FirstOrDefault(i => IsLeftEdge(i));
                if (leftCandidate > 0) {
                    curatedLeft = leftCandidate;
                }
            }
            var curatedRight = peak.RightIndex;
            if (peakTopChromatogramIndex + averagePeakWidth < peak.RightIndex) {
                int rightCandidate = Enumerable.Range(peakTopChromatogramIndex + (int)averagePeakWidth, peak.RightIndex - peakTopChromatogramIndex - (int)averagePeakWidth) // [peakTopChromatogramIndex + (int)averagePeakWidth, right)
                    .FirstOrDefault(j => IsRightEdge(j));
                if (rightCandidate > 0) {
                    curatedRight = rightCandidate;
                }
            }

            return (peakTopChromatogramIndex, new Peak(curatedLeft, curatedRight));
        }

        public int GetPeaktopIndex(Peak peak) {
            var peakTopIndex = -1;
            var peakTopIntensity = double.MinValue;
            for (int j = peak.LeftIndex; j <= peak.RightIndex; j++) {
                if (peakTopIntensity < _peakList[j].Intensity) {
                    peakTopIntensity = _peakList[j].Intensity;
                    peakTopIndex = j;
                }
            }
            return peakTopIndex;
        }

        public bool IsLeftEdge(int i) {
            return _peakList[i - 1].Intensity >= _peakList[i].Intensity;
        }

        public bool IsRightEdge(int i) {
            return _peakList[i].Intensity <= _peakList[i + 1].Intensity;
        }

        public (double MaxPeakHeight, double MinPeakHeight) DifferenceInPeakHeightFromEachEnd(Peak peak, int peaktop) {
            var peaktopInt = _peakList[peaktop].Intensity;
            var peakleftInt = _peakList[peak.LeftIndex].Intensity;
            var peakrightInt = _peakList[peak.RightIndex].Intensity;

            if (peakleftInt > peakrightInt) {
                return (peaktopInt - peakrightInt, peaktopInt - peakleftInt);
            }
            else {
                return (peaktopInt - peakleftInt, peaktopInt - peakrightInt);
            }
        }

        public double IntensityOfLowerEnd(Peak peak) {
            return Math.Min(_peakList[peak.LeftIndex].Intensity, _peakList[peak.RightIndex].Intensity);
        }

        public bool IsPeakStarted(int index, double slopeNoiseFoldCriteria) {
            return _differencialCoefficients.IsPeakStarted(index, _noise, slopeNoiseFoldCriteria);
        }

        public bool IsAmplitudeNoise(double intensity, double foldCriteria) {
            return _noise.IsAmplitudeNoise(intensity, foldCriteria);
        }

        public ChromatogramBaseline FindChromatogramBaseline(int noiseEstimateBin, int minimumNoiseBinCount, double minimumNoiseLevel, double noiseFactor)
        {
            // checking chromatogram properties
            var chromIntensityMedian = BasicMathematics.Median(_peakList.Select(peak => peak.Intensity).ToArray());
            var chromIntensityMax = _peakList.DefaultIfEmpty().Max(peak => peak?.Intensity) ?? double.MinValue;
            var chromIntensityMin = _peakList.DefaultIfEmpty().Min(peak => peak?.Intensity) ?? double.MaxValue;
            var isHighBaseline = chromIntensityMedian > (chromIntensityMax + chromIntensityMin) * 0.5;

            var baseline = Smoothing.SimpleMovingAverage(Smoothing.SimpleMovingAverage(_peakList, 10), 10);
            var baselineCorrectedPeaklist = Enumerable.Range(0, _peakList.Count)
                .Select(i => new ChromatogramPeak {
                    ID = _peakList[i].ID,
                    ChromXs = _peakList[i].ChromXs,
                    Mass = _peakList[i].Mass,
                    Intensity = Math.Max(0, _smoothedPeakList[i].Intensity - baseline[i].Intensity) });
            var amplitudeDiffs = baselineCorrectedPeaklist
                .Chunk(noiseEstimateBin)
                .Where(bin => bin.Count >= 1)
                .Select(bin => bin.Max(peak => peak.Intensity) - bin.Min(peak => peak.Intensity))
                .Where(diff => diff > 0)
                .ToList();
            double noiseLevel = minimumNoiseLevel;
            if (amplitudeDiffs.Count >= minimumNoiseBinCount) {
                noiseLevel = BasicMathematics.Median(amplitudeDiffs);
            }
            var noise = noiseLevel * noiseFactor;

            return new ChromatogramBaseline(chromIntensityMedian, isHighBaseline, noise, noiseFactor);
        }
    }
}
