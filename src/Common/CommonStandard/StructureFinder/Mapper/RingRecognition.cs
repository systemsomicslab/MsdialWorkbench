using CompMs.Common.StructureFinder.Descriptor;
using CompMs.Common.StructureFinder.Property;
using NCDK;
using NCDK.Graphs;
using NCDK.RingSearches;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;

namespace CompMs.Common.StructureFinder.Mapper
{
    public sealed class RingRecognition
    {
        private RingRecognition() { }

        public static void SetRingProperties(IAtomContainer container, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary,
          Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, MolecularFingerprint molecularDescriptor)
        {
            try
            {

                //var finder = new SSSRFinder(container);
                //var sssr = finder.findEssentialRings();
                //var ringsets = RingPartitioner.partitionRings(sssr);
                var finder = Cycles.FindSSSR(container);
                var sssr = finder.ToRingSet();
                var ringsets = RingPartitioner.PartitionRings(sssr);
                var ringCount = 0;

                if (ringsets.Count() != 0)
                {
                    for (int i = 0; i < ringsets.Count(); i++)
                    {
                        var ringset = (IRingSet)ringsets[i];
                        var ringsetProp = new RingsetProperty() { RingsetID = i, IRingSet = ringset };
                        var ringsetCount = ringset.Count();

                        if (ringsetCount == 1)
                        {
                            setSingleRingsetProperty(atomDictionary, bondDictionary, ringDictionary, ringsetDictionary, ringsetProp, ringCount, molecularDescriptor);
                        }
                        else if (ringsetCount == 2)
                        {
                            setDoubleRingsetProperty(atomDictionary, bondDictionary, ringDictionary, ringsetDictionary, ringsetProp, ringCount, molecularDescriptor);
                        }
                        else if (ringsetCount >= 3)
                        {
                            setMultiRingsetProperty(atomDictionary, bondDictionary, ringDictionary, ringsetDictionary, ringsetProp, ringCount, molecularDescriptor);
                        }

                        setMolecularDescriptor(ringsetProp, molecularDescriptor);

                        ringCount += ringsetCount;
                    }
                    PubChemFingerprint.SetSection2Properties(ringDictionary, ringsetDictionary, molecularDescriptor);
                }
            }
            catch (CDKException ex)
            {
                Debug.WriteLine(ex.ToString());
                Debug.WriteLine("Molecule was too complex, handle error");
            }
        }

        private static void setMolecularDescriptor(RingsetProperty ringsetProp, MolecularFingerprint molecularDescriptor)
        {
            switch (ringsetProp.RingsetFunctionType)
            {
                case RingsetFunctionType.Indole:
                    molecularDescriptor.SIKJAQJRHWYJAI = 1;
                    break;
                case RingsetFunctionType.Purine:
                    molecularDescriptor.KDCGOANMDULRCW = 1;
                    break;
                case RingsetFunctionType.TetrahydroPyrrolizine:
                    molecularDescriptor.ADGSVAHCPADPBZ = 1;
                    break;
                case RingsetFunctionType.Tropane:
                    molecularDescriptor.XLRPYZSEQKXZAA = 1;
                    break;
                case RingsetFunctionType.Chromone:
                    molecularDescriptor.OTAFHZMPRISVEM = 1;
                    break;
                case RingsetFunctionType.Coumarin:
                    molecularDescriptor.ZYGHJZDHTFUPRJ = 1;
                    break;
                case RingsetFunctionType.Quinoline:
                    molecularDescriptor.SMWDFEZZVXVKRB = 1;
                    break;
            }
        }


