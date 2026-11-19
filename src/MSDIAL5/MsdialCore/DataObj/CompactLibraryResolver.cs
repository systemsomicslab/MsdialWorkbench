using CompMs.Common.Components;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>Keeps the existing file format while compacting each owned record as it is read.</summary>
    internal sealed class CompactLibraryResolver : IFormatterResolver
    {
        public static readonly CompactLibraryResolver Instance = new CompactLibraryResolver();

        public IMessagePackFormatter<T> GetFormatter<T>() {
            if (typeof(T) == typeof(MoleculeMsReference)) {
                return (IMessagePackFormatter<T>)(object)ReferenceFormatter.Instance;
            }
            return StandardResolver.Instance.GetFormatter<T>();
        }

#pragma warning disable MsgPack009, MsgPack010
        private sealed class ReferenceFormatter : IMessagePackFormatter<MoleculeMsReference>
        {
            public static readonly ReferenceFormatter Instance = new ReferenceFormatter();
            private static readonly IMessagePackFormatter<MoleculeMsReference> Standard =
                StandardResolver.Instance.GetFormatterWithVerify<MoleculeMsReference>();

            public void Serialize(ref MessagePackWriter writer, MoleculeMsReference value, MessagePackSerializerOptions options) {
                // Expand only a transient record; leave the library's retained storage compact.
                Standard.Serialize(ref writer, value?.CreateSerializationCopy(), options);
            }

            public MoleculeMsReference Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                var value = Standard.Deserialize(ref reader, options);
                value?.TryCompactSpectrum();
                return value;
            }
        }
#pragma warning restore MsgPack009, MsgPack010
    }
}
