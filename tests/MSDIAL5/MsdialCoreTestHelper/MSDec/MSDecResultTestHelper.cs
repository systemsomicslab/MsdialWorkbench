using CompMs.Common.Components.Tests;
using CompMs.Common.DataObj.Result.Tests;
using CompMs.Common.Tests.Utility;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CompMs.MsdialCore.MSDec.Tests
{
    public static class MSDecResultTestHelper
    {
        public static void AreEqual(this Assert assert, MSDecResult expected, MSDecResult actual) {
            Assert.AreEqual(expected.ScanID, actual.ScanID);
            Assert.AreEqual(expected.PrecursorMz, actual.PrecursorMz);
            Assert.AreEqual(expected.IonMode, actual.IonMode);
            assert.AreEqual(expected.ChromXs, actual.ChromXs);
            Assert.AreEqual(expected.RawSpectrumID, actual.RawSpectrumID);
            Assert.AreEqual(expected.ModelPeakMz, actual.ModelPeakMz);
            Assert.AreEqual(expected.ModelPeakHeight, actual.ModelPeakHeight);
            Assert.AreEqual(expected.ModelPeakArea, actual.ModelPeakArea);
            Assert.AreEqual(expected.IntegratedHeight, actual.IntegratedHeight);
            Assert.AreEqual(expected.IntegratedArea, actual.IntegratedArea);
            Assert.AreEqual(expected.Splash, actual.Splash);
            CollectionAssert.AreEqual(expected.ModelMasses, actual.ModelMasses);
            Assert.AreEqual(expected.ModelPeakPurity, actual.ModelPeakPurity);
            Assert.AreEqual(expected.ModelPeakQuality, actual.ModelPeakQuality);
            Assert.AreEqual(expected.SignalNoiseRatio, actual.SignalNoiseRatio);
            Assert.AreEqual(expected.EstimatedNoise, actual.EstimatedNoise);
            Assert.AreEqual(expected.AmplitudeScore, actual.AmplitudeScore);
            Assert.AreEqual(expected.MspID, actual.MspID);
            Assert.AreEqual(expected.MspIDWhenOrdered, actual.MspIDWhenOrdered);
            CollectionAssert.AreEqual(expected.MspIDs, actual.MspIDs);
            assert.AreEqual(expected.MspBasedMatchResult, actual.MspBasedMatchResult);
            CollectionAssert.That.AreEqual(expected.Spectrum, actual.Spectrum, SpectrumPeakTestHelper.AreEqual);
            CollectionAssert.That.AreEqual(expected.ModelPeakChromatogram, actual.ModelPeakChromatogram, ChromatogramPeakTestHelper.AreEqual);
        }
    }
}
