using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class EadLipidAnnotator : ISerializableAnnotator<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, IMatchResultEvaluator<MsScanMatchResult>
    {
        public EadLipidAnnotator(EadLipidDatabase db, string id, int priority, MsRefSearchParameterBase parameter) {
            lipidGenerator = new LipidGenerator(new TotalChainVariationGenerator(chainGenerator: new Omega3nChainGenerator(), minLength: 6));
            Key = id ?? throw new System.ArgumentNullException(nameof(id));
            this.priority = priority;
            EadLipidDatabase = db ?? throw new System.ArgumentNullException(nameof(db));
            scorer = new MsReferenceScorer(id, priority, TargetOmics.Lipidomics, SourceType.GeneratedLipid, CollisionType.EAD);
            Parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            evaluator = MsScanMatchResultEvaluator.CreateEvaluator(Parameter);
        }

        public string Key { get; }

        private readonly ILipidGenerator lipidGenerator;
        private readonly EadLipidDatabase EadLipidDatabase;
        private readonly int priority;
        private readonly IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> scorer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;
        private readonly MsRefSearchParameterBase Parameter;

        public MsScanMatchResult Annotate((IAnnotationQuery, MoleculeMsReference) query) {
            return FindCandidates(query).FirstOrDefault();
        }

        public MsScanMatchResult CalculateScore((IAnnotationQuery, MoleculeMsReference) query, MoleculeMsReference reference) {
            var result = scorer.Score(query.Item1, reference);
            var parameter = query.Item1.Parameter;
            result.IsReferenceMatched = result.IsPrecursorMzMatch && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch) && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch) && !result.IsReferenceMatched;
            return result;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery, MoleculeMsReference) query) {
            return Search(query)
                .Select(candidate => scorer.Score(query.Item1, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return evaluator.IsAnnotationSuggested(result);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return evaluator.IsReferenceMatched(result);
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

            var lipids = GenerateLipid(lipid, lipidGenerator);

            var references = lipids.Select(l => EadLipidDatabase.Generate(l, reference.AdductType, reference)).Where(reference_ => reference_ != null).ToList();
            EadLipidDatabase.Register(references);
            return references;
        }

        private static IEnumerable<ILipid> GenerateLipid(ILipid lipid, ILipidGenerator lipidGenerator) {
            return lipid.Generate(lipidGenerator).SelectMany(l => GenerateLipid(l, lipidGenerator)).Prepend(lipid);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectReferenceMatchResults(results);
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectTopHit(results);
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }

        public IReferRestorationKey<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Save() {
            return new EadLipidDatabaseRestorationKey(Key, priority, Parameter, SourceType.GeneratedLipid);
        }
    }
}
