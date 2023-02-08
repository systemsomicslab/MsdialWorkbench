using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace MsdialCoreTests.Parser
{
    [TestClass]
    public class ChromatogramPeakInfoSerializeTest
    {
        [TestMethod]
        public void SerializeAndDeserialize()
        {
            var peakInfo = ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(2, 1000);
            var serializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1");

            ChromatogramPeakInfo result = null;
            using (var stream = new MemoryStream(100000)) {
                serializer.Serialize(stream, peakInfo);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.Deserialize(stream);
            }

            ChromatogramSerializerTestHelper.AreEqual(peakInfo, result);
        }

        [TestMethod]
        public void SerializeAllAndDeserializeAll() {
            var peaks = new List<ChromatogramPeakInfo>(10);
            for (int i = 0; i < 10; i++) {
                peaks.Add(ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(i, i * i * 20 + 100));
            }

            var serializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1");

            IEnumerable<ChromatogramPeakInfo> result = null;
            using (var stream = new MemoryStream(300000)) {
                serializer.SerializeAll(stream, peaks);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeAll(stream);

                foreach ((var peak, var res) in peaks.Zip(result, (e, a) => (e, a)))
                    ChromatogramSerializerTestHelper.AreEqual(peak, res);
            }
        }

        [TestMethod]
        public void SerializeAllAndDeserializeN() {
            var peaks = new List<ChromatogramPeakInfo>(10);
            for (int i = 0; i < 10; i++) {
                peaks.Add(ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(i, i * i * 20 + 100));
            }

            var serializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1");

            IEnumerable<ChromatogramPeakInfo> result = null;
            using (var stream = new MemoryStream(300000)) {
                serializer.SerializeAll(stream, peaks);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeN(stream, 5);

                foreach ((var peak, var res) in peaks.Zip(result, (e, a) => (e, a)))
                    ChromatogramSerializerTestHelper.AreEqual(peak, res);
            }
        }

        [TestMethod]
        public void SerializeAllAndDeserializeAt() {
            var peaks = new List<ChromatogramPeakInfo>(10);
            for (int i = 0; i < 10; i++) {
                peaks.Add(ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(i, i * i * 20 + 100));
            }

            var serializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1");

            ChromatogramPeakInfo result = null;
            using (var stream = new MemoryStream(300000)) {
                serializer.SerializeAll(stream, peaks);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeAt(stream, 7);
            }

            ChromatogramSerializerTestHelper.AreEqual(peaks[7], result);
        }

        [TestMethod]
        public void SerializeAndDeserializeSpecificType()
        {
            var peakInfo = ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(2, 1000, CompMs.Common.Components.ChromXType.Mz);
            var serializer = ChromatogramSerializerFactory.CreatePeakSerializer("CPS1");

            ChromatogramPeakInfo result = null;
            using (var stream = new MemoryStream(100000)) {
                serializer.Serialize(stream, peakInfo);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.Deserialize(stream);
            }

            ChromatogramSerializerTestHelper.AreEqual(peakInfo, result);
        }

        [TestMethod]
        public void SerializeAndDeserializeVersionCheck()
        {
            var version = "CPS1";
            var peakInfo = ChromatogramSerializerTestHelper.CreateChromatogramPeakInfo(2, 1000, CompMs.Common.Components.ChromXType.Mz);
            var serializer = (ChromatogramVersionDecorator<ChromatogramPeakInfo>)ChromatogramSerializerFactory.CreatePeakSerializer(version);

            using (var stream = new MemoryStream(100000)) {
                serializer.Serialize(stream, peakInfo);
                stream.Seek(0, SeekOrigin.Begin);
                var result = serializer.GetVersion(stream);
                Assert.AreEqual(version, result);
            }
        }
    }
}
