using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using NCDK;
using NCDK.Graphs.InChI;
using NCDK.Smiles;
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

        // The yyMMddHHmmss block of an accession "<prefix>yyMMddHHmmss<suffix>", read in the Gregorian calendar.
        private static DateTime AccessionTime(string accession, string prefix, string suffix) {
            StringAssert.StartsWith(accession, prefix);
            StringAssert.EndsWith(accession, suffix);
            var time = accession.Substring(prefix.Length, accession.Length - prefix.Length - suffix.Length);
            Assert.IsTrue(DateTime.TryParseExact(time, "yyMMddHHmmss", CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed), $"{accession}: \"{time}\" is not yyMMddHHmmss");
            return parsed;
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

        [TestMethod()]
        public void Peaks_NearDoubleMaxValueAreRefusedNotThrown() {
            // Reads the printed peaks back as MassBankSplash does; double.MaxValue printed with two decimals does not fit.
            string ParsingSplash(string printed) {
                foreach (var pair in printed.Split(' ')) {
                    double.Parse(pair.Split(':')[1], NumberStyles.Float, CultureInfo.InvariantCulture);
                }
                return FakeSplash(printed);
            }
            var handler = CreateHandler(ParsingSplash);
            var reference = Pyroglutamic();
            reference.Spectrum = new List<SpectrumPeak> { new SpectrumPeak(56.04679, double.MaxValue), new SpectrumPeak(84.04584, 100d), };

            var result = handler.BuildRecord(Peak(), reference, reference, reference);

            // On .NET Framework, which this project targets, "F2" prints 15 significant digits, so the printed intensity
            // rounds above double.MaxValue and cannot be read back.
            Assert.IsFalse(result.IsSuccess, "the record must be refused, not built from an intensity its SPLASH cannot read");
            StringAssert.Contains(result.SkipReason, "SPLASH");
            Assert.IsNull(result.Text);
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
                Assert.IsTrue(Math.Abs((parsed - DateTime.Now.Date).TotalDays) <= 1, $"DATE {date} is not the Gregorian date of today.");
                var accession = Value(lines, "ACCESSION");
                var time = AccessionTime(accession, $"MSBNK-{Contributor}-MSDIAL", "00001101");
                Assert.AreEqual(parsed, time.Date, $"The accession {accession} does not carry the Gregorian date of DATE {date}.");
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
        public void Accession_OfAnAlignmentSpotIsTheExportTimeAndTheSpotId() {
            var before = DateTime.Now;
            var handler = CreateHandler();
            var after = DateTime.Now;
            var reference = Pyroglutamic();

            var lines = Lines(handler.BuildRecord(Peak(id: 42), reference, reference, reference));

            var accession = Value(lines, "ACCESSION");
            Assert.AreEqual(accession, handler.GetAccession(Peak(id: 42)));
            var time = AccessionTime(accession, $"MSBNK-{Contributor}-MSDIAL", "00000042");
            Assert.IsTrue(time >= before.AddTicks(-(before.Ticks % TimeSpan.TicksPerSecond)) && time <= after, $"{accession} is not the time the handler was created");
            Assert.AreEqual(time.ToString("yyyy.MM.dd", CultureInfo.InvariantCulture), Value(lines, "DATE"));
            Assert.IsTrue(System.Text.RegularExpressions.Regex.IsMatch(accession.Substring($"MSBNK-{Contributor}-".Length), @"\A[A-Z0-9_]{1,64}\z"));
        }

        [TestMethod()]
        public void Accession_OfAPeakCarriesTheAnalysisFileId() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();

            // Peak IDs start at 0 in every analysis file: peak 42 of file 3 and peak 42 of file 12 are different peaks.
            var first = handler.BuildRecord(Peak(id: 42), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, analysisFileId: 3);
            var second = handler.BuildRecord(Peak(id: 42), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, analysisFileId: 12);
            var spot = handler.BuildRecord(Peak(id: 42), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched);

            var firstTime = AccessionTime(Value(Lines(first), "ACCESSION"), $"MSBNK-{Contributor}-MSDIAL", "F0003P00000042");
            var secondTime = AccessionTime(Value(Lines(second), "ACCESSION"), $"MSBNK-{Contributor}-MSDIAL", "F0012P00000042");
            var spotTime = AccessionTime(Value(Lines(spot), "ACCESSION"), $"MSBNK-{Contributor}-MSDIAL", "00000042");
            Assert.AreEqual(firstTime, secondTime, "one handler, one time for all its records");
            Assert.AreEqual(firstTime, spotTime);
            Assert.AreEqual(first.FileName, first.Accession + ".txt");
            Assert.AreEqual(first.Accession, handler.GetAccession(Peak(id: 42), 3));
            Assert.AreEqual(3, new[] { first.Accession, second.Accession, spot.Accession, }.Distinct().Count());

            var negative = handler.BuildRecord(Peak(id: 43), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, analysisFileId: -1);
            Assert.IsFalse(negative.IsSuccess);
            StringAssert.Contains(negative.SkipReason, "analysis file ID");
        }

        [TestMethod()]
        public void Accession_IsNeverIssuedTwiceByOneHandler() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            var empty = Pyroglutamic();
            empty.Spectrum = new List<SpectrumPeak>();

            // A refused record takes no accession.
            Assert.IsFalse(handler.BuildRecord(Peak(id: 7), empty, empty, empty).IsSuccess);
            var first = handler.BuildRecord(Peak(id: 7), reference, reference, reference);
            Assert.IsTrue(first.IsSuccess, first.SkipReason);

            var again = handler.BuildRecord(Peak(id: 7), reference, reference, reference);
            Assert.IsFalse(again.IsSuccess);
            Assert.AreEqual(MassBankRecordHandler.DuplicateAccessionReason, again.SkipReason);
            Assert.AreEqual(first.Accession, again.Accession);
            using (var stream = new MemoryStream()) {
                Assert.IsFalse(handler.TryWriteRecord(stream, Peak(id: 7), reference, reference, reference, out var reason));
                Assert.AreEqual(MassBankRecordHandler.DuplicateAccessionReason, reason);
                Assert.AreEqual(0, stream.Length, "a refused record writes nothing");
            }

            // The same peak of another analysis file, and the same spot of a new export, are other records.
            Assert.IsTrue(handler.BuildRecord(Peak(id: 7), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, analysisFileId: 1).IsSuccess);
            Assert.IsTrue(CreateHandler().BuildRecord(Peak(id: 7), reference, reference, reference).IsSuccess);
        }

        [TestMethod()]
        public void Accession_SoftwareCodeMustLeaveRoomForTheRecordId() {
            var handler = CreateHandler();
            handler.Software = new string('S', MassBankRecordHandler.MaxSoftwareCodeLength);
            Assert.AreEqual(0, handler.ValidateMetadata().Count, string.Join(Environment.NewLine, handler.ValidateMetadata()));
            var reference = Pyroglutamic();
            var result = handler.BuildRecord(new ChromatogramPeak(int.MaxValue, 130.0499021411, 1000d, new ChromXs(0.6358123779)), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, analysisFileId: int.MaxValue);
            Assert.IsTrue(result.IsSuccess, result.SkipReason);
            Assert.AreEqual(MassBankRecordHandler.MaxRecordIdLength, result.Accession.Length - $"MSBNK-{Contributor}-".Length);

            handler.Software += "S";
            Assert.AreEqual(1, handler.ValidateMetadata().Count);
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

            Assert.AreEqual(NcdkIsomericSmiles(smiles), Value(lines, "CH$SMILES"));
            StringAssert.StartsWith(Value(lines, "CH$IUPAC"), "InChI=1S/");
            Assert.AreEqual(formula, Value(lines, "CH$FORMULA"));
            Assert.AreEqual(exactMass, Value(lines, "CH$EXACT_MASS"));
            Assert.IsFalse(lines.Any(l => l.Contains("XXXXXXXXXXXXXX")), "The library InChIKey must not be written.");
            Assert.AreEqual(smiles.Contains("[2H]"), lines.Contains("COMMENT: CH$EXACT_MASS is the monoisotopic mass of the isotope-labelled compound; CH$FORMULA cannot show isotopes"));
        }

        private static string NcdkIsomericSmiles(string smiles) {
            return new SmilesGenerator(SmiFlavors.Isomeric).Create(new SmilesParser(CDK.Builder).ParseSmiles(smiles));
        }

        private static string NcdkInChIKey(string smiles) {
            return InChIGeneratorFactory.Instance.GetInChIGenerator(new SmilesParser(CDK.Builder).ParseSmiles(smiles)).GetInChIKey();
        }

        // NCDK and the Validator's CDK read the stereo of some double bonds differently (imine C=N, conjugated rings):
        // CH$SMILES is NCDK's own SMILES of the structure, so that both read the stereo NCDK put into the InChI.
        [DataTestMethod()]
        [DataRow(@"Clc1ccc(CN2CCS/C/2=N\C#N)cn1")] // thiacloprid: NCDK drops the imine stereo that CDK reads
        [DataRow(@"CC1=C(CCC(O)=O)C2=N\C\1=C/C1=C(C=C)C(C)=C(N1)\C=C1/N\C(=C/C3=N/C(=C\2)/C(CCC(O)=O)=C3C)C(C)=C1C=C")] // protoporphyrin IX
        public void Structure_SmilesIsNcdksOwnSmilesOfTheStructure(string smiles) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = smiles;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            var written = Value(lines, "CH$SMILES");
            Assert.AreEqual(NcdkIsomericSmiles(smiles), written);
            Assert.AreEqual("INCHIKEY " + NcdkInChIKey(smiles), Value(lines, "CH$LINK"));
            // The Validator reads CH$SMILES back and requires its InChIKey to be the one CH$LINK states.
            Assert.AreEqual("INCHIKEY " + NcdkInChIKey(written), Value(lines, "CH$LINK"), $"{written} read back is another structure");
            if (NcdkInChIKey(smiles).EndsWith("-UHFFFAOYSA-N", StringComparison.Ordinal)) {
                Assert.IsFalse(written.Contains('/') || written.Contains('\\'), $"{written} states stereo that the InChI does not have.");
            }
        }

        [DataTestMethod()]
        [DataRow("C[79Br]", "CH3Br", "93.94181", false)] // lighter than the element's average mass: NCDK reads its InChI wrongly
        [DataRow("C[81Br]", "CH3Br", "95.93977", true)]
        [DataRow("[13CH4]", "CH4", "17.03465", true)]
        public void Structure_IsotopesKeepTheStructure(string smiles, string formula, string exactMass, bool labelledComment) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = smiles;

            var result = handler.BuildRecord(Peak(), reference, reference, reference);
            var lines = Lines(result);

            Assert.AreEqual(NcdkIsomericSmiles(smiles), Value(lines, "CH$SMILES"), string.Join(" | ", result.Notes));
            Assert.AreEqual(formula, Value(lines, "CH$FORMULA"));
            Assert.AreEqual(exactMass, Value(lines, "CH$EXACT_MASS"));
            Assert.AreEqual(labelledComment, lines.Any(l => l.StartsWith("COMMENT: CH$EXACT_MASS is the monoisotopic mass of the isotope-labelled compound")));
        }

        [TestMethod()]
        public void Structure_WhoseInChILosesAnAtomIsNotAvailable() {
            // The InChI of [1H]C leaves the explicit protium out of its formula (CH3), so no formula fits both.
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.SMILES = "[1H]C";
            reference.Formula = new Formula();

            var result = handler.BuildRecord(Peak(), reference, reference, reference);
            var lines = Lines(result);

            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$IUPAC"));
            CollectionAssert.Contains(result.Notes.ToList(), "the structure was written as N/A: its InChI does not keep the formula of the SMILES");
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
            CollectionAssert.Contains(lines, "COMMENT: CH$EXACT_MASS is the monoisotopic mass of the isotope-labelled compound; CH$FORMULA cannot show isotopes");
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
            CollectionAssert.Contains(lines, "COMMENT: CH$EXACT_MASS was calculated from PRECURSOR_M/Z and the adduct [M+H]+; it is not a reference value");
        }

        // Glycine, C2H5NO2, M = 75.0320284; each precursor m/z from M, the proton mass 1.00727647 and the charge.
        [DataTestMethod()]
        [DataRow("[M+H]+", 76.03930487)]
        [DataRow("[M-H]-", 74.02475193)]
        [DataRow("[2M+H]+", 151.07133328)] // AdductIon.ConvertToExactMass gives 75.03230
        [DataRow("[2M-H]-", 149.05678034)] // and 75.03175
        [DataRow("[M+2H]2+", 38.52329067)]
        public void Unknown_NeutralMassIsThatOfOneMoleculeWhateverTheAdduct(string adductName, double precursorMz) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.AdductType = AdductIon.GetAdductIon(adductName);

            var lines = Lines(handler.BuildRecord(Peak(mz: precursorMz), reference, reference, reference, MassBankAnnotationLevel.Unknown));

            Assert.AreEqual("75.03203", Value(lines, "CH$EXACT_MASS"));
            Assert.AreEqual("N/A", Value(lines, "CH$FORMULA"));
            CollectionAssert.Contains(lines, $"COMMENT: CH$EXACT_MASS was calculated from PRECURSOR_M/Z and the adduct {reference.AdductType.AdductIonName}; it is not a reference value");
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
        public void EmptyName_WithoutAStructureIsWrittenAsUnknown() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "  ";
            reference.SMILES = string.Empty;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            Assert.AreEqual("Unknown", Value(lines, "CH$NAME"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
        }

        [DataTestMethod()]
        [DataRow("  ")]
        [DataRow("%%%")]
        public void EmptyName_WithAStructureIsRefused(string name) {
            // "Unknown" next to a structure would name the compound unknown while identifying it.
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = name;

            var result = handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched);

            Assert.IsFalse(result.IsSuccess);
            StringAssert.Contains(result.SkipReason, "no name");
        }

        [DataTestMethod()]
        [DataRow("low score: L-Pyroglutamic acid")]
        [DataRow("Unsettled: L-Pyroglutamic acid")]
        [DataRow("Putative: low score: L-Pyroglutamic acid")]
        [DataRow("Low score : L-Pyroglutamic acid")] // IsSuggestionName, and so the export policy, takes it for a suggestion
        [DataRow("putative:L-Pyroglutamic acid")]
        public void Suggested_KeepsTheStructureAndSaysSo(string name) {
            var handler = CreateHandler();
            handler.Comment = "project comment";
            var reference = Pyroglutamic();
            reference.Name = name;

            var result = handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Suggested);
            var lines = Lines(result);

            Assert.AreEqual("L-Pyroglutamic acid", Value(lines, "CH$NAME"), "CH$NAME is the compound name, without MS-DIAL's level prefix.");
            Assert.AreEqual("L-Pyroglutamic acid; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            var comments = lines.Where(l => l.StartsWith("COMMENT: ")).ToArray();
            CollectionAssert.AreEqual(new[] { "COMMENT: project comment", $"COMMENT: Original name: {name}", "COMMENT: MS-DIAL annotation level: suggested", }, comments);
            CollectionAssert.Contains(result.Notes.ToList(), "the suggestion prefix was removed from CH$NAME");
            Assert.AreEqual("INCHIKEY ODHCTXKNWHHXJC-VKHMYHEASA-N", Value(lines, "CH$LINK"));
        }

        // PC 34:1 as MS-DIAL names a lipid it matched at two levels; no structure, so that only the name is at stake.
        private static MoleculeMsReference Lipid(string name) {
            return new MoleculeMsReference
            {
                Name = name,
                Formula = FormulaStringParcer.OrganicElementsReader("C42H82NO8P"),
                Ontology = "PC",
                SMILES = string.Empty,
                InChIKey = string.Empty,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak(184.0733214, 10000.2), new SpectrumPeak(496.3397711, 150.1), new SpectrumPeak(760.5850813, 1200.9), },
            };
        }

        [TestMethod()]
        public void Name_OfASuggestedLipidWithUnresolvedChainsIsItsClassLevelName() {
            // MsReferenceScorer names a lipid "<class level>|<chain level>" when the chains were not resolved, and the
            // suggestion path prefixes "low score: ". Nothing measured the sn-chains, so CH$NAME must not state them.
            var handler = CreateHandler();
            var reference = Lipid("low score: PC 34:1|PC 16:0_18:1");

            var result = handler.BuildRecord(Peak(mz: 760.5851), reference, reference, reference, MassBankAnnotationLevel.Suggested, lipidChainsResolved: false);
            var lines = Lines(result);

            Assert.AreEqual("PC 34:1", Value(lines, "CH$NAME"));
            Assert.AreEqual("PC 34:1; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            var comments = lines.Where(l => l.StartsWith("COMMENT: ")).ToArray();
            CollectionAssert.AreEqual(new[] { "COMMENT: Original name: low score: PC 34:1|PC 16:0_18:1", "COMMENT: MS-DIAL annotation level: suggested", }, comments);
            Assert.IsFalse(lines.Except(comments).Any(l => l.Contains("16:0_18:1")), "only the original-name comment may mention the chains");
            CollectionAssert.Contains(result.Notes.ToList(), "the suggestion prefix was removed from CH$NAME");
            CollectionAssert.Contains(result.Notes.ToList(), "the dual lipid name was written at the level its evidence supports");
        }

        [TestMethod()]
        public void Name_OfAReferenceMatchedLipidWithResolvedChainsIsItsChainLevelName() {
            var handler = CreateHandler();
            var dual = Lipid("PC 34:1|PC 16:0_18:1");
            var lines = Lines(handler.BuildRecord(Peak(id: 1, mz: 760.5851), dual, dual, dual, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: true));
            Assert.AreEqual("PC 16:0_18:1", Value(lines, "CH$NAME"));
            Assert.AreEqual("PC 16:0_18:1; LC-ESI-QTOF; MS2", Value(lines, "RECORD_TITLE"));
            CollectionAssert.Contains(lines, "COMMENT: Original name: PC 34:1|PC 16:0_18:1");

            var chains = Lipid("PC 16:0_18:1");
            lines = Lines(handler.BuildRecord(Peak(id: 2, mz: 760.5851), chains, chains, chains, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: true));
            Assert.AreEqual("PC 16:0_18:1", Value(lines, "CH$NAME"));
            Assert.IsNull(Value(lines, "COMMENT"), "a name written as it is needs no original-name comment");
        }

        // POPC (PC 16:0/18:1), the chain-specific structure an MSP lipid reference named "PC 34:1|PC 16:0_18:1" carries.
        private const string PopcSmiles = "O=C(OCC(OC(=O)CCCCCCCC=CCCCCCCCC)COP(=O)([O-])OCC[N+](C)(C)C)CCCCCCCCCCCCCCC";
        private const string PopcInChIKey = "WTJKGGKOPKCXLL-UHFFFAOYSA-N";

        private static MoleculeMsReference LipidWithStructure(string name) {
            var reference = Lipid(name);
            reference.SMILES = PopcSmiles;
            reference.InChIKey = PopcInChIKey;
            return reference;
        }

        [DataTestMethod()]
        [DataRow("PC 34:1|PC 16:0_18:1", MassBankAnnotationLevel.ReferenceMatched)]
        [DataRow("PC 34:1|PC 16:0_18:1", MassBankAnnotationLevel.Unspecified)]
        [DataRow("low score: PC 34:1|PC 16:0_18:1", MassBankAnnotationLevel.Suggested)]
        [DataRow("no MS2: PC 34:1|PC 16:0_18:1", MassBankAnnotationLevel.Suggested)]
        public void Structure_OfAClassLevelLipidNameIsNotAvailable(string name, MassBankAnnotationLevel level) {
            // The reference's structure is POPC, one species of PC 34:1. In MassBank the InChIKey is the compound the
            // record identifies, so a record named "PC 34:1" must not carry it.
            var handler = CreateHandler();
            var reference = LipidWithStructure(name);

            var result = handler.BuildRecord(Peak(id: 1, mz: 760.5851), reference, reference, reference, level, lipidChainsResolved: false);
            var lines = Lines(result);

            Assert.AreEqual("PC 34:1", Value(lines, "CH$NAME"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.AreEqual("N/A", Value(lines, "CH$IUPAC"));
            Assert.IsNull(Value(lines, "CH$LINK"), "no InChIKey: it would name one species of the class");
            Assert.IsFalse(lines.Any(l => l.Contains(PopcInChIKey) || l.Contains("InChI=")), "nothing of the species' structure is written");
            // The library formula is the same for every species of the class.
            Assert.AreEqual("C42H82NO8P", Value(lines, "CH$FORMULA"));
            Assert.AreEqual("759.57781", Value(lines, "CH$EXACT_MASS"));
            Assert.IsFalse(lines.Any(l => l.StartsWith("COMMENT: CH$EXACT_MASS was calculated")), "the mass is the library formula's, not inferred");
            CollectionAssert.Contains(result.Notes.ToList(), MassBankRecordHandler.ClassLevelLipidStructureNote);
            CollectionAssert.Contains(result.Notes.ToList(), "the dual lipid name was written at the level its evidence supports");
        }

        [TestMethod()]
        public void Structure_OfAClassLevelLipidNameWithoutALibraryFormulaIsInferredFromThePrecursor() {
            var handler = CreateHandler();
            var reference = LipidWithStructure("PC 34:1|PC 16:0_18:1");
            reference.Formula = new Formula();

            var result = handler.BuildRecord(Peak(id: 1, mz: 760.5851), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: false);
            var lines = Lines(result);

            Assert.AreEqual("PC 34:1", Value(lines, "CH$NAME"));
            Assert.AreEqual("N/A", Value(lines, "CH$FORMULA"));
            Assert.AreEqual("N/A", Value(lines, "CH$SMILES"));
            Assert.IsNull(Value(lines, "CH$LINK"));
            Assert.AreEqual("759.57782", Value(lines, "CH$EXACT_MASS"), "760.5851 less the proton");
            CollectionAssert.Contains(lines, "COMMENT: CH$EXACT_MASS was calculated from PRECURSOR_M/Z and the adduct [M+H]+; it is not a reference value");
        }

        [TestMethod()]
        public void Structure_OfAChainLevelLipidNameIsWritten() {
            var handler = CreateHandler();

            var dual = LipidWithStructure("PC 34:1|PC 16:0_18:1");
            var result = handler.BuildRecord(Peak(id: 1, mz: 760.5851), dual, dual, dual, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: true);
            var lines = Lines(result);
            Assert.AreEqual("PC 16:0_18:1", Value(lines, "CH$NAME"));
            Assert.AreEqual("INCHIKEY " + PopcInChIKey, Value(lines, "CH$LINK"));
            Assert.AreNotEqual("N/A", Value(lines, "CH$SMILES"));
            CollectionAssert.DoesNotContain(result.Notes.ToList(), MassBankRecordHandler.ClassLevelLipidStructureNote);

            // A name that is not dual is written with its structure whatever the chain verdict: nothing was cut from it.
            var chains = LipidWithStructure("PC 16:0_18:1");
            result = handler.BuildRecord(Peak(id: 2, mz: 760.5851), chains, chains, chains, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: false);
            Assert.AreEqual("INCHIKEY " + PopcInChIKey, Value(Lines(result), "CH$LINK"));
            CollectionAssert.DoesNotContain(result.Notes.ToList(), MassBankRecordHandler.ClassLevelLipidStructureNote);
        }

        [TestMethod()]
        public void Structure_OfAClassLevelLipidNameWithoutAReferenceStructureIsNotNoted() {
            // Nothing was withheld, so the export summary has nothing to count.
            var reference = Lipid("PC 34:1|PC 16:0_18:1");
            var result = CreateHandler().BuildRecord(Peak(mz: 760.5851), reference, reference, reference, MassBankAnnotationLevel.ReferenceMatched, lipidChainsResolved: false);
            Assert.AreEqual("N/A", Value(Lines(result), "CH$SMILES"));
            CollectionAssert.DoesNotContain(result.Notes.ToList(), MassBankRecordHandler.ClassLevelLipidStructureNote);
        }

        // The rule of every other MS-DIAL export (NIST/MSP, SDF, MGF, tables, mzTab-M): AnnotationName.Canonical.
        [DataTestMethod()]
        [DataRow("PC 34:1|PC 16:0_18:1", false)]
        [DataRow("PC 34:1|PC 16:0_18:1", true)]
        [DataRow("no MS2: PC 34:1|PC 16:0_18:1", false)]
        [DataRow("w/o MS2: Glycine", false)]
        [DataRow("low score: Glycine", true)]
        [DataRow("Glycine", false)]
        public void Name_IsTheCanonicalNameOfTheOtherExports(string name, bool chainsResolved) {
            foreach (var level in new[] { MassBankAnnotationLevel.Unspecified, MassBankAnnotationLevel.ReferenceMatched, MassBankAnnotationLevel.Suggested, }) {
                var reference = Lipid(name);
                var lines = Lines(CreateHandler().BuildRecord(Peak(mz: 760.5851), reference, reference, reference, level, lipidChainsResolved: chainsResolved));
                Assert.AreEqual(CompMs.Common.Utility.AnnotationName.Canonical(name, chainsResolved), Value(lines, "CH$NAME"), level.ToString());
            }
        }

        [TestMethod()]
        public void Suggested_WithOnlyAPrefixAndAStructureIsRefused() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "low score: ";

            Assert.IsFalse(handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Suggested).IsSuccess);
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
        [DataRow("\tL-Pyroglutamic acid", MassBankAnnotationLevel.ReferenceMatched)]
        [DataRow("L-Pyroglutamic acid ", MassBankAnnotationLevel.ReferenceMatched)]
        [DataRow(" L-Pyroglutamic acid ", MassBankAnnotationLevel.Suggested)]
        public void Name_ChangedOnlyAtItsEndsIsNotedForTheSummary(string name, MassBankAnnotationLevel level) {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = name;

            var result = handler.BuildRecord(Peak(), reference, reference, reference, level);
            var lines = Lines(result);

            Assert.AreEqual("L-Pyroglutamic acid", Value(lines, "CH$NAME"));
            CollectionAssert.AreEqual(new[] { "the name was changed to characters MassBank accepts", }, result.Notes.ToArray());
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

        [DataTestMethod()]
        [DataRow('ᢅ')] [DataRow('ᢆ')] // letters to .NET Framework, marks to Java since Unicode 9
        [DataRow('ࡰ')] [DataRow('鿿')] [DataRow('Ꟁ')] [DataRow('ꟴ')] [DataRow('ౝ')] // letters to .NET 8, unassigned in Java 17
        public void Name_DropsLettersThatJavaDoesNotKnow(char letter) {
            Assert.AreEqual("ab", MassBankRecordHandler.SanitizeName("a" + letter + "b"));
            Assert.AreEqual("ab", MassBankRecordHandler.SanitizeCompoundClass("a" + letter + "b"));
        }

        [DataTestMethod()]
        [DataRow("α")] [DataRow("é")] [DataRow("中")] [DataRow("अ")] [DataRow("ก")] [DataRow("ᠠ")] // Greek, Latin, CJK, Devanagari, Thai, Mongolian
        public void Name_KeepsLettersOfOtherScripts(string letter) {
            Assert.AreEqual("a" + letter + "b", MassBankRecordHandler.SanitizeName("a" + letter + "b"));
        }

        [TestMethod()]
        public void Name_OriginalIsCutWithoutSplittingASurrogatePair() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Name = "%" + new string('e', 600 - "Original name: ".Length - 2) + "\U0001F600tail";

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            var original = Value(lines, "COMMENT");
            Assert.IsTrue(original.Length <= 600);
            Assert.IsFalse(char.IsHighSurrogate(original[original.Length - 1]), "half of a surrogate pair would be written as U+FFFD");
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

            // Each record of one handler needs a peak ID of its own: the handler never issues an accession twice.
            // Java's trim(), which the Validator's CONFIDENCE check uses, strips every control character too.
            handler.Comment = "CONFIDENCE\u0001\n\u001FCONFIDENCE\n\u0000\nA\u0007B\n" + new string('e', 599) + "\U0001F600tail";
            lines = Lines(handler.BuildRecord(Peak(id: 1102), reference, reference, reference));
            comments = lines.Where(l => l.StartsWith("COMMENT: ")).Select(l => l.Substring("COMMENT: ".Length)).ToArray();
            CollectionAssert.AreEqual(new[] { "A B", new string('e', 599), "\U0001F600tail", }, comments);

            handler.CollisionEnergy = string.Empty;
            handler.Comment = string.Empty;
            handler.MsdialVersion = string.Empty;
            lines = Lines(handler.BuildRecord(Peak(id: 1103), reference, reference, reference));
            Assert.IsFalse(lines.Any(l => l.Contains("COLLISION_ENERGY") || l.StartsWith("COMMENT") || l.StartsWith("MS$DATA_PROCESSING")));

            handler.Authors = "Tsugawa H,\tRIKEN\u0001";
            handler.Instrument = "\u0002Orbitrap";
            lines = Lines(handler.BuildRecord(Peak(id: 1104), reference, reference, reference));
            Assert.AreEqual("Tsugawa H, RIKEN", Value(lines, "AUTHORS"));
            Assert.AreEqual("Orbitrap", Value(lines, "AC$INSTRUMENT"));
        }

        [TestMethod()]
        public void Metadata_ThatIsOnlyControlCharactersOrTooLongIsRefused() {
            var handler = CreateHandler();
            handler.Authors = "\u0001";
            handler.Instrument = "\u007F";
            handler.InstrumentType = "LC-ESI-" + new string('Q', MassBankRecordHandler.MaxInstrumentTypeLength - 6);
            Assert.AreEqual(3, handler.ValidateProjectMetadata().Count, string.Join(Environment.NewLine, handler.ValidateProjectMetadata()));

            handler = CreateHandler();
            handler.InstrumentType = "LC-ESI-" + new string('Q', MassBankRecordHandler.MaxInstrumentTypeLength - 7);
            Assert.AreEqual(0, handler.ValidateProjectMetadata().Count);
            var reference = Pyroglutamic();
            reference.Name = new string('a', 50);
            var title = Value(Lines(handler.BuildRecord(Peak(), reference, reference, reference)), "RECORD_TITLE");
            Assert.AreEqual(600, title.Length);
        }

        [TestMethod()]
        public void RelativeIntensity_IsComputedFromThePrintedIntensities() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Spectrum = new List<SpectrumPeak> { new SpectrumPeak(56.04679, 0.006), new SpectrumPeak(84.04584, 0.016), new SpectrumPeak(130.04871, 0.026), };

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference));

            CollectionAssert.AreEqual(new[] { "  56.04679 0.01 333", "  84.04584 0.02 666", "  130.04871 0.03 999", }, PeakLines(lines));
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
