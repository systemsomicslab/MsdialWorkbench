using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;
using MessagePack;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public sealed class SpectrumFeature
    {
        private static readonly IMoleculeProperty UNKNOWN_MOLECULE = new MoleculeProperty();

        [Key(2)]
        private readonly IMoleculeProperty _molecule;

        [SerializationConstructor]
        public SpectrumFeature(double quantMass, IMSScanProperty scan, IMoleculeProperty molecule, MsScanMatchResultContainer matchResults) {
            PeakFeature = new BaseChromatogramPeakFeature
            {
                Mass = quantMass,
            };
            Scan = scan;
            _molecule = molecule;
            MatchResults = matchResults;
        }

        public SpectrumFeature(double quantMass, IMSScanProperty scan, IMoleculeProperty molecule) : this(quantMass, scan, molecule, new MsScanMatchResultContainer()) {

        }

        public SpectrumFeature(double quantMass, IMSScanProperty scan) : this(quantMass, scan, null) {

        }

        [IgnoreMember]
        public double QuantMass => PeakFeature.Mass;
        [Key(1)]
        public IMSScanProperty Scan { get; }
        [IgnoreMember]
        public IMoleculeProperty Molecule => _molecule ?? UNKNOWN_MOLECULE;
        [Key(3)]
        public MsScanMatchResultContainer MatchResults { get; }
        [Key(4)]
        public IChromatogramPeakFeature PeakFeature { get; }

        [IgnoreMember]
        public int PeakID => Scan.ScanID;
        [Key(5)]
        public int MS1RawSpectrumIdTop { get; set; }
        [Key(6)]
        public int MS1RawSpectrumIdLeft { get; set; }
        [Key(7)]
        public int MS1RawSpectrumIdRight { get; set; }

        [IgnoreMember]
        public IonMode IonMode => Scan.IonMode;

        public bool IsValidInChIKey() => Molecule.IsValidInChIKey();

        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) => MatchResults.IsReferenceMatched(evaluator);

        [Key(8)]
        public string Comment { get; set; } = string.Empty;
        [Key(9)]
        public ChromatogramPeakShape PeakShape { get; } = new ChromatogramPeakShape();
        [Key(10)]
        public FeatureFilterStatus FeatureFilterStatus { get; } = new FeatureFilterStatus();
        [IgnoreMember]
        public bool IsUnknown => _molecule is null;
    }
}
