using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.Algorithm.Annotation.LipidRt;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using System;

namespace CompMs.MsdialLcMsApi.Algorithm.Alignment;

public class LcmsAlignmentProcessFactory : AlignmentProcessFactory
{
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IMsdialDataStorage<MsdialLcmsParameter> _storage;

    public MsdialLcmsParameter LcmsParameter { get; }
    public IProgress<int>? Progress { get; set; }
    public bool SkipIonAbundanceCorrelationLinks { get; set; }
    public AlignmentRetentionTimeCorrectionCollection? AlignmentRtCorrection { get; set; }

    public LcmsAlignmentProcessFactory(
        IMsdialDataStorage<MsdialLcmsParameter> storage, 
        IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(storage.Parameter, storage.IupacDatabase) {
        LcmsParameter = storage.Parameter;
        _storage = storage;
        _evaluator = evaluator ?? throw new ArgumentNullException(nameof(evaluator));
    }

    public override IAlignmentRefiner CreateAlignmentRefiner() {
        return new LcmsAlignmentRefiner(LcmsParameter, Iupac, _evaluator, Progress, SkipIonAbundanceCorrelationLinks);
    }

    public override DataAccessor CreateDataAccessor() {
        return new LcmsDataAccessor(LcmsParameter, AlignmentRtCorrection);
    }

    public override IGapFiller CreateGapFiller() {
        return new LcmsGapFiller(LcmsParameter, AlignmentRtCorrection);
    }

    public override PeakAligner CreatePeakAligner() {
        // The libraries are read when the aligner is made rather than when the factory is, so the aligner sees the
        // databases the project has now.
        return new PeakAligner(this, Progress) {
            PredictedLibraryAnnotators = LipidRtInputBuilder.PredictedLibraryAnnotators(_storage.DataBases),
        };
    }

    public override IPeakJoiner CreatePeakJoiner() {
        return new LcmsPeakJoiner(LcmsParameter.AlignmentBaseParam, new LcmsDataAccessor(LcmsParameter, AlignmentRtCorrection), _evaluator, Progress);
    }
}
