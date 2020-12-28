using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentProcessFactory : AlignmentProcessFactory
    {
        public MsdialDimsParameter DimsParameter { get; }

        public DimsAlignmentProcessFactory(MsdialDimsParameter param, IupacDatabase iupac) : base(param, iupac){
            DimsParameter = param;
        }

        public override AlignmentRefiner CreateAlignmentRefiner() {
            return new DimsAlignmentRefiner(DimsParameter);
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
