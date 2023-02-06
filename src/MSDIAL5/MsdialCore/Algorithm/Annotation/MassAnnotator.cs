using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class MassAnnotator : ISerializableAnnotator<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private readonly TargetOmics omics;
        private readonly SourceType source;
        private readonly string sourceKey;

        public string Id => sourceKey;
        public MsRefSearchParameterBase Parameter { get; }
        public int Priority { get; }

        public MassAnnotator(
            MoleculeDataBase db,
            MsRefSearchParameterBase parameter,
            TargetOmics omics,
            SourceType source,
            string sourceKey,
            int priority) {

            this.Parameter = parameter;
            this.omics = omics;
            this.source = source;
            this.sourceKey = sourceKey;
            Priority = priority;
            ReferObject = db;
            searcher = new MassReferenceSearcher<MoleculeMsReference>(db.Database);
            evaluator = new MsScanMatchResultEvaluator(Parameter);
        }

        private readonly IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject;
        private readonly MassReferenceSearcher<MoleculeMsReference> searcher;
        private readonly IMatchResultEvaluator<MsScanMatchResult> evaluator;

        public MsScanMatchResult Annotate(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.NormalizedScan, query.Isotopes, parameter, omics, source, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.NormalizedScan, query.Isotopes, parameter, omics, source, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(
            IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, SourceType source, string sourceKey) {

            var candidates = SearchBound(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, scan, isotopes, candidate, candidate.IsotopicPeaks, parameter, omics, source, sourceKey);
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            var normScan = query.NormalizedScan;
            var result = CalculateScoreCore(query.Property, normScan, query.Isotopes, reference, reference.IsotopicPeaks, parameter, omics, source, Key);
            ValidateCore(result, query.Property, normScan, reference, parameter, omics);
            return result;
        }

        private MsScanMatchResult CalculateScoreCore(
            IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, SourceType source, string sourceKey) {

            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var matchedPeaksScores = omics == TargetOmics.Lipidomics
                ? MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd)
                : MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                WeightedDotProduct = (float)weightedDotProduct, SimpleDotProduct = (float)simpleDotProduct, ReverseDotProduct = (float)reverseDotProduct,
                MatchedPeaksPercentage = (float)matchedPeaksScores[0], MatchedPeaksCount = (float)matchedPeaksScores[1],
                AcurateMassSimilarity = (float)ms1Similarity, IsotopeSimilarity = (float)isotopeSimilarity,
                Source = source, AnnotatorID = sourceKey, Priority = Priority,
            };
            result.TotalScore = (float)CalculateAnnotatedScoreCore(result, parameter);

            return result;
        }

        public double CalculateAnnotatedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateAnnotatedScoreCore(result, parameter);
        }

        private static double CalculateAnnotatedScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public double CalculateSuggestedScore(MsScanMatchResult result, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return CalculateSuggestedScoreCore(result, parameter);
        }

        private static double CalculateSuggestedScoreCore(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public string Key => sourceKey;

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IAnnotationQuery<MsScanMatchResult> query) {
            var parameter = query.Parameter ?? Parameter;
            return SearchBound(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private IReadOnlyList<MoleculeMsReference> SearchBound(IMSProperty property, double ms1Tolerance) {
            return searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(ms1Tolerance, property.PrecursorMz)));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IAnnotationQuery<MsScanMatchResult> query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            ValidateCore(result, query.Property, query.NormalizedScan, reference, parameter, omics);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            IMSProperty property, IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter, TargetOmics omics) {

            if (omics == TargetOmics.Lipidomics)
                ValidateOnLipidomics(result, property, scan, reference, parameter);
            else
                ValidateBase(result, property, reference, parameter);

            result.IsReferenceMatched = result.IsPrecursorMzMatch && result.IsSpectrumMatch;
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch && !result.IsReferenceMatched;
        }

        private static void ValidateBase(MsScanMatchResult result, IMSProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;
        }

        private static void ValidateOnLipidomics(
            MsScanMatchResult result,
            IMSProperty property, IMSScanProperty scan,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter) {

            ValidateBase(result, property, reference, parameter);

            var name = MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, parameter.Ms2Tolerance, out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
            result.IsSpectrumMatch &= isLipidChainsMatch | isLipidClassMatch | isLipidPositionMatch | isOtherLipidMatch;

            if (result.IsOtherLipidMatch)
                return;

            result.Name = string.IsNullOrEmpty(name) ? reference.Name : name;
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results) {
            return evaluator.FilterByThreshold(results);
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results) {
            return evaluator.SelectReferenceMatchResults(results);
        }

        public IReferRestorationKey<IAnnotationQuery<MsScanMatchResult>, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase> Save() {
            switch (source) {
                case SourceType.MspDB:
                    return new MspDbRestorationKey(sourceKey, Priority);
                case SourceType.TextDB:
                    return new TextDbRestorationKey(sourceKey, Priority);
            }
            throw new NotSupportedException(source.ToString());
        }

        public bool IsReferenceMatched(MsScanMatchResult result) {
            return evaluator.IsReferenceMatched(result);
        }

        public bool IsAnnotationSuggested(MsScanMatchResult result) {
            return evaluator.IsAnnotationSuggested(result);
        }
    }
}
