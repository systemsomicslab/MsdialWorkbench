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



namespace CompMs.MspGenerator
{
    public sealed class qsarDescriptorOnNcdk
    {        
        public static Dictionary<string, double> NcdkDescriptors = new Dictionary<string, double>();

        public static void GenerateQsarDescriptorFileVS4(string inputFile, string outputFile)
        {
            var SmilesParser = new SmilesParser();
            var dummymol = SmilesParser.ParseSmiles("O=C(O)CCCCC"); // Header取得のためのDummy

            var atomCountDic = atomicNumbersCount(dummymol);
            var molDescriptorResultDic = calcQsarDescriptorOnNcdkAll(dummymol);

            var allDescriptorHeader = new List<string>();
            foreach (var item in atomCountDic)
            {
                allDescriptorHeader.Add(item.Key);
            }
            var MolDescriptorHeader = new List<string>();
            foreach (var item in molDescriptorResultDic)
            {
                if (item.Key == "geomShape") { continue; }

                allDescriptorHeader.Add(item.Key);
            }


            var headerLine = string.Empty;
            string[] headerArray = null;
            var queries = new List<string[]>();

            var counter = 0;
            using (var sr = new StreamReader(inputFile, true))
            {
                headerLine = sr.ReadLine();
                headerArray = headerLine.ToUpper().Split('\t');
                int InChIKey = Array.IndexOf(headerArray, "INCHIKEY");
                int SMILES = Array.IndexOf(headerArray, "SMILES");

                var line = "";

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("SMILES")) { continue; }
                    var lineArray = line.Split('\t');
                    var inchikey = lineArray[InChIKey];
                    var rawSmiles = lineArray[SMILES];

                    queries.Add(new string[] { counter.ToString(), inchikey, rawSmiles });
                    counter++;
                }
            }

            var syncObj = new object();
            var results = new List<DescriptorResultTemp>();
            //var resultArray = new DescriptorResultTemp[queries.Count];
            var atomContainers = new Dictionary<long, IAtomContainer>();
            counter = 0;

