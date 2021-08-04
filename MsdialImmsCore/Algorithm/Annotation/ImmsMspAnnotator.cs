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
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Annotation
{
    public class ImmsMspAnnotator : MspDbRestorableBase, IAnnotator<IMSIonProperty, IMSScanProperty>
    {
        private static readonly IComparer<IMSIonProperty> comparer = CompositeComparer.Build<IMSIonProperty>(MassComparer.Comparer, CollisionCrossSectionComparer.Comparer);

        private readonly TargetOmics omics;

        public MsRefSearchParameterBase Parameter { get; }

        public ImmsMspAnnotator(IEnumerable<MoleculeMsReference> mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey)
            : base(mspDB, sourceKey){
            this.db.Sort(comparer);
            this.Parameter = parameter;
            this.omics = omics;
            this.ReferObject = new DataBaseRefer(this.db);
        }

        public MsScanMatchResult Annotate(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, parameter, db, omics, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, parameter, db, omics, Key);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> mspDB, TargetOmics omics, string sourceKey) {

            (var lo, var hi) = SearchBoundIndex(property, mspDB, parameter.Ms1Tolerance, parameter.CcsTolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = mspDB[i];
                if (parameter.IsUseCcsForAnnotationFiltering
                    && Math.Abs(property.CollisionCrossSection - candidate.CollisionCrossSection) > parameter.CcsTolerance)
                    continue;
                var result = CalculateScoreCore(property, scan, isotopes, candidate, candidate.IsotopicPeaks, parameter, omics, sourceKey);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return CalculateScoreCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, reference, reference.IsotopicPeaks, parameter, omics, Key);
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

            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
            if (result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            result.TotalScore = scores.DefaultIfEmpty().Average();

            return result;
        }

        public IMatchResultRefer ReferObject { get; }
        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IMSIonProperty property, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;

            (var lo, var hi) = SearchBoundIndex(property, db, parameter.Ms1Tolerance, parameter.CcsTolerance);
            var candidates = db.GetRange(lo, hi - lo);
            if (!parameter.IsUseCcsForAnnotationFiltering) {
                return candidates;
            }
            return candidates.Where(candidate => Math.Abs(candidate.CollisionCrossSection - property.CollisionCrossSection) <= parameter.CcsTolerance).ToList();
        }

        private static (int lo, int hi) SearchBoundIndex(IMSIonProperty property, IReadOnlyList<MoleculeMsReference> mspDB, double ms1Tolerance, double ccsTolerance) {

            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var dummy = new MSIonProperty(
                property.PrecursorMz - ms1Tolerance,
                null, IonMode.Negative, null,
                property.CollisionCrossSection - ccsTolerance);
            var lo = SearchCollection.LowerBound(mspDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
            dummy.CollisionCrossSection = property.CollisionCrossSection + ccsTolerance;
            var hi = SearchCollection.UpperBound(mspDB, dummy, lo, mspDB.Count, comparer);
            return (lo, hi);
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), reference, parameter, omics);
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
                if (!SatisfyMs2Conditions(result, parameter)) {
                    continue;
                }
                if (result.TotalScore < parameter.TotalScoreCutoff) {
                    continue;
                }
                filtered.Add(result);
            }
            return filtered;
        }

        private static bool SatisfyMs2Conditions(MsScanMatchResult result, MsRefSearchParameterBase parameter) {
            if (!result.IsPrecursorMzMatch && !result.IsSpectrumMatch) {
                return false;
            }
            if (result.WeightedDotProduct < parameter.WeightedDotProductCutOff
                || result.SimpleDotProduct < parameter.SimpleDotProductCutOff
                || result.ReverseDotProduct < parameter.ReverseDotProductCutOff
                || result.MatchedPeaksPercentage < parameter.MatchedPeaksPercentageCutOff
                || result.MatchedPeaksCount < parameter.MinimumSpectrumMatch) {
                return false;
            }
            return true;
        }

        public List<MsScanMatchResult> SelectReferenceMatchResults(IEnumerable<MsScanMatchResult> results, MsRefSearchParameterBase parameter = null) {
            return FilterByThreshold(results, parameter)
                .Where(result => result.IsPrecursorMzMatch && result.IsSpectrumMatch)
                .ToList();
        }
    }
}
