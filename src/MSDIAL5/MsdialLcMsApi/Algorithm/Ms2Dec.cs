using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.Raw.Abstractions;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.Algorithm
{
    public class Ms2Dec {
        public double InitialProgress { get; set; } = 30.0;
        public double ProgressMax { get; set; } = 30.0;

        public Ms2Dec(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }
       
        public async Task<List<MSDecResult>> GetMS2DecResultsAsync(AnalysisFileBean file, IDataProvider provider,
            IReadOnlyList<ChromatogramPeakFeature> chromPeakFeatures, MsdialLcmsParameter param, ChromatogramPeaksDataSummary summary,
            IupacDatabase iupac, IProgress<int>? progress, double targetCE = -1, CancellationToken token = default) {

            var msdecResults = new List<MSDecResult>();
            var numThreads = param.NumThreads == 1 ? 1 : 2;
            ReportProgress reporter = ReportProgress.FromLength(progress, InitialProgress, ProgressMax);
            if (numThreads == 1) {
                foreach (var spot in chromPeakFeatures) {
                    var result = await GetMS2DecResultAsync(file, provider, spot, param, summary, iupac, targetCE, token).ConfigureAwait(false);
                    result.ScanID = spot.PeakID;
                    msdecResults.Add(result);
                    reporter.Report(result.ScanID, chromPeakFeatures.Count);
                }
                return msdecResults;
            }
            else {
                var counter = 0;
                var msdecResultArray = new MSDecResult[chromPeakFeatures.Count];
                var queue = new ConcurrentQueue<int>(Enumerable.Range(0, chromPeakFeatures.Count));
                var tasks = new Task[numThreads];
                for (int i = 0; i < numThreads; i++) {
                    tasks[i] = Task.Run(async () => { 
                        while (queue.TryDequeue(out var index)) {
                            var spot = chromPeakFeatures[index];
                            var result = await GetMS2DecResultAsync(file, provider, spot, param, summary, iupac, targetCE, token).ConfigureAwait(false);
                            result.ScanID = spot.PeakID;
                            msdecResultArray[index] = result;
                            reporter.Report(Interlocked.Increment(ref counter), chromPeakFeatures.Count);
                        }
                    });
                }
                await Task.WhenAll(tasks).ConfigureAwait(false);
                return msdecResultArray.ToList();
            }

        }

        public async Task<MSDecResult> GetMS2DecResultAsync(AnalysisFileBean file,
            IDataProvider provider, ChromatogramPeakFeature chromPeakFeature,
            MsdialLcmsParameter param, ChromatogramPeaksDataSummary summary,
            IupacDatabase iupac,
            double targetCE = -1,
            CancellationToken token = default) { // targetCE is used in multiple CEs option

            // check target CE ID
            var targetSpecID = DataAccess.GetTargetCEIDForMS2RawSpectrum(chromPeakFeature, targetCE);

            //first, the MS/MS spectrum at the scan point of peak top is stored.
            if (targetSpecID < 0) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
            RawSpectrum spectrum = await provider.LoadSpectrumAsync((ulong)targetSpecID, chromPeakFeature.RawDataIDType).ConfigureAwait(false);
            var acquisition = MsmsAcquisition.Get(file.AcquisitionType) ?? MsmsAcquisition.None;
            if (acquisition.NeedQ1Deconvolution) {
                var query = new SpectraLoadingQuery
                {
                    MSLevel = spectrum.MsLevel,
                    ExperimentID = spectrum.ExperimentID,
                    CollisionEnergy = spectrum.Precursor?.CollisionEnergy,
                    ScanTimeRange = new() { Start = spectrum.ScanStartTime, End = spectrum.ScanStartTime },
                    EnableQ1Deconvolution = true,
                };
                var spectra = await provider.LoadMSSpectraAsync(query, token).ConfigureAwait(false);
                spectrum = spectra.FirstOrDefault();
            }

            var cSpectrum = DataAccess.GetCentroidMassSpectra(spectrum, param.MS2DataType, param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
            if (cSpectrum.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            var curatedSpectra = new List<SpectrumPeak>(); // used for normalization of MS/MS intensities
            var precursorMz = chromPeakFeature.PeakFeature.Mass;
            var threshold = Math.Max(param.AmplitudeCutoff, 0.1);

            foreach (var peak in cSpectrum.Where(n => n.Intensity > threshold)) { //preparing MS/MS chromatograms -> peaklistList
                if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
                curatedSpectra.Add(peak);
            }
            if (curatedSpectra.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            if (!file.IsDoMs2ChromDeconvolution) {
                if (param.IsDoAndromedaMs2Deconvolution)
                    return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, param, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
                else
                    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
            }
            //if (param.AcquisitionType == Common.Enum.AcquisitionType.DDA) {
            //    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
            //}

            //check the RT range to be considered for chromatogram deconvolution
            var (startRt, endRt)= summary.GetPeakRange(chromPeakFeature);

            //preparing MS1 and MS/MS chromatograms
            //note that the MS1 chromatogram trace (i.e. EIC) is also used as the candidate of model chromatogram
            var rawSpectrum = new RawSpectra(provider, param.IonMode, file.AcquisitionType);
            var chromatogramRange = new ChromatogramRange(startRt, endRt, ChromXType.RT, ChromXUnit.Min);
            using ExtractedIonChromatogram eic = await rawSpectrum.GetMS1ExtractedChromatogramAsync(new MzRange(precursorMz, param.CentroidMs1Tolerance), chromatogramRange, token).ConfigureAwait(false);
            var ms1Peaklist = ((Chromatogram)eic).AsPeakArray();

            var startTime = ms1Peaklist[0].ChromXs.RT.Value;
            var endTime = ms1Peaklist[ms1Peaklist.Count - 1].ChromXs.RT.Value;
            var minimumDiff = double.MaxValue;
            var minimumID = (int)(ms1Peaklist.Count / 2);

            // Define the scan number of peak top in the array of MS1 chromatogram restricted by the retention time range
            foreach (var (peak, index) in ms1Peaklist.WithIndex()) {
                var diff = Math.Abs(peak.ChromXs.Value - chromPeakFeature.ChromXs.Value);
                if (diff < minimumDiff) {
                    minimumDiff = diff; minimumID = index;
                }
            }
            int topScanNum = minimumID;

            List<double> productMzs = curatedSpectra.Select(x => (double)x.Mass).ToList();
            var ms2ValuePeaksList = await DataAccess.GetMs2ValuePeaksAsync(provider, precursorMz, startTime, endTime, productMzs, param, file.AcquisitionType, targetCE, token: token).ConfigureAwait(false);
            var sMs2Chromatograms = new List<ExtractedIonChromatogram>();
            foreach (var (ms2Peaks, productMz) in ms2ValuePeaksList.ZipInternal(productMzs)) {
                ExtractedIonChromatogram chromatogram = new ExtractedIonChromatogram(ms2Peaks, ChromXType.RT, ChromXUnit.Min, productMz).ChromatogramSmoothing(param.SmoothingMethod, param.SmoothingLevel);
                sMs2Chromatograms.Add(chromatogram);
            }

            //Do MS2Dec deconvolution
            if (sMs2Chromatograms.Count > 0) {

                if (ms2ValuePeaksList[0].Length > 1 && ms2ValuePeaksList[0].Length < ms1Peaklist.Count - 1) {
                    var ms2peaklist = ms2ValuePeaksList[0];
                    startTime = ms2peaklist[0].Id;
                    endTime = ms2peaklist[ms2peaklist.Length - 1].Id;
                    minimumDiff = double.MaxValue;
                    minimumID = (int)(ms2peaklist.Length / 2);

                    // Define the scan number of peak top in the array of MS1 chromatogram restricted by the retention time range
                    foreach (var (peak, index) in ms2peaklist.WithIndex()) {
                        var diff = Math.Abs(peak.Time - chromPeakFeature.ChromXs.Value);
                        if (diff < minimumDiff) {
                            minimumDiff = diff; minimumID = index;
                        }
                    }
                    topScanNum = minimumID;
                }

                var msdecResult = MSDecHandler.GetMSDecResult(sMs2Chromatograms, param, topScanNum);
                if (msdecResult == null) { //if null (any pure chromatogram is not found.)
                    if (param.IsDoAndromedaMs2Deconvolution)
                        return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, param, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
                    else
                        return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
                }
                else {
                    if (param.IsDoAndromedaMs2Deconvolution) {
                        msdecResult.Spectrum = DataAccess.GetAndromedaMS2Spectrum(msdecResult.Spectrum, param, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
                    }
                    if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
                        msdecResult.Spectrum = MSDecObjectHandler.ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, chromPeakFeature, param);
                    }
                }
                msdecResult.ChromXs = chromPeakFeature.ChromXs;
                msdecResult.RawSpectrumID = targetSpecID;
                msdecResult.PrecursorMz = precursorMz;

                foreach (var chrom in sMs2Chromatograms) {
                    chrom.Dispose();
                }
                return msdecResult;
            }

            //var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>();
            //var ms2ChromPeaksList = DataAccess.GetMs2Peaklistlist(provider, precursorMz, startIndex, endIndex, curatedSpectra.Select(x => (double)x.Mass).ToList(), param, targetCE);

            //foreach (var chromPeaks in ms2ChromPeaksList) {
            //    var sChromPeaks = new Chromatogram(chromPeaks, ChromXType.RT, ChromXUnit.Min).Smoothing(param.SmoothingMethod, param.SmoothingLevel);
            //    smoothedMs2ChromPeaksList.Add(sChromPeaks);
            //}

            //Do MS2Dec deconvolution
            //if (smoothedMs2ChromPeaksList.Count > 0) {
            //    var msdecResult = MSDecHandler.GetMSDecResult(smoothedMs2ChromPeaksList, param, topScanNum);
            //    if (msdecResult == null) { //if null (any pure chromatogram is not found.)
            //        if (param.IsDoAndromedaMs2Deconvolution)
            //            return MSDecObjectHandler.GetAndromedaSpectrum(chromPeakFeature, curatedSpectra, param, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
            //        else
            //            return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
            //    }
            //    else {
            //        if (param.IsDoAndromedaMs2Deconvolution) {
            //            msdecResult.Spectrum = DataAccess.GetAndromedaMS2Spectrum(msdecResult.Spectrum, param, iupac, Math.Abs(chromPeakFeature.PeakCharacter.Charge));
            //        }
            //        if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
            //            msdecResult.Spectrum = MSDecObjectHandler.ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, chromPeakFeature, param);
            //        }
            //    }
            //    msdecResult.ChromXs = chromPeakFeature.ChromXs;
            //    msdecResult.RawSpectrumID = targetSpecID;
            //    msdecResult.PrecursorMz = precursorMz;
            //    return msdecResult;
            //}
            
            return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
        }
    }
}