            var descriptorsAll = new Dictionary<long, Dictionary<string, double>>();
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 4;
            Parallel.For(0, queries.Count, parallelOptions, i =>
            {
                var id = long.Parse(queries[i][0]);
                var inchikey = queries[i][1];
                var smiles = queries[i][2];
                var descriptors = new Dictionary<string, double>();

                var smilesParser = new SmilesParser();
                var atomContainer = smilesParser.ParseSmiles(smiles);
                if (atomContainer == null)
                {
                    var smilesParser2 = new SmilesParser(CDK.Builder, false);
                    atomContainer = smilesParser2.ParseSmiles(smiles);
                    //if (atomContainer == null)
                    //{
                    //    return null;
                    //}
                }

                atomContainers.Add(i, atomContainer);

                atomCountDic = atomicNumbersCount(atomContainers[i]);
                molDescriptorResultDic = calcQsarDescriptorOnNcdkAll(atomContainers[i]);
                foreach (var item in atomCountDic)
                {
                    descriptors.Add(item.Key, double.Parse(item.Value));
                }
                foreach (var item in molDescriptorResultDic)
                {
                        descriptors.Add(item.Key, double.Parse(item.Value));
                }

                var result = new DescriptorResultTemp() { ID = id, InChIKey = inchikey, SMILES = smiles, Descriptor = descriptors };
                //resultArray[id] = result;

                lock (syncObj)
                {
                    results.Add(result);
                    counter++;
                    if (!Console.IsOutputRedirected)
                    {
                        Console.Write("Progress {0}/{1}", counter, queries.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else
                    {
                        Console.WriteLine("Progress {0}/{1}", counter, queries.Count);
                    }
                }
            });


            //results = resultArray.ToList();

            using (var sw = new StreamWriter(outputFile, false, Encoding.ASCII))
            {

                sw.Write(headerLine);
                sw.Write("\t");
                sw.WriteLine(string.Join("\t", allDescriptorHeader));

                foreach (var result in results.OrderBy(n => n.ID))
                {
                    var descriptor = result.Descriptor;
                    sw.Write(string.Join("\t", new string[] { result.InChIKey, result.SMILES }));

                    foreach (var item in allDescriptorHeader)
                    {
                        sw.Write("\t");
                        sw.Write(result.Descriptor[item]);
                    }
                    sw.WriteLine("");
                }
            }
        }

        public static void GenerateQsarDescriptorFileVS3(string inputFile, string outputFile)
        {
            var SmilesParser = new SmilesParser();

            var allDescriptorResultDic = GenerateNCDKDescriptors("O=C(O)CCCCC"); // Header取得のためのDummy

            var allDescriptorHeader = new List<string>();
            foreach (var item in allDescriptorResultDic)
            {
                if (item.Key == "geomShape") { continue; }

                allDescriptorHeader.Add(item.Key);
            }

            var headerLine = string.Empty;
            string[] headerArray = null;
            var queries = new List<string[]>();

            var counter = 0;
            using (var sr = new StreamReader(inputFile, true))
            {
                headerLine = sr.ReadLine();
                headerArray = headerLine.ToUpper().Split('\t');
                int InChIKey = Array.IndexOf(headerArray, "INCHIKEY");
                int SMILES = Array.IndexOf(headerArray, "SMILES");

                var line = "";

                while ((line = sr.ReadLine()) != null)
                {
                    if (line.Contains("SMILES")) { continue; }
                    var lineArray = line.Split('\t');
                    var inchikey = lineArray[InChIKey];
                    var rawSmiles = lineArray[SMILES];

                    queries.Add(new string[] { counter.ToString(), inchikey, rawSmiles });
                    counter++;
                }
            }

            var syncObj = new object();
            var results = new List<DescriptorResultTemp>();
            //var resultArray = new DescriptorResultTemp[queries.Count];
            var atomContainers = new Dictionary<long, IAtomContainer>();
            counter = 0;

            var descriptorsAll = new Dictionary<long, Dictionary<string, double>>();
            ParallelOptions parallelOptions = new ParallelOptions();
            parallelOptions.MaxDegreeOfParallelism = 4;
            Parallel.For(0, queries.Count, parallelOptions, i =>
            {
                var id = long.Parse(queries[i][0]);
                var inchikey = queries[i][1];
                var smiles = queries[i][2];
                var descriptors = new Dictionary<string, double>();

                var smilesParser = new SmilesParser();
                var atomContainer = smilesParser.ParseSmiles(smiles);
                if (atomContainer == null)
                {
                    var smilesParser2 = new SmilesParser(CDK.Builder, false);
                    atomContainer = smilesParser2.ParseSmiles(smiles);
                    //if (atomContainer == null)
                    //{
                    //    return null;
                    //}
                }

                atomContainers.Add(i, atomContainer);
                ////NCDK.Geometries.AtomTools.Add3DCoordinates1(atomContainer);

                AtomicNumbersCountDescriptors(atomContainers[i]);
                AcidicGroupCountDescriptors(atomContainers[i]);
                ALogPDescriptors(atomContainers[i]);
                // AminoAcidCountDescriptors(atomContainer); // bit slow
                APolDescriptors(atomContainers[i]);
                AromaticAtomsCountDescriptors(atomContainers[i]);
                AromaticBondsCountDescriptors(atomContainers[i]);
                AtomCountDescriptors(atomContainers[i]);
                AutocorrelationChargeDescriptors(atomContainers[i]);
                AutocorrelationMassDescriptors(atomContainers[i]);
                AutocorrelationPolarizabilityDescriptors(atomContainers[i]);
                BasicGroupCountDescriptors(atomContainers[i]);
                BcutDescriptors(atomContainers[i]);
                BondCountDescriptors(atomContainers[i]);
                BPolDescriptors(atomContainers[i]);
                CarbonTypesDescriptors(atomContainers[i]);
                ChiChainDescriptors(atomContainers[i]);
                ChiClusterDescriptors(atomContainers[i]);
                ChiPathClusterDescriptors(atomContainers[i]);
                ChiPathDescriptors(atomContainers[i]);
                // CpsaDescriptors(atomContainer); //3D
                EccentricConnectivityIndexDescriptors(atomContainers[i]);
                FmfDescriptors(atomContainers[i]);
                FractionalCSP3Descriptors(atomContainers[i]);
                FractionalPSADescriptors(atomContainers[i]);
                FragmentComplexityDescriptors(atomContainers[i]);
                // GravitationalIndexDescriptors(atomContainer); //3D
                HBondAcceptorCountDescriptors(atomContainers[i]);
                HBondDonorCountDescriptors(atomContainers[i]);
                HybridizationRatioDescriptors(atomContainers[i]);
                JPlogPDescriptors(atomContainers[i]);
                KappaShapeIndicesDescriptors(atomContainers[i]);
                KierHallSmartsDescriptors(atomContainers[i]);
                LargestChainDescriptors(atomContainers[i]);
                LargestPiSystemDescriptors(atomContainers[i]);
                // LengthOverBreadthDescriptors(atomContainer); // 3D
                // LongestAliphaticChainDescriptors(atomContainer);  // some structure cause stack over flow
                MannholdLogPDescriptors(atomContainers[i]);
                MdeDescriptors(atomContainers[i]);
                // MomentOfInertiaDescriptors(atomContainer); // 3D
                PetitjeanNumberDescriptors(atomContainers[i]);
                PetitjeanShapeIndexDescriptors(atomContainers[i]);
                RotatableBondsCountDescriptors(atomContainers[i]);
                RuleOfFiveDescriptors(atomContainers[i]);
                SmallRingDescriptors(atomContainers[i]);
                SpiroAtomCountDescriptors(atomContainers[i]);
                TpsaDescriptors(atomContainers[i]);
                // VabcDescriptors(atomContainer);  // NaN return
                VadjMaDescriptors(atomContainers[i]);
                WeightDescriptors(atomContainers[i]);
                WeightedPathDescriptors(atomContainers[i]);
                // WhimDescriptors(atomContainer); // 3D
                WienerNumbersDescriptors(atomContainers[i]);
                XLogPDescriptors(atomContainers[i]);
                ZagrebIndexDescriptors(atomContainers[i]);

                descriptorsAll.Add(id, descriptors);

                var result = new DescriptorResultTemp() { ID = id, InChIKey = inchikey, SMILES = smiles, Descriptor = descriptorsAll[id] };
                //resultArray[id] = result;

                lock (syncObj)
                {
                    results.Add(result);
                    counter++;
                    if (!Console.IsOutputRedirected)
                    {
                        Console.Write("Progress {0}/{1}", counter, queries.Count);
                        Console.SetCursorPosition(0, Console.CursorTop);
                    }
                    else
                    {
                        Console.WriteLine("Progress {0}/{1}", counter, queries.Count);
                    }
                }
            });


            //results = resultArray.ToList();

            using (var sw = new StreamWriter(outputFile, false, Encoding.ASCII))
            {

                sw.Write(headerLine);
                sw.Write("\t");
                sw.WriteLine(string.Join("\t", allDescriptorHeader));

                foreach (var result in results.OrderBy(n => n.ID))
                {
                    var descriptor = result.Descriptor;
                    sw.Write(string.Join("\t", new string[] { result.InChIKey, result.SMILES }));

                    foreach (var item in allDescriptorHeader)
                    {
                        sw.Write("\t");
                        sw.Write(result.Descriptor[item]);
                    }
                    sw.WriteLine("");
                }
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

        public static void AtomicNumbersCountDescriptors(IAtomContainer mol)
        {
            var atomCountList = new List<string> {
                "H","B","C","N","O","S","P","F","Cl","Br","I"
            };

            var countAtom = 0;
            var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(mol);
            var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
            var atoms = MolecularFormulaManipulator.GetAtomCount(iMolecularFormula);

            var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);
            NcdkDescriptors["ExactMass"] = exactMass;
            foreach (var atom in atomCountList)
            {
                var elementsCount = MolecularFormulaManipulator.GetElementCount(iMolecularFormula, atom);
                NcdkDescriptors["n" + atom] = elementsCount;
                countAtom += elementsCount;
            }
            NcdkDescriptors["nX"] = atoms - countAtom;

            var heavyElements = atoms - (int)NcdkDescriptors["nH"];
            NcdkDescriptors["nHeavyAtom"] = heavyElements;
        }

        public static void AcidicGroupCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new AcidicGroupCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AcidicGroupCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        public static void ALogPDescriptors(IAtomContainer atom)
        {
            var descriptor = new ALogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ALogPDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        // bit slow
        public static void AminoAcidCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new AminoAcidCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AminoAcidCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void APolDescriptors(IAtomContainer atom)
        {
            var descriptor = new APolDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("APolDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AromaticAtomsCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new AromaticAtomsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AromaticAtomsCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AromaticBondsCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new AromaticBondsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AromaticBondsCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AtomCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new AtomCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AtomCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationChargeDescriptors(IAtomContainer atom)
        {
            var descriptor = new AutocorrelationDescriptorCharge();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AutocorrelationDescriptorCharge returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationMassDescriptors(IAtomContainer atom)
        {
            var descriptor = new AutocorrelationDescriptorMass();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AutocorrelationDescriptorMass returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void AutocorrelationPolarizabilityDescriptors(IAtomContainer atom)
        {
            var descriptor = new AutocorrelationDescriptorPolarizability();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AutocorrelationDescriptorPolarizability returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BasicGroupCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new BasicGroupCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("BasicGroupCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// MathNet.Numerics.dll is needed in a local directory
        /// </summary>
        /// <param name="atom"></param>
        public static void BcutDescriptors(IAtomContainer atom)
        {
            var descriptor = new BCUTDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("BCUTDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BondCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new BondCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("BondCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void BPolDescriptors(IAtomContainer atom)
        {
            var descriptor = new BPolDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("BPolDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void CarbonTypesDescriptors(IAtomContainer atom)
        {
            var descriptor = new CarbonTypesDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("AminoAcidCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiChainDescriptors(IAtomContainer atom)
        {
            var descriptor = new ChiChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ChiChainDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiClusterDescriptors(IAtomContainer atom)
        {
            var descriptor = new ChiClusterDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ChiClusterDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiPathClusterDescriptors(IAtomContainer atom)
        {
            var descriptor = new ChiPathClusterDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ChiPathClusterDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ChiPathDescriptors(IAtomContainer atom)
        {
            var descriptor = new ChiPathDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ChiPathDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        // need 3D coordinates
        public static void CpsaDescriptors(IAtomContainer atom)
        {
            var descriptor = new CPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("CPSADescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void EccentricConnectivityIndexDescriptors(IAtomContainer atom)
        {
            var descriptor = new EccentricConnectivityIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("EccentricConnectivityIndexDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FmfDescriptors(IAtomContainer atom)
        {
            var descriptor = new FMFDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("FMFDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FractionalCSP3Descriptors(IAtomContainer atom)
        {
            var descriptor = new FractionalCSP3Descriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("FractionalCSP3Descriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FractionalPSADescriptors(IAtomContainer atom)
        {
            var descriptor = new FractionalPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("FractionalPSADescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void FragmentComplexityDescriptors(IAtomContainer atom)
        {
            var descriptor = new FragmentComplexityDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("FragmentComplexityDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void GravitationalIndexDescriptors(IAtomContainer atom)
        {
            var descriptor = new GravitationalIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("GravitationalIndexDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HBondAcceptorCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new HBondAcceptorCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("HBondAcceptorCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HBondDonorCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new HBondDonorCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("HBondDonorCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void HybridizationRatioDescriptors(IAtomContainer atom)
        {
            var descriptor = new HybridizationRatioDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("HybridizationRatioDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void JPlogPDescriptors(IAtomContainer atom)
        {
            var descriptor = new JPlogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("JPlogPDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void KappaShapeIndicesDescriptors(IAtomContainer atom)
        {
            var descriptor = new KappaShapeIndicesDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("KappaShapeIndicesDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void KierHallSmartsDescriptors(IAtomContainer atom)
        {
            var descriptor = new KierHallSmartsDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("KierHallSmartsDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LargestChainDescriptors(IAtomContainer atom)
        {
            var descriptor = new LargestChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("LargestChainDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LargestPiSystemDescriptors(IAtomContainer atom)
        {
            var descriptor = new LargestPiSystemDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("LargestPiSystemDescriptors returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void LengthOverBreadthDescriptors(IAtomContainer atom)
        {
            var descriptor = new LengthOverBreadthDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("LengthOverBreadthDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void LongestAliphaticChainDescriptors(IAtomContainer atom)
        {
            var descriptor = new LongestAliphaticChainDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("LongestAliphaticChainDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void MannholdLogPDescriptors(IAtomContainer atom)
        {
            var descriptor = new MannholdLogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("MannholdLogPDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void MdeDescriptors(IAtomContainer atom)
        {
            var descriptor = new MDEDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("MDEDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void MomentOfInertiaDescriptors(IAtomContainer atom)
        {
            var descriptor = new MomentOfInertiaDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("MomentOfInertiaDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void PetitjeanNumberDescriptors(IAtomContainer atom)
        {
            var descriptor = new PetitjeanNumberDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("PetitjeanNumberDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void PetitjeanShapeIndexDescriptors(IAtomContainer atom)
        {
            var descriptor = new PetitjeanShapeIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("PetitjeanShapeIndexDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void RotatableBondsCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new RotatableBondsCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("RotatableBondsCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void RuleOfFiveDescriptors(IAtomContainer atom)
        {
            var descriptor = new RuleOfFiveDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("RuleOfFiveDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void SmallRingDescriptors(IAtomContainer atom)
        {
            var descriptor = new SmallRingDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("SmallRingDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void SpiroAtomCountDescriptors(IAtomContainer atom)
        {
            var descriptor = new SpiroAtomCountDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("SpiroAtomCountDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void TpsaDescriptors(IAtomContainer atom)
        {
            var descriptor = new TPSADescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("TPSADescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void VabcDescriptors(IAtomContainer atom)
        {
            var descriptor = new VABCDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("VABCDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void VadjMaDescriptors(IAtomContainer atom)
        {
            var descriptor = new VAdjMaDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("VAdjMaDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void WeightDescriptors(IAtomContainer atom)
        {
            var descriptor = new WeightDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("WeightDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void WeightedPathDescriptors(IAtomContainer atom)
        {
            var descriptor = new WeightedPathDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("WeightedPathDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }

        /// <summary>
        /// it needs 3D coordinates
        /// </summary>
        /// <param name="atom"></param>
        public static void WhimDescriptors(IAtomContainer atom)
        {
            var descriptor = new WHIMDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("WHIMDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }


        public static void WienerNumbersDescriptors(IAtomContainer atom)
        {
            var descriptor = new WienerNumbersDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("WienerNumbersDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void XLogPDescriptors(IAtomContainer atom)
        {
            var descriptor = new XLogPDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("XLogPDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }
        public static void ZagrebIndexDescriptors(IAtomContainer atom)
        {
            var descriptor = new ZagrebIndexDescriptor();
            var result = descriptor.Calculate(atom);
            if (result == null || result.Values.IsEmptyOrNull())
            {
                Console.WriteLine("ZagrebIndexDescriptor returned an error");
            }
            foreach (var item in result)
            {
                NcdkDescriptors[item.Key] = Convert.ToDouble(item.Value);
                // Console.WriteLine("item {0} value {1}", item.Key, Convert.ToDouble(item.Value));
            }
        }





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
            var bcut = new BCUTDescriptor.Result(null);
            var bcutValues = new Dictionary<string, string>();
            try
            {
                bcut = BCUT.Calculate(atom);
            }
            catch (Exception)
            {
                bcutValues.Add("BCUT", "NA");
                return bcutValues;
            }
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
