using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Pipelines;
using System.Linq;

namespace CompMs.Common.MessagePack
{
    public static class LargeListMessagePack
    {
        public const sbyte ExtensionTypeCode = 99;

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value) {
            var pipeWriter = PipeWriter.Create(stream, new StreamPipeWriterOptions(leaveOpen: true));
            var writer = new MessagePackWriter(pipeWriter);
            Serialize(ref writer, value, MessagePackSerializerOptions.Standard);
            writer.Flush();
            pipeWriter.Complete();
        }

        public static void Serialize<T>(ref MessagePackWriter writer, IReadOnlyList<T> value, MessagePackSerializerOptions options)
        {
            if (value == null)
            {
                writer.WriteNil();
            }
            else
            {
                using (var memory = new MemoryStream()) {
                    var pipeWriter = PipeWriter.Create(memory);
                    var writer_ = writer.Clone(pipeWriter);
                    var newOptions = options.WithCompression(MessagePackCompression.Lz4Block).WithCompressionMinLength(1);
                    MessagePackSerializer.Serialize(ref writer_, value, newOptions);
                    writer_.Flush();
                    pipeWriter.Complete();

                    writer.WriteExtensionFormat(new ExtensionResult(ExtensionTypeCode, memory.ToArray()));
                }
            }
        }

        public static List<T> Deserialize<T>(Stream stream)
        {
            using (var memory = new MemoryStream()) {
                stream.CopyTo(memory);
                var reader = new MessagePackReader(new ReadOnlyMemory<byte>(memory.GetBuffer(), 0, (int)memory.Length));
                return DeserializeCore<T>(ref reader, MessagePackSerializerOptions.Standard);
            }
        }

        public static IEnumerable<List<T>> DeserializeIncremental<T>(Stream stream) {
            using (var memory = new MemoryStream()) {
                stream.CopyTo(memory);
                var reader = new MessagePackReader(new ReadOnlyMemory<byte>(memory.GetBuffer(), 0, (int)memory.Length));
                foreach (var result in DeserializeIncrementalCore<T>(ref reader, MessagePackSerializerOptions.Standard)) {
                    yield return result;
                }
            }
        }

        public static IEnumerable<List<T>> DeserializeIncremental<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            return DeserializeIncrementalCore<T>(ref reader, options);
        }

        private static List<T> DeserializeCore<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            var res = new List<List<T>>();
            // HeaderSize: extension header(always 6 bytes) + length(always 5 bytes) = 11
            while (!reader.End) {
                var tmp = DeserializeEach<T>(ref reader, options);
                if (tmp != null && tmp.Count > 0) {
                    res.Add(tmp);
                }
            }
            return res.SelectMany(r => r).ToList();
        }

        private static IEnumerable<List<T>> DeserializeIncrementalCore<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            var results = new List<List<T>>();
            while (!reader.End)
            {
                results.Add(DeserializeEach<T>(ref reader, options));
            }
            return results;
        }

        private static List<T> DeserializeEach<T>(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.NextMessagePackType == MessagePackType.Extension)
            {
                var header = reader.ReadExtensionFormatHeader();
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    //var _length = reader.ReadInt32();
                    if (!reader.End)
                    {
                        // LZ4 Decode
                        var newOptions = options.WithCompression(MessagePackCompression.Lz4Block).WithCompressionMinLength(1);
                        return DeserializeList<T>(ref reader, newOptions);
                    }
                }
            }
            return new List<T>();
        }

        private static List<T> DeserializeList<T>(ref MessagePackReader reader, MessagePackSerializerOptions options)
        {
            if (reader.IsNil)
            {
                return null;
            }
            else
            {
                return MessagePackSerializer.Deserialize<List<T>>(ref reader, options);
            }
        }

        public static T DeserializeAt<T>(Stream stream, int index) {
            using (var memory = new MemoryStream()) {
                stream.CopyTo(memory);
                var reader = new MessagePackReader(new ReadOnlyMemory<byte>(memory.GetBuffer(), 0, (int)memory.Length));
                return DeserializeAt<T>(ref reader, MessagePackSerializerOptions.Standard, index);
            }
        }

        public static T DeserializeAt<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, int index)
        {
            while (!reader.End)
            {
                var success = TryDeserializeAtOrSkip<T>(ref reader, options, index, out var result, out int skipArraySize);
                if (success) {
                    return result;
                }
                index -= skipArraySize;
            }
            return default;
        }

        private static bool TryDeserializeAtOrSkip<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, int index, out T result, out int skipArraySize)
        {
            if (reader.NextMessagePackType == MessagePackType.Extension)
            {
                var header = reader.ReadExtensionFormatHeader();
                if (header.TypeCode == ExtensionTypeCode)
                {
                    // decode lz4
                    var newOptions = options.WithCompression(MessagePackCompression.Lz4Block).WithCompressionMinLength(1);
                    if (!reader.End)
                    {
                        // LZ4 Decode
                        var results = MessagePackSerializer.Deserialize<T[]>(ref reader, newOptions);
                        skipArraySize = results.Length;
                        if (skipArraySize > index) {
                            result = results[index];
                            return true;
                        }
                    }
                }
            }
            result = default;
            skipArraySize = 0;
            return false;
        }
    }
}
