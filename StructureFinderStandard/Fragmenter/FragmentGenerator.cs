using Riken.Metabolomics.StructureFinder.Property;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using Riken.Metabolomics.StructureFinder.Utility;
using NCDK;
using NCDK.Default;
using NCDK.Config;

namespace Riken.Metabolomics.StructureFinder.Fragmenter
{
    public class SplitBonds
    {
        private int treeDepth;
        private List<BondProperty> bonds;

        public SplitBonds()
        {
            bonds = new List<BondProperty>();
        }

        public int TreeDepth
        {
            get { return treeDepth; }
            set { treeDepth = value; }
        }

        public List<BondProperty> Bonds
        {
            get { return bonds; }
            set { bonds = value; }
        }
    }

    public sealed class FragmentGenerator
    {
        private FragmentGenerator() { }

        public static List<Fragment> GetFragmentCandidates(Structure structure, int treeDepthMax, double massMin)
        {

            //Console.WriteLine(structure.Title);
            var parentExactMass = structure.ExactMass;
            var fragmentCandidates = new List<Fragment>();
            
            var fragmentDoneList = new List<string>(); //defined as connected atoms and bonds hash key

            var treeDepth = 0;
            var parentID = 0;
            var productID = 0;

            var fragmentCandidate = new Fragment(treeDepth, parentID, productID, 
			                                     structure.AtomDictionary, structure.BondDictionary, 
			                                     new Dictionary<int, AtomProperty>(), new Dictionary<int, BondProperty>());
            fragmentCandidates.Add(fragmentCandidate);

            if (treeDepthMax == 0) return fragmentCandidates;

            var splitableBonds = getSplitableBonds(structure.BondDictionary, structure.RingDictionary, parentExactMass);
            if (splitableBonds.Count == 0) return fragmentCandidates;

            var frequentlySplitableBonds = getFrequentlySplitableBonds(structure.BondDictionary);
            var splitBondsList = getSplitBondsList(splitableBonds, treeDepthMax, frequentlySplitableBonds);
			foreach (var splitBonds in splitBondsList) {
				foreach (var bond in splitBonds.Bonds) {
					foreach (var atom in bond.ConnectedAtoms) {

						fragmentCandidate = new Fragment();
						fragmentCandidate = RecFragmentGerenator(fragmentCandidate, atom, splitBonds.Bonds, structure.AtomDictionary, structure.BondDictionary);
                        if (fragmentCandidate.CleavedBondDictionary.Count != splitBonds.Bonds.Count) continue; //check if fragment is done by means of the given bond pairs

                        var exactMass = fragmentCandidate.AtomDictionary.Values.Sum(n => n.AtomMass);
                        //Console.WriteLine(exactMass);
                        if (exactMass < 20 || exactMass < massMin - 30) continue;

                        var fragmentHash = generateFragmentHash(fragmentCandidate.AtomDictionary, fragmentCandidate.BondDictionary);
                        if (!fragmentDoneList.Contains(fragmentHash)) {
                            fragmentDoneList.Add(fragmentHash);
                            
                            fragmentCandidate.TreeDepth = splitBonds.TreeDepth;
                            fragmentCandidate.ExactMass = exactMass;
                            fragmentCandidate.BondDissociationEnergy = BondEnergyCalculator.TotalBondEnergy(fragmentCandidate.CleavedBondDictionary);
                            fragmentCandidates.Add(fragmentCandidate);
                        }
                    }
				}
			}

            ////check splitBondsList result
            //var counter = 0;
            //foreach (var splitBonds in splitBondsList) {
            //    var prop = counter.ToString() + "-Tree depth " + splitBonds.TreeDepth + " ";
            //    foreach (var bond in splitBonds.Bonds) {
            //        prop += bond.BondID + "  ";
            //    }
            //    Debug.WriteLine(prop);
            //    counter++;
            //}

            //check fragmentCandidates result
            //foreach (var fragment in fragmentCandidates) {
            //    var container = MoleculeConverter.DictionaryToAtomContainer(fragment.AtomDictionary, fragment.BondDictionary);
            //    var smiles = MoleculeConverter.AtomContainerToSmiles(container);

            //    Debug.WriteLine(fragment.TreeDepth + "\t" + fragment.ExactMass + "\t" + smiles);
            //}
            
            return fragmentCandidates.OrderBy(n => n.ExactMass).ToList();
        }

        
		/// <summary>
		/// Recursive code to generate fragments from the information of start atom, removed bonds, and structure information
		/// </summary>
		public static Fragment RecFragmentGerenator(Fragment fragment, AtomProperty startAtom, List<BondProperty> removedBonds, 
		                                     Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
		{
			var connectedBonds = atomDictionary[startAtom.AtomID].ConnectedBonds;

			foreach (var cBond in connectedBonds) {
				if (fragment.BondDictionary.ContainsKey(cBond.BondID)) continue;

				var isRemovedBond = false;
				foreach (var rBond in removedBonds) { // check bond connectivitie
					if (rBond.BondID == cBond.BondID) {

						if (!fragment.CleavedBondDictionary.ContainsKey(rBond.BondID)) {
							// register this removed bond, and replace the removed atom by H with the consideration of bond order

							fragment.CleavedBondDictionary[rBond.BondID] = rBond;
                            fragment.CleavedAtomDictionary[rBond.BondID] = startAtom; //do atom registration too

                            var isValenceFrexible = checkValenceFrexibility(startAtom, rBond);
                            if (isValenceFrexible == false) {
                                var rBondOrder = (int)rBond.BondType + 1;
                                var atomIdMax = fragment.AtomDictionary.Count > 0
                                    ? Math.Max(atomDictionary.Keys.Max(), fragment.AtomDictionary.Keys.Max())
                                    : atomDictionary.Keys.Max();
                                var bondIdMax = fragment.BondDictionary.Count > 0
                                    ? Math.Max(bondDictionary.Keys.Max(), fragment.BondDictionary.Keys.Max())
                                    : bondDictionary.Keys.Max();

                                //Debug.WriteLine(rBondOrder + "\t" + atomIdMax + "\t" + bondIdMax);

                                for (int i = 0; i < rBondOrder; i++) {
                                    atomIdMax++; bondIdMax++;

                                    //add new hydrogen atom- and its related bond
                                    insertHydrogenAtomAndBonds(fragment, startAtom, atomIdMax, bondIdMax);
                                }
                            }
						}

						isRemovedBond = true;
						break;
					}
				}
				if (isRemovedBond == true) continue;
                fragment.BondDictionary[cBond.BondID] = cBond;

                foreach (var cAtom in cBond.ConnectedAtoms) {
                    if (!fragment.AtomDictionary.ContainsKey(cAtom.AtomID))
                        fragment.AtomDictionary[cAtom.AtomID] = cAtom;
                }

				var nextAtom = cBond.ConnectedAtoms.Where(n => n.AtomID != startAtom.AtomID).ToList()[0];
				if (atomDictionary[nextAtom.AtomID].ConnectedBonds.Count == 1) continue;

				RecFragmentGerenator(fragment, nextAtom, removedBonds, atomDictionary, bondDictionary);
			}
			return fragment;
		}

        private static bool checkValenceFrexibility(AtomProperty atom, BondProperty removedBond)
        {
            var atomValence = atom.AtomValence;
            var removedBondOrder = (int)removedBond.BondType + 1;

            switch (atom.AtomString) {
                case "P":
                    if (atomValence - removedBondOrder == 3 || atomValence - removedBondOrder == 5) return true;
                    return false;
                case "S":
                    if (atomValence - removedBondOrder == 2 || atomValence - removedBondOrder == 4 || atomValence - removedBondOrder == 6) return true;
                    return false;
                default:
                    return false;
            }
        }

        /// <summary>
        /// cite (Atom) http://cdk.github.io/cdk/1.5/docs/api/org/openscience/cdk/Atom.html
        /// cite (Atomtype) http://cdk.github.io/cdk/1.5/docs/api/org/openscience/cdk/AtomType.html#hybridization
        /// </summary>
		private static void insertHydrogenAtomAndBonds(Fragment fragment, AtomProperty startAtom, int atomIdMax, int bondIdMax)
		{
            var iAtom = getHydrogenAtom();
            
			var nAtomH = new AtomProperty()
			{
				AtomID = atomIdMax,
				AtomCharge = 0,
				AtomMass = 1.007825035F,
				AtomString = "H",
                IAtom = iAtom
			};

			var nBond = new BondProperty()
			{
				BondID = bondIdMax,
				BondType = BondType.Single,
				BondDirection = BondDirection.None,
				IsAromaticity = false,
				IsInRing = false,
				RingID = -1,
                //IBond = new Bond(nAtomH.IAtom, startAtom.IAtom, IBond.Order.SINGLE),
                IBond = new Bond(nAtomH.IAtom, startAtom.IAtom, BondOrder.Single),
                ConnectedAtoms = new List<AtomProperty>() { nAtomH, startAtom }
			};

            nAtomH.ConnectedBonds = new List<BondProperty>() { nBond };

			fragment.AtomDictionary[atomIdMax] = nAtomH;
			fragment.BondDictionary[bondIdMax] = nBond;
		}

        private static Atom getHydrogenAtom()
        {
            //var iAtom = new Atom( new Element("H"));
            var iAtom = new Atom(ChemicalElement.H);

            iAtom.FormalCharge = 0;
            iAtom.Valency = 1;
            iAtom.FormalNeighbourCount = 1;
            iAtom.Hybridization = Hybridization.S;

            //IsotopeFactory.getInstance(iAtom.getBuilder()).configure(iAtom);
            //iAtom.setFormalCharge(java.lang.Integer.valueOf(0));
            //iAtom.setValency(java.lang.Integer.valueOf(1));
            //iAtom.setFormalNeighbourCount(java.lang.Integer.valueOf(1));
            //iAtom.setHybridization(IAtomType.Hybridization.S);

            return iAtom;
        }

		/// <summary>
		/// create split bond pairs
		/// two bonds will be insered at once when a bond is in ring.
		/// </summary>
		private static List<SplitBonds> getSplitBondsList(List<BondProperty> splitableBonds, int treeMax, List<BondProperty> freqFragBonds)
        {
            var splitDones = new List<string>();
            var splitBondsList = new List<SplitBonds>();
            var splitBondsListCopy = new List<SplitBonds>();

            var beginCount = 0;
            var endCount = 0;

            for (int i = 1; i <= treeMax + 1; i++) {
                if (i == 1) { // create empty list
                    splitBondsListCopy = new List<SplitBonds>();
                    foreach (var bond in splitableBonds) {
                        splitBondsListCopy.Add(new SplitBonds() { TreeDepth = i, Bonds = new List<BondProperty>() });
                    }
                }
                else {
                    //the previous bond pair list will be used as a start list of next candidates
                    splitBondsListCopy = getSplitBondsListCopy(splitBondsList, beginCount, endCount);
                }

                beginCount = splitBondsList.Count;
                if (i != treeMax + 1)
                    setSplitBonds(splitableBonds, splitBondsList, splitDones, splitBondsListCopy, i);
                else
                    setSplitBonds(splitableBonds, splitBondsList, splitDones, splitBondsListCopy, freqFragBonds);


                endCount = splitBondsList.Count;
            }

            return splitBondsList;
        }

        /// <summary>
        /// finally set one more bonds for those who has frequently monitored bond fragment
        /// </summary>
        private static void setSplitBonds(List<BondProperty> splitableBonds, List<SplitBonds> splitBondsList, 
            List<string> splitDones, List<SplitBonds> splitBondsListCopy, List<BondProperty> freqFragBonds)
        {
            foreach (var splitBonds in splitBondsListCopy) {
                foreach (var freqFrag in freqFragBonds) {

                    if (splitBondsList.Count > 5000) return; //now the bond split dictionary is limitted to reduce the computational time.

                    var flg = false;
                    foreach (var fBond in splitBonds.Bonds) {
                        if (fBond.BondID == freqFrag.BondID) {
                            flg = true; break;
                        }
                    }
                    if (flg) continue;

                    //make copy
                    var tempBonds = new SplitBonds() { TreeDepth = splitBonds.TreeDepth, Bonds = new List<BondProperty>() };
                    foreach (var bond in splitBonds.Bonds) tempBonds.Bonds.Add(bond);

                    var doneKey = splitDoneKeyGenerator(tempBonds.Bonds, freqFrag, null);
                    if (!splitDones.Contains(doneKey)) {
                        tempBonds.Bonds.Add(freqFrag);
                        splitDones.Add(doneKey);
                        splitBondsList.Add(tempBonds);
                    }
                }
            }
        }

        /// <summary>
        /// set bond pairs
        /// </summary>
        private static void setSplitBonds(List<BondProperty> splitableBonds, List<SplitBonds> splitBondsList, 
            List<string> splitDones, List<SplitBonds> splitBondsListCopy, int cycleID)
        {
            foreach (var splitBonds in splitBondsListCopy) {
                foreach (var sBond in splitableBonds) {
                    if (splitBonds.Bonds.Contains(sBond)) continue;
                    if (splitBondsList.Count > 5000) return; //now the bond split dictionary is limitted to reduce the computational time.
                    if (sBond.IsInRing == true && sBond.IsSharedBondInRings == false) {
                        foreach (var bondInRing in splitableBonds.Where(n => n.RingID == sBond.RingID)) {
                            if (bondInRing.BondID == sBond.BondID) continue;
                            if (cycleID > 1 && splitBonds.Bonds.Count(n => n.RingID == bondInRing.RingID) > 0) continue;

                            //make copy
                            var tempBonds = new SplitBonds() { TreeDepth = splitBonds.TreeDepth, Bonds = new List<BondProperty>() };
                            foreach (var bond in splitBonds.Bonds) tempBonds.Bonds.Add(bond);

                            var doneKey = splitDoneKeyGenerator(tempBonds.Bonds, sBond, bondInRing);
                            if (!splitDones.Contains(doneKey)) {
                                if (!tempBonds.Bonds.Contains(sBond))
                                    tempBonds.Bonds.Add(sBond);

                                if (!tempBonds.Bonds.Contains(bondInRing))
                                    tempBonds.Bonds.Add(bondInRing);
                                splitDones.Add(doneKey);

                                splitBondsList.Add(tempBonds);
                            }
                        }
                    }
                    else {
                        //make copy
                        var tempBonds = new SplitBonds() { TreeDepth = splitBonds.TreeDepth, Bonds = new List<BondProperty>() };
                        foreach (var bond in splitBonds.Bonds) tempBonds.Bonds.Add(bond);

                        var doneKey = splitDoneKeyGenerator(tempBonds.Bonds, sBond, null);
                        if (!splitDones.Contains(doneKey)) {
                            tempBonds.Bonds.Add(sBond);
                            splitDones.Add(doneKey);

                            splitBondsList.Add(tempBonds);
                        }
                    }
                }
            }
        }

		/// <summary>
		/// Generate split bonds hash key to check the duplicate generations
		/// </summary>
        private static string splitDoneKeyGenerator(List<BondProperty> listedBonds, BondProperty bond, BondProperty bondInRing = null)
        {
            var bondIDs = new List<int>();
            foreach (var dBond in listedBonds) bondIDs.Add(dBond.BondID);

            if (!bondIDs.Contains(bond.BondID))
                bondIDs.Add(bond.BondID);
            if (bondInRing != null && !bondIDs.Contains(bondInRing.BondID)) 
                bondIDs.Add(bondInRing.BondID);

            if (bondIDs.Count > 1) bondIDs.Sort();

            var hash = string.Empty;
            for (int i = 0; i < bondIDs.Count; i++) {
                if (i == bondIDs.Count - 1) hash += bondIDs[i].ToString();
                else hash += bondIDs[i].ToString() + "-";
            }

            return hash;
        }

        /// <summary>
        /// Newly copyed list will be used to determine the split bonds of next tree depth.
        /// </summary>
        private static List<SplitBonds> getSplitBondsListCopy(List<SplitBonds> splitBondsList, int beginCount, int endCount)
        {
            var splitBondsListCopy = new List<SplitBonds>();
            for (int i = beginCount; i < endCount; i++) {
               
                var copySplitBonds = new SplitBonds() { TreeDepth = splitBondsList[i].TreeDepth + 1, Bonds = new List<BondProperty>() };
                foreach (var bond in splitBondsList[i].Bonds) copySplitBonds.Bonds.Add(bond);

                splitBondsListCopy.Add(copySplitBonds);
            }

            return splitBondsListCopy;
        }

		/// <summary>
		/// Gets the splitable list.
		/// </summary>
        private static List<BondProperty> getSplitableBonds(Dictionary<int, BondProperty> bondDictionary, 
            Dictionary<int, RingProperty> ringDictionary, double exactMass)
        {
            var splitableBonds = new List<BondProperty>();

            foreach (var bond in bondDictionary.Values) {

                if (bond.IsAromaticity == true && !isInHeteroatomRingBond(bond, ringDictionary)) continue;
                if (bond.ConnectedAtoms.Count(n => n.AtomString == "H") > 0) continue;
                if (exactMass <= 400.0) {
                    if (bond.ConnectedAtoms.Count(n => n.AtomString == "O") > 0 && (int)bond.BondType + 1 > 1) continue; //exclude double bonds of keton body (R=O)
                }

                splitableBonds.Add(bond);
            }
            if (splitableBonds.Count == 0) return splitableBonds;
            splitableBonds = splitableBonds.OrderBy(n => n.BondID).ToList();

            return splitableBonds;
        }

        private static bool isInHeteroatomRingBond(BondProperty bond, 
            Dictionary<int, RingProperty> ringDictionary)
        {
            if (!bond.IsInRing) return true; //if no ring, return true

            foreach (var ringID in bond.SharedRingIDs) {
                var ring = ringDictionary[ringID];
                if (ring.RingFunctionType == RingFunctionType.Benzene) {
                    if (bond.BondType == BondType.Single && bond.ConnectedAtoms.Count(n => n.AtomFunctionType == AtomFunctionType.C_Ketone) > 0)
                        return true;
                    else
                        return false;
                }
            }

            foreach (var ringID in bond.SharedRingIDs) {
                var ring = ringDictionary[ringID];
                if (ring.ConnectedAtoms.Count - ring.RingEnv.CarbonInRing > 1) return true;
                if (ring.ConnectedAtoms.Count - ring.RingEnv.CarbonInRing == 1 &&
                    ring.ConnectedAtoms.Count(n => n.AtomString == "O") == 1) return true;
                if (ring.RingEnv.KetonOutRing > 0) return true;
            }
            return false;
        }

        /// <summary>
        /// check frequantly splitable bond's IDs
        /// currently, -OH, -COOH, -NH2, C=O, -PO3
        /// </summary>
        private static List<BondProperty> getFrequentlySplitableBonds(Dictionary<int, BondProperty> dictionary)
        {
            var freqFragBonds = new List<BondProperty>();
            foreach (var prop in dictionary) {
                var bondID = prop.Key;
                var bondProp = prop.Value;

                var atom1 = bondProp.ConnectedAtoms[0];
                var atom2 = bondProp.ConnectedAtoms[1];
                if (atom1.AtomString == "H" || atom2.AtomString == "H") continue;
                if (bondProp.BondType == BondType.Double) continue;

                if ((atom1.AtomFunctionType == AtomFunctionType.O_Hydroxy && atom2.AtomFunctionType != AtomFunctionType.C_Carboxylate) ||
                    (atom2.AtomFunctionType == AtomFunctionType.O_Hydroxy && atom1.AtomFunctionType != AtomFunctionType.C_Carboxylate)) {
                    if (!bondProp.IsSugarRingConnected)
                        freqFragBonds.Add(bondProp); //-OH for not in sugar
                }
                else if ((atom1.AtomFunctionType == AtomFunctionType.C_Carboxylate && atom2.AtomString == "C")
                    || (atom2.AtomFunctionType == AtomFunctionType.C_Carboxylate && atom1.AtomString == "C"))
                    freqFragBonds.Add(bondProp); //-COOH
                else if (atom1.AtomFunctionType == AtomFunctionType.N_PrimaryAmine || atom2.AtomFunctionType == AtomFunctionType.N_PrimaryAmine)
                    freqFragBonds.Add(bondProp); //-NH2
                else if ((atom1.AtomFunctionType == AtomFunctionType.P_Phosphodiester && atom2.AtomString == "O") ||
                    (atom2.AtomFunctionType == AtomFunctionType.P_Phosphodiester && atom1.AtomString == "O"))
                    freqFragBonds.Add(bondProp); //-PO3
                else if ((atom1.AtomFunctionType == AtomFunctionType.P_PhosphonatePO3 && atom2.AtomString == "O") ||
                    (atom2.AtomFunctionType == AtomFunctionType.P_PhosphonatePO3 && atom1.AtomString == "O"))
                    freqFragBonds.Add(bondProp); //-PO3
                else if ((atom1.AtomFunctionType == AtomFunctionType.P_PhosphonatePO4 && atom2.AtomString == "O") ||
                    (atom2.AtomFunctionType == AtomFunctionType.P_PhosphonatePO4 && atom1.AtomString == "O"))
                    freqFragBonds.Add(bondProp); //-PO3
            }

            return freqFragBonds;
        }

		/// <summary>
		/// Generates the fragment hash key to check the replicates.
		/// </summary>
		private static string generateFragmentHash(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var atomList = new List<int>();
            foreach (var element in atomDictionary) {
                var atomID = element.Key;
                var atomProperty = element.Value;

                if (atomProperty.AtomString == "H") continue;
                atomList.Add(atomID);
            }
            atomList.Sort();

            //var bondList = new List<int>();
            //foreach (var element in bondDictionary) {
            //    var bondID = element.Key;
            //    var bondProperty = element.Value;

            //    if (bondProperty.ConnectedAtoms.Count(n => n.AtomString == "H") > 0) continue;
            //    bondList.Add(bondID);
            //}
            //bondList.Sort();

            var hash = string.Empty;
            foreach (var atom in atomList) {
                hash += atom.ToString();
            }
            //hash += "-";
            //foreach (var bond in bondList) {
            //    hash += bond.ToString();
            //}

            return hash;
        }
    }

   
}
