using System;
using System.Collections.Generic;
using System.Linq;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;

namespace CompMs.MsdialGcMsApi.DataObj
{
    public class GcmsGapFiller : GapFiller
    {
        private readonly List<AnalysisFileBean> files;
        private readonly MsdialGcmsParameter param;
        private readonly AlignmentIndexType indexType;
        private Dictionary<int, float> FiehnRiDictionary;
        private Dictionary<int, FiehnRiCoefficient> FileId2FiehnRiCoefficient, FileId2RevFiehnRiCoefficient;
        private Dictionary<int, RiDictionaryInfo> FileId2RiDictionary;
        protected readonly double rtTol;
        private List<MoleculeMsReference> mspDB;
        private readonly bool isRepresentativeQuantMassBasedOnBasePeakMz;

        private bool IsReplaceMode => !mspDB.IsEmptyOrNull();
        private int Bin => param?.AccuracyType == AccuracyType.IsAccurate ? 2 : 0;

        protected override double AxTol => rtTol;

        public GcmsGapFiller(List<AnalysisFileBean> files, List<MoleculeMsReference> mspDB, MsdialGcmsParameter param) : base(param) {
            this.files = files;
            this.mspDB = mspDB;
            this.param = param;
            this.FileId2RiDictionary = param.FileIdRiInfoDictionary;
            this.isRepresentativeQuantMassBasedOnBasePeakMz = param.IsRepresentativeQuantMassBasedOnBasePeakMz;

            indexType = param.AlignmentIndexType;
            rtTol = param.RetentionTimeAlignmentTolerance;

            if (this.param.AlignmentIndexType == AlignmentIndexType.RI && this.param.RiCompoundType == RiCompoundType.Fames) {
                FiehnRiDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();
                FileId2FiehnRiCoefficient = new Dictionary<int, FiehnRiCoefficient>();
                FileId2RevFiehnRiCoefficient = new Dictionary<int, FiehnRiCoefficient>();
                foreach (var file in this.files) {
                    var id = file.AnalysisFileId;
                    var riDict = FileId2RiDictionary[id].RiDictionary;
                    FileId2FiehnRiCoefficient[id] = RetentionIndexHandler.GetFiehnRiCoefficient(FiehnRiDictionary, riDict);
                    FileId2RevFiehnRiCoefficient[id] = RetentionIndexHandler.GetFiehnRiCoefficient(riDict, FiehnRiDictionary);
                }
            }
        }

