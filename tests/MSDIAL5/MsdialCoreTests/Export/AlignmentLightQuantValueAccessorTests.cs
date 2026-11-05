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
}
