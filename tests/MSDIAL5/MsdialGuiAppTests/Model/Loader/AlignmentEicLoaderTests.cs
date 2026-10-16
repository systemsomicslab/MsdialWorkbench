using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.Loader.Tests;

[TestClass]
public class AlignmentEicLoaderTests {
    [TestMethod]
    public void AutomaticCorrectionUsesOriginalRtBoundsForTheStoredEic() {
        var chromatogram = new Chromatogram(new[] {
            new ValuePeak(0, 8.361d, 322.044d, 0d),
            new ValuePeak(1, 8.600d, 322.044d, 30000d),
            new ValuePeak(2, 8.619d, 322.044d, 128911d),
            new ValuePeak(3, 8.680d, 322.044d, 40000d),
            new ValuePeak(4, 8.735d, 322.044d, 5000d),
            new ValuePeak(5, 8.800d, 322.044d, 0d),
        }, ChromXType.RT, ChromXUnit.Min);
        var storedEic = new ChromatogramPeakInfo(
            9, chromatogram.AsPeakArray(), 8.619f, 8.361f, 8.735f);

        var correctedArea = AlignmentEicLoader.SelectPeakArea(
            chromatogram, storedEic, 8.25d, 8.62d, useOriginalRtPeakBounds: true);
        var legacyArea = AlignmentEicLoader.SelectPeakArea(
            chromatogram, storedEic, 8.25d, 8.62d, useOriginalRtPeakBounds: false);

        Assert.IsNotNull(correctedArea);
        Assert.IsNotNull(legacyArea);
        Assert.AreEqual(8.735d, correctedArea.GetRight().ChromXs.RT.Value, 1e-6);
        Assert.AreEqual(8.619d, legacyArea.GetRight().ChromXs.RT.Value, 1e-6);
    }
}
