using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCoreTests.Export;

[TestClass]
public class AlignmentProvenanceExporterTests
{
    // Assertions here compare WHOLE lines, not substrings. Substring assertions are what let three
    // overload disagreements ship unnoticed: they stopped before the mz column, and they could not see a
    // ragged field count at all.
    private const string ExpectedHeader =
        "alignment_master_id\talignment_local_id\tparent_alignment_id\tfile_id\tfile_name\t" +
        "is_representative\thas_source_peak\tpeak_origin\tsource_master_peak_id\tsource_peak_id\t" +
        "source_parent_peak_id\tms1_raw_spectrum_id\tms1_raw_spectrum_id_top\tms2_raw_spectrum_id\t" +
        "ms2_raw_spectrum_ids\tms2_collision_energies\trt_min\tmz\theight\tarea_above_zero\t" +
        "area_above_baseline";

    private static string[] ExportLines(AlignmentSpotProperty spot)
    {
        using var stream = new MemoryStream();
        new AlignmentProvenanceExporter().Export(stream, [spot]);
        return Lines(stream);
    }

    private static string[] ExportLines(AlignmentSpotProperty spot, AlignmentLightPeakStore store)
    {
        using var stream = new MemoryStream();
        new AlignmentProvenanceExporter().Export(stream, [spot], store);
        return Lines(stream);
    }

    private static string[] Lines(MemoryStream stream)
        => Encoding.UTF8.GetString(stream.ToArray())
            .Split('\n')
            .Select(line => line.TrimEnd('\r'))
            .Where(line => line.Length > 0)
            .ToArray();

    private static AlignmentSpotProperty SpotWith(params AlignmentChromPeakFeature[] peaks)
        => new AlignmentSpotProperty {
            MasterAlignmentID = 12,
            AlignmentID = 10,
            ParentAlignmentID = -1,
            RepresentativeFileID = 3,
            AlignedPeakProperties = [.. peaks],
        };

    [TestMethod]
    public void ExportWritesStableFeatureLinksAndRawSpectrumIds()
    {
        var lines = ExportLines(SpotWith(new AlignmentChromPeakFeature {
            FileID = 3,
            FileName = "sample-a",
            MasterPeakID = 42,
            PeakID = 40,
            ParentPeakID = -1,
            MS1RawSpectrumID = 100,
            MS1RawSpectrumIdTop = 101,
            MS2RawSpectrumID = 202,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [202] = 20d, [203] = 40d },
            ChromXsTop = new ChromXs(2.5),
            Mass = 300.125,
            PeakHeightTop = 1234d,
            PeakAreaAboveZero = 5678d,
            PeakAreaAboveBaseline = 4321d,
        }));

