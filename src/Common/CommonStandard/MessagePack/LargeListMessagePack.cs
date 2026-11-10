using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;

namespace CompMs.Common.MessagePack {
    /// <summary>
    /// Writes and reads a list as a sequence of LZ4 chunks in the layout MessagePack 1.x wrote,
    /// so files stay readable by earlier releases.
    /// </summary>
    /// <remarks>
    /// Each chunk is: ext32 header (6 bytes, type 99) + int32 uncompressed length (5 bytes) + LZ4 block.
    /// The uncompressed payload starts with the smallest array header for the chunk's item count,
    /// padded with zeros to 5 bytes, followed by the items.
    /// A null list is a bare nil; an empty list writes nothing.
    /// </remarks>
    public static class LargeListMessagePack {
        public static int OffsetCutoff = 1073741824;
        public const sbyte ExtensionTypeCode = 99;
        public const int HeaderSize = 11;
        private const int ArrayHeaderSize = 5;

        private static readonly MessagePackSerializerOptions CompressionOptions =
            MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(new IMessagePackFormatter[] { new RawPayloadFormatter() }, new IFormatterResolver[] { StandardResolver.Instance }))
                .WithCompression(MessagePackCompression.Lz4Block)
                .WithCompressionMinLength(1);

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value) {
            if (value is null) {
                stream.WriteByte(MessagePackCode.Nil);
                return;
            }

            var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance);
            var formatter = options.Resolver.GetFormatterWithVerify<T>();
            var buffer = new GrowableBuffer();
            var count = 0;
            buffer.Reset();
            for (int i = 0; i < value.Count; i++) {
                var writer = new MessagePackWriter(buffer);
                formatter.Serialize(ref writer, value[i], options);
                writer.Flush();
                count++;
                if (buffer.Length > OffsetCutoff) {
                    WriteChunk(stream, buffer, count);
                    buffer.Reset();
                    count = 0;
                }
            }
            if (count > 0) {
                WriteChunk(stream, buffer, count);
            }
        }

        private static void WriteChunk(Stream stream, GrowableBuffer buffer, int count) {
            buffer.WriteArrayHeaderAtStart(count);
            var compressed = MessagePackSerializer.Serialize(new RawPayload(buffer.Array, buffer.Length), CompressionOptions);

            // MessagePack 3 writes the smallest ext header; earlier releases read a fixed ext32 + int32 header.
            var reader = new MessagePackReader(compressed);
            var header = reader.ReadExtensionFormatHeader();
            if (header.TypeCode != ExtensionTypeCode) {
                throw new InvalidOperationException($"Unexpected extension type {header.TypeCode} while compressing a large list.");
            }
            var uncompressedLength = reader.ReadInt32();
            var lz4Offset = (int)reader.Consumed;
            var lz4Length = compressed.Length - lz4Offset;

            var head = new byte[HeaderSize];
            head[0] = MessagePackCode.Ext32;
            WriteBigEndian(head, 1, lz4Length + 5);
            head[5] = unchecked((byte)ExtensionTypeCode);
            head[6] = MessagePackCode.Int32;
            WriteBigEndian(head, 7, uncompressedLength);
            stream.Write(head, 0, head.Length);
            stream.Write(compressed, lz4Offset, lz4Length);
        }

        private static void WriteBigEndian(byte[] bytes, int offset, int value) {
            bytes[offset] = (byte)(value >> 24);
            bytes[offset + 1] = (byte)(value >> 16);
            bytes[offset + 2] = (byte)(value >> 8);
            bytes[offset + 3] = (byte)value;
        }

        public static List<T> Deserialize<T>(Stream stream) {
            var res = new List<T>();
            foreach (var chunk in DeserializeIncremental<T>(stream)) {
                res.AddRange(chunk);
            }
            return res;
        }

        public static IEnumerable<List<T>> DeserializeIncremental<T>(Stream stream) {
            var options = MessagePackSerializerOptions.Standard
                .WithResolver(CompositeResolver.Create(new IMessagePackFormatter[] { new ChunkFormatter<T>(index: null) }, new IFormatterResolver[] { StandardResolver.Instance }))
                .WithCompression(MessagePackCompression.Lz4Block);
            while (TryReadChunk(stream, out var chunk)) {
                yield return chunk is null ? new List<T>() : MessagePackSerializer.Deserialize<Chunk<T>>(chunk, options).Items;
            }
        }

        public static T DeserializeAt<T>(Stream stream, int index) {
            while (TryReadChunk(stream, out var chunk)) {
                if (chunk is null) {
                    continue;
                }
                var options = MessagePackSerializerOptions.Standard
                    .WithResolver(CompositeResolver.Create(new IMessagePackFormatter[] { new ChunkFormatter<T>(index) }, new IFormatterResolver[] { StandardResolver.Instance }))
                    .WithCompression(MessagePackCompression.Lz4Block);
                var result = MessagePackSerializer.Deserialize<Chunk<T>>(chunk, options);
                if (result.Items.Count > 0) {
                    return result.Items[0];
                }
                index -= result.Count;
            }
            return default;
        }

        /// <summary>
        /// Reads one chunk (header and LZ4 body) from the stream.
        /// Returns false when the stream ends before a whole chunk; <paramref name="chunk"/> is null when the header is not an LZ4 chunk.
        /// </summary>
        private static bool TryReadChunk(Stream stream, out byte[]? chunk) {
            chunk = null;
            var head = new byte[HeaderSize];
            if (!FillFromStream(stream, head, 0, HeaderSize)) {
                return false;
            }
            // The header is always ext32 (6 bytes) + int32 (5 bytes); the body follows in the stream.
            if (head[0] != MessagePackCode.Ext32 || unchecked((sbyte)head[5]) != ExtensionTypeCode) {
                return true;
            }
            var extLength = (head[1] << 24) | (head[2] << 16) | (head[3] << 8) | head[4];
            var bodyLength = extLength - 5;
            if (bodyLength < 0) {
                return true;
            }
            var bytes = new byte[HeaderSize + bodyLength];
            Buffer.BlockCopy(head, 0, bytes, 0, HeaderSize);
            if (!FillFromStream(stream, bytes, HeaderSize, bodyLength)) {
                return false;
            }
            chunk = bytes;
            return true;
        }

        private static bool FillFromStream(Stream input, byte[] buffer, int offset, int readSize) {
            int length = 0;
            while (length < readSize) {
                var read = input.Read(buffer, offset + length, readSize - length);
                if (read <= 0) {
                    break;
                }
                length += read;
            }
            return length == readSize;
        }

        private sealed class Chunk<T> {
            public Chunk(List<T> items, int count) {
                Items = items;
                Count = count;
            }

            public List<T> Items { get; }
            public int Count { get; }
        }

