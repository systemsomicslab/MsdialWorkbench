using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
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
            Scorer = scorer;
            Parameter = parameter;
        }

        private readonly ILipidGenerator lipidGenerator;
        private readonly EadLipidDatabase EadLipidDatabase;
        private readonly IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult> Scorer;
        private readonly MsRefSearchParameterBase Parameter;

        public MsScanMatchResult Annotate((IAnnotationQuery, MoleculeMsReference) query) {
            return FindCandidates(query).FirstOrDefault();
        }

        public MsScanMatchResult CalculateScore((IAnnotationQuery, MoleculeMsReference) query, MoleculeMsReference reference) {
            return Scorer.Score(query.Item1, reference);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfySuggestedConditions(result, parameter)).ToList();
        }

        public List<MsScanMatchResult> FindCandidates((IAnnotationQuery, MoleculeMsReference) query) {
            return Search(query)
                .Select(candidate => Scorer.Score(query.Item1, candidate))
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfySuggestedConditions(result, parameter ?? Parameter);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfyRefMatchedConditions(result, parameter ?? Parameter);
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
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfyRefMatchedConditions(result, parameter ?? Parameter)).ToList();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        private ILipid ConvertToLipid(IMoleculeProperty molecule) {
            return FacadeLipidParser.Default.Parse(molecule.Name);
        }
    }
}
