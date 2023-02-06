using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.Common.Components
{
    public sealed class Chromatogram
    {
        private readonly IReadOnlyList<IChromatogramPeak> _peaks;
        private readonly ChromXType _type;
        private readonly ChromXUnit _unit;

        public Chromatogram(IReadOnlyList<IChromatogramPeak> peaks, ChromXType type, ChromXUnit unit) {
            _peaks = peaks ?? throw new System.ArgumentNullException(nameof(peaks));
            _type = type;
            _unit = unit;
        }

        public IReadOnlyList<IChromatogramPeak> Peaks => _peaks;

        public ChromXs PeakChromXs(double chromValue, double mz) {
            var result = new ChromXs(chromValue, _type, _unit);
            if (_type != ChromXType.Mz) {
                result.Mz = new MzValue(mz);
            }
            return result;
        }

        public bool IsEmpty => _peaks.Count == 0;

        public List<ChromatogramPeak> Smoothing(SmoothingMethod method, int level) {
            switch (method) {
                case SmoothingMethod.SimpleMovingAverage:
                    return Algorithm.ChromSmoothing.Smoothing.SimpleMovingAverage(_peaks, level);
                case SmoothingMethod.SavitzkyGolayFilter:
                    return Algorithm.ChromSmoothing.Smoothing.SavitxkyGolayFilter(_peaks, level);
                case SmoothingMethod.BinomialFilter:
                    return Algorithm.ChromSmoothing.Smoothing.BinomialFilter(_peaks, level);
                case SmoothingMethod.LowessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LowessFilter(_peaks, level);
                case SmoothingMethod.LoessFilter:
                    return Algorithm.ChromSmoothing.Smoothing.LoessFilter(_peaks, level);
                case SmoothingMethod.LinearWeightedMovingAverage:
                default:
                    return Algorithm.ChromSmoothing.Smoothing.LinearWeightedMovingAverage(_peaks, level);
            }
        }

        public Chromatogram SmoothedChromatogram(SmoothingMethod method, int level) {
            return new Chromatogram(Smoothing(method, level), _type, _unit);
        }

        public PeakOfChromatogram AsPeak(int top, int left, int right) {
            return new PeakOfChromatogram(_peaks, _type, top, left, right);
        }

        public PeakOfChromatogram FindPeak(int minPoints, double width, IChromatogramPeakFeature peak) {
            var maxId = SearchPeakTop(peak.ChromXsTop);
            var leftId = SearchLeftEdge(maxId, minPoints, width, peak.ChromXsLeft);
            var rightId = SearchRightEdge(maxId, minPoints, width, peak.ChromXsRight);
            var max = FindHighestIntensity(leftId, rightId + 1, maxId);
            return AsPeak(max, leftId, rightId);
        }

        private int SearchPeakTop(ChromXs top) {
            var center = SearchNearestPoint(top, _peaks);
            var maxID = center;
            var maxInt = double.MinValue;
            //finding local maximum within -2 ~ +2
            for (int i = center - 2; i <= center + 2; i++) {
                if (i <= 0) {
                    continue;
                }
                if (i + 1 >= _peaks.Count) {
                    break;
                }

                if (_peaks[i].Intensity > maxInt && _peaks[i - 1].Intensity <= _peaks[i].Intensity && _peaks[i].Intensity <= _peaks[i + 1].Intensity) {
                    maxInt = _peaks[i].Intensity;
                    maxID = i;
                }
            }
            return maxID;
        }

        private int SearchLeftEdge(int top, int minPoints, double width, ChromXs left) {
            //finding left edge;
            int? minLeftId = null;
            var minLeftInt = _peaks[top].Intensity;
            var leftEdge = _peaks[top].ChromXs.GetChromByType(_type).Value - width;
            for (int i = top - minPoints; i >= 0; i--) {
                if (minLeftInt < _peaks[i].Intensity || leftEdge > _peaks[i].ChromXs.GetChromByType(_type).Value) {
                    break;
                }

                minLeftInt = _peaks[i].Intensity;
                minLeftId = i;
            }
            if (minLeftId.HasValue) {
                return minLeftId.Value;
            }

            return SearchNearestPoint(left, _peaks.Take(top + 1));
        }

        private int SearchRightEdge(int top, int minPoints, double width, ChromXs right) {
            //finding right edge;
            int? minRightId = null;
            var minRightInt = _peaks[top].Intensity;
            double rightEdge = _peaks[top].ChromXs.GetChromByType(_type).Value + width;
            for (int i = top + minPoints; i < _peaks.Count - 1; i++) {
                if (minRightInt < _peaks[i].Intensity || rightEdge < _peaks[i].ChromXs.GetChromByType(_type).Value) {
                    break;
                }
                minRightInt = _peaks[i].Intensity;
                minRightId = i;
            }
            if (minRightId.HasValue) {
                return minRightId.Value;
            }

            return top + SearchNearestPoint(right, _peaks.Skip(top));
        }

        private int SearchNearestPoint(ChromXs chrom, IEnumerable<IChromatogramPeak> peaklist) {
            var target = chrom.GetChromByType(_type).Value;
            return peaklist
                .Select(peak => Math.Abs(peak.ChromXs.GetChromByType(_type).Value - target))
                .Argmin();
        }


        private int FindHighestIntensity(int start, int end, int defaultId) {
            var realMaxInt = double.MinValue;
            var realMaxID = defaultId;
            for (int i = start; i < end; i++) {
                if (realMaxInt < _peaks[i].Intensity) {
                    realMaxInt = _peaks[i].Intensity;
                    realMaxID = i;
                }
            }
            return realMaxID;
        }
    }
}
