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
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly IDataProviderFactory<AnalysisFileBean> _rawDataProvider;
        private readonly IDataProviderFactory<AnalysisFileBean> _accumulatedDataProvider;

        public MsdialLcImMsParameter LcimmsParameter { get; }

        public LcimmsAlignmentProcessFactory(IMsdialDataStorage<MsdialLcImMsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> rawDataProvider, IDataProviderFactory<AnalysisFileBean> accumulatedDataProvider) : base(storage.Parameter, storage.IupacDatabase) {
            LcimmsParameter = storage.Parameter;
            _evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            _rawDataProvider = rawDataProvider ?? throw new System.ArgumentNullException(nameof(rawDataProvider));
            _accumulatedDataProvider = accumulatedDataProvider ?? throw new System.ArgumentNullException(nameof(accumulatedDataProvider));
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new LcimmsAlignmentRefiner(LcimmsParameter, Iupac, _evaluator);
        }

        public override DataAccessor CreateDataAccessor() {
            return new LcimmsDataAccessor();
        }

        public override GapFiller CreateGapFiller() {
            return new LcimmsGapFiller(LcimmsParameter);
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner3D(this, _rawDataProvider, _accumulatedDataProvider);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return new LcimmsPeakJoiner(
                LcimmsParameter.AlignmentBaseParam.RetentionTimeAlignmentTolerance, LcimmsParameter.AlignmentBaseParam.RetentionTimeAlignmentFactor,
                LcimmsParameter.AlignmentBaseParam.Ms1AlignmentTolerance, LcimmsParameter.AlignmentBaseParam.Ms1AlignmentFactor,
                LcimmsParameter.DriftTimeAlignmentTolerance, LcimmsParameter.DriftTimeAlignmentFactor);
        }
    }
}
