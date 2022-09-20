using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using NCDK.Smiles;
using NCDK.Graphs.InChI;
using CompMs.Common.Lipidomics;



namespace CompMs.MspGenerator
{
    public class LipidStructureGenerator
    {
        public static string[] LipidInchikeySmiles(LbmClass lipidClass, string lipidname)
        {

            switch (lipidClass)
            {
                case LbmClass.MG:
                case LbmClass.DG:
                case LbmClass.TG:
                case LbmClass.LPC:
                case LbmClass.LPA:
                case LbmClass.LPE:
                case LbmClass.LPG:
                case LbmClass.LPI:
                case LbmClass.LPS:
                case LbmClass.LDGTS:
                case LbmClass.LDGCC:
                case LbmClass.PC:
                case LbmClass.PA:
                case LbmClass.PE:
                case LbmClass.PG:
                case LbmClass.PI:
                case LbmClass.PS:
                case LbmClass.BMP:
                case LbmClass.HBMP:
                case LbmClass.CL:
                case LbmClass.DLCL:
                case LbmClass.MLCL:
                case LbmClass.PMeOH:
                case LbmClass.PEtOH:
                case LbmClass.PBtOH:
                case LbmClass.MMPE:
                case LbmClass.DMPE:
                case LbmClass.DGDG:
                case LbmClass.MGDG:
                case LbmClass.SQDG:
                case LbmClass.DGTS:
                case LbmClass.DGTA:
                case LbmClass.DGCC:
                case LbmClass.DGGA:
                case LbmClass.CE:
                case LbmClass.BRSE:
                case LbmClass.CASE:
                case LbmClass.SISE:
                case LbmClass.STSE:
                case LbmClass.EGSE:
                case LbmClass.DEGSE:
                case LbmClass.DSMSE:
                case LbmClass.AHexCS:
                case LbmClass.AHexBRS:
                case LbmClass.AHexCAS:
                case LbmClass.AHexSIS:
                case LbmClass.AHexSTS:
                case LbmClass.DCAE:
                case LbmClass.GDCAE:
                case LbmClass.GLCAE:
                case LbmClass.TDCAE:
                case LbmClass.TLCAE:
                case LbmClass.LCAE:
                case LbmClass.KLCAE:
                case LbmClass.KDCAE:
                case LbmClass.SMGDG:
                case LbmClass.GPNAE:
                case LbmClass.MGMG:
                case LbmClass.DGMG:
                case LbmClass.Ac2PIM1:
                case LbmClass.Ac2PIM2:
                case LbmClass.Ac3PIM2:
                case LbmClass.Ac4PIM2:
                case LbmClass.NAE:
                case LbmClass.CAR:
                case LbmClass.EtherTG:
                case LbmClass.EtherDG:
                case LbmClass.EtherPC:
                case LbmClass.EtherPE:
                case LbmClass.EtherPS:
                case LbmClass.EtherPI:
                case LbmClass.EtherPG:
                case LbmClass.EtherLPC:
                case LbmClass.EtherLPE:
                case LbmClass.EtherLPS:
                case LbmClass.EtherLPI:
                case LbmClass.EtherLPG:
                case LbmClass.EtherMGDG:
                case LbmClass.EtherSMGDG:
                case LbmClass.EtherDGDG:
                    var chainStrings = acylChainStringSeparatorVS2(lipidname);

                    return GlyceroLipidInchikeySmiles(lipidClass, chainStrings, lipidname);

                case LbmClass.Cer_ADS:
                case LbmClass.Cer_AS:
                case LbmClass.Cer_BS:
                case LbmClass.Cer_BDS:
                case LbmClass.Cer_NDS:
                case LbmClass.Cer_NS:
                case LbmClass.Cer_NP:
                case LbmClass.Cer_AP:
                case LbmClass.HexCer_NS:
                case LbmClass.HexCer_NDS:
                case LbmClass.HexCer_AP:
                case LbmClass.Hex2Cer:
                case LbmClass.Hex3Cer:
                case LbmClass.MIPC:
                case LbmClass.CerP:
                case LbmClass.SM:
                case LbmClass.GD1a:
                case LbmClass.GD1b:
                case LbmClass.GD2:
                case LbmClass.GD3:
                case LbmClass.GM1:
                case LbmClass.GT1b:
                case LbmClass.GQ1b:
                case LbmClass.NGcGM3:
                case LbmClass.GM3:
                case LbmClass.Cer_HS:
                case LbmClass.Cer_HDS:
                case LbmClass.PE_Cer:
                case LbmClass.PI_Cer:
                case LbmClass.HexCer_HS:
                case LbmClass.HexCer_HDS:
                case LbmClass.SL:
                case LbmClass.SHexCer:
                    chainStrings = acylChainStringSeparatorVS2(lipidname);
                    return CeramideInchikeySmiles(lipidClass, chainStrings, lipidname);


                //case LbmClass.FA:
                //case LbmClass.NAGly:
                //case LbmClass.NAGlySer:
                //case LbmClass.NAOrn:

                //case LbmClass.Cer_EODS:
                //case LbmClass.Cer_EOS:
                //case LbmClass.Cer_NDOS:
                //case LbmClass.Cer_EBDS:
                //case LbmClass.HexCer_EOS:
                //case LbmClass.AHexCer:
                //case LbmClass.ASM:
                //case LbmClass.Cer_OS:


                //case LbmClass.OxTG:
                //case LbmClass.TG_EST:
                //case LbmClass.OxPC:
                //case LbmClass.OxPE:
                //case LbmClass.OxPG:
                //case LbmClass.OxPI:
                //case LbmClass.OxPS:
                //case LbmClass.EtherOxPC:
                //case LbmClass.EtherOxPE:
                //case LbmClass.LNAPE:
                //case LbmClass.LNAPS:
                //case LbmClass.ADGGA:
                //case LbmClass.SHex:
                //case LbmClass.SSulfate:
                //case LbmClass.BAHex:
                //case LbmClass.BASulfate:
                //case LbmClass.Vitamin_E:
                //case LbmClass.Vitamin_D:
                //case LbmClass.VAE:
                //case LbmClass.BileAcid:
                //case LbmClass.CoQ:
                //case LbmClass.OxFA:
                //case LbmClass.FAHFA:
                //case LbmClass.PhytoSph:
                //case LbmClass.DHSph:
                //case LbmClass.Sph:
                //case LbmClass.ST:


                default: return null;
            }

        }

