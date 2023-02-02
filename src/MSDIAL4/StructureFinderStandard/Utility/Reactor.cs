using NCDK;
using NCDK.Default;
using NCDK.Graphs;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public enum AcidicProtonMoiety { IsAlcohol, IsAmine, Others }
    public enum KetoneMoiety { IsKetone, IsAldehyde, Others }

    public class AcidicProton
    {
        public IAtom AcidicProtonAtom { get; set; }
        public IAtom NextAtomToAcidicProton { get; set; }
        public IBond ConnectedBond { get; set; }
        public AcidicProtonMoiety Moiety { get; set; }
        public int PriorityLevel { get; set; }
    }

    public class MethoxymOxigen
    {
        public IAtom KetoneOxygen { get; set; }
        public IAtom NextCarbonToKetoneOxygen { get; set; }
        public IBond ConnectedBond { get; set; }
        public KetoneMoiety Moiety { get; set; }
        public int PriorityLevel { get; set; }
    }

    public sealed class Derivatization
    {
        private Derivatization() { }

        private static readonly IChemObjectBuilder builder = CDK.Builder;
        private static SmilesParser parser = new SmilesParser(builder);
        private static SmilesGenerator smilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);

        /// <summary>
        /// this is the source code to automatically add the TMS and MeOX derivatives into original SMILES.
        /// All of acidic hydrogens will be converterd to TMS.
        /// For MeOX derivatization, the oxigen of CC(=O)C is the target.
        /// 
        /// </summary>
        public static string TmsMeoxDerivatization(string smiles, int amineDerivativeLevel = 1)
        {
            var error = string.Empty;
            IAtomContainer container = null;

            try {
                //var smilesParser = new SmilesParser();
                container = parser.ParseSmiles(smiles);
                if (container != null && smiles.Contains('c')) {
                    Kekulization.Kekulize(container);
                }
            }
            catch (InvalidSmilesException) {
                error += "SMILES: cannot be converted.\r\n";
                return null;
            }

            if (!ConnectivityChecker.IsConnected(container)) {
                error += "SMILES: the connectivity is not correct.\r\n";
                return null;
            }
            return TmsMeoxDerivatization(container, amineDerivativeLevel);
        }

        private static string TmsMeoxDerivatization(IAtomContainer atomContainer, int amineDerivativeLevel = 1)
        {
            var errorMessage = string.Empty;
            var isValidatedStructure = ExplicitHydrogenAdder.AddExplicitHydrogens(atomContainer);
            if (isValidatedStructure == false) {
                errorMessage = "ExplicitHydrogenAdder was not passed.";
                return string.Empty;
            }

            var acidicProtons = new List<AcidicProton>();
            var methoxymOxigens = new List<MethoxymOxigen>();
            var atomBondsDict = MoleculeConverter.GetAtomBondsDictionary(atomContainer); // get atom to bonds dictionary

            var numberedContainer = moleculeNumberingWithReactionPlaceCount(atomContainer, atomBondsDict, out acidicProtons, out methoxymOxigens);

            var tmsCount = acidicProtons.Count;
            var meoxCount = methoxymOxigens.Count;
            var derivatizedContainer = ConvertToDerivatizedCompound(numberedContainer, atomBondsDict, acidicProtons, methoxymOxigens, tmsCount, meoxCount, amineDerivativeLevel);

            derivatizedContainer = AtomContainerManipulator.RemoveHydrogens(derivatizedContainer);
            var derivatizedSMILES = smilesGenerator.Create(derivatizedContainer);

            return derivatizedSMILES;
        }

        /// <summary>
        /// This is the cord to convert the original SMILES with the numbers of TMS and MEOX into derivative compound.
        /// The result is currently returned as IAtomContainer. But we can easily get the result as SMILES via CDK.
        /// </summary>
        public static IAtomContainer TmsMeoxDerivatization(string smiles, int tmsCount, int meoxCount)
        {
            IAtomContainer atomContainer = null;

            try {
                //var smilesParser = new SmilesParser();
                atomContainer = parser.ParseSmiles(smiles);
            }
            catch (InvalidSmilesException) {
                return null;
            }

            if (!ConnectivityChecker.IsConnected(atomContainer)) return null;

            return TmsMeoxDerivatization(atomContainer, tmsCount, meoxCount);
        }

        public static IAtomContainer TmsMeoxDerivatization(IAtomContainer atomContainer, int tmsCount, int meoxCount)
        {
            var errorMessage = string.Empty;
            var isValidatedStructure = ExplicitHydrogenAdder.AddExplicitHydrogens(atomContainer);
            if (isValidatedStructure == false) {
                errorMessage = "ExplicitHydrogenAdder was not passed.";
                return null;
            }
            //Console.WriteLine("ok1");
            var acidicProtons = new List<AcidicProton>();
            var methoxymOxigens = new List<MethoxymOxigen>();
            var atomBondsDict = MoleculeConverter.GetAtomBondsDictionary(atomContainer); // get atom to bonds dictionary
           // Console.WriteLine("ok2");

            var numberedContainer = moleculeNumberingWithReactionPlaceCount(atomContainer, atomBondsDict, out acidicProtons, out methoxymOxigens);
            //Console.WriteLine("ok3");

            if (acidicProtons.Count < tmsCount || methoxymOxigens.Count < meoxCount) return null;
            var derivatizedContainer = ConvertToDerivatizedCompound(numberedContainer, atomBondsDict, acidicProtons, methoxymOxigens, tmsCount, meoxCount);
           // Console.WriteLine("ok4");

            return derivatizedContainer;
        }

        public static IAtomContainer ConvertToDerivatizedCompound(IAtomContainer atomContainer, Dictionary<IAtom, List<IBond>> atomBondsDict,
            List<AcidicProton> acidicProtons, List<MethoxymOxigen> methoxymOxigens, int maxTms, int maxMeox, int amineDerivativeLevel = 1)
        {
            var tmsDerivatizedContainer = ConvertToTmsDerivativeCompound(atomContainer, atomBondsDict, acidicProtons, maxTms, amineDerivativeLevel);
            var meoxDerivatizedContainer = ConvertToMeoxDerivativeCompound(tmsDerivatizedContainer, methoxymOxigens, maxMeox);

            return meoxDerivatizedContainer;
        }

        private static IAtomContainer ConvertToMeoxDerivativeCompound(IAtomContainer atomContainer, List<MethoxymOxigen> methoxymOxigens, int maxMeoxCount)
        {
            if (methoxymOxigens.Count == 0) return atomContainer;
            var counter = 1;
            foreach (var meoxOxygen in methoxymOxigens.OrderBy(n => n.Moiety)) {
                if (counter > maxMeoxCount) break;

                //preparation of MeOX moiety
                var meoxMoiety = getMeoxMoierty();

                //remove atom and bond of ketone
                atomContainer.RemoveAtom(meoxOxygen.KetoneOxygen);
                //atomContainer.RemoveBond(meoxOxygen.ConnectedBond);

                //create new bond
                //var newBond = new Bond(meoxOxygen.NextCarbonToKetoneOxygen, meoxMoiety.Atoms[0], BondOrder.Double);
                atomContainer.Add(meoxMoiety);
                atomContainer.AddBond(meoxOxygen.NextCarbonToKetoneOxygen, meoxMoiety.Atoms[0], BondOrder.Double);

                counter++;
            }

            return atomContainer;
        }

        public static IAtomContainer ConvertToTmsDerivativeCompound(IAtomContainer atomContainer, Dictionary<IAtom, List<IBond>> atomBondsDict,
            List<AcidicProton> acidicProtons, int maxTmsCount, int amineDerivativeLevel = 1)
        {
            if (acidicProtons.Count == 0) return atomContainer;
            var counter = 1;
            var primaryAminesChecklist = new List<IAtom>();

            foreach (var acidicProton in acidicProtons.OrderBy(n => n.Moiety)) {
                if (counter > maxTmsCount) break;

                switch (amineDerivativeLevel) {
                    case 0: // check primary amines: if 'amineDerivativeLevel == 0', the acidic protons of N moiety is never converted to TMS.
                        if (acidicProton.NextAtomToAcidicProton.Symbol == "N") continue;
                        break;
                    case 1: // check primary amines: if 'amineDerivativeLevel == 1', -NH2 moiety is converted to NH-TMS.
                        if (primaryAminesChecklist.Contains(acidicProton.NextAtomToAcidicProton) && acidicProton.NextAtomToAcidicProton.Symbol == "N") continue;
                        else primaryAminesChecklist.Add(acidicProton.NextAtomToAcidicProton);
                        break;
                    case 2:
                        break;
                    default:
                        break;
                }

                //remove atom and bond of acidic proton
                atomContainer.RemoveAtom(acidicProton.AcidicProtonAtom);
                //atomContainer.RemoveBond(acidicProton.ConnectedBond);

                //create new bond
                var tmsMoiety = getTmsMoiety(); //preparation of TMS moiety
                //var newBond = new Bond(acidicProton.NextAtomToAcidicProton, tmsMoiety.getAtom(0), IBond.Order.SINGLE);
                atomContainer.Add(tmsMoiety);
                atomContainer.AddBond(acidicProton.NextAtomToAcidicProton, tmsMoiety.Atoms[0], BondOrder.Single);

                counter++;
            }

            return atomContainer;
        }

        public static void Test() {
            var element = ChemicalElement.Si;
            var atom = new Atom(element);
           // Console.WriteLine();
        }

        private static IAtomContainer getTmsMoiety()
        {
            var container = new AtomContainer();

            #region // create atoms
            var atomSi = new Atom(ChemicalElement.Si);
            var atomC1 = new Atom(ChemicalElement.C);
            var atomC2 = new Atom(ChemicalElement.C);
            var atomC3 = new Atom(ChemicalElement.C);

            var atomH1forC1 = new Atom(ChemicalElement.H);
            var atomH2forC1 = new Atom(ChemicalElement.H);
            var atomH3forC1 = new Atom(ChemicalElement.H);

            var atomH1forC2 = new Atom(ChemicalElement.H);
            var atomH2forC2 = new Atom(ChemicalElement.H);
            var atomH3forC2 = new Atom(ChemicalElement.H);

            var atomH1forC3 = new Atom(ChemicalElement.H);
            var atomH2forC3 = new Atom(ChemicalElement.H);
            var atomH3forC3 = new Atom(ChemicalElement.H);
            #endregion

            #region // create bonds
            var bondSiC1 = new Bond(atomSi, atomC1, BondOrder.Single);
            var bondSiC2 = new Bond(atomSi, atomC2, BondOrder.Single);
            var bondSiC3 = new Bond(atomSi, atomC3, BondOrder.Single);

            var bondC1H1 = new Bond(atomC1, atomH1forC1, BondOrder.Single);
            var bondC1H2 = new Bond(atomC1, atomH2forC1, BondOrder.Single);
            var bondC1H3 = new Bond(atomC1, atomH3forC1, BondOrder.Single);

            var bondC2H1 = new Bond(atomC2, atomH1forC2, BondOrder.Single);
            var bondC2H2 = new Bond(atomC2, atomH2forC2, BondOrder.Single);
            var bondC2H3 = new Bond(atomC2, atomH3forC2, BondOrder.Single);

            var bondC3H1 = new Bond(atomC3, atomH1forC3, BondOrder.Single);
            var bondC3H2 = new Bond(atomC3, atomH2forC3, BondOrder.Single);
            var bondC3H3 = new Bond(atomC3, atomH3forC3, BondOrder.Single);
            #endregion

            var atoms = new List<IAtom>()
            {
                atomSi, atomC1, atomC2, atomC3, atomH1forC1, atomH2forC1, atomH3forC1, atomH1forC2, atomH2forC2, atomH3forC2,
                atomH1forC3, atomH2forC3, atomH3forC3
            };

            var bonds = new List<IBond>()
            {
                bondSiC1, bondSiC2, bondSiC3, bondC1H1, bondC1H2, bondC1H3, bondC2H1, bondC2H2, bondC2H3, bondC3H1, bondC3H2, bondC3H3
            };

            container.SetAtoms(atoms.ToArray());
            container.SetBonds(bonds.ToArray());

            return container;
        }

        private static IAtomContainer getMeoxMoierty()
        {
            var container = new AtomContainer();

            #region // create atoms
            var atomN = new Atom(ChemicalElement.N);
            var atomO = new Atom(ChemicalElement.O);
            var atomC = new Atom(ChemicalElement.C);
            var atomH1 = new Atom(ChemicalElement.H);
            var atomH2 = new Atom(ChemicalElement.H);
            var atomH3 = new Atom(ChemicalElement.H);
            #endregion

            #region // create bonds
            var bondNO = new Bond(atomN, atomO, BondOrder.Single);
            var bondOC = new Bond(atomO, atomC, BondOrder.Single);
            var bondCH1 = new Bond(atomC, atomH1, BondOrder.Single);
            var bondCH2 = new Bond(atomC, atomH2, BondOrder.Single);
            var bondCH3 = new Bond(atomC, atomH3, BondOrder.Single);
            #endregion

            var atoms = new List<IAtom>()
            {
                atomN, atomO, atomC, atomH1, atomH2, atomH3
            };

            var bonds = new List<IBond>()
            {
                bondNO, bondOC, bondCH1, bondCH2, bondCH3
            };

            container.SetAtoms(atoms.ToArray());
            container.SetBonds(bonds.ToArray());

            return container;
        }

        private static IAtomContainer moleculeNumberingWithReactionPlaceCount(IAtomContainer atomContainer, Dictionary<IAtom, List<IBond>> atomBondsDict, out List<AcidicProton> acidicProtons, out List<MethoxymOxigen> methoxymOxigens)
        {
            MoleculeConverter.MoleculeNumbering(atomContainer); // atom and bond numbering

            acidicProtons = new List<AcidicProton>();
            methoxymOxigens = new List<MethoxymOxigen>();

            foreach (var atomBondsPair in atomBondsDict.ToList()) {
                var acidicProton = new AcidicProton();
                if (IsAcidicProton(atomBondsPair, atomBondsDict, out acidicProton)) {
                    acidicProtons.Add(acidicProton);
                }

                var methoxymOxigen = new MethoxymOxigen();
                if (IsMethoxymOxygen(atomBondsPair, atomBondsDict, out methoxymOxigen)) {
                    methoxymOxigens.Add(methoxymOxigen);
                }
            }

            return atomContainer;
        }

        private static bool IsMethoxymOxygen(KeyValuePair<IAtom, List<IBond>> atomBondsPair, Dictionary<IAtom, List<IBond>> atomBondsDict, out MethoxymOxigen methoxymOxigen)
        {
            methoxymOxigen = new MethoxymOxigen();

            var atom = atomBondsPair.Key;
            var bonds = atomBondsPair.Value;

            if (bonds.Count == 1 && bonds[0].Order == BondOrder.Double && atom.Symbol == "O") { //this is the source code for C=O

                foreach (var connectedAtom in bonds[0].Atoms) {
                    if (connectedAtom.Id == atom.Id) continue;
                    if (connectedAtom.Symbol == "C") {

                        if (isReactiveMeox(atom, connectedAtom, bonds[0], atomBondsDict)) {
                            methoxymOxigen.KetoneOxygen = atom;
                            methoxymOxigen.NextCarbonToKetoneOxygen = connectedAtom;
                            methoxymOxigen.ConnectedBond = bonds[0];
                            methoxymOxigen.Moiety = KetoneMoiety.IsKetone;
                        }
                        else {
                            return false;
                        }

                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            else {
                return false;
            }
            return false;
        }

        private static bool isReactiveMeox(IAtom ketoneOxygen, IAtom ketoneCarbon, IBond ketoneBond, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            var ketoneCarbonBonds = atomBondsDict[ketoneCarbon];
            foreach (var bond in ketoneCarbonBonds) {
                if (bond.Id == ketoneBond.Id) continue; // go to another bond from ketone oxygen : -R "-" C=O

                foreach (var atom in bond.Atoms) {
                    if (atom.Id == ketoneCarbon.Id) continue; // go to another atom from OH:  -"R" - C=O
                    if (atom.Symbol != "H" && atom.Symbol != "C") return false;
                }
            }

            return true;
        }

        private static bool IsAcidicProton(KeyValuePair<IAtom, List<IBond>> atomBondsPair, Dictionary<IAtom, List<IBond>> atomBondsDict, out AcidicProton acidicProton)
        {
            acidicProton = new AcidicProton();

            var atom = atomBondsPair.Key;
            var bonds = atomBondsPair.Value;

            if (bonds.Count == 1 && atom.Symbol == "H") { // this is the source code for terminal H

                foreach (var connectedAtom in bonds[0].Atoms) {
                    if (connectedAtom.Id == atom.Id) continue;
                    if (connectedAtom.Symbol == "N" || connectedAtom.Symbol == "O"
                        || connectedAtom.Symbol == "S" || connectedAtom.Symbol == "P") { // acidic proton should be next to O, N, P, or S

                        acidicProton.AcidicProtonAtom = atom;
                        acidicProton.NextAtomToAcidicProton = connectedAtom;
                        acidicProton.ConnectedBond = bonds[0];
                        acidicProton.Moiety = getAcidicProtonFunctionalMoiety(atom, connectedAtom, bonds[0], atomBondsDict);

                        return true;
                    }
                    else {
                        return false;
                    }
                }
            }
            else {
                return false;
            }

            return false;
        }

        private static AcidicProtonMoiety getAcidicProtonFunctionalMoiety(IAtom acidicProton, IAtom nextToAcidicProton, IBond connectedBond, Dictionary<IAtom, List<IBond>> atomBondsDict)
        {
            var symbol = nextToAcidicProton.Symbol;
            var nextAtomBonds = atomBondsDict[nextToAcidicProton];
            switch (symbol) {
                case "O":
                    return AcidicProtonMoiety.IsAlcohol;
                #region // if the detail of priority for acidic proton is required, please write this kind of code
                //foreach (var bond in nextAtomBonds) {
                //    if (bond.getID() == connectedBond.getID()) continue; // go to another bond from acidic proton : -R "-" OH

                //    foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>()) {
                //        if (atom.getID() == nextToAcidicProton.getID()) continue; // go to another atom from OH:  -"R" - OH

                //        var functionalAtom = atom;
                //        var functionalBonds = atomBondsDict[functionalAtom];
                //        var protonCount = 0;

                //        foreach (var functionalBond in functionalBonds) {
                //            foreach (var nextAtomToFunctionalAtom in functionalBond.atoms().ToWindowsEnumerable<IAtom>()) {
                //                if (nextAtomToFunctionalAtom.getID() == functionalAtom.getID()) continue;
                //                if (nextAtomToFunctionalAtom.getID() == nextToAcidicProton.getID()) continue;
                //                if (nextAtomToFunctionalAtom.getSymbol() == "H") protonCount++;
                //            }
                //        }
                //    }
                //}
                #endregion

                case "N":
                    return AcidicProtonMoiety.IsAmine;

                default:
                    return AcidicProtonMoiety.Others;
            }
        }

        public static string CreateMolecule()
        {
            var container = new AtomContainer();

            var atomC = new Atom(ChemicalElement.C);
            var atomH1 = new Atom(ChemicalElement.H);
            var atomH2 = new Atom(ChemicalElement.H);
            var atomH3 = new Atom(ChemicalElement.H);
            var atomH4 = new Atom(ChemicalElement.H);

            var bond_CH1 = new Bond(atomC, atomH1, BondOrder.Single);
            var bond_CH2 = new Bond(atomC, atomH2, BondOrder.Single);
            var bond_CH3 = new Bond(atomC, atomH3, BondOrder.Single);
            var bond_CH4 = new Bond(atomC, atomH4, BondOrder.Single);

            var atoms = new List<IAtom>()
            {
                atomC, atomH1, atomH2, atomH3, atomH4
            };

            var bonds = new List<IBond>()
            {
                bond_CH1, bond_CH2, bond_CH3, bond_CH4
            };

            container.SetAtoms(atoms.ToArray());
            container.SetBonds(bonds.ToArray());
            var smiles = smilesGenerator.Create(container);

            return smiles;
        }
    }
}
