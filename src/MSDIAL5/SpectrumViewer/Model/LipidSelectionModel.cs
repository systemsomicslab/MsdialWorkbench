using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.RegularExpressions;

namespace CompMs.App.SpectrumViewer.Model
{
    public class LipidSelectionModel : BindableBase
    {
        public LbmClass LipidClass {
            get => lipidClass;
            set => SetProperty(ref lipidClass, value);
        }
        private LbmClass lipidClass = LbmClass.PC;

        public ReadOnlyCollection<LbmClass> LipidClasses { get; } = Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>().ToList().AsReadOnly();

        public double Mass {
            get => mass;
            set => SetProperty(ref mass, value);
        }
        private double mass = 785.5935;

        public string ChainsType {
            get => chainsType;
            set => SetProperty(ref chainsType, value);
        }
        private string chainsType;

        public ReadOnlyCollection<string> ChainsTypes { get; } = new List<string>
        {
            "SubMolecularLevel", "MolecularSpeciesLevel", "PositionLevel",
        }.AsReadOnly();

        public string ChainsStr {
            get => chainsStr;
            set => SetProperty(ref chainsStr, value);
        }
        private string chainsStr = "36:2";

        public int ChainCount {
            get => chainCount;
            set => SetProperty(ref chainCount, value);
        }
        private int chainCount = 2;

        public ObservableCollection<ChainSelectionModel> Chains { get; } = new ObservableCollection<ChainSelectionModel>();

        public string QuickChainsText {
            get => quickChainsText;
            set => SetProperty(ref quickChainsText, value);
        }
        private string quickChainsText = string.Empty;

        // Lets the user type standard chain notation ("36:2", "18:0_18:2", "18:0/18:2", "O-18:1/16:0")
        // instead of building the same thing by hand through ChainsType + the chain rows below. Uses
        // BuildEtherParser so "O-"/"P-" alkyl/plasmalogen chains are recognized (its patterns are a
        // strict superset of the plain-acyl ones, so non-ether input still parses the same as before).
        // TotalChainParser.Parse() itself is NOT anchored to the full string (each production
        // {Class}LipidParser wraps it in "^...$" - see LipidParsers.cs), so a stray leading/trailing
        // fragment (e.g. a typo, or an unsupported plasm prefix) can otherwise match a *substring* and
        // silently produce chains that don't reflect what was actually typed. Require a full match here.
        public void ApplyQuickChainsText() {
            var text = QuickChainsText ?? string.Empty;
            var parser = TotalChainParser.BuildEtherParser(ChainCount);
            if (!Regex.IsMatch(text, $"^(?:{parser.Pattern})$")) {
                throw new InvalidOperationException(
                    $"Could not parse '{text}' as chain notation (e.g. \"36:2\", \"18:0_18:2\", \"18:0/18:2\", \"O-18:1/16:0\").");
            }
            var chains = parser.Parse(text);
            if (chains is null) {
                throw new InvalidOperationException(
                    $"Could not parse '{text}' as chain notation (e.g. \"36:2\", \"18:0_18:2\", \"18:0/18:2\", \"O-18:1/16:0\").");
            }
            switch (chains) {
                case PositionLevelChains p:
                    ChainsType = "PositionLevel";
                    ReplaceChains(p.GetDeterminedChains());
                    break;
                case MolecularSpeciesLevelChains m:
                    ChainsType = "MolecularSpeciesLevel";
                    ReplaceChains(m.GetDeterminedChains());
                    break;
                default:
                    ChainsType = "SubMolecularLevel";
                    ChainsStr = chains.ToString();
                    break;
            }
        }