        public static string[] LipidInchikeySmiles(LbmClass lipidClass, List<LipidChainInfo> chainList)
        {
            switch (lipidClass)
            {
                case LbmClass.MG:
                case LbmClass.DG:
                case LbmClass.TG:
                case LbmClass.LPC:
                case LbmClass.LPA:
                case LbmClass.LPE:
                case LbmClass.LPG:
                case LbmClass.LPI:
                case LbmClass.LPS:
                case LbmClass.LDGTS:
                case LbmClass.LDGCC:
                case LbmClass.PC:
                case LbmClass.PA:
                case LbmClass.PE:
                case LbmClass.PG:
                case LbmClass.PI:
                case LbmClass.PS:
                case LbmClass.BMP:
                case LbmClass.HBMP:
                case LbmClass.CL:
                case LbmClass.DLCL:
                case LbmClass.MLCL:
                case LbmClass.PMeOH:
                case LbmClass.PEtOH:
                case LbmClass.PBtOH:
                case LbmClass.MMPE:
                case LbmClass.DMPE:
                case LbmClass.DGDG:
                case LbmClass.MGDG:
                case LbmClass.SQDG:
                case LbmClass.DGTS:
                case LbmClass.DGTA:
                case LbmClass.DGCC:
                case LbmClass.DGGA:
                case LbmClass.CE:
                case LbmClass.BRSE:
                case LbmClass.CASE:
                case LbmClass.SISE:
                case LbmClass.STSE:
                case LbmClass.EGSE:
                case LbmClass.DEGSE:
                case LbmClass.DSMSE:
                case LbmClass.AHexCS:
                case LbmClass.AHexBRS:
                case LbmClass.AHexCAS:
                case LbmClass.AHexSIS:
                case LbmClass.AHexSTS:
                case LbmClass.DCAE:
                case LbmClass.GDCAE:
                case LbmClass.GLCAE:
                case LbmClass.TDCAE:
                case LbmClass.TLCAE:
                case LbmClass.LCAE:
                case LbmClass.KLCAE:
                case LbmClass.KDCAE:
                case LbmClass.SMGDG:
                case LbmClass.GPNAE:
                case LbmClass.MGMG:
                case LbmClass.DGMG:
                case LbmClass.Ac2PIM1:
                case LbmClass.Ac2PIM2:
                case LbmClass.Ac3PIM2:
                case LbmClass.Ac4PIM2:
                case LbmClass.NAE:
                case LbmClass.CAR:
                case LbmClass.EtherTG:
                case LbmClass.EtherDG:
                case LbmClass.EtherPC:
                case LbmClass.EtherPE:
                case LbmClass.EtherPS:
                case LbmClass.EtherPI:
                case LbmClass.EtherPG:
                case LbmClass.EtherLPC:
                case LbmClass.EtherLPE:
                case LbmClass.EtherLPS:
                case LbmClass.EtherLPI:
                case LbmClass.EtherLPG:
                case LbmClass.EtherMGDG:
                case LbmClass.EtherSMGDG:
                case LbmClass.EtherDGDG:

                    var chainStrings = new List<string>();

                    var chainOrderList = chainList.OrderBy(a => a.SnPosition)
                        .ThenByDescending(a => a.EtherFlag)
                        .ThenBy(a => a.DoubleNum)
                        .ThenBy(a => a.CNum);

                    var lipidname = lipidClass.ToString().Replace("Ether", "") + " ";
                    var count = 0;
                    foreach (var item in chainOrderList)
                    {
                        var lipidChainString = GenerateChainString(item); //return 18:2(9Z,12Z)
                        chainStrings.Add(lipidChainString);
                        if (count > 0)
                        {
                            if (item.SnPosition != 0)
                            {
                                lipidname = lipidname + "/" + lipidChainString;
                            }
                            else
                            {
                                lipidname = lipidname + "_" + lipidChainString;
                            }
                        }
                        else
                        {
                            lipidname = lipidname + lipidChainString;
                        }
                        count = count + 1;
                    }

                    return GlyceroLipidInchikeySmiles(lipidClass, chainStrings, lipidname);

                //case LbmClass.Cer_ADS:
                //case LbmClass.Cer_AS:
                //case LbmClass.Cer_BS:
                //case LbmClass.Cer_BDS:
                //case LbmClass.Cer_NDS:
                //case LbmClass.Cer_NS:
                //case LbmClass.Cer_NP:
                //case LbmClass.Cer_AP:
                //case LbmClass.HexCer_NS:
                //case LbmClass.HexCer_NDS:
                //case LbmClass.HexCer_AP:
                //case LbmClass.Hex2Cer:
                //case LbmClass.Hex3Cer:
                //case LbmClass.MIPC:
                //case LbmClass.CerP:
                //case LbmClass.SM:
                //case LbmClass.GD1a:
                //case LbmClass.GD1b:
                //case LbmClass.GD2:
                //case LbmClass.GD3:
                //case LbmClass.GM1:
                //case LbmClass.GT1b:
                //case LbmClass.GQ1b:
                //case LbmClass.NGcGM3:
                //case LbmClass.GM3:
                //case LbmClass.Cer_HS:
                //case LbmClass.Cer_HDS:
                //case LbmClass.PE_Cer:
                //case LbmClass.PI_Cer:
                //case LbmClass.HexCer_HS:
                //case LbmClass.HexCer_HDS:
                //case LbmClass.SL:
                //case LbmClass.SHexCer:
                //    return CeramideInchikeySmiles(lipidClass, lipidname);


                //case LbmClass.FA:
                //case LbmClass.NAGly:
                //case LbmClass.NAGlySer:
                //case LbmClass.NAOrn:

                //case LbmClass.Cer_EODS:
                //case LbmClass.Cer_EOS:
                //case LbmClass.Cer_NDOS:
                //case LbmClass.Cer_EBDS:
                //case LbmClass.HexCer_EOS:
                //case LbmClass.AHexCer:
                //case LbmClass.ASM:
                //case LbmClass.Cer_OS:


                //case LbmClass.OxTG:
                //case LbmClass.TG_EST:
                //case LbmClass.OxPC:
                //case LbmClass.OxPE:
                //case LbmClass.OxPG:
                //case LbmClass.OxPI:
                //case LbmClass.OxPS:
                //case LbmClass.EtherOxPC:
                //case LbmClass.EtherOxPE:
                //case LbmClass.LNAPE:
                //case LbmClass.LNAPS:
                //case LbmClass.ADGGA:
                //case LbmClass.SHex:
                //case LbmClass.SSulfate:
                //case LbmClass.BAHex:
                //case LbmClass.BASulfate:
                //case LbmClass.Vitamin_E:
                //case LbmClass.Vitamin_D:
                //case LbmClass.VAE:
                //case LbmClass.BileAcid:
                //case LbmClass.CoQ:
                //case LbmClass.OxFA:
                //case LbmClass.FAHFA:
                //case LbmClass.PhytoSph:
                //case LbmClass.DHSph:
                //case LbmClass.Sph:
                //case LbmClass.ST:


                default: return null;
            }

        }


