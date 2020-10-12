using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;
using System.Collections;

namespace CompMs.MspGenerator
{
    public class GlyceroLipidsMspGenerator
    {

        public static void twoEquallyAcylCainsGlyceroLipidGenerator(List<string> chains, string lipidClass, string output)
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            for (int i = 0; i < chainCount; i++)
            {
                for (int j = 0; j < chainCount; j++)
                {
                    var chainList = new List<string> { chains[i], chains[j] };
                    chainList.Sort();
                    wholeChainList.Add(string.Join("\t", chainList));
                }
            }
            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var chain1String = chainArray[0];
                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var chain2String = chainArray[1];
                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);

                        if (chain1Double > chain2Double)
                        {
                            (chain1String, chain2String) = (chain2String, chain1String);
                            (chain1Carbon, chain2Carbon) = (chain2Carbon, chain1Carbon);
                            (chain1Double, chain2Double) = (chain2Double, chain1Double);
                        }

                        //var chain1AcylMass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain1 Acyl
                        //var chain2AcylMass = chain2Carbon * 12 + (2 * chain2Carbon - 2 * chain2Double - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain2 Acyl

                        var totalChain = chain1Carbon + chain2Carbon;
                        var totalBond = chain1Double + chain2Double;

                        var name = "";
                        var shortName = "";
                        //var shortNameList = new List<string>();

                        var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                        var headerSmiles = smilesHeaderDict[lipidClass];
                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                        var rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11";

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = new MetaProperty();

