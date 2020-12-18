using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialLcmsApi.Parameter;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment
{
    public class LcmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        public MsdialLcmsParameter LcmsParameter { get; }

        public LcmsAlignmentProcessFactory(MsdialLcmsParameter param, IupacDatabase iupac) : base(param, iupac){
            LcmsParameter = param;
        }

        public override AlignmentRefiner CreateAlignmentRefiner() {
            return new LcmsAlignmentRefiner(LcmsParameter);
        }

        public override DataAccessor CreateDataAccessor() {
            return new LcmsDataAccessor();
        }

        public override GapFiller CreateGapFiller() {
            return new LcmsGapFiller(LcmsParameter);
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this);
        }

        public override PeakJoiner CreatePeakJoiner() {
            return new LcmsPeakJoiner(
                LcmsParameter.RetentionTimeAlignmentTolerance, LcmsParameter.RetentionTimeAlignmentTolerance,
                LcmsParameter.Ms1AlignmentTolerance, LcmsParameter.Ms1AlignmentFactor
                );
        }
    }
}