        public static string[] GlyceroLipidInchikeySmiles(LbmClass lipidClass, List<string> chainStrings, string lipidname) // ex.PC P-16:1/18:2(9Z,12Z)
        {
            var chainDictionary = AcylChainDic.FattyAcylChainDictionary;
            string[] result = new string[3];
            var chainSmiles = new List<string>();
            var chainCount = 10;
            foreach (var item in chainStrings)
            {
                var chain = LipidChainSmilesGen(item, chainDictionary);
                chain = chain + ("%" + chainCount);
                chainSmiles.Add(chain);
                chainCount = chainCount + 1;
            }

            var lipidClassString = lipidClass.ToString();
            if (lipidClassString == "LPC" || lipidClassString == "LPE" ||
                lipidClassString == "LPI" || lipidClassString == "LPS" ||
                lipidClassString == "LPG" || lipidClassString == "LPA" ||
                lipidClassString == "EtherLPC" || lipidClassString == "EtherLPE" ||
                lipidClassString == "EtherLPI" || lipidClassString == "EtherLPS" ||
                lipidClassString == "EtherLPG" ||
                lipidClassString == "LDGTS" || lipidClassString == "LDGCC"
                )
            {
                if (chainStrings.Count > 1)// need 2 chain adopt
                {
                    lipidClassString = lipidClassString.Replace("L", "");
                }
            }

            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClassString];

