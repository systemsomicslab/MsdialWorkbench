using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MspGenerator
{
    public class OtherLipidMspGenerator
    {

        public static void faChains(int minCarbon, int minDouble, int maxCarbon, int maxDouble, List<string> faChains)
        {
            foreach (var item in AcylChainDic.FattyAcylChainDictionary)
            {
                var chainCarbon = int.Parse(item.Value[0]);
                var chainDouble = int.Parse(item.Value[1]);
                if (chainCarbon >= minCarbon && chainCarbon <= maxCarbon && chainDouble >= minDouble && chainDouble <= maxDouble)
                {
                    faChains.Add(item.Key);
                };
            }
        }

        public static void faGenerator(List<string> chains, string lipidClass, string output)
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            wholeChainList = chains;

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = "O%10.";

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

                        var totalChain = fa1Chain;
                        var totalBond = fa1Double;

                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        var fa1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chainArray[0]])[3];
                        var rawSmiles = headerSmiles + fa1Smiles + "%10";
                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();

                        //(neg H)
                        if (adduct.AdductIonName == "[M-H]-")
                        {
                            //var fragmentList = new List<string>();

                            var fra01mass = meta.ExactMass - MassDictionary.Proton;
                            var fra01int = 999;
                            var fra01comment = "[M-H]-";
                            fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                            name = lipidClass + " " + chainArray[0];
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, lipidClass, fragmentList);

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

        public static void oxFaGenerator(List<string> chains, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();
            foreach (var chain in chains)
            {
                for (int i = 1; i < 5; i++)
                {
                    var chainString = chain + ":" + i;
                    wholeChainList.Add(chainString);
                }
            }

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = "O%10.";

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

                        var fa1String = chainArray[0];
                        var fa1Chain = int.Parse(fa1String.Split(':')[0]);
                        var fa1Double = int.Parse(fa1String.Split(':')[1]);
                        var fa1Ox = int.Parse(fa1String.Split(':')[2]);

                        var fa1Mass = fa1Chain * 12 + (2 * fa1Chain - 2 * fa1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass * (fa1Ox + 1);//fa1 acyl

                        var totalChain = fa1Chain;
                        var totalBond = fa1Double;

                        var shortName = "FA" + " " + totalChain + ":" + totalBond;

                        var name = shortName;

                        if (AcylChainDic.OxFaChainDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                        var fa1Smiles = new List<string>(AcylChainDic.OxFaChainDictionary[chainArray[0]])[3];
                        var rawSmiles = headerSmiles + fa1Smiles + "%10";
                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();

                        //(neg H)
                        if (adduct.AdductIonName == "[M-H]-")
                        {
                            //var fragmentList = new List<string>();

                            var fra01mass = meta.ExactMass - MassDictionary.Proton;
                            var fra01int = 999;
                            var fra01comment = "[M-H]-";
                            fragmentList.Add(fra01mass + "\t" + fra01int + "\t" + fra01comment);

                            name = "FA" + " " + fa1Chain + ":" + fa1Double + ";O" + fa1Ox;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, lipidClass, fragmentList);

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

        public static void alphaOxFaGenerator(List<string> chains, string lipidClass, string output)
        {
            var wholeChainList = chains;

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = "O%10.";

            var adducts = adductDic.lipidClassAdductDic["OxFA"];
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

                        var fa1String = chainArray[0];
                        var chain1Carbon = int.Parse(fa1String.Split(':')[0]);
                        var chain1Double = int.Parse(fa1String.Split(':')[1]);
                        var fa1Ox = 1;

                        var fa1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass * (fa1Ox + 1);//fa1 acyl

                        var totalChain = chain1Carbon;
                        var totalBond = chain1Double;

                        var shortName = "FA" + " " + totalChain + ":" + totalBond + ";O";

                        var name = shortName;

                        if (AcylChainDic.AlphaHydroxyFaChainDictionary.ContainsKey(chainArray[0]) == false) { continue; }
                        var fa1Smiles = new List<string>(AcylChainDic.AlphaHydroxyFaChainDictionary[chainArray[0]])[3].Replace("%10", "");
                        var rawSmiles = headerSmiles + fa1Smiles + "%10";
                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();

                        OtherLipidFragmentation.alphaOxFAFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                        name = "FA" + " " + chain1Carbon + ":" + chain1Double + ";" + "(2OH)";
                        var exportLipidClass = "OxFA";

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClass, fragmentList);

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

        public static void fahfasGenerator(List<string> Chain1, List<string> chain2, string lipidClass, string output)
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
                var filename = lipidClass + "_" + fileSurfix + ".msp";
                using (var sw = new StreamWriter(output + "\\" + filename, false, Encoding.ASCII))
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
                        //var chain2Ox = 0;

                        var totalChain = chain1Carbon + chain2Carbon;
                        var totalBond = chain1Double + chain2Double;

                        //var chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain1 Acyl
                        //var chain2Mass = chain2Carbon * 12 + (2 * chain2Carbon - 2 * chain2Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain2 Acyl

                        if (AcylChainDic.HydroxyFaChainDictionary.ContainsKey(chain1String) == false) { continue; }

                        var chain1Smiles = new List<string>(AcylChainDic.HydroxyFaChainDictionary[chain1String])[3];
                        if (lipidClass == "AAHFA")
                        {
                            chain1Smiles = new List<string>(AcylChainDic.AlphaHydroxyFaChainDictionary[chain1String])[3];
                        }

                        var chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];

                        var name = "";
                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;
                        //var shortNameList = new List<string>();

                        var exportLipidClassName = lipidClass;
                        var rawSmiles = headerSmiles + chain1Smiles + "%20" + "." + chain2Smiles + "%10";
                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();

                        switch (lipidClass)
                        {
                            case "FAHFA":
                                OtherLipidFragmentation.fahfaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                name = lipidClass + " " + chain2String + "/" + chain1String + ";O";
                                break;
                            case "NAGlySer_FAHFA":
                                OtherLipidFragmentation.fahfaGlySerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                exportLipidClassName = "NAGlySer";
                                //name = exportLipidClassName + " " + chain2String + "/" + chain1String + ";O";
                                name = exportLipidClassName + " " + chain1String + ";O(FA " + chain2String + ")";
                                break;
                            case "NAGly_FAHFA":
                                OtherLipidFragmentation.fahfaGlyFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                exportLipidClassName = "NAGly";
                                //name = exportLipidClassName + " " + chain2String + "/" + chain1String + ";O";
                                name = exportLipidClassName + " " + chain1String + ";O(FA " + chain2String + ")";
                                break;
                            case "NAOrn_FAHFA":
                                OtherLipidFragmentation.fahfaOrnFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                exportLipidClassName = "NAOrn";
                                //name = exportLipidClassName + " " + chain2String + "/" + chain1String + ";O";
                                name = exportLipidClassName + " " + chain1String + ";O(FA " + chain2String + ")";
                                break;
                            case "AAHFA":
                                OtherLipidFragmentation.aahfaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
                                exportLipidClassName = "FAHFA";
                                name = lipidClass + " " + chain2String + "/" + chain1String + ";O";
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

        public static void fahfaDmedGenerator(List<string> Chain1, List<string> chain2, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < Chain1.Count; i++)
            {
                for (int j = 0; j < chain2.Count; j++)
                {
                    var chainList = new List<string> { Chain1[i], chain2[j] };
                    wholeChainList.Add(string.Join("\t", chainList));
                }
            }

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = smilesHeaderDict[lipidClass];

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            var baseChainDic = AcylChainDic.fahfaDmedBaseChainDictionary;
            var extraChainDic = AcylChainDic.FattyAcylChainDictionary;
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();
                var shortNameList2 = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);
                var filename = lipidClass + "_" + fileSurfix + ".msp";
                var chainNameList = new List<string>();
                using (var sw = new StreamWriter(output + "\\" + filename, false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var baseChain = baseChainDic[chainArray[0]];
                        var baseChainCarbon = int.Parse(baseChain[0]);
                        var baseChainDouble = int.Parse(baseChain[1]);
                        var baseChainOxposition = int.Parse(baseChain[2]);
                        var baseChainString = baseChain[0] + ":" + baseChain[1] + ";O";


                        var extraChain = extraChainDic[chainArray[1]];
                        var extraChainCarbon = int.Parse(extraChain[0]);
                        var extraChainDouble = int.Parse(extraChain[1]);

                        var totalChain = baseChainCarbon + extraChainCarbon;
                        var totalBond = baseChainDouble + extraChainDouble;

                        var baseChainSmiles = baseChain[4];

                        var extraChainSmiles = extraChain[3];

                        var name = "";
                        var shortName = "FAHFA" + " " + totalChain + ":" + totalBond + ";O";

                        var exportLipidClassName = "FAHFA";
                        var rawSmiles = extraChainSmiles + "%10." + baseChainSmiles;
                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();
                        OtherLipidFragmentation.fahfaDmedFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, baseChainCarbon, baseChainDouble, extraChainCarbon, extraChainDouble, baseChainOxposition);
                        name = "FAHFA" + " " + chainArray[1] + "/" + baseChainString;
                        if (chainNameList.Contains(name))
                        {
                            continue;
                        }

                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, meta.Smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);

                        smileslist.Add(meta.inChIKey + "\t" + meta.Smiles);
                        chainNameList.Add(name);

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

        public static void singleAcylChainLipidGenerator(List<string> Chain1, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < Chain1.Count; i++)
            {
                var chainList = new List<string> { Chain1[i] };
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
                var filename = lipidClass + "_" + fileSurfix + ".msp";
                using (var sw = new StreamWriter(output + "\\" + filename, false, Encoding.ASCII))
                {
                    for (int i = 0; i < wholeChainList.Count; i++)
                    {
                        var chainArray = wholeChainList[i].Split('\t');

                        var chain1String = chainArray[0];
                        var chain1Carbon = int.Parse(chain1String.Split(':')[0]);
                        var chain1Double = int.Parse(chain1String.Split(':')[1]);

                        var totalChain = chain1Carbon;
                        var totalBond = chain1Double;

                        var chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass + MassDictionary.OxygenMass;//chain1 Acyl

                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];

                        var name = "";
                        var shortName = lipidClass + " " + totalChain + ":" + totalBond;
                        //var shortNameList = new List<string>();

                        var exportLipidClassName = lipidClass;
                        var rawSmiles = headerSmiles + chain1Smiles + "%10";
                        if (lipidClass.Contains("NA") && lipidClass.Contains("_FA"))
                        {

                            if (AcylChainDic.FattyAcylChainDictionary.ContainsKey(chain1String))
                            {
                                chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                                rawSmiles = headerSmiles + chain1Smiles + "%20";
                            }
                            else
                            {
                                continue;
                            }
                        }
                        if (lipidClass.Contains("NA") && lipidClass.Contains("_OxFA"))
                        {

                            if (AcylChainDic.AcylChainBetaOxDictionary.ContainsKey(chain1String))
                            {
                                chain1Smiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[chain1String])[3];
                                rawSmiles = headerSmiles + chain1Smiles + "%20";
                            }
                            else
                            {
                                continue;
                            }
                        }

                        var meta = Common.getMetaProperty(rawSmiles);

                        // fragment
                        var fragmentList = new List<string>();

                        switch (lipidClass)
                        {
                            case "CAR":
                                OtherLipidFragmentation.carFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                name = lipidClass + " " + chain1String;
                                break;

                            case "VAE":
                                OtherLipidFragmentation.vitAEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                name = lipidClass + " " + chain1String;
                                break;

                            case "NAE":
                                OtherLipidFragmentation.anandamideFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                name = lipidClass + " " + chain1String;
                                break;

                            case "NAGlySer_OxFA":
                                OtherLipidFragmentation.oxFAGlySerFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAGlySer";
                                name = exportLipidClassName + " " + chain1String + ";O";
                                break;
                            case "NAGly_OxFA":
                                OtherLipidFragmentation.oxFAGlyFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAGly";
                                name = exportLipidClassName + " " + chain1String + ";O";
                                break;
                            case "NAOrn_OxFA":
                                OtherLipidFragmentation.oxFAOrnFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAOrn";
                                name = exportLipidClassName + " " + chain1String + ";O";
                                break;
                            case "NATau_FA":
                                OtherLipidFragmentation.FATauFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NATau";
                                name = exportLipidClassName + " " + chain1String;
                                break;
                            case "NATau_OxFA":
                                OtherLipidFragmentation.oxFATauFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NATau";
                                name = exportLipidClassName + " " + chain1String + ";O";
                                break;

                            case "NAPhe_FA":
                                OtherLipidFragmentation.FAPheFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAPhe";
                                name = exportLipidClassName + " " + chain1String;
                                break;

                            case "NAPhe_OxFA":
                                OtherLipidFragmentation.oxFAPheFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAPhe";
                                name = exportLipidClassName + " " + chain1String + ";O";
                                break;

                            case "NAGly_FA":
                                OtherLipidFragmentation.FAGlyFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAGly";
                                name = exportLipidClassName + " " + chain1String;
                                break;

                            case "NAOrn_FA":
                                OtherLipidFragmentation.FAOrnFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                exportLipidClassName = "NAOrn";
                                name = exportLipidClassName + " " + chain1String;
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

        public static void singleAcylChainWithSteroidalLipidGenerator(List<string> Chain1, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            for (int i = 0; i < Chain1.Count; i++)
            {
                var chainList = new List<string> { Chain1[i] + "\t0" };
                wholeChainList.Add(string.Join("\t", chainList));
            }

            if (lipidClass == "DCAE" || lipidClass == "GDCAE" || lipidClass == "GLCAE" || lipidClass == "TDCAE" ||
                lipidClass == "TLCAE" || lipidClass == "KLCAE" || lipidClass == "KDCAE" || lipidClass == "LCAE")  // bileAcids with OxFA chain
            {
                for (int i = 0; i < Chain1.Count; i++)
                {
                    var chainList = new List<string> { Chain1[i] + "\t1" };
                    wholeChainList.Add(string.Join("\t", chainList));
                }
            }

            wholeChainList = wholeChainList.Distinct().ToList();
            var smileslist = new List<string>();
            var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
            var headerSmiles = "";

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
                        var chain1Ox = int.Parse(chainArray[1]);

                        //var chain1Mass = chain1Carbon * 12 + (2 * chain1Carbon - 2 * chain1Double) * MassDictionary.HydrogenMass + (MassDictionary.OxygenMass * (chain1Ox + 1));//chain1 Acyl
                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];

                        if (chain1Ox > 0)
                        {
                            if (AcylChainDic.OxFaChainDictionary.ContainsKey(chain1String + ":" + chain1Ox) == false) { continue; }

                            chain1Smiles = new List<string>(AcylChainDic.OxFaChainDictionary[chain1String + ":" + chain1Ox])[3];
                        }

                        var name = "";

                        var exportLipidClassName = lipidClass;
                        var rawSmiles = headerSmiles + chain1Smiles + "%10";
                        var meta = new MetaProperty();

                        // fragment
                        var fragmentList = new List<string>();

                        switch (lipidClass)
                        {
                            case "CE":
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.cholesterylEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = lipidClass + " " + chain1String;
                                break;

                            case "DCAE":
                            case "GDCAE":
                            case "GLCAE":
                            case "TDCAE":
                            case "TLCAE":
                            case "KLCAE":
                            case "KDCAE":
                            case "LCAE":
                                var cholicAcid = new Dictionary<string, string>()
                                { {"DCAE","SE 24:1;O4" },{"GDCAE","BA 24:1;O4;G" },{"GLCAE","BA 24:1;O3;G"},
                                    {"TDCAE","BA 24:1;O4;T"},{"TLCAE","BA 24:1;O3;T"},{ "KLCAE","SE 24:2;O4"},
                                    { "KDCAE","SE 24:2;O5"},{"LCAE","SE 24:1;O3" } };
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.cholicAcidEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox, lipidClass);
                                name = cholicAcid[lipidClass] + "/" + chain1String;
                                if (chain1Ox > 0)
                                {
                                    name = name + ";O" + chain1Ox;
                                }
                                break;

                            case "BRSE":
                            case "CASE":
                            case "SISE":
                            case "STSE":
                                var cags = new Dictionary<string, string>()
                                { {"BRSE","SE 28:2" },{"CASE","SE 28:1" },{"SISE","SE 29:1"},{"STSE","SE 29:2"} };
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.steroidalEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = cags[lipidClass] + "/" + chain1String;
                                break;

                            case "EGSE":
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.ergosterolEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = "SE 28:3" + "/" + chain1String;
                                break;

                            case "DEGSE":
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.ergosterolEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = "SE 28:4" + "/" + chain1String;
                                break;

                            case "DSMSE":
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.desmosterolEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = "SE 27:2" + "/" + chain1String;
                                break;


                            case "AHexBRS":
                            case "AHexCAS":
                            case "AHexCS":
                            case "AHexSIS":
                            case "AHexSTS":
                                var ahexCags = new Dictionary<string, string>()
                                { {"AHexBRS","ASG 28:2;O;Hex" },{"AHexCAS","ASG 28:1;O;Hex" },{"AHexCS","ASG 27:1;O;Hex" },{"AHexSIS","ASG 29:1;O;Hex"},{"AHexSTS","ASG 29:2;O;Hex"} };
                                headerSmiles = smilesHeaderDict[lipidClass];
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.aHexSteroidalEsterFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain1Ox);
                                name = ahexCags[lipidClass] + ";FA " + chain1String;
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

        public static void steroidWithGlyceroLipidGenerator(List<string> chains, string lipidClass, string output)
        {
            int chainCount = chains.Count;
            var wholeChainList = new List<string>();

            if (lipidClass == "CSLPHex" || lipidClass == "BRSLPHex" || lipidClass == "CASLPHex" || lipidClass == "SISLPHex" || lipidClass == "STSLPHex")
            {
                for (int i = 0; i < chainCount; i++)
                {
                    var chainList = new List<string> { chains[i] };
                    chainList.Add("0:0"); // dummy
                    wholeChainList.Add(string.Join("\t", chainList));
                }
            }
            else
            {
                for (int i = 0; i < chainCount; i++)
                {
                    for (int j = 0; j < chainCount; j++)
                    {
                        var chainList = new List<string> { chains[i], chains[j] };
                        chainList.Sort();
                        wholeChainList.Add(string.Join("\t", chainList));
                    }
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

                        if (chain2String != "0:0" && chain1Double > chain2Double)
                        {
                            (chain1String, chain2String) = (chain2String, chain1String);
                            (chain1Carbon, chain2Carbon) = (chain2Carbon, chain1Carbon);
                            (chain1Double, chain2Double) = (chain2Double, chain1Double);
                        }

                        var totalChain = chain1Carbon + chain2Carbon;
                        var totalBond = chain1Double + chain2Double;

                        var shortName = "";
                        var name = shortName;

                        var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                        var headerSmiles = smilesHeaderDict[lipidClass];
                        var chain1Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain1String])[3];
                        var chain2Smiles = "";
                        var rawSmiles = "";

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = new MetaProperty();

                        switch (lipidClass)
                        {
                            //normal
                            case "CSLPHex":
                            case "BRSLPHex":
                            case "CASLPHex":
                            case "SISLPHex":
                            case "STSLPHex":
                                var LPHex = new Dictionary<string, string>()
                                { {"BRSLPHex","SG 28:2;O;Hex;LPA " },{"CASLPHex","SG 28:1;O;Hex;LPA " },{"CSLPHex","SG 27:1;O;Hex;LPA " },{"SISLPHex","SG 29:1;O;Hex;LPA "},{"STSLPHex","SG 29:2;O;Hex;LPA "} };
                                name = LPHex[lipidClass] + chain1String;
                                rawSmiles = headerSmiles + chain1Smiles + "%10";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.steroidWithLpaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double);
                                break;
                            case "CSPHex":
                            case "BRSPHex":
                            case "CASPHex":
                            case "SISPHex":
                            case "STSPHex":
                                var PHex = new Dictionary<string, string>()
                                { {"BRSPHex","SG 28:2;O;Hex;PA " },{"CASPHex","SG 28:1;O;Hex;PA " },{"CSPHex","SG 27:1;O;Hex;PA " },{"SISPHex","SG 29:1;O;Hex;PA "},{"STSPHex","SG 29:2;O;Hex;PA "} };
                                name = PHex[lipidClass] + chain1String + "_" + chain2String;
                                shortName = PHex[lipidClass] + totalChain + ":" + totalBond;
                                if (adductIon == "[M+NH4]+")
                                {
                                    if (shortNameList.Contains(shortName)) { continue; }
                                    shortNameList.Add(shortName);
                                    name = shortName;
                                }
                                chain2Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[chain2String])[3];
                                rawSmiles = headerSmiles + chain1Smiles + "%10" + "." + chain2Smiles + "%20";
                                meta = Common.getMetaProperty(rawSmiles);
                                OtherLipidFragmentation.steroidWithPaFragment(fragmentList, adduct.AdductIonName, meta.ExactMass, chain1Carbon, chain1Double, chain2Carbon, chain2Double);
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

        public static void noChainSteroidalLipidGenerator(string lipidClass, string output)
        {
            var smileslist = new List<string>();
            var cholicAcids = new Dictionary<string, string>()
                                { {"LCA", "ST 24:1;O3"},{"7KLCA", "ST 24:2;O3"},{"DCA", "ST 24:1;O4"},{"CA", "ST 24:1;O5"},{"GLCA", "BA 24:1;O3;G"},
                                    {"GDCA", "BA 24:1;O4;G"},{"GCA", "BA 24:1;O5;G"},{"TLCA", "BA 24:1;O3;T"},{"TDCA", "BA 24:1;O4;T"},{"TCA", "BA 24:1;O5;T"},
                                };
            var cags = new Dictionary<string, string>()
                                {{"CE","27:1" },{"BRSE","28:2" },{"CASE","28:1" },{"SISE","29:1"},{"STSE","29:2"} ,{ "Cholestan","27:0"} };

            var lipidDic = new Dictionary<string, string>();

            if (lipidClass == "BAHex" || lipidClass == "BASulfate")
            {
                lipidDic = cholicAcids;
            }
            else if (lipidClass == "SPE" || lipidClass == "SHex" || lipidClass == "SSulfate" || lipidClass == "SPEHex" || lipidClass == "SPGHex")
            {
                lipidDic = cags;
            }

            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var shortNameList = new List<string>();

                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    foreach (var lipid in lipidDic)
                    {
                        var name = "";

                        var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                        var headerSmiles = "";
                        var rawSmiles = "";
                        var smiles = "";

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = new MetaProperty();

                        switch (lipidClass)
                        {
                            case "BAHex":
                                name = lipid.Value + ";Hex";
                                if (name.Contains("ST"))
                                {
                                    name = name.Replace("ST", "SG");
                                }
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "C1OC(CO)C(O)C(O)C1O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = rawSmiles;
                                OtherLipidFragmentation.baHexFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
                                break;
                            case "BASulfate":
                                name = lipid.Value + ";S";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "S(O)(=O)=O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = rawSmiles;
                                OtherLipidFragmentation.baSulfateFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);

                                break;
                            case "SHex":
                                name = "SG " + lipid.Value + ";O;Hex";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "C1%10OC(CO)C(O)C(O)C1O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = meta.Smiles;
                                OtherLipidFragmentation.steroidHexFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);

                                break;

                            case "SPE":
                                name = "ST " + lipid.Value + ";O;PE";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "P%10(O)(=O)OCCN";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = meta.Smiles;
                                OtherLipidFragmentation.steroidPEFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
                                break;

                            case "SPEHex":
                                name = "SG " + lipid.Value + ";O;Hex;PE";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "C1%10OC(COP(O)(=O)OCCN)C(O)C(O)C1O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = meta.Smiles;
                                OtherLipidFragmentation.steroidPEHexFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
                                break;

                            case "SPGHex":
                                name = "SG " + lipid.Value + ";O;Hex;PG";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "C1%10OC(COP(O)(=O)OCC(O)CO)C(O)C(O)C1O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = meta.Smiles;
                                OtherLipidFragmentation.steroidPGHexFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
                                break;

                            case "SSulfate":
                                name = "ST " + lipid.Value + ";O;S";
                                headerSmiles = smilesHeaderDict[lipid.Key];
                                rawSmiles = headerSmiles + "S%10(O)(=O)=O";
                                meta = Common.getMetaProperty(rawSmiles);
                                smiles = meta.Smiles;
                                OtherLipidFragmentation.steroidSulfateFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);

                                break;

                            default:
                                Console.WriteLine("Error in lipidClass switch. Please check settings...");
                                Console.ReadKey();
                                break;

                        }
                        //
                        var precursorMZ = Math.Round(meta.ExactMass + adduct.AdductIonMass, 4);
                        ExportMSP.exportMspFile(sw, precursorMZ, meta.Formula, name, smiles, meta.inChIKey, adduct.AdductIonName, ionmode, exportLipidClassName, fragmentList);

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

        public static void noChainLipidGenerator(string lipidClass, string output)
        {
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
                    var name = "";

                    var rawSmiles = "";

                    var fragmentList = new List<string>();
                    var exportLipidClassName = lipidClass;
                    var meta = new MetaProperty();

                    switch (lipidClass)
                    {
                        case "VitaminE":
                            rawSmiles = "CC1=C(C(=C2CCC(OC2=C1C)(C)CCCC(C)CCCC(C)CCCC(C)C)C)O";
                            name = "Tocopherol";
                            meta = Common.getMetaProperty(rawSmiles);
                            OtherLipidFragmentation.vitaminEFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
                            break;
                        case "Vitamin_D":
                            rawSmiles = "CC(CCCC(C)(C)O)C1CCC2C1(CCCC2=CC=C3CC(CCC3=C)O)C";
                            name = "25-hydroxycholecalciferol";
                            meta = Common.getMetaProperty(rawSmiles);
                            OtherLipidFragmentation.vitaminDFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
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

        public static void coenzymeQGenerator(string lipidClass, string output)
        {
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
                    var headerSmiles = "CC1=C(C(=O)C(=C(C1=O)OC)OC)";
                    var rawSmiles = headerSmiles;

                    for (int i = 1; i < 15; i++)
                    {
                        var name = "CoQ" + i;

                        rawSmiles = rawSmiles + "CC=C(C)C";

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);

                        OtherLipidFragmentation.coenzymeQFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
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

        public static void sterolsGenerator(string lipidClass, string output)
        {
            var smileslist = new List<string>();
            var adducts = adductDic.lipidClassAdductDic[lipidClass];
            var classDic = new Dictionary<string, string>()
                                {{"CE","27:1" },{"BRSE","28:2" },{"CASE","28:1" },{"SISE","29:1"},{"STSE","29:2"} ,{ "Cholestan","27:0"} ,
                                   {"EGSE","28:3" },{"DEGSE","28:4" },{"DSMSE","27:2" } };

            foreach (var adductIon in adducts)
            {
                var adduct = adductDic.adductIonDic[adductIon];
                var ionmode = adduct.IonMode;
                var fileSurfix = adduct.AdductSurfix + "_" + ionmode.Substring(0, 3);

                using (var sw = new StreamWriter(output + "\\" + lipidClass + "_" + fileSurfix + ".msp", false, Encoding.ASCII))
                {
                    foreach (var item in classDic)
                    {
                        var name = "ST " + item.Value + ";O";

                        var smilesHeaderDict = SmilesLipidHeader.HeaderDictionary;
                        var headerSmiles = smilesHeaderDict[item.Key];
                        var rawSmiles = headerSmiles.Replace("%10", "");

                        var fragmentList = new List<string>();
                        var exportLipidClassName = lipidClass;
                        var meta = Common.getMetaProperty(rawSmiles);

                        OtherLipidFragmentation.sterolsFragment(fragmentList, adduct.AdductIonName, meta.ExactMass);
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


        public static void lipidAGenerator(List<string> chains, string lipidClass, string output)
        {
            var wholeChainList = new List<string>();

            var q = from c1 in chains
                    from c2 in chains
                    from c3 in chains
                    from c4 in chains
                    from c5 in chains
                    from c6 in chains
                    select new { c1, c2, c3, c4, c5, c6 };

            foreach (var x in q)
            {
                wholeChainList.Add(string.Join("\t", x.c1, x.c2, x.c3, x.c4, x.c5, x.c6));
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
                        var hfa01String = chainArray[0];
                        var hfa02String = chainArray[1];
                        var fahfaHfa01String = chainArray[2];
                        var fahfaHfa02String = chainArray[3];
                        var fahfaFa01String = chainArray[4];
                        var fahfaFa02String = chainArray[5];

                        var hfa01Carbon = int.Parse(hfa01String.Split(':')[0]);
                        var hfa01Double = int.Parse(hfa01String.Split(':')[1]);

                        var hfa02Carbon = int.Parse(hfa02String.Split(':')[0]);
                        var hfa02Double = int.Parse(hfa02String.Split(':')[1]);

                        var fahfaHfa01Carbon = int.Parse(fahfaHfa01String.Split(':')[0]);
                        var fahfaHfa01Double = int.Parse(fahfaHfa01String.Split(':')[1]);

                        var fahfaHfa02Carbon = int.Parse(fahfaHfa02String.Split(':')[0]);
                        var fahfaHfa02Double = int.Parse(fahfaHfa02String.Split(':')[1]);

                        var fahfaFa01Carbon = int.Parse(fahfaFa01String.Split(':')[0]);
                        var fahfaFa01Double = int.Parse(fahfaFa01String.Split(':')[1]);

                        var fahfaFa02Carbon = int.Parse(fahfaFa02String.Split(':')[0]);
                        var fahfaFa02Double = int.Parse(fahfaFa02String.Split(':')[1]);

                        var name = "LipidA " + fahfaHfa02String + "-O-" + fahfaFa02String + "_N-" + fahfaHfa01String + "-O-" + fahfaFa01String + "_" + hfa02String + "_N-" + hfa01String;

                        var hfa01Smiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[hfa01String])[3];
                        var hfa02Smiles = new List<string>(AcylChainDic.AcylChainBetaOxDictionary[hfa02String])[3];
                        var fahfaHfa01Smiles = new List<string>(AcylChainDic.HydroxyFaChainDictionary[fahfaHfa01String])[3].Replace("%10", "%20");
                        var fahfaHfa02Smiles = new List<string>(AcylChainDic.HydroxyFaChainDictionary[fahfaHfa02String])[3].Replace("%10", "%30");
                        var fahfaFa01Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[fahfaFa01String])[3];
                        var fahfaFa02Smiles = new List<string>(AcylChainDic.FattyAcylChainDictionary[fahfaFa02String])[3];

                        var rawSmiles = headerSmiles + hfa01Smiles + "%10." + hfa02Smiles + "%11." + fahfaHfa01Smiles + "%12." + fahfaHfa02Smiles + "%13."
                                            + fahfaFa01Smiles + "%20." + fahfaFa02Smiles + "%30";

                        // fragment
                        var exportLipidClassName = lipidClass;
                        var fragmentList = new List<string>();
                        var meta = Common.getMetaProperty(rawSmiles);

                        OtherLipidFragmentation.lipidAFragmantation
                            (fragmentList, adduct.AdductIonName, meta.ExactMass, hfa01Carbon, hfa01Double, hfa02Carbon, hfa02Double, fahfaHfa01Carbon, fahfaHfa01Double, fahfaHfa02Carbon, fahfaHfa02Double, fahfaFa01Carbon, fahfaFa01Double, fahfaFa02Carbon, fahfaFa02Double);
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

    }
}