        // Copies the double-bond/oxidation *positions* too, not just their counts: a plasmalogen
        // ("P-") alkyl chain is only distinguished from a plain alkyl-ether ("O-") chain by having
        // an explicit double bond at position 1 (see AlkylChain.IsPlasmalogen). Dropping positions
        // here used to silently turn "P-18:0" into an undetermined "O-18:1" chain on Apply.
        private void ReplaceChains(IEnumerable<IChain> chains) {
            Chains.Clear();
            foreach (var chain in chains) {
                var chainModel = new ChainSelectionModel {
                    ChainType = chain is AlkylChain ? "Alkyl" : "Acyl",
                    CarbonCount = chain.CarbonCount,
                    DoubleBondCount = chain.DoubleBondCount,
                    OxidizedCount = chain.OxidizedCount,
                };
                foreach (var bond in chain.DoubleBond.Bonds) {
                    chainModel.DoubleBonds.Add(new DoubleBondSetModel {
                        Position = bond.Position,
                        BondType = bond.State switch {
                            DoubleBondState.E => "E",
                            DoubleBondState.Z => "Z",
                            _ => string.Empty,
                        },
                    });
                }
                foreach (var position in chain.Oxidized.Oxidises) {
                    chainModel.Oxidises.Add(new OxidizedSetModel { Position = position });
                }
                Chains.Add(chainModel);
            }
        }

        // The exact mass is never taken from the manually-editable Mass field: production code
        // (e.g. PCLipidParser) always derives it from the class-specific skeleton formula plus the
        // actual chains, so a hand-typed number here could silently drift from the real chains and
        // throw off every neutral-loss ion (e.g. "M+Proton-SN1Acyl-H2O") computed from it. Route
        // through the same registered parsers used by the real annotation pipeline instead, and
        // mirror the resulting mass back into Mass so it's visible (read-only) in the UI.
        public ILipid Create() {
            if (ChainsType != "SubMolecularLevel" && Chains.Count == 0) {
                // SeparatedChains (the base of MolecularSpeciesLevelChains/PositionLevelChains) throws
                // a bare ArgumentException("chains") for an empty array; catch it here instead so the
                // message actually tells the user what to do.
                throw new InvalidOperationException(
                    $"No chains are specified for {LipidClass} ({ChainsType}). " +
                    "Add at least one chain with the + button before generating.");
            }
            var chains = CreateChains();
            var chainsText = chains?.ToString();
            if (string.IsNullOrEmpty(chainsText)) {
                throw new InvalidOperationException(
                    $"No chains are specified for {LipidClass} ({ChainsType}). " +
                    "Add at least one chain with the + button before generating.");
            }
            // Many LbmClass values (OxPC, Cer_NS, HexCer_NS, ASM, ...) are never a parser lookup key by
            // themselves - a single parser Target ("PC", "Cer", "HexCer", "SM", ...) can emit several
            // different LbmClass outputs depending on the chains (oxidized count, hydroxylation
            // pattern, etc.), so "{LipidClass} ..." only resolves for classes whose name equals their
            // own parser's Target. LipidClassDictionary's DisplayName column is exactly the base name
            // LipidParsers.tt/the hand-written Cer/HexCer parsers key off (verified against
            // LipidClassProperties.csv, e.g. "OxPC,PC" / "Cer_NS,Cer" / "HexCer_NS,HexCer" /
            // "ASM,SM"), so try the class's own name first and fall back to that.
            var lipid = TryParseLipid(LipidClass, chainsText);
            if (lipid is null) {
                var displayName = LipidClassDictionary.Default.LbmItems.TryGetValue(LipidClass, out var prop) ? prop.DisplayName : null;
                if (!string.IsNullOrEmpty(displayName) && displayName != LipidClass.ToString()) {
                    lipid = TryParseLipid(displayName, chainsText);
                }
            }
            if (lipid is null) {
                throw new InvalidOperationException(
                    $"Could not resolve the exact mass for {LipidClass} {chainsText}: no registered " +
                    "lipid parser recognizes this class, or the chains above are not in a supported format.");
            }
            if (lipid.LipidClass != LipidClass) {
                throw new InvalidOperationException(
                    $"Parsing {LipidClass} {chainsText} produced {lipid.LipidClass} instead. The chains " +
                    "above don't carry whatever distinguishes this class (e.g. an Oxidized count for " +
                    "Ox* classes, or a hydroxylation pattern for Cer/HexCer subclasses).");
            }
            Mass = lipid.Mass;
            return lipid;
        }

