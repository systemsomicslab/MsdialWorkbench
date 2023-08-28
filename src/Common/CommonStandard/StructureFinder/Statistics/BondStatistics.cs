using CompMs.Common.DataObj;
using CompMs.Common.Enum;
using CompMs.Common.StructureFinder.Fragmenter;
using CompMs.Common.StructureFinder.Property;
using CompMs.Common.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.StructureFinder.Statistics
{
    public sealed class BondStatistics
    {
        private BondStatistics() { }

        private static double protonMass = 1.00727646677;

        public static void BondEnvironmentalFingerprintGenerator(Structure structure, RawData rawdata, List<Fragment> fragments, List<PeakFragmentPair> peakFragmentPairs,
            out List<MatchedIon> matchedProductIons, out List<MatchedIon> matchedNeutralLosses, out List<string> unCleavedBondDescriptorsList)
        {
            matchedProductIons = new List<MatchedIon>();
            matchedNeutralLosses = new List<MatchedIon>();
            unCleavedBondDescriptorsList = new List<string>();

            var cleavedBondIDs = new List<int>();
            var exactMassOfStructure = structure.ExactMass;

            foreach (var peakFragPair in peakFragmentPairs)
            {

                if (peakFragPair.MatchedFragmentInfo.FragmentID == -1)
                    continue;
                var fragment = fragments[peakFragPair.MatchedFragmentInfo.FragmentID];
                if (fragment.TreeDepth == 0) continue;

                var cleavedAtoms = fragment.CleavedAtomDictionary;
                var cleavedBonds = fragment.CleavedBondDictionary;

                var saturatedFragMass = fragment.ExactMass;
                var matchedFragMass = peakFragPair.MatchedFragmentInfo.MatchedMass;
                var rearrangedHydrogens = peakFragPair.MatchedFragmentInfo.RearrangedHydrogen;
                var intensity = peakFragPair.Peak.Intensity;
                var mz = peakFragPair.Peak.Mass;
                var isAdductContained = peakFragPair.MatchedFragmentInfo.IsSolventAdductFragment;
                var assignedAdductMass = peakFragPair.MatchedFragmentInfo.AssignedAdductMass;

                var prCleavedAtomsString = string.Empty;
                var nlCleavedAtomsString = string.Empty;
                #region // cleaved atoms for ionized and neutral loss
                var prAtomBondPairPriories = new List<AtomBondPairPriority>();
                var nlAtomBondPairPriories = new List<AtomBondPairPriority>();
                foreach (var bond in cleavedBonds)
                {
                    var atom = cleavedAtoms[bond.Value.BondID];
                    var atomBondPair = atom.AtomString + StructureEnumConverter.BondTypeToString(bond.Value.BondType);
                    var atomBondPairPriority = MoleculeConverter.GetAtomBondPairPriorityValue(atom, bond.Value);
                    prAtomBondPairPriories.Add(new AtomBondPairPriority() { AtomBondPair = atomBondPair, Score = atomBondPairPriority });

                    var nlAtom = bond.Value.ConnectedAtoms.Where(n => n.AtomID != atom.AtomID).ToList()[0];
                    var nlAtomBondPair = nlAtom.AtomString + StructureEnumConverter.BondTypeToString(bond.Value.BondType);
                    var nlAtomBondPairPriority = MoleculeConverter.GetAtomBondPairPriorityValue(nlAtom, bond.Value);
                    nlAtomBondPairPriories.Add(new AtomBondPairPriority() { AtomBondPair = nlAtomBondPair, Score = nlAtomBondPairPriority });
                }
                prAtomBondPairPriories = prAtomBondPairPriories.OrderByDescending(n => n.Score).ToList();
                nlAtomBondPairPriories = nlAtomBondPairPriories.OrderByDescending(n => n.Score).ToList();

                foreach (var pair in prAtomBondPairPriories)
                {
                    prCleavedAtomsString += pair.AtomBondPair;
                }

                foreach (var pair in nlAtomBondPairPriories)
                {
                    nlCleavedAtomsString += pair.AtomBondPair;
                }
                #endregion

                var bondDescriptorsList = new List<string>();
                #region // bond descriptor info
                foreach (var cBond in cleavedBonds)
                {

                    var cBondID = cBond.Key;
                    if (!cleavedBondIDs.Contains(cBondID)) cleavedBondIDs.Add(cBondID);

                    var descriptorString = string.Empty;
                    var bondDiscriptors = structure.BondDictionary[cBondID].Descriptor;

                    foreach (var discript in bondDiscriptors)
                    {
                        descriptorString += discript.Value + "\t";
                    }
                    bondDescriptorsList.Add(descriptorString);
                }
                #endregion

                if (fragment.TreeDepth == 1 && structure.BondDictionary.Count > fragment.BondDictionary.Count)
                {
                    #region // neutral loss info
                    var nFragment = new Fragment();
                    var cBondProp = cleavedBonds.Values.ToList()[0];
                    var tAtom = cleavedAtoms[cBondProp.BondID];
                    var nAtom = cBondProp.ConnectedAtoms.Where(n => n.AtomID != tAtom.AtomID).ToList()[0];

                    nFragment = FragmentGenerator.RecFragmentGerenator(nFragment, nAtom, cleavedBonds.Values.ToList(), structure.AtomDictionary, structure.BondDictionary);

                    var protonOffset = rawdata.IonMode == IonMode.Positive ? protonMass : -1 * protonMass;
                    if (rawdata.PrecursorType.Contains("[M]")) protonOffset = 0;

                    var nContainer = MoleculeConverter.DictionaryToAtomContainer(nFragment.AtomDictionary, nFragment.BondDictionary);
                    var nNeutralLossExactMass = nFragment.AtomDictionary.Sum(n => n.Value.AtomMass);
                    if (nNeutralLossExactMass < 12) continue;

                    var nExperimentalLoss = exactMassOfStructure + protonOffset - (matchedFragMass - assignedAdductMass);
                    var nMassDifference = nExperimentalLoss - nNeutralLossExactMass; //Experimental loss - theoretical loss
                    var nSmiles = MoleculeConverter.AtomContainerToSmiles(nContainer);

                    matchedNeutralLosses.Add(new MatchedIon()
                    {
                        Exactmass = nNeutralLossExactMass,
                        MatchedIntensity = (int)intensity,
                        MatchedMz = mz,
                        MatchedMass = nExperimentalLoss,
                        IsAdductContained = isAdductContained,
                        AssignedAdductMass = assignedAdductMass,
                        RearrangedHydrogen = nMassDifference,
                        Smiles = nSmiles,
                        TreeDepth = fragment.TreeDepth,
                        CleavedAtomBonds = nlCleavedAtomsString,
                        CleavedCount = cleavedBonds.Count,
                        BondDescriptorsList = bondDescriptorsList
                    });
                    #endregion
                }



                var pContainer = MoleculeConverter.DictionaryToAtomContainer(fragment.AtomDictionary, fragment.BondDictionary);
                var pSmiles = MoleculeConverter.AtomContainerToSmiles(pContainer);
                #region // product ion info
                matchedProductIons.Add(new MatchedIon()
                {
                    Exactmass = saturatedFragMass,
                    MatchedIntensity = (int)intensity,
                    MatchedMz = mz,
                    MatchedMass = matchedFragMass,
                    RearrangedHydrogen = rearrangedHydrogens,
                    IsAdductContained = isAdductContained,
                    AssignedAdductMass = assignedAdductMass,
                    Smiles = pSmiles,
                    TreeDepth = fragment.TreeDepth,
                    CleavedAtomBonds = prCleavedAtomsString,
                    CleavedCount = cleavedBonds.Count,
                    BondDescriptorsList = bondDescriptorsList
                });
                #endregion

            }

            ////get uncleavaged bonds too
            //foreach (var bond in structure.BondDictionary) {
            //    var bondID = bond.Key;
            //    if (cleavedBondIDs.Contains(bondID)) continue;
            //    if (bond.Value.ConnectedAtoms[0].AtomString == "H" || bond.Value.ConnectedAtoms[1].AtomString == "H") continue;

            //    var descriptorString = string.Empty;
            //    var bondDiscriptors = structure.BondDictionary[bondID].Descriptor;

            //    var atom1 = bond.Value.ConnectedAtoms[0];
            //    var atom2 = bond.Value.ConnectedAtoms[1];

            //    var fragment1 = new Fragment();
            //    fragment1 = FragmentGenerator.RecFragmentGerenator(fragment1, atom1, new List<BondProperty>() { bond.Value }, structure.AtomDictionary, structure.BondDictionary);
            //    var fragment1Mass = fragment1.AtomDictionary.Sum(n => n.Value.AtomMass);

            //    var fragment2 = new Fragment();
            //    fragment2 = FragmentGenerator.RecFragmentGerenator(fragment2, atom2, new List<BondProperty>() { bond.Value }, structure.AtomDictionary, structure.BondDictionary);
            //    var fragment2Mass = fragment1.AtomDictionary.Sum(n => n.Value.AtomMass);

            //    if (fragment1Mass < fragment2Mass) {
            //        descriptorString += fragment2Mass + "\t" + fragment1Mass + "\t";
            //    }
            //    else {
            //        descriptorString += fragment1Mass + "\t" + fragment2Mass + "\t";
            //    }

            //    foreach (var discript in bondDiscriptors) {
            //        descriptorString += discript.Value + "\t";
            //    }
            //    unCleavedBondDescriptorsList.Add(descriptorString);
            //}
        }

        public static void BondConnectivityListGenerator(Structure structure, out List<string> zeroBondPaths,
            out List<string> firstBondPaths, out List<string> secondBondPaths, out List<string> thirdBondPaths)
        {
            var atomDictionary = structure.AtomDictionary;
            var bondDictionary = structure.BondDictionary;
            zeroBondPaths = new List<string>();
            firstBondPaths = new List<string>();
            secondBondPaths = new List<string>();
            thirdBondPaths = new List<string>();

            bool isMildDescription = true;

            foreach (var zbond in bondDictionary)
            {
                var zeroBondID = zbond.Key;
                var zeroBondProp = zbond.Value;

                if (zbond.Value.ConnectedAtoms[0].AtomString == "H" || zbond.Value.ConnectedAtoms[1].AtomString == "H") continue;

                var zeroBondTypeString = StructureEnumConverter.BondTypeToString(zeroBondProp.BondType);
                var zeroBondString = MoleculeConverter.BondPropertyToString(zeroBondProp, true);
                if (!zeroBondPaths.Contains(zeroBondString)) zeroBondPaths.Add(zeroBondString);

                foreach (var zeroTargetAtom in zeroBondProp.ConnectedAtoms)
                {

                    var zeroTargetAtomID = zeroTargetAtom.AtomID;
                    var zeroTargetAtomString = zeroTargetAtom.AtomString;
                    if (isMildDescription) zeroTargetAtomString = convertAtomStringForHalogen(zeroTargetAtomString);

                    var zeroSourceAtom = zeroBondProp.ConnectedAtoms.Where(n => n.AtomID != zeroTargetAtomID).ToList()[0];
                    var zeroSourceAtomID = zeroSourceAtom.AtomID;
                    var zeroSourceAtomString = atomDictionary[zeroSourceAtomID].AtomString;
                    if (isMildDescription) zeroSourceAtomString = convertAtomStringForHalogen(zeroSourceAtomString);

                    var fBonds = zeroTargetAtom.ConnectedBonds.Where(n => n.BondID != zeroBondID);
                    if (fBonds.Count() == 0) continue;

                    foreach (var fBond in fBonds)
                    {
                        var firstBondID = fBond.BondID;
                        var firstBondTypeString = StructureEnumConverter.BondTypeToString(fBond.BondType);

                        var firstAtom = fBond.ConnectedAtoms.Where(n => n.AtomID != zeroTargetAtomID).ToList()[0];
                        var firstAtomID = firstAtom.AtomID;
                        var firstAtomString = firstAtom.AtomString;
                        if (isMildDescription) firstAtomString = convertAtomStringForHalogen(firstAtomString);

                        var firstBondString = zeroSourceAtomString + zeroBondTypeString + zeroTargetAtomString + firstBondTypeString + firstAtomString;
                        if (!firstBondPaths.Contains(firstBondString)) firstBondPaths.Add(firstBondString);

                        var sBonds = firstAtom.ConnectedBonds.Where(n => n.BondID != firstBondID);
                        if (sBonds.Count() == 0) continue;

                        foreach (var sBond in sBonds)
                        {

                            var sBondID = sBond.BondID;
                            var secondBondTypeString = StructureEnumConverter.BondTypeToString(sBond.BondType);

                            var secondAtom = sBond.ConnectedAtoms.Where(n => n.AtomID != firstAtomID).ToList()[0];
                            var secondAtomID = secondAtom.AtomID;
                            var secondAtomString = secondAtom.AtomString;
                            if (isMildDescription) secondAtomString = convertAtomStringForAll(secondAtomString);

                            var secondBondString = firstBondString + secondBondTypeString + secondAtomString;
                            if (!secondBondPaths.Contains(secondBondString)) secondBondPaths.Add(secondBondString);

                            var tBonds = secondAtom.ConnectedBonds.Where(n => n.BondID != sBondID);
                            if (tBonds.Count() == 0) continue;

                            foreach (var thirdBond in tBonds)
                            {
                                var thirdBondID = thirdBond.BondID;
                                var thirdBondTypeString = StructureEnumConverter.BondTypeToString(thirdBond.BondType);

                                var thirdAtom = thirdBond.ConnectedAtoms.Where(n => n.AtomID != secondAtomID).ToList()[0];
                                var thirdAtomString = thirdAtom.AtomString;
                                if (isMildDescription) thirdAtomString = convertAtomStringForAll(thirdAtomString);

                                var thirdBondString = secondBondString + thirdBondTypeString + thirdAtomString;
                                if (!thirdBondPaths.Contains(thirdBondString)) thirdBondPaths.Add(thirdBondString);
                            }
                        }
                    }
                }
            }
        }

        private static string convertAtomStringForHalogen(string atomString)
        {
            if (atomString == "F" || atomString == "Cl" || atomString == "Br" || atomString == "I") return "X";
            return atomString;
        }

        private static string convertAtomStringForAll(string atomString)
        {
            if (atomString == "F" || atomString == "Cl" || atomString == "Br" || atomString == "I") return "X";
            //if (atomString == "C" || atomString == "Si") return "C|Si";
            if (atomString == "N" || atomString == "O") return "N|O";
            if (atomString == "P" || atomString == "S") return "P|S";
            return atomString;
        }

    }

    public class AtomBondPairPriority
    {
        private string atomBondPair;
        private int score;

        public AtomBondPairPriority() { atomBondPair = string.Empty; score = 0; }

        public string AtomBondPair
        {
            get { return atomBondPair; }
            set { atomBondPair = value; }
        }

        public int Score
        {
            get { return score; }
            set { score = value; }
        }

    }

    public class MatchedIon
    {
        private string smiles;
        private double exactmass;
        private double matchedMass;
        private double rearrangedHydrogen;
        private double matchedMz;
        private int matchedIntensity;
        private int treeDepth;
        private int cleavedCount;
        private bool isAdductContained;
        private double assignedAdductMass;

        private string cleavedAtomBonds;
        private List<string> bondDescriptorsList;

        public MatchedIon()
        {
            bondDescriptorsList = new List<string>();
        }

        public List<string> BondDescriptorsList
        {
            get { return bondDescriptorsList; }
            set { bondDescriptorsList = value; }
        }

        public int CleavedCount
        {
            get { return cleavedCount; }
            set { cleavedCount = value; }
        }

        public string CleavedAtomBonds
        {
            get { return cleavedAtomBonds; }
            set { cleavedAtomBonds = value; }
        }

        public double AssignedAdductMass
        {
            get { return assignedAdductMass; }
            set { assignedAdductMass = value; }
        }

        public int TreeDepth
        {
            get { return treeDepth; }
            set { treeDepth = value; }
        }

        public string Smiles
        {
            get { return smiles; }
            set { smiles = value; }
        }

        public bool IsAdductContained
        {
            get { return isAdductContained; }
            set { isAdductContained = value; }
        }

        public double Exactmass
        {
            get { return exactmass; }
            set { exactmass = value; }
        }

        public double MatchedMass
        {
            get { return matchedMass; }
            set { matchedMass = value; }
        }

        public double RearrangedHydrogen
        {
            get { return rearrangedHydrogen; }
            set { rearrangedHydrogen = value; }
        }

        public double MatchedMz
        {
            get { return matchedMz; }
            set { matchedMz = value; }
        }

        public int MatchedIntensity
        {
            get { return matchedIntensity; }
            set { matchedIntensity = value; }
        }
    }
}
