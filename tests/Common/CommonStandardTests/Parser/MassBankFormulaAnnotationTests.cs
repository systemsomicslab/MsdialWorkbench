using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;

namespace CompMs.Common.Parser.Tests
{
    /// <summary>
    /// PK$ANNOTATION from a formula assignment (MS-FINDER's product ion formulas), and the processing software line.
    /// </summary>
    [TestClass()]
    public class MassBankFormulaAnnotationTests
    {
        private const double ElectronMass = 0.00054857990946;

        // One value of a PK$ANNOTATION line: the Validator's word() or these characters.
        private static readonly Regex ANNOTATION_VALUE = new Regex(@"\A[\p{L}\p{Nd}\-+,()\[\]{}\\/.:$^'`_*?<>=#]+\z", RegexOptions.CultureInvariant);

        private static string FakeSplash(string printedPeaks) => "splash-" + printedPeaks.Length.ToString(CultureInfo.InvariantCulture);

        private static MassBankRecordHandler CreateHandler() {
            return new MassBankRecordHandler(IonMode.Positive, "LC-ESI-QTOF", FakeSplash)
            {
                Authors = "Tsugawa H, Kyoto University",
                License = "CC BY",
                Instrument = "TripleTOF 6600, SCIEX",
                ContributorIdentifier = "MSFINDER_TEST",
            };
        }

        private static MoleculeMsReference Pyroglutamic(string formula = "C5H7NO3", string smiles = "C1CC(=O)N[C@@H]1C(=O)O", string name = "L-Pyroglutamic acid") {
            return new MoleculeMsReference
            {
                Name = name,
                Formula = string.IsNullOrEmpty(formula) ? new Formula() : FormulaStringParcer.OrganicElementsReader(formula),
                SMILES = smiles,
                AdductType = AdductIon.GetAdductIon("[M+H]+"),
                Spectrum = new List<SpectrumPeak>
                {
                    new SpectrumPeak(56.0467813204, 124.37),
                    new SpectrumPeak(56.0522047119, 308.91),
                    new SpectrumPeak(84.0458377361, 1638.24),
                    new SpectrumPeak(130.0487118721, 378.55),
                },
            };
        }

        private static ChromatogramPeak Peak(int id = 1) => new ChromatogramPeak(id, 130.04955, 1000d, new ChromXs(0.636));

        private static MassBankFormulaAnnotation Set(string precursorFormula, int charge, params (double Mz, string Formula)[] ions) {
            return new MassBankFormulaAnnotation(precursorFormula, ions.Select(i => new MassBankPeakAnnotation(i.Mz, i.Formula, charge)).ToList());
        }

        private static MassBankFormulaAnnotation PyroglutamicSet() {
            return Set("C5H7NO3", 1, (56.0467813204, "C3H6N"), (84.0458377361, "C4H6NO"), (130.0487118721, "C5H8NO3"));
        }

        private static string[] Lines(MassBankRecordResult result) {
            Assert.IsTrue(result.IsSuccess, result.SkipReason);
            return result.Text.Split('\n');
        }

        private static string[] AnnotationLines(string[] lines) {
            return lines.SkipWhile(l => !l.StartsWith("PK$ANNOTATION: ", StringComparison.Ordinal)).Skip(1).TakeWhile(l => l.StartsWith("  ", StringComparison.Ordinal)).ToArray();
        }

        private static string[] Comments(string[] lines) {
            return lines.Where(l => l.StartsWith("COMMENT: ", StringComparison.Ordinal)).Select(l => l.Substring("COMMENT: ".Length)).ToArray();
        }

        private static double Mass(string formula) {
            // Monoisotopic masses of the elements used here.
            var masses = new Dictionary<string, double> { ["C"] = 12d, ["H"] = 1.00782503207, ["N"] = 14.0030740048, ["O"] = 15.99491461956, };
            return Regex.Matches(formula, "([A-Z][a-z]?)([0-9]*)").Cast<Match>()
                .Sum(m => masses[m.Groups[1].Value] * (m.Groups[2].Value.Length == 0 ? 1 : int.Parse(m.Groups[2].Value, CultureInfo.InvariantCulture)));
        }

