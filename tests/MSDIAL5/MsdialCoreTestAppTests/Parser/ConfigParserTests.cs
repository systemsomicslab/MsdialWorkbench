using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class ConfigParserTests
{
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
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }

        public void Dispose()
        {
            Directory.Delete(Path, recursive: true);
        }
    }
}
