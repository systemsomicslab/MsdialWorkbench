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
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation
{
    public class DimsMspAnnotator : IAnnotator<IMSProperty, IMSScanProperty>
    {
        private static readonly IComparer<IMSScanProperty> comparer = MassComparer.Comparer;

        private readonly List<MoleculeMsReference> mspDB;
        private readonly TargetOmics omics;

        public MsRefSearchParameterBase Parameter { get; }

        public DimsMspAnnotator(IEnumerable<MoleculeMsReference> mspDB, MsRefSearchParameterBase parameter, TargetOmics omics) {
            this.mspDB = mspDB.OrderBy(msp => msp.PrecursorMz).ToList();
            Parameter = parameter;
            this.omics = omics;
        }

        public MsScanMatchResult Annotate(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, scan, parameter, mspDB, omics).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), parameter, mspDB, omics);
        }

        private static List<MsScanMatchResult> FindCandidatesCore(IMSProperty property, IMSScanProperty scan, MsRefSearchParameterBase parameter, IReadOnlyList<MoleculeMsReference> mspDB, TargetOmics omics) {
            (var lo, var hi) = SearchBoundIndex(property, mspDB, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(hi - lo);
            for (var i = lo; i < hi; i++) {
                var candidate = mspDB[i];
                var result = CalculateScoreCore(property, scan, candidate, parameter);
                result.LibraryIDWhenOrdered = i;
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.Where(result => result.TotalScore >= parameter.TotalScoreCutoff).OrderBy(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            return CalculateScoreCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), reference, parameter);
        }

        private static MsScanMatchResult CalculateScoreCore(IMSProperty property, IMSScanProperty scan, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var matchedPeaksScores = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            var ms1Tol = CalculateMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name, LibraryID = reference.ScanID, InChIKey = reference.InChIKey,
                WeightedDotProduct = (float)weightedDotProduct, SimpleDotProduct = (float)simpleDotProduct, ReverseDotProduct = (float)reverseDotProduct,
                MatchedPeaksPercentage = (float)matchedPeaksScores[0], MatchedPeaksCount = (float)matchedPeaksScores[1],
                AcurateMassSimilarity = (float)ms1Similarity,
                TotalScore = (float)((ms1Similarity + (weightedDotProduct + simpleDotProduct + reverseDotProduct) / 3 + matchedPeaksScores[0]) / 3)
            };

            var scores = new List<float> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
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

        public List<MoleculeMsReference> Search(IMSProperty property, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            (var lo, var hi) = SearchBoundIndex(property, mspDB, parameter.Ms1Tolerance);
            return mspDB.GetRange(lo, hi - lo);
        }

        private static (int lo, int hi) SearchBoundIndex(IMSProperty property, IReadOnlyList<MoleculeMsReference> mspDB, double tolerance) {
            tolerance = CalculateMassTolerance(tolerance, property.PrecursorMz);
            var dummy = new MSScanProperty { PrecursorMz = property.PrecursorMz - tolerance };
            var lo = SearchCollection.LowerBound(mspDB, dummy, comparer);
            dummy.PrecursorMz = property.PrecursorMz + tolerance;
            var hi = SearchCollection.UpperBound(mspDB, dummy, lo, mspDB.Count, comparer);
            return (lo, hi);
        }

        private static double CalculateMassTolerance(double tolerance, double mass) {
            if (mass <= 500)
                return tolerance;
            var ppm = Math.Abs(MolecularFormulaUtility.PpmCalculator(500.00, 500.00 + tolerance));
            return MolecularFormulaUtility.ConvertPpmToMassAccuracy(mass, ppm);
        }

        public void Validate(MsScanMatchResult result, IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, scan, reference, parameter, omics);
        }

        private static void ValidateCore(MsScanMatchResult result, IMSProperty property, IMSScanProperty scan, MoleculeMsReference reference, MsRefSearchParameterBase parameter, TargetOmics omics) {
            if (omics == TargetOmics.Lipidomics)
                ValidateOnLipidomics(result, property, scan, reference, parameter);
            else
                ValidateBase(result, scan, reference, parameter);
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

        private static void ValidateOnLipidomics(MsScanMatchResult result, IMSProperty property, IMSScanProperty scan, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            ValidateBase(result, property, reference, parameter);

            result.Name = MsScanMatching.GetRefinedLipidAnnotationLevel(scan, reference, parameter.Ms2Tolerance, out var isLipidClassMatch, out var isLipidChainsMatch, out var isLipidPositionMatch, out var isOtherLipidMatch);
            result.IsLipidChainsMatch = isLipidChainsMatch;
            result.IsLipidClassMatch = isLipidClassMatch;
            result.IsLipidPositionMatch = isLipidPositionMatch;
            result.IsOtherLipidMatch = isOtherLipidMatch;
        }
    }
}
