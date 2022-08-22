using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class PeakItem
    {
        public PeakItem(IChromatogramPeak chrom) {
            this.chrom = chrom;
        }
        private readonly IChromatogramPeak chrom;
        public IChromatogramPeak Chrom { get => this.chrom; }

        public double Intensity => chrom.Intensity;
        public double Time => chrom.ChromXs.Value;

        [System.Obsolete("Don't use ChromatogramPeakWrapper")]
        public ChromatogramPeakWrapper ConvertToChromatogramPeakWrapper() {
            return new ChromatogramPeakWrapper(chrom);
        }
    }
}
