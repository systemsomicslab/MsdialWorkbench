using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
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
            if (value is null)
            {
                writer.WriteNil();
            }
            else
            {
                var newOptions = options.WithCompression(MessagePackCompression.Lz4BlockArray);
                //MessagePackSerializer.Serialize(ref writer, new SerializingDataContainer<T> { Data = value }, newOptions);
                Memory<T> memory = (value as T[]) ?? value.ToArray();
                var length = memory.Length;
                var size = (int)Math.Sqrt(length) + 1;
                var iteration = (length + size - 1) / size;
                for (int i = 0; i < iteration; i++) {
                    MessagePackSerializer.Serialize(ref writer, new SerializingDataContainer<T> { Data = memory.Slice(i * size, Math.Min(size, length - i * size)) }, newOptions);
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
            var resolver = CompositeResolver.Create(
                new[] { DeserializedDataContainer<T>.CreateFormatter(index), },
                new[] { StandardResolver.Instance });
            var newOptions = options.WithCompression(MessagePackCompression.Lz4BlockArray).WithResolver(resolver);
            var deserialized = MessagePackSerializer.Deserialize<DeserializedDataContainer<T>>(ref reader, newOptions);
            if (deserialized.Data is null) {
                result = default;
            }
            else {
                result = deserialized.Data.FirstOrDefault();
            }
            skipArraySize = deserialized.Length;
            return result != null;
        }

        [MessagePackFormatter(typeof(SerializingDataContainer<>.DataContainerFormatter))]
        private sealed class SerializingDataContainer<T> {
            public sbyte VersionCode { get; } = 2;
            //public IReadOnlyList<T> Data { get; set; }
            public Memory<T> Data { get; set; }

            class DataContainerFormatter : IMessagePackFormatter<SerializingDataContainer<T>>
            {
                public SerializingDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    throw new NotSupportedException();
                }

                public void Serialize(ref MessagePackWriter writer, SerializingDataContainer<T> value, MessagePackSerializerOptions options) {
                    writer.WriteInt8(value.VersionCode);
                    //writer.WriteArrayHeader(value.Data.Count);
                    //for (int i = 0; i < value.Data.Count; i++) {
                    //    MessagePackSerializer.Serialize(ref writer, value.Data[i], options);
                    //}
                    writer.WriteArrayHeader(value.Data.Length);
                    for (int i = 0; i < value.Data.Length; i++) {
                        MessagePackSerializer.Serialize(ref writer, value.Data.Span[i], options);
                    }
                }
            }
        }


        [MessagePackFormatter(typeof(DeserializedDataContainer<>.DataContainerFormatter))]
        private sealed class DeserializedDataContainer<T> {
            public List<T> Data { get; set; }
            public int Length { get; set; }

            public static IMessagePackFormatter<DeserializedDataContainer<T>> CreateFormatter(int index) => new SpecificDataFormatter(index);

            class DataContainerFormatter : IMessagePackFormatter<DeserializedDataContainer<T>>
            {
                public DeserializedDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    int length;
                    if (reader.NextCode == MessagePackCode.Int8) {
                        _ = reader.ReadSByte();
                        length = reader.ReadArrayHeader();
                    }
                    else {
                        var reader_ = reader.CreatePeekReader();
                        length = reader_.ReadArrayHeader();
                        reader.ReadRaw(5);
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

            class SpecificDataFormatter : IMessagePackFormatter<DeserializedDataContainer<T>>
            {
                private readonly int _index;

                public SpecificDataFormatter(int index)
                {
                    _index = index;
                }

                public DeserializedDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    int length;
                    if (reader.NextCode == MessagePackCode.Int8) {
                        _ = reader.ReadSByte();
                        length = reader.ReadArrayHeader();
                    }
                    else {
                        var reader_ = reader.CreatePeekReader();
                        length = reader_.ReadArrayHeader();
                        reader.ReadRaw(5);
                    }
                    if (length <= _index) {
                        for (int i = 0; i < length; i++) {
                            reader.Skip();
                        }
                        return new DeserializedDataContainer<T> { Length = length, };
                    }
                    for (int i = 0; i < _index; i++) {
                        reader.Skip();
                    }
                    return new DeserializedDataContainer<T> { Data = new List<T> { MessagePackSerializer.Deserialize<T>(ref reader, options) }, Length = length };
                }

                public void Serialize(ref MessagePackWriter writer, DeserializedDataContainer<T> value, MessagePackSerializerOptions options) {
                    throw new NotSupportedException();
                }
            }
        }
    }
}
