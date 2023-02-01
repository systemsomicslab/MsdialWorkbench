using Riken.Metabolomics.StructureFinder.Descriptor;
using Riken.Metabolomics.StructureFinder.Property;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Utility
{
    public sealed class AtomRecognition
    {
        private AtomRecognition() { }

        
        public static void SetAtomProperties(Dictionary<int, AtomProperty> atomDictionary, MolecularFingerprint descriptors)
        {
            //set oxygen property
            foreach (var atomProp in atomDictionary.Values.Where(n => n.AtomString == "O")) {
                setOxygenAtomProperty(atomProp, descriptors);
            }
            
            setAtomEnvironmentProperties(atomDictionary);

            //set nitrogen, sulfur, and phosphrus property
            foreach (var atomProp in atomDictionary.Values) {
                if (atomProp.AtomString == "N")
                    setNitrogenAtomProperty(atomProp, descriptors);
                else if (atomProp.AtomString == "S")
                    setSulfurAtomProperty(atomProp, descriptors);
                else if (atomProp.AtomString == "P")
                    setPhosphorusAtomProperty(atomProp);
                else if (atomProp.AtomString == "H")
                    setHydrogenAtomProperty(atomProp);
                else if (atomProp.AtomString == "Si")
                    setSiliconAtomProperty(atomProp);
                else if (atomProp.AtomString == "F" ||
                    atomProp.AtomString == "Cl" ||
                    atomProp.AtomString == "Br" ||
                    atomProp.AtomString == "I") {
                    atomProp.AtomFunctionType = AtomFunctionType.X_Halogen;
                }
            }

            //set carbon property
            foreach (var atomProp in atomDictionary.Values.Where(n => n.AtomString == "C")) {
                setCarbonAtomProperty(atomProp, descriptors);
            }

            PubChemFingerprint.SetSection4and5Properties(descriptors, atomDictionary);
        }

        private static void setAtomEnvironmentProperties(Dictionary<int, AtomProperty> atomDictionary)
        {
            //set atom basic properties
            foreach (var atomProp in atomDictionary.Values) {
                foreach (var bondProp in atomProp.ConnectedBonds) {
                    var connectedAtom = bondProp.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                    var cAtomString = connectedAtom.AtomString;
                    var atomEnv = atomProp.AtomEnv;
                    if (cAtomString == "C") {
                        atomEnv.CarbonCount++;

                        if (bondProp.BondType == BondType.Double) {
                            if (atomProp.AtomString == "C") atomEnv.DoubleBond_CC_Count++;
                            else if (atomProp.AtomString == "N") atomEnv.DoubleBond_CN_Count++;
                            else if (atomProp.AtomString == "O") atomEnv.DoubleBond_CO_Count++;
                            else if (atomProp.AtomString == "S") atomEnv.DoubleBond_CS_Count++;
                        }
                        else if (bondProp.BondType == BondType.Triple) {
                            if (atomProp.AtomString == "C") atomEnv.TripleBond_CC_Count++;
                            else if (atomProp.AtomString == "N") {
                            }
                        }
                        else if (bondProp.BondType == BondType.Single) {
                            atomEnv.SingleBond_CarbonCount++;
                        }
                    }
                    else if (cAtomString == "H") {
                        atomEnv.HydrogenCount++;
                        atomEnv.SingleBond_HydrogenCount++;
                    }
                    else if (cAtomString == "N") {
                        atomEnv.NitrogenCount++;

                        if (bondProp.BondType == BondType.Double) {
                            if (atomProp.AtomString == "C") atomEnv.DoubleBond_CN_Count++;
                            else if (atomProp.AtomString == "N") atomEnv.DoubleBond_NN_Count++;
                            else if (atomProp.AtomString == "O") atomEnv.DoubleBond_NO_Count++;
                            else if (atomProp.AtomString == "P") atomEnv.DoubleBond_NP_Count++;
                        }
                        else if (bondProp.BondType == BondType.Triple) {
                            if (atomProp.AtomString == "C") atomEnv.TripleBond_CN_Count++;
                        }
                        else if (bondProp.BondType == BondType.Single) {
                            atomEnv.SingleBond_NitrogenCount++;
                        }
                    }
                    else if (cAtomString == "O") {
                        atomEnv.OxygenCount++;

                        if (connectedAtom.AtomFunctionType == AtomFunctionType.O_Hydroxy) atomEnv.HydroxyCount++;
                        if (connectedAtom.AtomFunctionType == AtomFunctionType.O_Ether) atomEnv.EtherCount++;
                        if (connectedAtom.AtomFunctionType == AtomFunctionType.O_HeteroEther) atomEnv.HeteroEtherCount++;

                        if (bondProp.BondType == BondType.Double) {
                            if (atomProp.AtomString == "C") atomEnv.DoubleBond_CO_Count++;
                            else if (atomProp.AtomString == "N") atomEnv.DoubleBond_NO_Count++;
                            else if (atomProp.AtomString == "P") atomEnv.DoubleBond_PO_Count++;
                            else if (atomProp.AtomString == "S") atomEnv.DoubleBond_SO_Count++;
                        }
                        else if (bondProp.BondType == BondType.Single) {
                            atomEnv.SingleBond_OxygenCount++;
                        }
                    }
                    else if (cAtomString == "S") {
                        atomEnv.SulfurCount++;

                        if (bondProp.BondType == BondType.Double) {
                            if (atomProp.AtomString == "C") atomEnv.DoubleBond_CS_Count++;
                            else if (atomProp.AtomString == "O") atomEnv.DoubleBond_SO_Count++;
                        }
                        else if (bondProp.BondType == BondType.Single) {
                            atomEnv.SingleBond_SulfurCount++;
                        }
                    }
                    else if (cAtomString == "P") {
                        atomEnv.PhosphorusCount++;

                        if (bondProp.BondType == BondType.Double) {
                            if (atomProp.AtomString == "N") atomEnv.DoubleBond_NP_Count++;
                            else if (atomProp.AtomString == "O") atomEnv.DoubleBond_PO_Count++;
                            else if (atomProp.AtomString == "P") atomEnv.DoubleBond_PP_Count++;
                        }
                        else if (bondProp.BondType == BondType.Single) {
                            atomEnv.SingleBond_PhosphorusCount++;
                        }
                    }
                    else if (cAtomString == "F") {
                        atomEnv.FCount++;
                        if (bondProp.BondType == BondType.Single) atomEnv.SingleBond_FCount++;
                    }
                    else if (cAtomString == "Cl") {
                        atomEnv.ClCount++;
                        if (bondProp.BondType == BondType.Single) atomEnv.SingleBond_ClCount++;
                    }
                    else if (cAtomString == "Br") {
                        atomEnv.BrCount++;
                        if (bondProp.BondType == BondType.Single) atomEnv.SingleBond_BrCount++;
                    }
                    else if (cAtomString == "I") {
                        atomEnv.ICount++;
                        if (bondProp.BondType == BondType.Single) atomEnv.SingleBond_ICount++;
                    }
                    else if (cAtomString == "Si") {
                        atomEnv.SiCount++;
                        if (bondProp.BondType == BondType.Single) atomEnv.SingleBond_SiCount++;
                    }
                }
            }
        }


        private static void setSiliconAtomProperty(AtomProperty atomProp)
        {
            var carbonCount = 0;
            foreach (var bond in atomProp.ConnectedBonds) {
                var connectedAtom = bond.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                if (connectedAtom.AtomString == "C")
                    carbonCount++;
            }
            if (carbonCount == 3) atomProp.AtomFunctionType = AtomFunctionType.Si_TriCarbon;
            else atomProp.AtomFunctionType = AtomFunctionType.Si_Other;
        }

        private static void setHydrogenAtomProperty(AtomProperty atomProp)
        {
            atomProp.AtomFunctionType = AtomFunctionType.H_NonacidicProton;
            var connectedAtom = atomProp.ConnectedBonds[0].ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
            if (connectedAtom.AtomString == "O" || 
                connectedAtom.AtomString == "N" || 
                connectedAtom.AtomString == "P" ||
                connectedAtom.AtomString == "S") {
                    atomProp.AtomFunctionType = AtomFunctionType.H_AcidicProton;
            }
        }

        private static void setCarbonAtomProperty(AtomProperty atomProp, MolecularFingerprint descriptors)
        {
            atomProp.AtomFunctionType = AtomFunctionType.C_Other;
            var env = atomProp.AtomEnv;
            switch (atomProp.ConnectedBonds.Count) {
                case 4:
                    #region
                    //Debug.WriteLine(atomProp.AtomID + "\t" + carbonCount + "\t" + hydrogenCount);
                    if (env.SingleBond_HydrogenCount >= 3) {
                        atomProp.AtomFunctionType = AtomFunctionType.C_Methyl;
                        descriptors.VNWKTOKETHGBQD = 1;
                    }
                    else if (env.SingleBond_CarbonCount + env.SingleBond_HydrogenCount == 4)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Alkane;
                    else if (env.SingleBond_CarbonCount == 2 && env.EtherCount == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Ketal;
                    else if (env.SingleBond_CarbonCount == 2 && env.EtherCount == 1 && env.HydroxyCount == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Acetal;
                    else if (env.SingleBond_CarbonCount == 1 && env.SingleBond_HydrogenCount == 1 && env.EtherCount == 1 && env.HydroxyCount == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Hemiacetal;
                    else if (env.HydroxyCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Alcohol;
                    else if (env.EtherCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Ether;
                    else if (env.PrimaryAmineCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_PrimaryAmine;
                    else if (env.ThiolCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Thiol;
                    else if (env.SingleBond_HydrogenCount == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Methylene;

                    if (env.PrimaryAmineCount >= 1) descriptors.BAVYZALUXZFZLV = 1;
                    if (env.HydroxyCount >= 1) descriptors.OKKJLVBELUTLKV = 1;

                    break;
                    #endregion
                case 3:
                    #region
                    if (env.DoubleBond_CC_Count == 1) {
                        if (env.CarbonCount + env.HydrogenCount == 3)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Alkene;
                        else if (env.HydroxyCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Enol;
                        else if (env.PrimaryAmineCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Enamine;
                        else if (env.EtherCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Ether;
                    }
                    else if (env.DoubleBond_CO_Count == 1) {
                        if (env.EtherCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Ester;
                        else if (env.CarbonCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Ketone;
                        else if (env.CarbonCount == 1 && env.HydrogenCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Aldehyde;
                        else if (env.OxygenCount == 2 && env.NitrogenCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Carbamate;
                        else if (env.CarbonCount == 1 && env.NitrogenCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Amide;
                        else if (env.HydroxyCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Carboxylate;
                        else if (env.NitrogenCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Carbamide;
                    }
                    else if (env.DoubleBond_CS_Count >= 1) {
                        if (env.NitrogenCount == 1 && env.CarbonCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_ThioAmide;
                        else if (env.CarbonCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Thione;
                    }
                    else if (env.DoubleBond_CN_Count >= 1) {
                        if (env.DoubleBond_CN_Count == 1 && env.SingleBond_NitrogenCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Carboxamidine;
                        else if (env.DoubleBond_CN_Count == 1 && env.SingleBond_NitrogenCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Guanidine;
                        else if (env.DoubleBond_CN_Count == 1 && env.HydroxyCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.C_CarboximicAcid;
                        else if (env.DoubleBond_CN_Count == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Carbodiamine;
                        else if (env.HydroxyAmineCount == 0 && env.CarbonCount + env.HydrogenCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.C_Imine;
                        else if (env.HydroxyAmineCount > 0) {
                            if (env.CarbonCount == 2)
                                atomProp.AtomFunctionType = AtomFunctionType.C_Ketoxime;
                            else if (env.CarbonCount == 1 && env.HydrogenCount == 1)
                                atomProp.AtomFunctionType = AtomFunctionType.C_Aldoxime;
                        }
                    }
                    break;
                    #endregion
                case 2:
                    #region
                    if (env.DoubleBond_CC_Count == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Allene;
                    else if (env.DoubleBond_CN_Count == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Carbodiimide;
                    else if (env.DoubleBond_CC_Count == 1 && env.DoubleBond_CO_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Ketene;
                    else if (env.DoubleBond_CN_Count == 1 && env.DoubleBond_CS_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Isothiocyanate;
                    else if (env.DoubleBond_CN_Count == 1 && env.DoubleBond_CO_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Isocyanate;
                    else if (env.TripleBond_CC_Count >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.C_Alkyne;
                    else if (env.TripleBond_CN_Count >= 1)  
                        atomProp.AtomFunctionType = AtomFunctionType.C_Nitrile;

                    break;
                    #endregion
            }

            if (atomProp.AtomFunctionType == AtomFunctionType.C_Other) {
                #region // for other carbon
                var halogenCount = env.FCount + env.ClCount + env.BrCount + env.ICount;
                var heteroCount = env.NitrogenCount + env.OxygenCount + env.SulfurCount + env.PhosphorusCount;

                if (halogenCount > 0 && heteroCount > 0)
                    atomProp.AtomFunctionType = AtomFunctionType.C_HalogenHeteroConjugated;
                else if (halogenCount > 0)
                    atomProp.AtomFunctionType = AtomFunctionType.C_HalogenConnected;
                else if (heteroCount > 0)
                    atomProp.AtomFunctionType = AtomFunctionType.C_HeteroatomConnected;

                #endregion
            }

            //set descriptors
            if (atomProp.AtomEnv.DoubleBond_CO_Count >= 1)
                descriptors.WSFSSNUMVMOOMR = 1;
            if (atomProp.AtomEnv.TripleBond_CN_Count > 0)
                descriptors.LELOWRISYMNNSU = 1;
        }

        private static void setPhosphorusAtomProperty(AtomProperty atomProp)
        {
            atomProp.AtomFunctionType = AtomFunctionType.P_Other;
            var env = atomProp.AtomEnv;
            
            switch (atomProp.ConnectedBonds.Count) {
                case 4:
                    #region
                    if (env.DoubleBond_PO_Count == 1) {
                        if (env.HeteroEtherCount == 2 && env.HydroxyCount == 1)
                            atomProp.AtomFunctionType = AtomFunctionType.P_Phosphodiester;
                        else if (env.HeteroEtherCount + env.HydroxyCount == 2)
                            atomProp.AtomFunctionType = AtomFunctionType.P_PhosphonatePO3;
                        else if (env.HeteroEtherCount + env.HydroxyCount == 3)
                            atomProp.AtomFunctionType = AtomFunctionType.P_PhosphonatePO4;
                        else if (env.CarbonCount == 3)
                            atomProp.AtomFunctionType = AtomFunctionType.P_PhosphineOxide;
                    }
                    break;
                    #endregion
                case 3:
                    #region
                    if (env.CarbonCount == 3) atomProp.AtomFunctionType = AtomFunctionType.P_Phosphine;
                    else if (env.HeteroEtherCount + env.HydroxyCount == 3) atomProp.AtomFunctionType = AtomFunctionType.P_PhosphiteEster;
                    else if (env.HeteroEtherCount + env.HydroxyCount == 2) atomProp.AtomFunctionType = AtomFunctionType.P_Phosphonite;
                    else if (env.HeteroEtherCount + env.HydroxyCount == 1) atomProp.AtomFunctionType = AtomFunctionType.P_Phosphinite;

                    break;
                    #endregion
            }
        }

        private static void setSulfurAtomProperty(AtomProperty atomProp, MolecularFingerprint descriptors)
        {
            atomProp.AtomFunctionType = AtomFunctionType.S_Other;
            var env = atomProp.AtomEnv;

            switch (atomProp.ConnectedBonds.Count) {

                case 4:
                    #region
                    if (env.DoubleBond_SO_Count == 2 && env.NitrogenCount >= 1) atomProp.AtomFunctionType = AtomFunctionType.S_Sulfonamide;
                    else if (env.DoubleBond_SO_Count == 2 && env.SingleBond_OxygenCount == 0) atomProp.AtomFunctionType = AtomFunctionType.S_Sulfone;
                    else if (env.DoubleBond_SO_Count == 2 && env.SingleBond_OxygenCount == 1) atomProp.AtomFunctionType = AtomFunctionType.S_Sulfonate;
                    else if (env.DoubleBond_SO_Count == 2 && env.SingleBond_OxygenCount == 2) atomProp.AtomFunctionType = AtomFunctionType.S_Sulfate;
                    #endregion
                    break;
                case 3:
                    #region
                    if (env.DoubleBond_SO_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.S_Sulfoxide;
                    #endregion
                    break;
                case 2:
                    #region
                    if (env.SingleBond_CarbonCount == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.S_ThioEther;
                    else if (env.SingleBond_SulfurCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.S_Disulfide;
                    else if (env.HydrogenCount >= 1) {
                        atomProp.AtomFunctionType = AtomFunctionType.S_Thiol;
                        foreach (var bond in atomProp.ConnectedBonds) {
                            var cAtom = bond.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                            cAtom.AtomEnv.ThiolCount++;
                        }
                    }
                    #endregion
                    break;
                case 1:
                    #region
                    if (env.DoubleBond_CS_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.S_Thione;
                    #endregion
                    break;
            }

            if (atomProp.AtomEnv.HydrogenCount >= 1)
                descriptors.RWSOTUBLDIXVET = 1;
        }

        private static void setNitrogenAtomProperty(AtomProperty atomProp, MolecularFingerprint descriptors)
        {
            atomProp.AtomFunctionType = AtomFunctionType.N_Other;
            switch (atomProp.ConnectedBonds.Count) {
                case 4:
                    #region
                    if (atomProp.AtomCharge == 1) {
                        if (atomProp.AtomEnv.NitrogenCount == 4)
                            atomProp.AtomFunctionType = AtomFunctionType.N_QuatenaryAmine;
                        else if (atomProp.AtomEnv.OxygenCount >= 1)
                            atomProp.AtomFunctionType = AtomFunctionType.N_Noxide;
                    }
                    break;
                    #endregion

                case 3:
                    #region
                    if (atomProp.AtomEnv.HydrogenCount == 2 && atomProp.AtomEnv.CarbonCount == 1) 
                        atomProp.AtomFunctionType = AtomFunctionType.N_PrimaryAmine;
                    else if (atomProp.AtomEnv.HydrogenCount == 1 && atomProp.AtomEnv.CarbonCount == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.N_SecondaryAmine;
                    else if (atomProp.AtomEnv.CarbonCount == 3)
                        atomProp.AtomFunctionType = AtomFunctionType.N_TertiaryAmine;
                    else if (atomProp.AtomEnv.HydroxyCount >= 1) 
                        atomProp.AtomFunctionType = AtomFunctionType.N_HydroxyAmine;
                    else if (atomProp.AtomEnv.NitrogenCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Hydrazine;
                    else if (atomProp.AtomEnv.DoubleBond_NO_Count >= 1 && atomProp.AtomEnv.SingleBond_OxygenCount >= 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Nitro;
                    break;
                    #endregion

                case 2:
                    #region
                    if (atomProp.AtomCharge == 1 && atomProp.AtomEnv.DoubleBond_NN_Count == 2)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Azide;
                    else if (atomProp.AtomEnv.DoubleBond_CN_Count == 1 && atomProp.AtomEnv.SingleBond_CarbonCount == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Imine;
                    else if (atomProp.AtomEnv.DoubleBond_NN_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Azo;
                    else if (atomProp.AtomEnv.HydroxyCount == 1 && atomProp.AtomEnv.DoubleBond_CN_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Ketoxime;
                    else if (atomProp.AtomEnv.DoubleBond_NO_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Nitroso;

                    break;
                    #endregion

                case 1:
                    #region
                    if (atomProp.AtomEnv.TripleBond_CN_Count == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.N_Nitrile;
                    break;
                    #endregion
            }

            if (atomProp.AtomEnv.HydroxyCount >= 1) {
                foreach (var bond in atomProp.ConnectedBonds) {
                    var cAtom = bond.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                    cAtom.AtomEnv.HydroxyAmineCount++;
                }
            }

            if (atomProp.AtomEnv.HydrogenCount >= 2) {
                descriptors.QGZKDVFQNNGYKY = 1;
                foreach (var bond in atomProp.ConnectedBonds) {
                    var cAtom = bond.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                    cAtom.AtomEnv.PrimaryAmineCount++;
                }
            }
        }

        private static void setOxygenAtomProperty(AtomProperty atomProp, MolecularFingerprint descriptors)
        {
            atomProp.AtomFunctionType = AtomFunctionType.O_Other;

            switch (atomProp.ConnectedBonds.Count) {
                case 3:
                    #region
                    if (atomProp.AtomCharge == 1) atomProp.AtomFunctionType = AtomFunctionType.O_TripleBondsTypeCOCC;
                    break;
                    #endregion
                case 2:
                    #region
                    var bond1 = atomProp.ConnectedBonds[0];
                    var bond2 = atomProp.ConnectedBonds[1];

                    var connectedAtom1 = bond1.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];
                    var connectedAtom2 = bond2.ConnectedAtoms.Where(n => n.AtomID != atomProp.AtomID).ToList()[0];

                    if ((bond1.BondType == BondType.Double || bond2.BondType == BondType.Double) && atomProp.AtomCharge == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.O_TripleBondsType1COC;
                    else {
                        if (connectedAtom1.AtomString == "H" || connectedAtom2.AtomString == "H")
                            atomProp.AtomFunctionType = AtomFunctionType.O_Hydroxy;
                        else if (connectedAtom1.AtomString == "C" && connectedAtom2.AtomString == "C")
                            atomProp.AtomFunctionType = AtomFunctionType.O_Ether;
                        else if (connectedAtom1.AtomString == "O" || connectedAtom2.AtomString == "O")
                            atomProp.AtomFunctionType = AtomFunctionType.O_Peroxide;
                        else 
                            atomProp.AtomFunctionType = AtomFunctionType.O_HeteroEther;
                    }

                    break;
                    #endregion
                case 1:
                    #region
                    var connectedBond = atomProp.ConnectedBonds[0];
                    if (connectedBond.BondType == BondType.Single && atomProp.AtomCharge == -1) 
                        atomProp.AtomFunctionType = AtomFunctionType.O_Hydroxy;
                    else if (connectedBond.BondType == BondType.Double)
                        atomProp.AtomFunctionType = AtomFunctionType.O_Ketone;
                    else if (connectedBond.BondType == BondType.Triple && atomProp.AtomCharge == 1)
                        atomProp.AtomFunctionType = AtomFunctionType.O_TripleBondsType2CO;

                    break;
                    #endregion
            }

            if (atomProp.AtomFunctionType == AtomFunctionType.O_Hydroxy)
                descriptors.XLYOFNOQVPJJNP = 1;

        }

    }
}
