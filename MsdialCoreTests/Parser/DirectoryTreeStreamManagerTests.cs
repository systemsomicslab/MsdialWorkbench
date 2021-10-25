using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.IO;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.Parser.Tests
{
    [TestClass()]
    public class DirectoryTreeStreamManagerTests
    {
        [TestMethod()]
        public async Task DirectoryTreeStreamManagerTest() {
            var folder = Path.GetTempPath();
            var manager = new DirectoryTreeStreamManager(folder);

            var s = await manager.Create("ABC");
            var t = manager.Create("XYZ");
            Assert.IsTrue(t.IsCompleted);

            s.Write(Encoding.UTF8.GetBytes("abc"));
            s.Close();

            s = await t;
            s.Write(Encoding.UTF8.GetBytes("xyz"));
            manager.Release(s);

            Assert.IsTrue(File.Exists(Path.Combine(folder, "ABC")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "XYZ")));

            s = await manager.Get("ABC");
            t = manager.Get("XYZ");
            Assert.IsTrue(t.IsCompleted);

            var buffer = new byte[3];
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("abc", Encoding.UTF8.GetString(buffer));
            s.Dispose();

            s = await t;
            s.Read(buffer, 0, buffer.Length);
            Assert.AreEqual("xyz", Encoding.UTF8.GetString(buffer));
            s.Close();

            File.Delete(Path.Combine(folder, "ABC"));
            File.Delete(Path.Combine(folder, "XYZ"));
        }
    }
}