using CompMs.App.MsdialConsole.Process;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.Algorithm;
using CompMs.MsdialCore.Algorithm.Annotation;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace MsdialCoreTestAppTests.Process;

/// <summary>
/// The Console writes each file's .mdpeak and .mdmsp before alignment. They used to read through
/// MSDecLoader(DeconvolutionFilePath, DeconvolutionFilePathList), which opens the unsuffixed .dcl whenever it
/// exists. A multi-energy AIF run writes none, so an unsuffixed file beside it is left over from an earlier run
/// under the same name: with fewer peaks the export stopped with ArgumentOutOfRangeException, and with as many it
/// silently exported the earlier run's spectra. The export now takes the same rule as the alignment's reader:
/// each peak at the energy of its representative annotation when that is an MS/MS reference-spectrum match, and
/// otherwise at the energy whose spectrum has the most product ions (the lowest such energy on a tie).
/// </summary>
[TestClass]
public sealed class LcmsPeakFileExportTests
{
    private const int PeakCount = 3;

    private string _scratch = null!;

    [TestInitialize]
    public void Initialize() {
        _scratch = Path.Combine(Path.GetTempPath(), $"msdial-peak-export-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_scratch);
    }

    [TestCleanup]
    public void Cleanup() {
        if (Directory.Exists(_scratch)) {
            Directory.Delete(_scratch, recursive: true);
        }
    }

    [TestMethod]
    public void EachPeakIsExportedAtItsAnnotationEnergyOrElseAtTheEnergyWithTheMostProductIons() {
        // Ions at 10 / 20 eV: peak 0 has 1 / 3, peak 1 has 3 / 1, peak 2 has 2 / 2.
        var file = MultiEnergyFile("aif", (energy, peak) => (energy, peak) switch
        {
            (10d, 0) => 1, (20d, 0) => 3,
            (10d, 1) => 3, (20d, 1) => 1,
            _ => 2,
        }, 10d, 20d);

        Export(file, annotationEnergies: new double?[] { 10d, null, null, });

        var expected = new List<double> { Marker(10d, 0), Marker(10d, 1), Marker(10d, 2), };
        CollectionAssert.AreEqual(expected, MdpeakMarkers(file), "0: annotated at 10 eV; 1: 10 eV has more ions; 2: a tie");
        CollectionAssert.AreEqual(expected, MdmspMarkers(file));

        Export(file, annotationEnergies: new double?[] { null, 20d, 30d, });

        expected = new List<double> { Marker(20d, 0), Marker(20d, 1), Marker(10d, 2), };
        CollectionAssert.AreEqual(expected, MdpeakMarkers(file), "0: 20 eV has more ions; 1: annotated at 20 eV; 2: no file at 30 eV, a tie");
        CollectionAssert.AreEqual(expected, MdmspMarkers(file));
    }

    [TestMethod]
    public void APeakWhoseRepresentativeIsNoMsMsReferenceMatchIsExportedAtTheEnergyWithTheMostProductIons() {
        // Peak 0 has 1 ion at 10 eV and 3 at 20 eV; every representative below sits at 10 eV.
        var file = MultiEnergyFile("aif", (energy, peak) => (energy, peak) switch
        {
            (10d, 0) => 1, (20d, 0) => 3,
            _ => 2,
        }, 10d, 20d);

        foreach (var (label, result) in RepresentativeDeconvolutionReaderTests.NotMsMsReferenceMatches(10d)) {
            Export(file, representatives: new[] { result, null, null, });

            Assert.AreEqual(Marker(20d, 0), MdpeakMarkers(file)[0], label);
            Assert.AreEqual(Marker(20d, 0), MdmspMarkers(file)[0], label);
        }

        Export(file, representatives: new[] { RepresentativeDeconvolutionReaderTests.MsMsReferenceMatch(10d), null, null, });

        Assert.AreEqual(Marker(10d, 0), MdpeakMarkers(file)[0], "an MS/MS reference match keeps its energy");
    }

    [TestMethod]
    public void EnergyFilesWithDifferentPeakCountsAreRefused() {
        var file = MultiEnergyFile("aif", 10d, 20d);
        MsdecResultsWriter.Write(file.DeconvolutionFilePathList[1], new List<MSDecResult> { Result(20d, 0) });

        var ex = Assert.ThrowsException<InvalidDataException>(() => RepresentativeDeconvolutionReader.LoadPerFileResults(file, []));

        StringAssert.Contains(ex.Message, "aif");
    }

    [TestMethod]
    public void AMultiEnergyFileIsExportedFromItsLowestEnergyAndNotFromAStaleShorterUnsuffixedFile() {
        var file = MultiEnergyFile("aif", 20d, 10d);
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, new List<MSDecResult> { Result(-1d, 0) });

        Export(file);

        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(10d, i)).ToList(), MdpeakMarkers(file));
        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(10d, i)).ToList(), MdmspMarkers(file));
    }

    [TestMethod]
    public void AStaleUnsuffixedFileWithAsManyPeaksIsNotExportedEither() {
        // E.g. a SWATH run of the same raw file at the same threshold, in a colliding minute.
        var file = MultiEnergyFile("aif", 10d, 20d);
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, Results(-1d));

        Export(file);

        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(10d, i)).ToList(), MdpeakMarkers(file), "the .mdpeak carried the stale run's spectra");
        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(10d, i)).ToList(), MdmspMarkers(file), "the .mdmsp carried the stale run's spectra");
    }

    [TestMethod]
    public void ASingleDeconvolutionFileIsExportedFromItsUnsuffixedFile() {
        var file = NewFile("dda");
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, Results(-1d));

        Export(file);

        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(-1d, i)).ToList(), MdpeakMarkers(file));
        CollectionAssert.AreEqual(Enumerable.Range(0, PeakCount).Select(i => Marker(-1d, i)).ToList(), MdmspMarkers(file));
    }

    [TestMethod]
    public void AMissingFirstEnergyFileIsReportedByName() {
        var file = MultiEnergyFile("aif", 10d, 20d);
        var first = file.DeconvolutionFilePathList[0];
        File.Delete(first);
        MsdecResultsWriter.Write(file.DeconvolutionFilePath, Results(-1d));

        var ex = Assert.ThrowsException<FileNotFoundException>(() => RepresentativeDeconvolutionReader.LoadPerFileResults(file, []));

        Assert.AreEqual(first, ex.FileName);
        StringAssert.Contains(ex.Message, "aif");
    }

    private void Export(AnalysisFileBean file, double?[]? annotationEnergies = null) {
        Export(file, annotationEnergies?.Select(e => e is double ce ? RepresentativeDeconvolutionReaderTests.MsMsReferenceMatch(ce) : null).ToArray());
    }

    private void Export(AnalysisFileBean file, MsScanMatchResult?[]? representatives) {
        var peaks = new ChromatogramPeakFeatureCollection(Enumerable.Range(0, PeakCount).Select(i => {
            var peak = new ChromatogramPeakFeature {
                MasterPeakID = i,
                PeakID = i,
                MSDecResultIdUsed = i,
            };
            peak.SetAdductType(AdductIon.GetAdductIon("[M+H]+"));
            if (representatives?[i] is MsScanMatchResult result) {
                peak.MatchResults.AddResult(result);
            }
            return peak;
        }).ToList());
        LcmsProcess.ExportPeakFile(
            file,
            peaks,
            _scratch,
            new AnalysisCSVExporterFactory("\t"),
            new NullProviderFactory(),
            new MarkerAccessor(),
            new NullRefer(),
            new MsdialLcmsParameter());
    }

    private List<double> MdpeakMarkers(AnalysisFileBean file) {
        return File.ReadAllLines(Path.Combine(_scratch, file.AnalysisFileName + ".mdpeak"))
            .Skip(1)
            .Select(line => double.Parse(line, CultureInfo.InvariantCulture))
            .ToList();
    }

    // Each spectrum holds one peak whose mass is the marker; MSP peak lines are "<mass>\t<intensity>".
    private List<double> MdmspMarkers(AnalysisFileBean file) {
        return File.ReadAllLines(Path.Combine(_scratch, file.AnalysisFileName + ".mdmsp"))
            .Where(line => line.EndsWith("\t" + MarkerIntensity.ToString(CultureInfo.InvariantCulture)))
            .Select(line => double.Parse(line.Split('\t')[0], CultureInfo.InvariantCulture))
            .ToList();
    }

    private AnalysisFileBean NewFile(string name) {
        return new AnalysisFileBean {
            AnalysisFileId = 0,
            AnalysisFileName = name,
            AnalysisFilePath = Path.Combine(_scratch, name + ".mzML"),
            DeconvolutionFilePath = Path.Combine(_scratch, name + "_202610711.dcl"),
        };
    }

    // Writes the per-energy files the way FileProcess does for an AIF file with more than one energy,
    // and lists them in ascending energy as FileProcess.SaveToFileAsync does.
    private AnalysisFileBean MultiEnergyFile(string name, params double[] energies) => MultiEnergyFile(name, (_, _) => 1, energies);

    private AnalysisFileBean MultiEnergyFile(string name, Func<double, int, int> ions, params double[] energies) {
        var file = NewFile(name);
        foreach (var energy in energies.OrderBy(e => e)) {
            var collection = new MSDecResultCollection(Enumerable.Range(0, PeakCount).Select(i => Result(energy, i, ions(energy, i))).ToList(), energy);
            var path = collection.GetDeconvolutionFilePathWithCE(file);
            MsdecResultsWriter.Write(path, collection.MSDecResults);
            file.DeconvolutionFilePathList.Add(path);
        }
        return file;
    }

    private const double MarkerIntensity = 777d;

    private static List<MSDecResult> Results(double energy) {
        return Enumerable.Range(0, PeakCount).Select(i => Result(energy, i)).ToList();
    }

    // The marker peak, and ions - 1 further peaks of intensity 1 that the .mdmsp reader skips.
    private static MSDecResult Result(double energy, int peak, int ions = 1) {
        var marker = Marker(energy, peak);
        var spectrum = new List<SpectrumPeak> { new SpectrumPeak { Mass = marker, Intensity = MarkerIntensity, } };
        spectrum.AddRange(Enumerable.Range(1, Math.Max(0, ions - 1)).Select(k => new SpectrumPeak { Mass = marker + k, Intensity = 1d, }));
        return new MSDecResult {
            ScanID = peak,
            PrecursorMz = marker,
            Spectrum = spectrum,
        };
    }

    // Which file and which peak a spectrum came from; negative for the stale unsuffixed file.
    private static double Marker(double energy, int peak) => energy * 1000d + (energy < 0 ? -peak : peak);

    private sealed class MarkerAccessor : IAnalysisMetadataAccessor
    {
        public string[] GetHeaders() => ["Marker"];

        public Dictionary<string, string> GetContent(ChromatogramPeakFeature feature, MSDecResult msdec, IDataProvider provider, AnalysisFileBean analysisFile, ExportStyle exportStyle) {
            return new Dictionary<string, string> { ["Marker"] = msdec.PrecursorMz.ToString(CultureInfo.InvariantCulture), };
        }
    }

    private sealed class NullProviderFactory : IDataProviderFactory<AnalysisFileBean>
    {
        public IDataProvider Create(AnalysisFileBean source) => null!;
    }

    private sealed class NullRefer : IMatchResultRefer<MoleculeMsReference?, MsScanMatchResult?>
    {
        public string Key => string.Empty;

        public MoleculeMsReference? Refer(MsScanMatchResult? result) => null;
    }
}
