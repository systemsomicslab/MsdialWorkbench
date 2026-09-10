using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// A search tolerance is not a match criterion: the retention-time verdict is capped, the score
    /// is not.
    /// </summary>
    /// <remarks>
    /// The two settings answer different questions. RtTolerance says which references are worth
    /// scoring, and its default is 100 minutes; the verdict says whether the retention times actually
    /// agree. Ticking "use retention time for scoring" while leaving the tolerance at its default
    /// used to mean that every reference in the library agreed on retention time, which then promoted
    /// the exported confidence code from 430 to 330. Searching a predicted-retention-time library
    /// loosely is reasonable; publishing "retention time matched" on the strength of that search is
    /// not.
    ///
    /// The cap therefore lands on the verdict only. <see cref="TheGaussianStillUsesTheUsersTolerance"/>
    /// is the test that holds that line, and it is the reason existing projects are not silently
    /// re-ranked: the total score is computed from RtSimilarity, which is untouched.
    ///
    /// A CONSEQUENCE WORTH KNOWING. Unlike a reference that carries no retention time -- which is
    /// exempt, see MissingReferenceRetentionTimeTests -- a reference that carries one and disagrees
    /// is rejected, and IsReferenceMatched and IsAnnotationSuggested share that clause, so it is
    /// rejected outright rather than demoted to a suggestion. That is not new behaviour; it is what
    /// already happened to anyone who set a realistic tolerance. What is new is that the default no
    /// longer exempts everyone from it.
    ///
    /// Cap values confirmed with the author of MS-DIAL, 2026-09-10.
    /// </remarks>
    [TestClass()]
    public class RetentionTimeMatchCapTests
    {
        [TestMethod()]
        public void TheDefaultWideToleranceNoLongerMakesADistantReferenceAMatch() {
            // 10 minutes apart, against the 100-minute default. This used to be a match.
            var result = Score(peakRetentionTime: 2d, referenceRetentionTime: 12d, rtTolerance: 100f);

            Assert.IsFalse(result.IsRtMatch, "10 minutes is not agreement, whatever the search window says");
        }

        [TestMethod()]
        public void TheGaussianStillUsesTheUsersTolerance() {
            // The cap must not reach the score. exp(-0.5 * (10 / 100)^2) = 0.99501 with the user's
            // 100-minute width; it would be 3.7e-6 if the width had been capped to 2 minutes.
            var result = Score(peakRetentionTime: 2d, referenceRetentionTime: 12d, rtTolerance: 100f);

            Assert.AreEqual(0.99501f, result.RtSimilarity, 1e-4f,
                "the width of the Gaussian is still the user's tolerance, so total scores do not move");
        }

        [TestMethod()]
        public void TheTermIsStillCountedInTheTotalEvenWhenTheVerdictIsRefused() {
            // Following from the above: this is a comparison that happened and disagreed, not one
            // that could not be made. The record says so, and the score keeps its contribution.
            var result = Score(peakRetentionTime: 2d, referenceRetentionTime: 12d, rtTolerance: 100f);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime));
            Assert.IsTrue(result.RtSimilarity > 0f);
        }

        [TestMethod()]
        public void TheConfidenceCodeFollowsTheVerdict() {
            var result = Score(peakRetentionTime: 2d, referenceRetentionTime: 12d, rtTolerance: 100f);

            Assert.IsTrue(result.IsSpectrumMatch, "precondition: the spectrum did match");
            Assert.AreEqual(430, DataAccess.GetAnnotationCode(result, MachineCategory.LCMS),
                "m/z + MS/MS matched, which is what happened; this used to be 330");
        }

        [TestMethod()]
        public void AToleranceNarrowerThanTheCapStillGoverns() {
            // The cap is an upper bound on the user's setting, not a replacement for it. At a
            // 1-minute tolerance a 1.5-minute difference is still a miss.
            var missed = Score(peakRetentionTime: 2d, referenceRetentionTime: 3.5d, rtTolerance: 1f);
            var hit = Score(peakRetentionTime: 2d, referenceRetentionTime: 2.5d, rtTolerance: 1f);

            Assert.IsFalse(missed.IsRtMatch, "1.5 minutes exceeds the analyst's own 1-minute tolerance");
            Assert.IsTrue(hit.IsRtMatch);
        }

        [TestMethod()]
        public void AgreementInsideTheCapIsStillAMatchAndStillEarnsTheCode() {
            // The control. A genuine retention-time match, found with a wide search window, is
            // unaffected: this is the case the cap must not damage.
            var result = Score(peakRetentionTime: 2d, referenceRetentionTime: 2.5d, rtTolerance: 100f);

            Assert.IsTrue(result.IsRtMatch);
            Assert.IsTrue(result.IsReferenceMatched);
            Assert.AreEqual(330, DataAccess.GetAnnotationCode(result, MachineCategory.LCMS));
        }

        [TestMethod()]
        public void TheCapItselfIsTwoMinutes() {
            // Stated once so that changing the constant is a visible decision rather than a silent
            // shift in what every export claims.
            Assert.AreEqual(2d, RetentionMatchPolicy.RetentionTimeMatchCapMinutes);
            Assert.AreEqual(2d, RetentionMatchPolicy.EffectiveRetentionTimeTolerance(100d), "capped");
            Assert.AreEqual(0.5d, RetentionMatchPolicy.EffectiveRetentionTimeTolerance(0.5d), "not raised");
        }

        private static MsScanMatchResult Score(double peakRetentionTime, double referenceRetentionTime, float rtTolerance) {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = rtTolerance,
                IsUseTimeForAnnotationScoring = true,
            };
            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(peakRetentionTime, ChromXType.RT, ChromXUnit.Min),
                Spectrum = Spectrum(),
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(referenceRetentionTime, ChromXType.RT, ChromXUnit.Min),
                Spectrum = Spectrum(),
            };

            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);
            return scorer.CalculateScore(target, target, null, reference, null, parameter);
        }

        /// <summary>Four identical peaks, so that IsSpectrumMatch is true and the code assertions mean something.</summary>
        private static List<SpectrumPeak> Spectrum() {
            return new List<SpectrumPeak>
            {
                new SpectrumPeak { Mass = 100.050, Intensity = 100, },
                new SpectrumPeak { Mass = 200.100, Intensity = 60, },
                new SpectrumPeak { Mass = 300.150, Intensity = 40, },
                new SpectrumPeak { Mass = 400.200, Intensity = 20, },
            };
        }
    }
}
