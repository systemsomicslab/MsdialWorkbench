using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class SpectrumFeatureTests
    {
        [TestMethod()]
        public void SerializationAndDeserializationTest() {
            var feature = SpectrumFeatureTestHelper.CreateSample();
            var memory = new MemoryStream();
            feature.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = SpectrumFeature.Load(memory);
            Assert.That.AreEqual(feature, actual);
        }
    }
}