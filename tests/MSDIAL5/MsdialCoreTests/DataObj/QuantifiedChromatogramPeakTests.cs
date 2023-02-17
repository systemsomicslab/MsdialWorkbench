using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class QuantifiedChromatogramPeakTests
    {
        [TestMethod()]
        public void SaveAndLoadTest() {
            var peak = QuantifiedChromatogramPeakTestHelper.CreateSample();
            var memory = new MemoryStream();
            peak.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = QuantifiedChromatogramPeak.Load(memory);
            Assert.That.AreEqual(peak, actual);
        }
    }
}