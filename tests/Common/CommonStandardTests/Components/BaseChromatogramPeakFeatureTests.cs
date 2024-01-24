using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class BaseChromatogramPeakFeatureTests
    {
        [TestMethod()]
        public void SerializationAndDeserializationTest() {
            var memory = new MemoryStream();
            BaseChromatogramPeakFeature peak = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = 1,
                ChromScanIdTop = 2,
                ChromScanIdRight = 3,
                ChromXsLeft = new ChromXs(new RetentionTime(4), new RetentionIndex(5), new DriftTime(6), new MzValue(7), ChromXType.RI),
                ChromXsTop = new ChromXs(new RetentionTime(8), new RetentionIndex(9), new DriftTime(10), new MzValue(11), ChromXType.RI),
                ChromXsRight = new ChromXs(new RetentionTime(12), new RetentionIndex(13), new DriftTime(14), new MzValue(15), ChromXType.RI) ,
                PeakHeightLeft = 16,
                PeakHeightTop = 17,
                PeakHeightRight = 18,
                PeakAreaAboveZero = 19,
                PeakAreaAboveBaseline = 20,
                Mass = 21,
            };
            MessagePack.MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<BaseChromatogramPeakFeature>(memory);
            Assert.That.AreEqual(peak, actual);
        }

        [TestMethod()]
        public void SerializationAndDeserializationAsInterfaceTest() {
            var memory = new MemoryStream();
            IChromatogramPeakFeature peak = new BaseChromatogramPeakFeature
            {
                ChromScanIdLeft = 1,
                ChromScanIdTop = 2,
                ChromScanIdRight = 3,
                ChromXsLeft = new ChromXs(new RetentionTime(4), new RetentionIndex(5), new DriftTime(6), new MzValue(7), ChromXType.RI),
                ChromXsTop = new ChromXs(new RetentionTime(8), new RetentionIndex(9), new DriftTime(10), new MzValue(11), ChromXType.RI),
                ChromXsRight = new ChromXs(new RetentionTime(12), new RetentionIndex(13), new DriftTime(14), new MzValue(15), ChromXType.RI) ,
                PeakHeightLeft = 16,
                PeakHeightTop = 17,
                PeakHeightRight = 18,
                PeakAreaAboveZero = 19,
                PeakAreaAboveBaseline = 20,
                Mass = 21,
            };
            MessagePack.MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromatogramPeakFeature>(memory);
            Assert.That.AreEqual(peak, actual);
        }
    }
}