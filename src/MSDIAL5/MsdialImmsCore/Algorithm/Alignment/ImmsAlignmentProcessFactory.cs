using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.Raw.Abstractions;

namespace CompMs.MsdialImmsCore.Algorithm.Alignment
{
    public class ImmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsdialImmsParameter ImmsParameter { get; }

        public ImmsAlignmentProcessFactory(IMsdialDataStorage<MsdialImmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> providerFactory) : base(storage.Parameter, storage.IupacDatabase, providerFactory) {
            ImmsParameter = storage.Parameter;
            this.evaluator = evaluator;
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this, ProviderFactory, null);
        }

        public override DataAccessor CreateDataAccessor() {
            return new ImmsDataAccessor(ImmsParameter.CentroidMs1Tolerance);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return new ImmsPeakJoiner(
                ImmsParameter.Ms1AlignmentTolerance, ImmsParameter.Ms1AlignmentFactor,
                ImmsParameter.DriftTimeAlignmentTolerance, ImmsParameter.DriftTimeAlignmentFactor);
        }

        public override IGapFiller CreateGapFiller() {
            return new ImmsGapFiller(
                ImmsParameter.DriftTimeAlignmentTolerance, ImmsParameter.Ms1AlignmentTolerance, ImmsParameter.IonMode,
                ImmsParameter.SmoothingMethod, ImmsParameter.SmoothingLevel, ImmsParameter.IsForceInsertForGapFilling);
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new ImmsAlignmentRefiner(ImmsParameter, Iupac, evaluator);
        }
    }
}
