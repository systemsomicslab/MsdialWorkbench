using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// The lipid RT check's method-file keys (SPEC section 11 with the addendum's defaults): read by the LC-MS reader into
/// the parameter, recorded as applied by the key record, refused when the value is not one of the choices, unknown to
/// every other mode, written by the LC-MS template, and the same keys the parameter text writes.
/// </summary>
[TestClass]
public sealed class LipidRtCheckMethodKeyTests
{
    private static readonly string[] Keys = {
        "Lipid RT check",
        "Lipid RT check operating point",
        "Lipid RT check chromatography",
        "Lipid RT check plausibility profile",
        "Lipid RT check class-level matches",
        "Lipid RT check minor duplicates",
        "Lipid RT check secondary-only features",
    };

    [TestMethod]
    public void ReadForLcms_AppliesEveryLipidRtCheckKey()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Target omics: Lipidomics
            Lipid RT check: False
            Lipid RT check operating point: strict
            Lipid RT check chromatography: ReversedPhase
            Lipid RT check plausibility profile: Mammalian
            Lipid RT check class-level matches: Consistent
            Lipid RT check minor duplicates: Block
            Lipid RT check secondary-only features: Block
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        var check = parameter.LipidRtCheckParam;
        Assert.AreEqual(false, check.IsEnabled);
        Assert.AreEqual(LipidRtOperatingPoint.Strict, check.OperatingPoint);
        Assert.AreEqual(LipidRtChromatography.ReversedPhase, check.Chromatography);
        Assert.AreEqual(LipidRtPlausibilityProfileKind.Mammalian, check.PlausibilityProfile);
        Assert.AreEqual(LipidRtClassLevelPolicy.Consistent, check.ClassLevelPolicy);
        Assert.AreEqual(LipidRtMinorDuplicatePolicy.Block, check.MinorDuplicatePolicy);
        Assert.AreEqual(LipidRtSecondaryOnlyPolicy.Block, check.SecondaryOnlyPolicy);
        Assert.IsFalse(check.RunsFor(parameter.TargetOmics));
        CollectionAssert.AreEqual(new[] { "Target omics" }.Concat(Keys).ToArray(), RecordedKeys(methodFile, "applied").ToArray());
        Assert.AreEqual(0, RecordedKeys(methodFile, "unrecognised").Count);
        Assert.AreEqual(0, RecordedKeys(methodFile, "unusable").Count);
    }

    [TestMethod]
    [DataRow("Auto", null)]
    [DataRow("auto", null)]
    [DataRow("True", true)]
    [DataRow("false", false)]
    public void ReadForLcms_TheSwitchTakesAutoTrueOrFalse(string value, bool? expected)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", $"Lipid RT check: False\nLipid RT check: {value}\n");

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(expected, parameter.LipidRtCheckParam.IsEnabled);
    }

    /// <summary>
    /// A value that is not one of the choices is refused and named, never read as the value a number happens to index
    /// or silently replaced by the default.
    /// </summary>
    [TestMethod]
    public void ReadForLcms_RefusesAValueThatIsNotAChoice()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Lipid RT check: yes
            Lipid RT check operating point: 0
            Lipid RT check plausibility profile: Plant
            Lipid RT check class-level matches: 2
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(
            new[] {
                "Lipid RT check: yes",
                "Lipid RT check operating point: 0",
                "Lipid RT check plausibility profile: Plant",
                "Lipid RT check class-level matches: 2",
            },
            RecordedKeys(methodFile, "unusable"));
        var defaults = new LipidRtCheckParameter();
        Assert.AreEqual(defaults.ComputeHash(), parameter.LipidRtCheckParam.ComputeHash(), "the run used the defaults");
        Assert.IsNull(parameter.LipidRtCheckParam.IsEnabled);
    }

    /// <summary>Only LC-MS processing runs the check, so every other mode reports its keys as having no effect.</summary>
    [TestMethod]
    public void OtherModes_ReportTheLipidRtCheckKeysAsUnrecognised()
    {
        var readers = new (string Mode, Action<string> Read)[] {
            ("gcms", path => ConfigParser.ReadForGcms(path)),
            ("dims", path => ConfigParser.ReadForDimsParameter(path)),
            ("imms", path => ConfigParser.ReadForImmsParameter(path)),
            ("lcimms", path => ConfigParser.ReadForLcImMsParameter(path)),
        };
        using var directory = new TemporaryDirectory();
        foreach (var (mode, read) in readers) {
            var methodFile = directory.CreateFile($"{mode}.txt", "Lipid RT check: False\nLipid RT check operating point: Mid\n");

            read(methodFile);

            CollectionAssert.AreEqual(new[] { "Lipid RT check", "Lipid RT check operating point" }, RecordedKeys(methodFile, "unrecognised"), mode);
        }
    }

    /// <summary>
    /// The setting lines of the parameter text are method-file lines: copied into a method file they reproduce the
    /// settings. The last three lines record what the run did with them and are not keys.
    /// </summary>
    [TestMethod]
    public void TheParameterTextLinesReadBackAsTheSameSettings()
    {
        var written = new MsdialLcmsParameter { TargetOmics = TargetOmics.Lipidomics };
        written.LipidRtCheckParam.IsEnabled = true;
        written.LipidRtCheckParam.OperatingPoint = LipidRtOperatingPoint.Balanced;
        written.LipidRtCheckParam.PlausibilityProfile = LipidRtPlausibilityProfileKind.Mammalian;
        written.LipidRtCheckParam.MinorDuplicatePolicy = LipidRtMinorDuplicatePolicy.Block;
        var text = written.ParametersAsText();
        var section = text.SkipWhile(line => line != "# Lipid RT check").Skip(1).TakeWhile(line => line.StartsWith("Lipid RT check")).ToList();
        CollectionAssert.AreEqual(Keys.Concat(new[] { "Lipid RT check runs for this project", "Lipid RT check rule version", "Lipid RT check parameter hash" }).ToArray(),
            section.Select(line => line.Substring(0, line.IndexOf(':'))).ToArray());
        StringAssert.Contains(string.Join("\n", section), "Lipid RT check runs for this project: True");
        StringAssert.Contains(string.Join("\n", section), "Lipid RT check parameter hash: " + written.LipidRtCheckParam.ComputeHash());

        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", string.Join("\n", section.Take(Keys.Length)) + "\n");
        var read = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(written.LipidRtCheckParam.IsEnabled, read.LipidRtCheckParam.IsEnabled);
        Assert.AreEqual(written.LipidRtCheckParam.ComputeHash(), read.LipidRtCheckParam.ComputeHash());
        CollectionAssert.AreEqual(Keys, RecordedKeys(methodFile, "applied"));
    }

    [TestMethod]
    public void OnlyTheLcmsParameterTextHasTheSection()
    {
        Assert.IsTrue(new MsdialLcmsParameter().ParametersAsText().Contains("# Lipid RT check"));
        Assert.IsFalse(new MsdialLcImMsParameter().ParametersAsText().Contains("# Lipid RT check"), "LC-IM-MS does not run the check");
    }

    [TestMethod]
    public void TheLcmsTemplateWritesTheKeysWithTheirDefaults()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Lcms);

        StringAssert.Contains(template, "Lipid RT check: Auto\n");
        StringAssert.Contains(template, "Lipid RT check operating point: HighPrecision\n");
        StringAssert.Contains(template, "Lipid RT check chromatography: Auto\n");
        StringAssert.Contains(template, "Lipid RT check plausibility profile: None\n");
        StringAssert.Contains(template, "Lipid RT check class-level matches: Corroborated\n");
        StringAssert.Contains(template, "Lipid RT check minor duplicates: Allow\n");
        StringAssert.Contains(template, "Lipid RT check secondary-only features: Evaluate\n");
        foreach (var mode in new[] { MethodFileMode.Gcms, MethodFileMode.Dims, MethodFileMode.Imms, MethodFileMode.Lcimms }) {
            Assert.IsFalse(MethodFileTemplate.Create(mode).Contains("Lipid RT check"), mode.ToString());
        }
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))[list]!.Select(item => (string)item!).ToList();
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(
                System.IO.Path.GetTempPath(),
                "MsdialCoreTestAppTests",
                Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
