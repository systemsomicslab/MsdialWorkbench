using Microsoft.VisualStudio.TestTools.UnitTesting;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.MsfinderCommon.DiagnosticRules;
using Riken.Metabolomics.MsfinderCommon.Utility;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.Tests
{
    /// <summary>
    /// The diagnostic-ion rule settings in MSFINDER.INI (design 5.1) and the byte-identical-off guarantees of
    /// the settings file (design 6.4 items 3 and 4, 6.2 culture list).
    /// </summary>
    [TestClass]
    public class DiagnosticRuleIniTests
    {
        private static MsfinderDiagnosticRuleSettings NonDefault() => new MsfinderDiagnosticRuleSettings {
            Evaluation = true,
            RankingWeight = 0.123456,
            RankingScope = DiagnosticRuleRankingScope.Measured,
            RuleSetDirectory = @"rules\dev-set",
            InstrumentType = "ESI-QTOF",
            Strict = true,
        };

        private static MsfinderDiagnosticRuleSettings ReadLines(params string[] lines) {
            var settings = new MsfinderDiagnosticRuleSettings();
            foreach (var line in lines) {
                Assert.IsTrue(DiagnosticRuleIni.TryRead(line, settings), line);
            }
            return settings;
        }

        private static string WriteText(MsfinderDiagnosticRuleSettings settings) {
            using (var writer = new StringWriter(CultureInfo.InvariantCulture)) {
                writer.NewLine = "\r\n";
                DiagnosticRuleIni.Write(writer, settings);
                return writer.ToString();
            }
        }

        private static void AssertSameSettings(MsfinderDiagnosticRuleSettings expected, MsfinderDiagnosticRuleSettings actual) {
            DeepAssert.AreEqual(expected, actual, "DiagnosticRules");
        }

        // ---- TryRead and Write ----

        [TestMethod]
        public void WriteThenReadRoundTripsEverySetting() {
            var source = NonDefault();
            var text = WriteText(source);
            var lines = text.Split(new[] { "\r\n" }, StringSplitOptions.RemoveEmptyEntries);
            Assert.AreEqual(DiagnosticRuleIni.BlockHeader, lines[0]);
            var read = ReadLines(lines.Skip(1).ToArray());
            AssertSameSettings(source, read);
            Assert.AreEqual(text, WriteText(read));
        }

        [TestMethod]
        public void WriteHasTheSixKeysInTableOrder() {
            var expected = "#Diagnostic-ion rules (experimental)\r\n"
                + "DiagnosticRuleEvaluation=True\r\n"
                + "DiagnosticRuleRankingWeight=0.123456\r\n"
                + "DiagnosticRuleRankingScope=Measured\r\n"
                + "DiagnosticRuleSetDirectory=rules\\dev-set\r\n"
                + "DiagnosticRuleInstrumentType=ESI-QTOF\r\n"
                + "DiagnosticRuleStrict=True\r\n"
                + "\r\n";
            Assert.AreEqual(expected, WriteText(NonDefault()));
        }

        [TestMethod]
        public void WriteOfDefaultSettingsWritesNothing() {
            Assert.AreEqual(string.Empty, WriteText(new MsfinderDiagnosticRuleSettings()));
            Assert.AreEqual(string.Empty, WriteText(null));
        }

        [DataTestMethod]
        [DataRow("NaN")]
        [DataRow("Infinity")]
        [DataRow("-Infinity")]
        [DataRow("-0.5")]
        [DataRow("0.1234567")]
        [DataRow("1e400")]
        [DataRow("")]
        [DataRow("abc")]
        public void BadWeightIsAnErrorAndLeavesTheDefault(string value) {
            var settings = ReadLines("DiagnosticRuleRankingWeight=" + value);
            Assert.AreEqual(0.0, settings.RankingWeight);
            Assert.AreEqual(1, settings.Errors.Count);
            Assert.IsTrue(settings.IsDefault);
        }

        [DataTestMethod]
        [DataRow("0", 0.0)]
        [DataRow("-0", 0.0)]
        [DataRow("0.5", 0.5)]
        [DataRow("0,5", 0.5)]
        [DataRow("1.250000", 1.25)]
        [DataRow("2.000001", 2.000001)]
        [DataRow("1e-3", 0.001)]
        [DataRow(" 3 ", 3.0)]
        public void GoodWeightIsRead(string value, double expected) {
            var settings = ReadLines("DiagnosticRuleRankingWeight=" + value);
            Assert.AreEqual(0, settings.Errors.Count, string.Join("; ", settings.Errors));
            Assert.AreEqual(expected, settings.RankingWeight);
        }

        [TestMethod]
        public void NegativeZeroWeightIsReadAsPositiveZero() {
            var settings = ReadLines("DiagnosticRuleRankingWeight=-0");
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(0.0), BitConverter.DoubleToInt64Bits(settings.RankingWeight));
            Assert.IsTrue(settings.IsDefault);
        }

        [DataTestMethod]
        [DataRow(0.1234567)]
        [DataRow(1e-7)]
        [DataRow(-0.5)]
        [DataRow(double.NaN)]
        [DataRow(double.PositiveInfinity)]
        public void GuiWeightOutsideTheIniRuleIsRefused(double value) {
            Assert.IsFalse(DiagnosticRuleIni.TryAcceptWeight(value, out _));
        }

        [DataTestMethod]
        [DataRow(0.0)]
        [DataRow(0.123457)]
        [DataRow(2.000001)]
        [DataRow(1e6)]
        public void GuiWeightAcceptedIsTheWeightTheIniReadsBack(double value) {
            Assert.IsTrue(DiagnosticRuleIni.TryAcceptWeight(value, out var weight));
            var written = new MsfinderDiagnosticRuleSettings { Evaluation = true, RankingWeight = weight };
            var writer = new StringWriter();
            DiagnosticRuleIni.Write(writer, written);
            var read = ReadLines(writer.ToString().Split('\n').Select(l => l.TrimEnd('\r')).Where(l => l.StartsWith("DiagnosticRule", StringComparison.Ordinal)).ToArray());
            Assert.AreEqual(0, read.Errors.Count, string.Join("; ", read.Errors));
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(value), BitConverter.DoubleToInt64Bits(read.RankingWeight));
        }

        [TestMethod]
        public void GuiNegativeZeroWeightIsAcceptedAsPositiveZero() {
            Assert.IsTrue(DiagnosticRuleIni.TryAcceptWeight(-0.0, out var weight));
            Assert.AreEqual(BitConverter.DoubleToInt64Bits(0.0), BitConverter.DoubleToInt64Bits(weight));
        }

        [DataTestMethod]
        [DataRow("True", true)]
        [DataRow("TRUE", true)]
        [DataRow("true", true)]
        [DataRow("False", false)]
        [DataRow("false", false)]
        public void BooleansAcceptTrueAndFalseInAnyCase(string value, bool expected) {
            var settings = ReadLines("DiagnosticRuleEvaluation=" + value, "DiagnosticRuleStrict=" + value);
            Assert.AreEqual(0, settings.Errors.Count);
            Assert.AreEqual(expected, settings.Evaluation);
            Assert.AreEqual(expected, settings.Strict);
        }

        [DataTestMethod]
        [DataRow("yes")]
        [DataRow("1")]
        [DataRow("T")]
        [DataRow("")]
        public void OtherBooleanTextIsAnError(string value) {
            var settings = ReadLines("DiagnosticRuleEvaluation=" + value, "DiagnosticRuleStrict=" + value);
            Assert.AreEqual(2, settings.Errors.Count);
            Assert.IsFalse(settings.Evaluation);
            Assert.IsFalse(settings.Strict);
        }

        [TestMethod]
        public void ScopeIsReviewedOrMeasured() {
            Assert.AreEqual(DiagnosticRuleRankingScope.Measured, ReadLines("DiagnosticRuleRankingScope=measured").RankingScope);
            Assert.AreEqual(DiagnosticRuleRankingScope.Reviewed, ReadLines("DiagnosticRuleRankingScope=Measured", "DiagnosticRuleRankingScope=REVIEWED").RankingScope);
            var bad = ReadLines("DiagnosticRuleRankingScope=All");
            Assert.AreEqual(DiagnosticRuleRankingScope.Reviewed, bad.RankingScope);
            Assert.AreEqual(1, bad.Errors.Count);
        }

        [TestMethod]
        public void TextSettingsAreReadAsWrittenAfterTrimming() {
            var settings = ReadLines(@"DiagnosticRuleSetDirectory= rules\a b ", "DiagnosticRuleInstrumentType=LC-ESI-QTOF");
            Assert.AreEqual(@"rules\a b", settings.RuleSetDirectory);
            Assert.AreEqual("LC-ESI-QTOF", settings.InstrumentType);
            Assert.AreEqual(0, settings.Errors.Count);
        }

        [TestMethod]
        public void KeysAreMatchedWithAsciiCaseIgnored() {
            var settings = ReadLines("diagnosticruleevaluation=True", "DIAGNOSTICRULESTRICT=True");
            Assert.IsTrue(settings.Evaluation);
            Assert.IsTrue(settings.Strict);
        }

        [TestMethod]
        public void UnknownDiagnosticRuleKeyIsConsumedAsAnError() {
            var settings = ReadLines("DiagnosticRuleWeight=1", "DiagnosticRule=x");
            Assert.AreEqual(2, settings.Errors.Count);
            Assert.IsTrue(settings.IsDefault);
        }

        [DataTestMethod]
        [DataRow("DiagnosticRule Evaluation=True")]
        [DataRow(" DiagnosticRuleEvaluation=True")]
        [DataRow("XDiagnosticRuleEvaluation=True")]
        [DataRow("DiagnosticRule2=True")]
        [DataRow("DiagnosticRuleEvaluation")]
        [DataRow("Diagnostic=True")]
        [DataRow("DıagnosticRuleEvaluation=True")]
        [DataRow("Ms1Tolerance=0.001")]
        public void OtherLinesAreLeftToTheLegacyReader(string line) {
            var settings = new MsfinderDiagnosticRuleSettings();
            Assert.IsFalse(DiagnosticRuleIni.TryRead(line, settings));
            Assert.IsTrue(settings.IsDefault);
            Assert.AreEqual(0, settings.Errors.Count);
        }

        // ---- IsDefault, Errors, Clone ----

        [TestMethod]
        public void IsDefaultTurnsFalseForEachSettingAlone() {
            Assert.IsTrue(new MsfinderDiagnosticRuleSettings().IsDefault);
            var changes = new List<Action<MsfinderDiagnosticRuleSettings>> {
                s => s.Evaluation = true,
                s => s.RankingWeight = 0.000001,
                s => s.RankingScope = DiagnosticRuleRankingScope.Measured,
                s => s.RuleSetDirectory = "x",
                s => s.InstrumentType = "x",
                s => s.Strict = true,
            };
            foreach (var change in changes) {
                var settings = new MsfinderDiagnosticRuleSettings();
                change(settings);
                Assert.IsFalse(settings.IsDefault);
                Assert.AreNotEqual(string.Empty, WriteText(settings));
            }
        }

        [TestMethod]
        public void ErrorsAreNeitherPartOfIsDefaultNorWritten() {
            var settings = ReadLines("DiagnosticRuleEvaluation=maybe");
            Assert.AreEqual(1, settings.Errors.Count);
            Assert.IsTrue(settings.IsDefault);
            Assert.AreEqual(string.Empty, WriteText(settings));

            settings.Evaluation = true;
            var text = WriteText(settings);
            Assert.IsFalse(text.Contains("maybe"), text);
            Assert.AreEqual(8, text.Split(new[] { "\r\n" }, StringSplitOptions.None).Length - 1);
        }

        [TestMethod]
        public void CloneCopiesEverySettingAndIsIndependent() {
            var source = NonDefault();
            source.Errors.Add("e");
            var clone = source.Clone();
            AssertSameSettings(source, clone);
            clone.Evaluation = false;
            clone.Errors.Add("f");
            Assert.IsTrue(source.Evaluation);
            Assert.AreEqual(1, source.Errors.Count);
        }

        [TestMethod]
        public void NewParametersStartAtTheDefault() {
            var param = new AnalysisParamOfMsfinder();
            Assert.IsNotNull(param.DiagnosticRules);
            Assert.IsTrue(param.DiagnosticRules.IsDefault);
            Assert.AreEqual(0, param.DiagnosticRules.Errors.Count);
        }

        // ---- The settings file (design 6.4 item 3) ----

        private static string WriteIni(AnalysisParamOfMsfinder param) {
            var output = IniTestFiles.NewTempFile();
            MsFinderIniParcer.Write(param, output);
            return output;
        }

        private static string ReadThenWrite(string input) => WriteIni(MsFinderIniParcer.Read(input));

        private static string InputWithLines(string input, params string[] lines) {
            var path = IniTestFiles.NewTempFile();
            File.WriteAllBytes(path, File.ReadAllBytes(input).Concat(Encoding.ASCII.GetBytes(string.Concat(lines.Select(l => l + "\r\n")))).ToArray());
            return path;
        }

        private static readonly string[] EveryKeyAtDefault = {
            "#Diagnostic-ion rules (experimental)",
            "DiagnosticRuleEvaluation=False",
            "DiagnosticRuleRankingWeight=0.000000",
            "DiagnosticRuleRankingScope=Reviewed",
            "DiagnosticRuleSetDirectory=",
            "DiagnosticRuleInstrumentType=",
            "DiagnosticRuleStrict=False",
        };

        [TestMethod]
        public void GoldensHoldWithEvaluationFalseAlone() {
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("Default.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Golden("Default.ini"), "DiagnosticRuleEvaluation=False")), "default + Evaluation=False");
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettings.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Input("FullSettings.ini"), "DiagnosticRuleEvaluation=False")), "full + Evaluation=False");
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettingsWithUnknownKeys.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Input("FullSettingsWithUnknownKeys.ini"), "DiagnosticRuleEvaluation=False")), "full with unknown keys + Evaluation=False");
        }

        [TestMethod]
        public void GoldensHoldWithEveryKeyAtDefault() {
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("Default.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Golden("Default.ini"), EveryKeyAtDefault)), "default + every key at default");
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettings.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Input("FullSettings.ini"), EveryKeyAtDefault)), "full + every key at default");
            IniTestFiles.AssertBytes(File.ReadAllBytes(IniTestFiles.Golden("FullSettingsWithUnknownKeys.ini")),
                ReadThenWrite(InputWithLines(IniTestFiles.Input("FullSettingsWithUnknownKeys.ini"), EveryKeyAtDefault)), "full with unknown keys + every key at default");
        }

        [DataTestMethod]
        [DataRow(true, 0.0, "DiagnosticBlock-EvaluationOn.ini")]
        [DataRow(false, 0.5, "DiagnosticBlock-EvaluationOffWeight.ini")]
        public void IniWithDiagnosticBlock(bool evaluation, double weight, string blockGolden) {
            var param = MsFinderIniParcer.Read(IniTestFiles.Input("FullSettings.ini"));
            param.DiagnosticRules.Evaluation = evaluation;
            param.DiagnosticRules.RankingWeight = weight;
            var output = WriteIni(param);

            var expected = File.ReadAllBytes(IniTestFiles.Golden("FullSettings.ini")).Concat(File.ReadAllBytes(IniTestFiles.Golden(blockGolden))).ToArray();
            var reread = MsFinderIniParcer.Read(output);
            IniTestFiles.AssertBytes(expected, output, "full settings + diagnostic block");

            Assert.AreEqual(evaluation, reread.DiagnosticRules.Evaluation);
            Assert.AreEqual(weight, reread.DiagnosticRules.RankingWeight);
            Assert.AreEqual(0, reread.DiagnosticRules.Errors.Count);
        }

        // ---- Key isolation (design 6.4 item 4) ----

        private static readonly string[] NonDefaultKeyLines = {
            "DiagnosticRuleEvaluation=True",
            "DiagnosticRuleRankingWeight=0.25",
            "DiagnosticRuleRankingScope=Measured",
            @"DiagnosticRuleSetDirectory=rules\dev-set",
            "DiagnosticRuleInstrumentType=ESI-QTOF",
            "DiagnosticRuleStrict=True",
        };

        [TestMethod]
        public void NewKeysDoNotLeakIntoOtherSettings() {
            var baseline = MsFinderIniParcer.Read(IniTestFiles.NewTempIni("#empty"));
            Assert.IsTrue(baseline.DiagnosticRules.IsDefault);
            var skip = new HashSet<string> { "param.DiagnosticRules" };
            foreach (var line in NonDefaultKeyLines) {
                var param = MsFinderIniParcer.Read(IniTestFiles.NewTempIni(line));
                Assert.IsFalse(param.DiagnosticRules.IsDefault, line);
                Assert.AreEqual(0, param.DiagnosticRules.Errors.Count, line);
                DeepAssert.AreEqual(baseline, param, "param", skip);
            }
        }

        [TestMethod]
        public void LegacyLinesDoNotLeakIntoDiagnosticRules() {
            var lines = File.ReadAllLines(IniTestFiles.Golden("Default.ini"))
                .Concat(File.ReadAllLines(IniTestFiles.Input("FullSettingsWithUnknownKeys.ini")))
                .Where(line => line.Length > 0 && line[0] != '#')
                .Distinct()
                .ToList();
            Assert.IsTrue(lines.Count > 150, "expected every written line and every full-settings line");
            var expected = new MsfinderDiagnosticRuleSettings();
            foreach (var line in lines) {
                var param = MsFinderIniParcer.Read(IniTestFiles.NewTempIni(line));
                DeepAssert.AreEqual(expected, param.DiagnosticRules, "DiagnosticRules <- " + line);
            }
        }

        // ---- Culture (design 6.2) ----

        private static IEnumerable<CultureInfo> Cultures() {
            yield return CultureInfo.InvariantCulture;
            foreach (var name in new[] { "en-US", "de-DE", "fr-FR", "sv-SE", "tr-TR", "cs-CZ", "ar-SA", "th-TH" }) {
                yield return new CultureInfo(name);
            }
            var odd = (CultureInfo)new CultureInfo("en-US").Clone();
            odd.NumberFormat.NumberDecimalSeparator = ",";
            odd.NumberFormat.NumberGroupSeparator = " ";
            odd.NumberFormat.NegativeSign = "−";
            yield return odd;
        }

        [TestMethod]
        public void IniRoundTripOfTheNewKeysIsTheSameUnderEveryCulture() {
            var source = NonDefault();
            var expectedBlock = WriteText(source);
            var original = CultureInfo.CurrentCulture;
            var originalUi = CultureInfo.CurrentUICulture;
            try {
                foreach (var culture in Cultures()) {
                    CultureInfo.CurrentCulture = culture;
                    CultureInfo.CurrentUICulture = culture;
                    Assert.AreEqual(expectedBlock, WriteText(source), culture.Name);

                    var param = new AnalysisParamOfMsfinder { DiagnosticRules = source.Clone() };
                    var output = WriteIni(param);
                    var text = File.ReadAllText(output, Encoding.ASCII);
                    var start = text.IndexOf(DiagnosticRuleIni.BlockHeader, StringComparison.Ordinal);
                    Assert.IsTrue(start > 0, culture.Name);
                    Assert.AreEqual(expectedBlock, text.Substring(start), culture.Name);

                    var reread = MsFinderIniParcer.Read(output);
                    AssertSameSettings(source, reread.DiagnosticRules);
                    File.Delete(output);
                }
            }
            finally {
                CultureInfo.CurrentCulture = original;
                CultureInfo.CurrentUICulture = originalUi;
            }
        }
    }
}
