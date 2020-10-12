using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using NCDK;
using NCDK.IO;
using NCDK.IO.Listener;
using NCDK.QSAR.Descriptors.Moleculars;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;
using RDotNet;
using CompMs.Common.DataStructure;

namespace CompMs.MspGenerator
{
    public class RtCcsPredictManager
    {

        public static void smilesToSdfOnNCDK(string workingDirectry, string toPredictFile)
        {
            //var toPredictFilePath = workingDirectry + "\\" + toPredictFile;
            var outputFileName = Path.GetFileNameWithoutExtension(toPredictFile);
            var outputSdfFolderPath = workingDirectry + @"\sdf\" + outputFileName;
            if (!Directory.Exists(outputSdfFolderPath))
            {
                Directory.CreateDirectory(outputSdfFolderPath);
            }
            var sdfFile = outputSdfFolderPath + "\\" + outputFileName + ".sdf";
            var outputFolderPath = workingDirectry;
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            var SmilesParser = new SmilesParser();
            var iAtomContList = new List<IAtomContainer>();
            var coordinate2D = new NCDK.Layout.StructureDiagramGenerator();

            //using (var sw = new StreamWriter(outputFolderPath + "\\InChIKey-Exactmass.txt", false))
            //{
            using (var sr = new StreamReader(toPredictFile, false))
            {
                var line = sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var linearray = line.Split('\t');
                    var inchikey = linearray[0];
                    var smiles = linearray[1];
                    if (smiles == "SMILES") { continue; }
                    var iAtomCont = SmilesParser.ParseSmiles(smiles);
                    iAtomCont.Title = inchikey;
                    coordinate2D.GenerateCoordinates(iAtomCont); // 2D座標計算 これは無くてもOK
                    iAtomContList.Add(iAtomCont);

                    //var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(iAtomCont);
                    //var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);
                    //sw.WriteLine(inchikey + '\t' + exactMass);
                }
            }
            //}

            using (var sw = new StreamWriter(sdfFile, false))
            {
                using (var writer = new MDLV2000Writer(sw))
                {
                    foreach (var iAtom in iAtomContList)
                    {
                        writer.WriteMolecule(iAtom);
                        sw.WriteLine("$$$$");
                    }
                }
            }
        }



        public static void runPaDEL(string workingDirectry, string padelDescriptortypes, string padelProgramPath, string toPredictFile)
        {
            var padelDescriptortypesPath = padelDescriptortypes;
            var toPredictFileName = Path.GetFileNameWithoutExtension(toPredictFile);
            var outputSdfFolderPath = workingDirectry + @"\sdf\" + toPredictFileName + "\\";
            var sdfFileName = Directory.GetFiles(outputSdfFolderPath);
            var outputFolderPath = workingDirectry + @"\PadelResult\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            var padelOutFile = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(sdfFileName[0]) + ".csv";
            var padelOption = " -dir " + outputSdfFolderPath + @" -file " + padelOutFile + " -2d -descriptortypes " + padelDescriptortypesPath + " -threads 1 -maxruntime 30000";

            Process p = Process.Start("java.exe", "-jar " + padelProgramPath + "PaDEL-Descriptor.jar " + padelOption);
            p.WaitForExit();
            //Console.ReadKey();
        }

        public static void runPaDEL2(string workingDirectry, string sdfsDir, string padelDescriptortypes, string padelProgramPath)
        {
            var padelDescriptortypesPath = padelDescriptortypes;
            var outputSdfFolderPath = sdfsDir;
            var sdfFileName = Directory.GetFiles(outputSdfFolderPath);
            var outputFolderPath = sdfsDir + @"\PadelResult\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            var padelOutFile = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(sdfFileName[0]) + ".csv";
            var padelOption = " -dir " + outputSdfFolderPath + @" -file " + padelOutFile + " -2d -descriptortypes " + padelDescriptortypesPath + " -threads 4 -maxruntime 600000";

            Process p = Process.Start("java.exe", "-jar " + padelProgramPath + "PaDEL-Descriptor.jar " + padelOption);
            p.WaitForExit();
            //Console.ReadKey();
        }


