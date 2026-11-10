using CompMs.Common.DataObj.Result;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCoreTests.Export;

[TestClass]
public class AlignmentLightQuantValueAccessorTests
{
    [TestMethod]
    public void GetQuantValuesUsesFileBackedRowsWhenSpotDoesNotHoldPeakProperties()
    {
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "sample-a" },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "sample-b" },
        };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, files);
        store.WriteSpotPeak(0, new AlignmentChromPeakFeature {
            FileID = 1,
            FileName = "sample-b",
            PeakHeightTop = 1234d,
        });
        var spot = new AlignmentSpotProperty { MasterAlignmentID = 0 };
        var accessor = new AlignmentLightQuantValueAccessor("Height", new ParameterBase(), store);

        var headers = accessor.GetQuantHeaders(files);
        var values = accessor.GetQuantValues(spot);

        CollectionAssert.AreEqual(new[] { "sample-a", "sample-b" }, headers);
        Assert.AreEqual("0", values["sample-a"]);
        Assert.AreEqual("1234", values["sample-b"]);
    }

    /// <summary>
    /// Light mode reads the representative's own verdict from the store; an in-silico one is marked
    /// "In-silico matched" and not "Reference matched", as the non-light path marks it (the author's
    /// decision of 2026-10-28). A file without a peak is neither.
    /// </summary>
    [TestMethod]
    public void QaReferenceMatchedLeavesOutAnInSilicoRepresentative()
    {
        var files = new[] {
            new AnalysisFileBean { AnalysisFileId = 0, AnalysisFileName = "acquired" },
            new AnalysisFileBean { AnalysisFileId = 1, AnalysisFileName = "in-silico" },
            new AnalysisFileBean { AnalysisFileId = 2, AnalysisFileName = "absent" },
        };
        using var store = AlignmentLightPeakStore.CreateTemp();
        store.Initialize(1, files);
        store.WriteSpotPeak(0, PeakMatchedBy(0, "acquired", AnnotationEvidenceSource.ReferenceSpectrum));
        store.WriteSpotPeak(0, PeakMatchedBy(1, "in-silico", AnnotationEvidenceSource.BySpectrumPredictionTool));
        var spot = new AlignmentSpotProperty { MasterAlignmentID = 0 };

        var referenceMatched = new AlignmentLightQuantValueAccessor("Reference matched", new ParameterBase(), store).GetQuantValues(spot);
        var inSilicoMatched = new AlignmentLightQuantValueAccessor("In-silico matched", new ParameterBase(), store).GetQuantValues(spot);

        Assert.AreEqual("TRUE", referenceMatched["acquired"]);
        Assert.AreEqual("FALSE", referenceMatched["in-silico"]);
        Assert.AreEqual("FALSE", referenceMatched["absent"]);
        Assert.AreEqual("FALSE", inSilicoMatched["acquired"]);
        Assert.AreEqual("TRUE", inSilicoMatched["in-silico"]);
        Assert.AreEqual("FALSE", inSilicoMatched["absent"]);

        var rows = store.ReadSpotPeaks(0);
        Assert.IsTrue(rows[1].IsReferenceMatched, "the representative's own verdict is kept as written");
        Assert.IsTrue(rows[1].IsInSilicoMatched);
        Assert.IsFalse(rows[0].IsInSilicoMatched);
    }

    private static AlignmentChromPeakFeature PeakMatchedBy(int fileId, string fileName, AnnotationEvidenceSource evidence)
    {
        var peak = new AlignmentChromPeakFeature {
            FileID = fileId,
            FileName = fileName,
            PeakHeightTop = 1000d,
            Name = "Compound",
        };
        peak.MatchResults.AddResult(new MsScanMatchResult {
            Name = "Compound",
            Source = SourceType.MspDB,
            EvidenceSource = evidence,
            IsReferenceMatched = true,
            IsSpectrumMatch = true,
            IsPrecursorMzMatch = true,
            TotalScore = 0.8f,
        });
        return peak;
    }
}
