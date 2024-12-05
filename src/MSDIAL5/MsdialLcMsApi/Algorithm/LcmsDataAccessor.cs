using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    class LcmsDataAccessor : DataAccessor
    {

        static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

        private readonly MsdialLcmsParameter lcmsParameter;

        public LcmsDataAccessor(MsdialLcmsParameter lcmsParameter) {
            this.lcmsParameter = lcmsParameter;
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
            var timeMin = detected.Min(x => x.ChromXsTop.RT.Value);
            var timeMax = detected.Max(x => x.ChromXsTop.RT.Value);
            var peakWidth = detected.Average(x => x.PeakWidth(ChromXType.RT));
            var tLeftRt = timeMin - peakWidth * 1.5F;
            var tRightRt = timeMax + peakWidth * 1.5F;
            if (tRightRt - tLeftRt > 5 && lcmsParameter.RetentionTimeAlignmentTolerance <= 2.5) {
                tLeftRt = spot.TimesCenter.Value - 2.5;
                tRightRt = spot.TimesCenter.Value + 2.5;
            }
            
            var chromatogramRange = new ChromatogramRange(tLeftRt, tRightRt, ChromXType.RT, ChromXUnit.Min);
            var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(peak.Mass, ms1MassTolerance, chromatogramRange);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist.ChromatogramSmoothing(this.lcmsParameter.SmoothingMethod, this.lcmsParameter.SmoothingLevel).AsPeakArray(),
                (float)peak.ChromXsTop.RT.Value, (float)peak.ChromXsLeft.RT.Value, (float)peak.ChromXsRight.RT.Value);
        }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var chromatogram = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
            chromatogram.Sort(Comparer);
            return new List<IMSScanProperty>(chromatogram);
        }
    }
}
