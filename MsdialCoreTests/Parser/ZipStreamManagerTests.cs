using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Text;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class ZipStreamManagerTests
    {
        [TestMethod()]
        public async Task ZipStreamManagerTest() {
            var stream = new MemoryStream();
            var manager = ZipStreamManager.OpenCreate(stream);

            var s = await manager.Create("ABC");
            var t = manager.Create("XYZ");

            Assert.IsFalse(t.IsCompleted);
            s.Write(Encoding.UTF8.GetBytes("abc"));
            Assert.IsFalse(t.IsCompleted);
            s.Close();
            await Task.Delay(100);
            Assert.IsTrue(t.IsCompleted);

            s = await t;
            s.Write(Encoding.UTF8.GetBytes("xyz"));
            manager.Release(s);

            manager.Dispose();

            manager = ZipStreamManager.OpenGet(stream);

            s = await manager.Get("ABC");
            t = manager.Get("XYZ");

            Assert.IsFalse(t.IsCompleted);
            var buffer = new byte[3];
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("abc", Encoding.UTF8.GetString(buffer));
            Assert.IsFalse(t.IsCompleted);
            s.Dispose();
            await Task.Delay(100);
            Assert.IsTrue(t.IsCompleted);

            s = await t;
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("xyz", Encoding.UTF8.GetString(buffer));
            s.Close();

            manager.Dispose();
        }
    }
}