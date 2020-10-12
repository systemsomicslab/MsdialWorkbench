using CompMs.Common.Extension;
using CompMs.Common.Parser;
using CompMs.StructureFinder.NcdkDescriptor;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace StructureFinderConsoleApp {
    public sealed class Code4NPR2020 {
        private Code4NPR2020() { }

        public static void CalculateTop50MostCommonFunctionalGroups2020() {
            var smilesfile = @"D:\Paper of Natural Product Reports\Statistics\smileslist_test.txt";
            var output = @"D:\Paper of Natural Product Reports\Statistics\top50fg_output_test.txt";
            var smileslist = new List<string>();
            using (var sr = new StreamReader(smilesfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    smileslist.Add(line);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                var headers = new List<string>();
                var values4null = new List<int>();
                for (int i = 0; i < 51; i++) { 
                    headers.Add("Top50FG_" + i);
                    values4null.Add(-1);
                }
                sw.WriteLine(String.Join("\t", headers.ToArray()));

                var counter = 0;
                foreach (var smiles in smileslist) {
                    var dict = NcdkDescriptor.Top50MostCommonFunctionalGroups2020Fingerprinter(smiles);
                    if (dict.IsEmptyOrNull()) {
                        sw.WriteLine(String.Join("\t", values4null.ToArray()));
                    }
                    else {
                        var fingerprints = new List<int>();
                        foreach (var head in headers) { fingerprints.Add((int)dict[head]); }
                        sw.WriteLine(String.Join("\t", fingerprints.ToArray()));
                    }
                   
                    counter++;

                    if (counter % 1000 == 0) { Console.WriteLine("Finished {0}", counter); }
                }
            }
        }

        public static void ExtractClassyFireOntologies() {
            var keyfile = @"D:\Paper of Natural Product Reports\Statistics\key.txt";
            var ontologyfile = @"D:\Paper of Natural Product Reports\Statistics\InchikeyClassyfireDB-VS4.icd";
            var output = @"D:\Paper of Natural Product Reports\Statistics\key_output.txt";
            var keylist = new List<string>();
            var key2Ontology = new Dictionary<string, List<string>>(); // [0] name [1] id
            using (var sr = new StreamReader(keyfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    keylist.Add(line);
                }
            }

            using (var sr = new StreamReader(ontologyfile, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    var shortInchikey = lineArray[0].Split('-')[0];
                    key2Ontology[shortInchikey] = new List<string>() { lineArray[1], lineArray[2] };
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var key in keylist) {
                    if (key2Ontology.ContainsKey(key)) {
                        sw.WriteLine(key2Ontology[key][0] + "\t" + key2Ontology[key][1]);
                    }
                    else {
                        sw.WriteLine("NA\tNA");
                    }
                }
            }
        }

        public static void CheckCoverageOfMspMsfinder() {
            var keyfile = @"D:\Paper of Natural Product Reports\Statistics\key.txt";
            var tablefile = @"D:\Paper of Natural Product Reports\Statistics\table.txt";
            var output = @"D:\Paper of Natural Product Reports\Statistics\key_checked.txt";

            var keylist = new List<string>();
            var key2MspOnly = new Dictionary<string, int>();
            var key2MsfinderOnly = new Dictionary<string, int>();
            var key2MspMf = new Dictionary<string, int>();
            using (var sr = new StreamReader(keyfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    keylist.Add(line);
                    key2MspOnly[line] = 0;
                    key2MsfinderOnly[line] = 0;
                    key2MspMf[line] = 0;
                }
            }

            var structurelist = new List<string[]>();
            using (var sr = new StreamReader(tablefile, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var linearray = line.Split('\t');
                    structurelist.Add(linearray);
                }
            }

            foreach (var structure in structurelist) {
                var superclass = structure[2];
                var isMsp = structure[6] == "TRUE" ? true : false;
                var isMsfinder = structure[7] == "TRUE" ? true : false;
                var isBoth = isMsp && isMsfinder ? true : false;
                if (!keylist.Contains(superclass)) continue;
                if (isBoth) {
                    key2MspMf[superclass]++;
                } else if (isMsp) {
                    key2MspOnly[superclass]++;
                } else if (isMsfinder) {
                    key2MsfinderOnly[superclass]++;
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Super class\tMSP only\tMSFINDER only\tMSP&MSFINDER");
                foreach (var key in keylist) {
                    sw.WriteLine(key + "\t" + key2MspOnly[key] + "\t" + key2MsfinderOnly[key] + "\t" + key2MspMf[key]);
                }
            }
        }

        public static void CheckCoverageOfTop50FG() {
            var keyfile = @"D:\Paper of Natural Product Reports\Statistics\key.txt";
            var tablefile = @"D:\Paper of Natural Product Reports\Statistics\table.txt";
            var output = @"D:\Paper of Natural Product Reports\Statistics\key_checked.txt";

            var keylist = new List<string>();
            var key2FGlist = new Dictionary<string, List<int>>();
            using (var sr = new StreamReader(keyfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    keylist.Add(line);
                    key2FGlist[line] = new List<int>();
                    for (int i = 0; i < 51; i++) key2FGlist[line].Add(0);
                }
            }

            var structurelist = new List<string[]>();
            using (var sr = new StreamReader(tablefile, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var linearray = line.Split('\t');
                    structurelist.Add(linearray);
                }
            }

            foreach (var structure in structurelist) {
                var superclass = structure[3];
                for (int i = 0; i < 51; i++) {
                    var column = i + 5;
                    var columnvalue = int.Parse(structure[column]);
                    key2FGlist[superclass][i] += columnvalue;
                }
            }

            var headers = new List<string>();
            for (int i = 0; i < 51; i++) {
                headers.Add("Top50FG_" + i);
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Super class" + "\t" + String.Join("\t", headers.ToArray()));
                foreach (var key in keylist) {
                    var valuearray = key2FGlist[key];
                    sw.WriteLine(key + "\t" + String.Join("\t", valuearray.ToArray()));
                }
            }
        }

        public static void CheckInChIKeyExistence() {
            var keyfile = @"D:\Paper of Natural Product Reports\Statistics\key.txt";
            var pairedfile = @"D:\Paper of Natural Product Reports\Statistics\paired.txt";
            var output = @"D:\Paper of Natural Product Reports\Statistics\key_checked.txt";

            var keylist = new List<string>();
            using (var sr = new StreamReader(keyfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    keylist.Add(line);
                }
            }

            var pairlist = new List<string>();
            using (var sr = new StreamReader(pairedfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    pairlist.Add(line);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var key in keylist) {
                    if (pairlist.Contains(key)) sw.WriteLine("True");
                    else sw.WriteLine("False");
                }
            }
        }

        public static void GenerateStructureTableFromMSPs() {
            var posMsp = @"D:\9_Spectral library curations\Distributed MSPs\MSMS-RIKEN-Pos-VS15.msp";
            var negMsp = @"D:\9_Spectral library curations\Distributed MSPs\MSMS-RIKEN-Neg-VS15.msp";
            var output = @"D:\Paper of Natural Product Reports\Statistics\msp_unique14inchikey.txt";
            var inchikey2strinfo = new Dictionary<string, List<string>>(); // [0] formula [1] ontology [2] inchikey [3] smiles
            var posQueries = MspFileParser.MspFileReader(posMsp);
            foreach (var query in posQueries) {
                var inchikey = query.InChIKey;
                if (inchikey.IsEmptyOrNull()) continue;
                var formula = query.Formula.FormulaString;
                var ontology = query.Ontology;
                var smiles = query.SMILES;
                var shortinchikey = inchikey.Substring(0, 14);
                if (!inchikey2strinfo.ContainsKey(shortinchikey)) {
                    inchikey2strinfo[shortinchikey] = new List<string>() { formula, ontology, inchikey, smiles };
                }
            }

            var negQueries = MspFileParser.MspFileReader(negMsp);
            foreach (var query in negQueries) {
                var inchikey = query.InChIKey;
                if (inchikey.IsEmptyOrNull()) continue;
                var formula = query.Formula.FormulaString;
                var ontology = query.Ontology;
                var smiles = query.SMILES;
                var shortinchikey = inchikey.Substring(0, 14);
                if (!inchikey2strinfo.ContainsKey(shortinchikey)) {
                    inchikey2strinfo[shortinchikey] = new List<string>() { formula, ontology, inchikey, smiles };
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Short InChIKey\tFormula\tOntology\tInChIKey\tSMILES");
                foreach (var item in inchikey2strinfo) {
                    sw.WriteLine(item.Key + "\t" + item.Value[0] + "\t" + item.Value[1] + "\t" + item.Value[2] + "\t" + item.Value[3]);
                }
            }
        }
    }
}
