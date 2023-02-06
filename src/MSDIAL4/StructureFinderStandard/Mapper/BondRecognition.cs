using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Descriptor;
using Riken.Metabolomics.StructureFinder.Property;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class BondRecognition
    {
        private BondRecognition() { }

        public static void SetBondProperties(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, MolecularFingerprint descriptor)
        {
            foreach (var bond in bondDictionary) {
                var bondID = bond.Key;
                var bondProp = bond.Value;

                var connectedAtom1 = bondProp.ConnectedAtoms[0];
                var connectedAtom2 = bondProp.ConnectedAtoms[1];

                setBasicBondProperties(bondProp, connectedAtom1, connectedAtom2, ringDictionary, descriptor);
            }

            //set atom aromatic environment
            foreach (var atom in atomDictionary) {
                foreach (var atomProp in atomDictionary.Values) {
                    foreach (var bondProp in atomProp.ConnectedBonds) {
                        if (bondProp.IsAromaticity) {
                            var connectedAtom = bondProp.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                            var cAtomString = connectedAtom.AtomString;
                    var atomEnv = atomProp.AtomEnv;
                            if (cAtomString == "C") atomEnv.AromaticCarbonCount++;
                            else if (cAtomString == "N") atomEnv.AromaticNitrogenCount++;
                            else if (cAtomString == "O") atomEnv.AromaticOxygenCount++;
                            else if (cAtomString == "S") atomEnv.AromaticSulfurCount++;
                        }
                    }
                }
            }

            //set bond environment
            foreach (var bondProp in bondDictionary.Values) {
                var bondEnv = new BondBasicEnvironmentProperty();
                setBondPropertiesAndMolecularDescriptors(bondProp, bondEnv, ringDictionary, ringsetDictionary, descriptor);
                bondProp.BondEnv = bondEnv;
            }
        }
        
        private static void setBasicBondProperties(BondProperty bondProp, 
            AtomProperty connectedAtom1, AtomProperty connectedAtom2, 
            Dictionary<int, RingProperty> ringDictionary, MolecularFingerprint descriptor)
        {
            if (connectedAtom1.AtomString == "H" || connectedAtom2.AtomString == "H") return;

            if (bondProp.IsInRing) { //check if the bond is in aromatic ring
                var ringIDs = bondProp.SharedRingIDs;
                foreach (var ringID in ringIDs) {
                    var ringProp = ringDictionary[ringID];
                    if (ringProp.IsAromaticRing) {
                        bondProp.IsAromaticity = true;
                        break;
                    }
                }
            }
            else { //check ring connections
                if (connectedAtom1.IsInRing) {

                    var ringIDs = connectedAtom1.SharedRingIDs;
                    foreach (var ringID in ringIDs){
                        var ringProp = ringDictionary[ringID];
                        if (ringProp.IsBenzeneRing) bondProp.IsBenzeneRingConnected = true;
                        if (ringProp.IsHeteroRing) bondProp.IsHeteroRingConnected = true;
                        if (ringProp.IsSugarRing) {
                            bondProp.IsSugarRingConnected = true;
                        }
                        if (ringProp.IsAromaticRing) bondProp.IsAromaticRingConnected = true;
                    }
                }
                if (connectedAtom2.IsInRing) {

                    var ringIDs = connectedAtom2.SharedRingIDs;
                    foreach (var ringID in ringIDs) {
                        var ringProp = ringDictionary[ringID];
                        if (ringProp.IsBenzeneRing) bondProp.IsBenzeneRingConnected = true;
                        if (ringProp.IsHeteroRing) bondProp.IsHeteroRingConnected = true;
                        if (ringProp.IsSugarRing) {
                            bondProp.IsSugarRingConnected = true;
                        }
                        if (ringProp.IsAromaticRing) bondProp.IsAromaticRingConnected = true;
                    }
                }
            }
            
        }

        /// <summary>
        /// get bond basic environment property and set descriptors section 6
        /// </summary>
        private static void setBondPropertiesAndMolecularDescriptors(
            BondProperty bond1, BondBasicEnvironmentProperty bondEnv, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, 
            MolecularFingerprint descriptor)
        {
            //connectedAtoms[0] is defined as source atom, and another is defined as target atom
            //to efficiently get molecular descriptors of more than section 6 (to describe long atom connectivity)
            //I created a little bit complicated source code...
            foreach (var atom1 in bond1.ConnectedAtoms) {

                PubChemFingerprint.SetfSection3Properties(descriptor, bond1);

                //set current bond strings
                var atom2 = bond1.ConnectedAtoms.Where(n => n.AtomID != atom1.AtomID).ToList()[0];
                var bond1String = StructureEnumConverter.BondTypeToString(bond1.BondType, bond1.IsAromaticity);
                var connectString1 = atom1.AtomString + bond1String + atom2.AtomString;
                var initialString = atom1.AtomString + bond1String;

                MassFragmentFingerprint.SetProperties(descriptor, connectString1, atom1, atom2, bond1, ringDictionary, ringsetDictionary);
                MassFragmentFingerprint.FarryAcidsDescriptors(descriptor, atom1, atom2, bond1);

                //get first bond away from the current bond
                var bonds2 = atom2.ConnectedBonds.Where(n => n.BondID != bond1.BondID);
                if (bonds2.Count() == 0) continue;

                foreach (var bond2 in bonds2) {
                    #region set second bond's bond environment
                    if (bond2.IsAromaticity) bondEnv.First_AromaticbondCount++;
                    switch (bond2.BondType) {
                        case BondType.Single: bondEnv.First_SinglebondCount++; break;
                        case BondType.Double: bondEnv.First_DoublebondCount++; break;
                        case BondType.Triple: bondEnv.First_TriplebondCount++; break;
                    }
                    #endregion

                    //set (source atom)-(target)-(fAtom) strings
                    var bond2String = StructureEnumConverter.BondTypeToString(bond2.BondType, bond2.IsAromaticity);
                    var atom3 = bond2.ConnectedAtoms.Where(n => n.AtomID != atom2.AtomID).ToList()[0];
                    var connectString2 = connectString1 + bond2String + atom3.AtomString;

                    PubChemFingerprint.SetSection7Properties(descriptor, connectString2,
                            atom1, atom2, atom3,
                            bond1, bond2, ringDictionary);

                    MassFragmentFingerprint.SetProperties(descriptor, connectString2,
                           atom1, atom2, atom3,
                           bond1, bond2, ringDictionary, ringsetDictionary);

                    MassFragmentFingerprint.FarryAcidsDescriptors(descriptor, atom1, atom2, atom3,
                        bond1, bond2);


                    #region set second bond's "atom" environment
                    switch (atom3.AtomString) {
                        case "C": bondEnv.First_CarbonCount++; break;
                        case "N": bondEnv.First_NitrogenCount++; break;
                        case "O": bondEnv.First_OxygenCount++; break;
                        case "S": bondEnv.First_SulfurCount++; break;
                        case "P": bondEnv.First_PhosphorusCount++; break;
                        case "F": bondEnv.First_HalogenCount++; break;
                        case "Cl": bondEnv.First_HalogenCount++; break;
                        case "Br": bondEnv.First_HalogenCount++; break;
                        case "I": bondEnv.First_HalogenCount++; break;
                        case "Si": bondEnv.First_SiliconCount++; break;
                    }
                    #endregion

                    //get second bond far away from the current bond
                    var bonds3 = atom3.ConnectedBonds.Where(n => n.BondID != bond2.BondID);
                    if (bonds3.Count() == 0) continue;

                    foreach (var bond3 in bonds3) {
                        #region set second bond's bond environment
                        if (bond3.IsAromaticity) bondEnv.Second_AromaticbondCount++;
                        switch (bond3.BondType) {
                            case BondType.Single: bondEnv.Second_SinglebondCount++; break;
                            case BondType.Double: bondEnv.Second_DoublebondCount++; break;
                            case BondType.Triple: bondEnv.Second_TriplebondCount++; break;
                        }
                        #endregion

                        //set (source atom)-(target)-(fAtom)-(sAtom) strings
                        var bond3String = StructureEnumConverter.BondTypeToString(bond3.BondType, bond3.IsAromaticity);
                        var atom4 = bond3.ConnectedAtoms.Where(n => n.AtomID != atom3.AtomID).ToList()[0];

                        if (atom4.AtomID == atom1.AtomID || atom4.AtomID == atom2.AtomID) //from here, we have to consider the same atom by the cicle effect
                            continue;

                        var connectString3 = connectString2 + bond3String + atom4.AtomString;
                        PubChemFingerprint.SetSection6Properties(descriptor, connectString3, initialString, 4);

                        PubChemFingerprint.SetSection6Properties(descriptor, connectString3, 
                            atom1, atom2, atom3, atom4,
                            bond1, bond2, bond3);

                        PubChemFingerprint.SetSection7Properties(descriptor, connectString3,
                            atom1, atom2, atom3, atom4,
                            bond1, bond2, bond3, ringDictionary);

                        MassFragmentFingerprint.SetProperties(descriptor, connectString3,
                            atom1, atom2, atom3, atom4,
                            bond1, bond2, bond3, ringDictionary, ringsetDictionary);

                        MassFragmentFingerprint.FarryAcidsDescriptors(descriptor, atom1, atom2, atom3, atom4,
                        bond1, bond2, bond3);

                       
                        #region set second bond's "atom" environment
                        switch (atom4.AtomString) {
                            case "C": bondEnv.Second_CarbonCount++; break;
                            case "N": bondEnv.Second_NitrogenCount++; break;
                            case "O": bondEnv.Second_OxygenCount++; break;
                            case "S": bondEnv.Second_SulfurCount++; break;
                            case "P": bondEnv.Second_PhosphorusCount++; break;
                            case "F": bondEnv.Second_HalogenCount++; break;
                            case "Cl": bondEnv.Second_HalogenCount++; break;
                            case "Br": bondEnv.Second_HalogenCount++; break;
                            case "I": bondEnv.Second_HalogenCount++; break;
                            case "Si": bondEnv.Second_SiliconCount++; break;
                        }
                        #endregion

                        if (atom1.AtomString == "H" && connectString2 != "H-C-C") continue;

                        //get third bond far way from the current bond
                        var bonds4 = atom4.ConnectedBonds.Where(n => n.BondID != bond3.BondID);
                        if (bonds4.Count() == 0) continue;

                        foreach (var bond4 in bonds4) {
                            
                            //set (source atom)-(target)-(fAtom)-(sAtom)-(tAtom) strings
                            var bond4String = StructureEnumConverter.BondTypeToString(bond4.BondType, bond4.IsAromaticity);
                            var atom5 = bond4.ConnectedAtoms.Where(n => n.AtomID != atom4.AtomID).ToList()[0];

                            if (atom5.AtomID == atom1.AtomID || atom5.AtomID == atom2.AtomID || atom5.AtomID == atom3.AtomID)
                                continue;

                            var connectString4 = connectString3 + bond4String + atom5.AtomString;
                            
                            PubChemFingerprint.SetSection6Properties(descriptor, connectString4, initialString, 5);
                            
                            PubChemFingerprint.SetSection6Properties(descriptor, connectString4,
                               atom1, atom2, atom3, atom4, atom5,
                               bond1, bond2, bond3, bond4);

                            MassFragmentFingerprint.SetProperties(descriptor, connectString4,
                               atom1, atom2, atom3, atom4, atom5,
                               bond1, bond2, bond3, bond4, 
                               ringDictionary, ringsetDictionary);

                            MassFragmentFingerprint.FarryAcidsDescriptors(descriptor, 
                                atom1, atom2, atom3, atom4, atom5,
                                bond1, bond2, bond3, bond4);

                            if (atom1.AtomString == "C" || atom1.AtomString == "N" || atom1.AtomString == "O" 
                                || atom1.AtomString == "P" || atom1.AtomString == "S") {
                                var bonds5 = atom5.ConnectedBonds.Where(n => n.BondID != bond4.BondID);
                                
                                if (bonds5.Count() == 0) continue;

                                //get fourth bond far way from the current bond
                                foreach (var bond5 in bonds5) {
                                    //set (source atom)-(target)-(fAtom)-(sAtom)-(tAtom)-(foAtom) strings
                                    var bond5String = StructureEnumConverter.BondTypeToString(bond5.BondType, bond5.IsAromaticity);
                                    var atom6 = bond5.ConnectedAtoms.Where(n => n.AtomID != atom5.AtomID).ToList()[0];

                                    if (atom6.AtomID == atom1.AtomID || atom6.AtomID == atom2.AtomID || atom6.AtomID == atom3.AtomID ||
                                        atom6.AtomID == atom4.AtomID)
                                        continue;

                                    var connectString5 = connectString4 + bond5String + atom6.AtomString;
                                    PubChemFingerprint.SetSection6Properties(descriptor, connectString5, initialString, 6);
                                    
                                    MassFragmentFingerprint.SetProperties(descriptor, connectString5,
                                       atom1, atom2, atom3, atom4, atom5, atom6,
                                       bond1, bond2, bond3, bond4, bond5, ringDictionary, ringsetDictionary);

                                    MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                        atom1, atom2, atom3, atom4, atom5, atom6,
                                        bond1, bond2, bond3, bond4, bond5);

                                    var bonds6 = atom6.ConnectedBonds.Where(n => n.BondID != bond5.BondID);
                                    if (bonds6.Count() == 0) continue;

                                    foreach (var bond6 in bonds6) {
                                       
                                        //set (source atom)-(target)-(fAtom)-(sAtom)-(tAtom)-(foAtom)-(fifAtom) strings
                                        var bond6String = StructureEnumConverter.BondTypeToString(bond6.BondType, bond6.IsAromaticity);
                                        var atom7 = bond6.ConnectedAtoms.Where(n => n.AtomID != atom6.AtomID).ToList()[0];

                                        if (atom7.AtomID == atom1.AtomID || atom7.AtomID == atom2.AtomID || atom7.AtomID == atom3.AtomID ||
                                            atom7.AtomID == atom4.AtomID || atom7.AtomID == atom5.AtomID)
                                            continue;

                                        var connectString6 = connectString5 + bond6String + atom7.AtomString;

                                        PubChemFingerprint.SetSection6Properties(descriptor, connectString6, initialString, 7);

                                        MassFragmentFingerprint.SetProperties(descriptor, connectString6,
                                            atom1, atom2, atom3, atom4, atom5, atom6, atom7,
                                            bond1, bond2, bond3, bond4, bond5, bond6, ringDictionary, ringsetDictionary);

                                        PubChemFingerprint.SetSection6Properties(descriptor, connectString6,
                                            atom1, atom2, atom3, atom4, atom5, atom6, atom7,
                                            bond1, bond2, bond3, bond4, bond5, bond6);

                                        MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                            atom1, atom2, atom3, atom4, atom5, atom6, atom7,
                                            bond1, bond2, bond3, bond4, bond5, bond6);

                                        //from here, the bonds containing Carbons only are examined for saponin, fatty acids, etc.
                                        if(atom1.AtomString == "C" && atom2.AtomString == "C" && atom3.AtomString == "C" && 
                                           atom4.AtomString == "C" && atom5.AtomString == "C" && atom6.AtomString == "C" && atom7.AtomString == "C") {

                                            var bonds7 = atom7.ConnectedBonds.Where(n => n.BondID != bond6.BondID);
                                            if (bonds7.Count() == 0) continue;

                                            foreach (var bond7 in bonds7) {

                                                var bond7String = StructureEnumConverter.BondTypeToString(bond7.BondType, bond7.IsAromaticity);
                                                var atom8 = bond7.ConnectedAtoms.Where(n => n.AtomID != atom7.AtomID).ToList()[0];

                                                if (atom8.AtomID == atom1.AtomID || atom8.AtomID == atom2.AtomID || atom8.AtomID == atom3.AtomID ||
                                                    atom8.AtomID == atom4.AtomID || atom8.AtomID == atom5.AtomID || atom8.AtomID == atom6.AtomID)
                                                    continue;

                                                var connectString7 = connectString6 + bond7String + atom8.AtomString;


                                                MassFragmentFingerprint.SetProperties(descriptor, connectString7,
                                                               atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, 
                                                               bond1, bond2, bond3, bond4, bond5, bond6, bond7, 
                                                               ringDictionary, ringsetDictionary);

                                                MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                                    atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8,
                                                    bond1, bond2, bond3, bond4, bond5, bond6, bond7);

                                                if (atom8.AtomString == "C") {

                                                    var bonds8 = atom8.ConnectedBonds.Where(n => n.BondID != bond7.BondID);
                                                    if (bonds8.Count() == 0) continue;

                                                    foreach (var bond8 in bonds8) {
                                                       
                                                        var bond8String = StructureEnumConverter.BondTypeToString(bond8.BondType, bond8.IsAromaticity);
                                                        var atom9 = bond8.ConnectedAtoms.Where(n => n.AtomID != atom8.AtomID).ToList()[0];

                                                        if (atom9.AtomID == atom1.AtomID || atom9.AtomID == atom2.AtomID || atom9.AtomID == atom3.AtomID ||
                                                            atom9.AtomID == atom4.AtomID || atom9.AtomID == atom5.AtomID || atom9.AtomID == atom6.AtomID ||
                                                            atom9.AtomID == atom7.AtomID)
                                                            continue;

                                                        var connectString8 = connectString7 + bond8String + atom9.AtomString;

                                                        MassFragmentFingerprint.LongChainAcylDescriptors(descriptor, connectString8,
                                                                atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9,
                                                                bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, ringDictionary, ringsetDictionary);

                                                        MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                                            atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9,
                                                            bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8);

                                                        if (atom9.AtomString == "C") {

                                                            var bonds9 = atom9.ConnectedBonds.Where(n => n.BondID != bond8.BondID);
                                                            if (bonds9.Count() == 0) continue;

                                                            foreach (var bond9 in bonds9) {

                                                                var bond9String = StructureEnumConverter.BondTypeToString(bond9.BondType, bond9.IsAromaticity);
                                                                var atom10 = bond9.ConnectedAtoms.Where(n => n.AtomID != atom9.AtomID).ToList()[0];
                                                                if (atom10.AtomID == atom1.AtomID || atom10.AtomID == atom2.AtomID || atom10.AtomID == atom3.AtomID ||
                                                                    atom10.AtomID == atom4.AtomID || atom10.AtomID == atom5.AtomID || atom10.AtomID == atom6.AtomID ||
                                                                    atom10.AtomID == atom7.AtomID || atom10.AtomID == atom8.AtomID)
                                                                    continue;

                                                                var connectString9 = connectString8 + bond9String + atom10.AtomString;

                                                                MassFragmentFingerprint.SetProperties(descriptor, connectString9,
                                                                atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10,
                                                                bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, ringDictionary, ringsetDictionary);

                                                                MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                                                  atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10,
                                                                  bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9);

                                                                if (atom10.AtomString == "C") {

                                                                    var bonds10 = atom10.ConnectedBonds.Where(n => n.BondID != bond9.BondID);
                                                                    if (bonds10.Count() == 0) continue;

                                                                    foreach (var bond10 in bonds10) {

                                                                        var bond10String = StructureEnumConverter.BondTypeToString(bond10.BondType, bond10.IsAromaticity);
                                                                        var atom11 = bond10.ConnectedAtoms.Where(n => n.AtomID != atom10.AtomID).ToList()[0];

                                                                        if (atom11.AtomID == atom1.AtomID || atom11.AtomID == atom2.AtomID || atom11.AtomID == atom3.AtomID ||
                                                                            atom11.AtomID == atom4.AtomID || atom11.AtomID == atom5.AtomID || atom11.AtomID == atom6.AtomID ||
                                                                            atom11.AtomID == atom7.AtomID || atom11.AtomID == atom8.AtomID || atom11.AtomID == atom9.AtomID)
                                                                            continue;

                                                                        var connectString10 = connectString9 + bond10String + atom11.AtomString;

                                                                        MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                                                          atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11,
                                                                          bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10);

                                                                        MassFragmentFingerprint.LongChainAcylDescriptors(descriptor, connectString10,
                                                                          atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11,
                                                                          bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10, 
                                                                          ringDictionary, ringsetDictionary);

                                                                        if (atom11.AtomString == "C") {

                                                                            var bonds11 = atom11.ConnectedBonds.Where(n => n.BondID != bond10.BondID);
                                                                            if (bonds11.Count() == 0) continue;

                                                                            foreach (var bond11 in bonds11) {

                                                                                var bond11String = StructureEnumConverter.BondTypeToString(bond11.BondType, bond11.IsAromaticity);
                                                                                var atom12 = bond11.ConnectedAtoms.Where(n => n.AtomID != atom11.AtomID).ToList()[0];

                                                                                if (atom12.AtomID == atom1.AtomID || atom12.AtomID == atom2.AtomID || atom12.AtomID == atom3.AtomID ||
                                                                                    atom12.AtomID == atom4.AtomID || atom12.AtomID == atom5.AtomID || atom12.AtomID == atom6.AtomID ||
                                                                                    atom12.AtomID == atom7.AtomID || atom12.AtomID == atom8.AtomID || atom12.AtomID == atom9.AtomID ||
                                                                                    atom12.AtomID == atom10.AtomID)
                                                                                    continue;

                                                                                var connectString11 = connectString10 + bond11String + atom12.AtomString;

                                                                                MassFragmentFingerprint.FarryAcidsDescriptors(descriptor,
                                                                                  atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11, atom12,
                                                                                  bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10, bond11);

                                                                                if (atom12.AtomString == "C") {

                                                                                    var bonds12 = atom12.ConnectedBonds.Where(n => n.BondID != bond11.BondID);
                                                                                    if (bonds12.Count() == 0) continue;

                                                                                    foreach (var bond12 in bonds12) {

                                                                                        var bond12String = StructureEnumConverter.BondTypeToString(bond12.BondType, bond12.IsAromaticity);
                                                                                        var atom13 = bond12.ConnectedAtoms.Where(n => n.AtomID != atom12.AtomID).ToList()[0];

                                                                                        if (atom13.AtomID == atom1.AtomID || atom13.AtomID == atom2.AtomID || atom13.AtomID == atom3.AtomID ||
                                                                                            atom13.AtomID == atom4.AtomID || atom13.AtomID == atom5.AtomID || atom13.AtomID == atom6.AtomID ||
                                                                                            atom13.AtomID == atom7.AtomID || atom13.AtomID == atom8.AtomID || atom13.AtomID == atom9.AtomID ||
                                                                                            atom13.AtomID == atom10.AtomID || atom13.AtomID == atom11.AtomID)
                                                                                            continue;

                                                                                        var connectString12 = connectString11 + bond12String + atom13.AtomString;

                                                                                        MassFragmentFingerprint.SetProperties(descriptor, connectString8,
                                                                                            atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11, atom12, atom13,
                                                                                            bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10, bond11, bond12,
                                                                                            ringDictionary, ringsetDictionary);

                                                                                        //from here, only for long chain contained metabolites
                                                                                        if (atom13.AtomString == "C" && !atom2.IsInRing && !atom3.IsInRing && !atom4.IsInRing && !atom5.IsInRing
                                                                                            && !atom6.IsInRing && !atom7.IsInRing && !atom8.IsInRing && !atom9.IsInRing && !atom10.IsInRing
                                                                                            && !atom11.IsInRing && !atom12.IsInRing) {

                                                                                            var atoms = new List<AtomProperty>() {atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9,
                                                                                            atom10, atom11, atom12, atom13 };
                                                                                            var bonds = new List<BondProperty>() {bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, 
                                                                                            bond10, bond11, bond12};

                                                                                            recAtomConnections(atoms, bonds, descriptor, ringDictionary, ringsetDictionary);
                                                                                            #region
                                                                                            //var bonds13 = atom13.ConnectedBonds.Where(n => n.BondID != bond12.BondID);
                                                                                            //if (bonds13.Count() == 0) continue;

                                                                                            //foreach (var bond13 in bonds13) {

                                                                                            //    var bond13String = StructureEnumConverter.BondTypeToString(bond13.BondType, bond13.IsAromaticity);
                                                                                            //    var atom14 = bond13.ConnectedAtoms.Where(n => n.AtomID != atom13.AtomID).ToList()[0];
                                                                                            //    var connectString13 = connectString12 + bond13String + atom14.AtomString;

                                                                                            //    if (atom14.AtomString == "C" && !atom13.IsInRing) {

                                                                                            //        var bonds14 = atom14.ConnectedBonds.Where(n => n.BondID != bond13.BondID);
                                                                                            //        if (bonds14.Count() == 0) continue;

                                                                                            //        foreach (var bond14 in bonds14) {

                                                                                            //            var bond14String = StructureEnumConverter.BondTypeToString(bond14.BondType, bond14.IsAromaticity);
                                                                                            //            var atom15 = bond14.ConnectedAtoms.Where(n => n.AtomID != atom14.AtomID).ToList()[0];
                                                                                            //            var connectString14 = connectString13 + bond14String + atom15.AtomString;

                                                                                            //            if (atom15.AtomString == "C" && !atom14.IsInRing) {

                                                                                            //                var bonds15 = atom15.ConnectedBonds.Where(n => n.BondID != bond14.BondID);
                                                                                            //                if (bonds15.Count() == 0) continue;

                                                                                            //                foreach (var bond15 in bonds15) {

                                                                                            //                    var bond15String = StructureEnumConverter.BondTypeToString(bond15.BondType, bond15.IsAromaticity);
                                                                                            //                    var atom16 = bond15.ConnectedAtoms.Where(n => n.AtomID != atom15.AtomID).ToList()[0];
                                                                                            //                    var connectString15 = connectString14 + bond15String + atom16.AtomString;

                                                                                            //                    MassFragmentFingerprintGenerator.SetMolecularDescriptorOfSection8(descriptor, connectString15,
                                                                                            //                        atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10,
                                                                                            //                        atom11, atom12, atom13, atom14, atom15, atom16, 
                                                                                            //                        bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10,
                                                                                            //                        bond11, bond12, bond13, bond14, bond15,
                                                                                            //                        ringDictionary, ringsetDictionary);

                                                                                            //                    if (atom16.AtomString == "C" && !atom15.IsInRing) {

                                                                                            //                        var bonds16 = atom16.ConnectedBonds.Where(n => n.BondID != bond15.BondID);
                                                                                            //                        if (bonds16.Count() == 0) continue;

                                                                                            //                        foreach (var bond16 in bonds16) {

                                                                                            //                            var bond16String = StructureEnumConverter.BondTypeToString(bond16.BondType, bond16.IsAromaticity);
                                                                                            //                            var atom17 = bond16.ConnectedAtoms.Where(n => n.AtomID != atom16.AtomID).ToList()[0];
                                                                                            //                            var connectString16 = connectString15 + bond16String + atom17.AtomString;

                                                                                            //                            if (atom17.AtomString == "C" && !atom16.IsInRing) {

                                                                                            //                                var bonds17 = atom17.ConnectedBonds.Where(n => n.BondID != bond16.BondID);
                                                                                            //                                if (bonds17.Count() == 0) continue;

                                                                                            //                                foreach (var bond17 in bonds17) {

                                                                                            //                                    var bond17String = StructureEnumConverter.BondTypeToString(bond17.BondType, bond17.IsAromaticity);
                                                                                            //                                    var atom18 = bond17.ConnectedAtoms.Where(n => n.AtomID != atom17.AtomID).ToList()[0];
                                                                                            //                                    var connectString17 = connectString16 + bond17String + atom18.AtomString;

                                                                                            //                                    if (atom18.AtomString == "C" && !atom17.IsInRing) {

                                                                                            //                                        var bonds18 = atom18.ConnectedBonds.Where(n => n.BondID != bond17.BondID);
                                                                                            //                                        if (bonds18.Count() == 0) continue;

                                                                                            //                                        foreach (var bond18 in bonds18) {

                                                                                            //                                            var bond18String = StructureEnumConverter.BondTypeToString(bond18.BondType, bond18.IsAromaticity);
                                                                                            //                                            var atom19 = bond18.ConnectedAtoms.Where(n => n.AtomID != atom18.AtomID).ToList()[0];
                                                                                            //                                            var connectString18 = connectString17 + bond18String + atom19.AtomString;

                                                                                            //                                            if (atom19.AtomString == "C" && !atom18.IsInRing) {

                                                                                            //                                                var bonds19 = atom19.ConnectedBonds.Where(n => n.BondID != bond18.BondID);
                                                                                            //                                                if (bonds19.Count() == 0) continue;

                                                                                            //                                                foreach (var bond19 in bonds19) {

                                                                                            //                                                    var bond19String = StructureEnumConverter.BondTypeToString(bond19.BondType, bond19.IsAromaticity);
                                                                                            //                                                    var atom20 = bond19.ConnectedAtoms.Where(n => n.AtomID != atom19.AtomID).ToList()[0];
                                                                                            //                                                    var connectString19 = connectString18 + bond19String + atom20.AtomString;

                                                                                            //                                                    if (atom20.AtomString == "C" && !atom19.IsInRing) {

                                                                                            //                                                        var bonds20 = atom20.ConnectedBonds.Where(n => n.BondID != bond19.BondID);
                                                                                            //                                                        if (bonds20.Count() == 0) continue;

                                                                                            //                                                        foreach (var bond20 in bonds20) {

                                                                                            //                                                            var bond20String = StructureEnumConverter.BondTypeToString(bond20.BondType, bond20.IsAromaticity);
                                                                                            //                                                            var atom21 = bond20.ConnectedAtoms.Where(n => n.AtomID != atom20.AtomID).ToList()[0];
                                                                                            //                                                            var connectString20 = connectString19 + bond20String + atom21.AtomString;

                                                                                            //                                                            if (atom21.AtomString == "C" && !atom20.IsInRing) {

                                                                                            //                                                                var bonds21 = atom21.ConnectedBonds.Where(n => n.BondID != bond20.BondID);
                                                                                            //                                                                if (bonds21.Count() == 0) continue;

                                                                                            //                                                                foreach (var bond21 in bonds21) {

                                                                                            //                                                                    var bond21String = StructureEnumConverter.BondTypeToString(bond21.BondType, bond21.IsAromaticity);
                                                                                            //                                                                    var atom22 = bond21.ConnectedAtoms.Where(n => n.AtomID != atom21.AtomID).ToList()[0];
                                                                                            //                                                                    var connectString21 = connectString20 + bond21String + atom22.AtomString;

                                                                                            //                                                                }
                                                                                            //                                                            }
                                                                                            //                                                        }
                                                                                            //                                                    }
                                                                                            //                                                }
                                                                                            //                                            }
                                                                                            //                                        }
                                                                                            //                                    }
                                                                                            //                                }
                                                                                            //                            }
                                                                                            //                        }
                                                                                            //                    }
                                                                                            //                }
                                                                                            //            }
                                                                                            //        }
                                                                                            //    }
                                                                                            //}
                                                                                            #endregion
                                                                                        }
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            bondEnv.First_HeteroatomCount = bondEnv.First_NitrogenCount + bondEnv.First_OxygenCount + bondEnv.First_PhosphorusCount + bondEnv.First_SulfurCount;
            bondEnv.Second_HeteroatomCount = bondEnv.Second_NitrogenCount + bondEnv.Second_OxygenCount + bondEnv.Second_PhosphorusCount + bondEnv.Second_SulfurCount;
        }

        private static void recAtomConnections(List<AtomProperty> atoms, List<BondProperty> bonds, 
            MolecularFingerprint descriptors, Dictionary<int, RingProperty> ringDictionary, 
            Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            if (atoms.Count > 46) return;

            var tAtoms = new List<AtomProperty>();
            var tBonds = new List<BondProperty>();
            foreach (var atom in atoms) { tAtoms.Add(atom); }
            foreach (var bond in bonds) { tBonds.Add(bond); }

            MassFragmentFingerprint.FattyAcidsDescriptors(descriptors, tAtoms, tBonds);
            if (tAtoms.Count == 16 && tBonds.Count(n => n.BondType == BondType.Single) == 15) {
                MassFragmentFingerprint.SetProperties(descriptors, tAtoms, tBonds,
                    ringDictionary, ringsetDictionary, 16);
            }
            var lastAtom = tAtoms[tAtoms.Count - 1];
            var lastBond = tBonds[tBonds.Count - 1];

            var newBonds = lastAtom.ConnectedBonds.Where(n => n.BondID != lastBond.BondID);
            if (newBonds.Count() == 0) return;

            foreach (var newBond in newBonds) {

                var newAtom = newBond.ConnectedAtoms.Where(n => n.AtomID != lastAtom.AtomID).ToList()[0];
                if (newAtom.AtomString == "C" && !newAtom.IsInRing && isUniqueAtom(tAtoms, newAtom)) {

                    tAtoms.Add(newAtom);
                    tBonds.Add(newBond);
                    recAtomConnections(tAtoms, tBonds, descriptors, ringDictionary, ringsetDictionary);
                }
            }
        }

        private static bool isUniqueAtom(List<AtomProperty> tAtoms, AtomProperty newAtom)
        {
            var counter = tAtoms.Count(n => n.AtomID == newAtom.AtomID);
            if (counter > 0) return false;
            else return true;
        }
    }
}