        private static ILipid TryParseLipid(object lookupClass, string chainsText) {
            return FacadeLipidParser.Default.Parse($"{lookupClass} {chainsText}");
        }

        public ITotalChain CreateChains() {
            switch (ChainsType) {
                case "PositionLevel":
                    return new PositionLevelChains(Chains.Select(c => c.Create()).ToArray());
                case "MolecularSpeciesLevel":
                    return new MolecularSpeciesLevelChains(Chains.Select(c => c.Create()).ToArray());
                case "SubMolecularLevel":
                default:
                    var parser = TotalChainParser.BuildParser(ChainCount);
                    return parser.Parse(ChainsStr);
            }
        }

        public void AddChain() {
            Chains.Add(new ChainSelectionModel());
        }

        public void RemoveChain() {
            if (Chains.Count == 0) {
                return;
            }
            Chains.RemoveAt(Chains.Count - 1);
        }
    }

    public class ChainSelectionModel : BindableBase
    {
        public string ChainType {
            get => chainType;
            set => SetProperty(ref chainType, value);
        }
        private string chainType = "Acyl";

        public ReadOnlyCollection<string> ChainTypes { get; } = new List<string>
        {
            "Acyl", "Alkyl",
        }.AsReadOnly();

        public int CarbonCount {
            get => carbonCount;
            set => SetProperty(ref carbonCount, value);
        }
        private int carbonCount = 18;

        public int DoubleBondCount {
            get => doubleBondCount;
            set => SetProperty(ref doubleBondCount, value);
        }
        private int doubleBondCount = 0;

        public ObservableCollection<DoubleBondSetModel> DoubleBonds { get; } = new ObservableCollection<DoubleBondSetModel>();

        public int OxidizedCount {
            get => oxidizedCount;
            set => SetProperty(ref oxidizedCount, value);
        }
        private int oxidizedCount = 0;

        public ObservableCollection<OxidizedSetModel> Oxidises { get; } = new ObservableCollection<OxidizedSetModel>();

        public IChain Create() {
            var db = new DoubleBond(Math.Max(DoubleBondCount, DoubleBonds.Count), DoubleBonds.Select(b => b.Create()).ToArray());
            var ox = new Oxidized(Math.Max(OxidizedCount, Oxidises.Count), Oxidises.Select(o => o.Position).ToArray());

            switch (ChainType) {
                case "Alkyl":
                    return new AlkylChain(CarbonCount, db, ox);
                case "Acyl":
                default:
                    return new AcylChain(CarbonCount, db, ox);
            }
        }

        public void AddDoubleBond() {
            DoubleBonds.Add(new DoubleBondSetModel());
        }

        public void AddOxidized() {
            Oxidises.Add(new OxidizedSetModel());
        }

        public void RemoveDoubleBond(DoubleBondSetModel db) {
            DoubleBonds.Remove(db);
        }

        public void RemoveOxidized(OxidizedSetModel ox) {
            Oxidises.Remove(ox);
        }
    }

    public class DoubleBondSetModel : BindableBase
    {
        public int Position {
            get => position;
            set => SetProperty(ref position, value);
        }
        private int position = 9;

        public string BondType {
            get => bondType;
            set => SetProperty(ref bondType, value);
        }
        private string bondType = string.Empty;

        public ReadOnlyCollection<string> BondTypes { get; } = new List<string>
        {
            string.Empty, "E", "Z",
        }.AsReadOnly();

        public DoubleBondInfo Create() {
            switch (BondType) {
                case "E":
                    return DoubleBondInfo.E(Position);
                case "Z":
                    return DoubleBondInfo.Z(position);
                case "":
                default:
                    return DoubleBondInfo.Create(Position);
            }
        }
    }

    public class OxidizedSetModel : BindableBase
    {
        public int Position {
            get => position;
            set => SetProperty(ref position, value);
        }
        private int position;
    }
}
