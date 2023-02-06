using Rfx.Riken.OsakaUniv;
using Riken.Metabolomics.StructureFinder.Descriptor;
using Riken.Metabolomics.StructureFinder.Fragmenter;
using Riken.Metabolomics.StructureFinder.SpectralAssigner;
using Riken.Metabolomics.StructureFinder.Statistics;
using Riken.Metabolomics.StructureFinder.Utility;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StructureFinderConsoleApp
{
    public class FragmentCount
    {
        private string shortInChIkey;
        private string fragmentSMILES;
        private int hrCount;
        private double hrCorrectedMass;
        private string cleavedAtoms;

        private Dictionary<string, int> fileCountPair;

        public FragmentCount() { fileCountPair = new Dictionary<string, int>(); }

        public string ShortInChIkey
        {
            get { return shortInChIkey; }
            set { shortInChIkey = value; }
        }

        public double HrCorrectedMass
        {
            get { return hrCorrectedMass; }
            set { hrCorrectedMass = value; }
        }

        public string CleavedAtoms
        {
            get { return cleavedAtoms; }
            set { cleavedAtoms = value; }
        }

        public string FragmentSMILES
        {
            get { return fragmentSMILES; }
            set { fragmentSMILES = value; }
        }

        public int HrCount
        {
            get { return hrCount; }
            set { hrCount = value; }
        }

        public Dictionary<string, int> FileCountPair
        {
            get { return fileCountPair; }
            set { fileCountPair = value; }
        }
    }

    public class DescriptorProperty
    {
        private string descriptor;
        private Dictionary<string, int> fileCountPair;

        public DescriptorProperty() { fileCountPair = new Dictionary<string, int>(); }

        public Dictionary<string, int> FileCountPair
        {
            get { return fileCountPair; }
            set { fileCountPair = value; }
        }

        public string Descriptor
        {
            get { return descriptor; }
            set { descriptor = value; }
        }
    }

    public sealed class BondPathStatistics
    {
        private BondPathStatistics() { }

        #region //remove duplicate information
        //same fragment info among the same metabolite will be excluded.
        public static void RemoveDuplicatePIandNL(string input, string output)
        {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                using (var sr = new StreamReader(input, Encoding.ASCII)) {

                    sw.WriteLine(sr.ReadLine());
                    var tempArrayList = new List<string[]>() { sr.ReadLine().Split('\t') };
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine().Trim();
                        if (line == string.Empty) continue;

                        var lineArray = line.Split('\t');
                        if (tempArrayList[tempArrayList.Count - 1][1] != lineArray[1]) {

                            //check duplicate
                            tempArrayList = tempArrayList.OrderBy(n => n[9]).ToList();

                            var rTempArrayList = getRefinedTempArrayListPIandNL(tempArrayList);
                            foreach (var array in rTempArrayList) {
                                writeCompressedField(sw, array, 16);
                            }

                            tempArrayList = new List<string[]>() { lineArray };
                        }
                        else {
                            tempArrayList.Add(lineArray);
                        }
                    }

                    //reminder
                    var fTempArrayList = getRefinedTempArrayListPIandNL(tempArrayList);
                    foreach (var array in fTempArrayList) {
                        writeCompressedField(sw, array, 16);
                    }
                }
            }
        }

        //same fragment info among the same metabolite will be excluded.
        public static void RemoveDuplicateDL(string input, string output)
        {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {

                using (var sr = new StreamReader(input, Encoding.ASCII)) {

                    sw.WriteLine(sr.ReadLine());
                    var tempArrayList = new List<string[]>() { sr.ReadLine().Split('\t') };
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine().Trim();
                        if (line == string.Empty) continue;

                        var lineArray = line.Split('\t');
                        if (tempArrayList[tempArrayList.Count - 1][1] != lineArray[1]) {

                            //check duplicate
                            tempArrayList = tempArrayList.OrderByDescending(n => n[7]).OrderBy(n => n[8]).ToList();

                            var rTempArrayList = getRefinedTempArrayListDL(tempArrayList);
                            foreach (var array in rTempArrayList) {
                                writeCompressedField(sw, array, 9);
                            }

                            tempArrayList = new List<string[]>() { lineArray };
                        }
                        else {
                            tempArrayList.Add(lineArray);
                        }
                    }

                    //reminder
                    var fTempArrayList = getRefinedTempArrayListDL(tempArrayList);
                    foreach (var array in fTempArrayList) {
                        writeCompressedField(sw, array, 9);
                    }
                }
            }
        }

        /// <summary>
        /// Descriptors
        /// first layer (one bond path): 48 (0~47)
        /// second layer (two bond path): 482 (48~529)
        /// third layer (three bond path): 1337 (530~1866) -> 1378 (530~1907) for C and Si separated
        /// bond descript hash must be separated by '-'
        /// first, second, and third layer will be distinguished by the number of 'layer': layer = 1 means first layer.
        /// Full layer will be distinguished by 0
        /// </summary>
        private static void writeCompressedField(StreamWriter sw, string[] array, int endMetaField)
        {
            for (int i = 0; i <= endMetaField; i++) {
                sw.Write(array[i] + "\t");
            }
            sw.WriteLine();
            //var descriptorHash = string.Empty;
            
            ////zero path
            //var start = endMetaField + 1;
            //for (int i = start; i < start + 48; i++) {
            //    descriptorHash += array[i];
            //}
            //descriptorHash += "-";

            ////first path
            //start = start + 48;
            //for (int i = start; i < start + 482; i++) {
            //    descriptorHash += array[i];
            //}
            //descriptorHash += "-";

            //start = start + 482;
            //for (int i = start; i < start + 1378; i++) {
            //    descriptorHash += array[i];
            //}
            //sw.WriteLine(descriptorHash);
        }

        private static List<string[]> getRefinedTempArrayListPIandNL(List<string[]> tempArrayList)
        {
            var rTempArrayList = new List<string[]>() { tempArrayList[0] };
            if (tempArrayList.Count == 1) return rTempArrayList;

            for (int i = 1; i < tempArrayList.Count; i++) {
                if (rTempArrayList[rTempArrayList.Count - 1][9] == tempArrayList[i][9]) {
                    var rIntensity = double.Parse(rTempArrayList[rTempArrayList.Count - 1][11]);
                    var tIntensity = double.Parse(tempArrayList[i][11]);
                    if (rIntensity < tIntensity) {
                        rTempArrayList.RemoveAt(rTempArrayList.Count - 1);
                        rTempArrayList.Add(tempArrayList[i]);
                    }
                }
                else {
                    rTempArrayList.Add(tempArrayList[i]);
                }
            }

            return rTempArrayList;
        }

        private static List<string[]> getRefinedTempArrayListDL(List<string[]> tempArrayList)
        {
            var rTempArrayList = new List<string[]>() { tempArrayList[0] };
            if (tempArrayList.Count == 1) return rTempArrayList;

            for (int i = 1; i < tempArrayList.Count; i++) {
                if (rTempArrayList[rTempArrayList.Count - 1][8] == tempArrayList[i][8] && rTempArrayList[rTempArrayList.Count - 1][7] != "0" && tempArrayList[i][7] != "0") {
                    var rIntensity = double.Parse(rTempArrayList[rTempArrayList.Count - 1][9]);
                    var tIntensity = double.Parse(tempArrayList[i][9]);
                    if (rIntensity < tIntensity) {
                        rTempArrayList.RemoveAt(rTempArrayList.Count - 1);
                        rTempArrayList.Add(tempArrayList[i]);
                    }
                }
                else if (rTempArrayList[rTempArrayList.Count - 1][8] == tempArrayList[i][8] && rTempArrayList[rTempArrayList.Count - 1][9] == tempArrayList[i][9] && rTempArrayList[rTempArrayList.Count - 1][7] == "0" && tempArrayList[i][7] == "0") {

                }
                else {
                    rTempArrayList.Add(tempArrayList[i]);
                }
            }

            return rTempArrayList;
        }
        #endregion

        #region // merge fragmet info
        public static void MergeFragmentInfo(string[] files, string output)
        {
            var fragmentList = new List<string[]>();
            foreach (var file in files) {
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine().Trim();
                        if (line == string.Empty) continue;

                        var lineArray = line.Split('\t');

                        //short inchikey, smiles, adduct, fragment smiles, fragment inchikey (empty), fragment short inchikey (empty), fragment exact mass, HR corrected fragment mass, intensity, tree depth, cleaved count, cleaved atoms, HR, filename
                        var rArray = new string[] { lineArray[1], lineArray[2], lineArray[3], lineArray[7], "", "", lineArray[8], lineArray[9], lineArray[10], lineArray[13], lineArray[14], lineArray[15], lineArray[16], System.IO.Path.GetFileNameWithoutExtension(file) };
                        fragmentList.Add(rArray);
                    }
                }
            }

            fragmentList = fragmentList.OrderBy(n => n[3]).ToList();

            var output_smiles = System.IO.Path.GetDirectoryName(output) + "\\" + System.IO.Path.GetFileNameWithoutExtension(output) + "-smiles.smiles";

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Short inchikey\tSMILES\tAdduct\tFragment SMILES\tFragment InChIkey\tFragment short InChIKey\tFragment exact mass\tHR corrected mass\tIntensity\tTree depth\tCleaved count\tCleaved atoms\tRearranged H\tFile name\t");

                foreach (var frag in fragmentList) {
                    foreach (var prop in frag) {
                        sw.Write(prop + "\t");
                    }
                    sw.WriteLine();
                }
            }

            using (var sw = new StreamWriter(output_smiles, false, Encoding.ASCII)) {
                foreach (var frag in fragmentList) {
                    sw.WriteLine(frag[3]);
                }
            }
        }

        public static void MergeInChIKeyToMergedFragmentInfoTable(string tablefile, string inchikeyfile, string output) {
            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                using (var sr = new StreamReader(tablefile, Encoding.ASCII)) {
                    sw.WriteLine(sr.ReadLine());
                    using (var sr2 = new StreamReader(inchikeyfile, Encoding.ASCII)) {
                        while (sr.Peek() > -1) {
                            var line = sr.ReadLine();
                            var lineArray = line.Split('\t');

                            var inchikeycode = sr2.ReadLine();
                            var originalinchikey = string.Empty;
                            var shortinchikey = string.Empty;
                            if (inchikeycode.ToLower().Contains("inchikey=")) {
                                originalinchikey = inchikeycode.Replace("InChIKey=", "");
                            }
                            else {
                                originalinchikey = inchikeycode;
                            }
                            shortinchikey = originalinchikey.Split('-')[0];

                            lineArray[4] = originalinchikey;
                            lineArray[5] = shortinchikey;

                            sw.WriteLine(String.Join("\t", lineArray));
                        }
                    }
                }
            }
        }

        public static void ExportFragmentStatistics(string input, string output)
        {
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

        public static void ExportHrStatistics(string input, string output)
        {
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
                    var cleavedAtoms = lineArray[11];
                    var mass = double.Parse(lineArray[7]);

                    if (!fileNames.Contains(fileName)) fileNames.Add(fileName);

                    var fragID = -1;
                    for (int i = 0; i < fragmentCounts.Count; i++) {
                        var fragCount = fragmentCounts[i];
                        if (fragCount.CleavedAtoms == cleavedAtoms && fragCount.HrCount == HRcount) {
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
                            CleavedAtoms = cleavedAtoms
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
                sw.Write("Cleaved atoms\tHR count\t");
                foreach (var filename in fileNames) {
                    sw.Write(filename + "\t");
                }
                sw.WriteLine();


                foreach (var frag in fragmentCounts) {
                    sw.Write(frag.CleavedAtoms + "\t" + frag.HrCount + "\t");

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
        #endregion

        #region // descriptor statistics

        public static void DescriptorStatistics(string input)
        {
            Console.WriteLine("Analysis started: {0}", input);
            var totalLineCount = 0;
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                sr.ReadLine();
                while (sr.Peek() > -1) {
                    sr.ReadLine();
                    totalLineCount++;
                }
            }

            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                var headerArray = sr.ReadLine().Split('\t').ToArray();
                var descriptorList = new List<string>();
                for (int i = 10; i < 1867 + 10; i++) {
                    descriptorList.Add(headerArray[i]);
                }

                var zeroStatistics = new Dictionary<string, int>();
                var oneStatistics = new Dictionary<string, int>();
                var twoStatistics = new Dictionary<string, int>();
                var threeStatistics = new Dictionary<string, int>();
                var fourStatistics = new Dictionary<string, int>();
                #region
                foreach (var descript in descriptorList) {
                    zeroStatistics[descript] = 0;
                    oneStatistics[descript] = 0;
                    twoStatistics[descript] = 0;
                    threeStatistics[descript] = 0;
                    fourStatistics[descript] = 0;
                }
                #endregion
                var zeroFirstLayerDlStatistics = new Dictionary<string, int>();
                var zeroSecondLayerDlStatistics = new Dictionary<string, int>();
                var zeroThirdLayerDlStatistics = new Dictionary<string, int>();
                var zeroFullLayerDlStatistics = new Dictionary<string, int>();

                var oneFirstLayerDlStatistics = new Dictionary<string, int>();
                var oneSecondLayerDlStatistics = new Dictionary<string, int>();
                var oneThirdLayerDlStatistics = new Dictionary<string, int>();
                var oneFullLayerDlStatistics = new Dictionary<string, int>();

                var twoFirstLayerDlStatistics = new Dictionary<string, int>();
                var twoSecondLayerDlStatistics = new Dictionary<string, int>();
                var twoThirdLayerDlStatistics = new Dictionary<string, int>();
                var twoFullLayerDlStatistics = new Dictionary<string, int>();

                var threeFirstLayerDlStatistics = new Dictionary<string, int>();
                var threeSecondLayerDlStatistics = new Dictionary<string, int>();
                var threeThirdLayerDlStatistics = new Dictionary<string, int>();
                var threeFullLayerDlStatistics = new Dictionary<string, int>();

                var fourFirstLayerDlStatistics = new Dictionary<string, int>();
                var fourSecondLayerDlStatistics = new Dictionary<string, int>();
                var fourThirdLayerDlStatistics = new Dictionary<string, int>();
                var fourFullLayerDlStatistics = new Dictionary<string, int>();

                while (sr.Peek() > -1) {

                    var lineCount = 0;

                    #region
                    var line = sr.ReadLine().Trim();
                    if (line == string.Empty) continue;

                    var lineArray = line.Split('\t');
                    var cleavedCount = int.Parse(lineArray[7]);
                    var descriptor = lineArray[10];

                    var counter = 0;
                    for (int i = 0; i < descriptor.Length; i++) {
                        if (descriptor[i] == '-' || descriptor[i] == '"') continue;
                        if (counter > descriptorList.Count - 1) break;
                        switch (cleavedCount) {
                            case 0:
                                zeroStatistics[descriptorList[counter]] += int.Parse(descriptor[i].ToString());
                                break;
                            case 1:
                                oneStatistics[descriptorList[counter]] += int.Parse(descriptor[i].ToString());
                                break;
                            case 2:
                                twoStatistics[descriptorList[counter]] += int.Parse(descriptor[i].ToString());
                                break;
                            case 3:
                                threeStatistics[descriptorList[counter]] += int.Parse(descriptor[i].ToString());
                                break;
                            case 4:
                                fourStatistics[descriptorList[counter]] += int.Parse(descriptor[i].ToString());
                                break;
                        }
                        counter++;
                    }

                    var descriptorArray = descriptor.Split('-');
                    var firstLayer = descriptorArray[0];
                    var secondLayer = descriptorArray[1];
                    var thirdLayer = descriptorArray[2];
                    var fullLayer = descriptor;

                    switch (cleavedCount) {
                        case 0:
                          
                            if (!zeroFirstLayerDlStatistics.ContainsKey(firstLayer)) zeroFirstLayerDlStatistics[firstLayer] = 1;
                            else zeroFirstLayerDlStatistics[firstLayer]++;

                            if (!zeroSecondLayerDlStatistics.ContainsKey(secondLayer)) zeroSecondLayerDlStatistics[secondLayer] = 1;
                            else zeroSecondLayerDlStatistics[secondLayer]++;

                            if (!zeroThirdLayerDlStatistics.ContainsKey(thirdLayer)) zeroThirdLayerDlStatistics[thirdLayer] = 1;
                            else zeroThirdLayerDlStatistics[thirdLayer]++;

                            if (!zeroFullLayerDlStatistics.ContainsKey(fullLayer)) zeroFullLayerDlStatistics[fullLayer] = 1;
                            else zeroFullLayerDlStatistics[fullLayer]++;

                            break;
                        case 1:
                            
                            if (!oneFirstLayerDlStatistics.ContainsKey(firstLayer)) oneFirstLayerDlStatistics[firstLayer] = 1;
                            else oneFirstLayerDlStatistics[firstLayer]++;

                            if (!oneSecondLayerDlStatistics.ContainsKey(secondLayer)) oneSecondLayerDlStatistics[secondLayer] = 1;
                            else oneSecondLayerDlStatistics[secondLayer]++;

                            if (!oneThirdLayerDlStatistics.ContainsKey(thirdLayer)) oneThirdLayerDlStatistics[thirdLayer] = 1;
                            else oneThirdLayerDlStatistics[thirdLayer]++;

                            if (!oneFullLayerDlStatistics.ContainsKey(fullLayer)) oneFullLayerDlStatistics[fullLayer] = 1;
                            else oneFullLayerDlStatistics[fullLayer]++;

                            break;
                        case 2:

                            if (!twoFirstLayerDlStatistics.ContainsKey(firstLayer)) twoFirstLayerDlStatistics[firstLayer] = 1;
                            else twoFirstLayerDlStatistics[firstLayer]++;

                            if (!twoSecondLayerDlStatistics.ContainsKey(secondLayer)) twoSecondLayerDlStatistics[secondLayer] = 1;
                            else twoSecondLayerDlStatistics[secondLayer]++;

                            if (!twoThirdLayerDlStatistics.ContainsKey(thirdLayer)) twoThirdLayerDlStatistics[thirdLayer] = 1;
                            else twoThirdLayerDlStatistics[thirdLayer]++;

                            if (!twoFullLayerDlStatistics.ContainsKey(fullLayer)) twoFullLayerDlStatistics[fullLayer] = 1;
                            else twoFullLayerDlStatistics[fullLayer]++;

                            break;
                        case 3:

                            if (!threeFirstLayerDlStatistics.ContainsKey(firstLayer)) threeFirstLayerDlStatistics[firstLayer] = 1;
                            else threeFirstLayerDlStatistics[firstLayer]++;

                            if (!threeSecondLayerDlStatistics.ContainsKey(secondLayer)) threeSecondLayerDlStatistics[secondLayer] = 1;
                            else threeSecondLayerDlStatistics[secondLayer]++;

                            if (!threeThirdLayerDlStatistics.ContainsKey(thirdLayer)) threeThirdLayerDlStatistics[thirdLayer] = 1;
                            else threeThirdLayerDlStatistics[thirdLayer]++;

                            if (!threeFullLayerDlStatistics.ContainsKey(fullLayer)) threeFullLayerDlStatistics[fullLayer] = 1;
                            else threeFullLayerDlStatistics[fullLayer]++;

                            break;
                        case 4:

                            if (!fourFirstLayerDlStatistics.ContainsKey(firstLayer)) fourFirstLayerDlStatistics[firstLayer] = 1;
                            else fourFirstLayerDlStatistics[firstLayer]++;

                            if (!fourSecondLayerDlStatistics.ContainsKey(secondLayer)) fourSecondLayerDlStatistics[secondLayer] = 1;
                            else fourSecondLayerDlStatistics[secondLayer]++;

                            if (!fourThirdLayerDlStatistics.ContainsKey(thirdLayer)) fourThirdLayerDlStatistics[thirdLayer] = 1;
                            else fourThirdLayerDlStatistics[thirdLayer]++;

                            if (!fourFullLayerDlStatistics.ContainsKey(fullLayer)) fourFullLayerDlStatistics[fullLayer] = 1;
                            else fourFullLayerDlStatistics[fullLayer]++;

                            break;
                    }
                    #endregion
                    lineCount++;

                    Console.Write("Done {0} / Total {1}", lineCount, totalLineCount);
                    Console.SetCursorPosition(0, Console.CursorTop);
                }

                Console.WriteLine();
                Console.WriteLine("Writing...");

                var inputfilename = System.IO.Path.GetFileNameWithoutExtension(input);
                var outputFolder = System.IO.Path.GetDirectoryName(input);
                var outputPropStatistics = outputFolder + "\\" + inputfilename + "-PropertyStatistics.txt";
                var outputFirstLayerStatistics = outputFolder + "\\" + inputfilename + "-FirstLayerStatistics.txt";
                var outputSecondLayerStatistics = outputFolder + "\\" + inputfilename + "-SecondLayerStatistics.txt";
                var outputThirdLayerStatistics = outputFolder + "\\" + inputfilename + "-ThirdLayerStatistics.txt";
                var outputFullLayerStatistics = outputFolder + "\\" + inputfilename + "-FullLayerStatistics.txt";

                #region
                using (var sw = new StreamWriter(outputPropStatistics, false, Encoding.ASCII)) {
                    sw.WriteLine("Descriptor\tZero cleaved\tOne cleaved\tTwo cleaved\tThree cleaved\tFour cleaved");
                    foreach (var header in descriptorList) {
                        sw.WriteLine(header + "\t" + zeroStatistics[header] + "\t" + oneStatistics[header] + "\t" + twoStatistics[header] + "\t" + threeStatistics[header] + "\t" + fourStatistics[header]);
                    }
                }

                using (var sw = new StreamWriter(outputFirstLayerStatistics, false, Encoding.ASCII)) {
                    sw.WriteLine("Cleaved count\tDescriptor\tCount");
                    foreach (var prop in zeroFirstLayerDlStatistics) {
                        sw.WriteLine("0\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in oneFirstLayerDlStatistics) {
                        sw.WriteLine("1\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in twoFirstLayerDlStatistics) {
                        sw.WriteLine("2\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in threeFirstLayerDlStatistics) {
                        sw.WriteLine("3\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in fourFirstLayerDlStatistics) {
                        sw.WriteLine("4\t" + prop.Key + "\t" + prop.Value);
                    }
                }

                using (var sw = new StreamWriter(outputSecondLayerStatistics, false, Encoding.ASCII)) {
                    sw.WriteLine("Cleaved count\tDescriptor\tCount");
                    foreach (var prop in zeroSecondLayerDlStatistics) {
                        sw.WriteLine("0\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in oneSecondLayerDlStatistics) {
                        sw.WriteLine("1\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in twoSecondLayerDlStatistics) {
                        sw.WriteLine("2\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in threeSecondLayerDlStatistics) {
                        sw.WriteLine("3\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in fourSecondLayerDlStatistics) {
                        sw.WriteLine("4\t" + prop.Key + "\t" + prop.Value);
                    }
                }

                using (var sw = new StreamWriter(outputThirdLayerStatistics, false, Encoding.ASCII)) {
                    sw.WriteLine("Cleaved count\tDescriptor\tCount");
                    foreach (var prop in zeroThirdLayerDlStatistics) {
                        sw.WriteLine("0\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in oneThirdLayerDlStatistics) {
                        sw.WriteLine("1\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in twoThirdLayerDlStatistics) {
                        sw.WriteLine("2\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in threeThirdLayerDlStatistics) {
                        sw.WriteLine("3\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in fourThirdLayerDlStatistics) {
                        sw.WriteLine("4\t" + prop.Key + "\t" + prop.Value);
                    }
                }

                using (var sw = new StreamWriter(outputFullLayerStatistics, false, Encoding.ASCII)) {
                    sw.WriteLine("Cleaved count\tDescriptor\tCount");
                    foreach (var prop in zeroFullLayerDlStatistics) {
                        sw.WriteLine("0\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in oneFullLayerDlStatistics) {
                        sw.WriteLine("1\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in twoFullLayerDlStatistics) {
                        sw.WriteLine("2\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in threeFullLayerDlStatistics) {
                        sw.WriteLine("3\t" + prop.Key + "\t" + prop.Value);
                    }

                    foreach (var prop in fourFullLayerDlStatistics) {
                        sw.WriteLine("4\t" + prop.Key + "\t" + prop.Value);
                    }
                }
                #endregion
            }
            Console.WriteLine("Analysis finished: {0}", input);

        }

        //public static void MergeDescriptorStatistics(string[] files, string output)
        //{
        //    var descriptorList = BondDescriptor.BondDescriptorList();
        //    var descriptorProps = new List<DescriptorProperty>();
        //    var filenames = new List<string>(); foreach (var file in files) { filenames.Add(System.IO.Path.GetFileNameWithoutExtension(file)); }

        //    //make matrix
        //    foreach (var file in files) {
        //        using (var sr = new StreamReader(file, Encoding.ASCII)) {
        //            sr.ReadLine();
        //            while (sr.Peek() > -1) {
        //                var line = sr.ReadLine().Trim();
        //                if (line == string.Empty) continue;

        //                var filename = System.IO.Path.GetFileNameWithoutExtension(file);
        //                var lineArray = line.Split('\t');
        //                var cleavedCount = int.Parse(lineArray[0]);
        //                var cleavedCountFileNameString = cleavedCount + "$" + filename;
        //                var descString = lineArray[1];
        //                var count = int.Parse(lineArray[2]);


        //                var flg = false;
        //                foreach (var prop in descriptorProps) {
        //                    if (prop.Descriptor == descString) {
        //                        prop.FileCountPair[cleavedCountFileNameString] = count;
        //                        flg = true;
        //                        break;
        //                    }
        //                }
        //                if (flg == false) {
        //                    var descprop = new DescriptorProperty() {
        //                        Descriptor = descString
        //                    };
        //                    for (int i = 0; i <= 4; i++) {
        //                        foreach (var name in filenames) {
        //                            descprop.FileCountPair[i + "$" + name] = 0;
        //                        }
        //                    }

        //                    descprop.FileCountPair[cleavedCountFileNameString] = count;
        //                    descriptorProps.Add(descprop);
        //                }
        //            }
        //        }
        //    }

        //    descriptorProps = descriptorProps.OrderBy(n => n.Descriptor).ToList();
        //    using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
        //        sw.Write("Descriptor hash\tDescriptor string\t");
        //        for (int i = 0; i < 5; i++) {
        //            foreach (var filename in filenames) {
        //                sw.Write(i + " cleaved\t");
        //            }
        //        }
        //        sw.WriteLine();

        //        sw.Write("Descriptor hash\tDescriptor string\t");
        //        for (int i = 0; i < 5; i++) {
        //            foreach (var filename in filenames) {
        //                sw.Write(filename + "\t");
        //            }
        //        }
        //        sw.WriteLine();

        //        for (int i = 0; i < descriptorProps.Count; i++) {
        //            var layer = 1;
        //            if (descriptorProps[i].Descriptor.Length == 482) layer = 2; else if (descriptorProps[i].Descriptor.Length == 1378) layer = 3; else if (descriptorProps[i].Descriptor.Length > 1378) layer = 0;
        //            var descriptorString = BondDescriptorHashKeyConverter.ConvertHashKeyToStrings(descriptorProps[i].Descriptor, descriptorList, layer);

        //            sw.Write(descriptorProps[i].Descriptor + "\t" + descriptorString + "\t");

        //            for (int j = 0; j < 5; j++) {
        //                foreach (var filename in filenames) {
        //                    var keystring = j + "$" + filename;
        //                    sw.Write(descriptorProps[i].FileCountPair[keystring] + "\t");
        //                }
        //            }
        //            sw.WriteLine();
        //        }
        //    }
        //}

        public static void MergeDescriptorPropStatistics(string[] files, string output)
        {
            var filenames = new List<string>(); foreach (var file in files) { filenames.Add(System.IO.Path.GetFileNameWithoutExtension(file)); }
            var descriptorsList = new List<List<string[]>>();

            //make matrix
            foreach (var file in files) {
                var descriptors = new List<string[]>();
                using (var sr = new StreamReader(file, Encoding.ASCII)) {
                    sr.ReadLine();
                    while (sr.Peek() > -1) {
                        var line = sr.ReadLine().Trim();
                        if (line == string.Empty) continue;

                        var lineArray = line.Split('\t');
                        descriptors.Add(lineArray);
                    }
                }
                descriptorsList.Add(descriptors);
            }

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.Write("Descriptor\t");
                for (int i = 0; i < 5; i++) {
                    foreach (var filename in filenames) {
                        sw.Write(i + " cleaved\t");
                    }
                }
                sw.WriteLine();

                sw.Write("Descriptor\t");
                for (int i = 0; i < 5; i++) {
                    foreach (var filename in filenames) {
                        sw.Write(filename + "\t");
                    }
                }
                sw.WriteLine();

                for (int i = 0; i < descriptorsList[0].Count; i++) {
                    sw.Write(descriptorsList[0][i][0] + "\t");

                    for (int k = 1; k <= 5; k++) {
                        for (int j = 0; j < descriptorsList.Count; j++) {
                            sw.Write(descriptorsList[j][i][k] + "\t");
                        }
                    }
                    sw.WriteLine();
                }
            }


        }
        #endregion

        public static void Run(string input, string zeroBondPath, string firstBondPath, 
            string secondBondPath, string thirdBondPath, string errorPath)
        {
            var smilesList = new List<string>();
            using (var sr = new StreamReader(input, Encoding.ASCII)) {
                while (sr.Peek() > -1) {
                    var line = sr.ReadLine().Trim();
                    if (line == string.Empty) continue;
                    smilesList.Add(line);
                }
            }

            var zeroBondPaths = new List<string>();
            var firstBondPaths = new List<string>();
            var secondBondPaths = new List<string>();
            var thirdBondPaths = new List<string>();
            var errorStrings = new List<string>();

            for (int i = 0; i < smilesList.Count; i++) {
                var errorString = string.Empty;
                var smiles = smilesList[i];
                var structure = MoleculeConverter.SmilesToStructure(smiles, out errorString);
                if (structure == null) {
                    Console.WriteLine("SMILES {0}, Error {1}", smiles, errorString);
                    errorStrings.Add(smiles + "\t" + errorString);
                    continue;
                }

                var tempZeroBondPaths = new List<string>();
                var tempFirstBondPaths = new List<string>();
                var tempSecondBondPaths = new List<string>();
                var tempThirdBondPaths = new List<string>();

                BondStatistics.BondConnectivityListGenerator(structure, out tempZeroBondPaths,
                    out tempFirstBondPaths, out tempSecondBondPaths, out tempThirdBondPaths);

                foreach (var path in tempZeroBondPaths) {
                    if (!zeroBondPaths.Contains(path)) zeroBondPaths.Add(path);
                }

                foreach (var path in tempFirstBondPaths) {
                    if (!firstBondPaths.Contains(path)) firstBondPaths.Add(path);
                }

                foreach (var path in tempSecondBondPaths) {
                    if (!secondBondPaths.Contains(path)) secondBondPaths.Add(path);
                }

                foreach (var path in tempThirdBondPaths) {
                    if (!thirdBondPaths.Contains(path)) thirdBondPaths.Add(path);
                }

                Console.Write("Progress {0}, zero path {1}, first path {2}, second path {3}, third path {4}"
                    , i + "/" + smilesList.Count, zeroBondPaths.Count, firstBondPaths.Count, secondBondPaths.Count, thirdBondPaths.Count);
                Console.SetCursorPosition(0, Console.CursorTop);
            }

            Console.WriteLine("Writing results");

            using (var sw = new StreamWriter(zeroBondPath, false, Encoding.ASCII)) {
                foreach (var path in zeroBondPaths) sw.WriteLine(path);
            }

            using (var sw = new StreamWriter(firstBondPath, false, Encoding.ASCII)) {
                foreach (var path in firstBondPaths) sw.WriteLine(path);
            }

            using (var sw = new StreamWriter(secondBondPath, false, Encoding.ASCII)) {
                foreach (var path in secondBondPaths) sw.WriteLine(path);
            }

            using (var sw = new StreamWriter(thirdBondPath, false, Encoding.ASCII)) {
                foreach (var path in thirdBondPaths) sw.WriteLine(path);
            }

            using (var sw = new StreamWriter(errorPath, false, Encoding.ASCII)) {
                foreach (var path in errorStrings) sw.WriteLine(path);
            }
        }

        //public static void StatisticsOfMatchedFragmentIons(string folderPath)
        //{
        //    Console.WriteLine(folderPath);
        //    var param = new AnalysisParamOfMsfinder() { Mass2Tolerance = 0.01F, MassTolType = MassToleranceType.Da, TreeDepth = 2 };
        //    var files = System.IO.Directory.GetFiles(folderPath, "*.msp", System.IO.SearchOption.TopDirectoryOnly);
        //    var neutralLossFile = System.IO.Path.GetDirectoryName(files[0]) + "-NL.txt";
        //    var productIonFile = System.IO.Path.GetDirectoryName(files[0]) + "-PI.txt";
        //    var descriptorFile = System.IO.Path.GetDirectoryName(files[0]) + "-DF-All.txt";
        //    var descriptorInitial = BondDescriptor.BondDescriptorDictionary();
        //    var descriptorHeader = string.Empty;
        //    foreach (var prop in descriptorInitial) descriptorHeader += prop.Key + "\t";

        //    using (var swNL = new StreamWriter(neutralLossFile, false, Encoding.ASCII)) {
        //        using (var swPI = new StreamWriter(productIonFile, false, Encoding.ASCII)) {
        //            using (var swDC = new StreamWriter(descriptorFile, false, Encoding.ASCII)) {

        //                swNL.Write("InChIKey\tShort InChIKey\tSMILES\tAdduct\tCollision energy\tInstrument\tInstrument type\tFragment SMILES\tFragment exact mass\tFragment HR corrected mass\t");
        //                swNL.Write("Matched peak mz\tMatched peak intensity\tAssigned adduct mass\tTree depth\tCleaved count\tCleaved atoms string\tHydrogen rearrangement\t");
        //                swNL.WriteLine(descriptorHeader);

        //                swPI.Write("InChIKey\tShort InChIKey\tSMILES\tAdduct\tCollision energy\tInstrument\tInstrument type\tFragment SMILES\tFragment exact mass\tFragment HR corrected mass\t");
        //                swPI.Write("Matched peak mz\tMatched peak intensity\tAssigned adduct mass\tTree depth\tCleaved count\tCleaved atoms string\tHydrogen rearrangement\t");
        //                swPI.WriteLine(descriptorHeader);

        //                swDC.Write("InChIKey\tShort InChIKey\tSMILES\tAdduct\tCollision energy\tInstrument\tInstrument type\tIs cleaved\tMatch mass\tMatch intensity\t");
        //                swDC.WriteLine(descriptorHeader);

        //                var counter = 0;
        //                foreach (var file in files) {
        //                    var molecule = RawDataParcer.RawDataFileReader(file, param);
        //                    var inchikey = molecule.InchiKey;
        //                    var shortInChiKey = inchikey.Substring(0, 14);
        //                    var instrument = molecule.Instrument;
        //                    var instrumentType = molecule.InstrumentType;
        //                    var smiles = molecule.Smiles;
        //                    var ce = molecule.CollisionEnergy.ToString();
        //                    var adduct = molecule.PrecursorType;

        //                    var errorString = string.Empty;
        //                    var structure = MoleculeConverter.SmilesToStructure(molecule.Smiles, out errorString);
        //                    var fragments = FragmentGenerator.GetFragmentCandidates(structure, 2, (float)molecule.Ms2Spectrum.PeakList.Min(n => n.Mz));
        //                    var peakFragPairs = FragmentPeakMatcher.GetSpectralAssignmentResult(fragments, molecule.Ms2Spectrum.PeakList,
        //                        AdductIonParcer.GetAdductIonBean(molecule.PrecursorType), param.TreeDepth,
        //                        (float)param.Mass2Tolerance, param.MassTolType, molecule.IonMode);

        //                    var matchedNeutralLosses = new List<MatchedIon>();
        //                    var matchedProductIons = new List<MatchedIon>();
        //                    var unCleavedBondDescriptors = new List<string>();
        //                    BondStatistics.BondEnvironmentalFingerprintGenerator(structure, molecule, fragments, peakFragPairs
        //                        , out matchedProductIons, out matchedNeutralLosses, out unCleavedBondDescriptors);

        //                    writeMatchedIonInformation(swNL, matchedNeutralLosses, inchikey, shortInChiKey,
        //                        smiles, adduct, ce, instrument, instrumentType);

        //                    writeMatchedIonInformation(swPI, matchedProductIons, inchikey, shortInChiKey,
        //                       smiles, adduct, ce, instrument, instrumentType);

        //                    writeFingerPringsResult(swDC, matchedProductIons, unCleavedBondDescriptors, inchikey, shortInChiKey,
        //                       smiles, adduct, ce, instrument, instrumentType);
        //                    counter++;
        //                    Console.WriteLine(counter + "/" + files.Length + "\t" + System.IO.Path.GetFileNameWithoutExtension(file));
        //                }
        //            }
        //        }
        //    }
        //}


        public static void StatisticsOfMatchedFragmentIons(string folderPath, string tag)
        {
            Console.WriteLine(folderPath);
            var param = new AnalysisParamOfMsfinder() { Mass2Tolerance = 0.025F, RelativeAbundanceCutOff = 1, MassTolType = MassToleranceType.Da, TreeDepth = 2 };
            var files = System.IO.Directory.GetFiles(folderPath, "*.msp", System.IO.SearchOption.TopDirectoryOnly);
            var neutralLossFile = System.IO.Path.GetDirectoryName(files[0]) + tag + "-NL.txt";
            var productIonFile = System.IO.Path.GetDirectoryName(files[0]) + tag + "-PI.txt";

            using (var swNL = new StreamWriter(neutralLossFile, false, Encoding.ASCII)) {
                using (var swPI = new StreamWriter(productIonFile, false, Encoding.ASCII)) {

                        swNL.Write("InChIKey\tShort InChIKey\tSMILES\tAdduct\tCollision energy\tInstrument\tInstrument type\tFragment SMILES\tFragment exact mass\tFragment HR corrected mass\t");
                        swNL.WriteLine("Matched peak mz\tMatched peak intensity\tAssigned adduct mass\tTree depth\tCleaved count\tCleaved atoms string\tHydrogen rearrangement\t");

                        swPI.Write("InChIKey\tShort InChIKey\tSMILES\tAdduct\tCollision energy\tInstrument\tInstrument type\tFragment SMILES\tFragment exact mass\tFragment HR corrected mass\t");
                        swPI.WriteLine("Matched peak mz\tMatched peak intensity\tAssigned adduct mass\tTree depth\tCleaved count\tCleaved atoms string\tHydrogen rearrangement\t");

                        var counter = 0;
                        foreach (var file in files) {
                            var molecule = RawDataParcer.RawDataFileReader(file, param);
                            var inchikey = molecule.InchiKey;
                            var shortInChiKey = inchikey.Substring(0, 14);
                            var instrument = molecule.Instrument;
                            var instrumentType = molecule.InstrumentType;
                            var smiles = molecule.Smiles;
                            var ce = molecule.CollisionEnergy.ToString();
                            var adduct = molecule.PrecursorType;

                            var errorString = string.Empty;
                            var structure = MoleculeConverter.SmilesToStructure(molecule.Smiles, out errorString);
                            if (molecule.Ms2Spectrum.PeakList.Count > 0 && structure != null) {
                                var fragments = FragmentGenerator.GetFragmentCandidates(structure, 2, (float)molecule.Ms2Spectrum.PeakList.Min(n => n.Mz));
                                var peakFragPairs = FragmentPeakMatcher.GetSpectralAssignmentResult(structure, fragments, molecule.Ms2Spectrum.PeakList,
                                    AdductIonParcer.GetAdductIonBean(molecule.PrecursorType), param.TreeDepth,
                                    (float)param.Mass2Tolerance, param.MassTolType, molecule.IonMode);

                                var matchedNeutralLosses = new List<MatchedIon>();
                                var matchedProductIons = new List<MatchedIon>();
                                var unCleavedBondDescriptors = new List<string>();
                                BondStatistics.BondEnvironmentalFingerprintGenerator(structure, molecule, fragments, peakFragPairs
                                    , out matchedProductIons, out matchedNeutralLosses, out unCleavedBondDescriptors);

                                writeMatchedIonInformation(swNL, matchedNeutralLosses, inchikey, shortInChiKey,
                                    smiles, adduct, ce, instrument, instrumentType);

                                writeMatchedIonInformation(swPI, matchedProductIons, inchikey, shortInChiKey,
                                   smiles, adduct, ce, instrument, instrumentType);
                            }

                            counter++;
                            Console.WriteLine(counter + "/" + files.Length + "\t" + System.IO.Path.GetFileNameWithoutExtension(file));
                    }
                }
            }
        }


        private static void writeMatchedIonInformation(StreamWriter sw, List<MatchedIon> matchedIons, string inchikey, string shortInChiKey,
            string smiles, string adduct, string ce, string instrument, string instrumentType)
        {
            foreach (var ion in matchedIons) {

                var descriptSum = sumDescriptors(ion.BondDescriptorsList);
                sw.WriteLine(inchikey + "\t" + shortInChiKey + "\t" + smiles + "\t" + adduct
                  + "\t" + ce + "\t" + instrument + "\t" + instrumentType
                  + "\t" + ion.Smiles + "\t" + ion.Exactmass + "\t" + ion.MatchedMass
                  + "\t" + ion.MatchedMz + "\t" + ion.MatchedIntensity + "\t" + ion.AssignedAdductMass
                  + "\t" + ion.TreeDepth + "\t" + ion.CleavedCount + "\t" + ion.CleavedAtomBonds + "\t" + ion.RearrangedHydrogen
                  + "\t" + descriptSum);


                //foreach (var descript in ion.BondDescriptorsList) {
                //    sw.WriteLine(inchikey + "\t" + shortInChiKey + "\t" + smiles + "\t" + adduct
                //   + "\t" + ce + "\t" + instrument + "\t" + instrumentType
                //   + "\t" + ion.Smiles + "\t" + ion.Exactmass + "\t" + ion.MatchedMass
                //   + "\t" + ion.MatchedMz + "\t" + ion.MatchedIntensity + "\t" + ion.AssignedAdductMass
                //   + "\t" + ion.TreeDepth + "\t" + ion.CleavedCount + "\t" + ion.CleavedAtomBonds + "\t" + ion.RearrangedHydrogen
                //   + "\t" + descript);
                //}
            }
        }

        private static void writeFingerPringsResult(StreamWriter sw, List<MatchedIon> matchedIons, List<string> uncleavedDescriptors, string inchikey, string shortInChiKey,
            string smiles, string adduct, string ce, string instrument, string instrumentType)
        {
            foreach (var ion in matchedIons) {
                //var descriptSum = sumDescriptors(ion.BondDescriptorsList);
                //sw.WriteLine(inchikey + "\t" + shortInChiKey + "\t" + smiles + "\t" + adduct
                //   + "\t" + ce + "\t" + instrument + "\t" + instrumentType + "\t" + ion.BondDescriptorsList.Count
                //   + "\t" + ion.MatchedMass + "\t" + ion.MatchedIntensity
                //   + "\t" + descriptSum);

                foreach (var descript in ion.BondDescriptorsList) {
                    sw.WriteLine(inchikey + "\t" + shortInChiKey + "\t" + smiles + "\t" + adduct
                   + "\t" + ce + "\t" + instrument + "\t" + instrumentType + "\t" + ion.BondDescriptorsList.Count
                   + "\t" + ion.MatchedMass + "\t" + ion.MatchedIntensity
                   + "\t" + descript);
                }
            }

            foreach (var descript in uncleavedDescriptors) {
                sw.WriteLine(inchikey + "\t" + shortInChiKey + "\t" + smiles + "\t" + adduct
                   + "\t" + ce + "\t" + instrument + "\t" + instrumentType + "\t" + "0"
                   + "\t" + descript); // matched mass1 and matched mass2 are included in descript
            }
        }

        private static string sumDescriptors(List<string> descriptorList)
        {
            var descriptor = new List<int>();
            for (int i = 0; i < descriptorList[0].Split('\t').Length - 1; i++) {
                descriptor.Add(0);
            }

            foreach (var desc in descriptorList) {
                var value = desc.Split('\t').ToList();
                for (int i = 0; i < value.Count - 1; i++) {
                    descriptor[i] += int.Parse(value[i]);
                }
            }

            var descriptorString = string.Empty;
            foreach (var desc in descriptor) {
                descriptorString += desc.ToString() + "\t";
            }

            return descriptorString;
        }

    }


}
