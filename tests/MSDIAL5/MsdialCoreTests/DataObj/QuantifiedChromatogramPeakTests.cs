using CompMs.Common.Components.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class QuantifiedChromatogramPeakTests
    {
        [TestMethod()]
        public void SaveAndLoadTest() {
            var peak = new QuantifiedChromatogramPeak(
                new ChromatogramPeakFeature { MasterPeakID = 1, Mass = 100d, },
                new ChromatogramPeakShape { AmplitudeOrderValue = 1, AmplitudeScoreValue = 1, },
                100, 50, 150);
            var memory = new MemoryStream();
            peak.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = QuantifiedChromatogramPeak.Load(memory);
            Assert.That.AreEqual(peak, actual);
        }
    }

    public static class QuantifiedChromatogramPeakTestHelper {
        public static void AreEqual(this Assert assert, QuantifiedChromatogramPeak expected, QuantifiedChromatogramPeak actual) {
            assert.AreEqual(expected.PeakFeature, actual.PeakFeature);
            assert.AreEqual(expected.PeakShape, actual.PeakShape);
            Assert.AreEqual(expected.MS1RawSpectrumIdTop, actual.MS1RawSpectrumIdTop);
            Assert.AreEqual(expected.MS1RawSpectrumIdLeft, actual.MS1RawSpectrumIdLeft);
            Assert.AreEqual(expected.MS1RawSpectrumIdRight, actual.MS1RawSpectrumIdRight);
        }
    }
}