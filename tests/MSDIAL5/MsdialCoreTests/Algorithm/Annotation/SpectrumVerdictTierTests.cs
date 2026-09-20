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
    /// The evidence tier a real scoring run produces, as opposed to the rule in isolation.
    /// </summary>
    /// <remarks>
    /// AnnotationEvidenceTests pins the grading rule. This pins the WIRING, which is the part that
    /// can silently rot: the tier is applied by a separate, later call than the one that records
    /// the evidence, because the verdict it reads is not set until validation, and in several
    /// annotators validation runs in the caller of the method that produces the result. An
    /// annotator that never makes that second call keeps the value it had before -- ReferenceSpectrum
    /// for everything compared, whether or not it matched -- which is wrong in the old way rather
    /// than blank, and no unit test on the rule would notice.
    ///
    /// So each mode needs a test that scores something real and reads the tier off the far end.
    /// This is the LC-MS one, covering MsReferenceScorer, which serves LcmsMspAnnotator and
    /// EadLipidAnnotator.
    /// </remarks>
    [TestClass()]
    public class SpectrumVerdictTierTests
    {
        [TestMethod()]
        public void AMatchingSpectrumIsRecordedAsAReferenceSpectrum() {
            var result = Score(matchedPeakCount: 8, referencePeakCount: 8);

            Assert.IsTrue(result.IsSpectrumMatch, "precondition");
            Assert.AreEqual(AnnotationEvidenceSource.ReferenceSpectrum, result.EvidenceSource);
        }

        [TestMethod()]
        public void ASpectrumThatFellShortIsRecordedAsWeak() {
            // Four of twenty reference fragments found: the matched-peaks percentage is 0.20
            // against a 0.25 cut-off, so the conjunction fails, while four is comfortably above
            // the three-fragment minimum. This is the shape of the 69%-against-70% case.
            var result = Score(matchedPeakCount: 4, referencePeakCount: 20);

            Assert.IsFalse(result.IsSpectrumMatch, "precondition: this did not meet the criteria");
            Assert.AreEqual(AnnotationEvidenceSource.WeakSpectrumMatch, result.EvidenceSource,
                $"matched {result.MatchedPeaksCount} peaks at {result.MatchedPeaksPercentage:F2}");
        }

        [TestMethod()]
        public void ASpectrumThatExplainedNothingIsRecordedAsUnmatched() {
            var result = Score(matchedPeakCount: 1, referencePeakCount: 20);

            Assert.IsFalse(result.IsSpectrumMatch, "precondition");
            Assert.AreEqual(AnnotationEvidenceSource.UnmatchedSpectrum, result.EvidenceSource,
                $"matched {result.MatchedPeaksCount} peaks");
        }

        [TestMethod()]
        public void TheThreeTiersAreDistinguishableOnTheSameRun() {
            // Stated as one assertion because the value of the tier is the ordering, not any single
            // level: a reader sorting on this column must get match, then fell-short, then nothing.
            var tiers = new[] {
                Score(matchedPeakCount: 8, referencePeakCount: 8).EvidenceSource,
                Score(matchedPeakCount: 4, referencePeakCount: 20).EvidenceSource,
                Score(matchedPeakCount: 1, referencePeakCount: 20).EvidenceSource,
            };

            CollectionAssert.AreEqual(
                new[] {
                    AnnotationEvidenceSource.ReferenceSpectrum,
                    AnnotationEvidenceSource.WeakSpectrumMatch,
                    AnnotationEvidenceSource.UnmatchedSpectrum,
                },
                tiers);
        }

        [TestMethod()]
        public void AFeatureWithNoProductIonSpectrumStaysPrecursorOnly() {
            // The distinction the author drew: no MS2 acquired is not the same finding as an MS2
            // that was acquired and failed, and only the second is evidence against the candidate.
            var result = Score(matchedPeakCount: 0, referencePeakCount: 20, targetHasSpectrum: false);

            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum), "precondition");
            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
        }

        /// <summary>
        /// Scores a target against a reference of <paramref name="referencePeakCount"/> fragments,
        /// of which the target reproduces the first <paramref name="matchedPeakCount"/>.
        /// </summary>
        private static MsScanMatchResult Score(int matchedPeakCount, int referencePeakCount, bool targetHasSpectrum = true) {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
            };
            var referenceSpectrum = Spectrum(referencePeakCount);
            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min),
                Spectrum = targetHasSpectrum
                    ? referenceSpectrum.Take(matchedPeakCount).ToList()
                    : new List<SpectrumPeak>(),
            };
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a reference",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = referenceSpectrum,
            };

            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true, DataBaseSource.Msp);
            return scorer.CalculateScore(target, target, null, reference, null, parameter);
        }

        private static List<SpectrumPeak> Spectrum(int count) {
            return Enumerable.Range(0, count)
                .Select(i => new SpectrumPeak { Mass = 100.0 + i * 25.0, Intensity = 100 - i * 2, })
                .ToList();
        }
    }
}
