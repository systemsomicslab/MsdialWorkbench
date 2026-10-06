using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Utility.Tests;

[TestClass()]
public class DataAccessTargetCollisionEnergyTests
{
    [TestMethod()]
    public void TheTargetEnergyScanNearestThePeakTopIsChosenWhateverTheInsertionOrder() {
        // Filled once from scan 20 towards the top (25), then again over a range starting further left:
        // the last entry at 35 eV is scan 12, far from the top.
        var feature = new ChromatogramPeakFeature
        {
            MS1RawSpectrumIdTop = 25,
            MS2RawSpectrumID = 26,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [20] = 35d, [26] = 35d, [24] = 10d, [12] = 35d, },
        };

        Assert.AreEqual(26, DataAccess.GetTargetCEIndexNearestPeakTop(feature, 35d));
        Assert.AreEqual(24, DataAccess.GetTargetCEIndexNearestPeakTop(feature, 10d));
        Assert.AreEqual(12, DataAccess.GetTargetCEIndexForMS2RawSpectrum(feature, 35d));
    }

    [TestMethod()]
    public void WithoutTheTargetEnergyOrWithoutATargetTheRepresentativeScanIsKept() {
        var feature = new ChromatogramPeakFeature
        {
            MS1RawSpectrumIdTop = 25,
            MS2RawSpectrumID = 26,
            MS2RawSpectrumID2CE = new Dictionary<int, double> { [26] = 30d, },
        };

        Assert.AreEqual(26, DataAccess.GetTargetCEIndexNearestPeakTop(feature, 6d));
        Assert.AreEqual(26, DataAccess.GetTargetCEIndexNearestPeakTop(feature, -1d));
    }
}
