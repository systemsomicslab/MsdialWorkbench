using CompMs.App.Msdial.Model.DataObj;
using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.DataObj.Tests.Helper;
using Microsoft.VisualStudio.TestTools.UnitTesting;
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
        
        var expectedPeaks = new ValuePeak[] {
            new ValuePeak(0, 1, 150, 1000),
            new ValuePeak(1, 2, 170, 2000),
        };
        var expectedPeakItems = expectedPeaks.Select(p => new PeakItem(p.ConvertToChromatogramPeak(ChromXType.RT, ChromXUnit.Min))).ToList();

        mockRawSpectra.ExpectedChromatogram = new ExtractedIonChromatogram(expectedPeaks, ChromXType.RT, ChromXUnit.Min, 160d);
        
        var loader = new ProductIonChromatogramLoader(mockRawSpectra, testRange);

        // Act
        var result = ((IWholeChromatogramLoader<(MzRange Precursor, MzRange Product)>)loader).LoadChromatogram(testState);

        // Assert
        Assert.AreEqual(expectedPeakItems.Count, result.ChromatogramPeaks.Count);
        for (int i = 0; i < expectedPeakItems.Count; i++)
        {
            Assert.AreEqual(expectedPeakItems[i].Time, result.ChromatogramPeaks[i].Time);
            Assert.AreEqual(expectedPeakItems[i].Intensity, result.ChromatogramPeaks[i].Intensity);
        }
    }
}