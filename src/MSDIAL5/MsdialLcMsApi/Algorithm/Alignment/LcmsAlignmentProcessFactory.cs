using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public class LcmsAlignmentProcessFactory : AlignmentProcessFactory
{
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;

    public MsdialLcmsParameter LcmsParameter { get; }
    public IProgress<int>? Progress { get; set; }

    public LcmsAlignmentProcessFactory(
        IMsdialDataStorage<MsdialLcmsParameter> storage, 
        IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(storage.Parameter, storage.IupacDatabase) {
        LcmsParameter = storage.Parameter;
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public override IAlignmentRefiner CreateAlignmentRefiner() {
        return new LcmsAlignmentRefiner(LcmsParameter, Iupac, _evaluator, Progress);
    }

    public override DataAccessor CreateDataAccessor() {
        return new LcmsDataAccessor(LcmsParameter);
    }

    public override GapFiller CreateGapFiller() {
        return new LcmsGapFiller(LcmsParameter);
    }

    public override PeakAligner CreatePeakAligner() {
        return new PeakAligner(this, Progress);
    }

    public override IPeakJoiner CreatePeakJoiner() {
        return new LcmsPeakJoiner(
            LcmsParameter.RetentionTimeAlignmentTolerance, LcmsParameter.RetentionTimeAlignmentFactor,
            LcmsParameter.Ms1AlignmentTolerance, LcmsParameter.Ms1AlignmentFactor,
            Progress
        );
    }
}
