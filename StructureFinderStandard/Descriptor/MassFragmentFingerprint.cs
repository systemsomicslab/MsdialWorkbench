using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Property;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.StructureFinder.Descriptor
{
    public sealed class MassFragmentFingerprint
    {
        private MassFragmentFingerprint() { }

        #region set basic properties
        public static void SetProperties(MolecularFingerprint descriptors, string connectString, 
            AtomProperty atom1, AtomProperty atom2, BondProperty bond1, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            var targetRingID = -1;
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            switch (connectString) {
                case "C-C":
                    #region
                    if (atom1.AtomEnv.HydrogenCount >= 2 && atom2.AtomEnv.HydrogenCount >= 2) {
                        descriptors.OTMSDBZUPAUEDD = 1;
                    }

                    if (atom1.AtomEnv.SingleBond_FCount >= 3 && atom2.AtomEnv.SingleBond_FCount >= 2) {
                        descriptors.GTLACDSXYULKMZ = 1;
                    }

                    if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Epoxide, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.CarbonOutRing >= 1)
                            descriptors.GOOHAUXETOMSMM = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.CarbonOutRing >= 1)
                            descriptors.XLSZMDLNRCVEIJ = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.CarbonOutRing >= 1)
                            descriptors.GDOPTJXRTPNYNR = 1;
                        if (atom2.AtomEnv.SingleBond_CarbonCount >= 4)
                            descriptors.QWHNJUXXYKPLQM = 1;
                        if (ringEnv.Carbon_AlkaneOutRing >= 1 && ringEnv.DoubleSingleCarbonsOutRing >= 1)
                            descriptors.LETYIFNDQBJGPJ = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        descriptors.YXFVVABEGXRONW = 1;
                        if (ringEnv.NitrogenOutRing >= 1)
                            descriptors.RNVCVTLRINQCPJ = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.CarbonOutRing >= 1)
                            descriptors.UAEPNZWRGJTJPN = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 2)
                            descriptors.SGVUHPSBDNVHKL = 1;
                        if (ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.DoubleBond_CarbonOutRing >= 1)
                            descriptors.ILIUIABCBOXIRX = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 2 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptors.XARGIVYWQPXRTC = 1;
                        if (ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 3)
                            descriptors.PYOLJOJPIPCRDP = 1;
                        if (ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 4)
                            descriptors.BUOAKQCLRBVIOX = 1;
                        if (ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 3 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptors.YLZKVXHQMPWDBJ = 1;
                        if (ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 4 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptors.VTXRDCQMLXABHJ = 1;
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 3 && ringEnv.CarbonOutRing >= 4 && ringEnv.DoubleSingleCarbonsOutRing >= 1)
                            descriptors.QPEHZWXCQFKJQO = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Cyclohexene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.KetonOutRing >= 1)
                            descriptors.RKSNPTXBQXBXDJ = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.CarbonOutRing >= 1)
                            descriptors.ITQTTZVARXURQS = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        descriptors.JWUJQDFVADABEY = 1;
                    }

                    if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out targetRingID) &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(atom2, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                        var benzene = ringDictionary[ring1ID];
                        var pyran = ringDictionary[ring2ID];
                        if (benzene.RingEnv.OxygenOutRing >= 3 && pyran.RingEnv.OxygenOutRing >= 3 && pyran.RingEnv.Carbon_OxygenOutRing >= 1)
                            descriptors.IGXZXBRGFRTKRI = 1;
                       
                        
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                       RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrrolidine, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring2Env.SingleBond_CarbonOutRing >= 2)
                            descriptors.YDNBZEZHOYJIKQ = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                      RingtypeChecker(atom2, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_OxygenOutRing >= 1)
                            descriptors.WHDIYCULVGAWGN = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1 &&
                            ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_OxygenOutRing >= 1)
                            descriptors.AIVADCLCYQKORG = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1 && ring1Env.Carbon_AlkaneOutRing >= 2 &&
                           ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_OxygenOutRing >= 1)
                            descriptors.IWMUXTZLTOTAQO = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.EtherOutRing >= 1 && ring1Env.Carbon_KetoneOutRing >= 1 &&
                           ring2Env.OxygenOutRing >= 2 && ring2Env.Carbon_OxygenOutRing >= 1)
                            descriptors.FVEOHTQVMGVKNQ = 1;
                    }
                    break;
                    #endregion
                case "C-N":
                    #region
                    if (atom1.AtomEnv.HydrogenCount >= 2 && atom2.AtomEnv.HydrogenCount >= 2) {
                        descriptors.BAVYZALUXZFZLV = 1;
                    }
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3) {
                        if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrrolidine, out targetRingID)) {
                            descriptors.AVFZOVWCLRSYKC = 1;
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.CarbonOutRing >= 2)
                                descriptors.PXHHIBMOFPCBJQ = 1;
                        }
                        if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                            descriptors.PAMIQIKDUOTOBW = 1;
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.Carbon_AlkaneOutRing >= 1)
                                descriptors.FMETWOPSCIOJSL = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 2)
                                descriptors.ONQLCPDIXPYJSS = 1;
                        }
                    }
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID))
                        descriptors.PAYRUJLWNCNPSJ = 1;
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Imidazole, out ring2ID)) {
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring2Env.SingleBond_CarbonOutRing >= 2)
                            descriptors.HQNBJNDMPLEUDS = 1;

                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID)) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring2Env.NitrogenOutRing >= 1 && ring2Env.Carbon_ImidateOutRing >= 1)
                                descriptors.NPBGQWVNBJQFCA = 1;

                            if (RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out targetRingID)) {

                                ring3ID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != ring2ID).ToList()[0];
                                var ring3Env = ringDictionary[ring3ID].RingEnv;

                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.CarbonOutRing >= 1 && ring3Env.NitrogenOutRing >= 3)
                                    descriptors.FFHPXOJTVQDVMO = 1;
                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.CarbonOutRing >= 1 && ring3Env.OxygenOutRing >= 1)
                                    descriptors.BGYISLAFYPDORJ = 1;
                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.CarbonOutRing >= 1 &&
                                    ring3Env.OxygenOutRing >= 1 && ring3Env.NitrogenOutRing >= 3)
                                    descriptors.CPSONQHMTCQJJX = 1;
                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.NitrogenOutRing >= 3)
                                    descriptors.OFEZSBMBBKLLBJ = 1;
                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.CarbonOutRing >= 1 && ring3Env.NitrogenOutRing >= 3 && ring3Env.OxygenOutRing >= 1)
                                    descriptors.RWRBHYBHSXNVPD = 1;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring3Env.NitrogenOutRing >= 3)
                                    descriptors.XGYIMTFOTBMPFP = 1;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring3Env.OxygenOutRing >= 1)
                                    descriptors.IPJDTNIZLKTLEU = 1;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring3Env.OxygenOutRing >= 2)
                                    descriptors.VKYHXDQOZGKJIN = 1;
                                if (ring1Env.OxygenOutRing >= 1 && ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.NitrogenOutRing >= 3 && ring3Env.OxygenOutRing >= 1)
                                    descriptors.YKBGVTZYEHREMT = 1;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 1)
                                    descriptors.UGQMRVRMYYASKQ = 1;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 1 && ring3Env.NitrogenOutRing >= 3)
                                    descriptors.NYHBQMYGNKIUIF = 1;
                            }
                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Piperazine, out targetRingID))
                        descriptors.PVOAHINGSUIXLS = 1;
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) &&
                        RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrimidine, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 1 && ring1Env.SingleBond_CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 1 && ring2Env.NitrogenOutRing >= 1)
                            descriptors.XCRWLLXZYREZPO = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring1Env.SingleBond_CarbonOutRing >= 1 &&
                            ring2Env.OxygenOutRing >= 2)
                            descriptors.FDCFKLBIAIKUKB = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.SingleBond_CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 1 && ring2Env.NitrogenOutRing >= 1)
                            descriptors.HLPAJQITBMEOML = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring1Env.Carbon_OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 1 && ring2Env.NitrogenOutRing >= 1)
                            descriptors.ZHHOTKZTEUZTHX = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptors.WUBAOANSQGKRHF = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.Carbon_OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 1 && ring2Env.NitrogenOutRing >= 1)
                            descriptors.UHDGCWIWMRVCDJ = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.Carbon_OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptors.DRTQHJPVMGBUCF = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) &&
                        RingtypeChecker(atom2, ringDictionary, RingFunctionType.Dihydropyridine, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.SingleBond_CarbonOutRing >= 1 && ring2Env.Carbon_ImidateOutRing >= 1) {
                            descriptors.AWOGPWWJNGIDKU = 1;
                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var ring1Env = ringDictionary[targetRingID].RingEnv;
                        if (ring1Env.NitrogenOutRing >= 2)
                            descriptors.MISVBCMQSJUHMH = 1;
                    }
                    break;
                    #endregion
                case "C-O":
                    #region
                    if (atom1.AtomEnv.HydrogenCount >= 2 && atom2.AtomFunctionType == AtomFunctionType.O_Hydroxy) {
                        descriptors.OKKJLVBELUTLKV = 1;
                    }
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptors.ISWSIDIOOBJBQZ = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_AlkeneOutRing >= 1 && atom2.AtomCharge == -1)
                            descriptors.CGVSVWWXNAFRRH = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.CarbonOutRing >= 1)
                            descriptors.BVIJQMCYYASIFP = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        descriptors.HPXRVTGHNJAIIH = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptors.MQWCXKGKQLNYQG = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 2) {
                            descriptors.ZBAXJUPCYVIBSP = 1;
                            if (ringEnv.Carbon_AlkaneOutRing >= 1)
                                descriptors.SOUITUXPBGREAE = 1;
                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptors.NSMOSDAEGJTOIQ = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                            descriptors.XIRBXKMCQNAYAT = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 1)
                            descriptors.MKMRBXQLEMYZOY = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptors.KSTOUTVBGBEWOS = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.NitrogenOutRing >= 1)
                            descriptors.XQCZBXHVTFVIFE = 1;
                    }

                    if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out targetRingID))
                            descriptors.PCKPVGOLPKLUHR = 1;
                        else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID))
                            descriptors.XAWPKHNOFIWWNZ = 1;
                    }
                    else if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.DihydroPurine, out targetRingID)) {
                        descriptors.CGTXYEVJPWEPGW = 1;
                    }

                    break;
                    #endregion
                case "S-C":
                    #region
                    if (atom1.AtomFunctionType == AtomFunctionType.S_Thiol
                        && atom2.AtomFunctionType == AtomFunctionType.C_Methyl) {
                            descriptors.LSDPWZHWYPCBBB = 1;
                    }
                    if (!bond1.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom1.AtomEnv.DoubleBond_SO_Count >= 2 && ringEnv.NitrogenOutRing >= 1)
                            descriptors.DYHJCQLXZIELGG = 1;
                    }
                    break;
                    #endregion
                case "C=C":
                    #region
                    descriptors.VGGSQFUCUMXWEO = 1;
                    break;
                    #endregion
                case "C=O":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        if (atom1.AtomEnv.NitrogenCount >= 1)
                            descriptors.UBQKCCHYAOITMY = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexadiene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.Carbon_AlkaneOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 2)
                            descriptors.CTIZSKHLELUAOT = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        descriptors.FWFSEYBSWVRWGL = 1;
                        if (ringEnv.SingleBond_CarbonOutRing >= 2) {
                            descriptors.NDFYWJZFESRLBQ = 1;
                            if (ringEnv.Carbon_AlkaneOutRing >= 1)
                                descriptors.KABCBIJVOXYNHG = 1;
                        }
                    }
                    else if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.DihydroIndole, out targetRingID)) {
                        descriptors.JYGFTBXVXVMTGB = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        descriptors.YEJRWHAVMIAJKC = 1;
                    }
                    else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Naphthalene, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                            var ringEnv = ringDictionary[ring1ID].RingEnv;
                            if (ringEnv.KetonOutRing >= 2 && ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 3)
                                descriptors.LULCPJWUGUVEFU = 1;
                        }
                    }
                    break;
                #endregion
                case "C=N":
                    #region
                    descriptors.WDWDWGRYHDPSDS = 1;
                    break;
                    #endregion
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, 
            BondProperty bond1, BondProperty bond2, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            //var hydrogenCount = atom1.AtomEnv.HydrogenCount + atom2.AtomEnv.HydrogenCount + atom3.AtomEnv.HydrogenCount;
            var targetRingID = -1;
            var ring1ID = -1;
            var ring2ID = -1;

            switch (connectString) {
                case "C-C=O":
                    #region
                    descriptor.IKHGUXGNUITLKF = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.HUMNYLRZRPPJDN = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.NitrogenOutRing >= 1)
                            descriptor.FXWFZIRWWNPPOV = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrolidine, out targetRingID))
                        descriptor.JIDDDPVQQUHACU = 1;
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        descriptor.HRVXPXCISZSDCC = 1;
                    }
                    break;
                    #endregion
                case "C-C-C":
                    #region
                    descriptor.ATUOYWHBWRKTHZ = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.NNPPMTNAJDCUHE = 1;
                    if (atom2.AtomEnv.DoubleBond_CC_Count == 1)
                        descriptor.VQTUBCCKSQIDNK = 1;
                    if (atom2.AtomEnv.HydroxyCount >= 1)
                        descriptor.KFZMGEQAYNKOFK = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.CSCPPACGZOOCGX = 1;
                    if (atom1.AtomEnv.SingleBond_FCount >= 3 && atom2.AtomEnv.SingleBond_FCount >= 2 && atom3.AtomEnv.SingleBond_FCount >= 2)
                        descriptor.UKACHOXRXFQJFN = 1;
                    if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.YNQLUTRBYVCPMQ = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.HMNKTRSOROOSPP = 1;
                        if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.PMRFBLQVGJNGLU = 1;
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1) {
                            descriptor.KWOLFJPFCHCOCG = 1;
                            if (ringEnv.OxygenOutRing >= 1)
                                descriptor.JECYUBVRTQDVAT = 1;
                            if (ringEnv.OxygenOutRing >= 3)
                                descriptor.XLEYFDVVXLMULC = 1;
                        }
                        if (ringEnv.OxygenOutRing >= 2)
                            descriptor.VGMJYYDKPUPTID = 1;
                        if (atom2.AtomEnv.DoubleBond_CC_Count >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.JAGRUUPXPPLSRX = 1;
                        if (atom2.AtomEnv.DoubleBond_CC_Count >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.PYWRGHUOBJZVGT = 1;

                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1 && ring2Env.KetonOutRing >= 1)
                                descriptor.NBBFAZFBNQZIFY = 1;
                        }
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                            descriptor.CZZYITDELCSZES = 1;
                        }
                        if (RingsettypeChecker(atom3, ringDictionary, ringsetDictionary, RingsetFunctionType.Benzodioxole, out ring2ID)) {
                            descriptor.ARGITQZGLVBTTI = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 2)
                                descriptor.GJPLRURZZCQESP = 1;
                        }
                    }
                    else if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        descriptor.NJQHZENQKNIRSY = 1;
                    }
                    else if (!bond2.IsInRing && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        descriptor.YLUDSYGJHAQGOD = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.SingleBond_CarbonOutRing >= 2)
                            descriptor.GNXGHNXLUPRSCR = 1;
                    }
                    break;
                    #endregion
                case "C=C-C":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.C_Methyl)
                        descriptor.QQONPFPTGQHPMA = 1;
                    if (atom3.IsInRing && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.PPBRXRYQALVLMV = 1;
                        if (atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.VEIIEWOTAHXGKS = 1;

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.JESXATFQYMPTNL = 1;
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.NUHCEMOQRZEGAQ = 1;
                    }
                    break;
                    #endregion
                case "C-C-O":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.O_Hydroxy)
                        descriptor.LFQSCWFLJHTTHZ = 1;
                    if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.DLFVBJFMPXGRIB = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.QTBSBXVTEAMEQO = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrolidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                            descriptor.VLJNHYLEOZPXFW = 1;
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.ONIBWKKTOPOVIA = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        descriptor.WVDDGKGOMKODPV = 1;

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1) {
                            descriptor.BVJSUAQZOZWCKN = 1;
                            if (atom2.AtomEnv.DoubleBond_CC_Count >= 1) {
                                descriptor.XCQCERPCWZQGIX = 1;
                                if (ringEnv.OxygenOutRing >= 2)
                                    descriptor.RGFKONOOOXBVQC = 1;
                            }
                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                            descriptor.ZBNZAJFNDPPMDT = 1;
                    }
                    break;
                    #endregion
                case "C-C-N":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.N_PrimaryAmine)
                        descriptor.QUSNBJAOOMFDIB = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.JJWLVOIRVHMVIS = 1;
                    if (!bond2.IsInRing && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        descriptor.KDISMIMTGUMORD = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                            descriptor.WFCLWJHOKCQYOQ = 1;
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.Carbon_AmideOutRing >= 1)
                            descriptor.XMPWCYNJUJLWKF = 1;
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                            descriptor.PQMCFIPTFAVVSI = 1;
                        if (ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.MZMHDMCGCUWTDS = 1;
                    }
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        descriptor.DPBWFNDFMCCGGJ = 1;
                        var nitrogenAtom = ringDictionary[targetRingID].ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];
                        var outsideAtoms = ringDictionary[targetRingID].RingEnv.OutsideAtomDictionary[nitrogenAtom.AtomID];
                        if (outsideAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                            descriptor.ISWZMNIHDVCPSK = 1;
                            if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && outsideAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1) {
                                descriptor.NWZYRRPOEASODK = 1;
                            }
                        }
                    }
                    if (!bond2.IsInRing && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Pyrrolidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                            descriptor.GNMSLDIYJOSUSW = 1;
                    }
                    if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out targetRingID))
                            descriptor.RNAODKZCUVVPEN = 1;
                        
                    }
                    break;
                    #endregion
                case "C-N-C":
                    #region
                    descriptor.ROSDSFDQCJNGOL = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.GETQZCLCWQTVFV = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom2.AtomCharge == 1)
                        descriptor.INXJOSXEHHMQQF = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        descriptor.AFBPFSWMIHJQDM = 1;
                    }
                    break;
                    #endregion
                case "C-S-C":
                    #region
                    descriptor.QMMFVYPAHWMCMS = 1;
                    break;
                    #endregion
                case "C-O-C":
                    #region

                    descriptor.LCGLNKUTAGEVQW = 1;

                    if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.RDOXTESZEPMUJZ = 1;
                        if (RingsettypeChecker(atom3, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                            descriptor.QJRWYBIKLXNYLF = 1;
                        }
                    }
                    if (!atom2.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out targetRingID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                            var chromoneIds = ringsetDictionary[targetRingID].RingIDs;
                            if (chromoneIds.Count == 2){
                                var benzene = ringDictionary[chromoneIds[0]].RingFunctionType == RingFunctionType.Benzene ?
                                    ringDictionary[chromoneIds[0]] : ringDictionary[chromoneIds[1]];
                                var pyran = ringDictionary[ring2ID];
                                if (benzene.RingEnv.OxygenOutRing >= 3 && pyran.RingEnv.OxygenOutRing >= 4 && pyran.RingEnv.SingleBond_CarbonOutRing >= 1)
                                    descriptor.XCVOCJFOQNDTNC = 1;
                            }
                          
                       
                    }
                    else if (!atom2.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out targetRingID) &&
                            RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                            RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                                var benzene = ringDictionary[ring1ID];
                                var pyran = ringDictionary[ring2ID];
                                if (ringsetDictionary[targetRingID].RingIDs.Count == 2) {
                                    var chromanePyranID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != benzene.RingID).ToList()[0];
                                    var chromanePyran = ringDictionary[chromanePyranID];
                                    if (benzene.RingEnv.OxygenOutRing >= 3 && pyran.RingEnv.OxygenOutRing >= 4 &&
                                        pyran.RingEnv.Carbon_OxygenOutRing >= 1 && chromanePyran.RingEnv.KetonOutRing >= 1)
                                        descriptor.PIIYZZRUYDYLOR = 1;
                                }
                        
                    }
                    else if (!atom2.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromenylium, out targetRingID) &&
                           RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrylium, out ring1ID) &&
                           RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                        var Pyrylium = ringDictionary[ring1ID];
                        var pyran = ringDictionary[ring2ID];

                        if (ringsetDictionary[targetRingID].RingIDs.Count == 2) {
                            var benzenID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != Pyrylium.RingID).ToList()[0];
                            var benzene = ringDictionary[benzenID];
                            if (pyran.RingEnv.OxygenOutRing >= 3 &&
                                pyran.RingEnv.SingleBond_CarbonOutRing >= 1 && benzene.RingEnv.OxygenOutRing >= 3)
                                descriptor.GIPXVCZDXUYTPQ = 1;
                        }

                    }
                    else if (!atom2.IsInRing && 
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                        if (ring1ID != ring2ID) {

                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 4 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_OxygenOutRing >= 1) {
                                descriptor.SXEHDCDGTPHFBR = 1;
                                if (ring1Env.CarbonOutRing >= 1)
                                    descriptor.WXEKRRWEMCHREJ = 1;
                                if (ring1Env.Carbon_OxygenOutRing >= 1)
                                    descriptor.CLCFCUQOMMGQKP = 1;
                                if (ring1Env.Carbon_DoubleOxygensOutRing >= 1 && ring2Env.Carbon_DoubleOxygensOutRing >= 1)
                                    descriptor.DNYRKEWXPBSYOS = 1;
                                if (ring1Env.Carbon_DoubleOxygensOutRing >= 1 && ring2Env.Carbon_DoubleOxygensOutRing >= 1) {
                                    descriptor.SFUQBVYPRJLLKE = 1;
                                    if (ring2Env.OxygenOutRing >= 4)
                                        descriptor.IMNADAQGVSDVMI = 1;
                                }
                            }
                            if (ring1Env.OxygenOutRing >= 4 && ring2Env.OxygenOutRing >= 4 &&
                                ring1Env.Carbon_OxygenOutRing >= 1) {
                                descriptor.BUEBVQCTEJTADB = 1;
                                if (ring2Env.SingleBond_CarbonOutRing >= 1) {
                                    descriptor.LTHOGZQBHZQCGR = 1;
                                    if (ring2Env.Carbon_OxygenOutRing >= 1)
                                        descriptor.HIWPGCMGAMJNRG = 1;
                                }
                                if (ring1Env.EtherOutRing >= 2)
                                    descriptor.FHWWVCXCPVVWII = 1;
                            }

                        }
                    }
                    else if (!atom2.IsInRing &&
                    RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                    RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {

                        var pyranEnv = ringDictionary[ring1ID].RingEnv;
                        var furanEnv = ringDictionary[ring2ID].RingEnv;

                        if (pyranEnv.OxygenOutRing >= 4 && pyranEnv.Carbon_OxygenOutRing >= 1 && 
                            furanEnv.OxygenOutRing >= 2 && furanEnv.Carbon_OxygenOutRing >= 2) {
                                descriptor.GJWZUOOUQNJKQC = 1;
                        }
                    }
                    else if (!atom2.IsInRing &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                        if (ring1ID != ring2ID) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3) {
                                descriptor.WWVGRACROSBOHO = 1;
                                if(ring2Env.Carbon_KetoneOutRing >= 1)
                                    descriptor.ZTCZPSUNIDBMPJ = 1;
                            }
                            if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2) {
                                descriptor.LAWZEAWGPUTPRO = 1;
                            }
                            if (ring1Env.OxygenOutRing >= 4 && ring2Env.OxygenOutRing >= 3) {
                                descriptor.QHNOMYQJGFFYSV = 1;
                            }
                            if (ring1Env.OxygenOutRing >= 4 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                                descriptor.HAKNARODOLMANY = 1;
                            if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_OxygenOutRing >= 1 &&
                                ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1 && ring2Env.Carbon_AlkaneOutRing >= 1)
                                descriptor.ARYSAKCPIBLSDO = 1;
                        }
                    }
                    else if (!atom2.IsInRing &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                        if (ring1ID != ring2ID) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;

                            //if (ring1Env.BrOutRing >= 1 && ring1Env.OxygenOutRing >= 2 && 
                            //    ring2Env.BrOutRing >= 2 && ring2Env.ClOutRing >= 1 && ring2Env.OxygenOutRing >= 2) {
                            //    descriptor.ZZZPESBACZCVFM = 1;
                            //}
                            if (ring1Env.BrOutRing >= 2 && ring1Env.OxygenOutRing >= 2 &&
                               ring2Env.BrOutRing >= 2 && ring2Env.OxygenOutRing >= 2) {
                                descriptor.FJIJWARHPUGHGQ = 1;
                            }
                            if (ring1Env.BrOutRing >= 1 && ring1Env.OxygenOutRing >= 2 &&
                               ring2Env.BrOutRing >= 3 && ring2Env.OxygenOutRing >= 2) {
                                descriptor.YOVPSVDERMWCLD = 1;
                            }
                        }
                    }

                    break;
                    #endregion
                case "C-S=O":
                    #region
                    descriptor.BOTYRLOGQKSOAE = 1;
                    break;
                    #endregion
                case "N=C-N":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.N_PrimaryAmine)
                        descriptor.PNKUSGQVOMIXLU = 1;
                    if (atom2.AtomEnv.DoubleBond_CN_Count == 1 && atom2.AtomEnv.SingleBond_NitrogenCount == 2)
                        descriptor.ZRALSGWEFCBTJO = 1;
                    break;
                    #endregion
                case "N=C-O":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.O_Hydroxy)
                        descriptor.ZHNUHDYFZUAESO = 1;
                    break;
                    #endregion
                case "O=N-O":
                    #region
                    descriptor.JCXJVPUVTGWSNB = 1;
                    break;
                    #endregion
                case "O=C-O":
                    #region
                    if (atom3.AtomFunctionType == AtomFunctionType.O_Hydroxy)
                        descriptor.BDAGIHXWWSANSR = 1;
                    break;
                    #endregion
                case "O=C-N":
                    #region
                    if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        descriptor.FEWLNYSYJNLUOO = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.Carbon_DoubleOxygensOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 2)
                            descriptor.IZNTWTMLHCGAJU = 1;
                    }
                    break;
                    #endregion
                case "O=S=O":
                    #region
                    descriptor.RAHZWNYVWXNFOC = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 1) descriptor.KEIVLHIFSZBKGU = 1;
                    if (atom2.AtomEnv.SingleBond_NitrogenCount >= 1) descriptor.AHZVGADFMXTGBU = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.BDHFUVZGWQCTTF = 1;
                    if (atom2.AtomEnv.OxygenCount >= 4) descriptor.QAOWNCQODCNURD = 1;
                    break;
                    #endregion
                case "O-P-O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_PO_Count >= 1) descriptor.ABLZXFCXXLZCGV = 1;
                    if (atom2.AtomEnv.OxygenCount >= 4) descriptor.NBIIXXVUZAFLBC = 1;
                    break;
                    #endregion
                case "P-O-P":
                    #region
                    if (atom1.AtomEnv.OxygenCount >= 3 && atom3.AtomEnv.OxygenCount >= 4)
                        descriptor.PAXPHUUREDAUGV = 1;
                    if (atom1.AtomEnv.OxygenCount >= 4 && atom3.AtomEnv.OxygenCount >= 4)
                        descriptor.XPPKVPWEQAFLFU = 1;
                        break;

                    #endregion
                case "C-O-P":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom3.AtomEnv.OxygenCount >= 3 && ringEnv.OxygenOutRing >= 4) {
                            descriptor.BJRKSJFOKUMMRO = 1;
                            if (ringEnv.CarbonOutRing >= 1) {
                                descriptor.XTZHIZMKJUVKDN = 1;
                                if (ringEnv.Carbon_OxygenOutRing >= 1)
                                    descriptor.VSABSRHRDFYAJK = 1;
                                if (atom3.AtomEnv.OxygenCount >= 4)
                                    descriptor.PTVXQARCLQPGIR = 1;
                                if (ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                                    descriptor.XQSXMCDHWGUAAV = 1;
                            }
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom3.AtomEnv.OxygenCount >= 3 && ringEnv.OxygenOutRing >= 6) {
                            descriptor.RZLISDJKQIPSBD = 1;
                        }
                    }
                    break;
                    #endregion
                case "N=C=S":
                    #region
                    descriptor.GRHBQAYDJPGGLF = 1;
                    break;
                    #endregion
                case "C-C-S":
                    #region
                    descriptor.DNJIEGIFACGWOD = 1;
                    if (atom1.AtomEnv.FCount >= 2 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.OxygenCount >= 2)
                        descriptor.XKCAXTAJNCEDGN = 1;
                    if (atom1.AtomEnv.FCount >= 2 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.OxygenCount >= 3)
                        descriptor.KZWJWYFPLXRYIL = 1;
                    if (atom3.AtomEnv.OxygenCount >= 3 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.NSBSWKMMSOEBLZ = 1;
                        if (ringEnv.OxygenOutRing >= 4)
                            descriptor.QFBWOLBPVQLZEH = 1;
                    }
                    break;
                    #endregion
                case "N-C-O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.XSQUKJJJFZCRTK = 1;
                    break;
                    #endregion
                case "O-C-O":
                    #region
                    descriptor.CKFGINPQOCXMAZ = 1;
                    break;
                    #endregion
                case "N-C=O":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.Carbon_KetoneOutRing >= 2)
                            descriptor.RZMDMVOHBMAFDF = 1;
                    }
                    break;
                #endregion
                case "C=C-O":
                    #region 
                    descriptor.IMROMDMJAWUWLK = 1;
                    break;
                #endregion
                case "C-N=C":
                    #region 
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        descriptor.GDIPMASOJOFGKK = 1;
                    }
                    break;
                    #endregion
            }
        }
        
        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, 
            BondProperty bond1, BondProperty bond2, BondProperty bond3, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            //var hydrogenCount = atom1.AtomEnv.HydrogenCount + atom2.AtomEnv.HydrogenCount 
            //    + atom3.AtomEnv.HydrogenCount + atom4.AtomEnv.HydrogenCount;
            var targetRingID = -1;
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;

            switch (connectString) {
                case "C-C-C=C":
                    #region
                    descriptor.VXNZUUAINFGPBY = 1;
                    if (atom2.AtomEnv.HydroxyCount > 0) descriptor.MKUWVMRNQOOSAT = 1;
                    break;
                    #endregion
                case "C-C=C-C":
                    #region
                    descriptor.IAQRGUVFOMOMEM = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2) descriptor.BKOOMYPCSUNDGP = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        descriptor.QROGIFZRVHSFLM = 1;

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2) {
                            descriptor.MDXYNRAMDATLBT = 1;
                            
                        }
                        if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                            descriptor.PJANXHGTPQOBST = 1;
                        }
                    }
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) 
                        && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.KetonOutRing >= 1 && ring1Env.DoubleBond_CarbonOutRing >= 1 && ring2Env.KetonOutRing >= 1 && ring2Env.CarbonOutRing >= 2) {
                            var branchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            foreach (var bAtom in branchedAtoms) {
                                if (bAtom.AtomEnv.SingleBond_OxygenCount >= 1)
                                    descriptor.PQRSLRAWPZCACH = 1;
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-N":
                    #region
                    if (atom4.AtomFunctionType == AtomFunctionType.N_PrimaryAmine) descriptor.WGYKZJWCGVVSQN = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.KDSNLYIMUZNERS = 1;
                    if (atom2.AtomEnv.HydroxyCount >= 1) descriptor.HXKKHQJGJAFBHI = 1;
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID))
                        descriptor.BHHGXPLMPWCGHP = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 4 && atom4.AtomCharge == 1) {
                        var branchAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (branchAtoms.Count(n => n.AtomEnv.OxygenCount >= 2) >= 1)
                            descriptor.JWNWCEAWZGLYTE = 1;
                    }
                    break;
                #endregion
                case "C-C=C-O":
                    #region 
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {

                        var ringEnv = ringDictionary[ring1ID].RingEnv;

                        descriptor.XLLXMBCBJGATSP = 1;
                        if (atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.AVHKOIHDSGYXMF = 1;
                        if (ringEnv.OxygenOutRing >= 2)
                            descriptor.OUWKTIRCGDAGJB = 1;
                        if (ringEnv.OxygenOutRing >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.ASROQXGLZMIYNN = 1;
                        if (ringEnv.EtherOutRing >= 1)
                            descriptor.HLOZUROOXAWGSD = 1;
                        if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.LTJNCZSXVJCHGE = 1;
                        if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 3)
                            descriptor.OCINWLFNVFQQLR = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.MGRQLSWDIFMJGV = 1;
                    }
                    break;
                    #endregion
                case "C-N-C=O":
                    #region
                    descriptor.ATHHXGZTWNVVOU = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID))
                        descriptor.DYDNPESBYVVLBO = 1;
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.KetonOutRing >= 2)
                            descriptor.HWAHQASPJMRWPP = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-O":
                    #region
                    descriptor.BDERNNFJNOPAEC = 1;
                    if (atom3.AtomEnv.DoubleBond_CN_Count >= 1) descriptor.QLNJFJADRCOGBJ = 1;
                    if (atom2.AtomEnv.HydroxyCount >= 1) descriptor.DNIAPMSPPWPWGF = 1;
                    if (atom3.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.XBDQKXXYIPTUBI = 1;
                    if (atom2.AtomEnv.DoubleBond_CC_Count >= 1 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.CERQOIWHTDAKMF = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.ZXEKIIBDNHEJCQ = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KQNPFQTWMSNSAP = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.JVTAAEKCZFNVCJ = 1;
                    if (atom2.AtomEnv.SingleBond_NitrogenCount >= 1 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.QNAYBMKLOCPYGJ = 1;
                    if (atom2.AtomEnv.SingleBond_NitrogenCount >= 1 && atom3.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.HQMLIDZJXVVKCW = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.WRMNZCZEMHIOCP = 1;

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.YCCILVSKPBXVIP = 1;
                        if (ringEnv.OxygenOutRing >= 2) {
                            if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.DYKBONVDLJHZLM = 1;
                        }
                        if (ringEnv.OxygenOutRing >= 3) {
                            descriptor.DFOATKRXQLALOL = 1;
                            if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.HPPCGHJRDOZHQY = 1;
                        }

                        var bAtoms = getBranchAtom(atom2, bond1, bond2, "C");
                        if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                            descriptor.DGNQXQJVAILJBW = 1;
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.JACRWUWPXAESPB = 1;
                        }
                    }
                    else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out ring1ID) &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out ring2ID)) {
                        if (atom3.AtomEnv.DoubleBond_CN_Count >= 1)
                            descriptor.ZOAMBXDOGPRZLP = 1;
                    }
                    break;
                    #endregion
                case "C-C-C=O":
                    #region
                    descriptor.NBBJYMSMWIIQGU = 1;
                    if (atom2.AtomEnv.HydroxyCount >= 1) descriptor.BSABBBMNWQWLLU = 1;
                    if (atom2.AtomEnv.PrimaryAmineCount >= 1) descriptor.QPMCUNAXNMSGTK = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {

                        var ringEnv = ringDictionary[ring1ID].RingEnv;
                        if (atom2.AtomEnv.DoubleBond_CC_Count >= 1) {
                            descriptor.ZFBRJUBOJXNIQM = 1;
                            if (ringEnv.OxygenOutRing >= 1)
                                descriptor.CJEQHXGMZPFAQE = 1;
                            if (ringEnv.EtherOutRing >= 1)
                                descriptor.JKTXGBOLZMGAIF = 1;
                        }
                    }
                    break;
                    #endregion
                case "C=C-C-O":
                    #region
                    descriptor.XXROGKLTLUQVRX = 1;
                    if (atom3.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.NIXOWILDQLNWCW = 1;
                    break;
                    #endregion
                case "C=C-C=O":
                    #region
                    descriptor.HGINCPLSRVDWNT = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.QRFGDGZPNFSNCE = 1;
                    break;
                    #endregion
                case "C-C-N-C":
                    #region
                    descriptor.LIWAQLJGPBVORC = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.DAZXVJBJRMWXJP = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2) descriptor.XHFGWHUWQXTGAT = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomCharge == 1)
                        descriptor.YOMFVLRTMZWACQ = 1; 
                    if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.FZERHIULMFGESH = 1;
                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Pteridine, out targetRingID) && 
                            RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrazine, out ring2ID)) {
                                ring3ID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != ring2ID).ToList()[0];
                                var ring3Env = ringDictionary[ring3ID].RingEnv;
                                if (ring3Env.NitrogenOutRing >= 3 && ring3Env.OxygenOutRing >= 1)
                                    descriptor.LBLNXZIGDJPBJM = 1;
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {

                        descriptor.RIWRFSMVIUAEBX = 1;

                        var ringEnv = ringDictionary[ring1ID].RingEnv;
                        if (atom3.AtomEnv.SingleBond_CarbonCount >= 3 && ringEnv.EtherOutRing >= 2)
                            descriptor.NWYBPFPJJYBYSR = 1;
                    }
                    else if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Thiazepine, out targetRingID)) {
                        if (RingsettypeChecker(atom4, ringDictionary, ringsetDictionary, RingsetFunctionType.DibenzoThiazepine, out targetRingID)) {
                            descriptor.LQGKZLBYKWGNGP = 1;
                            if (RingtypeChecker(bond1, ringDictionary, RingFunctionType.Piperazine, out ring2ID)) {
                                var ring2 = ringDictionary[ring2ID];
                                var ring2Env = ringDictionary[ring2ID].RingEnv;

                                var otherNitrogens = ring2.ConnectedAtoms.Where(n => n.AtomString == "N" && n.AtomID != atom3.AtomID).ToList();
                                if (otherNitrogens.Count == 1) {
                                    var oNitrogen = otherNitrogens[0];
                                    var oAtoms = ring2Env.OutsideAtomDictionary[oNitrogen.AtomID];
                                    if (oAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                        descriptor.KUKQJNQKJWEVCE = 1;

                                }
                            }
                        }
                    }
                    else if (bond2.IsSharedBondInRings && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrolidine, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.HydroPyrrolidine, out ring2ID)) {
                            descriptor.CSNWPSHAZNPDBA = 1;
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ring1Env.OxygenOutRing >= 1 && ring2Env.Carbon_OxygenOutRing >= 1)
                                descriptor.HJSJELVDQOXCHO = 1;
                    }
                    else if (bond2.IsSharedBondInRings && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Piperidine, out ring2ID)) {
                        if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)    
                            descriptor.LJPZHJUSICYOIX = 1;
                    }
                    else if (!bond1.IsInRing && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out targetRingID)) {
                            descriptor.RDIZNKNSMGEKKE = 1;
                            if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.HBBCKARIXVLLRZ = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-C":
                    #region
                    descriptor.IJDNQMDRQITEOD = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.QWTDNUCVQCZILF = 1;
                    if (atom3.AtomEnv.HydroxyCount >= 1) descriptor.BTANRVKWQNVYAZ = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.MXLMTQWGSQIYOW = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 4)
                        descriptor.HNRMPXKDFBEGFZ = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom2.AtomEnv.SingleBond_CarbonCount >= 3 & atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.LEBVLOLOVGHEFE = 1;
                    if (atom1.AtomEnv.FCount >= 3 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.FCount >= 2)
                        descriptor.ZQTIKDIHRRLSRV = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.ODLMAHJVESYWTB = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1)
                            descriptor.MWOMNLDJNQWJMK = 1;
                        if (atom4.AtomEnv.DoubleBond_CO_Count >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.REEQXZCFSBLNDH = 1;
                        if (atom4.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.XMIIGOLPHOKFCH = 1;
                        if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.DWGPKVSAWGQTPQ = 1;
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.NMHMNPHRMNGLLB = 1;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.MPWGZBWDLMDIHO = 1;
                    }
                    else if (bond2.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID)) {
                        if (atom2.AtomEnv.SingleBond_CarbonCount >= 4) {
                            descriptor.GGWBGCSGJCRVIQ = 1;
                            if (atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.KGBMCONWCCBLAE = 1;
                        }

                    }
                    else if (bond2.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID)) {

                        if (ring1ID != ring2ID) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ring1Env.SingleBond_CarbonOutRing >= 5 && ring1Env.Carbon_OxygenOutRing >= 1 &&
                                ring2Env.SingleBond_CarbonOutRing >= 4 && atom2.AtomEnv.SingleBond_CarbonCount >= 4)
                                descriptor.HZNDJPAXRXLIOU = 1;
                            if (ring1Env.SingleBond_CarbonOutRing >= 3 && ring1Env.DoubleBond_CarbonOutRing >= 1 && ring2Env.SingleBond_CarbonOutRing >= 3)
                                descriptor.UNWNSYMIUBZKCZ = 1;
                            if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                                ring1Env.OxygenOutRing >= 1 && ring2Env.SingleBond_CarbonOutRing >= 4)
                                descriptor.GZURMVKYRIAPGE = 1;
                        }
                    }
                    else if (bond2.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexene, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (atom2.AtomEnv.SingleBond_CarbonCount >= 4) {
                            descriptor.GCLZXEJWFDSXJZ = 1;
                            if (atom1.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.XWASVAMCARPLHN = 1;
                        }

                        if (ring1Env.Carbon_AlkaneOutRing >= 3 && atom2.AtomEnv.SingleBond_CarbonCount >= 4)
                            descriptor.NUQYJMDNHVQOME = 1;

                        if (ring1Env.KetonOutRing >= 1)
                            descriptor.FOCKPZOFYCTNIA = 1;
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom2.AtomEnv.SingleBond_CarbonCount >= 4 && atom4.AtomEnv.SingleBond_CarbonCount >= 4) {
                            var outAtoms = ring1Env.OutsideAtomDictionary[atom1.AtomID];
                            if (outAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1)
                                descriptor.KVAPIDLGYVSWRD = 1;
                        }

                    }
                    break;
                    #endregion
                case "C-O-C=O":
                    #region
                    descriptor.TZIHFWKZFHZASV = 1;
                    break;
                    #endregion
                case "C-N-C-N":
                    #region
                    descriptor.XOTDURGROKNGTI = 1;
                    break;
                    #endregion
                case "N-C-C-N":
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Morpholine, out targetRingID))
                        descriptor.RWIVICVCHVMHMU = 1;
                    break;
                case "O-C-C-O":
                    #region
                    descriptor.LYCAIKOWRPUZTN = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.AEMRFAOFKBGASW = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.VIBDJEWPNNCFQO = 1;
                    break;
                    #endregion
                case "O-C-C=O":
                    #region
                    if (atom1.AtomFunctionType == AtomFunctionType.O_Hydroxy) descriptor.WGCNASOHLSPBMP = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.HHLFWLYXYJOTON = 1;
                    break;
                    #endregion
                case "O-C-C-N":
                    #region
                    descriptor.HZAXFHJVJLSVMW = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.DHMQDGOQFOQNFH = 1;
                    if (!bond3.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.NitrogenOutRing >= 1)
                            descriptor.ATQNWDREMOIFSG = 1;
                        if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 1 && ringEnv.NitrogenOutRing >= 1)
                            descriptor.IHJYCNWBPOTSGA = 1;
                    }
                    break;
                    #endregion
                case "S-C-C-O":
                    #region
                    descriptor.DGVVWUTYPXICAM = 1;
                    break;
                    #endregion
                case "S-C-C-N":
                    #region
                    descriptor.UFULAYFCSOUIOV = 1;
                    break;
                    #endregion
                case "N-C-C-O":
                    #region
                    
                    if (atom1.AtomCharge == 1 && atom1.AtomEnv.SingleBond_CarbonCount == 4) descriptor.OEYIOHPDSNJKLS = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.BEBCJVAWIBVWNZ = 1;
                    break;
                    #endregion

                case "N-C-C=O":
                    descriptor.LYIIBVSRGJSHAV = 1;
                    break;
                case "C-C:C:N":
                    #region
                    if (atom2.IsInRing && RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        descriptor.ZFRKQXVRDFCRJG = 1;
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 2)
                            descriptor.GOVXKUCVZUROAN = 1;
                    }
                    break;
                    #endregion
                case "C:N:C:N":
                    #region
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out targetRingID)) {
                            if (atom1.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.FDGQSTZJBFJUBT = 1;
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.NitrogenCount >= 3)
                                descriptor.UYTPUPDQBNUYGX = 1;
                            if (atom1.AtomEnv.OxygenCount >= 1 && atom3.AtomEnv.OxygenCount >= 1)
                                descriptor.LRFVTYWOQMYALW = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-P":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && atom4.AtomEnv.OxygenCount >= 3)
                            descriptor.IZKQBJLVIODBSO = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom4.AtomEnv.OxygenCount >= 4) {
                            descriptor.CYZZKTRFOOKUMT = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-C":
                    #region
                    descriptor.XOBKSJJDNFUZPF = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KXKVLQRXCPHEJC = 1;
                    if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.DLRJIFUOBPOJNS = 1;
                    }
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.QPJVMBTYPHYUOC = 1;
                    }
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                    RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {

                        descriptor.BOTNYLSAWDQNEX = 1;

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 1)
                            descriptor.PSBWSIWOVNCTKX = 1;
                        if (ring2Env.OxygenOutRing >= 1)
                            descriptor.VYQNWZOUAUKGHI = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && ring2Env.CarbonOutRing >= 1)
                            descriptor.QLUHYVGUOXRLQR = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptor.RBYSZEUQPYQATQ = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 2)
                            descriptor.BXYSBICUNBDVDH = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.CQWRVJZHJPBJEC = 1;
                        if (ring2Env.OxygenOutRing >= 2 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.ZCFAMXDOQTZMTD = 1;
                        if (ring2Env.OxygenOutRing >= 3)
                            descriptor.NVOJWBBMMVWYGJ = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptor.TXJAUSJCVVFWCG = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.YGTYGSKGKAVGLY = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2 && atom2.AtomEnv.SingleBond_CarbonCount >= 1)
                            descriptor.ZVANOEGUOMGWAU = 1;
                        if (ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptor.LHBXEONLEYIMGE = 1;
                        if (ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.MSCGMSSVACOHTH = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 2)
                            descriptor.GHFOFTCKGUMTML = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.KZSHYRKQUJWKDY = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.DHUXSOZCLDKUJQ = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.RAHHWXLNOLAZBK = 1;
                        if (ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.XVOVEZNPYPHHRI = 1;
                        if (ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 2 && atom2.AtomEnv.SingleBond_CarbonCount >= 2)
                            descriptor.PDJBHHCCMUJBLL = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.SingleBond_CarbonCount >= 2)
                            descriptor.GWFJKRUNUNZHEM = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.KQQGBGQNHPNHQH = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 2 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.RGJPTNRDAZLRPY = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.UXJPJHCVFOJNSV = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1) {
                            var bAtoms = getBranchAtom(atom2, bond1, bond2, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.YOUSVGRZGTZUBG = 1;
                        }
                        if (ring1Env.EtherOutRing >= 1 && ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3)
                            descriptor.HSSQTNGDCXYNCA = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3 && ring2Env.EtherOutRing >= 2)
                            descriptor.LZVDWXGWBSMYEF = 1;
                        if (ring1Env.EtherOutRing >= 1 && atom2.AtomEnv.SingleBond_CarbonCount >= 2 && ring2Env.OxygenOutRing >= 3)
                            descriptor.KPINMAZYHUYPHM = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3)
                            descriptor.HXKIQVWRGNCIEH = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.HEXOPSWGNBGINC = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1)
                            descriptor.JVAHPDRCBKDCGK = 1;
                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CC_Count >= 1) {
                            var bAtoms = getBranchAtom(atom2, bond1, bond2, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.MOZSFSXUQDZNKW = 1;
                        }
                        if (ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.UEQAICZERAGAMU = 1;
                        if (ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 4 && ring2Env.EtherOutRing >= 2 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.UFWSPVHXCKOZKP = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.FABRUUBOLWFYIR = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.EtherOutRing >= 1 &&
                            ring2Env.OxygenOutRing >= 4 && ring2Env.EtherOutRing >= 2 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.LVDUPRJMLLNNGS = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.EtherOutRing >= 2 &&
                            ring2Env.OxygenOutRing >= 4 && ring2Env.EtherOutRing >= 2 && ring2Env.Carbon_KetoneOutRing >= 1)
                            descriptor.QULYEZPJHHROQJ = 1;

                    }

                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.DihydroPyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && ringEnv.DoublebondInRing >= 1) {
                            if (ringEnv.SingleBond_CarbonOutRing >= 2)
                                descriptor.IIVIBYBQXSHRGE = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 3)
                                descriptor.VKHCWIJJBRBDLT = 1;
                        }
                    } 
                    if (!bond3.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4) {
                            descriptor.UBUITGRMYNAZAX = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 1)
                                descriptor.HRWQYCOJZMKBGI = 1;
                            if (ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.WYUFTYLVLQZQNH = 1;
                        }
                    }
                    if (!bond1.IsInRing && !bond2.IsInRing && !bond3.IsInRing && 
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 4) {
                            descriptor.KEGVSAQKMDDMFR = 1;
                            if (ring2Env.CarbonOutRing >= 1) {
                                descriptor.UZIKLNYKVUKZQZ = 1;
                                if (ring2Env.Carbon_OxygenOutRing >= 1)
                                    descriptor.OUAYUOJVAXWVFB = 1;
                            }
                        }
                        if (ring1Env.OxygenOutRing >= 4 && ring2Env.OxygenOutRing >= 4) {
                            descriptor.QYNRIDLOTGRNML = 1;
                            if (ring2Env.SingleBond_CarbonOutRing >= 1) {
                                descriptor.OVVGHDNPYGTYIT = 1;
                                if (ring1Env.EtherOutRing >= 1)
                                    descriptor.ODYJCLZVHOXGIZ = 1;
                                if (ring1Env.Carbon_OxygenOutRing >= 1)
                                    descriptor.DLRVVLDZNNYCBX = 1;
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-N=C=S":
                    #region
                    descriptor.LGDSHSYDSCRFAB = 1;
                    break;
                    #endregion
                case "C-C-S-C":
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2)
                            descriptor.VBDHNUHBSKNOMC = 1;
                    }
                    break;
                case "C-N=C-O":
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.EtherOutRing >= 2)
                            descriptor.YRZSSOHWUWGVCZ = 1;
                    }
                    break;
                case "C-N=C-C":
                    if (!bond1.IsInRing && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.VCYYRDKGHLOTQU = 1;
                    }
                    break;
                case "C-O-C=C":
                    descriptor.XJRBAMWJDBPFIM = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.PCPXULICWBYAAW = 1;
                    } else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.QUWFMGIQXDVKNA = 1;
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.RSDMPVNLUKICCT = 1;
                    }
                    break;
                case "C-C-S=O":
                    descriptor.PAWYOSHGFARYKR = 1;
                    break;
                case "C-N-O-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.NSBIQPJIWUJBBX = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                    }
                    break;
                case "C-O-C-O":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1)
                            descriptor.POUBACQTFWJJGJ = 1;


                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out ring2ID)) {
                            var chroman = ringsetDictionary[ring2ID];
                            if (chroman.RingIDs.Count == 2) {
                                ring3ID = chroman.RingIDs.Where(n => n != ring1ID).ToList()[0];
                                var ring3Env = ringDictionary[ring3ID].RingEnv;

                                if (ring1Env.OxygenOutRing >= 3 && ring3Env.KetonOutRing >= 1)
                                    descriptor.PURVHZPPXMOOSB = 1;
                            }
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID)) {
                        var ringEnv = ringDictionary[ring1ID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.FRMCCLBYAFUKSG = 1;
                    }
                    break;
                case "F-C-S=O":
                    if (atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.OxygenCount >= 3)
                        descriptor.CGDXUTMWWHKMOE = 1;
                    break;
                case "N-C-C-P":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.OxygenCount >= 2) {
                            descriptor.DKUVJAGWIDKYRA = 1;
                            if (atom4.AtomEnv.OxygenCount >= 3) {
                                descriptor.BBTITIVJFGSHNE = 1;
                                var bAtoms = getBranchAtom(atom3, bond2, bond3, "P");
                                if (bAtoms.Count(n => n.AtomEnv.OxygenCount >= 1) >= 1)
                                    descriptor.NEWLRZGZKYZHLJ = 1;
                            }

                        }

                       // var ringEnv = ringDictionary[targetRingID].RingEnv;
                    }
                    break;
                case "C-C:C:C":
                    if (bond2.IsSharedBondInRings &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out ring2ID) || RingtypeChecker(atom1, ringDictionary, RingFunctionType.Hydropiperidine, out ring2ID))) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.EtherOutRing >= 2 && ring2Env.CarbonOutRing >= 4)
                            descriptor.HRSIPKSSEVRSPG = 1;
                    }
                    break;
                case "C-C-O=C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclohexadiene, out ring2ID) ||
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID))) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (atom3.AtomCharge == 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.WHJNSSCPQRQPTI = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.BOCRSCLNQLUNJO = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 3 && ring2Env.CarbonOutRing >= 1)
                            descriptor.YBAQMCGPGRBLJG = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 2 && ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                            descriptor.DTGLJMVWAASSAF = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3)
                            descriptor.XRNUSRYCHMSNHQ = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 3 && ring2Env.DoubleBond_CarbonOutRing >= 1)
                            descriptor.WPFYNWPCNMKUBY = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 2 && ring1Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.YDIQGFWMZGWLGI = 1;
                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 3 && ring2Env.DoubleBond_CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.MIWPEPMEDVNBCB = 1;
                    }
                    break;
                case "C-C=O-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                       RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (atom3.AtomCharge == 1 && ring1Env.OxygenOutRing >= 1 && ring2Env.SingleBond_CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 3)
                            descriptor.HWWPQFPZWDGFFV = 1;
                    }
                    break;
                case "C-C-C-S":
                    if (atom1.AtomEnv.FCount >= 2 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.OxygenCount >= 3)
                        descriptor.LTOQTEOVRRXGBX = 1;
                    break;
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, 
            AtomProperty atom4, AtomProperty atom5, 
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            //var hydrogenCount = atom1.AtomEnv.HydrogenCount + atom2.AtomEnv.HydrogenCount
            //  + atom3.AtomEnv.HydrogenCount + atom4.AtomEnv.HydrogenCount + atom5.AtomEnv.HydrogenCount;
            var targetRingID = -1;
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            switch (connectString) {
                case "C-C-C-C-C":
                    #region
                    descriptor.OFBQJSOFQDEBGM = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.AQIXEPGDORPWBJ = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.PFEOZHBOMNWTJB = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.AFABGHUZZDYHJO = 1;
                    if (atom4.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.JYVLIDXNZAXMDK = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.XLMFDCKSFJWJTP = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.SingleBond_CarbonCount >= 3 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.VWLITJGXNSANSN = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.DoubleBond_CC_Count >= 1)
                        descriptor.ADHCYQWFCLQBFG = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.BZHMBWZPUJHVEE = 1;
                    if (atom1.AtomEnv.FCount >= 3 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.FCount >= 2 && atom5.AtomEnv.FCount >= 2)
                        descriptor.WXFBZGUXZMEPIR = 1;
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringProp = ringDictionary[targetRingID];
                        if (ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom1.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom2.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom3.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom4.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom5.AtomID)) {

                                var ringEnv = ringDictionary[targetRingID].RingEnv;
                                if (ringEnv.KetonOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                                    descriptor.JORKABJLEXAPFV = 1;
                                if (ringEnv.KetonOutRing >= 2 && ringEnv.NitrogenOutRing >= 1)
                                    descriptor.HONVBDHDLJUKLX = 1;
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.MQSXUKPGWMJYBT = 1;
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.KetonOutRing >= 1 && atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                            descriptor.QYFCERPAFOOLBB = 1;
                    }
                    break;
                    #endregion
                case "C-C-C=C-C":
                    #region
                    descriptor.QMMOXUPEWRXHJS = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.GJYMQFMQRRNLCY = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.BEQGRRJLJLVQAQ = 1;

                    if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Naphthalene, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            if (ring1Env.OxygenOutRing >= 3 && ring1Env.KetonOutRing >= 2)
                                descriptor.VKFLFQDQIQVZMK = 1;
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {

                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;

                            //chalcone
                            if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 1)
                                descriptor.DXDRHHKMWQZJHT = 1;
                    }



                    break;
                    #endregion
                case "C-C-C-C=C":
                    #region
                    descriptor.YWAKXRMUMFPDSH = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.LDTAOIUHUHHCMU = 1;
                    break;
                #endregion
                case "C-C=C-C=C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out ring2ID) ||
                        RingtypeChecker(atom5, ringDictionary, RingFunctionType.Cyclohexadiene, out ring2ID))) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 2) {
                            var bAtoms = getBranchAtom(atom3, bond2, bond3, "O");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1)
                                descriptor.CEHNAQNSWRFJSE = 1;
                        }
                    }
                    break;
                case "C-C-C-C=O":
                    #region
                    descriptor.ZTQSAGDEMFDKMZ = 1;
                    if (atom3.AtomEnv.NitrogenCount >= 1) descriptor.FGEPRNXUNITOCW = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.DVOFEOSDXAVUJD = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.DFFAMJFPVBTTBX = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.ORDNBIQZKXYGTM = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                            descriptor.VYOIELONWKIZJS = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        descriptor.YGCZTXZTJXYWCO = 1;
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                            descriptor.CQIUZHAQYHXKRY = 1;

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.EtherOutRing >= 1)
                            descriptor.ZOXCMZXXNOSBHU = 1;
                        if (ringEnv.OxygenOutRing >= 1) {
                            descriptor.AMQRNDIFPLIQBP = 1;
                            if (atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.KSPZRGLDPYUSSZ = 1;
                        }
                        if (ringEnv.OxygenOutRing >= 2) {
                            if (ringEnv.EtherOutRing >= 1)
                                descriptor.QYOYBHCGTRXATB = 1;
                            if (atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.CNPKCQMGPCOELU = 1;
                            if (atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.DZAUWHJDUNRCTF = 1;
                        }
                        
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.DXGAIOIQACHYRK = 1;
                    }
                    else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out ring1ID) && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out ring2ID)) {
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                            descriptor.SOPHXQXKBAXPAL = 1;
                    }
                    break;
                    #endregion
                case "C-C=C-C-O":
                    #region
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 2) descriptor.NEJDKFPXHQRVMV = 1;
                    if (atom4.AtomEnv.DoubleBond_CO_Count >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.EtherOutRing >= 2 && ringEnv.OxygenOutRing >= 3)
                            descriptor.PCMORTLOPMLEFB = 1;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.NGSWKAQJJWESNS = 1;
                    }
                    break;
                    #endregion
                case "C-C=C-C=O":
                    #region
                    descriptor.MLUCVPSAIODCQM = 1;
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.KJPRLNWUNMBNBZ = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 2)
                            descriptor.CDICDSOGTRCHMG = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.QAIPRVGONGVQAS = 1;
                        if (atom2.AtomEnv.SingleBond_OxygenCount >= 1) {
                            descriptor.ONLCITAWNHDTRE = 1;
                            if (ringEnv.OxygenOutRing >= 1)
                                descriptor.YBSHFNPTZIZIGY = 1;
                        }
                        if (ringEnv.EtherOutRing >= 1)
                            descriptor.AXCXHFKZHDEKTP = 1;
                        if (ringEnv.EtherOutRing >= 2)
                            descriptor.KNUFNLWDGZQKKJ = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.CVYMCVWHEXKUCO = 1;
                    }
                    break;
                    #endregion
                case "C=C-C-C-O":
                    #region
                    descriptor.ZSPTYLOMNJNZNG = 1;
                    break;
                    #endregion
                case "C-C-C-C-N":
                    #region
                    descriptor.HQABUPZFAYXKJW = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.BMFVGAAISNGQNM = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.VJROPLWGFCORRM = 1;

                    if (atom2.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        var ringInsideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        if (!ringInsideAtomDict.ContainsKey(atom1.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom2.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom3.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom4.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom5.AtomID)) {
                            
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom1.AtomEnv.DoubleBond_CO_Count >= 1) {
                                descriptor.SRJOCJYGOFTFLH = 1;
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-C-O":
                    #region
                    descriptor.LRHPLDYGYMQRHN = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.PHTQWCKDNZKARW = 1;
                    if (atom4.AtomEnv.DoubleBond_CN_Count >= 1) descriptor.DNSISZSEWVHGLH = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.BMRWNKZVCUKKSR = 1;
                    if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.FERIUCNNQQJTOY = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.AFENDNXGAFYKQO = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.YAXKTBLXMTYWDQ = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomCharge == -1)
                        descriptor.WHBMMWSBFZVSSR = 1;
                    if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.QWCKQJZIFLGMSD = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.WLBGHZBIFQNGCH = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.WLAMNBDJUVNPJU = 1;
                    if (atom2.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.OQEBBZSWEGYTPG = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.PZUOEYPTQJILHP = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.DoubleBond_CN_Count >= 1)
                            descriptor.OBSIQMZKFXFYLV = 1;
                        if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.DoubleBond_CN_Count >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.PQFMNVGMJJMLAE = 1;
                    }
                    break;
                    #endregion
                case "C-C-N=C-O":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_CarbonCount >= 2) {
                        var secondConnectedBonds = atom2.ConnectedBonds.Where(n => n.BondID != bond1.BondID && n.BondID != bond2.BondID && n.ConnectedAtoms.Count(m => m.AtomString == "H") != 1).ToList();
                        if (secondConnectedBonds.Count == 0) return;
                        foreach (var sBond in secondConnectedBonds) {
                            var connectedAtom = sBond.ConnectedAtoms.Where(n => n.AtomID != atom2.AtomID).ToList()[0];
                            if (connectedAtom.AtomEnv.SingleBond_CarbonCount >= 2) {
                                descriptor.QAISXTKWTBOYAA = 1;
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-C=O":
                    #region
                    descriptor.WBJINCZRORDGAQ = 1;
                    break;
                    #endregion
                case "C-O-C-C-O":
                    #region
                    descriptor.XNWFRZJHXBZDAG = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 2) {
                        descriptor.YTTFFPATQICAQN = 1;
                        var bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.RKOGJKGQMPZCGG = 1;
                    }

                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4) {

                            descriptor.DCZHIIAMPOQNQY = 1;

                            if (ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.SMORULFPMBCEPX = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 1) {
                                descriptor.WEKZOSZDHREWIU = 1;
                                if (atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                                    descriptor.YFDGNWYGKKBZDJ = 1;
                            }
                        }
                        
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1) {
                            var bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.AQTKXCPRNZDOJU = 1;
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.NAKWZRBQGKLMCP = 1;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.QHHFGWJKHBSQCD = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.BUWCCEMQRIMVKX = 1;
                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out ring1ID)) {
                            if (ringEnv.Carbon_KetoneOutRing >= 1)
                                descriptor.SXGTVXYSXBZEBN = 1;

                            var otherRingEnv = ringDictionary[ringsetDictionary[ring1ID].RingIDs.Where(n => n != targetRingID).ToList()[0]].RingEnv;
                            if (ringEnv.OxygenOutRing >= 3 && otherRingEnv.KetonOutRing >= 1)
                                descriptor.MLLROTAVTINRFA = 1;

                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 6)
                            descriptor.QUTQPHMWNPVJTB = 1;
                    }
                    else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromenylium, out targetRingID)) {
                        var chromehylium = ringsetDictionary[targetRingID];
                        if (chromehylium.RingIDs.Count == 2) {
                            var benzen = ringDictionary[chromehylium.RingIDs[0]].RingFunctionType == RingFunctionType.Benzene 
                                ? ringDictionary[chromehylium.RingIDs[0]] 
                                : ringDictionary[chromehylium.RingIDs[1]];
                            if (benzen.RingEnv.OxygenOutRing >= 1)
                                descriptor.FSSKXIOSLFOPBN = 1;
                        }
                    }
                    break;
                    #endregion
                case "O-C-C-C-S":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1) 
                        descriptor.XUJNEKJLAYXESH = 1;
                    break;
                    #endregion
                case "C-C-N-C-C":
                    #region
                    descriptor.HPNMFZURTQLUMO = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) descriptor.GNVRJGIVDSQCOP = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) {
                        var thirdAtomConnectedBond = atom3.ConnectedBonds.Where(n => n.BondID != bond2.BondID && n.BondID != bond3.BondID).ToList()[0];
                        var thirdAtomConnectedAtom = thirdAtomConnectedBond.ConnectedAtoms.Where(n => n.AtomID != atom3.AtomID).ToList()[0];
                        if (thirdAtomConnectedAtom.AtomEnv.SingleBond_CarbonCount >= 1)
                            descriptor.ZMANZCXQSJIPKH = 1;
                    }
                    
                    break;
                    #endregion
                case "N-C-C-C-O":
                    #region
                    descriptor.WUGQZFFCHPXWKQ = 1;
                    if (atom4.AtomEnv.DoubleBond_CN_Count >= 1) descriptor.RSDOASZYYCOXIB = 1;
                    if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.UCMIRNVEIXFBKS = 1;

                    break;
                    #endregion
                case "O-C-C-C-O":
                    #region
                    descriptor.YPFDHNVEDLHUCE = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2) descriptor.PUPZLCDOIYMWBV = 1;
                    if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.JVTAAEKCZFNVCJ = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.PEDCQBHIVMGVHV = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.RBNPOMFGQQGHHO = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 4)
                        descriptor.SLCVBVWXLSEKPL = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.QWGRWMMWNDWRQN = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.MQVRLONNONYDJP = 1;
                    break;
                    #endregion
                case "C-N:C-C-C":
                    #region
                    if (bond2.IsInRing && RingtypeChecker(bond2, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        var ringInsideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        if (!ringInsideAtomDict.ContainsKey(atom1.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom2.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom3.AtomID) &&
                            !ringInsideAtomDict.ContainsKey(atom4.AtomID) &&
                            !ringInsideAtomDict.ContainsKey(atom5.AtomID)) {
                                if (atom3.AtomEnv.NitrogenCount == 1) {
                                    descriptor.NNUKGGJBMPLKIW = 1;
                                }
                        }
                    }
                    break;
                    #endregion
                case "N-C-C-C:N":
                    #region
                    if (atom4.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        descriptor.NTYJJOPFIAHURM = 1;
                    }
                    break;
                    #endregion
                case "O-C-C-C-C":
                    #region
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        var ringInsideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        if (ringInsideAtomDict.ContainsKey(atom1.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom2.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom3.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom4.AtomID) &&
                            ringInsideAtomDict.ContainsKey(atom5.AtomID)) {

                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                                descriptor.VVDIZWFSGJERMA = 1;
                            if (ringEnv.CarbonOutRing >= 2 && ringEnv.OxygenOutRing >= 1 && ringEnv.KetonOutRing >= 1)
                                descriptor.BSTCHFGEIXFVRN = 1;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.KZVAAIRBJJYZOW = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.CJJCPDZKQKUXSS = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 2)
                                descriptor.RFSUNEUAIZKAJO = 1;
                            if (ringEnv.Carbon_OxygenOutRing >= 2 && ringEnv.OxygenOutRing >= 2)
                                descriptor.MCHWWJLLPNDHGL = 1;
                            if (ringEnv.KetonOutRing >= 1 && ringEnv.Carbon_OxygenOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 3)
                                descriptor.XHFLOEDHTDPKPD = 1;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.KetonOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.GPDLGPXIXSDAIU = 1;
                        }
                    }
                    break;
                    #endregion
                case "O-C-C-O-P":
                    #region
                    if (atom5.AtomEnv.OxygenCount >= 3)
                        descriptor.ACXJKDUFAXMPOM = 1;
                    if (atom5.AtomEnv.OxygenCount >= 4) {
                        descriptor.PCIBVZXUNDZWRL = 1;
                    }
                    break;
                    #endregion
                case "C-C:C:C-O":
                    #region
                    if (atom2.IsInRing && RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Benzodioxole, out targetRingID)) {
                        descriptor.GHPODDMCSOYWNE = 1;
                    }
                    break;
                    #endregion
                case "N-C:N:C:N":
                    #region
                    if (atom2.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        if (RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out targetRingID)) {
                            descriptor.GFFGJBXGBJISGV = 1;
                            if (atom1.AtomEnv.SingleBond_CarbonCount >= 2)
                                descriptor.CKOMXBHMKXXTNW = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-N:C:C:N":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) 
                        && RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out targetRingID)) {
                            if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Imidazole, out ring2ID) 
                                && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrimidine, out ring3ID)) {
                                var ring1 = ringDictionary[ring1ID];
                                var ring2 = ringDictionary[ring2ID];
                                var ring3 = ringDictionary[ring3ID];

                                if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.CarbonOutRing >= 1 &&
                                    ring3.RingEnv.NitrogenOutRing >= 3 && ring3.RingEnv.OxygenOutRing >= 1)
                                    descriptor.FBLYADUDJIDSCH = 1;
                               
                            }

                    }
                    break;
                    #endregion
                case "N-C-C-O-P":
                    #region
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom5.AtomEnv.OxygenCount >= 4)
                        descriptor.YHHSONZFOIEMCP = 1;
                    if (atom5.AtomEnv.OxygenCount >= 4)
                        descriptor.SUHOOTKUPISOBE = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 3 && atom1.AtomCharge == 1 &&
                        atom5.AtomEnv.OxygenCount >= 4)
                        descriptor.XNORYOIOVQAIQB = 1;
                    break;
                    #endregion
                case "C-O-P-O-P":
                    #region
                    if (atom5.AtomEnv.OxygenCount >= 3 && atom3.AtomEnv.OxygenCount >= 3 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4)
                            descriptor.JLAOMXITMPJYDG = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-N-C":
                    #region
                    descriptor.GVWISOJSERXQBM = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.ZUHZZVMEUAUWHY = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.SASNBVQSOZSTPD = 1;
                    }
                    else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out targetRingID))
                            descriptor.NCIKQJBVUNUXLW = 1;
                    }
                    break;
                    #endregion
                case "O-C=C-C-O":
                    #region
                    if (atom4.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.TWSCVZUCNWUBGX = 1;
                    break;
                #endregion
                case "O-C=C-C=O":
                    #region
                    descriptor.GMSHJLUJOABYOM = 1;
                    break;
                    #endregion
                case "O-C-C-C=O":
                    #region
                    descriptor.AKXKFZDCRYJKTF = 1;
                    if (atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.UXPVGJOKPUABJV = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.MNQZXJOMYWMBOU = 1;
                    break;
                    #endregion
                case "C-C-O-C-C":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.XEKOWRVHYACXOJ = 1;
                    break;
                    #endregion
                case "N-C-C-C-N":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Morpholine, out targetRingID))
                        descriptor.UIKUBYKUYUSRSM = 1;
                    break;
                    #endregion
                case "C-O-N-C=O":
                    #region
                    descriptor.BYYUJTPNKCFZST = 1;
                    break;
                    #endregion
                case "C-C-O-C-O":
                    #region
                    descriptor.RRLWYLINGKISHN = 1;
                    break;
                    #endregion
                case "C-N-C-C-O":
                    #region
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.UEEJHVSXFDXPFK = 1;
                    break;
                    #endregion
                case "C-C=N-O-S":
                    #region
                    if (atom5.AtomEnv.OxygenCount >= 4)
                        descriptor.ZJARKNXLSAIKJE = 1;
                    break;
                    #endregion
                case "C-C-C-O-C":
                    #region
                    descriptor.VNKYTQGIUYNRMY = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.FVNIMHIOIXPIQT = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                        if (atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.VOUIHMBRJVKANW = 1;
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CO_Count >= 1) {
                            descriptor.FYRXXSCCOWRCLX = 1;
                        }

                        if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CO_Count >= 1 &&
                                ring2Env.OxygenOutRing >= 4) {
                                descriptor.YVORLBJZJIGULG = 1;
                                if (ring2Env.Carbon_OxygenOutRing >= 1)
                                    descriptor.KIBWUBXJNWEJNV = 1;
                            }
                        }

                    }
                    break;
                    #endregion
                case "C-C-N=C-N":
                    #region
                    descriptor.MCGDEFQPHREUNF = 1;
                    break;
                    #endregion
                case "C-O-C-C=O":
                    #region
                    descriptor.YSEFYOVWKJXNCH = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4) {
                            descriptor.MZWSHEJWANSXEH = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 1) {
                                descriptor.QPDOOAOPIHRGIC = 1;
                                if (atom3.AtomEnv.DoubleBond_CC_Count >= 1)
                                    descriptor.BIVCHTRQZFQIBX = 1;
                                if (ringEnv.Carbon_OxygenOutRing >= 1) {
                                    descriptor.QBYFEBYWGWYNTO = 1;
                                    if (atom3.AtomEnv.DoubleBond_CC_Count >= 1)
                                        descriptor.AMLAKRUHAQODRI = 1;
                                }
                            }
                        }
                    }
                        break;
                    #endregion
                case "C-O-C-C=C":
                    #region
                    if (atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.BAPJBEWLBFYGME = 1;
                    break;
                    #endregion
                case "O=C-C-S=O":
                    #region
                    if (atom4.AtomEnv.OxygenCount >= 3)
                        descriptor.JTJIXCMSHWPJJE = 1;
                    break;
                    #endregion
                case "C-C-O-P-O":
                    if (atom4.AtomEnv.OxygenCount >= 4 && atom5.AtomCharge == -1)
                        descriptor.ZJXZSIYSNXKHEA = 1;
                    break;
                case "C-C=C-O-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && atom3.AtomEnv.SingleBond_CarbonCount >= 1) {
                            descriptor.PZPQXEDPDKLPRN = 1;
                            if (ringEnv.EtherOutRing >= 1)
                                descriptor.AYKRZZCBPHNROJ = 1;
                        }

                        var bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                            descriptor.KPNHONAEPLEAJL = 1;
                            if (ringEnv.OxygenOutRing >= 1) {
                                descriptor.YODAPCPITHANRU = 1;
                            }
                        }

                        if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ring2Env.OxygenOutRing >= 2)
                                descriptor.YMJTVJQVXGBEJC = 1;
                            if (ringEnv.OxygenOutRing >= 1 && ring2Env.OxygenOutRing >= 2)
                                descriptor.ZRFRNFXJUHDIAP = 1;
                        }
                        else if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 2 && ring2Env.OxygenOutRing >= 4)
                                descriptor.BJHFCSYKUJICOO = 1;
                            if (ring2Env.OxygenOutRing >= 4 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                                descriptor.XBNXEPBRQKLHLS = 1;
                            }
                            if (ringEnv.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 4 && 
                                ring2Env.SingleBond_CarbonOutRing >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 1)
                                descriptor.JGCKLVYWXXUJPX = 1;
                        }
                    }
                    break;
                case "C-O-C=C-O":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.JSAAGGWDCXOBNL = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4) {
                            if (ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.XTESTHXDVBPIGF = 1;

                            var bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                                if (ringEnv.SingleBond_CarbonOutRing >= 1)
                                    descriptor.FLDSHIUBZABFCL = 1;
                                if (ringEnv.Carbon_OxygenOutRing >= 1)
                                    descriptor.IQGAPHDCDWBTLD = 1;
                            }
                        }
                    }
                    break;
                case "C-C-C-C-S":
                    if (atom1.AtomEnv.FCount >= 2 && atom2.AtomEnv.FCount >= 2 && atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.FCount >= 2 && atom5.AtomEnv.OxygenCount >= 3)
                        descriptor.UTWCWKUZICRKOA = 1;
                    break;
                case "C-C:C:C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out ring2ID) &&
                        RingtypeChecker(atom5, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3 && ring3Env.OxygenOutRing >= 3 && ring3Env.Carbon_OxygenOutRing >= 1)
                            descriptor.BXQPHIBPVVBVAB = 1;

                    }
                    break;
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, 
            AtomProperty atom5, AtomProperty atom6, 
            BondProperty bond1, BondProperty bond2, BondProperty bond3, 
            BondProperty bond4, BondProperty bond5, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            //var hydrogenCount = atom1.AtomEnv.HydrogenCount + atom2.AtomEnv.HydrogenCount
            //  + atom3.AtomEnv.HydrogenCount + atom4.AtomEnv.HydrogenCount
            //  + atom5.AtomEnv.HydrogenCount + atom6.AtomEnv.HydroxyCount;

            var targetRingID = -1;
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var bAtoms = new List<AtomProperty>();

            switch (connectString) {
                case "C-C-C-C-C-C":
                    #region
                    descriptor.VLKZOEOYAKHREP = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.VLJXXKKOSFGPHI = 1;
                    if (atom3.AtomEnv.DoubleBond_CC_Count >= 1 && atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.YXLCVBVDFKWWRW = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 3 && atom6.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.PFBUXLQVTTXUQM = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 3 && atom6.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.NZQMQVJXSRMTCJ = 1;
                    if (atom1.AtomEnv.FCount >= 3 && atom2.AtomEnv.FCount >= 2 &&
                        atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.FCount >= 2 &&
                        atom5.AtomEnv.FCount >= 2 && atom6.AtomEnv.FCount >= 2)
                        descriptor.XJSRKJAHJGCPGC = 1;

                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringProp = ringDictionary[targetRingID];
                        if (ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom1.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom2.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom3.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom4.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom5.AtomID) &&
                            ringProp.RingEnv.InsideAtomDictionary.ContainsKey(atom6.AtomID)) {

                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.SingleBond_CarbonOutRing >= 2) {
                                descriptor.KVZJLSYJROEPSQ = 1;
                                if (ringEnv.DoubleSingleCarbonsOutRing >= 1)
                                    descriptor.QEGNUYASOUJEHD = 1;
                            }
                            if (ringEnv.SingleBond_CarbonOutRing >= 1 && ringEnv.DoubleBond_CarbonOutRing >= 1)
                                descriptor.QJXCDRBHYIAFTH = 1;
                            if (ringEnv.CarbonOutRing >= 3 && ringEnv.Carbon_AlkaneOutRing >= 1 && ringEnv.DoubleSingleCarbonsOutRing >= 1)
                                descriptor.HIVFIUIKABOGIO = 1;
                            if (ringEnv.CarbonOutRing >= 3 && ringEnv.Carbon_AlkaneOutRing >= 1 &&
                                ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.DoubleBond_CarbonOutRing >= 1)
                                descriptor.NYFHUVYMLLAQHH = 1;
                            if (ringEnv.CarbonOutRing >= 4 && ringEnv.Carbon_AlkaneOutRing >= 2 && ringEnv.DoubleBond_CarbonOutRing >= 1)
                                descriptor.NXCPHYJTOJDSLE = 1;
                            if (ringEnv.CarbonOutRing >= 4 && ringEnv.Carbon_AlkaneOutRing >= 2)
                                descriptor.IRXJZDLOCNJAHE = 1;
                            if (ringEnv.CarbonOutRing >= 5 && ringEnv.DoubleSingleCarbonsOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1) {
                                var outAtoms = ringEnv.OutsideAtomDictionary;
                                foreach (var oAtom in outAtoms) {
                                    if (oAtom.Value.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 3) >= 1) {
                                        descriptor.OTBOAOSTJYUVHP = 1;
                                        break;
                                    }
                                }
                            }
                        }
                    }
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) && RingtypeChecker(atom5, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID)) {

                        var ring1Prop = ringDictionary[ring1ID];
                        var ring2Prop = ringDictionary[ring2ID];

                        if (ring1ID != ring2ID &&
                            ring1Prop.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 && ring2Prop.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1) {
                            var ring1AtomDict = ring1Prop.RingEnv.InsideAtomDictionary;
                            var ring2AtomDict = ring2Prop.RingEnv.InsideAtomDictionary;
                            if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && !ring2Prop.RingEnv.InsideAtomDictionary.ContainsKey(atom6.AtomID))
                                descriptor.XBENKLBOLWZLNQ = 1;
                        }
                    }
                    else if (bond2.IsSharedBondInRings && atom5.IsSharedAtomInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexene, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclopentane, out ring2ID) &&
                        RingtypeChecker(atom6, ringDictionary, RingFunctionType.Cyclopentane, out ring3ID)) {
                        if (ring2ID != ring3ID) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            var ring3Env = ringDictionary[ring3ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 1 && ring1Env.CarbonOutRing >= 3 &&
                                ring3Env.DoubleBond_CarbonOutRing >= 1 && ring3Env.OxygenOutRing >= 1)
                                descriptor.HGFZBZIWLICHFG = 1;
                        }
                    }
                    break;
                #endregion
                case "C-C=C-C-C=C":
                    #region
                    descriptor.PRBHEGAFLDMLAL = 1;
                    break;
                #endregion
                case "C-C-C-C-C-O":
                    #region
                    descriptor.AMQJEAYHLZJPGS = 1;
                    if (atom4.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.UZRUKLJLGXLGDM = 1;
                    if (atom4.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.SNDPXSYFESPGGJ = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.CJXCLBPFKGZXJP = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.FORGMRSGVSYZQR = 1;
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.CZWARROQQFCFJB = 1;
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom2.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.MPUUQNGXJSEWTF = 1;
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KABXUUFDPUOJMW = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.AALKGALVYCZETF = 1;
                    if (atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.WCVRQHFDJLLWFE = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.OVOVDHYEOQJKMD = 1;
                    break;
                #endregion
                case "C-C-N-C-C-O":
                    #region
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.RILLZYSZSDGYGV = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) {
                        var nBranchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (nBranchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                            descriptor.BFSVOASYOCHEOV = 1;
                    }
                    break;
                #endregion
                case "C-C-C=C-C-C":
                    #region
                    descriptor.ZQDPJFUHLCOCRG = 1;
                    break;
                #endregion
                case "C-C-C-C=C-C":
                    #region
                    descriptor.RYPKRALMXUUNKS = 1;
                    break;
                #endregion
                case "C-C-C-C-C=C":
                    #region
                    descriptor.LIKMAJRDDDTEIG = 1;
                    if (atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.BVOSSZSHBZQJOI = 1;

                    break;
                #endregion
                case "C-O-C-C-C-O":
                    #region
                    descriptor.JDFDHBSESGTDAL = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 2 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.GZXFYIDYTCWDDN = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4) {
                            descriptor.QTIFSOVGBSROEQ = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 2) {
                                var branchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                                if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) {
                                    descriptor.ADKODQNXEFIOIC = 1;
                                }
                            }
                        }
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1) {
                            if (ringEnv.OxygenOutRing >= 4) {
                                descriptor.BZQBQGKZJCGZCL = 1;
                                if (ringEnv.SingleBond_CarbonOutRing >= 1)
                                    descriptor.YHUSUSKGDGXWGS = 1;
                                if (ringEnv.Carbon_OxygenOutRing >= 1)
                                    descriptor.NHJUPBDCSOGIKX = 1;
                            }
                        }

                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 2
                            && ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1) {
                            var branchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) {
                                descriptor.DUUKYOAVWFMSKJ = 1;
                            }
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 6)
                            descriptor.BLQTVZFBJSHRHL = 1;
                    }

                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 &&
                    RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out targetRingID)) {
                        var chromoneRingIDs = ringsetDictionary[targetRingID].RingIDs;
                        if (chromoneRingIDs.Count == 2) {
                            var benzene = ringDictionary[chromoneRingIDs[0]].RingFunctionType == RingFunctionType.Benzene ?
                                ringDictionary[chromoneRingIDs[0]] : ringDictionary[chromoneRingIDs[1]];
                            if (benzene.RingEnv.OxygenOutRing >= 3)
                                descriptor.BWUSUWNUKKXAEV = 1;
                        }
                    }
                    break;
                #endregion
                case "C-C-C-C-C=O":
                    #region
                    descriptor.HGBOYTHUEUWSSQ = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.ZOFRRNUENOHELM = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.QCAAXUQRSHUEHU = 1;
                    
                    break;
                #endregion
                case "C=C-C=C-C-O":
                    #region
                    descriptor.ACZNIBVNGPLHAC = 1;
                    if (atom5.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.VAOJQRMYAXTUQI = 1;
                    break;
                #endregion
                case "C-C-C-C-C-N":
                    #region
                    descriptor.DPBLXKKOBLCELK = 1;
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.JDAMFKGXSUOWBV = 1;
                    break;
                #endregion
                case "C-C-O-C-O-C":
                    #region
                    descriptor.CHCLGECDSSWNCP = 1;
                    break;
                #endregion
                case "C-C-C-O-C-C":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CC_Count >= 1 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.SUPCQIBBMFXVTL = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.WDAXFOBOLVPGLV = 1;
                    break;
                #endregion
                case "C-C-C-N-C-C":
                    #region
                    if (atom5.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.VLSTXUUYLIALPB = 1;
                    if (atom2.AtomEnv.SingleBond_NitrogenCount >= 1 && atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.SKPCTMWFLGENTL = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.RNFDZDMIFOFNMC = 1;
                    break;
                #endregion
                case "C-C-C-C-N=C":
                    #region
                    if (atom6.AtomEnv.NitrogenCount >= 3)
                        descriptor.CGWBIHLHAGNJCX = 1;
                    break;
                #endregion
                case "C-S-C-C-C-N":
                    #region
                    descriptor.KKYSBGWCYXYOHA = 1;
                    break;
                #endregion
                case "O-C-C-C-C-N":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.WCVPFJVXEXJFLB = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.BTCSSZJGUNDROE = 1;
                    break;
                #endregion
                case "O-C=N-C-C-O":
                    #region
                    if (atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.UGJBHEZMOKVTIM = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 1 && atom4.AtomEnv.SingleBond_CarbonCount >= 2 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KTHDTJVBEPMMGL = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 2 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.SRBATDDRZARFDZ = 1;
                    break;
                #endregion
                case "O-C-C-C-C-O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KDYFGRWQOYBRFD = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.ARXKVVRQIIOZGF = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.UNXHWFMMPAWVPI = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.MRKRITSFTIHTAQ = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.JPIJQSOTBSSVTP = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.FJGNTEKSQVNVTJ = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.DZAIOXUZHHTJKN = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.JJMZVBNCCUWEDF = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_CarbonCount >= 3 && atom5.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.WXUAQHNMJWJLTG = 1;
                    break;
                #endregion
                case "O-C-C-C-C=O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.UIUJIQZEACWQSV = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.CGJJPOYORMGCGE = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.WDRISBUVHBMJEF = 1;
                    break;
                #endregion
                case "O-C-C=C-C-O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.RMQJECWPWQIIPW = 1;
                    break;
                #endregion
                case "O-C-C-C-C-C":
                    #region
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var insideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        var outsideAtomDict = ringDictionary[targetRingID].RingEnv.OutsideAtomDictionary;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;

                        if (insideAtomDict.ContainsKey(atom1.AtomID) &&
                            insideAtomDict.ContainsKey(atom2.AtomID) &&
                            insideAtomDict.ContainsKey(atom3.AtomID) &&
                            insideAtomDict.ContainsKey(atom4.AtomID) &&
                            insideAtomDict.ContainsKey(atom5.AtomID) &&
                            insideAtomDict.ContainsKey(atom6.AtomID)) {

                            if (ringEnv.OxygenOutRing >= 2)
                                descriptor.MMQYUDBFDDEJBP = 1;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                                descriptor.UXUFMKFUJJLWKM = 1;
                            if (ringEnv.OxygenOutRing >= 3)
                                descriptor.QXAMTEJJAZOINB = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 1)
                                descriptor.BJBURJZEESAQPG = 1;
                            if (ringEnv.Carbon_OxygenOutRing >= 1 && ringEnv.OxygenOutRing >= 3)
                                descriptor.MPCAJMNYNOGXPB = 1;
                            if (ringEnv.CarbonOutRing >= 1 && ringEnv.OxygenOutRing >= 4)
                                descriptor.SHZGCJCMOBCMKK = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                                descriptor.JMZFHBIADDYOTC = 1;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.WQZGKKKJIJFFOK = 1;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                                descriptor.AEMOLEFTQBMNLQ = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 2 && ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1)
                                descriptor.RMRBBZNNXRPEQH = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.SingleBond_CarbonOutRing >= 1 && ringEnv.NitrogenOutRing >= 1)
                                descriptor.TVTGZVYLUHVBAJ = 1;
                            if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.QFHKFGOUFKUPNX = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.PMMURAAUARKVCB = 1;
                            if (ringEnv.OxygenOutRing >= 4) {
                                descriptor.SRBFZHDQGSBBOR = 1;
                                if (ringEnv.EtherOutRing >= 1)
                                    descriptor.ZBDGHWFPLXXWRD = 1;
                            }
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 1 && ringEnv.SulfurOutRing >= 1)
                                descriptor.JUSMHIGDXPKSID = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1)
                                descriptor.QVNPTEPOWBEITR = 1;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1)
                                descriptor.QNKOVWCOVLYPKR = 1;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.EtherOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1)
                                descriptor.OHWCAVRRXKJCRB = 1;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.HOVAGTYPODGVJG = 1;
                        }
                    }
                    break;
                #endregion
                case "C:N:C:N:C:C":
                    #region
                    if (atom1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var insideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        if (insideAtomDict.ContainsKey(atom1.AtomID) &&
                            insideAtomDict.ContainsKey(atom2.AtomID) &&
                            insideAtomDict.ContainsKey(atom3.AtomID) &&
                            insideAtomDict.ContainsKey(atom4.AtomID) &&
                            insideAtomDict.ContainsKey(atom5.AtomID) &&
                            insideAtomDict.ContainsKey(atom6.AtomID)) {
                            if (atom1.AtomEnv.NitrogenCount >= 2 && atom3.AtomEnv.OxygenCount >= 1) {
                                descriptor.OPTASPLRGRRNAP = 1;
                            }
                            if (atom1.AtomEnv.OxygenCount >= 1 && atom3.AtomEnv.OxygenCount >= 1) {
                                descriptor.ISAKRJDGNUQOIC = 1;
                            }
                            if (atom1.AtomEnv.OxygenCount >= 1 && atom3.AtomEnv.OxygenCount >= 1 && atom6.AtomEnv.CarbonCount >= 3) {
                                descriptor.RWQNBRDOKXIBIV = 1;
                            }
                        }
                    }
                    break;
                #endregion
                case "N:C:C:C:C:C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.CarbonOutRing >= 1)
                            descriptor.AQSRRZGQRFFFGS = 1;
                        if (ringEnv.CarbonOutRing >= 3 && ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.SOYIPVHKLPDELX = 1;
                    }
                    break;
                #endregion
                case "C-C-C-C:C:N":
                    #region
                    if (bond4.IsInRing && RingtypeChecker(bond4, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        descriptor.MLAXEZHEGARMPE = 1;
                    }
                    else if (bond4.IsInRing && RingtypeChecker(bond4, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_NitrogenCount >= 1)
                            descriptor.HNDVDQJCIGZPNO = 1;
                    }
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && RingsettypeChecker(atom4, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID))
                        descriptor.GOLXRNDWAUTYKT = 1;
                    break;
                #endregion
                case "C-C:C:N:C-C":
                    #region
                    if (atom2.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.DJCJOWDAAZEMCI = 1;

                    }
                    break;
                #endregion
                case "C-C:C:N:C:C":
                    #region
                    if (atom2.IsInRing && RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.CarbonCount >= 2 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.GMUFGAIZRAMTEN = 1;
                    }
                    break;
                #endregion
                case "C:C:C:C:C:C": //benzen
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        var ringProp = ringDictionary[targetRingID];
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2)
                            descriptor.GHMLBKRAJCXXBS = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.CarbonOutRing >= 1)
                            descriptor.IWDCLRJOBJJRNH = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.HXDOZKJGKXYMEW = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.FUGYGGDSWSUORM = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.CarbonOutRing >= 1)
                            descriptor.CHLICZRVGGXEOD = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                            descriptor.ZBCATMYQYDCTIZ = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.LHGVFZTZFXWLCP = 1;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.QCDYQQDYXPDABM = 1;
                        if (ringEnv.OxygenOutRing >= 4)
                            descriptor.RDJUHLUBPADHNP = 1;
                        if (ringEnv.CarbonOutRing >= 1 && ringProp.RingEnv.ClOutRing >= 1)
                            descriptor.NPDACUSDTOMAMK = 1;
                        //if (ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                        //    descriptor.PECUPOXPPBBFLU = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.FBTSUTGMWBDAAC = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.HFLGBNBLMBSXEM = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.CarbonOutRing >= 1)
                            descriptor.BPHYZRNTQNPLFI = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.OxygenOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                            descriptor.PETRWTHZSKVLRE = 1;
                        if (ringEnv.EtherOutRing >= 2)
                            descriptor.ABDKAPXRBAPSQN = 1;
                        if (ringEnv.ClOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.AVPYQKSLYISFPO = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.YOMSJEATGXXYPX = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.SMQUZDBALVYZAC = 1;
                        //if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_KetoneOutRing >= 1)
                        //    descriptor.CRPNQSVBEWWHIJ = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.BTQAJGSMXCDDAJ = 1;
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.KUSIZNPTWCYZMX = 1;
                        if (ringEnv.EtherOutRing >= 2 && ringEnv.CarbonOutRing >= 1)
                            descriptor.GYPMBQZAVBFUIZ = 1;

                        if (ringEnv.EtherOutRing >= 2 && ringEnv.OxygenOutRing >= 3)
                            descriptor.XQDNFAMOIPNVES = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.GEEXFDOMGJGVHC = 1;
                        if (ringEnv.EtherOutRing >= 3)
                            descriptor.AGIQIOSHSMJYJP = 1;
                        if (ringEnv.BrOutRing >= 2 && ringEnv.OxygenOutRing >= 1)
                            descriptor.FAXWFCTVSHEODL = 1;
                        if (ringEnv.BrOutRing >= 2 && ringEnv.EtherOutRing >= 1)
                            descriptor.XGXUGXPKRBQINS = 1;
                        if (ringEnv.BrOutRing >= 2 && ringEnv.OxygenOutRing >= 2)
                            descriptor.PSHYJLFHLSZNLM = 1;
                        if (ringEnv.BrOutRing >= 2 && ringEnv.ClOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.AQDQLNMPFWYWEN = 1;
                        if (ringEnv.BrOutRing >= 3 && ringEnv.OxygenOutRing >= 2)
                            descriptor.HHRZYEXLGLQGKQ = 1;

                        if (ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                            descriptor.WPYMKLBDIGXBTP = 1;
                        if (ringEnv.NitrogenOutRing >= 1 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                            descriptor.ALYNCZNDIQEVRV = 1;
                        if (ringEnv.EtherOutRing >= 2)
                            descriptor.DPZNOMCNRMUKPS = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.BrOutRing >= 1)
                            descriptor.JPBDMIWPTFDFEU = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.BrOutRing >= 1)
                            descriptor.GZKWULQSPPFVMV = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 2 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.FQRQWPNYJOFDLO = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.OIPPWFOQEKKFEE = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.DWLZULQNIPIABE = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.UAJRSHJHFRVGMG = 1;
                        if (ringEnv.EtherOutRing >= 2 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.NJXYTXADXSRFTJ = 1;
                        if (ringEnv.Carbon_KetoneOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.IUNJCFABHJZSKB = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.HDNRAPAFJLXKBV = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1)
                            descriptor.HDVRLUFGYQYLFJ = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.PCYGLFXKCBFGPC = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_KetoneOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.SULYEHHGGXARJS = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.CAWZFQGJUGEKFU = 1;
                        if (ringEnv.EtherOutRing >= 2 && ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.RIZBLVRXRWHLFA = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.DZJPDDVDKXHRLF = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_KetoneOutRing >= 1)
                            descriptor.ABNNGIOYMQTQQO = 1;
                        if (ringEnv.EtherOutRing >= 2 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.NEBQMYHKOREVAL = 1;
                        if (ringEnv.EtherOutRing >= 3) {
                            descriptor.CRUILBNAQILVHZ = 1;
                            if (ringEnv.SingleBond_CarbonOutRing >= 1)
                                descriptor.KCIZTNZGSBSSRM = 1;
                        }
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.IIGNZLVHOZEOPV = 1;
                        if (ringEnv.EtherOutRing >= 1 && ringEnv.OxygenOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.NOTCZLKDULMKBR = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.ZZMFDMIBZYLXQE = 1;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.EtherOutRing >= 1 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.ZENOXNGFMSCLLL = 1;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.EtherOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1)
                            descriptor.FALWUVSXNUUXQA = 1;
                        if (ringEnv.BrOutRing >= 2 && ringEnv.IOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.JYJAXNKEZJHGMJ = 1;

                        var insideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        var outsideAtomDict = ringDictionary[targetRingID].RingEnv.OutsideAtomDictionary;

                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 2) {
                            var counter = 0;
                            foreach (var outAtoms in outsideAtomDict.Values) {
                                foreach (var oAtom in outAtoms) {
                                    if (oAtom.AtomEnv.SingleBond_CarbonCount >= 4)
                                        counter++;
                                }
                            }
                            if (counter >= 2)
                                descriptor.DKCPKDPYUFEZCP = 1;
                        }

                        if (insideAtomDict.ContainsKey(atom1.AtomID) &&
                            insideAtomDict.ContainsKey(atom2.AtomID) &&
                            insideAtomDict.ContainsKey(atom3.AtomID) &&
                            insideAtomDict.ContainsKey(atom4.AtomID) &&
                            insideAtomDict.ContainsKey(atom5.AtomID) &&
                            insideAtomDict.ContainsKey(atom6.AtomID)) {

                            if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Coumarin, out ring2ID)) {
                                if (ringEnv.OxygenOutRing >= 3)
                                    descriptor.ILEDWLMCKZNDJK = 1;
                                if (ringEnv.OxygenOutRing >= 2)
                                    descriptor.ORHBXUUXSCNDEV = 1;
                            }
                            else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out ring2ID)) {
                                descriptor.OTAFHZMPRISVEM = 1;
                                if (ringEnv.OxygenOutRing >= 2) {
                                    descriptor.WVJCRTSTRGRJJT = 1;
                                    if (ringsetDictionary[ring2ID].RingIDs.Count == 2) {
                                        var otherRingID = ringsetDictionary[ring2ID].RingIDs.Where(n => n != targetRingID).ToList()[0];
                                        var otherRing = ringDictionary[otherRingID];
                                        if (otherRing.RingEnv.EtherOutRing >= 1)
                                            descriptor.LRCHDYPFGNFWLO = 1;

                                    }
                                }
                                if (ringEnv.OxygenOutRing >= 3) {
                                    descriptor.NYCXYKOXLNBYID = 1;
                                    if (ringsetDictionary[ring2ID].RingIDs.Count == 2) {
                                        var otherRingID = ringsetDictionary[ring2ID].RingIDs.Where(n => n != targetRingID).ToList()[0];
                                        var otherRing = ringDictionary[otherRingID];
                                        if (otherRing.RingEnv.OxygenOutRing >= 2)
                                            descriptor.OZNMEZAXFKUCPN = 1;
                                        if (otherRing.RingEnv.EtherOutRing >= 1)
                                            descriptor.HXDPHXHZEUZQRO = 1;
                                    }
                                }
                            }
                            else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out ring2ID)) {

                                if (ringEnv.OxygenOutRing >= 3)
                                    descriptor.NDDUBDUPYJZIAK = 1;
                                if (ringsetDictionary[ring2ID].RingIDs.Count == 2) {
                                    var otherRingID = ringsetDictionary[ring2ID].RingIDs.Where(n => n != targetRingID).ToList()[0];
                                    var otherRing = ringDictionary[otherRingID];
                                    if (otherRing.RingEnv.OxygenOutRing >= 1)
                                        descriptor.GUBPZPIZJGZPPT = 1;
                                    if (otherRing.RingEnv.KetonOutRing >= 1)
                                        descriptor.MSTDXOZUKAQDRL = 1;
                                    if (otherRing.RingEnv.KetonOutRing >= 1 && ringEnv.EtherOutRing >= 2)
                                        descriptor.DISYDHABSCTQFK = 1;
                                    if (otherRing.RingEnv.KetonOutRing >= 1 && ringEnv.EtherOutRing >= 2 && ringEnv.OxygenOutRing >= 3)
                                        descriptor.WDIFRGCUOJVZLO = 1;
                                }
                                if (ringEnv.EtherOutRing >= 2)
                                    descriptor.YXNZWUPLVHBMCC = 1;
                            }
                            else if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromenylium, out ring2ID)) {
                                if (ringsetDictionary[ring2ID].RingIDs.Count == 2) {
                                    var otherRingID = ringsetDictionary[ring2ID].RingIDs.Where(n => n != targetRingID).ToList()[0];
                                    var otherRing = ringDictionary[otherRingID];
                                    if (otherRing.RingEnv.OxygenOutRing >= 1 && ringEnv.OxygenOutRing >= 3)
                                        descriptor.TYYUTXYNKKBNFV = 1;
                                    if (ringEnv.OxygenOutRing >= 2)
                                        descriptor.NJRNRSGIUZYMQH = 1;
                                    if (ringEnv.OxygenOutRing >= 1)
                                        descriptor.VJBCBLLQDMITLJ = 1;
                                    if (ringEnv.OxygenOutRing >= 3)
                                        descriptor.UKDDNHZSXJUZPY = 1;
                                    if (otherRing.RingEnv.EtherOutRing >= 1 && ringEnv.OxygenOutRing >= 2)
                                        descriptor.XSFUUZAFOVXTLP = 1;
                                    if (ringEnv.OxygenOutRing >= 3 && otherRing.RingEnv.EtherOutRing >= 1)
                                        descriptor.SCBVMAGDJBWWEM = 1;
                                }
                            }

                            if (ringEnv.FOutRing >= 2 && ringEnv.OxygenOutRing >= 1 && ringEnv.NitrogenOutRing >= 1) {
                                if (RingtypeChecker(atom6, ringDictionary, RingFunctionType.Pyridine, out ring2ID)) {
                                    var ring2Env = ringDictionary[ring2ID].RingEnv;
                                    if (ring2Env.KetonOutRing >= 1)
                                        descriptor.MCMFWKPSXYARAC = 1;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case "C-C-C:C:C:C":
                    #region
                    if (atom3.IsInRing && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 && ringEnv.OxygenOutRing >= 1)
                            descriptor.DZGWFCGJZKJUFP = 1;
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && ringEnv.OxygenOutRing >= 2)
                            descriptor.JUUBCHWRXWPFFH = 1;
                    }
                    break;
                #endregion
                case "O-C-C-N-C=O":
                    #region
                    if (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Pyrrolidine, out targetRingID)) {
                        var insideAtomDict = ringDictionary[targetRingID].RingEnv.InsideAtomDictionary;
                        if (!insideAtomDict.ContainsKey(atom1.AtomID) &&
                            !insideAtomDict.ContainsKey(atom2.AtomID) &&
                            insideAtomDict.ContainsKey(atom3.AtomID) &&
                            insideAtomDict.ContainsKey(atom4.AtomID) &&
                            !insideAtomDict.ContainsKey(atom5.AtomID) &&
                            !insideAtomDict.ContainsKey(atom6.AtomID)) {
                            if (atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.DHDRGOURKDLAOT = 1;
                        }
                    }
                    break;
                #endregion
                case "O-C-C-C-O-P":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.OxygenCount >= 3)
                        descriptor.YTNVMHWNIJXNGO = 1;
                    if (atom6.AtomEnv.OxygenCount >= 4) {
                        descriptor.HYCSHFLKPSMPGO = 1;
                        if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing > 0 && !ringEnv.InsideAtomDictionary.ContainsKey(atom1.AtomID))
                                descriptor.BVOBPNSQIRMLCA = 1;
                            if (ringEnv.OxygenOutRing > 0 && ringEnv.CarbonOutRing > 0 && !ringEnv.InsideAtomDictionary.ContainsKey(atom2.AtomID))
                                descriptor.JPBCCNVSNVZSQL = 1;
                        }

                        if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                            descriptor.BZQFBWGGLXLEPQ = 1;
                    }
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom6.AtomEnv.OxygenCount >= 4
                        && RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.ThFran_CycloO1POCCC1, out targetRingID))
                        descriptor.AZZVQQAZMSIAKQ = 1;

                    break;
                #endregion
                case "O-C-C-C:C:N":
                    #region
                    if (RingsettypeChecker(atom4, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        descriptor.MBBOMCVGYCRMEA = 1;
                    }
                    break;
                #endregion
                case "N-C-C-C:C:N":
                    #region
                    if (RingsettypeChecker(atom4, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        descriptor.APJYDQYYACXCRM = 1;
                    }
                    break;
                #endregion
                case "O-C=N-C-C-C":
                    #region
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 2 && RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var tempBonds = atom4.ConnectedBonds.Where(n => n.BondID != bond3.BondID && n.BondID != bond4.BondID).ToList();
                        foreach (var tBond in tempBonds) {
                            if (tBond.ConnectedAtoms.Count(n => n.AtomString == "H") >= 1)
                                continue;
                            var tAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom4.AtomID).ToList()[0];
                            if (tAtom.AtomEnv.DoubleBond_CO_Count >= 1 && tAtom.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.NSTPXGARCQOSAU = 1;
                        }
                    }
                    break;
                #endregion
                case "C-C-C-N=C-O":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringProp = ringDictionary[targetRingID];
                        if (ringProp.RingEnv.OxygenOutRing > 0) {
                            var tempBonds = atom3.ConnectedBonds.Where(n => n.BondID != bond2.BondID && n.BondID != bond3.BondID).ToList();
                            foreach (var tBond in tempBonds) {
                                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "C") == 2) {
                                    var tAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom3.AtomID).ToList()[0];
                                    if (tAtom.AtomEnv.OxygenCount >= 2) {
                                        descriptor.ROUWPHMRHBMAFE = 1;
                                    }
                                }
                            }
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrole, out ring1ID) &&
                        RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out ring2ID)) {
                        bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 && bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1)
                            descriptor.RNEMLJPSSOJRHX = 1;
                    }
                    break;
                #endregion
                case "C-C-C-C:C:C": // Vestitol
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)
                        && (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)
                        || RingtypeChecker(atom2, ringDictionary, RingFunctionType.DihydroPyran, out ring2ID))
                        && RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                        if (ring1ID != ring3ID && !bond1.IsInRing) {
                            var ring1 = ringDictionary[ring1ID];
                            var ring2 = ringDictionary[ring2ID];
                            var ring3 = ringDictionary[ring3ID];

                            if (ring2.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {
                                if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                    descriptor.XRVFNNUXNVWYTI = 1;
                            }
                        }
                    }
                    break;
                #endregion
                case "C-C:C:C:C:C": //isoflavone
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)
                        && RingtypeChecker(atom2, ringDictionary, RingFunctionType.AromaticPyran, out ring2ID)
                        && RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                        if (ring1ID != ring3ID && !bond1.IsInRing) {
                            var ring1 = ringDictionary[ring1ID];
                            var ring2 = ringDictionary[ring2ID];
                            var ring3 = ringDictionary[ring3ID];

                            if (ring2.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {

                                if (atom3.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1) {

                                    //isoflavone
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.ZQSIJRDFPHDXIC = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.TZBJGXHYKVUXJN = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.XKHHKXCBFHUOHM = 1;
                                    if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.WUADCCWRTIWANL = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.EtherOutRing >= 2)
                                        descriptor.KJGPBYUQZLUKLL = 1;
                                    if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                        descriptor.VOOFPOMXNLNEOF = 1;
                                    if (ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.WMKOZARWBMFKAS = 1;
                                    if (ring1.RingEnv.EtherOutRing >= 1)
                                        descriptor.RIKPNWPEMPODJD = 1;
                                    if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.HKQYGTCOTHHOMP = 1;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case "C-C-O-P-O-P":
                    #region
                    if (atom4.AtomEnv.OxygenCount >= 3 && atom6.AtomEnv.OxygenCount >= 3
                        && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2) {
                            if (atom4.AtomEnv.OxygenCount >= 4 || atom6.AtomEnv.OxygenCount >= 4) {
                                descriptor.CVOOKAVYLTYIQE = 1;
                            }
                            if (atom4.AtomEnv.OxygenCount >= 4 && atom6.AtomEnv.OxygenCount >= 4)
                                descriptor.HNPYBPXXPMVWIY = 1;
                        }
                    }
                    break;
                #endregion
                case "N-C-O-C-C-O": //nucleodide
                    #region
                    var furanRingID = -1;
                    if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Tetrahydrofuran, out furanRingID)) { //furan

                        var furanRing = ringDictionary[furanRingID];
                        var branchedAtoms = getBranchAtom(atom6, bond5, "P");

                        //purine
                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out targetRingID)) {

                            var ringset = ringsetDictionary[targetRingID];
                            if (ringset.RingIDs.Count == 2 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out ring1ID)) {
                                ring2ID = ringset.RingIDs[0] == ring1ID ? ringset.RingIDs[1] : ringset.RingIDs[0];

                                var imidazoleRing = ringDictionary[ring1ID];
                                var pyrimidineRing = ringDictionary[ring2ID];

                                if (pyrimidineRing.RingEnv.NitrogenOutRing >= 3 && pyrimidineRing.RingEnv.KetonOutRing == 0) { //Adenine
                                    if (furanRing.RingEnv.OxygenOutRing >= 2) { //A
                                        descriptor.OIRDTQYFTABQOQ = 1;
                                        if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1) {
                                            descriptor.PLLPSKJIVDRMFI = 1;
                                            var outsideAtomsOfFuranRing = furanRing.RingEnv.OutsideAtomDictionary.Values;
                                            foreach (var oAtoms in outsideAtomsOfFuranRing) {
                                                foreach (var oAtom in oAtoms) {
                                                    if (oAtom.AtomEnv.SingleBond_PhosphorusCount >= 1) {
                                                        descriptor.XLIAWOGMZGYPCO = 1;
                                                        break;
                                                    }
                                                }
                                            }
                                        }
                                        if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                            descriptor.UDMBCSSLTHHNCD = 1;
                                    }
                                    else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dA
                                        if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                            descriptor.XKFCKNDXVMFENB = 1;
                                    }
                                }
                                else if (pyrimidineRing.RingEnv.NitrogenOutRing >= 3 && pyrimidineRing.RingEnv.KetonOutRing == 1) { //Guanine
                                    if (furanRing.RingEnv.OxygenOutRing >= 2) { //G
                                        if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                            descriptor.RQFCJASXJCIDSX = 1;
                                    }
                                    else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dG

                                    }
                                }
                                else if (pyrimidineRing.RingEnv.NitorogenInRing >= 2 && pyrimidineRing.RingEnv.KetonOutRing == 1) { //Guanine - NH2
                                    if (furanRing.RingEnv.OxygenOutRing >= 2) { //G-NH2
                                        if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                            descriptor.DFAUVYAUUHAORK = 1;
                                    }
                                    else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dG-NH2

                                    }
                                }
                            }
                        }

                        //Pyrimidine
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {

                            var pyrimidineRing = ringDictionary[targetRingID];
                            if (pyrimidineRing.RingEnv.KetonOutRing >= 1 && pyrimidineRing.RingEnv.OxygenOutRing >= 2 && pyrimidineRing.RingEnv.CarbonOutRing >= 2) { //Thymine
                                if (furanRing.RingEnv.OxygenOutRing >= 2) { //T

                                }
                                else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dT
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                        descriptor.HYZIWFJILYVTQR = 1;
                                }
                            }
                            else if (pyrimidineRing.RingEnv.KetonOutRing >= 2 && pyrimidineRing.RingEnv.CarbonOutRing == 1) { //Uridine

                                if (furanRing.RingEnv.OxygenOutRing >= 2) { //U
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                        descriptor.MWQLKVPHFZFVLS = 1;
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                        descriptor.DJJCXFVJDGTHFX = 1;
                                }
                                else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dU
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                        descriptor.SEOHEWZQAGEAGZ = 1;
                                }
                            }
                            else if (pyrimidineRing.RingEnv.KetonOutRing == 1 && pyrimidineRing.RingEnv.NitrogenOutRing >= 1) { //Cytosine
                                if (furanRing.RingEnv.OxygenOutRing >= 2) { //C
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                        descriptor.LPOBTJSHGYZVBV = 1;
                                }
                                else if (furanRing.RingEnv.OxygenOutRing >= 1) { //dC
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                        descriptor.KNLJEJQKLJVQQJ = 1;
                                }
                            }
                        }
                        else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Dihydropyridine, out targetRingID)) {
                            var dihydroPyridine = ringDictionary[targetRingID];
                            if (dihydroPyridine.RingEnv.Carbon_AmideOutRing >= 1) {
                                if (furanRing.RingEnv.OxygenOutRing >= 2) { //
                                    if (branchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1)
                                        descriptor.UXFYKGHOTJIZHF = 1;
                                }
                                else if (furanRing.RingEnv.OxygenOutRing >= 1) { //d

                                }
                            }
                        }
                    }



                    break;
                #endregion
                case "C-C-C-C-C-S":
                    #region
                    if (atom1.AtomEnv.SingleBond_FCount >= 2 && atom2.AtomEnv.SingleBond_FCount >= 2 && atom3.AtomEnv.SingleBond_FCount >= 2 &&
                        atom4.AtomEnv.SingleBond_FCount >= 2 && atom5.AtomEnv.SingleBond_FCount >= 2 && atom6.AtomEnv.OxygenCount >= 3)
                        descriptor.JEGVBRYEXSGSKC = 1;
                    break;
                #endregion
                case "N-C-C-C-C=O":
                    #region
                    descriptor.DZQLQEYLEYWJIB = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.FVRHIJPPPSYMGI = 1;
                    break;
                #endregion
                case "O-C-C-N=C-O":
                    #region
                    bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                    if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 3) >= 1)
                        descriptor.QBYYLBWFBPAOKU = 1;
                    break;
                #endregion
                case "N-C-C-N-C=O":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Morpholine, out targetRingID)) {
                        descriptor.MMVGWKHDLPGACU = 1;
                    }
                    break;
                #endregion
                case "C-C-C-C-N-C":
                    #region
                    descriptor.QCOGKXLOEWLIDC = 1;
                    if (atom5.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.DJEQZVQFEPKLOY = 1;
                    //if (bond2.IsSharedBondInRings && bond3.IsSharedBondInRings && bond4.IsSharedBondInRings) {
                    //    if (RingtypeChecker(bond1, ringDictionary, RingFunctionType.Piperidine, out ring1ID) && RingtypeChecker(bond5, ringDictionary, RingFunctionType.Piperidine, out ring2ID)) {
                    //        if (ring1ID != ring2ID) {
                    //            var ring1 = ringDictionary[ring1ID];
                    //            var ring2 = ringDictionary[ring2ID];

                    //            if (ring1.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 && ring1.ConnectedBonds.Count(n => n.BondID == bond3.BondID) == 1 &&
                    //                ring1.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && ring2.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 &&
                    //                ring2.ConnectedBonds.Count(n => n.BondID == bond3.BondID) == 1 && ring2.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {
                    //                descriptor.SBYHFKPVCBCYGV = 1;
                    //                if (ring1.RingEnv.Carbon_OxygenOutRing >= 1 && ring2.RingEnv.Carbon_AlkeneOutRing >= 1)
                    //                    descriptor.GAFZBOMPQVRGKU = 1;
                    //                if (ring1.RingEnv.Carbon_AlkeneOutRing >= 1)
                    //                    descriptor.STMXRYQUMAMCAF = 1;
                    //            }
                    //        }
                    //    }
                    //}

                    break;
                #endregion
                case "O-C-C:C:C:C":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && bond3.IsInRing && bond3.IsSharedBondInRings &&
                        RingtypeChecker(bond1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) && RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring2Env.EtherOutRing >= 2)
                            descriptor.ORFFGRQMMWVHIB = 1;
                    }
                    break;
                #endregion
                case "C-C-C-O-C=O":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && atom3.AtomEnv.SingleBond_CarbonCount >= 2) {
                            var branchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (branchedAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1 &&
                                branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.YILOWADLNZNSNC = 1;
                        }
                    }
                    break;
                #endregion
                case "C-C-C-N-C=O":
                    #region
                    descriptor.SUUDTPGCUKBECW = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.ClOutRing >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 2) {
                            var branchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (branchedAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1 &&
                                branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) {
                                descriptor.JSKBTAJNHHGHSV = 1;
                                if (atom5.AtomEnv.SingleBond_CarbonCount >= 1) {
                                    descriptor.HLORMWAKCPDIPU = 1;
                                    var branchedAtoms2 = getBranchAtom(atom5, bond4, bond5, "C");
                                    if (branchedAtoms2.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                        descriptor.UUVDEUSHGUNZAH = 1;
                                }
                            }
                        }
                    }
                    break;
                #endregion
                case "C-O-C-C-C=C":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.VDNOKVNTQJWRAC = 1;
                    }
                    break;
                #endregion
                case "O-C:C:C:C:C":
                    #region
                    if (atom1.IsInRing && bond2.IsSharedBondInRings && bond4.IsSharedBondInRings) {
                        if ((RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) || RingtypeChecker(atom1, ringDictionary, RingFunctionType.DihydroPyran, out ring1ID)) &&
                            RingtypeChecker(bond3, ringDictionary, RingFunctionType.Benzene, out ring2ID) &&
                            RingtypeChecker(atom6, ringDictionary, RingFunctionType.AromaticPyran, out ring3ID)) {
                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            var ring3Env = ringDictionary[ring3ID].RingEnv;
                            if (ring1Env.CarbonOutRing >= 4 && ring2Env.OxygenOutRing >= 3 && ring3Env.KetonOutRing >= 1 && ring3Env.CarbonOutRing >= 3)
                                descriptor.VEKXYPCVZLIIMU = 1;
                        }
                    }
                    break;
                #endregion
                case "C-S-C-C-C=C":
                    #region
                    if (atom3.AtomEnv.DoubleBond_CN_Count >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 1)
                            descriptor.MXIYPGNETXDPDR = 1;
                    }
                    break;
                #endregion
                case "C=N-C-C-O-P":
                    #region
                    if (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom1.AtomEnv.SingleBond_CarbonCount >= 1 &&
                            atom6.AtomEnv.OxygenCount >= 3 && ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 1) {
                            descriptor.FWGOJLIMNNWOLX = 1;
                        }
                    }
                    break;
                #endregion
                case "C-O-C-C-O-C":
                    #region
                    if (!atom2.IsInRing && !atom5.IsInRing &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(bond3, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID) &&
                        RingtypeChecker(atom6, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1 && ring2Env.OxygenOutRing >= 4 &&
                            ring2Env.Carbon_OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 4 && ring3Env.CarbonOutRing >= 1)
                            descriptor.IZHLNVOWIUSANQ = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 4 && ring2Env.Carbon_OxygenOutRing >= 1 &&
                                ring3Env.OxygenOutRing >= 4 && ring3Env.CarbonOutRing >= 1) {
                            descriptor.RCWQRFAOSIMYAG = 1;

                            if (ring1Env.Carbon_AlkaneOutRing >= 1)
                                descriptor.HAJRFFOSWOEITM = 1;

                            if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out targetRingID)) {
                                var otherPyranEnv = ringDictionary[ringsetDictionary[targetRingID].RingIDs.Where(n => n != ring1ID).ToList()[0]].RingEnv;
                                if (otherPyranEnv.KetonOutRing >= 1)
                                    descriptor.HORDMQGYQIDHMI = 1;
                            }
                        }

                    }
                    break;
                #endregion
                case "C-C-C-C-O-C":
                    #region
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.OQAGVSWESNCJJT = 1;
                    break;
                #endregion
                case "C-N-C-C:C:C":
                    #region
                    if (bond4.IsSharedBondInRings &&
                        RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Hydropiperidine, out ring2ID) ||
                        RingtypeChecker(bond3, ringDictionary, RingFunctionType.Piperidine, out ring2ID))) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && ring1Env.EtherOutRing >= 1) {
                            descriptor.FXCIEQJPMSJEII = 1;
                            if (ring1Env.EtherOutRing >= 2)
                                descriptor.TXPPKWZEHFNZOE = 1;
                        }

                        if (bond2.IsSharedBondInRings &&
                            RingtypeChecker(atom1, ringDictionary, RingFunctionType.Piperidine, out ring3ID)) {
                            var ring3Env = ringDictionary[ring3ID].RingEnv;
                            if (ring1Env.EtherOutRing >= 2 && ring3Env.SingleBond_CarbonOutRing >= 3 && ring3Env.Carbon_AlkaneOutRing >= 2)
                                descriptor.NJVCBRZYGVLGQI = 1;
                            if (ring1Env.EtherOutRing >= 2 && ring3Env.SingleBond_CarbonOutRing >= 4 && ring3Env.Carbon_AlkaneOutRing >= 2)
                                descriptor.WWRALUWFOYYMKZ = 1;
                        }
                    }
                    break;
                #endregion
                case "O-C-C-O-P-O":
                    #region
                    if (atom5.AtomEnv.OxygenCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.WHRQRNQSZWMIMU = 1;
                    break;
                #endregion
                case "C-C=C-C-C-O":
                    #region
                    descriptor.FSUXYWPILZJGCC = 1;
                    break;
                #endregion
                case "C-O-C=C-C=O":
                    #region
                    descriptor.XJQSLJZYZFSHAI = 1;
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 1) {
                        bAtoms = getBranchAtom(atom4, bond3, bond4, "C");
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1)
                            descriptor.VKMRVNVYNKTCBS = 1;
                    }
                    break;
                #endregion
                case "C-C-O-C-C-O":
                    #region
                    descriptor.ZNQVEEAIQZEUHB = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3)
                            descriptor.DSOFUIOMIHBCLC = 1;
                    }
                    break;
                #endregion
                case "C-C-N-C-C-C":
                    #region 
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.SMBYUOXUISCLCF = 1;
                    break;
                #endregion
                case "C-C-C-O-C-O":
                    #region
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.QONBKISCDWCHKF = 1;
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.QCLDEGYWPRUFHE = 1;
                    }
                    break;
                #endregion
                case "C-O-C-C=C-O":
                    #region MyRegion
                    if (atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.FVYNPFNRUNVROH = 1;
                    #endregion
                    break;
                case "O-C-N-C-C-O":
                    #region 
                    if (atom2.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.CLAHOZSYMRNIPY = 1;
                    break;
                #endregion
                case "O-C-C-C-C-S":
                    #region 
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.FFFHZYDWPBMWHY = 1;
                    break;
                #endregion
                case "C-S-C=N-O-S":
                    if (atom3.AtomEnv.SingleBond_CarbonCount >= 1 && atom6.AtomEnv.OxygenCount >= 3)
                        descriptor.KQUUQYKFKUSHHJ = 1;
                    break;
                case "C-O-C-O-C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && ringEnv.Carbon_KetoneOutRing >= 1 && ringEnv.Carbon_AlkaneOutRing >= 1)
                            descriptor.VQJVACMKXDMZFT = 1;
                    }
                    break;
                case "C-C:C-O-C-O":
                    if (!bond1.IsInRing && RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromenylium, out targetRingID) &&
                           RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrylium, out ring1ID) &&
                           RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {

                        var Pyrylium = ringDictionary[ring1ID];
                        var benzene = ringDictionary[ring2ID];

                        if (ringsetDictionary[targetRingID].RingIDs.Count == 2) {
                            var benzen2ID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != Pyrylium.RingID).ToList()[0];
                            var benzene2 = ringDictionary[benzen2ID];

                            if (benzene.RingEnv.OxygenOutRing >= 2 &&
                                benzene2.RingEnv.OxygenOutRing >= 2) {
                                descriptor.AQRDRMRSQVOKBY = 1;
                                bAtoms = getBranchAtom(atom6, bond5, "C");
                                if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                    descriptor.KSKDCCGYHFTZTC = 1;


                                if (RingtypeChecker(atom6, ringDictionary, RingFunctionType.Tetrahydropyran, out ring4ID)) {
                                    var pyran = ringDictionary[ring4ID].RingEnv;
                                    if (pyran.OxygenOutRing >= 4 && pyran.Carbon_OxygenOutRing >= 1)
                                        descriptor.RKWHWFONKJEUEF = 1;
                                }

                            }
                        }
                    }
                    break;
                case "C=O-C=C-O-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID) ||
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexadiene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (atom2.AtomCharge == 1 && ringEnv.OxygenOutRing >= 3) {
                            bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            foreach (var bAtom in bAtoms) {
                                if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                                    var ring2Env = ringDictionary[ring2ID].RingEnv;
                                    if (ring2Env.EtherOutRing >= 1 && ring2Env.OxygenOutRing >= 1)
                                        descriptor.OJBWFYJOLJOTPP = 1;
                                }
                            }
                        }
                    }
                    break;
                case "C-C:O:C:C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out targetRingID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.AromaticPyran, out ring2ID) &&
                        RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out ring3ID) &&
                        RingtypeChecker(atom6, ringDictionary, RingFunctionType.Tetrahydropyran, out ring4ID)) {

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;
                        var ring4Env = ringDictionary[ring4ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 3 &&
                            ring4Env.OxygenOutRing >= 3 && ring4Env.Carbon_OxygenOutRing >= 1)
                            descriptor.SGEWCQFRYRRZDC = 1;
                    }
                        break;
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, 
            AtomProperty atom5, AtomProperty atom6, AtomProperty atom7,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, 
            BondProperty bond5, BondProperty bond6,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            //var hydrogenCount = atom1.AtomEnv.HydrogenCount + atom2.AtomEnv.HydrogenCount
            //  + atom3.AtomEnv.HydrogenCount + atom4.AtomEnv.HydrogenCount
            //  + atom5.AtomEnv.HydrogenCount + atom6.AtomEnv.HydroxyCount + atom7.AtomEnv.HydroxyCount;
           
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var ring5ID = -1;
            var targetRingID = -1;
            var bAtoms = new List<AtomProperty>();
            var atom1BranchedAtoms = new List<AtomProperty>();
            var atom2BranchedAtoms = new List<AtomProperty>();
            var atom3BranchedAtoms = new List<AtomProperty>();
            var atom4BranchedAtoms = new List<AtomProperty>();
            var atom5BranchedAtoms = new List<AtomProperty>();
            var atom6BranchedAtoms = new List<AtomProperty>();
            var atom7BranchedAtoms = new List<AtomProperty>();

            switch (connectString) {
                case "C-C-C-C-C-C-C": //gibberellin, steroid
                    #region
                    descriptor.IMNFDUFMRHMDMM = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.TVMXDCGIABBOFY = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                        descriptor.JVSWJIKNEAIKJW = 1;
                    if (atom1.AtomEnv.DoubleBond_CC_Count >= 1)
                        descriptor.KWKAKUADMBZCLK = 1;
                    if (atom7.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.FXHGMKSSBGDXIY = 1;
                    if (atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.CATSNJVOTSVZJV = 1;
                    if (atom5.AtomEnv.SingleBond_CarbonCount >= 2 && atom7.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.NUJGJRNETVAIRJ = 1;
                    if (atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.DoubleBond_CC_Count >= 1)
                        descriptor.VSMOENVRRABVKN = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.CETWDUZRCINIHU = 1;
                    if (atom1.AtomEnv.FCount >= 3 && atom2.AtomEnv.FCount >= 2 &&
                        atom3.AtomEnv.FCount >= 2 && atom4.AtomEnv.FCount >= 2 &&
                        atom5.AtomEnv.FCount >= 2 && atom6.AtomEnv.FCount >= 2 && atom7.AtomEnv.FCount >= 2)
                        descriptor.HBZVXKDQRIQMCW = 1;

                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) 
                        && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Cyclopentane, out ring2ID)
                        && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring3ID)) { //gibberellin substructure
                        #region
                            var ring1 = ringDictionary[ring1ID];
                            var ring2 = ringDictionary[ring2ID];
                            var ring3 = ringDictionary[ring3ID];

                            if (ring1.ConnectedBonds.Count(n =>n.BondID == bond3.BondID) > 0 && ring2.ConnectedBonds.Count(n => n.BondID == bond3.BondID )> 0 &&
                                ring2.ConnectedBonds.Count(n => n.BondID == bond5.BondID) > 0 && ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) > 0) {
                                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 && 
                                        ring3.RingEnv.KetonOutRing >= 1 && ring3.RingEnv.CarbonOutRing >= 3)
                                        descriptor.WCXGWJNKCOBVDB = 1; //gibberellin substructure
                            }

                            if (!bond4.IsInRing && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Cyclohexene, out ring4ID)) {
                                var ring4 = ringDictionary[ring4ID];

                                if (ring1.ConnectedBonds.Count(n => n.BondID == bond1.BondID) == 1 && ring2.ConnectedBonds.Count(n => n.BondID == bond1.BondID) == 1 &&
                                    ring1.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 && ring2.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 &&
                                    ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 && ring4.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 &&
                                    ring3.ConnectedBonds.Count(n => n.BondID == bond6.BondID) == 1 && ring4.ConnectedBonds.Count(n => n.BondID == bond6.BondID) == 1) {
                                    if (ring2.RingEnv.DoubleBond_CarbonOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.KetonOutRing >= 1 &&
                                        ring4.RingEnv.OxygenOutRing >= 2) {
                                            descriptor.OUGXDFQHDCEVIE = 1;
                                    }
                                }
                            }
                        #endregion
                    }
                    if (atom6.AtomEnv.SingleBond_CarbonCount >= 3 
                      && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID)
                      && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out ring2ID)
                      && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Cyclopentane, out ring3ID)) { //gibberellin
                        #region
                        if (ring2ID != ring3ID) {
                              var ring1 = ringDictionary[ring1ID];
                              var ring2 = ringDictionary[ring2ID];
                              var ring3 = ringDictionary[ring3ID];

                              if (ring1.ConnectedAtoms.Count(n => n.AtomID == atom1.AtomID) == 1 
                                  && ring2.ConnectedAtoms.Count(n => n.AtomID == atom1.AtomID) == 1) {
                                  
                                  if (ring1.ConnectedAtoms.Count(n => n.AtomID == atom2.AtomID) == 1 
                                      && ring2.ConnectedAtoms.Count(n => n.AtomID == atom2.AtomID) == 1 
                                      && ring3.ConnectedAtoms.Count(n => n.AtomID == atom2.AtomID) == 1) {

                                          if (ring1.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond2.BondID) == 1) {
                                              
                                              if (ring2.RingEnv.DoubleBond_CarbonOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 && ring1.RingEnv.OxygenOutRing >= 1)
                                                  descriptor.NXFLGXNCIBUWJR = 1;
                                              
                                              if (RingtypeChecker(atom6, ringDictionary, RingFunctionType.Cyclohexene, out ring4ID)) {
                                                  var ring4 = ringDictionary[ring4ID];
                                                  if (ring3.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && 
                                                      ring4.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {


                                                          if (ring2.RingEnv.DoubleBond_CarbonOutRing >= 1 && ring4.RingEnv.OxygenOutRing >= 1 && ring4.RingEnv.CarbonOutRing >= 3) {
                                                              descriptor.IOSBERPLDLSAIE = 1;
                                                              if (ring2.RingEnv.OxygenOutRing >= 1) {
                                                                  descriptor.GZNTYUKHRRFIEK = 1;
                                                                  if (ring4.RingEnv.Carbon_KetoneOutRing >= 1)
                                                                      descriptor.LRVALLDMDUHEPD = 1;
                                                              }
                                                          }


                                                          if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID)) {


                                                              var ring5 = ringDictionary[ring5ID];

                                                              if (ring3.ConnectedAtoms.Count(n => n.AtomID == atom5.AtomID) == 1
                                                                  && ring4.ConnectedAtoms.Count(n => n.AtomID == atom5.AtomID) == 1
                                                                  && ring5.ConnectedAtoms.Count(n => n.AtomID == atom5.AtomID) == 1) {

                                                                      if (ring4.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 &&
                                                                          ring5.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 &&
                                                                          ring5.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {


                                                                           if (ring2.RingEnv.DoubleBond_CarbonOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 &&
                                                                               ring4.RingEnv.OxygenOutRing >= 2 && ring4.RingEnv.CarbonOutRing >= 4 &&
                                                                               ring5.RingEnv.KetonOutRing >= 1 && ring5.RingEnv.CarbonOutRing >= 5) {

                                                                                   descriptor.UAQXHIYSKKLMPW = 1;
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
                        #endregion
                    }

                    break;
                    #endregion
                case "C-C-C-C-C=C-C":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.KPSZWAJWFMFMFF = 1;
                    break;
                    #endregion
                case "C-C-C-C=C-C-C":
                    #region
                    descriptor.WZHKDGJSXCTSCK = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom2.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.KPADFPAILITQBG = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.RXFZCBZCGBDPDT = 1;
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom7.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.BQOAKIRTXKZTKD = 1;
                    break;
                    #endregion
                case "C-C-C-C-C-C=C":
                    #region
                    descriptor.ZGEGCLOFRBLKSE = 1;
                    if (atom5.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.PZKFYTOLVRCMOA = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.IICQZTQZQSBHBY = 1;
                    break;
                    #endregion
                case "C=C-C-C-C-C=O":
                    #region
                    descriptor.USLRUYZDOLMIRJ = 1;
                    break;
                    #endregion
                case "C=C-C=C-C=C-C":
                    #region
                    if (atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.YGDXNKVVWAQSEN = 1;
                    break;
                    #endregion
                case "C-C-C=C-C-C=C":
                    #region
                    descriptor.FMAMSYPJXSEYSW = 1;
                    if (atom1.AtomEnv.DoubleBond_CC_Count >= 1)
                        descriptor.BUAQSUJYSBQTHA = 1;
                    if (atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.GDDAJHJRAKOILH = 1;
                    if (atom1.AtomEnv.DoubleBond_CC_Count >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.NABXNTPHRFMODH = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.NMFOAHIHPAPYJJ = 1;
                    break;
                    #endregion
                case "C-C=C-C-C=C-C":
                    #region
                    if (atom7.AtomEnv.DoubleBond_CC_Count >= 1) descriptor.PKHBEGZTQNOZLP = 1;
                    break;
                    #endregion
                case "C-C-C-C-C-C=O":
                    #region
                    descriptor.JARKCYVAAOWBJS = 1;
                    break;
                    #endregion
                case "C-C-C=C-C-C-O":
                    #region
                    descriptor.UFLHIIWVXFIJGU = 1;
                    break;
                    #endregion
                case "C-C=C-C-C-C-C":
                    #region
                    if (atom7.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.UDOAKURRCZMWOJ = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom7.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.ZUSUVEKHEZURSD = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.NBFKNCBRFJKDDR = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.LZHHQGKEJNRBAZ = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 2 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.OHEFFKYYKJVVOX = 1;
                    break;

                    #endregion
                case "C-C-C-C-C-C-O":
                    #region
                    descriptor.ZSIAUFGUXNUGDI = 1;
                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 && atom5.AtomEnv.SingleBond_NitrogenCount >= 1 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.KDXKERNSBIXSRK = 1;
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.RUIACMUVCHMOMF = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.SKCKOFZKJLZSFA = 1;
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 1 && atom2.AtomEnv.DoubleBond_CO_Count >= 1 &&
                        atom6.AtomEnv.SingleBond_CarbonCount >= 2) {
                            bAtoms = getBranchAtom(atom7, bond6, "C");
                            if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1)
                                descriptor.HYHMOIFJBNVOTK = 1;
                    }
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.MHPUGCYGQWGLJL = 1;
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && 
                        atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.LWWIYCLYWKAKRR = 1;
                    break;
                    #endregion
                case "C-C-C-C-C-C-N":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom2.AtomEnv.SingleBond_NitrogenCount >= 1) descriptor.YUZOKOFJOOANSW = 1;
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1) descriptor.SLXKOJJOQWFEFD = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.SingleBond_CarbonCount >= 3 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.IVAHELAXKRVMQR = 1;
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.JQASYHORSFXWGI = 1;
                    
                    break;
                    #endregion
                case "C-C-C-C-N-C=O":
                    #region
                    if (atom3.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.PMQMMDVXYSSGCD = 1;
                    break;
                    #endregion
                case "N-C-C-C-C-C-N":
                    #region
                    descriptor.VHRGRCVQAFMJIZ = 1;
                    break;
                    #endregion
                case "N-C-C-C-C-N-C":
                    #region
                    if (atom7.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.JAEJQOUXXMFTJU = 1;
                    break;
                    #endregion
                case "C-C-C-C-C-N-C":
                    #region
                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 && atom7.AtomEnv.DoubleBond_CO_Count >= 1) 
                        descriptor.DCDFYDJCNWNFFZ = 1;
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom5.AtomEnv.SingleBond_CarbonCount >= 2 && atom7.AtomEnv.DoubleBond_CO_Count >= 1 &&
                        atom7.AtomEnv.SingleBond_CarbonCount >= 1) {
                            bAtoms = getBranchAtom(atom5, bond4, bond5, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.DWAICOVNOFPYLS = 1;
                    }
                    break;
                    #endregion
                case "O-C-C-C-C-C-O":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.JFCQEDHGNNZCLN = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.DoubleBond_CN_Count >= 1) descriptor.GTFMAONWNTUZEW = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom6.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.WHUUTDBJXJRKMK = 1;
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.HEBKCHPVOIAQTA = 1;
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.ZDAWZDFBPUUDAY = 1;
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1 && atom6.AtomEnv.DoubleBond_CN_Count >= 1)
                        descriptor.ZDXPYRJPNDTMRX = 1;
                    break;
                    #endregion
                case "N-C-C-C-C-C-O":
                    #region
                    if (atom5.AtomEnv.SingleBond_NitrogenCount >= 1 && atom6.AtomEnv.DoubleBond_CO_Count >= 1) descriptor.AHLPHDHHMVZTML = 1;
                    break;
                    #endregion
                case "C-N-C-C-O-C-C":
                    #region
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.CQRNPUSDYPNGDN = 1;
                    break;
                    #endregion
                case "C-C-C-C:N:C=O":
                    #region
                    if (atom4.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyridine, out targetRingID)) {
                        descriptor.CDEBRYPYJRWMDC = 1;
                    }
                    break;
                    #endregion
                case "C-C:C:C:C:C:C": //flavone, flavonol, anthocyanidin, etc
                    #region
                    //flavone, flavonol
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)
                        && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                        if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.AromaticPyran, out ring2ID)) {
                            
                            if (ring1ID != ring3ID && !bond1.IsInRing) {
                                var ring1 = ringDictionary[ring1ID];
                                var ring2 = ringDictionary[ring2ID];
                                var ring3 = ringDictionary[ring3ID];

                                if (ring2.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1) {

                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.JSRZRMCBOQDRGK = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.UYUZDOBYKBYLLE = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.ARVXNSRONWYRIU = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.FVMMGLAHGMDCLW = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.NHBXJBAYARAWKJ = 1;

                                    if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) {

                                        //flavone
                                        descriptor.VHBFFQKBGNRLFZ = 1;
                                       
                                        
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.KZNIFHPLKGYRTM = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                            descriptor.OKRNDQLCMXUCGG = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.CarbonOutRing >= 2)
                                            descriptor.YXIWGOWCOPLSJZ = 1;
                                        if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.DANYIYRPLHHOCZ = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.IQPNAANSBPBGFQ = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.Carbon_OxygenOutRing >= 1)
                                            descriptor.HLAMYEBSNZBRER = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.CarbonOutRing >= 2)
                                            descriptor.NFSDHORLNPYHIE = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.EtherOutRing >= 2 &&
                                            ring3.RingEnv.Carbon_AlkaneOutRing >= 2)
                                            descriptor.VYYGXLYTVAMSDT = 1;
                                       
                                        if (ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.RTIXKCRFFJGDFG = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.CarbonOutRing >= 2)
                                            descriptor.ZLGRXDWWYMFIGI = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.Carbon_AlkaneOutRing >= 1)
                                            descriptor.XBOSFRCNQGTSLQ = 1;
                                        if (ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.COCYGNDCWFKTMF = 1;
                                        
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.EtherOutRing >= 2)
                                            descriptor.JPMYFOBNRRGFNO = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.SCZVLDHREVKTSH = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.EtherOutRing >= 2)
                                            descriptor.JDMXMMBASFOTIF = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 3 && ring1.RingEnv.EtherOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.HRGUSFBJBOKSML = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 && 
                                            ring3.RingEnv.EtherOutRing >= 2 && ring3.RingEnv.SingleBond_CarbonOutRing >= 2)
                                            descriptor.VQCXCCMCKDSXMQ = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3 &&
                                            ring3.RingEnv.Carbon_AlkaneOutRing >= 2)
                                            descriptor.SXTAZFUZGSUXKO = 1;
                                        if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && 
                                            ring3.RingEnv.OxygenOutRing >= 3)
                                            descriptor.MBNGWHIJMBWFHU = 1;


                                        if (atom3.AtomEnv.SingleBond_OxygenCount >= 1) {

                                            //flavonol
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.ABDQTIKKAYGHNV = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 4)
                                                descriptor.LFPHMXIOQBBTSS = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.REFJWTPEDVJJIY = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.OGQSUSFDBWGFFJ = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 4)
                                                descriptor.DAEBTLQZOWXOBW = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.IZQSVPBOUDKVDZ = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.EtherOutRing >= 3 && ring3.RingEnv.OxygenOutRing >= 4)
                                                descriptor.KWMAWXWUGIEVDG = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && 
                                                ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.DDNPCXHBFYJXBJ = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.EtherOutRing >= 1 &&
                                                ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.KIGVXRGRNLQNNI = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 3 && ring2.RingEnv.EtherOutRing >= 1 &&
                                                ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.ZUBFWGOFMRYWNS = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 3 && ring1.RingEnv.EtherOutRing >= 1 && 
                                                ring2.RingEnv.EtherOutRing >= 1 &&
                                                ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.FPFLMCPZDZURSF = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 3 && ring1.RingEnv.EtherOutRing >= 2 &&
                                                ring2.RingEnv.EtherOutRing >= 1 &&
                                                ring3.RingEnv.OxygenOutRing >= 4 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.WTTGIVJDHDPLCL = 1;
                                            if (ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.VCCRNZQBSJXYJD = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                                descriptor.ZLNYYIOAMRCRGD = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.VJJZJBUCDWKPLC = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.WEPBGSIAWZTEJR = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.IKMDFBPHZNJCSN = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 2)
                                                descriptor.XHEFDIBZLJXQHF = 1;
                                            if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                                descriptor.SQFSKOYWJBQGKQ = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && 
                                                ring3.RingEnv.OxygenOutRing >= 3 && ring3.RingEnv.EtherOutRing >= 2)
                                                descriptor.BJBUTJQYZDYRMJ = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.EtherOutRing >= 1 && 
                                                ring3.RingEnv.EtherOutRing >= 3 && ring3.RingEnv.OxygenOutRing >= 4)
                                                descriptor.YSXFFLGRZJWNFM = 1;
                                            if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && 
                                                ring2.RingEnv.EtherOutRing >= 1 &&
                                                ring3.RingEnv.EtherOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 4)
                                                descriptor.XUWTZJRCCPNNJR = 1;
                                        }
                                    }
                                }
                            }
                        }
                        else if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrylium, out ring2ID)) {
                            if (ring1ID != ring3ID && !bond1.IsInRing) {
                                var ring1 = ringDictionary[ring1ID];
                                var ring2 = ringDictionary[ring2ID];
                                var ring3 = ringDictionary[ring3ID];

                                if (ring2.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1) {

                                    // anthocyanidin
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.VEVZSMAEJFVWIL = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 &&
                                        ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.XFDQJKDGGOEYPI = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 3 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.JKHRCGUTYDNCLE = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 3 && ring1.RingEnv.EtherOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 &&
                                        ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.AFOLOMGWVXKIQL = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 3 && ring1.RingEnv.EtherOutRing >= 2 && ring2.RingEnv.OxygenOutRing >= 1 &&
                                        ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.KZMACGJDUUWFCH = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.XVFMGWDSJLBXDZ = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.QLWSWRWMXXKOBO = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.ZKMZBAABQFUXFE = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.GDNIGMNXEKGFIP = 1;
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C=N-C-C-C-C-C":
                    #region
                    if (atom1.AtomEnv.NitrogenCount >= 3 && atom7.AtomEnv.DoubleBond_CO_Count >= 1) {
                        descriptor.GCQWVYLQXJQXQL = 1;
                        if (atom6.AtomEnv.SingleBond_NitrogenCount >= 1) {
                            descriptor.QJYRUYURLPTHLR = 1;
                            if (atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.ODKSFYDXXFIFQN = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C=C-C:C:C:C":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 &&
                        atom4.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.CJXMVKYNVIGQBS = 1;
                        if (ringEnv.OxygenOutRing >= 2)
                            descriptor.AXMVYSVVTMKQSL = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-C-C-N=C":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.NitrogenCount >= 3)
                        descriptor.UKUBCVAQGIZRHL = 1;
                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_CarbonCount >= 2) {
                        var tempBonds = atom5.ConnectedBonds.Where(n => n.BondID != bond4.BondID && n.BondID != bond5.BondID).ToList();
                        foreach (var tBond in tempBonds) {
                            if (tBond.ConnectedAtoms.Count(n => n.AtomString == "H") >= 1) continue;
                            var tAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom5.AtomID).ToList()[0];
                            if (tAtom.AtomEnv.DoubleBond_CO_Count >= 1 && tAtom.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.LIEIRDDOGFQXEH = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-C-N=C-O":
                    #region
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 2) {
                        bAtoms = getBranchAtom(atom4, bond3, bond4, "C");
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1 && n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                            if (atom2.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.HFBHOAHFRNLZGN = 1;
                            if (atom3.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.IONXXIKCTQHZNC = 1;
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom1.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.ADZLWSMFHHHOBV = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-N=C-C-C-S":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.FSCGLKWYHHSLST = 1;
                    break;
                    #endregion
                case "C-C-C:C:C:C-O":
                    #region
                    if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.Pyrrole, out targetRingID) && RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out targetRingID))
                        descriptor.JYVGHERHVBFYJB = 1;
                    break;
                    #endregion
                case "O=C:N:C-C-C-C":
                    #region


                    if (RingtypeChecker(bond2, ringDictionary, RingFunctionType.Pyridine, out ring1ID) && RingtypeChecker(bond6, ringDictionary, RingFunctionType.Piperidine, out ring2ID)) {
                        var ring1Prop = ringDictionary[ring1ID];
                        var ring2Prop = ringDictionary[ring2ID];

                        if (ring1Prop.ConnectedBonds.Count(n => n.BondID == bond3.BondID) == 1 && ring2Prop.ConnectedBonds.Count(n => n.BondID == bond3.BondID) == 1) {
                            var ring1AtomDict = ring1Prop.RingEnv.InsideAtomDictionary;
                            var ring2AtomDict = ring2Prop.RingEnv.InsideAtomDictionary;

                            if (ring1AtomDict.ContainsKey(atom2.AtomID) && ring1AtomDict.ContainsKey(atom3.AtomID) &&
                                ring1AtomDict.ContainsKey(atom4.AtomID) && ring2AtomDict.ContainsKey(atom3.AtomID) &&
                                ring2AtomDict.ContainsKey(atom4.AtomID) && ring2AtomDict.ContainsKey(atom5.AtomID) &&
                                ring2AtomDict.ContainsKey(atom6.AtomID) && ring2AtomDict.ContainsKey(atom7.AtomID)) {
                                    descriptor.COPYQTHBVCHZHI = 1;
                                    if (atom7.AtomEnv.SingleBond_CarbonCount >= 3) {
                                        descriptor.LWGGUSPGCQNJBN = 1;
                                        if (atom5.AtomEnv.SingleBond_CarbonCount >= 3)
                                            descriptor.DGSAGELMAWVXIF = 1;
                                    }
                            }
                        }
                    }
                    break;
                    #endregion
                case "O=C-C=C-C:C:C":
                    #region
                    if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        if (atom7.AtomEnv.EtherCount >= 1)
                            descriptor.XHYAQFCRAQUBTD = 1;
                    }
                    break;
                    #endregion
                case "C-C:C:N:C:C:C":
                    #region
                    if (RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID)) {
                        if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Pyrrole, out targetRingID) && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                            if (atom7.AtomEnv.EtherCount >= 1) {
                                descriptor.LSMGVUHULSBVAW = 1;
                                if (atom1.AtomEnv.SingleBond_CarbonCount >= 2)
                                    descriptor.CAAUHCJBMYDKBU = 1;
                            }
                        }
                    }
                    else if (bond2.IsSharedBondInRings && bond5.IsSharedBondInRings &&
                       RingtypeChecker(atom1, ringDictionary, RingFunctionType.Hydropiperidine, out ring1ID) &&
                       RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrrole, out ring2ID) &&
                       RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                        descriptor.CFTOTSJVQRFXOF = 1;
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;
                       
                        if (ring1Env.CarbonOutRing >= 2)
                            descriptor.JOFKCNJIUXPJAC = 1;
                        if (ring1Env.CarbonOutRing >= 3)
                            descriptor.VENAANZHUCMOGD = 1;

                        var ringSharedNitrogens = ringDictionary[ring1ID].ConnectedAtoms.Where(n => n.AtomString == "N" && n.IsInRing == true).ToList();
                        if (ringSharedNitrogens.Count == 1) {
                            var sharedIds = ringSharedNitrogens[0].SharedRingIDs;
                            if (sharedIds.Count >= 2) {
                                foreach (var sharedID in sharedIds.Where(n => n != ring1ID)) {
                                    var ring4 = ringDictionary[sharedID];
                                    if (ring4.RingFunctionType == RingFunctionType.Piperidine) {
                                        descriptor.OURDZMSSMGUMKR = 1;
                                        if (ring3Env.EtherOutRing >= 1)
                                            descriptor.OBVMPKMPQYQXKI = 1;
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-O-C:C:C":
                    #region
                    if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out targetRingID)) {
                        if (RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                            if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.OFLNADCNQAQFEO = 1;
                        }
                    }
                    break;
                    #endregion
                case "O=C-C=C-C-C-C":
                    #region
                    if (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Cyclohexene, out ring1ID) && RingtypeChecker(atom6, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID)) {
                        
                        var ring1Prop = ringDictionary[ring1ID];
                        var ring2Prop = ringDictionary[ring2ID];

                        if (ring1Prop.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && ring2Prop.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {
                            if (atom5.AtomEnv.SingleBond_CarbonCount >= 4)
                                descriptor.OHERZLWVBJCXOF = 1;
                        }
                    }
                    break;
                    #endregion
                case "O-C-C-C-C:C:C":
                    #region
                    if (atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_NitrogenCount >= 1) {
                        if (RingtypeChecker(atom5, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                            descriptor.COLNVLDHVKWLRT = 1;
                        }
                    }
                   
                    break;
                    #endregion
                case "O=C-C-C-C:C:N":
                    #region
                    if (RingsettypeChecker(atom5, ringDictionary, ringsetDictionary, RingsetFunctionType.Indole, out targetRingID))
                        descriptor.OBNFCFYSFICKKX = 1;
                    break;
                    #endregion
                case "C-C-C-N:C:N:C":
                    #region
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && 
                        atom5.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.NitrogenCount >= 2) {
                            if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID))
                                descriptor.LTTDCEYZEHQDGW = 1;
                    }
                    break;
                    #endregion
                case "C-C-C:C:C:C:C":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CC_Count >= 1 && bond5.IsSharedBondInRings && 
                        atom4.AtomEnv.OxygenCount >= 1 && atom7.AtomEnv.OxygenCount >= 1 &&
                        RingsettypeChecker(atom3, ringDictionary, ringsetDictionary, RingsetFunctionType.Naphthalene, out targetRingID)) {
                            descriptor.CJMBZJYOZMJNEQ = 1;
                    }
                    break;
                    #endregion
                case "C-C-O-C:C:C-O":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID))
                        descriptor.FOTVZLOJAIEAOY = 1;
                    break;
                    #endregion
                case "C-C=C-C-C-C=C":
                    #region
                    if (atom1.IsInRing && atom1.AtomEnv.OxygenCount >= 1 && atom1.AtomEnv.DoubleBond_CC_Count >= 1 &&
                        atom2.AtomEnv.DoubleBond_CC_Count >= 1 && atom2.AtomEnv.SingleBond_CarbonCount >= 2) {
                            if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {

                                var ringProp = ringDictionary[targetRingID];
                                var outsideAtomDict = ringProp.RingEnv.OutsideAtomDictionary;
                                var isKetonExist = false;
                                var isDoubleCarbon = false;
                                var isDoubleBondInRing = ringProp.RingEnv.DoublebondInRing == 1 ? true : false;
                                if (isDoubleBondInRing == false) break;
                                foreach (var oAtoms in outsideAtomDict) {
                                    if (oAtoms.Value.Count(n => n.AtomEnv.DoubleBond_CO_Count > 0) > 0) isKetonExist = true;
                                    if (oAtoms.Value.Count(n => n.AtomString == "C") > 1) isDoubleCarbon = true;
                                }
                                if (isDoubleCarbon && isKetonExist) {
                                    descriptor.FSMINEQXZDUUTP = 1;
                                }
                            }
                    }
                    break;
                    #endregion
                case "C-N-C-C-C-C-C":
                    #region
                    if (atom1.AtomEnv.NitrogenCount >= 3 && atom6.AtomEnv.SingleBond_NitrogenCount >= 1 && 
                        atom7.AtomEnv.DoubleBond_CO_Count >= 1 &&
                        atom7.AtomEnv.SingleBond_OxygenCount >= 1) {
                            var tempBonds = atom6.ConnectedBonds.Where(n => n.BondID != bond5.BondID && n.BondID != bond6.BondID).ToList();
                            foreach (var tBond in tempBonds) {
                                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "N") > 0) {
                                    var nAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom6.AtomID).ToList()[0];
                                    if (nAtom.AtomEnv.DoubleBond_CN_Count >= 1) {
                                        var temp2Bonds = nAtom.ConnectedBonds.Where(n => n.BondID != tBond.BondID && n.BondType == BondType.Double).ToList();
                                        foreach (var t2Bond in temp2Bonds) {
                                            if (t2Bond.ConnectedAtoms.Count(n => n.AtomString == "C") > 0) {
                                                var cAtom = t2Bond.ConnectedAtoms.Where(n => n.AtomID != nAtom.AtomID).ToList()[0];
                                                if (cAtom.AtomEnv.SingleBond_OxygenCount >= 1) {
                                                    descriptor.MBDCBOQSTZRKJP = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                    }
                    break;
                    #endregion
                case "C-C-C=C-C-C-C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringProp = ringDictionary[targetRingID];
                        if (ringProp.RingEnv.OxygenOutRing > 1) {
                            var tempBonds = atom7.ConnectedBonds.Where(n => n.BondID != bond6.BondID).ToList();
                            foreach (var tBond in tempBonds) {
                                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "C") == 2) {
                                    var tAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom7.AtomID).ToList()[0];
                                    if (tAtom.AtomEnv.DoubleBond_CO_Count >= 1) {
                                        descriptor.CALZPZVTQZEUQP = 1;
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C=C-C-C-C-O":
                    #region
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && RingtypeChecker(bond4, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.KetonOutRing >= 1 && ringEnv.DoubleBond_CarbonOutRing > 0) {
                            var tempBonds = atom7.ConnectedBonds.Where(n => n.BondID != bond6.BondID).ToList();
                            foreach (var tBond in tempBonds) {
                                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "H") == 0) {
                                    var tAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != atom7.AtomID).ToList()[0];
                                    if (tAtom.AtomEnv.DoubleBond_CO_Count >= 1) {
                                        descriptor.VZHRTYHDYQRFPX = 1;
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C:N-C-C-C-N-C":
                    #region
                    if (RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Citizin, out targetRingID)) {
                        descriptor.ANJTVLIZGCUXLD = 1;
                        if (atom7.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.PCYQRXYBKKZUSR = 1;
                    }
                    break;
                    #endregion
                case "C:C:N:C:N-C-O":
                    #region
                    if (atom4.AtomEnv.OxygenCount >= 1 && RingtypeChecker(atom5, ringDictionary, RingFunctionType.Pyrimidine, out ring1ID) 
                        && RingtypeChecker(bond6, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 2 && ring1Env.CarbonOutRing >= 1 && ring2Env.OxygenOutRing >= 1 && ring2Env.CarbonOutRing >= 1)
                            descriptor.UGUILUGCFSCUKR = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-C-C:C:C": // flavanone, flavan
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) 
                        && (RingtypeChecker(atom2, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID) || RingtypeChecker(atom2, ringDictionary, RingFunctionType.DihydroPyran, out ring2ID))
                        && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                            if (ring1ID != ring3ID && !bond1.IsInRing) {
                                var ring1 = ringDictionary[ring1ID];
                                var ring2 = ringDictionary[ring2ID];
                                var ring3 = ringDictionary[ring3ID];

                                if (ring2.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1) {
                                    
                                    //flavan
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.PFTAWBLQPZVEMU = 1;
                                    if (ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.HMUJXQRRKBLVOO = 1;
                                //if (ring1.RingEnv.EtherOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                //    descriptor.SBHXYTNGIZCORC = 1;

                                if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) {

                                    //flavanone
                                    descriptor.ZONYXWQDUYMKFB = 1;
                                    if (ring2.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.HPQDBWACKYGCNM = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.FTVWIRXFELQLPI = 1;
                                    if (ring2.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.SUYJZKRQHBQNCA = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.ZPVNWCMRCGXRJD = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring2.RingEnv.OxygenOutRing >= 2 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.CXQWRCVTCMQVQX = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 2 && ring1.RingEnv.EtherOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.AIONOLUJZLIMTK = 1;
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.FURUXTVZLHCCNA = 1;
                                    if (ring3.RingEnv.OxygenOutRing >= 3)
                                        descriptor.URFCJEUYXNAHFI = 1;
                                }
                                }
                            }
                    }
                    break;
                    #endregion
                case "C-C=C:C:C:C:C": // aurone
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)
                        && RingtypeChecker(atom3, ringDictionary, RingFunctionType.Furan, out ring2ID)
                        && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                        if (ring1ID != ring3ID && !bond1.IsInRing && !bond2.IsInRing) {
                            var ring1 = ringDictionary[ring1ID];
                            var ring2 = ringDictionary[ring2ID];
                            var ring3 = ringDictionary[ring3ID];

                            if (ring2.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond5.BondID) == 1) {

                                if (atom4.AtomEnv.DoubleBond_CO_Count >= 1) {
                                    //aurone
                                    if (ring1.RingEnv.OxygenOutRing >= 1 && ring3.RingEnv.OxygenOutRing >= 2)
                                        descriptor.KEZLDSPIRVZOKZ = 1;
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-C-C-O-C":
                    #region
                    if (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Oxonane, out ring1ID) 
                        && RingtypeChecker(bond6, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {

                        var ring1 = ringDictionary[ring1ID];
                        var ring2 = ringDictionary[ring2ID];

                        if (ring1.ConnectedAtoms.Count(n => n.AtomID == atom6.AtomID) == 1 && ring2.ConnectedAtoms.Count(n => n.AtomID == atom6.AtomID) == 1) {

                            if (ring1.RingEnv.OxygenOutRing >= 1 && ring1.RingEnv.CarbonOutRing >= 4 &&
                                ring1.RingEnv.Carbon_AlkeneOutRing >= 1 && ring2.RingEnv.KetonOutRing >= 1 && 
                                ring2.RingEnv.CarbonOutRing >= 3 && ring2.RingEnv.DoublebondInRing == 1) {
                                descriptor.AZOMRYVPGFQMGZ = 1;
                            }
                            if (ring1.RingEnv.Carbon_OxygenOutRing >= 1 && ring1.RingEnv.CarbonOutRing >= 4 && ring1.RingEnv.DoublebondInRing == 1 &&
                               ring1.RingEnv.Carbon_AlkeneOutRing >= 1 && ring2.RingEnv.KetonOutRing >= 1 &&
                               ring2.RingEnv.CarbonOutRing >= 3 && ring2.RingEnv.DoublebondInRing == 1) {
                                descriptor.GHRJJPYBLOLTEO = 1;
                            }


                            if (RingtypeChecker(bond1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring3ID)) {
                                var ring3 = ringDictionary[ring3ID];

                                if (ring1.ConnectedBonds.Count(n => n.BondID == bond1.BondID) == 1 && ring3.ConnectedBonds.Count(n => n.BondID == bond1.BondID) == 1) {
                                    if (ring3.RingEnv.KetonOutRing >= 1 && ring3.RingEnv.DoubleBond_CarbonOutRing >= 1 &&
                                        ring2.RingEnv.KetonOutRing >= 1 && ring2.RingEnv.CarbonOutRing >= 3 && ring2.RingEnv.DoublebondInRing == 1)
                                        descriptor.OVMIWIRPAZMPMW = 1;
                                    if (ring3.RingEnv.KetonOutRing >= 1 && ring3.RingEnv.DoubleBond_CarbonOutRing >= 1 &&
                                        ring2.RingEnv.KetonOutRing >= 1 && ring2.RingEnv.CarbonOutRing >= 3 && ring2.RingEnv.DoublebondInRing == 1 && 
                                        ring1.RingEnv.Carbon_OxygenOutRing >= 1)
                                        descriptor.MESCNDMXSKOWHE = 1;
                                }
                            }
                        }
                    }
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1) {
                        if (RingtypeChecker(atom7, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.CarbonOutRing >= 1)
                                descriptor.OQRACADPMGWIGG = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-S-C=N-O":
                    #region
                    atom1BranchedAtoms = getBranchAtom(atom1, bond1, "C");
                    atom7BranchedAtoms = getBranchAtom(atom7, bond6, "S");

                    if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 && atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1) {
                        if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1) {
                            descriptor.BCZCEDXQUVQFNI = 1;
                            if (atom3.AtomEnv.SingleBond_OxygenCount >= 1) {
                                descriptor.VZRIGAQJSXYJDP = 1;
                                if (atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1) {
                                    descriptor.JDVGMXCLBRXCCE = 1;
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-O-C-C-C-C-O":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.CarbonOutRing >= 1)
                                descriptor.OOLQGZAAQGIVPH = 1;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_OxygenOutRing >= 1)
                                descriptor.UGJALIVEHMFBSP = 1;
                        }
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 6 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.NHYPIRXECJPNTG = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 2)
                                descriptor.CPODZCVBMHISBV = 1;

                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;

                        if (ring1Env.OxygenOutRing >= 2 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.SJTNJMYYSPVHOR = 1;

                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1 && 
                            atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.POMIUIQILZMMDR = 1;


                        if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Chroman, out ring2ID)) {
                            var chroman = ringsetDictionary[ring2ID];
                            if (chroman.RingIDs.Count == 2) {
                                ring3ID = chroman.RingIDs.Where(n => n != ring1ID).ToList()[0];
                                var ring3Env = ringDictionary[ring3ID].RingEnv;

                                if (ring3Env.KetonOutRing >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                                    descriptor.DEAZPSKQWDTLRW = 1;

                            }
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-C-C-C-O":
                    #region
                    if (atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.LOSWWGJGSSQDKH = 1;
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom5.AtomEnv.SingleBond_OxygenCount >= 1 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.PATWQGKLOAFEDC = 1;
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                        if (atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 2)
                                descriptor.LWQLZUYMERWSMQ = 1;

                        }
                    }
                    else if (!bond1.IsInRing && !atom3.IsInRing && !atom7.IsInRing &&
                            RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                            RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {

                                bAtoms = getBranchAtom(atom7, bond6, "C");
                                foreach (var bAtom in bAtoms) {
                                    if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {

                                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 3 &&
                                            ring2Env.CarbonOutRing >= 1 && ring3Env.OxygenOutRing >= 4 && ring3Env.CarbonOutRing >= 1)
                                            descriptor.UIIWEQMFARGUFG = 1;
                                        break;
                                    }
                                }
                    }

                    break;
                    #endregion
                case "C-C-C-O-C-C-O":
                    #region
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 
                        && RingtypeChecker(bond5, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_DoubleOxygensOutRing >= 1 && ringEnv.EtherOutRing >= 2) {
                                var branchedAtoms = getBranchAtom(atom7, bond6, "C");
                                if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                    descriptor.AQXYGNDEVDTNGB = 1;
                            }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1) {
                            descriptor.MLAMTXQKDKVEQN = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-C-C=C-C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) && 
                        RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 1) {
                            if (atom4.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.WZQIRMNZNKYTMX = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-C-C-C-C-S":
                    #region
                    if (atom1.AtomEnv.SingleBond_FCount >= 2 && atom2.AtomEnv.SingleBond_FCount >= 2 && atom3.AtomEnv.SingleBond_FCount >= 2 &&
                        atom4.AtomEnv.SingleBond_FCount >= 2 && atom5.AtomEnv.SingleBond_FCount >= 2 && atom6.AtomEnv.SingleBond_FCount >= 2 && atom7.AtomEnv.OxygenCount >= 3)
                        descriptor.SKZFCJKAAJTRQC = 1;
                    break;
                    #endregion
                case "C-C-O-P-O-P-O":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring1ID) && atom4.AtomEnv.OxygenCount >= 4 && atom6.AtomEnv.OxygenCount >= 4) {
                        var ringEnv1 = ringDictionary[ring1ID].RingEnv;
                        if (ringEnv1.OxygenOutRing >= 1)
                            descriptor.JOAKUIIGLMLEGB = 1;
                        if (ringEnv1.OxygenOutRing >= 2) {
                            var branchedAtoms = getBranchAtom(atom7, bond6, "C");
                            foreach (var bAtom in branchedAtoms) {
                                if (bAtom.IsInRing) {
                                    if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                                        var ringEnv2 = ringDictionary[ring2ID].RingEnv;
                                        if (ringEnv2.OxygenOutRing >= 4) {
                                            descriptor.SIGYXFJSXPUADL = 1;
                                            if (ringEnv2.CarbonOutRing >= 1)
                                                descriptor.VOTYPVGLIYAPMP = 1;
                                            if (ringEnv2.Carbon_OxygenOutRing >= 1)
                                                descriptor.ZZAHNEVHQYWAEU = 1;
                                            if (ringEnv2.Carbon_DoubleOxygensOutRing >= 1)
                                                descriptor.RPPSKUJCSABBGU = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-O-C=C-C-O-C":
                    #region
                    if (atom5.AtomEnv.DoubleBond_CO_Count >= 1) {
                        descriptor.AUTCCPQKLPMHDN = 1;
                        if (atom4.AtomEnv.CarbonCount >= 3) {
                            descriptor.XEQYPQHAQCSASZ = 1;
                            bAtoms = getBranchAtom(atom4, bond3, bond4, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1) {
                                descriptor.JLNIGUCDBFYXDC = 1;
                                if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 3) >= 1)
                                    descriptor.LZQFCQFMVUXLTE = 1;
                            }
                        }
                    }
                    if (atom4.AtomEnv.SingleBond_CarbonCount >= 2) {
                        bAtoms = getBranchAtom(atom4, bond3, bond4, "C");
                        foreach (var bAtom in bAtoms) {
                            if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Piperidine, out targetRingID)) {
                                var ringEnv = ringDictionary[targetRingID].RingEnv;

                                if (atom5.AtomEnv.DoubleBond_CO_Count >= 1 && ringEnv.SingleBond_CarbonOutRing >= 2) {
                                    descriptor.DDDFNRDAPKJTGF = 1;
                                    if (ringEnv.Carbon_AlkaneOutRing >= 1)
                                        descriptor.OKUIXBZDLGNNIT = 1;
                                }


                                if (ringEnv.Carbon_AlkaneOutRing >= 2) {
                                    descriptor.OKTTXPFUNSZDIO = 1;
                                    

                                    if (ringEnv.SingleBond_CarbonOutRing >= 3) {
                                        descriptor.FYMHCWBPOCZPKA = 1;
                                        var nitrogens = ringDictionary[targetRingID].ConnectedAtoms.Where(n => n.AtomString == "N").ToList();
                                        if (nitrogens.Count == 1) {
                                            var nitrogen = nitrogens[0];
                                            var outsideAtoms = ringEnv.OutsideAtomDictionary[nitrogen.AtomID];
                                            if (outsideAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                                descriptor.UBAHQWXPQYBZBI = 1;
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "O-C-C-C-O-C=O":
                    #region
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.BVDRUCCQKHGCRX = 1;
                    break;
                    #endregion
                case "C-S-C-C-C-C=O":
                    #region
                    if (atom5.AtomEnv.SingleBond_NitrogenCount >= 1)
                        descriptor.AMRLMYBWZLIJPE = 1;
                    break;
                    #endregion
                case "C-C-N=C-C-C-N":
                    #region
                    if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.QDHYJVJAGQLHBA = 1;
                    break;
                    #endregion
                case "C-C-N-C-C-O-C":
                    #region
                    bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                    if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                        descriptor.KBUULMSISDOTQC = 1;
                    break;
                    #endregion
                case "N-C-C-C-N-C=O":
                    #region
                    if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Imidazole, out targetRingID)) {
                        descriptor.ZHGAPOLZDJPAJB = 1;
                    }
                    else if (!bond1.IsInRing && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Morpholine, out targetRingID)) {
                        descriptor.PMLVVKMWRUMPQM = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-C=N-C-C":
                    #region
                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom7.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.FDEOGFDSMRBYGC = 1;
                    break;
                    #endregion
                case "C-N-C-C-C-C-O":
                    #region
                    if (atom2.AtomEnv.SingleBond_CarbonCount >= 3 && atom4.AtomEnv.DoubleBond_CO_Count >= 1 &&
                        atom6.AtomEnv.DoubleBond_CN_Count >= 1 && atom5.AtomEnv.DoubleBond_CC_Count >= 1) {
                            bAtoms = getBranchAtom(atom5, bond4, bond5, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.JLSURYXXEOGGEU = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-C-C=C-O":
                    #region
                    if (atom7.AtomEnv.SingleBond_CarbonCount >= 2 && atom5.AtomEnv.SingleBond_CarbonCount >= 2) {
                        bAtoms = getBranchAtom(atom5, bond4, bond5, "C");
                        if (bAtoms.Count(n => n.AtomFunctionType == AtomFunctionType.C_Ester) >= 1) {
                            descriptor.CVFFFSPGTZUPTF = 1;
                            if (atom4.AtomEnv.SingleBond_CarbonCount >= 3)
                                descriptor.TYXOEOVOUIVPQE = 1;
                        }
                    }
                    break;
                    #endregion
                case "O-C-C-C-C-O-P":
                    #region
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.OxygenCount >= 4)
                        descriptor.QRDCEYBRRFPBMZ = 1;
                    break;
                    #endregion
                case "O-C-C-C-C-C-N":
                    #region
                    if (!bond2.IsInRing && bond4.IsSharedBondInRings) {
                        if (RingtypeChecker(atom3, ringDictionary, RingFunctionType.DihydroPyran, out ring1ID) && 
                            RingtypeChecker(atom7, ringDictionary, RingFunctionType.Piperidine, out ring2ID)) {
                                var ring1 = ringDictionary[ring1ID];
                                var ring2 = ringDictionary[ring2ID];
                                var ring1Env = ringDictionary[ring1ID].RingEnv;
                                var ring2Env = ringDictionary[ring2ID].RingEnv;
                                if (ring1.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1 && ring2.ConnectedBonds.Count(n => n.BondID == bond4.BondID) == 1) {
                                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 2 && atom2.AtomEnv.DoubleBond_CO_Count >= 1 && ring1Env.CarbonOutRing >= 4) {
                                        descriptor.UFKDXENLDAZGOS = 1;
                                        if (atom7.AtomEnv.SingleBond_CarbonCount >= 3) {
                                            descriptor.HIKHPXKYXLQZFQ = 1;
                                            var outAtoms = ring2Env.OutsideAtomDictionary[atom7.AtomID];
                                            foreach (var oAtom in outAtoms) {
                                                if (oAtom.AtomEnv.SingleBond_CarbonCount >= 1)
                                                    descriptor.SJJYEHXVJQAFPK = 1;
                                            }
                                        }
                                    }
                                }
                        }
                    }
                    break;
                    #endregion
                case "C-N-C-C=C-C-O":
                    #region
                    if (RingsettypeChecker(atom1, ringDictionary, ringsetDictionary, RingsetFunctionType.Purine, out ring1ID) &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out ring2ID)) {
                            if (atom5.AtomEnv.SingleBond_CarbonCount >= 2)
                                descriptor.UZKQTCBAMSWPJD = 1;
                    }
                    break;
                    #endregion
                case "C-C=C-C-O-C-C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)){
                        var ringEnv = ringDictionary[ring1ID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 2 && atom4.AtomEnv.DoubleBond_CO_Count >= 1 && 
                            atom7.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1){
                            descriptor.HGZGMSVCFBWKLH = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-S-C-C-C-C":
                    #region
                    if (atom6.AtomEnv.SingleBond_NitrogenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom7.AtomEnv.DoubleBond_CO_Count >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 2)
                                descriptor.FFMZJRIUKRMUJN = 1;
                    }
                    break;
                    #endregion
                case "C-C-C-N=C-C-C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 2 && atom5.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 2) {
                            var atom3Branchs = getBranchAtom(atom3, bond2, bond3, "C");
                            var atom7Branches = getBranchAtom(atom7, bond6, "C");
                            if (atom3Branchs.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1 && n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1 &&
                                atom7Branches.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 3) >= 1)
                                descriptor.JTBLHXSTTJUWET = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-S-C-C-C-C-C":
                    #region
                    if (atom3.AtomEnv.DoubleBond_CN_Count >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && ringEnv.Carbon_OxygenOutRing >= 1) {
                            bAtoms = getBranchAtom(atom7, bond6, "S");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1) {
                                descriptor.KJPUBQHCMQSSNA = 1;
                                if (bAtoms.Count(n => n.AtomEnv.DoubleBond_SO_Count >= 1) >= 1)
                                    descriptor.HENJHBDLEUWCGF = 1;
                            }

                        }
                    }
                    break;
                    #endregion
                case "C-O-C:C:C:C:C":
                    #region
                    if (bond5.IsSharedBondInRings && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                        RingtypeChecker(atom7, ringDictionary, RingFunctionType.Benzene, out ring3ID)) {
                       
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                        if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrylium, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 3)
                                descriptor.CQZLVEWBMWVXBQ = 1;
                        }
                        else if (RingtypeChecker(atom4, ringDictionary, RingFunctionType.AromaticPyran, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_DoubleOxygensOutRing >= 1 && ring3Env.OxygenOutRing >= 3 && ring2Env.KetonOutRing >= 1)
                                descriptor.OGEXYUOJQNKSAL = 1;
                        }
                    }
                    break;
                    #endregion
                case "C-C-O-C-O-C-C":
                    #region
                    descriptor.KLKFAASOGCDTDT = 1;
                    if (atom7.AtomEnv.SingleBond_OxygenCount >= 1) {
                        descriptor.BXXXHBWYZKLUGM = 1;
                        if (atom6.AtomEnv.SingleBond_CarbonCount >= 2) {
                            bAtoms = getBranchAtom(atom6, bond5, bond6, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.QYZAWZFNVOMDEF = 1;
                        }
                    }
                    if (atom7.AtomEnv.DoubleBond_CO_Count >= 1) {
                        descriptor.KTRCDOAFABEGON = 1;
                        if (atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.HRARQIAGSKMHMV = 1;
                    }
                    break;
                    #endregion
                case "C-C-O-C=C-C-O":
                    #region
                    if (atom6.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 2)
                        descriptor.YAHJGDKGXVOYKU = 1;
                    break;
                    #endregion
                case "C:C:C:N:C:C:C":
                    #region
                    if (bond2.IsSharedBondInRings && bond5.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Pyrrole, out ring2ID) &&
                        RingtypeChecker(atom7, ringDictionary, RingFunctionType.Pyridine, out ring3ID)) {
                            descriptor.AIFRHYZBTHREPW = 1;
                    }
                    break;
                    #endregion
                case "C-C-N-C-C-C-O":
                    #region
                    if (bond2.IsSharedBondInRings && bond3.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrrolidine, out ring1ID) &&
                        RingtypeChecker(atom5, ringDictionary, RingFunctionType.Piperidine, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (atom3.AtomEnv.SingleBond_CarbonCount >= 3) {
                            descriptor.CYHOMWAPJJPNMW = 1;
                            bAtoms = getBranchAtom(atom7, bond6, "C");
                            if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1)
                                descriptor.CHCUDBFRYVCNDF = 1;
                        }
                    } 
                    break;
                    #endregion
                case "C-O-C-C-C-N-C":
                    #region
                    if (bond5.IsSharedBondInRings && RingtypeChecker(atom7, ringDictionary, RingFunctionType.Pyrrolidine, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.HydroPyrrolidine, out ring2ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 1) {
                            descriptor.NPZQKSBUUPRTOB = 1;
                            if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_CarbonCount >= 1) {
                                bAtoms = getBranchAtom(atom1, bond1, "C");
                                if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1 && n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                    descriptor.MFXCOLDTJDSQIV = 1;
                            }
                        }
                        if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && ring1Env.EtherOutRing >= 1) {
                            var outAtomList = ring1Env.OutsideAtomDictionary;
                            foreach (var outAtoms in outAtomList) {
                                foreach (var outAtom in outAtoms.Value) {
                                    if (outAtom.AtomString == "O") {
                                        var cBonds = outAtom.ConnectedBonds.Where(n => n.IsInRing == false).ToList();
                                        foreach (var cBond in cBonds) {
                                            foreach (var cAtom in cBond.ConnectedAtoms) {
                                                if (cAtom.AtomEnv.DoubleBond_CO_Count >= 1) {
                                                    descriptor.PNTFIUBSDLCBKC = 1;
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                    break;
                    #endregion
                case "C-O-C-O-C-C-O":
                    #region

                    if (!atom2.IsInRing && !atom7.IsInRing &&
                           RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;

                        atom1BranchedAtoms = getBranchAtom(atom1, bond1, "C");
                        if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1 &&
                            ring1Env.OxygenOutRing >= 4) {

                            atom7BranchedAtoms = getBranchAtom(atom7, bond6, "C");
                            foreach (var b7Atom in atom7BranchedAtoms) {
                                if (RingtypeChecker(b7Atom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                                    var ring2Env = ringDictionary[ring2ID].RingEnv;

                                    if (ring2Env.OxygenOutRing >= 4 & ring2Env.Carbon_AlkaneOutRing >= 1)
                                        descriptor.WVPWRXOKOWTKIB = 1;
                                }
                            }
                        }
                    }

                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID)) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        if (ring1Env.OxygenOutRing >= 3)
                            descriptor.VMUAZGMTADMRDK = 1;
                        if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1)
                            descriptor.FMNTWTDBPCQTDP = 1;
                        bAtoms = getBranchAtom(atom5, bond4, bond5, "C");
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.LBTWTOJXQNSHOG = 1;

                        if (!atom2.IsInRing && !atom7.IsInRing &&
                            RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            bAtoms = getBranchAtom(atom7, bond6, "C");
                            foreach (var bAtom in bAtoms) {
                                if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {
                                    var ring3Env = ringDictionary[ring3ID].RingEnv;
                                    if (ring1Env.OxygenOutRing >= 3 && ring2Env.OxygenOutRing >= 4 && ring3Env.OxygenOutRing >= 4 && ring3Env.CarbonOutRing >= 1)
                                        descriptor.YCNDWQCUDMBHPI = 1;
                                    if (ring1Env.OxygenOutRing >= 3 && ring1Env.Carbon_KetoneOutRing >= 1 &&
                                        ring2Env.OxygenOutRing >= 4 && ring3Env.OxygenOutRing >= 4 && ring3Env.CarbonOutRing >= 1) {
                                        descriptor.BMCWHUJLFHECDG = 1;
                                        if (ring1Env.Carbon_AlkaneOutRing >= 1)
                                            descriptor.VYYXQADWBBPULR = 1;
                                    }
                                }
                            }
                        }
                    }
                    else if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.AromaticPyran, out ring1ID)) {
                        var ring1 = ringDictionary[ring1ID];
                        var ring1Env = ringDictionary[ring1ID].RingEnv;

                        if (ringsetDictionary[ring1.RingsetID].RingIDs.Count == 2) {
                            var benzenID = ringsetDictionary[ring1.RingsetID].RingIDs.Where(n => n != ring1ID).ToList()[0];
                            var benzene = ringDictionary[benzenID];

                            if (!atom2.IsInRing && !atom7.IsInRing &&
                            RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {
                                var ring2Env = ringDictionary[ring2ID].RingEnv;
                                bAtoms = getBranchAtom(atom7, bond6, "C");
                                foreach (var bAtom in bAtoms) {
                                    if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {
                                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                                        if (ring2Env.OxygenOutRing >= 4 && ring3Env.OxygenOutRing >= 4 && ring3Env.SingleBond_CarbonOutRing >= 1 &&
                                            benzene.RingEnv.OxygenOutRing >= 3)
                                            descriptor.LJNXEWZPPMBSJJ = 1;
                                    }
                                }
                            }
                        }
                    }
                    if (!atom2.IsInRing && !atom7.IsInRing &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID)) {

                        var b1Atoms = getBranchAtom(atom7, bond6, "C");
                        foreach (var b1Atom in b1Atoms) {

                            if (RingtypeChecker(b1Atom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {

                                var b2Atoms = getBranchAtom(atom1, bond1, "C");
                                var ring1Env = ringDictionary[ring1ID].RingEnv;
                                var ring2Env = ringDictionary[ring2ID].RingEnv;

                                if (b2Atoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1 && ring1Env.OxygenOutRing >= 4 &&
                                        ring2Env.OxygenOutRing >= 4 && ring2Env.SingleBond_CarbonOutRing >= 1)
                                    descriptor.TYTGMCXGFOWKTG = 1;

                                foreach (var b2Atom in b2Atoms) {

                                    if (RingtypeChecker(b2Atom, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID)) {

                                        var ring3Env = ringDictionary[ring3ID].RingEnv;

                                        if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_OxygenOutRing >= 1 && 
                                            ring2Env.OxygenOutRing >= 3 &&
                                            ring3Env.OxygenOutRing >= 4 && ring3Env.SingleBond_CarbonOutRing >= 1)
                                            descriptor.TXIGVGWKASKMAT = 1;
                                    }
                                }
                            }
                        }

                    }
                    break;
                    #endregion
                case "O-C:C:C:C:C-C":
                    #region
                    if (bond2.IsSharedBondInRings && bond5.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Dioxolane, out ring1ID) &&
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Benzene, out ring2ID) &&
                        (RingtypeChecker(atom7, ringDictionary, RingFunctionType.Piperidine, out ring3ID) ||
                        RingtypeChecker(atom7, ringDictionary, RingFunctionType.Hydropiperidine, out ring3ID))){
                        var ring3Env = ringDictionary[ring3ID].RingEnv;
                        if (ring3Env.SingleBond_CarbonOutRing >= 1)
                            descriptor.XNZFFRJWTVYMBF = 1;
                    }
                    break;
                    #endregion
                case "C-C-N=C-N=C-N":
                    #region 
                    descriptor.YLHJZQNYUYVALI = 1;
                    break;
                #endregion
                case "O-C-C-O-C-C-O":
                    #region
                    descriptor.MTHSVFCYNBDYFN = 1;
                    break;
                    #endregion
                case "O-C-C-C-O-P-O":
                    if (atom3.AtomEnv.SingleBond_OxygenCount >= 1 && atom6.AtomEnv.DoubleBond_PO_Count >= 1)
                        descriptor.VSFDKTSQZYFUOP = 1;
                    break;
                case "C-C=C-O-C-C-O":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        descriptor.QANLIVZYWPBKPX = 1;
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.JUOCEVARIIPUNO = 1;
                        if (ringEnv.EtherOutRing >= 1 && atom3.AtomEnv.SingleBond_CarbonCount >= 1)
                            descriptor.DUKLXSCPUYTGMX = 1;
                        if (ringEnv.OxygenOutRing >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.ROTFEGAKYXTHPP = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.FDNTWSSVAAXJJX = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1 && atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.AKOMBGQKYZCJOF = 1;

                        bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1)>= 1) {
                            descriptor.FUTPFHGTGHIWFV = 1;
                            if (ringEnv.OxygenOutRing >= 1)
                                descriptor.KGKCIJKXFXINOM = 1;
                            if (atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.SEKORMOFYKJAJU = 1;
                        }
                    }
                    break;
                case "C-C=C-O-C-O-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                            descriptor.FMQIYCZYTNRLES = 1;
                        if (ringEnv.OxygenOutRing >= 2 && atom3.AtomEnv.SingleBond_CarbonCount >= 1)
                            descriptor.YSDLONOYOKMMAS = 1;

                        bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                        if (bAtoms.Count(n => n.AtomEnv.DoubleBond_CO_Count >= 1) >= 1) {
                            if (ringEnv.OxygenOutRing >= 1 && atom7.AtomEnv.SingleBond_CarbonCount >= 1)
                                descriptor.UTUBAYVKULAGMT = 1;
                        }
                    }
                    break;
                case "C-C=C-O-C-C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1 && atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.IHOWYPOFBITTRU = 1;
                    }
                    break;
                case "C:N:C-C-C-N-C":
                    if (bond2.IsSharedBondInRings && 
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyridine, out ring1ID) &&
                        (RingtypeChecker(atom4, ringDictionary, RingFunctionType.Piperidine, out ring2ID) || 
                        RingtypeChecker(atom4, ringDictionary, RingFunctionType.Hydropiperidine, out ring2ID))) {
                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;

                        if (ring1Env.KetonOutRing >= 1 && atom7.AtomEnv.DoubleBond_CO_Count >= 1 && atom7.AtomEnv.SingleBond_NitrogenCount >= 2)
                            descriptor.CXZNQDNVILLBLN = 1;

                    }
                    break;
                case "C-O-C-S-C=N-O":
                    atom1BranchedAtoms = getBranchAtom(atom1, bond1, "C");
                    atom3BranchedAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                    atom7BranchedAtoms = getBranchAtom(atom7, bond6, "S");

                    if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 2 && 
                        atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1 && 
                        atom5.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.BIDLAQGLNJPMQU = 1;

                    if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 &&
                        atom3BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 &&
                        atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1 &&
                        atom5.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.WCEGKXRIUJFLCT = 1;

                    if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 &&
                        atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 3) >= 1 &&
                        atom5.AtomEnv.SingleBond_CarbonCount >= 1)
                        descriptor.BIDLAQGLNJPMQU = 1;

                    break;

                case "P-O-C-C-C-O-P":
                    if (atom1.AtomEnv.OxygenCount >= 3 && atom7.AtomEnv.OxygenCount >= 4 && 
                        bond4.IsInRing && !bond3.IsInRing && !bond5.IsInRing) {
                        if (RingtypeChecker(bond4, ringDictionary, RingFunctionType.Tetrahydrofuran, out targetRingID)) {
                            var ringEnv = ringDictionary[targetRingID].RingEnv;
                            if (ringEnv.OxygenOutRing >= 2) {
                                descriptor.WMISJDZYUVCWEJ = 1;
                            }
                        }
                    }
                    break;
                case "C-C-C-O-C-C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3 && 
                            atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1 &&
                            atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.JMKLCSDHBFYFKV = 1;

                        if (ringEnv.OxygenOutRing >= 3 &&
                            atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1 &&
                            atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.DDNOLNFAGDJTJZ = 1;

                        if (ringEnv.OxygenOutRing >= 3 &&
                            atom2.AtomEnv.DoubleBond_CO_Count >= 1 &&
                            atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1) {
                            bAtoms = getBranchAtom(atom7, bond6, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.BUIKUQZPNWEHMR = 1;
                        }
                    }
                    break;
                case "C-O-C-C-C-C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 4 &&
                            atom4.AtomEnv.SingleBond_OxygenCount >= 1 && atom5.AtomEnv.SingleBond_OxygenCount >= 1 &&
                            atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.KLLPNSTZQPSZHU = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1 &&
                            atom5.AtomEnv.SingleBond_OxygenCount >= 1 &&
                            atom6.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.MPALGUFFCYBZBP = 1;
                        if (ringEnv.OxygenOutRing >= 1 && ringEnv.SingleBond_CarbonOutRing >= 1 &&
                           atom4.AtomEnv.SingleBond_OxygenCount >= 1 &&
                           atom5.AtomEnv.SingleBond_OxygenCount >= 1 && atom7.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.RLQSUKOOLWMMSY = 1;
                    }
                    break;
                case "C-O-C=C-O-C-O":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 3) {
                            bAtoms = getBranchAtom(atom3, bond2, bond3, "C");

                            foreach (var bAtom in bAtoms) {
                                if (RingtypeChecker(bAtom, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {
                                    var ring2Env = ringDictionary[ring2ID].RingEnv;
                                    if (ringEnv.OxygenOutRing >= 1)
                                        descriptor.ZGELUCMZDPAADK = 1;
                                    if (ringEnv.OxygenOutRing >= 2)
                                        descriptor.LLZWBQZRXRWZDE = 1;
                                }
                            }


                        }
                    }
                    break;
                case "C-O-C-C-O-C-C":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;

                        if (ringEnv.OxygenOutRing >= 4 && ringEnv.Carbon_DoubleOxygensOutRing >= 1) {
                            bAtoms = getBranchAtom(atom3, bond2, bond3, "C");
                            if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                                descriptor.HQZSWVRAXHHXOP = 1;
                        }
                    }

                    break;
                case "C-N-C-O-C-C-O":
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Pyrimidine, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.NitrogenOutRing >= 2) {
                            if (!atom7.IsInRing && RingtypeChecker(atom4, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID)) {
                                var ring2Env = ringDictionary[ring2ID].RingEnv;
                                if (ring2Env.OxygenOutRing >= 2 && ring2Env.Carbon_OxygenOutRing >= 1) {
                                    bAtoms = getBranchAtom(atom7, bond6, "P");
                                    if (bAtoms.Count(n => n.AtomEnv.OxygenCount >= 4) >= 1)
                                        descriptor.AEJYSWYTTROYBT = 1;
                                }
                            }



                        }
                    }
                    break;
                case "N-C-C-C-C-O-P":
                    atom1BranchedAtoms = getBranchAtom(atom1, bond1, "C");
                    atom7BranchedAtoms = getBranchAtom(atom7, bond6, "O");
                    if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1 && 
                        atom7BranchedAtoms.Count(n => n.AtomEnv.SingleBond_PhosphorusCount >= 2) >= 1 &&
                        atom2.AtomEnv.DoubleBond_CO_Count >= 1 && atom3.AtomEnv.SingleBond_OxygenCount >= 1 && 
                        atom4.AtomEnv.SingleBond_CarbonCount >= 4)
                        descriptor.TZXLXHWAZVVUPY = 1;
                    break;
                case "C-C:C-O-C-C-O":
                    if (!bond1.IsInRing && RingsettypeChecker(atom2, ringDictionary, ringsetDictionary, RingsetFunctionType.Chromone, out targetRingID) &&
                          RingtypeChecker(atom2, ringDictionary, RingFunctionType.AromaticPyran, out ring1ID) &&
                          RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring2ID)) {

                        var pyran = ringDictionary[ring1ID];
                        var benzene = ringDictionary[ring2ID];

                        if (ringsetDictionary[targetRingID].RingIDs.Count == 2) {
                            var benzen2ID = ringsetDictionary[targetRingID].RingIDs.Where(n => n != pyran.RingID).ToList()[0];
                            var benzene2 = ringDictionary[benzen2ID];

                            if (benzene.RingEnv.OxygenOutRing >= 2 &&
                                benzene2.RingEnv.OxygenOutRing >= 3) {
                                descriptor.YJDSHSSMZYNEPP = 1;
                            }
                        }
                    }
                    break;
                case "C-C-O-P-O-C-C":

                    atom1BranchedAtoms = getBranchAtom(atom1, bond1, "C");
                    atom7BranchedAtoms = getBranchAtom(atom7, bond6, "C");

                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 &&
                        atom4.AtomEnv.OxygenCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 2 &&
                        atom7.AtomEnv.SingleBond_OxygenCount >= 1) {
                        descriptor.ACOJOKPGGCIXCN = 1;

                        if (atom1BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 2) >= 1)
                            descriptor.LWZOUGMRXADXQM = 1;
                    }

                    if (atom1.AtomEnv.SingleBond_NitrogenCount >= 1 &&
                        atom4.AtomEnv.OxygenCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 2) {

                        if (atom7BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 1) >= 1) {
                            descriptor.OHLUOZVYBDODSP = 1;
                            if (atom7.AtomEnv.SingleBond_OxygenCount >= 1 && 
                                atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 4) >= 1)
                                descriptor.SUHOQUVVVLNYQR = 1;
                        }


                    }

                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom4.AtomEnv.OxygenCount >= 4 &&
                        atom7.AtomEnv.SingleBond_OxygenCount >= 1) {
                       
                        if (atom1BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1 &&
                            atom7BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.LLCSXHMJULHSJN = 1;
                    }
                    break;
                case "C-O-C-C-O-C-O":

                    if (!atom2.IsInRing && !atom3.IsInRing && atom5.IsInRing && !atom7.IsInRing) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Tetrahydropyran, out ring1ID) &&
                            RingtypeChecker(atom5, ringDictionary, RingFunctionType.Tetrahydropyran, out ring2ID)) {

                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 4 && ring1Env.Carbon_OxygenOutRing >= 1 && 
                                ring2Env.OxygenOutRing >= 4) {
                                bAtoms = getBranchAtom(atom7, bond6, "C");
                                if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 1) >= 1)
                                    descriptor.AWHCMAHYNRUGIC = 1;
                            }

                        }
                    }

                    break;

                case "N-C-C-O-P-O-C":
                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom1.AtomCharge == 1 &&
                        atom5.AtomEnv.OxygenCount >= 4) {
                        bAtoms = getBranchAtom(atom7, bond6, "C");

                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.KLVLAMGYGAOJBE = 1;

                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1 &&
                            bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.DFJZCSQJBNQERR = 1;


                    }
                    break;
                case "C-C-C-O-P-O-C":
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom2.AtomEnv.SingleBond_OxygenCount >= 1 &&
                        atom5.AtomEnv.OxygenCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 1) {
                        bAtoms = getBranchAtom(atom7, bond6, "C");

                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1 &&
                            bAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.GLDWNOHZSHZNOT = 1;
                        if (bAtoms.Count(n => n.AtomEnv.SingleBond_NitrogenCount >= 1) >= 1)
                            descriptor.JZNWSCPGTDBMEW = 1;



                    }
                    break;

                case "N-C-C-C-O-C-C":

                    if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom1.AtomCharge == 1 &&
                        atom7.AtomEnv.SingleBond_OxygenCount >= 1) {

                        atom2BranchedAtoms = getBranchAtom(atom2, bond1, bond2, "C");
                        atom7BranchedAtoms = getBranchAtom(atom7, bond6, "C");

                        if (atom2BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 2) >= 1 &&
                            atom7.AtomEnv.SingleBond_CarbonCount >= 2)
                            descriptor.QUXPINSZKIRTKH = 1;

                        if (atom2BranchedAtoms.Count(n => n.AtomEnv.OxygenCount >= 2) >= 1 &&
                            atom7BranchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1)
                            descriptor.NOXWNIQIEPDBLD = 1;
                    }

                    break;

                case "C-O-P-O-C-C-C":

                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 6 && atom3.AtomEnv.OxygenCount >= 4) {
                            descriptor.ZDBWHGOODQDHIY = 1;
                            if(atom6.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.RWZRMLYARYCZJA = 1;
                        }
                            
                    }

                    break;
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4,
            AtomProperty atom5, AtomProperty atom6, AtomProperty atom7, AtomProperty atom8,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4,
            BondProperty bond5, BondProperty bond6, BondProperty bond7,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var ring5ID = -1;
            var ring6ID = -1;
            var targetRingID = -1;

            switch (connectString) {
                case "C-C-C-C-C-C-C-C": //steroid
                    #region
                    if (atom2.AtomEnv.SingleBond_OxygenCount >= 1 && atom6.AtomEnv.DoubleBond_CO_Count >= 1)
                        descriptor.YFBDMUJOFKYAGE = 1;

                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.Carbon_KetoneOutRing >= 1 && ringEnv.EtherOutRing >= 1)
                            descriptor.ZBTPMRFIHVRWEM = 1;

                        if (ringEnv.EtherOutRing >= 1 && ringEnv.EsterOutRing >= 1)
                            descriptor.DBFVUHASLXHZCE = 1;
                    }
                    if (bond1.IsSharedBondInRings && bond3.IsSharedBondInRings && bond5.IsSharedBondInRings && 
                        bond1.SharedRingIDs.Count == 2 && bond3.SharedRingIDs.Count == 2 && bond5.SharedRingIDs.Count == 2) {

                            RingProperty ring1 = null;
                            RingProperty ring2 = null;
                            RingProperty ring3 = null;
                            RingProperty ring4 = null;

                            if (ringDictionary[bond1.SharedRingIDs[0]].ConnectedBonds.Count(n => n.IsSharedBondInRings) == 1) {
                                ring1 = ringDictionary[bond1.SharedRingIDs[0]];
                                ring2 = ringDictionary[bond1.SharedRingIDs[1]];
                                ring3 = ringDictionary[bond3.SharedRingIDs.Where(n => n != ring2.RingID).ToList()[0]];
                                ring4 = ringDictionary[bond5.SharedRingIDs.Where(n => n != ring3.RingID).ToList()[0]];
                            }
                            else {
                                ring1 = ringDictionary[bond1.SharedRingIDs[1]];
                                ring2 = ringDictionary[bond1.SharedRingIDs[0]];
                                ring3 = ringDictionary[bond3.SharedRingIDs.Where(n => n != ring2.RingID).ToList()[0]];
                                ring4 = ringDictionary[bond5.SharedRingIDs.Where(n => n != ring3.RingID).ToList()[0]];
                            }

                            if (ring1.ConnectedAtoms.Count == 6 && ring2.ConnectedAtoms.Count == 6 && 
                                ring3.ConnectedAtoms.Count == 6 && ring4.ConnectedAtoms.Count == 5) { //steroid 6-6-6-5
                                if (atom2.AtomEnv.SingleBond_CarbonCount >= 4 && ring1.RingEnv.DoublebondInRing >= 2 && ring1.RingEnv.KetonOutRing >= 1 &&
                                    ring4.RingEnv.OxygenOutRing >= 1 && ring4.RingEnv.Carbon_KetoneOutRing >= 1 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                                    atom8.AtomEnv.SingleBond_CarbonCount >= 3) {
                                        descriptor.LHUOUYOGBORMMH = 1;
                                        if (ring3.RingEnv.OxygenOutRing >= 1)
                                            descriptor.MDOJKSAAJAOYSQ = 1;
                                }

                            }
                    }

                    break;
                    #endregion
                case "C-C-C=C-C-C-C-C":
                    #region
                    if (atom8.AtomEnv.DoubleBond_CO_Count >= 1 && RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentane, out targetRingID)) {
                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.KetonOutRing >= 1 && ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_AlkeneOutRing >= 1)
                            descriptor.AFEBBKTVFDABFK = 1;
                    }
                    if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom8.AtomEnv.DoubleBond_CO_Count >= 1 && atom8.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.NMWHGTRTJQHXGY = 1;
                    break;
                    #endregion
                case "C-C-C-C-C:C:C:C":
                    if (bond1.IsSharedBondInRings && bond5.IsSharedBondInRings && bond7.IsSharedBondInRings && atom4.IsSharedAtomInRings) {
                        if (RingtypeChecker(bond1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) &&
                            RingtypeChecker(atom3, ringDictionary, RingFunctionType.Piperidine, out ring2ID) &&
                            RingtypeChecker(bond4, ringDictionary, RingFunctionType.Hydropiperidine, out ring3ID) &&
                            RingtypeChecker(bond6, ringDictionary, RingFunctionType.Pyrrole, out ring4ID) &&
                            RingtypeChecker(bond7, ringDictionary, RingFunctionType.Benzene, out ring5ID)) {

                                var ring1Env = ringDictionary[ring1ID].RingEnv;
                                var ring5Env = ringDictionary[ring5ID].RingEnv;
                                if (ring1Env.OxygenOutRing >= 2 && ring1Env.EtherOutRing >= 1 && ring1Env.Carbon_DoubleOxygensOutRing >= 1 &&
                                    ring5Env.EtherOutRing >= 1)
                                    descriptor.MDJQWFFIUHUJSB = 1;
                                if (ring1Env.EtherOutRing >= 1 && ring1Env.Carbon_DoubleOxygensOutRing >= 1 && ring5Env.EtherOutRing >= 1)
                                    descriptor.BHYGFFPNUSKIOG = 1;

                        }
                    }
                    break;
                case "C:C:C-C-C-C:C:C": //Medicarpin
                    if (bond2.IsSharedBondInRings && bond4.IsSharedBondInRings && bond6.IsSharedBondInRings) {
                        if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                            RingtypeChecker(bond3, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring2ID) &&
                            (RingtypeChecker(bond5, ringDictionary, RingFunctionType.Tetrahydropyran, out ring3ID) || 
                            RingtypeChecker(bond5, ringDictionary, RingFunctionType.DihydroPyran, out ring3ID)) &&
                            RingtypeChecker(atom8, ringDictionary, RingFunctionType.Benzene, out ring4ID)) {

                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            var ring3Env = ringDictionary[ring3ID].RingEnv;
                            var ring4Env = ringDictionary[ring4ID].RingEnv;

                            if (ring1Env.EtherOutRing >= 1 && ring4Env.OxygenOutRing >= 2)
                                descriptor.NSRJSISNDPOJOP = 1;

                        }
                    }
                    break;
                case "C-C:C:C:C:C:C:C": //Irilone

                    if (!bond1.IsInRing && bond4.IsSharedBondInRings && bond7.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out ring1ID) &&
                        RingtypeChecker(atom3, ringDictionary, RingFunctionType.AromaticPyran, out ring2ID) && 
                        RingtypeChecker(atom6, ringDictionary, RingFunctionType.Benzene, out ring3ID) &&
                        RingtypeChecker(bond7, ringDictionary, RingFunctionType.Dioxolane, out ring4ID)) {

                            var ring1Env = ringDictionary[ring1ID].RingEnv;
                            var ring2Env = ringDictionary[ring2ID].RingEnv;
                            var ring3Env = ringDictionary[ring3ID].RingEnv;
                            var ring4Env = ringDictionary[ring4ID].RingEnv;

                            if (ring1Env.OxygenOutRing >= 1 && ring2Env.KetonOutRing >= 1 && ring3Env.OxygenOutRing >= 3) {//Irilone
                                descriptor.NUGRQNBDTZWXTP = 1;
                            }
                    }

                    break;
             
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, 
            AtomProperty atom6, AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10, 
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5, 
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var ring5ID = -1;
            var ring6ID = -1;
            var targetRingID = -1;
            switch (connectString) {
                case "C-C-C-C-C-C-C-C-C-C": //Trinorlupeol
                    if (bond2.IsSharedBondInRings && bond4.IsSharedBondInRings && bond6.IsSharedBondInRings && bond8.IsSharedBondInRings &&
                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclopentene, out ring1ID) &&
                        RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID) &&
                        RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexane, out ring3ID) &&
                        RingtypeChecker(bond7, ringDictionary, RingFunctionType.Cyclohexane, out ring4ID) &&
                        RingtypeChecker(bond9, ringDictionary, RingFunctionType.Cyclohexane, out ring5ID)) {
                            if (ring2ID != ring3ID && ring3ID != ring4ID && ring4ID != ring5ID) {
                                if (atom2.AtomEnv.SingleBond_CarbonCount >= 4 && atom5.AtomEnv.SingleBond_CarbonCount >= 4 &&
                                    atom6.AtomEnv.SingleBond_CarbonCount >= 4 && atom8.AtomEnv.SingleBond_CarbonCount >= 4 &&
                                    atom10.AtomEnv.SingleBond_CarbonCount >= 4 && ringDictionary[ring5ID].RingEnv.OxygenOutRing >= 1)
                                    descriptor.HKKMGGKMNHKQMC = 1;
                            }
                    }
                    else if (bond2.IsSharedBondInRings && bond4.IsSharedBondInRings && bond6.IsSharedBondInRings &&
                         RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) &&
                         RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID) &&
                         RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexane, out ring3ID) &&
                         RingtypeChecker(bond7, ringDictionary, RingFunctionType.Cyclopentane, out ring4ID)) { //6-6-6-5 rings
                        #region

                        var ring1 = ringDictionary[ring1ID];
                        var ring2 = ringDictionary[ring2ID];
                        var ring3 = ringDictionary[ring3ID];
                        var ring4 = ringDictionary[ring4ID];

                        var ring1Env = ringDictionary[ring1ID].RingEnv;
                        var ring2Env = ringDictionary[ring2ID].RingEnv;
                        var ring3Env = ringDictionary[ring3ID].RingEnv;
                        var ring4Env = ringDictionary[ring4ID].RingEnv;

                        if (atom3.AtomEnv.SingleBond_CarbonCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 4 && 
                            atom9.AtomEnv.SingleBond_CarbonCount >= 3 && atom10.AtomEnv.SingleBond_CarbonCount >= 2) {
                            if (ring3Env.OxygenOutRing >= 1)
                                descriptor.RBTVCRWROCDIOQ = 1;
                            if (ring1Env.OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 1)
                                descriptor.WJXVLZCOWIIKOS = 1;
                        }
                        if (atom9.AtomEnv.SingleBond_OxygenCount >= 1 && ring1Env.OxygenOutRing >= 1 &&
                            ring2Env.OxygenOutRing >= 1 && ring3Env.OxygenOutRing >= 1 &&
                            atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4) {
                            descriptor.RKGHLTUWZNNHCL = 1;
                        }
                        #endregion
                    }
                    break;
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, string connectString, 
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, 
            AtomProperty atom6, AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10, 
            AtomProperty atom11, AtomProperty atom12, AtomProperty atom13, 
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5, 
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9, BondProperty bond10, 
            BondProperty bond11, BondProperty bond12, 
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary)
        {
            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var ring5ID = -1;
            var ring6ID = -1;
            var targetRingID = -1;

            if (bond2.IsSharedBondInRings && bond4.IsSharedBondInRings && bond6.IsSharedBondInRings &&
                        atom3.AtomEnv.SingleBond_CarbonCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 4 &&
                        atom9.AtomEnv.CarbonCount >= 3 && atom13.AtomEnv.CarbonCount >= 3 &&
                        (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) || RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexene, out ring1ID)) &&
                        (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID) || RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexene, out ring2ID)) &&
                        (RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexane, out ring3ID) || RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexene, out ring3ID)) && 
                        RingtypeChecker(bond7, ringDictionary, RingFunctionType.Cyclopentane, out ring4ID)) { //6-6-6-5 rings
                #region
                var ring1 = ringDictionary[ring1ID];
                var ring2 = ringDictionary[ring2ID];
                var ring3 = ringDictionary[ring3ID];
                var ring4 = ringDictionary[ring4ID];

                var ring1Env = ringDictionary[ring1ID].RingEnv;
                var ring2Env = ringDictionary[ring2ID].RingEnv;
                var ring3Env = ringDictionary[ring3ID].RingEnv;
                var ring4Env = ringDictionary[ring4ID].RingEnv;

                if (ring1.RingFunctionType == RingFunctionType.Cyclohexane && 
                    ring2.RingFunctionType == RingFunctionType.Cyclohexene && 
                    ring3.RingFunctionType == RingFunctionType.Cyclohexane) { //6-6(hexene)-6-5

                    descriptor.DTGDZMYNKLTSKC = 1;

                    if (ring1Env.OxygenOutRing >= 1) {
                        
                        if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Single &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Single) {

                            descriptor.HVYWMOMLDIMFJA = 1; //Cholesterol

                            if (atom12.AtomEnv.SingleBond_CarbonCount >= 3) { //Campersterol
                                descriptor.SGNBVLSWZMBQTH = 1;
                                var branchedAtoms = getBranchAtom(atom12, bond11, bond12, "C");
                                if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1) //ƒA-Sitosterol
                                    descriptor.KZJWDPNRJALLNS = 1;
                            }

                            if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings && atom10.AtomEnv.SingleBond_NitrogenCount >= 1 &&
                                RingtypeChecker(atom9, ringDictionary, RingFunctionType.Pyrrolidine, out ring5ID) &&
                                RingtypeChecker(atom11, ringDictionary, RingFunctionType.Piperidine, out ring6ID)) { //Solanidine
                                descriptor.JVKYZPBMZPJNAJ = 1;

                                if (ringDictionary[ring6ID].RingEnv.OxygenOutRing >= 1) { //Leptinidine
                                    descriptor.RFIYLZGMGGONQR = 1;
                                    var outsideAtoms = ringDictionary[ring6ID].RingEnv.OutsideAtomDictionary[atom11.AtomID];
                                    foreach (var oAtom in outsideAtoms) {
                                        if (oAtom.AtomString == "O") {
                                            var connectedBonds = oAtom.ConnectedBonds;
                                            foreach (var cBond in connectedBonds) {
                                                if (cBond.ConnectedAtoms.Count(n => n.AtomID == atom11.AtomID) >= 1) continue;
                                                var oConnectAtom = cBond.ConnectedAtoms.Where(n => n.AtomID != oAtom.AtomID).ToList()[0];
                                                if (oConnectAtom.AtomEnv.DoubleBond_CO_Count >= 1 && oConnectAtom.AtomEnv.SingleBond_CarbonCount >= 1)
                                                    descriptor.ZZYWILWDFCFHLX = 1; //Acetylleptinidine
                                            }
                                        }
                                    }
                                }
                            }
                            else if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings &&
                              atom10.AtomEnv.SingleBond_NitrogenCount >= 1 && atom10.AtomEnv.SingleBond_OxygenCount >= 1 &&
                              RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID) &&
                              RingtypeChecker(atom11, ringDictionary, RingFunctionType.Piperidine, out ring6ID)) { //Solasodine
                                descriptor.KWVISVAMQJWJSZ = 1;
                            }
                            else if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings &&
                                 atom10.AtomEnv.SingleBond_OxygenCount >= 2 &&
                                 RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID) &&
                                 RingtypeChecker(atom11, ringDictionary, RingFunctionType.Tetrahydropyran, out ring6ID)) { //6-6-6-5-furan-pyran
                                    descriptor.WQLVFSAGQJTQCK = 1; //Diosgenin
                                    if (ring1.RingEnv.OxygenOutRing >= 2) //Ruscogenin
                                        descriptor.QMQIQBOGXYYATH = 1;
                            }
                            else if (atom8.IsSharedAtomInRings && RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID)) {
                                if (atom10.AtomEnv.SingleBond_OxygenCount >= 2) {
                                    var branchedAtoms = getBranchAtom(atom13, bond12, "C");
                                    if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) { // Furost-5-en-3,22,26-triol
                                        descriptor.HHNBHWPLXYYKCZ = 1;
                                        if (ringDictionary[ring5ID].RingEnv.EtherOutRing >= 1) //22-Methoxy-furost-5-en-3,26-diol
                                            descriptor.ZHIBERYBFVVFNK = 1;
                                    }
                                }
                            }
                        }
                        else if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Double &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Single) {
                            var branchedAtoms = getBranchAtom(atom12, bond11, "C");
                            if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_CarbonCount >= 2) >= 1) // Stigmasterol
                                descriptor.HCXVJBMSMIARIN = 1;
                        }
                        else if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Single &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Double) {
                                if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4)
                                    descriptor.DICCPNLDOZNSML = 1; //Butyrospermol
                        }
                    }
                }
                else if (ring1.RingFunctionType == RingFunctionType.Cyclohexane && 
                    ring2.RingFunctionType == RingFunctionType.Cyclohexane && 
                    ring3.RingFunctionType == RingFunctionType.Cyclohexane) { //6-6-6-5

                    if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Single &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Single) {



                        if (ring1Env.OxygenOutRing >= 1) {
                            #region
                            if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings && atom10.AtomEnv.SingleBond_NitrogenCount >= 1 &&
                                RingtypeChecker(atom9, ringDictionary, RingFunctionType.Pyrrolidine, out ring5ID) &&
                                RingtypeChecker(atom11, ringDictionary, RingFunctionType.Piperidine, out ring6ID)) { //Demissidine
                                descriptor.JALVTHFTYRPDMB = 1;
                                if (ringDictionary[ring6ID].RingEnv.OxygenOutRing >= 1) { //Dihydroleptinidine
                                    descriptor.ATEWGTOGFJMCPH = 1;
                                    var outsideAtoms = ringDictionary[ring6ID].RingEnv.OutsideAtomDictionary[atom11.AtomID];
                                    foreach (var oAtom in outsideAtoms) {
                                        if (oAtom.AtomString == "O") {
                                            var connectedBonds = oAtom.ConnectedBonds;
                                            foreach (var cBond in connectedBonds) {
                                                if (cBond.ConnectedAtoms.Count(n => n.AtomID == atom11.AtomID) >= 1) continue;
                                                var oConnectAtom = cBond.ConnectedAtoms.Where(n => n.AtomID != oAtom.AtomID).ToList()[0];
                                                if (oConnectAtom.AtomEnv.DoubleBond_CO_Count >= 1 && oConnectAtom.AtomEnv.SingleBond_CarbonCount >= 1)
                                                    descriptor.CLRSTYUKOZOYFN = 1; //Acetyldihydroleptinidine
                                            }
                                        }
                                    }
                                }
                            }
                            else if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings &&
                                 atom10.AtomEnv.SingleBond_OxygenCount >= 2 &&
                                 RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID) &&
                                 RingtypeChecker(atom11, ringDictionary, RingFunctionType.Tetrahydropyran, out ring6ID)) { //6-6-6-5-furan-pyran

                                     if (atom13.AtomEnv.DoubleBond_CC_Count >= 1) //Spirost-25(27)-en-3-ol
                                         descriptor.GFGFAMNBRXAQGB = 1;
                                     else if (atom13.AtomEnv.DoubleBond_CC_Count == 0) //Sarsasapogenin
                                         descriptor.GMBQZIIUCVWOCD = 1;
                                     if (ring3Env.KetonOutRing >= 1) //3-Hydroxy-spirostan-12-one
                                         descriptor.QOLRLLFJMZLYQJ = 1;
                                     if (atom8.AtomEnv.SingleBond_OxygenCount >= 1)//Spirostan-3,17-diol
                                         descriptor.IIASKQTXRIMIGU = 1;
                                     if (ring2Env.OxygenOutRing >= 1) //Spirostan-3,6-diol
                                         descriptor.PZNPHSFXILSZTM = 1;
                                     if (ring1Env.EtherOutRing >= 2 && atom8.AtomEnv.OxygenCount >= 1 && ring3Env.KetonOutRing >= 1) //3,3-Dimethoxy-17-hydroxy-spirostan-3-al-12-one
                                         descriptor.FJZMHTUXQXEAPK = 1;
                                
                            }
                            else if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings &&
                              atom10.AtomEnv.SingleBond_NitrogenCount >= 1 && atom10.AtomEnv.SingleBond_OxygenCount >= 1 &&
                              RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID) &&
                              RingtypeChecker(atom11, ringDictionary, RingFunctionType.Piperidine, out ring6ID)) { //Soladulcidine
                                descriptor.XYNPYHXGMWJBLV = 1;
                            }
                            else if (atom8.IsSharedAtomInRings && RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID)) {
                                if (atom10.AtomEnv.SingleBond_OxygenCount >= 2) {
                                    var branchedAtoms = getBranchAtom(atom13, bond12, "C");
                                    if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) { // Furostan-3,22,26-triol
                                        descriptor.BLAWZIKKZHLPBD = 1;
                                        if (ringDictionary[ring5ID].RingEnv.EtherOutRing >= 1) //22-Methoxy-furostan-3,26-diol
                                            descriptor.XGSSLVNRUAYTBR = 1;
                                    }
                                }
                            }
                            #endregion
                        }
                    }
                    else if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Double && bond10.BondType == BondType.Single &&
                           bond11.BondType == BondType.Single && bond12.BondType == BondType.Single) {
                               if (atom8.IsSharedAtomInRings && RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID)) {
                                   var branchedAtoms = getBranchAtom(atom13, bond12, "C");
                                   if (branchedAtoms.Count(n => n.AtomEnv.SingleBond_OxygenCount >= 1) >= 1) { // Furost-20(22)-en-26-ol
                                       descriptor.ZIIVVHSRXUVJGR = 1;
                                       if (ringDictionary[ring1ID].RingEnv.OxygenOutRing >= 1) //Furost-20(22)-en-3,26-diol
                                           descriptor.IQDKIMJGXXRZGR = 1;
                                   }
                               }
                    }
                    else if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Single &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Double) {
                                if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4) {
                                    if (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclopropane, out ring5ID)) {
                                        descriptor.ONQRKEUAIJMULO = 1; //Cycloartenol
                                    }
                                }
                    }


                }
                else if (ring1.RingFunctionType == RingFunctionType.Cyclohexane && 
                    ring2.RingFunctionType == RingFunctionType.Cyclohexene &&
                    ring3.RingFunctionType == RingFunctionType.Cyclohexene) {//6-6(hexene)-6(hexene)-5
                    if (ring1Env.OxygenOutRing >= 1) {

                        if (bond8.BondType == BondType.Single && bond9.BondType == BondType.Single && bond10.BondType == BondType.Single &&
                            bond11.BondType == BondType.Single && bond12.BondType == BondType.Double) {
                                if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4)
                                    descriptor.CAHGCLMLTWQZNJ = 1; //Lanosterol
                        }
                    }
                }
                else if (ring1.RingFunctionType == RingFunctionType.Cyclohexene &&
                    ring2.RingFunctionType == RingFunctionType.Cyclohexane &&
                    ring3.RingFunctionType == RingFunctionType.Cyclohexane) {//6(hexene)-6-6-5
                    if (ring1Env.OxygenOutRing >= 1) {

                        if (atom8.IsSharedAtomInRings && atom10.IsSharedAtomInRings &&
                                 atom10.AtomEnv.SingleBond_OxygenCount >= 2 &&
                                 RingtypeChecker(atom9, ringDictionary, RingFunctionType.Tetrahydrofuran, out ring5ID) &&
                                 RingtypeChecker(atom11, ringDictionary, RingFunctionType.Tetrahydropyran, out ring6ID)) { //6-6-6-5-furan-pyran
                                     if (ring1Env.KetonOutRing >= 1 && ring3Env.OxygenOutRing >= 1 && atom8.AtomEnv.SingleBond_OxygenCount >= 1) //12,17-Dihydroxy-spirost-4-en-3-one
                                         descriptor.YWWQQPDLTAKFSH = 1;
                        }
                    }
                }
                #endregion
            }
            else if (bond2.IsSharedBondInRings && bond4.IsSharedBondInRings && bond6.IsSharedBondInRings &&
                        bond8.IsSharedBondInRings &&

                        RingtypeChecker(atom1, ringDictionary, RingFunctionType.Cyclohexane, out ring1ID) &&

                        (RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexane, out ring2ID) ||
                        RingtypeChecker(bond3, ringDictionary, RingFunctionType.Cyclohexene, out ring2ID)) &&

                        (RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexane, out ring3ID) ||
                        RingtypeChecker(bond5, ringDictionary, RingFunctionType.Cyclohexene, out ring3ID)) &&

                        RingtypeChecker(bond7, ringDictionary, RingFunctionType.Cyclohexane, out ring4ID) &&

                        (RingtypeChecker(bond9, ringDictionary, RingFunctionType.Cyclohexane, out ring5ID) ||
                        RingtypeChecker(bond9, ringDictionary, RingFunctionType.Cyclohexene, out ring5ID) ||
                        RingtypeChecker(bond9, ringDictionary, RingFunctionType.Cyclopentane, out ring5ID))
                ) {

                    var ring1 = ringDictionary[ring1ID];
                    var ring2 = ringDictionary[ring2ID];
                    var ring3 = ringDictionary[ring3ID];
                    var ring4 = ringDictionary[ring4ID];
                    var ring5 = ringDictionary[ring5ID];

                    var ring1Env = ringDictionary[ring1ID].RingEnv;
                    var ring2Env = ringDictionary[ring2ID].RingEnv;
                    var ring3Env = ringDictionary[ring3ID].RingEnv;
                    var ring4Env = ringDictionary[ring4ID].RingEnv;
                    var ring5Env = ringDictionary[ring5ID].RingEnv;

                    if (ring2.RingFunctionType == RingFunctionType.Cyclohexane &&
                            ring3.RingFunctionType == RingFunctionType.Cyclohexane &&
                            ring5.RingFunctionType == RingFunctionType.Cyclohexane) { //6-6-6-6-6
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.SingleBond_CarbonCount >= 3 &&
                            atom13.AtomEnv.SingleBond_CarbonCount >= 3) {
                            if (ring1Env.OxygenOutRing >= 1)
                                descriptor.XWMMEBCFHUKHEX = 1; //Taraxasterol
                        }
                    }
                    else if (ring2.RingFunctionType == RingFunctionType.Cyclohexene &&
                        ring3.RingFunctionType == RingFunctionType.Cyclohexane &&
                        ring5.RingFunctionType == RingFunctionType.Cyclohexane) { //6-6(hexene)-6-6-6
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom6.AtomEnv.SingleBond_CarbonCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.SingleBond_CarbonCount >= 3 &&
                            atom13.AtomEnv.SingleBond_CarbonCount >= 3) {
                                if (ring1Env.OxygenOutRing >= 1)
                                    descriptor.TZVDWGXUGGUMCE = 1; //Bauerenol
                        }
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom6.AtomEnv.SingleBond_CarbonCount >= 4 && atom7.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.SingleBond_CarbonCount >= 4) {
                            if (ring1Env.OxygenOutRing >= 1)
                                descriptor.ZDFUASMRJUVZJP = 1; //Multiflorenol
                        }
                    }
                    else if (ring2.RingFunctionType == RingFunctionType.Cyclohexane &&
                        ring3.RingFunctionType == RingFunctionType.Cyclohexane &&
                        ring5.RingFunctionType == RingFunctionType.Cyclohexene) { //6-6-6-6-6(hexene)
                        
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.CarbonCount >= 3 &&
                            atom13.AtomEnv.SingleBond_CarbonCount >= 3) {
                            if (ring1Env.OxygenOutRing >= 1)
                                descriptor.NGFFRJBGMSPDMS = 1; //ƒµ-Taraxasterol
                        }
                        else if(atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.SingleBond_CarbonCount >= 4) {
                                if (ring1Env.OxygenOutRing >= 1)
                                    descriptor.QMUXVPRGNJLGRT = 1; //Germanicol
                        }
                    }
                    else if (ring2.RingFunctionType == RingFunctionType.Cyclohexane &&
                            ring3.RingFunctionType == RingFunctionType.Cyclohexene &&
                            ring5.RingFunctionType == RingFunctionType.Cyclohexane) { //6-6-6(hexene)-6-6
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom12.AtomEnv.CarbonCount >= 4) {
                                if (ring1Env.OxygenOutRing >= 1) {
                                    descriptor.JFSHUTJDVKUMTJ = 1; //ƒA-Amyrin
                                    if (ring1.RingEnv.Carbon_OxygenOutRing >= 1) //24-Hydroxy-ƒA-amyrin
                                        descriptor.NTWLPZMPTFQYQI = 1;
                                    if (ring4Env.Carbon_OxygenOutRing >= 1) //Erythrodiol
                                        descriptor.PSZDOEIIIJFCFE = 1;
                                    if (atom10.AtomEnv.SingleBond_OxygenCount >= 1)
                                        descriptor.ZEGUWBQDYDXBNS = 1;
                                    if (ring5Env.Carbon_DoubleOxygensOutRing >= 1) //11-Deoxyo glycyrrhizic acid
                                        descriptor.JZFSMVXQUWRSIW = 1;
                                    if (ring1Env.Carbon_OxygenOutRing >= 1 && ring5Env.KetonOutRing >= 1) //Soyasapogenol E
                                        descriptor.FNRBOAGVUNHDIL = 1;
                                    if (ring4Env.Carbon_DoubleOxygensOutRing >= 1 && ring5Env.Carbon_DoubleOxygensOutRing >= 1) { //Oleanolic acid
                                        descriptor.MIJYXULNPSFWEK = 1;
                                        if (ring1Env.Carbon_KetoneOutRing >= 1) //Gypsogenin
                                            descriptor.QMHCWDVPABYZMC = 1;
                                        if (ring1Env.OxygenOutRing >= 2) { //2-Hydroxyoleanolic acid
                                            descriptor.MDZKJHQSJHYOHJ = 1;
                                            if (ring1Env.Carbon_KetoneOutRing >= 1) //Polygalagenin
                                                descriptor.RCOKCENREZHIEA = 1;
                                            if (ring1Env.Carbon_OxygenOutRing >= 1) //Bayogenin
                                                descriptor.RWNHLTKFBKYDOJ = 1;
                                            if (ring1Env.Carbon_DoubleOxygensOutRing >= 1) { //Medicagenic acid
                                                descriptor.IDGXIXSKISLYAC = 1;
                                                if (ring4Env.OxygenOutRing >= 1) //Zanhic acid
                                                    descriptor.PLERLWUIYWAWRU = 1;
                                            }

                                        }
                                        if (ring1Env.Carbon_OxygenOutRing >= 1) //Hederagenin
                                            descriptor.PGOYMURMZNDHNS = 1;
                                        if (ring1Env.Carbon_DoubleOxygensOutRing >= 1) //Gypsogenic acid
                                            descriptor.PAIBKVQNJKUVCE = 1;
                                    }
                                    if (ring1Env.Carbon_OxygenOutRing >= 1 && atom10.AtomEnv.SingleBond_OxygenCount >= 1) //Soyasapogenol B
                                        descriptor.YOQAQNKGFOLRGT = 1;
                                    if (ring3Env.KetonOutRing >= 1 && ring5Env.Carbon_DoubleOxygensOutRing >= 1) //Glycyrrhetinic acid
                                        descriptor.MPDGHEJMBKOTSU = 1;
                                    if (ring5Env.OxygenOutRing >= 2 && ring1Env.Carbon_OxygenOutRing >= 1) //Soyasapogenol A
                                        descriptor.CDDWAYFUFNQLRZ = 1;
                                    if (ring1Env.Carbon_OxygenOutRing >= 1 && ring3Env.KetonOutRing >= 1 && ring5Env.Carbon_DoubleOxygensOutRing >= 1) //24-Hydroxy glycyrrhizic acid
                                        descriptor.GSEPOEIKWTXTHS = 1;
                                }
                        }
                    }
                    else if (ring2.RingFunctionType == RingFunctionType.Cyclohexane &&
                            ring3.RingFunctionType == RingFunctionType.Cyclohexane &&
                            ring5.RingFunctionType == RingFunctionType.Cyclopentane) { //6-6-6-6-5
                        if (atom1.AtomEnv.SingleBond_CarbonCount >= 4 && atom3.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom5.AtomEnv.SingleBond_CarbonCount >= 4 && atom6.AtomEnv.SingleBond_CarbonCount >= 4 &&
                            atom9.AtomEnv.SingleBond_CarbonCount >= 4 && atom13.AtomEnv.CarbonCount >= 3) {
                            if (ring1Env.OxygenOutRing >= 1)
                                descriptor.MQYXUWHLBZFQQO = 1; //Lupeol
                        }
                    }
            }
        }

        public static void SetProperties(MolecularFingerprint descriptor, List<AtomProperty> atoms, List<BondProperty> bonds,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, int chainLength)
        {
            var targetRingID = -1;
            var atom1 = atoms[0];
            if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {
                var ringEnv = ringDictionary[targetRingID].RingEnv;
                if (chainLength == 16) {
                    if (ringEnv.OxygenOutRing >= 1)
                        descriptor.PTFIPECGHSYQNR = 1;
                    if (ringEnv.EtherOutRing >= 1)
                        descriptor.KFSQTHOUPXNGQR = 1;
                    if (ringEnv.OxygenOutRing >= 1 && ringEnv.Carbon_DoubleOxygensOutRing >= 1)
                        descriptor.ADFWQBGTDJIESE = 1;
                }
            }
        }
        #endregion

        #region long chain acyl descriptor
        public static void LongChainAcylDescriptors(MolecularFingerprint descriptor, string connectString,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5,
            AtomProperty atom6, AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10,
            AtomProperty atom11,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9, BondProperty bond10,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary) {

            var ring1ID = -1;
            var ring2ID = -1;
            var ring3ID = -1;
            var ring4ID = -1;
            var ring5ID = -1;
            var targetRingID = -1;

            switch (connectString) {
                case "C-C-C-C-C-C-C-C-C-C-C":
                    #region
                    if (RingtypeChecker(atom1, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;
                        if (ringEnv.OxygenOutRing >= 1)
                            descriptor.HRMIBYZWBMSUHC = 1;
                    }





                    break;
                    #endregion
            }

        }

        public static void LongChainAcylDescriptors(MolecularFingerprint descriptor, string connectString,
           AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5,
           AtomProperty atom6, AtomProperty atom7, AtomProperty atom8, AtomProperty atom9,
           BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
           BondProperty bond6, BondProperty bond7, BondProperty bond8,
           Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary) {
            var ring1ID = -1;
            var ring2ID = -1;
            var targetRingID = -1;

            switch (connectString) {
                case "C-C-C-C-C-C-C-C-C":
                    #region
                    if (RingtypeChecker(atom9, ringDictionary, RingFunctionType.Benzene, out targetRingID)) {

                        var ringEnv = ringDictionary[targetRingID].RingEnv;

                        if (ringEnv.OxygenOutRing >= 1) {
                            if (atom1.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.CZECIKXGUYQZJL = 1;
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.SLVFVSVYCYGPJP = 1;
                            if (atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.DYKBZKYHZBBMFK = 1;
                        }
                        if (ringEnv.EtherOutRing >= 1) {
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                                descriptor.IRIFVIYTQGKTLY = 1;
                            if (atom1.AtomEnv.SingleBond_OxygenCount >= 1 && atom1.AtomEnv.DoubleBond_CO_Count >= 1)
                                descriptor.JFPANMGCHQEUEM = 1;
                        }
                        if (ringEnv.Carbon_DoubleOxygensOutRing >= 1 && ringEnv.EtherOutRing >= 1 &&
                            atom1.AtomEnv.DoubleBond_CO_Count >= 1)
                            descriptor.ASNCGFWZNUIUHX = 1;
                        if (ringEnv.Carbon_KetoneOutRing >= 1 && ringEnv.EtherOutRing >= 1 &&
                           atom1.AtomEnv.DoubleBond_CO_Count >= 1 && atom1.AtomEnv.SingleBond_OxygenCount >= 1)
                            descriptor.DRJSFRIXGBWQQP = 1;
                    }
                    break;
                case "C-C=C-C-C-C-C-C-C":
                    if (atom4.AtomEnv.SingleBond_OxygenCount >= 1)
                        descriptor.ZIWXVGHUDKNNSH = 1;
                    break;
                    #endregion
            }
        }
        #endregion

        #region fatty acid descriptors
        public static void FattyAcidsDescriptors(MolecularFingerprint descriptors, List<AtomProperty> atomProperties, List<BondProperty> bondProperties)
        {
            var chainLength = atomProperties.Count;
            var doubleBonds = bondProperties.Count(n => n.BondType == BondType.Double);
            var acylString = chainLength.ToString() + "_" + doubleBonds.ToString();
            var terminalAtom = atomProperties[0];
            var isTerminalKetone = terminalAtom.AtomEnv.DoubleBond_CO_Count == 1 ? true : false;
            var isTerminalSingleBondO = terminalAtom.AtomEnv.SingleBond_OxygenCount == 1 ? true : false;
            var isTerminalCarboxylate = isTerminalKetone && isTerminalSingleBondO ? true : false;

            if (!isTerminalKetone) return;

            //branch H num and O num and C num
            var branchH = 0;
            var branchC = 0;
            var branchO = 0;

            for (int i = 1; i < atomProperties.Count; i++) {
                var atom = atomProperties[i];
                if (atom.ConnectedBonds.Count != atom.AtomEnv.CarbonCount + atom.AtomEnv.HydrogenCount + atom.AtomEnv.OxygenCount) return;

                branchH += atom.AtomEnv.SingleBond_HydrogenCount;
                branchO += atom.AtomEnv.OxygenCount;
                if (i == atomProperties.Count - 1)
                    branchC += atom.AtomEnv.CarbonCount - 1;
                else
                    branchC += atom.AtomEnv.CarbonCount - 2;
            }

            if (branchO > 1) return;

            if (isTerminalKetone && branchO == 0) {
                switch (acylString) {
                    #region
                    case "2_0": descriptors.IKHGUXGNUITLKF = 1; break; //Fatty acid 2:0-O
                    case "3_0": descriptors.NBBJYMSMWIIQGU = 1; break; //Fatty acid 3:0-O
                    case "4_0": descriptors.ZTQSAGDEMFDKMZ = 1; break; //Fatty acid 4:0-O
                    case "5_0": descriptors.HGBOYTHUEUWSSQ = 1; break; //Fatty acid 5:0-O
                    case "6_0": descriptors.JARKCYVAAOWBJS = 1; break; //Fatty acid 6:0-O
                    case "7_0": descriptors.FXHGMKSSBGDXIY = 1; break; //Fatty acid 7:0-O
                    case "8_0": descriptors.NUJGJRNETVAIRJ = 1; break; //Fatty acid 8:0-O
                    case "9_0": descriptors.GYHFUZHODSMOHU = 1; break; //Fatty acid 9:0-O
                    case "10_0": descriptors.KSMVZQYAVGTKIV = 1; break; //Fatty acid 10:0-O
                    case "11_0": descriptors.KMPQYAYAQWNLME = 1; break; //Fatty acid 11:0-O
                    case "12_0": descriptors.HFJRKMMYBMWEAD = 1; break; //Fatty acid 12:0-O
                    case "13_0": descriptors.BGEHHAVMRVXCGR = 1; break; //Fatty acid 13:0-O
                    case "13_1": descriptors.VNGRIQMEDUZTCJ = 1; break; //Fatty acid 13:1-O
                    case "14_0": descriptors.UHUFTBALEZWWIH = 1; break; //Fatty acid 14:0-O
                    case "14_1": descriptors.ANJAOCICJSRZSR = 1; break; //Fatty acid 14:1-O
                    case "15_0": descriptors.XGQJZNCFDLXSIJ = 1; break; //Fatty acid 15:0-O
                    case "15_1": descriptors.ILDMCKZJSLWXTH = 1; break; //Fatty acid 15:1-O
                    case "16_0": descriptors.NIOYUNMRJMEDGI = 1; break; //Fatty acid 16:0-O
                    case "16_1": descriptors.QFPVVMKZTVQDTL = 1; break; //Fatty acid 16:1-O
                    case "16_2": descriptors.KMGXOQZDOIYYBO = 1; break; //Fatty acid 16:2-O
                    case "16_3": descriptors.USMAGXMGZXKLHT = 1; break; //Fatty acid 16:3-O
                    case "16_4": descriptors.MCJZAFLSUPAMEV = 1; break; //Fatty acid 16:4-O
                    case "17_0": descriptors.PIYDVAYKYBWPPY = 1; break; //Fatty acid 17:0-O
                    case "17_1": descriptors.HJEWKIMQTQIIIN = 1; break; //Fatty acid 17:1-O
                    case "17_2": descriptors.SORBVCZQFFPNBZ = 1; break; //Fatty acid 17:2-O
                    case "18_0": descriptors.FWWQKRXKHIRPJY = 1; break; //Fatty acid 18:0-O
                    case "18_1": descriptors.ZENZJGDPWWLORF = 1; break; //Fatty acid 18:1-O
                    case "18_2": descriptors.HXLZULGRVFOIDK = 1; break; //Fatty acid 18:2-O
                    case "18_3": descriptors.TUCMDDWTBVMRTP = 1; break; //Fatty acid 18:3-O
                    case "18_4": descriptors.VWXPMSHGZMFPMK = 1; break; //Fatty acid 18:4-O
                    case "18_5": descriptors.IITKKCIKFTVFKR = 1; break; //Fatty acid 18:5-O
                    case "19_0": descriptors.SXIYYZWCMUFWBW = 1; break; //Fatty acid 19:0-O
                    case "19_1": descriptors.AKQYTSLMKKEXOK = 1; break; //Fatty acid 19:1-O
                    case "19_2": descriptors.GPXKCXWIFRWJHH = 1; break; //Fatty acid 19:2-O
                    case "20_0": descriptors.FWBUWJHWAKTPHI = 1; break; //Fatty acid 20:0-O
                    case "20_1": descriptors.WJLJKNYXWACGHC = 1; break; //Fatty acid 20:1-O
                    case "20_2": descriptors.SYNQCAFPBFGQPP = 1; break; //Fatty acid 20:2-O
                    case "20_3": descriptors.GVYIFDMCYYJQNW = 1; break; //Fatty acid 20:3-O
                    case "20_4": descriptors.XOZDREDGDDNHBM = 1; break; //Fatty acid 20:4-O
                    case "20_5": descriptors.KZSKFOJHFUBFSD = 1; break; //Fatty acid 20:5-O
                    case "21_0": descriptors.FJZCFGKQFDPNHS = 1; break; //Fatty acid 21:0-O
                    case "21_1": descriptors.ZPLPTXJQMQAZPW = 1; break; //Fatty acid 21:1-O
                    case "21_2": descriptors.RQBMSYDMYCXPOK = 1; break; //Fatty acid 21:2-O
                    case "22_0": descriptors.ULCXRAFXRZTNRO = 1; break; //Fatty acid 22:0-O
                    case "22_1": descriptors.YBXUDVOPLYWFSH = 1; break; //Fatty acid 22:1-O
                    case "22_2": descriptors.GDFMOYUXMKMYJF = 1; break; //Fatty acid 22:2-O
                    case "22_3": descriptors.OHNHBFLLYAQIOG = 1; break; //Fatty acid 22:3-O
                    case "22_4": descriptors.BLEIOPIOOBZCEQ = 1; break; //Fatty acid 22:4-O
                    case "22_5": descriptors.XCDIKQSRUWRPHH = 1; break; //Fatty acid 22:5-O
                    case "22_6": descriptors.OKFGGODLENCAOQ = 1; break; //Fatty acid 22:6-O
                    case "23_0": descriptors.IALIDHPAWNTXOK = 1; break; //Fatty acid 23:0-O
                    case "24_0": descriptors.HGINZVDZNQJVLQ = 1; break; //Fatty acid 24:0-O
                    case "25_0": descriptors.HAGKFWXVDSAFHB = 1; break; //Fatty acid 25:0-O
                    case "26_0": descriptors.QAXXQMIHMLTJQI = 1; break; //Fatty acid 26:0-O
                    case "27_0": descriptors.UEAAOADMOOTTQM = 1; break; //Fatty acid 27:0-O
                    case "28_0": descriptors.LVXORIXZNUNHGQ = 1; break; //Fatty acid 28:0-O
                    case "29_0": descriptors.AUSHGUYKHVWAKG = 1; break; //Fatty acid 29:0-O
                    case "30_0": descriptors.CGNVIRPGBUXJES = 1; break; //Fatty acid 30:0-O
                    case "31_0": descriptors.WBCFUJSINLYPNY = 1; break; //Fatty acid 31:0-O
                    case "32_0": descriptors.NNZVKPZICXRDJI = 1; break; //Fatty acid 32:0-O
                    case "33_0": descriptors.FPPJXOQMWRDTCG = 1; break; //Fatty acid 33:0-O
                    case "34_0": descriptors.ZSRZISBVEUGQRU = 1; break; //Fatty acid 34:0-O
                    case "35_0": descriptors.AAWVBAITTDKDJP = 1; break; //Fatty acid 35:0-O
                    case "36_0": descriptors.XHHLBVJECNRERM = 1; break; //Fatty acid 36:0-O
                    case "37_0": descriptors.NQNSAAVEWWLPEZ = 1; break; //Fatty acid 37:0-O
                    case "38_0": descriptors.OKYUSFSRNKXIQJ = 1; break; //Fatty acid 38:0-O
                    case "39_0": descriptors.YIVPVEUBUPQPMV = 1; break; //Fatty acid 39:0-O
                    case "40_0": descriptors.UEIBUXBHZHEFSR = 1; break; //Fatty acid 40:0-O
                    case "41_0": descriptors.IANNWYUKEQXKQQ = 1; break; //Fatty acid 41:0-O
                    case "42_0": descriptors.JPVKCBUGZAKQOC = 1; break; //Fatty acid 42:0-O
                    case "43_0": descriptors.SZIIOZOEWWDAIV = 1; break; //Fatty acid 43:0-O
                    case "44_0": descriptors.USUMOPGSPDLELV = 1; break; //Fatty acid 44:0-O
                    case "45_0": descriptors.LOPVYXZVTLQFQF = 1; break; //Fatty acid 45:0-O
                    case "46_0": descriptors.QCPOTKFRGCXZFU = 1; break; //Fatty acid 46:0-O

                    #endregion
                }
            }
            else if (isTerminalKetone && branchO == 1) {
                switch (acylString) {
                    #region
                    case "2_0": descriptors.WGCNASOHLSPBMP = 1; break; //Hydroxy fatty acid 2:0-O
                    case "3_0": descriptors.AKXKFZDCRYJKTF = 1; break; //Hydroxy fatty acid 3:0-O
                    case "4_0": descriptors.PIAOXUVIBAKVSP = 1; break; //Hydroxy fatty acid 4:0-O
                    case "5_0": descriptors.CNRGMQRNYAIBTN = 1; break; //Hydroxy fatty acid 5:0-O
                    case "6_0": descriptors.YRMUTQRWVGDUBW = 1; break; //Hydroxy fatty acid 6:0-O
                    case "7_0": descriptors.UKFNNBXGHVJQMJ = 1; break; //Hydroxy fatty acid 7:0-O
                    case "8_0": descriptors.QVGWQEOFNGJAOO = 1; break; //Hydroxy fatty acid 8:0-O
                    case "9_0": descriptors.PBZLKHXTZLJSSO = 1; break; //Hydroxy fatty acid 9:0-O
                    case "10_0": descriptors.AASQBYVPOFXFGU = 1; break; //Hydroxy fatty acid 10:0-O
                    case "11_0": descriptors.IOKPZOGAWQXXLF = 1; break; //Hydroxy fatty acid 11:0-O
                    case "12_0": descriptors.NGBPDDMTRGHYFM = 1; break; //Hydroxy fatty acid 12:0-O
                    case "13_0": descriptors.NAMFFBYYOKLDOM = 1; break; //Hydroxy fatty acid 13:0-O
                    case "13_1": descriptors.UYTDVOAIGROZFX = 1; break; //Hydroxy fatty acid 13:1-O
                    case "14_0": descriptors.ZTKXZDLAHNDYCJ = 1; break; //Hydroxy fatty acid 14:0-O
                    case "14_1": descriptors.OELPASIFBUUFIQ = 1; break; //Hydroxy fatty acid 14:1-O
                    case "15_0": descriptors.PXBOJWNZUUEBDU = 1; break; //Hydroxy fatty acid 15:0-O
                    case "15_1": descriptors.JQYGCQRQGRVLDN = 1; break; //Hydroxy fatty acid 15:1-O
                    case "16_0": descriptors.GBOCFSKPHBCOCX = 1; break; //Hydroxy fatty acid 16:0-O
                    case "16_1": descriptors.BXDTULNYBZHGMC = 1; break; //Hydroxy fatty acid 16:1-O
                    case "16_2": descriptors.IKDNWWNNSBFRPD = 1; break; //Hydroxy fatty acid 16:2-O
                    case "16_3": descriptors.XIOAXVAJCXMAIV = 1; break; //Hydroxy fatty acid 16:3-O
                    case "16_4": descriptors.YMDRHCPLTCCRLX = 1; break; //Hydroxy fatty acid 16:4-O
                    case "17_0": descriptors.XZNLDDNERIACTN = 1; break; //Hydroxy fatty acid 17:0-O
                    case "17_1": descriptors.FEJABJBBRPQHRG = 1; break; //Hydroxy fatty acid 17:1-O
                    case "17_2": descriptors.FXLBBNFRKMTPNR = 1; break; //Hydroxy fatty acid 17:2-O
                    case "18_0": descriptors.PZLYXEVGRAQCLK = 1; break; //Hydroxy fatty acid 18:0-O
                    case "18_1": descriptors.WXMZNRDJAZOZCO = 1; break; //Hydroxy fatty acid 18:1-O
                    case "18_2": descriptors.XDTCKKYGZMYRFG = 1; break; //Hydroxy fatty acid 18:2-O
                    case "18_3": descriptors.AYEXUOCADOPDOM = 1; break; //Hydroxy fatty acid 18:3-O
                    case "18_4": descriptors.DEEQOBNVYSHJLG = 1; break; //Hydroxy fatty acid 18:4-O
                    case "18_5": descriptors.SXRXBWFMAYCJSP = 1; break; //Hydroxy fatty acid 18:5-O
                    case "19_0": descriptors.RQRCANBXSSHEIY = 1; break; //Hydroxy fatty acid 19:0-O
                    case "19_1": descriptors.OBIITBOBZMXSFO = 1; break; //Hydroxy fatty acid 19:1-O
                    case "19_2": descriptors.DGXSKDFLQFJHGI = 1; break; //Hydroxy fatty acid 19:2-O
                    case "20_0": descriptors.NJWDYOOPZZRUMD = 1; break; //Hydroxy fatty acid 20:0-O
                    case "20_1": descriptors.MHIXDXZRIVKKHP = 1; break; //Hydroxy fatty acid 20:1-O
                    case "20_2": descriptors.JNCNZITXCFDRQT = 1; break; //Hydroxy fatty acid 20:2-O
                    case "20_3": descriptors.KHZUFLNDJSDGNR = 1; break; //Hydroxy fatty acid 20:3-O
                    case "20_4": descriptors.WRAABDGBHRJLHR = 1; break; //Hydroxy fatty acid 20:4-O
                    case "20_5": descriptors.KQCIODJBABTUAA = 1; break; //Hydroxy fatty acid 20:5-O
                    case "21_0": descriptors.AHMWOMXSNXJDGM = 1; break; //Hydroxy fatty acid 21:0-O
                    case "21_1": descriptors.AQAQOOGENKQRTR = 1; break; //Hydroxy fatty acid 21:1-O
                    case "21_2": descriptors.IXGRSGKLNDEHCM = 1; break; //Hydroxy fatty acid 21:2-O
                    case "22_0": descriptors.MKQYREVLMZYGEL = 1; break; //Hydroxy fatty acid 22:0-O
                    case "22_1": descriptors.JKCLGQXEZFEMHP = 1; break; //Hydroxy fatty acid 22:1-O
                    case "22_2": descriptors.UPMVPXJUQWHITN = 1; break; //Hydroxy fatty acid 22:2-O
                    case "22_3": descriptors.YDJRNYWFRANPOL = 1; break; //Hydroxy fatty acid 22:3-O
                    case "22_4": descriptors.SCOXRLWYDFLQPP = 1; break; //Hydroxy fatty acid 22:4-O
                    case "22_5": descriptors.HRQBVNFDMJPOBV = 1; break; //Hydroxy fatty acid 22:5-O
                    case "22_6": descriptors.PHIOZXXTYNODED = 1; break; //Hydroxy fatty acid 22:6-O

                    #endregion
                }
            }

            if (isTerminalCarboxylate && branchO == 0) {
                switch (acylString) {
                    #region
                    case "2_0": descriptors.QTBSBXVTEAMEQO = 1; break; //Fatty acid 2:0
                    case "3_0": descriptors.XBDQKXXYIPTUBI = 1; break; //Fatty acid 3:0
                    case "4_0": descriptors.FERIUCNNQQJTOY = 1; break; //Fatty acid 4:0
                    case "5_0": descriptors.NQPDZGIKBAWPEJ = 1; break; //Fatty acid 5:0
                    case "6_0": descriptors.FUZZWVXGSFPDMH = 1; break; //Fatty acid 6:0
                    case "7_0": descriptors.MNWFXJYAOYHMED = 1; break; //Fatty acid 7:0
                    case "8_0": descriptors.WWZKQHOCKIZLMA = 1; break; //Fatty acid 8:0
                    case "9_0": descriptors.FBUKVWPVBMHYJY = 1; break; //Fatty acid 9:0
                    case "10_0": descriptors.GHVNFZFCNZKVNT = 1; break; //Fatty acid 10:0
                    case "11_0": descriptors.ZDPHROOEEOARMN = 1; break; //Fatty acid 11:0
                    case "12_0": descriptors.POULHZVOKOAJMA = 1; break; //Fatty acid 12:0
                    case "13_0": descriptors.SZHOJFHSIKHZHA = 1; break; //Fatty acid 13:0
                    case "13_1": descriptors.KIWIPIAOWPKUNM = 1; break; //Fatty acid 13:1
                    case "14_0": descriptors.TUNFSRHWOTWDNC = 1; break; //Fatty acid 14:0
                    case "14_1": descriptors.YWWVWXASSLXJHU = 1; break; //Fatty acid 14:1
                    case "15_0": descriptors.WQEPLUUGTLDZJY = 1; break; //Fatty acid 15:0
                    case "15_1": descriptors.DJCQJZKZUCHHAL = 1; break; //Fatty acid 15:1
                    case "16_0": descriptors.IPCSVZSSVZVIGE = 1; break; //Fatty acid 16:0
                    case "16_1": descriptors.SECPZKHBENQXJG = 1; break; //Fatty acid 16:1
                    case "16_2": descriptors.RVEKLXYYCHAMDF = 1; break; //Fatty acid 16:2
                    case "16_3": descriptors.KBGYPXOSNDMZRV = 1; break; //Fatty acid 16:3
                    case "16_4": descriptors.IVTCJQZAGWTMBZ = 1; break; //Fatty acid 16:4
                    case "17_0": descriptors.KEMQGTRYUADPNZ = 1; break; //Fatty acid 17:0
                    case "17_1": descriptors.QSBYPNXLFMSGKH = 1; break; //Fatty acid 17:1
                    case "17_2": descriptors.LEIXEEFBKOMCEQ = 1; break; //Fatty acid 17:2
                    case "18_0": descriptors.QIQXTHQIDYTFRH = 1; break; //Fatty acid 18:0
                    case "18_1": descriptors.ZQPPMHVWECSIRJ = 1; break; //Fatty acid 18:1
                    case "18_2": descriptors.OYHQOLUKZRVURQ = 1; break; //Fatty acid 18:2
                    case "18_3": descriptors.DTOSIQBPPRVQHS = 1; break; //Fatty acid 18:3
                    case "18_4": descriptors.JIWBIWFOSCKQMA = 1; break; //Fatty acid 18:4
                    case "18_5": descriptors.LYJOUWBWJDKKEF = 1; break; //Fatty acid 18:5
                    case "19_0": descriptors.ISYWECDDZWTKFF = 1; break; //Fatty acid 19:0
                    case "19_1": descriptors.YOKHLRHWEXTWJR = 1; break; //Fatty acid 19:1
                    case "19_2": descriptors.BFXHARLBAGKNRH = 1; break; //Fatty acid 19:2
                    case "20_0": descriptors.VKOBVWXKNCXXDE = 1; break; //Fatty acid 20:0
                    case "20_1": descriptors.BITHHVVYSMSWAG = 1; break; //Fatty acid 20:1
                    case "20_2": descriptors.XSXIVVZCUAHUJO = 1; break; //Fatty acid 20:2
                    case "20_3": descriptors.AHANXAKGNAKFSK = 1; break; //Fatty acid 20:3
                    case "20_4": descriptors.HQPCSDADVLFHHO = 1; break; //Fatty acid 20:4
                    case "20_5": descriptors.JAZBEHYOTPTENJ = 1; break; //Fatty acid 20:5
                    case "21_0": descriptors.CKDDRHZIAZRDBW = 1; break; //Fatty acid 21:0
                    case "21_1": descriptors.HMQNHJJIYIZSOU = 1; break; //Fatty acid 21:1
                    case "21_2": descriptors.SGRSBJJDICHPHB = 1; break; //Fatty acid 21:2
                    case "22_0": descriptors.UKMSUNONTOPOIO = 1; break; //Fatty acid 22:0
                    case "22_1": descriptors.DPUOLQHDNGRHBS = 1; break; //Fatty acid 22:1
                    case "22_2": descriptors.HVGRZDASOHMCSK = 1; break; //Fatty acid 22:2
                    case "22_3": descriptors.RILVNGKQIULBOQ = 1; break; //Fatty acid 22:3
                    case "22_4": descriptors.IGFLWIHPPADNDL = 1; break; //Fatty acid 22:4
                    case "22_5": descriptors.YUFFSWGQGVEMMI = 1; break; //Fatty acid 22:5
                    case "22_6": descriptors.MBMBGCFOFBJSGT = 1; break; //Fatty acid 22:6
                    case "23_0": descriptors.XEZVDURJDFGERA = 1; break; //Fatty acid 23:0
                    case "24_0": descriptors.QZZGJDVWLFXDLK = 1; break; //Fatty acid 24:0
                    case "25_0": descriptors.MWMPEAHGUXCSMY = 1; break; //Fatty acid 25:0
                    case "26_0": descriptors.XMHIUKTWLZUKEX = 1; break; //Fatty acid 26:0
                    case "27_0": descriptors.VXZBFBRLRNDJCS = 1; break; //Fatty acid 27:0
                    case "28_0": descriptors.UTOPWMOLSKOLTQ = 1; break; //Fatty acid 28:0
                    case "29_0": descriptors.IHEJEKZAKSNRLY = 1; break; //Fatty acid 29:0
                    case "30_0": descriptors.VHOCUJPBKOZGJD = 1; break; //Fatty acid 30:0
                    case "31_0": descriptors.ONLMUMPTRGEPCH = 1; break; //Fatty acid 31:0
                    case "32_0": descriptors.ICAIHSUWWZJGHD = 1; break; //Fatty acid 32:0
                    case "33_0": descriptors.HQRWEDFDJHDPJC = 1; break; //Fatty acid 33:0
                    case "34_0": descriptors.UTGPYHWDXYRYGT = 1; break; //Fatty acid 34:0
                    case "35_0": descriptors.HVUCKZJUWZBJDP = 1; break; //Fatty acid 35:0
                    case "36_0": descriptors.LRKATBAZQAWAGV = 1; break; //Fatty acid 36:0
                    case "37_0": descriptors.DEQQJCLFURALOA = 1; break; //Fatty acid 37:0
                    case "38_0": descriptors.AJQRZOBUACOSBG = 1; break; //Fatty acid 38:0
                    case "39_0": descriptors.SDUXWTQRQALDFW = 1; break; //Fatty acid 39:0
                    case "40_0": descriptors.CWXZMNMLGZGDSW = 1; break; //Fatty acid 40:0
                    case "41_0": descriptors.NNAUXQJYQTWAKI = 1; break; //Fatty acid 41:0
                    case "42_0": descriptors.GIGASBXFYYRSBK = 1; break; //Fatty acid 42:0
                    case "43_0": descriptors.GOEYVZMRFNVCCC = 1; break; //Fatty acid 43:0
                    case "44_0": descriptors.ZMVSDJGYGGPPIZ = 1; break; //Fatty acid 44:0
                    case "45_0": descriptors.AIOCODLLACPMJZ = 1; break; //Fatty acid 45:0
                    case "46_0": descriptors.PLPABYLKGGTKBQ = 1; break; //Fatty acid 46:0

                    #endregion
                }
            }
            else if (isTerminalCarboxylate && branchO == 1) {
                switch (acylString) {
                    #region
                    case "2_0": descriptors.AEMRFAOFKBGASW = 1; break; //Hydroxy fatty acid 2:0
                    case "3_0": descriptors.ALRHLSYJTWAHJZ = 1; break; //Hydroxy fatty acid 3:0
                    case "4_0": descriptors.SJZRECIVHVDYJC = 1; break; //Hydroxy fatty acid 4:0
                    case "5_0": descriptors.PHOJOSOUIAQEDH = 1; break; //Hydroxy fatty acid 5:0
                    case "6_0": descriptors.YDCRNMJQROAWFT = 1; break; //Hydroxy fatty acid 6:0
                    case "7_0": descriptors.ICKVZOWXZYBERI = 1; break; //Hydroxy fatty acid 7:0
                    case "8_0": descriptors.LAWNVEFFZXVKAN = 1; break; //Hydroxy fatty acid 8:0
                    case "9_0": descriptors.XYXANQCOUCDVRX = 1; break; //Hydroxy fatty acid 9:0
                    case "10_0": descriptors.LMHJFKYQYDSOQO = 1; break; //Hydroxy fatty acid 10:0
                    case "11_0": descriptors.KTPMCFLPGSCUKH = 1; break; //Hydroxy fatty acid 11:0
                    case "12_0": descriptors.LXNOENXQFNYMGT = 1; break; //Hydroxy fatty acid 12:0
                    case "13_0": descriptors.DEXVEASLUGBXDF = 1; break; //Hydroxy fatty acid 13:0
                    case "13_1": descriptors.WPCPWFUUUOIOPR = 1; break; //Hydroxy fatty acid 13:1
                    case "14_0": descriptors.RIOQEURNPLKGHG = 1; break; //Hydroxy fatty acid 14:0
                    case "14_1": descriptors.YTOUXRDHJPZMFE = 1; break; //Hydroxy fatty acid 14:1
                    case "15_0": descriptors.ZNZLXMVSALEVHW = 1; break; //Hydroxy fatty acid 15:0
                    case "15_1": descriptors.IKEWYFJJKSVXOH = 1; break; //Hydroxy fatty acid 15:1
                    case "16_0": descriptors.GIEJMPHHFWMLHG = 1; break; //Hydroxy fatty acid 16:0
                    case "16_1": descriptors.WADMMPOYJUJWAB = 1; break; //Hydroxy fatty acid 16:1
                    case "16_2": descriptors.ZRAPKHPTJKSDDV = 1; break; //Hydroxy fatty acid 16:2
                    case "16_3": descriptors.SDACWHIIYVTWNU = 1; break; //Hydroxy fatty acid 16:3
                    case "16_4": descriptors.MOMWXVGXQLHTJS = 1; break; //Hydroxy fatty acid 16:4
                    case "17_0": descriptors.SUESMKJJGAOOGC = 1; break; //Hydroxy fatty acid 17:0
                    case "17_1": descriptors.ISKJMLYXFUULLY = 1; break; //Hydroxy fatty acid 17:1
                    case "17_2": descriptors.JBITZWPSOWZJRD = 1; break; //Hydroxy fatty acid 17:2
                    case "18_0": descriptors.YTITYUDOZJUZBE = 1; break; //Hydroxy fatty acid 18:0
                    case "18_1": descriptors.DQSUJQBHODXQRH = 1; break; //Hydroxy fatty acid 18:1
                    case "18_2": descriptors.VQCRNRSTXBCRKF = 1; break; //Hydroxy fatty acid 18:2
                    case "18_3": descriptors.WKFIWTREJDPNAT = 1; break; //Hydroxy fatty acid 18:3
                    case "18_4": descriptors.JZPSRJYCHQSSRG = 1; break; //Hydroxy fatty acid 18:4
                    case "18_5": descriptors.JSRBPYKOLODWGC = 1; break; //Hydroxy fatty acid 18:5
                    case "19_0": descriptors.UHBRCMNTMJCASZ = 1; break; //Hydroxy fatty acid 19:0
                    case "19_1": descriptors.NYGRZFWFEWFNMH = 1; break; //Hydroxy fatty acid 19:1
                    case "19_2": descriptors.IIRPAOCBGJCCAW = 1; break; //Hydroxy fatty acid 19:2
                    case "20_0": descriptors.BGEGHPOGIYNAEZ = 1; break; //Hydroxy fatty acid 20:0
                    case "20_1": descriptors.DIVCIRCXJNMPGK = 1; break; //Hydroxy fatty acid 20:1
                    case "20_2": descriptors.DEAPPYHSCUWMEW = 1; break; //Hydroxy fatty acid 20:2
                    case "20_3": descriptors.MFGADEKBMXNEKF = 1; break; //Hydroxy fatty acid 20:3
                    case "20_4": descriptors.PLUCAVLERAWBGO = 1; break; //Hydroxy fatty acid 20:4
                    case "20_5": descriptors.KONMRYXTSAJFFW = 1; break; //Hydroxy fatty acid 20:5
                    case "21_0": descriptors.BAIUBEVPLHSQCG = 1; break; //Hydroxy fatty acid 21:0
                    case "21_1": descriptors.XAWIBMFYJXZFOA = 1; break; //Hydroxy fatty acid 21:1
                    case "21_2": descriptors.IFIWPTBMSNMDAR = 1; break; //Hydroxy fatty acid 21:2
                    case "22_0": descriptors.AMRLFIVZLCQZPG = 1; break; //Hydroxy fatty acid 22:0
                    case "22_1": descriptors.AYCQQDJXMCSMAV = 1; break; //Hydroxy fatty acid 22:1
                    case "22_2": descriptors.IIAGTUAVIBVHMZ = 1; break; //Hydroxy fatty acid 22:2
                    case "22_3": descriptors.DMYHHDOBDAMXAT = 1; break; //Hydroxy fatty acid 22:3
                    case "22_4": descriptors.HOBYCVCGJUBHHN = 1; break; //Hydroxy fatty acid 22:4
                    case "22_5": descriptors.ICCPODUZIIZNAR = 1; break; //Hydroxy fatty acid 22:5
                    case "22_6": descriptors.FRUPIWUHZRKAOK = 1; break; //Hydroxy fatty acid 22:6
                    #endregion
                }
            }
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors, 
            AtomProperty atom1, AtomProperty atom2, 
            BondProperty bond1)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2 };
            var bonds = new List<BondProperty>() { bond1 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3,
            BondProperty bond1, BondProperty bond2)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3 };
            var bonds = new List<BondProperty>() { bond1, bond2 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4,
            BondProperty bond1, BondProperty bond2, BondProperty bond3)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7, AtomProperty atom8,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C" || atom8.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing || atom8.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6, bond7 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7, AtomProperty atom8, AtomProperty atom9,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7, BondProperty bond8)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C" || atom8.AtomString != "C" || atom9.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing || atom8.IsInRing || atom9.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C" || atom8.AtomString != "C" || atom9.AtomString != "C" || atom10.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing || atom8.IsInRing || atom9.IsInRing || atom10.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10, AtomProperty atom11,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9, BondProperty bond10)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C" || atom8.AtomString != "C" || atom9.AtomString != "C" || atom10.AtomString != "C"
                 || atom11.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing || atom8.IsInRing || atom9.IsInRing || atom10.IsInRing
                 || atom11.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }

        public static void FarryAcidsDescriptors(MolecularFingerprint descriptors,
            AtomProperty atom1, AtomProperty atom2, AtomProperty atom3, AtomProperty atom4, AtomProperty atom5, AtomProperty atom6,
            AtomProperty atom7, AtomProperty atom8, AtomProperty atom9, AtomProperty atom10, AtomProperty atom11, AtomProperty atom12,
            BondProperty bond1, BondProperty bond2, BondProperty bond3, BondProperty bond4, BondProperty bond5,
            BondProperty bond6, BondProperty bond7, BondProperty bond8, BondProperty bond9, BondProperty bond10, BondProperty bond11)
        {
            if (atom1.AtomString != "C" || atom2.AtomString != "C" || atom3.AtomString != "C" || atom4.AtomString != "C" || atom5.AtomString != "C"
                 || atom6.AtomString != "C" || atom7.AtomString != "C" || atom8.AtomString != "C" || atom9.AtomString != "C" || atom10.AtomString != "C"
                 || atom11.AtomString != "C" || atom12.AtomString != "C") return;
            if (atom1.IsInRing || atom2.IsInRing || atom3.IsInRing || atom4.IsInRing || atom5.IsInRing
                || atom6.IsInRing || atom7.IsInRing || atom8.IsInRing || atom9.IsInRing || atom10.IsInRing
                 || atom11.IsInRing || atom12.IsInRing) return;

            var atoms = new List<AtomProperty>() { atom1, atom2, atom3, atom4, atom5, atom6, atom7, atom8, atom9, atom10, atom11, atom12 };
            var bonds = new List<BondProperty>() { bond1, bond2, bond3, bond4, bond5, bond6, bond7, bond8, bond9, bond10, bond11 };
            FattyAcidsDescriptors(descriptors, atoms, bonds);
        }
        #endregion

        #region utility
        private static List<AtomProperty> getBranchAtom(AtomProperty targetAtom, BondProperty excludedBond1, BondProperty excludedBond2, string branchedAtomString)
        {
            var branchAtoms = new List<AtomProperty>();
            var tempBonds = targetAtom.ConnectedBonds.Where(n => n.BondID != excludedBond1.BondID && n.BondID != excludedBond2.BondID).ToList();
            foreach (var tBond in tempBonds) {
                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "H") > 0) continue;
                var bAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != targetAtom.AtomID).ToList()[0];
                if (bAtom.AtomString == branchedAtomString)
                    branchAtoms.Add(bAtom);
            }
            return branchAtoms;
        }

        private static List<AtomProperty> getBranchAtom(AtomProperty targetAtom, BondProperty excludedBond, string branchedAtomString)
        {
            var branchAtoms = new List<AtomProperty>();
            var tempBonds = targetAtom.ConnectedBonds.Where(n => n.BondID != excludedBond.BondID).ToList();
            foreach (var tBond in tempBonds) {
                if (tBond.ConnectedAtoms.Count(n => n.AtomString == "H") > 0) continue;
                var bAtom = tBond.ConnectedAtoms.Where(n => n.AtomID != targetAtom.AtomID).ToList()[0];
                if (bAtom.AtomString == branchedAtomString)
                    branchAtoms.Add(bAtom);
            }
            return branchAtoms;
        }

        public static bool RingsettypeChecker(AtomProperty atomProp, Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary,
            RingsetFunctionType targetRingsetType, out int targetRingsetID)
        {
            targetRingsetID = -1;
            if (!atomProp.IsInRing) return false;

            foreach (var ringID in atomProp.SharedRingIDs) {
                var ringsetID = ringDictionary[ringID].RingsetID;
                var ringsetType = ringsetDictionary[ringsetID].RingsetFunctionType;
                if (ringsetType == targetRingsetType) {
                    targetRingsetID = ringsetID;
                    return true;
                }
            }
            return false;
        }

        public static bool RingtypeChecker(AtomProperty atomProp, Dictionary<int, RingProperty> ringDictionary,
            RingFunctionType targetRingType, out int targetRingID)
        {
            targetRingID = -1;
            if (!atomProp.IsInRing) return false;

            foreach (var ringID in atomProp.SharedRingIDs) {
                var ringType = ringDictionary[ringID].RingFunctionType;
                if (ringType == targetRingType) {
                    targetRingID = ringID;
                    return true;
                }
            }
            return false;
        }

        public static bool RingtypeChecker(BondProperty bondProp, Dictionary<int, RingProperty> ringDictionary,
            RingFunctionType targetRingType, out int targetRingID)
        {
            targetRingID = -1;
            if (!bondProp.IsInRing) return false;
            foreach (var ringID in bondProp.SharedRingIDs) {
                var ringType = ringDictionary[ringID].RingFunctionType;
                if (ringType == targetRingType) {
                    targetRingID = ringID;
                    return true;
                }
            }
            return false;
        }
        #endregion
    }
}
