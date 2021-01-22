using NCDK;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using NCDK.QSAR;
using NCDK.QSAR.Descriptors.Moleculars;
using System.Security.AccessControl;
using NCDK.Validate;
using NCDK.Tools.Manipulator;
using NCDK.Smiles;
using System.IO;
using System.Net;
using CompMs.Common.Extension;
using NCDK.Fingerprints;

namespace CompMs.StructureFinder.NcdkDescriptor {
    public sealed class NcdkDescriptor {
        private NcdkDescriptor() { }
        public static Dictionary<string, double> NcdkDescriptors = new Dictionary<string, double>();


        public static Dictionary<string, double> GenerateAllNCDKDescriptors(string smiles) {

            var smilesParser = new SmilesParser();
            var atomContainer = smilesParser.ParseSmiles(smiles);
            if (atomContainer == null) {
                var smilesParser2 = new SmilesParser(CDK.Builder, false);
                atomContainer = smilesParser2.ParseSmiles(smiles);
                if (atomContainer == null) {
                    return null;
                }
            }
            //NCDK.Geometries.AtomTools.Add3DCoordinates1(atomContainer);

            AtomicNumbersCountDescriptors(atomContainer);
            AcidicGroupCountDescriptors(atomContainer);
            ALogPDescriptors(atomContainer);
            // AminoAcidCountDescriptors(atomContainer); // bit slow
            APolDescriptors(atomContainer);
            AromaticAtomsCountDescriptors(atomContainer);
            AromaticBondsCountDescriptors(atomContainer);
            AtomCountDescriptors(atomContainer);
            AutocorrelationChargeDescriptors(atomContainer);
            AutocorrelationMassDescriptors(atomContainer);
            AutocorrelationPolarizabilityDescriptors(atomContainer);
            BasicGroupCountDescriptors(atomContainer);
            BcutDescriptors(atomContainer);
            BondCountDescriptors(atomContainer);
            BPolDescriptors(atomContainer);
            CarbonTypesDescriptors(atomContainer);
            ChiChainDescriptors(atomContainer);
            ChiClusterDescriptors(atomContainer);
            ChiPathClusterDescriptors(atomContainer);
            ChiPathDescriptors(atomContainer);
            // CpsaDescriptors(atomContainer); //3D
            EccentricConnectivityIndexDescriptors(atomContainer);
            FmfDescriptors(atomContainer);
            FractionalCSP3Descriptors(atomContainer);
            FractionalPSADescriptors(atomContainer);
            FragmentComplexityDescriptors(atomContainer);
            // GravitationalIndexDescriptors(atomContainer); //3D
            HBondAcceptorCountDescriptors(atomContainer);
            HBondDonorCountDescriptors(atomContainer);
            HybridizationRatioDescriptors(atomContainer);
            JPlogPDescriptors(atomContainer);
            KappaShapeIndicesDescriptors(atomContainer);
            KierHallSmartsDescriptors(atomContainer);
            LargestChainDescriptors(atomContainer);
            LargestPiSystemDescriptors(atomContainer);
            // LengthOverBreadthDescriptors(atomContainer); // 3D
            // LongestAliphaticChainDescriptors(atomContainer);  // some structure cause stack over flow
            MannholdLogPDescriptors(atomContainer);
            MdeDescriptors(atomContainer);
            // MomentOfInertiaDescriptors(atomContainer); // 3D
            PetitjeanNumberDescriptors(atomContainer);
            PetitjeanShapeIndexDescriptors(atomContainer);
            RotatableBondsCountDescriptors(atomContainer);
            RuleOfFiveDescriptors(atomContainer);
            SmallRingDescriptors(atomContainer);
            SpiroAtomCountDescriptors(atomContainer);
            TpsaDescriptors(atomContainer);
            // VabcDescriptors(atomContainer);  // NaN return
            VadjMaDescriptors(atomContainer);
            WeightDescriptors(atomContainer);
            WeightedPathDescriptors(atomContainer);
            // WhimDescriptors(atomContainer); // 3D
            WienerNumbersDescriptors(atomContainer);
            XLogPDescriptors(atomContainer);
            ZagrebIndexDescriptors(atomContainer);

            // fingerprinters
            ExecutePubchemFingerprinter(atomContainer);
            ExecuteKlekotaRothFingerprinter(atomContainer);
            ExecuteMACCSFingerprinter(atomContainer);
            Top50MostCommonFunctionalGroups2020Fingerprinter(atomContainer);

            return NcdkDescriptors;
        }

