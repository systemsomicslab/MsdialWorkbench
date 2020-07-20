using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NCDK.QSAR.Descriptors.Moleculars;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;
using System.Collections;

namespace CompMs.MspGenerator
{
    public class Common
    {
        public static void jointMspFiles(string path,string filename)
        {
            var mspFiles = Directory.GetFiles(path, "*.msp");
            using (var wfs = new FileStream(path + "\\" + filename, FileMode.Create, FileAccess.Write))
            {
                // 結合するファイルを順に読んで、結果ファイルに書き込む
                foreach (var mspFile in mspFiles)
                {
                    var rbuf = new byte[1024 * 1024];

                    using (var rfs = new FileStream(mspFile, FileMode.Open, FileAccess.Read))
                    {
                        var readByte = 0;
                        var leftByte = rfs.Length;
                        while (leftByte > 0)
                        {
                            // 指定のサイズずつファイルを読み込む
                            readByte = rfs.Read(rbuf, 0, (int)Math.Min(rbuf.Length, leftByte));

                            // 読み込んだ内容を結果ファイルに書き込む
                            wfs.Write(rbuf, 0, readByte);

                            // 残りの読み込みバイト数を更新
                            leftByte -= readByte;
                        }
                    }
                }
            }

        }

        public static void jointTxtFiles(string path, string filename)
        {
            var txtFiles = Directory.GetFiles(path, "*.txt");
            using (var wfs = new FileStream(path + "\\" + filename, FileMode.Create, FileAccess.Write))
            {
                // 結合するファイルを順に読んで、結果ファイルに書き込む
                foreach (var mspFile in txtFiles)
                {
                    var rbuf = new byte[1024 * 1024];

                    using (var rfs = new FileStream(mspFile, FileMode.Open, FileAccess.Read))
                    {
                        var readByte = 0;
                        var leftByte = rfs.Length;
                        while (leftByte > 0)
                        {
                            // 指定のサイズずつファイルを読み込む
                            readByte = rfs.Read(rbuf, 0, (int)Math.Min(rbuf.Length, leftByte));

                            // 読み込んだ内容を結果ファイルに書き込む
                            wfs.Write(rbuf, 0, readByte);

                            // 残りの読み込みバイト数を更新
                            leftByte -= readByte;
                        }
                    }
                }
            }

        }


        public static MetaProperty getMetaProperty(string rawSmiles)
        {
            var SmilesParser = new SmilesParser();
            var SmilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);
            var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
            var smiles = SmilesGenerator.Create(iAtomContainer);
            var iAtomContainer2 = SmilesParser.ParseSmiles(smiles);


            var InChIGeneratorFactory = new InChIGeneratorFactory();
            var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer2).GetInChIKey();

