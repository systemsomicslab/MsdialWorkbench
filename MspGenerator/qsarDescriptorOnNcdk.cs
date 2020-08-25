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

namespace CompMs.MspGenerator
{
    public class qsarDescriptorOnNcdk
    {

        public static void outputDescriptors(string inputFile, string output)
        {
            var SmilesParser = new SmilesParser();
            var dummymol = SmilesParser.ParseSmiles("O=C(O)CCCCC"); // Header取得のためのDummy

            var atomCountDic = atomicNumbersCount(dummymol);
            var molDescriptorResultDic = calcQsarDescriptorOnNcdkAll(dummymol);

            var atomCountDicHeader = new List<string>();
            foreach (var item in atomCountDic)
            {
                atomCountDicHeader.Add(item.Key);
            }
            var MolDescriptorHeader = new List<string>();
            foreach (var item in molDescriptorResultDic)
            {
                MolDescriptorHeader.Add(item.Key);
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII))
            {

                using (var sr = new StreamReader(inputFile, true))
                {
                    var headerLine = sr.ReadLine();
                    var headerArray = headerLine.ToUpper().Split('\t');
                    int InChIKey = Array.IndexOf(headerArray, "INCHIKEY");
                    int SMILES = Array.IndexOf(headerArray, "SMILES");

                    sw.Write(headerLine);
                    sw.Write("\t");
                    sw.Write(string.Join("\t", atomCountDicHeader));
                    sw.Write("\t");
                    sw.WriteLine(string.Join("\t", MolDescriptorHeader));

                    var line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SMILES")) { continue; }
                        var lineArray = line.Split('\t');
                        var inchikey = lineArray[InChIKey];
                        var rawSmiles = lineArray[SMILES];

                        var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
                        atomCountDic = atomicNumbersCount(iAtomContainer);
                        molDescriptorResultDic = calcQsarDescriptorOnNcdkAll(iAtomContainer);
                        var atomCountDatas = new List<string>();
                        var molDescriptorResult = new List<string>();


                        foreach (var item in atomCountDicHeader)
                        {
                            atomCountDatas.Add(atomCountDic[item]);
                        }
                        foreach (var item in MolDescriptorHeader)
                        {
                            if (!molDescriptorResultDic.ContainsKey(item))
                            {
                                molDescriptorResult.Add("NA");
                            }
                            else
                            {
                                molDescriptorResult.Add(molDescriptorResultDic[item]);
                            }


                        }

                        //foreach (var item in atomCountDic)
                        //{
                        //    atomCountDatas.Add(item.Value);
                        //}
                        //foreach (var item in molDescriptorResultDic)
                        //{
                        //    molDescriptorResult.Add(item.Value);
                        //}

                        sw.Write(line);
                        sw.Write("\t");
                        sw.Write(string.Join("\t", atomCountDatas));
                        sw.Write("\t");
                        sw.WriteLine(string.Join("\t", molDescriptorResult));


                    }

                }
            }

        }


        public static Dictionary<string, string> atomicNumbersCount(IAtomContainer mol)
        {
            var atomCountList = new List<string>
            {
            "H","B","C","N","O","S","P","F","Cl","Br","I"
            };
            var atomCountDic = new Dictionary<string, string>();
            var countAtom = 0;

            var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(mol);
            var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
            var atoms = MolecularFormulaManipulator.GetAtomCount(iMolecularFormula);

            var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);
            atomCountDic.Add("ExactMass", exactMass.ToString());

            foreach (var atom in atomCountList)
            {
                var elementsCount = MolecularFormulaManipulator.GetElementCount(iMolecularFormula, atom);
                atomCountDic.Add("n" + atom, elementsCount.ToString());
                countAtom = countAtom + elementsCount;
            }
            atomCountDic.Add("nX", (atoms - countAtom).ToString());

            var heavyElements = atoms - int.Parse(atomCountDic["nH"]);
            atomCountDic.Add("nHeavyAtom", heavyElements.ToString());

            return atomCountDic;
        }


        public static Dictionary<string, string> calcQsarDescriptorOnNcdkAll(IAtomContainer mol)
        {
            NCDK.Geometries.AtomTools.Add3DCoordinates1(mol);

            var AcidicGroupCount = acidicGroupCountDescriptor(mol);
            var ALogP = aLogPDescriptor(mol);
            //AminoAcidCount = aminoAcidCountDescriptor(mol);
            var APol = aPolDescriptor(mol);
            var AromaticAtomsCount = aromaticAtomsCountDescriptor(mol);
            var AromaticBondsCount = aromaticBondsCountDescriptor(mol);
            var AtomCount = atomCountDescriptor(mol);
            var AutocorrelationCharge = autocorrelationChargeDescriptor(mol);
            var AutocorrelationMass = autocorrelationMassDescriptor(mol);
            var AutocorrelationPolarizability = autocorrelationPolarizabilityDescriptor(mol);
            var BasicGroupCount = basicGroupCountDescriptor(mol);
            var BCUT = bcutDescriptor(mol);
            var BondCount = bondCountDescriptor(mol);
            var BPol = bPolDescriptor(mol);
            var CarbonTypes = carbonTypesDescriptor(mol);
            var ChiChain = chiChainDescriptor(mol);
            var ChiCluster = chiClusterDescriptor(mol);
            var ChiPathCluster = chiPathClusterDescriptor(mol);
            var ChiPath = chiPathDescriptor(mol);
            //CPSA = cpsaDescriptor(mol);
            var EccentricConnectivityIndex = eccentricConnectivityIndexDescriptor(mol);
            var FMF = fmfDescriptor(mol);
            var FractionalCSP3 = fractionalCSP3Descriptor(mol);
            var FractionalPSA = fractionalPSADescriptor(mol);
            var FragmentComplexity = fragmentComplexityDescriptor(mol);
            //GravitationalIndex = gravitationalIndexDescriptor(mol);
            var HBondAcceptorCount = hBondAcceptorCountDescriptor(mol);
            var HBondDonorCount = hBondDonorCountDescriptor(mol);
            var HybridizationRatio = hybridizationRatioDescriptor(mol);
            var JPlogP = jPlogPDescriptor(mol);
            var KappaShapeIndices = kappaShapeIndicesDescriptor(mol);
            var KierHallSmarts = kierHallSmartsDescriptor(mol);
            var LargestChain = largestChainDescriptor(mol);
            var LargestPiSystem = largestPiSystemDescriptor(mol);
            //LengthOverBreadth = lengthOverBreadthDescriptor(mol);
            //var LongestAliphaticChain = longestAliphaticChainDescriptor(mol);
            var MannholdLogP = mannholdLogPDescriptor(mol);
            var MDE = mdeDescriptor(mol);
            //MomentOfInertia = momentOfInertiaDescriptor(mol);
            var PetitjeanNumber = petitjeanNumberDescriptor(mol);
            var PetitjeanShapeIndex = petitjeanShapeIndexDescriptor(mol);
            var RotatableBondsCount = rotatableBondsCountDescriptor(mol);
            var RuleOfFive = ruleOfFiveDescriptor(mol);
            var SmallRing = smallRingDescriptor(mol);
            var SpiroAtomCount = spiroAtomCountDescriptor(mol);
            var TPSA = tpsaDescriptor(mol);
            //VABC = vabcDescriptor(mol);
            var VAdjMa = vadjMaDescriptor(mol);
            var Weight = weightDescriptor(mol);
            var WeightedPath = weightedPathDescriptor(mol);
            //WHIM = whimDescriptor(mol);
            var WienerNumbers = wienerNumbersDescriptor(mol);
            var XLogP = xLogPDescriptor(mol);
            var ZagrebIndex = zagrebIndexDescriptor(mol);
            //};

            var MolDescriptorResuitDic = new Dictionary<string, string>();


            foreach (var item in AcidicGroupCount)//AcidicGroupCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ALogP)//ALogP
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in AminoAcidCount)//AminoAcidCount
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in APol)//APol
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in AromaticAtomsCount)//AromaticAtomsCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in AromaticBondsCount)//AromaticBondsCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in AtomCount)//AtomCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in AutocorrelationCharge)//AutocorrelationCharge
            {
                MolDescriptorResuitDic.Add("ATSc" + item.Key, item.Value);
            }

            foreach (var item in AutocorrelationMass)//AutocorrelationMass
            {
                MolDescriptorResuitDic.Add("ATSm" + item.Key, item.Value);
            }

            foreach (var item in AutocorrelationPolarizability)//AutocorrelationPolarizability
            {
                MolDescriptorResuitDic.Add("ATSp" + item.Key, item.Value);
            }

            foreach (var item in BasicGroupCount)//BasicGroupCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in BCUT)//BCUT
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in BondCount)//BondCount
            {
                MolDescriptorResuitDic.Add("BondCount" + item.Key, item.Value);
            }

            foreach (var item in BPol)//BPol
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in CarbonTypes)//CarbonTypes
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ChiChain)//ChiChain
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ChiCluster)//ChiCluster
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ChiPathCluster)//ChiPathCluster
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ChiPath)//ChiPath
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in CPSA)//CPSA
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in EccentricConnectivityIndex)//EccentricConnectivityIndex
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in FMF)//FMF
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in FractionalCSP3)//FractionalCSP3
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in FractionalPSA)//FractionalPSA
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in FragmentComplexity)//FragmentComplexity
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in GravitationalIndex)//GravitationalIndex
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in HBondAcceptorCount)//HBondAcceptorCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in HBondDonorCount)//HBondDonorCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in HybridizationRatio)//HybridizationRatio
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in JPlogP)//JPlogP
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in KappaShapeIndices)//KappaShapeIndices
            {
                MolDescriptorResuitDic.Add("KappaShapeIndices" + item.Key, item.Value);
            }

            foreach (var item in KierHallSmarts)//KierHallSmarts
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in LargestChain)//LargestChain
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in LargestPiSystem)//LargestPiSystem
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in LengthOverBreadth)//LengthOverBreadth
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            //foreach (var item in LongestAliphaticChain)//LongestAliphaticChain
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in MannholdLogP)//MannholdLogP
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in MDE)//MDE
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in MomentOfInertia)//MomentOfInertia
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in PetitjeanNumber)//PetitjeanNumber
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in PetitjeanShapeIndex)//PetitjeanShapeIndex
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in RotatableBondsCount)//RotatableBondsCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in RuleOfFive)//RuleOfFive
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in SmallRing)//SmallRing
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in SpiroAtomCount)//SpiroAtomCount
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in TPSA)//TPSA
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in VABC)//VABC
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in VAdjMa)//VAdjMa
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in Weight)//Weight
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in WeightedPath)//WeightedPath
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            //foreach (var item in WHIM)//WHIM
            //{
            //    MolDescriptorResuitDic.Add(item.Key, item.Value);
            //}

            foreach (var item in WienerNumbers)//WienerNumbers
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in XLogP)//XLogP
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }

            foreach (var item in ZagrebIndex)//ZagrebIndex
            {
                MolDescriptorResuitDic.Add(item.Key, item.Value);
            }


            return MolDescriptorResuitDic;
        }


        public static Dictionary<string, string> acidicGroupCountDescriptor(IAtomContainer atom)
        {
            var AcidicGroupCount = new AcidicGroupCountDescriptor();
            var acidicGroupCount = AcidicGroupCount.Calculate(atom);
            var acidicGroupCountValues = new Dictionary<string, string>();
            foreach (
            var item in acidicGroupCount)
            { acidicGroupCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return acidicGroupCountValues;
        }
        public static Dictionary<string, string> aLogPDescriptor(IAtomContainer atom)
        {
            var ALogP = new ALogPDescriptor();
            var aLogP = new ALogPDescriptor.Result(0.0, 0.0, 0.0);
            var aLogPValues = new Dictionary<string, string>();
            try
            {
                aLogP = ALogP.Calculate(atom);

            }
            catch (Exception)
            {
                aLogPValues.Add("aLogP", "NA");
                return aLogPValues;

            }
            foreach (var item in aLogP)
            {
                aLogPValues.Add(item.Key.ToString(), item.Value.ToString());
            }
            return aLogPValues;
        }
        public static Dictionary<string, string> aminoAcidCountDescriptor(IAtomContainer atom)
        {
            var AminoAcidCount = new AminoAcidCountDescriptor();
            var aminoAcidCount = AminoAcidCount.Calculate(atom);
            var aminoAcidCountValues = new Dictionary<string, string>();
            foreach (
            var item in aminoAcidCount)
            { aminoAcidCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return aminoAcidCountValues;
        }
        public static Dictionary<string, string> aPolDescriptor(IAtomContainer atom)
        {
            var APol = new APolDescriptor();
            var aPol = APol.Calculate(atom);
            var aPolValues = new Dictionary<string, string>();
            foreach (
            var item in aPol)
            { aPolValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return aPolValues;
        }
        public static Dictionary<string, string> aromaticAtomsCountDescriptor(IAtomContainer atom)
        {
            var AromaticAtomsCount = new AromaticAtomsCountDescriptor();
            var aromaticAtomsCount = AromaticAtomsCount.Calculate(atom);
            var aromaticAtomsCountValues = new Dictionary<string, string>();
            foreach (
            var item in aromaticAtomsCount)
            { aromaticAtomsCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return aromaticAtomsCountValues;
        }
        public static Dictionary<string, string> aromaticBondsCountDescriptor(IAtomContainer atom)
        {
            var AromaticBondsCount = new AromaticBondsCountDescriptor();
            var aromaticBondsCount = AromaticBondsCount.Calculate(atom);
            var aromaticBondsCountValues = new Dictionary<string, string>();
            foreach (
            var item in aromaticBondsCount)
            { aromaticBondsCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return aromaticBondsCountValues;
        }
        public static Dictionary<string, string> atomCountDescriptor(IAtomContainer atom)
        {
            var AtomCount = new AtomCountDescriptor();
            var atomCount = AtomCount.Calculate(atom);
            var atomCountValues = new Dictionary<string, string>();
            foreach (
            var item in atomCount)
            { atomCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return atomCountValues;
        }
        public static Dictionary<string, string> autocorrelationChargeDescriptor(IAtomContainer atom)
        {
            var AutocorrelationCharge = new AutocorrelationDescriptorCharge();
            var autocorrelationCharge = AutocorrelationCharge.Calculate(atom);
            var autocorrelationChargeValues = new Dictionary<string, string>();
            foreach (
            var item in autocorrelationCharge)
            { autocorrelationChargeValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return autocorrelationChargeValues;
        }
        public static Dictionary<string, string> autocorrelationMassDescriptor(IAtomContainer atom)
        {
            var AutocorrelationMass = new AutocorrelationDescriptorMass();
            var autocorrelationMass = AutocorrelationMass.Calculate(atom);
            var autocorrelationMassValues = new Dictionary<string, string>();
            foreach (
            var item in autocorrelationMass)
            { autocorrelationMassValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return autocorrelationMassValues;
        }
        public static Dictionary<string, string> autocorrelationPolarizabilityDescriptor(IAtomContainer atom)
        {
            var AutocorrelationPolarizability = new AutocorrelationDescriptorPolarizability();
            var autocorrelationPolarizability = AutocorrelationPolarizability.Calculate(atom);
            var autocorrelationPolarizabilityValues = new Dictionary<string, string>();
            foreach (
            var item in autocorrelationPolarizability)
            { autocorrelationPolarizabilityValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return autocorrelationPolarizabilityValues;
        }
        public static Dictionary<string, string> basicGroupCountDescriptor(IAtomContainer atom)
        {
            var BasicGroupCount = new BasicGroupCountDescriptor();
            var basicGroupCount = BasicGroupCount.Calculate(atom);
            var basicGroupCountValues = new Dictionary<string, string>();
            foreach (
            var item in basicGroupCount)
            { basicGroupCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return basicGroupCountValues;
        }
        public static Dictionary<string, string> bcutDescriptor(IAtomContainer atom)
        {
            var BCUT = new BCUTDescriptor();
            var bcut = BCUT.Calculate(atom);
            var bcutValues = new Dictionary<string, string>();
            if (bcut.Values == null)
            {
                bcutValues.Add("BCUT", "NA");
            }
            else
            {
                foreach (var item in bcut)
                { bcutValues.Add(item.Key.ToString(), item.Value.ToString()); }
            }

            return bcutValues;
        }
        public static Dictionary<string, string> bondCountDescriptor(IAtomContainer atom)
        {
            var BondCount = new BondCountDescriptor();
            var bondCount = BondCount.Calculate(atom);
            var bondCountValues = new Dictionary<string, string>();
            foreach (
            var item in bondCount)
            { bondCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return bondCountValues;
        }
        public static Dictionary<string, string> bPolDescriptor(IAtomContainer atom)
        {
            var BPol = new BPolDescriptor();
            var bPol = BPol.Calculate(atom);
            var bPolValues = new Dictionary<string, string>();
            foreach (
            var item in bPol)
            { bPolValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return bPolValues;
        }
        public static Dictionary<string, string> carbonTypesDescriptor(IAtomContainer atom)
        {
            var CarbonTypes = new CarbonTypesDescriptor();
            var carbonTypes = CarbonTypes.Calculate(atom);
            var carbonTypesValues = new Dictionary<string, string>();
            foreach (
            var item in carbonTypes)
            { carbonTypesValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return carbonTypesValues;
        }
        public static Dictionary<string, string> chiChainDescriptor(IAtomContainer atom)
        {
            var ChiChain = new ChiChainDescriptor();
            var chiChain = ChiChain.Calculate(atom);
            var chiChainValues = new Dictionary<string, string>();
            if (chiChain.Values == null)
            {
                chiChainValues.Add("chiChain", "NA");
            }
            else
            {
                foreach (var item in chiChain)
                {
                    if (!chiChainValues.ContainsKey(item.Key))
                    {
                        chiChainValues.Add(item.Key.ToString(), item.Value.ToString());
                    }
                }
            }
            return chiChainValues;
        }
        public static Dictionary<string, string> chiClusterDescriptor(IAtomContainer atom)
        {
            var ChiCluster = new ChiClusterDescriptor();
            var chiCluster = ChiCluster.Calculate(atom);
            var chiClusterValues = new Dictionary<string, string>();
            if (chiCluster.Values == null)
            {
                chiClusterValues.Add("ChiCluster3", "NA");
            }
            else
            {
                foreach (var item in chiCluster)
                {
                    if (!chiClusterValues.ContainsKey(item.Key))
                    {
                        chiClusterValues.Add(item.Key.ToString(), item.Value.ToString());
                    }
                }
            }

            return chiClusterValues;
        }
        public static Dictionary<string, string> chiPathClusterDescriptor(IAtomContainer atom)
        {
            var ChiPathCluster = new ChiPathClusterDescriptor();
            var chiPathCluster = ChiPathCluster.Calculate(atom);
            var chiPathClusterValues = new Dictionary<string, string>();
            if (chiPathCluster.Values == null)
            {
                chiPathClusterValues.Add("chiPathCluster", "NA");
            }
            else
            {
                foreach (var item in chiPathCluster)
                {
                    if (!chiPathClusterValues.ContainsKey(item.Key))
                    {
                        chiPathClusterValues.Add(item.Key.ToString(), item.Value.ToString());
                    }
                }
            }
            return chiPathClusterValues;
        }
        public static Dictionary<string, string> chiPathDescriptor(IAtomContainer atom)
        {
            var ChiPath = new ChiPathDescriptor();
            var chiPath = ChiPath.Calculate(atom);
            var chiPathValues = new Dictionary<string, string>();
            if (chiPath.Values == null)
            {
                chiPathValues.Add("ChiPath", "NA");
            }
            else
            {
                foreach (var item in chiPath)
                {
                    if (!chiPathValues.ContainsKey(item.Key))
                    {
                        chiPathValues.Add(item.Key.ToString(), item.Value.ToString());
                    }
                }
            }
            return chiPathValues;
        }
        public static Dictionary<string, string> cpsaDescriptor(IAtomContainer atom)
        {
            var CPSA = new CPSADescriptor();
            var cpsa = CPSA.Calculate(atom);
            var cpsaValues = new Dictionary<string, string>();
            foreach (
            var item in cpsa)
            { cpsaValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return cpsaValues;
        }
        public static Dictionary<string, string> eccentricConnectivityIndexDescriptor(IAtomContainer atom)
        {
            var EccentricConnectivityIndex = new EccentricConnectivityIndexDescriptor();
            var eccentricConnectivityIndex = EccentricConnectivityIndex.Calculate(atom);
            var eccentricConnectivityIndexValues = new Dictionary<string, string>();
            foreach (
            var item in eccentricConnectivityIndex)
            { eccentricConnectivityIndexValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return eccentricConnectivityIndexValues;
        }
        public static Dictionary<string, string> fmfDescriptor(IAtomContainer atom)
        {
            var FMF = new FMFDescriptor();
            var fmf = FMF.Calculate(atom);
            var fmfValues = new Dictionary<string, string>();

            if (fmf.Values == null)
            {
                fmfValues.Add("FMF", "NA");
            }
            else
            {
                foreach (var item in fmf)
                {
                    fmfValues.Add(item.Key.ToString(), item.Value.ToString());
                }
            }
            return fmfValues;
        }
        public static Dictionary<string, string> fractionalCSP3Descriptor(IAtomContainer atom)
        {
            var FractionalCSP3 = new FractionalCSP3Descriptor();
            var fractionalCSP3 = FractionalCSP3.Calculate(atom);
            var fractionalCSP3Values = new Dictionary<string, string>();
            foreach (
            var item in fractionalCSP3)
            { fractionalCSP3Values.Add(item.Key.ToString(), item.Value.ToString()); }
            return fractionalCSP3Values;
        }
        public static Dictionary<string, string> fractionalPSADescriptor(IAtomContainer atom)
        {
            var FractionalPSA = new FractionalPSADescriptor();
            var fractionalPSA = new FractionalPSADescriptor.Result(0.0);
            var fractionalPSAValues = new Dictionary<string, string>();
            try
            {
                fractionalPSA = FractionalPSA.Calculate(atom);
            }
            catch (Exception)
            //catch (Exception e) when (e is InvalidOperationException || e is System.NullReferenceException)
            {
                fractionalPSAValues.Add("fractionalPSA", "NA");

                return fractionalPSAValues;
            }
            foreach (var item in fractionalPSA)
            {
                fractionalPSAValues.Add(item.Key.ToString(), item.Value.ToString());
            }
            return fractionalPSAValues;
        }
        public static Dictionary<string, string> fragmentComplexityDescriptor(IAtomContainer atom)
        {
            var FragmentComplexity = new FragmentComplexityDescriptor();
            var fragmentComplexity = FragmentComplexity.Calculate(atom);
            var fragmentComplexityValues = new Dictionary<string, string>();
            foreach (
            var item in fragmentComplexity)
            { fragmentComplexityValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return fragmentComplexityValues;
        }
        public static Dictionary<string, string> gravitationalIndexDescriptor(IAtomContainer atom)
        {
            var GravitationalIndex = new GravitationalIndexDescriptor();
            var gravitationalIndex = GravitationalIndex.Calculate(atom);
            var gravitationalIndexValues = new Dictionary<string, string>();
            foreach (
            var item in gravitationalIndex)
            { gravitationalIndexValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return gravitationalIndexValues;
        }
        public static Dictionary<string, string> hBondAcceptorCountDescriptor(IAtomContainer atom)
        {
            var HBondAcceptorCount = new HBondAcceptorCountDescriptor();
            var hBondAcceptorCount = HBondAcceptorCount.Calculate(atom);
            var hBondAcceptorCountValues = new Dictionary<string, string>();
            foreach (
            var item in hBondAcceptorCount)
            { hBondAcceptorCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return hBondAcceptorCountValues;
        }
        public static Dictionary<string, string> hBondDonorCountDescriptor(IAtomContainer atom)
        {
            var HBondDonorCount = new HBondDonorCountDescriptor();
            var hBondDonorCount = HBondDonorCount.Calculate(atom);
            var hBondDonorCountValues = new Dictionary<string, string>();
            foreach (
            var item in hBondDonorCount)
            { hBondDonorCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return hBondDonorCountValues;
        }
        public static Dictionary<string, string> hybridizationRatioDescriptor(IAtomContainer atom)
        {
            var HybridizationRatio = new HybridizationRatioDescriptor();
            var hybridizationRatio = HybridizationRatio.Calculate(atom);
            var hybridizationRatioValues = new Dictionary<string, string>();
            foreach (
            var item in hybridizationRatio)
            { hybridizationRatioValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return hybridizationRatioValues;
        }
        public static Dictionary<string, string> jPlogPDescriptor(IAtomContainer atom)
        {
            var JPlogP = new JPlogPDescriptor();
            var jPlogP = JPlogP.Calculate(atom);
            var jPlogPValues = new Dictionary<string, string>();
            foreach (
            var item in jPlogP)
            { jPlogPValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return jPlogPValues;
        }
        public static Dictionary<string, string> kappaShapeIndicesDescriptor(IAtomContainer atom)
        {
            var KappaShapeIndices = new KappaShapeIndicesDescriptor();
            var kappaShapeIndices = KappaShapeIndices.Calculate(atom);
            var kappaShapeIndicesValues = new Dictionary<string, string>();
            foreach (
            var item in kappaShapeIndices)
            { kappaShapeIndicesValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return kappaShapeIndicesValues;
        }
        public static Dictionary<string, string> kierHallSmartsDescriptor(IAtomContainer atom)
        {
            var KierHallSmarts = new KierHallSmartsDescriptor();
            var kierHallSmarts = KierHallSmarts.Calculate(atom);
            var kierHallSmartsValues = new Dictionary<string, string>();
            foreach (
            var item in kierHallSmarts)
            { kierHallSmartsValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return kierHallSmartsValues;
        }
        public static Dictionary<string, string> largestChainDescriptor(IAtomContainer atom)
        {
            var LargestChain = new LargestChainDescriptor();
            var largestChain = LargestChain.Calculate(atom);
            var largestChainValues = new Dictionary<string, string>();
            foreach (
            var item in largestChain)
            { largestChainValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return largestChainValues;
        }
        public static Dictionary<string, string> largestPiSystemDescriptor(IAtomContainer atom)
        {
            var LargestPiSystem = new LargestPiSystemDescriptor();
            var largestPiSystem = LargestPiSystem.Calculate(atom);
            var largestPiSystemValues = new Dictionary<string, string>();
            foreach (
            var item in largestPiSystem)
            { largestPiSystemValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return largestPiSystemValues;
        }
        public static Dictionary<string, string> lengthOverBreadthDescriptor(IAtomContainer atom)
        {
            var LengthOverBreadth = new LengthOverBreadthDescriptor();
            var lengthOverBreadth = LengthOverBreadth.Calculate(atom);
            var lengthOverBreadthValues = new Dictionary<string, string>();
            foreach (
            var item in lengthOverBreadth)
            { lengthOverBreadthValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return lengthOverBreadthValues;
        }
        public static Dictionary<string, string> longestAliphaticChainDescriptor(IAtomContainer atom)
        {
            var LongestAliphaticChain = new LongestAliphaticChainDescriptor();
            var longestAliphaticChain = LongestAliphaticChain.Calculate(atom);
            var longestAliphaticChainValues = new Dictionary<string, string>();
            foreach (
            var item in longestAliphaticChain)
            { longestAliphaticChainValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return longestAliphaticChainValues;
        }
        public static Dictionary<string, string> mannholdLogPDescriptor(IAtomContainer atom)
        {
            var MannholdLogP = new MannholdLogPDescriptor();
            var mannholdLogP = MannholdLogP.Calculate(atom);
            var mannholdLogPValues = new Dictionary<string, string>();
            foreach (
            var item in mannholdLogP)
            { mannholdLogPValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return mannholdLogPValues;
        }
        public static Dictionary<string, string> mdeDescriptor(IAtomContainer atom)
        {
            var MDE = new MDEDescriptor();
            var mde = MDE.Calculate(atom);
            var mdeValues = new Dictionary<string, string>();
            foreach (
            var item in mde)
            { mdeValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return mdeValues;
        }
        public static Dictionary<string, string> momentOfInertiaDescriptor(IAtomContainer atom)
        {
            var MomentOfInertia = new MomentOfInertiaDescriptor();
            var momentOfInertia = MomentOfInertia.Calculate(atom);
            var momentOfInertiaValues = new Dictionary<string, string>();
            foreach (
            var item in momentOfInertia)
            { momentOfInertiaValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return momentOfInertiaValues;
        }
        public static Dictionary<string, string> petitjeanNumberDescriptor(IAtomContainer atom)
        {
            var PetitjeanNumber = new PetitjeanNumberDescriptor();
            var petitjeanNumber = PetitjeanNumber.Calculate(atom);
            var petitjeanNumberValues = new Dictionary<string, string>();
            foreach (
            var item in petitjeanNumber)
            { petitjeanNumberValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return petitjeanNumberValues;
        }
        public static Dictionary<string, string> petitjeanShapeIndexDescriptor(IAtomContainer atom)
        {
            var PetitjeanShapeIndex = new PetitjeanShapeIndexDescriptor();
            var petitjeanShapeIndex = PetitjeanShapeIndex.Calculate(atom);
            var petitjeanShapeIndexValues = new Dictionary<string, string>();
            foreach (
            var item in petitjeanShapeIndex)
            { petitjeanShapeIndexValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return petitjeanShapeIndexValues;
        }
        public static Dictionary<string, string> rotatableBondsCountDescriptor(IAtomContainer atom)
        {
            var RotatableBondsCount = new RotatableBondsCountDescriptor();
            var rotatableBondsCount = RotatableBondsCount.Calculate(atom);
            var rotatableBondsCountValues = new Dictionary<string, string>();
            foreach (
            var item in rotatableBondsCount)
            { rotatableBondsCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return rotatableBondsCountValues;
        }
        public static Dictionary<string, string> ruleOfFiveDescriptor(IAtomContainer atom)
        {
            var RuleOfFive = new RuleOfFiveDescriptor();
            var ruleOfFive = RuleOfFive.Calculate(atom);
            var ruleOfFiveValues = new Dictionary<string, string>();
            foreach (
            var item in ruleOfFive)
            { ruleOfFiveValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return ruleOfFiveValues;
        }
        public static Dictionary<string, string> smallRingDescriptor(IAtomContainer atom)
        {
            var SmallRing = new SmallRingDescriptor();
            var smallRing = SmallRing.Calculate(atom);
            var smallRingValues = new Dictionary<string, string>();
            foreach (
            var item in smallRing)
            { smallRingValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return smallRingValues;
        }
        public static Dictionary<string, string> spiroAtomCountDescriptor(IAtomContainer atom)
        {
            var SpiroAtomCount = new SpiroAtomCountDescriptor();
            var spiroAtomCount = SpiroAtomCount.Calculate(atom);
            var spiroAtomCountValues = new Dictionary<string, string>();
            foreach (
            var item in spiroAtomCount)
            { spiroAtomCountValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return spiroAtomCountValues;
        }
        public static Dictionary<string, string> tpsaDescriptor(IAtomContainer atom)
        {
            var TPSA = new TPSADescriptor();
            var tpsa = new TPSADescriptor.Result(0.0);
            var tpsaValues = new Dictionary<string, string>();
            try
            {
                tpsa = TPSA.Calculate(atom);
            }
            catch (Exception)
            //catch (Exception e) when (e is InvalidOperationException || e is System.NullReferenceException)
            {
            }

            foreach (
            var item in tpsa)
            { tpsaValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return tpsaValues;
        }
        public static Dictionary<string, string> vabcDescriptor(IAtomContainer atom)
        {
            var VABC = new VABCDescriptor();
            var vabc = VABC.Calculate(atom);
            var vabcValues = new Dictionary<string, string>();
            foreach (
            var item in vabc)
            { vabcValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return vabcValues;
        }
        public static Dictionary<string, string> vadjMaDescriptor(IAtomContainer atom)
        {
            var VAdjMa = new VAdjMaDescriptor();
            var vadjMa = VAdjMa.Calculate(atom);
            var vadjMaValues = new Dictionary<string, string>();
            foreach (
            var item in vadjMa)
            { vadjMaValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return vadjMaValues;
        }
        public static Dictionary<string, string> weightDescriptor(IAtomContainer atom)
        {
            var Weight = new WeightDescriptor();
            var weight = Weight.Calculate(atom);
            var weightValues = new Dictionary<string, string>();
            foreach (
            var item in weight)
            { weightValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return weightValues;
        }
        public static Dictionary<string, string> weightedPathDescriptor(IAtomContainer atom)
        {
            var WeightedPath = new WeightedPathDescriptor();
            var weightedPath = WeightedPath.Calculate(atom);
            var weightedPathValues = new Dictionary<string, string>();
            foreach (
            var item in weightedPath)
            { weightedPathValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return weightedPathValues;
        }
        public static Dictionary<string, string> whimDescriptor(IAtomContainer atom)
        {
            var WHIM = new WHIMDescriptor();
            var whim = WHIM.Calculate(atom);
            var whimValues = new Dictionary<string, string>();
            foreach (
            var item in whim)
            { whimValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return whimValues;
        }
        public static Dictionary<string, string> wienerNumbersDescriptor(IAtomContainer atom)
        {
            var WienerNumbers = new WienerNumbersDescriptor();
            var wienerNumbers = WienerNumbers.Calculate(atom);
            var wienerNumbersValues = new Dictionary<string, string>();
            foreach (
            var item in wienerNumbers)
            { wienerNumbersValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return wienerNumbersValues;
        }
        public static Dictionary<string, string> xLogPDescriptor(IAtomContainer atom)
        {
            var XLogP = new XLogPDescriptor();
            var xLogP = XLogP.Calculate(atom);
            var xLogPValues = new Dictionary<string, string>();
            foreach (
            var item in xLogP)
            { xLogPValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return xLogPValues;
        }
        public static Dictionary<string, string> zagrebIndexDescriptor(IAtomContainer atom)
        {
            var ZagrebIndex = new ZagrebIndexDescriptor();
            var zagrebIndex = ZagrebIndex.Calculate(atom);
            var zagrebIndexValues = new Dictionary<string, string>();
            foreach (
            var item in zagrebIndex)
            { zagrebIndexValues.Add(item.Key.ToString(), item.Value.ToString()); }
            return zagrebIndexValues;
        }

    }

}
