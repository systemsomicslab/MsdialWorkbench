using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialLcImMsApi.Algorithm {
    public class Ms2Dec {

        public List<MSDecResult> GetMS2DecResults(List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialLcImMsParameter param, ChromatogramPeaksDataSummary summary,
            Action<int> reportAction, System.Threading.CancellationToken token, double targetCE = -1) {

            var msdecResults = new List<MSDecResult>();

            foreach (var rtChromPeak in chromPeakFeatures) {
                var rtDecResult = MSDecObjectHandler.GetDefaultMSDecResult(rtChromPeak);
                rtDecResult.ScanID = rtChromPeak.MasterPeakID;
                msdecResults.Add(rtDecResult);
                foreach (var dtChromPeak in rtChromPeak.DriftChromFeatures.OrEmptyIfNull()) {
                    var result = GetMS2DecResult(spectrumList, rtChromPeak, dtChromPeak, param, summary, targetCE);
                    result.ScanID = dtChromPeak.MasterPeakID;
                    msdecResults.Add(result);
                }
            }
            return msdecResults;
        }

        public MSDecResult GetMS2DecResult(List<RawSpectrum> spectrumList, ChromatogramPeakFeature rtChromPeak, ChromatogramPeakFeature dtChromPeak,
            MsdialLcImMsParameter param, ChromatogramPeaksDataSummary summary, double targetCE) {
            if (dtChromPeak.MS2RawSpectrumID < 0) return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);

            rtChromPeak.MS2RawSpectrumID = 1; // needed for visualization
            List<SpectrumPeak> cSpectrum = null;
            if (param.IsAccumulateMS2Spectra) {
                cSpectrum = DataAccess.GetAccumulatedMs2Spectra(spectrumList, dtChromPeak, rtChromPeak, param);
            }
            else {
                cSpectrum = DataAccess.GetCentroidMassSpectra(spectrumList, param.MS2DataType, dtChromPeak.MS2RawSpectrumID,
                    param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
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
            if (param.AcquisitionType == Common.Enum.AcquisitionType.DDA) {
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
            }

            //check the DT range to be considered for chromatogram deconvolution
            var peakWidth = dtChromPeak.PeakWidth();
            if (peakWidth >= summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3) peakWidth = summary.AveragePeakWidthOnDtAxis + summary.StdevPeakWidthOnDtAxis * 3; // width should be less than mean + 3*sigma for excluding redundant peak feature
            if (peakWidth <= summary.MedianPeakWidthOnDtAxis) peakWidth = summary.MedianPeakWidthOnDtAxis; // currently, the median peak width is used for very narrow peak feature

            var minDT = (float)(dtChromPeak.ChromXsTop.Value - peakWidth * 1.5F);
            var maxDT = (float)(dtChromPeak.ChromXsTop.Value + peakWidth * 1.5F);

            var ms2ChromPeaksList = DataAccess.GetAccumulatedMs2PeakListList(spectrumList, rtChromPeak, curatedSpectra, minDT, maxDT, param.IonMode);
            var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>();

            foreach (var chromPeaks in ms2ChromPeaksList) {
                var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
                smoothedMs2ChromPeaksList.Add(sChromPeaks);
            }

            //Do MS2Dec deconvolution
            if (smoothedMs2ChromPeaksList.Count > 0) {

                var topScanNum = 0;
                var minDiff = 1000.0;
                var minID = 0;
                foreach (var tmpPeaklist in smoothedMs2ChromPeaksList[0]) {
                    var diff = Math.Abs(tmpPeaklist.ChromXs.Value - dtChromPeak.ChromXs.Value);
                    if (diff < minDiff) {
                        minDiff = diff;
                        minID = (int)(tmpPeaklist.ID);
                    }
                }
                topScanNum = minID;

                var msdecResult = MSDecHandler.GetMSDecResult(smoothedMs2ChromPeaksList, param, topScanNum);
                if (msdecResult == null) //if null (any pure chromatogram is not found.)
                    return MSDecObjectHandler.GetMSDecResultByRawSpectrum(dtChromPeak, curatedSpectra);
                else {
                    if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
                        msdecResult.Spectrum = MSDecObjectHandler.ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, dtChromPeak, param);
                    }
                }
                msdecResult.ChromXs = dtChromPeak.ChromXs;
                msdecResult.PrecursorMz = precursorMz;
                return msdecResult;
            }

            return MSDecObjectHandler.GetDefaultMSDecResult(dtChromPeak);
        }
    }
}
