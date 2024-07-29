using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public sealed class EadLipidAnnotator : 
        ISerializableAnnotator<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase>, 
        IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>, 
        IMatchResultEvaluator<MsScanMatchResult>
    {
        private readonly ILipidGenerator _lipidGenerator;
        private readonly EadLipidDatabase _lipidDatabase;
        private readonly IReferenceScorer<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult> _scorer;
        private readonly IMatchResultEvaluator<MsScanMatchResult> _evaluator;
        private readonly MsRefSearchParameterBase _parameter;
        private readonly LipidDescription _description;

        public EadLipidAnnotator(EadLipidDatabase db, string id, int priority, MsRefSearchParameterBase parameter) {
            _lipidGenerator = new DGTSLipidGeneratorDecorator(new LipidGenerator(new TotalChainVariationGenerator(chainGenerator: new Omega3nChainGenerator(), minLength: 12)));
            _description = LipidDescription.Class | LipidDescription.Chain | LipidDescription.SnPosition | LipidDescription.DoubleBondPosition;

            Id = id ?? throw new ArgumentNullException(nameof(id));
            Priority = priority;
            _lipidDatabase = db ?? throw new ArgumentNullException(nameof(db));

            switch (db.Source) {
                case DataBaseSource.OadLipid:
                    _lipidGenerator = new DGTSLipidGeneratorDecorator(new LipidGenerator(new OadChainVariationGenerator(chainGenerator: new Omega3nChainNoOxiVariationGenerator(), minLength: 12)));
                    _description = LipidDescription.Class | LipidDescription.Chain | LipidDescription.DoubleBondPosition;
                    _scorer = new MsReferenceScorer(id, priority, TargetOmics.Lipidomics, SourceType.GeneratedLipid, CollisionType.OAD, useMs2: true);
                    break;
                case DataBaseSource.EidLipid:
                    _scorer = new MsReferenceScorer(id, priority, TargetOmics.Lipidomics, SourceType.GeneratedLipid, CollisionType.EID, useMs2: true);
                    break;
                case DataBaseSource.EieioLipid:
                    _scorer = new MsReferenceScorer(id, priority, TargetOmics.Lipidomics, SourceType.GeneratedLipid, CollisionType.EIEIO, useMs2: true);
                    break;
            }
            _parameter = parameter ?? throw new ArgumentNullException(nameof(parameter));
            _evaluator = new MsScanMatchResultEvaluator(_parameter);
        }

        public string Id { get; }

        [Obsolete]
        public string Key => Id;
        public int Priority { get; }

        public MsScanMatchResult Annotate((IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference) query) {
            return FindCandidates(query).FirstOrDefault();
        }

        public MsScanMatchResult CalculateScore((IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference) query, MoleculeMsReference reference) {
            var result = _scorer.Score(query.Item1, reference);
            var parameter = query.Item1.Parameter;
            result.IsReferenceMatched = result.IsPrecursorMzMatch && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch) && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch) && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch) && !result.IsReferenceMatched;
            return result;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return _evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference) query) {
            return Search(query)
                .Select(candidate => CalculateScore(query, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return _evaluator.IsAnnotationSuggested(result);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return _evaluator.IsReferenceMatched(result);
        }

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return ((IMatchResultRefer<MoleculeMsReference, MsScanMatchResult>)_lipidDatabase).Refer(result);
        }

        public List<MoleculeMsReference> Search((IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference) query) {
            var reference = query.Item2;
            var lipid = ConvertToLipid(reference);
            if (lipid is null) {
                return new List<MoleculeMsReference>(0);
            }
            var lipids = GenerateLipid(lipid).Where(lipid_ => lipid_.Description.HasFlag(_description));
            var references = _lipidDatabase.Generates(lipids, lipid, reference.AdductType, reference).Where(r => !(r is null)).ToList();
            return references;
        }

        private IEnumerable<ILipid> GenerateLipid(ILipid lipid) {
            if (_lipidDatabase.Source == DataBaseSource.OadLipid) {
                return _lipidGenerator.GenerateUntil(lipid, l => l.AnnotationLevel >= 1).Prepend(lipid);
            }
            return _lipidGenerator.GenerateUntil(lipid, _ => true).Prepend(lipid);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return _evaluator.SelectReferenceMatchResults(results);
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return _evaluator.SelectTopHit(results);
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }

        public IReferRestorationKey<(IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference), MoleculeMsReference, MsScanMatchResult, EadLipidDatabase> Save() {
            return new EadLipidDatabaseRestorationKey(Key, Priority, _parameter, SourceType.GeneratedLipid);
        }
    }
}
