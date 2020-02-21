using org.openscience.cdk.exception;
using org.openscience.cdk.graph;
using org.openscience.cdk.interfaces;
using org.openscience.cdk.silent;
using org.openscience.cdk.smiles;
using org.openscience.cdk.smsd.tools;
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
                var smilesParser = new SmilesParser(SilentChemObjectBuilder.getInstance());
                container = smilesParser.parseSmiles(smiles);
                if (container != null && smiles.Contains('c')) {
                    Kekulization.Kekulize(container);
                }
            }
            catch (InvalidSmilesException) {
                error += "SMILES: cannot be converted.\r\n";
                return null;
            }

            if (!ConnectivityChecker.isConnected(container)) {
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

            derivatizedContainer = ExtAtomContainerManipulator.convertExplicitToImplicitHydrogens(derivatizedContainer);

            var sg = new SmilesGenerator();
            var derivatizedSMILES = sg.createSMILES(derivatizedContainer);

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
                var smilesParser = new SmilesParser(org.openscience.cdk.silent.SilentChemObjectBuilder.getInstance());
                atomContainer = smilesParser.parseSmiles(smiles);
            }
            catch (InvalidSmilesException) {
                return null;
            }

            if (!ConnectivityChecker.isConnected(atomContainer)) return null;

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

            var acidicProtons = new List<AcidicProton>();
            var methoxymOxigens = new List<MethoxymOxigen>();
            var atomBondsDict = MoleculeConverter.GetAtomBondsDictionary(atomContainer); // get atom to bonds dictionary

            var numberedContainer = moleculeNumberingWithReactionPlaceCount(atomContainer, atomBondsDict, out acidicProtons, out methoxymOxigens);

            if (acidicProtons.Count < tmsCount || methoxymOxigens.Count < meoxCount) return null;
            var derivatizedContainer = ConvertToDerivatizedCompound(numberedContainer, atomBondsDict, acidicProtons, methoxymOxigens, tmsCount, meoxCount);

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
                atomContainer.removeAtom(meoxOxygen.KetoneOxygen);
                atomContainer.removeBond(meoxOxygen.ConnectedBond);

                //create new bond
                var newBond = new Bond(meoxOxygen.NextCarbonToKetoneOxygen, meoxMoiety.getAtom(0), IBond.Order.DOUBLE);
                atomContainer.add(meoxMoiety);
                atomContainer.addBond(newBond);

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
                        if (acidicProton.NextAtomToAcidicProton.getSymbol() == "N") continue;
                        break;
                    case 1: // check primary amines: if 'amineDerivativeLevel == 1', -NH2 moiety is converted to NH-TMS.
                        if (primaryAminesChecklist.Contains(acidicProton.NextAtomToAcidicProton) && acidicProton.NextAtomToAcidicProton.getSymbol() == "N") continue;
                        else primaryAminesChecklist.Add(acidicProton.NextAtomToAcidicProton);
                        break;
                    case 2:
                        break;
                    default:
                        break;
                }

                //remove atom and bond of acidic proton
                atomContainer.removeAtom(acidicProton.AcidicProtonAtom);
                atomContainer.removeBond(acidicProton.ConnectedBond);

                //create new bond
                var tmsMoiety = getTmsMoiety(); //preparation of TMS moiety
                var newBond = new Bond(acidicProton.NextAtomToAcidicProton, tmsMoiety.getAtom(0), IBond.Order.SINGLE);
                atomContainer.add(tmsMoiety);
                atomContainer.addBond(newBond);

                counter++;
            }

            return atomContainer;
        }

        private static IAtomContainer getTmsMoiety()
        {
            var container = new AtomContainer();

            #region // create atoms
            var atomSi = new Atom(new Element("Si"));
            var atomC1 = new Atom(new Element("C"));
            var atomC2 = new Atom(new Element("C"));
            var atomC3 = new Atom(new Element("C"));

            var atomH1forC1 = new Atom(new Element("H"));
            var atomH2forC1 = new Atom(new Element("H"));
            var atomH3forC1 = new Atom(new Element("H"));

            var atomH1forC2 = new Atom(new Element("H"));
            var atomH2forC2 = new Atom(new Element("H"));
            var atomH3forC2 = new Atom(new Element("H"));

            var atomH1forC3 = new Atom(new Element("H"));
            var atomH2forC3 = new Atom(new Element("H"));
            var atomH3forC3 = new Atom(new Element("H"));
            #endregion

            #region // create bonds
            var bondSiC1 = new Bond(atomSi, atomC1, IBond.Order.SINGLE);
            var bondSiC2 = new Bond(atomSi, atomC2, IBond.Order.SINGLE);
            var bondSiC3 = new Bond(atomSi, atomC3, IBond.Order.SINGLE);

            var bondC1H1 = new Bond(atomC1, atomH1forC1, IBond.Order.SINGLE);
            var bondC1H2 = new Bond(atomC1, atomH2forC1, IBond.Order.SINGLE);
            var bondC1H3 = new Bond(atomC1, atomH3forC1, IBond.Order.SINGLE);

            var bondC2H1 = new Bond(atomC2, atomH1forC2, IBond.Order.SINGLE);
            var bondC2H2 = new Bond(atomC2, atomH2forC2, IBond.Order.SINGLE);
            var bondC2H3 = new Bond(atomC2, atomH3forC2, IBond.Order.SINGLE);

            var bondC3H1 = new Bond(atomC3, atomH1forC3, IBond.Order.SINGLE);
            var bondC3H2 = new Bond(atomC3, atomH2forC3, IBond.Order.SINGLE);
            var bondC3H3 = new Bond(atomC3, atomH3forC3, IBond.Order.SINGLE);
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

            container.setAtoms(atoms.ToArray());
            container.setBonds(bonds.ToArray());

            return container;
        }

        private static IAtomContainer getMeoxMoierty()
        {
            var container = new AtomContainer();

            #region // create atoms
            var atomN = new Atom(new Element("N"));
            var atomO = new Atom(new Element("O"));
            var atomC = new Atom(new Element("C"));
            var atomH1 = new Atom(new Element("H"));
            var atomH2 = new Atom(new Element("H"));
            var atomH3 = new Atom(new Element("H"));
            #endregion

            #region // create bonds
            var bondNO = new Bond(atomN, atomO, IBond.Order.SINGLE);
            var bondOC = new Bond(atomO, atomC, IBond.Order.SINGLE);
            var bondCH1 = new Bond(atomC, atomH1, IBond.Order.SINGLE);
            var bondCH2 = new Bond(atomC, atomH2, IBond.Order.SINGLE);
            var bondCH3 = new Bond(atomC, atomH3, IBond.Order.SINGLE);
            #endregion

            var atoms = new List<IAtom>()
            {
                atomN, atomO, atomC, atomH1, atomH2, atomH3
            };

            var bonds = new List<IBond>()
            {
                bondNO, bondOC, bondCH1, bondCH2, bondCH3
            };

            container.setAtoms(atoms.ToArray());
            container.setBonds(bonds.ToArray());

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

            if (bonds.Count == 1 && bonds[0].getOrder() == IBond.Order.DOUBLE && atom.getSymbol() == "O") { //this is the source code for C=O

                foreach (var connectedAtom in bonds[0].atoms().ToWindowsEnumerable<IAtom>()) {
                    if (connectedAtom.getID() == atom.getID()) continue;
                    if (connectedAtom.getSymbol() == "C") {

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
                if (bond.getID() == ketoneBond.getID()) continue; // go to another bond from ketone oxygen : -R "-" C=O

                foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>()) {
                    if (atom.getID() == ketoneCarbon.getID()) continue; // go to another atom from OH:  -"R" - C=O
                    if (atom.getSymbol() != "H" && atom.getSymbol() != "C") return false;
                }
            }

            return true;
        }

        private static bool IsAcidicProton(KeyValuePair<IAtom, List<IBond>> atomBondsPair, Dictionary<IAtom, List<IBond>> atomBondsDict, out AcidicProton acidicProton)
        {
            acidicProton = new AcidicProton();

            var atom = atomBondsPair.Key;
            var bonds = atomBondsPair.Value;

            if (bonds.Count == 1 && atom.getSymbol() == "H") { // this is the source code for terminal H

                foreach (var connectedAtom in bonds[0].atoms().ToWindowsEnumerable<IAtom>()) {
                    if (connectedAtom.getID() == atom.getID()) continue;
                    if (connectedAtom.getSymbol() == "N" || connectedAtom.getSymbol() == "O"
                        || connectedAtom.getSymbol() == "S" || connectedAtom.getSymbol() == "P") { // acidic proton should be next to O, N, P, or S

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
            var symbol = nextToAcidicProton.getSymbol();
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

            var atomC = new Atom(new Element("C"));
            var atomH1 = new Atom(new Element("H"));
            var atomH2 = new Atom(new Element("H"));
            var atomH3 = new Atom(new Element("H"));
            var atomH4 = new Atom(new Element("H"));

            var bond_CH1 = new Bond(atomC, atomH1, IBond.Order.SINGLE);
            var bond_CH2 = new Bond(atomC, atomH2, IBond.Order.SINGLE);
            var bond_CH3 = new Bond(atomC, atomH3, IBond.Order.SINGLE);
            var bond_CH4 = new Bond(atomC, atomH4, IBond.Order.SINGLE);

            var atoms = new List<IAtom>()
            {
                atomC, atomH1, atomH2, atomH3, atomH4
            };

            var bonds = new List<IBond>()
            {
                bond_CH1, bond_CH2, bond_CH3, bond_CH4
            };

            container.setAtoms(atoms.ToArray());
            container.setBonds(bonds.ToArray());

            var sg = new SmilesGenerator();
            var smi = sg.createSMILES(container);

            return smi.ToString();
        }
    }
}
