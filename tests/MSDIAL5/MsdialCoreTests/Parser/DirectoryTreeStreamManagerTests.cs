using Accord.Statistics.Kernels;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
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

            using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                var s = await manager.Create("ABC");
                var t = manager.Create("XYZ");
                Assert.IsTrue(t.IsCompleted);

                s.Write(Encoding.UTF8.GetBytes("abc"));
                s.Close();

                s = await t;
                s.Write(Encoding.UTF8.GetBytes("xyz"));
                manager.Release(s);

                using var subManager = manager.Join("DEF");
                var u =  await subManager.Create("GHI");
                u.Write(Encoding.UTF8.GetBytes("ghi"));
                u.Close();

                var v =  await manager.Create("JKL");
                v.Write(Encoding.UTF8.GetBytes("jkl"));
                v.Close();

                manager.Complete();
            }

            Assert.IsTrue(File.Exists(Path.Combine(folder, "ABC")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "XYZ")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "DEF", "GHI")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "JKL")));

            using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                var s = await manager.Get("ABC");
                var t = manager.Get("XYZ");
                Assert.IsTrue(t.IsCompleted);

                var buffer = new byte[3];
                s.Read(buffer, 0, buffer.Length);
                Assert.AreEqual("abc", Encoding.UTF8.GetString(buffer));
                s.Dispose();

                s = await t;
                s.Read(buffer, 0, buffer.Length);
                Assert.AreEqual("xyz", Encoding.UTF8.GetString(buffer));
                s.Close();

                using var subManager = manager.Join("DEF");
                var u = await subManager.Get("GHI");
                u.Read(buffer, 0, buffer.Length);
                Assert.AreEqual("ghi", Encoding.UTF8.GetString(buffer));
                u.Close();

                var v = await manager.Get("JKL");
                v.Read(buffer, 0, buffer.Length);
                Assert.AreEqual("jkl", Encoding.UTF8.GetString(buffer));
                v.Close();

                manager.Complete();
            }

            File.Delete(Path.Combine(folder, "ABC"));
            File.Delete(Path.Combine(folder, "XYZ"));
            File.Delete(Path.Combine(folder, "DEF", "GHI"));
            File.Delete(Path.Combine(folder, "JKL"));
        }

        [TestMethod()]
        public async Task FailedTest() {
            var folder = Path.GetTempPath();
            using (var stream = File.Open(Path.Combine(folder, "ABC"), FileMode.Create)) {
                stream.Write(Encoding.UTF8.GetBytes("abc"));
            }
            using (var stream = File.Open(Path.Combine(folder, "XYZ"), FileMode.Create)) {
                stream.Write(Encoding.UTF8.GetBytes("xyz"));
            }
            Assert.IsTrue(File.Exists(Path.Combine(folder, "ABC")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "XYZ")));
            
            try {
                using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                    var s = await manager.Create("ABC");
                    var t = manager.Create("XYZ");

                    s.Write(Encoding.UTF8.GetBytes("Failed"));
                    manager.Release(s);

                    s = await t;
                    s.Write(Encoding.UTF8.GetBytes("Failed"));

                    throw new Exception();

#pragma warning disable CS0162 // Unreachable code detected
                    manager.Complete();
#pragma warning restore CS0162 // Unreachable code detected
                }
            }
            catch {

            }

            Assert.IsTrue(File.Exists(Path.Combine(folder, "ABC")));
            using (var stream = File.Open(Path.Combine(folder, "ABC"), FileMode.Open)) {
                var buffer = new byte[1024];
                stream.Read(buffer, 0, buffer.Length);
                StringAssert.StartsWith( Encoding.UTF8.GetString(buffer), "abc");
            }
            Assert.IsTrue(File.Exists(Path.Combine(folder, "XYZ")));
            using (var stream = File.Open(Path.Combine(folder, "XYZ"), FileMode.Open)) {
                var buffer = new byte[1024];
                stream.Read(buffer, 0, buffer.Length);
                StringAssert.StartsWith( Encoding.UTF8.GetString(buffer), "xyz");
            }
        }
    }
}