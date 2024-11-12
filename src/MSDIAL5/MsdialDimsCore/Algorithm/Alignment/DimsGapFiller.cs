using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsGapFiller : GapFiller
    {
        private readonly Dictionary<int, RawPeakElement[]> peakElementsMemo;
        protected readonly double mzTol;

        protected override double AxTol => mzTol;

        public DimsGapFiller(double mzTol, SmoothingMethod smoothingMethod, int smoothingLevel, bool isForceInsert)
            : base(smoothingMethod, smoothingLevel, isForceInsert) {
            this.mzTol = mzTol;
            peakElementsMemo = new Dictionary<int, RawPeakElement[]>();
        }

        public DimsGapFiller(MsdialDimsParameter param)
            : this(param.CentroidMs1Tolerance, param.SmoothingMethod, param.SmoothingLevel, param.IsForceInsertForGapFilling) { }

        protected override ChromXs GetCenter(AlignmentSpotProperty spot, IEnumerable<AlignmentChromPeakFeature> peaks) {
            return new ChromXs(peaks.Average(peak => peak.ChromXsTop.Mz.Value), ChromXType.Mz, ChromXUnit.Mz);
        }

        protected override double GetPeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            return peaks.Max(peak => peak.PeakWidth(ChromXType.Mz));
        }

        protected override List<ChromatogramPeak> GetPeaks(Ms1Spectra ms1Spectra, RawSpectra rawSpectra, IReadOnlyList<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {
            lock (peakElementsMemo) {
                if (!peakElementsMemo.ContainsKey(fileID))
                    peakElementsMemo[fileID] = DataAccess.AccumulateMS1Spectrum(spectrum); // TODO: remove cache (too much memory use)
            }
            var peakElements = peakElementsMemo[fileID];
            var peaklist = DataAccess.ConvertRawPeakElementToChromatogramPeakList(peakElements, center.Mz.Value - peakWidth * 2.0, center.Mz.Value + peakWidth * 2.0);
            return new Chromatogram(peaklist, ChromXType.Mz, ChromXUnit.Mz).ChromatogramSmoothing(smoothingMethod, smoothingLevel).AsPeakArray();
        }
    }
}
