using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcmsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm {
    public class Ms2Dec {
        public double InitialProgress { get; set; } = 30.0;
        public double ProgressMax { get; set; } = 30.0;

        public Ms2Dec(double InitialProgress, double ProgressMax) {
            this.InitialProgress = InitialProgress;
            this.ProgressMax = ProgressMax;
        }
       
        public List<MSDecResult> GetMS2DecResults(IReadOnlyList<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialLcmsParameter param, ChromatogramPeaksDataSummary summary, IupacDatabase iupac,
            Action<int> reportAction, System.Threading.CancellationToken token, double targetCE = -1) {

            var msdecResults = new List<MSDecResult>();
            foreach (var spot in chromPeakFeatures) {
                var result = GetMS2DecResult(spectrumList, spot, param, summary, iupac, targetCE);
                result.ScanID = spot.PeakID;
                msdecResults.Add(result);
                ReportProgress.Show(InitialProgress, ProgressMax, result.ScanID, chromPeakFeatures.Count(), reportAction);
            }
            return msdecResults;
        }

        public MSDecResult GetMS2DecResult(IReadOnlyList<RawSpectrum> spectrumList,
            ChromatogramPeakFeature chromPeakFeature, MsdialLcmsParameter param,
            ChromatogramPeaksDataSummary summary, IupacDatabase iupac,
            double targetCE = -1) { // targetCE is used in multiple CEs option

            // check target CE ID
            var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(chromPeakFeature, targetCE);

            //first, the MS/MS spectrum at the scan point of peak top is stored.
            var cSpectrum = DataAccess.GetCentroidMassSpectra(spectrumList, param.MS2DataType, targetSpecID, 
                param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
            if (cSpectrum.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            var curatedSpectra = new List<SpectrumPeak>(); // used for normalization of MS/MS intensities
            var precursorMz = chromPeakFeature.Mass;
            var threshold = Math.Max(param.AmplitudeCutoff, 0.1);

            foreach (var peak in cSpectrum.Where(n => n.Intensity > threshold)) { //preparing MS/MS chromatograms -> peaklistList
                if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
                curatedSpectra.Add(peak);
            }
            if (curatedSpectra.IsEmptyOrNull()) return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            if (!param.IsDoMs2ChromDeconvolution) {
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
            var rawSpectrum = new RawSpectra(spectrumList, ChromXType.RT, ChromXUnit.Min, param.IonMode);
            var ms1Peaklist = rawSpectrum.GetMs1Chromatogram(precursorMz, param.CentroidMs1Tolerance, startRt, endRt).Peaks;

            var startScanNum = ms1Peaklist[0].ID;
            var endScanNum = ms1Peaklist[ms1Peaklist.Count - 1].ID;
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
            var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>();
            var ms2ChromPeaksList = DataAccess.GetMs2Peaklistlist(spectrumList, precursorMz, startScanNum, endScanNum,
                curatedSpectra.Select(x => x.Mass).ToList(), param, targetCE);

            foreach (var chromPeaks in ms2ChromPeaksList) {
                var sChromPeaks = new Chromatogram(chromPeaks).Smoothing(param.SmoothingMethod, param.SmoothingLevel);
                smoothedMs2ChromPeaksList.Add(sChromPeaks);
            }

            //Do MS2Dec deconvolution
            if (smoothedMs2ChromPeaksList.Count > 0) {
                var msdecResult = MSDecHandler.GetMSDecResult(smoothedMs2ChromPeaksList, param, topScanNum);
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
                return msdecResult;
            }
            
            return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
        }
    }
}
