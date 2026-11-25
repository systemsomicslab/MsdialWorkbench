using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Linq;

namespace CompMs.Common.Parser.Tests
{
    [TestClass()]
    public class MassBankVocabularyTests
    {
        [TestMethod()]
        public void VocabulariesAreTheGrammarTokens() {
            CollectionAssert.AreEqual(new[] { "LC", "GC", "CE", }, MassBankVocabulary.Separations.ToArray());
            CollectionAssert.AreEqual(new[] { "ESI", "APCI", "APPI", "EI", "CI", "FI", "FD", "FAB", "MALDI", "SI", }, MassBankVocabulary.Ionisations.ToArray());
            CollectionAssert.AreEqual(new[] { "Q", "TOF", "IT", "FT", "B", "E", }, MassBankVocabulary.AnalyzerTokens.ToArray());
            // recordformat/license.ini of MassBank-lib
            CollectionAssert.AreEqual(new[] { "CC BY", "CC BY-SA", "CC BY-NC", "CC BY-NC-SA", "CC BY-NC-ND", "CC0", "dl-de/by-2-0", }, MassBankVocabulary.Licenses.ToArray());
            CollectionAssert.AreEqual(MassBankVocabulary.Licenses.ToArray(), MassBankRecordHandler.AvailableLicenses.ToArray());
        }

        [TestMethod()]
        public void EveryAnalyzerPresetComposesAValidInstrumentType() {
            CollectionAssert.AreEqual(
                new[] { "QTOF", "QFT", "ITFT", "QQ", "QIT", "IT", "ITTOF", "TOF", "TOFTOF", "Q", "FT", },
                MassBankVocabulary.AnalyzerPresets.Select(p => p.Analyzer).ToArray());
            foreach (var preset in MassBankVocabulary.AnalyzerPresets) {
                Assert.IsFalse(string.IsNullOrWhiteSpace(preset.Description), preset.Analyzer);
                Assert.IsTrue(MassBankVocabulary.IsValidInstrumentType(MassBankVocabulary.ComposeInstrumentType("LC", "ESI", preset.Analyzer)), preset.Analyzer);
            }
        }

        // Each of these was run through the MassBank Validator (2.2.10-SNAPSHOT) inside a record.
        [DataTestMethod()]
        [DataRow("ESI-QTOF")]
        [DataRow("ESI-ITFT")]
        [DataRow("LC-ESI-QTOF")]
        [DataRow("LC-ESI-ITFT")]
        [DataRow("LC-ESI-QFT")]
        [DataRow("LC-ESI-FT")]
        [DataRow("LC-ESI-QQ")]
        [DataRow("LC-ESI-QQQ")]
        [DataRow("LC-ESI-TOF")]
        [DataRow("LC-ESI-TOFQ")]
        [DataRow("LC-ESI-IT")]
        [DataRow("LC-ESI-QIT")]
        [DataRow("LC-ESI-ITTOF")]
        [DataRow("LC-ESI-EBEB")]
        [DataRow("LC-ESI-QFTTOF")]
        [DataRow("GC-EI-TOF")]
        [DataRow("GC-EI-Q")]
        [DataRow("GC-CI-Q")]
        [DataRow("LC-APCI-QTOF")]
        [DataRow("LC-APPI-QTOF")]
        [DataRow("MALDI-TOFTOF")]
        [DataRow("LC-MALDI-TOF")]
        [DataRow("CE-ESI-TOF")]
        [DataRow("FAB-EBEB")]
        [DataRow("LC-FD-B")]
        [DataRow("LC-FI-B")]
        [DataRow("LC-SI-Q")]
        [DataRow("EI-B")]
        [DataRow("CI-Q")]
        [DataRow("SI-E")]
        public void IsValidInstrumentType_AcceptsWhatTheGrammarAccepts(string value) {
            Assert.IsTrue(MassBankVocabulary.IsValidInstrumentType(value));
            Assert.IsTrue(MassBankVocabulary.TryParseInstrumentType(value, out var separation, out var ionisation, out var analyzer));
            Assert.AreEqual(value, MassBankVocabulary.ComposeInstrumentType(separation, ionisation, analyzer));
        }

