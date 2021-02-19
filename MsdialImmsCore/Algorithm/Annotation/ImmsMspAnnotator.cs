using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Parameter;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialImmsCore.Algorithm.Annotation
{
    public class ImmsMspAnnotator<T> : IAnnotator<T, MSDecResult>
        where T : IMSProperty, IIonProperty
    {
        private static readonly IComparer<IMSScanProperty> comparer = CompositeComparer.Build(MassComparer.Comparer, ChromXsComparer.DriftComparer);

        private readonly List<MoleculeMsReference> mspDB;
        private readonly TargetOmics omics;

        public MsRefSearchParameterBase Parameter { get; }

        public ImmsMspAnnotator(IEnumerable<MoleculeMsReference> mspDB, MsRefSearchParameterBase parameter, TargetOmics omics) {
            this.mspDB = mspDB.ToList();
            this.mspDB.Sort(comparer);
            this.Parameter = parameter;
            this.omics = omics;
        }

        public MsScanMatchResult Annotate(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, parameter, mspDB, omics).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, parameter, mspDB, omics);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(
            T property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes,
            MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> mspDB, TargetOmics omics) {

            (var lo, var hi) = SearchBoundIndex(property, mspDB, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = mspDB[i];
                if (candidate.ChromXs.Drift.Value < property.ChromXs.Drift.Value - parameter.CcsTolerance
                    || property.ChromXs.Drift.Value + parameter.CcsTolerance < candidate.ChromXs.Drift.Value)
                    continue;
                var result = CalculateScoreCore(property, scan, isotopes, candidate, candidate.IsotopicPeaks, parameter, omics);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            return CalculateScoreCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), isotopes, reference, reference.IsotopicPeaks, parameter, omics);
        }

        private static MsScanMatchResult CalculateScoreCore(
            T property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes,
            MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes,
            MsRefSearchParameterBase parameter, TargetOmics omics) {

            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var matchedPeaksScores = omics == TargetOmics.Lipidomics
                ? MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd)
                : MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var ccsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                WeightedDotProduct = (float)weightedDotProduct, SimpleDotProduct = (float)simpleDotProduct, ReverseDotProduct = (float)reverseDotProduct,
                MatchedPeaksPercentage = (float)matchedPeaksScores[0], MatchedPeaksCount = (float)matchedPeaksScores[1],
                AcurateMassSimilarity = (float)ms1Similarity, CcsSimilarity = (float)ccsSimilarity, IsotopeSimilarity = (float)isotopeSimilarity,
            };

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

        public MoleculeMsReference Refer(MsScanMatchResult result) {
            if (result.LibraryIDWhenOrdered >= 0 && result.LibraryIDWhenOrdered < mspDB.Count) {
                var msp = mspDB[result.LibraryIDWhenOrdered];
                if (msp.InChIKey == result.InChIKey)
                    return msp;
            }
            return mspDB.FirstOrDefault(msp => msp.InChIKey == result.InChIKey);
        }

        public List<MoleculeMsReference> Search(T property, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;

            (var lo, var hi) = SearchBoundIndex(property, mspDB, parameter.Ms1Tolerance);
            return mspDB.GetRange(lo, hi - lo);
        }

        private static (int lo, int hi) SearchBoundIndex(T property, IReadOnlyList<MoleculeMsReference> mspDB, double ms1Tolerance) {

            ms1Tolerance = CalculateMassTolerance(ms1Tolerance, property.PrecursorMz);
            var dummy = new MSScanProperty { PrecursorMz = property.PrecursorMz - ms1Tolerance };
            var lo = SearchCollection.LowerBound(mspDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + ms1Tolerance;
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
            T property, MSDecResult scan, IReadOnlyList<IsotopicPeak> isotopes,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {

            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), reference, parameter, omics);
        }

        private static void ValidateCore(
            MsScanMatchResult result,
            T property, IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter, TargetOmics omics) {

            if (omics == TargetOmics.Lipidomics)
                ValidateOnLipidomics(result, property, scan, reference, parameter);
            else
                ValidateBase(result, property, reference, parameter);
        }

        private static void ValidateBase(MsScanMatchResult result, T property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            result.IsCcsMatch = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection) <= parameter.CcsTolerance;
        }

        private static void ValidateOnLipidomics(
            MsScanMatchResult result,
            T property, IMSScanProperty scan,
            MoleculeMsReference reference, MsRefSearchParameterBase parameter) {

            ValidateBase(result, property, reference, parameter);

            result.Name = MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, parameter.Ms2Tolerance, out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
        }
    }
}
