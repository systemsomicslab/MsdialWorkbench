using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.Common.Components;
using CompMs.Common.FormulaGenerator.Parser;
using System.Linq;
using CompMs.Common.Extension;
using System.Collections.Specialized;
using Rfx.Riken.OsakaUniv;

namespace CompMs.MspGenerator
{
    public class MergeRTandCCSintoMsp
    {
        public static void generateDicOfPredict(string predictedFilesDirectry,string dbFileName)
        {
            var predictedList = new List<string>();
            var headerLine = "";
            var predictedFileList = new List<string>(Directory.GetFiles(predictedFilesDirectry));

            if(File.Exists(dbFileName))
            {
                using (var sr = new StreamReader(dbFileName, false))
                {
                    headerLine = sr.ReadLine();
                    while (sr.Peek() > -1)
                    {
                        var line = sr.ReadLine();
                        if (line == null || line.Contains("InChIKey")) { continue; }
                        var lineArray = line.Split('\t');
                        if (lineArray.Length < 11) { continue; }
                        predictedList.Add(line);
                    }
                }
            }
            else
            {
                File.Create(dbFileName).Close();
            }

            foreach (var predictedFile in predictedFileList)
            {
                using (var sr = new StreamReader(predictedFile, false))
                {
                    headerLine = sr.ReadLine();
                    while (sr.Peek() > -1)
                    {
                        var line = sr.ReadLine();
                        if(line == null || line.Contains("InChIKey")) { continue; }
                        var lineArray = line.Split('\t');
                        if(lineArray.Length < 11) { continue; }
                        predictedList.Add(line);
                    }
                }
            }
            predictedList = predictedList.Distinct().ToList();

            using (var sw = new StreamWriter(dbFileName,false,Encoding.ASCII) )
            {
                sw.WriteLine(headerLine);
                foreach(var item in predictedList)
                {
                    sw.WriteLine(item);
                }
            }

        }

        public static void generateInchikeyAndSmilesListFromMsp(string mspFilePath)
        {
            var outputFilePath = Path.GetDirectoryName(mspFilePath) + "\\" + Path.GetFileNameWithoutExtension(mspFilePath) + "_InChIKey-SMILES.txt";
            var mspDB = MspFileParser.MspFileReader(mspFilePath);
            var inchikeyToSmiles = new Dictionary<string, string>();
            foreach (var query in mspDB)
            {
                if (!inchikeyToSmiles.ContainsKey(query.InChIKey))
                {
                    inchikeyToSmiles[query.InChIKey] = query.SMILES;
                }
            }
            using (var sw = new StreamWriter(outputFilePath,false,Encoding.ASCII))
            {
                sw.WriteLine("InChIKey\tSMILES");
                foreach(var item in inchikeyToSmiles) 
                {
                    sw.WriteLine(item.Key + "\t" + item.Value);
                }
            }

        }

