using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.MspGenerator
{
    public class misc
    {
        public static void compairRtTrainAndPredict(string trainFilePath, string calculatedFilePath, string outputFolderPath)
        {

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

            using (var sw = new StreamWriter(outputFolderPath))
            {
                using (var sr = new StreamReader(trainFilePath, true))
                {
                    var line = sr.ReadLine();
                    sw.WriteLine(line + "\tpredicted RT");
                    var lineArray = line.Split('\t');
                    while (sr.Peek() > -1)
                    {
                        line = sr.ReadLine();
                        if (line == string.Empty) continue;
                        lineArray = line.Split('\t');
                        var inchikey = lineArray[0];
                        if (inchikeyToPredictedRt.ContainsKey(inchikey))
                        {
                            sw.WriteLine(line + "\t" + inchikeyToPredictedRt[inchikey]);
                        }
                    }
                }
            }
        }
    }
}