using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialGcMsApi.Algorithm.Alignment
{
    public class GcmsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsdialGcmsParameter GcmsParameter { get; }
        public List<AnalysisFileBean> Files { get; }
        public List<MoleculeMsReference> MspDB { get; }
        public Action<int> ReportAction { get; set; }

        public GcmsAlignmentProcessFactory(List<AnalysisFileBean> files, IMsdialDataStorage<MsdialGcmsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator) : base(storage.Parameter, storage.IupacDatabase) {
            Files = files;
            GcmsParameter = storage.Parameter;
            this.evaluator = evaluator ?? throw new System.ArgumentNullException(nameof(evaluator));
            MspDB = storage.MspDB;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new GcmsAlignmentRefiner(GcmsParameter, Iupac, evaluator);
        }

        public override DataAccessor CreateDataAccessor() {
            return new GcmsDataAccessor(GcmsParameter);
        }

        public override GapFiller CreateGapFiller() {
            switch (GcmsParameter.AlignmentIndexType) {
                case Common.Enum.AlignmentIndexType.RT:
                    return new GcmsRTGapFiller(Files, MspDB, GcmsParameter);
                case Common.Enum.AlignmentIndexType.RI:
                default:
                    return new GcmsRIGapFiller(Files, MspDB, GcmsParameter);
            }
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this, ReportAction);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            switch (GcmsParameter.AlignmentIndexType) {
                case Common.Enum.AlignmentIndexType.RT:
                    return GcmsPeakJoiner.CreateRTJoiner(
                        GcmsParameter.RiCompoundType,
                        GcmsParameter.MspSearchParam,
                        GcmsParameter.AlignmentBaseParam);
                case Common.Enum.AlignmentIndexType.RI:
                default:
                    return GcmsPeakJoiner.CreateRIJoiner(
                        GcmsParameter.RiCompoundType,
                        GcmsParameter.MspSearchParam,
                        GcmsParameter.RetentionIndexAlignmentTolerance,
                        GcmsParameter.AlignmentBaseParam);
            }
        }
    }
}
