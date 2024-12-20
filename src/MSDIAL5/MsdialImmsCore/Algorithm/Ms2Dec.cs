using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm;

public sealed class Ms2Dec
{
    public List<MSDecResult> GetMS2DecResults(
        AnalysisFileBean file,
        IDataProvider provider,
        IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures,
        MsdialImmsParameter parameter,
        ChromatogramPeaksDataSummaryDto summary,
        IupacDatabase iupac,
        double targetCE,
        int numThread,
        ReportProgress reporter,
        System.Threading.CancellationToken token) {

        return chromPeakFeatures
            .AsParallel()
            .AsOrdered()
            .WithCancellation(token)
            .WithDegreeOfParallelism(numThread)
            .Select(spot => {
                var result = GetMS2DecResult(file, provider, spot, parameter, summary, iupac, targetCE);
                reporter.Report(spot.MasterPeakID, chromPeakFeatures.Count);
                return result;
            }).ToList();
    }

    public MSDecResult GetMS2DecResult(
        AnalysisFileBean file,
        IDataProvider provider,
        ChromatogramPeakFeature chromPeakFeature, MsdialImmsParameter parameter, ChromatogramPeaksDataSummaryDto summary,
        IupacDatabase iupac, double targetCE = -1) {

        var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(chromPeakFeature, targetCE);
        var precursorMz = chromPeakFeature.Mass;

        if (targetSpecID < 0 || targetSpecID >= provider.Count())
            return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

        var curatedSpectra = GetCuratedSpectrum(provider.LoadMsSpectrumFromIndex(targetSpecID), precursorMz, parameter);
        if (curatedSpectra.IsEmptyOrNull())
            return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

        if (!file.IsDoMs2ChromDeconvolution) {
            if (parameter.IsDoAndromedaMs2Deconvolution)
                return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, parameter, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
            else
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
        }
        //if (parameter.AcquisitionType == AcquisitionType.DDA) {
        //    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
        //}

        var ms2obj = provider.LoadMsSpectrumFromIndex(chromPeakFeature.MS2RawSpectrumID);
        var isDiaPasef = Math.Max(ms2obj.Precursor.TimeEnd, ms2obj.Precursor.TimeBegin) > 0 ? true : false;

        if (isDiaPasef) {
            if (parameter.IsDoAndromedaMs2Deconvolution) {
                return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, parameter, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
            }
            else {
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
            }
        }

        var ms1Chromatogram = GetMs1Peaklist(
            provider,
            chromPeakFeature,
            parameter.CentroidMs1Tolerance,
            summary,
            parameter.IonMode,
            parameter, file.AcquisitionType);

        //var ms2ChromPeaksList = GetMs2PeaksList(provider, precursorMz, curatedSpectra.Select(x => x.Mass).ToList(), ms1Chromatogram, parameter, targetCE);
        var ms2ChromPeaksList = GetMs2PeaksList(provider, precursorMz, curatedSpectra.Select(x => x.Mass).ToList(), ms1Chromatogram, parameter, targetCE, file.AcquisitionType);

        if (ms2ChromPeaksList.IsEmptyOrNull()) {
            return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
        }

        //Do MS2Dec deconvolution
        var msdecResult = RunDeconvolution(chromPeakFeature, ms1Chromatogram.AsPeakArray(), ms2ChromPeaksList, parameter);
        if (msdecResult == null) { //if null (any pure chromatogram is not found.)
            if (parameter.IsDoAndromedaMs2Deconvolution)
                return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, parameter, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
            else
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
        }

        if (parameter.IsDoAndromedaMs2Deconvolution) {
            msdecResult.Spectrum = DataAccess.GetAndromedaMS2Spectrum(msdecResult.Spectrum, parameter, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
        }
        if (parameter.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
            msdecResult.Spectrum = MSDecObjectHandler.ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, chromPeakFeature, parameter);
        }

        msdecResult.ScanID = chromPeakFeature.PeakID;
        msdecResult.ChromXs = chromPeakFeature.ChromXs;
        msdecResult.RawSpectrumID = targetSpecID;
        msdecResult.PrecursorMz = precursorMz;

        return msdecResult;
    }

    private static List<SpectrumPeak> GetCuratedSpectrum(RawSpectrum ms2Spectrum, double precursorMz, MsdialImmsParameter parameter) {

        //first, the MS/MS spectrum at the scan point of peak top is stored.
        var cSpectrum = DataAccess.GetCentroidMassSpectra(
            ms2Spectrum, parameter.MS2DataType, parameter.AmplitudeCutoff,
            parameter.Ms2MassRangeBegin, parameter.Ms2MassRangeEnd);
        if (cSpectrum.IsEmptyOrNull())
            return new List<SpectrumPeak>();

        var threshold = Math.Max(parameter.AmplitudeCutoff, 0.1);
        var curatedSpectra = cSpectrum.Where(n => n.Intensity > threshold);

        if (parameter.RemoveAfterPrecursor)
            return curatedSpectra.Where(spectra => spectra.Mass <= precursorMz + parameter.KeptIsotopeRange).ToList();
        return curatedSpectra.ToList();
    }

