using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation
{
    public class LcmsTextDBAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private static readonly IComparer<IMSProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.RTComparer);

        public LcmsTextDBAnnotator(MoleculeDataBase textDB, MsRefSearchParameterBase parameter, string annotatorID, int priority)
            : base(textDB.Database, parameter, annotatorID, priority, SourceType.TextDB) {
            Id = annotatorID;
            this.db.Sort(comparer);
            this.ReferObject = textDB;
            Evaluator = new MsScanMatchResultEvaluator(parameter);
        }

        public string Id { get; }

        private readonly IMatchResultEvaluator<MsScanMatchResult> Evaluator;

        public MsScanMatchResult Annotate(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Isotopes, parameter, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Isotopes, parameter, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, string annotatorID) {

            var candidates = parameter.IsUseTimeForAnnotationFiltering
                ? SearchWithRtCore(property, parameter.Ms1Tolerance, parameter.RtTolerance)
                : SearchCore(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, isotopes, candidate, candidate.IsotopicPeaks, parameter, annotatorID);
                ValidateCore(result, property, candidate, parameter);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            var result = CalculateScoreCore(query.Property, query.Isotopes, reference, reference.IsotopicPeaks, parameter, Key);
            ValidateCore(result, query.Property, reference, parameter);
            return result;
        }

        private MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, string sourceKey) {

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                AcurateMassSimilarity = (float)ms1Similarity, IsotopeSimilarity = (float)isotopeSimilarity,
                Source = SourceType.TextDB, AnnotatorID = sourceKey, Priority = Priority,
                // No Spectrum bit: a text database holds no reference spectrum, so nothing here
                // opens one. That is the fact this record is meant to make visible.
                MeasuredTerms = MeasuredTerms.None
                    .WithComparedValues(MeasuredTerms.AccurateMass, property.PrecursorMz, reference.PrecursorMz)
                    .With(MeasuredTerms.Isotope, isotopeSimilarity),
            };
            if (parameter.IsUseTimeForAnnotationScoring) {
                // Guarded overload. It returns the -1 not-computed sentinel when either side
                // carries no usable value, which is what keeps a reference with no retention
                // time out of the score average below. The `RtSimilarity >= 0` test there was
                // always written for this sentinel; it simply never received one, because the
                // three-argument overload scores whatever it is handed. See RetentionMatchPolicy.
                var rtSimilarity = MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance, out _);
                result.RtSimilarity = (float)rtSimilarity;
                result.MeasuredTerms = result.MeasuredTerms.WithComparedValues(
                    MeasuredTerms.RetentionTime, property.ChromXs.RT.Value, reference.ChromXs.RT.Value);
            }
            // A text database holds no reference spectrum, so nothing here opened one and the
            // evidence is the precursor mass (with whatever time or CCS terms the mode adds).
            // ValidateBase then sets IsReferenceMatched from those alone, which is what makes this
            // record necessary: without it an export cannot separate these names from MS/MS
            // reference matches.
            result.EvidenceSource = AnnotationEvidenceSource.PrecursorOnly;
            result.TotalScore = (float)CalculateTotalScoreCore(result, parameter);

            return result;
        }

        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateTotalScoreCore(result, parameter);
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateTotalScoreCore(result, parameter);
        }

        private static double CalculateTotalScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject { get; }
        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return parameter.IsUseTimeForAnnotationFiltering
                ? SearchWithRtCore(query.Property, parameter.Ms1Tolerance, parameter.RtTolerance).ToList()
                : SearchCore(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private MassReferenceSearcher<MoleculeMsReference> Searcher
            => searcher ?? (searcher = new MassReferenceSearcher<MoleculeMsReference>(db));
        private MassReferenceSearcher<MoleculeMsReference> searcher;
        private IReadOnlyList<MoleculeMsReference> SearchCore(IMSProperty property, double massTolerance) {
            return Searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz)));
        }

        private MassRtReferenceSearcher<MoleculeMsReference> SearcherWithRt
            => searcherWithRt ?? (searcherWithRt = new MassRtReferenceSearcher<MoleculeMsReference>(db));
        private MassRtReferenceSearcher<MoleculeMsReference> searcherWithRt;
        private IReadOnlyList<MoleculeMsReference> SearchWithRtCore(IMSProperty property, double massTolerance, double rtTolerance) {
            return SearcherWithRt.Search(MSSearchQuery.CreateMassRtQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.ChromXs.RT.Value, rtTolerance));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            ValidateCore(result, query.Property, reference, parameter);
        }

       // private static readonly double MsdialRtMatchThreshold = 0.5;
        private static void ValidateCore(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            result.IsRtMatch = RetentionMatchPolicy.IsRetentionTimeMatch(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance);

            // A retention-time-anchored text database is exactly where mixed entries occur,
            // and this annotator never sets IsAnnotationSuggested: failing the requirement
            // here would erase the row rather than lower it.
            result.IsReferenceMatched = result.IsPrecursorMzMatch
                && RetentionMatchPolicy.RetentionTimeRequirementMet(
                    parameter.IsUseTimeForAnnotationScoring, property.ChromXs.RT.Value, reference.ChromXs.RT.Value, result.IsRtMatch);
            // This annotator had no suggestion path at all, so a retention-time disagreement left
            // both verdicts false and the candidate vanished -- on exactly the path a
            // retention-time-anchored text database is used for. A text database row is
            // precursor-only evidence by construction, which is what a suggestion means.
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && !result.IsReferenceMatched;
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return Evaluator.SelectTopHit(results);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return Evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return Evaluator.SelectReferenceMatchResults(results);
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return Evaluator.IsReferenceMatched(result);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return Evaluator.IsAnnotationSuggested(result);
        }
    }
}
