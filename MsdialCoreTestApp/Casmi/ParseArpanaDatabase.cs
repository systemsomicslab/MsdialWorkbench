using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.App.MsdialConsole.Casmi {
    public sealed class ParseArpanaDatabase {
        private ParseArpanaDatabase() {}

        public static void Convert2InChIKeyRtList(string input, string output) {
            var inchi2rt = new Dictionary<string, double>();
            var files = Directory.GetFiles(input);
            foreach (var file in files) {
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    while (sr.Peek() > -1) {
                        var wkstr = sr.ReadLine();
                        if (wkstr != null) {

                            var wArray = wkstr.Split('\t');
                            var inchikey = wArray[0].Split('_')[wArray[0].Split('_').Length - 1];
                            if (inchikey == null || inchikey == string.Empty) {
                                continue;
                            }

                            var rtstring = wArray[2];
                            if (double.TryParse(rtstring, out double rt) && rt > 0 && !inchi2rt.ContainsKey(inchikey)) {
                                inchi2rt[inchikey] = rt;
                            }
                        }
                    }
                }
            }
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("InChIKey\tRT");
                foreach (var query in inchi2rt) {
                    sw.WriteLine(query.Key + "\t" + query.Value);
                }
            }
        }

        public static void InsertSmiles2InChIKeyRtList(string inputLib, string inputMspPos, string inputMspNeg, string output) {

            var inchi2rt = new Dictionary<string, double>();
            using (var sr = new StreamReader(inputLib, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var wkstr = sr.ReadLine();
                    var wArray = wkstr.Split('\t');
                    inchi2rt[wArray[0]] = double.Parse(wArray[1]);

                }
            }
            var mspPosRecords = MspFileParser.MspFileReader(inputMspPos);
            var mspNegRecords = MspFileParser.MspFileReader(inputMspNeg);

            var inchi2smiles = new Dictionary<string, string>();
            foreach (var record in mspPosRecords) {
                var inchikey = record.InChIKey;
                var smiles = record.SMILES;
                if (inchikey == null || inchikey == string.Empty || smiles == null || smiles == string.Empty) {
                    continue;
                }
                if (!inchi2smiles.ContainsKey(inchikey)) {
                    inchi2smiles[inchikey] = smiles;
                }
            }

            foreach (var record in mspNegRecords) {
                var inchikey = record.InChIKey;
                var smiles = record.SMILES;
                if (inchikey == null || inchikey == string.Empty || smiles == null || smiles == string.Empty) {
                    continue;
                }
                if (!inchi2smiles.ContainsKey(inchikey)) {
                    inchi2smiles[inchikey] = smiles;
                }
            }

            // inchikey smiles rt
            using (var sw = new StreamWriter(output)) {
                sw.WriteLine("Name\tInChIKey\tSMILES\tRT");
                var counter = 1;
                foreach (var query in inchi2rt) {
                    if (inchi2smiles.ContainsKey(query.Key)) {
                        sw.WriteLine("Compound_" + counter.ToString() + "\t" + query.Key + "\t" + inchi2smiles[query.Key] + "\t" + query.Value);
                        counter++;
                    }
                }
            }
        }

        public static void CreatInChIKeySmilesList(string inputmsfinderlib, string inputMspPos, string inputMspNeg, string output) {

            var inchi2smiles = new Dictionary<string, string>();
            using (var sr = new StreamReader(inputmsfinderlib, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var wkstr = sr.ReadLine();
                    var wArray = wkstr.Split('\t');
                    inchi2smiles[wArray[1]] = wArray[6];

                }
            }
            var mspPosRecords = MspFileParser.MspFileReader(inputMspPos);
            var mspNegRecords = MspFileParser.MspFileReader(inputMspNeg);

            foreach (var record in mspPosRecords) {
                var inchikey = record.InChIKey;
                var smiles = record.SMILES;
                if (inchikey == null || inchikey == string.Empty || smiles == null || smiles == string.Empty) {
                    continue;
                }
                if (!inchi2smiles.ContainsKey(inchikey)) {
                    inchi2smiles[inchikey] = smiles;
                }
            }

            foreach (var record in mspNegRecords) {
                var inchikey = record.InChIKey;
                var smiles = record.SMILES;
                if (inchikey == null || inchikey == string.Empty || smiles == null || smiles == string.Empty) {
                    continue;
                }
                if (!inchi2smiles.ContainsKey(inchikey)) {
                    inchi2smiles[inchikey] = smiles;
                }
            }

            // inchikey smiles
            using (var sw = new StreamWriter(output)) {
                //sw.WriteLine("Name\tInChIKey\tSMILES");
                var counter = 1;
                foreach (var query in inchi2smiles) {
                    if (inchi2smiles.ContainsKey(query.Key)) {
                        //sw.WriteLine("Predicted_" + counter.ToString() + "\t" + query.Key + "\t" + query.Value);
                        sw.WriteLine(query.Value);
                        counter++;
                    }
                }
            }
        }
    }
}