        [DataTestMethod()]
        [DataRow("")]
        [DataRow("QTOF")]
        [DataRow("Orbitrap")]
        [DataRow("LC-ESI-Orbitrap")]
        [DataRow("LC-ESI-QEXACTIVE")]
        [DataRow("LC-ESI-TIMSTOF")]
        [DataRow("LC-ESI-TIMS-QTOF")]
        [DataRow("LC-ESI-IMS-QTOF")]
        [DataRow("HILIC-ESI-QTOF")]
        [DataRow("UPLC-ESI-QTOF")]
        [DataRow("SFC-ESI-QTOF")]
        [DataRow("DI-ESI-QTOF")]
        [DataRow("LC-ESI-Q-TOF")]
        [DataRow("LC-ESI-QqQ")]
        [DataRow("LC-ESI-QTOF-MS")]
        [DataRow("LC-EAD-QTOF")]
        [DataRow("LC-HESI-QFT")]
        [DataRow("LC-nanoESI-QTOF")]
        [DataRow("lc-esi-qtof")]
        [DataRow("LC-ESI-")]
        [DataRow("LC-QTOF")]
        [DataRow("LC-LC-ESI-QTOF")]
        [DataRow(" LC-ESI-QTOF")]
        [DataRow("LC-ESI-QTOF ")]
        [DataRow("LC-ESI-QTOF\n")]
        [DataRow("LC-ESI-QTOF edited")]
        [DataRow(null)]
        public void IsValidInstrumentType_RejectsWhatTheGrammarRejects(string value) {
            Assert.IsFalse(MassBankVocabulary.IsValidInstrumentType(value));
            Assert.IsFalse(MassBankVocabulary.TryParseInstrumentType(value, out var separation, out var ionisation, out var analyzer));
            Assert.AreEqual(string.Empty, separation);
            Assert.AreEqual(string.Empty, ionisation);
            Assert.AreEqual(string.Empty, analyzer);
        }

        [TestMethod()]
        public void TryParseInstrumentType_SplitsTheParts() {
            Assert.IsTrue(MassBankVocabulary.TryParseInstrumentType("LC-ESI-QFT", out var separation, out var ionisation, out var analyzer));
            Assert.AreEqual(("LC", "ESI", "QFT"), (separation, ionisation, analyzer));
            Assert.IsTrue(MassBankVocabulary.TryParseInstrumentType("MALDI-TOFTOF", out separation, out ionisation, out analyzer));
            Assert.AreEqual(("", "MALDI", "TOFTOF"), (separation, ionisation, analyzer));
        }

        [TestMethod()]
        public void ComposeInstrumentType_LeavesOutAnEmptySeparation() {
            Assert.AreEqual("LC-ESI-QTOF", MassBankVocabulary.ComposeInstrumentType("LC", "ESI", "QTOF"));
            Assert.AreEqual("ESI-QTOF", MassBankVocabulary.ComposeInstrumentType("", "ESI", "QTOF"));
            Assert.AreEqual("ESI-QTOF", MassBankVocabulary.ComposeInstrumentType(null, "ESI", "QTOF"));
            Assert.IsFalse(MassBankVocabulary.IsValidInstrumentType(MassBankVocabulary.ComposeInstrumentType("LC", "ESI", "")));
        }

        [DataTestMethod()]
        [DataRow("RIKEN_IMS", true)]
        [DataRow("XXX", true)]
        [DataRow("A", true)]
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ012345", true)] // 32
        [DataRow("ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456", false)] // 33
        [DataRow("Xxx", false)]
        [DataRow("riken", false)]
        [DataRow("RIKEN-IMS", false)]
        [DataRow("RIKEN IMS", false)]
        [DataRow("RIKEN/IMS", false)]
        [DataRow("", false)]
        [DataRow("ABC\n", false)]
        [DataRow("ÄBC", false)]
        [DataRow(null, false)]
        public void IsValidContributorId(string value, bool expected) {
            Assert.AreEqual(expected, MassBankVocabulary.IsValidContributorId(value));
        }

        [DataTestMethod()]
        [DataRow("CC BY", true)]
        [DataRow("CC BY-NC-ND", true)]
        [DataRow("dl-de/by-2-0", true)]
        [DataRow("CC0", true)]
        [DataRow("CC-BY", false)]
        [DataRow("CC BY 4.0", false)]
        [DataRow("cc by", false)]
        [DataRow("CC BY-ND", false)]
        [DataRow("CC BY ", false)]
        [DataRow("", false)]
        [DataRow(null, false)]
        public void IsValidLicense(string value, bool expected) {
            Assert.AreEqual(expected, MassBankVocabulary.IsValidLicense(value));
        }

        [DataTestMethod()]
        [DataRow("35 +/- 15 V", true)]
        [DataRow("40", true)]
        [DataRow("Ramp 20-40 V", true)]
        [DataRow("30±5 eV", true)]
        [DataRow("", false)]
        [DataRow("   ", false)]
        [DataRow(" 40 eV", false)]
        [DataRow("40 eV ", false)]
        [DataRow("40\r\n50", false)]
        [DataRow(null, false)]
        public void IsValidCollisionEnergy(string value, bool expected) {
            Assert.AreEqual(expected, MassBankVocabulary.IsValidCollisionEnergy(value));
        }

        [DataTestMethod()]
        [DataRow("Tsugawa H, Kyoto University", true)]
        [DataRow("A", true)]
        [DataRow(" padded ", true)]
        [DataRow("", false)]
        [DataRow(" ", false)]
        [DataRow("first\nsecond", false)]
        [DataRow(null, false)]
        public void IsValidAuthorsAndInstrument(string value, bool expected) {
            Assert.AreEqual(expected, MassBankVocabulary.IsValidAuthors(value));
            Assert.AreEqual(expected, MassBankVocabulary.IsValidInstrument(value));
        }
    }
}
