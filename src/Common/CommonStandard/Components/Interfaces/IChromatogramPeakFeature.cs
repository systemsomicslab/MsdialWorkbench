using CompMs.Common.Components;
using MessagePack;

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

    public static class ChromaogramPeakFEatureExtension {
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
}
