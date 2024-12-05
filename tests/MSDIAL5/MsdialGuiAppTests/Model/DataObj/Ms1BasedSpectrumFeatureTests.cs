using CompMs.MsdialCore.DataObj.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.App.Msdial.Model.DataObj.Tests
{
    [TestClass()]
    public class Ms1BasedSpectrumFeatureTests
    {
        [TestMethod()]
        public void GetCurrentSpectrumFeatureTest() {
            var spectrumFeature = SpectrumFeatureTestHelper.CreateSample();
            spectrumFeature.Comment = "";
            var model = new Ms1BasedSpectrumFeature(spectrumFeature)
            {
                Comment = "ABCD",
                QuantifiedChromatogramPeak = QuantifiedChromatogramPeakTestHelper.CreateSample().WithMs1RawSpectrumIdTop(80),
            };

            var actual = model.GetCurrentSpectrumFeature();
            Assert.AreEqual(80, actual.QuantifiedChromatogramPeak.MS1RawSpectrumIdTop);
            Assert.AreEqual("ABCD", actual.Comment);
        }
    }
}