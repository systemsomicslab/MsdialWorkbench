using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialDimsCore.Algorithm.Annotation.Tests
{
    /// <summary>
    /// A direct-infusion candidate is scored on the terms that were compared, not on a fixed three.
    /// </summary>
    /// <remarks>
    /// DimsMspAnnotator averaged the mass term together with two spectral terms unconditionally.
    /// When a feature carries no product-ion spectrum -- the ordinary case in direct infusion, where
    /// an MS1 survey may be all there is -- both Ms2 calculators return their Empty, which holds 0
    /// in every spectral field rather than the -1 the scoring functions returned. So a precursor
    /// mass agreeing to within tolerance, worth 1.0 on its own, was published as 0.33.
    ///
    /// Every other annotator already assembles the terms it computed and averages those.
    /// CalculateAnnotatedScoreCore, in the same file, does the same. This one site did not.
    /// </remarks>
    [TestClass()]
    public class DimsScoreCountsOnlyComparedTermsTests
    {
        private const double Tolerance = 1e-6;

        /// <summary>
        /// THE DEFECT. A feature with no spectrum keeps the score its mass agreement earned.
        /// </summary>
        [TestMethod()]
        public void AFeatureWithNoSpectrumIsScoredOnItsMassAlone() {
            var result = ScoreAgainstReference(WithoutSpectrum(), TargetOmics.Metabolomics);

            Assert.IsTrue(result.AcurateMassSimilarity > 0.9f, "the masses agree closely");
            Assert.AreEqual(result.AcurateMassSimilarity, result.TotalScore, Tolerance,
                "one term was compared, so it is the whole of the score");
            // What it used to be: (mass + 0 + 0) / 3. The two zeros were fabricated by
            // Ms2MatchResult.Empty and had never been measured against anything.
            Assert.AreNotEqual(result.AcurateMassSimilarity / 3f, result.TotalScore, 0.05f);
        }

        /// <summary>
        /// The spectral fields are still zero, and the record still says why.
        /// </summary>
        /// <remarks>
        /// The zeros themselves are not removed -- they are what Empty writes through Assign, and
        /// several consumers read those fields. What changes is that they no longer enter an
        /// average as though they were measurements. MeasuredTerms and the evidence source are what
        /// tell a reader the difference, and they are asserted here so the score and the record
        /// cannot drift apart.
        /// </remarks>
        [TestMethod()]
        public void TheRecordSaysNoSpectrumWasCompared() {
            var result = ScoreAgainstReference(WithoutSpectrum(), TargetOmics.Metabolomics);

            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.AccurateMass));
            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
            Assert.AreEqual(0f, result.SquaredWeightedDotProduct, "Empty still writes its zeros");
        }

        /// <summary>
        /// IsSpectrumComparisonPerformed cannot be the guard here, which is why the calculator is
        /// asked instead.
        /// </summary>
        /// <remarks>
        /// That predicate infers "a comparison happened" from the spectral fields being
        /// non-negative. Empty's zeros are non-negative and the source is MspDB, so it answers TRUE
        /// for exactly the candidates the score has to exclude. Pinning it here means that if
        /// someone later simplifies the guard to the predicate, this says what breaks.
        /// </remarks>
        [TestMethod()]
        public void TheInertPredicateWouldHaveMissedThisCandidate() {
            var result = ScoreAgainstReference(WithoutSpectrum(), TargetOmics.Metabolomics);

            Assert.IsTrue(result.IsSpectrumComparisonPerformed,
                "it reports a comparison that never happened; Ms2MatchResult.SpectrumCompared does not");
        }

        /// <summary>
        /// The lipid branch goes the same way, including keeping the reference name.
        /// </summary>
        /// <remarks>
        /// The two branches used to hold separate ms2Result locals, so the fix required hoisting one
        /// out; this is the half that the hoist could have broken. LipidMs2MatchResult.Empty carries
        /// an empty Name and IsOtherLipidMatch false, so the annotator falls back to the reference's
        /// own name exactly as before.
        /// </remarks>
        [TestMethod()]
        public void TheLipidBranchIsScoredTheSameWayAndKeepsTheReferenceName() {
            var result = ScoreAgainstReference(WithoutSpectrum(), TargetOmics.Lipidomics);

            Assert.AreEqual(result.AcurateMassSimilarity, result.TotalScore, Tolerance);
            Assert.AreEqual("PC 16:0_18:1", result.Name);
        }

        /// <summary>
        /// A CANDIDATE WHOSE SPECTRUM WAS COMPARED IS UNTOUCHED.
        /// </summary>
        /// <remarks>
        /// The boundary of the change, and the reason it is not a rescaling of the score. When all
        /// three terms were measured the divisor is still three and the value is identical; only the
        /// candidates that had terms fabricated for them move.
        /// </remarks>
        [TestMethod()]
        public void AComparedSpectrumStillContributesAllThreeTerms() {
            var result = ScoreAgainstReference(WithSpectrum(), TargetOmics.Metabolomics);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
            var threeTerms = new[] {
                (double)result.AcurateMassSimilarity,
                new[] { result.WeightedDotProduct, result.SimpleDotProduct, result.ReverseDotProduct }.Average(),
                result.MatchedPeaksPercentage,
            }.Average();
            Assert.AreEqual(threeTerms, result.TotalScore, Tolerance);
            Assert.IsTrue(result.TotalScore < result.AcurateMassSimilarity,
                "and a spectral disagreement still costs the candidate, as it should");
        }

        /// <summary>
        /// The consequence for the ordering the previous commit introduced.
        /// </summary>
        /// <remarks>
        /// Both halves are needed for the annotation to come out right. The rank says a compared
        /// spectrum outranks a bare mass; this says the bare mass is worth what it measured. With
        /// the score divided by three, a precursor-only candidate also fell below TotalScoreCutoff
        /// for any cutoff above a third -- discarded before the ordering ever saw it.
        /// </remarks>
        [TestMethod()]
        public void APrecursorOnlyCandidateNowClearsAnOrdinaryCutoff() {
            var result = ScoreAgainstReference(WithoutSpectrum(), TargetOmics.Metabolomics);

            Assert.IsTrue(result.TotalScore > 0.7f,
                "under the old score this was about 0.33 and a 0.7 cutoff dropped it");
        }

        private static MsScanMatchResult ScoreAgainstReference(ChromatogramPeakFeature target, TargetOmics omics) {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                TotalScoreCutoff = 0,
            };
            var reference = new MoleculeMsReference
            {
                Name = "PC 16:0_18:1",
                InChIKey = "a",
                PrecursorMz = 760.585,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 478.329, Intensity = 10, },
                    new SpectrumPeak { Mass = 760.585, Intensity = 30, },
                },
            };
            var annotator = new DimsMspAnnotator(
                new MoleculeDataBase(new[] { reference }, "MspDB", DataBaseSource.Msp, SourceType.MspDB, "MspDBPath"),
                parameter, omics, "MspDB", -1);

            var query = new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
            return annotator.CalculateScore(query, reference);
        }

        /// <summary>
        /// A direct-infusion feature that carries no product-ion spectrum: nothing was fragmented,
        /// so the scoring functions return their not-compared sentinel and the calculator returns
        /// Empty.
        /// </summary>
        private static ChromatogramPeakFeature WithoutSpectrum() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 760.586,
                Spectrum = new List<SpectrumPeak>(),
            };
        }

        private static ChromatogramPeakFeature WithSpectrum() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 760.586,
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 760.585, Intensity = 20, },
                },
            };
        }
    }
}
