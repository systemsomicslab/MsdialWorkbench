using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;

namespace CompMs.MsdialCore
{
    /// <summary>
    /// The handful of MessagePack 1.x byte-level helpers the compatibility tests use to cut and splice payloads,
    /// rebuilt on the MessagePack 3 reader and writer so those tests keep their original wording.
    /// </summary>
    internal static class MessagePackBinary
    {
        public static int ReadArrayHeader(byte[] bytes, int offset, out int readSize) {
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset, bytes.Length - offset));
            var count = reader.ReadArrayHeader();
            readSize = (int)reader.Consumed;
            return count;
        }

        public static int ReadNextBlock(byte[] bytes, int offset) {
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset, bytes.Length - offset));
            reader.Skip();
            return (int)reader.Consumed;
        }

        public static int ReadInt32(byte[] bytes, int offset, out int readSize) {
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset, bytes.Length - offset));
            var value = reader.ReadInt32();
            readSize = (int)reader.Consumed;
            return value;
        }

        public static MessagePackType GetMessagePackType(byte[] bytes, int offset) {
            var reader = new MessagePackReader(new ReadOnlyMemory<byte>(bytes, offset, bytes.Length - offset));
            return reader.NextMessagePackType;
        }

        public static int WriteArrayHeader(ref byte[] bytes, int offset, int count) {
            var buffer = new System.Buffers.ArrayBufferWriter<byte>();
            var writer = new MessagePackWriter(buffer);
            writer.WriteArrayHeader(count);
            writer.Flush();
            return WriteRaw(ref bytes, offset, buffer.WrittenSpan);
        }

        public static int WriteNil(ref byte[] bytes, int offset) {
            return WriteRaw(ref bytes, offset, new[] { MessagePackCode.Nil });
        }

        public static void EnsureCapacity(ref byte[] bytes, int offset, int appendLength) {
            var required = offset + appendLength;
            if (bytes.Length < required) {
                Array.Resize(ref bytes, Math.Max(required, bytes.Length * 2));
            }
        }

        public static void FastResize(ref byte[] bytes, int newSize) {
            Array.Resize(ref bytes, newSize);
        }

        private static int WriteRaw(ref byte[] bytes, int offset, ReadOnlySpan<byte> data) {
            EnsureCapacity(ref bytes, offset, data.Length);
            data.CopyTo(new Span<byte>(bytes, offset, data.Length));
            return data.Length;
        }
    }

    /// <summary>
    /// MessagePack 1.x's LZ4 entry points, on MessagePack 3's Lz4Block compression (the same ext 99 layout).
    /// </summary>
    internal static class LZ4MessagePackSerializer
    {
        private static readonly MessagePackSerializerOptions Options =
            MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance).WithCompression(MessagePackCompression.Lz4Block);

        public static T Deserialize<T>(byte[] bytes) {
            return MessagePackSerializer.Deserialize<T>(bytes, Options);
        }

        public static byte[] Decode(byte[] bytes) {
            var options = Options.WithResolver(CompositeResolver.Create(new IMessagePackFormatter[] { new RawFormatter() }, new IFormatterResolver[] { StandardResolver.Instance }));
            return MessagePackSerializer.Deserialize<Raw>(bytes, options).Bytes;
        }

        private sealed class Raw
        {
            public byte[] Bytes { get; set; } = Array.Empty<byte>();
        }

        private sealed class RawFormatter : IMessagePackFormatter<Raw>
        {
            public Raw Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                return new Raw { Bytes = System.Buffers.BuffersExtensions.ToArray(reader.ReadRaw()), };
            }

            public void Serialize(ref MessagePackWriter writer, Raw value, MessagePackSerializerOptions options) {
                throw new NotSupportedException();
            }
        }
    }
}
