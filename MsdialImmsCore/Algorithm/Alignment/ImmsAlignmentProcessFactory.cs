using CompMs.Common.DataObj.Database;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment
{
    public class ImmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly DataBaseMapper mapper;

        public MsdialImmsParameter ImmsParameter { get; }

        public ImmsAlignmentProcessFactory(MsdialImmsParameter parameter, IupacDatabase iupac, DataBaseMapper mapper) : base(parameter, iupac) {
            ImmsParameter = parameter;
            this.mapper = mapper;
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this);
        }

        public override DataAccessor CreateDataAccessor() {
            return new ImmsDataAccessor(ImmsParameter.CentroidMs1Tolerance);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return new ImmsPeakJoiner(
                ImmsParameter.Ms1AlignmentTolerance, ImmsParameter.Ms1AlignmentFactor,
                ImmsParameter.DriftTimeAlignmentTolerance, ImmsParameter.DriftTimeAlignmentFactor);
        }

        public override GapFiller CreateGapFiller() {
            return new ImmsGapFiller(
                ImmsParameter.DriftTimeAlignmentTolerance, ImmsParameter.Ms1AlignmentTolerance, ImmsParameter.IonMode,
                ImmsParameter.SmoothingMethod, ImmsParameter.SmoothingLevel, ImmsParameter.IsForceInsertForGapFilling);
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new ImmsAlignmentRefiner(ImmsParameter, Iupac, mapper);
        }
    }
}
