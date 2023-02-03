using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class DriftTimeTests
    {
        [TestMethod()]
        public void DriftTimeTest() {
            var memory = new MemoryStream();
            var dt = new DriftTime(10d, ChromXUnit.K0);
            MessagePack.MessagePackDefaultHandler.SaveToStream<IChromX>(dt, memory);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromX>(memory);
            Assert.IsInstanceOfType(actual, typeof(DriftTime));
            Assert.That.AreEqual(dt, actual);
        }
    }
}