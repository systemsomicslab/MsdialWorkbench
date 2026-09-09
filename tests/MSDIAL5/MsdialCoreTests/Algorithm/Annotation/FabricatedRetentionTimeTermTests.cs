using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// Pins a confirmed defect: a retention-time term is scored against references that have no
    /// retention time, and the fabricated value reaches both the match verdict and the total score.
    /// </summary>
    /// <remarks>
    /// THESE TESTS ARE MEANT TO FAIL. They record what MS-DIAL does today so that the fix is a
    /// visible, deliberate change rather than a silent one. When the scoring is routed through the
    /// guarded GetGaussianSimilarity overload, every assertion here that names a fabricated value
    /// must be updated to the not-computed sentinel, and the ones about IsRtMatch must be updated
    /// to false. The comments say which value is correct.
    ///
    /// The mechanism. MsScanMatching has two GetGaussianSimilarity overloads. The four-argument one
    /// guards -- either side not positive and it returns -1 -- and reports agreement through an out
    /// parameter. The three-argument one does not guard: it evaluates
    /// exp(-0.5 * ((actual - reference) / tolerance)^2) on whatever it is handed. MsReferenceScorer
    /// and the mode annotators call the unguarded one, and a MoleculeMsReference with no retention
    /// time carries ChromX.RetentionTime.Default, whose value is -1. So the exponential is
    /// evaluated against -1 as though it were a measurement.
    ///
    /// Two things then go wrong together, and the second is worse. The Gaussian returns a high
    /// number whenever the peak's retention time is within a tolerance-width of -1, which for a
    /// realistic first-pass tolerance means every early-eluting peak. And IsRtMatch is computed
    /// separately, by a plain Math.Abs comparison in ValidateBase, so it is set true on the same
    /// grounds -- which means the fix has TWO sites, not one: switching the Gaussian alone would
    /// leave the verdict wrongly true.
    ///
    /// Mixed libraries are where this bites. Entries that carry a retention time are scored
    /// honestly; entries that do not get a bonus that rises as the peak elutes earlier. The author
    /// has confirmed this was never intended.
    ///
    /// The evidence record added in the preceding commits already contradicts the value, which is
    /// what makes the defect self-evidencing rather than a matter of opinion: MeasuredTerms leaves
    /// RetentionTime clear here, because it is recorded from the inputs, while RtSimilarity holds a
    /// number. One of the two is wrong, and it is not the flag.
    ///
    /// Tagged PinnedDefect: <c>dotnet test --filter TestCategory=PinnedDefect</c> lists every test
    /// that records behaviour known to be wrong. Each is expected to fail when its defect is fixed,
    /// and each names the correct value on the assertion.
    /// </remarks>
    [TestClass()]
    [TestCategory("PinnedDefect")]
    public class FabricatedRetentionTimeTermTests
    {
        /// <summary>A first-pass tolerance. The wider it is, the larger the fabricated term.</summary>
        private const float RtTolerance = 5f;

        [TestMethod()]
        public void AReferenceWithNoRetentionTimeStillScoresARetentionTimeTerm() {
            var result = Score(peakRetentionTime: 0.4d, referenceHasRetentionTime: false);

            // CORRECT VALUE AFTER THE FIX: -1f, the not-computed sentinel, and the term excluded
            // from the total. exp(-0.5 * ((0.4 - -1) / 5)^2) = 0.9616.
            Assert.AreEqual(0.9616f, result.RtSimilarity, 1e-4f,
                "today an early-eluting peak scores 0.96 on retention time against a reference "
                + "that has none, because the missing value is -1 and the unguarded Gaussian "
                + "treats it as a measurement");
        }

        [TestMethod()]
        public void AReferenceWithNoRetentionTimeIsAlsoJudgedToMatchOnRetentionTime() {
            var result = Score(peakRetentionTime: 0.4d, referenceHasRetentionTime: false);

            // CORRECT VALUE AFTER THE FIX: false. This is the second site: ValidateBase computes
            // IsRtMatch with its own Math.Abs comparison, |0.4 - -1| = 1.4 <= 5, so guarding the
            // Gaussian alone would leave this true.
            Assert.IsTrue(result.IsRtMatch,
                "and the verdict agrees with the fabricated score, on a reference that has no "
                + "retention time to agree with");
        }

        [TestMethod()]
        public void TheRecordAlreadyContradictsTheValue() {
            var result = Score(peakRetentionTime: 0.4d, referenceHasRetentionTime: false);

            // This pair is the whole argument. Both assertions pass today, and they cannot both be
            // right. After the fix they agree, because RtSimilarity becomes the sentinel.
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime),
                "the evidence record says no retention time was compared");
            Assert.IsTrue(result.RtSimilarity > 0f,
                "while the score field says one was, and holds a high value");
        }

        [TestMethod()]
        public void TheFabricatedTermChangesTheTotalScore() {
            // The damage, stated as a comparison rather than as an absolute. The same reference,
            // which has no retention time either way, gets a different total depending on whether
            // the run scores retention time -- so a term the reference cannot participate in moves
            // its ranking.
            //
            // The direction is worth naming because it is not the obvious one. MsReferenceScorer
            // AVERAGES the included terms, and the MS/MS term carries a factor of 3, so appending
            // a fourth-of-the-weight retention-time term DILUTES the average and LOWERS the score:
            // 1.939 becomes 1.613 here. A reference with no retention time is therefore penalised,
            // by an amount that depends on how early the peak eluted -- which is meaningless. That
            // dilution is the separately-deferred divisor question (scores.Average() over a
            // variable-length list of differently-weighted terms); this test is only about the
            // term existing at all.
            var withRetentionTimeScoring = Score(peakRetentionTime: 0.4d, referenceHasRetentionTime: false);
            var withoutRetentionTimeScoring = Score(peakRetentionTime: 0.4d, referenceHasRetentionTime: false, useRetentionTime: false);

            // CORRECT AFTER THE FIX: equal, because a reference with no retention time contributes
            // no retention-time term whether or not the run scores retention time.
            Assert.AreNotEqual(withoutRetentionTimeScoring.TotalScore, withRetentionTimeScoring.TotalScore,
                "a reference with no retention time should score the same either way");
        }

        [TestMethod()]
        public void AReferenceThatDoesCarryARetentionTimeIsScoredHonestly() {
            // The control. Nothing here should change when the fix lands, and it is what makes the
            // failure of the tests above readable as a defect rather than as a regression.
            var result = Score(peakRetentionTime: 2.0d, referenceHasRetentionTime: true);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime));
            Assert.IsTrue(result.IsRtMatch);
            Assert.AreEqual(1f, result.RtSimilarity, 1e-4f, "the peak and the reference agree exactly");
        }

        private static MsScanMatchResult Score(double peakRetentionTime, bool referenceHasRetentionTime, bool useRetentionTime = true) {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = RtTolerance,
                IsUseTimeForAnnotationScoring = useRetentionTime,
            };
            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(peakRetentionTime, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                },
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30, },
                },
            };
            if (referenceHasRetentionTime) {
                reference.ChromXs = new ChromXs(peakRetentionTime, ChromXType.RT, ChromXUnit.Min);
            }
            // else: leave the default, whose RT is ChromX.RetentionTime.Default == -1.

            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);
            return scorer.CalculateScore(target, target, null, reference, null, parameter);
        }
    }
}
