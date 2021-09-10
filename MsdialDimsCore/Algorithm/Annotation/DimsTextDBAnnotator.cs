using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation
{
    public class DimsTextDBAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> referObject;
        private readonly MassReferenceSearcher<MoleculeMsReference> searcher;

        public DimsTextDBAnnotator(MoleculeDataBase textDB, MsRefSearchParameterBase parameter, string id)
            : base(textDB.Database, parameter, id, SourceType.TextDB) {

            referObject = textDB;
            searcher = new MassReferenceSearcher<MoleculeMsReference>(textDB.Database);
        }

        public MsScanMatchResult Annotate(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return SelectTopHit(FindCandidatesCore(query.Property, query.Parameter ?? Parameter), parameter);
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery query) {
            return FindCandidatesCore(query.Property, query.Parameter ?? Parameter)
                .OrderByDescending(result => result.TotalScore)
                .ToList();
        }

        private IEnumerable<MsScanMatchResult> FindCandidatesCore(IMSProperty property, MsRefSearchParameterBase parameter) {
            return SearchCore(property, parameter.Ms1Tolerance)
                .Select(candidate => CalculateScoreCore(property, candidate, parameter));
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return SearchCore(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private IReadOnlyList<MoleculeMsReference> SearchCore(IMSProperty property, double ms1Tolerance) {
            return searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(ms1Tolerance, property.PrecursorMz)));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500) {
                return tolerance;
            }
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500d, 500d + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery query, MoleculeMsReference reference) {
            return CalculateScoreCore(query.Property, reference, query.Parameter ?? Parameter);
        }

        private static MassMatchCalculator MassCalculator
            => massCalculator ?? (massCalculator = new MassMatchCalculator());
        private static MassMatchCalculator massCalculator;

        private MsScanMatchResult CalculateScoreCore(IMSProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var result = new MsScanMatchResult
            {
                Name = reference.Name,
                LibraryID = reference.ScanID,
                InChIKey = reference.InChIKey,
                Source = SourceType.TextDB,
                AnnotatorID = Key,
            };

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var massResult = MassCalculator.Calculate(new MassMatchQuery(property.PrecursorMz, ms1Tol), reference);

            result.TotalScore = (float)massResult.Scores.Average();
            massResult.Assign(result);
            return result;
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfySuggestedConditions(result) && !SatisfyRefMatchedConditions(result);
        }

        public bool IsReferenceMatched(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return SatisfyRefMatchedConditions(result);
        }

        private bool SatisfyRefMatchedConditions(MsScanMatchResult result) {
            return result.IsPrecursorMzMatch;
        }

        private bool SatisfySuggestedConditions(MsScanMatchResult result) {
            return result.IsPrecursorMzMatch;
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Where(result => SatisfySuggestedConditions(result)).ToList();
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Where(result => SatisfyRefMatchedConditions(result)).ToList();
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.DefaultIfEmpty().Argmax(result => result?.TotalScore ?? double.MinValue);
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return referObject.Refer(result);
        }
    }
}
