using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.Common.Mathematics.Basic;
using CompMs.MsdialCore.Algorithm;
using MessagePack;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.DataObj {
    class ScanNumberSummary {
        public ScanNumberSummary(int min, int max) {
            Min = min;
            Max = max;
        }

        public int Min { get; }
        public int Max { get; }

        internal void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinScanNumber = Min;
            dto.MaxScanNumber = Max;
        }
    }

    class RetentionTimeSummary {
        public RetentionTimeSummary(float min, float max) {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }

        internal void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinRetentionTime = Min;
            dto.MaxRetentionTime = Max;
        }
    }

    class MassSummary {
        public MassSummary(float min, float max) {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }

        internal void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinMass = Min;
            dto.MaxMass = Max;
        }
    }

    class IntensitySummary {
        public IntensitySummary(float min, float max) {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }

        internal void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinIntensity = Min;
            dto.MaxIntensity = Max;
        }
    }

    class DriftTimeSummary {
        public DriftTimeSummary(float min, float max) {
            Min = min;
            Max = max;
        }

        public float Min { get; }
        public float Max { get; }

        internal void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinDriftTime = Min;
            dto.MaxDriftTime = Max;
        }
    }

    class RawSpectraSummary {
        public RawSpectraSummary(
            ScanNumberSummary scanNumberSummary,
            RetentionTimeSummary retentionTimeSummary,
            MassSummary massSummary,
            IntensitySummary intensitySummary,
            DriftTimeSummary driftTimeSummary) {
            _scanNumberSummary = scanNumberSummary;
            _retentionTimeSummary = retentionTimeSummary;
            _massSummary = massSummary;
            _intensitySummary = intensitySummary;
            _driftTimeSummary = driftTimeSummary;
        }

        private readonly ScanNumberSummary _scanNumberSummary;
        private readonly RetentionTimeSummary _retentionTimeSummary;
        private readonly MassSummary _massSummary;
        private readonly IntensitySummary _intensitySummary;
        private readonly DriftTimeSummary _driftTimeSummary;

        public void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            _scanNumberSummary.SetToDto(dto);
            _retentionTimeSummary.SetToDto(dto);
            _massSummary.SetToDto(dto);
            _intensitySummary.SetToDto(dto);
            _driftTimeSummary.SetToDto(dto);
        }

        public static RawSpectraSummary Summarize(IDataProvider provider) {
            var (firstNo, lastNo) = provider.GetScanNumberRange();
            var (minRt, maxRt) = provider.GetRetentionTimeRange();
            var (minMz, maxMz) = provider.GetMassRange();
            var (minInt, maxInt) = provider.GetIntensityRange();
            var (minDt, maxDt) = provider.GetDriftTimeRange();
            return new RawSpectraSummary(
                new ScanNumberSummary(firstNo, lastNo),
                new RetentionTimeSummary((float)minRt, (float)maxRt),
                new MassSummary((float)minMz, (float)maxMz),
                new IntensitySummary((float)minInt, (float)maxInt),
                new DriftTimeSummary((float)minDt, (float)maxDt));
        }
    }

    class RtChromatogramPeakSummary {
        public RtChromatogramPeakSummary(
            float minPeakWidth,
            float averagePeakWidth,
            float medianPeakWidth,
            float maxPeakWidth,
            float stdevPeakWidth,
            float minPeakHeight,
            float averagePeakHeight,
            float medianPeakHeight,
            float maxPeakHeight,
            float stdevPeakHeight,
            float minPeakTop,
            float averageminPeakTop,
            float medianminPeakTop,
            float maxPeakTop,
            float stdevPeakTop) {

            _minPeakWidthOnRtAxis = minPeakWidth;
            _averagePeakWidthOnRtAxis = averagePeakWidth;
            _medianPeakWidthOnRtAxis = medianPeakWidth;
            _maxPeakWidthOnRtAxis = maxPeakWidth;
            _stdevPeakWidthOnRtAxis = stdevPeakWidth;
            _minPeakHeightOnRtAxis = minPeakHeight;
            _averagePeakHeightOnRtAxis = averagePeakHeight;
            _medianPeakHeightOnRtAxis = medianPeakHeight;
            _maxPeakHeightOnRtAxis = maxPeakHeight;
            _stdevPeakHeightOnRtAxis = stdevPeakHeight;
            _minPeakTopRT = minPeakTop;
            _averageminPeakTopRT = averageminPeakTop;
            _medianminPeakTopRT = medianminPeakTop;
            _maxPeakTopRT = maxPeakTop;
            _stdevPeakTopRT = stdevPeakTop;
        }

        private RtChromatogramPeakSummary() {

        }

        private readonly float _minPeakWidthOnRtAxis;
        private readonly float _averagePeakWidthOnRtAxis;
        private readonly float _medianPeakWidthOnRtAxis;
        private readonly float _maxPeakWidthOnRtAxis;
        private readonly float _stdevPeakWidthOnRtAxis;
        private readonly float _minPeakHeightOnRtAxis;
        private readonly float _averagePeakHeightOnRtAxis;
        private readonly float _medianPeakHeightOnRtAxis;
        private readonly float _maxPeakHeightOnRtAxis;
        private readonly float _stdevPeakHeightOnRtAxis;
        private readonly float _minPeakTopRT;
        private readonly float _averageminPeakTopRT;
        private readonly float _medianminPeakTopRT;
        private readonly float _maxPeakTopRT;
        private readonly float _stdevPeakTopRT;

        public double CoercePeakWidth(double peakWidth) {
            if (peakWidth >= _averagePeakWidthOnRtAxis + _stdevPeakWidthOnRtAxis * 3) { // width should be less than mean + 3*sigma for excluding redundant peak feature
                return _averagePeakWidthOnRtAxis + _stdevPeakWidthOnRtAxis * 3;
            }
            if (peakWidth <= _medianPeakWidthOnRtAxis) { // currently, the median peak width is used for very narrow peak feature
                return _medianPeakWidthOnRtAxis;
            }
            return peakWidth;
        }

        public void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinPeakWidthOnRtAxis = _minPeakWidthOnRtAxis;
            dto.AveragePeakWidthOnRtAxis = _averagePeakWidthOnRtAxis;
            dto.MedianPeakWidthOnRtAxis = _medianPeakWidthOnRtAxis;
            dto.MaxPeakWidthOnRtAxis = _maxPeakWidthOnRtAxis;
            dto.StdevPeakWidthOnRtAxis = _stdevPeakWidthOnRtAxis;
            dto.MinPeakHeightOnRtAxis = _minPeakHeightOnRtAxis;
            dto.MedianPeakHeightOnRtAxis = _medianPeakHeightOnRtAxis;
            dto.AveragePeakHeightOnRtAxis = _averagePeakHeightOnRtAxis;
            dto.MaxPeakHeightOnRtAxis = _maxPeakHeightOnRtAxis;
            dto.StdevPeakHeightOnRtAxis = _stdevPeakHeightOnRtAxis;
            dto.MinPeakTopRT = _minPeakTopRT;
            dto.AverageminPeakTopRT = _averageminPeakTopRT;
            dto.MedianminPeakTopRT = _medianminPeakTopRT;
            dto.MaxPeakTopRT = _maxPeakTopRT;
            dto.StdevPeakTopRT = _stdevPeakTopRT;
        }

        public static RtChromatogramPeakSummary Summarize(IReadOnlyList<ChromatogramPeakFeature> chromatogramPeakFeatures) {
            if (!chromatogramPeakFeatures.Any()) {
                return new RtChromatogramPeakSummary();
            }

            var peakWidthArray = chromatogramPeakFeatures.Select(chromatogramPeakFeature => chromatogramPeakFeature.PeakWidth()).ToArray();
            var minPeakWidthOnRtAxis = (float)peakWidthArray.Min();
            var averagePeakWidthOnRtAxis = (float)peakWidthArray.Average();
            var medianPeakWidthOnRtAxis = (float)BasicMathematics.Median(peakWidthArray);
            var maxPeakWidthOnRtAxis = (float)peakWidthArray.Max();
            var stdevPeakWidthOnRtAxis = (float)BasicMathematics.Stdev(peakWidthArray);

            var peakHeightArray = chromatogramPeakFeatures.Select(chromatogramPeakFeature => chromatogramPeakFeature.PeakHeightTop).ToArray();
            var minPeakHeightOnRtAxis = (float)peakHeightArray.Min();
            var averagePeakHeightOnRtAxis = (float)peakHeightArray.Average();
            var medianPeakHeightOnRtAxis = (float)BasicMathematics.Median(peakHeightArray);
            var maxPeakHeightOnRtAxis = (float)peakHeightArray.Max();
            var stdevPeakHeightOnRtAxis = (float)BasicMathematics.Stdev(peakHeightArray);

            var peaktopRtArray = chromatogramPeakFeatures.Select(chromatogramPeakFeature => chromatogramPeakFeature.ChromXs.Value).ToArray();
            var minPeakTopRT = (float)peaktopRtArray.Min();
            var averageminPeakTopRT = (float)peaktopRtArray.Average();
            var medianminPeakTopRT = (float)BasicMathematics.Median(peaktopRtArray);
            var maxPeakTopRT = (float)peaktopRtArray.Max();
            var stdevPeakTopRT = (float)BasicMathematics.Stdev(peaktopRtArray);

            return new RtChromatogramPeakSummary(
                minPeakWidthOnRtAxis,
                averagePeakWidthOnRtAxis,
                medianPeakWidthOnRtAxis,
                maxPeakWidthOnRtAxis,
                stdevPeakWidthOnRtAxis,
                minPeakHeightOnRtAxis,
                averagePeakHeightOnRtAxis,
                medianPeakHeightOnRtAxis,
                maxPeakHeightOnRtAxis,
                stdevPeakHeightOnRtAxis,
                minPeakTopRT,
                averageminPeakTopRT,
                medianminPeakTopRT,
                maxPeakTopRT,
                stdevPeakTopRT);
        }
    }

    class DtChromatogramPeakSummary {
        public DtChromatogramPeakSummary(
            float minPeakWidth,
            float averagePeakWidth,
            float medianPeakWidth,
            float maxPeakWidth,
            float stdevPeakWidth,
            float minPeakHeight,
            float averagePeakHeight,
            float medianPeakHeight,
            float maxPeakHeight,
            float stdevPeakHeight,
            float minPeakTop,
            float averageminPeakTop,
            float medianminPeakTop,
            float maxPeakTop,
            float stdevPeakTop) {

            _minPeakWidthOnDtAxis = minPeakWidth;
            _averagePeakWidthOnDtAxis = averagePeakWidth;
            _medianPeakWidthOnDtAxis = medianPeakWidth;
            _maxPeakWidthOnDtAxis = maxPeakWidth;
            _stdevPeakWidthOnDtAxis = stdevPeakWidth;
            _minPeakHeightOnDtAxis = minPeakHeight;
            _averagePeakHeightOnDtAxis = averagePeakHeight;
            _medianPeakHeightOnDtAxis = medianPeakHeight;
            _maxPeakHeightOnDtAxis = maxPeakHeight;
            _stdevPeakHeightOnDtAxis = stdevPeakHeight;
            _minPeakTopDT = minPeakTop;
            _averageminPeakTopDT = averageminPeakTop;
            _medianminPeakTopDT = medianminPeakTop;
            _maxPeakTopDT = maxPeakTop;
            _stdevPeakTopDT = stdevPeakTop;
        }

        private DtChromatogramPeakSummary() {

        }

        private readonly float _minPeakWidthOnDtAxis;
        private readonly float _averagePeakWidthOnDtAxis;
        private readonly float _medianPeakWidthOnDtAxis;
        private readonly float _maxPeakWidthOnDtAxis;
        private readonly float _stdevPeakWidthOnDtAxis;
        private readonly float _minPeakHeightOnDtAxis;
        private readonly float _averagePeakHeightOnDtAxis;
        private readonly float _medianPeakHeightOnDtAxis;
        private readonly float _maxPeakHeightOnDtAxis;
        private readonly float _stdevPeakHeightOnDtAxis;
        private readonly float _minPeakTopDT;
        private readonly float _averageminPeakTopDT;
        private readonly float _medianminPeakTopDT;
        private readonly float _maxPeakTopDT;
        private readonly float _stdevPeakTopDT;

        public void SetToDto(ChromatogramPeaksDataSummaryDto dto) {
            dto.MinPeakWidthOnDtAxis = _minPeakWidthOnDtAxis;
            dto.AveragePeakWidthOnDtAxis = _averagePeakWidthOnDtAxis;
            dto.MedianPeakWidthOnDtAxis = _medianPeakWidthOnDtAxis;
            dto.MaxPeakWidthOnDtAxis = _maxPeakWidthOnDtAxis;
            dto.StdevPeakWidthOnDtAxis = _stdevPeakWidthOnDtAxis;
            dto.MinPeakHeightOnDtAxis = _minPeakHeightOnDtAxis;
            dto.MedianPeakHeightOnDtAxis = _medianPeakHeightOnDtAxis;
            dto.AveragePeakHeightOnDtAxis = _averagePeakHeightOnDtAxis;
            dto.MaxPeakHeightOnDtAxis = _maxPeakHeightOnDtAxis;
            dto.StdevPeakHeightOnDtAxis = _stdevPeakHeightOnDtAxis;
            dto.MinPeakTopDT = _minPeakTopDT;
            dto.AverageminPeakTopDT = _averageminPeakTopDT;
            dto.MedianminPeakTopDT = _medianminPeakTopDT;
            dto.MaxPeakTopDT = _maxPeakTopDT;
            dto.StdevPeakTopDT = _stdevPeakTopDT;
        }

        public static DtChromatogramPeakSummary Summarize(IReadOnlyList<ChromatogramPeakFeature> chromatogramPeakFeatures) {
            var dtChromatogramPeakFeatures = chromatogramPeakFeatures.SelectMany(rtChromatogramPeakFeature => rtChromatogramPeakFeature.DriftChromFeatures.OrEmptyIfNull()).ToArray();
            if (!dtChromatogramPeakFeatures.Any()) {
                return new DtChromatogramPeakSummary();
            }

            var dtPeakWidths = dtChromatogramPeakFeatures.Select(dtChromatogramPeakFeature => dtChromatogramPeakFeature.PeakWidth()).ToArray();
            var minPeakWidthOnDtAxis = (float)dtPeakWidths.Min();
            var averagePeakWidthOnDtAxis = (float)dtPeakWidths.Average();
            var medianPeakWidthOnDtAxis = (float)BasicMathematics.Median(dtPeakWidths);
            var maxPeakWidthOnDtAxis = (float)dtPeakWidths.Max();
            var stdevPeakWidthOnDtAxis = (float)BasicMathematics.Stdev(dtPeakWidths);

            var dtPeakHeights = dtChromatogramPeakFeatures.Select(dtChromatogramPeakFeature => dtChromatogramPeakFeature.PeakHeightTop).ToArray();
            var minPeakHeightOnDtAxis = (float)dtPeakHeights.Min();
            var averagePeakHeightOnDtAxis = (float)dtPeakHeights.Average();
            var medianPeakHeightOnDtAxis = (float)BasicMathematics.Median(dtPeakHeights);
            var maxPeakHeightOnDtAxis = (float)dtPeakHeights.Max();
            var stdevPeakHeightOnDtAxis = (float)BasicMathematics.Stdev(dtPeakHeights);

            var peaktopDts = dtChromatogramPeakFeatures.Select(dtChromatogramPeakFeature => dtChromatogramPeakFeature.ChromXs.Value).ToArray();
            var minPeakTopDT = (float)peaktopDts.Min();
            var averageminPeakTopDT = (float)peaktopDts.Average();
            var medianminPeakTopDT = (float)BasicMathematics.Median(peaktopDts);
            var maxPeakTopDT = (float)peaktopDts.Max();
            var stdevPeakTopDT = (float)BasicMathematics.Stdev(peaktopDts);

            return new DtChromatogramPeakSummary(
                minPeakWidthOnDtAxis,
                averagePeakWidthOnDtAxis,
                medianPeakWidthOnDtAxis,
                maxPeakWidthOnDtAxis,
                stdevPeakWidthOnDtAxis,
                minPeakHeightOnDtAxis,
                averagePeakHeightOnDtAxis,
                medianPeakHeightOnDtAxis,
                maxPeakHeightOnDtAxis,
                stdevPeakHeightOnDtAxis,
                minPeakTopDT,
                averageminPeakTopDT,
                medianminPeakTopDT,
                maxPeakTopDT,
                stdevPeakTopDT);
        }
    }

    public class ChromatogramPeaksDataSummary {

        private readonly RawSpectraSummary _rawSpectraSummary;
        private readonly RtChromatogramPeakSummary _rtChromatogramPeakSummary;
        private readonly DtChromatogramPeakSummary _dtChromatogramPeakSummary;

        private ChromatogramPeaksDataSummary(RawSpectraSummary rawSpectraSummary, RtChromatogramPeakSummary rtChromatogramPeakSummary, DtChromatogramPeakSummary dtChromatogramPeakSummary) {
            _rawSpectraSummary = rawSpectraSummary;
            _rtChromatogramPeakSummary = rtChromatogramPeakSummary;
            _dtChromatogramPeakSummary = dtChromatogramPeakSummary;
        }

        public (double start, double end) GetPeakRange(ChromatogramPeakFeature chromatogramPeakFeature) {
            var peakWidth = _rtChromatogramPeakSummary.CoercePeakWidth(chromatogramPeakFeature.PeakWidth(ChromXType.RT));
            var topRt = chromatogramPeakFeature.ChromXsTop.RT.Value;
            var startRt = topRt - peakWidth * 1.5d;
            var endRt = topRt + peakWidth * 1.5d;
            return (startRt, endRt);
        }

        public static ChromatogramPeaksDataSummary Summarize(IDataProvider provider, IReadOnlyList<ChromatogramPeakFeature> chromatogramPeakFeatures) {
            var rawSpectraSummary = RawSpectraSummary.Summarize(provider);
            var rtChromatogramPeakSummary = RtChromatogramPeakSummary.Summarize(chromatogramPeakFeatures);
            var dtChromatogramPeakSummary = DtChromatogramPeakSummary.Summarize(chromatogramPeakFeatures);
            return new ChromatogramPeaksDataSummary(rawSpectraSummary, rtChromatogramPeakSummary, dtChromatogramPeakSummary);
        }

        public ChromatogramPeaksDataSummaryDto ConvertToDto() {
            var result = new ChromatogramPeaksDataSummaryDto();
            _rawSpectraSummary.SetToDto(result);
            _rtChromatogramPeakSummary.SetToDto(result);
            _dtChromatogramPeakSummary.SetToDto(result);
            return result;
        }

        public static ChromatogramPeaksDataSummary ConvertFromDto(ChromatogramPeaksDataSummaryDto dto) {
            var scanNumberSummary = new ScanNumberSummary(dto.MinScanNumber, dto.MaxScanNumber);
            var retentionTimeSummary = new RetentionTimeSummary(dto.MinRetentionTime, dto.MaxRetentionTime);
            var massSummary = new MassSummary(dto.MinMass, dto.MaxMass);
            var intensitySummary = new IntensitySummary(dto.MinIntensity, dto.MaxIntensity);
            var driftTimeSummary = new DriftTimeSummary(dto.MinDriftTime, dto.MaxDriftTime);
            var rawSpectraSummary = new RawSpectraSummary(scanNumberSummary, retentionTimeSummary, massSummary, intensitySummary, driftTimeSummary);

            var rtChromatogramPeakSummary = new RtChromatogramPeakSummary(
                dto.MinPeakWidthOnRtAxis,
                dto.AveragePeakWidthOnRtAxis,
                dto.MedianPeakWidthOnRtAxis,
                dto.MaxPeakWidthOnRtAxis,
                dto.StdevPeakWidthOnRtAxis,
                dto.MinPeakHeightOnRtAxis,
                dto.AveragePeakHeightOnRtAxis,
                dto.MedianPeakHeightOnRtAxis,
                dto.MaxPeakHeightOnRtAxis,
                dto.StdevPeakHeightOnRtAxis,
                dto.MinPeakTopRT,
                dto.AverageminPeakTopRT,
                dto.MedianminPeakTopRT,
                dto.MaxPeakTopRT,
                dto.StdevPeakTopRT);

            var dtChromatogramPeakSummary = new DtChromatogramPeakSummary(
                dto.MinPeakWidthOnDtAxis,
                dto.AveragePeakWidthOnDtAxis,
                dto.MedianPeakWidthOnDtAxis,
                dto.MaxPeakWidthOnDtAxis,
                dto.StdevPeakWidthOnDtAxis,
                dto.MinPeakHeightOnDtAxis,
                dto.AveragePeakHeightOnDtAxis,
                dto.MedianPeakHeightOnDtAxis,
                dto.MaxPeakHeightOnDtAxis,
                dto.StdevPeakHeightOnDtAxis,
                dto.MinPeakTopDT,
                dto.AverageminPeakTopDT,
                dto.MedianminPeakTopDT,
                dto.MaxPeakTopDT,
                dto.StdevPeakTopDT);

            return new ChromatogramPeaksDataSummary(rawSpectraSummary, rtChromatogramPeakSummary, dtChromatogramPeakSummary);
        }
    }

    /// <summary>
    /// Class for serialize and deserialize chromatogram information. (data transfer object)
    /// The use of getter and setter and the use for any method is prohibited.
    /// </summary>
    [MessagePackObject]
    public class ChromatogramPeaksDataSummaryDto
    {
        // property
        [Key(0)]
        public int MinScanNumber { get; set; }
        [Key(1)]
        public int MaxScanNumber { get; set; }
        [Key(2)]
        public float MinRetentionTime { get; set; }
        [Key(3)]
        public float MaxRetentionTime { get; set; }
        [Key(4)]
        public float MinMass { get; set; }
        [Key(5)]
        public float MaxMass { get; set; }

        [Key(6)]
        public float MinIntensity { get; set; }
        [Key(7)]
        public float MaxIntensity { get; set; }
       
        [Key(9)]
        public float MinPeakWidthOnRtAxis { get; set; }
        [Key(10)]
        public float AveragePeakWidthOnRtAxis { get; set; }
        [Key(11)]
        public float MedianPeakWidthOnRtAxis { get; set; }
        [Key(12)]
        public float MaxPeakWidthOnRtAxis { get; set; }
        [Key(13)]
        public float StdevPeakWidthOnRtAxis { get; set; }
        [Key(14)]
        public float MinPeakHeightOnRtAxis { get; set; }
        [Key(15)]
        public float MedianPeakHeightOnRtAxis { get; set; }
        [Key(16)]
        public float AveragePeakHeightOnRtAxis { get; set; }
        [Key(17)]
        public float MaxPeakHeightOnRtAxis { get; set; }
        [Key(18)]
        public float StdevPeakHeightOnRtAxis { get; set; }
        [Key(19)]
        public float MinPeakTopRT { get; set; }
        [Key(20)]
        public float AverageminPeakTopRT { get; set; }
        [Key(21)]
        public float MedianminPeakTopRT { get; set; }
        [Key(22)]
        public float MaxPeakTopRT { get; set; }
        [Key(23)]
        public float StdevPeakTopRT { get; set; }
        [Key(24)]
        public float MinDriftTime { get; set; }
        [Key(25)]
        public float MaxDriftTime { get; set; }

        [Key(26)]
        public float MinPeakWidthOnDtAxis { get; set; }
        [Key(27)]
        public float AveragePeakWidthOnDtAxis { get; set; }
        [Key(28)]
        public float MedianPeakWidthOnDtAxis { get; set; }
        [Key(29)]
        public float MaxPeakWidthOnDtAxis { get; set; }
        [Key(30)]
        public float StdevPeakWidthOnDtAxis { get; set; }

        [Key(31)]
        public float MinPeakHeightOnDtAxis { get; set; }
        [Key(32)]
        public float MedianPeakHeightOnDtAxis { get; set; }
        [Key(33)]
        public float AveragePeakHeightOnDtAxis { get; set; }
        [Key(34)]
        public float MaxPeakHeightOnDtAxis { get; set; }
        [Key(35)]
        public float StdevPeakHeightOnDtAxis { get; set; }
        [Key(36)]
        public float MinPeakTopDT { get; set; }
        [Key(37)]
        public float AverageminPeakTopDT { get; set; }
        [Key(38)]
        public float MedianminPeakTopDT { get; set; }
        [Key(39)]
        public float MaxPeakTopDT { get; set; }
        [Key(40)]
        public float StdevPeakTopDT { get; set; }
    }
}
