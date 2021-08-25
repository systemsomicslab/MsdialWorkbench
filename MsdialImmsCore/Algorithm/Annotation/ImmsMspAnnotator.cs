using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Annotation
{
    public class ImmsMspAnnotator : StandardRestorableBase, ISerializableAnnotator<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult, MoleculeDataBase>
    {
        private static readonly IComparer<IMSIonProperty> comparer = CompositeComparer.Build<IMSIonProperty>(MassComparer.Comparer, CollisionCrossSectionComparer.Comparer);

        private readonly TargetOmics omics;

        public ImmsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey)
            : base(mspDB.Database, parameter, sourceKey, SourceType.MspDB){
            this.db.Sort(comparer);
            this.omics = omics;
            this.ReferObject = mspDB;
        }

        public MsScanMatchResult Annotate(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, parameter, omics, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return FindCandidatesCore(query.Property, query.Scan, query.Isotopes, parameter, omics, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey) {

            var normScan = DataAccess.GetNormalizedMSScanProperty(scan, parameter);
            var candidates = parameter.IsUseCcsForAnnotationFiltering
                ? SearchWithCcsCore(property, parameter.Ms1Tolerance, parameter.CcsTolerance)
                : SearchCore(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, normScan, isotopes, candidate, candidate.IsotopicPeaks, parameter, omics, sourceKey);
                ValidateCore(result, property, normScan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IAnnotationQuery query, MoleculeMsReference reference) {
            var parameter = query.Parameter ?? Parameter;
            var scan = DataAccess.GetNormalizedMSScanProperty(query.Scan, parameter);
            var result = CalculateScoreCore(query.Property, scan, query.Isotopes, reference, reference.IsotopicPeaks, parameter, omics, Key);
            ValidateCore(result, query.Property, scan, reference, parameter, omics);
            return result;
        }

        private static MsScanMatchResult CalculateScoreCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey) {

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
                Source = SourceType.MspDB, SourceKey = sourceKey
            };

            if (parameter.IsUseCcsForAnnotationScoring) {
                var ccsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);
                result.CcsSimilarity = (float)ccsSimilarity;
            }
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
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
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
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            return scores.DefaultIfEmpty().Average();
        }

        public IMatchResultRefer<MoleculeMsReference, MsScanMatchResult> ReferObject { get; }

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        private MassCcsReferenceSearcher<MoleculeMsReference> SearcherCcs
            => searcherCcs ?? (searcherCcs = new MassCcsReferenceSearcher<MoleculeMsReference>(db));
        private MassCcsReferenceSearcher<MoleculeMsReference> searcherCcs;

        private MassReferenceSearcher<MoleculeMsReference> Searcher
            => searcher ?? (searcher = new MassReferenceSearcher<MoleculeMsReference>(db));
        private MassReferenceSearcher<MoleculeMsReference> searcher;

        public List<MoleculeMsReference> Search(IAnnotationQuery query) {
            var parameter = query.Parameter ?? Parameter;
            return parameter.IsUseCcsForAnnotationFiltering
                ? SearchWithCcsCore(query.Property, parameter.Ms1Tolerance, parameter.CcsTolerance).ToList()
                : SearchCore(query.Property, parameter.Ms1Tolerance).ToList();
        }

        private IReadOnlyList<MoleculeMsReference> SearchCore(IMSIonProperty property, double massTolerance) {
            return Searcher.Search(new MassSearchQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz)));
        }

        private IReadOnlyList<MoleculeMsReference> SearchWithCcsCore(IMSIonProperty property, double massTolerance, double ccsTolerance) {
            return SearcherCcs.Search(MSIonSearchQuery.CreateMassCcsQuery(property.PrecursorMz, CalculateMassTolerance(massTolerance, property.PrecursorMz), property.CollisionCrossSection, ccsTolerance));
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter, TargetOmics omics) {

            if (omics == TargetOmics.Lipidomics)
                ValidateOnLipidomics(result, property, scan, reference, parameter);
            else
                ValidateBase(result, property, reference, parameter);
        }

        private static readonly double MsdialCcsMatchThreshold = 10d;
        private static void ValidateBase(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            if (parameter.IsUseCcsForAnnotationScoring) {
                var diff = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection);
                result.IsCcsMatch = diff <= MsdialCcsMatchThreshold && diff <= parameter.CcsTolerance;
            }
        }

        private static void ValidateOnLipidomics(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter) {

            ValidateBase(result, property, reference, parameter);

            MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, parameter.Ms2Tolerance, out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
            result.IsSpectrumMatch &= isLipidChainsMatch | isLipidClassMatch | isLipidPositionMatch | isOtherLipidMatch;

            if (result.IsOtherLipidMatch)
                return;

            var molecule = LipidomicsConverter.ConvertMsdialLipidnameToLipidMoleculeObjectVS2(reference);
            if (molecule == null || molecule.SublevelLipidName == null || molecule.LipidName == null) {
                result.Name = reference.Name; // for others and splash etc in compoundclass
            }
            else if (molecule.SublevelLipidName == molecule.LipidName) {
                result.Name = molecule.LipidName;
            }
            else {
                result.Name = $"{molecule.SublevelLipidName}|{molecule.LipidName}";
            }
        }

        public MsScanMatchResult SelectTopHit(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return results.Argmax(result => result.TotalScore);
        }

        public List<MsScanMatchResult> FilterByThreshold(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            var filtered = new List<MsScanMatchResult>();
            foreach (var result in results) {
                if (SatisfySuggestedConditions(result, parameter)) {
                    filtered.Add(result);
                }
                else if (SatisfyRefMatchedConditions(result, parameter)) {
                    filtered.Add(result);
                }
            }
            return filtered;
        }

        private static bool SatisfyRefMatchedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            if (!result.IsPrecursorMzMatch || !result.IsSpectrumMatch) {
                return false;
            }
            if (result.WeightedDotProduct < parameter.WeightedDotProductCutOff
                || result.SimpleDotProduct < parameter.SimpleDotProductCutOff
                || result.ReverseDotProduct < parameter.ReverseDotProductCutOff
                || result.MatchedPeaksPercentage < parameter.MatchedPeaksPercentageCutOff
                || result.MatchedPeaksCount < parameter.MinimumSpectrumMatch) {
                return false;
            }
            return CalculateAnnotatedScoreCore(result, parameter) >= parameter.TotalScoreCutoff;
        }

        private static bool SatisfySuggestedConditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            return result.IsPrecursorMzMatch && CalculateSuggestedScoreCore(result, parameter) >= parameter.TotalScoreCutoff;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            if (parameter is null) {
                parameter = Parameter;
            }
            return results.Where(result => SatisfyRefMatchedConditions(result, parameter)).ToList();
        }
    }
}
