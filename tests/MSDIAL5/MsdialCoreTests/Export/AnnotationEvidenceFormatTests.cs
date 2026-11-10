using CompMs.Common.DataObj.Result;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.Export.Tests
{
    /// <summary>
    /// The four decisions the evidence columns rest on: not-recorded is never the enum's own default
    /// name, zero is never not-recorded, the flag separator is never a comma, and the flag order is
    /// fixed.
    /// </summary>
    /// <remarks>
    /// Each of these is a way the columns could be quietly wrong rather than visibly broken, so each
    /// gets a test of its own. See AnnotationEvidenceFormat for why each rule exists.
    /// </remarks>
    [TestClass()]
    public class AnnotationEvidenceFormatTests
    {
        [TestMethod()]
        public void TheDefaultOfEachEnumIsWrittenAsNotRecordedAndNotAsItsOwnName() {
            var unrecorded = new MsScanMatchResult();

            // "None" and "Unspecified" are what every project saved before keys 39-43 existed
            // deserializes to. Writing those words would publish "nothing was compared" about a run
            // that simply never recorded the fact.
            Assert.AreEqual("null", AnnotationEvidenceFormat.Terms(unrecorded));
            Assert.AreEqual("null", AnnotationEvidenceFormat.Source(unrecorded));
        }

        [TestMethod()]
        public void ANullResultIsAlsoNotRecorded() {
            Assert.AreEqual("null", AnnotationEvidenceFormat.Terms(null));
            Assert.AreEqual("null", AnnotationEvidenceFormat.Source(null));
        }

        [TestMethod()]
        public void AZeroCountIsAMeasurementAndOnlyNullIsNotRecorded() {
            // The distinction the int? typing was chosen for, and the one an accidental trip through
            // ValueOrNull would destroy: two of its four overloads return "null" for anything within
            // 1e-10 of zero.
            Assert.AreEqual("0", AnnotationEvidenceFormat.Count(0), "no candidate was found, and that is a fact");
            Assert.AreEqual("null", AnnotationEvidenceFormat.Count(null), "the run did not record a population");
            Assert.AreEqual("12", AnnotationEvidenceFormat.Count(12));
        }

        [TestMethod()]
        public void FlagsAreJoinedWithAPipeAndNeverAComma() {
            // Enum.ToString() would give "Spectrum, AccurateMass" here. AlignmentLongCSVExporter
            // writes metadata values unquoted, and with ExportFormat.Csv that comma becomes a live
            // separator, so the row would gain a phantom column and everything to its right would
            // shift.
            var terms = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass;

            var text = AnnotationEvidenceFormat.TermList(terms);

            Assert.AreEqual("Spectrum|AccurateMass", text);
            Assert.IsFalse(text.Contains(","));
            Assert.IsFalse(text.Contains("\t"));
        }

        [TestMethod()]
        public void FlagsComeOutInDeclarationOrderWhicheverOrderTheyWereSet() {
            // Listed explicitly by the formatter rather than left to Enum.ToString(), so that the
            // column stays stable if a member is ever added out of order.
            var terms = MeasuredTerms.Isotope | MeasuredTerms.RetentionTime | MeasuredTerms.Spectrum;

            Assert.AreEqual("Spectrum|RetentionTime|Isotope", AnnotationEvidenceFormat.TermList(terms));
        }

        [TestMethod()]
        public void ASingleFlagIsWrittenAlone() {
            Assert.AreEqual("Ccs", AnnotationEvidenceFormat.TermList(MeasuredTerms.Ccs));
        }

        [TestMethod()]
        public void ARecordedResultRendersItsValues() {
            var result = new MsScanMatchResult
            {
                MeasuredTerms = MeasuredTerms.Spectrum | MeasuredTerms.AccurateMass,
                EvidenceSource = AnnotationEvidenceSource.ReferenceSpectrum,
            };

            Assert.AreEqual("Spectrum|AccurateMass", AnnotationEvidenceFormat.Terms(result));
            Assert.AreEqual("ReferenceSpectrum", AnnotationEvidenceFormat.Source(result));
        }

        /// <summary>
        /// Both in-silico directions publish as one tag, because the evidence inventory records
        /// in-silico assignment as one category. Which tool it was is on the record beside this, in
        /// AnnotatorID.
        /// </summary>
        [TestMethod()]
        public void BothInSilicoDirectionsArePublishedAsOneTag() {
            var structurePredicted = new MsScanMatchResult { EvidenceSource = AnnotationEvidenceSource.ByStructurePredictionTool, };
            var spectrumPredicted = new MsScanMatchResult { EvidenceSource = AnnotationEvidenceSource.BySpectrumPredictionTool, };

            Assert.AreEqual("InSilico", AnnotationEvidenceFormat.Source(structurePredicted));
            Assert.AreEqual("InSilico", AnnotationEvidenceFormat.Source(spectrumPredicted));
            Assert.AreEqual(AnnotationEvidenceFormat.InSilico, AnnotationEvidenceFormat.Source(structurePredicted));
        }

        [TestMethod()]
        public void EveryOtherMemberStillPublishesItsOwnName() {
            var ruleBased = new MsScanMatchResult { EvidenceSource = AnnotationEvidenceSource.RuleBased, };
            Assert.AreEqual("RuleBased", AnnotationEvidenceFormat.Source(ruleBased));
        }

        [TestMethod()]
        public void TheNotRecordedTokenMatchesTheScoreColumnsBesideIt() {
            // The two formatters must agree, or a reader has to learn two conventions to read one
            // row. AnnotationScoreFormat exists because the two exports once disagreed about exactly
            // this token.
            Assert.AreEqual(AnnotationScoreFormat.NotComputed, AnnotationEvidenceFormat.NotRecorded);
        }
    }
}
