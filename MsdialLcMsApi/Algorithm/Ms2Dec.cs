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
using System.Text;

namespace CompMs.MsdialLcMsApi.Algorithm {
    public class Ms2Dec {
        public List<MSDecResult> GetMS2DecResults(List<RawSpectrum> spectrumList, List<ChromatogramPeakFeature> peakSpots,
            MsdialLcmsParameter param, IupacDatabase iupac,
            Action<int> reportAction, int AifFlag, System.Threading.CancellationToken token) {

            var msdecResults = new List<MSDecResult>();

            foreach (var spot in peakSpots) {

            }
            return msdecResults;
        }

        public MSDecResult GetMS2DecResult(List<RawSpectrum> spectrumList,
            ChromatogramPeakFeature chromPeakFeature, MsdialLcmsParameter param, ChromatogramPeaksDataSummary summary, int AifFlag) {
            
            //first, the MS/MS spectrum at the scan point of peak top is stored.
            var cSpectrum = DataAccess.GetCentroidMassSpectra(spectrumList, param.DataTypeMS2, chromPeakFeature.MS2RawSpectrumID, 
                param.AmplitudeCutoff, param.Ms2MassRangeBegin, param.Ms2MassRangeEnd);
            if (!cSpectrum.IsNotEmptyOrNull()) return GetDefaultMSDecResult(chromPeakFeature);


            //check the RT range to be considered for chromatogram deconvolution
            var peakWidth = chromPeakFeature.PeakWidth();
            if (peakWidth >= summary.AveragePeakWidth + summary.StdevPeakWidth * 3) peakWidth = summary.AveragePeakWidth + summary.StdevPeakWidth * 3; // width should be less than mean + 3*sigma for excluding redundant peak feature
            if (peakWidth <= summary.MedianPeakWidth) peakWidth = summary.MedianPeakWidth; // currently, the median peak width is used for very narrow peak feature

            var startRt = (float)(chromPeakFeature.ChromXsTop.Value - peakWidth * 1.5F);
            var endRt = (float)(chromPeakFeature.ChromXsTop.Value + peakWidth * 1.5F);

            //preparing MS1 and MS/MS chromatograms
            //note that the MS1 chromatogram trace (i.e. EIC) is also used as the candidate of model chromatogram
            var precursorMz = chromPeakFeature.Mass;
            var ms1Peaklist = DataAccess.GetMs1Peaklist(spectrumList, (float)precursorMz, param.CentroidMs1Tolerance, param.IonMode, ChromXType.RT, ChromXUnit.Min, startRt, endRt);

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
            int focusedPeakTopScanNumber = minimumID;

            #region must be modified for AIF
            ////get scan dictionary ID for MS1 and MS2
            //foreach (var value in projectProp.ExperimentID_AnalystExperimentInformationBean) {
            //    if (value.Value.MsType == MsType.SCAN) {
            //        ms1LevelId = value.Key;
            //        break;
            //    }
            //}
            //if (AifFlag == 0) {
            //    foreach (var value in projectProp.ExperimentID_AnalystExperimentInformationBean) {
            //        if (value.Value.MsType == MsType.SWATH && value.Value.StartMz < precursorMz && precursorMz <= value.Value.EndMz) {
            //            ms2LevelId = value.Key;
            //            break;
            //        }
            //    }
            //}
            //else {
            //    ms2LevelId = projectProp.Ms2LevelIdList[AifFlag - 1];
            //}
            #endregion
            if (cSpectrum.Count != 0) {
                var smoothedMs2ChromPeaksList = new List<List<ChromatogramPeak>>();
                var curatedSpectra = new List<SpectrumPeak>(); // used for normalization of MS/MS intensities
                foreach (var peak in cSpectrum.Where(n => n.Intensity > param.AmplitudeCutoff)) { //preparing MS/MS chromatograms -> peaklistList
                    if (param.RemoveAfterPrecursor && precursorMz + param.KeptIsotopeRange < peak.Mass) continue;
                    curatedSpectra.Add(peak);
                }
                if (param.MethodType == Common.Enum.MethodType.ddMSMS) {
                    return GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
                }

                var ms2ChromPeaksList = DataAccess.GetMs2Peaklistlist(spectrumList, precursorMz, startScanNum, endScanNum,
                    curatedSpectra.Select(x => x.Mass).ToList(), param);
                foreach (var chromPeaks in ms2ChromPeaksList) {
                    var sChromPeaks = DataAccess.GetSmoothedPeaklist(chromPeaks, param.SmoothingMethod, param.SmoothingLevel);
                    smoothedMs2ChromPeaksList.Add(sChromPeaks);
                }

                //Do MS2Dec deconvolution
                if (smoothedMs2ChromPeaksList.Count > 0) {
                    var msdecResult = MSDecHandler.GetMSDecResult(smoothedMs2ChromPeaksList, param, focusedPeakTopScanNumber);
                    if (msdecResult == null) //if null (any pure chromatogram is not found.)
                        return GetMSDecResultByRawSpectrum(chromPeakFeature, curatedSpectra);
                    else {
                        if (param.KeepOriginalPrecursorIsotopes) { //replace deconvoluted precursor isotopic ions by the original precursor ions
                            msdecResult.Spectrum = ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(msdecResult, curatedSpectra, chromPeakFeature, param);
                        }
                    }
                    msdecResult.ChromXs = chromPeakFeature.ChromXs;
                    msdecResult.PrecursorMz = precursorMz;
                    return msdecResult;
                }
            }

            return GetDefaultMSDecResult(chromPeakFeature);
        }

        public static List<SpectrumPeak> ReplaceDeconvolutedIsopicIonsToOriginalPrecursorIons(MSDecResult result, List<SpectrumPeak> curatedSpectra,
            ChromatogramPeakFeature chromPeakFeature, ParameterBase param) {
            var isotopicRange = param.KeptIsotopeRange;
            var replacedSpectrum = new List<SpectrumPeak>();

            foreach (var spec in result.Spectrum) {
                if (spec.Mass < chromPeakFeature.PrecursorMz - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            foreach (var spec in curatedSpectra) {
                if (spec.Mass >= chromPeakFeature.PrecursorMz - param.CentroidMs2Tolerance)
                    replacedSpectrum.Add(spec);
            }

            return replacedSpectrum.OrderBy(n => n.Mass).ToList();
        }

        public static MSDecResult GetDefaultMSDecResult(ChromatogramPeakFeature chromPeakFeature) {
            var result = new MSDecResult();

            result.ChromXs = chromPeakFeature.ChromXs;
            result.PrecursorMz = chromPeakFeature.Mass;
            result.ModelPeakMz = (float)chromPeakFeature.Mass;
            result.ModelPeakHeight = (float)chromPeakFeature.PeakHeightTop;
            return result;
        }

        public static MSDecResult GetMSDecResultByRawSpectrum(ChromatogramPeakFeature chromPeakFeature, List<SpectrumPeak> spectra) {

            var result = new MSDecResult();
            result.ChromXs = chromPeakFeature.ChromXs;
            result.PrecursorMz = chromPeakFeature.Mass;
            result.ModelPeakMz = (float)chromPeakFeature.Mass;
            result.ModelPeakHeight = (float)chromPeakFeature.PeakHeightTop;
            result.Spectrum = spectra;
            return result;
        }
    }
}
