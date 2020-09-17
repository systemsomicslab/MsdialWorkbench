using System;
using System.Collections.Generic;

using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;

namespace CompMs.MsdialDimsCore.DataObj
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

        public override PeakAligner CreatePeakAliner() {
            return new PeakAligner(this);
        }

        public override PeakJoiner CreatePeakJoiner() {
            return new DimsPeakJoiner(DimsParameter.Ms1AlignmentTolerance, DimsParameter.Ms1AlignmentFactor);
        }
    }
}