    private static Chromatogram GetMs1Peaklist(
        IDataProvider provider, ChromatogramPeakFeature chromPeakFeature,
        double centroidMs1Tolerance, ChromatogramPeaksDataSummaryDto summary, IonMode ionMode, MsdialImmsParameter parameter, AcquisitionType type) {

        //check the Drift time range to be considered for chromatogram deconvolution
        var peakWidth = chromPeakFeature.PeakWidth();
        if (peakWidth >= summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3)
            peakWidth = summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3; // width should be less than mean + 3*sigma for excluding redundant peak feature
        if (peakWidth <= summary.MedianPeakWidthOnDtAxis)
            peakWidth = summary.MedianPeakWidthOnDtAxis; // currently, the median peak width is used for very narrow peak feature
        var startDt = (float)(chromPeakFeature.ChromXsTop.Value - peakWidth * 1.5F);
        var endDt = (float)(chromPeakFeature.ChromXsTop.Value + peakWidth * 1.5F);

        //preparing MS1 and MS/MS chromatograms
        //note that the MS1 chromatogram trace (i.e. EIC) is also used as the candidate of model chromatogram
        var rawSpectra = new RawSpectra(provider, ionMode, type);
        var chromatogramRange = new ChromatogramRange(startDt, endDt, ChromXType.Drift, ChromXUnit.Msec);
        return rawSpectra.GetMS1ExtractedChromatogram(new MzRange(chromPeakFeature.Mass, centroidMs1Tolerance), chromatogramRange);
    }

    //private static List<List<ChromatogramPeak>> GetMs2PeaksList(
    //    IDataProvider provider,
    //    double precursorMz, List<double> productMz,
    //    Chromatogram ms1Chromatogram,
    //    MsdialImmsParameter parameter, double targetCE) {

    //    var ms2ChromPeaksList = DataAccess.GetMs2Peaklistlist(provider, precursorMz, ms1Chromatogram.Peaks.First().ID, ms1Chromatogram.Peaks.Last().ID, productMz, parameter, targetCE, ChromXType.Drift, ChromXUnit.Msec);

    //    var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>(ms2ChromPeaksList.Count);
    //    foreach (var chromPeaks in ms2ChromPeaksList) {
    //        var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.Drift, ChromXUnit.Msec).Smoothing(parameter.SmoothingMethod, parameter.SmoothingLevel);
    //        smoothedMs2ChromPeaksList.Add(sChromPeaks);
    //    }
    //    return smoothedMs2ChromPeaksList;
    //}

    private static List<ExtractedIonChromatogram> GetMs2PeaksList(
        IDataProvider provider,
        double precursorMz, List<double> productMz,
        Chromatogram ms1Chromatogram,
        MsdialImmsParameter parameter, double targetCE, AcquisitionType type) {

        IReadOnlyList<IChromatogramPeak> peaks = ms1Chromatogram.AsPeakArray();
        var ms2ChromPeaksList = DataAccess.GetMs2ValuePeaks(provider, precursorMz, peaks.First().ID, peaks.Last().ID, productMz, parameter, type, targetCE, ChromXType.Drift, ChromXUnit.Msec);

        var smoothedMs2ChromPeaksList = new List<ExtractedIonChromatogram>(ms2ChromPeaksList.Count);
        foreach (var (chromPeaks, mz) in ms2ChromPeaksList.ZipInternal(productMz)) {
            var sChromPeaks = new ExtractedIonChromatogram(chromPeaks, ChromXType.Drift, ChromXUnit.Msec, mz).ChromatogramSmoothing(parameter.SmoothingMethod, parameter.SmoothingLevel);
            smoothedMs2ChromPeaksList.Add(sChromPeaks);
        }
        return smoothedMs2ChromPeaksList;
    }

    private static MSDecResult RunDeconvolution(
        ChromatogramPeakFeature chromPeakFeature,
        IReadOnlyList<IChromatogramPeak> ms1PeakList,
        List<ExtractedIonChromatogram> ms2PeaksList,
        MsdialImmsParameter parameter) {

        var topScanNum = SearchSpectrumIndex(chromPeakFeature, ms1PeakList);
        return MSDecHandler.GetMSDecResult(ms2PeaksList, parameter, topScanNum);
    }

    private static int SearchSpectrumIndex(ChromatogramPeakFeature chromPeakFeature, IReadOnlyList<IChromatogramPeak> ms1Peaklist) {
        var minimumDiff = double.MaxValue;
        var minimumID = ms1Peaklist.Count / 2;
        // Define the scan number of peak top in the array of MS1 chromatogram restricted by the retention time range
        foreach (var (peak, index) in ms1Peaklist.WithIndex()) {
            var diff = Math.Abs(peak.ChromXs.Value - chromPeakFeature.ChromXs.Value);
            if (diff < minimumDiff) {
                minimumDiff = diff; minimumID = index;
            }
        }
        return minimumID;
    }
}