        public static void ExecutePubchemFingerprinter(IAtomContainer mol) {
            var fingerprinter = new PubchemFingerprinter();
            var result = fingerprinter.GetBitFingerprint(mol);
            var counter = result.Length;
            for (int i = 0; i < counter; i++) {
                NcdkDescriptors["PubChem_" + i] = Convert.ToInt32(result[i]);
                //Console.WriteLine("item {0} value {1}", "PubChem_" + i, Convert.ToInt32(result[i]));
            }
        }

        public static void ExecuteKlekotaRothFingerprinter(IAtomContainer mol) {
            var fingerprinter = new KlekotaRothFingerprinter();
            var result = fingerprinter.GetBitFingerprint(mol);
            var counter = result.Length;
            for (int i = 0; i < counter; i++) {
                NcdkDescriptors["KRoth_" + i] = Convert.ToInt32(result[i]);
                //Console.WriteLine("item {0} value {1}", "KRoth_" + i, Convert.ToInt32(result[i]));
            }
        }

        public static void ExecuteMACCSFingerprinter(IAtomContainer mol) {
            var fingerprinter = new MACCSFingerprinter();
            var result = fingerprinter.GetBitFingerprint(mol);
            var counter = result.Length;
            for (int i = 0; i < counter; i++) {
                NcdkDescriptors["MACCS_" + i] = Convert.ToInt32(result[i]);
                //Console.WriteLine("item {0} value {1}", "MACCS_" + i, Convert.ToInt32(result[i]));
            }
        }

        public static Dictionary<string, double> Top50MostCommonFunctionalGroups2020Fingerprinter(string smiles) {
            var smilesParser = new SmilesParser();
            var atomContainer = smilesParser.ParseSmiles(smiles);
            if (atomContainer == null) {
                var smilesParser2 = new SmilesParser(CDK.Builder, false);
                atomContainer = smilesParser2.ParseSmiles(smiles);
                if (atomContainer == null) {
                    return null;
                }
            }
            Top50MostCommonFunctionalGroups2020Fingerprinter(atomContainer);
            return NcdkDescriptors;
        }

        public static Dictionary<string, double> TargetSubstructureFingerPrinter(string smiles) {
            var smilesParser = new SmilesParser();
            var atomContainer = smilesParser.ParseSmiles(smiles);
            if (atomContainer == null) {
                var smilesParser2 = new SmilesParser(CDK.Builder, false);
                atomContainer = smilesParser2.ParseSmiles(smiles);
                if (atomContainer == null) {
                    return null;
                }
            }
            TargetSubstructureFingerPrinter(atomContainer);
            return NcdkDescriptors;
        }

        public static void TargetSubstructureFingerPrinter(IAtomContainer mol) {
            var smartslist = new List<string>() {
                "c1ccccc1",
                "c1ccccc1[#8]",
                "*-[OH]",
                "[#8]-C1OC(CO)C(O)C(O)C1O",
                "NCCCCC(N*)C(=O)O",
                "OP(O)([#8])=O"
            }; // smarts codes
            var fingerprinter = new SubstructureFingerprinter(smartslist);
            var result = fingerprinter.GetBitFingerprint(mol);
            var counter = result.Length;
            for (int i = 0; i < counter; i++) {
                NcdkDescriptors["Top50FG_" + i] = Convert.ToInt32(result[i]);
                //Console.WriteLine("item {0} value {1}", "Top50FG_" + i, Convert.ToInt32(result[i]));
            }
        }

