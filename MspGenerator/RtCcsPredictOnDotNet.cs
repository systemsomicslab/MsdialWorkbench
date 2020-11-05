using CompMs.StructureFinder.NcdkDescriptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using XGBoost;
using NCDK.Smiles;

namespace CompMs.MspGenerator
{
    public class RtCcsPredictOnDotNet
    {
        public static void GenerateQsarDescriptorFile(string inputFile, string outputFile)
        {
            var SmilesParser = new SmilesParser();

            var allDescriptorResultDic = NcdkDescriptor.GenerateAllNCDKDescriptors("O=C(O)CCCCC");// Header取得のためのDummy

            var allDescriptorHeader = new List<string>();
            foreach (var item in allDescriptorResultDic)
            {
                if (item.Key == "geomShape") { continue; }

                allDescriptorHeader.Add(item.Key);
            }

            using (var sw = new StreamWriter(outputFile, false, Encoding.ASCII))
            {
                using (var sr = new StreamReader(inputFile, true))
                {
                    var headerLine = sr.ReadLine();
                    var headerArray = headerLine.ToUpper().Split('\t');
                    int InChIKey = Array.IndexOf(headerArray, "INCHIKEY");
                    int SMILES = Array.IndexOf(headerArray, "SMILES");

                    sw.Write(headerLine);
                    sw.Write("\t");
                    sw.WriteLine(string.Join("\t", allDescriptorHeader));

                    var line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        if (line.Contains("SMILES")) { continue; }
                        var lineArray = line.Split('\t');
                        var inchikey = lineArray[InChIKey];
                        var rawSmiles = lineArray[SMILES];

                        var iAtomContainer = SmilesParser.ParseSmiles(rawSmiles);
                        allDescriptorResultDic = new Dictionary<string, double>(NcdkDescriptor.GenerateAllNCDKDescriptors(rawSmiles));
                        var allDescriptorResult = new List<string>();
                        foreach (var item in allDescriptorHeader)
                        {
                            if (item == "geomShape") { continue; }

                            if (!allDescriptorResultDic.ContainsKey(item))
                            {
                                allDescriptorResult.Add("NA");
                            }
                            else
                            {
                                allDescriptorResult.Add(allDescriptorResultDic[item].ToString());
                            }
                        }
                        sw.Write(line);
                        sw.Write("\t");
                        sw.WriteLine(string.Join("\t", allDescriptorResult));
                    }
                }
            }
        }



        public static void mergeRtAndCcsResultFiles2(string workingDirectry, string outFile, string rtTrainFile, string rtTestFile, string ccsTrainFile, string ccsTestFile)
        {
            var outputFolderPath = workingDirectry + @"\predictResult\";
            if (!Directory.Exists(outputFolderPath))
            {
                Directory.CreateDirectory(outputFolderPath);
            }

            var resultFile = outputFolderPath + "\\" + outFile;

            var allResultDic = new Dictionary<string, List<string>>();
            var rtResultDic = RtPredictionOnXgboost(workingDirectry + rtTrainFile, workingDirectry + rtTestFile);
            var ccsResultDic = CcsPredictionOnXgboost(workingDirectry + ccsTrainFile, workingDirectry + ccsTestFile);

            var ccsAdductHeaderList = new List<string>();
            foreach (var item in adductscoreDic)
            {
                ccsAdductHeaderList.Add(item.Key);
            }

            using (var sw = new StreamWriter(resultFile, false, Encoding.ASCII))
            {
                var headerList = new List<string>();

                headerList.Add("InChIKey");
                headerList.Add("SMILES");
                headerList.Add("RT");
                headerList.AddRange(ccsAdductHeaderList);
                var headerItem = string.Join("\t", headerList);
                sw.WriteLine(headerItem);

                foreach (var rtItem in rtResultDic)
                {
                    var writeLineItem = new List<string>();
                    writeLineItem.Add(rtItem.Key);
                    writeLineItem.Add(rtItem.Value.ToString());
                    // add CCS result 
                    if (ccsResultDic.ContainsKey(rtItem.Key))
                    {
                        var ccsResultValueList = new List<string>();
                        var ccsResult = ccsResultDic[rtItem.Key];
                        foreach (var adduct in ccsAdductHeaderList)
                        {
                            writeLineItem.Add(ccsResult[adduct].ToString());
                        }
                    }
                    sw.WriteLine(string.Join("\t", writeLineItem));
                }
            }

            using (var sw = new StreamWriter(resultFile+"CCS.txt", false, Encoding.ASCII))
            {
                var headerList = new List<string>();

                headerList.Add("InChIKey");
                headerList.Add("SMILES");
                headerList.Add("RT");
                headerList.AddRange(ccsAdductHeaderList);
                var headerItem = string.Join("\t", headerList);
                sw.WriteLine(headerItem);

                //to test code
                foreach (var ccsItem in ccsResultDic)
                {
                    var writeLineItem = new List<string>();
                    writeLineItem.Add(ccsItem.Key);
                    writeLineItem.Add(ccsItem.Value.ToString());
                    // add CCS result 
                    if (ccsResultDic.ContainsKey(ccsItem.Key))
                    {
                        var ccsResultValueList = new List<string>();
                        var ccsResult = ccsResultDic[ccsItem.Key];
                        foreach (var adduct in ccsAdductHeaderList)
                        {
                            writeLineItem.Add(ccsResult[adduct].ToString());
                        }
                    }
                    sw.WriteLine(string.Join("\t", writeLineItem));
                }
            }

        }

