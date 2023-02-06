using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.Annotation;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.Lipoquality
{
    public sealed class LipoqualityDatabaseManagerUtility
    {
        private LipoqualityDatabaseManagerUtility() { }

        public static LipoqualityAnnotation ConvertMsdialLipidnameToLipidAnnotation(MspFormatCompoundInformationBean query, string metaboliteName)
        {
            var lipidannotation = new LipoqualityAnnotation();

            switch (query.CompoundClass) {

                //Glycerolipid
                case "MAG":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "DAG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "TAG":
                    setTripleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Lyso phospholipid
                case "LPC":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LPE":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LPG":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LPI":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LPS":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LPA":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LDGTS":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "LDGTA":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Phospholipid
                case "PC":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PE":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PI":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PA":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "BMP":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "HBMP":
                    setTripleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "CL":
                    setQuadAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Ether linked phospholipid
                case "EtherPC":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "EtherPE":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Oxidized phospholipid
                case "OxPC":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "OxPE":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "OxPG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "OxPI":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "OxPS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;


                //Oxidized ether linked phospholipid
                case "EtherOxPC":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "EtherOxPE":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Oxidized ether linked phospholipid
                case "PMeOH":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PEtOH":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "PBtOH":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Plantlipid
                case "MGDG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "DGDG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "SQDG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "DGTS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "DGTA":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "GlcADG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "AcylGlcADG":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Others
                case "CE":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "ACar":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "FA":
                case "DMEDFA":
                case "OxFA":
                case "DMEDOxFA":
                    setSingleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "FAHFA":
                case "DMEDFAHFA":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Sphingomyelin
                case "SM":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                //Ceramide
                case "Cer_ADS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_AS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_BDS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_BS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_EODS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_EOS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_NDS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_NS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_NP":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "GlcCer_NS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "GlcCer_NDS":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "Cer_AP":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "GlcCer_AP":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;

                case "SHexCer":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                case "GM3":
                    setDoubleAcylChainsLipidAnnotation(lipidannotation, query, metaboliteName);
                    break;
                    
            }
            return lipidannotation;
        }

        //private static void setSingleAcylChainslipidAnnotation(LipidAnnotation lipidannotation, MspFormatCompoundInformationBean query)
        //{
        //    var name = query.Name;
        //    var nameArray = query.Name.Split(';').ToArray();

        //    var lipidinfo = nameArray[0].Trim();

        //    var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
        //    var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
        //    var totalChain = lipidinfo.Split(' ')[1]; if (query.CompoundClass == "CE") totalChain = lipidinfo.Split(' ')[0];
        //    var sn1AcylChain = lipidinfo.Split(' ')[1]; if (query.CompoundClass == "CE") sn1AcylChain = lipidinfo.Split(' ')[0];

        //    lipidannotation.Name = lipidinfo;
        //    lipidannotation.IonMode = query.IonMode;
        //    lipidannotation.LipidSuperClass = lipidSuperClass;
        //    lipidannotation.LipidClass = lipidclass;
        //    lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
        //    lipidannotation.Sn1AcylChain = sn1AcylChain;
        //    lipidannotation.Smiles = query.Smiles;
        //    lipidannotation.Inchikey = query.InchiKey;
        //    lipidannotation.Formula = query.Formula;
        //}

        private static void setSingleAcylChainsLipidAnnotation(LipoqualityAnnotation lipidannotation, 
            MspFormatCompoundInformationBean query, string metabolitename) {
            var name = metabolitename;
            var nameArray = metabolitename.Split(';').ToArray();

            var lipidinfo = nameArray[0].Trim();

            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
            var totalChain = lipidinfo.Split(' ')[1]; 
            var sn1AcylChain = lipidinfo.Split(' ')[1]; 

            lipidannotation.Name = lipidinfo;
            lipidannotation.IonMode = query.IonMode;
            lipidannotation.LipidSuperClass = lipidSuperClass;
            lipidannotation.LipidClass = lipidclass;
            lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
            lipidannotation.TotalChain = totalChain;
            lipidannotation.Sn1AcylChain = sn1AcylChain;
            lipidannotation.Smiles = query.Smiles;
            lipidannotation.Inchikey = query.InchiKey;
            lipidannotation.Formula = query.Formula;
        }

        private static void setDoubleAcylChainsLipidAnnotation(LipoqualityAnnotation lipidannotation,
            MspFormatCompoundInformationBean query, string metabolitename) {

            var nameArray = metabolitename.Split(';').ToArray();
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
            var totalLipidInfo = nameArray[0].Trim();
            var totalChain = totalLipidInfo.Split(' ')[1];

            if (nameArray.Length == 2) {
                var adductInfo = nameArray[1].Trim();
                lipidannotation.Name = totalLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;

            } else if (nameArray.Length == 3) {

                var detailLipidInfo = nameArray[1].Trim();
                var adductInfo = nameArray[2].Trim();

                var chainsInfo = getLipidChainsInformation(detailLipidInfo);
                var sn1AcylChain = chainsInfo[0];
                var sn2AcylChain = chainsInfo[1];

                lipidannotation.Name = detailLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Sn1AcylChain = sn1AcylChain;
                lipidannotation.Sn2AcylChain = sn2AcylChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;
            }
        }

        private static void setTripleAcylChainsLipidAnnotation(LipoqualityAnnotation lipidannotation, 
            MspFormatCompoundInformationBean query, 
            string metabolitename) {

            var nameArray = metabolitename.Split(';').ToArray();
            var totalLipidInfo = nameArray[0].Trim();
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
            var totalChain = totalLipidInfo.Split(' ')[1];

            if (nameArray.Length == 2) {
                var adductInfo = nameArray[1].Trim();

                lipidannotation.Name = totalLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;
            }
            else if (nameArray.Length == 3) {

                var detailLipidInfo = nameArray[1].Trim();
                var adductInfo = nameArray[2].Trim();

                var chainsInfo = getLipidChainsInformation(detailLipidInfo);
                var sn1AcylChain = chainsInfo[0];
                var sn2AcylChain = chainsInfo[1];
                var sn3AcylChain = chainsInfo[2];

                lipidannotation.Name = detailLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Sn1AcylChain = sn1AcylChain;
                lipidannotation.Sn2AcylChain = sn2AcylChain;
                lipidannotation.Sn3AcylChain = sn3AcylChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;
            }
        }

        private static void setQuadAcylChainsLipidAnnotation(LipoqualityAnnotation lipidannotation,
            MspFormatCompoundInformationBean query,
            string metabolitename) {

            var nameArray = metabolitename.Split(';').ToArray();
            var totalLipidInfo = nameArray[0].Trim();
            var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
            var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
            var totalChain = totalLipidInfo.Split(' ')[1];

            if (nameArray.Length == 2) {
                var adductInfo = nameArray[1].Trim();

                lipidannotation.Name = totalLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;
            }
            else if (nameArray.Length == 3) {

                var detailLipidInfo = nameArray[1].Trim();
                var adductInfo = nameArray[2].Trim();

                var chainsInfo = getLipidChainsInformation(detailLipidInfo);
                var sn1AcylChain = chainsInfo[0];
                var sn2AcylChain = chainsInfo[1];
                var sn3AcylChain = chainsInfo[2];
                var sn4AcylChain = chainsInfo[3];

                lipidannotation.Name = detailLipidInfo;
                lipidannotation.IonMode = query.IonMode;
                lipidannotation.LipidSuperClass = lipidSuperClass;
                lipidannotation.LipidClass = lipidclass;
                lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
                lipidannotation.TotalChain = totalChain;
                lipidannotation.Sn1AcylChain = sn1AcylChain;
                lipidannotation.Sn2AcylChain = sn2AcylChain;
                lipidannotation.Sn3AcylChain = sn3AcylChain;
                lipidannotation.Sn4AcylChain = sn4AcylChain;
                lipidannotation.Smiles = query.Smiles;
                lipidannotation.Inchikey = query.InchiKey;
                lipidannotation.Formula = query.Formula;
            }
        }


        //private static void setDoubleAcylChainslipidAnnotation(LipidAnnotation lipidannotation, 
        //    MspFormatCompoundInformationBean query)
        //{
        //    var nameArray = query.Name.Split(';').ToArray();

        //    var totalLipidInfo = nameArray[0].Trim();
        //    var detailLipidInfo = nameArray[1].Trim();
        //    var adductInfo = nameArray[2].Trim();

        //    var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
        //    var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
        //    var totalChain = totalLipidInfo.Split(' ')[1];

        //    var chainsInfo = getLipidChainsInformation(detailLipidInfo);
        //    var sn1AcylChain = chainsInfo[0];
        //    var sn2AcylChain = chainsInfo[1];
        //    if (sn1AcylChain.Contains("P-")) sn1AcylChain = sn1AcylChain.Replace("P-", "e").Replace(":0", ":1");

        //    lipidannotation.Name = detailLipidInfo;
        //    lipidannotation.IonMode = query.IonMode;
        //    lipidannotation.LipidSuperClass = lipidSuperClass;
        //    lipidannotation.LipidClass = lipidclass;
        //    lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
        //    lipidannotation.Sn1AcylChain = sn1AcylChain;
        //    lipidannotation.Sn2AcylChain = sn2AcylChain;
        //    lipidannotation.Smiles = query.Smiles;
        //    lipidannotation.Inchikey = query.InchiKey;
        //    lipidannotation.Formula = query.Formula;
        //}

        //private static void setTripleAcylChainslipidAnnotation(LipidAnnotation lipidannotation, MspFormatCompoundInformationBean query)
        //{
        //    var nameArray = query.Name.Split(';').ToArray();

        //    var totalLipidInfo = nameArray[0].Trim();
        //    var detailLipidInfo = nameArray[1].Trim();
        //    var adductInfo = nameArray[2].Trim();

        //    var lipidSuperClass = ConvertMsdialClassDefinitionToSuperClass(query.CompoundClass);
        //    var lipidclass = ConvertMsdialClassDefinitionToTraditionalClassDefinition(query.CompoundClass);
        //    var totalChain = totalLipidInfo.Split(' ')[1];

        //    var chainsInfo = getLipidChainsInformation(detailLipidInfo);
        //    var sn1AcylChain = chainsInfo[0];
        //    var sn2AcylChain = chainsInfo[1];
        //    var sn3AcylChain = chainsInfo[2];

        //    lipidannotation.Name = detailLipidInfo;
        //    lipidannotation.IonMode = query.IonMode;
        //    lipidannotation.LipidSuperClass = lipidSuperClass;
        //    lipidannotation.LipidClass = lipidclass;
        //    lipidannotation.Adduct = AdductIonParcer.GetAdductIonBean(query.AdductIonBean.AdductIonName);
        //    lipidannotation.Sn1AcylChain = sn1AcylChain;
        //    lipidannotation.Sn2AcylChain = sn2AcylChain;
        //    lipidannotation.Sn3AcylChain = sn3AcylChain;
        //    lipidannotation.Smiles = query.Smiles;
        //    lipidannotation.Inchikey = query.InchiKey;
        //    lipidannotation.Formula = query.Formula;
        //}

        private static List<string> getLipidChainsInformation(string detailLipidInfo)
        {
            var chains = new List<string>();
            string[] acylArray = null;

            if (detailLipidInfo.Contains("(") && !detailLipidInfo.Contains("Cyc")) {

                if (detailLipidInfo.Contains("/")) {
                    acylArray = detailLipidInfo.Split('(')[1].Split(')')[0].Split('/');
                }
                else {
                    acylArray = detailLipidInfo.Split('(')[1].Split(')')[0].Split('-');
                }
            }
            else {
                if (detailLipidInfo.Contains("/")) {
                    acylArray = detailLipidInfo.Split(' ')[1].Split('/');
                }
                else {
                    acylArray = detailLipidInfo.Split(' ')[1].Split('-');
                }
            }

            for (int i = 0; i < acylArray.Length; i++) {
                if (i == 0 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 1 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 2 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
                if (i == 3 && acylArray[i] != string.Empty) chains.Add(acylArray[i]);
            }
            return chains;
        }

        public static string ConvertMsdialClassDefinitionToTraditionalClassDefinition(string lipidclass)
        {
            switch (lipidclass) {
                case "MAG": return "MAG";
                case "DAG": return "DAG";
                case "TAG": return "TAG";

                case "LPC": return "LPC";
                case "LPA": return "LPA";
                case "LPE": return "LPE";
                case "LPG": return "LPG";
                case "LPI": return "LPI";
                case "LPS": return "LPS";
                case "LDGTS": return "LDGTS";
                case "LDGTA": return "LDGTA";

                case "PC": return "PC";
                case "PA": return "PA";
                case "PE": return "PE";
                case "PG": return "PG";
                case "PI": return "PI";
                case "PS": return "PS";
                case "BMP": return "BMP";
                case "HBMP": return "HBMP";
                case "CL": return "CL";

                case "EtherPC": return "EtherPC";
                case "EtherPE": return "EtherPE";

                case "OxPC": return "OxPC";
                case "OxPE": return "OxPE";
                case "OxPG": return "OxPG";
                case "OxPI": return "OxPI";
                case "OxPS": return "OxPS";

                case "EtherOxPC": return "EtherOxPC";
                case "EtherOxPE": return "EtherOxPE";

                case "PMeOH": return "PMeOH";
                case "PEtOH": return "PEtOH";
                case "PBtOH": return "PBtOH";

                case "DGDG": return "DGDG";
                case "MGDG": return "MGDG";
                case "SQDG": return "SQDG";
                case "DGTS": return "DGTS";
                case "DGTA": return "DGTA";
                case "GlcADG": return "GlcADG";
                case "AcylGlcADG": return "AcylGlcADG";

                case "CE": return "CE";
                case "ACar": return "ACar";
                case "FA": return "FA";
                case "OxFA": return "OxFA";
                case "FAHFA": return "FAHFA";
                case "DMEDFAHFA": return "DMEDFAHFA";
                case "DMEDFA": return "DMEDFA";
                case "DMEDOxFA": return "DMEDOxFA";

                case "Cer_ADS": return "Cer-ADS";
                case "Cer_AS": return "Cer-AS";
                case "Cer_BDS": return "Cer-BDS";
                case "Cer_BS": return "Cer-BS";
                case "Cer_NDS": return "Cer-NDS";
                case "Cer_NS": return "Cer-NS";
                case "Cer_NP": return "Cer-NP";
                case "Cer_AP": return "Cer-AP";
                case "Cer_EODS": return "Cer-EODS";
                case "Cer_EOS": return "Cer-EOS";

                case "GlcCer_NS": return "HexCer-NS";
                case "GlcCer_NDS": return "HexCer-NDS";
                case "GlcCer_AP": return "HexCer-AP";

                case "SM": return "SM";

                case "SHexCer": return "SHexCer";
                case "GM3": return "GM3";
                case "GM3[NeuAc]": return "GM3";

                default: return "Unassigned lipid";
            }
        }

        public static string ConvertMsdialClassDefinitionToSuperClass(string lipidclass)
        {
            switch (lipidclass) {
                case "MAG": return "Glycerolipid";
                case "DAG": return "Glycerolipid";
                case "TAG": return "Glycerolipid";

                case "LPC": return "Lyso phospholipid";
                case "LPA": return "Lyso phospholipid";
                case "LPE": return "Lyso phospholipid";
                case "LPG": return "Lyso phospholipid";
                case "LPI": return "Lyso phospholipid";
                case "LPS": return "Lyso phospholipid";
                case "LDGTS": return "Lyso algal lipid";
                case "LDGTA": return "Lyso algal lipid";

                case "PC": return "Phospholipid";
                case "PA": return "Phospholipid";
                case "PE": return "Phospholipid";
                case "PG": return "Phospholipid";
                case "PI": return "Phospholipid";
                case "PS": return "Phospholipid";
                case "BMP": return "Phospholipid";
                case "HBMP": return "Phospholipid";
                case "CL": return "Phospholipid";

                case "EtherPC": return "Ether linked phospholipid";
                case "EtherPE": return "Ether linked phospholipid";

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

                case "DGDG": return "Plant lipid";
                case "MGDG": return "Plant lipid";
                case "SQDG": return "Plant lipid";
                case "DGTS": return "Algal lipid";
                case "DGTA": return "Algal lipid";
                case "GlcADG": return "Plant lipid";
                case "AcylGlcADG": return "Plant lipid";

                case "CE": return "Cholesterol ester";
                case "ACar": return "Acyl carnitine";
                case "FA": return "Free fatty acid";
                case "OxFA": return "Free fatty acid";
                case "FAHFA": return "Fatty acid ester of hydroxyl fatty acid";
                case "DMEDFAHFA": return "Fatty acid ester of hydroxyl fatty acid";
                case "DMEDFA": return "Free fatty acid";
                case "DMEDOxFA": return "Free fatty acid";

                case "Cer_ADS": return "Ceramide";
                case "Cer_AS": return "Ceramide";
                case "Cer_BDS": return "Ceramide";
                case "Cer_BS": return "Ceramide";
                case "Cer_NDS": return "Ceramide";
                case "Cer_NS": return "Ceramide";
                case "Cer_NP": return "Ceramide";
                case "Cer_AP": return "Ceramide";

                case "Cer_EODS": return "Acyl ceramide";
                case "Cer_EOS": return "Acyl ceramide";

                case "GlcCer_NS": return "Hexosyl ceramide";
                case "GlcCer_NDS": return "Hexosyl ceramide";
                case "GlcCer_AP": return "Hexosyl ceramide";

                case "SM": return "Sphingomyelin";
                case "SHexCer": return "Sulfatide";
                case "GM3": return "Ganglioside";
                case "GM3[NeuAc]": return "Ganglioside";

                default: return "Unassigned lipid";
            }
        }
    }
}
