using CompMs.Common.Components;
using CompMs.Common.Interfaces;
using MessagePack;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class SpectrumFeature
    {
        private static readonly IMoleculeProperty UNKNOWN_MOLECULE = new MoleculeProperty();

        private readonly IMoleculeProperty _molecule;

        [SerializationConstructor]
        public SpectrumFeature(double quantMass, IMSScanProperty scan, IMoleculeProperty molecule, MsScanMatchResultContainer matchResults) {
            QuantMass = quantMass;
            Scan = scan;
            _molecule = molecule;
            MatchResults = matchResults;
        }

        public SpectrumFeature(double quantMass, IMSScanProperty scan, IMoleculeProperty molecule) : this(quantMass, scan, molecule, new MsScanMatchResultContainer()) {

        }

        public SpectrumFeature(double quantMass, IMSScanProperty scan) : this(quantMass, scan, null) {

        }

        [Key(0)]
        public double QuantMass { get; }
        [Key(1)]
        public IMSScanProperty Scan { get; }
        [Key(2)]
        public IMoleculeProperty Molecule => _molecule ?? UNKNOWN_MOLECULE;
        [Key(3)]
        public MsScanMatchResultContainer MatchResults { get; }
        [IgnoreMember]
        public bool IsUnknown => _molecule is null;
    }
}
