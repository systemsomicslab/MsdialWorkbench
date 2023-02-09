using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class SpectrumFeature
    {
        public IMSScanProperty Scan { get; }
        public double QuantMass { get; }
        public IMoleculeProperty Molecule { get; }
        public MsScanMatchResultContainer MatchResults { get; }
    }
}
