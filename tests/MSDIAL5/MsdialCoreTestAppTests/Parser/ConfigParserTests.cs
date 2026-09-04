using CompMs.App.MsdialConsole.Parser;
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
