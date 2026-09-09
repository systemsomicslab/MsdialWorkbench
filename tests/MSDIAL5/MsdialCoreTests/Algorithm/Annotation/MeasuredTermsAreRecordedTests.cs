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
    /// One witness per place in MsdialCore that computes a similarity term, asserting that the term
    /// is recorded where it is computed.
    /// </summary>
    /// <remarks>
    /// These pin a fact, not a value: no assertion here reads a score. The point is that a run
    /// which measured a term says so, and a run which did not, does not -- so that no consumer has
    /// to guess afterwards from a value that has been clamped, defaulted or collapsed to a
    /// singleton on the way out.
    ///
    /// The site-per-test shape is deliberate. Nine places build a match result from freshly
    /// computed terms, they differ only in which terms their acquisition mode has, and an omission
    /// in one of them leaves that whole mode's evidence blank while every other test still passes.
    /// The witnesses for the mode-specific annotators live in their own projects' test assemblies,
    /// under the same file name.
    /// </remarks>
    [TestClass()]
    public class MeasuredTermsAreRecordedTests
    {
        [TestMethod()]
        public void MsReferenceScorerRecordsEveryTermItComputes() {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
            };
            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);

            var result = scorer.CalculateScore(Target(), Target(), ScanIsotopes(), Reference(), ReferenceIsotopes(), parameter);

            Assert.AreEqual(
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.RetentionTime | MeasuredTerms.Isotope,
                result.MeasuredTerms);
        }

        [TestMethod()]
        public void MsReferenceScorerRecordsCcsWhenTheRunScoresIt() {
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                CcsTolerance = 5f,
                IsUseCcsForAnnotationScoring = true,
            };
            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);

            var result = scorer.CalculateScore(Target(), Target(), null, Reference(), null, parameter);

            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.Ccs));
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime),
                "the run did not enable retention time, so no retention-time term was measured");
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Isotope),
                "neither side carried isotopic peaks, so no isotope term was measured");
        }

        [TestMethod()]
        public void MsReferenceScorerLeavesSpectrumClearWhenThereIsNoSpectrumToCompare() {
            var parameter = new MsRefSearchParameterBase { Ms1Tolerance = 0.01f, Ms2Tolerance = 0.05f, };
            var reference = Reference();
            reference.Spectrum = new List<SpectrumPeak>();
            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);

            var result = scorer.CalculateScore(Target(), Target(), null, reference, null, parameter);

            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum),
                "an empty reference spectrum means no comparison was performed");
            Assert.IsTrue(result.MeasuredTerms.HasFlag(MeasuredTerms.AccurateMass),
                "the mass term was still computed");
        }

        [TestMethod()]
        public void MsReferenceScorerLeavesRetentionTimeClearWhenTheReferenceHasNone() {
            // The reference carries no retention time. The value the scorer stores in RtSimilarity
            // is nevertheless not zero: this site calls the unguarded GetGaussianSimilarity
            // overload, which evaluates the exponential against the missing 0 rather than
            // returning the not-compared sentinel. The flag is what says no comparison happened.
            // Only the flag is asserted here, so that correcting the value does not have to
            // rewrite this test.
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                IsUseTimeForAnnotationScoring = true,
            };
            var reference = Reference();
            reference.ChromXs = new ChromXs(0, ChromXType.RT, ChromXUnit.Min);
            var scorer = new MsReferenceScorer("MspDB", -1, TargetOmics.Metabolomics, SourceType.MspDB, CollisionType.CID, useMs2: true);

            var result = scorer.CalculateScore(Target(), Target(), null, reference, null, parameter);

            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.RetentionTime),
                "a reference with no retention time cannot have had its retention time compared");
        }

        [TestMethod()]
        public void MassAnnotatorRecordsEveryTermItComputes() {
            var reference = Reference();
            reference.IsotopicPeaks = ReferenceIsotopes();
            var parameter = new MsRefSearchParameterBase { Ms1Tolerance = 0.01f, Ms2Tolerance = 0.05f, TotalScoreCutoff = 0, };
            var db = new MoleculeDataBase(new List<MoleculeMsReference> { reference, }, "MspDB", DataBaseSource.Msp, SourceType.MspDB, "MspPath");
            var annotator = new MassAnnotator(db, parameter, TargetOmics.Metabolomics, SourceType.MspDB, "MspDB", -1);
            var target = Target();
            var query = new AnnotationQuery(target, target, ScanIsotopes(), null, parameter, annotator, ignoreIsotopicPeak: false);

            var result = annotator.CalculateScore(query, reference);

            Assert.AreEqual(
                MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass | MeasuredTerms.Isotope,
                result.MeasuredTerms);
        }

        [TestMethod()]
        public void MassMatchCalculatorRecordsTheMassTerm() {
            var calculator = new MassMatchCalculator();
            var result = calculator.Calculate(new MassMatchQuery(810.604, 0.01), Reference());

            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreEqual(MeasuredTerms.AccurateMass, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void MassMatchCalculatorLeavesTheMassTermClearWhenTheQueryHasNoMass() {
            // Guarded on the inputs rather than on the returned value, because this calculator uses
            // the unguarded GetGaussianSimilarity overload and so gets no sentinel back.
            var calculator = new MassMatchCalculator();
            var result = calculator.Calculate(new MassMatchQuery(0d, 0.01), Reference());

            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreEqual(MeasuredTerms.None, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void Ms2MatchCalculatorRecordsTheSpectrumTerm() {
            var calculator = new Ms2MatchCalculator();
            var target = Target();
            var result = calculator.Calculate(new MSScanMatchQuery(target, SpectrumParameter()), Reference());

            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreEqual(MeasuredTerms.Spectrum, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void AnEmptyMs2MatchResultDoesNotClaimTheSpectrumTerm() {
            // Ms2MatchResult.Empty is what the calculator returns when the scoring functions gave
            // their not-compared -1, and it holds 0 in every spectral field. Without the flag it
            // carried, Assign would be indistinguishable from a comparison that scored 0. This is
            // the sentinel collapse the flag exists to survive.
            var calculator = new Ms2MatchCalculator();
            var target = Target();
            var reference = Reference();
            reference.Spectrum = new List<SpectrumPeak>();

            var result = calculator.Calculate(new MSScanMatchQuery(target, SpectrumParameter()), reference);
            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreSame(Ms2MatchResult.Empty, result, "the calculator is expected to have collapsed to Empty here");
            Assert.AreEqual(MeasuredTerms.None, recorded.MeasuredTerms);
            Assert.AreEqual(0f, recorded.SquaredSimpleDotProduct,
                "and the field it wrote is a plain 0, which is why the flag is the only way to tell");
        }

        [TestMethod()]
        public void LipidMs2MatchCalculatorRecordsTheSpectrumTerm() {
            var calculator = new LipidMs2MatchCalculator();
            var target = Target();
            var result = calculator.Calculate(new MSScanMatchQuery(target, SpectrumParameter()), Reference());

            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.IsTrue(recorded.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
        }

        [TestMethod()]
        public void AnEmptyLipidMs2MatchResultDoesNotClaimTheSpectrumTerm() {
            var calculator = new LipidMs2MatchCalculator();
            var target = Target();
            var reference = Reference();
            reference.Spectrum = new List<SpectrumPeak>();

            var result = calculator.Calculate(new MSScanMatchQuery(target, SpectrumParameter()), reference);
            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreSame(LipidMs2MatchResult.Empty, result, "the calculator is expected to have collapsed to Empty here");
            Assert.AreEqual(MeasuredTerms.None, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void IsotopesMatchCalculatorRecordsTheIsotopeTerm() {
            var calculator = new IsotopesMatchCalculator();
            var reference = Reference();
            reference.IsotopicPeaks = ReferenceIsotopes();

            var result = calculator.Calculate(new IsotopesMatchQuery(ScanIsotopes(), 810.604, 0.01), reference);
            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreEqual(MeasuredTerms.Isotope, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void IsotopesMatchCalculatorLeavesTheIsotopeTermClearWithNothingToCompare() {
            var calculator = new IsotopesMatchCalculator();

            var result = calculator.Calculate(new IsotopesMatchQuery(null, 810.604, 0.01), Reference());
            var recorded = new MsScanMatchResult();
            result.Assign(recorded);

            Assert.AreEqual(MeasuredTerms.None, recorded.MeasuredTerms);
        }

        [TestMethod()]
        public void TermsFromSeveralCalculatorsAccumulateOnOneResult() {
            // MsScanMatchResultScorer hands the same result to every calculator in turn, so each
            // Assign has to add its own term without clearing the ones already recorded.
            var recorded = new MsScanMatchResult();
            var reference = Reference();
            reference.IsotopicPeaks = ReferenceIsotopes();
            var target = Target();

            new MassMatchCalculator().Calculate(new MassMatchQuery(target.PrecursorMz, 0.01), reference).Assign(recorded);
            new Ms2MatchCalculator().Calculate(new MSScanMatchQuery(target, SpectrumParameter()), reference).Assign(recorded);
            new IsotopesMatchCalculator().Calculate(new IsotopesMatchQuery(ScanIsotopes(), target.PrecursorMz, 0.01), reference).Assign(recorded);

            Assert.AreEqual(
                MeasuredTerms.AccurateMass | MeasuredTerms.Spectrum | MeasuredTerms.Isotope,
                recorded.MeasuredTerms);
        }

        private static MsRefSearchParameterBase SpectrumParameter() {
            return new MsRefSearchParameterBase
            {
                Ms2Tolerance = 0.05f,
                MassRangeBegin = 0,
                MassRangeEnd = 2000,
                SquaredWeightedDotProductCutOff = 0.5f,
                SquaredSimpleDotProductCutOff = 0.5f,
                SquaredReverseDotProductCutOff = 0.5f,
                MatchedPeaksPercentageCutOff = 0.5f,
                MinimumSpectrumMatch = 3,
            };
        }

        private static ChromatogramPeakFeature Target() {
            return new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                CollisionCrossSection = 102,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 86.094, Intensity = 5, },
                    new SpectrumPeak { Mass = 184.073, Intensity = 100, },
                    new SpectrumPeak { Mass = 524.367, Intensity = 1, },
                    new SpectrumPeak { Mass = 810.604, Intensity = 25, },
                },
            };
        }

        private static MoleculeMsReference Reference() {
            return new MoleculeMsReference
            {
                ScanID = 0,
                Name = "PC 18:0_20:4",
                CompoundClass = "PC",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                CollisionCrossSection = 100,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(2, ChromXType.RT, ChromXUnit.Min),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak { Mass = 184.073, Intensity = 100 },
                    new SpectrumPeak { Mass = 506.361, Intensity = 5 },
                    new SpectrumPeak { Mass = 524.372, Intensity = 5 },
                    new SpectrumPeak { Mass = 526.330, Intensity = 5 },
                    new SpectrumPeak { Mass = 544.340, Intensity = 5 },
                    new SpectrumPeak { Mass = 810.601, Intensity = 30 },
                },
            };
        }

        private static List<IsotopicPeak> ScanIsotopes() {
            return new List<IsotopicPeak>
            {
                new IsotopicPeak { RelativeAbundance = 1, },
                new IsotopicPeak { RelativeAbundance = 3.5, },
                new IsotopicPeak { RelativeAbundance = 4, },
            };
        }

        private static List<IsotopicPeak> ReferenceIsotopes() {
            return new List<IsotopicPeak>
            {
                new IsotopicPeak { RelativeAbundance = 1, },
                new IsotopicPeak { RelativeAbundance = 4, },
                new IsotopicPeak { RelativeAbundance = 6, },
            };
        }
    }
}
