﻿using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Alignment;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.Raw.Abstractions;

namespace CompMs.MsdialDimsCore.Algorithm.Alignment
{
    public class DimsAlignmentProcessFactory : AlignmentProcessFactory
    {
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsdialDimsParameter DimsParameter { get; }

        public DimsAlignmentProcessFactory(IMsdialDataStorage<MsdialDimsParameter> storage, IMatchResultEvaluator<MsScanMatchResult> evaluator, IDataProviderFactory<AnalysisFileBean> providerFactory) : base(storage.Parameter, storage.IupacDatabase, providerFactory) {
            DimsParameter = storage.Parameter;
            this.evaluator = evaluator;
        }

        public override IAlignmentRefiner CreateAlignmentRefiner() {
            return new DimsAlignmentRefiner(DimsParameter, Iupac, evaluator);
        }

        public override DataAccessor CreateDataAccessor() {
            return new DimsDataAccessor();
        }

        public override GapFiller CreateGapFiller() {
            return new DimsGapFiller(DimsParameter);
        }

        public override PeakAligner CreatePeakAligner() {
            return new PeakAligner(this, ProviderFactory, null);
        }

        public override IPeakJoiner CreatePeakJoiner() {
            return new DimsPeakJoiner(DimsParameter.Ms1AlignmentTolerance, DimsParameter.Ms1AlignmentFactor);
        }
    }
}
