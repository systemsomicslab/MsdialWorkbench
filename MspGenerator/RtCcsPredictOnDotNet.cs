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



        public static void mergeRtAndCcsResultFiles2(string resultFile, string rtTrainFile, string rtTestFile, string ccsTrainFile, string ccsTestFile)
        {

            var allResultDic = new Dictionary<string, List<string>>();
            var rtResultDic = RtPredictionOnXgboost(rtTrainFile, rtTestFile);
            var ccsResultDic = CcsPredictionOnXgboost(ccsTrainFile, ccsTestFile);

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
                // to print ccs
                foreach (var ccsItem in ccsResultDic)
                {
                    var writeLineItem = new List<string>();
                    writeLineItem.Add(ccsItem.Key);

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
                int descriptorStartOrder = Array.IndexOf(headerArrayUpper, "RT");

                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    for (var i = descriptorStartOrder; i < lineArray.Length; i++)
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

                int descriptorStartOrder = Math.Max(smilesOrder, inchikeyOrder) + 1;

                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    if (smilesOrder > -1)
                    {
                        inchikeyList.Add(lineArray[inchikeyOrder] + '\t' + lineArray[smilesOrder]);
                    }
                    else
                    {
                        inchikeyList.Add(lineArray[inchikeyOrder]);
                    }

                    for (var i = descriptorStartOrder; i < lineArray.Length; i++)
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
            int maxDepth = 5; //default=3; use tune result
            float learningRate = 0.02F;
            int nEstimators = 1000;
            bool silent = true;
            string objective = "reg:linear";
            int nThread = -1;
            float gamma = 1; // default=0 on R 
            int minChildWeight = 10; // default=1 on R 
            int maxDeltaStep = 0;
            float subsample = 0.5F; // default=1 on R 
            float colSampleByTree = 1; // default=1 on R 
            float colSampleByLevel = 1; 
            float regAlpha = 0; // default=0 on R 
            float regLambda = 1; // default=1 on R 
            float scalePosWeight = 1;
            float baseScore = 0.5F;
            int seed = 0;
            float missing = float.NaN;

            var xgbc = new XGBoost.XGBRegressor(maxDepth, learningRate, nEstimators, silent,
                objective, nThread, gamma, minChildWeight, maxDeltaStep, subsample, colSampleByTree,
                colSampleByLevel, regAlpha, regLambda, scalePosWeight, baseScore, seed, missing);
            XGBArray arrTrain = ConvertToXGBArray(vectorsTrain);
            xgbc.Fit(arrTrain.Vectors, arrTrain.Target);


            XGBArray arrTest = ConvertToXGBArray(vectorsTest);
            var outcomeTest = xgbc.Predict(arrTest.Vectors);

            var rtDic = new Dictionary<string, float>();
            for (int i = 0; i < inchikeyList.Count; i++)
            {
                if (rtDic.ContainsKey(inchikeyList[i])) { continue; };
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
                int descriptorStartOrder = Array.IndexOf(headerArrayUpper, "CCS");

                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    for (var i = descriptorStartOrder; i < lineArray.Length; i++)
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

                int smilesOrder = Array.IndexOf(headerArrayUpper, "SMILES");
                int inchikeyOrder = Array.IndexOf(headerArrayUpper, "INCHIKEY");


                var line = "";
                while ((line = sr.ReadLine()) != null)
                {
                    var lineArray = line.Split('\t');
                    var itemlist = new List<float>();
                    var target = 0.0f;
                    int descriptorStartOrder = Math.Max(smilesOrder, inchikeyOrder) + 1;

                    if (smilesOrder > -1)
                    {
                        inchikeyList.Add(lineArray[inchikeyOrder] + '\t' + lineArray[smilesOrder]);
                    }
                    else
                    {
                        inchikeyList.Add(lineArray[inchikeyOrder]);
                    }

                    for (var i = descriptorStartOrder; i < lineArray.Length; i++)
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

            int maxDepth = 5; //default=3; use tune result
            float learningRate = 0.025F;
            int nEstimators = 700;
            bool silent = true;
            string objective = "reg:linear";
            int nThread = -1;
            float gamma = 0.01F;
            int minChildWeight = 0;
            int maxDeltaStep = 0;
            float subsample = 0.5F;
            float colSampleByTree = 0.75F;
            float colSampleByLevel = 1;
            float regAlpha = 0;
            float regLambda = 1;// default=1 on R 
            float scalePosWeight = 1;
            float baseScore = 0.5F;
            int seed = 0;
            float missing = float.NaN;

            var xgbc = new XGBoost.XGBRegressor(maxDepth , learningRate, nEstimators, silent, 
                objective , nThread, gamma , minChildWeight, maxDeltaStep, subsample, colSampleByTree,
                colSampleByLevel, regAlpha, regLambda, scalePosWeight, baseScore , seed , missing );
            XGBArray arrTrain = ConvertToXGBArray(vectorsTrain);
            xgbc.Fit(arrTrain.Vectors, arrTrain.Target);


            XGBArray arrTest = ConvertToXGBArray(vectorsTest);
            var outcomeTest = xgbc.Predict(arrTest.Vectors);

            var ccsResultDic = new Dictionary<string, Dictionary<string, float>>();
            var count = 0;

            for (int i = 0; i < inchikeyList.Count; i++)
            {
                if (ccsResultDic.ContainsKey(inchikeyList[i])) { continue; };
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

        public static void ExtractDescriptorToPredict(string descriptorFile, string descriptorListFile)
        {
            var descriptorList = new List<string>();
            var newFileName = Path.GetDirectoryName(descriptorFile) + "\\" + Path.GetFileNameWithoutExtension(descriptorFile) + "_Extracted.tsv";
            using (var srList = new StreamReader(descriptorListFile, true))
            {
                var headerLine = srList.ReadLine();
                var line = "";
                while ((line = srList.ReadLine()) != null)
                {
                    var lineArray = line.Split(' ');
                    descriptorList.Add(lineArray[1]);
                }
                descriptorList.Add("RT");
                descriptorList.Add("CCS");
                descriptorList.Add("AdductScore");

            }

            using (var sw = new StreamWriter(newFileName, false, Encoding.ASCII))
            {
                using (var sr = new StreamReader(descriptorFile, true))
                {
                    var headerLine = sr.ReadLine();
                    headerLine = headerLine.Replace("-", ".");
                    headerLine = headerLine.Replace("*", ".");

                    var headerArray = headerLine.Split('\t');
                    var headerArrayUpper = headerLine.ToUpper().Split('\t');
                    var descriptorStartOrder = Math.Max(Array.IndexOf(headerArrayUpper, "INCHIKEY"), Array.IndexOf(headerArrayUpper, "SMILES")) + 1;

                    var newHeaderList = new List<string>();
                    for (int i = 0; i < descriptorStartOrder; i++)
                    {
                        newHeaderList.Add(headerArray[i]);
                    }
                    for (int i = descriptorStartOrder; i < headerArray.Length; i++)
                    {
                        if (descriptorList.Contains(headerArray[i]))
                        {
                            newHeaderList.Add(headerArray[i]);
                        }
                    }

                    var newHeaderLine = string.Join("\t", newHeaderList);
                    sw.WriteLine(newHeaderLine);

                    var line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        var lineArray = line.Split('\t');
                        var newLineList = new List<string>();
                        for (int i = 0; i < descriptorStartOrder; i++)
                        {
                            newLineList.Add(lineArray[i]);
                        }

                        for (int i = descriptorStartOrder; i < lineArray.Length; i++)
                        {
                            if (descriptorList.Contains(headerArray[i]))
                            {
                                newLineList.Add(lineArray[i]);
                            }
                        }

                        sw.WriteLine(string.Join("\t", newLineList));


                    }
                }

            }

        }

        public static void ExtractDescriptorToPredictFromPadel(string padelOutFileName, string rtDescriptorListFile, string ccsDescriptorListFile)
        {
            var newRtDescriptorFileName = Path.GetDirectoryName(padelOutFileName) + "\\" + Path.GetFileNameWithoutExtension(padelOutFileName) + "_ExtractedFromPadelResult_RT.tsv";
            var newCcsDescriptorFileName = Path.GetDirectoryName(padelOutFileName) + "\\" + Path.GetFileNameWithoutExtension(padelOutFileName) + "_ExtractedFromPadelResult_CCS.tsv";

            var rtSelectedDescriptorHeader = "Name\tRT";
            var ccsSelectedDescriptorHeader = "Name\tCCS\tMass";

            using (var sr = new StreamReader(rtDescriptorListFile, true))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    rtSelectedDescriptorHeader = rtSelectedDescriptorHeader + "\t" + line;
                }
            }
            using (var sr = new StreamReader(ccsDescriptorListFile, false))
            {
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    ccsSelectedDescriptorHeader = ccsSelectedDescriptorHeader + "\t" + line;
                }
            }

            using (var sw = new StreamWriter(newRtDescriptorFileName, false, Encoding.ASCII))
            {
                using (var sr = new StreamReader(padelOutFileName, true))
                {
                    var headerLine = sr.ReadLine();
                    var headerArray = headerLine.Split(',');
                    var selectDescriptorArray = rtSelectedDescriptorHeader.Split('\t');

                    sw.WriteLine(rtSelectedDescriptorHeader.Replace("Name", "InChIKey"));

                    var line = "";

                    while ((line = sr.ReadLine()) != null)
                    {
                        var lineArray = line.Split(',');
                        var newLineList = new List<string>();
                        var lineDic = new Dictionary<string, string>();

                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            lineDic.Add(headerArray[i], lineArray[i]);
                        }
                        foreach (var item in selectDescriptorArray)
                        {
                            if (lineDic.ContainsKey(item))
                            {
                                newLineList.Add(lineDic[item]);
                            }
                            else
                            {
                                newLineList.Add("0"); // maybe case of "RT"
                            }
                        }

                        sw.WriteLine(string.Join("\t", newLineList).Replace("\"", ""));
                    }
                }
            }
            using (var sw = new StreamWriter(newCcsDescriptorFileName, false, Encoding.ASCII))
            {
                using (var sr = new StreamReader(padelOutFileName, true))
                {
                    var headerLine = sr.ReadLine();
                    var headerArray = headerLine.Split(',');
                    var selectDescriptorArray = ccsSelectedDescriptorHeader.Split('\t');

                    sw.WriteLine(ccsSelectedDescriptorHeader.Replace("Name", "InChIKey"));

                    var line = "";
                    while ((line = sr.ReadLine()) != null)
                    {
                        var lineArray = line.Split(',');
                        var newLineList = new List<string>();
                        var lineDic = new Dictionary<string, string>();

                        for (int i = 0; i < lineArray.Length; i++)
                        {
                            lineDic.Add(headerArray[i], lineArray[i]);
                        }
                        foreach (var item in selectDescriptorArray)
                        {
                            if (lineDic.ContainsKey(item))
                            {
                                newLineList.Add(lineDic[item]);
                            }
                            else if(item =="Mass")
                            {
                                newLineList.Add(lineDic["MW"]);
                            }
                            else
                            {
                                newLineList.Add("0");
                            }
                        }

                        sw.WriteLine(string.Join("\t", newLineList).Replace("\"", ""));
                    }
                }
            }
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
