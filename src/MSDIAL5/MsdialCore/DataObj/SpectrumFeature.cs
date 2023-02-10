using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.MsdialCore.Algorithm.Annotation;

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
        public IChromatogramPeakFeature PeakFeature { get; }

        public int PeakID => Scan.ScanID;
        public int MS1RawSpectrumIdTop { get; set; }
        public int MS1RawSpectrumIdLeft { get; set; }
        public int MS1RawSpectrumIdRight { get; set; }

        public IonMode IonMode { get; set; }

        public bool IsValidInChIKey() {
            return !string.IsNullOrWhiteSpace(Molecule.InChIKey) && Molecule.InChIKey.Length == 27;
        }

        public bool IsReferenceMatched(IMatchResultEvaluator<MsScanMatchResult> evaluator) {
            if (MatchResults.IsManuallyModifiedRepresentative) {
                return !MatchResults.IsUnknown;
            }
            return MatchResults.IsReferenceMatched(evaluator);
        }

        public string Comment { get; set; } = string.Empty;
        public IonFeatureCharacter PeakCharacter { get; set; } = new IonFeatureCharacter();
        public ChromatogramPeakShape PeakShape { get; set; } = new ChromatogramPeakShape();
        public FeatureFilterStatus FeatureFilterStatus { get; set; } = new FeatureFilterStatus();
    }
}