        public static Dictionary<string, float> RtPredictionOnXgboost(string rtTrainFile, string testFile)
        {
            // read trainFile and set to array
            var vectorsTrain = new List<XGVector<Array>>();
            var inchikeyList = new List<string>();

            using (var sr = new StreamReader(rtTrainFile, true))
            {
                var headerLine = sr.ReadLine();
                headerLine = headerLine.Replace("-", "_");
                headerLine = headerLine.Replace(".", "_");
                headerLine = headerLine.Replace("*", "_asterisk");

                var headerArray = headerLine.Split('\t');
                var headerArrayUpper = headerLine.ToUpper().Split('\t');
                int smilesOrder = Array.IndexOf(headerArrayUpper, "SMILES");

                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    for (var i = smilesOrder + 1; i < lineArray.Length; i++)
                    {
                        var item = float.Parse(lineArray[i]);
                        if (headerArray[i] == "RT")
                        {
                            target = item;
                        }
                        else
                        {
                            itemlist.Add(item);
                        }
                    }
                    XGVector<Array> newVector = new XGVector<Array>();
                    float[] recordsList = (float[])itemlist.ToArray();
                    //newVector.Original = recordsList;
                    newVector.Features = recordsList;
                    newVector.Target = target;
                    vectorsTrain.Add(newVector);
                }
            }

            // read testFile and set to array
            var testArrayList = new List<XGBArray>();
            var vectorsTest = new List<XGVector<Array>>();

