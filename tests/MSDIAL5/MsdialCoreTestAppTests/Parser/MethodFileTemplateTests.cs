using CompMs.App.MsdialConsole.Parser;
using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialGcMsApi.Parameter;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialLcImMsApi.Parameter;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Newtonsoft.Json.Serialization;
using System;
using System.Collections.Generic;
using System.CommandLine;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class MethodFileTemplateTests
{
    /// <summary>
    /// EVERY KEY IN A TEMPLATE IS ONE THE READER UNDERSTANDS.
    /// </summary>
    /// <remarks>
    /// The point of the template is that an analyst can start from it and trust every line. A key
    /// written here and reported as having no effect would put the template in exactly the position
    /// of the method files it replaces.
    /// </remarks>
    [TestMethod]
    [DataRow(MethodFileMode.Gcms, IonMode.Positive)]
    [DataRow(MethodFileMode.Gcms, IonMode.Negative)]
    [DataRow(MethodFileMode.Lcms, IonMode.Positive)]
    [DataRow(MethodFileMode.Lcms, IonMode.Negative)]
    [DataRow(MethodFileMode.Dims, IonMode.Positive)]
    [DataRow(MethodFileMode.Dims, IonMode.Negative)]
    [DataRow(MethodFileMode.Imms, IonMode.Positive)]
    [DataRow(MethodFileMode.Imms, IonMode.Negative)]
    [DataRow(MethodFileMode.Lcimms, IonMode.Positive)]
    [DataRow(MethodFileMode.Lcimms, IonMode.Negative)]
    public void Template_ReadsBackWithoutAKeyThatHadNoEffect(MethodFileMode mode, IonMode ionMode)
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile("method.txt", MethodFileTemplate.Create(mode, ionMode));

        var (_, report) = Read(mode, method);

        Assert.IsFalse(report.Contains("NO EFFECT"), report);
        Assert.IsFalse(report.Contains("could not be read"), report);
    }

    /// <summary>
    /// READING A TEMPLATE BACK CHANGES NOTHING.
    /// </summary>
    /// <remarks>
    /// The template says its values are the built-in defaults. This holds it to that across the
    /// whole parameter object, not only the properties the template names: a key that the reader
    /// assigns to a different property than the one the template took its value from shows up here
    /// even when both spellings are understood.
    /// </remarks>
    [TestMethod]
    [DataRow(MethodFileMode.Gcms, IonMode.Positive)]
    [DataRow(MethodFileMode.Gcms, IonMode.Negative)]
    [DataRow(MethodFileMode.Lcms, IonMode.Positive)]
    [DataRow(MethodFileMode.Lcms, IonMode.Negative)]
    [DataRow(MethodFileMode.Dims, IonMode.Positive)]
    [DataRow(MethodFileMode.Dims, IonMode.Negative)]
    [DataRow(MethodFileMode.Imms, IonMode.Positive)]
    [DataRow(MethodFileMode.Imms, IonMode.Negative)]
    [DataRow(MethodFileMode.Lcimms, IonMode.Positive)]
    [DataRow(MethodFileMode.Lcimms, IonMode.Negative)]
    public void Template_ReadsBackAsTheBuiltInDefaults(MethodFileMode mode, IonMode ionMode)
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile("method.txt", MethodFileTemplate.Create(mode, ionMode));

        var (parsed, _) = Read(mode, method);
        var defaults = CreateDefault(mode);
        Assert.AreEqual(ionMode, parsed.IonMode);
        defaults.IonMode = ionMode;
        if (mode != MethodFileMode.Gcms) {
#pragma warning disable CS0618 // Type or member is obsolete
            // CommonProcess turns None into DDA before anything reads it; the template writes the DDA
            // a run actually uses.
            Assert.AreEqual(AcquisitionType.DDA, parsed.ProjectParam.AcquisitionType);
            defaults.ProjectParam.AcquisitionType = AcquisitionType.DDA;
#pragma warning restore CS0618 // Type or member is obsolete
            // The parameter starts with no adducts and PeakCharacterEstimator then searches the
            // proton adduct of the ion mode; the template writes that adduct out.
            CollectionAssert.AreEqual(new[] { MethodFileTemplate.DefaultAdduct(ionMode) }, parsed.SearchedAdductIons.Select(a => a.AdductIonName).ToArray());
            Assert.AreEqual(0, defaults.SearchedAdductIons.Count);
            defaults.SearchedAdductIons = parsed.SearchedAdductIons;
        }
        var expected = Snapshot(defaults);
        Assert.IsTrue(expected.SelectTokens("$..MassSliceWidth").Any(), "the snapshot must reach the peak-picking parameters");
        Assert.IsTrue(expected.SelectTokens("$..TotalScoreCutoff").Count() >= 2, "the snapshot must reach the search parameters");

        var differences = Differences(expected, Snapshot(parsed), "").ToList();

        Assert.AreEqual(0, differences.Count, string.Join(Environment.NewLine, differences));
    }

    /// <summary>
    /// The comparison above can see a changed value, so its silence means something.
    /// </summary>
    [TestMethod]
    public void Template_AChangedValueIsSeenByTheComparison()
    {
        using var directory = new TemporaryDirectory();
        var template = MethodFileTemplate.Create(MethodFileMode.Lcms);
        StringAssert.Contains(template, "Minimum peak height: 1000\n");
        var method = directory.CreateFile("method.txt", template.Replace("Minimum peak height: 1000\n", "Minimum peak height: 500\n"));

        var (parsed, _) = Read(MethodFileMode.Lcms, method);
        var differences = Differences(Snapshot(new MsdialLcmsParameter()), Snapshot(parsed), "").ToList();

        Assert.IsTrue(differences.Any(d => d.Contains("MinimumAmplitude") && d.Contains("500")), string.Join(Environment.NewLine, differences));
    }

    [TestMethod]
    public void SnapshotDetectsChangesToTheForwardedModificationLists()
    {
        var parameter = new MsdialLcmsParameter();
        var expected = Snapshot(parameter);
        parameter.VariableModifications[0].IsSelected = !parameter.VariableModifications[0].IsSelected;
        parameter.FixedModifications[0].IsSelected = !parameter.FixedModifications[0].IsSelected;

        var differences = Differences(expected, Snapshot(parameter), "").ToArray();

        Assert.IsTrue(differences.Any(d => d.Contains("ProteomicsParam.VariableModifications")));
        Assert.IsTrue(differences.Any(d => d.Contains("ProteomicsParam.FixedModifications")));
    }

    /// <summary>
    /// The blank filtering lines of a template can be edited to either comparison without the
    /// reader refusing the file.
    /// </summary>
    /// <remarks>
    /// The reader refuses a method file whose blank filtering lines disagree, so a template that
    /// also carried a ratio shorthand would be refused the moment the analyst changed one line.
    /// </remarks>
    [TestMethod]
    public void Template_BlankFilteringCanBeSwitchedToSampleAverage()
    {
        using var directory = new TemporaryDirectory();
        var template = MethodFileTemplate.Create(MethodFileMode.Lcms);
        StringAssert.Contains(template, "Blank filtering: SampleMaxOverBlankAve\n");
        StringAssert.Contains(template, "Fold change for blank filtering: 5\n");
        var method = directory.CreateFile("method.txt", template
            .Replace("Blank filtering: SampleMaxOverBlankAve\n", "Blank filtering: SampleAveOverBlankAve\n")
            .Replace("Fold change for blank filtering: 5\n", "Fold change for blank filtering: 3\n"));

        var (parsed, report) = Read(MethodFileMode.Lcms, method);

        Assert.AreEqual(BlankFiltering.SampleAveOverBlankAve, parsed.BlankFiltering);
        Assert.AreEqual(3f, parsed.FoldChangeForBlankFiltering);
        Assert.IsFalse(report.Contains("NO EFFECT"), report);
    }

    [TestMethod]
    public void LcmsTemplate_ReadsBackTheConsoleOptionsAsTheirDefaults()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile("method.txt", MethodFileTemplate.Create(MethodFileMode.Lcms));

        Assert.IsFalse(ConfigParser.ReadAlignmentLightMode(method));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(method));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(method));
        Assert.AreEqual(1, ConfigParser.ReadLbmAnnotatorPriority(method));
        Assert.AreEqual(0, ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter()).Count);
        Assert.AreEqual(0, ConfigParser.ReadTextAnnotatorSettings(method, new MsdialLcmsParameter()).Count);
    }

    [TestMethod]
    public void Template_WritesNumbersInTheInvariantCulture()
    {
        var original = System.Threading.Thread.CurrentThread.CurrentCulture;
        try {
            System.Threading.Thread.CurrentThread.CurrentCulture = new System.Globalization.CultureInfo("de-DE");
            var template = MethodFileTemplate.Create(MethodFileMode.Lcms);

            StringAssert.Contains(template, "Mass slice width: 0.1");
        }
        finally {
            System.Threading.Thread.CurrentThread.CurrentCulture = original;
        }
    }

    /// <summary>
    /// A template cannot hold an ion mode the reader would silently run as Positive.
    /// </summary>
    [TestMethod]
    [DataRow("template lcms --ionmode Both")]
    [DataRow("template lcms --ionmode 5")]
    [DataRow("template xyz")]
    public void TemplateCommand_RefusesWhatTheReaderCannotRun(string commandLine)
    {
        var root = new RootCommand();
        MainProcess.SetTemplateCommand(root);

        var result = root.Parse(commandLine);

        Assert.AreNotEqual(0, result.Errors.Count, commandLine);
    }

    [TestMethod]
    [DataRow("template lcms --ionmode Negative")]
    [DataRow("template LCMS --ionmode negative")]
    public void TemplateCommand_AcceptsModesAndIonModesInAnyCase(string commandLine)
    {
        var root = new RootCommand();
        MainProcess.SetTemplateCommand(root);

        var result = root.Parse(commandLine);

        Assert.AreEqual(0, result.Errors.Count, string.Join(Environment.NewLine, result.Errors.Select(e => e.Message)));
    }

    [TestMethod]
    public void Template_RefusesAnIonModeOtherThanPositiveOrNegative()
    {
        Assert.ThrowsException<ArgumentOutOfRangeException>(() => MethodFileTemplate.Create(MethodFileMode.Lcms, IonMode.Both));
    }

    /// <summary>
    /// AN EAD RUN STARTED FROM THE TEMPLATE CAN HAVE THE GUI'S LBM CUT-OFFS.
    /// </summary>
    /// <remarks>
    /// The template writes the CID cut-offs for the LBM library, and the Console lowers only the
    /// cut-offs a file leaves unset. Its comment therefore tells an EAD user to blank five lines;
    /// this holds that advice to giving the values the GUI uses.
    /// </remarks>
    [TestMethod]
    [DataRow(MethodFileMode.Lcms)]
    [DataRow(MethodFileMode.Dims)]
    public void Template_EadWithTheLbmCutOffsBlankedHasTheGuiValues(MethodFileMode mode)
    {
        using var directory = new TemporaryDirectory();
        var template = MethodFileTemplate.Create(mode);
        StringAssert.Contains(template, "\nCollision type:\n");
        string[] cutOffs = [
            "Square root of weighted dot product cutoff for LBM-based annotation",
            "Square root of simple dot product cutoff for LBM-based annotation",
            "Square root of reverse dot product cutoff for LBM-based annotation",
            "Matched peaks percentage cutoff for LBM-based annotation",
            "Minimum spectrum match for LBM-based annotation",
        ];
        var lines = template.Split('\n').Select(line =>
            line == "Collision type:" ? "Collision type: EAD"
            : line.StartsWith("Target omics:", StringComparison.Ordinal) ? "Target omics: Lipidomics"
            : cutOffs.FirstOrDefault(key => line.StartsWith(key + ":", StringComparison.Ordinal)) is { } key ? key + ":"
            : line);
        var method = directory.CreateFile("method.txt", string.Join("\n", lines));

        var (parsed, report) = Read(mode, method);

        Assert.AreEqual(CollisionType.EIEIO, parsed.CollistionType);
        Assert.AreEqual(0.05f, parsed.LbmSearchParam.WeightedDotProductCutOff);
        Assert.AreEqual(0.05f, parsed.LbmSearchParam.SimpleDotProductCutOff);
        Assert.AreEqual(0.05f, parsed.LbmSearchParam.ReverseDotProductCutOff);
        Assert.AreEqual(0f, parsed.LbmSearchParam.MatchedPeaksPercentageCutOff);
        Assert.AreEqual(1f, parsed.LbmSearchParam.MinimumSpectrumMatch);
        Assert.IsFalse(report.Contains("NO EFFECT"), report);
    }

    [TestMethod]
    [DataRow(MethodFileMode.Lcms)]
    [DataRow(MethodFileMode.Dims)]
    public void Template_GeneratedLipidKeysReadBackAsTheGuiDefaults(MethodFileMode mode)
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile("method.txt", MethodFileTemplate.Create(mode));

        var setting = ConfigParser.ReadGeneratedLipidAnnotatorSetting(method);

        var expected = GeneratedLipidAnnotatorSetting.CreateDefaultSearchParameter();
        Assert.IsNull(setting.Enabled);
        Assert.IsNull(setting.Priority);
        Assert.AreEqual(expected.Ms1Tolerance, setting.SearchParameter.Ms1Tolerance);
        Assert.AreEqual(expected.Ms2Tolerance, setting.SearchParameter.Ms2Tolerance);
        Assert.AreEqual(expected.WeightedDotProductCutOff, setting.SearchParameter.WeightedDotProductCutOff);
        Assert.AreEqual(expected.SimpleDotProductCutOff, setting.SearchParameter.SimpleDotProductCutOff);
        Assert.AreEqual(expected.ReverseDotProductCutOff, setting.SearchParameter.ReverseDotProductCutOff);
        Assert.AreEqual(expected.MatchedPeaksPercentageCutOff, setting.SearchParameter.MatchedPeaksPercentageCutOff);
        Assert.AreEqual(expected.MinimumSpectrumMatch, setting.SearchParameter.MinimumSpectrumMatch);
        Assert.AreEqual(expected.TotalScoreCutoff, setting.SearchParameter.TotalScoreCutoff);
    }

    /// <summary>
    /// Only the modes that build the generated lipid library offer its settings.
    /// </summary>
    [TestMethod]
    [DataRow(MethodFileMode.Gcms)]
    [DataRow(MethodFileMode.Imms)]
    [DataRow(MethodFileMode.Lcimms)]
    public void Template_OtherModesDoNotOfferTheGeneratedLipidLibrary(MethodFileMode mode)
    {
        var template = MethodFileTemplate.Create(mode);

        Assert.IsFalse(template.Contains("Collision type"), template);
        Assert.IsFalse(template.Contains("generated lipid", StringComparison.OrdinalIgnoreCase), template);
    }

    private static ParameterBase CreateDefault(MethodFileMode mode) => mode switch {
        MethodFileMode.Gcms => new MsdialGcmsParameter(),
        MethodFileMode.Lcms => new MsdialLcmsParameter(),
        MethodFileMode.Dims => new MsdialDimsParameter(),
        MethodFileMode.Imms => new MsdialImmsParameter(),
        MethodFileMode.Lcimms => new MsdialLcImMsParameter(),
        _ => throw new ArgumentOutOfRangeException(nameof(mode)),
    };

    private static (ParameterBase, string) Read(MethodFileMode mode, string methodFile)
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try {
            Console.SetOut(captured);
            ParameterBase parameter = mode switch {
                MethodFileMode.Gcms => ConfigParser.ReadForGcms(methodFile),
                MethodFileMode.Lcms => ConfigParser.ReadForLcmsParameter(methodFile),
                MethodFileMode.Dims => ConfigParser.ReadForDimsParameter(methodFile),
                MethodFileMode.Imms => ConfigParser.ReadForImmsParameter(methodFile),
                MethodFileMode.Lcimms => ConfigParser.ReadForLcImMsParameter(methodFile),
                _ => throw new ArgumentOutOfRangeException(nameof(mode)),
            };
            return (parameter, captured.ToString());
        }
        finally {
            Console.SetOut(original);
        }
    }

    private static JToken Snapshot(ParameterBase parameter)
    {
        // The top-level accessors forward these large lists to ProteomicsParam. Verify the
        // forwarding and compare the lists there, instead of serializing both copies.
        Assert.AreSame(parameter.ProteomicsParam.VariableModifications, parameter.VariableModifications);
        Assert.AreSame(parameter.ProteomicsParam.FixedModifications, parameter.FixedModifications);
        // A property the serializer cannot read would drop out of both snapshots and so never be
        // compared. It is recorded and fails the test rather than being skipped in silence.
        var errors = new List<string>();
        var json = JsonConvert.SerializeObject(parameter, new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore,
            ContractResolver = SnapshotContractResolver.Instance,
            Error = (_, args) => {
                errors.Add($"{args.ErrorContext.Path}: {args.ErrorContext.Error.Message}");
                args.ErrorContext.Handled = true;
            },
        });
        Assert.AreEqual(0, errors.Count, "properties the snapshot could not read: " + string.Join(Environment.NewLine, errors));
        var token = JToken.Parse(json);
        // Both are DateTime.Now at construction, so two fresh objects never agree on them.
        foreach (var stamp in token.SelectTokens("$..FinalSavedDate").Concat(token.SelectTokens("$..ProjectStartDate")).ToList()) {
            stamp.Replace(JValue.CreateNull());
        }
        return token;
    }

    private sealed class SnapshotContractResolver : DefaultContractResolver {
        public static readonly SnapshotContractResolver Instance = new();

        protected override JsonProperty CreateProperty(System.Reflection.MemberInfo member, MemberSerialization memberSerialization) {
            var property = base.CreateProperty(member, memberSerialization);
            if (member.DeclaringType == typeof(ParameterBase)
                && (member.Name == nameof(ParameterBase.VariableModifications) || member.Name == nameof(ParameterBase.FixedModifications))) {
                property.ShouldSerialize = _ => false;
            }
            return property;
        }
    }

    private static IEnumerable<string> Differences(JToken expected, JToken actual, string path)
    {
        if (expected is JObject expectedObject && actual is JObject actualObject) {
            foreach (var name in expectedObject.Properties().Select(p => p.Name).Union(actualObject.Properties().Select(p => p.Name))) {
                foreach (var difference in Differences(expectedObject[name] ?? JValue.CreateNull(), actualObject[name] ?? JValue.CreateNull(), path + "." + name)) {
                    yield return difference;
                }
            }
            yield break;
        }
        if (!JToken.DeepEquals(expected, actual)) {
            yield return $"{path}: default {expected.ToString(Formatting.None)}, read back {actual.ToString(Formatting.None)}";
        }
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

        public string CreateFile(string name, string content)
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content);
            return path;
        }

        public void Dispose()
        {
            try {
                Directory.Delete(Path, recursive: true);
            }
            catch (IOException) {
            }
            catch (UnauthorizedAccessException) {
            }
        }
    }
}
