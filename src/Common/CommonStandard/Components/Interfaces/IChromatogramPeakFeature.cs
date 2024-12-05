using CompMs.Common.Components;
using MessagePack;
using MessagePack.Formatters;
using System;

namespace CompMs.Common.Interfaces {
    [Union(0, typeof(BaseChromatogramPeakFeature))]
    public interface IChromatogramPeakFeature {

        // basic property
        int ChromScanIdLeft { get; set; }
        int ChromScanIdTop { get; set; }
        int ChromScanIdRight { get; set; }
        ChromXs ChromXsLeft { get; set; }
        ChromXs ChromXsTop { get; set; }
        ChromXs ChromXsRight { get; set; }
        double PeakHeightLeft { get; set; }
        double PeakHeightTop { get; set; }
        double PeakHeightRight { get; set; }
        double PeakAreaAboveZero { get; set; }
        double PeakAreaAboveBaseline { get; set; }
        double Mass { get; set; }
        double PeakWidth();
        double PeakWidth(ChromXType type);
    }

    public static class ChromatogramPeakFeatureExtension {
        public static bool IsOverlapedWithChromXType(this IChromatogramPeakFeature self, IChromatogramPeakFeature other, ChromXType type) {
            IChromX selfTop = self.ChromXsTop.GetChromByType(type);
            IChromX otherTop = other.ChromXsTop.GetChromByType(type);
            return selfTop.Value > otherTop.Value
                ? self.ChromXsLeft.GetChromByType(type).Value < otherTop.Value
                : other.ChromXsLeft.GetChromByType(type).Value < selfTop.Value;
        }

        public static bool IsOverlaped(this IChromatogramPeakFeature self, IChromatogramPeakFeature other) {
            return self.ChromXsTop.Value > other.ChromXsTop.Value
                ? self.ChromXsLeft.Value < other.ChromXsTop.Value
                : other.ChromXsLeft.Value < self.ChromXsTop.Value;
        }
    }

    public class ChromatogramPeakFeatureInterfaceFormatter : IMessagePackFormatter<IChromatogramPeakFeature>
    {
        public IChromatogramPeakFeature Deserialize(byte[] bytes, int offset, IFormatterResolver formatterResolver, out int readSize) {
            return formatterResolver.GetFormatterWithVerify<BaseChromatogramPeakFeature>().Deserialize(bytes, offset, formatterResolver, out readSize);
        }

        public int Serialize(ref byte[] bytes, int offset, IChromatogramPeakFeature value, IFormatterResolver formatterResolver) {
            if (value == null) {
                return MessagePackBinary.WriteNil(ref bytes, offset);
            }
            if (value is BaseChromatogramPeakFeature bp) {
                return formatterResolver.GetFormatterWithVerify<BaseChromatogramPeakFeature>().Serialize(ref bytes, offset, bp, formatterResolver);
            }
            else {
                var currentOffset = offset;
                currentOffset += MessagePackBinary.WriteArrayHeader(ref bytes, currentOffset, 12);
                currentOffset += MessagePackBinary.WriteInt32(ref bytes, currentOffset, value.ChromScanIdLeft);
                currentOffset += MessagePackBinary.WriteInt32(ref bytes, currentOffset, value.ChromScanIdTop);
                currentOffset += MessagePackBinary.WriteInt32(ref bytes, currentOffset, value.ChromScanIdRight);
                var chromFormatter = formatterResolver.GetFormatterWithVerify<ChromXs>();
                currentOffset += chromFormatter.Serialize(ref bytes, currentOffset, value.ChromXsLeft, formatterResolver);
                currentOffset += chromFormatter.Serialize(ref bytes, currentOffset, value.ChromXsTop, formatterResolver);
                currentOffset += chromFormatter.Serialize(ref bytes, currentOffset, value.ChromXsRight, formatterResolver);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.PeakHeightLeft);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.PeakHeightTop);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.PeakHeightRight);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.PeakAreaAboveZero);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.PeakAreaAboveBaseline);
                currentOffset += MessagePackBinary.WriteDouble(ref bytes, currentOffset, value.Mass);
                return currentOffset - offset;
            }
        }
    }
}
