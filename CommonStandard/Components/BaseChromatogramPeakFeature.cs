using CompMs.Common.Interfaces;

namespace CompMs.Common.Components
{
    public class BaseChromatogramPeakFeature : IChromatogramPeakFeature
    {
        public int ChromScanIdLeft { get; set; }
        public int ChromScanIdTop { get; set; }
        public int ChromScanIdRight { get; set; }
        public ChromXs ChromXsLeft { get; set; }
        public ChromXs ChromXsTop { get; set; }
        public ChromXs ChromXsRight { get; set; }
        public double PeakHeightLeft { get; set; }
        public double PeakHeightTop { get; set; }
        public double PeakHeightRight { get; set; }
        public double PeakAreaAboveZero { get; set; }
        public double PeakAreaAboveBaseline { get; set; }
        public double Mass { get; set; }

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
