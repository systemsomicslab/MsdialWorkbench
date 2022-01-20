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
            lipidGenerator = new LipidGenerator();
            Key = id ?? throw new System.ArgumentNullException(nameof(id));
            this.priority = priority;
            EadLipidDatabase = db ?? throw new System.ArgumentNullException(nameof(db));
            scorer = new MsReferenceScorer(id, priority, TargetOmics.Lipidomics, SourceType.GeneratedLipid, CollisionType.EAD);
            Parameter = parameter ?? throw new System.ArgumentNullException(nameof(parameter));
            evaluator = MsScanMatchResultEvaluator.CreateEvaluatorWithSpectrum();
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
            return scorer.Score(query.Item1, reference);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            return evaluator.FilterByThreshold(results, parameter ?? Parameter);
        }

        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery, MoleculeMsReference) query) {
            return Search(query)
                .Select(candidate => scorer.Score(query.Item1, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return evaluator.IsAnnotationSuggested(result, parameter ?? Parameter);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return evaluator.IsReferenceMatched(result, parameter ?? Parameter);
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
            var references = lipids.Select(l => EadLipidDatabase.Generate(l, reference.AdductType, reference)).Where(reference_ => reference_ != null).ToList();
            EadLipidDatabase.Register(references);
            return references;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            return evaluator.SelectReferenceMatchResults(results, parameter ?? Parameter);
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter) {
            return evaluator.SelectTopHit(results, parameter ?? Parameter);
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }

        public IReferRestorationKey<(IAnnotationQuery, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Save() {
            return new EadLipidDatabaseRestorationKey(Key, priority, Parameter, SourceType.GeneratedLipid);
        }
    }
}
