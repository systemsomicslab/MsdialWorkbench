using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.Parser;
using CompMs.Common.Mathematics.Basic;
using CompMs.Common.Parser;
using CompMs.StructureFinder.NcdkDescriptor;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StructureFinderConsoleApp {
    public sealed class Code4NPR2020 {
        private Code4NPR2020() { }

        public static void CalculatePrecursorMzVS2(string input, string output) {

            // input: @"E:\6_Projects\1_naturalproductprofiling_protocol\agc_compoundlist_formula.txt"
            // output: @"E:\6_Projects\1_naturalproductprofiling_protocol\agc_compoundlist_preMzList.txt"

            input = @"E:\6_Projects\1_naturalproductprofiling_protocol\agc_compoundlist_formula.txt";
            output = @"E:\6_Projects\1_naturalproductprofiling_protocol\agc_compoundlist_preMzList.txt";
            var formulaList = new List<string>();
            using (var sr = new StreamReader(input, true)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    formulaList.Add(line);
                }
            }
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                foreach (var formulaString in formulaList) {
                    var formulaObj = FormulaStringParcer.Convert2FormulaObjV2(formulaString);
                    var mass = formulaObj.Mass;

                    var adductproton = AdductIon.GetAdductIon("[M+H]+");
                    var mass_proton = adductproton.ConvertToMz(mass);

                    sw.WriteLine(formulaObj.FormulaString + "\t" + mass + "\t" + mass_proton);


                    //Console.WriteLine("Formula {0}, ExactMass {1}, [M+H]+ {2}", formulaObj.FormulaString, mass, mass + protonMass);
                }
            }

            Console.ReadLine();
           

        }


        public static void CalculatePrecursorMz(string input, string output) {
            var smilescodes = new List<string>();
            using (var sr = new StreamReader(input, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    smilescodes.Add(line);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("[M+H]+\t[M+Na]+\t[M+K]+\t[M-H]-");
                var adductproton = AdductIon.GetAdductIon("[M+H]+");
                var adductNa = AdductIon.GetAdductIon("[M+Na]+");
                var adductK = AdductIon.GetAdductIon("[M+K]+");
                var adductProtonLoss = AdductIon.GetAdductIon("[M-H]-");
                for (int i = 0; i < smilescodes.Count; i++) {
                    var structure = MoleculeConverter.SmilesToStructure(smilescodes[i], out string error);
                    var precursorMzProton = adductproton.ConvertToMz(structure.ExactMass);
                    var precursorMzNa = adductNa.ConvertToMz(structure.ExactMass);
                    var precursorMzK = adductK.ConvertToMz(structure.ExactMass);
                    var precursorMzProtonLoss = adductProtonLoss.ConvertToMz(structure.ExactMass);
                    sw.WriteLine(precursorMzProton + "\t" + precursorMzNa + "\t" + precursorMzK + "\t" + precursorMzProtonLoss);
                }
            }
        }

        public static void CheckPrecursorMzExistence() {

            var tablefile = @"C:\Users\hiroshi.tsugawa\Desktop\temp_matrix.txt";
            var mzfile = @"C:\Users\hiroshi.tsugawa\Desktop\temp_premz.txt";
            var output = @"C:\Users\hiroshi.tsugawa\Desktop\temp.txt";

            var name2mz = new Dictionary<string, double>();
            using (var sr = new StreamReader(tablefile, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    var metname = lineArray[3];
                    var mz = double.Parse(lineArray[18]);
                    if (!name2mz.ContainsKey(metname)) name2mz[metname] = mz;
                }
            }

            var mzValues = new List<double>();
            using (var sr = new StreamReader(mzfile, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var mz = double.Parse(line);
                    mzValues.Add(mz);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                
                for (int i = 0; i < mzValues.Count; i++) {
                    var mz = mzValues[i];
                    var flg = false;
                    foreach (var item in name2mz) {
                        if (Math.Abs(mz - item.Value) < 0.01) {
                            sw.WriteLine(item.Key);
                            flg = true;
                            break;
                        }
                    }
                    if (flg == false)
                        sw.WriteLine("null");
                }
            }
        }

        public static void Check144Existence(string input, string output) {
            var spectrumList = new List<string>();
            using (var sr = new StreamReader(input, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    spectrumList.Add(lineArray[12]);
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Existence");
                for (int i = 0; i < spectrumList.Count; i++) {
                    var spectrumObj = TextLibraryParser.TextToSpectrumList(spectrumList[i], ':', ' ');
                    var flg = false;
                    foreach (var spec in spectrumObj) {
                        if (Math.Abs(spec.Mass- 144.0807) < 0.01) {
                            flg = true;
                            break;
                        }
                    }
                    if (flg) sw.WriteLine("True");
                    else sw.WriteLine("False");
                }
            }
        }

        public static void ExtractCCSValues(string input, string ccsinput, string output) {
            var inchikeys = new List<string>();
            using (var sr = new StreamReader(input, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    inchikeys.Add(lineArray[9].Split('-')[0]);
                }
            }

            var inchi2ccs = new Dictionary<string, string>();
            using (var sr = new StreamReader(ccsinput, true)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    var inchikey = lineArray[2].Split('-')[0];
                    var adduct = lineArray[4];
                    if (adduct != "[M+H]+") continue;
                    var ccs = lineArray[6];
                    inchi2ccs[inchikey] = ccs;
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("InChIKey\tCCS");
                for (int i = 0; i < inchikeys.Count; i++) {
                    if (inchi2ccs.ContainsKey(inchikeys[i])) {
                        sw.WriteLine(inchikeys[i] + "\t" + inchi2ccs[inchikeys[i]]);
                    }
                    else {
                        sw.WriteLine(inchikeys[i] + "\t" + "null");
                    }
                }
            }

        }

        public static void GenerateEdgesByTanimotoIndex(string input, string output) {

            var fingerprints = new List<double[]>();
            var name2fingerprints = new Dictionary<string, List<double>>();
            var titles = new List<string>();
            var title2id = new Dictionary<string, int>();
            var id2title = new Dictionary<int, string>();
            var counter = 0;
            using (var sr = new StreamReader(input, true)) { 
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    titles.Add(lineArray[0]);
                    name2fingerprints[lineArray[0]] = new List<double>();
                    for (int i = 1; i < lineArray.Length; i++) {
                        name2fingerprints[lineArray[0]].Add(double.Parse(lineArray[i]));
                    }
                    fingerprints.Add(name2fingerprints[lineArray[0]].ToArray());

                    title2id[lineArray[0]] = counter;
                    id2title[counter] = lineArray[0];
                    counter++;
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Source (title)\tTarget (title)\tSource (Comment)\tTarget (Comment)\tSource (ID)\tTarget (ID)\tScore\tColor\tEdge information");
                for (int i = 0; i < fingerprints.Count; i++) {
                    for (int j = i + 1; j < fingerprints.Count; j++) {
                        var tIndex = BasicMathematics.TanimotoIndex(fingerprints[i], fingerprints[j]);
                        if (tIndex > 0.85) {
                            var info = new List<string>() { id2title[i], id2title[j], "null", "null", i.ToString(), j.ToString(), Math.Round(tIndex, 3).ToString(), "blue", "Structure similarity" };
                            sw.WriteLine(String.Join("\t", info));
                        }
                    }
                }
            }
        }

        public static void GenerateFragmentStatisticsForEachOntology() {

            var dictionaryfile = @"D:\Paper of Natural Product Reports\Statistics\dictionaryfile.txt";
            var superclassfile = @"D:\Paper of Natural Product Reports\Statistics\superclasslist.txt";

            var input1 = @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\nl\Merged fragment info-NL-Pos.txt";
            var output1 = @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\nl\Fragment_stat_NL_Pos_";

            var input2 = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\nl\Merged fragment info-NL-Neg.txt";
            var output2 = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\nl\Fragment_stat_NL_Neg_";

            var input3 = @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Merged fragment info-PI-Pos-inchikey-merged.txt";
            var output3 = @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\duplicate_removed\pi\Fragment_stat_PI_Pos_";

            var input4 = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Merged fragment info-PI-Neg-inchikey-merged.txt";
            var output4 = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\duplicate_removed\pi\Fragment_stat_PI_Neg_";

            var inchi2superclass = new Dictionary<string, string>();
            using (var sr = new StreamReader(dictionaryfile, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    inchi2superclass[lineArray[0]] = lineArray[3];
                }
            }

            var superclasses = new List<string>();
            using (var sr = new StreamReader(superclassfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    superclasses.Add(line);

                }
            }

            var chars = Path.GetInvalidFileNameChars();
            foreach (var sclass in superclasses) {
                var tagname = new string(sclass.Select(c => chars.Contains(c) ? '_' : c).ToArray());
                var output1_path = output1 + tagname + ".txt";
                var output2_path = output2 + tagname + ".txt";
                var output3_path = output3 + tagname + ".txt";
                var output4_path = output4 + tagname + ".txt";

                ExportFragmentStatistics(input1, sclass, inchi2superclass, output1_path);
                ExportFragmentStatistics(input2, sclass, inchi2superclass, output2_path);
                ExportFragmentStatistics(input3, sclass, inchi2superclass, output3_path);
                ExportFragmentStatistics(input4, sclass, inchi2superclass, output4_path);
                Console.WriteLine(sclass + " finished");
            }
        }

        public static void GetHydrogenCorrectedFormulaStrings(string input, string output, string tag) {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Formula");
                using (var sr = new StreamReader(input, true)) { // key contains the list of short inchikey without header
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine();
                        if (line.IsEmptyOrNull()) continue;
                        var lineArray = line.Split('\t');
                        var smiles = lineArray[0];
                        var hoffset = int.Parse(lineArray[1]);
                        var cFormula = GetHydrogenCorrectedFormulaString(smiles, hoffset);
                        sw.WriteLine(cFormula + tag);
                    }
                }

            }
        }

        public static string GetHydrogenCorrectedFormulaString(string smiles, int hOffset) {
            var structure = MoleculeConverter.SmilesToStructure(smiles, out string error);
            var formula = structure.Formula;
            var cFormula = new Formula(formula.Cnum, formula.Hnum + hOffset, formula.Nnum, formula.Onum, formula.Pnum, formula.Snum, formula.Fnum, formula.Clnum, formula.Brnum, formula.Inum, formula.Sinum);
            return cFormula.FormulaString;
        }

        public static void ExportFragmentStatistics(string input, string filteredontology, Dictionary<string, string> inchikey2ontology, string output) {
            var fragmentCounts = new List<FragmentCount>();
            var fileNames = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine().Trim();
                    if (line == string.Empty) continue;

                    var lineArray = line.Split('\t');

                    var shortInChIKey = lineArray[5];
                    var HRcount = (int)Math.Round(double.Parse(lineArray[12]));
                    var fragSmiles = lineArray[3];
                    var fileName = lineArray[13];
                    var mass = double.Parse(lineArray[7]);

                    var ontology = inchikey2ontology.ContainsKey(lineArray[0]) ? inchikey2ontology[lineArray[0]] : string.Empty;
                    var isfiltered = !filteredontology.IsEmptyOrNull() && !ontology.IsEmptyOrNull() && filteredontology != ontology ? true : false;
                    if (isfiltered) continue;

                    if (!fileNames.Contains(fileName)) fileNames.Add(fileName);

                    var fragID = -1;
                    for (int i = 0; i < fragmentCounts.Count; i++) {
                        var fragCount = fragmentCounts[i];
                        if (fragCount.ShortInChIkey == shortInChIKey && fragCount.HrCount == HRcount) {
                            fragID = i;
                            break;
                        }
                    }
                    if (fragID == -1) {
                        var fragmentCount = new FragmentCount() {
                            HrCount = HRcount,
                            ShortInChIkey = shortInChIKey,
                            FragmentSMILES = fragSmiles,
                            HrCorrectedMass = mass,
                        };


                        fragmentCount.FileCountPair[fileName] = 1;
                        fragmentCounts.Add(fragmentCount);
                    }
                    else {
                        if (!fragmentCounts[fragID].FileCountPair.ContainsKey(fileName)) {
                            fragmentCounts[fragID].FileCountPair[fileName] = 1;
                        }
                        else {
                            fragmentCounts[fragID].FileCountPair[fileName]++;
                        }
                    }
                }
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.Write("Fragment Short inchikey\tFragment SMILES\tHR count\tHR corrected mass\t");
                foreach (var filename in fileNames) {
                    sw.Write(filename + "\t");
                }
                sw.WriteLine();


                foreach (var frag in fragmentCounts) {
                    sw.Write(frag.ShortInChIkey + "\t" + frag.FragmentSMILES + "\t" + frag.HrCount + "\t" + frag.HrCorrectedMass + "\t");

                    foreach (var filename in fileNames) {
                        var isContained = false;
                        foreach (var pair in frag.FileCountPair) {
                            var name = pair.Key;
                            var count = pair.Value;

                            if (filename == name) {
                                isContained = true;
                                sw.Write(count + "\t");
                                break;
                            }
                        }

                        if (isContained == false) {
                            sw.Write(0 + "\t");
                        }
                    }
                    sw.WriteLine();
                }
            }
        }


        public static void CalculateInformationRichnessOfMsmsInEachSuperClass() {
            var mspfile = @"D:\9_Spectral library curations\Fragment curation\20200910\MSMS-RIKEN-PosNeg-VS15-ForStatistics.msp";
            var dictionaryfile = @"D:\Paper of Natural Product Reports\Statistics\dictionaryfile.txt";
            var superclassfile = @"D:\Paper of Natural Product Reports\Statistics\superclasslist.txt";
            var output = @"D:\Paper of Natural Product Reports\Statistics\spectrumquality_result.txt";

            var inchi2superclass = new Dictionary<string, string>();
            using (var sr = new StreamReader(dictionaryfile, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    inchi2superclass[lineArray[0]] = lineArray[3];
                }
            }

            var superclasses = new List<string>();
            var superclassDict = new Dictionary<string, List<double>>();
            using (var sr = new StreamReader(superclassfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    superclasses.Add(line);
                    superclassDict[line] = new List<double>() { 0, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0 }; // count, total, 10%, 20%, 30%, 40%, 50%, 60%, 70%, 80%, 90%

                }
            }

            var mspRecords = MspFileParser.MspFileReader(mspfile);
            foreach (var record in mspRecords) {
                var inchikey = record.InChIKey;
                var shortInchi = inchikey.Split('-')[0];
                if (!inchi2superclass.ContainsKey(shortInchi)) continue;
                var superclass = inchi2superclass[shortInchi];
                var spectrum = record.Spectrum;
                var specCount = spectrum.Count;
                if (specCount == 0) continue;

                var maxIntensity = spectrum.Max(n => n.Intensity);

                var count10 = spectrum.Count(n => n.Intensity > maxIntensity * 0.1);
                var count20 = spectrum.Count(n => n.Intensity > maxIntensity * 0.2);
                var count30 = spectrum.Count(n => n.Intensity > maxIntensity * 0.3);
                var count40 = spectrum.Count(n => n.Intensity > maxIntensity * 0.4);
                var count50 = spectrum.Count(n => n.Intensity > maxIntensity * 0.5);
                var count60 = spectrum.Count(n => n.Intensity > maxIntensity * 0.6);
                var count70 = spectrum.Count(n => n.Intensity > maxIntensity * 0.7);
                var count80 = spectrum.Count(n => n.Intensity > maxIntensity * 0.8);
                var count90 = spectrum.Count(n => n.Intensity > maxIntensity * 0.9);

                superclassDict[superclass][0] += specCount;
                superclassDict[superclass][1] += count10;
                superclassDict[superclass][2] += count20;
                superclassDict[superclass][3] += count30;
                superclassDict[superclass][4] += count40;
                superclassDict[superclass][5] += count50;
                superclassDict[superclass][6] += count60;
                superclassDict[superclass][7] += count70;
                superclassDict[superclass][8] += count80;
                superclassDict[superclass][9] += count90;
                superclassDict[superclass][10]++;
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                var header = new List<string>() { "Total", "10%", "20%", "30%", "40%", "50%", "60%", "70%", "80%", "90%", "Met count" };
                sw.WriteLine(String.Join("\t", header));
                foreach (var sclass in superclasses) {
                    sw.WriteLine(sclass + "\t" + String.Join("\t", superclassDict[sclass].ToArray()));
                }
            }
        }

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

        public static void ExtractSubstructureContainingStructureQueries() {
            var smilesfile = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\MSMS-RIKEN-Neg-VS15-Combined.smiles";
            var output = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\output.txt";
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
                var headertemp = NcdkDescriptor.TargetSubstructureFingerPrinter("CCCCC");
                for (int i = 0; i < headertemp.Count; i++) {
                    headers.Add("Top50FG_" + i);
                    values4null.Add(-1);
                }
                sw.WriteLine(String.Join("\t", headers.ToArray()));

                var counter = 0;
                foreach (var smiles in smileslist) {
                    var dict = NcdkDescriptor.TargetSubstructureFingerPrinter(smiles);
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
            var keyfile = @"E:\6_Projects\PROJECT_ImagingMS\CCSref\inchikeys.txt";
            var ontologyfile = @"E:\6_Projects\PROJECT_NPR_REVIEW\Statistics\InchikeyClassyfireDB-VS4.icd";
            var output = @"E:\6_Projects\PROJECT_ImagingMS\CCSref\inchikeys_classified.txt";
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

        public static void GetStatisticsOfOntologyForEachPolarity() {
            var posMsp = @"D:\9_Spectral library curations\Fragment curation\20200910\Pos\MSMS-RIKEN-Pos-VS15-Combined.msp";
            var negMsp = @"D:\9_Spectral library curations\Fragment curation\20200910\Neg\MSMS-RIKEN-Neg-VS15-Combined.msp";
            var outputPos = @"D:\Paper of Natural Product Reports\Statistics\msp_superclass_stats_pos.txt";
            var outputNeg = @"D:\Paper of Natural Product Reports\Statistics\msp_superclass_stats_neg.txt";
            var dictionaryfile = @"D:\Paper of Natural Product Reports\Statistics\dictionaryfile.txt";
            var superclassfile = @"D:\Paper of Natural Product Reports\Statistics\superclasslist.txt";

            var inchi2superclass = new Dictionary<string, string>();
            using (var sr = new StreamReader(dictionaryfile, true)) { // key contains the list of short inchikey without header
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    var lineArray = line.Split('\t');
                    inchi2superclass[lineArray[0]] = lineArray[3];
                }
            }

            var superclasses = new List<string>();
            using (var sr = new StreamReader(superclassfile, true)) { // key contains the list of short inchikey without header
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line.IsEmptyOrNull()) continue;
                    superclasses.Add(line);

                }
            }

            var posQueries = MspFileParser.MspFileReader(posMsp);
            var negQueries = MspFileParser.MspFileReader(negMsp);

            using (var sw = new StreamWriter(outputPos, false, Encoding.ASCII)) {
                sw.WriteLine("Super class\tCount");
                foreach (var sclass in superclasses) {
                    var counter = 0;
                    foreach (var query in posQueries) {
                        var shortinchikey = query.InChIKey.Split('-')[0];
                        var ontoloty = inchi2superclass[shortinchikey];
                        if (sclass == ontoloty) counter++;
                    }
                    sw.WriteLine(sclass + "\t" + counter);
                }
            }

            using (var sw = new StreamWriter(outputNeg, false, Encoding.ASCII)) {
                sw.WriteLine("Super class\tCount");
                foreach (var sclass in superclasses) {
                    var counter = 0;
                    foreach (var query in negQueries) {
                        var shortinchikey = query.InChIKey.Split('-')[0];
                        var ontoloty = inchi2superclass[shortinchikey];
                        if (sclass == ontoloty) counter++;
                    }
                    sw.WriteLine(sclass + "\t" + counter);
                }
            }
        }
    }
}
