using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation
{
    public class LcmsMspReferenceScorer : IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
    {
        public LcmsMspReferenceScorer(string id, int priority, TargetOmics omics) {
            Id = id;
            Priority = priority;
            Omics = omics;
        }

        public string Id { get; }
        public int Priority { get; }
        public TargetOmics Omics { get; }

        public MsScanMatchResult Score(IAnnotationQuery query, MoleculeMsReference reference) {
            return CalculateScore(query.Property, query.NormalizedScan, query.Isotopes, reference, reference.IsotopicPeaks, query.Parameter);
        }

        public MsScanMatchResult CalculateScore(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes, MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes, MsRefSearchParameterBase parameter) {
            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var matchedPeaksScores = Omics == TargetOmics.Lipidomics
                ? MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd)
                : MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            var ms1Tol = MolecularFormulaUtility.CalculateMassToleranceBasedOn500Da(parameter.Ms1Tolerance, property.PrecursorMz);
            var ms1Similarity = MsScanMatching.GetGaussianSimilarity(property.PrecursorMz, reference.PrecursorMz, ms1Tol);

            var isotopeSimilarity = MsScanMatching.GetIsotopeRatioSimilarity(scanIsotopes, referenceIsotopes, property.PrecursorMz, ms1Tol);

            var result = new MsScanMatchResult
            {
                Name = reference.Name,
                LibraryID = reference.ScanID,
                InChIKey = reference.InChIKey,
                WeightedDotProduct = (float)weightedDotProduct,
                SimpleDotProduct = (float)simpleDotProduct,
                ReverseDotProduct = (float)reverseDotProduct,
                MatchedPeaksPercentage = (float)matchedPeaksScores[0],
                MatchedPeaksCount = (float)matchedPeaksScores[1],
                AcurateMassSimilarity = (float)ms1Similarity,
                IsotopeSimilarity = (float)isotopeSimilarity,
                Source = SourceType.MspDB,
                AnnotatorID = Id,
                Priority = Priority,
            };

            if (parameter.IsUseTimeForAnnotationScoring) {
                var rtSimilarity = MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance);
                result.RtSimilarity = (float)rtSimilarity;
            }

            var scores = new List<double> { };
            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct + result.SimpleDotProduct + result.ReverseDotProduct) / 3);
            if (result.MatchedPeaksPercentage >= 0)
                scores.Add(result.MatchedPeaksPercentage);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity);
            result.TotalScore = (float)scores.DefaultIfEmpty().Average();

            Validate(result, property, scan, reference, parameter);

            return result;
        }

        public void Validate(
            MsScanMatchResult result,
            IMSIonProperty property, IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter) {

            ValidateBase(result, property, reference, parameter);
            if (Omics == TargetOmics.Lipidomics) {
                ValidateOnLipidomics(result, scan, reference, parameter);
            }
        }

        private void ValidateBase(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = MolecularFormulaUtility.CalculateMassToleranceBasedOn500Da(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            var diff = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value);
            result.IsRtMatch = diff <= parameter.RtTolerance;
        }

        private void ValidateOnLipidomics(
            MsScanMatchResult result,
            IMSScanProperty scan, MoleculeMsReference reference,
            MsRefSearchParameterBase parameter) {

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
    }
}
