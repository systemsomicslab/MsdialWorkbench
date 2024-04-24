using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests;

[TestClass()]
public class RetentionTimeCorrectionBeanTests
{
    private string _sampleFile;
    private RetentionTimeCorrectionBean _rtCorrectionBean;

    [TestInitialize]
    public void Initialize() {
        _sampleFile = Path.GetTempFileName();
        _rtCorrectionBean = new RetentionTimeCorrectionBean(_sampleFile)
        {
            OriginalRt = [1d, 2d],
            RtDiff = [2d, 3d],
            PredictedRt = [3d, 5d],
        };
        _rtCorrectionBean.Save();
    }

    [TestCleanup]
    public void Cleanup() {
        if (File.Exists(_sampleFile)) {
            File.Delete(_sampleFile);
        }
    }

    [TestMethod()]
    public void ClearPredicts_Test() {
        var rtCorrection = _rtCorrectionBean;
        rtCorrection.Restore();
        rtCorrection.ClearPredicts();
        Assert.IsNull(rtCorrection.OriginalRt);
        Assert.IsNull(rtCorrection.RtDiff);
        Assert.IsNull(rtCorrection.PredictedRt);
    }

    [TestMethod()]
    public void Restore_works() {
        var rtCorrection = _rtCorrectionBean;
        rtCorrection.Restore();
        CollectionAssert.AreEqual(new[] { 1d, 2d }, rtCorrection.OriginalRt);
        CollectionAssert.AreEqual(new[] { 2d, 3d }, rtCorrection.RtDiff);
        CollectionAssert.AreEqual(new[] { 3d, 5d }, rtCorrection.PredictedRt);
    }
}