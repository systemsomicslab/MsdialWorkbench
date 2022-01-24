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
    public class LcmsMspAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID, int priority)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            ReferObject = mspDB;
            scorer = new MsReferenceScorer(annotatorID, priority, omics, SourceType.MspDB, CollisionType.HCD);
            evaluator = MsScanMatchResultEvaluator.CreateEvaluatorWithSpectrum(parameter);
        }

        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;
        private readonly IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> scorer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsScanMatchResult Annotate(IAnnotationQuery query) {
            return FindCandidatesCore(query, Parameter).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery query) {
            return FindCandidatesCore(query, Parameter);
        }

        private List<MsScanMatchResult> FindCandidatesCore(IAnnotationQuery query, MsRefSearchParameterBase parameter) {
            return SearchCore(query.Property, parameter)
                .Select(candidate => scorer.Score(query, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery query, MoleculeMsReference reference) {
            return scorer.Score(query, reference);
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery query) {
            return SearchCore(query.Property, query.Parameter ?? Parameter).ToList();
        }

        private IEnumerable<MoleculeMsReference> SearchCore(IMSProperty property, MsRefSearchParameterBase parameter) {
            if (parameter.IsUseTimeForAnnotationFiltering) {
                return SearcherWithRt.Search(MSSearchQuery.CreateMassRtQuery(property.PrecursorMz, MolecularFormulaUtility.CalculateMassToleranceBasedOn500Da(parameter.Ms1Tolerance, property.PrecursorMz), property.ChromXs.RT.Value, parameter.RtTolerance));
            }
            else {
                return Searcher.Search(new MassSearchQuery(property.PrecursorMz, MolecularFormulaUtility.CalculateMassToleranceBasedOn500Da(parameter.Ms1Tolerance, property.PrecursorMz)));
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
