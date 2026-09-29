using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.IO;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class ConfigParserTests
{
    [TestMethod]
    public void ReadCommonParameter_UpdatesActiveBlankFilteringFoldChange()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "sample max / blank average", "7");

        Assert.IsTrue(result.IsApplied);
        Assert.AreEqual(7f, parameter.SampleMaxOverBlankAverage);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
    }
 
    [TestMethod]
    public void ReadCommonParameter_AcceptsAMinimumPeakHeightWrittenAsARealNumber()
    {
        // THE REGRESSION. MinimumAmplitude is a double, ParameterBase:589 writes it back as one,
        // and MS-DIAL Interactive writes it from a Python float, so every method file in the
        // reanalysis workspace carried "Minimum peak height: 500.0". The arm parsed it with
        // int.TryParse and returned true regardless, so the value was discarded, MinimumAmplitude
        // kept its built-in 1000, and the run recorded 500 in its audit trail. Every threshold the
        // contract's zero-threshold diagnostic produced was thrown away exactly this way.
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "500.0");

        Assert.IsTrue(result.IsApplied);
        Assert.AreEqual(500d, parameter.MinimumAmplitude);
    }

    [TestMethod]
    public void ReadCommonParameter_StillAcceptsAWholeNumberedThreshold()
    {
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "0").IsApplied);
        Assert.AreEqual(0d, parameter.MinimumAmplitude, "the diagnostic run sets the threshold to zero");
    }

    [TestMethod]
    public void ReadCommonParameter_ReportsAValueItCannotReadInsteadOfClaimingItApplied()
    {
        // The distinction the old bool could not make. An unusable value is not an unknown key:
        // the key is spelled correctly, so nobody reading the method file would suspect it.
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "minimum peak height", "quite high");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.IsFalse(result.IsApplied);
        Assert.IsFalse(result.IsUnknownKey);
        Assert.AreEqual(1000d, parameter.MinimumAmplitude, "the built-in default, untouched");
    }

    [TestMethod]
    public void ReadCommonParameter_KeepsAnUnknownKeyDistinctFromAnUnusableValue()
    {
        var parameter = new MsdialLcmsParameter();

        var result = ConfigParser.ReadCommonParameter(parameter, "a parameter that does not exist", "7");

        Assert.IsTrue(result.IsUnknownKey);
        Assert.IsFalse(result.IsUnusableValue);
    }

    [TestMethod]
    public void ReadCommonParameter_RefusesACountThatNamesNoWholeNumber()
    {
        // "5" and "5.0" are the same count because the writers disagree about which they emit.
        // "5.7" is not a count, and rounding it to 6 would hide that whoever wrote it believed
        // this parameter could express something it cannot.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "smoothing level", "5.0").IsApplied);
        Assert.AreEqual(5, parameter.SmoothingLevel);
        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "smoothing level", "5.7").IsUnusableValue);
        Assert.AreEqual(5, parameter.SmoothingLevel, "the refused value left the previous one alone");
    }

    [TestMethod]
    public void ReadCommonParameter_ReadsARealNumberTheSameWayOnEveryMachine()
    {
        // A method file is machine-written. It must mean the same thing where the decimal
        // separator is a comma, so the readers parse with the invariant culture.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "mass slice width", "0.1").IsApplied);
        Assert.AreEqual(0.1f, parameter.MassSliceWidth, 1e-7f);
    }

    [TestMethod]
    public void ReadCommonParameter_ReadsTheTwoKeysABadRenameHadMadeUnreachable()
    {
        // A `value` -> `valueLower` rename was applied inside the case labels themselves, so
        // "Sigma window value" and "Replace true zero values with 1/2 of minimum peak height over
        // all samples" could never be set from a method file however correctly they were spelled.
        // Both are written into every exported method file by ParameterBase.
        var parameter = new MsdialLcmsParameter();

        Assert.IsTrue(ConfigParser.ReadCommonParameter(parameter, "sigma window value", "0.7").IsApplied);
        Assert.AreEqual(0.7f, parameter.SigmaWindowValue, 1e-7f);

        var replace = ConfigParser.ReadCommonParameter(
            parameter, "replace true zero values with 1/2 of minimum peak height over all samples", "true");

        Assert.IsTrue(replace.IsApplied);
        Assert.IsTrue(parameter.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples);
    }

    [TestMethod]
    public void ReadCommonParameter_TreatsAThreadCountOutsideTheUsableRangeAsUnusable()
    {
        var parameter = new MsdialLcmsParameter();
        var before = parameter.NumThreads;

        var result = ConfigParser.ReadCommonParameter(parameter, "number of threads", "0");

        Assert.IsTrue(result.IsUnusableValue);
        Assert.AreEqual(before, parameter.NumThreads);
    }

    [TestMethod]
    public void ReadForLcms_WritesWhatHappenedToEveryKeyBesideTheMethodFile()
    {
        // The Console said all of this on stdout and nowhere else. Stdout reaches a log the caller
        // keeps for as long as it keeps the job, and the contract's retained artifacts do not
        // include it, so an audit reading a unit's workspace could see the method file's declared
        // settings and could not see which of them the run had used.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Minimum peak height: 500.0
            Mass slice width: 0.1
            Sigma window value: 0.7
            A parameter that does not exist: 7
            Smoothing level: 5.7
            Msp file path:
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        var record = Path.Combine(Path.GetDirectoryName(methodFile)!, "method.keys.json");
        Assert.IsTrue(File.Exists(record), "the key record must land beside the method file");
        var parsed = JObject.Parse(File.ReadAllText(record));

        Assert.AreEqual("msdial-method-file-keys.v1", (string?)parsed["schema"]);
        Assert.AreEqual("method.txt", (string?)parsed["method_file"]);
        Assert.AreEqual(64, ((string?)parsed["method_file_sha256"])!.Length, "sha256 of the file that was read");

        var applied = parsed["applied"]!.Select(item => (string)item!).ToList();
        var unrecognised = parsed["unrecognised"]!.Select(item => (string)item!).ToList();
        var unusable = parsed["unusable"]!.Select(item => (string)item!).ToList();
        var blank = parsed["blank"]!.Select(item => (string)item!).ToList();

        CollectionAssert.Contains(applied, "Minimum peak height");
        CollectionAssert.Contains(applied, "Mass slice width");
        CollectionAssert.Contains(applied, "Sigma window value");
        CollectionAssert.Contains(unrecognised, "A parameter that does not exist");
        CollectionAssert.Contains(unusable, "Smoothing level: 5.7");
        CollectionAssert.Contains(blank, "Msp file path");
    }

    [TestMethod]
    public void ReadForLcms_KeyRecordSeparatesAnUnusableValueFromAnUnknownKey()
    {
        // The two findings the old bool could not tell apart, now readable from the workspace.
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Minimum peak height: quite high
            Nonexistent parameter: 1
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        var parsed = JObject.Parse(
            File.ReadAllText(Path.Combine(Path.GetDirectoryName(methodFile)!, "method.keys.json")));

        CollectionAssert.Contains(parsed["unusable"]!.Select(i => (string)i!).ToList(), "Minimum peak height: quite high");
        CollectionAssert.Contains(parsed["unrecognised"]!.Select(i => (string)i!).ToList(), "Nonexistent parameter");
        CollectionAssert.DoesNotContain(parsed["applied"]!.Select(i => (string)i!).ToList(), "Minimum peak height");
    }

    /// <summary>
    /// The keys LcmsProcess reads for itself are recorded as applied, not as having no effect.
    /// </summary>
    /// <remarks>
    /// LcmsProcess reads these with readers of their own, after the key record has been written,
    /// and the record used to know only ReadCommonParameter. A repository run's record listed
    /// "MSP annotator settings file path", "LBM annotator priority" and "Alignment light mode" as
    /// unrecognised with NO EFFECT while each of them governed the run, and the reanalysis audits
    /// trust that record.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_KeyRecordListsTheKeysLcmsProcessReadsForItselfAsApplied()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\n" +
            $"library\t{msp}\n");
        var methodFile = directory.CreateFile(
            "method.txt",
            $"""
            MSP annotator settings file path: {settings}
            LBM annotator priority: 3
            Alignment light mode: True
            A parameter that does not exist: 7
            """);

        var (_, report) = ReadLcmsWithReport(methodFile);

        var applied = RecordedKeys(methodFile, "applied");
        foreach (var key in new[] { "MSP annotator settings file path", "LBM annotator priority", "Alignment light mode" }) {
            CollectionAssert.Contains(applied, key);
            Assert.IsFalse(report.Contains($"'{key}'"), $"'{key}' took effect and must not be reported as having none");
        }
        CollectionAssert.AreEqual(new[] { "A parameter that does not exist" }, RecordedKeys(methodFile, "unrecognised"));
        StringAssert.Contains(report, "'A parameter that does not exist' was not recognised and had NO EFFECT");

        // And the run really does take them: the record describes what the side readers do.
        Assert.AreEqual(1, ConfigParser.ReadMspAnnotatorSettings(methodFile, new MsdialLcmsParameter()).Count);
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(methodFile));
        Assert.IsTrue(ConfigParser.ReadAlignmentLightMode(methodFile));
    }

    [TestMethod]
    [DataRow("MSP annotator settings file path", "settings.tsv")]
    [DataRow("MSP annotation settings file path", "settings.tsv")]
    [DataRow("MSP search settings file path", "settings.tsv")]
    [DataRow("Text annotator settings file path", "settings.tsv")]
    [DataRow("Text library annotator settings file path", "settings.tsv")]
    [DataRow("Text DB annotator settings file path", "settings.tsv")]
    [DataRow("Text annotation settings file path", "settings.tsv")]
    [DataRow("LBM annotator priority", "2")]
    [DataRow("LBM annotation priority", "2")]
    [DataRow("Alignment light mode", "true")]
    [DataRow("Alignment light", "false")]
    [DataRow("Console alignment light mode", "true")]
    [DataRow("Detailed alignment provenance", "true")]
    [DataRow("Export detailed alignment provenance", "false")]
    [DataRow("Annotation candidates", "true")]
    [DataRow("Export annotation candidates", "false")]
    public void ReadForLcms_KeyRecordAcceptsEverySpellingTheSideReadersAccept(string key, string value)
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", $"{key}: {value}\n");

        ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(new[] { key }, RecordedKeys(methodFile, "applied"));
        Assert.AreEqual(0, RecordedKeys(methodFile, "unrecognised").Count);
    }

    /// <summary>
    /// A value the side reader passes over is recorded as unusable, as the run used the default.
    /// </summary>
    /// <remarks>
    /// "2.0" is not a priority to the reader and "yes" is not a switch, so those lines are skipped
    /// and a later usable line, or the default, governs. Calling them applied would repeat the
    /// failure this record exists to end.
    /// </remarks>
    [TestMethod]
    public void ReadForLcms_KeyRecordCallsAValueTheSideReaderPassesOverUnusable()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Alignment light mode: yes
            LBM annotator priority: 2.0
            LBM annotator priority: 4
            """);

        ConfigParser.ReadForLcmsParameter(methodFile);

        CollectionAssert.AreEqual(
            new[] { "Alignment light mode: yes", "LBM annotator priority: 2.0" },
            RecordedKeys(methodFile, "unusable"));
        CollectionAssert.AreEqual(new[] { "LBM annotator priority" }, RecordedKeys(methodFile, "applied"));
        Assert.IsFalse(ConfigParser.ReadAlignmentLightMode(methodFile), "the default the run used");
        Assert.AreEqual(4, ConfigParser.ReadLbmAnnotatorPriority(methodFile), "the first usable line the run used");
    }

    /// <summary>
    /// Only LC-MS reads these keys, so every other mode still reports them as having no effect.
    /// </summary>
    [TestMethod]
    public void OtherModes_StillReportTheLcmsOnlyKeysAsUnrecognised()
    {
        var readers = new (string Mode, Action<string> Read)[] {
            ("gcms", path => ConfigParser.ReadForGcms(path)),
            ("dims", path => ConfigParser.ReadForDimsParameter(path)),
            ("imms", path => ConfigParser.ReadForImmsParameter(path)),
            ("lcimms", path => ConfigParser.ReadForLcImMsParameter(path)),
        };
        using var directory = new TemporaryDirectory();
        foreach (var (mode, read) in readers) {
            var methodFile = directory.CreateFile(
                $"{mode}.txt",
                """
                MSP annotator settings file path: settings.tsv
                LBM annotator priority: 3
                Alignment light mode: true
                """);

            read(methodFile);

            CollectionAssert.AreEqual(
                new[] { "MSP annotator settings file path", "LBM annotator priority", "Alignment light mode" },
                RecordedKeys(methodFile, "unrecognised"),
                mode);
        }
    }

    private static List<string> RecordedKeys(string methodFile, string list)
    {
        var record = Path.Combine(
            Path.GetDirectoryName(methodFile)!,
            Path.GetFileNameWithoutExtension(methodFile) + ".keys.json");
        return JObject.Parse(File.ReadAllText(record))[list]!.Select(item => (string)item!).ToList();
    }

    [TestMethod]
    public void ReadForGcms_AcceptsEqualsSyntaxQuotesAndGuiFieldNames()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            Msp file path = "references\Fiehn Library.msp"
            RI dictionary file path: "ri\FAME Mapping.txt"
            RI compound type = Fames
            Retention type: RI
            Alignment index type = RI
            Retention index alignment tolerance: 1234.5
            Retention index tolerance for identification = 567.5
            Weighted dot product cutoff: 0.55
            Simple dot product cutoff = 0.56
            Reverse dot product cutoff: 0.57
            Matched peaks percentage cutoff = 0.58
            Minimum spectrum match: 4
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "references", "Fiehn Library.msp")),
            parameter.MspFilePath);
        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "ri", "FAME Mapping.txt")),
            parameter.RiDictionaryFilePath);
        Assert.AreEqual(RiCompoundType.Fames, parameter.RiCompoundType);
        Assert.AreEqual(RetentionType.RI, parameter.RetentionType);
        Assert.AreEqual(AlignmentIndexType.RI, parameter.AlignmentIndexType);
        Assert.AreEqual(1234.5f, parameter.RetentionIndexAlignmentTolerance);
        Assert.AreEqual(567.5f, parameter.MspSearchParam.RiTolerance);
        Assert.AreEqual(0.55f, parameter.MspSearchParam.WeightedDotProductCutOff);
        Assert.AreEqual(0.56f, parameter.MspSearchParam.SimpleDotProductCutOff);
        Assert.AreEqual(0.57f, parameter.MspSearchParam.ReverseDotProductCutOff);
        Assert.AreEqual(0.58f, parameter.MspSearchParam.MatchedPeaksPercentageCutOff);
        Assert.AreEqual(4f, parameter.MspSearchParam.MinimumSpectrumMatch);
    }

    [TestMethod]
    public void ReadForGcms_AcceptsLegacyRiPathAndAnnotationFieldNames()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "legacy-method.txt",
            """
            RI index file pathes: dictionaries.txt
            RI tolerance for MSP-based annotation: 2000
            """);

        var parameter = ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(
            Path.GetFullPath(Path.Combine(directory.Path, "dictionaries.txt")),
            parameter.RiDictionaryFilePath);
        Assert.AreEqual(2000f, parameter.MspSearchParam.RiTolerance);
    }

    [TestMethod]
    public void ReadMspAnnotatorSettings_UsesPerAnnotatorTargetOmics()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tpriority\ttarget_omics\tms2_tolerance\n" +
            $"high\t{msp}\t2\tMetabolomics\t0.05\n" +
            $"inherit\t{msp}\t1\t\t0.25\n");
        var method = directory.CreateFile(
            "method.txt",
            $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(2, parsed.Count);
        Assert.AreEqual(TargetOmics.Metabolomics, parsed[0].TargetOmics);
        Assert.IsNull(parsed[1].TargetOmics);
        Assert.AreEqual(0.05F, parsed[0].SearchParameter.Ms2Tolerance, 0.0001F);
        Assert.AreEqual(0.25F, parsed[1].SearchParameter.Ms2Tolerance, 0.0001F);
    }

    /// <summary>
    /// A run with no GUI can still say whether a library's spectra were acquired or computed.
    /// </summary>
    /// <remarks>
    /// The GUI asks this through the database-kind dropdown. The reanalysis pipeline never opens
    /// one, so without this the Console could only ever build DataBaseSource.Msp and an in-silico
    /// library -- NEIMS, CFM-ID, ICEBERG -- would be published as a reference-spectrum match.
    /// </remarks>
    [TestMethod]
    public void ReadMspAnnotatorSettings_ReadsTheLibraryKind()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tlibrary_kind\n" +
            $"acquired\t{msp}\tacquired\n" +
            $"computed\t{msp}\tpredicted\n" +
            $"silent\t{msp}\t\n");
        var method = directory.CreateFile(
            "method.txt",
            $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(3, parsed.Count);
        Assert.AreEqual(DataBaseSource.Msp, parsed[0].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[1].DataBaseSource);
        Assert.AreEqual(DataBaseSource.Msp, parsed[2].DataBaseSource,
            "silence means acquired, so an existing settings file runs unchanged");
    }

    [TestMethod]
    public void ReadMspAnnotatorSettings_AcceptsTheSpellingsPeopleWillActuallyType()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tspectra_source\n" +
            $"a\t{msp}\tin silico\n" +
            $"b\t{msp}\tIn-Silico\n" +
            $"c\t{msp}\tGenerated\n" +
            $"d\t{msp}\tExperimental\n");
        var method = directory.CreateFile("method.txt", $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[0].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[1].DataBaseSource);
        Assert.AreEqual(DataBaseSource.PredictedMsp, parsed[2].DataBaseSource);
        Assert.AreEqual(DataBaseSource.Msp, parsed[3].DataBaseSource);
    }

    /// <summary>
    /// A typo does not lose the run; it is reported and the library is treated as acquired.
    /// </summary>
    /// <remarks>
    /// The alternative -- failing the run -- costs a whole reanalysis for a misspelt word, and the
    /// alternative to THAT -- guessing "predicted" -- would publish an in-silico claim nobody made.
    /// </remarks>
    [TestMethod]
    public void ReadMspAnnotatorSettings_TreatsAnUnknownLibraryKindAsAcquired()
    {
        using var directory = new TemporaryDirectory();
        var msp = directory.CreateFile("library.msp");
        var settings = directory.CreateFile(
            "msp_annotator_settings.tsv",
            $"annotator_id\tmsp_file_path\tlibrary_kind\n" +
            $"typo\t{msp}\tpredicated\n");
        var method = directory.CreateFile("method.txt", $"Msp annotator settings file path: {settings}\n");

        var parsed = ConfigParser.ReadMspAnnotatorSettings(method, new MsdialLcmsParameter());

        Assert.AreEqual(1, parsed.Count);
        Assert.AreEqual(DataBaseSource.Msp, parsed[0].DataBaseSource);
    }

    /// <summary>
    /// A METHOD-FILE KEY THAT HAD NO EFFECT SAYS SO.
    /// </summary>
    /// <remarks>
    /// Every dispatcher used to discard the boolean saying whether a reader had claimed the line,
    /// under a comment reading "// write something if needed". So a misspelt key, a key from a newer
    /// MS-DIAL, or a key copied from another mode's template was read, matched nothing, and
    /// vanished; the run used the built-in default and the analyst had every reason to believe their
    /// value had been applied. For a reanalysis campaign that is a silently wrong result with a
    /// method file that appears to document it correctly.
    ///
    /// Reported rather than fatal, and that was measured: run against MS-DIAL's own shipped
    /// lipidomics template this finds twenty-seven such keys, so failing would reject every method
    /// file in existence.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_ReportsAKeyThatHadNoEffect()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Mass slice width: 0.05" + "\n" +
            "Mass slize width: 0.5" + "\n");

        var (parameter, report) = ReadLcmsWithReport(method);

        Assert.AreEqual(0.05f, parameter.MassSliceWidth, 0.0001f, "the correctly spelled key applies");
        StringAssert.Contains(report, "Mass slize width");
        StringAssert.Contains(report, "NO EFFECT");
        Assert.IsFalse(report.Contains("'Mass slice width'"), "a key that worked is not reported");
    }

    /// <summary>
    /// A blank value is reported separately, because it is how a method file says "none".
    /// </summary>
    /// <remarks>
    /// "Msp file path:" with nothing after it is how the templates say there is no MSP library, so
    /// it is not an error. It is still named, because from the analyst's side it looks identical to
    /// a value that failed to apply.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_SeparatesABlankValueFromAnUnrecognisedKey()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Msp file path:" + "\n" +
            "Not a real setting: 3" + "\n");

        var (_, report) = ReadLcmsWithReport(method);

        StringAssert.Contains(report, "left blank");
        StringAssert.Contains(report, "Msp file path");
        StringAssert.Contains(report, "1 parameter(s) had no effect");
    }

    /// <summary>
    /// A method file whose keys are all understood says nothing.
    /// </summary>
    /// <remarks>
    /// The silence matters as much as the warning: a report that fires on every run is one nobody
    /// reads, and the point of this is that the twenty-seven in the template become visible.
    /// </remarks>
    [TestMethod]
    public void ReadForLcmsParameter_IsSilentWhenEveryKeyApplies()
    {
        using var directory = new TemporaryDirectory();
        var method = directory.CreateFile(
            "method.txt",
            "Mass slice width: 0.05" + "\n" +
            "# a comment is not a key" + "\n" +
            "Number of threads: 4" + "\n");

        var (_, report) = ReadLcmsWithReport(method);

        Assert.AreEqual(string.Empty, report.Trim(), report);
    }

    private static (MsdialLcmsParameter, string) ReadLcmsWithReport(string methodFile)
    {
        var original = Console.Out;
        var captured = new StringWriter();
        try {
            Console.SetOut(captured);
            var parameter = ConfigParser.ReadForLcmsParameter(methodFile);
            return (parameter, captured.ToString());
        }
        finally {
            Console.SetOut(original);
        }
    }

    [TestMethod]
    public void ReadLbmAnnotatorPriority_DefaultsToOneAndReadsConfiguredValue()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var configuredMethod = directory.CreateFile("configured.txt", "LBM annotator priority: 3\n");

        Assert.AreEqual(1, ConfigParser.ReadLbmAnnotatorPriority(defaultMethod));
        Assert.AreEqual(3, ConfigParser.ReadLbmAnnotatorPriority(configuredMethod));
    }

    [TestMethod]
    public void ReadDetailedAlignmentProvenance_DefaultsToFalseAndAcceptsBothAliases()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var shortAlias = directory.CreateFile("short.txt", "Detailed alignment provenance: True\n");
        var longAlias = directory.CreateFile("long.txt", "Export detailed alignment provenance: true\n");
        var explicitlyOff = directory.CreateFile("off.txt", "Detailed alignment provenance: FALSE\n");
        // A value that is neither true nor false must leave the default alone rather than throw: the
        // audit sidecar is optional, so an unparseable setting means "not requested", not "fail the run".
        var nonBoolean = directory.CreateFile("bad.txt", "Detailed alignment provenance: yes please\n");

        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(defaultMethod));
        Assert.IsTrue(ConfigParser.ReadDetailedAlignmentProvenance(shortAlias));
        Assert.IsTrue(ConfigParser.ReadDetailedAlignmentProvenance(longAlias));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(explicitlyOff));
        Assert.IsFalse(ConfigParser.ReadDetailedAlignmentProvenance(nonBoolean));
    }

    [TestMethod]
    public void ReadAnnotationCandidateExport_DefaultsToFalseAndAcceptsBothAliases()
    {
        using var directory = new TemporaryDirectory();
        var defaultMethod = directory.CreateFile("default.txt", "Ion mode: Negative\n");
        var shortAlias = directory.CreateFile("short.txt", "Annotation candidates: True\n");
        var longAlias = directory.CreateFile("long.txt", "Export annotation candidates: true\n");
        var explicitlyOff = directory.CreateFile("off.txt", "Annotation candidates: FALSE\n");
        var nonBoolean = directory.CreateFile("bad.txt", "Annotation candidates: all of them\n");

        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(defaultMethod));
        Assert.IsTrue(ConfigParser.ReadAnnotationCandidateExport(shortAlias));
        Assert.IsTrue(ConfigParser.ReadAnnotationCandidateExport(longAlias));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(explicitlyOff));
        Assert.IsFalse(ConfigParser.ReadAnnotationCandidateExport(nonBoolean));
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
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
