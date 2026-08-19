using CompMs.Common.Interfaces;
using MessagePack;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Globalization;

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
            MessagePackReader reader = new MessagePackReader(bytes);
            var actual = formatter.Deserialize(ref reader,  MessagePackSerializerOptions.Standard);

            var arraywriter = new TestBufferWriter();
            var writer = new MessagePackWriter(arraywriter);
            formatter.Serialize(ref writer, actual, MessagePackSerializerOptions.Standard);
            writer.Flush();

            Assert.AreEqual(bytes.Length, reader.Consumed);
            CollectionAssert.AreEqual(bytes, arraywriter.WrittenMemory.ToArray());
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

        sealed class TestBufferWriter : IBufferWriter<byte>
        {
            private byte[] _buffer = new byte[256];
            private int _written;

            public int WrittenCount => _written;

            public void Advance(int count) => _written += count;

            public Memory<byte> GetMemory(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return _buffer.AsMemory(_written);
            }

            public Span<byte> GetSpan(int sizeHint = 0)
            {
                Ensure(sizeHint);
                return _buffer.AsSpan(_written);
            }

            public ReadOnlyMemory<byte> WrittenMemory => _buffer.AsMemory(0, _written);

            private void Ensure(int sizeHint)
            {
                sizeHint = Math.Max(sizeHint, 1);

                if (_buffer.Length - _written >= sizeHint)
                    return;

                Array.Resize(ref _buffer,
                    Math.Max(_buffer.Length * 2, _written + sizeHint));
            }
        }
    }
}
