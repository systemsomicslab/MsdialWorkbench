using CompMs.Common.Components;
using CompMs.Common.Interfaces;

namespace CompMs.MsdialCore.DataObj
{
    public sealed class SpectrumFeature
    {
        private static readonly IMoleculeProperty UNKNOWN_MOLECULE = new MoleculeProperty();

        private readonly IMoleculeProperty _molecule;

        public SpectrumFeature(double quantMass, IMSScanProperty scan, IMoleculeProperty molecule) {
            QuantMass = quantMass;
            Scan = scan;
            _molecule = molecule;
            MatchResults = new MsScanMatchResultContainer();
        }

        public SpectrumFeature(double quantMass, IMSScanProperty scan) : this(quantMass, scan, null) {

        }

        public double QuantMass { get; }
        public IMSScanProperty Scan { get; }
        public IMoleculeProperty Molecule => _molecule ?? UNKNOWN_MOLECULE;
        public MsScanMatchResultContainer MatchResults { get; }
    }
}
