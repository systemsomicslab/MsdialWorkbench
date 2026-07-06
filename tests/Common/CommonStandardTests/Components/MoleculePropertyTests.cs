using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Interfaces;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Globalization;
using MessagePack.Resolvers;

namespace CompMs.Common.Components.Tests
{
    [TestClass()]
    public class MoleculePropertyTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(GetSerializedMoleculePropertyTestData))]
        public void DeserializeSavedMoleculePropertyBytesTest(string name, string hexBytes) {
            var bytes = HexToBytes(hexBytes);
            var formatter = MoleculePropertyExtension.Formatter;
            var actual = formatter.Deserialize(bytes, 0, StandardResolver.Instance, out var readSize);
            var buffer = new byte[bytes.Length + 32];
            var writeSize = formatter.Serialize(ref buffer, 0, actual, StandardResolver.Instance);

            Assert.AreEqual(bytes.Length, readSize);
            var actualBytes = new byte[writeSize];
            Buffer.BlockCopy(buffer, 0, actualBytes, 0, writeSize);
            CollectionAssert.AreEqual(bytes, actualBytes);
            Assert.AreEqual(name, actual.Name);
        }

        public static IEnumerable<object[]> GetSerializedMoleculePropertyTestData {
            get {
                yield return new object[] { "Glucose", "95A7476C75636F7365DC001BA0CB0000000000000000CB0000000000000000CB00000000000000000000000000000000000000000000000000000000C20080A0A0A0" };
            }
        }

        private static byte[] HexToBytes(string hex) {
            var bytes = new byte[hex.Length / 2];
            for (var i = 0; i < bytes.Length; i++) {
                bytes[i] = byte.Parse(hex.Substring(i * 2, 2), NumberStyles.HexNumber);
            }
            return bytes;
        }
    }
}