#pragma warning disable MsgPack009, MsgPack010
        private sealed class ChunkFormatter<T> : IMessagePackFormatter<Chunk<T>> {
            private readonly int? _index;

            public ChunkFormatter(int? index) {
                _index = index;
            }

            public Chunk<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                var peek = reader.CreatePeekReader();
                var count = peek.ReadArrayHeader();
                reader.ReadRaw(ArrayHeaderSize);

                var formatter = options.Resolver.GetFormatterWithVerify<T>();
                if (_index is int index) {
                    if (index >= count) {
                        return new Chunk<T>(new List<T>(0), count);
                    }
                    for (int i = 0; i < index; i++) {
                        reader.Skip();
                    }
                    return new Chunk<T>(new List<T> { formatter.Deserialize(ref reader, options) }, count);
                }

                var items = new List<T>(count);
                for (int i = 0; i < count; i++) {
                    items.Add(formatter.Deserialize(ref reader, options));
                }
                return new Chunk<T>(items, count);
            }

            public void Serialize(ref MessagePackWriter writer, Chunk<T> value, MessagePackSerializerOptions options) {
                throw new NotSupportedException();
            }
        }

        private readonly struct RawPayload {
            public RawPayload(byte[] array, int length) {
                Array = array;
                Length = length;
            }

            public byte[] Array { get; }
            public int Length { get; }
        }

        private sealed class RawPayloadFormatter : IMessagePackFormatter<RawPayload> {
            public RawPayload Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                throw new NotSupportedException();
            }

            public void Serialize(ref MessagePackWriter writer, RawPayload value, MessagePackSerializerOptions options) {
                writer.WriteRaw(new ReadOnlySpan<byte>(value.Array, 0, value.Length));
            }
        }
#pragma warning restore MsgPack009, MsgPack010

        /// <summary>
        /// Holds one chunk's uncompressed payload, with 5 bytes reserved at the start for the array header.
        /// </summary>
        private sealed class GrowableBuffer : IBufferWriter<byte> {
            private byte[] _array = new byte[65536];

            public byte[] Array => _array;
            public int Length { get; private set; }

            public void Reset() {
                Length = ArrayHeaderSize;
            }

            public void WriteArrayHeaderAtStart(int count) {
                var header = new ArrayBufferWriterLite(ArrayHeaderSize);
                var writer = new MessagePackWriter(header);
                writer.WriteArrayHeader(count);
                writer.Flush();
                System.Array.Clear(_array, 0, ArrayHeaderSize);
                Buffer.BlockCopy(header.Array, 0, _array, 0, header.Length);
            }

            public void Advance(int count) {
                Length += count;
            }

            public Memory<byte> GetMemory(int sizeHint = 0) {
                Ensure(sizeHint);
                return new Memory<byte>(_array, Length, _array.Length - Length);
            }

            public Span<byte> GetSpan(int sizeHint = 0) {
                Ensure(sizeHint);
                return new Span<byte>(_array, Length, _array.Length - Length);
            }

            private void Ensure(int sizeHint) {
                if (sizeHint < 1) {
                    sizeHint = 1;
                }
                if (_array.Length - Length >= sizeHint) {
                    return;
                }
                var newSize = Math.Max((long)_array.Length * 2, (long)Length + sizeHint);
                if (newSize > int.MaxValue) {
                    newSize = Math.Max(int.MaxValue - 64, (long)Length + sizeHint);
                }
                System.Array.Resize(ref _array, (int)newSize);
            }
        }

        private sealed class ArrayBufferWriterLite : IBufferWriter<byte> {
            public ArrayBufferWriterLite(int capacity) {
                Array = new byte[capacity];
            }

            public byte[] Array { get; private set; }
            public int Length { get; private set; }

            public void Advance(int count) => Length += count;

            public Memory<byte> GetMemory(int sizeHint = 0) {
                Ensure(sizeHint);
                return new Memory<byte>(Array, Length, Array.Length - Length);
            }

            public Span<byte> GetSpan(int sizeHint = 0) {
                Ensure(sizeHint);
                return new Span<byte>(Array, Length, Array.Length - Length);
            }

            private void Ensure(int sizeHint) {
                if (Array.Length - Length < Math.Max(sizeHint, 1)) {
                    var array = Array;
                    System.Array.Resize(ref array, Math.Max(Array.Length * 2, Length + Math.Max(sizeHint, 1)));
                    Array = array;
                }
            }
        }
    }
}
