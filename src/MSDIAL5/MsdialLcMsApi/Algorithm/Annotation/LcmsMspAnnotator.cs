using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation
{
    public class LcmsMspAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID, int priority)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            Id = annotatorID;
            ReferObject = mspDB;
            scorer = new MsReferenceScorer(annotatorID, priority, omics, SourceType.MspDB, CollisionType.CID, true);
            evaluator = new MsScanMatchResultEvaluator(parameter);
        }

        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, CollisionType type, string annotatorID, int priority)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            Id = annotatorID;
            ReferObject = mspDB;
            scorer = new MsReferenceScorer(annotatorID, priority, omics, SourceType.MspDB, type, true);
            evaluator = new MsScanMatchResultEvaluator(parameter);
        }

        public string Id { get; }
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;
        private readonly IReferenceScorer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> scorer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsScanMatchResult Annotate(IAnnotationQuery<MsScanMatchResult> query) {
            return FindCandidatesCore(query, query.Parameter ?? Parameter).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            return FindCandidatesCore(query, query.Parameter ?? Parameter).ToList();
        }

        private IEnumerable<MsScanMatchResult> FindCandidatesCore(IAnnotationQuery<MsScanMatchResult> query, MsRefSearchParameterBase parameter) {
            return SearchCore(query.Property, parameter)
                .Select(candidate => CalculateScore(query, candidate))
                .OrderByDescending(result => result.TotalScore);
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var result = scorer.Score(query, reference);
            result.IsReferenceMatched = result.IsPrecursorMzMatch && (!query.Parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && (!query.Parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && !result.IsReferenceMatched;
            return result;
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery<MsScanMatchResult> query) {
            return SearchCore(query.Property, query.Parameter ?? Parameter).ToList();
        }

        private IEnumerable<MoleculeMsReference> SearchCore(IMSProperty property, MsRefSearchParameterBase parameter) {
            if (parameter.IsUseTimeForAnnotationFiltering) {
                return SearcherWithRt.Search(MSSearchQuery.CreateMassRtQuery(property.PrecursorMz, MolecularFormulaUtility.FixMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz), property.ChromXs.RT.Value, parameter.RtTolerance));
            }
            else {
                return Searcher.Search(new MassSearchQuery(property.PrecursorMz, MolecularFormulaUtility.FixMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz)));
            }
        }

        private MassReferenceSearcher<MoleculeMsReference> Searcher
            => searcher ?? (searcher = new MassReferenceSearcher<MoleculeMsReference>(db));
        private MassReferenceSearcher<MoleculeMsReference> searcher;

        private MassRtReferenceSearcher<MoleculeMsReference> SearcherWithRt
            => searcherWithRt ?? (searcherWithRt = new MassRtReferenceSearcher<MoleculeMsReference>(db));
        private MassRtReferenceSearcher<MoleculeMsReference> searcherWithRt;

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectTopHit(results);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectReferenceMatchResults(results);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return evaluator.IsReferenceMatched(result);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return evaluator.IsAnnotationSuggested(result);
        }
    }
}