        protected override ChromXs GetCenter(IEnumerable<AlignmentChromPeakFeature> peaks) {
            var peaklist = peaks.ToList();
            var quantMass = GetQuantmass(peaklist);
            if (indexType == AlignmentIndexType.RT) {
                return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RT.Value), ChromXType.RT, ChromXUnit.Min)
                {
                    RI = new RetentionIndex(peaklist.Average(peak => peak.ChromXsTop.RI.Value)),
                    Mz = new MzValue(quantMass),
                };
            }
            return new ChromXs(peaklist.Average(peak => peak.ChromXsTop.RI.Value), ChromXType.RI, ChromXUnit.None)
            {
                RT = new RetentionTime(peaklist.Average(peak => peak.ChromXsTop.RT.Value)),
                Mz = new MzValue(quantMass),
            };
        }

        protected override double GetAveragePeakWidth(IEnumerable<AlignmentChromPeakFeature> peaks) {
            if (indexType == AlignmentIndexType.RI) {
                return peaks.Max(peak => peak.PeakWidth(ChromXType.RI));
            }
            else {
                return peaks.Max(peak => peak.PeakWidth(ChromXType.RT));
            }
        }

        /// <summary>
        /// peak width is RT range or RI range
        /// </summary>
        /// <param name="center"></param>
        /// <param name="peakWidth"></param>
        /// <param name="spectrumList"></param>
        /// <param name="fileID"></param>
        /// <returns></returns>
        protected override List<ChromatogramPeak> GetPeaks(List<RawSpectrum> spectrum, ChromXs center, double peakWidth, int fileID, SmoothingMethod smoothingMethod, int smoothingLevel) {

            var centralRT = center.RT.Value;
            var centralRI = center.RI.Value;
            var maxRt = centralRT + peakWidth * 0.5; // temp
            var maxRi = centralRI + peakWidth * 0.5; // temp
            var minRt = centralRT - peakWidth * 0.5; // temp
            var minRi = centralRI - peakWidth * 0.5; // temp

            var centralMz = center.Mz.Value;
            #region // RT conversion if needed
            if (indexType == AlignmentIndexType.RI) {
                var riDictionary = FileId2RiDictionary[fileID].RiDictionary;
                if (param.RiCompoundType == RiCompoundType.Alkanes) {
                    centralRT = RetentionIndexHandler.ConvertKovatsRiToRetentiontime(riDictionary, centralRI);
                    maxRt = RetentionIndexHandler.ConvertKovatsRiToRetentiontime(riDictionary, maxRi);
                    minRt = RetentionIndexHandler.ConvertKovatsRiToRetentiontime(riDictionary, minRi);
                }
                else {
                    var revFiehnRiCoeff = FileId2RevFiehnRiCoefficient[fileID];
                    centralRT = RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, centralRI);
                    maxRt = RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, maxRi);
                    minRt = RetentionIndexHandler.ConvertFiehnRiToRetentionTime(revFiehnRiCoeff, minRi);
                }
            }
            #endregion
            var rtTol = maxRt - minRt;
            var peaklist = DataAccess.GetBaselineCorrectedPeaklistByMassAccuracy(
               spectrum, centralRT,
               centralRT - rtTol * 3.0F,
               centralRT + rtTol * 3.0F, centralMz, param);
            return DataAccess.GetSmoothedPeaklist(peaklist, smoothingMethod, smoothingLevel);
        }

        private double GetQuantmass(List<AlignmentChromPeakFeature> peaks) {
            var repFileID = DataObjConverter.GetRepresentativeFileID(peaks);
            var mspId = peaks[repFileID].MspID();
            
            if (IsReplaceMode && repFileID >= 0 && mspId >= 0) {
                var refQuantMass = mspDB[mspId].QuantMass;
                if (param.MassRangeBegin <= refQuantMass && refQuantMass <= param.MassRangeEnd) {
                    return refQuantMass;
                }
            }

            var dclFile = files[repFileID].DeconvolutionFilePath;
            var msdecResult = MsdecResultsReader.ReadMSDecResult(dclFile, peaks[repFileID].SeekPointToDCLFile);
            var spectrum = msdecResult.Spectrum;
            var basepeak = GetBasePeak(spectrum);
            if (isRepresentativeQuantMassBasedOnBasePeakMz) {
                return basepeak.Mass;
            }

            var bin = Bin;
            var maxPeakHeight = peaks.Max(peak => peak.PeakHeightTop);
            var quantMassGroup = peaks.Where(peak => peak.PeakHeightTop > maxPeakHeight * 0.1)
                                      .GroupBy(peak => Math.Round(peak.Mass, bin));
            var quantMassCandidate = quantMassGroup.Argmax(group => group.Count()).Average(peak => peak.Mass);
            var isQuantMassExist = SuitableQuantMassExists(quantMassCandidate, basepeak.Intensity, spectrum, param.CentroidMs1Tolerance, 10.0F * 0.01);
            if (isQuantMassExist) {
                return quantMassCandidate;
            }

            var repQuantMass = peaks[repFileID].Mass;
            var isSuitableQuantMassExist = SuitableQuantMassExists(repQuantMass, basepeak.Intensity, spectrum, bin, 10.0 * 0.01);
            if (isSuitableQuantMassExist)
                return repQuantMass;

            return basepeak.Mass;
        }

        private static SpectrumPeak GetBasePeak(List<SpectrumPeak> spectrum) {
            return spectrum.Argmax(peak => peak.Intensity);
        }

        // spectrum should be ordered by m/z value
        private static bool SuitableQuantMassExists(double mass, double intensity, List<SpectrumPeak> spectrum, double bin, double threshold) {
            return spectrum.Where(peak => mass - bin <= peak.Mass)
                           .TakeWhile(peak => peak.Mass <= mass + bin)
                           .Any(peak => Math.Abs(peak.Mass - mass) <= bin
                                     && peak.Intensity > intensity * threshold);
        }
    }
}
