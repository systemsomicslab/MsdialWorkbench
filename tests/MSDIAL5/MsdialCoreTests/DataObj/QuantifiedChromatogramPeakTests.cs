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
}