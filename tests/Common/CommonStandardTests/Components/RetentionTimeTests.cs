using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class RetentionTimeTests
    {
        [TestMethod()]
        public void RetentionTimeSeralizeTest() {
            var stream = new MemoryStream();
            var rt = new RetentionTime(10d, ChromXUnit.Sec);
            MessagePack.MessagePackDefaultHandler.SaveToStream<IChromX>(rt, stream);
            stream.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromX>(stream);
            Assert.IsInstanceOfType(actual, typeof(RetentionTime));
            Assert.That.AreEqual(rt, actual);
        }
    }
}