using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class RetentionIndexTests
    {
        [TestMethod()]
        public void RetentionIndexTest() {
            var memory = new MemoryStream();
            var ri = new RetentionIndex(10d, ChromXUnit.Min);
            MessagePack.MessagePackDefaultHandler.SaveToStream<IChromX>(ri, memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = MessagePack.MessagePackDefaultHandler.LoadFromStream<IChromX>(memory);
            Assert.IsInstanceOfType(actual, typeof(RetentionIndex));
            Assert.AreEqual(actual.Value, ri.Value);
            Assert.AreEqual(actual.Unit, ri.Unit);
            Assert.AreEqual(actual.Type, ri.Type);
        }
    }
}