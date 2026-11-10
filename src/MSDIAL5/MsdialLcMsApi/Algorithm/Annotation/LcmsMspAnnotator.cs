using CompMs.Common.Algorithm.Scoring;
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
        /// <param name="allowsSuggestions">False keeps only reference matches; see <see cref="AllowsSuggestions"/>.</param>
        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID, int priority, bool allowsSuggestions = true)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            Id = annotatorID;
            ReferObject = mspDB;
            scorer = new MsReferenceScorer(annotatorID, priority, omics, SourceType.MspDB, CollisionType.CID, true, mspDB.DataBaseSource);
            evaluator = new MsScanMatchResultEvaluator(parameter);
            AllowsSuggestions = allowsSuggestions;
        }

        /// <param name="allowsSuggestions">False keeps only reference matches; see <see cref="AllowsSuggestions"/>.</param>
        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, CollisionType type, string annotatorID, int priority, bool allowsSuggestions = true)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            Id = annotatorID;
            ReferObject = mspDB;
            scorer = new MsReferenceScorer(annotatorID, priority, omics, SourceType.MspDB, type, true, mspDB.DataBaseSource);
            evaluator = new MsScanMatchResultEvaluator(parameter);
            AllowsSuggestions = allowsSuggestions;
        }

        public string Id { get; }

        /// <summary>
        /// Whether a candidate this annotator did not accept may stand as a suggestion.
        /// </summary>
        /// <remarks>
        /// True unless the caller says otherwise, which is what every annotator did before. False
        /// leaves the reference match exactly as it was and takes away only the suggestion: the
        /// precursor-only candidate of a feature with no product-ion spectrum, and the candidate whose
        /// spectrum was compared and fell short. FilterByThreshold keeps a result only when one of the
        /// two verdicts holds, so such a candidate is then not stored at all. The Console sets it per
        /// annotator from its settings table, for an in-silico library whose mass windows hold
        /// hundreds of generated records each.
        ///
        /// Not part of the restoration key, so an annotator rebuilt when a project is reopened
        /// suggests again. The stored results are unaffected; only a re-annotation would differ.
        /// </remarks>
        public bool AllowsSuggestions { get; }

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
            // The peak and the reference are no longer in scope here, so the requirement is
            // read from the record the scorer just wrote from those same two values.
            var rtRequirementMet = RetentionMatchPolicy.RetentionTimeRequirementMet(query.Parameter.IsUseTimeForAnnotationScoring, result);
            result.IsReferenceMatched = result.IsPrecursorMzMatch && rtRequirementMet && result.IsSpectrumMatch;
            // No retention clause on the suggestion. "Use retention time for SCORING" must not
            // reject a candidate -- that is what "use retention time for FILTERING" is for, and
            // the two settings mean different things. Because both verdicts used to share the
            // clause and FilterByThreshold is their disjunction, a retention-time disagreement
            // deleted the candidate from the stored results outright. It now costs the reference
            // match and leaves the precursor-only suggestion standing, which is what the evidence
            // supports. This is the shape MassAnnotator and DimsMspAnnotator already had.
            result.IsAnnotationSuggested = AllowsSuggestions && result.IsPrecursorMzMatch && !result.IsReferenceMatched;
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
