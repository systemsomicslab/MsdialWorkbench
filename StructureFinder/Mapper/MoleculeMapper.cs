using org.openscience.cdk.interfaces;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using org.openscience.cdk.ringsearch;
using java.lang;
using org.openscience.cdk.aromaticity;
using System.Diagnostics;
using org.openscience.cdk.config;
using org.openscience.cdk.exception;
using Riken.Metabolomics.StructureFinder.Descriptor;
using org.openscience.cdk;
using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Database;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class MoleculeMapper 
    {
        private MoleculeMapper() { }

        public static void MapMolecule(IAtomContainer container, 
            out Dictionary<int, AtomProperty> atomDictionary, 
            out Dictionary<int, BondProperty> bondDictionary,
            out Dictionary<int, RingProperty> ringDictionary,
            out Dictionary<int, RingsetProperty> ringsetDictionary,
            out StructureFunctionType structureType, 
            out MolecularFingerprint molecularDescriptor,
            out string smiles)
        {
            atomDictionary = new Dictionary<int, AtomProperty>();
            bondDictionary = new Dictionary<int, BondProperty>();
            ringDictionary = new Dictionary<int, RingProperty>();
            ringsetDictionary = new Dictionary<int, RingsetProperty>();
            structureType = StructureFunctionType.Other;
            molecularDescriptor = new MolecularFingerprint();

            var sw = new Stopwatch();

            sw.Start();
            //Debug.WriteLine("Numbering start");
            NumberingMolecule(container, atomDictionary, bondDictionary); //numbering
            //Debug.WriteLine("Lap: {0}", sw.ElapsedMilliseconds);
            //sw.Restart();

            //Debug.WriteLine("Setting atom- and bond properties");
            SetAtomAndBondProperties(atomDictionary, bondDictionary); //set atom- and bond properties
            //Debug.WriteLine("Lap: {0}", sw.ElapsedMilliseconds);
            //sw.Restart();

            //Debug.WriteLine("Set atom function types");
            AtomRecognition.SetAtomProperties(atomDictionary, molecularDescriptor);
            //Debug.WriteLine("Lap: {0}", sw.ElapsedMilliseconds);
            //sw.Restart();

            //Debug.WriteLine("Ring recognition");
            RingRecognition.SetRingProperties(container, atomDictionary, bondDictionary, 
                ringDictionary, ringsetDictionary, molecularDescriptor); //ring recognition
            //Debug.WriteLine("Lap: {0}", sw.ElapsedMilliseconds);
            //sw.Restart();

            //Debug.WriteLine("Bond recognition");
            BondRecognition.SetBondProperties(atomDictionary, bondDictionary, ringDictionary, ringsetDictionary, molecularDescriptor);
            // Debug.WriteLine("Lap: {0}", sw.ElapsedMilliseconds);
            //sw.Restart();

            smiles = MoleculeConverter.AtomContainerToSmiles(container);

            //var infoArray = molecularDescriptor.GetType().GetProperties();
            //foreach (var info in infoArray) {
            //    if ((int)info.GetValue(molecularDescriptor, null) == 1) {
            //        Debug.WriteLine(info.Name);
            //    }
            //}
            //Debug.WriteLine("Structure recognition");

            //var nContainer = MoleculeConverter.DictionaryToAtomContainer(atomDictionary, bondDictionary);
            //MoleculeImage.MoleculeImageInConsoleApp(nContainer, 800, 800, true);


            //foreach (var atomProp in atomDictionary.Values) {

            //    Debug.WriteLine("atom ID {0}, atom string {1}, atom function type {2}",
            //        atomProp.AtomID, atomProp.AtomString, atomProp.AtomFunctionType.ToString());
            //}

            //#region ring detector and atom- and bond property settings
            //// do ring detection with the original molecule
            //// Set a really large timeout, because we don't want to crash just because it took a long time.
            //// The size limit of 7 below should stop it looping forever.
            //if (isAromaticRecognition) {
            //    var allRingsFinder = new AllRingsFinder();
            //    allRingsFinder.setTimeout(10000);

            //    try {
            //        var allRings = allRingsFinder.findAllRings(container, Integer.valueOf(15));
            //        if (allRings.getAtomContainerCount() > 0) {
                        
            //            for (int i = 0; i < allRings.getAtomContainerCount(); i++) {
            //                var ring = allRings.getAtomContainer(i);
            //                ring.setID(i.ToString());
            //            }

            //            foreach (var element in bondDictionary) {
            //                var bondId = element.Key;
            //                var bondProp = element.Value;

            //                #region //lets see if it is a ring and aromatic
            //                var rings = allRings.getRings(bondProp.IBond);
            //                if (rings.getAtomContainerCount() == 0) {
            //                    bondProp.IsAromaticity = false;
            //                    bondProp.IsInRing = false;
            //                    bondProp.RingID = -1;
            //                }
            //                else {
            //                    bondProp.IsInRing = true;
            //                    bondProp.RingID = int.Parse(rings.getAtomContainer(0).getID());
            //                    for (int i = 0; i < rings.getAtomContainerCount(); i++) {
            //                        var ring = rings.getAtomContainer(i);
            //                        var isAromatic = getAromaticity(ring);
            //                        if (isAromatic == true) {
            //                            bondProp.IsAromaticity = true;
            //                            break;
            //                        }
            //                    }
            //                }
            //                #endregion
            //            }
            //        }
            //    }
            //    catch (CDKException e) {
            //        Debug.WriteLine("Molecule was too complex, handle error");
            //    }
            //}
            //#endregion

            

            //setBondDescriptorsAndLikelihood(atomDictionary, bondDictionary);
            //foreach (var bond in bondDictionary) {
            //    Debug.WriteLine("Bond ID {0}, BondString {1}", bond.Key, MoleculeConverter.BondPropertyToString(bond.Value));
            //    foreach (var descriptor in bond.Value.Descriptor) {
            //        Debug.WriteLine("Descript {0}, Count {1}", descriptor.Key, descriptor.Value);
            //    }
            //}
        }

        public static List<string> GetAssignedSubstructureInChIKeys(MolecularFingerprint descriptor) {

            var inchikeys = new List<string>();
            var infoArray = descriptor.GetType().GetProperties();
            foreach (var info in infoArray) {
                if ((int)info.GetValue(descriptor, null) == 1) {
                    if (info.Name.Length == 14)
                        inchikeys.Add(info.Name);
                }
            }

            return inchikeys;
        }

        public static void SetAtomAndBondProperties(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var dictTemp = new Dictionary<int, List<BondProperty>>();
            foreach (var element in bondDictionary) {
                var bondId = element.Key;
                var bondProp = element.Value;

                //set atom prop
                foreach (var atom in bondProp.IBond.atoms().ToWindowsEnumerable<IAtom>()) {
                    var atomID = int.Parse(atom.getID());

                    bondProp.ConnectedAtoms.Add(atomDictionary[atomID]);
                    if (!dictTemp.ContainsKey(atomID)) {
                        dictTemp[atomID] = new List<BondProperty>();
                    }
                    dictTemp[atomID].Add(bondProp);
                }
            }

            //set bond prop
            foreach (var element in atomDictionary) {
                var atomID = element.Key;
                var atomProp = element.Value;

                atomProp.ConnectedBonds = dictTemp[atomID];
                atomProp.AtomValence = 0;
                foreach (var bond in atomProp.ConnectedBonds) {
                    atomProp.AtomValence += (int)bond.BondType + 1;
                }
            }
        }

        public static List<string> GetAssignedFragmentOntologies(List<string> inchikeys) {
            if (inchikeys == null || inchikeys.Count == 0) return new List<string>();
            var ontologies = new List<string>();

            foreach (var key in inchikeys) {

                var ontology = InChIKeyOntologyPairs.Pairs[key];
                if (!ontologies.Contains(ontology))
                    ontologies.Add(ontology);
            }

            return ontologies;
        }

        public static void NumberingMolecule(IAtomContainer container, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var countAtom = 1;
            var countBond = 0;
            var alreadyDone = new List<IAtom>();

            foreach (var bond in container.bonds().ToWindowsEnumerable<IBond>()) {

                bond.setID(countBond.ToString(CultureInfo.InvariantCulture));

                var bondProp = getBondProperty(bond);
                bondDictionary[countBond] = bondProp;
                countBond++;

                foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>()) {
                    if (!alreadyDone.Contains(atom)) {

                        if (atom.getHybridization() == IAtom.Hybridization.PLANAR3 && atom.getSymbol() == "N") {
                            atom.setHybridization(IAtom.Hybridization.SP3);
                        }
                        atom.setID(countAtom.ToString(CultureInfo.InvariantCulture));

                        var atomProp = getAtomProperty(atom);
                        atomDictionary[countAtom] = atomProp;
                        countAtom++;

                        alreadyDone.Add(atom);
                    }
                }
            }
        }

        //private static void setBondDescriptorsAndLikelihood(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        //{
        //    bool isMildDescription = true;
        //    foreach (var zbond in bondDictionary) {

        //        #region //test code 1
        //        var zBondID = zbond.Key;
        //        var zeroBondProp = zbond.Value;

        //        if (zbond.Value.ConnectedAtoms[0].AtomString == "H" || zbond.Value.ConnectedAtoms[1].AtomString == "H") continue;

        //        zbond.Value.Descriptor = BondDescriptor.BondDescriptorDictionary();

        //        var zBondDescripter = zbond.Value.Descriptor;
        //        var zeroBondTypeString = StructureEnumConverter.BondTypeToString(zeroBondProp.BondType);
        //        var zeroBondString = MoleculeConverter.BondPropertyToString(zeroBondProp, true);
        //        if (zBondDescripter.ContainsKey(zeroBondString)) zBondDescripter[zeroBondString]++;

        //        foreach (var zeroTargetAtom in zeroBondProp.ConnectedAtoms) {
                    
        //            var zeroTargetAtomID = zeroTargetAtom.AtomID;
        //            var zeroTargetAtomString = zeroTargetAtom.AtomString;
        //            if (isMildDescription) zeroTargetAtomString = convertAtomStringForHalogen(zeroTargetAtomString);

        //            var zeroSourceAtom = zeroBondProp.ConnectedAtoms.Where(n => n.AtomID != zeroTargetAtomID).ToList()[0];
        //            var zeroSourceAtomID = zeroSourceAtom.AtomID;
        //            var zeroSourceAtomString = atomDictionary[zeroSourceAtomID].AtomString;
        //            if (isMildDescription) zeroSourceAtomString = convertAtomStringForHalogen(zeroSourceAtomString);

        //            var fBonds = zeroTargetAtom.ConnectedBonds.Where(n => n.BondID != zBondID);
        //            if (fBonds.Count() == 0) continue;

        //            foreach (var fBond in fBonds) {

        //                var firstBondID = fBond.BondID;
        //                var firstBondTypeString = StructureEnumConverter.BondTypeToString(fBond.BondType);

        //                var firstAtom = fBond.ConnectedAtoms.Where(n => n.AtomID != zeroTargetAtomID).ToList()[0];
        //                var firstAtomID = firstAtom.AtomID;
        //                var firstAtomString = firstAtom.AtomString;
        //                if (isMildDescription) firstAtomString = convertAtomStringForHalogen(firstAtomString);

        //                var firstBondString = zeroSourceAtomString + zeroBondTypeString + zeroTargetAtomString + firstBondTypeString + firstAtomString;
        //                if (zBondDescripter.ContainsKey(firstBondString)) zBondDescripter[firstBondString]++;

        //                var sBonds = firstAtom.ConnectedBonds.Where(n => n.BondID != firstBondID);
        //                if (sBonds.Count() == 0) continue;

        //                foreach (var sBond in sBonds) {

        //                    var sBondID = sBond.BondID;
        //                    var secondBondTypeString = StructureEnumConverter.BondTypeToString(sBond.BondType);

        //                    var secondAtom = sBond.ConnectedAtoms.Where(n => n.AtomID != firstAtomID).ToList()[0];
        //                    var secondAtomID = secondAtom.AtomID;
        //                    var secondAtomString = secondAtom.AtomString;
        //                    if (isMildDescription) secondAtomString = convertAtomStringForAll(secondAtomString);

        //                    var secondBondString = firstBondString + secondBondTypeString + secondAtomString;

        //                    if (zBondDescripter.ContainsKey(secondBondString)) zBondDescripter[secondBondString]++;
        //                }
        //            }
        //        }
        //        #endregion

        //        zeroBondProp.CleavageLikelihood = 1.0F;
        //    }
        //}

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

        private static bool getAromaticity(IAtomContainer ring)
        {
            var aromatic = CDKHueckelAromaticityDetector.detectAromaticity(ring);
            if (aromatic) {
                return true;
            }
            else {
                for (int i = 0; i < ring.getAtomCount(); i++) {
                    var atom = ring.getAtom(i);

                    //Debug.WriteLine("Hybridization {0}\tAtomTypeName {1}", atom.getHybridization().toString().ToString(), atom.getAtomTypeName());
                    if (atom.getHybridization() != IAtomType.Hybridization.SP2 && atom.getHybridization() != IAtomType.Hybridization.PLANAR3
                        && atom.getAtomTypeName() != "N.planar3" && atom.getAtomTypeName() != "N.amide") return false;
                }
                return true;
                #region
                //var bondCount = ring.getAtomCount();
                //var doubleBondCount = 0;
                //foreach (var bond in ring.bonds().ToWindowsEnumerable<IBond>()) {
                //    if (bond == null) continue;
                //    if (bond.getOrder() == IBond.Order.DOUBLE) doubleBondCount++;
                //    else if (bond.getOrder() == IBond.Order.SINGLE) {
                //        foreach (var atom in bond.atoms().ToWindowsEnumerable<IAtom>()) {
                //            if (atom == null) continue;
                //            if (atom.getSymbol() == "N" && atom.getAtomTypeName() == "N.amide") {
                //                doubleBondCount++;
                //                break;
                //            }
                //        }
                //    }
                //}

                //if (bondCount <= 6 && doubleBondCount >= 2) return true;
                //else return false;
                #endregion
            }
        }

        //here, atomfunctionType is defined for oxygen
        private static AtomProperty getAtomProperty(IAtom atom)
        {
            //Debug.WriteLine(atom.getID() + "\t" + atom.getSymbol() + "\t" + atom.getFormalCharge() + "\t" + atom.getExactMass());

            return new AtomProperty() {
                IAtom = atom,
                AtomID = int.Parse(atom.getID()),
                AtomString = atom.getSymbol(),
                AtomCharge = atom.getFormalCharge().intValue(),
                AtomMass = atom.getExactMass().floatValue(),
                AtomPriority = MoleculeConverter.GetAtomPriorityValue(atom.getSymbol())
            };
        }

        private static BondProperty getBondProperty(IBond bond)
        {
            return new BondProperty() {
                IBond = bond,
                BondID = int.Parse(bond.getID()), 
                BondDirection = CdkConverter.ConvertToCSharpBondDirection(bond.getStereo()), 
                BondType = CdkConverter.ConvertToCSharpBondType(bond.getOrder()),
                CleavageLikelihood = 1.0F, 
            };
        }

        public static bool MapAtomMass(IAtomContainer container, List<LabeledAtom> labeledAtoms = null)
        {
            for (int i = 0; i < container.getAtomCount(); i++) {
                var iAtom = container.getAtom(i);

                try {
                    IsotopeFactory.getInstance(iAtom.getBuilder()).configure(iAtom);

                    if (labeledAtoms != null && labeledAtoms.Count != 0) {
                        replaceAtomMass(iAtom, labeledAtoms);
                    }
                }
                catch (IllegalArgumentException) {
                    // This means it failed to get the mass for this element, so its an unknown element like "R" for example
                    return false;
                }
            }

            return true;
        }

        private static void replaceAtomMass(IAtom iAtom, List<LabeledAtom> labeledAtoms)
        {
            var atomSymbol = iAtom.getSymbol();
            foreach (var labeledAtom in labeledAtoms) {
                if (atomSymbol == labeledAtom.AtomString) {
                    iAtom.setExactMass(java.lang.Double.valueOf(labeledAtom.ReplacedMass));
                    break;
                }
            }
        }
    }
}
