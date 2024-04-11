using CompMs.Common.MessagePack;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Riken.Metabolomics.Lipidomics
{
    public sealed class LipidomicsConverter
    {
        private LipidomicsConverter() { }

        public static LipidMolecule ConvertMsdialLipidnameToLipidMoleculeObject(MspFormatCompoundInformationBean query)
        {
            var molecule = new LipidMolecule();

            switch (query.CompoundClass)
            {

                //Glycerolipid
                case "MAG":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DAG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "TAG":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherTAG":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherDAG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Lyso phospholipid
                case "LPC":
                case "LPC_d5":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LPE":
                case "LPE_d5":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LPG":
                case "LPG_d5":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LPI":
                case "LPI_d5":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LPS":
                case "LPS_d5":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LPA":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LDGTS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LDGTA":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LDGCC":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Phospholipid
                case "PC":
                case "PC_d5":
                case "bmPC":
                case "PE":
                case "PE_d5":
                case "PG":
                case "PG_d5":
                case "PI":
                case "PS":
                case "PS_d5":
                case "PT":
                case "PA":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "BMP":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HBMP":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "CL":
                    setQuadAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DLCL":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LCL":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Ether linked Lyso phospholipid
                case "EtherLPC":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherLPE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherLPG":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherLPI":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherLPS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Ether linked phospholipid
                case "EtherPC":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherPE":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                //Ether linked phospholipid
                case "EtherPG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherPI":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherPS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Ether linked phospholipid
                case "EtherMGDG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherDGDG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;


                //Oxidized phospholipid
                case "OxPC":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "OxPE":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "OxPG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "OxPI":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "OxPS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;


                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "EtherOxPE":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "PEtOH":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "PBtOH":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //N-acyl lipids
                case "LNAPE":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "LNAPS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "NAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "NAAG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "NAAGS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "NAAO":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Plantlipid
                case "MGDG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DGDG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "SQDG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DGTS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DGTA":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DGCC":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "GlcADG":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AcylGlcADG":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "DGGA":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "ADGGA":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Sterols
                case "CE":
                case "CE_d7":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "ACar":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "FAHFA":
                case "DMEDFAHFA":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "CoQ":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "DCAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "GDCAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "GLCAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "TDCAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "TLCAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Vitamin":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "VAE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "BileAcid":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "BRSE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "CASE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "SISE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "STSE":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexCS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexBRS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexCAS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexSIS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexSTS":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "SHex":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "SSulfate":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "BAHex":
                    setSingleMetaboliteInfor(molecule, query);
                    break;
                case "BASulfate":
                    setSingleMetaboliteInfor(molecule, query);
                    break;


                //Sphingomyelin
                case "SM":
                case "SM_d9":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                //Ceramide
                case "CerP":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_ADS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_AS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_BDS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_BS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_EODS":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_EOS":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_NDS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_NS":
                case "Cer_NS_d7":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_NP":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_AP":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_OS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_O":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_DOS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_NS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_NDS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_AP":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_O":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_HDS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_HS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexCer_EOS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexHexCer_NS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexHexHexCer_NS":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexHexCer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "HexHexHexCer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Hex2Cer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Hex3Cer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "PE_Cer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "PI_Cer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                case "SHexCer":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "SL":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;

                case "GM3":
                    setDoubleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cholesterol":
                    setSingleMetaboliteInfor(molecule, query);
                    //setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "CholesterolSulfate":
                    setSingleMetaboliteInfor(molecule, query);
                    //setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Phytosphingosine":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Sphinganine":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Sphingosine":
                    setSingleAcylChainsLipidAnnotation(molecule, query);
                    break;

                case "AcylCer_BDS":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AcylHexCer":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AcylSM":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "Cer_EBDS":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "AHexCer":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                case "ASM":
                    setTripleAcylChainsLipidAnnotation(molecule, query);
                    break;
                default:
                    return null;
            }
            return molecule;
        }

        public static LipidMolecule ConvertMsdialLipidnameToLipidMoleculeObjectVS2(MspFormatCompoundInformationBean query)
        {
            var molecule = new LipidMolecule();
            var lipidclass = query.CompoundClass;

            /*  FattyAcyls [FA], Glycerolipids [GL], Glycerophospholipids [GP], Sphingolipids [SP]
            *  SterolLipids [ST], PrenolLipids [PR], Saccharolipids [SL], Polyketides [PK]
            */

            var lipidcategory = ConvertMsdialClassDefinitionToSuperClassVS2(lipidclass);
            var lbmclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidclass);

            if (lipidcategory == "FattyAcyls" || lipidcategory == "Glycerolipids" ||
                lipidcategory == "Glycerophospholipids" || lipidcategory == "Sphingolipids" || lbmclass == LbmClass.VAE)
            {
                SetLipidAcylChainProperties(molecule, query);
            }
            else if (lipidcategory == "SterolLipids" || lipidcategory == "PrenolLipids")
            {
                if (lbmclass == LbmClass.Vitamin_D || lbmclass == LbmClass.Vitamin_E || lbmclass == LbmClass.SHex ||
                    lbmclass == LbmClass.SSulfate || lbmclass == LbmClass.BAHex || lbmclass == LbmClass.BASulfate ||
                    lbmclass == LbmClass.BileAcid ||
                    query.Name == "Cholesterol" || query.Name == "CholesterolSulfate")
                {
                    SetSingleLipidStructure(molecule, query);
                }
                else if (lbmclass == LbmClass.CoQ)
                {
                    SetCoqMolecule(molecule, query);
                }
                else
                {
                    SetLipidAcylChainProperties(molecule, query);
                }
            }

            molecule.LipidClass = lbmclass;
            molecule.LipidCategory = lipidcategory;
            molecule.LipidSubclass = lipidclass;

            if (molecule.LipidName == null || molecule.LipidName == string.Empty || molecule.Adduct == null)
            {
                molecule.IsValidatedFormat = false;
            }
            else
            {
                molecule.IsValidatedFormat = true;
            }
            return molecule;
        }

        public static LipidMolecule ConvertMsdialLipidnameToLipidMoleculeObjectVS2(string lipidname, string ontology)
        {
            var molecule = new LipidMolecule();
            var lipidclass = ontology;

            /*  FattyAcyls [FA], Glycerolipids [GL], Glycerophospholipids [GP], Sphingolipids [SP]
            *  SterolLipids [ST], PrenolLipids [PR], Saccharolipids [SL], Polyketides [PK]
            */

            var lipidcategory = ConvertMsdialClassDefinitionToSuperClassVS2(lipidclass);
            var lbmclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(ontology);

            //Console.WriteLine(lipidcategory + "\t" + lbmclass.ToString() + "\t" + ontology);
            if (lipidcategory == "FattyAcyls" || lipidcategory == "Glycerolipids" ||
                lipidcategory == "Glycerophospholipids" || lipidcategory == "Sphingolipids" || lbmclass == LbmClass.VAE)
            {
                SetLipidAcylChainProperties(molecule, lipidname, ontology);
            }
            else if (lipidcategory == "SterolLipids" || lipidcategory == "PrenolLipids")
            {
                if (lbmclass == LbmClass.Vitamin_D || lbmclass == LbmClass.Vitamin_E || lbmclass == LbmClass.SHex ||
                    lbmclass == LbmClass.SSulfate || lbmclass == LbmClass.BAHex || lbmclass == LbmClass.BASulfate ||
                    lbmclass == LbmClass.BileAcid ||
                    lipidname == "Cholesterol" || lipidname == "CholesterolSulfate")
                {
                    SetSingleLipidStructure(molecule, lipidname, ontology);
                }
                else if (lbmclass == LbmClass.CoQ)
                {
                    SetCoqMolecule(molecule, lipidname, ontology);
                }
                else
                {
                    SetLipidAcylChainProperties(molecule, lipidname, ontology);
                }
            }

            molecule.LipidClass = lbmclass;
            molecule.LipidCategory = lipidcategory;
            molecule.LipidSubclass = lipidclass;

            if (molecule.LipidName == null || molecule.LipidName == string.Empty)
            {
                molecule.IsValidatedFormat = false;
            }
            else
            {
                molecule.IsValidatedFormat = true;
            }
            return molecule;
        }


        public static string LipidomicsAnnotationLevel(string lipidname, MspFormatCompoundInformationBean query, string adduct)
        {
            var lipidclass = lipidname.Split(' ')[0];
            if (lipidname.Contains("e;") || lipidname.Contains("p;") || lipidname.Contains("e+") || lipidname.Contains("e/"))
                lipidclass = "Ether" + lipidclass;

            var lbmLipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);
            var querylipidClass = ConvertLbmClassEnumToMsdialClassDefinition(lbmLipidclass);
            var isInconsistency = false;
            if (lipidclass != querylipidClass) isInconsistency = true;
            if (lipidclass == "Cer-HS" && (querylipidClass == "Cer-AS" || querylipidClass == "Cer-BS")) isInconsistency = false;
            if (lipidclass == "Cer-HDS" && (querylipidClass == "Cer-ADS" || querylipidClass == "Cer-BDS")) isInconsistency = false;


            var level = "Class resolved";
            var lipidsemicoronCount = lipidname.Length - lipidname.Replace(";", "").Length;
            var querysemicoronCount = query.Name.Length - query.Name.Replace(";", "").Length;


            switch (query.CompoundClass)
            {

                //Glycerolipid
                case "MAG":
                    level = "Chain resolved";
                    break;
                case "DAG":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "TAG":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "EtherTAG":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "EtherDAG":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                //Lyso phospholipid
                case "LPC":
                    level = "Chain resolved";
                    break;
                case "LPE":
                    level = "Chain resolved";
                    break;
                case "LPG":
                    level = "Chain resolved";
                    break;
                case "LPI":
                    level = "Chain resolved";
                    break;
                case "LPS":
                    level = "Chain resolved";
                    break;
                case "LPA":
                    level = "Chain resolved";
                    break;
                case "LDGTS":
                    level = "Chain resolved";
                    break;
                case "LDGCC":
                    level = "Chain resolved";
                    break;

                //Phospholipid
                case "PC":
                    if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "bmPC":
                    if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PE":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PG":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PS":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PT":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PA":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "BMP":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "HBMP":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "CL":
                    if (lipidsemicoronCount == querysemicoronCount)
                    {
                        if (lipidname.Split(';')[1].Split('-').Length < 4)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;
                case "DLCL":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "LCL":
                    if (lipidsemicoronCount == querysemicoronCount)
                    {
                        if (lipidname.Split(';')[1].Split('-').Length < 3)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked Lyso phospholipid
                case "EtherLPC":
                    level = "Chain resolved";
                    break;
                case "EtherLPE":
                    level = "Chain resolved";
                    break;
                case "EtherLPG":
                    level = "Chain resolved";
                    break;
                case "EtherLPI":
                    level = "Chain resolved";
                    break;
                case "EtherLPS":
                    level = "Chain resolved";
                    break;

                //Ether linked phospholipid
                case "EtherPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPE":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                //Ether linked phospholipid
                case "EtherPG":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked phospholipid
                case "EtherMGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherDGDG":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized phospholipid
                case "OxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPG":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherOxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PEtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PBtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                //N-acyl lipids
                case "LNAPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "LNAPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "NAE":
                    level = "Chain resolved";
                    break;
                case "NAAG":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "NAAGS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "NAAO":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;

                //Plantlipid
                case "MGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "DGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "SQDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "DGTS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "DGCC":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "DGGA":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "ADGGA":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;

                //Sterols
                case "CE":
                case "CE_d7":
                    level = "Chain resolved";
                    break;
                case "ACar":
                    level = "Chain resolved";
                    break;
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    level = "Chain resolved";
                    break;
                case "FAHFA":
                case "DMEDFAHFA":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "CoQ":
                    level = "Chain resolved";
                    break;
                case "DCAE":
                    level = "Chain resolved";
                    break;
                case "GDCAE":
                    level = "Chain resolved";
                    break;
                case "GLCAE":
                    level = "Chain resolved";
                    break;
                case "TDCAE":
                    level = "Chain resolved";
                    break;
                case "TLCAE":
                    level = "Chain resolved";
                    break;
                case "Vitamin":
                    level = "Lipid assigned";
                    break;
                case "VAE":
                    level = "Chain resolved";
                    break;
                case "BileAcid":
                    level = "Lipid assigned";
                    break;
                case "BRSE":
                    level = "Chain resolved";
                    break;
                case "CASE":
                    level = "Chain resolved";
                    break;
                case "SISE":
                    level = "Chain resolved";
                    break;
                case "STSE":
                    level = "Chain resolved";
                    break;
                case "AHexCS":
                    level = "Chain resolved";
                    break;
                case "AHexBRS":
                    level = "Chain resolved";
                    break;
                case "AHexCAS":
                    level = "Chain resolved";
                    break;
                case "AHexSIS":
                    level = "Chain resolved";
                    break;
                case "AHexSTS":
                    level = "Chain resolved";
                    break;
                case "SHex":
                    level = "Lipid assigned";
                    break;
                case "SSulfate":
                    level = "Lipid assigned";
                    break;
                case "BAHex":
                    level = "Lipid assigned";
                    break;
                case "BASulfate":
                    level = "Lipid assigned";
                    break;


                //Sphingomyelin
                case "SM":
                    if (query.Name.Contains("t"))
                    {

                    }
                    else if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                //Ceramide
                case "CerP":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_ADS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_AS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_BDS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_BS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_EODS":
                    if (lipidsemicoronCount == querysemicoronCount)
                    {
                        if (lipidname.Split(';')[1].Contains("-O-") && lipidname.Split(';')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_EOS":
                    if (lipidsemicoronCount == querysemicoronCount)
                    {
                        if (lipidname.Split(';')[1].Contains("-O-") && lipidname.Split(';')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_NDS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_NS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_NP":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_AP":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_OS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_O":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_DOS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_NS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_NDS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_AP":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_O":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_HDS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_HS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "HexCer_EOS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain semi-resolved";
                    break;
                case "HexHexCer_NS":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexHexCer_NS":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "Hex2Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "Hex3Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PE_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "PI_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;

                case "SHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "SL":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;

                case "GM3":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == querysemicoronCount)
                            level = "Chain resolved";
                    }
                    break;
                case "Cholesterol":
                    level = "Lipid assigned";
                    break;
                case "CholesterolSulfate":
                    level = "Lipid assigned";
                    break;
                case "Phytosphingosine":
                    level = "Chain resolved";
                    break;
                case "Sphinganine":
                    level = "Chain resolved";
                    break;
                case "Sphingosine":
                    level = "Chain resolved";
                    break;
                case "AcylHexCer":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "Cer_EBDS":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "AHexCer":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain resolved";
                    break;
                case "ASM":
                    if (lipidsemicoronCount == querysemicoronCount)
                        level = "Chain semi-resolved";
                    break;
                case "Others":
                    level = "Annotated by experimental MS/MS";
                    break;
                default:
                    break;
            }

            if (isInconsistency)
            {
                return level + "; inconsistency with MSP file";
            }
            else
                return level;
        }

        public static string LipidomicsAnnotationLevel(string lipidname, string ontology, string adduct)
        {
            var lipidclass = lipidname.Split(' ')[0];
            if (lipidname.Contains("e;") || lipidname.Contains("p;") || lipidname.Contains("e+") || lipidname.Contains("e/"))
                lipidclass = "Ether" + lipidclass;

            var level = "Class resolved";
            var lipidsemicoronCount = lipidname.Length - lipidname.Replace(";", "").Length;

            switch (ontology)
            {

                //Glycerolipid
                case "MAG":
                    level = "Chain resolved";
                    break;
                case "DAG":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "TAG":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "EtherTAG":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "EtherDAG":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                //Lyso phospholipid
                case "LPC":
                    level = "Chain resolved";
                    break;
                case "LPE":
                    level = "Chain resolved";
                    break;
                case "LPG":
                    level = "Chain resolved";
                    break;
                case "LPI":
                    level = "Chain resolved";
                    break;
                case "LPS":
                    level = "Chain resolved";
                    break;
                case "LPA":
                    level = "Chain resolved";
                    break;
                case "LDGTS":
                    level = "Chain resolved";
                    break;
                case "LDGTA":
                    level = "Chain resolved";
                    break;
                case "LDGCC":
                    level = "Chain resolved";
                    break;

                //Phospholipid
                case "PC":
                case "PC_d5":
                case "bmPC":
                    if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PE":
                case "PE_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PG":
                case "PG_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PS":
                case "PS_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PT":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PA":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "BMP":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "HBMP":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "CL":
                    if (lipidsemicoronCount == 2)
                    {
                        if (lipidname.Split(';')[1].Split('-').Length < 4)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;
                case "DLCL":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "LCL":
                    if (lipidsemicoronCount == 3)
                    {
                        if (lipidname.Split(';')[1].Split('-').Length < 3)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked Lyso phospholipid
                case "EtherLPC":
                    level = "Chain resolved";
                    break;
                case "EtherLPE":
                    level = "Chain resolved";
                    break;
                case "EtherLPG":
                    level = "Chain resolved";
                    break;
                case "EtherLPI":
                    level = "Chain resolved";
                    break;
                case "EtherLPS":
                    level = "Chain resolved";
                    break;

                //Ether linked phospholipid
                case "EtherPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPE(Plasmalogen)":
                    {
                        if (adduct == "[M+H]+" || adduct == "[M-H]-")
                        {
                            if (lipidsemicoronCount == 2)
                                level = "Chain resolved";
                        }
                        break;
                    }
                case "EtherPE":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                //Ether linked phospholipid
                case "EtherPG":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked phospholipid
                case "EtherMGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherDGDG":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized phospholipid
                case "OxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPG":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherOxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PEtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PBtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                //N-acyl lipids
                case "LNAPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "LNAPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "NAE":
                    level = "Chain resolved";
                    break;
                case "NAAG":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "NAAGS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "NAAO":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;

                //Plantlipid
                case "MGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "DGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "SQDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "DGTS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "DGTA":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "DGCC":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "DGGA":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "ADGGA":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;

                //Sterols
                case "CE":
                case "CE_d7":
                    level = "Chain resolved";
                    break;
                case "ACar":
                    level = "Chain resolved";
                    break;
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    level = "Chain resolved";
                    break;
                case "FAHFA":
                case "DMEDFAHFA":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "CoQ":
                    level = "Chain resolved";
                    break;
                case "DCAE":
                    level = "Chain resolved";
                    break;
                case "GDCAE":
                    level = "Chain resolved";
                    break;
                case "GLCAE":
                    level = "Chain resolved";
                    break;
                case "TDCAE":
                    level = "Chain resolved";
                    break;
                case "TLCAE":
                    level = "Chain resolved";
                    break;
                case "Vitamin":
                    level = "Lipid assigned";
                    break;
                case "VAE":
                    level = "Chain resolved";
                    break;
                case "BileAcid":
                    level = "Lipid assigned";
                    break;
                case "BRSE":
                    level = "Chain resolved";
                    break;
                case "CASE":
                    level = "Chain resolved";
                    break;
                case "SISE":
                    level = "Chain resolved";
                    break;
                case "STSE":
                    level = "Chain resolved";
                    break;
                case "AHexCS":
                    level = "Chain resolved";
                    break;
                case "AHexBRS":
                    level = "Chain resolved";
                    break;
                case "AHexCAS":
                    level = "Chain resolved";
                    break;
                case "AHexSIS":
                    level = "Chain resolved";
                    break;
                case "AHexSTS":
                    level = "Chain resolved";
                    break;
                case "SHex":
                    level = "Lipid assigned";
                    break;
                case "SSulfate":
                    level = "Lipid assigned";
                    break;
                case "BAHex":
                    level = "Lipid assigned";
                    break;
                case "BASulfate":
                    level = "Lipid assigned";
                    break;


                //Sphingomyelin
                case "SM":
                case "SM_d9":
                    if (lipidname.Contains("t"))
                    {

                    }
                    else if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                //Ceramide
                case "CerP":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_ADS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_AS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_BDS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_BS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_EODS":
                    if (lipidsemicoronCount == 2)
                    {
                        if (lipidname.Split(';')[1].Contains("-O-") && lipidname.Split(';')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_EOS":
                    if (lipidsemicoronCount == 2)
                    {
                        if (lipidname.Split(';')[1].Contains("-O-") && lipidname.Split(';')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_NDS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_NS":
                case "Cer_NS_d7":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_NP":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_AP":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_OS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_O":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_DOS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_NS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_NDS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_AP":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_O":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_HDS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_HS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "HexCer_EOS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain semi-resolved";
                    break;
                case "HexHexCer_NS":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexHexCer_NS":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "HexHexHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "Hex2Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "Hex3Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PE_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "PI_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;

                case "SHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "SL":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;

                case "GM3":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 2)
                            level = "Chain resolved";
                    }
                    break;
                case "Cholesterol":
                    level = "Lipid assigned";
                    break;
                case "CholesterolSulfate":
                    level = "Lipid assigned";
                    break;
                case "Phytosphingosine":
                    level = "Chain resolved";
                    break;
                case "Sphinganine":
                    level = "Chain resolved";
                    break;
                case "Sphingosine":
                    level = "Chain resolved";
                    break;
                case "AcylHexCer":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "Cer_EBDS":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "AHexCer":
                    if (lipidsemicoronCount == 2)
                        level = "Chain resolved";
                    break;
                case "ASM":
                    if (lipidsemicoronCount == 2)
                        level = "Chain semi-resolved";
                    break;
                case "Others":
                    level = "Annotated by experimental MS/MS";
                    break;
                default:
                    break;
            }
            return level;
        }

        public static string LipidomicsAnnotationLevelVS2(string lipidname, string ontology, string adduct)
        {
            var level = "Class resolved";
            var lipidsemicoronCount = lipidname.Length - lipidname.Replace("|", "").Length;

            switch (ontology)
            {
                //Glycerolipid
                case "MG":
                    level = "Chain resolved";
                    break;
                case "DG":
                case "DG_d5":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "TG":
                case "TG_d5":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "EtherTG":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "EtherDG":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                //Lyso phospholipid
                case "LPC":
                case "LPC_d5":
                    level = "Chain resolved";
                    break;
                case "LPE":
                case "LPE_d5":
                    level = "Chain resolved";
                    break;
                case "LPG":
                case "LPG_d5":
                    level = "Chain resolved";
                    break;
                case "LPI":
                case "LPI_d5":
                    level = "Chain resolved";
                    break;
                case "LPS":
                case "LPS_d5":
                    level = "Chain resolved";
                    break;
                case "LPA":
                    level = "Chain resolved";
                    break;
                case "LDGTS":
                    level = "Chain resolved";
                    break;
                case "LDGTA":
                    level = "Chain resolved";
                    break;
                case "LDGCC":
                    level = "Chain resolved";
                    break;

                //Phospholipid
                case "PC":
                case "PC_d5":
                case "bmPC":
                    if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PE":
                case "PE_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PG":
                case "PG_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PI":
                case "PI_d5":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PS":
                case "PS_d5":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PT":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PA":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "BMP":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "HBMP":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "CL":
                    if (lipidsemicoronCount == 1)
                    {
                        if (lipidname.Split('|')[1].Split('_').Length < 4)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;
                case "DLCL":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "MLCL":
                    if (lipidsemicoronCount == 1)
                    {
                        if (lipidname.Split('|')[1].Split('_').Length < 3)
                            level = "Chain semi-resolved";
                        else
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked Lyso phospholipid
                case "EtherLPC":
                    level = "Chain resolved";
                    break;
                case "EtherLPE":
                    level = "Chain resolved";
                    break;
                case "EtherLPG":
                    level = "Chain resolved";
                    break;
                case "EtherLPI":
                    level = "Chain resolved";
                    break;
                case "EtherLPS":
                    level = "Chain resolved";
                    break;

                //Ether linked phospholipid
                case "EtherPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPE(Plasmalogen)":
                    {
                        if (adduct == "[M+H]+" || adduct == "[M-H]-")
                        {
                            if (lipidsemicoronCount == 1)
                                level = "Chain resolved";
                        }
                        break;
                    }
                case "EtherPE":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                //Ether linked phospholipid
                case "EtherPG":
                    if (adduct == "[M+H]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                //Ether linked phospholipid
                case "EtherMGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherSMGDG":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherDGDG":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized phospholipid
                case "OxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPG":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPI":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "OxPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;


                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    if (adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "EtherOxPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PEtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PBtOH":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                //N-acyl lipids
                case "LNAPE":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "LNAPS":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "NAE":
                    level = "Chain resolved";
                    break;
                case "NAGly":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "NAGlySer":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "NAOrn":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "NAPhe":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "NATau":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;

                //Plantlipid
                case "MGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "DGDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "SQDG":
                    if (adduct == "[M+NH4]+" || adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "DGTS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "DGTA":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "DGCC":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "DGGA":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "ADGGA":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;

                //Sterols
                case "CE":
                case "CE_d7":
                    level = "Chain resolved";
                    break;
                case "CAR":
                    level = "Chain resolved";
                    break;
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    level = "Chain resolved";
                    break;
                case "FAHFA":
                case "DMEDFAHFA":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "CoQ":
                    level = "Chain resolved";
                    break;
                case "DCAE":
                    level = "Chain resolved";
                    break;
                case "GDCAE":
                    level = "Chain resolved";
                    break;
                case "GLCAE":
                    level = "Chain resolved";
                    break;
                case "TDCAE":
                    level = "Chain resolved";
                    break;
                case "TLCAE":
                    level = "Chain resolved";
                    break;
                case "Vitamin_E":
                    level = "Lipid assigned";
                    break;
                case "VitaminDE":
                    level = "Lipid assigned";
                    break;
                case "VAE":
                    level = "Chain resolved";
                    break;
                case "BileAcid":
                    level = "Lipid assigned";
                    break;
                case "BRSE":
                    level = "Chain resolved";
                    break;
                case "CASE":
                    level = "Chain resolved";
                    break;
                case "SISE":
                    level = "Chain resolved";
                    break;
                case "STSE":
                    level = "Chain resolved";
                    break;
                case "AHexCS":
                    level = "Chain resolved";
                    break;
                case "AHexBRS":
                    level = "Chain resolved";
                    break;
                case "AHexCAS":
                    level = "Chain resolved";
                    break;
                case "AHexSIS":
                    level = "Chain resolved";
                    break;
                case "AHexSTS":
                    level = "Chain resolved";
                    break;
                case "SHex":
                    level = "Lipid assigned";
                    break;
                case "SSulfate":
                    level = "Lipid assigned";
                    break;
                case "BAHex":
                    level = "Lipid assigned";
                    break;
                case "BASulfate":
                    level = "Lipid assigned";
                    break;


                //Sphingomyelin
                case "SM":
                case "SM_d9":
                    if (lipidname.Contains("t"))
                    {

                    }
                    else if (adduct == "[M+H]+" || adduct == "[M+CH3COO]-" || adduct == "[M+HCOO]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                //Ceramide
                case "CerP":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_ADS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_AS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_BDS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_BS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_EODS":
                    if (lipidsemicoronCount == 1)
                    {
                        if (lipidname.Split('|')[1].Contains("(FA") && lipidname.Split('|')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_EOS":
                    if (lipidsemicoronCount == 1)
                    {
                        if (lipidname.Split('|')[1].Contains("(FA") && lipidname.Split('|')[1].Contains("/"))
                            level = "Chain resolved";
                        else
                            level = "Chain semi-resolved";
                    }
                    break;
                case "Cer_NDS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_NS":
                case "Cer_NS_d7":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_NP":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_AP":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_OS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_O":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "Cer_DOS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_NS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_NDS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_AP":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_O":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_HDS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_HS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "HexCer_EOS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain semi-resolved";
                    break;
                case "Hex2Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "Hex3Cer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PE_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "PI_Cer":
                    if (adduct == "[M-H]-")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;

                case "SHexCer":
                    if (adduct == "[M+H]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "SL":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;

                case "GM3":
                    if (adduct == "[M+NH4]+")
                    {
                        if (lipidsemicoronCount == 1)
                            level = "Chain resolved";
                    }
                    break;
                case "Cholesterol":
                    level = "Lipid assigned";
                    break;
                case "CholesterolSulfate":
                    level = "Lipid assigned";
                    break;
                case "PhytoSph":
                    level = "Chain resolved";
                    break;
                case "DHSph":
                    level = "Chain resolved";
                    break;
                case "Sph":
                    level = "Chain resolved";
                    break;
                case "Cer_EBDS":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "AHexCer":
                    if (lipidsemicoronCount == 1)
                        level = "Chain resolved";
                    break;
                case "ASM":
                    if (lipidsemicoronCount == 1)
                        level = "Chain semi-resolved";
                    break;
                case "Others":
                    level = "Annotated by experimental MS/MS";
                    break;
                default:
                    break;
            }
            return level;
        }


        public static string GetLipoqualityDatabaseLinkUrl(MspFormatCompoundInformationBean query)
        {
            var molecule = ConvertMsdialLipidnameToLipidMoleculeObject(query);
            if (molecule == null) return string.Empty;
            return "http://jcbl.jp/wiki/Lipoquality:Resource?lc=" + molecule.LipidName + "&ct=c";
        }

        // now for Cholesterol and CholesterolSulfate
        private static void setSingleMetaboliteInfor(LipidMolecule molecule, MspFormatCompoundInformationBean query)
        {

            var name = query.Name;
            var nameArray = name.Split(';').ToArray();

            var lipidinfo = nameArray[0].Trim();
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);

            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;
            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = "0:0";
            molecule.TotalCarbonCount = 0;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = "0:0";
            molecule.Sn1CarbonCount = 0;
            molecule.Sn1DoubleBondCount = 0;
        }

        // now for Cholesterol and CholesterolSulfate
        public static void SetSingleLipidStructure(LipidMolecule molecule, MspFormatCompoundInformationBean query)
        {

            var lipidinfo = query.Name;
            SetBasicMoleculerProperties(molecule, query);

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.TotalChainString = "0:0";
            molecule.TotalCarbonCount = 0;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = "0:0";
            molecule.Sn1CarbonCount = 0;
            molecule.Sn1DoubleBondCount = 0;
        }

        public static void SetSingleLipidStructure(LipidMolecule molecule, string lipidname, string ontology)
        {

            molecule.LipidName = lipidname;
            molecule.SublevelLipidName = lipidname;
            molecule.TotalChainString = "0:0";
            molecule.TotalCarbonCount = 0;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = "0:0";
            molecule.Sn1CarbonCount = 0;
            molecule.Sn1DoubleBondCount = 0;
        }

        public static void SetCoqMolecule(LipidMolecule molecule, MspFormatCompoundInformationBean query)
        {

            SetBasicMoleculerProperties(molecule, query);

            var lipidinfo = query.Name;
            var carbonCountString = lipidinfo.Substring(3); // CoQ3 -> 3
            var carbonCount = 0;
            int.TryParse(carbonCountString, out carbonCount);

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.TotalChainString = carbonCountString;
            molecule.TotalCarbonCount = carbonCount;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = carbonCountString;
            molecule.Sn1CarbonCount = carbonCount;
            molecule.Sn1DoubleBondCount = 0;
        }

        public static void SetCoqMolecule(LipidMolecule molecule, string lipidname, string ontology)
        {

            var carbonCountString = lipidname.Substring(3); // CoQ3 -> 3
            var carbonCount = 0;
            int.TryParse(carbonCountString, out carbonCount);

            molecule.LipidName = lipidname;
            molecule.SublevelLipidName = lipidname;
            molecule.TotalChainString = carbonCountString;
            molecule.TotalCarbonCount = carbonCount;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = carbonCountString;
            molecule.Sn1CarbonCount = carbonCount;
            molecule.Sn1DoubleBondCount = 0;
        }

        public static void setSingleMetaboliteInfor(LipidMolecule molecule, string lipidname, string lipidclassString)
        {

            var name = lipidname;
            var nameArray = name.Split(';').ToArray();

            var lipidinfo = name;
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(lipidclassString);

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = "0:0";
            molecule.TotalCarbonCount = 0;
            molecule.TotalDoubleBondCount = 0;
            molecule.Sn1AcylChainString = "0:0";
            molecule.Sn1CarbonCount = 0;
            molecule.Sn1DoubleBondCount = 0;
        }


        private static void setSingleAcylChainsLipidAnnotation(LipidMolecule molecule,
            MspFormatCompoundInformationBean query)
        {
            var name = query.Name;
            var nameArray = name.Split(';').ToArray();

            var lipidinfo = nameArray[0].Trim();
            //if (lipidinfo == "PI-Cer d34:0+O") {
            //    Console.WriteLine();
            //}

            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);

            if (lipidinfo.Split(' ').Length < 2) return;
            var totalChain = lipidinfo.Split(' ')[1];
            var sn1AcylChainString = lipidinfo.Split(' ')[1];
            if (totalChain == null || totalChain == string.Empty || !totalChain.Contains(':') || (totalChain.Contains('(') && !totalChain.Contains('+'))) return;

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;

            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);
            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);

            //var totalCarbon = int.Parse(totalChain.Split(':')[0]);
            //var totalDouble = int.Parse(totalChain.Split(':')[1]);
            //var sn1Carbon = int.Parse(sn1AcylChain.Split(':')[0]);
            //var sn1DoubleBond = int.Parse(sn1AcylChain.Split(':')[1]);

            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;
            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
        }

        public static void SetLipidAcylChainProperties(LipidMolecule molecule, MspFormatCompoundInformationBean query)
        {
            var lipidname = query.Name.Trim(); // e.g. ST 28:2;O;Hex;PA 12:0_12:0, SE 28:2/8:0
            var chainStrings = acylChainStringSeparatorVS2(lipidname);
            var ontology = query.CompoundClass;
            //var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClassVS2(ontology);

            // set basic properties
            SetBasicMoleculerProperties(molecule, query);
            if (chainStrings == null) return;

            switch (chainStrings.Count())
            {
                case 1: setMonoAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 2: setDiAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 3: setTriAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 4: setTetraAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
            }

            return;
        }

        public static void SetLipidAcylChainProperties(LipidMolecule molecule, string lipidname, string ontology)
        {
            var chainStrings = acylChainStringSeparatorVS2(lipidname);
            if (chainStrings == null) return;
            switch (chainStrings.Count())
            {
                case 1: setMonoAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 2: setDiAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 3: setTriAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
                case 4: setTetraAcylChainProperty(molecule, lipidname, ontology, chainStrings); break;
            }

            return;
        }


        public static void SetBasicMoleculerProperties(LipidMolecule molecule, MspFormatCompoundInformationBean query)
        {
            // var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(query.CompoundClass);
            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;

            //Console.WriteLine(query.Name + "\t" + query.AdductIonBean.AdductIonName);

            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;
            // molecule.LipidClass = lipidclass;
        }

        private static void setMonoAcylChainProperty(LipidMolecule molecule,
            string lipidname, string ontology, List<string> chainStrings)
        {
            if (chainStrings.Count != 1) return;
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(ontology);
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            setChainPropertiesVS2(chainStrings[0], out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            var totalCarbonCount = sn1CarbonCount;
            var totalDoubleBond = sn1DoubleBond;
            var totalOxidizedCount = sn1OxidizedCount;
            //var totalChain = getTotalChainString(totalCarbonCount, totalDoubleBond, totalOxidizedCount, lipidclass);
            var totalChain = chainStrings[0];

            molecule.LipidName = lipidname;
            molecule.SublevelLipidName = lipidname;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = chainStrings[0];
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
        }

        public static LipidMolecule GetLipidMoleculeNameProperties(string lipidname)
        {
            if (lipidname.Split(' ').Length == 1) return new LipidMolecule() { SublevelLipidName = lipidname };

            var lipidheader = lipidname.Split(' ')[0];
            var acylchains = lipidname.Split(' ')[1];
            var chainStrings = acylChainStringSeparator(lipidname);
            var chainsCount = chainStrings.Count();
            if (chainsCount == 0) return new LipidMolecule() { SublevelLipidName = lipidname };

            var molecule = new LipidMolecule();
            switch (chainsCount)
            {
                case 1: setSingleAcylChainsLipidAnnotation(molecule, lipidname, lipidheader); break;
                case 2: setDoubleAcylChainsLipidAnnotation(molecule, lipidname, lipidheader); break;
                case 3: setTripleAcylChainsLipidAnnotation(molecule, lipidname, lipidheader); break;
                case 4: setQuadAcylChainsLipidAnnotation(molecule, lipidname, lipidheader); break;
            }
            return molecule;
        }


        public static void setSingleAcylChainsLipidAnnotation(LipidMolecule molecule,
            string lipidname, string lipidclassString)
        {
            var name = lipidname;
            var lipidinfo = name;
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(lipidclassString);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(lipidclassString);

            if (lipidinfo.Split(' ').Length < 2) return;
            var totalChain = lipidinfo.Split(' ')[1];
            var sn1AcylChainString = lipidinfo.Split(' ')[1];
            if (totalChain == null || totalChain == string.Empty || !totalChain.Contains(':') || (totalChain.Contains('(') && !totalChain.Contains('+'))) return;

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;

            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);
            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);

            molecule.LipidName = lipidinfo;
            molecule.SublevelLipidName = lipidinfo;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
        }

        private static void setDoubleAcylChainsLipidAnnotation(LipidMolecule molecule,
            MspFormatCompoundInformationBean query)
        {

            var nameArray = query.Name.Split(';').ToArray();
            if (nameArray.Length == 2)
            { // e.g. in positive PC Na adduct, only sublevel information is resigtered like PC 36:3; [M+Na]+
                setSingleAcylChainsLipidAnnotation(molecule, query);
                return;
            }

            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);

            //if (lipidclass == LbmClass.Cer_O) {
            //    Console.WriteLine();
            //}


            var sublevelLipidName = nameArray[0].Trim(); // SM d48:2
            var totalChain = sublevelLipidName.Split(' ')[1]; // d48:2
            var lipidName = nameArray[1].Trim(); // SM d18:1/30:1
            var adductInfo = nameArray[2].Trim();

            if (totalChain == null || totalChain == string.Empty || !totalChain.Contains(':') || (totalChain.Contains('(') && !totalChain.Contains('+'))) return;

            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() != 2) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);
            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);

            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;
            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
        }

        private static void setDiAcylChainProperty(LipidMolecule molecule,
            string lipidname, string ontology, List<string> chainStrings)
        {// e.g. SM 18:1;2O/30:1, PE 16:0_18:0;O, MGDG 2:0_2:0, ST 28:2;O;Hex;PA 12:0_12:0

            if (chainStrings.Count != 2) return;
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(ontology);
            var lipidHeader = GetLipidHeaderString(lipidname);

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];

            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            setChainPropertiesVS2(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainPropertiesVS2(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);

            var totalCarbonCount = sn1CarbonCount + sn2CarbonCount;
            var totalDoubleBond = sn1DoubleBond + sn2DoubleBond;
            var totalOxidizedCount = sn1OxidizedCount + sn2OxidizedCount;

            var chainPrefix = sn1AcylChainString.StartsWith("O-") ? "O-" : sn1AcylChainString.StartsWith("P-") ? "P-" : string.Empty;
            var totalChain = getTotalChainString(totalCarbonCount, totalDoubleBond, totalOxidizedCount, lipidclass, chainPrefix, 2);
            var sublevelLipidName = lipidHeader + " " + totalChain;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidname;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
        }

        private static string getTotalChainString(int carbon, int rdb, int oxidized, LbmClass lipidclass, string chainPrefix, int acylChainCount)
        {
            var rdbString = rdb.ToString();

            if (lipidclass == LbmClass.Cer_EODS || lipidclass == LbmClass.Cer_EBDS
                || lipidclass == LbmClass.ASM
                || lipidclass == LbmClass.FAHFA || lipidclass == LbmClass.NAGly || lipidclass == LbmClass.NAGlySer || lipidclass == LbmClass.NAGlySer
                || lipidclass == LbmClass.TG_EST || lipidclass == LbmClass.DMEDFAHFA)
            {
                rdbString = (rdb + 1).ToString();
                oxidized = oxidized + 1;
            }

            if (lipidclass == LbmClass.Cer_EOS || lipidclass == LbmClass.HexCer_EOS)
            {
                if (acylChainCount != 2)
                {
                    rdbString = (rdb + 1).ToString();
                    oxidized = oxidized + 1;
                }
            }


            if (chainPrefix == "P-")
            {
                rdbString = (rdb - 1).ToString();
            }
            var oxString = oxidized == 0
                 ? string.Empty
                 : oxidized == 1
                     ? ";O"
                     : ";O" + oxidized;
            return chainPrefix + carbon + ":" + rdbString + oxString;
        }

        public static void setDoubleAcylChainsLipidAnnotation(LipidMolecule molecule,
            string lipidname, string lipidclassString)
        {

            var lipidheader = lipidname.Split(' ')[0];
            var acylchains = lipidname.Split(' ')[1];
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(lipidclassString);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(lipidclassString);
            var lipidName = lipidname; // SM d18:1/30:1
            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() != 2) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            string sn1Prefix, sn1Suffix;
            string sn2Prefix, sn2Suffix;

            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);

            getPrefixSuffix(sn1AcylChainString, out sn1Prefix, out sn1Suffix);
            getPrefixSuffix(sn2AcylChainString, out sn2Prefix, out sn2Suffix);

            var oxtotal = sn1OxidizedCount + sn2OxidizedCount;
            var oxString = oxtotal == 0 ? string.Empty : "+"  + "O" + oxtotal;

            var totalChain = sn1Prefix + (sn1CarbonCount + sn2CarbonCount).ToString() + ":" +
                (sn1DoubleBond + sn2DoubleBond).ToString() + sn1Suffix + oxString; // d48:2
            var sublevelLipidName = lipidheader + " " + totalChain; // SM d48:2
            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
        }

        private static void setTripleAcylChainsLipidAnnotation(LipidMolecule molecule,
            MspFormatCompoundInformationBean query)
        {

            var nameArray = query.Name.Split(';').ToArray();
            if (nameArray.Length == 2)
            { // e.g. in positive PC Na adduct, only sublevel information is resigtered like PC 36:3; [M+Na]+
                setSingleAcylChainsLipidAnnotation(molecule, query);
                return;
            }

            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);
            var sublevelLipidName = nameArray[0].Trim(); // TAG 64:6
            var totalChain = sublevelLipidName.Split(' ')[1]; // 64:6
            var lipidName = nameArray[1].Trim(); // TAG 18:0-22:0-24:6
            var adductInfo = nameArray[2].Trim();

            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() == 2)
            {
                setDoubleAcylChainsLipidAnnotation(molecule, query);
                return;
            }
            if (chainStrings.Count() != 3) return;
            if (totalChain == null || totalChain == string.Empty || !totalChain.Contains(':') || (totalChain.Contains('(') && !totalChain.Contains('+'))) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;
            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);
            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainProperties(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);

            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;
            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
        }

        private static void setTriAcylChainProperty(LipidMolecule molecule,
           string lipidname, string ontology, List<string> chainStrings)
        {

            if (chainStrings.Count != 3) return;

            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(ontology);
            var lipidHeader = GetLipidHeaderString(lipidname);

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];

            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;

            setChainPropertiesVS2(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainPropertiesVS2(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainPropertiesVS2(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);

            var totalCarbonCount = sn1CarbonCount + sn2CarbonCount + sn3CarbonCount;
            var totalDoubleBond = sn1DoubleBond + sn2DoubleBond + sn3DoubleBond;
            var totalOxidizedCount = sn1OxidizedCount + sn2OxidizedCount + sn3OxidizedCount;
            var chainPrefix = sn1AcylChainString.StartsWith("O-") ? "O-" : sn1AcylChainString.StartsWith("P-") ? "P-" : string.Empty;
            var totalChain = getTotalChainString(totalCarbonCount, totalDoubleBond, totalOxidizedCount, lipidclass, chainPrefix, 3);
            ////add MT
            //if (lipidname.Substring(0, 2)=="O-")
            //{
            //    totalChain = "O-" + totalChain;
            //}

            var sublevelLipidName = lipidHeader + " " + totalChain;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidname;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
        }

        private static void setTetraAcylChainProperty(LipidMolecule molecule,
           string lipidname, string ontology, List<string> chainStrings)
        {

            if (chainStrings.Count != 4) return;

            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(ontology);
            var lipidHeader = GetLipidHeaderString(lipidname);

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];
            var sn4AcylChainString = chainStrings[3];

            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;
            int sn4CarbonCount, sn4DoubleBond, sn4OxidizedCount;

            setChainPropertiesVS2(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainPropertiesVS2(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainPropertiesVS2(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);
            setChainPropertiesVS2(sn4AcylChainString, out sn4CarbonCount, out sn4DoubleBond, out sn4OxidizedCount);

            var totalCarbonCount = sn1CarbonCount + sn2CarbonCount + sn3CarbonCount + sn4CarbonCount;
            var totalDoubleBond = sn1DoubleBond + sn2DoubleBond + sn3DoubleBond + sn4DoubleBond;
            var totalOxidizedCount = sn1OxidizedCount + sn2OxidizedCount + sn3OxidizedCount + sn4OxidizedCount;
            var chainPrefix = sn1AcylChainString.StartsWith("O-") ? "O-" : sn1AcylChainString.StartsWith("P-") ? "P-" : string.Empty;
            var totalChain = getTotalChainString(totalCarbonCount, totalDoubleBond, totalOxidizedCount, lipidclass, chainPrefix, 4);
            var sublevelLipidName = lipidHeader + " " + totalChain;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidname;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
            molecule.Sn4AcylChainString = sn4AcylChainString;
            molecule.Sn4CarbonCount = sn4CarbonCount;
            molecule.Sn4DoubleBondCount = sn4DoubleBond;
            molecule.Sn4Oxidizedount = sn4OxidizedCount;
        }

        public static string GetLipidHeaderString(string lipidname)
        {
            var lipidHeader = lipidname.Split(' ')[0];
            if (lipidHeader == "SE" || lipidHeader == "ST" || lipidHeader == "SG" || lipidHeader == "BA" || lipidHeader == "ASG")
            {
                var dummyString = string.Empty;
                RetrieveSterolHeaderChainStrings(lipidname, out lipidHeader, out dummyString);
            }
            return lipidHeader;
        }

        public static void setTripleAcylChainsLipidAnnotation(LipidMolecule molecule,
            string lipidname, string lipidclassString)
        {

            var lipidheader = lipidname.Split(' ')[0];
            var acylchains = lipidname.Split(' ')[1];
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(lipidclassString);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(lipidclassString);
            var lipidName = lipidname; // SM d18:1/30:1
            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() != 3) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;

            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainProperties(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);

            string sn1Prefix, sn1Suffix;
            string sn2Prefix, sn2Suffix;
            string sn3Prefix, sn3Suffix;

            getPrefixSuffix(sn1AcylChainString, out sn1Prefix, out sn1Suffix);
            getPrefixSuffix(sn2AcylChainString, out sn2Prefix, out sn2Suffix);
            getPrefixSuffix(sn3AcylChainString, out sn3Prefix, out sn3Suffix);

            var oxtotal = sn1OxidizedCount + sn2OxidizedCount + sn3OxidizedCount;
            var oxString = oxtotal == 0 ? string.Empty : "+" + "O" + oxtotal;
            var totalChain = sn1Prefix + (sn1CarbonCount + sn2CarbonCount + sn3CarbonCount).ToString() + ":" +
                (sn1DoubleBond + sn2DoubleBond + sn3DoubleBond).ToString() + sn1Suffix + oxString; // d48:2
            var sublevelLipidName = lipidheader + " " + totalChain; // SM d48:2
            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
        }

        private static void setQuadAcylChainsLipidAnnotation(LipidMolecule molecule,
            MspFormatCompoundInformationBean query)
        {

            var nameArray = query.Name.Split(';').ToArray();
            if (nameArray.Length == 2)
            { // e.g. in positive PC Na adduct, only sublevel information is resigtered like PC 36:3; [M+Na]+
                setSingleAcylChainsLipidAnnotation(molecule, query);
                return;
            }

            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(query.CompoundClass);
            var sublevelLipidName = nameArray[0].Trim(); // TAG 64:6
            var totalChain = sublevelLipidName.Split(' ')[1]; // 64:6
            var lipidName = nameArray[1].Trim(); // TAG 18:0-22:0-24:6
            var adductInfo = nameArray[2].Trim();

            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() == 2)
            { //e.g. in positive CL, the chains are defined as double acyls
                setDoubleAcylChainsLipidAnnotation(molecule, query);
                return;
            }
            if (chainStrings.Count() != 4) return;
            if (totalChain == null || totalChain == string.Empty || !totalChain.Contains(':') || (totalChain.Contains('(') && !totalChain.Contains('+'))) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];
            var sn4AcylChainString = chainStrings[3];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;
            int sn4CarbonCount, sn4DoubleBond, sn4OxidizedCount;

            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);
            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainProperties(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);
            setChainProperties(sn4AcylChainString, out sn4CarbonCount, out sn4DoubleBond, out sn4OxidizedCount);

            molecule.Mz = query.PrecursorMz;
            molecule.Smiles = query.Smiles;
            molecule.InChIKey = query.InchiKey;
            molecule.Formula = query.Formula;
            molecule.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            molecule.IonMode = query.IonMode;

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
            molecule.Sn4AcylChainString = sn4AcylChainString;
            molecule.Sn4CarbonCount = sn4CarbonCount;
            molecule.Sn4DoubleBondCount = sn4DoubleBond;
            molecule.Sn4Oxidizedount = sn4OxidizedCount;
        }

        public static void setQuadAcylChainsLipidAnnotation(LipidMolecule molecule,
            string lipidname, string lipidclassString)
        {

            var lipidheader = lipidname.Split(' ')[0];
            var acylchains = lipidname.Split(' ')[1];
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(lipidclassString);
            var lipidclass = ConvertMsdialClassDefinitionToLbmClassEnum(lipidclassString);
            var lipidName = lipidname; // SM d18:1/30:1
            var chainStrings = acylChainStringSeparator(lipidName);
            if (chainStrings.Count() != 4) return;

            var sn1AcylChainString = chainStrings[0];
            var sn2AcylChainString = chainStrings[1];
            var sn3AcylChainString = chainStrings[2];
            var sn4AcylChainString = chainStrings[3];

            int totalCarbonCount, totalDoubleBond, totalOxidizedCount;
            int sn1CarbonCount, sn1DoubleBond, sn1OxidizedCount;
            int sn2CarbonCount, sn2DoubleBond, sn2OxidizedCount;
            int sn3CarbonCount, sn3DoubleBond, sn3OxidizedCount;
            int sn4CarbonCount, sn4DoubleBond, sn4OxidizedCount;

            setChainProperties(sn1AcylChainString, out sn1CarbonCount, out sn1DoubleBond, out sn1OxidizedCount);
            setChainProperties(sn2AcylChainString, out sn2CarbonCount, out sn2DoubleBond, out sn2OxidizedCount);
            setChainProperties(sn3AcylChainString, out sn3CarbonCount, out sn3DoubleBond, out sn3OxidizedCount);
            setChainProperties(sn4AcylChainString, out sn4CarbonCount, out sn4DoubleBond, out sn4OxidizedCount);

            string sn1Prefix, sn1Suffix;
            string sn2Prefix, sn2Suffix;
            string sn3Prefix, sn3Suffix;
            string sn4Prefix, sn4Suffix;

            getPrefixSuffix(sn1AcylChainString, out sn1Prefix, out sn1Suffix);
            getPrefixSuffix(sn2AcylChainString, out sn2Prefix, out sn2Suffix);
            getPrefixSuffix(sn3AcylChainString, out sn3Prefix, out sn3Suffix);
            getPrefixSuffix(sn4AcylChainString, out sn4Prefix, out sn4Suffix);

            var oxtotal = sn1OxidizedCount + sn2OxidizedCount + sn3OxidizedCount + sn4OxidizedCount;
            var oxString = oxtotal == 0 ? string.Empty : "+" + "O" + oxtotal;
            var totalChain = sn1Prefix + (sn1CarbonCount + sn2CarbonCount + sn3CarbonCount + sn4CarbonCount).ToString() + ":" +
                (sn1DoubleBond + sn2DoubleBond + sn3DoubleBond + sn4DoubleBond).ToString() + sn1Suffix + oxString; // d48:2
            var sublevelLipidName = lipidheader + " " + totalChain; // SM d48:2
            setChainProperties(totalChain, out totalCarbonCount, out totalDoubleBond, out totalOxidizedCount);

            molecule.SublevelLipidName = sublevelLipidName;
            molecule.LipidName = lipidName;
            molecule.LipidClass = lipidclass;
            molecule.TotalChainString = totalChain;
            molecule.TotalCarbonCount = totalCarbonCount;
            molecule.TotalDoubleBondCount = totalDoubleBond;
            molecule.TotalOxidizedCount = totalOxidizedCount;
            molecule.Sn1AcylChainString = sn1AcylChainString;
            molecule.Sn1CarbonCount = sn1CarbonCount;
            molecule.Sn1DoubleBondCount = sn1DoubleBond;
            molecule.Sn1Oxidizedount = sn1OxidizedCount;
            molecule.Sn2AcylChainString = sn2AcylChainString;
            molecule.Sn2CarbonCount = sn2CarbonCount;
            molecule.Sn2DoubleBondCount = sn2DoubleBond;
            molecule.Sn2Oxidizedount = sn2OxidizedCount;
            molecule.Sn3AcylChainString = sn3AcylChainString;
            molecule.Sn3CarbonCount = sn3CarbonCount;
            molecule.Sn3DoubleBondCount = sn3DoubleBond;
            molecule.Sn3Oxidizedount = sn3OxidizedCount;
            molecule.Sn4AcylChainString = sn4AcylChainString;
            molecule.Sn4CarbonCount = sn4CarbonCount;
            molecule.Sn4DoubleBondCount = sn4DoubleBond;
            molecule.Sn4Oxidizedount = sn4OxidizedCount;
        }

        private static void setChainPropertiesVS2(string chainString, out int carbonCount, out int doubleBondCount, out int oxidizedCount)
        {

            carbonCount = 0;
            doubleBondCount = 0;
            oxidizedCount = 0;

            //pattern: 18:1, 18:1e, 18:1p d18:1, t20:0, n-18:0, N-19:0, 20:3+3O, 18:2-SN1, O-18:1, P-18:1, N-18:1, 16:0;O, 18:2;2O -> 18:2;O2, 18:2;(2OH) -> 18:2(2OH)
            //try convertion
            var isPlasmenyl = chainString.Contains("P-") ? true : false;
            chainString = chainString.Replace("O-", "").Replace("P-", "").Replace("N-", "").Replace("e", "").Replace("p", "").Replace("m", "").Replace("n-", "").Replace("d", "").Replace("t", "");

            // for oxidized moiety parser
            if (chainString.Contains(";"))
            { // e.g. 18:2;2O, 18:2;(2OH),18:2;O2
                var chain = chainString.Split(';')[0];
                var oxidizedmoiety = chainString.Split(';')[1]; //2O, (2OH)
                //modified by MT 2020/12/11 & 2021/01/12
                var expectedOxCount = oxidizedmoiety.Replace("O", string.Empty).Replace("H", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
                if (expectedOxCount == string.Empty || expectedOxCount == "")
                {
                    expectedOxCount = "1";
                }
                else if (oxidizedmoiety.Contains("(2OH)") || oxidizedmoiety.Contains("(3OH)"))
                {
                    expectedOxCount = "1";
                }
                int.TryParse(expectedOxCount, out oxidizedCount);
                chainString = chain;
            }
            else if (chainString.Contains("+"))
            { //20:3+3O
                var chain = chainString.Split('+')[0]; // 20:3
                var expectedOxCount = chainString.Split('+')[1].Replace("O", ""); //2
                if (expectedOxCount == string.Empty || expectedOxCount == "")
                {
                    expectedOxCount = "1";
                }
                int.TryParse(expectedOxCount, out oxidizedCount);
                chainString = chain;
            }
            else if (chainString.Contains("("))
            { // e.g. 18:2(2OH)
                var chain = chainString.Split('(')[0];
                var oxidizedmoiety = chainString.Split('(')[1]; //2OH)
                //modified by MT 2020/12/11 & 2021/01/12
                var expectedOxCount = oxidizedmoiety.Replace("O", string.Empty).Replace("H", string.Empty).Replace("(", string.Empty).Replace(")", string.Empty);
                if (expectedOxCount == string.Empty || expectedOxCount == "")
                {
                    expectedOxCount = "1";
                }
                else if (oxidizedmoiety.Contains("2OH)") || oxidizedmoiety.Contains("3OH)"))
                {
                    expectedOxCount = "1";
                }
                int.TryParse(expectedOxCount, out oxidizedCount);
                chainString = chain;
            }


            // for SN1/SN2 string parser 
            if (chainString.Contains("-SN"))
            {
                if (chainString.Contains("-SN1"))
                {
                    chainString = chainString.Replace("-SN1", "");
                }
                else if (chainString.Contains("-SN2"))
                {
                    chainString = chainString.Replace("-SN2", "");
                }
            }

            // here all string expected as (carbon):(double)
            var carbon = chainString.Split(':')[0];
            var doublebond = chainString.Split(':')[1];
            int.TryParse(carbon, out carbonCount);
            int.TryParse(doublebond, out doubleBondCount);
            if (isPlasmenyl) doubleBondCount++;
        }

        private static void setChainProperties(string chainString, out int carbonCount, out int doubleBondCount, out int oxidizedCount)
        {

            carbonCount = -1;
            doubleBondCount = -1;
            oxidizedCount = -1;

            //pattern: 18:1, 18:1e, 18:1p d18:1, t20:0, n-18:0, N-19:0, 20:3+3O, 20:3+2O(2Cyc), 18:2-SN1, O-18:1, P-18:1, N-18:1, 16:0;O, 18:2;2O, 18:2;(2OH)

            if (chainString.Contains("e") || chainString.Contains("p") || chainString.Contains("m") ||
                chainString.StartsWith("d") || chainString.StartsWith("t") || chainString.StartsWith("n-") || chainString.StartsWith("N-") ||
                chainString.StartsWith("O-") || chainString.StartsWith("P-"))
            {
                chainString = chainString.Replace("O-", "").Replace("P-", "").Replace("N-", "").Replace("e", "").Replace("p", "").Replace("m", "").Replace("n-", "").Replace("d", "").Replace("t", "");

                var carbon = chainString.Split(':')[0];
                var doublebond = chainString.Split(':')[1];
                int.TryParse(carbon, out carbonCount);
                int.TryParse(doublebond, out doubleBondCount);
                oxidizedCount = 0;
            }

            if (chainString.Contains("-SN1") || chainString.Contains("-SN2"))
            {
                if (chainString.Contains("-SN1"))
                {
                    chainString = chainString.Replace("-SN1", "");
                }
                else if (chainString.Contains("-SN2"))
                {
                    chainString = chainString.Replace("-SN2", "");
                }
                var carbon = chainString.Split(':')[0];
                var doublebond = chainString.Split(':')[1];
                int.TryParse(carbon, out carbonCount);
                int.TryParse(doublebond, out doubleBondCount);
                oxidizedCount = 0;
            }

            if (chainString.Contains("+") && chainString.Contains("("))
            { // it means 20:3+2O(2Cyc) case
                var separatedArray = chainString.Replace("Cyc)", "").Split('(');
                if (separatedArray.Length == 2)
                {
                    int cycleCount;
                    if (int.TryParse(separatedArray[1], out cycleCount))
                    {
                        var oxAcylString = separatedArray[0]; // should be 20:3+2O
                        var acylString = oxAcylString.Split('+')[0]; // 20:3
                        var carbonString = acylString.Split(':')[0]; //20
                        var doublebondString = acylString.Split(':')[1]; //3
                        var oxCountString = oxAcylString.Split('+')[1].Replace("O", ""); //2

                        if (oxCountString == string.Empty || oxCountString == "")
                        {
                            oxCountString = "1";
                        }

                        int.TryParse(carbonString, out carbonCount);
                        int.TryParse(oxCountString, out oxidizedCount);
                        int.TryParse(doublebondString, out doubleBondCount);
                        doubleBondCount += cycleCount;
                    }
                }
            }
            else if (chainString.Contains("+"))
            { // it means 20:3+2O case
                var acylString = chainString.Split('+')[0]; // 20:3
                var carbonString = acylString.Split(':')[0]; //20
                var doublebondString = acylString.Split(':')[1]; //3
                var oxCountString = chainString.Split('+')[1].Replace("O", ""); //2
                if (oxCountString == string.Empty || oxCountString == "")
                {
                    oxCountString = "1";
                }

                int.TryParse(carbonString, out carbonCount);
                int.TryParse(oxCountString, out oxidizedCount);
                int.TryParse(doublebondString, out doubleBondCount);
            }
            else
            {
                var carbon = chainString.Split(':')[0];
                var doublebond = chainString.Split(':')[1];
                int.TryParse(carbon, out carbonCount);
                int.TryParse(doublebond, out doubleBondCount);
                oxidizedCount = 0;
            }
        }

        private static void getPrefixSuffix(string chainString, out string prefix, out string suffix)
        {

            prefix = string.Empty;
            suffix = string.Empty;

            if (chainString.Contains("e"))
            {
                suffix = "e";
            }
            else if (chainString.Contains("p"))
            {
                suffix = "p";
            }

            if (chainString.Contains("m"))
            {
                prefix = "m";
            }
            else if (chainString.Contains("d"))
            {
                prefix = "d";
            }
            else if (chainString.Contains("t"))
            {
                prefix = "t";
            }
            else if (chainString.Contains("n-"))
            {
                prefix = "n-";
            }
        }


        // this method is not capable of oxidized form.
        private static List<string> acylChainStringSeparator(string moleculeString)
        {
            var chains = new List<string>();
            string[] acylArray = null;

            if (moleculeString.Contains("-O-"))
            {
                var cMolString = moleculeString.Replace("-O-", "-");
                if (cMolString.Contains("/"))
                {
                    cMolString = cMolString.Replace("/", "-");
                }

                acylArray = cMolString.Split(' ')[1].Split('-');
            }
            else if (moleculeString.Contains("(") && !moleculeString.Contains("Cyc"))
            {

                if (moleculeString.Contains("/"))
                {
                    acylArray = moleculeString.Split('(')[1].Split(')')[0].Split('/');
                }
                else
                {
                    acylArray = moleculeString.Split('(')[1].Split(')')[0].Split('-');
                }
            }
            else if (moleculeString.Contains("/n-"))
            {
                var cMolString = moleculeString.Replace("/n-", "-");
                if (cMolString.Contains("/"))
                {
                    cMolString = cMolString.Replace("/", "-");
                }

                acylArray = cMolString.Split(' ')[1].Split('-');
            }
            else
            {
                var cMolString = moleculeString;
                if (cMolString.Contains("/"))
                {
                    cMolString = cMolString.Replace("/", "-");
                }
                acylArray = cMolString.Split(' ')[1].Split('-');
            }

            for (int i = 0; i < acylArray.Length; i++)
            {
                if (i == 0 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 1 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 2 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 3 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
            }
            return chains;
        }

        private static List<string> acylChainStringSeparatorVS2(string moleculeString)
        {

            if (moleculeString.Split(' ').Length == 1) return null;

            // pattern [1] ADGGA 12:0_12:0_12:0
            // pattern [2] AHexCer (O-14:0)16:1;2O/14:0;O, ADGGA (O-24:0)17:2_22:6  
            // pattern [2_1] AHexCer (O-14:0)42:2;3O, ADGGA (O-24:0)36:2  
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
            if (chainString.Contains('|')) return null;
            Regex reg = new Regex(@"\(d([0-9]*)\)");
            chainString = reg.Replace(chainString, "");

            var pattern2 = @"(\()(?<chain1>.+?)(\))(?<chain2>.+?)([/_])(?<chain3>.+?$)";
            var pattern2_1 = @"(\()(?<chain1>.+?)(\))(?<chain2>.+?$)";
            var pattern3 = @"(?<chain1>.+?)(\(FA )(?<chain2>.+?)(\))";
            var pattern4 = @"(?<chain1>.+?)(/)(?<chain2>.+?)(\(FA )(?<chain3>.+?)(\))";
            var pattern12 = @"(\(FA )(?<chain2>.+?)(\))(?<chain1>.+?$)";
            var pattern13 = @"(\()(?<chain1>.+?)(\))(?<chain2>.+?)(_)(?<chain3>.+?$)";

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
            else if (chainString.Contains("(O-"))
            { // pattern 2
                if (Regex.IsMatch(chainString, "[/_]"))
                {
                    // pattern 2
                    var regexes = Regex.Match(chainString, pattern2).Groups;
                    chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value, regexes["chain3"].Value };
                    //Console.WriteLine();
                }
                else
                {
                    // pattern 2_1
                    var regexes = Regex.Match(chainString, pattern2_1).Groups;
                    chains = new List<string>() { regexes["chain1"].Value, regexes["chain2"].Value };
                    //Console.WriteLine();
                }
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

        public static List<string> GetLipidClasses()
        {
            var names = new List<string>();
            foreach (var lipid in Enum.GetValues(typeof(LbmClass)).Cast<LbmClass>())
            {
                var cName = ConvertLbmClassEnumToMsdialClassDefinitionVS2(lipid);
                if (cName != "Undefined" && !names.Contains(cName))
                    names.Add(cName);
            }
            return names;
        }

        public static string ConvertLbmClassEnumToMsdialClassDefinition(LbmClass lipidclass)
        {
            switch (lipidclass)
            {
                case LbmClass.MG: return "MAG";
                case LbmClass.DG: return "DAG";
                case LbmClass.TG: return "TAG";
                case LbmClass.EtherTG: return "EtherTAG";
                case LbmClass.EtherDG: return "EtherDAG";
                case LbmClass.LPC: return "LPC";
                case LbmClass.LPA: return "LPA";
                case LbmClass.LPE: return "LPE";
                case LbmClass.LPG: return "LPG";
                case LbmClass.LPI: return "LPI";
                case LbmClass.LPS: return "LPS";
                case LbmClass.LDGTS: return "LDGTS";
                case LbmClass.LDGTA: return "LDGTA";
                case LbmClass.LDGCC: return "LDGCC";
                case LbmClass.PC: return "PC";
                case LbmClass.PA: return "PA";
                case LbmClass.PE: return "PE";
                case LbmClass.PG: return "PG";
                case LbmClass.PI: return "PI";
                case LbmClass.PS: return "PS";
                case LbmClass.PT: return "PT";
                case LbmClass.BMP: return "BMP";
                case LbmClass.HBMP: return "HBMP";
                case LbmClass.CL: return "CL";
                case LbmClass.DLCL: return "DLCL";
                case LbmClass.MLCL: return "LCL";
                case LbmClass.EtherPC: return "EtherPC";
                case LbmClass.EtherPE: return "EtherPE";
                case LbmClass.EtherPS: return "EtherPS";
                case LbmClass.EtherPI: return "EtherPI";
                case LbmClass.EtherPG: return "EtherPG";
                case LbmClass.EtherLPC: return "EtherLPC";
                case LbmClass.EtherLPE: return "EtherLPE";
                case LbmClass.EtherLPS: return "EtherLPS";
                case LbmClass.EtherLPI: return "EtherLPI";
                case LbmClass.EtherLPG: return "EtherLPG";
                case LbmClass.OxPC: return "OxPC";
                case LbmClass.OxPE: return "OxPE";
                case LbmClass.OxPG: return "OxPG";
                case LbmClass.OxPI: return "OxPI";
                case LbmClass.OxPS: return "OxPS";
                case LbmClass.EtherOxPC: return "EtherOxPC";
                case LbmClass.EtherOxPE: return "EtherOxPE";
                case LbmClass.PMeOH: return "PMeOH";
                case LbmClass.PEtOH: return "PEtOH";
                case LbmClass.PBtOH: return "PBtOH";
                case LbmClass.LNAPE: return "LNAPE";
                case LbmClass.LNAPS: return "LNAPS";
                case LbmClass.DGDG: return "DGDG";
                case LbmClass.MGDG: return "MGDG";
                case LbmClass.SQDG: return "SQDG";
                case LbmClass.DGTS: return "DGTS";
                case LbmClass.DGTA: return "DGTA";
                case LbmClass.DGCC: return "DGCC";
                case LbmClass.DGGA: return "DGGA";
                case LbmClass.ADGGA: return "ADGGA";
                case LbmClass.EtherMGDG: return "EtherMGDG";
                case LbmClass.EtherDGDG: return "EtherDGDG";
                case LbmClass.CE: return "CE";
                case LbmClass.BRSE: return "BRSE";
                case LbmClass.CASE: return "CASE";
                case LbmClass.SISE: return "SISE";
                case LbmClass.STSE: return "STSE";
                case LbmClass.AHexCS: return "AHexCS";
                case LbmClass.AHexBRS: return "AHexBRS";
                case LbmClass.AHexCAS: return "AHexCAS";
                case LbmClass.AHexSIS: return "AHexSIS";
                case LbmClass.AHexSTS: return "AHexSTS";
                case LbmClass.DCAE: return "DCAE";
                case LbmClass.GDCAE: return "GDCAE";
                case LbmClass.GLCAE: return "GLCAE";
                case LbmClass.TDCAE: return "TDCAE";
                case LbmClass.TLCAE: return "TLCAE";
                case LbmClass.Cholesterol: return "Cholesterol";
                case LbmClass.CholesterolSulfate: return "CholesterolSulfate";
                case LbmClass.SHex: return "SHex";
                case LbmClass.SSulfate: return "SSulfate";
                case LbmClass.BAHex: return "BAHex";
                case LbmClass.BASulfate: return "BASulfate";
                case LbmClass.Vitamin_E: return "Vitamin";
                case LbmClass.VAE: return "VAE";
                case LbmClass.BileAcid: return "BileAcid";
                case LbmClass.CoQ: return "CoQ";
                case LbmClass.CAR: return "ACar";
                case LbmClass.FA: return "FA";
                case LbmClass.OxFA: return "OxFA";
                case LbmClass.NAE: return "NAE";
                case LbmClass.NAGly: return "NAAG";
                case LbmClass.NAGlySer: return "NAAGS";
                case LbmClass.NAOrn: return "NAAO";
                case LbmClass.FAHFA: return "FAHFA";
                case LbmClass.DMEDFAHFA: return "DMEDFAHFA";
                case LbmClass.DMEDFA: return "DMEDFA";
                case LbmClass.DMEDOxFA: return "DMEDOxFA";
                case LbmClass.PhytoSph: return "Phytosphingosine";
                case LbmClass.DHSph: return "Sphinganine";
                case LbmClass.Sph: return "Sphingosine";
                case LbmClass.Cer_ADS: return "Cer-ADS";
                case LbmClass.Cer_AS: return "Cer-AS";
                case LbmClass.Cer_BS: return "Cer-BS";
                case LbmClass.Cer_BDS: return "Cer-BDS";
                case LbmClass.Cer_NDS: return "Cer-NDS";
                case LbmClass.Cer_NS: return "Cer-NS";
                case LbmClass.Cer_NP: return "Cer-NP";
                case LbmClass.Cer_AP: return "Cer-AP";
                case LbmClass.Cer_EODS: return "Cer-EODS";
                case LbmClass.Cer_EOS: return "Cer-EOS";
                case LbmClass.Cer_OS: return "Cer-OS";
                case LbmClass.Cer_HS: return "Cer-HS";
                case LbmClass.Cer_HDS: return "Cer-HDS";
                case LbmClass.Cer_NDOS: return "Cer-NDOS";
                case LbmClass.HexCer_NS: return "HexCer-NS";
                case LbmClass.HexCer_NDS: return "HexCer-NDS";
                case LbmClass.HexCer_AP: return "HexCer-AP";
                case LbmClass.HexCer_HS: return "HexCer-HS";
                case LbmClass.HexCer_HDS: return "HexCer-HDS";
                case LbmClass.Hex2Cer: return "Hex2Cer";
                case LbmClass.Hex3Cer: return "Hex3Cer";
                case LbmClass.PE_Cer: return "PE-Cer";
                case LbmClass.PI_Cer: return "PI-Cer";
                case LbmClass.CerP: return "CerP";
                case LbmClass.SM: return "SM";
                case LbmClass.SHexCer: return "SHexCer";
                case LbmClass.SL: return "SL";
                case LbmClass.GM3: return "GM3";
                case LbmClass.Ac2PIM1: return "Ac2PIM1";
                case LbmClass.Ac2PIM2: return "Ac2PIM2";
                case LbmClass.Ac3PIM2: return "Ac3PIM2";
                case LbmClass.Ac4PIM2: return "Ac4PIM2";
                case LbmClass.Cer_EBDS: return "Cer-EBDS";
                case LbmClass.AHexCer: return "AHexCer";
                case LbmClass.ASM: return "ASM";
                case LbmClass.EtherSMGDG: return "EtherSMGDG";
                case LbmClass.SMGDG: return "SMGDG";
                case LbmClass.GPNAE: return "GPNAE";
                case LbmClass.MGMG: return "MGMG";
                case LbmClass.DGMG: return "DGMG";
                case LbmClass.GD1a: return "GD1a";
                case LbmClass.GD1b: return "GD1b";
                case LbmClass.GD2: return "GD2";
                case LbmClass.GD3: return "GD3";
                case LbmClass.GM1: return "GM1";
                case LbmClass.ST: return "ST";
                case LbmClass.GT1b: return "GT1b";
                case LbmClass.GQ1b: return "GQ1b";
                case LbmClass.NGcGM3: return "NGcGM3";

                case LbmClass.LPC_d5: return "LPC_d5";
                case LbmClass.LPE_d5: return "LPE_d5";
                case LbmClass.LPG_d5: return "LPG_d5";
                case LbmClass.LPI_d5: return "LPI_d5";
                case LbmClass.LPS_d5: return "LPS_d5";

                case LbmClass.PC_d5: return "PC_d5";
                case LbmClass.PE_d5: return "PE_d5";
                case LbmClass.PG_d5: return "PG_d5";
                case LbmClass.PI_d5: return "PI_d5";
                case LbmClass.PS_d5: return "PS_d5";

                case LbmClass.DG_d5: return "DG_d5";
                case LbmClass.TG_d5: return "TG_d5";

                case LbmClass.CE_d7: return "CE_d7";
                case LbmClass.Cer_NS_d7: return "Cer_NS_d7";
                case LbmClass.SM_d9: return "SM_d9";

                case LbmClass.bmPC: return "bmPC";
                default: return "Undefined";
            }
        }

        public static string ConvertLbmClassEnumToMsdialClassDefinitionVS2(LbmClass lipidclass)
        {
            switch (lipidclass)
            {
                case LbmClass.MG: return "MG";
                case LbmClass.DG: return "DG";
                case LbmClass.TG: return "TG";
                case LbmClass.OxTG: return "OxTG";
                case LbmClass.TG_EST: return "TG_EST";
                case LbmClass.EtherTG: return "EtherTG";
                case LbmClass.EtherDG: return "EtherDG";
                case LbmClass.LPC: return "LPC";
                case LbmClass.LPA: return "LPA";
                case LbmClass.LPE: return "LPE";
                case LbmClass.LPG: return "LPG";
                case LbmClass.LPI: return "LPI";
                case LbmClass.LPS: return "LPS";
                case LbmClass.LDGTS: return "LDGTS";
                case LbmClass.LDGTA: return "LDGTA";
                case LbmClass.LDGCC: return "LDGCC";
                case LbmClass.PC: return "PC";
                case LbmClass.PA: return "PA";
                case LbmClass.PE: return "PE";
                case LbmClass.PG: return "PG";
                case LbmClass.PI: return "PI";
                case LbmClass.PS: return "PS";
                case LbmClass.PT: return "PT";
                case LbmClass.BMP: return "BMP";
                case LbmClass.HBMP: return "HBMP";
                case LbmClass.CL: return "CL";
                case LbmClass.DLCL: return "DLCL";
                case LbmClass.MLCL: return "MLCL";
                case LbmClass.EtherPC: return "EtherPC";
                case LbmClass.EtherPE: return "EtherPE";
                case LbmClass.EtherPS: return "EtherPS";
                case LbmClass.EtherPI: return "EtherPI";
                case LbmClass.EtherPG: return "EtherPG";
                case LbmClass.EtherLPC: return "EtherLPC";
                case LbmClass.EtherLPE: return "EtherLPE";
                case LbmClass.EtherLPS: return "EtherLPS";
                case LbmClass.EtherLPI: return "EtherLPI";
                case LbmClass.EtherLPG: return "EtherLPG";
                case LbmClass.OxPC: return "OxPC";
                case LbmClass.OxPE: return "OxPE";
                case LbmClass.OxPG: return "OxPG";
                case LbmClass.OxPI: return "OxPI";
                case LbmClass.OxPS: return "OxPS";
                case LbmClass.EtherOxPC: return "EtherOxPC";
                case LbmClass.EtherOxPE: return "EtherOxPE";
                case LbmClass.PMeOH: return "PMeOH";
                case LbmClass.PEtOH: return "PEtOH";
                case LbmClass.PBtOH: return "PBtOH";
                case LbmClass.MMPE: return "MMPE";
                case LbmClass.DMPE: return "DMPE";
                case LbmClass.LNAPE: return "LNAPE";
                case LbmClass.LNAPS: return "LNAPS";
                case LbmClass.DGDG: return "DGDG";
                case LbmClass.MGDG: return "MGDG";
                case LbmClass.SQDG: return "SQDG";
                case LbmClass.DGTS: return "DGTS";
                case LbmClass.DGTA: return "DGTA";
                case LbmClass.DGCC: return "DGCC";
                case LbmClass.DGGA: return "DGGA";
                case LbmClass.ADGGA: return "ADGGA";
                case LbmClass.EtherMGDG: return "EtherMGDG";
                case LbmClass.EtherDGDG: return "EtherDGDG";
                case LbmClass.CE: return "CE";
                case LbmClass.BRSE: return "BRSE";
                case LbmClass.CASE: return "CASE";
                case LbmClass.SISE: return "SISE";
                case LbmClass.STSE: return "STSE";
                case LbmClass.EGSE: return "EGSE";
                case LbmClass.DEGSE: return "DEGSE";
                case LbmClass.DSMSE: return "DSMSE";
                case LbmClass.AHexCS: return "AHexCS";
                case LbmClass.AHexBRS: return "AHexBRS";
                case LbmClass.AHexCAS: return "AHexCAS";
                case LbmClass.AHexSIS: return "AHexSIS";
                case LbmClass.AHexSTS: return "AHexSTS";
                case LbmClass.DCAE: return "DCAE";
                case LbmClass.GDCAE: return "GDCAE";
                case LbmClass.GLCAE: return "GLCAE";
                case LbmClass.TDCAE: return "TDCAE";
                case LbmClass.TLCAE: return "TLCAE";
                case LbmClass.LCAE: return "LCAE";
                case LbmClass.KLCAE: return "KLCAE";
                case LbmClass.KDCAE: return "KDCAE";
                case LbmClass.Cholesterol: return "Cholesterol";
                case LbmClass.CholesterolSulfate: return "CholesterolSulfate";
                case LbmClass.SHex: return "SHex";
                case LbmClass.SSulfate: return "SSulfate";
                case LbmClass.BAHex: return "BAHex";
                case LbmClass.BASulfate: return "BASulfate";
                case LbmClass.Vitamin_E: return "Vitamin_E";
                case LbmClass.Vitamin_D: return "Vitamin_D";
                case LbmClass.VAE: return "VAE";
                case LbmClass.BileAcid: return "BileAcid";
                case LbmClass.CoQ: return "CoQ";
                case LbmClass.CAR: return "CAR";
                case LbmClass.FA: return "FA";
                case LbmClass.OxFA: return "OxFA";
                case LbmClass.NAE: return "NAE";
                case LbmClass.NAGly: return "NAGly";
                case LbmClass.NAGlySer: return "NAGlySer";
                case LbmClass.NAOrn: return "NAOrn";
                case LbmClass.NAPhe: return "NAPhe";
                case LbmClass.NATau: return "NATau";
                case LbmClass.FAHFA: return "FAHFA";
                case LbmClass.DMEDFAHFA: return "DMEDFAHFA";
                case LbmClass.DMEDFA: return "DMEDFA";
                case LbmClass.DMEDOxFA: return "DMEDFA";
                case LbmClass.PhytoSph: return "PhytoSph";
                case LbmClass.DHSph: return "DHSph";
                case LbmClass.Sph: return "Sph";
                case LbmClass.Cer_ADS: return "Cer-ADS";
                case LbmClass.Cer_AS: return "Cer-AS";
                case LbmClass.Cer_BS: return "Cer-BS";
                case LbmClass.Cer_BDS: return "Cer-BDS";
                case LbmClass.Cer_NDS: return "Cer-NDS";
                case LbmClass.Cer_NS: return "Cer-NS";
                case LbmClass.Cer_NP: return "Cer-NP";
                case LbmClass.Cer_AP: return "Cer-AP";
                case LbmClass.Cer_EODS: return "Cer-EODS";
                case LbmClass.Cer_EOS: return "Cer-EOS";
                case LbmClass.Cer_OS: return "Cer-OS";
                case LbmClass.Cer_HS: return "Cer-HS";
                case LbmClass.Cer_HDS: return "Cer-HDS";
                case LbmClass.Cer_NDOS: return "Cer-NDOS";
                case LbmClass.HexCer_NS: return "HexCer-NS";
                case LbmClass.HexCer_NDS: return "HexCer-NDS";
                case LbmClass.HexCer_AP: return "HexCer-AP";
                case LbmClass.HexCer_HS: return "HexCer-HS";
                case LbmClass.HexCer_HDS: return "HexCer-HDS";
                case LbmClass.HexCer_EOS: return "HexCer-EOS";
                case LbmClass.Hex2Cer: return "Hex2Cer";
                case LbmClass.Hex3Cer: return "Hex3Cer";
                case LbmClass.PE_Cer: return "PE-Cer";
                case LbmClass.PI_Cer: return "PI-Cer";
                case LbmClass.MIPC: return "MIPC";
                case LbmClass.CerP: return "CerP";
                case LbmClass.SM: return "SM";
                case LbmClass.SHexCer: return "SHexCer";
                case LbmClass.SL: return "SL";
                case LbmClass.GM3: return "GM3";
                case LbmClass.Ac2PIM1: return "Ac2PIM1";
                case LbmClass.Ac2PIM2: return "Ac2PIM2";
                case LbmClass.Ac3PIM2: return "Ac3PIM2";
                case LbmClass.Ac4PIM2: return "Ac4PIM2";
                case LbmClass.Cer_EBDS: return "Cer-EBDS";
                case LbmClass.AHexCer: return "AHexCer";
                case LbmClass.ASM: return "ASM";
                case LbmClass.EtherSMGDG: return "EtherSMGDG";
                case LbmClass.SMGDG: return "SMGDG";
                case LbmClass.GPNAE: return "GPNAE";
                case LbmClass.MGMG: return "MGMG";
                case LbmClass.DGMG: return "DGMG";

                case LbmClass.GD1a: return "GD1a";
                case LbmClass.GD1b: return "GD1b";
                case LbmClass.GD2: return "GD2";
                case LbmClass.GD3: return "GD3";
                case LbmClass.GM1: return "GM1";
                case LbmClass.ST: return "ST";
                case LbmClass.GT1b: return "GT1b";
                case LbmClass.GQ1b: return "GQ1b";
                case LbmClass.NGcGM3: return "NGcGM3";
                case LbmClass.SPEHex: return "SPEHex";
                case LbmClass.SPGHex: return "SPGHex";
                case LbmClass.CSLPHex: return "CSLPHex";
                case LbmClass.BRSLPHex: return "BRSLPHex";
                case LbmClass.CASLPHex: return "CASLPHex";
                case LbmClass.SISLPHex: return "SISLPHex";
                case LbmClass.STSLPHex: return "STSLPHex";
                case LbmClass.CSPHex: return "CSPHex";
                case LbmClass.CASPHex: return "CASPHex";
                case LbmClass.SISPHex: return "SISPHex";
                case LbmClass.STSPHex: return "STSPHex";
                case LbmClass.SPE: return "SPE";

                case LbmClass.PC_d5: return "PC_d5";
                case LbmClass.PE_d5: return "PE_d5";
                case LbmClass.PS_d5: return "PS_d5";
                case LbmClass.PG_d5: return "PG_d5";
                case LbmClass.PI_d5: return "PI_d5";
                case LbmClass.SM_d9: return "SM_d9";
                case LbmClass.Cer_NS_d7: return "Cer_NS_d7";
                case LbmClass.TG_d5: return "TG_d5";
                case LbmClass.CE_d7: return "CE_d7";
                case LbmClass.LPC_d5: return "PC_d5";
                case LbmClass.LPE_d5: return "PE_d5";
                case LbmClass.LPS_d5: return "PS_d5";
                case LbmClass.LPG_d5: return "PG_d5";
                case LbmClass.LPI_d5: return "PI_d5";
                case LbmClass.DG_d5: return "TG_d5";

                case LbmClass.bmPC: return "bmPC";

                default: return "Undefined";
            }
        }

        public static string ConvertMsdialLbmStringToMsdialOfficialOntology(string lipidclass)
        {
            var lbmclass = ConvertMsdialClassDefinitionToLbmClassEnumVS2(lipidclass);
            return ConvertLbmClassEnumToMsdialClassDefinitionVS2(lbmclass);
        }


        public static LbmClass ConvertMsdialClassDefinitionToLbmClassEnum(string lipidclass)
        {
            switch (lipidclass)
            {
                case "MAG": return LbmClass.MG;
                case "DAG": return LbmClass.DG;
                case "TAG": return LbmClass.TG;
                case "EtherDAG": return LbmClass.EtherDG;
                case "EtherTAG": return LbmClass.EtherTG;

                case "LPC": return LbmClass.LPC;
                case "LPA": return LbmClass.LPA;
                case "LPE": return LbmClass.LPE;
                case "LPG": return LbmClass.LPG;
                case "LPI": return LbmClass.LPI;
                case "LPS": return LbmClass.LPS;
                case "LDGTS": return LbmClass.LDGTS;
                case "LDGTA": return LbmClass.LDGTA;
                case "LDGCC": return LbmClass.LDGCC;

                case "EtherLPC": return LbmClass.EtherLPC;
                case "EtherLPE": return LbmClass.EtherLPE;
                case "EtherLPG": return LbmClass.EtherLPG;
                case "EtherLPI": return LbmClass.EtherLPI;
                case "EtherLPS": return LbmClass.EtherLPS;

                case "PC": return LbmClass.PC;
                case "PA": return LbmClass.PA;
                case "PE": return LbmClass.PE;
                case "PG": return LbmClass.PG;
                case "PI": return LbmClass.PI;
                case "PS": return LbmClass.PS;
                case "PT": return LbmClass.PT;
                case "BMP": return LbmClass.BMP;
                case "HBMP": return LbmClass.HBMP;
                case "CL": return LbmClass.CL;
                case "DLCL": return LbmClass.DLCL;
                case "LCL": return LbmClass.MLCL;

                case "EtherPC": return LbmClass.EtherPC;
                case "EtherPE": return LbmClass.EtherPE;
                case "EtherPG": return LbmClass.EtherPG;
                case "EtherPI": return LbmClass.EtherPI;
                case "EtherPS": return LbmClass.EtherPS;
                case "EtherMGDG": return LbmClass.EtherMGDG;
                case "EtherDGDG": return LbmClass.EtherDGDG;

                case "OxPC": return LbmClass.OxPC;
                case "OxPE": return LbmClass.OxPE;
                case "OxPG": return LbmClass.OxPG;
                case "OxPI": return LbmClass.OxPI;
                case "OxPS": return LbmClass.OxPS;

                case "EtherOxPC": return LbmClass.EtherOxPC;
                case "EtherOxPE": return LbmClass.EtherOxPE;

                case "PMeOH": return LbmClass.PMeOH;
                case "PEtOH": return LbmClass.PEtOH;
                case "PBtOH": return LbmClass.PBtOH;

                case "LNAPE": return LbmClass.LNAPE;
                case "LNAPS": return LbmClass.LNAPS;

                case "DGDG": return LbmClass.DGDG;
                case "MGDG": return LbmClass.MGDG;
                case "SQDG": return LbmClass.SQDG;
                case "DGTS": return LbmClass.DGTS;
                case "DGTA": return LbmClass.DGTA;
                case "DGCC": return LbmClass.DGCC;
                case "GlcADG": return LbmClass.DGGA;
                case "AcylGlcADG": return LbmClass.ADGGA;
                case "DGGA": return LbmClass.DGGA;
                case "ADGGA": return LbmClass.ADGGA;

                case "CE": return LbmClass.CE;
                case "Cholesterol": return LbmClass.Cholesterol;
                case "CholesterolSulfate": return LbmClass.CholesterolSulfate;
                case "SHex": return LbmClass.SHex;
                case "SSulfate": return LbmClass.SSulfate;
                case "BAHex": return LbmClass.BAHex;
                case "BASulfate": return LbmClass.BASulfate;

                case "BRSE": return LbmClass.BRSE;
                case "CASE": return LbmClass.CASE;
                case "SISE": return LbmClass.SISE;
                case "STSE": return LbmClass.STSE;

                case "AHexCS": return LbmClass.AHexCS;
                case "AHexBRS": return LbmClass.AHexBRS;
                case "AHexCAS": return LbmClass.AHexCAS;
                case "AHexSIS": return LbmClass.AHexSIS;
                case "AHexSTS": return LbmClass.AHexSTS;

                case "DCAE": return LbmClass.DCAE;
                case "GDCAE": return LbmClass.GDCAE;
                case "GLCAE": return LbmClass.GLCAE;
                case "TDCAE": return LbmClass.TDCAE;
                case "TLCAE": return LbmClass.TLCAE;

                case "Vitamin": return LbmClass.Vitamin_E;
                case "VAE": return LbmClass.VAE;
                case "BileAcid": return LbmClass.BileAcid;
                case "CoQ": return LbmClass.CoQ;

                case "ACar": return LbmClass.CAR;
                case "FA": return LbmClass.FA;
                case "OxFA": return LbmClass.OxFA;
                case "FAHFA": return LbmClass.FAHFA;
                case "DMEDFAHFA": return LbmClass.DMEDFAHFA;
                case "DMEDFA": return LbmClass.DMEDFA;
                case "DMEDOxFA": return LbmClass.DMEDOxFA;

                case "NAE": return LbmClass.NAE;
                case "NAAG": return LbmClass.NAGly;
                case "NAAGS": return LbmClass.NAGlySer;
                case "NAAO": return LbmClass.NAOrn;

                case "Phytosphingosine": return LbmClass.PhytoSph;
                case "Sphinganine": return LbmClass.DHSph;
                case "Sphingosine": return LbmClass.Sph;


                case "Cer-ADS": return LbmClass.Cer_ADS;
                case "Cer-AS": return LbmClass.Cer_AS;
                case "Cer-BDS": return LbmClass.Cer_BDS;
                case "Cer-BS": return LbmClass.Cer_BS;
                case "Cer-NDS": return LbmClass.Cer_NDS;
                case "Cer-NS": return LbmClass.Cer_NS;
                case "Cer-NP": return LbmClass.Cer_NP;
                case "Cer-AP": return LbmClass.Cer_AP;
                case "Cer-EODS": return LbmClass.Cer_EODS;
                case "Cer-EOS": return LbmClass.Cer_EOS;
                case "Cer-OS": return LbmClass.Cer_OS;
                case "Cer-HS": return LbmClass.Cer_HS;
                case "Cer-HDS": return LbmClass.Cer_HDS;
                case "Cer-NDOS": return LbmClass.Cer_NDOS;

                case "HexCer-NS": return LbmClass.HexCer_NS;
                case "HexCer-NDS": return LbmClass.HexCer_NDS;
                case "HexCer-AP": return LbmClass.HexCer_AP;
                case "HexCer-HS": return LbmClass.HexCer_HS;
                case "HexCer-HDS": return LbmClass.HexCer_HDS;
                case "HexCer-EOS": return LbmClass.HexCer_EOS;
                case "HexHexCer-NS": return LbmClass.Hex2Cer;
                case "HexHexHexCer-NS": return LbmClass.Hex3Cer;
                case "HexHexCer": return LbmClass.Hex2Cer;
                case "HexHexHexCer": return LbmClass.Hex3Cer;
                case "Hex2Cer": return LbmClass.Hex2Cer;
                case "Hex3Cer": return LbmClass.Hex3Cer;
                case "PE-Cer": return LbmClass.PE_Cer;
                case "PI-Cer": return LbmClass.PI_Cer;

                case "Cer_ADS": return LbmClass.Cer_ADS;
                case "Cer_AS": return LbmClass.Cer_AS;
                case "Cer_BDS": return LbmClass.Cer_BDS;
                case "Cer_BS": return LbmClass.Cer_BS;
                case "Cer_NDS": return LbmClass.Cer_NDS;
                case "Cer_NS": return LbmClass.Cer_NS;
                case "Cer_NP": return LbmClass.Cer_NP;
                case "Cer_AP": return LbmClass.Cer_AP;
                case "Cer_EODS": return LbmClass.Cer_EODS;
                case "Cer_EOS": return LbmClass.Cer_EOS;
                case "Cer_OS": return LbmClass.Cer_OS;
                case "Cer_HS": return LbmClass.Cer_HS;
                case "Cer_HDS": return LbmClass.Cer_HDS;
                case "Cer_NDOS": return LbmClass.Cer_NDOS;

                case "HexCer_NS": return LbmClass.HexCer_NS;
                case "HexCer_NDS": return LbmClass.HexCer_NDS;
                case "HexCer_AP": return LbmClass.HexCer_AP;
                case "HexCer_EOS": return LbmClass.HexCer_EOS;
                case "HexCer_HS": return LbmClass.HexCer_HS;
                case "HexCer_HDS": return LbmClass.HexCer_HDS;
                case "HexHexCer_NS": return LbmClass.Hex2Cer;
                case "HexHexHexCer_NS": return LbmClass.Hex3Cer;
                case "PE_Cer": return LbmClass.PE_Cer;
                case "PI_Cer": return LbmClass.PI_Cer;

                case "CerP": return LbmClass.CerP;

                case "SM": return LbmClass.SM;
                case "SHexCer": return LbmClass.SHexCer;
                case "GM3": return LbmClass.GM3;
                case "SL": return LbmClass.SL;

                case "Ac2PIM1": return LbmClass.Ac2PIM1;
                case "Ac2PIM2": return LbmClass.Ac2PIM2;
                case "Ac3PIM2": return LbmClass.Ac3PIM2;
                case "Ac4PIM2": return LbmClass.Ac4PIM2;

                case "AcylCer-BDS": return LbmClass.Cer_EBDS;
                case "AcylCer_BDS": return LbmClass.Cer_EBDS;
                case "AcylHexCer": return LbmClass.AHexCer;
                case "AcylSM": return LbmClass.ASM;
                case "Cer_EBDS": return LbmClass.Cer_EBDS;
                case "Cer-EBDS": return LbmClass.Cer_EBDS;
                case "AHexCer": return LbmClass.AHexCer;
                case "ASM": return LbmClass.ASM;

                case "GPNAE": return LbmClass.GPNAE;
                case "MGMG": return LbmClass.MGMG;
                case "DGMG": return LbmClass.DGMG;

                case "GD1a": return LbmClass.GD1a;
                case "GD1b": return LbmClass.GD1b;
                case "GD2": return LbmClass.GD2;
                case "GD3": return LbmClass.GD3;
                case "GM1": return LbmClass.GM1;
                case "GT1b": return LbmClass.GT1b;
                case "GQ1b": return LbmClass.GQ1b;
                case "NGcGM3": return LbmClass.NGcGM3;

                case "ST": return LbmClass.ST;
                case "LPC_d5": return LbmClass.LPC_d5;
                case "LPE_d5": return LbmClass.LPE_d5;
                case "LPG_d5": return LbmClass.LPG_d5;
                case "LPI_d5": return LbmClass.LPI_d5;
                case "LPS_d5": return LbmClass.LPS_d5;

                case "PC_d5": return LbmClass.PC_d5;
                case "PE_d5": return LbmClass.PE_d5;
                case "PG_d5": return LbmClass.PG_d5;
                case "PI_d5": return LbmClass.PI_d5;
                case "PS_d5": return LbmClass.PS_d5;

                case "DG_d5": return LbmClass.DG_d5;
                case "TG_d5": return LbmClass.TG_d5;

                case "CE_d7": return LbmClass.CE_d7;
                case "Cer_NS_d7": return LbmClass.Cer_NS_d7;
                case "SM_d9": return LbmClass.SM_d9;

                case "bmPC": return LbmClass.bmPC;

                default: return LbmClass.Undefined;
            }
        }

        public static LbmClass ConvertMsdialClassDefinitionToLbmClassEnumVS2(string lipidclass)
        {
            switch (lipidclass)
            {
                case "MG": return LbmClass.MG;
                case "DG": return LbmClass.DG;
                case "TG": return LbmClass.TG;
                case "OxTG": return LbmClass.OxTG;
                case "TG_EST": return LbmClass.TG_EST;
                case "EtherDG": return LbmClass.EtherDG;
                case "EtherTG": return LbmClass.EtherTG;

                case "LPC": return LbmClass.LPC;
                case "LPA": return LbmClass.LPA;
                case "LPE": return LbmClass.LPE;
                case "LPG": return LbmClass.LPG;
                case "LPI": return LbmClass.LPI;
                case "LPS": return LbmClass.LPS;
                case "LDGTS": return LbmClass.LDGTS;
                case "LDGTA": return LbmClass.LDGTA;
                case "LDGCC": return LbmClass.LDGCC;

                case "EtherLPC": return LbmClass.EtherLPC;
                case "EtherLPE": return LbmClass.EtherLPE;
                case "EtherLPG": return LbmClass.EtherLPG;
                case "EtherLPI": return LbmClass.EtherLPI;
                case "EtherLPS": return LbmClass.EtherLPS;

                case "PC": return LbmClass.PC;
                case "PA": return LbmClass.PA;
                case "PE": return LbmClass.PE;
                case "PG": return LbmClass.PG;
                case "PI": return LbmClass.PI;
                case "PS": return LbmClass.PS;
                case "PT": return LbmClass.PT;
                case "BMP": return LbmClass.BMP;
                case "HBMP": return LbmClass.HBMP;
                case "CL": return LbmClass.CL;
                case "DLCL": return LbmClass.DLCL;
                case "MLCL": return LbmClass.MLCL;

                case "Ac2PIM1": return LbmClass.Ac2PIM1;
                case "Ac2PIM2": return LbmClass.Ac2PIM2;
                case "Ac3PIM2": return LbmClass.Ac3PIM2;
                case "Ac4PIM2": return LbmClass.Ac4PIM2;

                case "EtherPC": return LbmClass.EtherPC;
                case "EtherPE": return LbmClass.EtherPE;
                case "EtherPE_O": return LbmClass.EtherPE;
                case "EtherPE_P": return LbmClass.EtherPE;
                case "EtherPG": return LbmClass.EtherPG;
                case "EtherPI": return LbmClass.EtherPI;
                case "EtherPS": return LbmClass.EtherPS;
                case "EtherMGDG": return LbmClass.EtherMGDG;
                case "EtherDGDG": return LbmClass.EtherDGDG;
                case "EtherSMGDG": return LbmClass.EtherSMGDG;

                case "OxPC": return LbmClass.OxPC;
                case "OxPE": return LbmClass.OxPE;
                case "OxPG": return LbmClass.OxPG;
                case "OxPI": return LbmClass.OxPI;
                case "OxPS": return LbmClass.OxPS;

                case "EtherOxPC": return LbmClass.EtherOxPC;
                case "EtherOxPE": return LbmClass.EtherOxPE;

                case "PMeOH": return LbmClass.PMeOH;
                case "PEtOH": return LbmClass.PEtOH;
                case "PBtOH": return LbmClass.PBtOH;
                case "MMPE": return LbmClass.MMPE;
                case "DMPE": return LbmClass.DMPE;

                case "LNAPE": return LbmClass.LNAPE;
                case "LNAPS": return LbmClass.LNAPS;

                case "DGDG": return LbmClass.DGDG;
                case "MGDG": return LbmClass.MGDG;
                case "SMGDG": return LbmClass.SMGDG;
                case "SQDG": return LbmClass.SQDG;
                case "DGTS": return LbmClass.DGTS;
                case "DGTA": return LbmClass.DGTA;
                case "DGCC": return LbmClass.DGCC;
                case "DGGA": return LbmClass.DGGA;
                case "ADGGA": return LbmClass.ADGGA;

                case "CE": return LbmClass.CE;
                case "Cholesterol": return LbmClass.Cholesterol;
                case "CholesterolSulfate": return LbmClass.CholesterolSulfate;
                case "SHex": return LbmClass.SHex;
                case "SSulfate": return LbmClass.SSulfate;
                case "BAHex": return LbmClass.BAHex;
                case "BASulfate": return LbmClass.BASulfate;

                case "BRSE": return LbmClass.BRSE;
                case "CASE": return LbmClass.CASE;
                case "SISE": return LbmClass.SISE;
                case "STSE": return LbmClass.STSE;
                case "EGSE": return LbmClass.EGSE;
                case "DEGSE": return LbmClass.DEGSE;
                case "DSMSE": return LbmClass.DSMSE;

                case "AHexCS": return LbmClass.AHexCS;
                case "AHexBRS": return LbmClass.AHexBRS;
                case "AHexCAS": return LbmClass.AHexCAS;
                case "AHexSIS": return LbmClass.AHexSIS;
                case "AHexSTS": return LbmClass.AHexSTS;

                case "BRSLPHex": return LbmClass.BRSLPHex;
                case "BRSPHex": return LbmClass.BRSPHex;
                case "CASLPHex": return LbmClass.CASLPHex;
                case "CASPHex": return LbmClass.CASPHex;
                case "CSLPHex": return LbmClass.CSLPHex;
                case "CSPHex": return LbmClass.CSPHex;
                case "SISLPHex": return LbmClass.SISLPHex;
                case "SISPHex": return LbmClass.SISPHex;
                case "STSLPHex": return LbmClass.STSLPHex;
                case "STSPHex": return LbmClass.STSPHex;

                case "SPE": return LbmClass.SPE;
                case "SPEHex": return LbmClass.SPEHex;
                case "SPGHex": return LbmClass.SPGHex;

                case "DCAE": return LbmClass.DCAE;
                case "GDCAE": return LbmClass.GDCAE;
                case "GLCAE": return LbmClass.GLCAE;
                case "TDCAE": return LbmClass.TDCAE;
                case "TLCAE": return LbmClass.TLCAE;
                case "LCAE": return LbmClass.LCAE;
                case "KLCAE": return LbmClass.KLCAE;
                case "KDCAE": return LbmClass.KDCAE;

                case "Vitamin_E": return LbmClass.Vitamin_E;
                case "Vitamin E": return LbmClass.Vitamin_E;
                case "Vitamin_D": return LbmClass.Vitamin_D;
                case "Vitamin D": return LbmClass.Vitamin_D;
                case "VAE": return LbmClass.VAE;
                case "BileAcid": return LbmClass.BileAcid;
                case "CoQ": return LbmClass.CoQ;

                case "CAR": return LbmClass.CAR;
                case "FA": return LbmClass.FA;
                case "OxFA": return LbmClass.OxFA;
                case "FAHFA": return LbmClass.FAHFA;
                case "DMEDFAHFA": return LbmClass.DMEDFAHFA;
                case "DMEDFA": return LbmClass.DMEDFA;

                case "NAE": return LbmClass.NAE;
                case "NAGly": return LbmClass.NAGly;
                case "NAGlySer": return LbmClass.NAGlySer;
                case "NAOrn": return LbmClass.NAOrn;
                case "NAPhe": return LbmClass.NAPhe;
                case "NATau": return LbmClass.NATau;

                case "PhytoSph": return LbmClass.PhytoSph;
                case "DHSph": return LbmClass.DHSph;
                case "Sph": return LbmClass.Sph;


                case "Cer-ADS": return LbmClass.Cer_ADS;
                case "Cer-AS": return LbmClass.Cer_AS;
                case "Cer-BDS": return LbmClass.Cer_BDS;
                case "Cer-BS": return LbmClass.Cer_BS;
                case "Cer-NDS": return LbmClass.Cer_NDS;
                case "Cer-NS": return LbmClass.Cer_NS;
                case "Cer-NP": return LbmClass.Cer_NP;
                case "Cer-AP": return LbmClass.Cer_AP;
                case "Cer-EODS": return LbmClass.Cer_EODS;
                case "Cer-EOS": return LbmClass.Cer_EOS;
                case "Cer-OS": return LbmClass.Cer_OS;
                case "Cer-HS": return LbmClass.Cer_HS;
                case "Cer-HDS": return LbmClass.Cer_HDS;
                case "Cer-NDOS": return LbmClass.Cer_NDOS;

                case "HexCer-NS": return LbmClass.HexCer_NS;
                case "HexCer-NDS": return LbmClass.HexCer_NDS;
                case "HexCer-AP": return LbmClass.HexCer_AP;
                case "HexCer-HS": return LbmClass.HexCer_HS;
                case "HexCer-HDS": return LbmClass.HexCer_HDS;
                case "HexCer-EOS": return LbmClass.HexCer_EOS;
                case "Hex2Cer": return LbmClass.Hex2Cer;
                case "Hex3Cer": return LbmClass.Hex3Cer;
                case "PE-Cer": return LbmClass.PE_Cer;
                case "PI-Cer": return LbmClass.PI_Cer;
                case "PE-Cer+O": return LbmClass.PE_Cer;
                case "PI-Cer+O": return LbmClass.PI_Cer;

                case "Cer_ADS": return LbmClass.Cer_ADS;
                case "Cer_AS": return LbmClass.Cer_AS;
                case "Cer_BDS": return LbmClass.Cer_BDS;
                case "Cer_BS": return LbmClass.Cer_BS;
                case "Cer_NDS": return LbmClass.Cer_NDS;
                case "Cer_NS": return LbmClass.Cer_NS;
                case "Cer_NP": return LbmClass.Cer_NP;
                case "Cer_AP": return LbmClass.Cer_AP;
                case "Cer_EODS": return LbmClass.Cer_EODS;
                case "Cer_EOS": return LbmClass.Cer_EOS;
                case "Cer_OS": return LbmClass.Cer_OS;
                case "Cer_HS": return LbmClass.Cer_HS;
                case "Cer_HDS": return LbmClass.Cer_HDS;
                case "Cer_NDOS": return LbmClass.Cer_NDOS;

                case "HexCer_NS": return LbmClass.HexCer_NS;
                case "HexCer_NDS": return LbmClass.HexCer_NDS;
                case "HexCer_AP": return LbmClass.HexCer_AP;
                case "HexCer_EOS": return LbmClass.HexCer_EOS;
                case "HexCer_HS": return LbmClass.HexCer_HS;
                case "HexCer_HDS": return LbmClass.HexCer_HDS;
                case "PE_Cer": return LbmClass.PE_Cer;
                case "PI_Cer": return LbmClass.PI_Cer;
                case "PE_Cer+O": return LbmClass.PE_Cer;
                case "PI_Cer+O": return LbmClass.PI_Cer;
                case "MIPC": return LbmClass.MIPC;

                case "CerP": return LbmClass.CerP;

                case "SM": return LbmClass.SM;
                case "SHexCer": return LbmClass.SHexCer;
                case "SL": return LbmClass.SL;
                case "SM+O": return LbmClass.SM;
                case "SHexCer+O": return LbmClass.SHexCer;
                case "SL+O": return LbmClass.SL;
                case "GM3": return LbmClass.GM3;

                case "Cer_EBDS": return LbmClass.Cer_EBDS;
                case "Cer-EBDS": return LbmClass.Cer_EBDS;
                case "AHexCer": return LbmClass.AHexCer;
                case "ASM": return LbmClass.ASM;

                case "GPNAE": return LbmClass.GPNAE;
                case "MGMG": return LbmClass.MGMG;
                case "DGMG": return LbmClass.DGMG;

                case "GD1a": return LbmClass.GD1a;
                case "GD1b": return LbmClass.GD1b;
                case "GD2": return LbmClass.GD2;
                case "GD3": return LbmClass.GD3;
                case "GM1": return LbmClass.GM1;
                case "GT1b": return LbmClass.GT1b;
                case "GQ1b": return LbmClass.GQ1b;
                case "NGcGM3": return LbmClass.NGcGM3;

                case "ST": return LbmClass.ST;

                case "LPC_d5": return LbmClass.LPC_d5;
                case "LPE_d5": return LbmClass.LPE_d5;
                case "LPG_d5": return LbmClass.LPG_d5;
                case "LPI_d5": return LbmClass.LPI_d5;
                case "LPS_d5": return LbmClass.LPS_d5;

                case "PC_d5": return LbmClass.PC_d5;
                case "PE_d5": return LbmClass.PE_d5;
                case "PG_d5": return LbmClass.PG_d5;
                case "PI_d5": return LbmClass.PI_d5;
                case "PS_d5": return LbmClass.PS_d5;

                case "DG_d5": return LbmClass.DG_d5;
                case "TG_d5": return LbmClass.TG_d5;

                case "CE_d7": return LbmClass.CE_d7;
                case "Cer_NS_d7": return LbmClass.Cer_NS_d7;
                case "SM_d9": return LbmClass.SM_d9;

                case "bmPC": return LbmClass.bmPC;

                default: return LbmClass.Undefined;
            }
        }


        public static string ConvertMsdialClassDefinitionToSuperClass(string lipidclass)
        {
            switch (lipidclass)
            {
                case "MAG": return "Glycerolipid";
                case "DAG": return "Glycerolipid";
                case "TAG": return "Glycerolipid";
                case "EtherDAG": return "Ether linked glycerolipid";
                case "EtherTAG": return "Ether linked glycerolipid";

                case "LPC": return "Lyso phospholipid";
                case "LPA": return "Lyso phospholipid";
                case "LPE": return "Lyso phospholipid";
                case "LPG": return "Lyso phospholipid";
                case "LPI": return "Lyso phospholipid";
                case "LPS": return "Lyso phospholipid";
                case "LDGTS": return "Lyso algal lipid";
                case "LDGTA": return "Lyso algal lipid";
                case "LDGCC": return "Lyso algal lipid";

                case "PC": return "Phospholipid";
                case "PA": return "Phospholipid";
                case "PE": return "Phospholipid";
                case "PG": return "Phospholipid";
                case "PI": return "Phospholipid";
                case "PS": return "Phospholipid";
                case "PT": return "Phospholipid";
                case "BMP": return "Phospholipid";
                case "HBMP": return "Phospholipid";
                case "CL": return "Phospholipid";
                case "DLCL": return "Phospholipid";
                case "LCL": return "Phospholipid";

                case "EtherPC": return "Ether linked phospholipid";
                case "EtherPE": return "Ether linked phospholipid";
                case "EtherPS": return "Ether linked phospholipid";
                case "EtherPG": return "Ether linked phospholipid";
                case "EtherPI": return "Ether linked phospholipid";

                case "EtherLPC": return "Ether linked lyso phospholipid";
                case "EtherLPE": return "Ether linked lyso phospholipid";
                case "EtherLPS": return "Ether linked lyso phospholipid";
                case "EtherLPG": return "Ether linked lyso phospholipid";
                case "EtherLPI": return "Ether linked lyso phospholipid";

                case "OxPC": return "Oxidized phospholipid";
                case "OxPE": return "Oxidized phospholipid";
                case "OxPG": return "Oxidized phospholipid";
                case "OxPI": return "Oxidized phospholipid";
                case "OxPS": return "Oxidized phospholipid";

                case "EtherOxPC": return "Oxidized ether linked phospholipid";
                case "EtherOxPE": return "Oxidized ether linked phospholipid";

                case "PMeOH": return "Header modified phospholipid";
                case "PEtOH": return "Header modified phospholipid";
                case "PBtOH": return "Header modified phospholipid";

                case "LNAPE": return "N-acyl phospholipid";
                case "LNAPS": return "N-acyl phospholipid";

                case "NAE": return "N-acyl amide";
                case "NAAG": return "N-acyl amide";
                case "NAAGS": return "N-acyl amide";
                case "NAAO": return "N-acyl amide";

                case "DGDG": return "Plant lipid";
                case "MGDG": return "Plant lipid";
                case "SQDG": return "Plant lipid";
                case "DGTS": return "Algal lipid";
                case "DGTA": return "Algal lipid";
                case "DGCC": return "Algal lipid";
                case "GlcADG": return "Plant lipid";
                case "AcylGlcADG": return "Plant lipid";
                case "DGGA": return "Plant lipid";
                case "ADGGA": return "Plant lipid";
                case "EtherMGDG": return "Ether linked plant lipid";
                case "EtherDGDG": return "Ether linked plant lipid";

                case "CE": return "Cholesterol ester";
                case "Cholesterol": return "Cholesterol";
                case "CholesterolSulfate": return "Sterol sulfate";

                case "SHex": return "Sterol hexoside";
                case "SSulfate": return "Sterol sulfate";
                case "BAHex": return "Sterol hexoside";
                case "BASulfate": return "Sterol sulfate";

                case "ST": return "Sterol";

                case "BRSE": return "Steryl ester";
                case "CASE": return "Steryl ester";
                case "SISE": return "Steryl ester";
                case "STSE": return "Steryl ester";
                case "AHexCS": return "Steryl acyl hexoside";
                case "AHexBRS": return "Steryl acyl hexoside";
                case "AHexCAS": return "Steryl acyl hexoside";
                case "AHexSIS": return "Steryl acyl hexoside";
                case "AHexSTS": return "Steryl acyl hexoside";

                case "DCAE": return "Bile acid ester";
                case "GDCAE": return "Bile acid ester";
                case "GLCAE": return "Bile acid ester";
                case "TDCAE": return "Bile acid ester";
                case "TLCAE": return "Bile acid ester";

                case "CoQ": return "Coenzyme Q";
                case "Vitamin": return "Vitamin";
                case "VAE": return "Vitamin";
                case "BileAcid": return "Bile acid";

                case "ACar": return "Acyl carnitine";
                case "FA": return "Free fatty acid";
                case "OxFA": return "Free fatty acid";
                case "DMEDFA": return "Free fatty acid";
                case "DMEDOxFA": return "Free fatty acid";
                case "FAHFA": return "Fatty acid ester of hydroxyl fatty acid";
                case "DMEDFAHFA": return "Fatty acid ester of hydroxyl fatty acid";

                case "Phytosphingosine": return "Phytosphingosine";
                case "Sphinganine": return "Sphinganine";
                case "Sphingosine": return "Sphingosine";

                case "Cer_ADS": return "Ceramide";
                case "Cer_AS": return "Ceramide";
                case "Cer_BDS": return "Ceramide";
                case "Cer_BS": return "Ceramide";
                case "Cer_NDS": return "Ceramide";
                case "Cer_NS": return "Ceramide";
                case "Cer_NP": return "Ceramide";
                case "Cer_AP": return "Ceramide";
                case "Cer_OS": return "Ceramide";
                case "Cer_HS": return "Ceramide";
                case "Cer_HDS": return "Ceramide";
                case "Cer_NDOS": return "Ceramide";
                case "Cer_EODS": return "Acyl ceramide";
                case "Cer_EOS": return "Acyl ceramide";

                case "Cer-ADS": return "Ceramide";
                case "Cer-AS": return "Ceramide";
                case "Cer-BDS": return "Ceramide";
                case "Cer-BS": return "Ceramide";
                case "Cer-NDS": return "Ceramide";
                case "Cer-NS": return "Ceramide";
                case "Cer-NP": return "Ceramide";
                case "Cer-AP": return "Ceramide";
                case "Cer-OS": return "Ceramide";
                case "Cer-HS": return "Ceramide";
                case "Cer-HDS": return "Ceramide";
                case "Cer-NDOS": return "Ceramide";
                case "CerP": return "Ceramide phosphate";
                case "Cer-EODS": return "Acyl ceramide";
                case "Cer-EOS": return "Acyl ceramide";

                case "GlcCer_NS": return "Hexosyl ceramide";
                case "GlcCer_NDS": return "Hexosyl ceramide";
                case "GlcCer_AP": return "Hexosyl ceramide";

                case "HexCer-NS": return "Hexosyl ceramide";
                case "HexCer-NDS": return "Hexosyl ceramide";
                case "HexCer-AP": return "Hexosyl ceramide";
                case "HexCer-HS": return "Hexosyl ceramide";
                case "HexCer-HDS": return "Hexosyl ceramide";
                case "HexCer-EOS": return "Hexosyl ceramide";
                case "HexHexCer-NS": return "Hexosyl ceramide";
                case "HexHexHexCer-NS": return "Hexosyl ceramide";
                case "HexHexCer": return "Hexosyl ceramide";
                case "HexHexHexCer": return "Hexosyl ceramide";
                case "Hex2Cer": return "Hexosyl ceramide";
                case "Hex3Cer": return "Hexosyl ceramide";

                case "Cer_EBDS": return "Acyl ceramide";
                case "Cer-EBDS": return "Acyl ceramide";
                case "AHexCer": return "Acyl ceramide";
                case "ASM": return "Acyl sphingomyelin";

                case "PI-Cer": return "Ceramide";
                case "PE-Cer": return "Ceramide";

                case "SM": return "Sphingomyelin";
                case "SHexCer": return "Sulfatide";
                case "SL": return "Sulfonolipid";
                case "GM3": return "Ganglioside";
                case "GM3[NeuAc]": return "Ganglioside";

                case "GPNAE": return "Phospholipid";
                case "MGMG": return "Glycerolipid";
                case "DGMG": return "Glycerolipid";

                case "GD1a": return "Ganglioside";
                case "GD1b": return "Ganglioside";
                case "GD2": return "Ganglioside";
                case "GD3": return "Ganglioside";
                case "GM1": return "Ganglioside";
                case "GT1b": return "Ganglioside";
                case "GQ1b": return "Ganglioside";
                case "NGcGM3": return "Ganglioside";

                case "LPC_d5": return "Glycerophospholipids";
                case "LPE_d5": return "Glycerophospholipids";
                case "LPG_d5": return "Glycerophospholipids";
                case "LPI_d5": return "Glycerophospholipids";
                case "LPS_d5": return "Glycerophospholipids";

                case "PC_d5": return "Glycerophospholipids";
                case "PE_d5": return "Glycerophospholipids";
                case "PG_d5": return "Glycerophospholipids";
                case "PI_d5": return "Glycerophospholipids";
                case "PS_d5": return "Glycerophospholipids";

                case "DG_d5": return "Glycerolipids";
                case "TG_d5": return "Glycerolipids";

                case "CE_d7": return "SterolLipids";
                case "Cer_NS_d7": return "Sphingolipids";
                case "SM_d9": return "Sphingolipids";

                case "bmPC": return "Glycerophospholipids";

                default: return "Unassigned lipid";
            }
        }

        public static string ConvertMsdialClassDefinitionToSuperClassVS2(string lipidclass)
        {

            /*  
             *  FattyAcyls [FA]
             *  Glycerolipids [GL]
             *  Glycerophospholipids [GP]
             *  Sphingolipids [SP]
             *  SterolLipids [ST]
             *  PrenolLipids [PR]
             *  Saccharolipids [SL]
             *  Polyketides [PK]
             * 
             */

            switch (lipidclass)
            {

                case "NAE": return "FattyAcyls";
                case "NAGly": return "FattyAcyls";
                case "NAGlySer": return "FattyAcyls";
                case "NAOrn": return "FattyAcyls";
                case "NAPhe": return "FattyAcyls";
                case "NATau": return "FattyAcyls";

                case "CAR": return "FattyAcyls";
                case "FA": return "FattyAcyls";
                case "OxFA": return "FattyAcyls";
                case "FAHFA": return "FattyAcyls";
                case "DMEDFAHFA": return "FattyAcyls";
                case "DMEDFA": return "FattyAcyls";
                case "DMEDOxFA": return "FattyAcyls";

                case "MG": return "Glycerolipids";
                case "DG": return "Glycerolipids";
                case "TG": return "Glycerolipids";
                case "OxTG": return "Glycerolipids";
                case "TG_EST": return "Glycerolipids";
                case "EtherDG": return "Glycerolipids";
                case "EtherTG": return "Glycerolipids";
                case "LDGTS": return "Glycerolipids";
                case "LDGTA": return "Glycerolipids";
                case "LDGCC": return "Glycerolipids";

                case "DGDG": return "Glycerolipids";
                case "MGDG": return "Glycerolipids";
                case "SQDG": return "Glycerolipids";
                case "DGTS": return "Glycerolipids";
                case "DGTA": return "Glycerolipids";
                case "DGCC": return "Glycerolipids";
                case "DGGA": return "Glycerolipids";
                case "ADGGA": return "Glycerolipids";
                case "EtherMGDG": return "Glycerolipids";
                case "EtherDGDG": return "Glycerolipids";
                case "EtherSDGDG": return "Glycerolipids";

                case "LPC": return "Glycerophospholipids";
                case "LPA": return "Glycerophospholipids";
                case "LPE": return "Glycerophospholipids";
                case "LPG": return "Glycerophospholipids";
                case "LPI": return "Glycerophospholipids";
                case "LPS": return "Glycerophospholipids";

                case "PC": return "Glycerophospholipids";
                case "PA": return "Glycerophospholipids";
                case "PE": return "Glycerophospholipids";
                case "PG": return "Glycerophospholipids";
                case "PI": return "Glycerophospholipids";
                case "PS": return "Glycerophospholipids";
                case "PT": return "Glycerophospholipids";
                case "BMP": return "Glycerophospholipids";
                case "HBMP": return "Glycerophospholipids";
                case "CL": return "Glycerophospholipids";
                case "DLCL": return "Glycerophospholipids";
                case "MLCL": return "Glycerophospholipids";
                case "SMGDG": return "Glycerolipids";
                case "EtherSMGDG": return "Glycerolipids";

                case "EtherPC": return "Glycerophospholipids";
                case "EtherPE": return "Glycerophospholipids";
                case "EtherPE_O": return "Glycerophospholipids";
                case "EtherPE_P": return "Glycerophospholipids";
                case "EtherPS": return "Glycerophospholipids";
                case "EtherPG": return "Glycerophospholipids";
                case "EtherPI": return "Glycerophospholipids";

                case "EtherLPC": return "Glycerophospholipids";
                case "EtherLPE": return "Glycerophospholipids";
                case "EtherLPS": return "Glycerophospholipids";
                case "EtherLPG": return "Glycerophospholipids";
                case "EtherLPI": return "Glycerophospholipids";

                case "OxPC": return "Glycerophospholipids";
                case "OxPE": return "Glycerophospholipids";
                case "OxPG": return "Glycerophospholipids";
                case "OxPI": return "Glycerophospholipids";
                case "OxPS": return "Glycerophospholipids";

                case "EtherOxPC": return "Glycerophospholipids";
                case "EtherOxPE": return "Glycerophospholipids";

                case "PMeOH": return "Glycerophospholipids";
                case "PEtOH": return "Glycerophospholipids";
                case "PBtOH": return "Glycerophospholipids";
                case "MMPE": return "Glycerophospholipids";
                case "DMPE": return "Glycerophospholipids";

                case "LNAPE": return "Glycerophospholipids";
                case "LNAPS": return "Glycerophospholipids";

                case "Ac2PIM1": return "Glycerophospholipids";
                case "Ac2PIM2": return "Glycerophospholipids";
                case "Ac3PIM2": return "Glycerophospholipids";
                case "Ac4PIM2": return "Glycerophospholipids";

                case "BRSLPHex": return "SterolLipids";
                case "BRSPHex": return "SterolLipids";
                case "CASLPHex": return "SterolLipids";
                case "CASPHex": return "SterolLipids";
                case "CSLPHex": return "SterolLipids";
                case "CSPHex": return "SterolLipids";
                case "SISLPHex": return "SterolLipids";
                case "SISPHex": return "SterolLipids";
                case "STSLPHex": return "SterolLipids";
                case "STSPHex": return "SterolLipids";

                case "SPE": return "SterolLipids";
                case "SPEHex": return "SterolLipids";
                case "SPGHex": return "SterolLipids";

                case "CE": return "SterolLipids";
                case "Cholesterol": return "SterolLipids";
                case "CholesterolSulfate": return "SterolLipids";

                case "SHex": return "SterolLipids";
                case "SSulfate": return "SterolLipids";
                case "BAHex": return "SterolLipids";
                case "BASulfate": return "SterolLipids";

                case "BRSE": return "SterolLipids";
                case "CASE": return "SterolLipids";
                case "SISE": return "SterolLipids";
                case "STSE": return "SterolLipids";
                case "EGSE": return "SterolLipids";
                case "DEGSE": return "SterolLipids";
                case "DSMSE": return "SterolLipids";
                case "AHexCS": return "SterolLipids";
                case "AHexBRS": return "SterolLipids";
                case "AHexCAS": return "SterolLipids";
                case "AHexSIS": return "SterolLipids";
                case "AHexSTS": return "SterolLipids";

                case "ST": return "SterolLipids";

                case "DCAE": return "SterolLipids";
                case "GDCAE": return "SterolLipids";
                case "GLCAE": return "SterolLipids";
                case "TDCAE": return "SterolLipids";
                case "TLCAE": return "SterolLipids";
                case "LCAE": return "SterolLipids";
                case "KLCAE": return "SterolLipids";
                case "KDCAE": return "SterolLipids";
                case "Vitamin_D": return "SterolLipids";
                case "Vitamin D": return "SterolLipids";
                case "BileAcid": return "SterolLipids";

                case "CoQ": return "PrenolLipids";
                case "Vitamin_E": return "PrenolLipids";
                case "Vitamin E": return "PrenolLipids";
                case "VAE": return "PrenolLipids";

                case "PhytoSph": return "Sphingolipids";
                case "DHSph": return "Sphingolipids";
                case "Sph": return "Sphingolipids";

                case "Cer_ADS": return "Sphingolipids";
                case "Cer_AS": return "Sphingolipids";
                case "Cer_BDS": return "Sphingolipids";
                case "Cer_BS": return "Sphingolipids";
                case "Cer_NDS": return "Sphingolipids";
                case "Cer_NS": return "Sphingolipids";
                case "Cer_NP": return "Sphingolipids";
                case "Cer_AP": return "Sphingolipids";
                case "Cer_OS": return "Sphingolipids";
                case "Cer_HS": return "Sphingolipids";
                case "Cer_HDS": return "Sphingolipids";
                case "Cer_NDOS": return "Sphingolipids";
                case "Cer_EODS": return "Sphingolipids";
                case "Cer_EOS": return "Sphingolipids";

                case "Cer-ADS": return "Sphingolipids";
                case "Cer-AS": return "Sphingolipids";
                case "Cer-BDS": return "Sphingolipids";
                case "Cer-BS": return "Sphingolipids";
                case "Cer-NDS": return "Sphingolipids";
                case "Cer-NS": return "Sphingolipids";
                case "Cer-NP": return "Sphingolipids";
                case "Cer-AP": return "Sphingolipids";
                case "Cer-OS": return "Sphingolipids";
                case "Cer-HS": return "Sphingolipids";
                case "Cer-HDS": return "Sphingolipids";
                case "Cer-NDOS": return "Sphingolipids";
                case "CerP": return "Sphingolipids";
                case "Cer-EODS": return "Sphingolipids";
                case "Cer-EOS": return "Sphingolipids";

                case "HexCer-NS": return "Sphingolipids";
                case "HexCer-NDS": return "Sphingolipids";
                case "HexCer-AP": return "Sphingolipids";
                case "HexCer-HS": return "Sphingolipids";
                case "HexCer-HDS": return "Sphingolipids";
                case "HexCer-EOS": return "Sphingolipids";
                case "HexCer_NS": return "Sphingolipids";
                case "HexCer_NDS": return "Sphingolipids";
                case "HexCer_AP": return "Sphingolipids";
                case "HexCer_HS": return "Sphingolipids";
                case "HexCer_HDS": return "Sphingolipids";
                case "HexCer_EOS": return "Sphingolipids";
                case "Hex2Cer": return "Sphingolipids";
                case "Hex3Cer": return "Sphingolipids";
                case "Cer_EBDS": return "Sphingolipids";
                case "Cer-EBDS": return "Sphingolipids";
                case "AHexCer": return "Sphingolipids";
                case "ASM": return "Sphingolipids";

                case "PI-Cer": return "Sphingolipids";
                case "PE-Cer": return "Sphingolipids";
                case "PI_Cer": return "Sphingolipids";
                case "PE_Cer": return "Sphingolipids";
                case "PI-Cer+O": return "Sphingolipids";
                case "PE-Cer+O": return "Sphingolipids";
                case "PI_Cer+O": return "Sphingolipids";
                case "PE_Cer+O": return "Sphingolipids";
                case "MIPC": return "Sphingolipids";

                case "SM": return "Sphingolipids";
                case "SHexCer": return "Sphingolipids";
                case "SL": return "Sphingolipids";
                case "SM+O": return "Sphingolipids";
                case "SHexCer+O": return "Sphingolipids";
                case "SL+O": return "Sphingolipids";
                case "GM3": return "Sphingolipids";
                case "GM3[NeuAc]": return "Sphingolipids";

                case "GPNAE": return "Glycerophospholipids";
                case "MGMG": return "Glycerolipids";
                case "DGMG": return "Glycerolipids";

                case "GD1a": return "Sphingolipids";
                case "GD1b": return "Sphingolipids";
                case "GD2": return "Sphingolipids";
                case "GD3": return "Sphingolipids";
                case "GM1": return "Sphingolipids";
                case "GT1b": return "Sphingolipids";
                case "GQ1b": return "Sphingolipids";
                case "NGcGM3": return "Sphingolipids";

                case "LPC_d5": return "Glycerophospholipids";
                case "LPE_d5": return "Glycerophospholipids";
                case "LPG_d5": return "Glycerophospholipids";
                case "LPI_d5": return "Glycerophospholipids";
                case "LPS_d5": return "Glycerophospholipids";

                case "PC_d5": return "Glycerophospholipids";
                case "PE_d5": return "Glycerophospholipids";
                case "PG_d5": return "Glycerophospholipids";
                case "PI_d5": return "Glycerophospholipids";
                case "PS_d5": return "Glycerophospholipids";

                case "DG_d5": return "Glycerolipids";
                case "TG_d5": return "Glycerolipids";

                case "CE_d7": return "SterolLipids";
                case "Cer_NS_d7": return "Sphingolipids";
                case "SM_d9": return "Sphingolipids";

                case "bmPC": return "Glycerophospholipids";

                default: return "Unassigned lipid";
            }
        }

        public static void AsciiToSerializedObject(string input, string output)
        {
            var queries = LbmFileParcer.Read(input);
            MspMethods.SaveMspToFile(queries, output);
            //MessagePackHandler.SaveToFile<List<MspFormatCompoundInformationBean>>(queries, output);
        }

        public static List<MspFormatCompoundInformationBean> SerializedObjectToMspQeries(string input)
        {
            //var queries = MessagePackHandler.LoadFromFile<List<MspFormatCompoundInformationBean>>(input);
            var queries = MspMethods.LoadMspFromFile(input);
            return queries;
        }

    }
}
