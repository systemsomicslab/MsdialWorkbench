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
            IStreamManager manager = ZipStreamManager.OpenCreate(stream);

            var s = await manager.Create("ABC");
            var t = manager.Create("XYZ");

            Assert.IsFalse(t.IsCompleted);
            Write(s, Encoding.UTF8.GetBytes("abc"));
            Assert.IsFalse(t.IsCompleted);
            s.Close();
            await Task.Delay(100);
            Assert.IsTrue(t.IsCompleted);

            s = await t;
            Write(s, Encoding.UTF8.GetBytes("xyz"));
            manager.Release(s);

            var subManager = manager.Join("DEF");
            s = await subManager.Create("GHI");
            t = manager.Create("JKL");

            Assert.IsFalse(t.IsCompleted);
            Write(s, Encoding.UTF8.GetBytes("ghi"));
            Assert.IsFalse(t.IsCompleted);
            s.Close();
            await Task.Delay(100);
            Assert.IsTrue(t.IsCompleted);
            s = await t;
            Write(s, Encoding.UTF8.GetBytes("jkl"));
            manager.Release(s);

            subManager.Dispose();
            manager.Complete();
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

            subManager = manager.Join("DEF");

            s = await subManager.Get("GHI");
            t = manager.Get("JKL");

            Assert.IsFalse(t.IsCompleted);
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("ghi", Encoding.UTF8.GetString(buffer));
            Assert.IsFalse(t.IsCompleted);
            s.Dispose();
            await Task.Delay(100);
            Assert.IsTrue(t.IsCompleted);

            s = await t;
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("jkl", Encoding.UTF8.GetString(buffer));
            s.Close();

            subManager.Dispose();
            manager.Complete();
            manager.Dispose();
        }

        [TestMethod]
        public void FreeFileHandleAfterCompleteTest() {
            var stream = new MemoryStream();
            using (IStreamManager manager = ZipStreamManager.OpenCreate(stream)) {
                manager.Complete();
                stream.Close();
            }
        }

        private void Write(Stream s, byte[] c) {
            s.Write(c, 0, c.Length);
        }
    }
}