using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// A candidate with no product-ion spectrum must not have an MS/MS term in its total score.
    /// </summary>
    /// <remarks>
    /// The scorers guarded the MS/MS term with `WeightedDotProduct >= 0 && SimpleDotProduct >= 0 &&
    /// ReverseDotProduct >= 0`, because MsScanMatching returns -1 from those functions when there is
    /// nothing to compare. The squared-metrics rename in #589 made the dot products derived properties
    /// clamped with `Math.Max(squared, 0f)`, so the guard could no longer fail and the MS/MS term was
    /// always added. `MatchedPeaksPercentage` has no such clamp and is still -1, so the term went
    /// negative: a `no MS2: ` row reported `Total score = -0.143` on the FastLC demo instead of a score
    /// derived from precursor m/z and retention time alone.
    /// </remarks>
    [TestClass]
    public class UncomputedMsMsTermTests
    {
        private static MoleculeMsReference Reference() {
            return new MoleculeMsReference
            {
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                PrecursorMz = 810.601,
                InChIKey = "DUMMYINCHIKEY",
                ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                },
            };
        }

        private static MsRefSearchParameterBase Parameter() {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
            };
        }

        /// <summary>A precursor-only feature: MS2RawSpectrumID was negative, so the scan carries no peaks.</summary>
        private static ChromatogramPeakFeature PrecursorOnlyTarget() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
                MS2RawSpectrumID = -1,
                Spectrum = new List<SpectrumPeak>(),
            };
        }

        private static MsReferenceScorer Scorer() {
            return new MsReferenceScorer("MspDB", -1, TargetOmics.Lipidomics, SourceType.MspDB, CollisionType.CID, true);
        }

        [TestMethod]
        public void PrecursorOnlyCandidateCarriesTheNotComputedSentinel() {
            var target = PrecursorOnlyTarget();

            var result = Scorer().CalculateScore(target, target, null, Reference(), null, Parameter());

            Assert.AreEqual(-1f, result.SquaredWeightedDotProduct);
            Assert.AreEqual(-1f, result.SquaredSimpleDotProduct);
            Assert.AreEqual(-1f, result.SquaredReverseDotProduct);
            Assert.AreEqual(-1f, result.MatchedPeaksCount);
            Assert.AreEqual(-1f, result.MatchedPeaksPercentage);
            Assert.IsFalse(result.IsSpectrumComparisonPerformed);
            Assert.IsFalse(result.IsSpectrumMatch);
        }

        [TestMethod]
        public void PrecursorOnlyCandidateIsScoredFromRetentionAndMassAlone() {
            var target = PrecursorOnlyTarget();

            var result = Scorer().CalculateScore(target, target, null, Reference(), null, Parameter());

            // Lipidomics CID weights retention at .5 and mass at 1.0; the MS/MS term is absent.
            var expected = new[] { (double)result.AcurateMassSimilarity, result.RtSimilarity * .5, }.Average();
            Assert.AreEqual(expected, result.TotalScore, 1e-6);
        }

        [TestMethod]
        public void PrecursorOnlyCandidateNoLongerScoresNegative() {
            var target = PrecursorOnlyTarget();

            var result = Scorer().CalculateScore(target, target, null, Reference(), null, Parameter());

            Assert.IsTrue(result.TotalScore > 0f, $"expected a positive total score, got {result.TotalScore}");
        }

        [TestMethod]
        public void EnhancedDotProductKeepsTheSentinelInsteadOfNaN() {
            var target = PrecursorOnlyTarget();

            var result = Scorer().CalculateScore(target, target, null, Reference(), null, Parameter());

            Assert.IsFalse(float.IsNaN(result.EnhancedDotProduct));
            Assert.AreEqual(-1f, result.EnhancedDotProduct);
            Assert.AreEqual(-1f, result.SpectralEntropy);
        }

        /// <summary>
        /// The ranking contract does not depend on a precursor-only candidate scoring below zero.
        /// Representative orders on (IsManuallyModified, IsReferenceMatched, IsAnnotationSuggested,
        /// Priority, TotalScore), so a spectrum-matched candidate wins even when the precursor-only
        /// candidate has the higher total score.
        /// </summary>
        [TestMethod]
        public void SpectrumMatchedCandidateOutranksPrecursorOnlyWithAHigherTotalScore() {
            var matched = new MsScanMatchResult
            {
                Name = "Matched compound",
                Source = SourceType.MspDB,
                AnnotatorID = "msp_annotator_1",
                IsReferenceMatched = true,
                IsSpectrumMatch = true,
                SimpleDotProduct = 0.9f,
                MatchedPeaksCount = 6f,
                MatchedPeaksPercentage = 0.75f,
                TotalScore = 1.2f,
            };
            var precursorOnly = new MsScanMatchResult
            {
                Name = "Suggested compound",
                Source = SourceType.MspDB,
                AnnotatorID = "msp_annotator_1",
                IsAnnotationSuggested = true,
                SquaredSimpleDotProduct = -1f,
                SquaredWeightedDotProduct = -1f,
                SquaredReverseDotProduct = -1f,
                MatchedPeaksCount = -1f,
                MatchedPeaksPercentage = -1f,
                TotalScore = 1.9f,
            };
            var container = new MsScanMatchResultContainer();
            container.AddResults(new List<MsScanMatchResult> { precursorOnly, matched, });

            Assert.AreSame(matched, container.Representative);
        }
    }
}
