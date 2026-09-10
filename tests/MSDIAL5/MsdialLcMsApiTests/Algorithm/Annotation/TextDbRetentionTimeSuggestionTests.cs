using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialLcMsApi.Algorithm.Annotation.Tests
{
    /// <summary>
    /// A text-database row whose retention time disagrees is lowered to a suggestion, not erased.
    /// </summary>
    /// <remarks>
    /// This annotator had no suggestion path at all: ValidateCore set IsReferenceMatched and left
    /// IsAnnotationSuggested at its default of false. Since FilterByThreshold is the disjunction of
    /// the two and StandardAnnotationProcess stores only what that returns, a retention-time
    /// disagreement did not demote the row, it deleted it -- with no trace in the file that a
    /// reference of that mass had ever been considered.
    ///
    /// That matters most here of all the annotators, because a retention-time-anchored text database
    /// is the case text databases are used for in the public-repository campaign, and because a text
    /// database row is precursor-only evidence by construction. "The mass agreed, the retention time
    /// did not" is exactly what a suggestion is for.
    ///
    /// The reference match itself is untouched: <see cref="AnAgreeingRowIsStillAReferenceMatch"/> is
    /// the control. Note that the flag it asserts is itself under separate review -- a text database
    /// holds no spectrum, so calling a mass agreement a reference match is the defect pinned by
    /// TextDbVerdictWithoutASpectrumTests. This test asserts today's answer for that question and
    /// only changes the retention-time half.
    /// </remarks>
    [TestClass()]
    public class TextDbRetentionTimeSuggestionTests
    {
        [TestMethod()]
        public void ADisagreeingRowIsNoLongerAReferenceMatch() {
            var result = Annotate(peakRetentionTime: 10.0d);

            Assert.IsFalse(result.IsRtMatch);
            Assert.IsFalse(result.IsReferenceMatched, "8 minutes away from the library retention time");
        }

        [TestMethod()]
        public void ButItSurvivesAsASuggestion() {
            var result = Annotate(peakRetentionTime: 10.0d);

            Assert.IsTrue(result.IsAnnotationSuggested,
                "the precursor mass still agreed; before this the row vanished from the results "
                + "entirely, because this annotator set no suggestion at all");
        }

        [TestMethod()]
        public void AndTheFilterThatDecidesWhatIsStoredKeepsIt() {
            var result = Annotate(peakRetentionTime: 10.0d);
            var evaluator = new MsScanMatchResultEvaluator(Parameter());

            var kept = evaluator.FilterByThreshold(new List<MsScanMatchResult> { result, });

            Assert.AreEqual(1, kept.Count);
        }

        [TestMethod()]
        public void AnAgreeingRowIsStillAReferenceMatch() {
            // The control: the change must only affect rows that miss.
            var result = Annotate(peakRetentionTime: 2.2d);

            Assert.IsTrue(result.IsRtMatch);
            Assert.IsTrue(result.IsReferenceMatched);
            Assert.IsFalse(result.IsAnnotationSuggested, "a reference match is not also a suggestion");
        }

        private static MsRefSearchParameterBase Parameter() {
            return new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationScoring = true,
            };
        }

        private static MsScanMatchResult Annotate(double peakRetentionTime) {
            var parameter = Parameter();
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a retention-time-anchored row",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min),
            };
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference> { reference, }, "TextDB", DataBaseSource.Text, SourceType.TextDB, "TextPath");
            var annotator = new LcmsTextDBAnnotator(database, parameter, "TextDB", -1);
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
            var query = new AnnotationQuery(target, target, null, null, parameter, annotator, ignoreIsotopicPeak: false);
            return annotator.CalculateScore(query, reference);
        }
    }
}
