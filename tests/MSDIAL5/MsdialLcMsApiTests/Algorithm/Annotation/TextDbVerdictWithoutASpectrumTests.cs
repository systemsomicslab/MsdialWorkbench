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
    /// A text-database hit is a reference match although no spectrum is compared.
    /// </summary>
    /// <remarks>
    /// A text library holds no spectra. What it lists are compounds the user is confident are in
    /// the sample, retention time included, so a row whose precursor mass (and retention time)
    /// agrees is a reference match: IsReferenceMatched is true by design, not on the strength of a
    /// spectrum.
    ///
    /// The evidence record still says what was measured: EvidenceSource is PrecursorOnly and the
    /// Spectrum bit of MeasuredTerms is clear. The two answer different questions, the verdict
    /// whether the library supports the name and the record what the run compared, so a reader of
    /// an export who needs to know whether a spectrum was compared reads the record.
    ///
    /// LC-MS is asserted here. The Dims, Imms and Lcimms text-database annotators set the flag the
    /// same way, and their evidence records are checked in MeasuredTermsAreRecordedTests of each
    /// project.
    /// </remarks>
    [TestClass()]
    public class TextDbVerdictWithoutASpectrumTests
    {
        [TestMethod()]
        public void ATextDatabaseHitIsAReferenceMatchWithNoSpectrumCompared() {
            var result = Annotate();

            Assert.IsTrue(result.IsReferenceMatched,
                "the user trusts what a text library lists, so an agreeing row is a reference match");
            Assert.IsFalse(result.IsAnnotationSuggested);
        }

        [TestMethod()]
        public void TheEvidenceRecordSaysNoSpectrumWasCompared() {
            var result = Annotate();

            Assert.AreEqual(AnnotationEvidenceSource.PrecursorOnly, result.EvidenceSource);
            Assert.IsFalse(result.MeasuredTerms.HasFlag(MeasuredTerms.Spectrum));
            Assert.AreEqual(0f, result.SquaredSimpleDotProduct,
                "no spectral score was written at all -- this is the unset default, which is "
                + "exactly why the record beside it is needed to tell that apart from a real zero");
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
