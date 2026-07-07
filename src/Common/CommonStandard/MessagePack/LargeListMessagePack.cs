using MessagePack;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;
using System.IO;

namespace CompMs.Common.MessagePack {
    public static class LargeListMessagePack {
        public const sbyte ExtensionTypeCode = 99;
        public const int HeaderSize = 11;

        private static MessagePackSerializerOptions Options => MessagePackSerializerOptions.Standard.WithResolver(StandardResolver.Instance).WithCompression(MessagePackCompression.Lz4BlockArray);

        public static void Serialize<T>(Stream stream, IReadOnlyList<T> value, IFormatterResolver resolver = null) {
            if (value is null) {
                MessagePackSerializer.Serialize(stream, value, Options.WithResolver(resolver ?? StandardResolver.Instance));
                return;
            }

            MessagePackSerializer.Serialize(stream, value, Options.WithResolver(resolver ?? StandardResolver.Instance));
        }

        public static List<T> Deserialize<T>(Stream stream, IFormatterResolver resolver = null) {
            return MessagePackSerializer.Deserialize<List<T>>(stream, Options.WithResolver(resolver ?? StandardResolver.Instance));
        }

        public static IEnumerable<List<T>> DeserializeIncremental<T>(Stream stream, IFormatterResolver resolver = null) {
            yield return Deserialize<T>(stream, resolver);
        }

        public static T DeserializeAt<T>(Stream stream, int index, IFormatterResolver resolver = null) {
            var list = Deserialize<T>(stream, resolver);
            return index >= 0 && index < list.Count ? list[index] : default;
        }
    }
}
