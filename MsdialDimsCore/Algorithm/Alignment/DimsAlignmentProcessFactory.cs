using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using System;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly DataBaseMapper mapper;

        public MsdialDimsParameter DimsParameter { get; }

        public DimsAlignmentProcessFactory(MsdialDimsParameter param, IupacDatabase iupac, DataBaseMapper mapper) : base(param, iupac){
            DimsParameter = param;
            this.mapper = mapper;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new DimsAlignmentRefiner(DimsParameter, Iupac, mapper);
        }

        public override DataAccessor CreateDataAccessor() {
            return new DimsDataAccessor();
        }

        public override GapFiller CreateGapFiller() {
            return new DimsGapFiller(DimsParameter);
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return new DimsPeakJoiner(DimsParameter.Ms1AlignmentTolerance, DimsParameter.Ms1AlignmentFactor);
        }
    }
}
