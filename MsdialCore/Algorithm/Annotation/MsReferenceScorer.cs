using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Interfaces;
using CompMs.Common.Lipidomics;
using CompMs.Common.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation
{
    public class MsReferenceScorer : IReferenceScorer<IAnnotationQuery, MoleculeMsReference, MsScanMatchResult>
    {
        public MsReferenceScorer(string id, int priority, TargetOmics omics, SourceType source, CollisionType collisionType, bool useMs2) {
            this.id = id;
            this.priority = priority;
            this.omics = omics;
            this.source = source;
            this.collisionType = collisionType;
            this.useMs2 = useMs2;
        }

        private readonly string id;
        private readonly int priority;
        private readonly TargetOmics omics;
        private readonly SourceType source;
        private readonly CollisionType collisionType;
        private readonly bool useMs2;

        public MsScanMatchResult Score(IAnnotationQuery query, MoleculeMsReference reference) {
            return CalculateScore(query.Property, query.NormalizedScan, query.Isotopes, reference, reference.IsotopicPeaks, query.Parameter);
        }

        public MsScanMatchResult CalculateScore(IMSIonProperty property, IMSScanProperty scan, IReadOnlyList<IsotopicPeak> scanIsotopes, MoleculeMsReference reference, IReadOnlyList<IsotopicPeak> referenceIsotopes, MsRefSearchParameterBase parameter) {
            var weightedDotProduct = MsScanMatching.GetWeightedDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var simpleDotProduct = MsScanMatching.GetSimpleDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var reverseDotProduct = MsScanMatching.GetReverseDotProduct(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            var spectrumPenalty = reference.Spectrum != null && reference.Spectrum.Count == 1 ? true : false;
            double[] matchedPeaksScores = null;
            if (omics == TargetOmics.Lipidomics) {
                if (this.collisionType == CollisionType.EIEIO) {
                    matchedPeaksScores = MsScanMatching.GetEieioBasedLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                }
                else if (this.collisionType == CollisionType.OAD) {
                    matchedPeaksScores = MsScanMatching.GetOadBasedLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                }
                else {
                    matchedPeaksScores = MsScanMatching.GetLipidomicsMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
                }
            }
            else {
                matchedPeaksScores = MsScanMatching.GetMatchedPeaksScores(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);
            }

            var ms1Tol = MolecularFormulaUtility.FixMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
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
                Source = source,
                AnnotatorID = id,
                Priority = priority,
            };

            if (parameter.IsUseTimeForAnnotationScoring) {
                var rtSimilarity = MsScanMatching.GetGaussianSimilarity(property.ChromXs.RT.Value, reference.ChromXs.RT.Value, parameter.RtTolerance);
                result.RtSimilarity = (float)rtSimilarity;
            }
            if (parameter.IsUseCcsForAnnotationScoring) {
                var CcsSimilarity = MsScanMatching.GetGaussianSimilarity(property.CollisionCrossSection, reference.CollisionCrossSection, parameter.CcsTolerance);
                result.CcsSimilarity = (float)CcsSimilarity;
            }

            var scores = new List<double> { };
            var dotProductFactor = 3.0;
            var revesrseDotProdFactor = 2.0;
            var presensePercentageFactor = 1.0;
            var rtFactor = 1.0;
            var ccsFactor = 1.0;
            var massFactor = 1.0;
            var isotopeFactor = 0.0;
            if (omics == TargetOmics.Lipidomics) {
                dotProductFactor = 1.0; revesrseDotProdFactor = 2.0; presensePercentageFactor = 3.0; rtFactor = 0.5; ccsFactor = 0.5;
            }
            if (omics == TargetOmics.Metabolomics && spectrumPenalty == true) {
                dotProductFactor = 1.5;
                revesrseDotProdFactor = 1.0;
                presensePercentageFactor = 0.5;

            }

            if (result.AcurateMassSimilarity >= 0)
                scores.Add(result.AcurateMassSimilarity * massFactor);
            if (result.WeightedDotProduct >= 0 && result.SimpleDotProduct >= 0 && result.ReverseDotProduct >= 0)
                scores.Add((result.WeightedDotProduct * dotProductFactor + result.SimpleDotProduct * dotProductFactor + result.ReverseDotProduct * revesrseDotProdFactor + result.MatchedPeaksPercentage * presensePercentageFactor) / 4);
            if (parameter.IsUseTimeForAnnotationScoring && result.RtSimilarity >= 0)
                scores.Add(result.RtSimilarity * rtFactor);
            if (parameter.IsUseCcsForAnnotationScoring && result.CcsSimilarity >= 0)
                scores.Add(result.CcsSimilarity * ccsFactor);
            if (result.IsotopeSimilarity >= 0)
                scores.Add(result.IsotopeSimilarity * isotopeFactor);
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
            if (omics == TargetOmics.Lipidomics) {
                if (collisionType == CollisionType.EIEIO || collisionType == CollisionType.OAD) {
                    ValidateOnEadLipidomics(result, scan, reference, parameter);
                }
                else {
                    ValidateOnLipidomics(result, scan, reference, parameter);
                }
            }
            result.IsReferenceMatched = result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch)
                && (!useMs2 || result.IsSpectrumMatch);
            result.IsAnnotationSuggested = result.IsPrecursorMzMatch
                && (!parameter.IsUseTimeForAnnotationScoring || result.IsRtMatch)
                && (!parameter.IsUseCcsForAnnotationScoring || result.IsCcsMatch)
                && !result.IsReferenceMatched;
        }

        private void ValidateBase(MsScanMatchResult result, IMSIonProperty property, MoleculeMsReference reference, MsRefSearchParameterBase parameter) {
            result.IsSpectrumMatch = result.WeightedDotProduct >= parameter.WeightedDotProductCutOff
                && result.SimpleDotProduct >= parameter.SimpleDotProductCutOff
                && result.ReverseDotProduct >= parameter.ReverseDotProductCutOff
                && result.MatchedPeaksPercentage >= parameter.MatchedPeaksPercentageCutOff
                && result.MatchedPeaksCount >= parameter.MinimumSpectrumMatch;

            var ms1Tol = MolecularFormulaUtility.FixMassTolerance(parameter.Ms1Tolerance, property.PrecursorMz);
            result.IsPrecursorMzMatch = Math.Abs(property.PrecursorMz - reference.PrecursorMz) <= ms1Tol;

            if (parameter.IsUseTimeForAnnotationScoring) {
                result.IsRtMatch = Math.Abs(property.ChromXs.RT.Value - reference.ChromXs.RT.Value) <= parameter.RtTolerance;
            }

            if (parameter.IsUseCcsForAnnotationScoring) {
                result.IsCcsMatch = Math.Abs(property.CollisionCrossSection - reference.CollisionCrossSection) <= parameter.CcsTolerance;
            }
        }

        private void ValidateOnLipidomics(
            MsScanMatchResult result,
            IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter) {

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

        private void ValidateOnEadLipidomics(
            MsScanMatchResult result, 
            IMSScanProperty scan,
            MoleculeMsReference reference,
            MsRefSearchParameterBase parameter) {

            (var lipid, _) = collisionType == CollisionType.OAD
                ? MsScanMatching.GetOadBasedLipidMoleculeAnnotationResult(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd)
                : MsScanMatching.GetEieioBasedLipidMoleculeAnnotationResult(scan, reference, parameter.Ms2Tolerance, parameter.MassRangeBegin, parameter.MassRangeEnd);

            if (lipid is null) {
                lipid = FacadeLipidParser.Default.Parse(reference.Name);
            }
            if (lipid is null) {
                return;
            }
            result.Name = lipid.Name;
            result.IsLipidClassMatch = lipid.Description.HasFlag(LipidDescription.Class);
            result.IsLipidChainsMatch = lipid.Description.HasFlag(LipidDescription.Chain);
            result.IsLipidPositionMatch = lipid.Description.HasFlag(LipidDescription.SnPosition);
            result.IsLipidDoubleBondPositionMatch = lipid.Description.HasFlag(LipidDescription.DoubleBondPosition);
            result.IsOtherLipidMatch = false;
            result.IsSpectrumMatch &= result.IsLipidChainsMatch | result.IsLipidClassMatch | result.IsLipidPositionMatch | result.IsOtherLipidMatch;
        }
    }
}
