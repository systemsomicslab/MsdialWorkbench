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
}
