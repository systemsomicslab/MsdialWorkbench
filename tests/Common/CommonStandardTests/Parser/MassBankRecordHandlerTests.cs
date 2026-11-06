using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class MassBankRecordHandlerTests
    {
        private const string Contributor = "MSDIAL_TEST";

        // Stands in for NSSplash: what matters here is that the handler hashes exactly the peak list it prints.
        private static string FakeSplash(string printedPeaks) {
            using (var sha = SHA1.Create()) {
                return "splash-" + string.Concat(sha.ComputeHash(Encoding.UTF8.GetBytes(printedPeaks)).Select(b => b.ToString("x2", CultureInfo.InvariantCulture)));
            }
        }

        private static MassBankRecordHandler CreateHandler(Func<string, string> splash = null) {
            return new MassBankRecordHandler(IonMode.Positive, "LC-ESI-QTOF", splash ?? FakeSplash)
            {
                Authors = "Tsugawa H, Kyoto University",
                License = "CC BY",
                Instrument = "TripleTOF 6600, SCIEX",
                ContributorIdentifier = Contributor,
            };
        }

        // L-pyroglutamic acid as a deconvoluted spectrum holds it: full double precision.
        private static MoleculeMsReference Pyroglutamic() {
            return new MoleculeMsReference
            {
                Name = "L-Pyroglutamic acid",
                Formula = FormulaStringParcer.OrganicElementsReader("C5H7NO3"),
                Ontology = "Alpha amino acids and derivatives",
                SMILES = "C1CC(=O)N[C@@H]1C(=O)O",
                InChIKey = "ODHCTXKNWHHXJC-VKHMYHEASA-N",
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak(56.0467913204, 124.3710022),
                    new SpectrumPeak(56.0522047119, 308.9120178),
                    new SpectrumPeak(84.0458377361, 1638.2399902),
                    new SpectrumPeak(130.0487118721, 378.5499878),
                },
            };
        }

        private static ChromatogramPeak Peak(int id = 1101, double mz = 130.0499021411, double rt = 0.6358123779) {
            return new ChromatogramPeak(id, mz, 1000d, new ChromXs(rt));
        }

        private static string[] Lines(MassBankRecordResult result) {
            Assert.IsTrue(result.IsSuccess, result.SkipReason);
            return result.Text.Split('\n');
        }

        private static string Value(string[] lines, string tag) {
            var prefix = tag + ": ";
            var line = lines.SingleOrDefault(l => l.StartsWith(prefix, StringComparison.Ordinal));
            return line?.Substring(prefix.Length);
        }

        private static string[] PeakLines(string[] lines) {
            return lines.SkipWhile(l => l != "PK$PEAK: m/z int. rel.int.").Skip(1).TakeWhile(l => l != "//").ToArray();
        }

        [TestMethod()]
        public void WriteRecordTest_InChIKeyLinkHasNoColonAfterDatabaseName() {
            var handler = CreateHandler(_ => "NA");
            var peak = new ChromatogramPeak(1, 76.03930, 1000d, new ChromXs(1.5));
            var reference = new MoleculeMsReference
            {
                Name = "Glycine",
                Formula = new Formula(),
                SMILES = "NCC(=O)O",
                InChIKey = "DHMQDGOQFOQNFH-UHFFFAOYSA-N",
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak(30.03383, 100d), },
            };

            string[] lines;
            using (var stream = new MemoryStream()) {
                handler.WriteRecord(stream, peak, reference, reference, reference);
                lines = Encoding.UTF8.GetString(stream.ToArray()).Split(new[] { "\r\n", "\n" }, System.StringSplitOptions.None);
            }

            // MassBank Record Format 2.6.0, 2.2.8: "CH$LINK: INCHIKEY UFFBMTHBGFGIHF-UHFFFAOYSA-N"
            var link = lines.Single(line => line.StartsWith("CH$LINK: INCHIKEY"));
            Assert.AreEqual("CH$LINK: INCHIKEY DHMQDGOQFOQNFH-UHFFFAOYSA-N", link);
        }

        [TestMethod()]
        public void Splash_IsComputedFromThePrintedPeaks() {
            string received = null;
            var handler = CreateHandler(peaks => { received = peaks; return FakeSplash(peaks); });
            var reference = Pyroglutamic();

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            // Recompute from the record text, as the Validator does.
            var printed = string.Join(" ", PeakLines(lines).Select(l => l.Trim().Split(' ')).Select(f => f[0] + ":" + f[1]));
            Assert.AreEqual("56.04679:124.37 56.05220:308.91 84.04584:1638.24 130.04871:378.55", printed);
            Assert.AreEqual(printed, received);
            Assert.AreEqual(FakeSplash(printed), Value(lines, "PK$SPLASH"));
            CollectionAssert.AreEqual(
                new[] { "  56.04679 124.37 76", "  56.05220 308.91 188", "  84.04584 1638.24 999", "  130.04871 378.55 231", },
                PeakLines(lines));
            Assert.AreEqual("4", Value(lines, "PK$NUM_PEAK"));
        }

        [TestMethod()]
        public void Peaks_ThatPrintTheSameAreMergedAndSorted() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Spectrum = new List<SpectrumPeak>
            {
                new SpectrumPeak(130.04871, 378d),
                new SpectrumPeak(84.045851, 1638d),
                new SpectrumPeak(56.04678, 124d),
                new SpectrumPeak(84.045849, 900d),
                new SpectrumPeak(60.0, 0.001), // prints as 0.00
            };

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            CollectionAssert.AreEqual(new[] { "  56.04678 124.00 76", "  84.04585 1638.00 999", "  130.04871 378.00 231", }, PeakLines(lines));
            Assert.AreEqual("3", Value(lines, "PK$NUM_PEAK"));
            Assert.AreEqual(FakeSplash("56.04678:124.00 84.04585:1638.00 130.04871:378.00"), Value(lines, "PK$SPLASH"));
            Assert.IsFalse(lines.Any(l => l.StartsWith("PK$ANNOTATION")), "PK$ANNOTATION is omitted when no peak is annotated.");
        }

        [DataTestMethod()]
        [DataRow("de-DE")]
        [DataRow("th-TH")]
        [DataRow("ar-SA")]
        [DataRow("fr-FR")]
        public void Numbers_AndDates_AreInvariantWhateverTheCulture(string culture) {
            var previous = Thread.CurrentThread.CurrentCulture;
            try {
                Thread.CurrentThread.CurrentCulture = new CultureInfo(culture);
                var handler = CreateHandler();
                var reference = Pyroglutamic();
                reference.CollisionCrossSection = 152.43;
                var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

                var date = Value(lines, "DATE");
                Assert.IsTrue(DateTime.TryParseExact(date, "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed), date);
                Assert.IsTrue(Math.Abs((parsed - DateTime.UtcNow.Date).TotalDays) <= 1, $"DATE {date} is not the Gregorian date of today.");
                Assert.AreEqual($"MSBNK-{Contributor}-MSDIAL{parsed.ToString("yyMMdd", CultureInfo.InvariantCulture)}00001101", Value(lines, "ACCESSION"));
                Assert.AreEqual("129.04259", Value(lines, "CH$EXACT_MASS"));
                CollectionAssert.Contains(lines, "MS$FOCUSED_ION: PRECURSOR_M/Z 130.04990");
                CollectionAssert.Contains(lines, "AC$CHROMATOGRAPHY: RETENTION_TIME 0.636 min");
                CollectionAssert.Contains(lines, "AC$CHROMATOGRAPHY: CCS 152.43");
                CollectionAssert.AreEqual(new[] { "  56.04679 124.37 76", "  56.05220 308.91 188", "  84.04584 1638.24 999", "  130.04871 378.55 231", }, PeakLines(lines));
            }
            finally {
                Thread.CurrentThread.CurrentCulture = previous;
            }
        }

        [TestMethod()]
        public void Structure_InChIKeyComesFromTheGeneratedInChI_NotFromTheLibrary() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.InChIKey = "ODHCTXKNWHHXJC-UHFFFAOYSA-N"; // flat key next to a stereo SMILES

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("InChI=1S/C5H7NO3/c7-4-2-1-3(6-4)5(8)9/h3H,1-2H2,(H,6,7)(H,8,9)/t3-/m0/s1", Value(lines, "CH$IUPAC"));
            Assert.AreEqual("INCHIKEY ODHCTXKNWHHXJC-VKHMYHEASA-N", Value(lines, "CH$LINK"));
            Assert.AreEqual("C5H7NO3", Value(lines, "CH$FORMULA"));
            Assert.AreEqual("129.04259", Value(lines, "CH$EXACT_MASS"));
        }

        [DataTestMethod()]
        [DataRow("C[N+](C)(C)CCO", "C5H14NO", "[C5H14NO]+", "104.10699")] // permanent cation, in MassBank charge notation
        [DataRow("C[N+](C)(C)CC(=O)[O-]", "C5H11NO2", "C5H11NO2", "117.07898")] // zwitterion
        [DataRow("C[N+](C)(C)CCO.[Cl-]", "C5H14ClNO", "C5H14ClNO", "139.07639")] // salt: both components kept
        [DataRow("C[Se]CC[C@@H](C(=O)O)N", "C5H11NO2Se", "C5H11NO2Se", "196.99550")] // Se, which the library formula reader drops
        [DataRow("[2H]C([2H])([2H])N1C=NC2=C1C(=O)N(C(=O)N2C)C", "C8H7D3N4O2", "C8H10N4O2", "197.09921")] // labelled: formula collapsed, mass labelled
        [DataRow("Cl", "HCl", "ClH", "35.97668")] // no carbon: alphabetical
        public void Structure_FormulaAndMassAreDerivedFromTheStructure(string smiles, string libraryFormula, string formula, string exactMass) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = smiles;
            reference.Formula = FormulaStringParcer.OrganicElementsReader(libraryFormula);
            reference.InChIKey = "XXXXXXXXXXXXXX-XXXXXXXXXX-X";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual(smiles, Value(lines, "CH$SMILES"));
            StringAssert.StartsWith(Value(lines, "CH$IUPAC"), "InChI=1S/");
            Assert.AreEqual(formula, Value(lines, "CH$FORMULA"));
            Assert.AreEqual(exactMass, Value(lines, "CH$EXACT_MASS"));
            Assert.IsFalse(lines.Any(l => l.Contains("XXXXXXXXXXXXXX")), "The library InChIKey must not be written.");
        }

        [DataTestMethod()]
        [DataRow("")] // InChIKey but no SMILES, common in MSP libraries
        [DataRow("c1ccnc1")] // no Kekule structure
        [DataRow("C1CC(=O")] // unclosed branch
        [DataRow("*NCCO")] // wildcard
        [DataRow("C$C")] // NCDK throws on the quadruple bond
        public void Structure_WithoutAUsableStructure_IsNotAvailable(string smiles) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = smiles;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$IUPAC"));
            Assert.IsNull(Value(lines, "CH$LINK"));
            Assert.AreEqual("C5H7NO3", Value(lines, "CH$FORMULA"), "The library formula is kept when it is valid.");
            Assert.AreEqual("129.04259", Value(lines, "CH$EXACT_MASS"), "The mass is that of the formula, not of the measured precursor.");
        }

        [TestMethod()]
        public void Structure_LibraryFormulaIsWrittenInHillOrderWithIsotopesCollapsed() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = string.Empty;
            reference.Formula = FormulaStringParcer.OrganicElementsReader("C8H7D3N4O2"); // stored as C8H7[2H]3N4O2

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("C8H10N4O2", Value(lines, "CH$FORMULA"));
            Assert.AreEqual("197.09921", Value(lines, "CH$EXACT_MASS"));
        }

        [TestMethod()]
        public void Unknown_HasNoStructureAndTheMeasuredNeutralMass() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = string.Empty;
            reference.SMILES = string.Empty;
            reference.InChIKey = string.Empty;
            reference.Ontology = string.Empty;
            reference.Formula = new Formula();

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Unknown));

            Assert.AreEqual("Unknown; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            Assert.AreEqual("Unknown", Value(lines, "CH$NAME"));
            Assert.IsNull(Value(lines, "CH$COMPOUND_CLASS"));
            Assert.AreEqual("N/A", Value(lines, "CH$FORMULA"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$IUPAC"));
            Assert.IsNull(Value(lines, "CH$LINK"));
            Assert.AreEqual("129.04263", Value(lines, "CH$EXACT_MASS")); // [M+H]+ at 130.04990
        }

        [TestMethod()]
        public void Unknown_LevelIgnoresWhatTheMoleculeStillCarries() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Unknown));

            Assert.AreEqual("Unknown", Value(lines, "CH$NAME"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$FORMULA"));
            Assert.IsNull(Value(lines, "CH$COMPOUND_CLASS"));
        }

        [TestMethod()]
        public void EmptyName_IsWrittenAsUnknown() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "  ";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("Unknown", Value(lines, "CH$NAME"));
        }

        [TestMethod()]
        public void Suggested_KeepsTheStructureAndSaysSo() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "low score: L-Pyroglutamic acid";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Suggested));

            Assert.AreEqual("low score: L-Pyroglutamic acid", Value(lines, "CH$NAME"));
            CollectionAssert.Contains(lines, "COMMENT: MS-DIAL annotation level: suggested");
            Assert.AreEqual("INCHIKEY ODHCTXKNWHHXJC-VKHMYHEASA-N", Value(lines, "CH$LINK"));
        }

        [DataTestMethod()]
        [DataRow("low score: X", true)]
        [DataRow("no MS2: X", true)]
        [DataRow("w/o MS2: X", true)]
        [DataRow("Unsettled: X", true)]
        [DataRow("Putative: X", true)]
        [DataRow("PC 34:1|PC 16:0_18:1", false)]
        [DataRow("Glycine", false)]
        [DataRow("", false)]
        public void IsSuggestionName(string name, bool expected) {
            Assert.AreEqual(expected, MassBankRecordHandler.IsSuggestionName(name));
        }

        [TestMethod()]
        public void Name_IsSanitisedAndTheOriginalKeptInAComment() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "(±)-Pyroglutamic acid; standard 5′-isomer & \"x\"=y";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("(+/-)-Pyroglutamic acid, standard 5'-isomer and 'x'-y", Value(lines, "CH$NAME"));
            Assert.AreEqual("(+/-)-Pyroglutamic acid, standard 5'-isomer and 'x'-y; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            CollectionAssert.Contains(lines, "COMMENT: Original name: (±)-Pyroglutamic acid; standard 5′-isomer & \"x\"=y");
        }

        [DataTestMethod()]
        [DataRow("PC 34:1|PC 16:0_18:1")]
        [DataRow("Cer 18:1;O2/16:0")]
        [DataRow("α-Tocopherol")]
        [DataRow("Caféine")]
        [DataRow("w/o MS2: Glycine")]
        public void Name_ThatTheGrammarAcceptsIsKept(string name) {
            Assert.AreEqual(name, MassBankRecordHandler.SanitizeName(name));
        }

        [TestMethod()]
        public void RecordTitle_IsAtMost600Characters() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = string.Concat(Enumerable.Repeat("Pyroglutamyl-", 60)) + "acid";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.IsTrue(Value(lines, "RECORD_TITLE").Length <= 600);
            Assert.AreEqual(reference.Name, Value(lines, "CH$NAME"));
        }

        [DataTestMethod()]
        [DataRow("", null)]
        [DataRow("Alpha amino acids and derivatives", "Alpha amino acids and derivatives")]
        [DataRow("Natural Product; Lipid", "Natural Product; Lipid")]
        [DataRow("Carbohydrates & carbohydrate conjugates", "Carbohydrates and carbohydrate conjugates")]
        [DataRow("A;B|C #1 = 50%", "A,B/C 1 - 50")]
        [DataRow("#; ~", "-")]
        [DataRow("#", null)]
        public void CompoundClass_IsSanitisedAndOmittedWhenEmpty(string ontology, string expected) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Ontology = ontology;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual(expected, Value(lines, "CH$COMPOUND_CLASS"));
        }

        // MS-DIAL's default adducts (Resources/AdductIonResource_Positive.txt and _Negative.txt), each as MS-DIAL stores it.
        [DataTestMethod()]
        [DataRow("[M+H]+")] [DataRow("[M+NH4]+")] [DataRow("[M+Na]+")] [DataRow("[M+CH3OH+H]+")] [DataRow("[M+K]+")] [DataRow("[M+Li]+")]
        [DataRow("[M+ACN+H]+")] [DataRow("[M+H-H2O]+")] [DataRow("[M+H-2H2O]+")] [DataRow("[M+2Na-H]+")] [DataRow("[M+IsoProp+H]+")]
        [DataRow("[M+ACN+Na]+")] [DataRow("[M+2K-H]+")] [DataRow("[M+DMSO+H]+")] [DataRow("[M+2ACN+H]+")] [DataRow("[M+IsoProp+Na+H]+")]
        [DataRow("[M-C6H10O4+H]+")] [DataRow("[M-C6H10O5+H]+")] [DataRow("[M-C6H8O6+H]+")] [DataRow("[2M+H]+")] [DataRow("[2M+NH4]+")]
        [DataRow("[2M+Na]+")] [DataRow("[2M+3H2O+2H]+")] [DataRow("[2M+K]+")] [DataRow("[2M+ACN+H]+")] [DataRow("[2M+ACN+Na]+")]
        [DataRow("[M+2H]2+")] [DataRow("[M+H+NH4]2+")] [DataRow("[M+H+Na]2+")] [DataRow("[M+H+K]2+")] [DataRow("[M+ACN+2H]2+")]
        [DataRow("[M+2Na]2+")] [DataRow("[M+2ACN+2H]2+")] [DataRow("[M+3ACN+2H]2+")] [DataRow("[M+3H]3+")] [DataRow("[M+2H+Na]3+")]
        [DataRow("[M+H+2Na]3+")] [DataRow("[M+3Na]3+")]
        [DataRow("[M-H]-")] [DataRow("[M-H2O-H]-")] [DataRow("[M+Na-2H]-")] [DataRow("[M+Cl]-")] [DataRow("[M+K-2H]-")] [DataRow("[M+FA-H]-")]
        [DataRow("[M+Hac-H]-")] [DataRow("[M+C2H3N+Na-2H]-")] [DataRow("[M+Br]-")] [DataRow("[M+TFA-H]-")] [DataRow("[M-C6H10O4-H]-")]
        [DataRow("[M-C6H10O5-H]-")] [DataRow("[M-C6H8O6-H]-")] [DataRow("[M+CH3COONa-H]-")] [DataRow("[2M-H]-")] [DataRow("[2M+FA-H]-")]
        [DataRow("[2M+Hac-H]-")] [DataRow("[3M-H]-")] [DataRow("[M-2H]2-")] [DataRow("[M-3H]3-")]
        public void PrecursorType_OfEveryDefaultAdductIsValid(string adductName) {
            var stored = AdductIon.GetAdductIon(adductName).AdductIonName;
            var precursorType = MassBankRecordHandler.ToMassBankPrecursorType(stored);
            Assert.IsTrue(MassBankRecordHandler.IsValidPrecursorType(precursorType), $"{adductName} -> {precursorType}");
        }

        [DataTestMethod()]
        [DataRow("[M+IsoProp+H]+", "[M+C3H8O+H]+")]
        [DataRow("[M+IsoProp+Na+H]+", "[M+C3H8O+Na+H]+")]
        [DataRow("[M+DMSO+H]+", "[M+C2H6OS+H]+")]
        [DataRow("[M+TFA-H]-", "[M+C2HF3O2-H]-")]
        [DataRow("[2M+Hac-H]-", "[2M+CH3COO]-")]
        [DataRow("[M+Hac-H]-", "[M+CH3COO]-")]
        [DataRow("[M+Hac+Na-2H]-", "[M+CH3COOH+Na-2H]-")]
        [DataRow("[M+FA-H]-", "[M+FA-H]-")]
        public void PrecursorType_AbbreviationsBecomeFormulas(string adductName, string expected) {
            Assert.AreEqual(expected, MassBankRecordHandler.ToMassBankPrecursorType(adductName));
        }

        [DataTestMethod()]
        [DataRow("[M]+", true)] [DataRow("[M]+*", true)] [DataRow("[M+2H]++", true)] [DataRow("[M+H]+/[M+Na]+", true)] [DataRow("[M+13C+H]+", true)]
        [DataRow("", false)] [DataRow("[M+H]1+", false)] [DataRow("[M+e]-", false)] [DataRow("[M+D]+", false)] [DataRow("M+H", false)]
        [DataRow("[M+H]", false)] [DataRow("[m+h]+", false)] [DataRow("[M+H] +", false)] [DataRow("[M+H]+ ", false)] [DataRow("[M+Hac-H]-", false)]
        [DataRow("[M+IsoProp+H]+", false)]
        public void IsValidPrecursorType_FollowsTheGrammar(string value, bool expected) {
            Assert.AreEqual(expected, MassBankRecordHandler.IsValidPrecursorType(value));
        }

        [TestMethod()]
        public void IonMode_IsTheSpotsOwnPolarity() {
            var handler = CreateHandler(); // the project is positive
            var reference = Pyroglutamic();
            reference.AdductType = AdductIon.GetAdductIon("[M-H]-");

            var lines = Lines(handler.BuildRecord(Peak(mz: 128.0353), reference, reference, reference));

            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: ION_MODE NEGATIVE");
            CollectionAssert.Contains(lines, "MS$FOCUSED_ION: PRECURSOR_TYPE [M-H]-");
        }

        [TestMethod()]
        public void Record_WithoutPeaksOrPolarityIsRefusedAndNothingIsWritten() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Spectrum = new List<SpectrumPeak>();
            using (var stream = new MemoryStream()) {
                Assert.IsFalse(handler.TryWriteRecord(stream, Peak(), reference, reference, reference, out var reason));
                Assert.AreEqual("no MS/MS peaks", reason);
                Assert.AreEqual(0, stream.Length);
            }
            Assert.AreEqual(0, MassBankRecordHandler.CountExportablePeaks(reference));
            Assert.ThrowsException<InvalidOperationException>(() => handler.WriteRecord(new MemoryStream(), Peak(), reference, reference, reference));

            var both = new MassBankRecordHandler(IonMode.Both, "LC-ESI-QTOF", FakeSplash) { Authors = "A", License = "CC BY", Instrument = "I", ContributorIdentifier = Contributor, };
            var noAdduct = Pyroglutamic();
            noAdduct.AdductType = AdductIon.Default;
            var result = both.BuildRecord(Peak(), noAdduct, noAdduct, noAdduct);
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.SkipReason, "POSITIVE or NEGATIVE");
        }

        [TestMethod()]
        public void Metadata_IsValidatedAndNeverReplaced() {
            var handler = new MassBankRecordHandler(IonMode.Positive, "QTOF", FakeSplash)
            {
                Authors = "",
                License = "CC BY-NC-ND",
                Instrument = " ",
                ContributorIdentifier = "Xxx",
                CollisionEnergy = "40\n50",
            };
            Assert.AreEqual("CC BY-NC-ND", handler.License, "A licence is written as given, never replaced by CC BY.");
            var problems = handler.ValidateMetadata();
            Assert.AreEqual(5, problems.Count, string.Join(Environment.NewLine, problems));

            handler.License = "CC-BY";
            Assert.AreEqual("CC-BY", handler.License);
            Assert.AreEqual(6, handler.ValidateMetadata().Count);

            var reference = Pyroglutamic();
            var result = handler.BuildRecord(Peak(), reference, reference, reference);
            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.SkipReason, "metadata");
            Assert.AreEqual(0, CreateHandler().ValidateMetadata().Count);
        }

        [TestMethod()]
        public void ProjectFields_AreWrittenWhereTheGrammarWantsThem() {
            var handler = CreateHandler();
            handler.CollisionEnergy = " 35 +/- 15 V ";
            handler.Comment = "First line\r\n\r\n  Second line  \nCONFIDENCE\n" + new string('x', 700);
            handler.MsdialVersion = "5.5.261106+3f8ebeea";
            var reference = Pyroglutamic();

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: COLLISION_ENERGY 35 +/- 15 V");
            CollectionAssert.Contains(lines, "MS$DATA_PROCESSING: WHOLE MS-DIAL 5.5.261106+3f8ebeea");
            var comments = lines.Where(l => l.StartsWith("COMMENT: ")).Select(l => l.Substring("COMMENT: ".Length)).ToArray();
            CollectionAssert.AreEqual(new[] { "First line", "Second line", new string('x', 600), new string('x', 100), }, comments);

            handler.CollisionEnergy = string.Empty;
            handler.Comment = string.Empty;
            handler.MsdialVersion = string.Empty;
            lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));
            Assert.IsFalse(lines.Any(l => l.Contains("COLLISION_ENERGY") || l.StartsWith("COMMENT") || l.StartsWith("MS$DATA_PROCESSING")));
        }

        [TestMethod()]
        public void Record_HasTheFieldsInTheGrammarsOrder() {
            var handler = CreateHandler();
            handler.CollisionEnergy = "40 V";
            handler.Comment = "comment";
            handler.MsdialVersion = "5.5";
            var reference = Pyroglutamic();
            reference.Spectrum[2].SpectrumComment = SpectrumComment.precursor;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            var tags = lines.Where(l => l.Length > 0 && !l.StartsWith("  ")).Select(l => l == "//" ? l : l.Substring(0, l.IndexOf(": ", StringComparison.Ordinal))).ToArray();
            CollectionAssert.AreEqual(new[]
            {
                "ACCESSION", "RECORD_TITLE", "DATE", "AUTHORS", "LICENSE", "COMMENT", "CH$NAME", "CH$COMPOUND_CLASS", "CH$FORMULA", "CH$EXACT_MASS",
                "CH$SMILES", "CH$IUPAC", "CH$LINK", "AC$INSTRUMENT", "AC$INSTRUMENT_TYPE", "AC$MASS_SPECTROMETRY", "AC$MASS_SPECTROMETRY",
                "AC$MASS_SPECTROMETRY", "AC$CHROMATOGRAPHY", "MS$FOCUSED_ION", "MS$FOCUSED_ION", "MS$DATA_PROCESSING", "PK$SPLASH", "PK$ANNOTATION",
                "PK$NUM_PEAK", "PK$PEAK", "//",
            }, tags);
            CollectionAssert.Contains(lines, "  84.04584 precursor");
            Assert.AreEqual(string.Empty, lines.Last(), "The record ends with a line feed after //.");
            Assert.IsFalse(lines.Any(l => l.Contains('\r')));
        }
    }
}
