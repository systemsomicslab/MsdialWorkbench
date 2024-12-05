using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    public abstract class GapFiller3D : GapFiller
    {
        public abstract double AxTolFirst { get; }
        public abstract double AxTolSecond { get; }

        public GapFiller3D(SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert) : base(smoothingMethod, smoothingLevel, isForceInsert) { }
        public GapFiller3D(ParameterBase param) : base(param) { }

        public void GapFillFirst(RawSpectra rawSpectra, AlignmentSpotProperty spot, int fileID) {
            var peaks = spot.AlignedPeakProperties;
            var filtered = peaks.Where(peak => peak.PeakID >= 0);
            var chromXCenter = GetCenterFirst(filtered);
            var peakWidth = GetAveragePeakWidthFirst(filtered);
            var peaklist = GetPeaksFirst(rawSpectra, chromXCenter, peakWidth, fileID, smoothingMethod, smoothingLevel);
            var target = peaks.FirstOrDefault(peak => peak.FileID == fileID);
            GapFillCore(peaklist, chromXCenter, AxTolFirst, target);
        }

        public void GapFillSecond(List<RawSpectrum> spectra, AlignmentSpotProperty spot, AlignmentSpotProperty parent, int fileID) {
            var peaks = spot.AlignedPeakProperties;
            var filtered = peaks.Where(peak => peak.PeakID >= 0);
            var chromXCenter = GetCenterSecond(filtered, parent);
            var peakWidth = GetAveragePeakWidthSecond(filtered);
            var peaklist = GetPeaksSecond(spectra, chromXCenter, peakWidth, fileID, smoothingMethod, smoothingLevel);
            var target = peaks.FirstOrDefault(peak => peak.FileID == fileID);
            GapFillCore(peaklist, chromXCenter, AxTolSecond, target);
        }

        protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return GetCenterFirst(peaks);
        }
        protected abstract ChromXs GetCenterFirst(IEnumerable<AlignmentChromPeakFeature> peaks);
        protected abstract ChromXs GetCenterSecond(IEnumerable<AlignmentChromPeakFeature> peaks, AlignmentSpotProperty parent);

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return GetPeakWidth(peaks);
        }
        protected abstract double GetAveragePeakWidthFirst(IEnumerable<AlignmentChromPeakFeature> peaks);
        protected abstract double GetAveragePeakWidthSecond(IEnumerable<AlignmentChromPeakFeature> peaks);

        protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            return GetPeaksFirst(rawSpectra, center, peakWidth, fileID, smoothingMethod, smoothingLevel);
        }
        protected abstract List<ChromatogramPeak> GetPeaksFirst(RawSpectra rawSpectra, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel);
        protected abstract List<ChromatogramPeak> GetPeaksSecond(IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel);
    }
}
