using CompMs.MsdialCore.MSDec;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.IO;
using System.Text;

namespace CompMs.App.Msdial.Model.Loader.Tests
{
    [TestClass()]
    public class MSDecLoaderTests {
        [TestMethod()]
        public void DisposeTest() {
            var stream = new MockStream();
            helper(stream);
            Assert.IsFalse(stream.Disposed);

            var loader = new MSDecLoader(stream);
            Assert.IsFalse(stream.Disposed);

            loader.Dispose();
            Assert.IsTrue(stream.Disposed);
        }

        private void helper(Stream s){
            s.Write(Encoding.ASCII.GetBytes("TE"), 0, 2); // test
            s.Write(BitConverter.GetBytes(-1), 0, 4);
            s.Write(BitConverter.GetBytes(false), 0, 1);
            s.Write(BitConverter.GetBytes(0), 0, 4);
        }

        class MockStream : MemoryStream
        {
            public bool Disposed { get; private set; } = false;

            public override void Close() {
                Disposed = true;
                base.Close();
            }
        }
    }
}