using CompMs.App.MsdialConsole.Parser;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.MsdialCore.Utility;
using CompMs.MsdialGcMsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace MsdialCoreTestAppTests.Parser;

[TestClass]
public sealed class MspRelativeAmplitudeCutoffTests
{
    // The MSP-based annotation section of the GC-MS Console demo method (gcmsparam_kovats.txt, 2020),
    // with the dot product keys as the Kovats bench method writes them.
    private const string KovatsMspSection = """
        Ionization: EI
        Machine category: GCMS
        Retention type: RI
        RI compound: Alkanes
        RI tolerance for MSP-based annotation: 20
        Mass range begin for MSP-based annotation: 85
        Mass range end for MSP-based annotation: 600
        Relative amplitude cutoff for MSP-based annotation: 0.1
        Absolute amplitude cutoff for MSP-based annotation: 5
        Weighted dot product cutoff for MSP-based annotation: 0.6
        Simple dot product cutoff for MSP-based annotation: 0.6
        Reverse dot product cutoff for MSP-based annotation: 0.8
        Matched peaks percentage cutoff for MSP-based annotation: 0.1
        Minimum spectrum match for MSP-based annotation: 3
        Total score cutoff for MSP-based annotation: 0.8
        MS1 tolerance for MSP-based annotation: 0.5
        MS2 tolerance for MSP-based annotation: 0.5
        Use retention information for MSP-based annotation scoring: False
        Use retention information for MSP-based annotation filtering: False
        """;

    [TestMethod]
    public void MspRelativeAmplitudeCutoffIsReadAsPercentOfTheBasePeakLikeTheGui()
    {
        // The GUI shows this field in percent and stores value / 100 (MsRefSearchParameterBaseViewModel).
        // The 2020 Console demo files write 0.1, which the 2020 code applied as relcutoff * 0.01.
        var parameter = new MsdialGcmsParameter();

        var outcome = ConfigParser.ReadCommonParameter(parameter, "Relative amplitude cutoff for MSP-based annotation", "0.1");

        Assert.IsTrue(outcome.IsApplied);
        Assert.AreEqual(0.001f, parameter.MspSearchParam.RelativeAmpCutoff, 1e-7f);
    }

    [TestMethod]
    public void APerfectEiSelfMatchPassesTheSpectrumGateUnderTheKovatsDemoMethod()
    {
        using var directory = new TemporaryDirectory();
        var methodFile = directory.CreateFile("method.txt", KovatsMspSection);
        var search = ConfigParser.ReadForGcms(methodFile).MspSearchParam;

        // A TMS-like EI spectrum: six ions above 10 % of the base peak and 28 library ions
        // between 1 % and 9 %, which together carry about a third of the m/z-weighted intensity.
        var peaks = new (double Mz, double Intensity)[] {
            (103, 60), (117, 45), (129, 70), (133, 30), (147, 400), (157, 25), (169, 40), (189, 55),
            (191, 250), (204, 500), (205, 85), (217, 999), (218, 90), (231, 35), (243, 30), (256, 20),
            (271, 45), (289, 30), (305, 150), (306, 40), (319, 120), (320, 35), (333, 25), (345, 20),
            (361, 60), (362, 25), (373, 30), (389, 15), (405, 20), (435, 25), (450, 20), (495, 15),
            (507, 20), (540, 10),
        };
        var reference = new MoleculeMsReference {
            Name = "synthetic TMS derivative",
            ChromXs = new ChromXs(1877.1, ChromXType.RI, ChromXUnit.None),
            Spectrum = peaks.Select(p => new SpectrumPeak(p.Mz, p.Intensity)).ToList(),
        };
        // The measured spectrum is the library spectrum itself, in detector counts.
        var measured = new MSScanProperty {
            ChromXs = new ChromXs(1878.7, ChromXType.RI, ChromXUnit.None),
            Spectrum = peaks.Select(p => new SpectrumPeak(p.Mz, p.Intensity * 7500)).ToList(),
        };

        // What CalculateMatchScore.CalculateMatches does for each reference in the window.
        var query = DataAccess.GetNormalizedMSScanProperty(measured, search);
        var result = MsScanMatching.CompareEIMSScanProperties(query, reference, search, isUseRetentionIndex: true, retentionMatchTolerance: 20);

        Assert.IsTrue(result.IsSpectrumMatch,
            $"sqW {result.SquaredWeightedDotProduct:F3} sqS {result.SquaredSimpleDotProduct:F3} sqR {result.SquaredReverseDotProduct:F3} "
            + $"matched% {result.MatchedPeaksPercentage:F3} n {result.MatchedPeaksCount} query peaks {query.Spectrum.Count}/{peaks.Length}");
    }

    private sealed class TemporaryDirectory : IDisposable
    {
        public TemporaryDirectory()
        {
            Path = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "MsdialCoreTestAppTests", Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(Path);
        }

        public string Path { get; }

        public string CreateFile(string name, string content = "")
        {
            var path = System.IO.Path.Combine(Path, name);
            File.WriteAllText(path, content, new UTF8Encoding(encoderShouldEmitUTF8Identifier: true));
            return path;
        }

        public void Dispose() => Directory.Delete(Path, recursive: true);
    }
}
