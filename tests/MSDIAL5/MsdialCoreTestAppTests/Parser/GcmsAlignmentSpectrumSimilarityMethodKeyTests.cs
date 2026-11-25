using CompMs.App.MsdialConsole.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// The GC-MS alignment's similarity threshold and EI weight are "Spectrum similarity tolerance /
/// factor for alignment". GcmsPeakJoiner reads them; it reads no MS1 alignment setting, so the
/// GC-MS template writes the spectrum similarity keys and not "MS1 tolerance for alignment".
/// </summary>
[TestClass]
public sealed class GcmsAlignmentSpectrumSimilarityMethodKeyTests
{
    private const string ToleranceLine = "\nSpectrum similarity tolerance for alignment: 0.5\n";
    private const string FactorLine = "\nSpectrum similarity factor for alignment: 0.5\n";

    [TestMethod]
    public void TheGcmsTemplateWritesTheSpectrumSimilaritySettingsAndNoMs1AlignmentSetting()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Gcms);

        // 0.5, the GC-MS default, not the 0.8 that the alignment parameter starts with elsewhere.
        StringAssert.Contains(template, ToleranceLine);
        StringAssert.Contains(template, FactorLine);
        Assert.IsFalse(template.Contains("MS1 tolerance for alignment"), template);
        Assert.IsFalse(template.Contains("MS1 factor for alignment"), template);
    }

    [TestMethod]
    public void AGcmsMethodFileSetsTheSettingsTheJoinerReads()
    {
        var template = ReplaceOnce(ReplaceOnce(MethodFileTemplate.Create(MethodFileMode.Gcms),
            ToleranceLine, "\nSpectrum similarity tolerance for alignment: 0.65\n"),
            FactorLine, "\nSpectrum similarity factor for alignment: 0.3\n");
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", template);

        var parameter = (MsdialGcmsParameter)ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(0.65f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
        Assert.AreEqual(0.3f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
    }

    [TestMethod]
    public void AGcmsMethodFileWithoutTheSpectrumSimilarityKeysAlignsWithTheGcmsDefaults()
    {
        // A method file with no spectrum similarity key and an "MS1 tolerance for alignment", here
        // 0.015 as in a method file written like an LC-MS one. The MS1 tolerance is an m/z tolerance
        // by name and is not read as the similarity threshold.
        var template = ReplaceOnce(ReplaceOnce(MethodFileTemplate.Create(MethodFileMode.Gcms),
            ToleranceLine, "\nMS1 tolerance for alignment: 0.015\n"),
            FactorLine, "\n");
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", template);

        var parameter = (MsdialGcmsParameter)ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(0.5f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
        Assert.AreEqual(0.5f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
        Assert.AreEqual(0.015f, parameter.AlignmentBaseParam.Ms1AlignmentTolerance);
    }

    private static string ReplaceOnce(string text, string oldValue, string newValue)
    {
        var index = text.IndexOf(oldValue, StringComparison.Ordinal);
        Assert.IsTrue(index >= 0, $"'{oldValue.Trim()}' is not in the template");
        return text.Substring(0, index) + newValue + text.Substring(index + oldValue.Length);
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
