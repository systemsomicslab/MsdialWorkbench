using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Enum;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Utility.Tests;

[TestClass()]
public class RawFileFormatClassifierTests
{
    private string _folder = string.Empty;

    [TestInitialize()]
    public void Setup() {
        _folder = Path.Combine(Path.GetTempPath(), $"msdial-rawfileformat-{Guid.NewGuid():N}");
        Directory.CreateDirectory(_folder);
    }

    [TestCleanup()]
    public void Cleanup() {
        Directory.Delete(_folder, recursive: true);
    }

    /// <summary>
    /// These formats are told by the extension alone, in any letter case, so the file need not be there.
    /// </summary>
    [DataTestMethod()]
    [DataRow("sample.abf", RawFileFormat.Abf)]
    [DataRow("sample.ABF", RawFileFormat.Abf)]
    [DataRow("sample.ibf", RawFileFormat.Ibf)]
    [DataRow("sample.IBF", RawFileFormat.Ibf)]
    [DataRow("sample.wiff", RawFileFormat.SciexWiff)]
    [DataRow("sample.WIFF", RawFileFormat.SciexWiff)]
    [DataRow("sample.wiff2", RawFileFormat.SciexWiff2)]
    [DataRow("sample.WIFF2", RawFileFormat.SciexWiff2)]
    [DataRow("sample.lcd", RawFileFormat.ShimadzuLcd)]
    [DataRow("sample.LCD", RawFileFormat.ShimadzuLcd)]
    [DataRow("sample.qgd", RawFileFormat.ShimadzuQgd)]
    [DataRow("sample.QGD", RawFileFormat.ShimadzuQgd)]
    [DataRow("sample.mzML", RawFileFormat.MzML)]
    [DataRow("sample.mzml", RawFileFormat.MzML)]
    [DataRow("sample.MZML", RawFileFormat.MzML)]
    [DataRow("sample.imzML", RawFileFormat.ImzML)]
    [DataRow("sample.IMZML", RawFileFormat.ImzML)]
    [DataRow("sample.cdf", RawFileFormat.NetCdf)]
    [DataRow("sample.CDF", RawFileFormat.NetCdf)]
    [DataRow("sample.lrp", RawFileFormat.KanomaxLrp)]
    [DataRow("sample.LRP", RawFileFormat.KanomaxLrp)]
    [DataRow("sample.hmd", RawFileFormat.HiveHmd)]
    [DataRow("sample.HMD", RawFileFormat.HiveHmd)]
    [DataRow("sample.mzb", RawFileFormat.HiveMzb)]
    [DataRow("sample.MZB", RawFileFormat.HiveMzb)]
    public void AFileIsToldByItsExtension(string name, RawFileFormat expected) {
        var missingFolder = Path.Combine(_folder, "not-there");

        Assert.AreEqual(expected, RawFileFormatClassifier.Classify(Path.Combine(missingFolder, name)));
        Assert.IsFalse(Directory.Exists(missingFolder), "classifying must not create anything");
    }

