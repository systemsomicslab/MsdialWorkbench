using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using RDotNet;
using CompMs.Common.MessagePack;
using CompMs.Common.Parser;
using CompMs.Common.Components;
using CompMs.Common.FormulaGenerator.Parser;
using System.Linq;
using CompMs.Common.Extension;

namespace CompMs.MspGenerator
{
    public class MergeRTandCCSintoMsp
    {
        public static void mergeRTandCCSintoMsp()
        {
            var mspFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\AAHFA_H_Neg.msp";
            var outputFolderPath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\result\";
            var calculatedRtFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\sdf_202006021827_PaDEL.txt";
            var calculatedCcsFilePath = @"D:\takahashi\desktop\Tsugawa-san_work\20200601RTprediction\sdf_202006021827_PaDEL.txt";

            var outputFilePath = outputFolderPath + "\\" + Path.GetFileNameWithoutExtension(mspFilePath) + "_converted.lbm2";

            Console.WriteLine("Loading the msp file.");

            var mspDB = MspFileParcer.MspFileReader(mspFilePath);
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
            using (var sr = new StreamReader(calculatedRtFilePath, true))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var linearray = line.Split(' ');
                    var inchikey = linearray[1];
                    var predictedRtString = linearray[2];
                    var predictedRt = -1.0F;
                    if (float.TryParse(predictedRtString, out predictedRt) && !inchikeyToPredictedRt.ContainsKey(inchikey))
                    {
                        inchikeyToPredictedRt[inchikey] = predictedRt;
                    }
                }
            }

            var inchikeyToPredictedCcs = new Dictionary<string, float>();
            using (var sr = new StreamReader(calculatedCcsFilePath, true))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) continue;
                    var linearray = line.Split(' ');
                    var inchikey = linearray[1];
                    var predictedCcsString = linearray[2];
                    var predictedCcs = -1.0F;
                    if (float.TryParse(predictedCcsString, out predictedCcs) && !inchikeyToPredictedCcs.ContainsKey(inchikey))
                    {
                        inchikeyToPredictedCcs[inchikey] = predictedCcs;
                    }
                }
            }


            foreach (var query in mspDB)
            {
                if (inchikeyToPredictedRt.ContainsKey(query.InChIKey))
                {
                    query.ChromXs = new ChromXs(inchikeyToPredictedRt[query.InChIKey], ChromXType.RT, ChromXUnit.Min);
                }
                else
                {
                    Console.WriteLine("Error at {0}", query.InChIKey);
                }

                if (inchikeyToPredictedCcs.ContainsKey(query.InChIKey))
                {
                    query.CollisionCrossSection = inchikeyToPredictedCcs[query.InChIKey];
                }
                else
                {
                    Console.WriteLine("Error at {0}", query.InChIKey);
                }
            }

            MoleculeMsRefMethods.SaveMspToFile(mspDB, outputFilePath);
        }

    }
}
