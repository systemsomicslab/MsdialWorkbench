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
        public IChromatogramPeakFeature Deserialize(ref MessagePackReader reader, MessagePackSerializerOptions options) {
            return options.Resolver.GetFormatterWithVerify<BaseChromatogramPeakFeature>().Deserialize(ref reader, options);
        }

        public void Serialize(ref MessagePackWriter writer, IChromatogramPeakFeature value, MessagePackSerializerOptions options) {
            if (value == null) {
                writer.WriteNil();
                return;
            }
            if (value is BaseChromatogramPeakFeature bp) {
                options.Resolver.GetFormatterWithVerify<BaseChromatogramPeakFeature>().Serialize(ref writer, bp, options);
                return;
            }

            writer.WriteArrayHeader(12);
            writer.Write(value.ChromScanIdLeft);
            writer.Write(value.ChromScanIdTop);
            writer.Write(value.ChromScanIdRight);
            var chromFormatter = options.Resolver.GetFormatterWithVerify<ChromXs>();
            chromFormatter.Serialize(ref writer, value.ChromXsLeft, options);
            chromFormatter.Serialize(ref writer, value.ChromXsTop, options);
            chromFormatter.Serialize(ref writer, value.ChromXsRight, options);
            writer.Write(value.PeakHeightLeft);
            writer.Write(value.PeakHeightTop);
            writer.Write(value.PeakHeightRight);
            writer.Write(value.PeakAreaAboveZero);
            writer.Write(value.PeakAreaAboveBaseline);
            writer.Write(value.Mass);
        }
    }
}