    [DataTestMethod()]
    [DataRow("sample.iabf")]
    [DataRow("sample.ibd")]
    [DataRow("sample.mzXML")]
    [DataRow("sample.mzData")]
    [DataRow("sample.txt")]
    [DataRow("sample.raw.zip")]
    [DataRow("sample")]
    public void AnyOtherExtensionIsUnknown(string name) {
        File.WriteAllBytes(Path.Combine(_folder, name), new byte[0]);

        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(Path.Combine(_folder, name)));
    }

    [TestMethod()]
    public void NoPathIsUnknown() {
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(null));
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(string.Empty));
    }

    /// <summary>
    /// A Waters .raw is a folder and a Thermo .raw a file; a .raw that is not there is neither.
    /// </summary>
    [DataTestMethod()]
    [DataRow("sample.raw", "file", RawFileFormat.ThermoRaw)]
    [DataRow("sample.RAW", "file", RawFileFormat.ThermoRaw)]
    [DataRow("sample.raw", "folder", RawFileFormat.WatersRaw)]
    [DataRow("sample.RAW", "folder", RawFileFormat.WatersRaw)]
    [DataRow("sample.raw", "missing", RawFileFormat.Unknown)]
    public void ADotRawIsToldByWhatItIs(string name, string kind, RawFileFormat expected) {
        var path = Path.Combine(_folder, name);
        if (kind == "file") {
            File.WriteAllBytes(path, new byte[0]);
        }
        else if (kind == "folder") {
            Directory.CreateDirectory(path);
        }

        Assert.AreEqual(expected, RawFileFormatClassifier.Classify(path));
    }

    /// <summary>
    /// Agilent and Bruker both write a .d folder. It is checked in the order RawDataHandler reads it: AcqData first,
    /// then analysis.tdf, analysis.tsf and analysis.baf.
    /// </summary>
    [DataTestMethod()]
    // content: entries separated by "|", a folder name ending in "/"; "" for an empty .d folder, null for no folder at all.
    [DataRow("AcqData/", RawFileFormat.AgilentMassHunterD)]
    [DataRow("analysis.tdf", RawFileFormat.BrukerTdf)]
    [DataRow("analysis.tsf", RawFileFormat.BrukerTsf)]
    [DataRow("analysis.baf", RawFileFormat.BrukerBaf)]
    [DataRow("AcqData/|analysis.tdf", RawFileFormat.AgilentMassHunterD)]
    [DataRow("AcqData/|analysis.baf", RawFileFormat.AgilentMassHunterD)]
    [DataRow("analysis.tdf|analysis.tsf|analysis.baf", RawFileFormat.BrukerTdf)]
    [DataRow("analysis.tsf|analysis.baf", RawFileFormat.BrukerTsf)]
    [DataRow("AcqData", RawFileFormat.Unknown)]
    [DataRow("analysis.tdf/", RawFileFormat.Unknown)]
    [DataRow("", RawFileFormat.Unknown)]
    [DataRow(null, RawFileFormat.Unknown)]
    public void ADotDIsToldByWhatItHolds(string content, RawFileFormat expected) {
        foreach (var name in new[] { "sample.d", "SAMPLE2.D" }) {
            var folder = Path.Combine(_folder, name);
            if (content is not null) {
                Directory.CreateDirectory(folder);
                foreach (var entry in content.Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)) {
                    if (entry.EndsWith("/")) {
                        Directory.CreateDirectory(Path.Combine(folder, entry.TrimEnd('/')));
                    }
                    else {
                        File.WriteAllBytes(Path.Combine(folder, entry), new byte[0]);
                    }
                }
            }

            Assert.AreEqual(expected, RawFileFormatClassifier.Classify(folder), name);
        }
    }

    [DataTestMethod()]
    [DataRow("a\0b.raw")]
    [DataRow("a\0b.d")]
    [DataRow("a<b>c|d.d")]
    [DataRow("a\"b.raw")]
    public void APathThatCannotBeCheckedIsUnknown(string name) {
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(name));
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(Path.Combine(_folder, "x") + name));
    }

    [TestMethod()]
    public void AVeryLongPathIsUnknownRatherThanAnError() {
        var path = Path.Combine(_folder, new string('x', 40000) + ".raw");

        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Classify(path));
    }

    [TestMethod()]
    public void RecordIfUnknownRecordsWhatTheRawDataTell() {
        var path = Path.Combine(_folder, "sample.raw");
        File.WriteAllBytes(path, new byte[0]);
        var file = new AnalysisFileBean { AnalysisFilePath = path, };

        Assert.IsTrue(RawFileFormatClassifier.RecordIfUnknown(file));
        Assert.AreEqual(RawFileFormat.ThermoRaw, file.RawFileFormat);
    }

    [TestMethod()]
    public void RecordIfUnknownNeverReplacesARecordedFormat() {
        var path = Path.Combine(_folder, "sample.raw");
        File.WriteAllBytes(path, new byte[0]);
        var file = new AnalysisFileBean { AnalysisFilePath = path, RawFileFormat = RawFileFormat.WatersRaw, };

        Assert.IsFalse(RawFileFormatClassifier.RecordIfUnknown(file));
        Assert.AreEqual(RawFileFormat.WatersRaw, file.RawFileFormat);
    }

    [TestMethod()]
    public void RecordIfUnknownLeavesAFileThatCannotBeToldUnknown() {
        var file = new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone.raw"), };

        Assert.IsFalse(RawFileFormatClassifier.RecordIfUnknown(file));
        Assert.AreEqual(RawFileFormat.Unknown, file.RawFileFormat);
        Assert.IsFalse(RawFileFormatClassifier.RecordIfUnknown(null));
    }

    [DataTestMethod()]
    [DataRow(RawFileFormat.ThermoRaw, ".raw")]
    [DataRow(RawFileFormat.WatersRaw, ".RAW")]
    [DataRow(RawFileFormat.AgilentMassHunterD, ".d")]
    [DataRow(RawFileFormat.BrukerTdf, ".D")]
    [DataRow(RawFileFormat.BrukerTsf, ".d")]
    [DataRow(RawFileFormat.BrukerBaf, ".d")]
    [DataRow(RawFileFormat.SciexWiff, ".wiff")]
    [DataRow(RawFileFormat.SciexWiff2, ".WIFF2")]
    [DataRow(RawFileFormat.ShimadzuLcd, ".lcd")]
    [DataRow(RawFileFormat.ShimadzuQgd, ".QGD")]
    [DataRow(RawFileFormat.MzML, ".mzML")]
    [DataRow(RawFileFormat.ImzML, ".imzML")]
    [DataRow(RawFileFormat.NetCdf, ".CDF")]
    [DataRow(RawFileFormat.Abf, ".abf")]
    [DataRow(RawFileFormat.Ibf, ".ibf")]
    [DataRow(RawFileFormat.KanomaxLrp, ".lrp")]
    [DataRow(RawFileFormat.HiveHmd, ".hmd")]
    [DataRow(RawFileFormat.HiveMzb, ".mzb")]
    public void AFormatIsConsistentOnlyWithItsOwnExtension(RawFileFormat format, string extension) {
        Assert.IsTrue(RawFileFormatClassifier.IsConsistentWith(format, extension));
        Assert.IsTrue(RawFileFormatClassifier.IsConsistentWith(format, extension.ToUpperInvariant()));
        Assert.IsTrue(RawFileFormatClassifier.IsConsistentWith(format, extension.ToLowerInvariant()));
        foreach (var other in new[] { ".raw", ".d", ".wiff", ".wiff2", ".mzml", ".abf", ".txt", "", null }) {
            if (!string.Equals(other, extension, StringComparison.OrdinalIgnoreCase)) {
                Assert.IsFalse(RawFileFormatClassifier.IsConsistentWith(format, other), $"{format} with {other ?? "null"}");
            }
        }
    }

    [TestMethod()]
    public void UnknownIsConsistentWithNothing() {
        foreach (var extension in new[] { ".raw", ".d", ".mzml", ".txt", "", null }) {
            Assert.IsFalse(RawFileFormatClassifier.IsConsistentWith(RawFileFormat.Unknown, extension), extension ?? "null");
        }
    }

    /// <summary>
    /// A format added to the enum without an extension would never be trusted by the exporter.
    /// </summary>
    [TestMethod()]
    public void EveryKnownFormatHasAnExtension() {
        var extensions = new[] { ".raw", ".d", ".wiff", ".wiff2", ".lcd", ".qgd", ".mzml", ".imzml", ".cdf", ".abf", ".ibf", ".lrp", ".hmd", ".mzb" };
        foreach (var format in System.Enum.GetValues(typeof(RawFileFormat)).Cast<RawFileFormat>().Where(f => f != RawFileFormat.Unknown)) {
            Assert.IsTrue(extensions.Any(extension => RawFileFormatClassifier.IsConsistentWith(format, extension)), format.ToString());
        }
    }

    [TestMethod()]
    public void ARecordedFormatDescribesAFileWhoseRawDataAreGone() {
        var file = new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone.d"), RawFileFormat = RawFileFormat.BrukerTdf, };

        Assert.AreEqual(RawFileFormat.BrukerTdf, RawFileFormatClassifier.Resolve(file));
    }

    [TestMethod()]
    public void ARecordedFormatThatDoesNotFitThePathGivesWayToTheDisk() {
        var folder = Path.Combine(_folder, "sample.d");
        Directory.CreateDirectory(folder);
        File.WriteAllBytes(Path.Combine(folder, "analysis.tdf"), new byte[0]);
        var present = new AnalysisFileBean { AnalysisFilePath = folder, RawFileFormat = RawFileFormat.ThermoRaw, };
        var gone = new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone.d"), RawFileFormat = RawFileFormat.ThermoRaw, };

        Assert.AreEqual(RawFileFormat.BrukerTdf, RawFileFormatClassifier.Resolve(present));
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Resolve(gone));
    }

    [TestMethod()]
    public void NothingRecordedIsToldFromTheDisk() {
        var path = Path.Combine(_folder, "sample.raw");
        Directory.CreateDirectory(path);

        Assert.AreEqual(RawFileFormat.WatersRaw, RawFileFormatClassifier.Resolve(new AnalysisFileBean { AnalysisFilePath = path, }));
        Assert.AreEqual(RawFileFormat.Unknown, RawFileFormatClassifier.Resolve(new AnalysisFileBean { AnalysisFilePath = Path.Combine(_folder, "gone.raw"), }));
    }
}
