using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MspGenerator
{
    public class CeramideMspGenerator
    {

        public static void twoChainsCeramideGenerator(List<string> sphingoChains, List<string> nAcylChains, string lipidClass, string output)
        {
            int sphingoChainCount = sphingoChains.Count;
            int nAcylChainCount = nAcylChains.Count;

            var wholeChainList = new List<string>();

            for (int i = 0; i < sphingoChainCount; i++)
            {
                for (int j = 0; j < nAcylChainCount; j++)
                {
                        var chainList = new List<string> { sphingoChains[i], nAcylChains[j] };
                        wholeChainList.Add(string.Join("\t", chainList));
                }
            }
            wholeChainList = wholeChainList.Distinct().ToList();

            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();
                var shortNameList2 = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var sphString = chainArray[0].Split(':');
                        var sphCarbon = int.Parse(sphString[0]);
                        var sphDouble = int.Parse(sphString[1]);
                        //var sphMass = (sphChain * 12 + (2 * sphChain - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + MassDictionary.NitrogenMass);//sph-mono-Oxy

                        var acylString = chainArray[1].Split(':');
                        var acylCarbon = int.Parse(acylString[0]);
                        var acylDouble = int.Parse(acylString[1]);
                        var acylOx = 0;
                        //var acylMass = (acylChain * 12 + (2 * acylChain - 2 * acylDouble) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//acyl

                        var totalChain = sphCarbon + acylCarbon;
                        var totalBond = sphDouble + acylDouble;

                        var name = "";
                        var shortName = "";
                        //var shortNameList = new List<string>();

                        var sphSmiles = "";
                        var acylSmiles = "";

                        if (lipidClass.Contains("DS")&& sphDouble >0)
                        {
                            continue;
                        }
                        if(sphDouble == 0)
                        {
                            if(lipidClass == "Cer_AS" || lipidClass == "Cer_BS"|| lipidClass == "Cer_NS"|| lipidClass == "Cer_HS" 
                                || lipidClass == "Cer_EOS" || lipidClass == "HexCer_HS" || lipidClass == "HexCer_NS")
                            {
                                continue;
                            }
                        }

                        if (lipidClass.EndsWith("P") && lipidClass !="CerP")
                        {
                            if (AcylChainDic.sphingoBasePDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBasePDictionary[chainArray[0]])[3];
                            //sphMass = sphMass + 2 * MassDictionary.OxygenMass;
                        }
                        else if (lipidClass.EndsWith("S"))
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }

                        if (lipidClass.Contains('A'))
                        {
                            if (AcylChainDic.AcylChainAlphaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainAlphaOxDictionary[chainArray[1]])[3];
                            acylOx = acylOx + 1;
                        }
                        else if (lipidClass.Contains('B'))
                        {
                            if (AcylChainDic.AcylChainBetaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[chainArray[1]])[3];
                            acylOx = acylOx + 1;
                        }
                        else if (lipidClass.Contains('N'))
                        {
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                        }
                        else if (lipidClass.Contains("_H"))
                        {
                            if (AcylChainDic.AcylChainAlphaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainAlphaOxDictionary[chainArray[1]])[3]; // temporary use "A" acyl chain
                            acylOx = acylOx + 1;
                        }

                        //else if (lipidClass == "HexCer_OS")
                        //{
                        //    if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                        //    acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                        //    acylSmiles = "O" + acylSmiles;
                        //    acylOx = acylOx + 1;
                        //}
                        else if (lipidClass == "CerP" || lipidClass == "Hex2Cer" || lipidClass == "Hex3Cer" || lipidClass == "GM3") //NS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "SM") //NS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "SM+O") //NP
                        {
                            if (AcylChainDic.sphingoBasePDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBasePDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + 2 * MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "SL") //NS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "SL+O") //AS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.AcylChainAlphaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainAlphaOxDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                            acylOx = acylOx + 1;
                        }
                        else if (lipidClass == "SHexCer") //NS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "SHexCer+O") //AS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.AcylChainAlphaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainAlphaOxDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                            acylOx = acylOx + 1;
                        }
                        else if (lipidClass == "PE_Cer_d") //NS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                        }
                        else if (lipidClass == "PE_Cer_d+O") //BS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.AcylChainBetaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                            acylOx = acylOx + 1;
                        }
                        else if (lipidClass == "PI_Cer_d+O") //BS
                        {
                            if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                            sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                            if (AcylChainDic.AcylChainBetaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                            acylSmiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[chainArray[1]])[3];
                            //sphMass = sphMass + MassDictionary.OxygenMass;
                            acylOx = acylOx + 1;
                        }

                        if (sphSmiles == "" || acylSmiles == "") 
                        {
                            Console.WriteLine("Error. Please check settings...");
                            Console.ReadKey();

                        }

                        var rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" ;

                        var meta = Common.getMetaProperty(rawSmiles);
                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;

                        switch (lipidClass)
                        {
                            //normal
                            case "Cer_AS":
                                CermideFragmentation.CerAsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";(2OH)";
                                shortName = "Cer " + totalChain +":"+ totalBond + ";3O";
                                break;
                            case "Cer_ADS":
                                CermideFragmentation.CerAdsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";(2OH)";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;
                            case "Cer_AP":
                                CermideFragmentation.CerApFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";3O/" + chainArray[1] + ";(2OH)";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";4O";
                                break;
                            case "Cer_NS":
                                CermideFragmentation.CerNsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] ;
                                shortName = "Cer " + totalChain + ":" + totalBond + ";2O";
                                break;
                            case "Cer_NDS":
                                CermideFragmentation.CerNdsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] ;
                                shortName = "Cer " + totalChain + ":" + totalBond + ";2O";
                                break;
                            case "Cer_NP":
                                CermideFragmentation.CerNpFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";3O/" + chainArray[1] ;
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;
                            case "Cer_BS":
                                CermideFragmentation.CerBsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";(3OH)";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;
                            case "Cer_BDS":
                                CermideFragmentation.CerBdsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";(3OH)";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;

                            case "Cer_HS":
                                CermideFragmentation.CerHFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;

                            case "Cer_HDS":
                                CermideFragmentation.CerHFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "Cer " + totalChain + ":" + totalBond + ";3O";
                                break;

                            case "HexCer_AP":
                                CermideFragmentation.HexCerApFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "HexCer " + chainArray[0] + ";3O/" + chainArray[1] + ";(2OH)";
                                shortName = "HexCer " + totalChain + ":" + totalBond + ";4O";
                                break;
                            case "HexCer_NS":
                                CermideFragmentation.HexCerNFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "HexCer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "HexCer " + totalChain + ":" + totalBond + ";2O";
                                break;
                            case "HexCer_NDS":
                                CermideFragmentation.HexCerNFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                name = "HexCer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "HexCer " + totalChain + ":" + totalBond + ";2O";
                                break;
                            case "Hex2Cer":
                                name = "Hex2Cer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "Hex2Cer " + totalChain + ":" + totalBond + ";2O";
                                if(adduct.IonMode =="Negative")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.Hex2CerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "Hex3Cer":
                                name = "Hex3Cer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "Hex3Cer " + totalChain + ":" + totalBond + ";2O";
                                if (adduct.IonMode == "Negative")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.Hex3CerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "HexCer_HS":
                                name = "HexCer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "HexCer " + totalChain + ":" + totalBond + ";3O";
                                CermideFragmentation.HexCerHFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "HexCer_HDS":
                                name = "HexCer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "HexCer " + totalChain + ":" + totalBond + ";3O";

                                CermideFragmentation.HexCerHFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;

                            case "CerP":
                                name = "CerP " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "CerP " + totalChain + ":" + totalBond + ";2O";
                                CermideFragmentation.CerPFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;

                            case "GM3":
                                name = "GM3 " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "GM3 " + totalChain + ":" + totalBond + ";2O";
                                if (adduct.IonMode == "Negative")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.GM3Fragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            //conbination
                            case "SM":
                                name = "SM " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "SM " + totalChain + ":" + totalBond + ";2O";
                                exportLipidClassName = "SM";
                                if (adduct.AdductIonName == "[M+Na]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.SmFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "SM+O":
                                name = "SM " + chainArray[0] + ";3O/" + chainArray[1];
                                shortName = "SM " + totalChain + ":" + totalBond + ";3O";
                                exportLipidClassName = "SM";
                                if (shortNameList.Contains(shortName)) { continue; }
                                shortNameList.Add(shortName);
                                name = shortName;
                                CermideFragmentation.SmAddOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "SL":
                                name = "SL " + chainArray[0] + ";O/" + chainArray[1];
                                shortName = "SL " + totalChain + ":" + totalBond + ";O";
                                exportLipidClassName = "SL";
                                CermideFragmentation.sulfonolipidFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "SL+O":
                                name = "SL " + chainArray[0] + ";O/" + chainArray[1] + ";O";
                                shortName = "SL " + totalChain + ":" + totalBond + ";O";
                                exportLipidClassName = "SL";
                                CermideFragmentation.sladdOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "SHexCer":
                                name = "SHexCer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "SHexCer " + totalChain + ":" + totalBond + ";2O";
                                exportLipidClassName = "SHexCer";
                                if (adduct.IonMode == "Negative")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.SHexCerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "SHexCer+O":
                                name = "SHexCer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "SHexCer " + totalChain + ":" + totalBond + ";3O";
                                exportLipidClassName = "SHexCer";
                                if (adduct.IonMode == "Negative")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.SHexCerAddOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;

                            case "PE_Cer_d":
                                name = "PE-Cer " + chainArray[0] + ";2O/" + chainArray[1];
                                shortName = "PE-Cer " + totalChain + ":" + totalBond + ";2O";
                                exportLipidClassName = "PE_Cer";
                                CermideFragmentation.peCerdFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "PE_Cer_d+O":
                                name = "PE-Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "PE-Cer " + totalChain + ":" + totalBond + ";3O";
                                exportLipidClassName = "PE_Cer";
                                CermideFragmentation.peCerdAddOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;
                            case "PI_Cer_d+O":
                                name = "PI-Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                shortName = "PI-Cer " + totalChain + ":" + totalBond + ";3O";
                                exportLipidClassName = "PI_Cer";
                                if (adduct.IonMode == "Positive")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                CermideFragmentation.piCerdAddOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon,sphDouble, acylCarbon,acylDouble, acylOx);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                break;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);

                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                    }
                }
                var smilesOutputFile = output + "\\" + lipidClass + "_InChIKey-smiles.txt";
                smileslist = smileslist.Distinct().ToList();
                using (var sw = new StreamWriter(smilesOutputFile, false, Encoding.ASCII))
                {
                    sw.WriteLine("InChIKey\tSMILES");
                    foreach (var smiles in smileslist)
                        sw.WriteLine(smiles);
                }
            }
        }

        public static void threeChainsCeramideGenerator(List<string> sphingoChains, List<string> nAcylChains, List<string> extraAcylChains, string lipidClass, string output)
        {
            int sphingoChainCount = sphingoChains.Count;
            int nAcylChainCount = nAcylChains.Count;
            int extraAcylChainsCount = extraAcylChains.Count;

            var wholeChainList = new List<string>();

            for (int i = 0; i < sphingoChainCount; i++)
            {
                for (int j = 0; j < nAcylChainCount; j++)
                {
                    for (int k = 0; k < extraAcylChainsCount; k++)
                    {
                        var chainList = new List<string> { sphingoChains[i], nAcylChains[j] , extraAcylChains[k]};
                        wholeChainList.Add(string.Join("\t", chainList));
                    }
                }
            }
            wholeChainList = wholeChainList.Distinct().ToList();

            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();
                var shortNameList2 = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var sphString = chainArray[0].Split(':');
                        var sphCarbon = int.Parse(sphString[0]);
                        var sphDouble = int.Parse(sphString[1]);
                        //var sphMass = (sphChain * 12 + (2 * sphChain - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + MassDictionary.NitrogenMass);//sph-mono-Oxy

                        var acylString = chainArray[1].Split(':');
                        var acylCarbon = int.Parse(acylString[0]);
                        var acylDouble = int.Parse(acylString[1]);
                        var acylOx = 0;
                        //var acylMass = (acylChain * 12 + (2 * acylChain - 2 * acylDouble) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//acyl

                        var extraAcylString = chainArray[2].Split(':');
                        var extraAcylCarbon = int.Parse(extraAcylString[0]);
                        var extraAcylDouble = int.Parse(extraAcylString[1]);
                        var extraAcylOx = 0;
                        //var extraAcylMass = (extraAcylChain * 12 + (2 * extraAcylChain - 2 * extraAcylDouble) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//acyl

                        var totalChain = sphCarbon + acylCarbon + extraAcylCarbon;
                        var totalBond = sphDouble + acylDouble + extraAcylDouble;

                        var subChain = sphCarbon + acylCarbon;
                        var subBond = sphDouble + acylDouble;

                        var name = "";
                        var shortName = "";
                        var subName = "";
                        var subName2 = "";

                        var sphSmiles = "";
                        var acylSmiles = ""; 
                        var extraAcylSmiles = "";

                        var rawSmiles = "";
                        //var smiles = "";
                        //var InChIKey = "";
                        //var formula = "";
                        //var exactMass = 0.0;
                        var meta = new MetaProperty();

                        if (lipidClass.Contains("DS") && sphDouble > 0)
                        {
                            continue;
                        }
                        else if (sphDouble == 0)
                        {
                            if(lipidClass.Contains("OS"))
                            {
                            continue;
                            }
                        }

                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;

                        switch (lipidClass)
                        {
                            //normal
                            case "ASM": // NS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                                var sphSmiles2 = sphSmiles.Replace("C(O)", "C(O%10)");
                                rawSmiles = headerSmiles + sphSmiles2 + "%20" + "." + acylSmiles + "%30" + "."+extraAcylSmiles+"%10";

                                //sphMass = sphMass + MassDictionary.OxygenMass;

                                meta = Common.getMetaProperty(rawSmiles);
                                name = "SM " + chainArray[0] + ";2O/" + chainArray[1] + "(FA " + chainArray[2]+")";
                                subName = "SM "  + subChain + ":" + subBond + ";2O" + "(FA " + chainArray[2] + ")";
                                shortName = "SM " + totalChain + ":" + (totalBond + 1) + ";3O";
                                // ASM cannot determine ceramide chain
                                if (shortNameList.Contains(subName)) { continue; }
                                shortNameList.Add(subName);
                                name = subName;
                                CermideFragmentation.asmFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);


                                break;
                            case "Cer_EODS": // EODS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                                acylSmiles = "O%10"+ acylSmiles;
                                acylOx = acylOx + 1;

                                rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" + "." + extraAcylSmiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O(FA " + chainArray[2] + ")";
                                subName = "Cer " + subChain + ":" + subBond + ";3O" + "(FA " + chainArray[2] + ")";
                                shortName = "Cer " + totalChain + ":" + (totalBond + 1)+ ";4O";
                                CermideFragmentation.cerEodsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);

                                break;
                            case "Cer_EOS": // EOS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                                acylSmiles = "O%10" + acylSmiles;
                                acylOx = acylOx + 1;

                                rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" + "." + extraAcylSmiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";O(FA " + chainArray[2] + ")";
                                subName = "Cer " + subChain + ":" + subBond + ";3O" + "(FA " + chainArray[2] + ")";
                                subName2 = "Cer " + chainArray[0] + ";2O/" + (acylCarbon + extraAcylCarbon) + ":" + (acylDouble + extraAcylDouble + 1) + ";2O";
                                shortName = "Cer " + totalChain + ":" + (totalBond + 1) + ";4O";
                                if (adduct.IonMode == "Positive")
                                {
                                    if (shortNameList.Contains(subName2)) { continue; }
                                    shortNameList.Add(subName2);

                                    name = subName2;
                                }
                                CermideFragmentation.cerEosFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);
                                break;
                            case "HexCer_EOS": // EOS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                acylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                                acylSmiles = "O%10" + acylSmiles;
                                acylOx = acylOx + 1;
                                rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" + "." + extraAcylSmiles + "%10";

                                meta = Common.getMetaProperty(rawSmiles);
                                name = "HexCer " + chainArray[0] + ";2O/" + chainArray[1] + ";O(FA " + chainArray[2] + ")";
                                subName = "HexCer " + subChain + ":" + subBond + ";3O" + "(FA " + chainArray[2] + ")";
                                subName2 = "HexCer " + chainArray[0] + ";2O/" + (acylCarbon + extraAcylCarbon) + ":" + (acylDouble + extraAcylDouble + 1) + ";2O";
                                shortName = "HexCer " + totalChain + ":" + (totalBond + 1) + ";4O";
                                if (adduct.IonMode == "Positive")
                                {
                                    if (shortNameList.Contains(subName2)) { continue; }
                                    shortNameList.Add(subName2);
                                    name = subName2;
                                }
                                if (adduct.IonMode == "Negative")
                                {
                                    if (shortNameList2.Contains(subName)) { continue; }
                                    shortNameList2.Add(subName);
                                    name = subName;
                                }
                                CermideFragmentation.hexCerEosFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);
                                break;

                            case "Cer_EBDS": // BDS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                if (AcylChainDic.AcylChainBetaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                                acylSmiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                                acylOx = acylOx + 1;

                                acylSmiles = acylSmiles.Replace("C(O)", "C(O%10)");
                                rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" + "." + extraAcylSmiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);

                                name = "Cer " + chainArray[0] + ";2O/" + chainArray[1] + ";(3OH)(FA " + chainArray[2] + ")";
                                subName = "Cer " + subChain + ":" + subBond + ";3O" + "(FA " + chainArray[2] + ")";
                                shortName = "Cer " + totalChain + ":" + (totalBond + 1) + ";4O";
                                CermideFragmentation.cerEbdsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);

                                break;
                            case "AHexCer": //AS
                                if (AcylChainDic.sphingoBaseSDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[chainArray[0]])[3];
                                if (AcylChainDic.AcylChainAlphaOxDictionary.ContainsKey(chainArray[1]) == false) { continue; }
                                acylSmiles = new List<string>(AcylChainDic.AcylChainAlphaOxDictionary[chainArray[1]])[3];
                                extraAcylSmiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];

                                acylOx = acylOx + 1;

                                rawSmiles = headerSmiles + sphSmiles + "%20" + "." + acylSmiles + "%30" + "." + extraAcylSmiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);

                                name = "AHexCer (O-" + chainArray[2] + ")" + chainArray[0] + ";2O/" + chainArray[1] + ";O";
                                subName = "Cer " + subChain + ":" + subBond + ";3O" + "(FA " + chainArray[2] + ")";
                                shortName = "Cer " + totalChain + ":" + (totalBond + 1) + ";4O";
                                CermideFragmentation.acylHexCerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble, acylCarbon, acylDouble, acylOx, extraAcylCarbon, extraAcylDouble, extraAcylOx);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                break;

                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);

                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                    }
                }
                var smilesOutputFile = output + "\\" + lipidClass + "_InChIKey-smiles.txt";
                smileslist = smileslist.Distinct().ToList();
                using (var sw = new StreamWriter(smilesOutputFile, false, Encoding.ASCII))
                {
                    sw.WriteLine("InChIKey\tSMILES");
                    foreach (var smiles in smileslist)
                        sw.WriteLine(smiles);
                }
            }
        }

        public static void singleChainCeramideGenerator(List<string> sphingoChains, string lipidClass, string output)
        {
            int sphingoChainCount = sphingoChains.Count;

            var wholeChainList = new List<string>();

            for (int i = 0; i < sphingoChainCount; i++)
            {
                    var chainList = new List<string> { sphingoChains[i] };
                    wholeChainList.Add(string.Join("\t", chainList));
            }
            wholeChainList = wholeChainList.Distinct().ToList();

            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var sphString = chainArray[0];
                        var sphCarbon = int.Parse(chainArray[0].Split(':')[0]);
                        var sphDouble = int.Parse(chainArray[0].Split(':')[1]);
                        //var sphMass = (sphChain * 12 + (2 * sphChain - 2 * sphDouble + 3) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass + MassDictionary.NitrogenMass);//sph-mono-Oxy

                        if (lipidClass=="DHSph" && sphDouble > 0)
                        {
                            continue;
                        }
                        else if (lipidClass == "Sph" && sphDouble == 0)
                        {
                            continue;
                        }

                        var name = "";
                        var sphSmiles = "";
                        var rawSmiles ="";
                        var meta = new MetaProperty();

                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;

                        switch (lipidClass)
                        {
                            case "Sph":
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[sphString])[3];
                                //sphMass = sphMass + MassDictionary.OxygenMass;
                                rawSmiles = headerSmiles + sphSmiles + "%20";
                                meta = Common.getMetaProperty(rawSmiles);
                                CermideFragmentation.sphingosineFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble);
                                name = "SPB " + sphString + ";2O";
                                break;

                            case "DHSph":
                                sphSmiles = new List<string>(AcylChainDic.sphingoBaseSDictionary[sphString])[3];
                                //sphMass = sphMass + MassDictionary.OxygenMass;
                                rawSmiles = headerSmiles + sphSmiles + "%20";
                                meta = Common.getMetaProperty(rawSmiles);
                                CermideFragmentation.sphinganineFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble);
                                name = "SPB " + sphString + ";2O";
                                break;

                            case "PhytoSph":
                                sphSmiles = new List<string>(AcylChainDic.sphingoBasePDictionary[sphString])[3];
                                //sphMass = sphMass + MassDictionary.OxygenMass * 2;
                                rawSmiles = headerSmiles + sphSmiles + "%20";
                                meta = Common.getMetaProperty(rawSmiles);
                                CermideFragmentation.phytosphingosineFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, sphCarbon, sphDouble);
                                name = "SPB " + sphString + ";3O";
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                break;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);

                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                    }
                }
                var smilesOutputFile = output + "\\" + lipidClass + "_InChIKey-smiles.txt";
                smileslist = smileslist.Distinct().ToList();
                using (var sw = new StreamWriter(smilesOutputFile, false, Encoding.ASCII))
                {
                    sw.WriteLine("InChIKey\tSMILES");
                    foreach (var smiles in smileslist)
                        sw.WriteLine(smiles);
                }
            }
        }

    }
}

