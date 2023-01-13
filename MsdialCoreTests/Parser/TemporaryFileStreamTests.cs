using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class TemporaryFileStreamTests
    {
        [TestMethod()]
        public void MoveTest() {
            var filePath = Path.GetTempFileName();

            using (var stream = new TemporaryFileStream(filePath)) {
                using (var writer = new StreamWriter(stream, leaveOpen: true)) {
                    writer.WriteLine("ABC");
                    writer.WriteLine("XYZ");
                }
                stream.Move();
            }
            Assert.IsTrue(File.Exists(filePath));

            using (var stream = File.Open(filePath, FileMode.Open))
            using (var reader = new StreamReader(stream)) {
                Assert.AreEqual("ABC", reader.ReadLine());
                Assert.AreEqual("XYZ", reader.ReadLine());
            }
        }
    }
}