            using (var sr = new StreamReader(testFile, true))
            {
                var headerLine = sr.ReadLine();
                headerLine = headerLine.Replace("-", "_");
                headerLine = headerLine.Replace(".", "_");
                headerLine = headerLine.Replace("*", "_asterisk");

                var headerArray = headerLine.Split('\t');
                var headerArrayUpper = headerLine.ToUpper().Split('\t');

                int smilesOrder = Array.IndexOf(headerArrayUpper, "SMILES");
                int inchikeyOrder = Array.IndexOf(headerArrayUpper, "INCHIKEY");


                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;

                    inchikeyList.Add(lineArray[inchikeyOrder] + '\t' + lineArray[smilesOrder]);

                    for (var i = smilesOrder + 1; i < lineArray.Length; i++)
                    {
                        var item = float.Parse(lineArray[i]);
                        if (headerArray[i] == "RT")
                        {
                            target = item;
                        }
                        else
                        {
                            itemlist.Add(item);
                        }


                    }
                    XGVector<Array> newVector = new XGVector<Array>();
                    float[] recordsList = (float[])itemlist.ToArray();
                    //newVector.Original = recordsList;
                    newVector.Features = recordsList;
                    newVector.Target = target;
                    vectorsTest.Add(newVector);
                }
            }

            var xgbc = new XGBoost.XGBRegressor();
            XGBArray arrTrain = ConvertToXGBArray(vectorsTrain);
            xgbc.Fit(arrTrain.Vectors, arrTrain.Target);


            XGBArray arrTest = ConvertToXGBArray(vectorsTest);
            var outcomeTest = xgbc.Predict(arrTest.Vectors);

            var rtDic = new Dictionary<string, float>();
            for (int i = 0; i < inchikeyList.Count; i++)
            {
                rtDic.Add(inchikeyList[i], outcomeTest[i]);
            }

            //Console.ReadKey();
            return rtDic;

        }


        public static Dictionary<string, Dictionary<string, float>> CcsPredictionOnXgboost(string rtTrainFile, string testFile)
        {
            // read trainFile and set to array
            var trainArrayList = new List<XGBArray>();
            var vectorsTrain = new List<XGVector<Array>>();
            var inchikeyList = new List<string>();

            using (var sr = new StreamReader(rtTrainFile, true))
            {
                var headerLine = sr.ReadLine();
                headerLine = headerLine.Replace("-", "_");
                headerLine = headerLine.Replace(".", "_");
                headerLine = headerLine.Replace("*", "_asterisk");

                var headerArray = headerLine.Split('\t');
                var headerArrayUpper = headerLine.ToUpper().Split('\t');
                int smilesOrder = Array.IndexOf(headerArrayUpper, "SMILES");

                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    for (var i = smilesOrder + 1; i < lineArray.Length; i++)
                    {
                        var item = float.Parse(lineArray[i]);
                        if (headerArray[i] == "CCS")
                        {
                            target = item;
                        }
                        else
                        {
                            itemlist.Add(item);
                        }
                    }
                    XGVector<Array> newVector = new XGVector<Array>();
                    float[] recordsList = (float[])itemlist.ToArray();
                    newVector.Original = recordsList;
                    newVector.Features = recordsList;
                    newVector.Target = target;
                    vectorsTrain.Add(newVector);
                }
            }

            // read testFile and set to array
            var testArrayList = new List<XGBArray>();
            var vectorsTest = new List<XGVector<Array>>();

            using (var sr = new StreamReader(testFile, true))
            {
                var headerLine = sr.ReadLine();
                headerLine = headerLine.Replace("-", "_");
                headerLine = headerLine.Replace(".", "_");
                headerLine = headerLine.Replace("*", "_asterisk");

                var headerArray = headerLine.Split('\t');
                var headerArrayUpper = headerLine.ToUpper().Split('\t');

                int smilesOrder = Array.IndexOf(headerArray, "SMILES");
                int inchikeyOrder = Array.IndexOf(headerArrayUpper, "INCHIKEY");


                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;

                    inchikeyList.Add(lineArray[inchikeyOrder] + '\t' + lineArray[smilesOrder]);

                    for (var i = smilesOrder + 1; i < lineArray.Length; i++)
                    {
                        var item = float.Parse(lineArray[i]);
                        if (headerArray[i] == "AdductScore")
                        {
                            continue;
                        }
                        if (headerArray[i] == "CCS")
                        {
                            target = item;
                        }
                        else
                        {
                            itemlist.Add(item);
                        }

                    }
                    foreach (var adductscore in adductscoreDic)
                    {
                        XGVector<Array> newVector = new XGVector<Array>();
                        var itemlist2 = new List<float>(itemlist);
                        itemlist2.Insert(0, adductscore.Value);
                        float[] recordsList = (float[])itemlist2.ToArray();
                        newVector.Original = recordsList;
                        newVector.Features = recordsList;
                        newVector.Target = target;
                        vectorsTest.Add(newVector);

                    }
                }
            }

            var xgbc = new XGBoost.XGBRegressor();
            XGBArray arrTrain = ConvertToXGBArray(vectorsTrain);
            xgbc.Fit(arrTrain.Vectors, arrTrain.Target);


            XGBArray arrTest = ConvertToXGBArray(vectorsTest);
            var outcomeTest = xgbc.Predict(arrTest.Vectors);

            var ccsResultDic = new Dictionary<string, Dictionary<string, float>>();
            var count = 0;

            for (int i = 0; i < inchikeyList.Count; i++)
            {
                var ccsAdductResult = new Dictionary<string, float>();
                foreach (var item in adductscoreDic)
                {
                    ccsAdductResult.Add(item.Key, outcomeTest[count]);
                    count = count + 1;
                }

                ccsResultDic.Add(inchikeyList[i], ccsAdductResult);
            }

            //Console.ReadKey();
            return ccsResultDic;
        }

        public static XGBArray ConvertToXGBArray(List<XGVector<Array>> vectorsTrain)
        {
            var arr = new XGBArray();
            arr.Target = vectorsTrain.Select(v => v.Target).ToArray();
            arr.Vectors = vectorsTrain.Select(v => v.Features).ToArray();
            return arr;
        }

        public static Dictionary<string, float>
            adductscoreDic = new Dictionary<string, float>()
            {
            //  "[M]+" "[M+H]+" "[M+NH4]+" "[M+Na]+"  "[M-H]-"  "[M+HCOO]-"  "[M+CH3COO]-" "[M+H-H2O]+" "[M-2H]2-"
                { "[M]+", -0.00054858f },
                { "[M+H]+",1.00727642f},
                { "[M+NH4]+",18.03382555f},
                { "[M+Na]+",22.9892207f},
                { "[M-H]-" ,-1.00727642f},
                { "[M+HCOO]-",44.99820285f},
                {"[M+CH3COO]-" ,59.01385292f},
                {"[M+H-H2O]+" ,-17.00328358f},
                {"[M-2H]2-" ,-1.00727642f}
            };
        public class XGVector<T>
        {
            /// <summary>
            /// The original object
            /// </summary>
            public T Original { get; set; }
            /// <summary>
            /// Attributes of the feature vector
            /// </summary>
            public float[] Features { get; set; }
            public float Target { get; set; }
        }
        public class XGBArray
        {
            public float[][] Vectors { get; set; }
            public float[] Target { get; set; }
        }

    }
}
