using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialImmsCore.Parameter;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.Algorithm
{
    public class Ms2Dec
    {
        public double InitialProgress { get; set; } = 30.0;
        public double ProgressMax { get; set; } = 30.0;

        public Ms2Dec(double initialProgress, double progressMax) {
            InitialProgress = initialProgress;
            ProgressMax = progressMax;
        }

        public List<MSDecResult> GetMS2DecResults(
            IReadOnlyList<RawSpectrum> spectrumList,
            List<ChromatogramPeakFeature> chromPeakFeatures,
            MsdialImmsParameter parameter,
            ChromatogramPeaksDataSummary summary,
            double targetCE,
            Action<int> reportAction,
            int numThread) {

            return chromPeakFeatures
                .AsParallel()
                .AsOrdered()
                .WithDegreeOfParallelism(numThread)
                .Select(spot => {
                    var result = GetMS2DecResult(spectrumList, spot, parameter, summary, targetCE);
                    ReportProgress.Show(InitialProgress, ProgressMax, spot.MasterPeakID, chromPeakFeatures.Count, reportAction);
                    return result;
                }).ToList();
        }

        public MSDecResult GetMS2DecResult(
            IReadOnlyList<RawSpectrum> spectrumList,
            ChromatogramPeakFeature chromPeakFeature,
            MsdialImmsParameter parameter, ChromatogramPeaksDataSummary summary,
            double targetCE = -1) {

            var targetSpecID = DataAccess.GetTargetCEIndexForMS2RawSpectrum(chromPeakFeature, targetCE);
            var precursorMz = chromPeakFeature.Mass;

            var curatedSpectra = GetCuratedSpectrum(spectrumList[targetSpecID], precursorMz, parameter);
            if (curatedSpectra.IsEmptyOrNull())
                return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);

            if (parameter.AcquisitionType == AcquisitionType.DDA) {
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
            }

            var ms1Peaklist = GetMs1Peaklist(
                spectrumList,
                chromPeakFeature,
                parameter.CentroidMs1Tolerance,
                summary,
                parameter.IonMode);

            var ms2ChromPeaksList = GetMs2PeaksList(
                spectrumList,
                precursorMz, curatedSpectra.Select(x => x.Mass).ToList(),
                ms1Peaklist.First().ID, ms1Peaklist.Last().ID,
                parameter, targetCE);

            if (ms2ChromPeaksList.IsEmptyOrNull()) {
                return MSDecObjectHandler.GetDefaultMSDecResult(chromPeakFeature);
            }

            //Do MS2Dec deconvolution
            var msdecResult = RunDeconvolution(chromPeakFeature, ms1Peaklist, ms2ChromPeaksList, parameter);
            if (msdecResult == null) //if null (any pure chromatogram is not found.)
                return MSDecObjectHandler.GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);

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

        private static List<ChromatogramPeak> GetMs1Peaklist(
            IReadOnlyList<RawSpectrum> spectrumList, ChromatogramPeakFeature chromPeakFeature,
            double centroidMs1Tolerance, ChromatogramPeaksDataSummary summary, IonMode ionMode) {

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
            return DataAccess.GetMs1Peaklist(
                spectrumList,
                (float)chromPeakFeature.Mass, centroidMs1Tolerance, ionMode,
                ChromXType.Drift, ChromXUnit.Msec,
                startDt, endDt);
        }

        private static List<List<ChromatogramPeak>> GetMs2PeaksList(
            IReadOnlyList<RawSpectrum> spectrumList,
            double precursorMz, List<double> productMz,
            int startScanNum, int endScanNum,
            MsdialImmsParameter parameter, double targetCE) {

            var ms2ChromPeaksList = DataAccess.GetMs2Peaklistlist(
                spectrumList, precursorMz,
                startScanNum, endScanNum,
                productMz, parameter, targetCE);

            var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>(ms2ChromPeaksList.Count);
            foreach (var chromPeaks in ms2ChromPeaksList) {
                var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, parameter.SmoothingMethod, parameter.SmoothingLevel);
                smoothedMs2ChromPeaksList.Add(sChromPeaks);
            }
            return smoothedMs2ChromPeaksList;
        }

        private static MSDecResult RunDeconvolution(
            ChromatogramPeakFeature chromPeakFeature,
            List<ChromatogramPeak> ms1PeakList,
            List<List<ChromatogramPeak>> ms2PeaksList,
            MsdialImmsParameter parameter) {

            var topScanNum = SearchSpectrumIndex(chromPeakFeature, ms1PeakList);
            return MSDecHandler.GetMSDecResult(ms2PeaksList, parameter, topScanNum);
        }

        private static int SearchSpectrumIndex(ChromatogramPeakFeature chromPeakFeature, List<ChromatogramPeak> ms1Peaklist) {
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
}
