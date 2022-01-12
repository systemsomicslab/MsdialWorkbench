using CompMs.Common.Enum;
using CompMs.Common.Lipidomics;
using CompMs.CommonMVVM;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

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

        public ILipid Create() {
            return new Lipid(LipidClass, Mass, CreateChains());
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
