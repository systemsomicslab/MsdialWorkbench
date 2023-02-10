using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;

using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;

namespace MsdialCoreTests.Parser
{
    [TestClass]
    public class ChromatogramSpotInfoSerializeTest
    {
        [TestMethod]
        public void SerializeAndDeserialize()
        {
            var spotInfo = ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(5, 100, ChromXType.RT);
            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            ChromatogramSpotInfo result = null;
            using (var stream = new MemoryStream(1000000)) {
                serializer.Serialize(stream, spotInfo);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.Deserialize(stream);
            }

            ChromatogramSerializerTestHelper.AreEqual(spotInfo, result);
        }

        [TestMethod]
        public void SerializeAllAndDeserializeAll() {
            var spots = new List<ChromatogramSpotInfo>(10);
            for (int i = 0; i < 10; i++) {
                spots.Add(ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(i * 2 + 1, i * i + 100, ChromXType.RT));
            }

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            IEnumerable<ChromatogramSpotInfo> result = null;
            using (var stream = new MemoryStream(3000000)) {
                serializer.SerializeAll(stream, spots);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeAll(stream);

                foreach ((var spot, var res) in spots.Zip(result, (e, a) => (e, a)))
                    ChromatogramSerializerTestHelper.AreEqual(spot, res);
            }
        }

        [TestMethod]
        public void SerializeAllAndDeserializeN() {
            var spots = new List<ChromatogramSpotInfo>(10);
            for (int i = 0; i < 10; i++) {
                spots.Add(ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(i * 2 + 1, i * i + 100, ChromXType.RT));
            }

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            IEnumerable<ChromatogramSpotInfo> result = null;
            using (var stream = new MemoryStream(3000000)) {
                serializer.SerializeAll(stream, spots);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeN(stream, 5);

                foreach ((var spot, var res) in spots.Zip(result, (e, a) => (e, a)))
                    ChromatogramSerializerTestHelper.AreEqual(spot, res);
            }
        }

        [TestMethod]
        public void SerializeAllAndDeserializeAt() {
            var spots = new List<ChromatogramSpotInfo>(10);
            for (int i = 0; i < 10; i++) {
                spots.Add(ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(i * 2 + 1, i * i+ 100, ChromXType.RT));
            }

            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1");

            ChromatogramSpotInfo result = null;
            using (var stream = new MemoryStream(3000000)) {
                serializer.SerializeAll(stream, spots);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.DeserializeAt(stream, 7);
            }

            ChromatogramSerializerTestHelper.AreEqual(spots[7], result);
        }

        [TestMethod]
        public void SerializeAndDeserializeSpecificType()
        {
            var spotInfo = ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(5, 100, ChromXType.Mz);
            var serializer = ChromatogramSerializerFactory.CreateSpotSerializer("CSS1", ChromXType.Mz);

            ChromatogramSpotInfo result = null;
            using (var stream = new MemoryStream(1000000)) {
                serializer.Serialize(stream, spotInfo);
                stream.Seek(0, SeekOrigin.Begin);
                result = serializer.Deserialize(stream);
            }

            ChromatogramSerializerTestHelper.AreEqual(spotInfo, result);
        }

        [TestMethod]
        public void SerializeAndDeserializeVersionCheck()
        {
            var version = "CSS1";
            var spotInfo = ChromatogramSerializerTestHelper.CreateChromatogramSpotInfo(5, 100, ChromXType.Mz);
            var serializer = (ChromatogramVersionDecorator<ChromatogramSpotInfo>)ChromatogramSerializerFactory.CreateSpotSerializer(version, ChromXType.Mz);

            using (var stream = new MemoryStream(1000000)) {
                serializer.Serialize(stream, spotInfo);
                stream.Seek(0, SeekOrigin.Begin);
                var result = serializer.GetVersion(stream);
                Assert.AreEqual(version, result);
            }
        }
    }
}
