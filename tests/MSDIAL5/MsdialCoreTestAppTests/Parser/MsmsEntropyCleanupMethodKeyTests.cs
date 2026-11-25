using CompMs.App.MsdialConsole.Parser;
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
/// The MS/MS entropy cleanup's method-file keys: read by the LC-MS reader, refused when the value is unusable, unknown
/// to every other mode, written by the LC-MS template with the cleanup off, and the same keys the parameter text writes.
/// </summary>
[TestClass]
public sealed class MsmsEntropyCleanupMethodKeyTests
{
    private static readonly string[] Keys = {
        "MS/MS entropy cleanup",
        "MS/MS maximum spectral entropy",
        "MS/MS entropy cleanup protected percent of base peak",
    };

    [TestMethod]
    public void ReadForLcms_AppliesEveryKey()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            MS/MS entropy cleanup: True
            MS/MS maximum spectral entropy: 3.5
            MS/MS entropy cleanup protected percent of base peak: 20
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.IsTrue(parameter.ChromDecBaseParam.IsDoMsmsEntropyCleanup);
        Assert.AreEqual(3.5, parameter.ChromDecBaseParam.MsmsMaxSpectralEntropy);
        Assert.AreEqual(20d, parameter.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak);
        CollectionAssert.AreEqual(Keys, RecordedKeys(methodFile, "applied"));
    }

    [TestMethod]
    public void ReadForLcms_WithoutTheKeysTheCleanupIsOff()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", "Ion mode: Positive\n");

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.IsFalse(parameter.ChromDecBaseParam.IsDoMsmsEntropyCleanup);
        Assert.AreEqual(4.5, parameter.ChromDecBaseParam.MsmsMaxSpectralEntropy);
        Assert.AreEqual(0d, parameter.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak);
    }

    /// <summary>A ceiling that is not finite and positive, or a percentage outside 0 to 100, is refused, not applied.</summary>
    [TestMethod]
    public void ReadForLcms_RefusesAnUnusableValue()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile(
            "method.txt",
            """
            MS/MS entropy cleanup: yes
            MS/MS maximum spectral entropy: 0
            MS/MS maximum spectral entropy: -1
            MS/MS maximum spectral entropy: NaN
            MS/MS maximum spectral entropy: Infinity
            MS/MS entropy cleanup protected percent of base peak: 150
            """);

        var parameter = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.AreEqual(6, RecordedKeys(methodFile, "unusable").Count);
        Assert.IsFalse(parameter.ChromDecBaseParam.IsDoMsmsEntropyCleanup);
        Assert.AreEqual(4.5, parameter.ChromDecBaseParam.MsmsMaxSpectralEntropy, "the run used the default");
        Assert.AreEqual(0d, parameter.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak);
    }

    /// <summary>Only LC-MS deconvolution runs the cleanup, so every other mode reports its keys as having no effect.</summary>
    [TestMethod]
    public void OtherModes_ReportTheKeysAsUnrecognised()
    {
        var readers = new (string Mode, Action<string> Read)[] {
            ("gcms", path => ConfigParser.ReadForGcms(path)),
            ("dims", path => ConfigParser.ReadForDimsParameter(path)),
            ("imms", path => ConfigParser.ReadForImmsParameter(path)),
            ("lcimms", path => ConfigParser.ReadForLcImMsParameter(path)),
        };
        using var directory = new TemporaryDirectory();
        foreach (var (mode, read) in readers) {
            var methodFile = directory.CreateFile($"{mode}.txt", "MS/MS entropy cleanup: True\nMS/MS maximum spectral entropy: 4\n");

            read(methodFile);

            CollectionAssert.AreEqual(new[] { "MS/MS entropy cleanup", "MS/MS maximum spectral entropy" }, RecordedKeys(methodFile, "unrecognised"), mode);
        }
    }

    /// <summary>The section of the parameter text is method-file lines: copied into a method file they reproduce the settings.</summary>
    [TestMethod]
    public void TheParameterTextLinesReadBackAsTheSameSettings()
    {
        var written = new MsdialLcmsParameter();
        written.ChromDecBaseParam.IsDoMsmsEntropyCleanup = true;
        written.ChromDecBaseParam.MsmsMaxSpectralEntropy = 3.25;
        written.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak = 12.5;
        var section = written.ParametersAsText().SkipWhile(line => line != "# MS/MS entropy cleanup").Skip(1).TakeWhile(line => line.StartsWith("MS/MS")).ToList();
        CollectionAssert.AreEqual(new[] {
            "MS/MS entropy cleanup: True",
            "MS/MS maximum spectral entropy: 3.25",
            "MS/MS entropy cleanup protected percent of base peak: 12.5",
        }, section);

        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", string.Join("\n", section) + "\n");
        var read = ConfigParser.ReadForLcmsParameter(methodFile);

        Assert.IsTrue(read.ChromDecBaseParam.IsDoMsmsEntropyCleanup);
        Assert.AreEqual(3.25, read.ChromDecBaseParam.MsmsMaxSpectralEntropy);
        Assert.AreEqual(12.5, read.ChromDecBaseParam.MsmsEntropyCleanupProtectedPercentOfBasePeak);
        CollectionAssert.AreEqual(Keys, RecordedKeys(methodFile, "applied"));
    }

    [TestMethod]
    public void OnlyTheLcmsParameterTextHasTheSection()
    {
        Assert.IsTrue(new MsdialLcmsParameter().ParametersAsText().Contains("MS/MS entropy cleanup: False"));
        Assert.IsFalse(new MsdialLcImMsParameter().ParametersAsText().Contains("# MS/MS entropy cleanup"), "LC-IM-MS does not run the cleanup");
    }

    [TestMethod]
    public void TheLcmsTemplateWritesTheKeysWithTheCleanupOff()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Lcms);

        StringAssert.Contains(template, "MS/MS entropy cleanup: False\n");
        StringAssert.Contains(template, "MS/MS maximum spectral entropy: 4.5\n");
        StringAssert.Contains(template, "MS/MS entropy cleanup protected percent of base peak: 0\n");
        foreach (var mode in new[] { MethodFileMode.Gcms, MethodFileMode.Dims, MethodFileMode.Imms, MethodFileMode.Lcimms }) {
            Assert.IsFalse(MethodFileTemplate.Create(mode).Contains("MS/MS entropy cleanup"), mode.ToString());
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
