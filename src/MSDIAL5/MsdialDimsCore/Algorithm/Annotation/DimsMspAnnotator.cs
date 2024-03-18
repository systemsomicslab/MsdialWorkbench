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

            if (omics == TargetOmics.Lipidomics) {
                var ms2Result = LipidMs2Calculator.Calculate(new MSScanMatchQuery(normScan, parameter), reference);
                results.Add(ms2Result);
                if (!ms2Result.IsOtherLipidMatch) {
                    result.Name = string.IsNullOrEmpty(ms2Result.Name) ? reference.Name : ms2Result.Name;
                }
            }
            else {
                var ms2Result = Ms2Calculator.Calculate(new MSScanMatchQuery(normScan, parameter), reference);
                results.Add(ms2Result);
            }

            result.TotalScore = (float)results.SelectMany(res => res.Scores).Average();
            results.ForEach(res => res.Assign(result));

            result.IsReferenceMatched = result.IsPrecursorMzMatch && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && !result.IsReferenceMatched;
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
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
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
