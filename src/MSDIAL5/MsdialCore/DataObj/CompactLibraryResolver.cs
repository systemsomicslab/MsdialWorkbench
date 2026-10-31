using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using MessagePack;
using MessagePack.Formatters;
using MessagePack.Resolvers;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.DataObj
{
    /// <summary>Retains existing library keys and compact-storage eligibility rules.</summary>
    internal sealed class CompactLibraryResolver : IFormatterResolver
    {
        public static readonly CompactLibraryResolver Instance = new CompactLibraryResolver();
        private static readonly ReferenceFormatter Formatter = new ReferenceFormatter();
        private static readonly List<SpectrumPeak> SpectrumPlaceholder = new List<SpectrumPeak>();

        private CompactLibraryResolver() { }

        public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(MoleculeMsReference)
            ? (IMessagePackFormatter<T>)(object)Formatter : StandardResolver.Instance.GetFormatter<T>();

        private sealed class ReferenceFormatter : IMessagePackFormatter<MoleculeMsReference>
        {
            private readonly IMessagePackFormatter<MoleculeMsReference> _standard = StandardResolver.Instance.GetFormatter<MoleculeMsReference>();

            public int Serialize(ref byte[] bytes, int offset, MoleculeMsReference value, IFormatterResolver resolver) {
                if (value?.IsSpectrumCompact == true) {
                    var metadata = value.CreateSerializationMetadataCopy();
                    metadata.Spectrum = SpectrumPlaceholder;
                    return _standard.Serialize(ref bytes, offset, metadata,
                        new SpectrumWriteResolver(value.GetSpectrumPeakData()));
                }
                return _standard.Serialize(ref bytes, offset, value, resolver);
            }

            public MoleculeMsReference Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize) {
                // Historical giant blocks retain the legacy decode cadence to avoid measured loading-peak regressions.
                var reader = bytes.Length > LargeListMessagePack.MaxReusableBufferSize
                    ? resolver : SpectrumReadResolver.Instance;
                var value = _standard.Deserialize(bytes, offset, reader, out readSize);
                value?.TryCompactSpectrum();
                return value;
            }
        }

        private sealed class SpectrumWriteResolver(SpectrumPeakData spectrum) : IFormatterResolver, IMessagePackFormatter<List<SpectrumPeak>>
        {
            public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(List<SpectrumPeak>)
                ? (IMessagePackFormatter<T>)(object)this : StandardResolver.Instance.GetFormatter<T>();

            public int Serialize(ref byte[] bytes, int offset, List<SpectrumPeak> value, IFormatterResolver resolver) {
                if (!ReferenceEquals(value, SpectrumPlaceholder)) {
                    return StandardResolver.Instance.GetFormatter<List<SpectrumPeak>>().Serialize(ref bytes, offset, value, StandardResolver.Instance);
                }
                var start = offset;
                offset += MessagePackBinary.WriteArrayHeader(ref bytes, offset, spectrum.Count);
                for (var i = 0; i < spectrum.Count; i++) {
                    // Older readers restore omitted trailing fields to the same default values.
                    MessagePackBinary.EnsureCapacity(ref bytes, offset, 19);
                    bytes[offset++] = 0x92;
                    offset += WriteNumber(ref bytes, offset, spectrum.MassAt(i));
                    offset += WriteNumber(ref bytes, offset, spectrum.IntensityAt(i));
                }
                return offset - start;
            }

            private static int WriteNumber(ref byte[] bytes, int offset, double value) {
                // Preserve every double bit; shorter encodings are used only when conversion is exact.
                if (value == 0d && BitConverter.DoubleToInt64Bits(value) != 0L) {
                    return MessagePackBinary.WriteDouble(ref bytes, offset, value);
                }
                if (value >= int.MinValue && value <= uint.MaxValue && (long)value == value) {
                    return MessagePackBinary.WriteInt64(ref bytes, offset, (long)value);
                }
                var single = (float)value;
                if (value == (double)single && !double.IsInfinity(value)) {
                    return MessagePackBinary.WriteSingle(ref bytes, offset, single);
                }
                return MessagePackBinary.WriteDouble(ref bytes, offset, value);
            }

            public List<SpectrumPeak> Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize)
                => StandardResolver.Instance.GetFormatter<List<SpectrumPeak>>().Deserialize(bytes, offset, StandardResolver.Instance, out readSize);
        }

        private sealed class SpectrumReadResolver : IFormatterResolver
        {
            public static readonly SpectrumReadResolver Instance = new SpectrumReadResolver();
            private static readonly PlainSpectrumFormatter Formatter = new PlainSpectrumFormatter();

            public IMessagePackFormatter<T> GetFormatter<T>() => typeof(T) == typeof(List<SpectrumPeak>)
                ? (IMessagePackFormatter<T>)(object)Formatter : StandardResolver.Instance.GetFormatter<T>();
        }

        private sealed class PlainSpectrumFormatter : IMessagePackFormatter<List<SpectrumPeak>>
        {
            private static readonly IMessagePackFormatter<List<SpectrumPeak>> Standard = StandardResolver.Instance.GetFormatter<List<SpectrumPeak>>();
            private static readonly IMessagePackFormatter<SpectrumPeak> PeakFormatter = StandardResolver.Instance.GetFormatter<SpectrumPeak>();
            private const int PlainPeakSize = 39;
            // Existing SpectrumPeak keys 2..13: null comment, unused slots, default flags/IDs and +0 fragmentation score.
            public static readonly byte[] DefaultTail = {
                0xc0, 0xc0, 0xc0, 0xc0, 0, 0, 0xc0, 0xc0, 0xc0, 0, 0xc2, 0xcb, 0, 0, 0, 0, 0, 0, 0, 0,
            };
            private static readonly long TailStart = BitConverter.ToInt64(DefaultTail, 0);
            private static readonly long TailMiddle = BitConverter.ToInt64(DefaultTail, 8);
            private static readonly int TailEnd = BitConverter.ToInt32(DefaultTail, 16);

            public int Serialize(ref byte[] bytes, int offset, List<SpectrumPeak> value, IFormatterResolver resolver)
                => Standard.Serialize(ref bytes, offset, value, StandardResolver.Instance);

            public List<SpectrumPeak> Deserialize(byte[] bytes, int offset, IFormatterResolver resolver, out int readSize) {
                if (MessagePackBinary.IsNil(bytes, offset)) {
                    return Standard.Deserialize(bytes, offset, StandardResolver.Instance, out readSize);
                }
                var start = offset;
                var count = MessagePackBinary.ReadArrayHeader(bytes, offset, out var size);
                offset += size;
                // Preserve the legacy allocation order; direct numeric reads inflated measured heap residency.
                var peaks = new List<SpectrumPeak>(count);
                for (var i = 0; i < count; i++) {
                    if (bytes[offset] == 0x92) {
                        var mass = ReadNumber(bytes, offset + 1, out var massSize);
                        var intensity = ReadNumber(bytes, offset + 1 + massSize, out var intensitySize);
                        peaks.Add(new SpectrumPeak(mass, intensity));
                        offset += 1 + massSize + intensitySize;
                    }
                    else if (IsPlainPeak(bytes, offset)) {
                        peaks.Add(new SpectrumPeak(
                            MessagePackBinary.ReadDouble(bytes, offset + 1, out _),
                            MessagePackBinary.ReadDouble(bytes, offset + 10, out _)));
                        offset += PlainPeakSize;
                    }
                    else {
                        peaks.Add(PeakFormatter.Deserialize(bytes, offset, StandardResolver.Instance, out size));
                        offset += size;
                    }
                }
                readSize = offset - start;
                return peaks;
            }

            private static double ReadNumber(byte[] bytes, int offset, out int readSize) {
                var code = bytes[offset];
                if (code < 0x80) { readSize = 1; return code; }
                if (code == 0xcc) { readSize = 2; return bytes[offset + 1]; }
                if (code == 0xcd) { readSize = 3; return (bytes[offset + 1] << 8) | bytes[offset + 2]; }
                return MessagePackBinary.ReadDouble(bytes, offset, out readSize);
            }

            private static bool IsPlainPeak(byte[] bytes, int offset) {
                // Alternate encodings in full peak records retain the standard formatter's semantics.
                return bytes.Length - offset >= PlainPeakSize
                    && bytes[offset] == 0x9e && bytes[offset + 1] == 0xcb && bytes[offset + 10] == 0xcb
                    && BitConverter.ToInt64(bytes, offset + 19) == TailStart
                    && BitConverter.ToInt64(bytes, offset + 27) == TailMiddle
                    && BitConverter.ToInt32(bytes, offset + 35) == TailEnd;
            }
        }
    }
}
