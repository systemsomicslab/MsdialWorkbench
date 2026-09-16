using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm
{
    class ImmsDataAccessor : DataAccessor
    {
        private readonly double _massTolerance;

        public ImmsDataAccessor(double massTolerance) {
            _massTolerance = massTolerance;
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(prop => prop.MasterPeakID >= 0).ToArray();
            if (!detected.Any()) {
                throw new ArgumentException(nameof(spot));
            }
            var chromMax = detected.Max(x => x.ChromXsRight.Drift.Value);
            var chromMin = detected.Min(x => x.ChromXsLeft.Drift.Value);
            var tolerance = detected.Average(x => x.PeakWidth(ChromXType.Drift)) * 1.5f;

            var chromatogram = ms1Spectra.GetMs1ExtractedChromatogram(new MzRange(peak.Mass, _massTolerance), ChromatogramRange.FromTimes(new DriftTime(chromMin - tolerance), new DriftTime(chromMax + tolerance)));
            return new ChromatogramPeakInfo(peak.FileID, chromatogram.AsPeakArray(), peak.ChromXsTop.Drift, peak.ChromXsLeft.Drift, peak.ChromXsRight.Drift);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            return [.. chromatogram];
        }
    }
}
