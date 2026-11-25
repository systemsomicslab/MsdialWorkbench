using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using RfxFormula = Rfx.Riken.OsakaUniv.Formula;
using RfxIonMode = Rfx.Riken.OsakaUniv.IonMode;
using RfxPeak = Rfx.Riken.OsakaUniv.Peak;
using RfxProductIon = Rfx.Riken.OsakaUniv.ProductIon;
using RfxRawData = Rfx.Riken.OsakaUniv.RawData;

namespace CompMs.Common.Parser.Tests
{
    /// <summary>
    /// The MassBank record of an MS-FINDER query, its export settings and its summary.
    /// </summary>
    [TestClass()]
    public class MsfinderMassBankRecordTests
    {
        private const string Version = "3.73";

        private static string FakeSplash(string printedPeaks) => "splash-" + printedPeaks.Length.ToString(CultureInfo.InvariantCulture);

        private static MsfinderMassBankExportSettings Settings() => new MsfinderMassBankExportSettings
        {
            ContributorId = "MSFINDER_TEST",
            Authors = "Tsugawa H, Kyoto University",
            License = "CC BY",
            Instrument = "TripleTOF 6600, SCIEX",
            InstrumentType = "LC-ESI-QTOF",
        };

        private static MassBankRecordHandler CreateHandler(Func<string, string> splash = null) => Settings().CreateHandler(Version, splash ?? FakeSplash);

        // The hands-on query of L-pyroglutamic acid, as MS-FINDER reads an MS-DIAL MassBank record before PR #872.
        private static RfxRawData Query(string name = "L-Pyroglutamic acid") {
            return new RfxRawData
            {
                Name = name,
                MetaboliteName = name,
                Formula = "C5H7NO3",
                Smiles = "C1CC(=O)N[C@@H]1C(=O)O",
                InchiKey = "ODHCTXKNWHHXJC-UHFFFAOYSA-N",
                Ontology = "Alpha amino acids and derivatives",
                PrecursorMz = 130.04955,
                PrecursorType = "[M+H]+",
                IonMode = RfxIonMode.Positive,
                RetentionTime = 0.635775,
                RetentionIndex = -1,
                Ccs = -1,
                CollisionEnergy = 40,
                Comment = string.Empty,
                Authors = string.Empty,
                License = "CC BY",
                Instrument = "TripleTOF 6600",
                InstrumentType = "QTOF",
            };
        }

        private static List<RfxPeak> Spectrum() {
            return new List<RfxPeak>
            {
                new RfxPeak { Mz = 56.04678, Intensity = 124, },
                new RfxPeak { Mz = 56.05220, Intensity = 308, },
                new RfxPeak { Mz = 84.04584, Intensity = 1638, },
                new RfxPeak { Mz = 130.04871, Intensity = 378, },
            };
        }

        private static MsfinderFormulaCandidate Candidate(string formula, params (double Mz, string Formula)[] ions) {
            return new MsfinderFormulaCandidate(formula, ions.Select(i => new RfxProductIon { Mass = i.Mz, Formula = new RfxFormula { FormulaString = i.Formula, }, }).ToList());
        }

        private static MsfinderFormulaCandidate[] Candidates() {
            return new[]
            {
                Candidate("C4H5N3O2", (84.04584, "C3H4N2O")),
                Candidate("C5H7NO3", (56.04678, "C3H6N"), (56.05220, "C3H6N"), (84.04584, "C4H6NO")),
            };
        }

        private static string[] Lines(MassBankRecordResult result) {
            Assert.IsTrue(result.IsSuccess, result.SkipReason);
            return result.Text.Split('\n');
        }

