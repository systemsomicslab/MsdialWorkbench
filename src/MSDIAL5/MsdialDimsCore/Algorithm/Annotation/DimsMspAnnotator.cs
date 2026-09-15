using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation
{
    public class DimsMspAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private readonly TargetOmics omics;

        public DimsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey, int priority)
            : base(mspDB.Database, parameter, sourceKey, priority, SourceType.MspDB) {
            this.omics = omics;
            Id = sourceKey;
            ReferObject = mspDB;
            searcher = new MassReferenceSearcher<MoleculeMsReference>(mspDB.Database);
            evaluator = new MsScanMatchResultEvaluator(parameter);
        }

        public string Id { get; }

        private readonly MassReferenceSearcher<MoleculeMsReference> searcher;
        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsScanMatchResult Annotate(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.NormalizedScan, parameter, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.NormalizedScan, parameter, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(IMSProperty property, IMSScanProperty scan, MsRefSearchParameterBase parameter, string sourceKey) {
            var candidates = SearchBound(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, scan, candidate, parameter, sourceKey);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            return CalculateScoreCore(query.Property, query.NormalizedScan, reference, parameter, Key);
        }

        private MsScanMatchResult CalculateScoreCore(IMSProperty property, IMSScanProperty normScan, MoleculeMsReference reference, MsRefSearchParameterBase parameter, string source) {
            var result = new MsScanMatchResult
            {
                Name = reference.Name,
                LibraryID = reference.ScanID,
                InChIKey = reference.InChIKey,
                Source = SourceType.MspDB,
                AnnotatorID = source,
                Priority = Priority,
            };
            var results = new List<IMatchResult>();

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var massResult = MassCalculator.Calculate(new MassMatchQuery(property.PrecursorMz, ms1Tol), reference);
            results.Add(massResult);

            Ms2MatchResult ms2Result;
            if (omics == TargetOmics.Lipidomics) {
                var lipidResult = LipidMs2Calculator.Calculate(new MSScanMatchQuery(normScan, parameter), reference);
                ms2Result = lipidResult;
                if (!lipidResult.IsOtherLipidMatch) {
                    result.Name = string.IsNullOrEmpty(lipidResult.Name) ? reference.Name : lipidResult.Name;
                }
            }
            else {
                ms2Result = Ms2Calculator.Calculate(new MSScanMatchQuery(normScan, parameter), reference);
            }
            results.Add(ms2Result);

            // AVERAGED OVER THE TERMS THAT WERE ACTUALLY COMPARED. When a feature carries no
            // product-ion spectrum -- the ordinary case in direct infusion, where an MS1 survey may
            // be all there is -- both calculators return their Empty, which holds 0 in every
            // spectral field rather than the -1 the scoring functions returned. Averaging that in
            // divided a candidate's score by three: a precursor mass agreeing to within tolerance,
            // worth 1.0 on its own, was published as 0.33.
            //
            // The zeros are fabricated rather than measured, and this is the only annotator that
            // ever counted them. MassAnnotator, MsReferenceScorer and CalculateAnnotatedScoreCore
            // twenty lines below all build a list of the terms they actually computed and average
            // that; this one alone averaged a fixed three. So the fix is not a new convention, it is
            // this site joining the existing one.
            //
            // SpectrumCompared rather than IsSpectrumComparisonPerformed: that predicate is inert
            // here. It infers "a comparison happened" from the fields being non-negative, and
            // Empty's zeros are non-negative, so for an MspDB result it answers true for exactly the
            // candidates this has to exclude. The calculator knows, and says so.
            var measured = results.Where(res => !(res is Ms2MatchResult ms2) || ms2.SpectrumCompared);
            result.TotalScore = (float)measured.SelectMany(res => res.Scores).DefaultIfEmpty().Average();
            results.ForEach(res => res.Assign(result));

            // After the Assign loop, not in the initializer: this annotator's MeasuredTerms arrive
            // through the calculators, so before this point the record says nothing was measured.
            // The evidence cannot be recorded inside the calculators either -- a calculator sees
            // only a query and a reference and cannot know what kind of database the reference came
            // from -- which is why this site needs its own assignment even though the previous
            // commit's term recording reached it for free.
            result.EvidenceSource = AnnotationEvidence.ForDatabaseMatch(result.MeasuredTerms, omics);

            result.IsReferenceMatched = result.IsPrecursorMzMatch && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && !result.IsReferenceMatched;
            // No ValidateCore in this annotator: Ms2MatchResult.Assign has already set
            // IsSpectrumMatch by the time this runs, so the verdict is final here.
            AnnotationEvidence.RecordSpectrumVerdict(result, omics, parameter.MinimumSpectrumMatch);
            return result;
        }

        private static MassMatchCalculator MassCalculator
            => massCalculator ?? (massCalculator = new MassMatchCalculator());
        private static MassMatchCalculator massCalculator;

        private static Ms2MatchCalculator Ms2Calculator
            => ms2Calculator ?? (ms2Calculator = new Ms2MatchCalculator());
        private static Ms2MatchCalculator ms2Calculator;
        
        private static LipidMs2MatchCalculator LipidMs2Calculator
            => lipidMs2Calculator ?? (lipidMs2Calculator = new LipidMs2MatchCalculator());

        private static LipidMs2MatchCalculator lipidMs2Calculator;
        
        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return CalculateAnnotatedScoreCore(result);
        }

        private static double CalculateAnnotatedScoreCore(MsScanMatchResult result) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            // The dot-product getters clamp the not-computed -1 to 0, so testing them cannot detect a
            // candidate that was never compared against a reference spectrum. Ask the match result.
            if (result.IsSpectrumComparisonPerformed)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
            return scores.DefaultIfEmpty().Average();
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            return CalculateSuggestedScoreCore(result);
        }

        private static double CalculateSuggestedScoreCore(MsScanMatchResult result) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return SearchBound(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private IReadOnlyList<MoleculeMsReference> SearchBound(IMSProperty property, double tolerance) {
            return searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(tolerance, property.PrecursorMz)));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

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
