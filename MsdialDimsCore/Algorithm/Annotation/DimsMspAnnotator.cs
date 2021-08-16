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
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation
{
    public class DimsMspAnnotator : MspDbRestorableBase, ISerializableAnnotator<IMSProperty, IMSScanProperty, MoleculeDataBase>
    {
        private readonly TargetOmics omics;

        public MsRefSearchParameterBase Parameter { get; }

        public DimsMspAnnotator(MoleculeDataBase mspDB, MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey)
            : base(mspDB.Database, sourceKey) {
            Parameter = parameter;
            this.omics = omics;
            ReferObject = mspDB;
            searcher = new MassReferenceSearcher<MoleculeMsReference>(mspDB.Database);
        }

        private readonly MassReferenceSearcher<MoleculeMsReference> searcher;
        private readonly IMatchResultRefer ReferObject;

        public MsScanMatchResult Annotate(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), parameter, omics, Key).FirstOrDefault();
        }

        public List<MsScanMatchResult> FindCandidates(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            return FindCandidatesCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), parameter, omics, Key);
        }

        private List<MsScanMatchResult> FindCandidatesCore(IMSProperty property, IMSScanProperty scan, MsRefSearchParameterBase parameter, TargetOmics omics, string sourceKey) {
            var candidates = SearchBound(property, parameter.Ms1Tolerance);
            var results = new List<MsScanMatchResult>(candidates.Count);
            foreach (var candidate in candidates) {
                var result = CalculateScoreCore(property, scan, candidate, parameter, sourceKey);
                ValidateCore(result, property, scan, candidate, parameter, omics);
                results.Add(result);
            }
            return results.OrderByDescending(result => result.TotalScore).ToList();
        }

        public MsScanMatchResult CalculateScore(IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            return CalculateScoreCore(property, DataAccess.GetNormalizedMSScanProperty(scan, parameter), reference, parameter, Key);
        }

        private static MsScanMatchResult CalculateScoreCore(IMSProperty property, IMSScanProperty scan, MoleculeMsReference reference, MsRefSearchParameterBase parameter, string sourceKey) {
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
                TotalScore = (float)((ms1Similarity + (weightedDotProduct + simpleDotProduct + reverseDotProduct) / 3 + matchedPeaksScores[0]) / 3),
                Source = SourceType.MspDB, SourceKey = sourceKey
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

        public override MoleculeMsReference Refer(MsScanMatchResult result) {
            return ReferObject.Refer(result);
        }

        public List<MoleculeMsReference> Search(IMSProperty property, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;

            return SearchBound(property, parameter.Ms1Tolerance).ToList();
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

        public void Validate(MsScanMatchResult result, IMSProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> isotopes, MoleculeMsReference reference, MsRefSearchParameterBase parameter = null) {
            if (parameter == null)
                parameter = Parameter;
            ValidateCore(result, property, DataAccess.GetNormalizedMSScanProperty(scan, Parameter), reference, parameter, omics);
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