        private static string Value(string[] lines, string tag) {
            var prefix = tag + ": ";
            return lines.SingleOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal))?.Substring(prefix.Length);
        }

        [TestMethod()]
        public void Build_WritesTheQueryWithMsfindersAccessionAndAnnotation() {
            string printed = null;
            var handler = CreateHandler(peaks => { printed = peaks; return FakeSplash(peaks); });

            var result = MsfinderMassBankRecord.Build(handler, Query(), Spectrum(), Candidates(), 7);

            var lines = Lines(result);
            StringAssert.StartsWith(result.Accession, "MSBNK-MSFINDER_TEST-MSFINDER");
            StringAssert.EndsWith(result.Accession, "00000007");
            Assert.AreEqual(result.Accession.Length, "MSBNK-MSFINDER_TEST-MSFINDER".Length + 12 + 8);
            Assert.AreEqual("L-Pyroglutamic acid; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            Assert.AreEqual("Tsugawa H, Kyoto University", Value(lines, "AUTHORS"));
            Assert.AreEqual("LC-ESI-QTOF", Value(lines, "AC$INSTRUMENT_TYPE"));
            Assert.AreEqual("TripleTOF 6600, SCIEX", Value(lines, "AC$INSTRUMENT"));
            // MS_TYPE before ION_MODE, as the grammar requires.
            var msType = Array.IndexOf(lines, "AC$MASS_SPECTROMETRY: MS_TYPE MS2");
            var ionMode = Array.IndexOf(lines, "AC$MASS_SPECTROMETRY: ION_MODE POSITIVE");
            Assert.IsTrue(msType >= 0 && msType < ionMode);
            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: COLLISION_ENERGY 40");
            CollectionAssert.Contains(lines, "AC$CHROMATOGRAPHY: RETENTION_TIME 0.636 min");
            CollectionAssert.Contains(lines, "MS$FOCUSED_ION: PRECURSOR_TYPE [M+H]+");
            CollectionAssert.Contains(lines, "MS$DATA_PROCESSING: WHOLE MS-FINDER 3.73");
            CollectionAssert.Contains(lines, "PK$ANNOTATION: m/z tentative_formula formula_count mass error(ppm)");
            Assert.AreEqual(3, lines.Count(l => l.StartsWith("  ", StringComparison.Ordinal) && l.Contains("+ 1 ")), "The product ions of C5H7NO3, the CH$FORMULA, not of the top candidate.");
            Assert.AreEqual("56.04678:124.00 56.05220:308.00 84.04584:1638.00 130.04871:378.00", printed);
            Assert.AreEqual(FakeSplash(printed), Value(lines, "PK$SPLASH"));
        }

        [TestMethod()]
        public void Build_GeneratesTheInChIKeyFromTheStructureAndNeverCopiesTheQuerys() {
            var query = Query();
            query.InchiKey = "AAAAAAAAAAAAAA-UHFFFAOYSA-N";

            var lines = Lines(MsfinderMassBankRecord.Build(CreateHandler(), query, Spectrum(), Candidates(), 1));

            Assert.AreEqual("INCHIKEY ODHCTXKNWHHXJC-VKHMYHEASA-N", Value(lines, "CH$LINK"));
        }

        [DataTestMethod()]
        [DataRow(null, MassBankAnnotationLevel.Unknown)]
        [DataRow("", MassBankAnnotationLevel.Unknown)]
        [DataRow("  ", MassBankAnnotationLevel.Unknown)]
        [DataRow("Unknown", MassBankAnnotationLevel.Unknown)]
        [DataRow("unknown", MassBankAnnotationLevel.Unknown)]
        [DataRow("Unknown|ID=12|MZ=165.0537|RT=2.444", MassBankAnnotationLevel.Unknown)]
        [DataRow("low score: 4-Coumaric acid", MassBankAnnotationLevel.Suggested)]
        [DataRow("w/o MS2: 4-Coumaric acid", MassBankAnnotationLevel.Suggested)]
        [DataRow("Unsettled: 4-Coumaric acid", MassBankAnnotationLevel.Suggested)]
        [DataRow("4-Coumaric acid", MassBankAnnotationLevel.Unspecified)]
        [DataRow("Unknown compound 3", MassBankAnnotationLevel.Unspecified)]
        public void Classify_ReadsTheLevelFromTheName(string name, MassBankAnnotationLevel expected) {
            Assert.AreEqual(expected, MsfinderMassBankRecord.Classify(name));
        }

        [TestMethod()]
        public void Build_WritesAnMsdialUnknownWithoutItsStructure() {
            var query = Query("Unknown|ID=12|MZ=130.0496|RT=0.636");

            var result = MsfinderMassBankRecord.Build(CreateHandler(), query, Spectrum(), Candidates(), 2);

            var lines = Lines(result);
            Assert.AreEqual("Unknown", Value(lines, "CH$NAME"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$FORMULA"));
            Assert.IsNull(Value(lines, "CH$LINK"));
            // The record states no formula, so the first candidate's product ions are written and the comment names it.
            CollectionAssert.Contains(lines, "COMMENT: " + MassBankRecordHandler.TentativePrecursorFormulaComment + "C4H5N3O2");
            Assert.AreEqual(1, lines.Count(l => l.StartsWith("  84.04584 C3H4N2O+ 1 ", StringComparison.Ordinal)));
        }

        [TestMethod()]
        public void Build_MarksASuggestionAndTakesTheMetaboliteNameFirst() {
            var query = Query();
            query.Name = "Scan 1234";
            query.MetaboliteName = "low score: L-Pyroglutamic acid";

            var lines = Lines(MsfinderMassBankRecord.Build(CreateHandler(), query, Spectrum(), Candidates(), 3));

            Assert.AreEqual("L-Pyroglutamic acid", Value(lines, "CH$NAME"));
            CollectionAssert.Contains(lines, "COMMENT: " + MassBankRecordHandler.SuggestedAnnotationComment);
            CollectionAssert.Contains(lines, "COMMENT: Original name: low score: L-Pyroglutamic acid");

            query.MetaboliteName = string.Empty;
            Assert.AreEqual("Scan 1234", MsfinderMassBankRecord.CompoundName(query));
        }

        [TestMethod()]
        public void Build_WritesTheQuerysOwnValuesAndPutsTheHandlersBack() {
            var handler = CreateHandler();
            handler.Comment = "export comment";
            handler.CollisionEnergy = "20 V";
            var query = Query();
            query.PrecursorType = "[M-H]-";
            query.PrecursorMz = 128.03532;
            query.IonMode = RfxIonMode.Negative;
            query.CollisionEnergy = 35.5;
            query.Comment = "first; second";
            var negative = new[] { Candidate("C5H7NO3", (84.04584, "C4H6NO")), };

            var lines = Lines(MsfinderMassBankRecord.Build(handler, query, Spectrum(), negative, 4));

            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: ION_MODE NEGATIVE");
            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: COLLISION_ENERGY 35.5");
            CollectionAssert.Contains(lines, "COMMENT: first; second");
            Assert.IsTrue(lines.Any(l => l.StartsWith("  84.04584 C4H6NO- 1 ", StringComparison.Ordinal)));
            Assert.AreEqual("export comment", handler.Comment);
            Assert.AreEqual("20 V", handler.CollisionEnergy);
            Assert.AreEqual(CompMs.Common.Enum.IonMode.Positive, handler.IonMode);
        }

        [TestMethod()]
        public void Build_RefusesAQueryWithoutPeaksOrAnAccessionTwice() {
            var handler = CreateHandler();

            Assert.AreEqual(MassBankRecordHandler.NoPeaksReason, MsfinderMassBankRecord.Build(handler, Query(), new List<RfxPeak>(), Candidates(), 5).SkipReason);
            Assert.IsTrue(MsfinderMassBankRecord.Build(handler, Query(), Spectrum(), Candidates(), 6).IsSuccess);
            Assert.AreEqual(MassBankRecordHandler.DuplicateAccessionReason, MsfinderMassBankRecord.Build(handler, Query(), Spectrum(), Candidates(), 6).SkipReason);
        }

        [DataTestMethod()]
        [DataRow(40d, "40")]
        [DataRow(35.5, "35.5")]
        [DataRow(12.34567, "12.346")]
        [DataRow(0d, "")]
        [DataRow(-1d, "")]
        [DataRow(double.NaN, "")]
        [DataRow(double.PositiveInfinity, "")]
        public void CollisionEnergyText_IsTheNumberInInvariantCulture(double value, string expected) {
            Assert.AreEqual(expected, MsfinderMassBankRecord.CollisionEnergyText(value));
        }

        [TestMethod()]
        public void Settings_ValidateWhatTheFilesSayAndMakeAnMsfinderHandler() {
            var suggested = MsfinderMassBankExportSettings.Suggest(new[] { Query("a"), Query("b"), });
            Assert.AreEqual(string.Empty, suggested.Authors);
            Assert.AreEqual("CC BY", suggested.License);
            Assert.AreEqual("TripleTOF 6600", suggested.Instrument);
            Assert.AreEqual("QTOF", suggested.InstrumentType);
            Assert.AreEqual(string.Empty, suggested.ContributorId);

            var problems = suggested.Validate(Version);
            Assert.IsTrue(problems.Any(p => p.Contains("Instrument type \"QTOF\"")));
            Assert.IsTrue(problems.Any(p => p.StartsWith("Authors", StringComparison.Ordinal)));
            Assert.IsTrue(problems.Any(p => p.StartsWith("Contributor ID", StringComparison.Ordinal)));
            Assert.AreEqual(0, Settings().Validate(Version).Count);

            var handler = CreateHandler();
            Assert.AreEqual(MsfinderMassBankRecord.SoftwareCode, handler.Software);
            Assert.AreEqual(MsfinderMassBankRecord.ProcessingSoftware, handler.ProcessingSoftware);
            Assert.AreEqual(Version, handler.MsdialVersion);
        }

        [TestMethod()]
        public void Settings_SuggestOnlyWhatEveryQueryAgreesOn() {
            var other = Query();
            other.Instrument = "Q Exactive";
            other.Authors = "  ";

            var suggested = MsfinderMassBankExportSettings.Suggest(new[] { Query(), other, null, });

            Assert.AreEqual(string.Empty, suggested.Instrument);
            Assert.AreEqual(string.Empty, suggested.Authors);
            Assert.AreEqual("QTOF", suggested.InstrumentType);
            Assert.AreEqual(string.Empty, MsfinderMassBankExportSettings.Suggest(null).License);
        }

        [TestMethod()]
        public void Settings_NeverExportIntoTheProjectFolder() {
            var project = Path.Combine(Path.GetTempPath(), "msfinder_project");

            Assert.IsNotNull(MsfinderMassBankExportSettings.ValidateDestination(string.Empty, project));
            Assert.IsNotNull(MsfinderMassBankExportSettings.ValidateDestination("relative\\folder", project));
            Assert.IsNotNull(MsfinderMassBankExportSettings.ValidateDestination(project, project));
            Assert.IsNotNull(MsfinderMassBankExportSettings.ValidateDestination(project.ToUpperInvariant() + Path.DirectorySeparatorChar, project));
            Assert.IsNull(MsfinderMassBankExportSettings.ValidateDestination(project + "_MassBank", project));
            Assert.IsNull(MsfinderMassBankExportSettings.ValidateDestination(project + "_MassBank", null));
        }

        [TestMethod()]
        public void Summary_CountsWhatWasWrittenSkippedAndAlreadyThere() {
            var folder = Path.Combine(Path.GetTempPath(), "msfinder_massbank_summary_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            try {
                var handler = CreateHandler();
                var summary = new MsfinderMassBankExportSummary();
                var written = MsfinderMassBankRecord.Build(handler, Query("Unknown"), Spectrum(), Candidates(), 1);
                var path = Path.Combine(folder, written.FileName);
                File.WriteAllText(path, written.Text);
                summary.AddWritten(written, path);
                summary.AddSkipped("refused: " + MassBankRecordHandler.NoPeaksReason);
                summary.AddSkipped("refused: " + MassBankRecordHandler.NoPeaksReason);
                File.WriteAllText(Path.Combine(folder, "MSBNK-OTHER-RECORD0001.txt"), "earlier export");
                File.WriteAllText(Path.Combine(folder, "notes.txt"), "not a record");

                summary.CountOtherRecords(folder, handler.Identifier);

                Assert.AreEqual(1, summary.Written);
                Assert.AreEqual(2, summary.Skipped);
                Assert.AreEqual(1, summary.OtherRecords);
                var message = summary.ToMessage(folder);
                StringAssert.StartsWith(message, $"1 MassBank record was written to {folder}.");
                StringAssert.Contains(message, "2 queries were not exported:");
                StringAssert.Contains(message, "  2  refused: " + MassBankRecordHandler.NoPeaksReason);
                StringAssert.Contains(message, "1 MassBank record in the destination was not written by this export");
                StringAssert.Contains(message, "Written with these adjustments for MassBank:");
            }
            finally {
                Directory.Delete(folder, true);
            }
        }
    }
}
