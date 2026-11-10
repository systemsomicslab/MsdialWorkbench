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
    /// Pins today's answer: a text-database annotation is marked IsReferenceMatched on precursor
    /// mass agreement alone, with no spectrum ever opened.
    /// </summary>
    /// <remarks>
    /// THIS TEST IS MEANT TO FAIL when IsReferenceMatched is given the meaning its name carries.
    /// It records the present behaviour so that the flip is a deliberate change, and so that
    /// whoever makes it can see from one place what the four text-database annotators currently
    /// assert. The correct value after that change is noted on the assertion.
    ///
    /// Why it matters here rather than in the abstract: IsReferenceMatched is a stored key read at
    /// well over a hundred sites, it is the first component of the container's ranking key, and it
    /// is what the exporters and the mzTab-M writer treat as "this name is supported by a reference
    /// match". A text-database row carries no spectrum at all, so on this path the flag means only
    /// that a mass agreed -- and in the public-repository campaign, where text databases are used
    /// for retention-time-anchored libraries, those names are today indistinguishable in an export
    /// from an MS/MS reference match.
    ///
    /// The evidence record added in the preceding commits is what makes this checkable rather than
    /// arguable: on the very same object, EvidenceSource says PrecursorOnly and MeasuredTerms
    /// leaves the Spectrum bit clear. The flag and the record disagree, and the record is the one
    /// derived from what actually ran.
    ///
    /// LC-MS is asserted here because it is the campaign's mode. The other three text-database
    /// annotators -- Dims, Imms, Lcimms -- set the flag the same way, and each already has an
    /// evidence-record witness in its own project proving no spectrum was opened, so a fix must
    /// visit all four.
    ///
    /// Tagged PinnedDefect: <c>dotnet test --filter TestCategory=PinnedDefect</c> lists every test
    /// that records behaviour known to be wrong. Each is expected to fail when its defect is fixed,
    /// and each names the correct value on the assertion.
    /// </remarks>
    [TestClass()]
    [TestCategory("PinnedDefect")]
    public class TextDbVerdictWithoutASpectrumTests
    {
        [TestMethod()]
        public void ATextDatabaseHitIsCalledAReferenceMatchWithNoSpectrumCompared() {
            var result = Annotate();

            // CORRECT VALUE AFTER THE FIX: false, with IsAnnotationSuggested true instead --
            // a precursor-only suggestion, which is what it is.
            Assert.IsTrue(result.IsReferenceMatched,
                "today a mass agreement alone is recorded as a reference match");
        }

        [TestMethod()]
        public void TheEvidenceRecordOnTheSameObjectSaysNoSpectrumWasOpened() {
            var result = Annotate();

            // Both of these stay true after the fix. They are the reason the assertion above is a
            // defect and not a design choice: the run itself recorded that it compared no spectrum.
            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
            Assert.AreEqual(0f, result.SquaredSimpleDotProduct,
                "and no spectral score was written at all -- this is the unset default, which is "
                + "exactly why the record beside it is needed to tell that apart from a real zero");
        }

        [TestMethod()]
        public void SoTheVerdictAndTheRecordContradictEachOther() {
            var result = Annotate();

            // The pair, stated as one assertion so that the contradiction itself is what is
            // pinned. It holds today, and that is the defect. AFTER THE FIX invert this to
            // IsFalse: a result cannot both be a reference match and have had no spectrum
            // compared.
            Assert.IsTrue(
                result.IsReferenceMatched && !result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum),
                "today one object asserts both that a reference matched and that no spectrum was "
                + "opened; the record is the half derived from what actually ran");
        }

        private static MsScanMatchResult Annotate() {
            var reference = new MoleculeMsReference
            {
                ScanID = 0,
                Name = "a text database row",
                InChIKey = "DUMMYINCHIKEY",
                PrecursorMz = 810.601,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                ChromXs = new ChromXs(2.0, ChromXType.RT, ChromXUnit.Min),
                // No Spectrum: a text database holds none. That is the whole point.
            };
            var parameter = new MsRefSearchParameterBase
            {
                Ms1Tolerance = 0.01f,
                Ms2Tolerance = 0.05f,
                RtTolerance = 0.5f,
                TotalScoreCutoff = 0,
                IsUseTimeForAnnotationScoring = true,
            };
            var database = new MoleculeDataBase(
                new List<MoleculeMsReference> { reference, }, "TextDB", DataBaseSource.Text, SourceType.TextDB, "TextPath");
            var annotator = new LcmsTextDBAnnotator(database, parameter, "TextDB", -1);
            var target = new ChromatogramPeakFeature
            {
                PrecursorMz = 810.604,
                ChromXs = new ChromXs(2.2, ChromXType.RT, ChromXUnit.Min),
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
