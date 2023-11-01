using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Algorithm
{
    internal sealed class GcmsDataAccessor : DataAccessor
    {
        private MsdialGcmsParameter _parameter;

        public GcmsDataAccessor(MsdialGcmsParameter parameter) {
            switch (parameter.AlignmentIndexType) {
                case AlignmentIndexType.RI:
                    Comparer = ChromXsComparer.RIComparer;
                    break;
                case AlignmentIndexType.RT:
                    Comparer = ChromXsComparer.RTComparer;
                    break;
                default:
                    Comparer = ChromXsComparer.RIComparer;
                    break;
            }
            _parameter = parameter;
        }

        IComparer<IMSScanProperty> Comparer { get; }

        public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
            var msdecResults = MsdecResultsReader.ReadMSDecResults(analysisFile.DeconvolutionFilePath, out _, out _);
            msdecResults.Sort(Comparer);
            return msdecResults.Cast<IMSScanProperty>().ToList();
        }

        public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, IReadOnlyList<RawSpectrum> spectrum, float ms1MassTolerance) {
            //TODO: RI version
            var detected = spot.AlignedPeakProperties.Where(x => x.MasterPeakID >= 0);
            var timeMin = detected.Min(x => x.ChromXsTop.RT.Value);
            var timeMax = detected.Max(x => x.ChromXsTop.RT.Value);
            var peakWidth = detected.Average(x => x.PeakWidth(ChromXType.RT));
            var tLeftRt = timeMin - peakWidth * 1.5F;
            var tRightRt = timeMax + peakWidth * 1.5F;
            if (tRightRt - tLeftRt > 5 && _parameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance <= 2.5) {
                tLeftRt = spot.TimesCenter.Value - 2.5;
                tRightRt = spot.TimesCenter.Value + 2.5;
            }
            
            var chromatogramRange = new ChromatogramRange(tLeftRt, tRightRt, ChromXType.RT, ChromXUnit.Min);
            var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(peak.Mass, ms1MassTolerance, chromatogramRange);
            return new ChromatogramPeakInfo(
                peak.FileID, peaklist.Smoothing(_parameter.PeakPickBaseParam.SmoothingMethod, _parameter.PeakPickBaseParam.SmoothingLevel),
                (float)peak.ChromXsTop.RT.Value, (float)peak.ChromXsLeft.RT.Value, (float)peak.ChromXsRight.RT.Value);
        }
    }
}
