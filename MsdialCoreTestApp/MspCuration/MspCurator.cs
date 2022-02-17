using CompMs.Common.Parser;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.App.MsdialConsole.MspCuration {

    public sealed class MspCurator {
        private MspCurator() { }

        public static void WriteRtMzInChIKey(string mspfile) {
            var mspRecords = MspFileParser.MspFileReader(mspfile);
            var filename = System.IO.Path.GetFileNameWithoutExtension(mspfile);
            var directory = Path.GetDirectoryName(mspfile);
            var output = Path.Combine(directory, filename + "_kiuchi_temp.txt");
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("RT\tMZ\tInChIKey");
                foreach (var query in mspRecords) {
                    sw.WriteLine(query.ChromXs.RT.Value + "\t" + query.PrecursorMz + "\t" + query.InChIKey);
                }
            }
        }

        public static void AddRT2MspQueries(string mspfile, string textfile) {
            Console.WriteLine("Reading msp file");
            var mspRecords = MspFileParser.MspFileReader(mspfile);
            var inchi2RT = new Dictionary<string, string>();
            using (var sr = new StreamReader(textfile)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty || line is null) continue;
                    var linearray = line.Split('\t');
                    var shortInchi = linearray[3].Substring(0, 14);
                    if (inchi2RT.ContainsKey(shortInchi)) continue;
                    inchi2RT[shortInchi] = linearray[2];
                }
            }

            // add retention time field to msp record
            foreach (var mRecord in mspRecords) {
                if (mRecord.InChIKey.IsEmptyOrNull()) {
                    mRecord.ChromXs.RT.Value = -1;
                    continue;
                }
                var shortInChi = mRecord.InChIKey.Substring(0, 14);
                if (inchi2RT.ContainsKey(shortInChi)) {
                    double d;
                    if (double.TryParse(inchi2RT[shortInChi], out d)) {
                        mRecord.ChromXs.RT.Value = d;
                    }
                    else {
                        mRecord.ChromXs.RT.Value = -1;
                    }
                }
                else {
                    mRecord.ChromXs.RT.Value = -1;
                }
            }

            var filename = System.IO.Path.GetFileNameWithoutExtension(mspfile);
            var directory = Path.GetDirectoryName(mspfile);

            var outputfileWithRT = Path.Combine(directory, filename + "_PfppRT.msp");
            var outputfileWithoutRT = Path.Combine(directory, filename + "_WithoutRT.msp");

            Console.WriteLine("Writing to msp file");
            MspFileParser.WriteAsMsp(outputfileWithRT, mspRecords.Where(n => n.ChromXs.RT.Value > 0).ToList());
            MspFileParser.WriteAsMsp(outputfileWithoutRT, mspRecords.Where(n => n.ChromXs.RT.Value ==  -1).ToList());
        }
    }
}