        [TestMethod()]
        public void FormulaAnnotation_WritesTheSetWhosePrecursorFormulaIsChFormula() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            var sets = new[]
            {
                Set("C4H5N3O2", 1, (84.0458377361, "C3H4N2O")),
                Set("C6H11NO2", 1, (84.0458377361, "C5H10N")),
                PyroglutamicSet(),
            };

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: sets));

            CollectionAssert.Contains(lines, "PK$ANNOTATION: m/z tentative_formula formula_count mass error(ppm)");
            var theoretical = Mass("C4H6NO") - ElectronMass;
            var ppm = (84.04584 - theoretical) / theoretical * 1e6;
            CollectionAssert.AreEqual(new[]
            {
                $"  56.04678 C3H6N+ 1 {(Mass("C3H6N") - ElectronMass).ToString("F5", CultureInfo.InvariantCulture)} {((56.04678 - (Mass("C3H6N") - ElectronMass)) / (Mass("C3H6N") - ElectronMass) * 1e6).ToString("F2", CultureInfo.InvariantCulture)}",
                $"  84.04584 C4H6NO+ 1 {theoretical.ToString("F5", CultureInfo.InvariantCulture)} {ppm.ToString("F2", CultureInfo.InvariantCulture)}",
                $"  130.04871 C5H8NO3+ 1 {(Mass("C5H8NO3") - ElectronMass).ToString("F5", CultureInfo.InvariantCulture)} {((130.04871 - (Mass("C5H8NO3") - ElectronMass)) / (Mass("C5H8NO3") - ElectronMass) * 1e6).ToString("F2", CultureInfo.InvariantCulture)}",
            }, AnnotationLines(lines));
            Assert.IsFalse(Comments(lines).Any(c => c.StartsWith("PK$ANNOTATION", StringComparison.Ordinal)), "CH$FORMULA already names the precursor formula.");
        }

        [TestMethod()]
        public void FormulaAnnotation_IsLeftOutWithANoteWhenNoSetIsChFormula() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();

            var result = handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: new[] { Set("C4H5N3O2", 1, (84.0458377361, "C3H4N2O")), });

            var lines = Lines(result);
            Assert.IsFalse(lines.Any(l => l.StartsWith("PK$ANNOTATION", StringComparison.Ordinal)));
            CollectionAssert.Contains(result.Notes.ToList(), MassBankRecordHandler.NoMatchingFormulaAnnotationNote);
        }

        [TestMethod()]
        public void FormulaAnnotation_WithoutChFormulaTakesTheFirstReadableSetAndNamesItsFormula() {
            var handler = CreateHandler();
            var reference = Pyroglutamic(formula: string.Empty, smiles: string.Empty, name: "Unknown");
            var sets = new[] { Set("C5H7NO3TMS", 1, (84.0458377361, "C4H6NO")), PyroglutamicSet(), Set("C6H11NO2", 1, (84.0458377361, "C5H10N")), };

            var result = handler.BuildRecord(Peak(), reference, reference, reference, MassBankAnnotationLevel.Unknown, formulaAnnotations: sets);

            var lines = Lines(result);
            Assert.AreEqual("CH$FORMULA: N/A", lines.Single(l => l.StartsWith("CH$FORMULA: ", StringComparison.Ordinal)));
            CollectionAssert.Contains(Comments(lines), MassBankRecordHandler.TentativePrecursorFormulaComment + "C5H7NO3");
            Assert.AreEqual(3, AnnotationLines(lines).Length);
            Assert.AreEqual(0, result.Notes.Count(n => n == MassBankRecordHandler.NoMatchingFormulaAnnotationNote));
        }

        [TestMethod()]
        public void FormulaAnnotation_OfANegativeIonAddsTheElectron() {
            var handler = CreateHandler();
            var reference = new MoleculeMsReference
            {
                Name = "4-Coumaric acid",
                Formula = FormulaStringParcer.OrganicElementsReader("C9H8O3"),
                SMILES = "OC(=O)/C=C/c1ccc(O)cc1",
                AdductType = AdductIon.GetAdductIon("[M-H]-"),
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak(119.05023, 1000d), new SpectrumPeak(163.04007, 400d), },
            };

            var lines = Lines(handler.BuildRecord(new ChromatogramPeak(2, 163.04007, 1000d, new ChromXs(2.4)), reference, reference, reference,
                formulaAnnotations: new[] { Set("C9H8O3", -1, (119.05023, "C8H7O"), (163.04007, "C9H7O3")), }));

            var annotation = AnnotationLines(lines);
            StringAssert.StartsWith(annotation[0], $"  119.05023 C8H7O- 1 {(Mass("C8H7O") + ElectronMass).ToString("F5", CultureInfo.InvariantCulture)} ");
            StringAssert.StartsWith(annotation[1], "  163.04007 C9H7O3- 1 ");
            CollectionAssert.Contains(lines, "AC$MASS_SPECTROMETRY: ION_MODE NEGATIVE");
        }

        [TestMethod()]
        public void FormulaAnnotation_LeavesOutWhatItCannotWrite() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            var set = new MassBankFormulaAnnotation("C5H7NO3", new[]
            {
                new MassBankPeakAnnotation(56.0467813204, "C3H6Xx", 1), // not an element
                new MassBankPeakAnnotation(56.0522047119, "c3h6n", 1), // not a formula
                new MassBankPeakAnnotation(56.0522047119, "C2[13C]H6N", 1), // isotope-labelled: CH$FORMULA notation cannot show it
                new MassBankPeakAnnotation(84.0458377361, "C4H6NO", 2), // doubly charged
                new MassBankPeakAnnotation(double.NaN, "C4H6NO", 1),
                new MassBankPeakAnnotation(99.99999, "C5H9NO", 1), // a peak PK$PEAK does not print
                null,
                new MassBankPeakAnnotation(130.0487118721, "C5H8NO3", 1),
            });

            var result = handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: new[] { set, });

            var lines = Lines(result);
            Assert.AreEqual(1, AnnotationLines(lines).Length);
            StringAssert.StartsWith(AnnotationLines(lines)[0], "  130.04871 C5H8NO3+ 1 ");
            CollectionAssert.Contains(result.Notes.ToList(), MassBankRecordHandler.UnreadableFormulaAnnotationNote);
        }

        [TestMethod()]
        public void FormulaAnnotation_GivesEachFormulaOfAPeakALineWithTheirCount() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            // Two peaks that print as one m/z, and the same formula twice.
            reference.Spectrum.Insert(0, new SpectrumPeak(56.046784, 99d));
            var set = Set("C5H7NO3", 1, (56.0467813204, "C3H6N"), (56.046784, "C2H4N2"), (84.0458377361, "C4H6NO"), (84.0458377361, "C4H6NO"));

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: new[] { set, }));

            var annotation = AnnotationLines(lines);
            Assert.AreEqual(3, annotation.Length);
            StringAssert.StartsWith(annotation[0], "  56.04678 C3H6N+ 2 ");
            StringAssert.StartsWith(annotation[1], "  56.04678 C2H4N2+ 2 ");
            StringAssert.StartsWith(annotation[2], "  84.04584 C4H6NO+ 1 ");
        }

        [TestMethod()]
        public void FormulaAnnotation_MatchesAChargedChFormulaByItsComposition() {
            var handler = CreateHandler();
            var reference = new MoleculeMsReference
            {
                Name = "Choline",
                Formula = new Formula { FormulaString = "C5H14NO", },
                SMILES = "C[N+](C)(C)CCO",
                AdductType = AdductIon.GetAdductIon("[M]+"),
                Spectrum = new List<SpectrumPeak> { new SpectrumPeak(60.08078, 900d), new SpectrumPeak(104.10699, 1000d), },
            };

            var lines = Lines(handler.BuildRecord(new ChromatogramPeak(3, 104.10699, 1000d, new ChromXs(1.0)), reference, reference, reference,
                formulaAnnotations: new[] { Set("C5H14NO", 1, (60.08078, "C3H10N"), (104.10699, "C5H14NO")), }));

            CollectionAssert.Contains(lines, "CH$FORMULA: [C5H14NO]+");
            Assert.AreEqual(2, AnnotationLines(lines).Length);
        }

        [TestMethod()]
        public void FormulaAnnotation_FollowsTheGrammarAndReplacesThePeakComments() {
            var handler = CreateHandler();
            var reference = Pyroglutamic();
            reference.Spectrum[2].SpectrumComment = SpectrumComment.precursor;

            var lines = Lines(handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: new[] { PyroglutamicSet(), }));

            Assert.AreEqual(1, lines.Count(l => l.StartsWith("PK$ANNOTATION: ", StringComparison.Ordinal)), "One PK$ANNOTATION block.");
            var header = lines.Single(l => l.StartsWith("PK$ANNOTATION: ", StringComparison.Ordinal)).Substring("PK$ANNOTATION: ".Length).Split(' ');
            var previous = 0m;
            foreach (var line in AnnotationLines(lines)) {
                var values = line.Substring(2).Split(' ');
                Assert.AreEqual(header.Length, values.Length, line);
                Assert.IsTrue(values.All(v => ANNOTATION_VALUE.IsMatch(v)), line);
                var mz = decimal.Parse(values[0], NumberStyles.Float, CultureInfo.InvariantCulture);
                Assert.IsTrue(mz >= previous, "The annotation lines are sorted by m/z.");
                previous = mz;
            }
            var peakIndex = Array.IndexOf(lines, "PK$NUM_PEAK: 4");
            Assert.IsTrue(Array.FindIndex(lines, l => l.StartsWith("PK$SPLASH: ", StringComparison.Ordinal)) < Array.FindIndex(lines, l => l.StartsWith("PK$ANNOTATION: ", StringComparison.Ordinal)));
            Assert.IsTrue(Array.FindIndex(lines, l => l.StartsWith("PK$ANNOTATION: ", StringComparison.Ordinal)) < peakIndex);
        }

        [DataTestMethod()]
        [DataRow("de-DE")]
        [DataRow("fr-FR")]
        [DataRow("ar-SA")]
        [DataRow("th-TH")]
        public void FormulaAnnotation_IsTheSameInEveryCulture(string culture) {
            string Build() {
                var handler = CreateHandler();
                var reference = Pyroglutamic();
                return string.Join("\n", AnnotationLines(Lines(handler.BuildRecord(Peak(), reference, reference, reference, formulaAnnotations: new[] { PyroglutamicSet(), }))));
            }
            var invariant = Under(CultureInfo.InvariantCulture, Build);

            Assert.AreEqual(invariant, Under(new CultureInfo(culture), Build));
        }

        [TestMethod()]
        public void ProcessingSoftware_IsTheProgramDataProcessingNames() {
            var handler = CreateHandler();
            handler.ProcessingSoftware = "MS-FINDER";
            handler.MsdialVersion = "3.73";
            var reference = Pyroglutamic();

            CollectionAssert.Contains(Lines(handler.BuildRecord(Peak(), reference, reference, reference)), "MS$DATA_PROCESSING: WHOLE MS-FINDER 3.73");

            var msdial = CreateHandler();
            msdial.MsdialVersion = "5.5.261106";
            CollectionAssert.Contains(Lines(msdial.BuildRecord(Peak(), reference, reference, reference)), "MS$DATA_PROCESSING: WHOLE MS-DIAL 5.5.261106");

            handler.ProcessingSoftware = " ";
            Assert.IsTrue(handler.ValidateMetadata().Any(p => p.Contains("processing software")));
            handler.MsdialVersion = string.Empty;
            Assert.AreEqual(0, handler.ValidateMetadata().Count, "Without a version the software is not written, so it need not be named.");
        }

        [DataTestMethod()]
        [DataRow("C5H7NO3", "C5H7NO3")]
        [DataRow("H7C5O3N", "C5H7NO3")]
        [DataRow("C5H4D3NO3", "C5H7NO3")]
        [DataRow("H2O", "H2O")]
        [DataRow("ClNa", "ClNa")]
        [DataRow("C5H7NO3TMS", null)]
        [DataRow("c5h7no3", null)]
        [DataRow("", null)]
        [DataRow(null, null)]
        public void ToMassBankFormula_IsTheChFormulaNotation(string formula, string expected) {
            Assert.AreEqual(expected, MassBankRecordHandler.ToMassBankFormula(formula));
        }

        private static T Under<T>(CultureInfo culture, Func<T> action) {
            var thread = Thread.CurrentThread;
            var saved = thread.CurrentCulture;
            var savedUi = thread.CurrentUICulture;
            try {
                thread.CurrentCulture = culture;
                thread.CurrentUICulture = culture;
                return action();
            }
            finally {
                thread.CurrentCulture = saved;
                thread.CurrentUICulture = savedUi;
            }
        }
    }
}
