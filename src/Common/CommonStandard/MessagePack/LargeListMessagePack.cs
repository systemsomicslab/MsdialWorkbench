using MessagePack;
using MessagePack.LZ4;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.Common.MessagePack {
    public static class LargeListMessagePack
    {
        public static int OffsetCutoff = 1073741824;
        public const sbyte ExtensionTypeCode = 99;
        public const int NotCompressionSize = 64;
        public const int HeaderSize = 11;
        // Four times the molecule-library writer's target; historical giant blocks keep per-block allocations.
        public const int MaxReusableBufferSize = 64 * 1024 * 1024;

        private static byte[] GetBuffer()
        {
            return new byte[65536];
            //if (buffer is null)
            //{
            //    buffer = new byte[65536];
            //}
            //return buffer;
        }

        private static byte[] GetBufferLZ4()
        {
            return new byte[65536];
            //if (bufferLz is null)
            //{
            //    bufferLz = new byte[65536];
            //}
            //return bufferLz;
        }

        public static IFormatterResolver DefaultResolver {
            get {
                return StandardResolver.Instance;
            }
        }

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value, IFormatterResolver resolver = null)
            => Serialize(stream, value, resolver, OffsetCutoff);

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value, IFormatterResolver resolver, int blockSize)
        {
            if (blockSize <= 0) { throw new ArgumentOutOfRangeException(nameof(blockSize)); }
            if (resolver == null) resolver = DefaultResolver;
            var bytes = GetBuffer();
            var offset = 0;
            if (value == null)
            {
                offset += MessagePackBinary.WriteNil(ref bytes, offset);
                stream.Write(bytes, 0, offset);
            }
            else
            {
                var formatter = resolver.GetFormatterWithVerify<T>();
                var startOffSet = offset;
                var c = value.Count;
                offset = 5;
                var lastCounter = -1;
                var bufferLz4 = GetBufferLZ4();
                for (int i = 0; i < c; i++)
                {
                    offset += formatter.Serialize(ref bytes, offset, value[i], resolver);
                    if (offset > blockSize)
                    {
                        MessagePackBinary.WriteArrayHeader(ref bytes, startOffSet, i - lastCounter);
                        lastCounter = i;
                        bufferLz4 = ToLZ4Binary(new ArraySegment<byte>(bytes, 0, offset));
                        stream.Write(bufferLz4, 0, bufferLz4.Length);
                        offset = 5; // size of MessagePackBinary.WriteArrayHeader
                        bytes = GetBuffer();
                    }
                }
                if (lastCounter < c - 1)
                {
                    MessagePackBinary.WriteArrayHeader(ref bytes, startOffSet, c - lastCounter - 1);
                    bufferLz4 = ToLZ4Binary(new ArraySegment<byte>(bytes, 0, offset));
                    stream.Write(bufferLz4, startOffSet, bufferLz4.Length);
                }
            }
        }

        public static byte[] ToLZ4Binary(ArraySegment<byte> messagePackBinary)
        {
            var buffer = ToLZ4BinaryCore(messagePackBinary);
            return MessagePackBinary.FastCloneWithResize(buffer.Array, buffer.Count);
        }


        static ArraySegment<byte> ToLZ4BinaryCore(ArraySegment<byte> serializedData)
        {
            //if (serializedData.Count < NotCompressionSize)
            //{
            //    // This data couldn't decode.
            //    return serializedData;
            //}
            //else
            //{
                var offset = 0;
                var buffer = GetBufferLZ4();
                var maxOutCount = LZ4Codec.MaximumOutputLength(serializedData.Count);
                if (buffer.Length < 6 + 5 + maxOutCount) // (ext header size + fixed length size)
                {
                    buffer = new byte[6 + 5 + maxOutCount];
                }

                // acquire ext header position
                var extHeaderOffset = offset;
                offset += (6 + 5);

                // write body
                var lz4Length = LZ4Codec.Encode(serializedData.Array, serializedData.Offset, serializedData.Count, buffer, offset, buffer.Length - offset);
                // Console.WriteLine("lz4Length" + lz4Length);
                // write extension header(always 6 bytes)
                extHeaderOffset += MessagePackBinary.WriteExtensionFormatHeaderForceExt32Block(ref buffer, extHeaderOffset, (sbyte)ExtensionTypeCode, lz4Length + 5);

                // write length(always 5 bytes)
                MessagePackBinary.WriteInt32ForceInt32Block(ref buffer, extHeaderOffset, serializedData.Count);

                return new ArraySegment<byte>(buffer, 0, 6 + 5 + lz4Length);
            //}
        }

        public static List<T> Deserialize<T>(Stream stream, IFormatterResolver resolver = null)
        {
            return DeserializeCore<T>(stream, resolver);
        }

        /// <summary>Reuse buffers only when formatters create owned values without input-byte aliases.</summary>
        public static List<T> Deserialize<T>(Stream stream, IFormatterResolver resolver, bool reuseBuffers)
        {
            return DeserializeCore<T>(stream, resolver, reuseBuffers);
        }

        public static IEnumerable<List<T>> DeserializeIncremental<T>(Stream stream, IFormatterResolver resolver = null) {
            return DeserializeIncrementalCore<T>(stream, resolver);
        }

        static int FillFromStream(Stream input, ref byte[] buffer, int offset, int readSize)
        {
            int length = 0;
            while (length < readSize)
            {
                var read = input.Read(buffer, offset + length, readSize - length);
                if (read <= 0)
                {
                    break;
                }
                length += read;
            }
            return length;
        }


        static List<T> DeserializeCore<T>(Stream stream, IFormatterResolver resolver, bool reuseBuffers = false)
        {
            var buffer = GetBuffer();
            var decodedBuffer = Array.Empty<byte>();
            var res = new List<T>();
            int headerLength;
            var hasBlocks = false;
            // HeaderSize: extension header(always 6 bytes) + length(always 5 bytes) = 11
            while ((headerLength = FillFromStream(stream, ref buffer, 0, HeaderSize)) == HeaderSize)
            {
                hasBlocks = true;
                var tmp = DeserializeEach<T>(stream, buffer, resolver, ref decodedBuffer, reuseBuffers);
                if (tmp != null && tmp.Count > 0)
                {
                    AddList(res, tmp);
                }
            }
            // A null list is written as a lone nil rather than a compressed block.
            if (reuseBuffers && headerLength != 0 && !(!hasBlocks && headerLength == 1 && buffer[0] == 0xc0)) {
                throw new EndOfStreamException("Incomplete MessagePack block header.");
            }
            return res;
        }

        private static IEnumerable<List<T>> DeserializeIncrementalCore<T>(Stream stream, IFormatterResolver resolver) {
            var buffer = GetBuffer();
            // HeaderSize: extension header(always 6 bytes) + length(always 5 bytes) = 11
            while (FillFromStream(stream, ref buffer, 0, HeaderSize) == HeaderSize)
            {
                yield return DeserializeEach<T>(stream, buffer, resolver);
            }
        }

        static void AddList<T>(List<T> original, List<T> tmp)
        {
            foreach (var t in tmp)
            {
                original.Add(t);
            }
        }

        static List<T> DeserializeEach<T>(Stream stream, byte[] buffer, IFormatterResolver resolver)
        {
            var decodedBuffer = Array.Empty<byte>();
            return DeserializeEach<T>(stream, buffer, resolver, ref decodedBuffer, false);
        }

        static List<T> DeserializeEach<T>(Stream stream, byte[] buffer, IFormatterResolver resolver,
            ref byte[] decodedBuffer, bool reuseBuffers)
        {
            var bytes = new ArraySegment<byte>(buffer, 0, HeaderSize);
            int readSize;
            // Console.WriteLine("MessagePackType: " + MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset));
            if (MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes.Array, bytes.Offset, out readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = bytes.Offset + readSize;
                    var length = MessagePackBinary.ReadInt32(bytes.Array, offset, out readSize);
                    offset += readSize;
                    if (reuseBuffers && (offset != HeaderSize || length < 5 || header.Length <= 5 || header.Length - 5L > int.MaxValue)) {
                        throw new InvalidOperationException("Invalid MessagePack block header.");
                    }
                    int bufferLength = (int)header.Length - 5;
                    var compressedBuffer = new byte[bufferLength];

                    if (FillFromStream(stream, ref compressedBuffer, 0, bufferLength) == bufferLength)
                    {
                        bytes = new ArraySegment<byte>(compressedBuffer, 0, bufferLength);
                        offset = 0;
                        // LZ4 Decode
                        var len = bytes.Count;
                        byte[] bufferLz4;
                        if (reuseBuffers && length <= MaxReusableBufferSize) {
                            if (decodedBuffer.Length < length) { decodedBuffer = new byte[BlockCapacity(length)]; }
                            bufferLz4 = decodedBuffer;
                        }
                        else {
                            bufferLz4 = new byte[length];
                        }
                        var decoded = LZ4Codec.Decode(bytes.Array, bytes.Offset, len, bufferLz4, 0, length);
                        // Encoded bytes need not remain rooted while the reference objects are being constructed.
                        bytes = default;
                        compressedBuffer = null;
                        if (reuseBuffers && decoded != length) { throw new InvalidOperationException("Unexpected decompressed block length."); }
                        var result = DeserializeList<T>(bufferLz4, offset, resolver, out readSize, reuseBuffers ? (int?)length : null);
                        if (reuseBuffers && readSize != length) { throw new InvalidOperationException("Unexpected MessagePack block length."); }
                        return result;
                    }
                    if (reuseBuffers) { throw new EndOfStreamException("Incomplete compressed MessagePack block."); }
                }
            }
            // Console.WriteLine("Not working well");
            if (reuseBuffers) { throw new InvalidOperationException("Unsupported MessagePack block header."); }
            return new List<T>();
        }

        private static int BlockCapacity(int length) => (int)Math.Min(int.MaxValue, ((long)length + 65535) / 65536 * 65536);
        static List<T> DeserializeList<T>(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize, int? dataLength = null)
        {
            if (formatterResolver == null) formatterResolver = DefaultResolver;
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                readSize = 1;
                return null;
            }
            else
            {
                var startOffset = 0;
                var formatter = formatterResolver.GetFormatterWithVerify<T>();
                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out readSize);
                offset = 5;
                if (dataLength.HasValue && len > dataLength.Value - offset) { throw new InvalidOperationException("Invalid MessagePack record count."); }
                var list = new List<T>(len);
                for (int i = 0; i < len; i++)
                {
                    list.Add(formatter.Deserialize(bytes, offset, formatterResolver, out readSize));
                    offset += readSize;
                }
                readSize = offset - startOffset;
                return list;
            }
        }

        public static T DeserializeAt<T>(Stream stream, int index, IFormatterResolver resolver = null)
        {
            var buffer = GetBuffer();
            // HeaderSize: extension header(always 6 bytes) + length(always 5 bytes) = 11
            while (FillFromStream(stream, ref buffer, 0, HeaderSize) == HeaderSize)
            {
                var success = TryDeserializeAtOrSkip<T>(stream, buffer, resolver, index, out var result, out int skipArraySize);
                if (success) {
                    return result;
                }
                index -= skipArraySize;
            }
            return default;
        }

        static bool TryDeserializeAtOrSkip<T>(Stream stream, byte[] buffer, IFormatterResolver resolver, int index, out T result, out int skipArraySize)
        {
            var bytes = new ArraySegment<byte>(buffer, 0, HeaderSize);
            if (MessagePackBinary.GetMessagePackType(bytes.Array, bytes.Offset) == MessagePackType.Extension)
            {
                var header = MessagePackBinary.ReadExtensionFormatHeader(bytes.Array, bytes.Offset, out int readSize);
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var offset = bytes.Offset + readSize;
                    var length = MessagePackBinary.ReadInt32(bytes.Array, offset, out readSize);
                    offset += readSize;
                    int bufferLength = (int)header.Length - 5;
                    buffer = GetBuffer(); // use LZ4 Pool

                    if (buffer.Length < bufferLength)
                    {
                        buffer = new byte[bufferLength];
                    }

                    if (FillFromStream(stream, ref buffer, 0, bufferLength) == bufferLength)
                    {
                        bytes = new ArraySegment<byte>(buffer, 0, bufferLength);
                        offset = 0;
                        // LZ4 Decode
                        var len = bytes.Count;
                        var bufferLz4 = new byte[length];
                        LZ4Codec.Decode(bytes.Array, bytes.Offset, len, bufferLz4, 0, length);
                        var success = TryDeserializeAt(bufferLz4, offset, resolver, index, out result, out skipArraySize);
                        if (success) {
                            return true;
                        }
                    }
                }
            }
            result = default;
            skipArraySize = 0;
            return false;
        }

        private static bool TryDeserializeAt<T>(byte[] bytes, int offset, IFormatterResolver formatterResolver, int index, out T result, out int skipArraySize) {
            if (formatterResolver == null) formatterResolver = DefaultResolver;
            if (MessagePackBinary.IsNil(bytes, offset))
            {
                skipArraySize = 1;
                result = default;
                return index == 0;
            }
            else
            {
                var formatter = formatterResolver.GetFormatterWithVerify<T>();
                var len = MessagePackBinary.ReadArrayHeader(bytes, offset, out var readSize);
                skipArraySize = len;
                offset += 5;
                if (len > index) {
                    for (int i = 0; i < index; i++)
                    {
                        offset += MessagePackBinary.ReadNextBlock(bytes, offset);
                    }
                    result = formatter.Deserialize(bytes, offset, formatterResolver, out int tmpReadSize);
                    return true;
                }
                result = default;
                return false;
            }
        }
    }
}
