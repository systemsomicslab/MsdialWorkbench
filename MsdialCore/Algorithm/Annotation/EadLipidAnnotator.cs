using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class EadLipidAnnotator : IAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult>
    {
        public string Key { get; } = "EadLipid";

        public EadLipidAnnotator(string dbPath, IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> scorer, MsRefSearchParameterBase parameter) {
            lipidGenerator = new LipidGenerator();
            EadLipidDatabase = new EadLipidDatabase(dbPath, Key);
            this.scorer = scorer;
            Parameter = parameter;
            evaluator = MsScanMatchResultEvaluator.CreateEvaluatorWithSpectrum();
        }

        private readonly ILipidGenerator lipidGenerator;
        private readonly EadLipidDatabase EadLipidDatabase;
        private readonly IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> scorer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;
        private readonly MsRefSearchParameterBase Parameter;

        public MsScanMatchResult Annotate((IAnnotationQuery, MoleculeMsReference) query) {
            return FindCandidates(query).FirstOrDefault();
        }

        public MsScanMatchResult CalculateScore((IAnnotationQuery, MoleculeMsReference) query, MoleculeMsReference reference) {
            return scorer.Score(query.Item1, reference);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return evaluator.FilterByThreshold(results, parameter ?? Parameter);
        }

        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery, MoleculeMsReference) query) {
            return Search(query)
                .Select(candidate => scorer.Score(query.Item1, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return evaluator.IsAnnotationSuggested(result, parameter ?? Parameter);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return evaluator.IsReferenceMatched(result, parameter ?? Parameter);
        }

        private static bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch
                && result.IsSpectrumMatch
                && (!parameter.IsUseTimeForAnnotationFiltering || result.IsRtMatch);
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return EadLipidDatabase.Refer(result);
        }

        public List<MoleculeMsReference> Search((IAnnotationQuery, MoleculeMsReference) query) {
            var reference = query.Item2;
            var lipid = ConvertToLipid(reference);
            if (lipid is null) {
                return new List<MoleculeMsReference>(0);
            }

            var lipids = lipid.Generate(lipidGenerator);
            var references = lipids.Select(l => EadLipidDatabase.Generate(l, reference.AdductType, reference)).ToList();
            EadLipidDatabase.Register(references);
            return references;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return evaluator.SelectReferenceMatchResults(results, parameter ?? Parameter);
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return evaluator.SelectTopHit(results, parameter ?? Parameter);
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }
    }
}