        public static void mergeRTandCCSintoMsp(string mspFilePath , string calculatedFilePath, string outputFolderPath)
        {
            var outputFileName = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(mspFilePath) + "_converted.lbm2";
            var outputFileNameDev = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(mspFilePath) + "_converted_dev.lbm2";

            Console.WriteLine("Loading the msp file.");

            var mspDB = MspFileParser.MspFileReader(mspFilePath);
            var mspDB2 =  MspFileParcer.MspFileReader(mspFilePath);
            var inchikeyToSmiles = new Dictionary<string, string>();
            foreach (var query in mspDB)
            {
                if (!inchikeyToSmiles.ContainsKey(query.InChIKey))
                {
                    inchikeyToSmiles[query.InChIKey] = query.SMILES;
                }
            }

            var tempCsvFilePath = outputFolderPath + "\\" + "temp.csv";
            var counter = 0;
            using (var sw = new StreamWriter(tempCsvFilePath, false, Encoding.ASCII))
            {
                sw.WriteLine("Name,InChIKey,SMILES");
                foreach (var pair in inchikeyToSmiles)
                {
                    sw.WriteLine("ID_" + counter + "," + pair.Key + "," + pair.Value);
                    counter++;
                }
            }

            var inchikeyToPredictedRt = new Dictionary<string, float>();
            using (var sr = new StreamReader(calculatedFilePath, true))
            {
                var line = sr.ReadLine();
                var lineArray = line.Split('\t');
                while (sr.Peek() > -1)
                {
                    line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    lineArray = line.Split('\t');
                    var inchikey = lineArray[0];
                    var predictedRtString = lineArray[2];
                    var predictedRt = -1.0F;
                    if (float.TryParse(predictedRtString, out predictedRt) && !inchikeyToPredictedRt.ContainsKey(inchikey))
                    {
                        inchikeyToPredictedRt[inchikey] = predictedRt;
                    }
                }
            }

            var inchikeyToPredictedCcs = new Dictionary<string, Dictionary<string, string>>();

            using (var sr = new StreamReader(calculatedFilePath, true))
            {
                var header = sr.ReadLine();
                var headerArray = header.Split('\t');
                var adduct = new List<string>();
                foreach (string str in headerArray)
                {
                    adduct.Add(str);
                }

                while (sr.Peek() > -1)
                {
                    var adductAndCcs = new Dictionary<string, string>();
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var lineArray = line.Split('\t');
                    var inchikey = lineArray[0];
                    for (int i=2; i < headerArray.Count();i++)
                    {
                        adductAndCcs.Add(adduct[i], lineArray[i]);
                    }

                    if (!inchikeyToPredictedCcs.ContainsKey(lineArray[0]))
                    {
                        inchikeyToPredictedCcs.Add(inchikey, adductAndCcs);
                    }
                }
            }

            var errCount = 0;
            var errList = new List<string>();
            foreach (var query in mspDB)
            {
                if(query.InChIKey=="" || query.InChIKey ==null)
                {
                    continue;
                }

                if (inchikeyToPredictedRt.ContainsKey(query.InChIKey))
                {
                    if(inchikeyToPredictedRt[query.InChIKey] == 0) 
                    {
                        continue; 
                    }
                    query.ChromXs = new ChromXs(inchikeyToPredictedRt[query.InChIKey], ChromXType.RT, ChromXUnit.Min);
                }
                else
                {
                    errCount = errCount + 1;
                    errList.Add(query.InChIKey+"\t"+ query.SMILES);
                    Console.WriteLine("Error at {0}", query.InChIKey);
                }

                if (inchikeyToPredictedCcs.ContainsKey(query.InChIKey))
                {
                    var CCSs = inchikeyToPredictedCcs[query.InChIKey];
                    if (CCSs.ContainsKey(query.AdductType.AdductIonName))
                    {
                        var adductCCS = CCSs[query.AdductType.AdductIonName];
                        if (adductCCS == "" || adductCCS == "0") { continue; }
                        query.CollisionCrossSection = double.Parse(adductCCS);
                    }
                }
                else
                {
                    errCount = errCount + 1;
                    errList.Add(query.InChIKey + "\t" + query.SMILES);
                    Console.WriteLine("Error at {0}", query.InChIKey);
                }
            }

            if (errCount > 0)
            {
                var tempCsvFilePath2 = outputFolderPath + "\\" + "temp2.txt";
                errList = errList.Distinct().ToList();

                using (var sw = new StreamWriter(tempCsvFilePath2, false, Encoding.ASCII))
                {
                    sw.WriteLine("InChIKey\tSMILES");
                    foreach (var item in errList)
                    {
                        sw.WriteLine(item);
                    }
                }

                Console.WriteLine("empty parameters found...see temp2.txt");
                Console.ReadKey();
            }
            else
            {
                MoleculeMsRefMethods.SaveMspToFile(mspDB, outputFileNameDev);
                MspMethods.SaveMspToFile(mspDB2, outputFileName);
            }
        }

        public static string getInchikeyFirstHalf(string inchikey)
        {
            var inchikeyHalf = "";
            if (inchikey.Length>0)
            {
                inchikeyHalf = inchikey.Substring(0, 14);
            }

            return inchikeyHalf;
        }

    }
}
