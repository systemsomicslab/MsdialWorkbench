using MessagePack;
using MessagePack.Formatters;
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
                var newOptions = options.WithCompression(MessagePackCompression.Lz4BlockArray);
                MessagePackSerializer.Serialize(ref writer, new SerializingDataContainer<T> { Data = value }, newOptions);
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
            if (!reader.End)
            {
                return MessagePackSerializer.Deserialize<DeserializedDataContainer<T>>(ref reader, options.WithCompression(MessagePackCompression.Lz4BlockArray)).Data;
            }
            return new List<T>();
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
            var newOptions = options.WithCompression(MessagePackCompression.Lz4BlockArray);
            if (!reader.End)
            {
                var results = MessagePackSerializer.Deserialize<DeserializedDataContainer<T>>(ref reader, newOptions);
                skipArraySize = results.Length;
                if (skipArraySize > index) {
                    result = results.Data[index];
                    return true;
                }
            }
            result = default;
            skipArraySize = 0;
            return false;
        }

        [MessagePackFormatter(typeof(SerializingDataContainer<>.DataContainerFormatter))]
        internal sealed class SerializingDataContainer<T> {
            public IReadOnlyList<T> Data { get; set; }

            class DataContainerFormatter : IMessagePackFormatter<SerializingDataContainer<T>>
            {
                public SerializingDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    throw new NotSupportedException();
                }

                public void Serialize(ref MessagePackWriter writer, SerializingDataContainer<T> value, MessagePackSerializerOptions options) {
                    writer.WriteInt32(value.Data.Count);
                    for (int i = 0; i < value.Data.Count; i++) {
                        MessagePackSerializer.Serialize(ref writer, value.Data[i], options);
                    }
                }
            }
        }


        [MessagePackFormatter(typeof(DeserializedDataContainer<>.DataContainerFormatter))]
        internal sealed class DeserializedDataContainer<T> {
            public int Length { get; set; }
            public List<T> Data { get; set; }

            class DataContainerFormatter : IMessagePackFormatter<DeserializedDataContainer<T>>
            {
                public DeserializedDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    int length;
                    if (reader.NextMessagePackType == MessagePackType.Integer) {
                        length = reader.ReadInt32(); // length
                    }
                    else if (reader.NextMessagePackType == MessagePackType.Array) {
                        var reader_ = reader.CreatePeekReader();
                        length = reader_.ReadArrayHeader();
                        reader.ReadRaw(5);
                    }
                    else {
                        throw new NotSupportedException($"Unknown MessagePackType: {reader.NextMessagePackType}");
                    }
                    var data = new List<T>(length);
                    for (int i = 0; i < length; i++) {
                        data.Add(MessagePackSerializer.Deserialize<T>(ref reader, options));
                    }
                    return new DeserializedDataContainer<T> { Length = length, Data = data };
                }

                public void Serialize(ref MessagePackWriter writer, DeserializedDataContainer<T> value, MessagePackSerializerOptions options) {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