            var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(iAtomContainer);
            var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
            var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);

            var JPlogPDescriptor = new JPlogPDescriptor();
            var logP = JPlogPDescriptor.Calculate(iAtomContainer).JLogP;

            var meta = new MetaProperty()
            {
                Smiles = smiles,
                Formula = formula,
                inChIKey = InChIKey,
                ExactMass = exactMass,
                LogP = logP
            };

            return meta;
        }

        public static List<string> GenerateAcylChains(int minCarbon, int minDouble, int maxCarbon, int maxDouble)
        {
                var acylChains = new List<string>();
            foreach (var item in AcylChainDic.FattyAcylChainDictionary)
            {
                var chainCarbon = int.Parse(item.Value[0]);
                var chainDouble = int.Parse(item.Value[1]);
                if (chainCarbon >= minCarbon && chainCarbon <= maxCarbon && chainDouble >= minDouble && chainDouble <= maxDouble)
                {
                    acylChains.Add(item.Key);
                };
            }
            return acylChains;
        }

        public static List<string> GenerateSphingoChains(int minCarbon, int minDouble, int maxCarbon, int maxDouble)
        {
                var sphingoChains = new List<string>();
            foreach (var item in AcylChainDic.sphingoBaseSDictionary)
            {
                var chainCarbon = int.Parse(item.Value[0]);
                var chainDouble = int.Parse(item.Value[1]);
                if (chainCarbon >= minCarbon && chainCarbon <= maxCarbon && chainDouble >= minDouble && chainDouble <= maxDouble)
                {
                    sphingoChains.Add(item.Key);
                };
            }
            return sphingoChains;
        }

        public static void GenerateFaAcylChains(int minCarbon, int minDouble, int maxCarbon, int maxDouble, List<string> acylChains) //junk
        {
            foreach (var item in AcylChainDic.PhenylFaChainDictionary)
            {
                var chainCarbon = int.Parse(item.Value[0]);
                var chainDouble = int.Parse(item.Value[1]);
                if (chainCarbon >= minCarbon && chainCarbon <= maxCarbon && chainDouble >= minDouble && chainDouble <= maxDouble)
                {
                    acylChains.Add(item.Key);
                };
            }
        }

        // junction
        // 2chains in 
        public static void switchingLipid(List<string> chain1, List<string> chain2, string lipidClass, string output)
        {
            switch (lipidClass)
            {
            // cer
                //normal
                case "Cer_AS":
                case "Cer_ADS":
                case "Cer_AP":
                case "Cer_NS":
                case "Cer_NDS":
                case "Cer_NP":
                case "Cer_BS":
                case "Cer_BDS":
                case "Cer_HS":
                case "Cer_HDS":
                case "HexCer_AP":
                case "HexCer_NS":
                case "HexCer_NDS":
                case "Hex2Cer":
                case "Hex3Cer":
                case "HexCer_HS":
                case "HexCer_HDS":
                case "CerP":
                case "GM3":
                case "MIPC":
                    CeramideMspGenerator.twoChainsCeramideGenerator(chain1, chain2, lipidClass, output);
                    break;

                // ceramide conbination
                case "SM":
                case "SM+O":
                case "SL":
                case "SL+O":
                case "SHexCer":
                case "SHexCer+O":
                case "PE_Cer_d":
                case "PE_Cer_d+O":
                case "PI_Cer_d+O":
                    CeramideMspGenerator.twoChainsCeramideGenerator(chain1, chain2, lipidClass, output);
                    break;

                // GP GL
                case "EtherPC":
                case "EtherPG":
                case "EtherPI":
                case "EtherPS":
                case "EtherDG":
                case "EtherDGDG":
                case "EtherMGDG":
                case "EtherSMGDG":
                case "OxPC":
                case "OxPE":
                case "OxPG":
                case "OxPI":
                case "OxPS":
                case "EtherOxPC":
                case "EtherOxPE":
                case "LNAPE":
                case "LNAPS":
                    GlyceroLipidsMspGenerator.twoIndependentChainsLipidGenerator(chain1, chain2, lipidClass, output);
                    break;
                case "EtherPE":
                case "EtherPE_O":
                    GlyceroLipidsMspGenerator.twoIndependentChainsLipidGenerator(chain1, chain2, lipidClass, output);
                    break;
                //two And One Set Cains
                case "MLCL": // 
                case "HBMP": // 
                case "ADGGA": // 
                case "OxTG":
                case "EtherTG":
                    GlyceroLipidsMspGenerator.twoAndOneSetCainsGlyceroLipidGenerator(chain1, chain2, lipidClass, output);
                    break;
                //three and one set chains
                case "FAHFATG": // 
                    GlyceroLipidsMspGenerator.fahfaTgGlyceroLipidGenerator(chain1, chain2, lipidClass, output);
                    break;
                //FAHFA
                case "FAHFA":
                case "AAHFA":
                case "NAGlySer_FAHFA":
                case "NAGly_FAHFA":
                case "NAOrn_FAHFA":
                    OtherLipidMspGenerator.fahfasGenerator(chain1, chain2, lipidClass, output);
                    break;

                ////conbination
                //case "NAGlySer":
                //case "NAGly":
                //case "NAOrn":
                //    OtherLipidMspGenerator.fahfasGenerator(chain1, chain2, lipidClass, output);
                //    OtherLipidMspGenerator.singleAcylChainLipidGenerator(chain1, lipidClass + "_OxFA", output);
                //    break;


                default:
                    Console.WriteLine("Error in lipidClass switch. Please check settings...");
                    Console.ReadKey();
                    break;
            }
        }
        // 3chains in 
        public static void switchingLipid(List<string> chain1, List<string> chain2, List<string> chain3, string lipidClass, string output)
        {
            switch (lipidClass)
            {
                // cer
                case "ASM": // NS
                case "Cer_EODS": // EODS
                case "Cer_EOS": // EOS
                case "HexCer_EOS": // EOS
                case "Cer_EBDS": // BDS
                case "AHexCer": //AS
                    CeramideMspGenerator.threeChainsCeramideGenerator(chain1, chain2, chain3, lipidClass, output);
                    break;

                default:
                    Console.WriteLine("Error in lipidClass switch. Please check settings...");
                    Console.ReadKey();
                    break;


            }
        }
        // single chain in 
        public static void switchingLipid(List<string> chain1, string lipidClass, string output)
        {
            switch (lipidClass)
            {
                // cer
                case "Sph":
                case "DHSph":
                case "PhytoSph":
                    CeramideMspGenerator.singleChainCeramideGenerator(chain1, lipidClass, output);
                    break;
                //two Equally Acyl Cains GP
                case "PC":  
                case "PE":  
                case "PG":  
                case "PI":  
                case "PS":  
                case "PA":  
                case "PEtOH":  
                case "PMeOH":  
                case "BMP":
                case "MMPE":
                case "DMPE":
                // two Equally Acyl Cains GL
                case "DG":  
                case "MGDG":  
                case "DGDG":  
                case "SQDG":  
                case "DGTS":  
                case "DGGA":  
                case "DLCL":  
                case "SMGDG":  
                case "DGCC":
                    GlyceroLipidsMspGenerator.twoEquallyAcylCainsGlyceroLipidGenerator(chain1, lipidClass, output);
                    break;
                //GL GP
                case "LPC": // 
                case "LPCSN1": // 
                case "LPE": // 
                case "LPG": // 
                case "LPI": // 
                case "LPS": // 
                case "LPA": // 
                case "EtherLPC": // 
                case "EtherLPE": // 
                case "EtherLPE_P": // 
                case "EtherLPG": // 
                case "MG": // 
                case "LDGTS": // 
                case "LDGCC": // 
                    GlyceroLipidsMspGenerator.singleChainGlyceroLipidGenerator(chain1, lipidClass, output);
                    break;

                // three Equally chains
                case "TG": 
                    GlyceroLipidsMspGenerator.threeEquallyCainsGlyceroLipidGenerator(chain1, lipidClass, output);
                    break;
                // four Equally chains
                case "CL": // 
                    GlyceroLipidsMspGenerator.cardiolipinGenerator(chain1, lipidClass, output);
                    break;
                //FA
                case "FA":
                    OtherLipidMspGenerator.faGenerator(chain1, lipidClass, output);
                    break;
                case "OxFA":
                    OtherLipidMspGenerator.oxFaGenerator(chain1, lipidClass, output);
                    break;
                case "alphaOxFA":
                    OtherLipidMspGenerator.alphaOxFaGenerator(chain1, lipidClass, output);
                    break;
                    //single chain
                case "CAR":
                case "VAE":
                case "NAE":

                case "NAGlySer_OxFA":
                case "NAGly_OxFA":
                case "NAOrn_OxFA":

                    OtherLipidMspGenerator.singleAcylChainLipidGenerator(chain1, lipidClass, output);
                     break;
                case "CE":
                case "DCAE":
                case "GDCAE":
                case "GLCAE":
                case "TDCAE":
                case "TLCAE":
                case "KLCAE":
                case "KDCAE":
                case "LCAE":
                case "BRSE":
                case "CASE":
                case "SISE":
                case "STSE":
                case "AHexBRS":
                case "AHexCAS":
                case "AHexCS":
                case "AHexSIS":
                case "AHexSTS":

                case "ErgoSE":
                case "DehydroErgoSE":

                    OtherLipidMspGenerator.singleAcylChainWithSteroidalLipidGenerator(chain1, lipidClass, output);
                    break;

                case "CSLPHex":
                case "BRSLPHex":
                case "CASLPHex":
                case "SISLPHex":
                case "STSLPHex":
                case "CSPHex":
                case "BRSPHex":
                case "CASPHex":
                case "SISPHex":
                case "STSPHex":
                    OtherLipidMspGenerator.steroidWithGlyceroLipidGenerator(chain1, lipidClass, output);
                    break;
                case "Ac2PIM1": // 
                case "Ac2PIM2": // 
                case "Ac3PIM2": // 
                case "Ac4PIM2": // 
                    GlyceroLipidsMspGenerator.bacterialLipidGenerator(chain1, lipidClass, output);
                    break;
                case "LipidA": // 
                    OtherLipidMspGenerator.lipidAGenerator(chain1, lipidClass, output);
                    break;
                default:
                    Console.WriteLine("Error in lipidClass switch. Please check settings...");
                    Console.ReadKey();
                    break;
            }
        }

        public static void switchingLipid(string lipidClass, string output)
        {
            switch (lipidClass)
            {
                case "BAHex":
                case "BASulfate":
                case "SHex":
                case "SPE":
                case "SPEHex":
                case "SPGHex":
                case "SSulfate":
                    OtherLipidMspGenerator.noChainSteroidalLipidGenerator(lipidClass, output);
                    break;

                case "VitaminE":
                case "Vitamin_D":
                    OtherLipidMspGenerator.noChainLipidGenerator(lipidClass, output);
                    break;

                case "CoQ":
                    OtherLipidMspGenerator.coenzymeQGenerator(lipidClass, output);
                    break;

                default:
                    Console.WriteLine("Error in lipidClass switch. Please check settings...");
                    Console.ReadKey();
                    break;
            }
        }


        public static void GenerateSmilesList(List<string> acylChain1, List<string> acylChain2,string lipidClass, string nameSurfix,string output)
        {
            var wholeChainList = new List<string>();
            var headerSmiles = SmilesLipidHeader.HeaderDictionary[lipidClass];
            var chain1Smiles = "";
            var chain2Smiles = "";
            var rawSmiles = "";
            var name = "";
            var smileslist = new List<string>();
            var smileslist2 = new List<string>();

            var meta = new MetaProperty();

            for (int i = 0; i < acylChain1.Count; i++)
            {
                for (int j = 0; j < acylChain2.Count; j++)
                {
                    var FA1 = acylChain1[i];
                    var FA2 = acylChain2[j];

                    if (AcylChainDic.PhenylFaChainDictionary.ContainsKey(FA1) == false || AcylChainDic.FattyAcylChainDictionary.ContainsKey(FA2) == false)
                    {
                        continue;
                    }

                    chain1Smiles = new List<string>(AcylChainDic.PhenylFaChainDictionary[FA1])[3];
                    chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[FA2])[3];

                    rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11";
                    name = lipidClass + nameSurfix + " " + FA1 + "_"+FA2;
                    meta = Common.getMetaProperty(rawSmiles);
                    smileslist.Add(name +"\t" + meta.Smiles);

                    smileslist2.Add(meta.ExactMass + "\t" + meta.Formula +"\t" + name + "\t" + meta.Smiles);
                }
            }


            using (var sw = new StreamWriter(output, false, Encoding.ASCII))
            {
                foreach (var smiles in smileslist)
                    sw.WriteLine(smiles);
            }

            using (var sw = new StreamWriter(output + ".meta.txt", false, Encoding.ASCII))
            {
                foreach (var line in smileslist2)
                    sw.WriteLine(line);
            }
        }

        public static void fromSMILEStoMeta(string inputFile, string outputFile)
        {
            var metaList = new List<string>();
            var meta = new MetaProperty();

            var adducts = new List<string>() { "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M-H]-", "[M+HCOO]-", "[M+CH3COO]-" };

            using (var sw = new StreamWriter(Path.GetDirectoryName(outputFile) + "\\" + Path.GetFileNameWithoutExtension(outputFile) + "_meta.txt", false, Encoding.ASCII))
            {
                sw.WriteLine(String.Join("\t", new string[] { "NAME", "ExactMass","LogP", "Formula", "SMILES","InChIKey", "[M+H]+", "[M+NH4]+", "[M+Na]+", "[M-H]-", "[M+HCOO]-", "[M+CH3COO]-" }));

                using (var sr = new StreamReader(inputFile, Encoding.ASCII))
                {
                    var line = "";

                    while ((line = sr.ReadLine()) != null)
                    {
                        var lineArray = line.Split('\t');
                        var name = lineArray[0];
                        var rawSmiles = lineArray[1];

                        meta = Common.getMetaProperty(rawSmiles);
                        var adductMzList = "";

                        foreach (var adduct in adducts)
                        {
                            adductMzList = adductMzList + "\t" +
                                (
                                meta.ExactMass + adductDic.adductIonDic[adduct].AdductIonMass
                                );
                        }

                        sw.WriteLine
                            (
                            name + "\t" + meta.ExactMass + "\t" + meta.LogP + "\t" + meta.Formula + "\t" + meta.Smiles + "\t" + meta.inChIKey + "\t" + adductMzList 
                            );

                    }
                }
            }
        }

    }
}
