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
            BaseChromatogramPeakFeature peak = new BaseChromatogramPeakFeature();
            MessagePack.MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<BaseChromatogramPeakFeature>(memory);
            Assert.That.AreEqual(peak, actual);
        }

        [TestMethod()]
        public void SerializationAndDeserializationAsInterfaceTest() {
            var memory = new MemoryStream();
            IChromatogramPeakFeature peak = new BaseChromatogramPeakFeature();
            MessagePack.MessagePackDefaultHandler.SaveToStream(peak, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromatogramPeakFeature>(memory);
            Assert.That.AreEqual(peak, actual);
        }
    }
}