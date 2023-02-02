using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace Msdial.Gcms.Dataprocess.Utility
{
    public sealed class DatabaseGcUtility
    {
        private DatabaseGcUtility() { }

        public static List<MspFormatCompoundInformationBean> GetMspDbQueries(string filePath)
        {
            return MspFileParcer.MspFileReader(filePath);
        }

        public static Dictionary<int, float> GetRiDictionary(string filePath)
        {
            var dict = new Dictionary<int, float>();
            using (var sr = new StreamReader(filePath, Encoding.ASCII))
            {
                sr.ReadLine();
                while (sr.Peek() > -1)
                {
                    var line = sr.ReadLine();
                    if (line == string.Empty) break;
                    var lineArray = line.Split('\t');
                    if (lineArray.Length < 2) continue;

                    int carbon; float rt;
                    if (int.TryParse(lineArray[0], out carbon) && float.TryParse(lineArray[1], out rt))
                        dict[carbon] = rt;
                }
            }
            if (dict.Count == 0) return null;
            return dict;
        }
    }
}
