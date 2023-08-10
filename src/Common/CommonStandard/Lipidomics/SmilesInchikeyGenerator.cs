using System;
using System.Collections.Generic;
using System.Text;
using NCDK.Smiles;
using NCDK.Graphs.InChI;
using System.Linq;

namespace CompMs.Common.Lipidomics
{
    public class SmilesInchikeyGenerator
    {
        public static SmilesInchikey Generate(Lipid lipid)
        {
            if (lipid.Chains is PositionLevelChains plChains)
            {
                var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                var headerSmiles = smilesHeaderDict[lipid.LipidClass.ToString()];
                if (headerSmiles==null) return null;

                var chainList = new List<string>();
                var jointPosition = 10;
                
                foreach (var chain in lipid.Chains.GetDeterminedChains())
                {
                    var oxidized = chain.Oxidized;
                    var doubleBond = chain.DoubleBond;
                    if (oxidized.UnDecidedCount > 0 || doubleBond.UnDecidedCount > 0)
                    {
                        return null;
                    }
                    var smilesInchikeyGenerator = new SmilesInchikeyGenerator();
                    var chainSmiles = smilesInchikeyGenerator.ChainSmilesGen(chain);
                    if (chain is SphingoChain)
                    {
                        chainSmiles = chainSmiles.Remove(0,5).Insert(1, "%" + jointPosition);
                    }
                    else
                    {
                        chainSmiles = chainSmiles.Insert(1, "%" + jointPosition);
                    }
                    chainList.Add(chainSmiles);
                    jointPosition = jointPosition + 10;
                }

                var rawSmiles = headerSmiles + string.Join(".", chainList);

                var SmilesParser = new SmilesParser();
                var SmilesGenerator = new SmilesGenerator(SmiFlavors.StereoCisTrans);
                var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
                var smiles = SmilesGenerator.Create(iAtomContainer);
                var InChIGeneratorFactory = new InChIGeneratorFactory();
                var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer).GetInChIKey();

                return new SmilesInchikey() { Smiles = smiles, InchiKey = InChIKey };
            }
            return null;
        }
        public string ChainSmilesGen(IChain chain)
        {
            var doubleBond = chain.DoubleBond;
            var oxidized = chain.Oxidized;

            if (doubleBond.UnDecidedCount > 0)
            {
                return null;
            }
            if (chain.CarbonCount + ":" + chain.DoubleBondCount == "0:0")
            {
                return "H";
            }

            string chainSmiles = "";
            for (int i = 1; i < chain.CarbonCount + 1; i++)
            {
                chainSmiles = chainSmiles + "C";
                if (oxidized.Oxidises.Contains(i))
                {
                    chainSmiles = chainSmiles + "(O)";
                }

                if (doubleBond.Bonds.Any(n => i == n.Position))
                {
                    var item = doubleBond.Bonds.Where(n => i == n.Position).ToArray();
                    //chainSmiles = chainSmiles + "=";
                    chainSmiles = chainSmiles + item[0].State.ToString().ToUpper()[0];
                }
            }

            if (chain is AcylChain)
            {
                chainSmiles = chainSmiles.Insert(1,"(=O)");
            }

            chainSmiles = chainSmiles.Replace("CEC", @"\C=C\");
            chainSmiles = chainSmiles.Replace("CZC", @"/C=C\");
            chainSmiles = chainSmiles.Replace("CUC", @"C=C");

            return chainSmiles;
        }
    }
    public class SmilesInchikey
    {
        private string smiles;
        private string inchikey;

        public SmilesInchikey()
        {
            smiles = null;
            inchikey = null;
        }
        public string Smiles { get; set; }
        public string InchiKey { get; set; }
    }
    public sealed class SmilesLipidHeader
    {
        private SmilesLipidHeader() { }

        public static Dictionary<string, string> HeaderDictionary = new Dictionary<string, string>()
        {
            {"PC", "C(O%10)C(O%20)COP([O-])(=O)OCC[N+](C)(C)C."},
            {"PA", "C(O%10)C(O%20)COP(O)(O)=O."},
            {"PE", "C(O%10)C(O%20)COP(O)(=O)OCCN."},
            {"PG", "C(O)(CO)COP(O)(=O)OCC(O%20)C(O%10)."},
            {"PI", "C(O%10)C(O%20)COP(O)(=O)OC1C(O)C(O)C(O)C(O)C1O."},
            {"PS", "C(N)(COP(O)(=O)OCC(O%20)C(O%10))C(O)=O."},
            {"PEtOH", "C(O%10)C(O%20)COP(O)(OCC)=O."},
            {"PMeOH", "C(O%10)C(O%20)COP(O)(OC)=O."},

            //{"LPC", "C(O%10)C(O)COP([O-])(=O)OCC[N+](C)(C)C."},
            ////{"LPCSN1", "C(O%10)C(O)COP([O-])(=O)OCC[N+](C)(C)C."},
            //{"LPA", "C(O%10)C(O)COP(O)(O)=O."},
            //{"LPE", "C(O%10)C(O)COP(O)(=O)OCCN."},
            //{"LPG", "C(O)(CO)COP(O)(=O)OCC(O)C(O%10)."},
            //{"LPI", "C(O%10)C(O)COP(O)(=O)OC1C(O)C(O)C(O)C(O)C1O."},
            //{"LPS", "C(N)(COP(O)(=O)OCC(O)C(O%10))C(O)=O."},
            //{"EtherLPC", "C(O)C(O%10)COP([O-])(=O)OCC[N+](C)(C)C."},
            //{"EtherLPE", "C(O%10)C(O)COP(O)(=O)OCCN."},
            ////{"EtherLPE_P", "C(O%10)C(O)COP(O)(=O)OCCN."},
            //{"EtherLPG", "C(O)(CO)COP(O)(=O)OCC(O)C(O%10)."},
            //{"EtherLPI", "C(O%10)C(O)COP(O)(=O)OC1C(O)C(O)C(O)C(O)C1O."},
            //{"EtherLPS", "C(N)(COP(O)(=O)OCC(O)C(O%10))C(O)=O."},
            //{"EtherLPA", "C(O%10)C(O)COP(O)(O)=O."},
            {"TG", "C(O%10)C(O%20)C(O%30)."},
            {"DG", "C(O%10)C(O%20)C(O)."},
            {"MG", "C(O%10)C(O)C(O)."},
            {"BMP", "OCC(O%10)COP(O)(=O)OCC(O%20)C(O)."},
            {"DGDG", "C(O%10)C(O%20)COC1OC(COC2OC(CO)C(O)C(O)C2O)C(O)C(O)C1O."},
            {"MGDG", "C(O%10)C(O%20)COC1OC(CO)C(O)C(O)C1O."},
            {"SQDG", "C(O%10)C(O%20)COC1OC(CS(O)(=O)=O)C(O)C(O)C1O."},
            {"DGTS", "C(O%10)C(O%20)COCCC(C([O-])=O)[N+](C)(C)C."},
            {"DGGA", "OC1C(O)C(OCC(O%10)C(O%20))OC(C1O)C(O)=O."},
            {"DLCL", "OC(CO%10)COP(O)(=O)OCC(O)COP(O)(=O)OCC(O)CO%20."},
            {"SMGDG", "OCC1OC(OCC(CO%10)O%20)C(O)C(OS(O)(=O)=O)C1O."},
            {"DGCC", "C[N+](C)(C)CCOC(OCC(CO%10)O%20)C([O-])=O."},

            {"EtherPC", "C(O%10)C(O%20)COP([O-])(=O)OCC[N+](C)(C)C."},
            {"EtherPE", "C(O%10)C(O%20)COP(O)(=O)OCCN."},
            //{"EtherPE_O", "C(O%10)C(O%20)COP(O)(=O)OCCN."},
            {"EtherPG", "C(O)(CO)COP(O)(=O)OCC(O%20)C(O%10)."},
            {"EtherPI", "C(O%10)C(O%20)COP(O)(=O)OC1C(O)C(O)C(O)C(O)C1O."},
            {"EtherPS", "C(N)(COP(O)(=O)OCC(O%20)C(O%10))C(O)=O."},
            {"EtherDG", "C(O%10)C(O%20)C(O)."},
            {"EtherDGDG", "C(O%10)C(O%20)COC1OC(COC2OC(CO)C(O)C(O)C2O)C(O)C(O)C1O."},
            {"EtherMGDG", "C(O%10)C(O%20)COC1OC(CO)C(O)C(O)C1O."},
            {"EtherSMGDG", "OCC1OC(OCC(CO%10)O%20)C(O)C(OS(O)(=O)=O)C1O."},

            {"OxPC", "C(O%10)C(O%20)COP([O-])(=O)OCC[N+](C)(C)C."},
            {"OxPE", "C(O%10)C(O%20)COP(O)(=O)OCCN."},
            {"OxPG", "C(O)(CO)COP(O)(=O)OCC(O%20)C(O%10)."},
            {"OxPI", "C(O%10)C(O%20)COP(O)(=O)OC1C(O)C(O)C(O)C(O)C1O."},
            {"OxPS", "C(N)(COP(O)(=O)OCC(O%20)C(O%10))C(O)=O."},

            {"EtherOxPC", "C(O%10)C(O%20)COP([O-])(=O)OCC[N+](C)(C)C."},
            {"EtherOxPE", "C(O%10)C(O%20)COP(O)(=O)OCCN."},

            {"LNAPE", "OC(CO%10)COP(O)(=O)OCCN%20."},
            {"LNAPS", "OC(CO%10)COP(O)(=O)OCC(N%20)C(O)=O."},

            {"LDGTS", "C(O%10)C(O)COCCC(C([O-])=O)[N+](C)(C)C."},
            {"LDGCC", "C[N+](C)(C)CCOC(OCC(CO%10)O)C([O-])=O."},

            {"MLCL", "OC(CO%10)COP(O)(=O)OCC(O)COP(O)(=O)OCC(O%20)CO%30."},
            {"HBMP", "OCC(O%10)COP(O)(=O)OCC(O%20)C(O%30)."},
            {"ADGGA", "OC1C(O)C(OC(OCC(O%10)C(O%20))C1(O%30))C(O)=O."},
            {"OxTG", "C(O%10)C(O%20)C(O%30)."},
            {"EtherTG", "C(O%10)C(O%20)C(O%30)."},

            {"CL", "OC(COP(O)(=O)OCC(O%10)C(O%20))COP(O)(=O)OCC(O%30)C(O%40)."},
            {"TG_EST", "C(O%20)C(O%30)C(O%40)."},

            {"MMPE", "C(O%10)C(O%20)COP(O)(=O)OCCNC."},
            {"DMPE", "C(O%10)C(O%20)COP(O)(=O)OCCN(C)C."},

            {"GPNAE", "OCC(O)COP(O)(=O)OCCN%10."},
            {"DGMG", "C(O%10)C(O)COC1OC(COC2OC(CO)C(O)C(O)C2O)C(O)C(O)C1O."},
            {"MGMG", "C(O%10)C(O)COC1OC(CO)C(O)C(O)C1O."},

            //{"NAPE", "OP(=O)(OCC%20)OCC(O%10)C(O%20)."},

            //ceramide 2chains
            {"Cer_ADS", "OCC%10N%20."},
            {"Cer_AP", "OCC%10N%20."},
            {"Cer_AS", "OCC%10N%20."},
            {"Cer_BDS", "OCC%10N%20."},
            {"Cer_BS", "OCC%10N%20."},
            {"Cer_HDS", "OCC%10N%20."},
            {"Cer_HS", "OCC%10N%20."},
            {"Cer_NDS", "OCC%10N%20."},
            {"Cer_NP", "OCC%10N%20."},
            {"Cer_NS", "OCC%10N%20."},
            {"HexCer_ADS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_AP", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_AS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_BDS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_BS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_HDS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_HS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_NDS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_NP", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_NS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},
            {"HexCer_OS", "OCC1OC(OCC%10N%20)C(O)C(O)C1O."},

            {"GM1", "CC(=O)NC1C(O)CC(OC2C(O)C(OC3C(O)C(O)C(OCC%10N%20)OC3CO)OC(CO)C2OC2OC(CO)C(O)C(OC3OC(CO)C(O)C(O)C3O)C2NC(C)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GM3", "CC(O)=NC1C(O)CC(OC2C(O)C(CO)OC(OC3C(CO)OC(OCC%10N%20)C(O)C3O)C2O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GD1a", "CC(=O)NC1C(O)CC(OC2C(O)C(CO)OC(OC3C(O)C(CO)OC(OC4C(CO)OC(OC5C(O)C(O)C(OCC%10N%20)OC5CO)C(O)C4OC4(CC(O)C(NC(C)=O)C(O4)C(O)C(O)CO)C(O)=O)C3NC(C)=O)C2O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GD1b", "CC(=O)NC1C(O)CC(OC(CO)C(O)C2OC(CC(O)C2NC(C)=O)(OC2C(O)C(OC3C(O)C(O)C(OCC%10N%20)OC3CO)OC(CO)C2OC2OC(CO)C(O)C(OC3OC(CO)C(O)C(O)C3O)C2NC(C)=O)C(O)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GD2", "CC(=O)NC1C(O)CC(OC(CO)C(O)C2OC(CC(O)C2NC(C)=O)(OC2C(O)C(OC3C(O)C(O)C(OCC%10N%20)OC3CO)OC(CO)C2OC2OC(CO)C(O)C(O)C2NC(C)=O)C(O)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GD3", "CC(=O)NC1C(O)CC(OCC(O)C(O)C2OC(CC(O)C2NC(C)=O)(OC2C(O)C(CO)OC(OC3C(O)C(O)C(OCC%10N%20)OC3CO)C2O)C(O)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GT1b", "CC(=O)NC1C(O)CC(OC(CO)C(O)C2OC(CC(O)C2NC(C)=O)(OC2C(O)C(OC3C(O)C(O)C(OCC%10N%20)OC3CO)OC(CO)C2OC2OC(CO)C(O)C(OC3OC(CO)C(O)C(OC4(CC(O)C(NC(C)=O)C(O4)C(O)C(O)CO)C(O)=O)C3O)C2NC(C)=O)C(O)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"GQ1b", "CC(=O)NC1C(O)CC(OC(CO)C(O)C2OC(CC(O)C2NC(C)=O)(OC2C(O)C(CO)OC(OC3C(O)C(CO)OC(OC4C(CO)OC(OC5C(O)C(O)C(OCC%10N%20)OC5CO)C(O)C4OC4(CC(O)C(NC(C)=O)C(O4)C(O)C(CO)OC4(CC(O)C(NC(C)=O)C(O4)C(O)C(O)CO)C(O)=O)C(O)=O)C3NC(C)=O)C2O)C(O)=O)(OC1C(O)C(O)CO)C(O)=O."},
            {"NGcGM3", "OCC(O)C(O)C1OC(CC(O)C1N=C(O)CO)(OC1C(O)C(CO)OC(OC2C(O)C(O)C(OCC%10N%20)OC2CO)C1O)C(O)=O."},
            {"CerP", "OP(O)(=O)OCC%10N%20."},
            {"Hex2Cer", "N%20C%10COC1OC(CO)C(OC2OC(CO)C(O)C(O)C2O)C(O)C1O."},
            {"Hex3Cer", "N%20C%10COC1OC(CO)C(OC2OC(CO)C(OC3OC(CO)C(O)C(O)C3O)C(O)C2O)C(O)C1O."},

            {"MIPC", "OCC1OC(OC2C(O)C(O)C(O)C(O)C2OP(O)(=O)OCC(%10)N%20)C(O)C(O)C1O."},


            ////ceramide need chain conbination
            {"SHexCer", "OCC1OC(OCC%10N%20)C(O)C(OS(O)(=O)=O)C1O."},
            //{"SHexCer+O", "OCC1OC(OCC%10N%20)C(O)C(OS(O)(=O)=O)C1O."},
            {"SM", "C[N+](C)(C)CCOP([O-])(=O)OCC%10N%20."},
            //{"SM+O", "C[N+](C)(C)CCOP([O-])(=O)OCC%10N%20."},
            {"SL", "C%10N%20CS(O)(=O)=O."},
            //{"SL+O", "C%10N%20CS(O)(=O)=O."},
            //{"PI_Cer", "OC1C(O)C(O)C(OP(O)(=O)OCC(%10)N%20)C(O)C1O."},
            ////{"PI_Cer_d+O", "OC1C(O)C(O)C(OP(O)(=O)OCC(%10)N%20)C(O)C1O."},
            //{"PE_Cer", "NCCOP(O)(=O)OCC%10N%20."},
            ////{"PE_Cer_d+O", "NCCOP(O)(=O)OCC%10N%20."},

            ////ceramide 3chains
            //{"AHexCer", "OC1C(O)C(CO)OC(OCC%20N%30)C1O%10."},
            //{"Cer_EBDS", "OCC%20N%30."},
            //{"Cer_EODS", "OCC%20N%30."},
            //{"Cer_EOS", "OCC%20N%30."},
            //{"HexCer_EOS", "OCC1OC(OCC%20N%30)C(O)C(O)C1O."},
            //{"ASM", "C[N+](C)(C)CCOP([O-])(=O)OCC%20N%30."},

            ////ceramide single chains
            //{"Sph", "NC(%10)CO."},
            //{"DHSph", "NC(%10)CO."},
            //{"PhytoSph", "NC(%10)CO."},


            ////{"PG-Cer", "C(O)(CO)COP(O)(=O)OCC%20N%30."},
            ////{"PS-Cer", "C(N)(COP(O)(=O)OCC%20N%30)C(O)=O."},
            ////{"lysoDGDG", "C%10C(O)COC1OC(COC2OC(CO)C(O)C(O)C2O)C(O)C(O)C1O."},
            ////{"lysoMGDG", "C%10C(O)COC1OC(CO)C(O)C(O)C1O."},


            ////FAHFA
            //{"FAHFA", "O%20."},
            //{"AAHFA", "O%20."},
            //{"NAGlySer_FAHFA", "OCC(NC(=O)CN%20)C(O)=O."},
            //{"NAGly_FAHFA", "OC(=O)CN%20."},
            //{"NAOrn_FAHFA", "NCCCC(N%20)C(O)=O."},

            //// single acyl chain
            //{"CAR", "C[N+](C)(C)CC(CC([O-])=O)O%10."},  //  old SMILES {"CAR", "C[N+](C)(C)CC(CC(O)=O)O%10."},  20200713 adduct change [M+] -> [M+H]+
            //{"NAE", "N%10CCO."},
            //{"VAE", "CC(=CCO%10)C=CC=C(C)C=CC1=C(C)CCCC1(C)C."},
            //{"NAGlySer_OxFA", "OCC(NC(=O)CN%20)C(O)=O."},
            //{"NAGly_OxFA", "OC(=O)CN%20."},
            //{"NAOrn_OxFA", "NCCCC(N%20)C(O)=O."},
            //{"NATau_FA", "OS(=O)(=O)CCN%20."},
            //{"NAPhe_OxFA", "OC(=O)C(CC1=CC=CC=C1)N%20."},



            //steroid and cholic acid 
            {"DCAE", "CC(CCC(O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CC(O)C12C."},
            {"GDCAE", "CC(CCC(=O)NCC(O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CC(O)C12C."},
            {"GLCAE", "CC(CCC(=O)NCC(O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CCC12C."},
            {"TDCAE", "CC(CCC(O)=NCCS(O)(=O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CC(O)C12C."},
            {"TLCAE", "CC(CCC(O)=NCCS(O)(=O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CCC12C."},
            {"KLCAE", "CC(CCC(O)=O)C1CCC2C3C(CCC12C)C1(C)CCC(O%10)CC1CC3=O."},
            {"KDCAE", "CC(CCC(O)=O)C1CCC2C3C(CC(O)C12C)C1(C)CCC(O%10)CC1CC3=O."},
            {"LCAE", "CC(CCC(O)=O)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CCC12C."},

            {"CE", "CC(C)CCCC(C)C1CCC2C3CC=C4CC(O%10)CCC4(C)C3CCC12C."},

            {"BRSE", "CC(C)C(C)C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)O%10."},
            {"CASE", "CC(C)C(C)CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)O%10."},
            {"SISE", "CCC(CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)O%10)C(C)C."},
            {"STSE", "CCC(C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)O%10)C(C)C."},

            {"Cholestan", "CC(C)CCCC(C)C1CCC2C3CCC4CC(O%10)CCC4(C)C3CCC12C."},

            {"EGSE", "CC(C)C(C)C=CC(C)C1CCC2C3=CC=C4CC(CCC4(C)C3CCC12C)O%10."},
            {"DEGSE", "CC(C)C(C)C=CC(C)C1CCC2C3=CC=C4CC(CCC4(C)C3=CCC12C)O%10."},

            {"DSMSE", "CC(CCC=C(C)C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)O%10."},

            //{"EGSE", "CC(C)[C@@H](C)C=C[C@@H](C)[C@H]1CC[C@H]2C3=CC=C4C[C@H](CC[C@]4(C)[C@H]3CC[C@]12C)O%10."},
            //{"DEGSE", "CC(C)[C@@H](C)C=C[C@@H](C)[C@H]1CC[C@H]2C3=CC=C4C[C@H](CC[C@]4(C)C3=CC[C@]12C)O%10."},
            
            //{"DSMSE", "C[C@H](CCC=C(C)C)[C@H]1CC[C@H]2[C@@H]3CC=C4C[C@H](CC[C@]4(C)[C@H]3CC[C@]12C)O%10."},


            {"AHexCS", "CC(C)CCCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(C(O%10))C(O)C(O)C1O."},
            {"AHexBRS", "CC(C)C(C)C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(C(O%10))C(O)C(O)C1O."},
            {"AHexCAS", "CC(C)C(C)CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(C(O%10))C(O)C(O)C1O."},
            {"AHexSIS", "CCC(CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(C(O%10))C(O)C(O)C1O)C(C)C."},
            {"AHexSTS", "CCC(C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(C(O%10))C(O)C(O)C1O)C(C)C."},

            {"CSLPHex", "CC(C)CCCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO)O%10)C(O)C(O)C1O."},
            {"BRSLPHex", "CC(C)C(C)C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO)O%10)C(O)C(O)C1O."},
            {"CASLPHex", "CC(C)C(C)CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO)O%10)C(O)C(O)C1O."},
            {"SISLPHex", "CCC(CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO)O%10)C(O)C(O)C1O)C(C)C."},
            {"STSLPHex", "CCC(C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO)O%10)C(O)C(O)C1O)C(C)C."},

            {"CSPHex", "CC(C)CCCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO%10)O%20)C(O)C(O)C1O."},
            {"BRSPHex", "CC(C)C(C)C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO%10)O%20)C(O)C(O)C1O."},
            {"CASPHex", "CC(C)C(C)CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO%10)O%20)C(O)C(O)C1O."},
            {"SISPHex", "CCC(CCC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO%10)O%20)C(O)C(O)C1O)C(C)C."},
            {"STSPHex", "CCC(C=CC(C)C1CCC2C3CC=C4CC(CCC4(C)C3CCC12C)OC1OC(COP(O)(=O)OCC(CO%10)O%20)C(O)C(O)C1O)C(C)C."},

            ////bacterial lipid
            //{"Ac2PIM1", "OCC1OC(OC2C(O)C(O)C(O)C(O)C2OP(O)(=O)OCC(O%10)C(O%20))C(O)C(O)C1O."},
            //{"Ac2PIM2", "OCC1OC(OC2C(O)C(O)C(O)C(OC3OC(CO)C(O)C(O)C3O)C2OP(O)(=O)OCC(O%10)C(O%20))C(O)C(O)C1O."},
            //{"Ac3PIM2", "OCC1OC(OC2C(O)C(O)C(O)C(OC3OC(C(O%30))C(O)C(O)C3O)C2OP(O)(=O)OCC(O%10)C(O%20))C(O)C(O)C1O."},
            //{"Ac4PIM2", "OCC1OC(OC2C(O)C(O)C(O%40)C(OC3OC(C(O%30))C(O)C(O)C3O)C2OP(O)(=O)OCC(O%10)C(O%20))C(O)C(O)C1O."},

            //{"LipidA", "OCC1O[C@@H](OCC2O[C@H](OP(O)(O)=O)C(N%10)[C@@H](O%20)[C@@H]2O)C(N%30)[C@@H](O%40)[C@@H]1OP(O)(O)=O."},

        };
    }

}