        Assert.AreEqual(ExpectedHeader, lines[0]);
        Assert.AreEqual(
            "12\t10\t-1\t3\tsample-a\ttrue\ttrue\tdetected\t42\t40\t-1\t100\t101\t202\t202;203\t" +
            "202:20;203:40\t2.5\t300.125\t1234\t5678\t4321",
            lines[1]);
    }

    [TestMethod]
    public void MzComesFromMassRatherThanTheChromatogramAxis()
    {
        // On an aligned peak ChromXsTop carries the chromatogram axis, so its Mz is not the precursor
        // m/z. Reading it here reported a sentinel for every member that had a source peak, and a real
        // value only for the gap-filled ones. Mass is the field the .mdalign MZ column uses.
        var lines = ExportLines(SpotWith(new AlignmentChromPeakFeature {
            FileID = 3,
            FileName = "sample-a",
            MasterPeakID = 1,
            PeakID = 1,
            ParentPeakID = -1,
            ChromXsTop = new ChromXs(2.5),
            Mass = 481.3159,
        }));

        Assert.AreEqual("481.3159", lines[1].Split('\t')[17]);
    }

    [TestMethod]
    public void AGapFilledMemberPublishesNoRawSpectrumPointer()
    {
        // The gap filler marks the cell with -2 and leaves the spectrum ids at their default 0. A 0 is a
        // real scan index, so writing it would point an auditor at a spectrum unrelated to the member.
        var lines = ExportLines(SpotWith(new AlignmentChromPeakFeature {
            FileID = 3,
            FileName = "sample-a",
            MasterPeakID = -2,
            PeakID = -2,
            ParentPeakID = -2,
            MS1RawSpectrumID = 0,
            MS1RawSpectrumIdTop = 0,
            MS2RawSpectrumID = 0,
            ChromXsTop = new ChromXs(0d, ChromXType.Mz, ChromXUnit.Mz),
            Mass = 481.3159,
            PeakHeightTop = 12d,
            PeakAreaAboveZero = 34d,
            PeakAreaAboveBaseline = 0d,
        }));

        var fields = lines[1].Split('\t');
        Assert.AreEqual(21, fields.Length);
        Assert.AreEqual("false", fields[6]);
        Assert.AreEqual("gap_filled", fields[7]);
        // Negative peak ids normalize to the single sentinel the compact matrix export also uses.
        Assert.AreEqual("-1", fields[8]);
        Assert.AreEqual("-1", fields[9]);
        Assert.AreEqual("-1", fields[10]);
        // Every raw-spectrum pointer is withheld, rather than reported as scan 0.
        Assert.AreEqual("", fields[11]);
        Assert.AreEqual("", fields[12]);
        Assert.AreEqual("", fields[13]);
        Assert.AreEqual("", fields[14]);
        Assert.AreEqual("", fields[15]);
        // The recovered quantities are still real measurements and are kept.
        Assert.AreEqual("481.3159", fields[17]);
        Assert.AreEqual("12", fields[18]);
    }

    [TestMethod]
    public void AnUndetectedMemberIsReportedAsAbsentRatherThanGapFilled()
    {
        var lines = ExportLines(SpotWith(new AlignmentChromPeakFeature {
            FileID = 3,
            FileName = "sample-a",
            MasterPeakID = -1,
            PeakID = -1,
            ParentPeakID = -1,
        }));

        var fields = lines[1].Split('\t');
        Assert.AreEqual("false", fields[6]);
        Assert.AreEqual("absent", fields[7]);
    }

    [TestMethod]
    public void ExportReadsAlignmentLightFileBackedRows()
    {
        var file = new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-light" };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, [file]);
        store.WriteSpotPeak(0, new AlignmentChromPeakFeature {
            FileID = 0,
            FileName = "sample-light",
            MasterPeakID = 9,
            PeakID = 9,
            MS1RawSpectrumIdTop = 50,
            MS2RawSpectrumID = 51,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [51] = 20d },
            ChromXsTop = new ChromXs(1.5),
            Mass = 250.25,
            PeakHeightTop = 500d,
            PeakAreaAboveZero = 1500d,
        });
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 0,
            AlignmentID = 0,
            ParentAlignmentID = -1,
            RepresentativeFileID = 0,
        };

        var lines = ExportLines(spot, store);

        Assert.AreEqual(ExpectedHeader, lines[0]);
        // The four empty cells are columns the light store does not persist, not columns the run failed
        // to establish. Both meanings render as an empty field, which is why the narrower light-mode
        // contract is documented next to the Headers array rather than inferred from the data.
        Assert.AreEqual(
            "0\t0\t-1\t0\tsample-light\ttrue\ttrue\tdetected\t9\t9\t\t\t50\t51\t51\t\t1.5\t250.25\t500\t1500\t",
            lines[1]);
    }

    [TestMethod]
    public void AnUnwrittenLightRowPublishesNoRawSpectrumPointer()
    {
        // A spot with no peak written for a file reads back as the store's missing row. It must not claim
        // MS2 spectrum 0, which is what gating on MS2RawSpectrumID >= 0 would have done for the light
        // overload while the in-memory overload withheld the value.
        var file = new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-light" };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, [file]);
        var spot = new AlignmentSpotProperty {
            MasterAlignmentID = 0,
            AlignmentID = 0,
            ParentAlignmentID = -1,
            RepresentativeFileID = 0,
        };

        var fields = ExportLines(spot, store)[1].Split('\t');

        Assert.AreEqual(21, fields.Length);
        Assert.AreEqual("false", fields[6]);
        Assert.AreEqual("absent", fields[7]);
        Assert.AreEqual("-1", fields[8]);
        Assert.AreEqual("", fields[12]);
        Assert.AreEqual("", fields[13]);
        Assert.AreEqual("", fields[14]);
    }

    [TestMethod]
    public void TheLightStoreCarriesTheReferenceMatchedVerdictRatherThanALibraryId()
    {
        // A suggested annotation holds a library id, so deriving the reference-matched flag from the id
        // counted a precursor-only or low-score suggestion as a reference match in light mode while the
        // normal-mode QA matrix reported it as false.
        var file = new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-light" };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(2, [file]);

        var suggested = new AlignmentChromPeakFeature { FileID = 0, FileName = "sample-light", MasterPeakID = 1, PeakID = 1 };
        suggested.MatchResults.AddResult(new MsScanMatchResult {
            LibraryID = 7,
            IsReferenceMatched = false,
            IsAnnotationSuggested = true,
        });
        store.WriteSpotPeak(0, suggested);

        var matched = new AlignmentChromPeakFeature { FileID = 0, FileName = "sample-light", MasterPeakID = 2, PeakID = 2 };
        matched.MatchResults.AddResult(new MsScanMatchResult {
            LibraryID = 7,
            IsReferenceMatched = true,
        });
        store.WriteSpotPeak(1, matched);

        Assert.IsFalse(store.ReadSpotPeaks(0).Single().IsReferenceMatched);
        Assert.IsTrue(store.ReadSpotPeaks(1).Single().IsReferenceMatched);
        // Both carry the same library id, so the id alone cannot separate them.
        Assert.AreEqual(7, store.ReadSpotPeaks(0).Single().RepresentativeLibraryID);
        Assert.AreEqual(7, store.ReadSpotPeaks(1).Single().RepresentativeLibraryID);
    }
}