            var rawSmiles = headerSmiles + string.Join(".", chainSmiles);

            var SmilesParser = new SmilesParser();
            var SmilesGenerator = new SmilesGenerator(SmiFlavors.StereoCisTrans);
            var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
            var smiles = SmilesGenerator.Create(iAtomContainer);
            var InChIGeneratorFactory = new InChIGeneratorFactory();
            var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer).GetInChIKey();

            result[0] = lipidname;
            result[1] = InChIKey;
            result[2] = smiles;

            //Console.ReadLine();
            return result;
        }

        public static string[] CeramideInchikeySmiles(LbmClass lipidClass, List<string> chainStrings, string lipidname) // 
        {
            string[] result = new string[3];
            var chainSmiles = new List<string>();
            var chainCount = 20;
            var lipidClassString = lipidClass.ToString();
            if (lipidClassString == "SHexCer" || lipidClassString == "SM" ||
                lipidClassString == "SL" || lipidClassString == "PI_Cer" ||
                lipidClassString == "PE_Cer")
            {
                if (chainStrings[0].Contains("3O"))
                {
                    lipidClassString = lipidClassString + "+O";
                }
            }
            var chainDictionary = Common.CeramideChainConbinationDicList(lipidClassString);
            for (int i = 0; i < chainStrings.Count; i++)
            {
                var lipidChain = chainStrings[i].Split(';')[0];
                var chain = LipidChainSmilesGen(lipidChain, chainDictionary[i]);
                chain = chain + "%" + chainCount;
                chainSmiles.Add(chain);
                chainCount = chainCount + 10;
            }

            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass.ToString()];

            var rawSmiles = headerSmiles + string.Join(".", chainSmiles);

            var SmilesParser = new SmilesParser();
            var SmilesGenerator = new SmilesGenerator(SmiFlavors.StereoCisTrans);
            var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
            var smiles = SmilesGenerator.Create(iAtomContainer);
            var InChIGeneratorFactory = new InChIGeneratorFactory();
            var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer).GetInChIKey();

            result[0] = lipidname;
            result[1] = InChIKey;
            result[2] = smiles;

            //Console.ReadLine();
            return result;
        }

        public static string LipidChainSmilesGen(string chainString, Dictionary<string, List<string>> chainDictionary)
        {
            var chainString2 = Regex.Replace(chainString, @"[OP\)]-", "").Split('(');

            var cNum = Int32.Parse(chainString2[0].Split(':')[0]);
            var doubleNum = Int32.Parse(chainString2[0].Split(':')[1]);
            if (cNum + ":" + doubleNum == "0:0")
            {
                return "H";
            }

            if (chainString.Contains("P-"))
            {
                chainDictionary = AcylChainDic.etherChainPlasmenylDictionary;
                doubleNum = doubleNum + 1;
            }
            else if (chainString.Contains("O-"))
            {
                chainDictionary = AcylChainDic.etherChainDictionary;
            }
            var chainSmiles = Regex.Replace(chainDictionary[cNum + ":" + doubleNum][3], @"[\\/]", "");

            if (chainString2.Length > 1)
            {
                chainSmiles = chainSmiles.Replace("C=C", "CC");
                var doublePositions = chainString2[1].Replace(")", "");

                var doublePositionArray = doublePositions.Split(',');
                var count = 0;
                foreach (var item in doublePositionArray)
                {
                    var position = Int32.Parse(Regex.Replace(item, @"[^0-9]", ""));
                    chainSmiles = chainSmiles.Insert((cNum - position), "X");
                    chainSmiles = chainSmiles.Replace("XC", "X");
                    count = count + 1;

                    if (item.Contains("E"))
                    {
                        chainSmiles = chainSmiles.Replace("X", "E");
                    }
                    else if (item.Contains("Z"))
                    {
                        chainSmiles = chainSmiles.Replace("X", "Z");
                    }
                    else
                    {
                        chainSmiles = chainSmiles.Replace("X", "Y");

                    }

                }
                if (chainString.Contains("P-"))
                {
                    chainSmiles = Regex.Replace(chainSmiles, @"CC\Z", @"/C=C\");
                }
                chainSmiles = chainSmiles.Replace("CE", @"\C=C\");
                chainSmiles = chainSmiles.Replace("CZ", @"/C=C\");
                chainSmiles = chainSmiles.Replace("Y", @"=C");
            }

            return chainSmiles;
        }

        public static string GenerateChainString(LipidChainInfo LipidChainInfo)
        {
            var lipidChainString = LipidChainInfo.CNum + ":" + LipidChainInfo.DoubleNum;
            if (LipidChainInfo.EtherFlag)
            {
                lipidChainString = "O-" + lipidChainString;
            }

            if (LipidChainInfo.DoublePosition != null)
            {
                var doublePositionList = LipidChainInfo.DoublePosition.ToList();
                if (LipidChainInfo.EtherFlag && doublePositionList.Contains("1"))
                {
                    lipidChainString = "P-" + LipidChainInfo.CNum + ":" + (LipidChainInfo.DoubleNum - 1);
                    doublePositionList.Remove("1");
                }
                if (LipidChainInfo.DoubleNum > 0)
                {
                    lipidChainString = lipidChainString + "(" + string.Join(",", doublePositionList) + ")";

                }

            }

            return lipidChainString;
        }

        public string AcylChainSmilesGen(AcylChain acyl)
        {
            string chainSmiles = new string('C', acyl.CarbonCount) + "=O";
            var doubleBond = acyl.DoubleBond;
            var oxidized = acyl.Oxidized;
            if (doubleBond.UnDecidedCount > 0)
            {
                return null;
            }
            if (acyl.CarbonCount + ":" + acyl.DoubleBondCount == "0:0")
            {
                return "H";
            }
            if (doubleBond.Count == 0)
            {
                return chainSmiles;
            }
            if (doubleBond.DecidedCount > 0)
            {
                foreach (var item in doubleBond.Bonds)
                {
                    var reversePosition = acyl.CarbonCount - item.Position;
                    var state = item.State.ToString()[0].ToString().ToUpper();
                    chainSmiles = chainSmiles.Remove(reversePosition, 1).Insert(reversePosition, state);
                }
                chainSmiles = chainSmiles.Replace("CE", @"\C=C\");
                chainSmiles = chainSmiles.Replace("CZ", @"/C=C\");
                chainSmiles = chainSmiles.Replace("CU", @"C=C");
            }
            else
            {
                return null;
            }

            foreach (var item in oxidized.Oxidises)
            {
                var oxPosition = item;
                chainSmiles = chainSmiles.Insert((acyl.CarbonCount - oxPosition), "(O)");
            }

            return chainSmiles;
        }






        private static List<string> acylChainStringSeparatorVS2(string moleculeString)
        {

            if (moleculeString.Split(' ').Length == 1) return null;

            // pattern [1] ADGGA 12:0_12:0_12:0
            // pattern [2] AHexCer (O-14:0)16:1;2O/14:0;O
            // pattern [3] SM 30:1;2O(FA 14:0)
            // pattern [4] Cer 14:0;2O/12:0;(3OH)(FA 12:0) -> [0]14:0;2O, [1]12:0;(3OH), [3]12:0
            // pattern [5] Cer 14:1;2O/12:0;(2OH)
            // pattern [6] DGDG O-8:0_2:0
            // pattern [7] LNAPS 2:0/N-2:0 
            // pattern [8] SM 30:1;2O(FA 14:0)
            // pattern [9] ST 28:2;O;Hex;PA 12:0_12:0
            // pattern [10] SE 28:2/8:0
            // pattern [11] TG 16:0_16:1_18:0;O(FA 16:0)
            // pattern [12]  LPE-N (FA 16:0)18:1  (LNAPE,LNAPS)
            var headerString = moleculeString.Split(' ')[0].Trim();
            string chainString = string.Empty;
            if (headerString == "SE" || headerString == "ST" || headerString == "SG" || headerString == "BA" || headerString == "ASG")
            {
                RetrieveSterolHeaderChainStrings(moleculeString, out headerString, out chainString);
            }
            else
            {
                chainString = moleculeString.Substring(headerString.Length + 1);
            }
            List<string> chains = null;
            string[] acylArray = null;

            // d-substituted compound support 20220920
            Regex reg = new Regex(@"\(d([0-9]*)\)");
            chainString = reg.Replace(chainString, "");

            var pattern2 = @"(\()(?<chain1>.+?)(\))(?<chain2>.+?)(/)(?<chain3>.+?$)";
            var pattern3 = @"(?<chain1>.+?)(\(FA )(?<chain2>.+?)(\))";
            var pattern4 = @"(?<chain1>.+?)(/)(?<chain2>.+?)(\(FA )(?<chain3>.+?)(\))";
            var pattern12 = @"(\(FA )(?<chain2>.+?)(\))(?<chain1>.+?$)";

            if (chainString.Contains("/") && chainString.Contains("(FA"))
            { // pattern 4
                var regexes = Regex.Match(chainString, pattern4).Groups;
                chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value, regexes["chain3"].Value };
            }
            else if (chainString.Contains("(FA"))
            {  // pattern 3
                var regexes = Regex.Match(chainString, pattern3).Groups;
                var chain1strings = regexes["chain1"].Value;
                if (chain1strings.Contains("_"))
                {
                    chains = new List<string>();
                    foreach (var chainstring in chain1strings.Split('_').ToArray()) chains.Add(chainstring);
                    chains.Add(regexes["chain2"].Value);
                }
                else if (chain1strings == "") // pattern 12
                {
                    regexes = Regex.Match(chainString, pattern12).Groups;
                    chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value };
                    //Console.WriteLine();
                }
                else
                {
                    chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value };
                }
                //Console.WriteLine();
            }
            else if (chainString.Contains("(O-") && chainString.Contains("/"))
            { // pattern 2
                var regexes = Regex.Match(chainString, pattern2).Groups;
                chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value, regexes["chain3"].Value };
                //Console.WriteLine();
            }
            else
            {
                chainString = chainString.Replace('/', '_');
                acylArray = chainString.Split('_');
                chains = new List<string>();
                for (int i = 0; i < acylArray.Length; i++)
                {
                    if (i == 0 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                    if (i == 1 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                    if (i == 2 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                    if (i == 3 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                }
            }

            return chains;
        }

        public static void RetrieveSterolHeaderChainStrings(string moleculeString, out string headerString, out string chainString)
        {

            headerString = string.Empty;
            chainString = string.Empty;
            if (moleculeString.Contains("/"))
            {
                var splitterArray = moleculeString.Split('/');
                chainString = splitterArray[splitterArray.Length - 1];
            }
            else
            {
                var splitterArray = moleculeString.Split(' ');
                chainString = splitterArray[splitterArray.Length - 1];
            }
        }




    }

    public class LipidChainInfo
    {
        private int cNum;
        private int doubleNum;
        private string[] doublePosition;
        private int oxNum;
        private int snPosition;
        private bool etherFlag;

        public LipidChainInfo()
        {
            cNum = 0;
            doubleNum = 0;
            doublePosition = null;
            oxNum = 0;
            snPosition = 0;
            etherFlag = false;
        }
        public int CNum { get; set; }
        public int DoubleNum { get; set; }
        public string[] DoublePosition { get; set; }
        public int OxNum { get; set; }
        public int SnPosition { get; set; }
        public bool EtherFlag { get; set; }

    }

}
