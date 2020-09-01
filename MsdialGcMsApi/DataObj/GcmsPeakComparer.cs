using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.MsdialGcMsApi.DataObj {
    public class GcmsPeakComparer : PeakComparer {
        private AlignmentIndexType indextype;
        private RiCompoundType riCompoundType;
        private MsRefSearchParameterBase msMatchParam;

        public GcmsPeakComparer(AlignmentIndexType indextype, RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam) {
            this.indextype = indextype;
            this.riCompoundType = riCompoundType;
            this.msMatchParam = msMatchParam;
        }

        public override int Compare(IMSScanProperty x, IMSScanProperty y) {
            if (indextype == AlignmentIndexType.RT)
                return x.ChromXs.RT.Value.CompareTo(y.ChromXs.RT.Value);
            else
                return x.ChromXs.RI.Value.CompareTo(y.ChromXs.RI.Value);
        }

        public override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI ? true : false);
            var isRetentionMatch = indextype == AlignmentIndexType.RI ? result.IsRiMatch : result.IsRtMatch;
            if (result.IsSpectrumMatch && isRetentionMatch) return true;
            return false;
        }

        public override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI ? true : false);
            return result.TotalScore;
        }

        public override ChromXs GetCenter(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            return new ChromXs {
                RI = new RetentionIndex(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.RI.Value)),
                RT = new RetentionTime(chromFeatures.Select(n => n.ChromXsTop).Average(p => p.RT.Value))
            };
        }

        public override double GetAveragePeakWidth(IEnumerable<IChromatogramPeakFeature> chromFeatures) {
            if (indextype == AlignmentIndexType.RT)
                return chromFeatures.Max(n => n.PeakWidth(ChromXType.RT));
            else
                return chromFeatures.Max(n => n.PeakWidth(ChromXType.RI));
        }
    }

    public class GcmsAlignmentProcessFactory : AlignmentProcessFactory {
        private MsdialGcmsParameter Param;
        private Dictionary<int, float> FiehnRiDictionary;
        private Dictionary<int, RiDictionaryInfo> FileId2RiDictionary;
        private Dictionary<int, FiehnRiCoefficient> FileId2FiehnRiCoefficient;
        private Dictionary<int, FiehnRiCoefficient> FileId2RevFiehnRiCoefficient;
        private List<AnalysisFileBean> Files;

        public GcmsAlignmentProcessFactory(List<AnalysisFileBean> files, MsdialGcmsParameter param, List<MoleculeMsReference> mspdb) {
            this.Files = files;
            this.Param = param;
            this.FileId2RiDictionary = param.FileIdRiInfoDictionary;
            this.MspDB = mspdb;
            this.IsRepresentativeQuantMassBasedOnBasePeakMz = param.IsRepresentativeQuantMassBasedOnBasePeakMz;

            if (this.Param.AlignmentIndexType == AlignmentIndexType.RI && this.Param.RiCompoundType == RiCompoundType.Fames) {
                FiehnRiDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();
                FileId2FiehnRiCoefficient = new Dictionary<int, FiehnRiCoefficient>();
                FileId2RevFiehnRiCoefficient = new Dictionary<int, FiehnRiCoefficient>();
                foreach (var file in Files) {
                    var id = file.AnalysisFileId;
                    var riDict = FileId2RiDictionary[id].RiDictionary;
                    FileId2FiehnRiCoefficient[id] = RetentionIndexHandler.GetFiehnRiCoefficient(FiehnRiDictionary, riDict);
                    FileId2RevFiehnRiCoefficient[id] = RetentionIndexHandler.GetFiehnRiCoefficient(riDict, FiehnRiDictionary);
                }
            }

            this.IonMode = param.IonMode;
            this.SmoothingMethod = param.SmoothingMethod;
            this.SmoothingLevel = param.SmoothingLevel;
            this.MzTol = param.Ms1AlignmentTolerance;
            this.RtTol = param.RetentionTimeAlignmentTolerance;
            this.IsForceInsert = param.IsForceInsertForGapFilling;
        }

        /// <summary>
        /// peak width is RT range or RI range
        /// </summary>
        /// <param name="center"></param>
        /// <param name="peakWidth"></param>
        /// <param name="spectrumList"></param>
        /// <param name="fileID"></param>
        /// <returns></returns>
        public override List<ChromatogramPeak> PeaklistOnChromCenter(ChromXs center, double peakWidth, List<RawSpectrum> spectrumList, int fileID) {
            var centralRT = center.RT.Value;
            var centralRI = center.RI.Value;
            var maxRt = centralRT + peakWidth * 0.5; // temp
            var maxRi = centralRI + peakWidth * 0.5; // temp
            var minRt = centralRT - peakWidth * 0.5; // temp
            var minRi = centralRI - peakWidth * 0.5; // temp

            var centralMz = center.Mz.Value;
            #region // RT conversion if needed
            if (Param.AlignmentIndexType == AlignmentIndexType.RI) {
                var riDictionary = FileId2RiDictionary[fileID].RiDictionary;
                if (Param.RiCompoundType == RiCompoundType.Alkanes) {
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
               spectrumList, centralRT,
               centralRT - rtTol * 3.0F,
               centralRT + rtTol * 3.0F, centralMz, Param);
            return peaklist;
        }
    }
}
