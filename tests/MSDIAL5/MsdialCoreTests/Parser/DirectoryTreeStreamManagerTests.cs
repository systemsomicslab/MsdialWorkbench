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

                Write(s, "abc");
                s.Close();

                s = await t;
                Write(s, "xyz");
                manager.Release(s);

                using (var subManager = manager.Join("DEF")) {
                    var u = await subManager.Create("GHI");
                    Write(u, "ghi");
                    u.Close();
                }

                var v =  await manager.Create("JKL");
                Write(v, "jkl");
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

                using (var subManager = manager.Join("DEF")) {
                    var u = await subManager.Get("GHI");
                    u.Read(buffer, 0, buffer.Length);
                    Assert.AreEqual("ghi", Encoding.UTF8.GetString(buffer));
                    u.Close();
                }

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
                Write(stream, "abc");
            }
            using (var stream = File.Open(Path.Combine(folder, "XYZ"), FileMode.Create)) {
                Write(stream, "xyz");
            }
            Assert.IsTrue(File.Exists(Path.Combine(folder, "ABC")));
            Assert.IsTrue(File.Exists(Path.Combine(folder, "XYZ")));
            
            try {
                using (IStreamManager manager = new DirectoryTreeStreamManager(folder)) {
                    var s = await manager.Create("ABC");
                    var t = manager.Create("XYZ");

                    Write(s, "Failed");
                    manager.Release(s);

                    s = await t;
                    Write(s, "Failed");

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

        private void Write(Stream s, string c) {
            var b = Encoding.UTF8.GetBytes(c);
            s.Write(b, 0, b.Length);
        }
    }
}