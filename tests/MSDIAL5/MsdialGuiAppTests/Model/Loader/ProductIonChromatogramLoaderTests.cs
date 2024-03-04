using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.DataObj.Tests.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.App.Msdial.Model.Loader.Tests;

[TestClass()]
public class ProductIonChromatogramLoaderTests
{
    [TestMethod()]
    public void LoadChromatogramTest() {
        // Arrange
        var mockRawSpectra = new MockRawSpectra();
        var testRange = new ChromatogramRange(0, 100, ChromXType.RT, ChromXUnit.Min);
        var testState = (new MzRange(400, 500), new MzRange(100, 200));
        
        var expectedPeaks = new List<ChromatogramPeak> {
            new ChromatogramPeak(0, 150, 1000, new RetentionTime(1)),
            new ChromatogramPeak(1, 170, 2000, new RetentionTime(2)),
        };
        var expectedPeakItems = expectedPeaks.Select(p => new PeakItem(p)).ToList();

        mockRawSpectra.ExpectedChromatogram = new Chromatogram(expectedPeaks, ChromXType.RT, ChromXUnit.Min);
        
        var loader = new ProductIonChromatogramLoader(mockRawSpectra, testRange);

        // Act
        var result = loader.LoadChromatogram(testState);

        // Assert
        Assert.AreEqual(expectedPeakItems.Count, result.Count);
        for (int i = 0; i < expectedPeakItems.Count; i++)
        {
            Assert.AreEqual(expectedPeakItems[i].Time, result[i].Time);
            Assert.AreEqual(expectedPeakItems[i].Intensity, result[i].Intensity);
        }
    }
}