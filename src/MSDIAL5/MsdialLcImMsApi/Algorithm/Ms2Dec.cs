using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcImMsApi.Algorithm; 
public sealed class Ms2Dec {
    private readonly AnalysisFileBean _analysisFile;

    public Ms2Dec(AnalysisFileBean analysisFile) {
        _analysisFile = analysisFile ?? throw new ArgumentNullException(nameof(analysisFile));
    }

    public List<MSDecResult> GetMS2DecResults(IDataProvider provider, List<ChromatogramPeakFeature> chromPeakFeatures,
        MsdialLcImMsParameter param, ChromatogramPeaksDataSummaryDto summary, IupacDatabase iupac,
        ReportProgress reporter, System.Threading.CancellationToken token, double targetCE = -1) {

        var msdecResults = new List<MSDecResult>();
        var counter = 0;
        foreach (var rtChromPeak in chromPeakFeatures) {
            var rtDecResult = MSDecObjectHandler.GetDefaultMSDecResult(rtChromPeak);
            rtDecResult.ScanID = rtChromPeak.MasterPeakID;
            msdecResults.Add(rtDecResult);
            foreach (var dtChromPeak in rtChromPeak.DriftChromFeatures.OrEmptyIfNull()) {
                var result = GetMS2DecResult(provider, rtChromPeak, dtChromPeak, param, summary, iupac, targetCE);
                result.ScanID = dtChromPeak.MasterPeakID;
                msdecResults.Add(result);
            }
            counter++;
            reporter.Report(counter, chromPeakFeatures.Count);
        }
        return msdecResults;
    }

    public MSDecResult GetMS2DecResult(IDataProvider provider, ChromatogramPeakFeature rtChromPeak, ChromatogramPeakFeature dtChromPeak, MsdialLcImMsParameter param, ChromatogramPeaksDataSummaryDto summary, IupacDatabase iupac, double targetCE) {
        if (dtChromPeak.MS2RawSpectrumID < 0) return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);

        // check target CE ID
        var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(dtChromPeak, targetCE);

        List<SpectrumPeak> cSpectrum = null;
        if (param.IsAccumulateMS2Spectra) {
            cSpectrum = DataAccess.GetAccumulatedMs2Spectra(provider, dtChromPeak, rtChromPeak, param);
        }
        else {
            if (targetSpecID < 0) {
                cSpectrum = new List<SpectrumPeak>();
            }
            else {
                cSpectrum = DataAccess.GetCentroidMassSpectra(provider.LoadMsSpectrumFromIndex(targetSpecID), param.MS2DataType, param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
            }
        }
        if (cSpectrum.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);

        var precursorMz = rtChromPeak.Mass;
        var curatedSpectra = new List<SpectrumPeak>(); // used for normalization of MS/MS intensities
        var threshold = Math.Max(param.AmplitudeCutoff, 0.1);
        foreach (var peak in cSpectrum.Where(n => n.Intensity > threshold)) { //preparing MS/MS chromatograms -> peaklistList
            if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
            curatedSpectra.Add(peak);
        }

        if (curatedSpectra.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);
        if (!_analysisFile.IsDoMs2ChromDeconvolution) {
            if (param.IsDoAndromedaMs2Deconvolution)
                return MSDecObjectHandler.GetAndromedaSpectrum(dtChromPeak, curatedSpectra, param, iupac, Math.Abs(dtChromPeak.PeakCharacter.Charge));
            else
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
        }
        //if (param.AcquisitionType == Common.Enum.AcquisitionType.DDA) {
        //    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
        //}

        var ms2obj = provider.LoadMsSpectrumFromIndex(dtChromPeak.MS2RawSpectrumID);
        var isDiaPasef = Math.Max(ms2obj.Precursor.TimeEnd, ms2obj.Precursor.TimeBegin) > 0;

        if (isDiaPasef) {
            if (param.IsDoAndromedaMs2Deconvolution) {
                return MSDecObjectHandler.GetAndromedaSpectrum(dtChromPeak, curatedSpectra, param, iupac, Math.Abs(dtChromPeak.PeakCharacter.Charge));
            }
            else {
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
            }
        }

        //check the DT range to be considered for chromatogram deconvolution
        var peakWidth = dtChromPeak.PeakWidth();
        if (peakWidth >= summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3) peakWidth = summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3; // width should be less than mean + 3*sigma for excluding redundant peak feature
        if (peakWidth <= summary.MedianPeakWidthOnDtAxis) peakWidth = summary.MedianPeakWidthOnDtAxis; // currently, the median peak width is used for very narrow peak feature

        var minDT = (float)(dtChromPeak.ChromXsTop.Value - peakWidth * 1.5F);
        var maxDT = (float)(dtChromPeak.ChromXsTop.Value + peakWidth * 1.5F);

        var ms2ChromPeaksList = DataAccess.GetAccumulatedMs2PeakListList(provider, rtChromPeak, curatedSpectra, minDT, maxDT, param.IonMode);
        var smoothedMs2ChromPeaksList = new List<ExtractedIonChromatogram>();

        foreach (var (chromPeaks, mz) in ms2ChromPeaksList) {
            var sChromPeaks = new ExtractedIonChromatogram(chromPeaks, ChromXType.Drift, ChromXUnit.Msec, mz).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
            smoothedMs2ChromPeaksList.Add(sChromPeaks);
        }

        //Do MS2Dec deconvolution
        if (smoothedMs2ChromPeaksList.Count > 0) {

            var topScanNum = 0;
            var minDiff = 1000.0;
            var minID = 0;
            var chromatogram = smoothedMs2ChromPeaksList[0];
            for (int i = 0; i < chromatogram.Length; i++) {
                var diff = Math.Abs(chromatogram.Time(i) - dtChromPeak.ChromXs.Value);
                if (diff < minDiff) {
                    minDiff = diff;
                    minID = chromatogram.Id(i);
                }
            }

            //foreach (var tmpPeaklist in smoothedMs2ChromPeaksList[0]) {
            //    var diff = Math.Abs(tmpPeaklist.ChromXs.Value - dtChromPeak.ChromXs.Value);
            //    if (diff < minDiff) {
            //        minDiff = diff;
            //        minID = tmpPeaklist.ID;
            //    }
            //}
            topScanNum = minID;

            var msdecResult = MSDecHandler.GetMSDecResult(smoothedMs2ChromPeaksList, param, topScanNum);
            if (msdecResult == null) { //if null (any pure chromatogram is not found.)
                if (param.IsDoAndromedaMs2Deconvolution)
                    return MSDecObjectHandler.GetAndromedaSpectrum(dtChromPeak, curatedSpectra, param, iupac, Math.Abs(dtChromPeak.PeakCharacter.Charge));
                else
                    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
            }
            else {
                if (param.IsDoAndromedaMs2Deconvolution) {
                    msdecResult.Spectrum = DataAccess.GetAndromedaMS2Spectrum(msdecResult.Spectrum, param, iupac, Math.Abs(dtChromPeak.PeakCharacter.Charge));
                }
                if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
                    msdecResult.Spectrum = MSDecObjectHandler.ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, dtChromPeak, param);
                }
            }
            msdecResult.ChromXs = dtChromPeak.ChromXs;
            msdecResult.RawSpectrumID = targetSpecID;
            msdecResult.PrecursorMz = precursorMz;
            return msdecResult;
        }

        return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);
    }
}
