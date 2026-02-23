using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment;
public class GcmsAlignmentProcessFactory : AlignmentProcessFactory
{
    private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
    private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> _refer;

    public MsdialGcmsParameter GcmsParameter { get; }
    public IProgress<int>? Progress { get; set; }

    public GcmsAlignmentProcessFactory(IMsdialDataStorage<MsdialGcmsParameter> storage) : base(storage.Parameter, storage.IupacDatabase) {
        GcmsParameter = storage.Parameter;
        _evaluator = FacadeMatchResultEvaluator.FromDataBases(storage.DataBases);
        _refer = storage.DataBaseMapper;
    }

    public override IAlignmentRefiner CreateAlignmentRefiner() {
        return new GcmsAlignmentRefiner(GcmsParameter, Iupac, _evaluator);
    }

    public override DataAccessor CreateDataAccessor() {
        return new GcmsDataAccessor(GcmsParameter);
    }

    public override IGapFiller CreateGapFiller() {
        switch (GcmsParameter.AlignmentIndexType) {
            case Common.Enum.AlignmentIndexType.RT:
                return new GcmsRTGapFiller(GcmsParameter);
            case Common.Enum.AlignmentIndexType.RI:
            default:
                return new GcmsRIGapFiller(GcmsParameter);
        }
    }

    public override PeakAligner CreatePeakAligner() {
        return new PeakAligner(this, Progress);
    }

    public override IPeakJoiner CreatePeakJoiner() {
        switch (GcmsParameter.AlignmentIndexType) {
            case Common.Enum.AlignmentIndexType.RT:
                return GcmsPeakJoiner.CreateRTJoiner(
                    GcmsParameter,
                    _evaluator,
                    _refer,
                    new GcmsDataAccessor(GcmsParameter),
                    Progress);
            case Common.Enum.AlignmentIndexType.RI:
            default:
                return GcmsPeakJoiner.CreateRIJoiner(
                    GcmsParameter.RetentionIndexAlignmentTolerance,
                    GcmsParameter,
                    _evaluator,
                    _refer,
                    new GcmsDataAccessor(GcmsParameter),
                    Progress);
        }
    }

    public PeakQuantCalculation CreatePeakQuantCalculation(IDataProviderFactory<AnalysisFileBean> providerFactory) {
        var accessor = new GcmsDataAccessor(GcmsParameter);
        var gapFiller = (GcmsGapFiller)CreateGapFiller();
        return new PeakQuantCalculation(gapFiller, accessor, providerFactory, GcmsParameter);
    }
}
