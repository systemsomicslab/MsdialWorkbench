using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.DataObj.Tests
{
    [TestClass()]
    public class AnnotatedMSDecResultTests
    {
        [TestMethod()]
        public void SaveAndLoadTest() {
            var obj = AnnotatedMSDecResultTestHelper.CreateSample();

            var memory = new MemoryStream();
            obj.Save(memory);
            memory.Seek(0, SeekOrigin.Begin);
            var actual = AnnotatedMSDecResult.Load(memory);

            Assert.That.AreEqual(obj, actual);
        }
    }
}