                        meta = Common.getMetaProperty(rawSmiles);
                        name = lipidClass + " " + chain1String + "_" + chain2String;
                        shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        switch (lipidClass)
                        {
                            //normal
                            case "PC": // 
                                if (adductIon == "[M+Na]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                GlycerolipidFragmentation.pcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PE": // 
                                if (adductIon == "[M+Na]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                GlycerolipidFragmentation.peFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PG": // 
                                GlycerolipidFragmentation.pgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PI": // 
                                if (adductIon == "[M+Na]+" || adductIon == "[M+NH4]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                GlycerolipidFragmentation.piFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PS": // 
                                if (adductIon == "[M+Na]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                GlycerolipidFragmentation.psFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PA": // 
                                GlycerolipidFragmentation.paFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;

                            case "PEtOH": // 
                                GlycerolipidFragmentation.petohFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "PMeOH": // 
                                GlycerolipidFragmentation.pmeohFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;

                            case "DG": // 
                                if (adductIon == "[M+Na]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                GlycerolipidFragmentation.dgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;

                            case "BMP": // 
                                GlycerolipidFragmentation.bmpFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "MGDG": // 
                                GlycerolipidFragmentation.mgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DGDG": // 
                                GlycerolipidFragmentation.dgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "SQDG": // 
                                GlycerolipidFragmentation.sqdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DGTS": // 
                                GlycerolipidFragmentation.dgtsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DGGA": // 
                                GlycerolipidFragmentation.dggaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DLCL": // 
                                GlycerolipidFragmentation.dlclFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "SMGDG": // 
                                GlycerolipidFragmentation.smgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DGCC": // 
                                GlycerolipidFragmentation.dgccFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;

                            case "MMPE": // 
                                GlycerolipidFragmentation.mmpeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "DMPE": // 
                                GlycerolipidFragmentation.dmpeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                break;


                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void twoIndependentChainsLipidGenerator(List<string> Chain1, List<string> chain2, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < Chain1.Count; i++)
            {
                for (int j = 0; j < chain2.Count; j++)
                {
                    if (lipidClass.Contains("Ox"))
                    {
                        for (int k = 1; k < 5; k++)
                        {
                            var chain2addO = chain2[j] + ":" + k;
                            var chainList = new List<string> { Chain1[i], chain2addO };
                            wholeChainList.Add(string.Join("\t", chainList));
                        }
                    }
                    else
                    {
                        var chainList = new List<string> { Chain1[i], chain2[j] };
                        wholeChainList.Add(string.Join("\t", chainList));
                    }
                }
            }
            //wholeChainList = wholeChainList.Distinct().ToList();

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

                        var chain1String = chainArray[0];
                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var chain2raw = chainArray[1];
                        var chain2String = chainArray[1].Split(':')[0] + ":" + chainArray[1].Split(':')[1];
                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);
                        var chain2Ox = 0;


                        var totalChain = chain1Carbon + chain2Carbon;
                        var totalBond = chain1Double + chain2Double;

                        //var chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain1 Acyl
                        //var chain2Mass = chain2Carbon * 12 + (2 * chain2Carbon - 2 * chain2Double - 2- 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain2 Acyl

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];

                        if (lipidClass.Contains("Ether"))
                        {
                            //chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass; // chain mass

                            if (lipidClass == "EtherPE" && AcylChainDic.etherChainPlasmenylDictionary.ContainsKey(chain1String))
                            {
                                chain1Smiles = new List<string>(AcylChainDic.etherChainPlasmenylDictionary[chain1String])[3];
                            }
                            else
                            {
                                chain1Smiles = new List<string>(AcylChainDic.etherChainDictionary[chain1String])[3];
                            }
                        }

                        if (lipidClass.Contains("Ox"))
                        {

                            chain2Ox = int.Parse(chainArray[1].Split(':')[2]);
                            //chain2Mass = chain2Mass + (chain2Ox * MassDictionary.OxygenMass); // chain mass
                            if (AcylChainDic.OxFaChainDictionary.ContainsKey(chain2raw) == false) { continue; }

                            chain2Smiles = new List<string>(AcylChainDic.OxFaChainDictionary[chain2raw])[3];
                        }

                        var name = "";
                        var shortName = "";
                        //var shortNameList = new List<string>();

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = new MetaProperty();
                        var rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11";
                        meta = Common.getMetaProperty(rawSmiles);

                        name = "";
                        shortName = "";

                        switch (lipidClass)
                        {
                            //normal
                            case "EtherPC":
                                GlycerolipidFragmentation.etherPcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PC O-" + chain1String + "_" + chain2String;
                                shortName = "PC O-" + totalChain + ":" + totalBond;
                                if (adductIon == "[M+H]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                break;
                            case "EtherPE":
                                GlycerolipidFragmentation.etherPePFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PE O-" + chain1String + "_" + chain2String;
                                shortName = "PE O-" + totalChain + ":" + totalBond;
                                if (adductIon == "[M+H]+" && chain1Double > 0)
                                {
                                    name = "PE P-" + chain1Carbon + ":" + (chain1Double - 1) + "_" + chain2String;
                                    shortName = "PE O-" + totalChain + ":" + (totalBond - 1);
                                }

                                break;
                            case "EtherPE_O":
                                GlycerolipidFragmentation.etherPeOFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PE O-" + chain1String + "_" + chain2String;
                                shortName = "PE O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherPG":
                                GlycerolipidFragmentation.etherPgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PG O-" + chain1String + "_" + chain2String;
                                shortName = "PG O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherPI":
                                GlycerolipidFragmentation.etherPiFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PI O-" + chain1String + "_" + chain2String;
                                shortName = "PI O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherPS":
                                GlycerolipidFragmentation.etherPsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "PS O-" + chain1String + "_" + chain2String;
                                shortName = "PS O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherDG":
                                GlycerolipidFragmentation.etherDgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "DAG O-" + chain1String + "_" + chain2String;
                                shortName = "DAG O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherDGDG":
                                GlycerolipidFragmentation.etherDgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "DGDG O-" + chain1String + "_" + chain2String;
                                shortName = "DGDG O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherMGDG":
                                GlycerolipidFragmentation.etherMgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "MGDG O-" + chain1String + "_" + chain2String;
                                shortName = "MGDG O-" + totalChain + ":" + totalBond;
                                break;
                            case "EtherSMGDG":
                                GlycerolipidFragmentation.etherSmgdgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "SMGDG O-" + chain1String + "_" + chain2String;
                                shortName = "SMGDG O-" + totalChain + ":" + totalBond;
                                break;
                            case "OxPC":
                                GlycerolipidFragmentation.oxPcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PC " + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PC " + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "OxPE":
                                GlycerolipidFragmentation.oxPeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PE " + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PE " + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "OxPG":
                                GlycerolipidFragmentation.oxPgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PG " + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PG " + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "OxPI":
                                GlycerolipidFragmentation.oxPiFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PI " + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PI " + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "OxPS":
                                GlycerolipidFragmentation.oxPsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PS " + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PS " + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "EtherOxPC":
                                GlycerolipidFragmentation.etherOxPcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PC O-" + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PC O-" + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "EtherOxPE":
                                GlycerolipidFragmentation.etherOxPeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain2Ox);
                                name = "PE O-" + chain1String + "_" + chain2String + ";" + chain2Ox + "O";
                                shortName = "PE O-" + totalChain + ":" + totalBond + ";" + chain2Ox + "O";
                                break;
                            case "LNAPE":
                                GlycerolipidFragmentation.lnapeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "LNAPE " + chain1String + "/N-" + chain2String;
                                shortName = "LNAPE " + totalChain + ":" + totalBond;
                                break;
                            case "LNAPS":
                                GlycerolipidFragmentation.lnapsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                name = "LNAPS " + chain1String + "/N-" + chain2String;
                                shortName = "LNAPS " + totalChain + ":" + totalBond;
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void singleChainGlyceroLipidGenerator(List<string> chains, string lipidClass, string output)
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            for (int i = 0; i < chainCount; i++)
            {
                var chainList = new List<string> { chains[i] };
                wholeChainList.Add(string.Join("\t", chainList));
            }
            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var chain1String = chainArray[0];
                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);
                        //var chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double - 2) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain1 acyl

                        var totalChain = chain1Carbon;
                        var totalBond = chain1Double;

                        var name = "";
                        //var shortNameList = new List<string>();

                        var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                        var headerSmiles = smilesHeaderDict[lipidClass];
                        var chain1Smiles = "";
                        if (lipidClass.Contains("Ether"))
                        {
                            //chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass; // chain mass
                            if (lipidClass == "EtherLPE_P")
                            {
                                if (AcylChainDic.etherChainPlasmenylDictionary.ContainsKey(chain1String) == false)
                                {
                                    continue;
                                }
                                chain1Smiles = new List<string>(AcylChainDic.etherChainPlasmenylDictionary[chain1String])[3];
                            }
                            else
                            {
                                chain1Smiles = new List<string>(AcylChainDic.etherChainDictionary[chain1String])[3];
                            }
                        }
                        else
                        {
                            chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        }
                        var rawSmiles = headerSmiles + chain1Smiles + "%10";

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);

                        switch (lipidClass)
                        {
                            //normal
                            case "LPC": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, lipidClass);
                                break;
                            case "LPCSN1": // 
                                var lipidClass2 = "LPC";
                                name = lipidClass2 + " " + chain1String + "-SN1";
                                GlycerolipidFragmentation.lpcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, lipidClass);
                                break;

                            case "LPE": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "LPG": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "LPI": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpiFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "LPS": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "LPA": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.lpaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;

                            case "EtherLPC": // 
                                name = "LPC O-" + chain1String;
                                GlycerolipidFragmentation.etherLpcFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "EtherLPE": // 
                                name = "LPE O-" + chain1String;
                                GlycerolipidFragmentation.etherLpeFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "EtherLPE_P": // 
                                name = "LPE P-" + chain1Carbon + ":" + (chain1Double - 1);
                                GlycerolipidFragmentation.etherLpePFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "EtherLPG": // 
                                name = "LPG O-" + chain1String;
                                GlycerolipidFragmentation.etherLpgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;

                            case "MG": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.mgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;

                            case "LDGTS": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.ldgtsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;
                            case "LDGCC": // 
                                name = lipidClass + " " + chain1String;
                                GlycerolipidFragmentation.ldgccFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double);
                                break;


                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void threeEquallyCainsGlyceroLipidGenerator(List<string> chains, string lipidClass, string output) //TG
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            for (int i = 0; i < chainCount; i++)
            {
                for (int j = 0; j < chainCount; j++)
                {
                    for (int k = 0; k < chainCount; k++)
                    {
                        {
                            var chainList = new List<string> { chains[i], chains[j], chains[k] };
                            chainList.Sort();
                            wholeChainList.Add(string.Join("\t", chainList));
                        }
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
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');
                        var chain1String = chainArray[0];
                        var chain2String = chainArray[1];
                        var chain3String = chainArray[2];

                        var chain1 = new FattyAcidChain { ChainString = chain1String, ChainLength = int.Parse(chain1String.Split(':')[0]), BondCount = int.Parse(chain1String.Split(':')[1]) };
                        var chain2 = new FattyAcidChain { ChainString = chain2String, ChainLength = int.Parse(chain2String.Split(':')[0]), BondCount = int.Parse(chain2String.Split(':')[1]) };
                        var chain3 = new FattyAcidChain { ChainString = chain3String, ChainLength = int.Parse(chain3String.Split(':')[0]), BondCount = int.Parse(chain3String.Split(':')[1]) };
                        var threeFas = new List<FattyAcidChain>() { chain1, chain2, chain3 };

                        threeFas = threeFas.OrderBy(n => n.BondCount).ThenBy(n => n.ChainLength).ToList();

                        chain1String = threeFas[0].ChainString;
                        var chain1Carbon = threeFas[0].ChainLength;
                        var chain1Double = threeFas[0].BondCount;

                        chain2String = threeFas[1].ChainString;
                        var chain2Carbon = threeFas[1].ChainLength;
                        var chain2Double = threeFas[1].BondCount;

                        chain3String = threeFas[2].ChainString;
                        var chain3Carbon = threeFas[2].ChainLength;
                        var chain3Double = threeFas[2].BondCount;

                        //var chain1Mass = (chain1Chain * 12 + (2 * chain1Chain - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain1 acyl
                        //var chain2Mass = (chain2Chain * 12 + (2 * chain2Chain - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain2 acyl
                        //var chain3Mass = (chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl

                        var totalChain = chain1Carbon + chain2Carbon + chain3Carbon;

                        if (totalChain > 88)
                        {
                            continue;
                        }

                        var totalBond = chain1Double + chain2Double + chain3Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                        var chain3Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain3String])[3];
                        var rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11" + "." + chain3Smiles + "%12";

                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = new MetaProperty();

                        switch (lipidClass)
                        {
                            //normal
                            case "TG": // 
                                meta = Common.getMetaProperty(rawSmiles);
                                name = lipidClass + " " + chain1String + "_" + chain2String + "_" + chain3String;
                                GlycerolipidFragmentation.tgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void twoAndOneSetCainsGlyceroLipidGenerator(List<string> chains1, List<string> chains2, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < chains1.Count; i++)
            {
                for (int j = 0; j < chains1.Count; j++)
                {
                    for (int k = 0; k < chains2.Count; k++)
                    {
                        if (lipidClass.Contains("Ox"))
                        {
                            for (int l = 1; l < 5; l++)
                            {
                                var chainList = new List<string> { chains1[i], chains1[j] };
                                chainList.Sort();
                                var chain2addO = chains2[k] + ":" + l;
                                chainList.Add(chain2addO);
                                wholeChainList.Add(string.Join("\t", chainList));
                            }
                        }
                        else
                        {
                            var chainList = new List<string> { chains1[i], chains1[j] };
                            chainList.Sort();

                            chainList.Add(chains2[k]);
                            wholeChainList.Add(string.Join("\t", chainList));
                        }
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
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var chain1String = chainArray[0];
                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var chain2String = chainArray[1];
                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);

                        if (chain1Double > chain2Double)
                        {
                            (chain1String, chain2String) = (chain2String, chain1String);
                            (chain1Carbon, chain2Carbon) = (chain2Carbon, chain1Carbon);
                            (chain1Double, chain2Double) = (chain2Double, chain1Double);
                        }

                        //var chain1Mass = (chain1Chain * 12 + (2 * chain1Chain - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain1 acyl
                        //var chain2Mass = (chain2Chain * 12 + (2 * chain2Chain - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain2 acyl

                        var chain3raw = chainArray[2];
                        var chain3String = chainArray[2].Split(':')[0] + ":" + chainArray[2].Split(':')[1];
                        var chain3Carbon = int.Parse(chain3String.Split(':')[0]);
                        var chain3Double = int.Parse(chain3String.Split(':')[1]);
                        //var chain3Mass = (chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl

                        var chain3Ox = 0;

                        var totalChain = chain1Carbon + chain2Carbon + chain3Carbon;
                        var totalBond = chain1Double + chain2Double + chain3Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                        var chain3Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain3String])[3];

                        if (lipidClass.Contains("Ox"))
                        {
                            chain3Ox = int.Parse(chainArray[2].Split(':')[2]);
                            //chain3Mass = chain3Mass + (chain3Ox * MassDictionary.OxygenMass); // chain mass
                            if (AcylChainDic.OxFaChainDictionary.ContainsKey(chain3raw) == false) { continue; }

                            chain3Smiles = new List<string>(AcylChainDic.OxFaChainDictionary[chain3raw])[3];
                        }
                        if (lipidClass.Contains("Ether"))
                        {
                            //chain3Mass = chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass; // chain mass
                            chain3Smiles = new List<string>(AcylChainDic.etherChainDictionary[chain3String])[3];
                        }


                        var rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11" + "." + chain3Smiles + "%12";

                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);

                        switch (lipidClass)
                        {
                            //normal
                            case "ADGGA": // 
                                GlycerolipidFragmentation.adggaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                if (adduct.IonMode == "Positive")
                                {
                                    name = lipidClass + " (O-" + chain3String + ")" + chain1String + "_" + chain2String;
                                }
                                else
                                {
                                    name = lipidClass + " " + chain3String + "_" + chain1String + "_" + chain2String;
                                }
                                break;
                            case "MLCL": // 
                                GlycerolipidFragmentation.mlclFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                name = lipidClass + " " + chain3String + "_" + chain1String + "_" + chain2String;
                                break;
                            case "HBMP": // 
                                GlycerolipidFragmentation.hbmpFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                name = lipidClass + " " + chain3String + "_" + chain1String + "_" + chain2String;
                                break;
                            case "OxTG":
                                GlycerolipidFragmentation.oxTgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double, chain3Ox);
                                name = "TG" + " " + chain1String + "_" + chain2String + "_" + chain3String + ";" + chain3Ox + "O";
                                break;
                            case "EtherTG":
                                GlycerolipidFragmentation.etherTgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                name = "TG" + " O-" + chain3String + "_" + chain1String + "_" + chain2String;

                                break;



                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void cardiolipinGenerator(List<string> chains, string lipidClass, string output) //CL
        {
            var halfChainList = new List<string>();

            for (int i = 0; i < chains.Count; i++)
            {
                for (int j = 0; j < chains.Count; j++)
                {
                    var chain1 = new FattyAcidChain { ChainString = chains[i], ChainLength = int.Parse(chains[i].Split(':')[0]), BondCount = int.Parse(chains[i].Split(':')[1]) };
                    var chain2 = new FattyAcidChain { ChainString = chains[j], ChainLength = int.Parse(chains[j].Split(':')[0]), BondCount = int.Parse(chains[j].Split(':')[1]) };
                    var Fas = new List<FattyAcidChain>() { chain1, chain2 };
                    Fas = Fas.OrderBy(n => n.BondCount).ThenBy(n => n.ChainLength).ToList();
                    var chainList = new List<string>() { Fas[0].ChainString, Fas[1].ChainString };
                    halfChainList.Add(string.Join("\t", chainList));
                }
            }
            halfChainList = halfChainList.Distinct().ToList();


            var wholeChainList = new List<string>();
            for (int i = 0; i < halfChainList.Count; i++)
            {
                for (int j = 0; j < halfChainList.Count; j++)
                {
                    var chainList = new List<string> { halfChainList[i], halfChainList[j] };
                    chainList.Sort();
                    wholeChainList.Add(string.Join("\t", chainList));
                }
            }

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];
            var shortNameList01 = new List<string>();

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
                        var chain1String = chainArray[0];
                        var chain2String = chainArray[1];
                        var chain3String = chainArray[2];
                        var chain4String = chainArray[3];

                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);

                        var chain3Carbon = int.Parse(chain3String.Split(':')[0]);
                        var chain3Double = int.Parse(chain3String.Split(':')[1]);

                        var chain4Carbon = int.Parse(chain4String.Split(':')[0]);
                        var chain4Double = int.Parse(chain4String.Split(':')[1]);

                        //var chain1Mass = (chain1Chain * 12 + (2 * chain1Chain - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain1 acyl
                        //var chain2Mass = (chain2Chain * 12 + (2 * chain2Chain - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain2 acyl
                        //var chain3Mass = (chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl
                        //var chain4Mass = (chain4Chain * 12 + (2 * chain4Chain - 2 * chain4Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl

                        var totalChain = chain1Carbon + chain2Carbon + chain3Carbon + chain4Carbon;
                        var totalBond = chain1Double + chain2Double + chain3Double + chain4Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                        var chain3Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain3String])[3];
                        var chain4Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain4String])[3];
                        var rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11" + "." + chain3Smiles + "%12" + "." + chain4Smiles + "%13";

                        // fragment
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);
                        var fragmentList = new List<string>();

                        switch (lipidClass)
                        {
                            //normal
                            case "CL": // 
                                name = lipidClass + " " + chain1String + "_" + chain2String + "_" + chain3String + "_" + chain4String;

                                if (adductIon == "[M+NH4]+")
                                {
                                    var chain12Chain = chain1Carbon + chain2Carbon;
                                    var chain34Chain = chain3Carbon + chain4Carbon;
                                    var chain12Double = chain1Double + chain2Double;
                                    var chain34Double = chain3Double + chain4Double;

                                    if (chain12Double > chain34Double)
                                    {
                                        (chain12Chain, chain34Chain) = (chain34Chain, chain12Chain);
                                        (chain12Double, chain34Double) = (chain34Double, chain12Double);
                                    }
                                    var shortName2 = lipidClass + " " + chain12Chain + ":" + chain12Double + "_" + chain34Chain + ":" + chain34Double;

                                    if (shortNameList01.Contains(shortName2)) { continue; }
                                    shortNameList01.Add(shortName2);
                                    name = shortName2;
                                }
                                GlycerolipidFragmentation.cardiolipinFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double, chain4Carbon, chain4Double);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
                                break;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        if(adduct.AdductIonName == "[M-2H]2-")
                        {
                            precursorMZ = Math.Round(meta.ExactMass / 2 + adduct.AdductIonMass, 4);
                        }
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);


                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                    }
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

        public static void fahfaTgGlyceroLipidGenerator(List<string> chains1, List<string> chains2, string lipidClass, string output) //CL
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < chains1.Count; i++)
            {
                for (int j = 0; j < chains1.Count; j++)
                {
                    for (int k = 0; k < chains1.Count; k++)
                    {
                        for (int l = 0; l < chains2.Count; l++)
                        {
                            var chain1 = new FattyAcidChain { ChainString = chains1[i], ChainLength = int.Parse(chains1[i].Split(':')[0]), BondCount = int.Parse(chains1[i].Split(':')[1]) };
                            var chain2 = new FattyAcidChain { ChainString = chains1[j], ChainLength = int.Parse(chains1[j].Split(':')[0]), BondCount = int.Parse(chains1[j].Split(':')[1]) };

                            var fas = new List<FattyAcidChain>() { chain1, chain2 };
                            fas = fas.OrderBy(n => n.BondCount).ThenBy(n => n.ChainLength).ToList();
                            var chainList = new List<string>() { fas[0].ChainString, fas[1].ChainString };
                            chainList.Add(chains1[k]);

                            //if (lipidClass.Contains("Ox"))
                            //{
                            //    for (int m = 1; m < 5; m++)
                            //    {
                            //        var chain2addO = chains2[l] + ":" + m;
                            //        chainList.Add(chain2addO);
                            //        wholeChainList.Add(string.Join("\t", chainList));
                            //    }
                            //}
                            //else
                            //{
                            chainList.Add(chains2[l]);
                            wholeChainList.Add(string.Join("\t", chainList));

                            //}
                        }
                    }
                }
            }
            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];
            var shortNameList = new List<string>();

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
                        var chain1String = chainArray[0];
                        var chain2String = chainArray[1];
                        var chain3String = chainArray[2];
                        var chain4String = chainArray[3];

                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);

                        var chain3Carbon = int.Parse(chain3String.Split(':')[0]);
                        var chain3Double = int.Parse(chain3String.Split(':')[1]);

                        var chain4Carbon = int.Parse(chain4String.Split(':')[0]);
                        var chain4Double = int.Parse(chain4String.Split(':')[1]);

                        //var chain1Mass = (chain1Chain * 12 + (2 * chain1Chain - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain1 acyl
                        //var chain2Mass = (chain2Chain * 12 + (2 * chain2Chain - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain2 acyl
                        //var chain3Mass = (chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass) + MassDictionary.OxygenMass;//chain3 acyl
                        //var chain4Mass = (chain4Chain * 12 + (2 * chain4Chain - 2 * chain4Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl

                        var totalChain = chain1Carbon + chain2Carbon + chain3Carbon + chain4Carbon;
                        var totalBond = chain1Double + chain2Double + chain3Double + chain4Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                        var chain3Smiles = new List<string>(AcylChainDic.HydroxyFaChainDictionary[chain3String])[3];
                        var chain4Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain4String])[3];

                        var rawSmiles = headerSmiles + chain1Smiles + "%11" + "." + chain2Smiles + "%12" + "." + chain3Smiles + "%13" + "." + chain4Smiles + "%10";

                        // fragment
                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);

                        switch (lipidClass)
                        {
                            //normal
                            case "FAHFATG": // 
                                name = "TG" + " " + chain1String + "_" + chain2String + "_" + chain3String + ";1O(FA " + chain4String + ")";
                                GlycerolipidFragmentation.fahfaTgFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double, chain4Carbon, chain4Double);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
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

        public static void bacterialLipidGenerator(List<string> chains, string lipidClass, string output) //Ac*PIM*
        {
            var wholeChainList = new List<string>();
            //chains.Add("18:0:methyl");

            for (int i = 0; i < chains.Count; i++)
            {
                for (int j = 0; j < chains.Count; j++)
                {
                    for (int k = 0; k < chains.Count; k++)
                    {
                        for (int l = 0; l < chains.Count; l++)
                        {
                            var sn3 = chains[k];
                            var sn4 = chains[l];

                            if (lipidClass == "Ac2PIM1" || lipidClass == "Ac2PIM2")
                            {
                                sn3 = "0:0";
                                sn4 = "0:0";
                            }
                            if (lipidClass == "Ac3PIM2")
                            {
                                sn4 = "0:0";
                            }
                            var chainList = new List<string> { chains[i], chains[j], sn3, sn4 };
                            wholeChainList.Add(string.Join("\t", chainList));
                        }
                    }
                }
            }

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];
            var shortNameList01 = new List<string>();

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                var filename = output + "\\" + lipidClass + "_" + fileSurfix + ".msp";

                using (var sw = new StreamWriter(filename, false, Encoding.ASCII))
                {

                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');
                        var chain1String = chainArray[0];
                        var chain2String = chainArray[1];
                        var chain3String = chainArray[2];
                        var chain4String = chainArray[3];

                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);
                        if (chain1String == "18:0:methyl")
                        { chain1Carbon = chain1Carbon + 1; }

                        var chain2Carbon = int.Parse(chain2String.Split(':')[0]);
                        var chain2Double = int.Parse(chain2String.Split(':')[1]);
                        if (chain2String == "18:0:methyl")
                        { chain2Carbon = chain2Carbon + 1; }

                        var chain3Carbon = int.Parse(chain3String.Split(':')[0]);
                        var chain3Double = int.Parse(chain3String.Split(':')[1]);
                        if (chain3String == "18:0:methyl")
                        { chain3Carbon = chain3Carbon + 1; }

                        var chain4Carbon = int.Parse(chain4String.Split(':')[0]);
                        var chain4Double = int.Parse(chain4String.Split(':')[1]);
                        if (chain4String == "18:0:methyl")
                        { chain4Carbon = chain4Carbon + 1; }

                        //var chain1Mass = (chain1Chain * 12 + (2 * chain1Chain - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain1 acyl
                        //var chain2Mass = (chain2Chain * 12 + (2 * chain2Chain - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain2 acyl
                        //var chain3Mass = (chain3Chain * 12 + (2 * chain3Chain - 2 * chain3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl
                        //var chain4Mass = (chain4Chain * 12 + (2 * chain4Chain - 2 * chain4Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//chain3 acyl

                        var totalChain = chain1Carbon + chain2Carbon + chain3Carbon + chain4Carbon;
                        var totalBond = chain1Double + chain2Double + chain3Double + chain4Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var chain1Smiles = "";
                        var chain2Smiles = "";
                        var chain3Smiles = "";
                        var chain4Smiles = "";

                        var rawSmiles = "";

                        if (chain1String == "18:0:methyl")
                        { chain1Smiles = "CCCCCCCCC(C)CCCCCCCCC(=O)"; chain1String = "18:0(methyl)"; }
                        else
                        { chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3]; }

                        if (chain2String == "18:0:methyl")
                        { chain2Smiles = "CCCCCCCCC(C)CCCCCCCCC(=O)"; chain2String = "18:0(methyl)"; }
                        else
                        { chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3]; }

                        if (chain3String == "18:0:methyl")
                        { chain3Smiles = "CCCCCCCCC(C)CCCCCCCCC(=O)"; chain3String = "18:0(methyl)"; }
                        else
                        { chain3Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain3String])[3]; }

                        if (chain4String == "18:0:methyl")
                        { chain4Smiles = "CCCCCCCCC(C)CCCCCCCCC(=O)"; chain4String = "18:0(methyl)"; }
                        else
                        { chain4Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain4String])[3]; }

                        if (lipidClass == "Ac2PIM1" || lipidClass == "Ac2PIM2")
                        {
                            rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11";
                        }
                        else if (lipidClass == "Ac3PIM2")
                        {
                            rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11" + "." + chain3Smiles + "%12";
                        }
                        else if (lipidClass == "Ac4PIM2")
                        {
                            rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%11" + "." + chain3Smiles + "%12" + "." + chain4Smiles + "%13";
                        }

                        // fragment
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);
                        var fragmentList = new List<string>();

                        switch (lipidClass)
                        {
                            //normal
                            case "Ac2PIM1": // 
                                name = lipidClass + " " + chain1String + "_" + chain2String;
                                GlycerolipidFragmentation.Ac2PIM1Fragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "Ac2PIM2": // 
                                name = lipidClass + " " + chain1String + "_" + chain2String;
                                GlycerolipidFragmentation.Ac2PIM2Fragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double);
                                break;
                            case "Ac3PIM2": // 
                                name = lipidClass + " " + chain1String + "_" + chain2String + "_" + chain3String;
                                GlycerolipidFragmentation.Ac3PIM2Fragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double);
                                break;
                            case "Ac4PIM2": // 
                                name = lipidClass + " " + chain1String + "_" + chain2String + "_" + chain3String + "_" + chain4String;
                                GlycerolipidFragmentation.Ac4PIM2Fragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon,chain1Double, chain2Carbon, chain2Double, chain3Carbon, chain3Double, chain4Carbon, chain4Double);
                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
                                break;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);


                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                    }
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

        public static void adggaGenerator(List<string> chains, string lipidClass, string output)
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            for (int i = 0; i < chainCount; i++)
            {
                for (int j = 0; j < chainCount; j++)
                {
                    for (int k = 0; k < chainCount; k++)
                    {
                        {
                            var chainList = new List<string> { chains[i], chains[j] };
                            chainList.Sort();
                            chainList.Add(chains[k]);
                            wholeChainList.Add(string.Join("\t", chainList));
                        }
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
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var fa1String = chainArray[0].Split(':');
                        var fa1Chain = int.Parse(fa1String[0]);
                        var fa1Double = int.Parse(fa1String[1]);
                        var fa1Mass = (fa1Chain * 12 + (2 * fa1Chain - 2 * fa1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//fa1 acyl

                        var fa2String = chainArray[1].Split(':');
                        var fa2Chain = int.Parse(fa2String[0]);
                        var fa2Double = int.Parse(fa2String[1]);
                        var fa2Mass = (fa2Chain * 12 + (2 * fa2Chain - 2 * fa2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//fa2 acyl

                        var fa3String = chainArray[2].Split(':');
                        var fa3Chain = int.Parse(fa3String[0]);
                        var fa3Double = int.Parse(fa3String[1]);
                        var fa3Mass = (fa3Chain * 12 + (2 * fa3Chain - 2 * fa3Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass);//fa3 acyl

                        var totalChain = fa1Chain + fa2Chain + fa3Chain;
                        var totalBond = fa1Double + fa2Double + fa3Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var fa1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[0]])[3];
                        var fa2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[1]])[3];
                        var fa3Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[2]])[3];
                        var rawSmiles = headerSmiles + fa1Smiles + "%10" + "." + fa2Smiles + "%11" + "." + fa3Smiles + "%12";

                        var SmilesParser = new SmilesParser();
                        var SmilesGenerator = new SmilesGenerator(SmiFlavors.Canonical);
                        var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
                        var smiles = SmilesGenerator.Create(iAtomContainer);
                        //var smiles2 = SmilesGenerator.Create(iAtomContainer, SmiFlavors.Canonical, new int[iAtomContainer.Atoms.Count]);

                        var InChIGeneratorFactory = new InChIGeneratorFactory();
                        var InChIKey = InChIGeneratorFactory.GetInChIGenerator(iAtomContainer).GetInChIKey();

                        //var JPlogPDescriptor = new JPlogPDescriptor();
                        //var logP = JPlogPDescriptor.Calculate(iAtomContainer).JLogP;

                        var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(iAtomContainer);
                        var formula = MolecularFormulaManipulator.GetString(iMolecularFormula);
                        var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);

                        // fragment
                        var fragmentList = new List<string>();

                        //(pos NH4)
                        if (adduct.AdductIonName == "[M+NH4]+")
                        {
                            //var fragmentList = new List<string>();

                            var fra01mass = exactMass + MassDictionary.NH4Adduct;
                            var fra01int = 300;
                            var fra01comment = "[M+NH4]+";
                            fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                            var fra02mass = exactMass - 194.042652622 - fa3Mass + 2 * MassDictionary.HydrogenMass + MassDictionary.Proton;
                            var fra02int = 100;
                            var fra02comment = "[M-SN3-C6H10O7+H]+";
                            fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                            var fra03mass = 194.042652622 + fa3Mass - MassDictionary.H2OMass - 2 * MassDictionary.HydrogenMass + MassDictionary.Proton;
                            var fra03int = 999;
                            var fra03comment = "[C6H10O7+SN3+H]+";
                            fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                            var fra04mass = fa1Mass + MassDictionary.CarbonMass * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                            var fra04int = 300;
                            var fra04comment = "[SN1+C3H4O2+H]+";
                            fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                            var fra05mass = fa2Mass + MassDictionary.CarbonMass * 3 + MassDictionary.HydrogenMass * 4 + MassDictionary.OxygenMass * 2 + MassDictionary.Proton;
                            var fra05int = 300;
                            var fra05comment = "[SN2+C3H4O2+H]+";
                            fragmentList.Add(fra05mass + "\t" + fra05int + "\t" + fra05comment);

                            var fra06mass = fa3Mass - MassDictionary.HydrogenMass * 2 + MassDictionary.Proton;
                            var fra06int = 300;
                            var fra06comment = "[SN3-2H+H]+";
                            fragmentList.Add(fra06mass + "\t" + fra06int + "\t" + fra06comment);

                            name = lipidClass + " (O-" + chainArray[2] + ")" + chainArray[0] + "_" + chainArray[1];
                        }
                        //(neg H)
                        if (adduct.AdductIonName == "[M-H]-")
                        {
                            //var fragmentList = new List<string>();

                            var fra01mass = exactMass - MassDictionary.Proton;
                            var fra01int = 999;
                            var fra01comment = "[M-H]-";
                            fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                            var fra02mass = fa1Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                            var fra02int = 300;
                            var fra02comment = "[SN1]-";
                            fragmentList.Add(fra02mass + "\t" + fra02int + "\t" + fra02comment);

                            var fra03mass = fa2Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                            var fra03int = 300;
                            var fra03comment = "[SN2]-";
                            fragmentList.Add(fra03mass + "\t" + fra03int + "\t" + fra03comment);

                            var fra04mass = fa3Mass + MassDictionary.OxygenMass - MassDictionary.Proton;
                            var fra04int = 300;
                            var fra04comment = "[SN3]-";
                            fragmentList.Add(fra04mass + "\t" + fra04int + "\t" + fra04comment);

                            name = lipidClass + " " + chainArray[2] + "_" + chainArray[0] + "_" + chainArray[1];
                        }

                        //
                        var precursorMZ = Math.Round(exactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, formula, name, smiles, InChIKey, adduct.AdductIonName, ionmode, lipidClass, fragmentList);

                        smileslist.Add(InChIKey + "\t" + smiles);
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

