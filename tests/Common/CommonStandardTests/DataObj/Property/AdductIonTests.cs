using CompMs.Common.MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.IO;
using System.Globalization;

namespace CompMs.Common.DataObj.Property.Tests
{
    [TestClass()]
    public class AdductIonTests
    {
        [DataTestMethod()]
        [DynamicData(nameof(GetAdductIonTestData))]
        public void GetAdductIonTest(string adductName) {
            var adductIon = AdductIon.GetAdductIon(adductName);
            var newAdduct = AdductIon.GetAdductIon(adductIon.AdductIonName);
            Assert.AreEqual(adductIon, newAdduct);
        }

        public static IEnumerable<object[]> GetAdductIonTestData {
            get {
                yield return new object[] { "[M+H]+", };
                yield return new object[] { "[M-H]-", };
                yield return new object[] { "[M+2H]2+", };
                yield return new object[] { "[M+Na]+", };
            }
        }

        [DataTestMethod()]
        [DynamicData(nameof(GetAdductIonTestData))]
        public void GetAdductIonSerializationTest(string adductName) {
            var adductIon = AdductIon.GetAdductIon(adductName);
            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(adductIon, memory);
            memory.Position = 0;
            var newAdduct = MessagePackDefaultHandler.LoadFromStream<AdductIon>(memory);
            Assert.AreEqual(adductIon, newAdduct);
        }

        [DataTestMethod()]
        public void SerializeNullAdductTest() {
            var adductIon = (AdductIon)null;
            var memory = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(adductIon, memory);
            memory.Position = 0;
            var newAdduct = MessagePackDefaultHandler.LoadFromStream<AdductIon>(memory);
            Assert.AreEqual(AdductIon.Default, newAdduct);
        }

        [DataTestMethod()]
        [DynamicData(nameof(GetSerializedAdductIonTestData))]
        public void DeserializeSavedAdductBytesTest(string adductName, string hexBytes) {
            var bytes = HexToBytes(hexBytes);
            var actual = MessagePackDefaultHandler.LoadFromStream<AdductIon>(new MemoryStream(bytes));
            var roundTrip = new MemoryStream();
            MessagePackDefaultHandler.SaveToStream(actual, roundTrip);

            CollectionAssert.AreEqual(bytes, roundTrip.ToArray());

            Assert.AreEqual(AdductIon.GetAdductIon(adductName), actual);
        }

        public static IEnumerable<object[]> GetSerializedAdductIonTestData {
            get {
                yield return new object[] { "[M+H]+", "9ACB3FF0200D240D4AF601A65B4D2B485D2B0100C3CB3F1E26644B61B0D3CB0000000000000000C2C2" };
                yield return new object[] { "[M-H]-", "9ACBBFF0200D240D4AF601A65B4D2D485D2D0101C3CBBF1E26644B61B0D3CB0000000000000000C2C2" };
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