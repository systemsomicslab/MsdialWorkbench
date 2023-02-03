using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class MzValueTests
    {
        [TestMethod()]
        public void MzValueTest() {
            var memory = new MemoryStream();
            var mz = new MzValue(10d, ChromXUnit.None);
            MessagePack.MessagePackDefaultHandler.SaveToStream<IChromX>(mz, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromX>(memory);
            Assert.IsInstanceOfType(actual, typeof(MzValue));
            Assert.That.AreEqual(mz, actual);
        }
    }
}