        public static void selectDescriptor(string workingDirectry, string padelOutFileName, string descriptorSelecterRTFile, string descriptorSelecterCSSFile)
        {
            var outputFolderPath = workingDirectry;
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            var padelOutFile = padelOutFileName;
            var outFileName = Path.GetFileNameWithoutExtension(padelOutFileName);

            var rtSelectedDescriptorFile = outputFolderPath + "\\RTpred_" + outFileName + ".csv";
            var ccsSelectedDescriptorFile = outputFolderPath + "\\CCSpred_" + outFileName + ".csv";

            var descriptorSelecterRTList = new List<string>();
            var descriptorSelecterCSSList = new List<string>();

            var rtSelectedDescriptorHeader = "Name";
            var ccsSelectedDescriptorHeader = "Name,Mass";

            using (var sr = new StreamReader(descriptorSelecterRTFile, true))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    descriptorSelecterRTList.Add(line);
                    rtSelectedDescriptorHeader = rtSelectedDescriptorHeader + "," + line;
                }
            }
            using (var sr = new StreamReader(descriptorSelecterCSSFile, false))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    descriptorSelecterCSSList.Add(line);
                    ccsSelectedDescriptorHeader = ccsSelectedDescriptorHeader + "," + line;
                }
            }

            var rtDescriptorList = new List<List<string>>();
            var ccsDescriptorList = new List<List<string>>();
            var exactmassDic = new Dictionary<string, string>();

            using (var sr = new StreamReader(padelOutFile, false))
            {
                var line = sr.ReadLine();
                var keyArray = line.Split(',');
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    var lineArray = line.Split(',');
                    var descripterDicRt = new Dictionary<string, string>();
                    var descripterDicCcs = new Dictionary<string, string>();
                    var selectedRtList = new List<string>();
                    var selectedCcsList = new List<string>();
                    for (int i = 0; i < lineArray.Count(); i++)
                    {
                        if (i == 0)
                        {
                            selectedRtList.Add(lineArray[i].Trim('"'));
                            selectedCcsList.Add(lineArray[i].Trim('"'));
                        }
                        if (descriptorSelecterRTList.Contains(keyArray[i]))
                        {
                            descripterDicRt.Add(keyArray[i], lineArray[i]);
                        }
                        if (descriptorSelecterCSSList.Contains(keyArray[i]))
                        {
                            descripterDicCcs.Add(keyArray[i], lineArray[i]);
                        }
                    }
                    foreach (var desc in descriptorSelecterRTList)
                    {
                        selectedRtList.Add(descripterDicRt[desc]);
                    }
                    foreach (var desc in descriptorSelecterCSSList)
                    {
                        selectedCcsList.Add(descripterDicCcs[desc]);
                    }

                    rtDescriptorList.Add(selectedRtList);
                    ccsDescriptorList.Add(selectedCcsList);
                    if (!exactmassDic.ContainsKey(lineArray[0].Trim('"')))
                    {
                        exactmassDic.Add(lineArray[0].Trim('"'), descripterDicCcs["MW"]);
                    }
                }
            }

            using (var swRT = new StreamWriter(rtSelectedDescriptorFile, false))
            {
                rtSelectedDescriptorHeader = rtSelectedDescriptorHeader.Replace('-', '_');

                swRT.WriteLine(rtSelectedDescriptorHeader);
                foreach (var desc in rtDescriptorList)
                {
                    var descString = desc[0];
                    for (int i = 1; i < desc.Count; i++)
                    {
                        descString = descString + "," + desc[i];
                    }
                    swRT.WriteLine(descString);
                }
            }

            //var toExactmassFileDic = new Dictionary<string, string>();
            //using (var sr = new StreamReader(outputFolderPath + "\\InChIKey-Exactmass.txt", false))
            //{
            //    while (sr.Peek() > -1)
            //    {
            //        var line = sr.ReadLine();
            //        var lineArray = line.Split('\t');
            //        toExactmassFileDic.Add(lineArray[0], lineArray[1]);
            //    }
            //}

            //using (var swCcs = new StreamWriter(ccsSelectedDescriptorFile, false))
            //{
            //    ccsSelectedDescriptorHeader = ccsSelectedDescriptorHeader.Replace('-', '_');

            //    swCcs.WriteLine(ccsSelectedDescriptorHeader);
            //    foreach (var desc in ccsDescriptorList)
            //    {
            //        var descString = desc[0] + "," + exactmassDic[desc[0]];
            //        for (int i = 1; i < desc.Count; i++)
            //        {
            //            descString = descString + "," + desc[i];
            //        }
            //        swCcs.WriteLine(descString);
            //    }
            //}

            var adductList = adductDic.adductIonDic;
            foreach (var adduct in adductList)
            {
                var fileSurfix = adduct.Value.AdductSurfix + "_" + adduct.Value.IonMode.Substring(0, 3);
                var fileName = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(ccsSelectedDescriptorFile);
                using (var swCcs = new StreamWriter(fileName + "_" + fileSurfix + ".csv", false))
                {
                    ccsSelectedDescriptorHeader = ccsSelectedDescriptorHeader.Replace('-', '_');
                    var keyArray = ccsSelectedDescriptorHeader.Split(',');
                    var keyList = new List<string>(keyArray);
                    keyList.Insert(1, "AdductScore");
                    swCcs.WriteLine(string.Join(",", keyList));

                    foreach (var desc in ccsDescriptorList)
                    {
                        var descString = desc[0] + "," + adduct.Value.AdductIonMass.ToString() + "," + exactmassDic[desc[0]];
                        for (int i = 1; i < desc.Count; i++)
                        {
                            descString = descString + "," + desc[i];
                        }
                        swCcs.WriteLine(descString);
                    }

                    //using (var sr = new StreamReader(ccsSelectedDescriptorFile, false))
                    //{
                    //    var line = sr.ReadLine();
                    //    var keyArray = line.Split(',');
                    //    var keyList = new List<string>(keyArray);
                    //    keyList.Insert(1, "AdductScore");

                    //    while (sr.Peek() > -1)
                    //    {
                    //        line = sr.ReadLine();
                    //        var lineArray = line.Split(',');
                    //        var lineList = new List<string>(lineArray);
                    //        lineList.Insert(1, adduct.Value.AdductIonMass.ToString());
                    //        swCcs.WriteLine(string.Join(",", lineList));
                    //    }
                    //}
                }
            }
        }

        public static void mergeRtAndCcsResultFiles(string workingDirectry, string toPredictFile)
        {
            var outputFolderPath = workingDirectry + @"\predictResult\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            var toPredictFileName = Path.GetFileNameWithoutExtension(toPredictFile);
            var resultFile = outputFolderPath + "\\PredictedResult_" + toPredictFileName + ".txt";

            var toPredictFileDic = new Dictionary<string, string>();
            using (var sr = new StreamReader(toPredictFile, true))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == "" || line == "\0" || line.Contains("InChIKey"))
                    {
                        continue;
                    }
                    var lineArray = line.Split('\t');
                    if (!toPredictFileDic.ContainsKey(lineArray[0]))
                    {
                        toPredictFileDic.Add(lineArray[0], lineArray[1]);
                    }
                }
            }

            var allResultDic = new Dictionary<string, List<string>>();
            var rtResultDic = new Dictionary<string, string>();
            var readRtFileName = "RTpred_" + toPredictFileName + ".txt";

            using (
                var sr = new StreamReader(workingDirectry + readRtFileName, false))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    var lineArray = line.Split(' ');
                    if (!rtResultDic.ContainsKey(lineArray[1]))
                    {
                        rtResultDic.Add(lineArray[1], lineArray[2]);
                    }
                }
            }

            var ccsResultDic = new Dictionary<string, Dictionary<string, string>>();
            var ccsAdductHeaderList = new List<string>();

            var ccsResultFiles = new List<string>(Directory.GetFiles(workingDirectry, "CCSpred_" + toPredictFileName + "*.txt"));
            foreach (string file in ccsResultFiles)
            {
                var fileName = Path.GetFileNameWithoutExtension(file);
                var adductSurfixFind = toPredictFileName.Length + 9;
                var adductSurfix = fileName.Substring(adductSurfixFind, fileName.Length - adductSurfixFind);
                var adduct = adductDic.adductIonSurfixDic[adductSurfix].ToString();
                ccsAdductHeaderList.Add(adduct);

                using (var sr = new StreamReader(file, false))
                {
                    while (sr.Peek() > -1)
                    {
                        var line = sr.ReadLine();
                        var lineArray = line.Split(' ');
                        var checkAdductString = lineArray[1] + adduct;
                        if (!ccsResultDic.ContainsKey(lineArray[1]))
                        {
                            var adductCcs = new Dictionary<string, string>() { { adduct, lineArray[2] } };
                            if (!ccsResultDic.ContainsValue(adductCcs))
                            {
                                //adductCcs.Add(adduct, lineArray[2]);
                                ccsResultDic.Add(lineArray[1], adductCcs);
                            }
                        }
                        else
                        {
                            var adductCcs = ccsResultDic[lineArray[1]];
                            //var adductCcs = new Dictionary<string, string>() { { adduct, lineArray[2] } };
                            if (!adductCcs.ContainsKey(adduct))
                            {
                                //adductCcs.Add(adduct, lineArray[2]);
                                adductCcs.Add(adduct, lineArray[2]);
                                ccsResultDic.Remove(lineArray[1]);
                                ccsResultDic.Add(lineArray[1], adductCcs);
                            }
                        }

                    }
                }
            }

            using (var sw = new StreamWriter(resultFile, false, Encoding.ASCII))
            {
                using (var sr = new StreamReader(toPredictFile, true))
                {
                    var headerList = new List<string>();

                    headerList.Add("InChIKey");
                    headerList.Add("SMILES");
                    headerList.Add("RT");
                    headerList.AddRange(ccsAdductHeaderList);

                    sw.WriteLine(string.Join("\t", headerList));

                    while (sr.Peek() > -1)
                    {
                        var line = sr.ReadLine();
                        if (line == "" || line == "\0" || line.Contains("InChIKey")) { continue; }

                        var lineArray = line.Split('\t');

                        var resultLine = new List<string>();

                        resultLine.Add(lineArray[0]);
                        resultLine.Add(lineArray[1]);

                        var rtResultValue = "";
                        if (rtResultDic.ContainsKey(lineArray[0]))
                        {
                            rtResultValue = rtResultDic[lineArray[0]];
                        }
                        resultLine.Add(rtResultValue);

                        if (ccsResultDic.ContainsKey(lineArray[0]))
                        {
                            var cssResultValueList = new List<string>();
                            var cssResult = ccsResultDic[lineArray[0]];
                            foreach (var item in ccsAdductHeaderList)
                            {
                                cssResultValueList.Add(cssResult[item]);
                            }
                            resultLine.AddRange(cssResultValueList);
                        }

                        sw.WriteLine(string.Join("\t", resultLine));
                    }
                }

            }
        }

        public static void generateSdfsOnNCDK(string workingDirectry)
        {
            var files = Directory.GetFiles(workingDirectry);
            foreach (string file in files)
            {
                //var fileName = Path.GetFileName(file);
                smilesToSdfOnNCDK(workingDirectry, file);
            }
        }

        public static void runFoldersToPaDEL(string workingDirectry, string padelDescriptortypes, string padelProgramPath)
        {
            var directries = Directory.GetDirectories(workingDirectry);
            foreach (string directry in directries)
            {
                //var directryName = Path.GetDirectoryName(directry);
                runPaDEL2(workingDirectry, directry, padelDescriptortypes, padelProgramPath);
            }
        }

        public static void runFolderToFitting
            (string workingDirectry, string toPredictFileDirectry, string padelOutFileDirectry, string descriptorSelecerRTFile, string descriptorSelecerCSSFile,
            string rPath, string rScriptAvdModelPath, string rtModelingRdsFile, string ccsModelingRdsFile)
        {
            var files = Directory.GetFiles(padelOutFileDirectry);
            foreach (string file in files)
            {
                var fileName = Path.GetFileName(file);
                selectDescriptor(workingDirectry, file, descriptorSelecerRTFile, descriptorSelecerCSSFile);
            }

            RtCcsPredictOnR.runPredict(workingDirectry, rPath, rScriptAvdModelPath, rtModelingRdsFile, ccsModelingRdsFile);

            files = Directory.GetFiles(toPredictFileDirectry);

            foreach (string file in files)
            {
                var fileName = Path.GetFileName(file);
                mergeRtAndCcsResultFiles(workingDirectry, file);
            }
        }

    }
}
