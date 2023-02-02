using CompMs.Common.Interfaces;

namespace CompMs.App.Msdial.Model.DataObj
{
    public sealed class PeakItem
    {
        private readonly IChromatogramPeak _chrom;

        public PeakItem(IChromatogramPeak chrom) {
            _chrom = chrom;
        }

        public IChromatogramPeak Chrom => _chrom;

        public double Intensity => _chrom.Intensity;
        public double Time => _chrom.ChromXs.Value;
    }
}
