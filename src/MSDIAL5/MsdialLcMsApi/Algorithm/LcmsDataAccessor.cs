using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.Algorithm.Alignment;
using CompMs.MsdialLcmsApi.Parameter;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm;

class LcmsDataAccessor : DataAccessor, IFeatureAccessor<ChromatogramPeakFeature>
{

    static readonly IComparer<IMSScanProperty> Comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

    private readonly MsdialLcmsParameter lcmsParameter;
    private readonly AlignmentRetentionTimeCorrectionCollection? alignmentRtCorrection;

    public LcmsDataAccessor(
        MsdialLcmsParameter lcmsParameter,
        AlignmentRetentionTimeCorrectionCollection? alignmentRtCorrection = null) {
        this.lcmsParameter = lcmsParameter;
        this.alignmentRtCorrection = alignmentRtCorrection;
    }

    public override ChromatogramPeakInfo AccumulateChromatogram(AlignmentChromPeakFeature peak, AlignmentSpotProperty spot, Ms1Spectra ms1Spectra, float ms1MassTolerance) {
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
        if (alignmentRtCorrection is not null) {
            tLeftRt = alignmentRtCorrection.Restore(peak.FileID, tLeftRt);
            tRightRt = alignmentRtCorrection.Restore(peak.FileID, tRightRt);
        }
        
        var chromatogramRange = new ChromatogramRange(tLeftRt, tRightRt, ChromXType.RT, ChromXUnit.Min);
        var peaklist = ms1Spectra.GetMs1ExtractedChromatogram(peak.Mass, ms1MassTolerance, chromatogramRange);
        var top = alignmentRtCorrection?.Restore(peak.FileID, peak.ChromXsTop.RT.Value) ?? peak.ChromXsTop.RT.Value;
        var left = alignmentRtCorrection?.Restore(peak.FileID, peak.ChromXsLeft.RT.Value) ?? peak.ChromXsLeft.RT.Value;
        var right = alignmentRtCorrection?.Restore(peak.FileID, peak.ChromXsRight.RT.Value) ?? peak.ChromXsRight.RT.Value;
        return new ChromatogramPeakInfo(
            peak.FileID, peaklist.ChromatogramSmoothing(this.lcmsParameter.SmoothingMethod, this.lcmsParameter.SmoothingLevel).AsPeakArray(),
            (float)top, (float)left, (float)right);
    }

    List<ChromatogramPeakFeature> IFeatureAccessor<ChromatogramPeakFeature>.GetMSScanProperties(AnalysisFileBean analysisFile) {
        var chromatogram = MsdialPeakSerializer.LoadChromatogramPeakFeatures(analysisFile.PeakAreaBeanInformationFilePath);
        if (alignmentRtCorrection is not null) {
            foreach (var peak in chromatogram) {
                alignmentRtCorrection.Correct(peak, analysisFile.AnalysisFileId);
            }
        }
        chromatogram.Sort(Comparer);
        return chromatogram;
    }

    public override List<IMSScanProperty> GetMSScanProperties(AnalysisFileBean analysisFile) {
        var chromatogram = ((IFeatureAccessor<ChromatogramPeakFeature>)this).GetMSScanProperties(analysisFile);
        return [.. chromatogram];
    }
}
