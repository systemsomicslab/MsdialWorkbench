using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment
{
    public class LcmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly DataBaseMapper mapper;

        public MsdialLcmsParameter LcmsParameter { get; }

        public LcmsAlignmentProcessFactory(MsdialLcmsParameter param, IupacDatabase iupac, DataBaseMapper mapper) : base(param, iupac){
            LcmsParameter = param;
            this.mapper = mapper;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new LcmsAlignmentRefiner(LcmsParameter, Iupac, mapper);
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

        public override IPeakJoiner CreatePeakJoiner() {
            return new LcmsPeakJoiner(
                LcmsParameter.RetentionTimeAlignmentTolerance, LcmsParameter.RetentionTimeAlignmentFactor,
                LcmsParameter.Ms1AlignmentTolerance, LcmsParameter.Ms1AlignmentFactor
                );
        }
    }
}