        private static void setMultiRingsetProperty(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, RingsetProperty ringsetProp, int currentRingCount,
            MolecularFingerprint molecularDescriptor)
        {
            ringsetProp.RingsetFunctionType = RingsetFunctionType.OtherRingset;
            ringsetProp.RingIDs = new List<int>();

            var sharedRingCount = 0;
            var aromaticRingCount = 0;
            var heteroAtomContainedRingCount = 0;

            for (int i = 0; i < ringsetProp.IRingSet.Count(); i++)
            {
                var ring = ringsetProp.IRingSet[i];
                var ringProp = new RingProperty() { RingID = currentRingCount, RingsetID = ringsetProp.RingsetID, IRing = ring };

                setRingProperty(ringProp, atomDictionary, bondDictionary, molecularDescriptor);

                ringDictionary[currentRingCount] = ringProp;
                ringsetProp.RingIDs.Add(currentRingCount);
                currentRingCount++;

                sharedRingCount += ringProp.ConnectedBonds.Count(n => n.IsSharedBondInRings);
                if (ringProp.IsAromaticRing) aromaticRingCount++;
                if (ringProp.IsHeteroRing) heteroAtomContainedRingCount++;
            }

            if (aromaticRingCount > 0) ringsetProp.IsAromaticRingset = true;
            if (heteroAtomContainedRingCount > 0) ringsetProp.IsHeteroRingset = true;

            if (ringsetProp.IRingSet.Count() == 3)
            {

                var ring1 = ringDictionary[ringsetProp.RingIDs[0]];
                var ring2 = ringDictionary[ringsetProp.RingIDs[1]];
                var ring3 = ringDictionary[ringsetProp.RingIDs[2]];

                var benzenCounter = 0;
                var sevenRingCounter = 0;
                if (ring1.RingFunctionType == RingFunctionType.Benzene) benzenCounter++;
                if (ring2.RingFunctionType == RingFunctionType.Benzene) benzenCounter++;
                if (ring3.RingFunctionType == RingFunctionType.Benzene) benzenCounter++;
                if (ring1.ConnectedAtoms.Count == 7) sevenRingCounter++;
                if (ring2.ConnectedAtoms.Count == 7) sevenRingCounter++;
                if (ring3.ConnectedAtoms.Count == 7) sevenRingCounter++;

                if (ring1.ConnectedAtoms.Count == 6 && ring2.ConnectedAtoms.Count == 6 && ring3.ConnectedAtoms.Count == 6 &&
                    ring1.RingEnv.CarbonInRing == 5 && ring1.RingEnv.NitorogenInRing == 1 &&
                    ring2.RingEnv.CarbonInRing == 5 && ring2.RingEnv.NitorogenInRing == 1 &&
                    ring3.RingEnv.CarbonInRing == 5 && ring3.RingEnv.NitorogenInRing == 1)
                {

                    #region
                    if ((ring1.IsAromaticRing && !ring2.IsAromaticRing && !ring3.IsAromaticRing)
                        || (!ring1.IsAromaticRing && ring2.IsAromaticRing && !ring3.IsAromaticRing)
                        || (!ring1.IsAromaticRing && !ring2.IsAromaticRing && ring3.IsAromaticRing))
                    {

                        if (!ring1.IsAromaticRing && ring2.IsAromaticRing && !ring3.IsAromaticRing)
                        {
                            ring1 = ringDictionary[ringsetProp.RingIDs[1]];
                            ring2 = ringDictionary[ringsetProp.RingIDs[0]];
                            ring3 = ringDictionary[ringsetProp.RingIDs[2]];
                        }
                        else if (!ring1.IsAromaticRing && !ring2.IsAromaticRing && ring3.IsAromaticRing)
                        {
                            ring1 = ringDictionary[ringsetProp.RingIDs[2]];
                            ring2 = ringDictionary[ringsetProp.RingIDs[0]];
                            ring3 = ringDictionary[ringsetProp.RingIDs[1]];
                        }

                        if (ring1.ConnectedBonds.Count(n => n.IsSharedBondInRings) == 1)
                        {

                            var ring1N = ring1.ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];
                            var ring2N = ring2.ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];
                            var ring3N = ring3.ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];

                            if (ring1N.AtomID == ring2N.AtomID || ring3N.AtomID == ring1N.AtomID)
                            {

                                RingProperty ringTemp2 = null;
                                RingProperty ringTemp3 = null;
                                if (ring1N.AtomID == ring2N.AtomID)
                                {
                                    ringTemp2 = ring2;
                                    ringTemp3 = ring3;
                                }
                                else
                                {
                                    ringTemp2 = ring3;
                                    ringTemp3 = ring2;
                                }

                                if (ringTemp2.ConnectedBonds.Count(n => n.IsSharedBondInRings) == 3 && ringTemp3.ConnectedBonds.Count(n => n.IsSharedBondInRings) == 2)
                                {
                                    ringsetProp.RingsetFunctionType = RingsetFunctionType.Citizin;
                                }
                            }
                        }
                    }
                    #endregion
                }
                else if (benzenCounter == 2 && sevenRingCounter == 1)
                {

                    var sevenRing = ring1;
                    var sixRing1 = ring2;
                    var sixRing2 = ring3;
                    if (ring2.ConnectedAtoms.Count == 7)
                    {
                        sevenRing = ring2;
                        sixRing1 = ring1;
                        sixRing2 = ring3;
                    }
                    else if (ring3.ConnectedAtoms.Count == 7)
                    {
                        sevenRing = ring3;
                        sixRing1 = ring1;
                        sixRing2 = ring2;
                    }

                    if (sevenRing.RingEnv.NitorogenInRing == 1 && sevenRing.RingEnv.SulfurInRing == 1)
                    {
                        var nitrogenAtom = sevenRing.ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];
                        var sulfurAtom = sevenRing.ConnectedAtoms.Where(n => n.AtomString == "S").ToList()[0];
                        if (!nitrogenAtom.IsSharedAtomInRings && !sulfurAtom.IsSharedAtomInRings)
                        {
                            var shardBond1 = sixRing1.ConnectedBonds.Where(n => n.IsSharedBondInRings).ToList();
                            var shardBond2 = sixRing2.ConnectedBonds.Where(n => n.IsSharedBondInRings).ToList();
                            var shardBonsOfSevenRing = sevenRing.ConnectedBonds.Where(n => n.IsSharedBondInRings).ToList();
                            if (shardBond1.Count == 1 && shardBond2.Count == 1 && shardBonsOfSevenRing.Count == 2)
                            {
                                if (shardBonsOfSevenRing.Count(n => n.BondID == shardBond1[0].BondID) == 1 &&
                                    shardBonsOfSevenRing.Count(n => n.BondID == shardBond2[0].BondID) == 1)
                                {
                                    ringsetProp.RingsetFunctionType = RingsetFunctionType.DibenzoThiazepine;
                                }
                            }
                        }
                    }
                }

            }
            else if (sharedRingCount > 0)
            {
                if (aromaticRingCount > 0 && heteroAtomContainedRingCount > 0)
                    ringsetProp.RingsetFunctionType = RingsetFunctionType.ConjugatedAromaticHeteroatomContainedRingset;
                else if (aromaticRingCount > 0)
                    ringsetProp.RingsetFunctionType = RingsetFunctionType.ConjugatedAromaticRingset;
                else if (heteroAtomContainedRingCount > 0)
                    ringsetProp.RingsetFunctionType = RingsetFunctionType.ConjugatedHeteroatomContainedRingset;
                else if (aromaticRingCount == 0 && sharedRingCount == ringsetProp.RingIDs.Count * 2 - 2 && ringsetProp.RingIDs.Count > 3)
                    ringsetProp.RingsetFunctionType = RingsetFunctionType.SteroidRingset;

            }

            ringsetDictionary[ringsetProp.RingsetID] = ringsetProp;
        }

        private static void setDoubleRingsetProperty(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary, RingsetProperty ringsetProp, int currentRingCount,
            MolecularFingerprint molecularDescriptor)
        {
            ringsetProp.RingsetFunctionType = RingsetFunctionType.OtherRingset;
            ringsetProp.RingIDs = new List<int>() { currentRingCount };

            var ring1 = ringsetProp.IRingSet[0];
            var ringProp1 = new RingProperty() { RingID = currentRingCount, RingsetID = ringsetProp.RingsetID, IRing = ring1 };

            setRingProperty(ringProp1, atomDictionary, bondDictionary, molecularDescriptor);

            ringDictionary[currentRingCount] = ringProp1;
            currentRingCount++;

            ringsetProp.RingIDs.Add(currentRingCount);

            var ring2 = ringsetProp.IRingSet[1];
            var ringProp2 = new RingProperty() { RingID = currentRingCount, RingsetID = ringsetProp.RingsetID, IRing = ring2 };

            setRingProperty(ringProp2, atomDictionary, bondDictionary, molecularDescriptor);

            ringDictionary[currentRingCount] = ringProp2;
            currentRingCount++;

            if (ringProp1.IsHeteroRing || ringProp2.IsHeteroRing) ringsetProp.IsHeteroRingset = true;
            if (ringProp1.IsAromaticRing || ringProp2.IsAromaticRing) ringsetProp.IsAromaticRingset = true;

            var isBondShared = ringProp1.ConnectedBonds.Count(n => n.IsSharedBondInRings) == 1 ? true : false;

            if (isBondShared)
            {
                if (ringProp1.ConnectedAtoms.Count == 6 && ringProp2.ConnectedAtoms.Count == 6)
                {

                    ringsetProp.RingsetFunctionType = RingsetFunctionType.OtherC6C6Ringset;
                    #region
                    if (ringProp1.RingFunctionType == RingFunctionType.Benzene || ringProp2.RingFunctionType == RingFunctionType.Benzene)
                    {

                        var benzene = ringProp1.RingFunctionType == RingFunctionType.Benzene ? ringProp1 : ringProp2;
                        var otherRing = ringProp1; if (benzene.RingID == otherRing.RingID) otherRing = ringProp2;

                        if (otherRing.RingFunctionType == RingFunctionType.Benzene)
                            ringsetProp.RingsetFunctionType = RingsetFunctionType.Naphthalene;
                        else if (otherRing.RingFunctionType == RingFunctionType.Pyridine)
                        {
                            if (isRingSharedAtomContainsHeteroatoms(otherRing))
                                ringsetProp.RingsetFunctionType = RingsetFunctionType.Quinoline;
                            else
                                ringsetProp.RingsetFunctionType = RingsetFunctionType.Isoquinoline;
                        }
                        else if (otherRing.RingFunctionType == RingFunctionType.Pyrimidine)
                        {
                            ringsetProp.RingsetFunctionType = RingsetFunctionType.Quinazoline;
                        }
                        else if (otherRing.RingFunctionType == RingFunctionType.Pyridazine)
                        {
                            if (!isRingSharedAtomContainsHeteroatoms(ringProp1))
                                ringsetProp.RingsetFunctionType = RingsetFunctionType.Phthalazine;
                        }
                        else if (otherRing.RingEnv.CarbonInRing == 5 && otherRing.RingEnv.OxygenInRing == 1)
                        {
                            if (otherRing.RingFunctionType == RingFunctionType.Pyrylium)
                            {
                                ringsetProp.RingsetFunctionType = RingsetFunctionType.Chromenylium;
                            }
                            else if (otherRing.RingEnv.KetonOutRing > 0 && otherRing.RingEnv.EsterInRing > 0)
                            {
                                if (isRingSharedAtomContainsHeteroatoms(otherRing))
                                    ringsetProp.RingsetFunctionType = RingsetFunctionType.Coumarin;
                            }
                            else if (otherRing.RingEnv.KetonOutRing > 0 && otherRing.RingEnv.EsterInRing == 0 && otherRing.RingEnv.DoublebondInRing + otherRing.RingEnv.AromaticBondInRing >= 2)
                            {
                                if (isRingSharedAtomContainsHeteroatoms(otherRing))
                                    ringsetProp.RingsetFunctionType = RingsetFunctionType.Chromone;
                            }
                            else if (otherRing.RingEnv.EtherInRing > 0 && otherRing.RingEnv.EsterInRing == 0)
                            {
                                if (isRingSharedAtomContainsHeteroatoms(otherRing))
                                    ringsetProp.RingsetFunctionType = RingsetFunctionType.Chroman;
                            }
                        }
                    }
                    else if ((ringProp1.RingFunctionType == RingFunctionType.Pyrazine && ringProp2.RingFunctionType == RingFunctionType.Pyrimidine)
                           || (ringProp1.RingFunctionType == RingFunctionType.Pyrimidine && ringProp2.RingFunctionType == RingFunctionType.Pyrazine))
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Pteridine;
                    #endregion
                }
                else if ((ringProp1.ConnectedAtoms.Count == 6 && ringProp2.ConnectedAtoms.Count == 5)
                   || (ringProp1.ConnectedAtoms.Count == 5 && ringProp2.ConnectedAtoms.Count == 6))
                {

                    ringsetProp.RingsetFunctionType = RingsetFunctionType.OtherC6C5Ringset;
                    #region

                    var ring6 = ringProp1.ConnectedAtoms.Count == 6 ? ringProp1 : ringProp2;
                    var ring5 = ringProp1; if (ring6.RingID == ring5.RingID) ring5 = ringProp2;

                    if (ring6.RingFunctionType == RingFunctionType.Benzene && ring5.RingFunctionType == RingFunctionType.Pyrrole)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Indole;
                    else if (ring6.RingFunctionType == RingFunctionType.Benzene && (ring5.RingFunctionType == RingFunctionType.HydroPyrrolidine || ring5.RingFunctionType == RingFunctionType.Pyrrolidine))
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.DihydroIndole;
                    else if (ring6.RingFunctionType == RingFunctionType.Benzene && ring5.RingFunctionType == RingFunctionType.Imidazole)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Benzimidazole;
                    else if (ring6.RingFunctionType == RingFunctionType.Benzene && ring5.RingFunctionType == RingFunctionType.Furan)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Benzofuran;
                    else if (ring6.RingFunctionType == RingFunctionType.Pyrimidine && ring5.RingFunctionType == RingFunctionType.Imidazole)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Purine;
                    else if (ring6.RingFunctionType == RingFunctionType.Dihydropyrimidine && ring5.RingFunctionType == RingFunctionType.Imidazole)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.DihydroPurine;
                    else if (ring6.RingFunctionType == RingFunctionType.Benzene && ring5.RingFunctionType == RingFunctionType.Dioxolane)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Benzodioxole;
                    else if (ring6.RingFunctionType == RingFunctionType.CycloO1POCCC1 && ring5.RingFunctionType == RingFunctionType.Tetrahydrofuran)
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.ThFran_CycloO1POCCC1;
                    #endregion
                }
                else if (ringProp1.ConnectedAtoms.Count == 5 && ringProp2.ConnectedAtoms.Count == 5)
                {
                    ringsetProp.RingsetFunctionType = RingsetFunctionType.OtherC5C5Ringset;

                    #region
                    if ((ringProp1.RingFunctionType == RingFunctionType.HydroPyrrolidine && ringProp2.RingFunctionType == RingFunctionType.Pyrrolidine)
                        || (ringProp1.RingFunctionType == RingFunctionType.Pyrrolidine && ringProp2.RingFunctionType == RingFunctionType.HydroPyrrolidine))
                    {

                        var sharedNitrogen = ringProp1.ConnectedAtoms.Where(n => n.AtomString == "N").ToList()[0];
                        if (sharedNitrogen.IsSharedAtomInRings && sharedNitrogen.AtomEnv.SingleBond_CarbonCount >= 3)
                        {
                            ringsetProp.RingsetFunctionType = RingsetFunctionType.TetrahydroPyrrolizine;
                        }
                    }
                    #endregion
                }
            }

            var isAtomShared = ringProp1.ConnectedAtoms.Count(n => n.IsSharedAtomInRings) == 3 ? true : false; //to deal with a bridge chemical
            if (isAtomShared)
            {
                if ((ringProp1.ConnectedAtoms.Count == 6 && ringProp2.ConnectedAtoms.Count == 5)
                   || (ringProp1.ConnectedAtoms.Count == 5 && ringProp2.ConnectedAtoms.Count == 6))
                {

                    var ring1Nitrogens = ringProp1.ConnectedAtoms.Where(n => n.AtomString == "N").ToList();
                    var ring2Nitrogens = ringProp2.ConnectedAtoms.Where(n => n.AtomString == "N").ToList();

                    if (ring1Nitrogens.Count == 1 && ring2Nitrogens.Count == 1
                        && ring1Nitrogens[0].AtomID == ring2Nitrogens[0].AtomID
                        && ring1Nitrogens[0].AtomEnv.SingleBond_CarbonCount == 3)
                    {
                        ringsetProp.RingsetFunctionType = RingsetFunctionType.Tropane;
                    }
                }
            }

            ringsetDictionary[ringsetProp.RingsetID] = ringsetProp;
        }

        private static void setSingleRingsetProperty(Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary,
            Dictionary<int, RingProperty> ringDictionary, Dictionary<int, RingsetProperty> ringsetDictionary,
            RingsetProperty ringsetProp, int currentRingCount, MolecularFingerprint molecularDescriptor)
        {
            ringsetProp.RingsetFunctionType = RingsetFunctionType.SingleRingset;
            ringsetProp.RingIDs = new List<int>() { currentRingCount };

            var ring = ringsetProp.IRingSet[0];
            var ringID = currentRingCount;
            var ringProp = new RingProperty() { RingID = ringID, RingsetID = ringsetProp.RingsetID, IRing = ring };

            setRingProperty(ringProp, atomDictionary, bondDictionary, molecularDescriptor);

            if (ringProp.IsHeteroRing) ringsetProp.IsHeteroRingset = true;
            if (ringProp.IsAromaticRing) ringsetProp.IsAromaticRingset = true;

            ringDictionary[ringID] = ringProp;
            ringsetDictionary[ringsetProp.RingsetID] = ringsetProp;

            currentRingCount++;
        }

        private static bool isRingSharedAtomContainsHeteroatoms(RingProperty ringProp)
        {
            var sharedAtoms = ringProp.ConnectedAtoms.Where(n => n.IsSharedAtomInRings == true);
            var isOneNeighborAtomContainsHeteroatom = false;
            foreach (var atom in sharedAtoms)
            {
                foreach (var bond in atom.ConnectedBonds)
                {
                    if (bond.ConnectedAtoms.Count(n => n.AtomString != "C") > 0)
                    {
                        isOneNeighborAtomContainsHeteroatom = true;
                        break;
                    }
                }
                if (isOneNeighborAtomContainsHeteroatom) break;
            }
            return isOneNeighborAtomContainsHeteroatom;
        }

        private static void setRingProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary,
            Dictionary<int, BondProperty> bondDictionary, MolecularFingerprint descriptors)
        {
            ringProp.RingFunctionType = RingFunctionType.Other;
            switch (ringProp.IRing.Atoms.Count())
            {
                case 3:
                    set3RingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
                case 4:
                    set4RingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
                case 5:
                    set5RingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
                case 6:
                    set6RingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
                case 7:
                    set7RingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
                default:
                    setOtherTypeRingProperty(ringProp, atomDictionary, bondDictionary);
                    break;
            }

            setMolecularDescriptor(ringProp, descriptors);
        }

        private static void setMolecularDescriptor(RingProperty ringProp, MolecularFingerprint descriptors)
        {
            switch (ringProp.RingFunctionType)
            {
                case RingFunctionType.Cyclopropane:
                    descriptors.LVZWSLJZHVFIQJ = 1;
                    break;
                case RingFunctionType.Imidazole:
                    descriptors.RAXXELZNTBOGNW = 1;
                    break;
                case RingFunctionType.Pyrrolidine:
                    descriptors.RWRDLPDLKQPQOW = 1;
                    break;
                case RingFunctionType.Benzene:
                    descriptors.UHOVQNZJYSORNB = 1;
                    break;
                case RingFunctionType.Pyridine:
                    descriptors.JUJWROOIHBZHMG = 1;
                    break;
                case RingFunctionType.Piperidine:
                    descriptors.NQRYJNQNLNOLGT = 1;
                    break;
                case RingFunctionType.Morpholine:
                    descriptors.YNAVUWVOSKDBBP = 1;
                    break;
                case RingFunctionType.Triazole:
                    descriptors.NSPMIYGKQJPBQR = 1;
                    break;
                case RingFunctionType.Cyclopentane:
                    descriptors.RGSFGYAAUTVSQA = 1;
                    break;
                case RingFunctionType.Cyclohexane:
                    descriptors.XDTMQSROBMDMFD = 1;
                    break;
            }
        }

        private static void setOtherTypeRingProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);
            var ringAtomCount = ringProp.ConnectedAtoms.Count;

            if (ringEnv.Sp2AtomInRing == ringAtomCount)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > ringAtomCount - 2)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;

            ringProp.RingFunctionType = RingFunctionType.Other;

            switch (ringProp.ConnectedAtoms.Count)
            {
                case 9:
                    switch (ringEnv.CarbonInRing)
                    {
                        case 8:
                            if (!ringProp.IsAromaticRing && ringEnv.OxygenInRing == 1)
                                ringProp.RingFunctionType = RingFunctionType.Oxonane;
                            break;
                    }
                    break;
            }
            ringProp.RingEnv = ringEnv;
        }

        private static void set7RingProperty(RingProperty ringProp,
            Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);

            if (ringEnv.Sp2AtomInRing == 6)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > 4)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;

            switch (ringEnv.CarbonInRing)
            {
                case 7:
                    if (ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.CycloOctane;
                    else if (ringEnv.DoublebondInRing == 1)
                        ringProp.RingFunctionType = RingFunctionType.CycloOctene;
                    else if (ringEnv.DoublebondInRing == 2)
                        ringProp.RingFunctionType = RingFunctionType.CycloOctadiene;
                    else if (ringEnv.DoublebondInRing == 3)
                        ringProp.RingFunctionType = RingFunctionType.CycloOctatriene;
                    break;
                case 6:
                    if (ringEnv.NitorogenInRing == 1 && ringEnv.DoublebondInRing == 3)
                        ringProp.RingFunctionType = RingFunctionType.Azepine;
                    break;
                case 5:
                    if (ringEnv.NitorogenInRing == 2 && ringEnv.DoublebondInRing == 3)
                        ringProp.RingFunctionType = RingFunctionType.Diazepine;
                    else if (ringEnv.NitorogenInRing == 1 && ringEnv.SulfurInRing == 1 && ringEnv.DoublebondInRing >= 2)
                        ringProp.RingFunctionType = RingFunctionType.Thiazepine;
                    break;
                default:
                    ringProp.RingFunctionType = RingFunctionType.OtherC7Ring;
                    break;
            }
            ringProp.RingEnv = ringEnv;
        }

        private static void set6RingProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);

            if (ringEnv.Sp2AtomInRing == 6)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > 3)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;
            if (ringProp.IsAromaticRing && ringEnv.CarbonInRing == 6)
                ringProp.IsBenzeneRing = true;

            switch (ringEnv.CarbonInRing)
            {

                case 6:
                    if (ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Benzene;
                    else if (ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Cyclohexane;
                    else if (ringEnv.DoublebondInRing == 1)
                        ringProp.RingFunctionType = RingFunctionType.Cyclohexene;
                    else if (ringEnv.DoublebondInRing == 2)
                        ringProp.RingFunctionType = RingFunctionType.Cyclohexadiene;


                    break;

                case 5:

                    if (ringEnv.NitorogenInRing == 1 && ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Piperidine;
                    else if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Pyridine;
                    else if (ringEnv.NitorogenInRing == 1 && ringEnv.DoublebondInRing == 1)
                        ringProp.RingFunctionType = RingFunctionType.Hydropiperidine;
                    else if (ringEnv.NitorogenInRing == 1 && ringEnv.DoublebondInRing == 2)
                        ringProp.RingFunctionType = RingFunctionType.Dihydropyridine;
                    else if (ringEnv.OxygenInRing == 1 && (ringEnv.DoublebondInRing == 0 && ringEnv.AromaticBondInRing == 0))
                        ringProp.RingFunctionType = RingFunctionType.Tetrahydropyran;
                    else if (ringEnv.OxygenInRing == 1 && ringProp.IsAromaticRing)
                    {
                        var oxygenAtom = ringProp.ConnectedAtoms.Where(n => n.AtomString == "O").ToList()[0];
                        if (oxygenAtom.AtomCharge == 1)
                            ringProp.RingFunctionType = RingFunctionType.Pyrylium;
                        else
                            ringProp.RingFunctionType = RingFunctionType.AromaticPyran;
                    }
                    else if (ringEnv.OxygenInRing == 1 && (ringEnv.DoublebondInRing == 1 || ringEnv.AromaticBondInRing == 1))
                        ringProp.RingFunctionType = RingFunctionType.DihydroPyran;
                    else if (ringEnv.OxygenInRing == 1 && ringEnv.DoublebondInRing == 2)
                    {

                        var oxygenAtom = ringProp.ConnectedAtoms.Where(n => n.AtomString == "O").ToList()[0];
                        var oxygenConnectedBonds = oxygenAtom.ConnectedBonds;
                        if (oxygenConnectedBonds.Count == 2)
                        {

                            var atom1 = oxygenConnectedBonds[0].ConnectedAtoms.Where(n => n.AtomID != oxygenAtom.AtomID).ToList()[0];
                            var atom2 = oxygenConnectedBonds[1].ConnectedAtoms.Where(n => n.AtomID != oxygenAtom.AtomID).ToList()[0];

                            var atom1DoubleBonds = atom1.ConnectedBonds.Count(n => n.BondType == BondType.Double);
                            var atom2DoubleBonds = atom2.ConnectedBonds.Count(n => n.BondType == BondType.Double);

                            if (atom1DoubleBonds + atom2DoubleBonds == 2)
                                ringProp.RingFunctionType = RingFunctionType.FhPyran;
                            else if (atom1DoubleBonds + atom2DoubleBonds == 1)
                                ringProp.RingFunctionType = RingFunctionType.ThPyran;
                        }
                    }


                    break;

                case 4:

                    if (ringEnv.NitorogenInRing == 2 && ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Piperazine;
                    else if (ringEnv.NitorogenInRing == 1 && ringEnv.OxygenInRing == 1 && ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Morpholine;
                    else if (ringEnv.NitorogenInRing == 2 && ringEnv.NcnConnect == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Pyrimidine;
                    else if (ringEnv.NitorogenInRing == 2 && ringEnv.NcnConnect == 1 && ringEnv.DoublebondInRing == 2)
                        ringProp.RingFunctionType = RingFunctionType.Dihydropyrimidine;
                    else if (ringEnv.NitorogenInRing == 2 && ringEnv.CncConnect == 2 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Pyrazine;
                    else if (ringEnv.NitorogenInRing == 2 && ringEnv.NncConnect == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Pyridazine;

                    break;

                case 3:
                    if (ringEnv.OpoConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.CycloO1POCCC1;
                    break;
                default:
                    ringProp.RingFunctionType = RingFunctionType.OtherC6Ring;
                    break;
            }

            ringProp.RingEnv = ringEnv;
        }

        private static void set5RingProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);

            if (ringEnv.Sp2AtomInRing == 5)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > 2)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;

            switch (ringEnv.CarbonInRing)
            {
                case 5:
                    if (ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Cyclopentane;
                    else if (ringEnv.DoublebondInRing == 1)
                        ringProp.RingFunctionType = RingFunctionType.Cyclopentene;
                    else if (ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Cyclopendadiene;
                    break;
                case 4:
                    if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Pyrrole;
                    else if (ringEnv.OxygenInRing == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Furan;
                    else if (ringEnv.SulfurInRing == 1 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Thiofene;
                    else if (ringEnv.OxygenInRing == 1 && ringProp.IsAromaticRing == false)
                        ringProp.RingFunctionType = RingFunctionType.Tetrahydrofuran;
                    else if (ringEnv.NitorogenInRing == 1 && !ringProp.IsAromaticRing && ringEnv.DoublebondInRing == 0)
                        ringProp.RingFunctionType = RingFunctionType.Pyrrolidine;
                    else if (ringEnv.NitorogenInRing == 1 && !ringProp.IsAromaticRing && ringEnv.DoublebondInRing == 1)
                        ringProp.RingFunctionType = RingFunctionType.HydroPyrrolidine;

                    break;
                case 3:
                    if (ringEnv.NitorogenInRing == 2 && ringProp.IsAromaticRing && ringEnv.NcnConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Imidazole;
                    else if (ringEnv.NitorogenInRing == 2 && ringProp.IsAromaticRing && ringEnv.NncConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Pyrazole;
                    else if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing && ringEnv.ScnConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Thiazole;
                    else if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing && ringEnv.CsnConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Isothioazole;
                    else if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing && ringEnv.OcnConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Oxazole;
                    else if (ringEnv.NitorogenInRing == 1 && ringProp.IsAromaticRing && ringEnv.ConConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.IsoOxazole;
                    else if (ringEnv.OxygenInRing == 2 && ringEnv.OcoConnect == 1)
                        ringProp.RingFunctionType = RingFunctionType.Dioxolane;
                    break;
                case 2:
                    if (ringEnv.NitorogenInRing == 3 && ringProp.IsAromaticRing)
                        ringProp.RingFunctionType = RingFunctionType.Triazole;
                    break;
                default:
                    ringProp.RingFunctionType = RingFunctionType.OtherC5Ring;
                    break;
            }

            ringProp.RingEnv = ringEnv;
        }

        private static void setImidazoleAtomNumbering(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var connectedAtomsCount = ringProp.ConnectedAtoms.Count;
            var startID = 0;
            var isBackDirection = false;
            for (int i = 0; i < connectedAtomsCount; i++)
            {
                var atom = ringProp.ConnectedAtoms[i];
                var connectedInsideBonds = ringProp.RingEnv.InsideBondDictionary[atom.AtomID];
                if (atom.AtomString == "N" && connectedInsideBonds.Count(n => n.BondType == BondType.Single) == 2)
                {
                    startID = i;
                    var nextPointer = i + 1; if (nextPointer > connectedAtomsCount - 1) nextPointer = 0;

                    var nextAtom = ringProp.ConnectedAtoms[nextPointer];
                    var nextAtomConnectedAtomsInRing = ringProp.RingEnv.InsideAtomDictionary[nextAtom.AtomID];
                    if (nextAtomConnectedAtomsInRing.Count(n => n.AtomString == "N") != 2)
                    {
                        isBackDirection = true;
                    }

                    break;
                }
            }

            var counter = 0;
            var increment = isBackDirection == true ? -1 : 1;
            while (counter < connectedAtomsCount)
            {
                ringProp.ConnectedAtoms[startID].AtomIdInRing = counter + 1;
                startID = startID + increment;

                if (startID == -1) startID = connectedAtomsCount - 1;
                else if (startID == connectedAtomsCount) startID = 0;

                counter++;
            }
        }

        private static void set4RingProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);

            if (ringEnv.Sp2AtomInRing == 4)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > 1)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;

            switch (ringEnv.CarbonInRing)
            {
                case 4:
                    if (ringEnv.DoublebondInRing == 0) ringProp.RingFunctionType = RingFunctionType.Cyclobutane;
                    else if (ringEnv.DoublebondInRing == 1) ringProp.RingFunctionType = RingFunctionType.Cyclobutene;
                    else if (ringEnv.DoublebondInRing == 2) ringProp.RingFunctionType = RingFunctionType.Cyclobutadiene;
                    break;
                case 3:
                    if (ringEnv.OxygenInRing == 1) ringProp.RingFunctionType = RingFunctionType.Azetidine;
                    else if (ringEnv.NitorogenInRing == 1) ringProp.RingFunctionType = RingFunctionType.Oxetane;
                    break;
                default:
                    ringProp.RingFunctionType = RingFunctionType.OtherC4Ring;
                    break;
            }

            ringProp.RingEnv = ringEnv;
        }

        private static void set3RingProperty(RingProperty ringProp,
            Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = GetRingEnvironmentProperty(ringProp, atomDictionary, bondDictionary);

            if (ringEnv.Sp2AtomInRing == 3)
                ringProp.IsAromaticRing = true;
            if (ringEnv.KetonOutRing > 0)
                ringProp.IsKetoneRing = true;
            if (ringEnv.EtherInRing == 1 && ringEnv.HydroxyOutRing + ringEnv.EtherOutRing + ringEnv.KetonOutRing + ringEnv.PrimaryAmineOutRing > 0)
                ringProp.IsSugarRing = true;
            if (ringEnv.EsterInRing > 0)
                ringProp.IsEsterRing = true;
            if (ringEnv.NitorogenInRing > 0 || ringEnv.OxygenInRing > 0 || ringEnv.SulfurInRing > 0 || ringEnv.PhosphorusInRing > 0)
                ringProp.IsHeteroRing = true;

            if (ringEnv.CarbonInRing == 3) ringProp.RingFunctionType = RingFunctionType.Cyclopropane;
            else if (ringEnv.CarbonInRing == 2 && ringEnv.OxygenInRing == 1) ringProp.RingFunctionType = RingFunctionType.Epoxide;
            else if (ringEnv.CarbonInRing == 2 && ringEnv.NitorogenInRing == 1) ringProp.RingFunctionType = RingFunctionType.Aziridine;
            else ringProp.RingFunctionType = RingFunctionType.OtherC3Ring;

            ringProp.RingEnv = ringEnv;
        }

        public static RingBasicEnvironmentProperty GetRingEnvironmentProperty(RingProperty ringProp, Dictionary<int, AtomProperty> atomDictionary, Dictionary<int, BondProperty> bondDictionary)
        {
            var ringEnv = new RingBasicEnvironmentProperty();
            ringProp.ConnectedAtoms = new List<AtomProperty>();
            ringProp.ConnectedBonds = new List<BondProperty>();

            var ringAtomIDs = new List<int>();
            for (int i = 0; i < ringProp.IRing.Atoms.Count(); i++)
            {
                var atom = ringProp.IRing.Atoms[i];
                var atomID = int.Parse(atom.Id);
                var atomProp = atomDictionary[atomID];

                setRingEnvironmentProperty(ringProp, ringEnv, atomProp);

                ringProp.ConnectedAtoms.Add(atomProp);
                ringAtomIDs.Add(atomProp.AtomID);
            }

            for (int i = 0; i < ringProp.ConnectedAtoms.Count; i++)
            {

                var ringAtom = ringProp.ConnectedAtoms[i];
                var connectedBonds = ringAtom.ConnectedBonds;

                var outsideAtoms = new List<AtomProperty>();
                var insideAtoms = new List<AtomProperty>();
                var outsideBonds = new List<BondProperty>();
                var insideBonds = new List<BondProperty>();

                foreach (var cBond in connectedBonds)
                {
                    var counterAtom = cBond.ConnectedAtoms.Where(n => n.AtomID != ringAtom.AtomID).ToList()[0];
                    if (!ringAtomIDs.Contains(counterAtom.AtomID))
                    {
                        outsideAtoms.Add(counterAtom);
                        outsideBonds.Add(cBond);
                    }
                    else
                    {
                        insideAtoms.Add(counterAtom);
                        insideBonds.Add(cBond);
                    }
                }
                ringEnv.OutsideAtomDictionary[ringAtom.AtomID] = outsideAtoms;
                ringEnv.InsideAtomDictionary[ringAtom.AtomID] = insideAtoms;
                ringEnv.OutsideBondDictionary[ringAtom.AtomID] = outsideBonds;
                ringEnv.InsideBondDictionary[ringAtom.AtomID] = insideBonds;

                setRingEnvironmentProperty(ringProp, ringEnv, outsideAtoms, outsideBonds);
                if (insideAtoms.Count == 2)
                    setRingEnvironmentProperty(ringProp, ringEnv, insideAtoms[0], ringAtom, insideAtoms[1]);
            }

            for (int i = 0; i < ringProp.IRing.Bonds.Count(); i++)
            {
                var bond = ringProp.IRing.Bonds[i];
                var bondID = int.Parse(bond.Id);
                var bondProp = bondDictionary[bondID];

                setRingEnvironmentProperty(ringProp, ringEnv, bondProp);

                ringProp.ConnectedBonds.Add(bondProp);
            }

            return ringEnv;
        }

        private static void setRingEnvironmentProperty(RingProperty ringProp, RingBasicEnvironmentProperty ringEnv,
            AtomProperty leftAtom, AtomProperty middleAtom, AtomProperty rightAtom)
        {
            var threeAtomsString = leftAtom.AtomString + middleAtom.AtomString + rightAtom.AtomString;
            if (threeAtomsString == "NCN") ringEnv.NcnConnect++;
            else if (threeAtomsString == "OCN" || threeAtomsString == "NCO") ringEnv.OcnConnect++;
            else if (threeAtomsString == "SCN" || threeAtomsString == "NCS") ringEnv.ScnConnect++;
            else if (threeAtomsString == "SCS") ringEnv.ScsConnect++;
            else if (threeAtomsString == "NNC" || threeAtomsString == "CNN") ringEnv.NncConnect++;
            else if (threeAtomsString == "NNN") ringEnv.NnnConnect++;
            else if (threeAtomsString == "CNC") ringEnv.CncConnect++;
            else if (threeAtomsString == "ONN" || threeAtomsString == "NNO") ringEnv.OnnConnect++;
            else if (threeAtomsString == "COC") ringEnv.OnnConnect++;
            else if (threeAtomsString == "CON" || threeAtomsString == "NOC") ringEnv.ConConnect++;
            else if (threeAtomsString == "CSC") ringEnv.CscConnect++;
            else if (threeAtomsString == "CSN" || threeAtomsString == "NSC") ringEnv.CsnConnect++;
            else if (threeAtomsString == "OCO") ringEnv.OcoConnect++;
            else if (threeAtomsString == "OPO") ringEnv.OpoConnect++;
        }

        private static void setRingEnvironmentProperty(RingProperty ringProp, RingBasicEnvironmentProperty ringEnv, BondProperty bondProp)
        {
            //set bond prop
            if (bondProp.IsInRing == false)
            {
                bondProp.IsInRing = true;
                bondProp.RingID = ringProp.RingID;
                bondProp.SharedRingIDs.Add(ringProp.RingID);
            }
            else
            { //if the bond is already in ring, it means the bond is shared by two rings.
                bondProp.IsSharedBondInRings = true;
                bondProp.SharedRingIDs.Add(ringProp.RingID);
            }

            //set ring env prop
            if (bondProp.BondType == BondType.Double) ringEnv.DoublebondInRing++;
            if (ringProp.ConnectedAtoms.Count == ringEnv.Sp2AtomInRing) ringEnv.AromaticBondInRing++;
        }

        private static void setRingEnvironmentProperty(RingProperty ringProp, RingBasicEnvironmentProperty ringEnv, AtomProperty atomProp)
        {
            //set atom prop
            if (atomProp.IsInRing == false)
            {
                atomProp.IsInRing = true;
                atomProp.RingID = ringProp.RingID;
                atomProp.SharedRingIDs.Add(ringProp.RingID);
            }
            else
            { // if the atom is already in ring, it means that the atom is shared by multiple rings
                atomProp.IsSharedAtomInRings = true;
                atomProp.SharedRingIDs.Add(ringProp.RingID);
            }

            //set ring env prop
            if (atomProp.AtomFunctionType == AtomFunctionType.C_Ester)
                ringEnv.EsterInRing++;
            else if (atomProp.AtomFunctionType == AtomFunctionType.O_Ether)
                ringEnv.EtherInRing++;
            else if (atomProp.AtomFunctionType == AtomFunctionType.O_TripleBondsType1COC)
                ringEnv.OxoniumInRing++;

            //atom in ring
            if (atomProp.AtomString == "C") ringEnv.CarbonInRing++;
            else if (atomProp.AtomString == "N") ringEnv.NitorogenInRing++;
            else if (atomProp.AtomString == "O") ringEnv.OxygenInRing++;
            else if (atomProp.AtomString == "S") ringEnv.SulfurInRing++;
            else if (atomProp.AtomString == "P") ringEnv.PhosphorusInRing++;

            //sp2 checking
            if (IsSP2BondOrbital(atomProp.IAtom)) ringEnv.Sp2AtomInRing++;
        }

        private static void setRingEnvironmentProperty(RingProperty ringProp, RingBasicEnvironmentProperty ringEnv, List<AtomProperty> outsideAtoms, List<BondProperty> outsideBonds)
        {
            var firstFlg = false;
            for (int i = 0; i < outsideAtoms.Count; i++)
            {
                var atomProp = outsideAtoms[i];
                var bondProp = outsideBonds[i];
                //ring environment
                if (atomProp.AtomString == "H") ringEnv.HydrogenOutRing++;
                else if (atomProp.AtomString == "C")
                {
                    ringEnv.CarbonOutRing++;
                    if (bondProp.BondType == BondType.Single)
                    {
                        ringEnv.SingleBond_CarbonOutRing++;
                        if (firstFlg == false)
                        {
                            firstFlg = true;
                        }
                        else
                        {
                            ringEnv.DoubleSingleCarbonsOutRing++;
                        }
                    }
                    else if (bondProp.BondType == BondType.Double)
                        ringEnv.DoubleBond_CarbonOutRing++;
                }
                else if (atomProp.AtomString == "N") ringEnv.NitrogenOutRing++;
                else if (atomProp.AtomString == "O") ringEnv.OxygenOutRing++;
                else if (atomProp.AtomString == "S") ringEnv.SulfurOutRing++;
                else if (atomProp.AtomString == "P") ringEnv.PhosphorusOutRing++;
                else if (atomProp.AtomString == "F") ringEnv.FOutRing++;
                else if (atomProp.AtomString == "Cl") ringEnv.ClOutRing++;
                else if (atomProp.AtomString == "Br") ringEnv.BrOutRing++;
                else if (atomProp.AtomString == "I") ringEnv.IOutRing++;

                if (atomProp.AtomFunctionType == AtomFunctionType.O_Hydroxy)
                    ringEnv.HydroxyOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.O_Ketone)
                    ringEnv.KetonOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.O_Ether)
                    ringEnv.EtherOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.C_Methyl)
                    ringEnv.MethylOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.C_Carboxylate)
                    ringEnv.CarboxylateOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.C_Ester)
                    ringEnv.EsterOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.C_Alcohol)
                    ringEnv.AlcoholOutRing++;
                else if (atomProp.AtomFunctionType == AtomFunctionType.N_PrimaryAmine)
                    ringEnv.PrimaryAmineOutRing++;

                if (bondProp.BondType == BondType.Single && atomProp.AtomString == "C")
                {
                    if (atomProp.AtomEnv.SingleBond_CarbonCount >= 2)
                        ringEnv.Carbon_AlkaneOutRing++;
                    if (atomProp.AtomEnv.DoubleBond_CC_Count >= 1)
                        ringEnv.Carbon_AlkeneOutRing++;
                    if (atomProp.AtomEnv.DoubleBond_CO_Count >= 1)
                        ringEnv.Carbon_KetoneOutRing++;
                    if (atomProp.AtomEnv.SingleBond_OxygenCount >= 1)
                        ringEnv.Carbon_OxygenOutRing++;
                    if (atomProp.AtomEnv.SingleBond_NitrogenCount >= 1)
                        ringEnv.Carbon_NitrogenOutRing++;
                    if (atomProp.AtomEnv.SingleBond_OxygenCount >= 1 && atomProp.AtomEnv.DoubleBond_CO_Count >= 1)
                        ringEnv.Carbon_DoubleOxygensOutRing++;
                    if (atomProp.AtomEnv.DoubleBond_CO_Count >= 1 && atomProp.AtomEnv.SingleBond_NitrogenCount >= 1)
                        ringEnv.Carbon_AmideOutRing++;
                    if (atomProp.AtomEnv.DoubleBond_CN_Count >= 1 && atomProp.AtomEnv.SingleBond_OxygenCount >= 1)
                        ringEnv.Carbon_ImidateOutRing++;
                }

            }
        }

        public static bool IsSP2BondOrbital(IAtom atom)
        {
            if (atom.Hybridization != Hybridization.SP2 && atom.Hybridization != Hybridization.Planar3
                   && atom.AtomTypeName.ToLower() != "n.planar3" && atom.AtomTypeName.ToLower() != "n.amide" && atom.AtomTypeName.ToLower() != "o.planar3")
            {
                return false;
            }
            else
            {
                return true;
            }
        }

    }
}
