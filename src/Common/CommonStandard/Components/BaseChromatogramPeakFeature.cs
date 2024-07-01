using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.Common.Components
{
    [MessagePackObject]
    public class BaseChromatogramPeakFeature : IChromatogramPeakFeature
    {
        [Key(0)]
        public int ChromScanIdLeft { get; set; }
        [Key(1)]
        public int ChromScanIdTop { get; set; }
        [Key(2)]
        public int ChromScanIdRight { get; set; }
        [Key(3)]
        public ChromXs ChromXsLeft { get; set; } = new ChromXs();
        [Key(4)]
        public ChromXs ChromXsTop { get; set; } = new ChromXs();
        [Key(5)]
        public ChromXs ChromXsRight { get; set; } = new ChromXs();
        [Key(6)]
        public double PeakHeightLeft { get; set; }
        [Key(7)]
        public double PeakHeightTop { get; set; }
        [Key(8)]
        public double PeakHeightRight { get; set; }
        [Key(9)]
        public double PeakAreaAboveZero { get; set; }
        [Key(10)]
        public double PeakAreaAboveBaseline { get; set; }
        [Key(11)]
        public double Mass { get; set; }

        public double PeakWidth() {
            return ChromXsRight.Value - ChromXsLeft.Value;
        }

        public double PeakWidth(ChromXType type) {
            switch (type) {
                case ChromXType.RT: return ChromXsRight.RT.Value - ChromXsLeft.RT.Value;
                case ChromXType.RI: return ChromXsRight.RI.Value - ChromXsLeft.RI.Value;
                case ChromXType.Drift: return ChromXsRight.Drift.Value - ChromXsLeft.Drift.Value;
                default: return ChromXsRight.Value - ChromXsLeft.Value;
            }
        }
    }
}
