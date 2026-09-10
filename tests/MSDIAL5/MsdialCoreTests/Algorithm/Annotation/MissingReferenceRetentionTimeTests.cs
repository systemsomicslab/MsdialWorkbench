using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// A reference that carries no retention time neither gains nor loses by retention time.
    /// </summary>
    /// <remarks>
    /// This file replaces FabricatedRetentionTimeTermTests, which pinned the opposite behaviour as a
    /// known defect. Every value it named as "correct after the fix" is asserted here.
    ///
    /// WHAT USED TO HAPPEN. A MoleculeMsReference with no retention time holds
    /// ChromX.RetentionTime.Default, whose value is -1. The unguarded three-argument
    /// GetGaussianSimilarity evaluated exp(-0.5 * ((0.4 - -1) / 5)^2) = 0.9616 on it, and a separate
    /// Math.Abs comparison in ValidateBase set IsRtMatch true from |0.4 - -1| = 1.4 &lt;= 5. So a peak
    /// agreed, on retention time, with a reference that had none -- and agreed more strongly the
    /// earlier it eluted.
    ///
    /// WHAT THE REAL DAMAGE WAS, and why <see cref="TheExportedConfidenceCodeNoLongerClaimsRetentionTimeConfirmation"/>
    /// is the test that matters most here. DataAccess.GetAnnotationCode reads IsRtMatch and nothing
    /// else to promote 430 ("m/z + MS/MS matched") to 330 ("RT + MS/MS matched"). That code is
    /// written to the "Annotation tag (VS1.0)" column of every analysis and alignment export, so the
    /// fabricated agreement published a claim of retention-time confirmation for references that had
    /// no retention time to confirm. The total-score movement was a side effect by comparison.
    ///
    /// WHY THE REQUIREMENT IS AN EXEMPTION. A reference with no retention time keeps its reference
    /// match on the mass and the spectrum, rather than being demoted for failing a comparison that
    /// was never possible. <see cref="ItSurvivesTheThresholdFilterThatDecidesWhatIsStored"/> holds
    /// the storage line, and
    /// <see cref="AReferenceWhoseRetentionTimeDisagreesLosesTheMatchButKeepsTheSuggestion"/> keeps
    /// the exemption from swallowing genuine disagreement -- a reference that HAS a retention time
    /// and misses loses the match, but stays a suggestion, because a scoring setting must not
    /// decide whether a candidate exists.
    ///
    /// Confirmed as unintended by the author of MS-DIAL, 2026-09-10.
    /// </remarks>
    [TestClass()]
    public class MissingReferenceRetentionTimeTests
    {
        /// <summary>A first-pass tolerance. Under the old code, the wider this was the larger the fabricated term.</summary>
        private const float RtTolerance = 5f;

        [TestMethod()]
        public void AReferenceWithNoRetentionTimeScoresNoRetentionTimeTerm() {
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);

            Assert.AreEqual(-1f, result.RtSimilarity,
                "the not-computed sentinel, which is what keeps the term out of the score average; "
                + "this used to be 0.9616");
        }

        [TestMethod()]
        public void AndIsNotJudgedToMatchOnRetentionTime() {
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);

            Assert.IsFalse(result.IsRtMatch,
                "the second site: ValidateBase computes this with its own comparison, so guarding "
                + "the Gaussian alone would have left it true");
        }

        [TestMethod()]
        public void TheRecordAndTheValueNowAgree() {
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);

            // These two used to contradict each other, which is what made the defect self-evidencing:
            // the evidence record said no retention time was compared while the score field held a
            // high number. They are derived from the same fact now.
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime));
            Assert.IsTrue(result.RtSimilarity < 0f);
        }

        [TestMethod()]
        public void TheTotalScoreIsTheSameWhetherOrNotTheRunScoresRetentionTime() {
            // The same reference, which has no retention time either way, must not be ranked
            // differently by a term it cannot participate in. Under the old code these differed
            // (1.939 against 1.613): MsReferenceScorer averages the included terms and the MS/MS term
            // carries a factor of 3, so the fabricated term DILUTED the average and lowered the
            // score. A reference with no retention time was penalised by an amount that depended on
            // how early the peak eluted.
            var withRetentionTimeScoring = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);
            var withoutRetentionTimeScoring = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null, useRetentionTime: false);

            Assert.AreEqual(withoutRetentionTimeScoring.TotalScore, withRetentionTimeScoring.TotalScore);
        }

        [TestMethod()]
        public void TheExportedConfidenceCodeNoLongerClaimsRetentionTimeConfirmation() {
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);

            Assert.IsTrue(result.IsSpectrumMatch, "precondition: this candidate did match a spectrum");
            Assert.AreEqual(430, DataAccess.GetAnnotationCode(result, MachineCategory.LCMS),
                "430 is m/z + MS/MS matched, which is what actually happened. This used to be 330, "
                + "RT + MS/MS matched, on a reference with no retention time");
        }

        [TestMethod()]
        public void ItIsStillAReferenceMatchOnMassAndSpectrumAlone() {
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);

            Assert.IsTrue(result.IsReferenceMatched,
                "the retention requirement is waived, not failed: there was no retention time to "
                + "disagree with, and the mass and the spectrum both matched");
        }

        [TestMethod()]
        public void ItSurvivesTheThresholdFilterThatDecidesWhatIsStored() {
            // The reason the exemption exists at all. FilterByThreshold is
            // IsAnnotationSuggested || IsReferenceMatched, and StandardAnnotationProcess stores only
            // what it returns -- so a candidate that failed the retention requirement would not be
            // demoted, it would never reach the file, and nothing downstream could read its m/z or
            // MS/MS evidence either.
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: null);
            var evaluator = new MsScanMatchResultEvaluator(Parameter(useRetentionTime: true));

            var kept = evaluator.FilterByThreshold(new List<MsScanMatchResult> { result, });

            Assert.AreEqual(1, kept.Count, "a reference with no retention time is still reportable");
        }

        [TestMethod()]
        public void AReferenceWhoseRetentionTimeDisagreesLosesTheMatchButKeepsTheSuggestion() {
            // The exemption must not swallow real disagreement. Here both sides carry a retention
            // time, they are 19.6 minutes apart against a 5 minute tolerance, and the evidence record
            // says the comparison happened -- so the verdict stands and the reference match is
            // refused.
            //
            // It stays a suggestion, though. "Use retention time for SCORING" must not reject a
            // candidate; that is what "use retention time for FILTERING" is for. The two verdicts
            // used to share the retention clause, so a disagreement deleted the candidate outright
            // rather than lowering it -- with nothing left in the file for a reader to judge.
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: 20d);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime),
                "this comparison was possible, so it is on the record");
            Assert.IsFalse(result.IsRtMatch);
            Assert.IsFalse(result.IsReferenceMatched, "the retention times disagree, so it is not a reference match");
            Assert.IsTrue(result.IsAnnotationSuggested, "but the precursor mass still agreed, and that is a suggestion");
        }

        [TestMethod()]
        public void AndThatSuggestionSurvivesTheFilterThatDecidesWhatIsStored() {
            // The point of the previous test, stated where it bites. Scoring settings decide how
            // well a candidate ranks; they must not decide whether it exists.
            var result = Score(peakRetentionTime: 0.4d, referenceRetentionTime: 20d);
            var evaluator = new MsScanMatchResultEvaluator(Parameter(useRetentionTime: true));

            var kept = evaluator.FilterByThreshold(new List<MsScanMatchResult> { result, });

            Assert.AreEqual(1, kept.Count, "a retention-time disagreement lowers a candidate, it does not erase it");
        }

        [TestMethod()]
        public void AReferenceThatDoesCarryAnAgreeingRetentionTimeIsScoredHonestly() {
            // The control. Nothing here changed, and it is what makes the assertions above readable
            // as a fix rather than as a blanket disabling of retention-time scoring.
            var result = Score(peakRetentionTime: 2.0d, referenceRetentionTime: 2.0d);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime));
            Assert.IsTrue(result.IsRtMatch);
            Assert.AreEqual(1f, result.RtSimilarity, 1e-4f, "the peak and the reference agree exactly");
            Assert.AreEqual(330, DataAccess.GetAnnotationCode(result, MachineCategory.LCMS),
                "and here the RT + MS/MS code is earned");
        }

        private static MsRefSearchParameterBase Parameter(bool useRetentionTime) {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = RtTolerance,
                IsUseTimeForAnnotationScoring = useRetentionTime,
            };
        }

        private static MsScanMatchResult Score(double peakRetentionTime, double? referenceRetentionTime, bool useRetentionTime = true) {
            var parameter = Parameter(useRetentionTime);
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
                Spectrum = Spectrum(),
            };
            if (referenceRetentionTime.HasValue) {
                reference.ChromXs = new ChromXs(referenceRetentionTime.Value, ChromXType.RT, ChromXUnit.Min);
            }
            // else: leave the default, whose RT is ChromX.RetentionTime.Default == -1. That is how a
            // library entry with no RETENTIONTIME field arrives, and also how one written back out
            // as "RETENTIONTIME: -1" arrives when it is read again.

            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);
            return scorer.CalculateScore(target, target, null, reference, null, parameter);
        }

        /// <summary>
        /// Four peaks, identical on both sides, so that IsSpectrumMatch is true: the default
        /// MinimumSpectrumMatch is 3, and the confidence-code and verdict assertions above need a
        /// candidate whose spectrum genuinely matched.
        /// </summary>
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