        public static void Top50MostCommonFunctionalGroups2020Fingerprinter(IAtomContainer mol) {
            // https://pubs.acs.org/doi/10.1021/acs.jmedchem.0c00754
            var smartslist = new List<string>() {
                "[#6]N([#6])C([#6])=O",
                "[#6]O[#6]",
                "[#6]N([#6])[#6]",
                "[#6]F",
                "[#6]Cl",
                "[#6]N[#6]",
                "OC",
                "[#6]C(O)=O",
                "O=c",
                "Oc",
                "[#6]N([#6])S([#6])(=O)=O",
                "Nc",
                "NC",
                "[#6]OC([#6])=O",
                "C=C",
                "[#6]C([#6])=O",
                "[#6]N([#6])C(=O)N([#6])[#6]",
                "[#6]S[#6]",
                "C#N",
                "[#6]OC(=O)N([#6])[#6]",
                "[#6]Br",
                "[#6]S([#6])(=O)=O",
                "[#6]N=C(N([#6])[#6])N([#6])[#6]",
                "[#6]N(=O)=O",
                "C#C",
                "[#6]N(O)C([#6])=O",
                "[#6]OCO[#6]",
                "[#6]N=CN([#6])[#6]",
                "[#6]N([#6])C(=O)C=C",
                "C=c",
                "[#6]I",
                "[#6]S",
                "[#6]SS[#6]",
                "[#6]C(=O)C=C",
                "[#6]N(C([#6])=O)S([#6])(=O)=O",
                "[#6]P(O)(O)=O",
                "[#6]N=C",
                "[n+]",
                "[#6]N([#6])C(=O)C([#6])=O",
                "[#6]OC(=O)C=C",
                "[#6]N(C([#6])=O)C([#6])=O",
                "[#6]ON=C",
                "[#6]C=O",
                "[#6][N+]([#6])([#6])[#6]",
                "[#6]OP(O)(O)=O",
                "[#6]N=c",
                "S=c",
                "[#6]N(N=C)C([#6])=O",
                "OC(=O)C=C",
                "[#6]N([#6])N([#6])C([#6])=O",
                "[#6]C(n)=O"
            }; // smarts codes for top 50 FG
            var fingerprinter = new SubstructureFingerprinter(smartslist);
            var result = fingerprinter.GetBitFingerprint(mol);
            var counter = result.Length;
            for (int i = 0; i < counter; i++) {
                NcdkDescriptors["Top50FG_" + i] = Convert.ToInt32(result[i]);
                //Console.WriteLine("item {0} value {1}", "Top50FG_" + i, Convert.ToInt32(result[i]));
            }
        }


        public static void AtomicNumbersCountDescriptors(IAtomContainer mol) {
            var atomCountList = new List<string> {
                "H","B","C","N","O","S","P","F","Cl","Br","I"
            };

            var countAtom = 0;
            var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(mol);
            var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
            var atoms = MolecularFormulaManipulator.GetAtomCount(iMolecularFormula);

            var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);
            NcdkDescriptors["ExactMass"] = exactMass;
            foreach (var atom in atomCountList) {
                var elementsCount = MolecularFormulaManipulator.GetElementCount(iMolecularFormula, atom);
                NcdkDescriptors["n" + atom] = elementsCount;
                countAtom += elementsCount;
            }
            NcdkDescriptors["nX"] = atoms - countAtom;
            
            var heavyElements = atoms - (int)NcdkDescriptors["nH"];
            NcdkDescriptors["nHeavyAtom"] = heavyElements;
        }

