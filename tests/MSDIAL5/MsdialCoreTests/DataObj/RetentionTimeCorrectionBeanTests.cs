using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass()]
public class RetentionTimeCorrectionBeanTests
{
    private string _sampleFile;

    [TestInitialize]
    public void Initialize() {
        _sampleFile = Path.GetTempFileName();
        RetentionTimeCorrectionMethod.SaveRetentionCorrectionResult(_sampleFile, [1d, 2d], [2d, 3d], [3d, 5d]);
    }

    [TestCleanup]
    public void Cleanup() {
        if (File.Exists(_sampleFile)) {
            File.Delete(_sampleFile);
        }
    }

    [TestMethod()]
    public void ClearCacheTest() {
        var rtCorrection = new RetentionTimeCorrectionBean(_sampleFile);
        rtCorrection.Restore();
        rtCorrection.ClearCache();
        Assert.IsNull(rtCorrection.OriginalRt);
        Assert.IsNull(rtCorrection.RtDiff);
        Assert.IsNull(rtCorrection.PredictedRt);
    }

    [TestMethod()]
    public void Restore_works() {
        var rtCorrection = new RetentionTimeCorrectionBean(_sampleFile);
        rtCorrection.Restore();
        CollectionAssert.AreEqual(new[] { 1d, 2d }, rtCorrection.OriginalRt);
        CollectionAssert.AreEqual(new[] { 2d, 3d }, rtCorrection.RtDiff);
        CollectionAssert.AreEqual(new[] { 3d, 5d }, rtCorrection.PredictedRt);
    }
}