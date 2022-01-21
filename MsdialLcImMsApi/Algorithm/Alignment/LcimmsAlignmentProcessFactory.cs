using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;

namespace CompMs.MsdialLcImMsApi.Algorithm.Alignment
{
    public class LcimmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsdialLcImMsParameter LcimmsParameter { get; }

        public LcimmsAlignmentProcessFactory(IMsdialDataStorage<MsdialLcImMsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(storage.Parameter, storage.IupacDatabase) {
            LcimmsParameter = storage.Parameter;
            this.evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new LcimmsAlignmentRefiner(LcimmsParameter, Iupac, evaluator);
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

        public override IPeakJoiner CreatePeakJoiner() {
            return new LcimmsPeakJoiner(
                LcimmsParameter.RetentionTimeAlignmentTolerance, LcimmsParameter.RetentionTimeAlignmentTolerance,
                LcimmsParameter.Ms1AlignmentTolerance, LcimmsParameter.Ms1AlignmentFactor,
                LcimmsParameter.DriftTimeAlignmentTolerance, LcimmsParameter.DriftTimeAlignmentFactor
                );
        }
    }
}
