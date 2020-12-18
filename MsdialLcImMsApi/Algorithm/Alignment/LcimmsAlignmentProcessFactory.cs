using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialLcImMsApi.Parameter;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        public MsdialLcImMsParameter LcimmsParameter { get; }

        public LcimmsAlignmentProcessFactory(MsdialLcImMsParameter param, IupacDatabase iupac) : base(param, iupac){
            LcimmsParameter = param;
        }

        public override AlignmentRefiner CreateAlignmentRefiner() {
            return new LcimmsAlignmentRefiner(LcimmsParameter);
        }

        public override DataAccessor CreateDataAccessor() {
            return new LcimmsDataAccessor();
        }

        public override GapFiller CreateGapFiller() {
            return new LcimmsGapFiller(LcimmsParameter);
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner3D(this);
        }

        public override PeakJoiner CreatePeakJoiner() {
            return new LcimmsPeakJoiner(
                LcimmsParameter.RetentionTimeAlignmentTolerance, LcimmsParameter.RetentionTimeAlignmentTolerance,
                LcimmsParameter.Ms1AlignmentTolerance, LcimmsParameter.Ms1AlignmentFactor,
                LcimmsParameter.DriftTimeAlignmentTolerance, LcimmsParameter.DriftTimeAlignmentFactor
                );
        }
    }
}
