using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.Common.MessagePack {
    public static class LargeListMessagePack {
        public const sbyte ExtensionTypeCode = 99;

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value) {
            if (value is null) {
                MessagePackSerializer.Serialize(stream, value, MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance).WithCompression(MessagePackCompression.Lz4BlockArray));
                return;
            }

            var options = MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance).WithCompression(MessagePackCompression.Lz4BlockArray);
            Memory<T> memory = (value as T[]) ?? value.ToArray();
            var length = memory.Length;
            var size = (int)Math.Sqrt(length) + 1;
            var iteration = (length + size - 1) / size;
            for (int i = 0; i < iteration; i++) {
                MessagePackSerializer.Serialize(stream, new SerializingDataContainer<T> { Data = memory.Slice(i * size, Math.Min(size, length - i * size)) }, options);
            }
        }

        public static List<T> Deserialize<T>(Stream stream) {
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

        public static T DeserializeAt<T>(Stream stream, int index) {
            using (var memory = new MemoryStream()) {
                stream.CopyTo(memory);
                var reader = new MessagePackReader(new ReadOnlyMemory<byte>(memory.GetBuffer(), 0, (int)memory.Length));
                return DeserializeAt<T>(ref reader, MessagePackSerializerOptions.Standard, index);
            }
        }

        public static T DeserializeAt<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, int index) {
            while (!reader.End) {
                var success = TryDeserializeAtOrSkip<T>(ref reader, options, index, out var result, out int skipArraySize);
                if (success) {
                    return result;
                }
                index -= skipArraySize;
            }

            return default;
        }

        private static List<T> DeserializeCore<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            var res = new List<List<T>>();
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
            while (!reader.End) {
                results.Add(DeserializeEach<T>(ref reader, options));
            }

            return results;
        }

        private static List<T> DeserializeEach<T>(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            var resolver = CompositeResolver.Create(
                [new DeserializedDataContainer<T>.DeserializedDataContainerFormatter(),],
                [StandardResolver.Instance]);
            if (!reader.End) {
                return MessagePackSerializer.Deserialize<DeserializedDataContainer<T>>(ref reader, options.WithResolver(resolver).WithCompression(MessagePackCompression.Lz4BlockArray)).Data;
            }

            return [];
        }

        private static bool TryDeserializeAtOrSkip<T>(ref MessagePackReader reader, MessagePackSerializerOptions options, int index, out T result, out int skipArraySize) {
            var resolver = CompositeResolver.Create(
                [DeserializedDataContainer<T>.CreateFormatter(index),],
                [StandardResolver.Instance]);
            var newOptions = options.WithCompression(MessagePackCompression.Lz4BlockArray).WithResolver(resolver);
            var deserialized = MessagePackSerializer.Deserialize<DeserializedDataContainer<T>>(ref reader, newOptions);
            result = deserialized.Data is null ? default : deserialized.Data.FirstOrDefault();
            skipArraySize = deserialized.Length;
            return result != null;
        }

        internal sealed class SerializingDataContainer<T> {
            public sbyte VersionCode { get; } = 2;
            public Memory<T> Data { get; set; }

            internal class DataContainerFormatter : IMessagePackFormatter<SerializingDataContainer<T>?> {
                public SerializingDataContainer<T> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                    throw new NotSupportedException();
                }

                public void Serialize(ref MessagePackWriter writer, SerializingDataContainer<T> value, MessagePackSerializerOptions options) {
                    writer.WriteInt8(value.VersionCode);
                    writer.WriteArrayHeader(value.Data.Length);
                    for (int i = 0; i < value.Data.Length; i++) {
                        MessagePackSerializer.Serialize(ref writer, value.Data.Span[i], options);
                    }
                }
            }
        }

        private sealed class DeserializedDataContainer<T> {
            public List<T> Data { get; set; }
            public int Length { get; set; }

            public static IMessagePackFormatter<DeserializedDataContainer<T>> CreateFormatter(int index) => new SpecificDataFormatter(index);

#pragma warning disable MsgPack009
            internal class DeserializedDataContainerFormatter : IMessagePackFormatter<DeserializedDataContainer<T>?> {
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

            internal class SpecificDataFormatter : IMessagePackFormatter<DeserializedDataContainer<T>?> {
                private readonly int _index;

                public SpecificDataFormatter(int index) {
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
#pragma warning restore MsgPack009
        }
    }
}
