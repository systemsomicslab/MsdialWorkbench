using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

        Assert.IsTrue(result);
        Assert.AreEqual(7f, parameter.SampleMaxOverBlankAverage);
        Assert.AreEqual(7f, parameter.FoldChangeForBlankFiltering);
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
