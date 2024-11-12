using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialDimsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm;

public sealed class Ms2Dec
{
    public List<MSDecResult> GetMS2DecResults(
        IDataProvider provider, List<ChromatogramPeakFeature> chromPeakFeatures,
        MsdialDimsParameter param, ChromatogramPeaksDataSummaryDto summary,
        ReportProgress reporter,
        double targetCE = -1) {

        var msdecResults = new List<MSDecResult>();
        foreach (var peak in chromPeakFeatures) {
            var result = GetMS2DecResult(provider, peak, param, summary, targetCE);
            result.ScanID = peak.PeakID;
            msdecResults.Add(result);
            reporter.Report(result.ScanID, chromPeakFeatures.Count);
        }
        return msdecResults;
    }

    private static MSDecResult GetMS2DecResult(
        IDataProvider provider, ChromatogramPeakFeature chromPeakFeature,
        MsdialDimsParameter param, ChromatogramPeaksDataSummaryDto summary,
        double targetCE = -1) {

        var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(chromPeakFeature, targetCE);

        if (targetSpecID < 0) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
        var cSpectrum = DataAccess.GetCentroidMassSpectra(provider.LoadMsSpectrumFromIndex(targetSpecID), param.MS2DataType, param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
        if (cSpectrum.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

        var curatedSpectra = new List<SpectrumPeak>();
        var precursorMz = chromPeakFeature.Mass;
        var threshold = Math.Max(param.AmplitudeCutoff, 0.1); // TODO: remove magic number

        foreach (var peak in cSpectrum.Where(n => n.Intensity > threshold)) { //preparing MS/MS chromatograms -> peaklistList
            if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
            curatedSpectra.Add(peak);
        }
        if (curatedSpectra.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

        // if (param.AcquisitionType == CompMs.Common.Enum.AcquisitionType.DDA)
        return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
    }
}
