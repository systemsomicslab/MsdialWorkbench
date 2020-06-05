using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text;
using MathNet.Numerics.Interpolation;
using NCDK;
using NCDK.IO;
using NCDK.IO.Listener;
using NCDK.QSAR.Descriptors.Moleculars;
using NCDK.Smiles;
using NCDK.Tools.Manipulator;
using NCDK.Graphs.InChI;



namespace CompMs.MspGenerator
{
    public class RtCcsPredictManager
    {
        public static void smilesToSdfOnNCDK(string workingDirectry, string toPredictFile)
        {
            var toPredictFilePath = workingDirectry + toPredictFile;
            var outputSdfFolderPath = workingDirectry + @"\sdf\";
            if (!Directory.Exists(outputSdfFolderPath))
            {
                Directory.CreateDirectory(outputSdfFolderPath);
            }
            var sdfFile = outputSdfFolderPath + "\\output.sdf";
            var outputFolderPath = workingDirectry + @"\result\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            var SmilesParser = new SmilesParser();
            var iAtomContList = new List<IAtomContainer>();
            //var coordinate2D = new NCDK.Layout.StructureDiagramGenerator();

            using (var sw = new StreamWriter(outputFolderPath + "\\InChIKey-Exactmass.txt", false))
            {
                using (var sr = new StreamReader(toPredictFilePath, true))
                {
                    var line = sr.ReadLine();
                    while (sr.Peek() > -1)
                    {
                        line = sr.ReadLine();
                        if (line == string.Empty) continue;
                        var linearray = line.Split(',');
                        var inchikey = linearray[0];
                        var smiles = linearray[1];
                        var iAtomCont = SmilesParser.ParseSmiles(smiles);
                        iAtomCont.Title = inchikey;
                        //coordinate2D.GenerateCoordinates(iAtomCont); // 2D座標計算 これは無くてもOK
                        iAtomContList.Add(iAtomCont);

                        var iMolecularFormula = MolecularFormulaManipulator.GetMolecularFormula(iAtomCont);
                        var exactMass = MolecularFormulaManipulator.GetMass(iMolecularFormula, MolecularWeightTypes.MonoIsotopic);
                        sw.WriteLine(inchikey + "\\t" + exactMass);
                    }
                }
            }

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

        public static void runPaDEL(string workingDirectry,string padelDescriptortypes, string padelProgramPath)
        {
            //var padelProgramPath = @"F:\takahashi\RTprediction\New_RT_pipeline\PaDEL-Descriptor\";
            var padelDescriptortypesPath = workingDirectry + padelDescriptortypes;
            var outputSdfFolderPath = workingDirectry + @"\sdf\";
            var outputFolderPath = workingDirectry + @"\result\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }
            var padelOutFile = outputFolderPath + "\\compounds_paDELoutAll.csv";
            var padelOption = " -dir " + outputSdfFolderPath + @" -file " + padelOutFile + " -2d -descriptortypes " + padelDescriptortypesPath + " -retainorder";

            Process.Start("java.exe", "-jar " + padelProgramPath + "PaDEL-Descriptor.jar " + padelOption);
            //Console.ReadKey();
        }

        public static void selectDescriptor(string workingDirectry, string descriptorSelecerRTFile, string descriptorSelecerCSSFile)
        {
            var outputFolderPath = workingDirectry + @"\result\";
            var padelOutFile = outputFolderPath + "\\compounds_paDELoutAll.csv";
            descriptorSelecerRTFile = workingDirectry + descriptorSelecerRTFile;
            descriptorSelecerCSSFile = workingDirectry + descriptorSelecerCSSFile;

            var rtSelectedDescriptorFile = outputFolderPath + "\\compounds_paDELoutRT.csv";
            var ccsSelectedDescriptorFile = outputFolderPath + "\\compounds_paDELoutCcs.csv";


            var descriptorSelecterRTList = new List<string>();
            var descriptorSelecterCSSList = new List<string>();

            var rtSelectedDescriptorHeader = "Name";
            var ccsSelectedDescriptorHeader = "Name,Exactmass";

            using (var sr = new StreamReader(descriptorSelecerRTFile, false))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    descriptorSelecterRTList.Add(line);
                    rtSelectedDescriptorHeader = rtSelectedDescriptorHeader + "," + line;
                }
            }
            using (var sr = new StreamReader(descriptorSelecerCSSFile, false))
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

            using (var sr = new StreamReader(padelOutFile, true))
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
                    foreach(var desc in descriptorSelecterRTList)
                    {
                        selectedRtList.Add(descripterDicRt[desc]);
                    }
                    foreach (var desc in descriptorSelecterCSSList)
                    {
                        selectedCcsList.Add(descripterDicCcs[desc]);
                    }

                    rtDescriptorList.Add(selectedRtList);
                    ccsDescriptorList.Add(selectedCcsList);
                }
            }

            using (var swRT = new StreamWriter(rtSelectedDescriptorFile, false))
            {
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

            var toExactmassFileDic = new Dictionary<string, string>();
            using (var sr = new StreamReader(outputFolderPath + "\\InChIKey-Exactmass.txt", false))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    var lineArray = line.Split("\\t");
                    toExactmassFileDic.Add(lineArray[0], lineArray[1]);
                }
            }

            using (var swCcs = new StreamWriter(ccsSelectedDescriptorFile, false))
            {
                swCcs.WriteLine(ccsSelectedDescriptorHeader);
                foreach (var desc in ccsDescriptorList)
                {
                    var descString = desc[0] + "," + toExactmassFileDic[desc[0]];
                    for (int i = 1; i < desc.Count; i++)
                    {
                        descString = descString + "," + desc[i];
                    }
                    swCcs.WriteLine(descString);
                }
            }

            var adductList = adductDic.adductIonDic;
            foreach (var adduct in adductList)
            {
                var fileSurfix = adduct.Value.AdductSurfix + "_" + adduct.Value.IonMode.Substring(0, 3);
                using (var swCcs = new StreamWriter(outputFolderPath + "\\paDELoutCcs_" + fileSurfix + ".csv", false))
                {

                    using (var sr = new StreamReader(ccsSelectedDescriptorFile, false))
                    {
                        var line = sr.ReadLine();
                        var keyArray = line.Split(',');
                        var keyList = new List<string>(keyArray);
                        keyList.Insert(1, "AdductScore");
                        swCcs.WriteLine(string.Join(",", keyList)) ;

                        while (sr.Peek() > -1)
                        {
                            line = sr.ReadLine();
                            var lineArray = line.Split(",");
                            var lineList = new List<string>(lineArray);
                            lineList.Insert(1, adduct.Value.AdductIonMass.ToString());
                            swCcs.WriteLine(string.Join(",", lineList));
                        }
                    }

                }
            }
        }

        public static void mergeDescriptorAndSmiles(string workingDirectry, string toPredictFile)
        {
            var toPredictFilePath = workingDirectry + toPredictFile;
            var outputFolderPath = workingDirectry + @"\result\";


            var toPredictFileDic = new Dictionary<string, string>();
            using (var sr = new StreamReader(toPredictFilePath, false))
            {
                var line = sr.ReadLine();

                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    var lineArray = line.Split("\\t");
                    toPredictFileDic.Add(lineArray[0], lineArray[1]);
                }
            }

        }

    }
}
