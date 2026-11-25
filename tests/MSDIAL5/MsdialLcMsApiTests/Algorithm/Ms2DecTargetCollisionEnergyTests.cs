using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;

namespace CompMs.MsdialLcMsApi.Algorithm.Tests;

/// <summary>
/// A feature without a product-ion scan at the target collision energy is still seeded from the nearest scan of
/// another energy, as the GUI does. It used to print "Target CE cannot be found." once per feature (2,308 lines in one
/// Waters MSE run); the deconvolution of a file at an energy now prints one summary line with the counts.
/// </summary>
[TestClass]
public sealed class Ms2DecTargetCollisionEnergyTests
{
    [TestMethod]
    public void FeaturesWithoutTheTargetEnergyAreSummarisedInOneLinePerFileAndEnergy() {
        var provider = Provider();
        var features = new[] { Feature(0, ms2: 1), Feature(1, ms2: 1), Feature(2, ms2: 1), Feature(3, ms2: -1), };
        var ms2Dec = new Ms2Dec(30d, 30d);

        var output = CaptureConsole(() => {
            var results = ms2Dec.GetMS2DecResults(File("QC-AIF-01"), provider, features, Parameter(), Summary(), null!, null, CancellationToken.None, targetCE: 6d);
            Assert.AreEqual(features.Length, results.Count);
        });

        Assert.AreEqual(3, ms2Dec.FeaturesSeededFromAnotherEnergy);
        Assert.AreEqual(1, ms2Dec.FeaturesWithoutProductIonScan);
        var lines = Lines(output);
        Assert.AreEqual(1, lines.Length, output);
        Assert.AreEqual(
            "QC-AIF-01: Target CE 6 eV: 3 features without a scan at that energy; the nearest other-energy scan was used as seed; "
            + "1 feature without a product-ion scan at any energy; their spectrum is empty.",
            lines[0]);
        Assert.IsFalse(output.Contains("Target CE cannot be found."));
    }

    [TestMethod]
    public void FeaturesWithTheTargetEnergyPrintNothing() {
        var ms2Dec = new Ms2Dec(30d, 30d);

        var output = CaptureConsole(() => ms2Dec.GetMS2DecResults(File("QC-AIF-02"), Provider(), [Feature(0, ms2: 1), Feature(1, ms2: 1)], Parameter(), Summary(), null!, null, CancellationToken.None, targetCE: 30d));

        Assert.AreEqual(0, ms2Dec.FeaturesSeededFromAnotherEnergy);
        Assert.AreEqual(0, ms2Dec.FeaturesWithoutProductIonScan);
        Assert.AreEqual(0, Lines(output).Length, output);
    }

    [TestMethod]
    public void TheCountsStartAgainForEachFileAndEnergy() {
        var ms2Dec = new Ms2Dec(30d, 30d);
        CaptureConsole(() => ms2Dec.GetMS2DecResults(File("a"), Provider(), [Feature(0, ms2: 1), Feature(1, ms2: 1)], Parameter(), Summary(), null!, null, CancellationToken.None, targetCE: 6d));

        var output = CaptureConsole(() => ms2Dec.GetMS2DecResults(File("b"), Provider(), [Feature(0, ms2: 1)], Parameter(), Summary(), null!, null, CancellationToken.None, targetCE: 6d));

        Assert.AreEqual(1, ms2Dec.FeaturesSeededFromAnotherEnergy);
        Assert.AreEqual("b: Target CE 6 eV: 1 feature without a scan at that energy; the nearest other-energy scan was used as seed.", Lines(output).Single());
    }

    [TestMethod]
    public void TheSummaryUsesInvariantDigitGrouping() {
        Assert.AreEqual(
            "f: Target CE 30 eV: 1,234 features without a scan at that energy; the nearest other-energy scan was used as seed.",
            Ms2Dec.FormatTargetCEMissingSummary("f", 30d, 1234, 0));
        Assert.AreEqual(
            "Target CE 12.5 eV: 2 features without a product-ion scan at any energy; their spectrum is empty.",
            Ms2Dec.FormatTargetCEMissingSummary(null, 12.5d, 0, 2));
        Assert.IsNull(Ms2Dec.FormatTargetCEMissingSummary("f", 30d, 0, 0));
    }

    // Scan 0 is MS1; scan 1 is a 30 eV product-ion scan with no peaks, so Ms2Dec stops at the seed and
    // returns the default result without reading chromatograms.
    private static StandardDataProvider Provider() {
        return new StandardDataProvider(new[]
        {
            new RawSpectrum { Index = 0, MsLevel = 1, ScanStartTime = 1d, Spectrum = [], },
            new RawSpectrum { Index = 1, MsLevel = 2, ScanStartTime = 1d, CollisionEnergy = 30d, Spectrum = [], Precursor = new RawPrecursorIon { SelectedIonMz = 525d, }, },
        });
    }

    private static ChromatogramPeakFeature Feature(int id, int ms2) {
        return new ChromatogramPeakFeature
        {
            PeakID = id,
            MasterPeakID = id,
            MS1RawSpectrumIdTop = 0,
            MS2RawSpectrumID = ms2,
            MS2RawSpectrumID2CE = ms2 >= 0 ? new Dictionary<int, double> { [ms2] = 30d, } : new Dictionary<int, double>(),
        };
    }

    private static AnalysisFileBean File(string name) => new AnalysisFileBean { AnalysisFileName = name, AcquisitionType = AcquisitionType.AIF, };

    private static MsdialLcmsParameter Parameter() => new MsdialLcmsParameter { NumThreads = 2, };

    private static ChromatogramPeaksDataSummary Summary() => ChromatogramPeaksDataSummary.ConvertFromDto(new ChromatogramPeaksDataSummaryDto());

    private static string[] Lines(string output) => output.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);

    private static string CaptureConsole(Action action) {
        var original = Console.Out;
        using var writer = new StringWriter();
        Console.SetOut(writer);
        try {
            action();
        }
        finally {
            Console.SetOut(original);
        }
        return writer.ToString();
    }
}
