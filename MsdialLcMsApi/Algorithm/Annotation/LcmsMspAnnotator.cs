using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
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
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;

        public LcmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string annotatorID, int priority)
            : base(mspDB.Database, parameter, annotatorID, priority, SourceType.MspDB) {
            ReferObject = mspDB;
            scorer = new LcmsMspReferenceScorer(annotatorID, priority, omics);
        }

        private readonly IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> scorer;

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

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfySuggestedConditions(result, parameter)).ToList();
        }

        private static bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && result.IsSpectrumMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch);
        }

        private static bool SatisfySuggestedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfyRefMatchedConditions(result, parameter)).ToList();
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfyRefMatchedConditions(result, parameter ?? Parameter);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return SatisfySuggestedConditions(result, parameter) && !SatisfyRefMatchedConditions(result, parameter);
        }
    }
}
