using System;
using System.Collections.Generic;

using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public class GcmsPeakJoiner : PeakJoiner
    {
        private readonly AlignmentIndexType indextype;
        private readonly RiCompoundType riCompoundType;
        private readonly MsRefSearchParameterBase msMatchParam;

        public GcmsPeakJoiner(AlignmentIndexType indextype, RiCompoundType riCompoundType, MsRefSearchParameterBase msMatchParam) {
            this.indextype = indextype;
            this.riCompoundType = riCompoundType;
            this.msMatchParam = msMatchParam;
        }

        public override void AlignPeaksToMaster(List<AlignmentSpotProperty> spots, List<IMSScanProperty> masters, List<IMSScanProperty> targets, int fileId) {
            var n = masters.Count;
            var maxMatchs = new double[n];

            foreach (var target in targets) {
                // TODO: check tolerance
                int? matchIdx = null;
                double matchFactor = double.MinValue;
                for (var i = 0; i < n; i++) {
                    var factor = GetSimilality(masters[i], target);
                    if (factor > maxMatchs[i] && factor > matchFactor) {
                        matchIdx = i;
                        matchFactor = factor;
                    }
                }
                if (matchIdx.HasValue)
                    DataObjConverter.SetAlignmentChromPeakFeatureFromMSDecResult(spots[matchIdx.Value].AlignedPeakProperties[fileId], target as MSDecResult);
            }
        }

        protected override bool Equals(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI);
            var isRetentionMatch = indextype == AlignmentIndexType.RI ? result.IsRiMatch : result.IsRtMatch;
            return result.IsSpectrumMatch && isRetentionMatch;
        }

        protected override double GetSimilality(IMSScanProperty x, IMSScanProperty y) {
            var result = MsScanMatching.CompareEIMSScanProperties(x, y, this.msMatchParam, indextype == AlignmentIndexType.RI);
            return result.TotalScore;
        }
    }
}
