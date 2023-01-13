using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class TemporaryFileStreamTests
    {
        private string filePath;

        [TestMethod()]
        public void MoveTest() {
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

        [TestMethod()]
        [DataRow(false, false)]
        [DataRow(true, true)]
        public void DisposeTest(bool fileExists, bool moveBeforeDispose) {
            using (var stream = new TemporaryFileStream(filePath, moveBeforeDispose)) {
                using (var writer = new StreamWriter(stream, leaveOpen: true)) {
                    writer.WriteLine("ABC");
                    writer.WriteLine("XYZ");
                }
            }
            Assert.AreEqual(fileExists, File.Exists(filePath));

            if (fileExists) {
                using (var stream = File.Open(filePath, FileMode.Open))
                using (var reader = new StreamReader(stream)) {
                    Assert.AreEqual("ABC", reader.ReadLine());
                    Assert.AreEqual("XYZ", reader.ReadLine());
                }
            }
        }

        [TestInitialize()]
        public void TestInitialize() {
            filePath = Path.GetTempFileName();
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        }

        [TestCleanup()]
        public void TestCleanup() {
            if (File.Exists(filePath)) {
                File.Delete(filePath);
            }
        }
    }
}