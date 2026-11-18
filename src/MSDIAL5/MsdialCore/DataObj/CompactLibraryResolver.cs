using CompMs.Common.Components;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>Writes exact compact peaks with existing keys and compacts each owned record as it is read.</summary>
    internal sealed class CompactLibraryResolver : IFormatterResolver
    {
        public static readonly CompactLibraryResolver Instance = new CompactLibraryResolver();
        private static readonly List<SpectrumPeak> SpectrumPlaceholder = new List<SpectrumPeak>();

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
                if (value?.IsSpectrumCompact == true) {
                    var metadata = value.CreateSerializationMetadataCopy(SpectrumPlaceholder);
                    var spectrumOptions = options.WithResolver(new SpectrumWriteResolver(value.GetSpectrumPeakData()));
                    Standard.Serialize(ref writer, metadata, spectrumOptions);
                    return;
                }
                Standard.Serialize(ref writer, value, options);
            }

            public MoleculeMsReference Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
                var value = Standard.Deserialize(ref reader, options);
                value?.TryCompactSpectrum();
                return value;
            }
        }
#pragma warning restore MsgPack009, MsgPack010

#pragma warning disable MsgPack009, MsgPack010
        private sealed class SpectrumWriteResolver : IFormatterResolver, IMessagePackFormatter<List<SpectrumPeak>>
        {
            private readonly SpectrumPeakData _spectrum;
            private static readonly IMessagePackFormatter<List<SpectrumPeak>> StandardList =
                StandardResolver.Instance.GetFormatterWithVerify<List<SpectrumPeak>>();

            public SpectrumWriteResolver(SpectrumPeakData spectrum) => _spectrum = spectrum;

            public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(List<SpectrumPeak>)
                ? (IMessagePackFormatter<T>)(object)this
                : StandardResolver.Instance.GetFormatter<T>();

            public void Serialize(ref MessagePackWriter writer, List<SpectrumPeak> value, MessagePackSerializerOptions options) {
                if (!ReferenceEquals(value, SpectrumPlaceholder)) {
                    StandardList.Serialize(ref writer, value, options.WithResolver(StandardResolver.Instance));
                    return;
                }
                writer.WriteArrayHeader(_spectrum.Count);
                for (var i = 0; i < _spectrum.Count; i++) {
                    // A plain peak's omitted keys have the same defaults in historical readers.
                    writer.WriteArrayHeader(2);
                    WriteNumber(ref writer, _spectrum.MassAt(i));
                    WriteNumber(ref writer, _spectrum.IntensityAt(i));
                }
            }

            private static void WriteNumber(ref MessagePackWriter writer, double value) {
                // Use shorter numeric forms only when they preserve the original double exactly.
                if (value == 0d && BitConverter.DoubleToInt64Bits(value) != 0L) {
                    writer.Write(value);
                }
                else if (value >= int.MinValue && value <= uint.MaxValue && (long)value == value) {
                    writer.Write((long)value);
                }
                else {
                    var single = (float)value;
                    if (value == (double)single && !double.IsInfinity(value)) {
                        writer.Write(single);
                    }
                    else {
                        writer.Write(value);
                    }
                }
            }

            public List<SpectrumPeak> Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) =>
                StandardList.Deserialize(ref reader, options.WithResolver(StandardResolver.Instance));
        }
#pragma warning restore MsgPack009, MsgPack010
    }
}