        public static void AcidicGroupCountDescriptors(IAtomContainer atom) {
            var descriptor = new AcidicGroupCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AcidicGroupCountDescriptor returned an error");
            }
            foreach (var item in result) { 
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        public static void ALogPDescriptors(IAtomContainer atom) {
            var descriptor = new ALogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ALogPDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        // bit slow
        public static void AminoAcidCountDescriptors(IAtomContainer atom) {
            var descriptor = new AminoAcidCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AminoAcidCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void APolDescriptors(IAtomContainer atom) {
            var descriptor = new APolDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("APolDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AromaticAtomsCountDescriptors(IAtomContainer atom) {
            var descriptor = new AromaticAtomsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AromaticAtomsCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AromaticBondsCountDescriptors(IAtomContainer atom) {
            var descriptor = new AromaticBondsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AromaticBondsCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AtomCountDescriptors(IAtomContainer atom) {
            var descriptor = new AtomCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AtomCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationChargeDescriptors(IAtomContainer atom) {
            var descriptor = new AutocorrelationDescriptorCharge();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AutocorrelationDescriptorCharge returned an error");
            }
            foreach (var item in result) {
                //NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                NcdkDescriptors["ATSc" + item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationMassDescriptors(IAtomContainer atom) {
            var descriptor = new AutocorrelationDescriptorMass();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AutocorrelationDescriptorMass returned an error");
            }
            foreach (var item in result) {
                //NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                NcdkDescriptors["ATSm" + item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationPolarizabilityDescriptors(IAtomContainer atom) {
            var descriptor = new AutocorrelationDescriptorPolarizability();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AutocorrelationDescriptorPolarizability returned an error");
            }
            foreach (var item in result) {
                //NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                NcdkDescriptors["ATSp" + item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BasicGroupCountDescriptors(IAtomContainer atom) {
            var descriptor = new BasicGroupCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("BasicGroupCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// MathNet.Numerics.dll is needed in a local directory
        /// </summary>
        /// <param name="atom"></param>
        public static void BcutDescriptors(IAtomContainer atom) {
            var descriptor = new BCUTDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("BCUTDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BondCountDescriptors(IAtomContainer atom) {
            var descriptor = new BondCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("BondCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BPolDescriptors(IAtomContainer atom) {
            var descriptor = new BPolDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("BPolDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void CarbonTypesDescriptors(IAtomContainer atom) {
            var descriptor = new CarbonTypesDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("AminoAcidCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiChainDescriptors(IAtomContainer atom) {
            var descriptor = new ChiChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ChiChainDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiClusterDescriptors(IAtomContainer atom) {
            var descriptor = new ChiClusterDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ChiClusterDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiPathClusterDescriptors(IAtomContainer atom) {
            var descriptor = new ChiPathClusterDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ChiPathClusterDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiPathDescriptors(IAtomContainer atom) {
            var descriptor = new ChiPathDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ChiPathDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        // need 3D coordinates
        public static void CpsaDescriptors(IAtomContainer atom) {
            var descriptor = new CPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("CPSADescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void EccentricConnectivityIndexDescriptors(IAtomContainer atom) {
            var descriptor = new EccentricConnectivityIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("EccentricConnectivityIndexDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FmfDescriptors(IAtomContainer atom) {
            var descriptor = new FMFDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("FMFDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FractionalCSP3Descriptors(IAtomContainer atom) {
            var descriptor = new FractionalCSP3Descriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("FractionalCSP3Descriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FractionalPSADescriptors(IAtomContainer atom) {
            var descriptor = new FractionalPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("FractionalPSADescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FragmentComplexityDescriptors(IAtomContainer atom) {
            var descriptor = new FragmentComplexityDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("FragmentComplexityDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void GravitationalIndexDescriptors(IAtomContainer atom) {
            var descriptor = new GravitationalIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("GravitationalIndexDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HBondAcceptorCountDescriptors(IAtomContainer atom) {
            var descriptor = new HBondAcceptorCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("HBondAcceptorCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HBondDonorCountDescriptors(IAtomContainer atom) {
            var descriptor = new HBondDonorCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("HBondDonorCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HybridizationRatioDescriptors(IAtomContainer atom) {
            var descriptor = new HybridizationRatioDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("HybridizationRatioDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void JPlogPDescriptors(IAtomContainer atom) {
            var descriptor = new JPlogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("JPlogPDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void KappaShapeIndicesDescriptors(IAtomContainer atom) {
            var descriptor = new KappaShapeIndicesDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("KappaShapeIndicesDescriptor returned an error");
            }
            foreach (var item in result) {
                //NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                NcdkDescriptors["KappaShapeIndices" + item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void KierHallSmartsDescriptors(IAtomContainer atom) {
            var descriptor = new KierHallSmartsDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("KierHallSmartsDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LargestChainDescriptors(IAtomContainer atom) {
            var descriptor = new LargestChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("LargestChainDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LargestPiSystemDescriptors(IAtomContainer atom) {
            var descriptor = new LargestPiSystemDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("LargestPiSystemDescriptors returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void LengthOverBreadthDescriptors(IAtomContainer atom) {
            var descriptor = new LengthOverBreadthDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("LengthOverBreadthDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LongestAliphaticChainDescriptors(IAtomContainer atom) {
            var descriptor = new LongestAliphaticChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("LongestAliphaticChainDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void MannholdLogPDescriptors(IAtomContainer atom) {
            var descriptor = new MannholdLogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("MannholdLogPDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void MdeDescriptors(IAtomContainer atom) {
            var descriptor = new MDEDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("MDEDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void MomentOfInertiaDescriptors(IAtomContainer atom) {
            var descriptor = new MomentOfInertiaDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("MomentOfInertiaDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void PetitjeanNumberDescriptors(IAtomContainer atom) {
            var descriptor = new PetitjeanNumberDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("PetitjeanNumberDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void PetitjeanShapeIndexDescriptors(IAtomContainer atom) {
            var descriptor = new PetitjeanShapeIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("PetitjeanShapeIndexDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void RotatableBondsCountDescriptors(IAtomContainer atom) {
            var descriptor = new RotatableBondsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("RotatableBondsCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void RuleOfFiveDescriptors(IAtomContainer atom) {
            var descriptor = new RuleOfFiveDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("RuleOfFiveDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void SmallRingDescriptors(IAtomContainer atom) {
            var descriptor = new SmallRingDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("SmallRingDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void SpiroAtomCountDescriptors(IAtomContainer atom) {
            var descriptor = new SpiroAtomCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("SpiroAtomCountDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void TpsaDescriptors(IAtomContainer atom) {
            var descriptor = new TPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("TPSADescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void VabcDescriptors(IAtomContainer atom) {
            var descriptor = new VABCDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("VABCDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void VadjMaDescriptors(IAtomContainer atom) {
            var descriptor = new VAdjMaDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("VAdjMaDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void WeightDescriptors(IAtomContainer atom) {
            var descriptor = new WeightDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("WeightDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void WeightedPathDescriptors(IAtomContainer atom) {
            var descriptor = new WeightedPathDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("WeightedPathDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void WhimDescriptors(IAtomContainer atom) {
            var descriptor = new WHIMDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("WHIMDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }


        public static void WienerNumbersDescriptors(IAtomContainer atom) {
            var descriptor = new WienerNumbersDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("WienerNumbersDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void XLogPDescriptors(IAtomContainer atom) {
            var descriptor = new XLogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("XLogPDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ZagrebIndexDescriptors(IAtomContainer atom) {
            var descriptor = new ZagrebIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull()) {
                Console.WriteLine("ZagrebIndexDescriptor returned an error");
            }
            foreach (var item in result) {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        public static Dictionary<string, double> GenerateNCDKDescriptors(string smiles)
        //without finger print
        {

            var smilesParser = new SmilesParser();
            var atomContainer = smilesParser.ParseSmiles(smiles);
            if (atomContainer == null)
            {
                var smilesParser2 = new SmilesParser(CDK.Builder, false);
                atomContainer = smilesParser2.ParseSmiles(smiles);
                if (atomContainer == null)
                {
                    return null;
                }
            }
            //NCDK.Geometries.AtomTools.Add3DCoordinates1(atomContainer);

            AtomicNumbersCountDescriptors(atomContainer);
            AcidicGroupCountDescriptors(atomContainer);
            ALogPDescriptors(atomContainer);
            // AminoAcidCountDescriptors(atomContainer); // bit slow
            APolDescriptors(atomContainer);
            AromaticAtomsCountDescriptors(atomContainer);
            AromaticBondsCountDescriptors(atomContainer);
            AtomCountDescriptors(atomContainer);
            AutocorrelationChargeDescriptors(atomContainer);
            AutocorrelationMassDescriptors(atomContainer);
            AutocorrelationPolarizabilityDescriptors(atomContainer);
            BasicGroupCountDescriptors(atomContainer);
            BcutDescriptors(atomContainer);
            BondCountDescriptors(atomContainer);
            BPolDescriptors(atomContainer);
            CarbonTypesDescriptors(atomContainer);
            ChiChainDescriptors(atomContainer);
            ChiClusterDescriptors(atomContainer);
            ChiPathClusterDescriptors(atomContainer);
            ChiPathDescriptors(atomContainer);
            // CpsaDescriptors(atomContainer); //3D
            EccentricConnectivityIndexDescriptors(atomContainer);
            FmfDescriptors(atomContainer);
            FractionalCSP3Descriptors(atomContainer);
            FractionalPSADescriptors(atomContainer);
            FragmentComplexityDescriptors(atomContainer);
            // GravitationalIndexDescriptors(atomContainer); //3D
            HBondAcceptorCountDescriptors(atomContainer);
            HBondDonorCountDescriptors(atomContainer);
            HybridizationRatioDescriptors(atomContainer);
            JPlogPDescriptors(atomContainer);
            KappaShapeIndicesDescriptors(atomContainer);
            KierHallSmartsDescriptors(atomContainer);
            LargestChainDescriptors(atomContainer);
            LargestPiSystemDescriptors(atomContainer);
            // LengthOverBreadthDescriptors(atomContainer); // 3D
            // LongestAliphaticChainDescriptors(atomContainer);  // some structure cause stack over flow
            MannholdLogPDescriptors(atomContainer);
            MdeDescriptors(atomContainer);
            // MomentOfInertiaDescriptors(atomContainer); // 3D
            PetitjeanNumberDescriptors(atomContainer);
            PetitjeanShapeIndexDescriptors(atomContainer);
            RotatableBondsCountDescriptors(atomContainer);
            RuleOfFiveDescriptors(atomContainer);
            SmallRingDescriptors(atomContainer);
            SpiroAtomCountDescriptors(atomContainer);
            TpsaDescriptors(atomContainer);
            // VabcDescriptors(atomContainer);  // NaN return
            VadjMaDescriptors(atomContainer);
            WeightDescriptors(atomContainer);
            WeightedPathDescriptors(atomContainer);
            // WhimDescriptors(atomContainer); // 3D
            WienerNumbersDescriptors(atomContainer);
            XLogPDescriptors(atomContainer);
            ZagrebIndexDescriptors(atomContainer);

            //// fingerprinters
            //ExecutePubchemFingerprinter(atomContainer);
            //ExecuteKlekotaRothFingerprinter(atomContainer);
            //ExecuteMACCSFingerprinter(atomContainer);
            //Top50MostCommonFunctionalGroups2020Fingerprinter(atomContainer);

            return NcdkDescriptors;
        }

        public static Dictionary<string, double> GenerateNCDKDescriptors(string[] query)
        //without finger print
        {

            var smilesParser = new SmilesParser();
            var atomContainer = smilesParser.ParseSmiles(query[2]);
            if (atomContainer == null)
            {
                var smilesParser2 = new SmilesParser(CDK.Builder, false);
                atomContainer = smilesParser2.ParseSmiles(query[2]);
                if (atomContainer == null)
                {
                    return null;
                }
            }
            //NCDK.Geometries.AtomTools.Add3DCoordinates1(atomContainer);

            AtomicNumbersCountDescriptors(atomContainer);
            AcidicGroupCountDescriptors(atomContainer);
            ALogPDescriptors(atomContainer);
            // AminoAcidCountDescriptors(atomContainer); // bit slow
            APolDescriptors(atomContainer);
            AromaticAtomsCountDescriptors(atomContainer);
            AromaticBondsCountDescriptors(atomContainer);
            AtomCountDescriptors(atomContainer);
            AutocorrelationChargeDescriptors(atomContainer);
            AutocorrelationMassDescriptors(atomContainer);
            AutocorrelationPolarizabilityDescriptors(atomContainer);
            BasicGroupCountDescriptors(atomContainer);
            BcutDescriptors(atomContainer);
            BondCountDescriptors(atomContainer);
            BPolDescriptors(atomContainer);
            CarbonTypesDescriptors(atomContainer);
            ChiChainDescriptors(atomContainer);
            ChiClusterDescriptors(atomContainer);
            ChiPathClusterDescriptors(atomContainer);
            ChiPathDescriptors(atomContainer);
            // CpsaDescriptors(atomContainer); //3D
            EccentricConnectivityIndexDescriptors(atomContainer);
            FmfDescriptors(atomContainer);
            FractionalCSP3Descriptors(atomContainer);
            FractionalPSADescriptors(atomContainer);
            FragmentComplexityDescriptors(atomContainer);
            // GravitationalIndexDescriptors(atomContainer); //3D
            HBondAcceptorCountDescriptors(atomContainer);
            HBondDonorCountDescriptors(atomContainer);
            HybridizationRatioDescriptors(atomContainer);
            JPlogPDescriptors(atomContainer);
            KappaShapeIndicesDescriptors(atomContainer);
            KierHallSmartsDescriptors(atomContainer);
            LargestChainDescriptors(atomContainer);
            LargestPiSystemDescriptors(atomContainer);
            // LengthOverBreadthDescriptors(atomContainer); // 3D
            // LongestAliphaticChainDescriptors(atomContainer);  // some structure cause stack over flow
            MannholdLogPDescriptors(atomContainer);
            MdeDescriptors(atomContainer);
            // MomentOfInertiaDescriptors(atomContainer); // 3D
            PetitjeanNumberDescriptors(atomContainer);
            PetitjeanShapeIndexDescriptors(atomContainer);
            RotatableBondsCountDescriptors(atomContainer);
            RuleOfFiveDescriptors(atomContainer);
            SmallRingDescriptors(atomContainer);
            SpiroAtomCountDescriptors(atomContainer);
            TpsaDescriptors(atomContainer);
            // VabcDescriptors(atomContainer);  // NaN return
            VadjMaDescriptors(atomContainer);
            WeightDescriptors(atomContainer);
            WeightedPathDescriptors(atomContainer);
            // WhimDescriptors(atomContainer); // 3D
            WienerNumbersDescriptors(atomContainer);
            XLogPDescriptors(atomContainer);
            ZagrebIndexDescriptors(atomContainer);

            //// fingerprinters
            //ExecutePubchemFingerprinter(atomContainer);
            //ExecuteKlekotaRothFingerprinter(atomContainer);
            //ExecuteMACCSFingerprinter(atomContainer);
            //Top50MostCommonFunctionalGroups2020Fingerprinter(atomContainer);

            return NcdkDescriptors;
        }

    }
}
