using CompMs.App.MsdialConsole.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

/// <summary>
/// The GC-MS alignment's EI similarity threshold and weight are "Spectrum similarity tolerance /
/// factor for alignment". GcmsPeakJoiner reads them; it reads no MS1 alignment setting, so the
/// GC-MS template writes the spectrum similarity keys and not "MS1 tolerance for alignment".
/// </summary>
[TestClass]
public sealed class GcmsAlignmentSpectrumSimilarityMethodKeyTests
{
    [TestMethod]
    public void TheGcmsTemplateWritesTheSpectrumSimilaritySettingsAndNoMs1AlignmentSetting()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Gcms);

        StringAssert.Contains(template, "\nSpectrum similarity tolerance for alignment: 0.8\n");
        StringAssert.Contains(template, "\nSpectrum similarity factor for alignment: 0.5\n");
        Assert.IsFalse(template.Contains("MS1 tolerance for alignment"), template);
        Assert.IsFalse(template.Contains("MS1 factor for alignment"), template);
    }

    [TestMethod]
    public void AGcmsMethodFileSetsTheSettingsTheJoinerReads()
    {
        var template = MethodFileTemplate.Create(MethodFileMode.Gcms)
            .Replace("\nSpectrum similarity tolerance for alignment: 0.8\n", "\nSpectrum similarity tolerance for alignment: 0.65\n")
            .Replace("\nSpectrum similarity factor for alignment: 0.5\n", "\nSpectrum similarity factor for alignment: 0.3\n");
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", template);

        var parameter = (MsdialGcmsParameter)ConfigParser.ReadForGcms(methodFile);

        Assert.AreEqual(0.65f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance);
        Assert.AreEqual(0.3f, parameter.AlignmentBaseParam.SpectrumSimilarityAlignmentFactor);
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
