using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace StructureFinderConsoleApp {

    public class ParentFragmentPairs {
        private string parentShortInChIKey;
        private string parentSmiles;
        private string parentAdduct;

        private List<string> frequentFragInChIKeys;
        private List<string> mergedFragInChIKeys;
        private List<string> productIonInChIKeys;
        private List<string> neutralLossInChIKeys;

        public ParentFragmentPairs() {
            frequentFragInChIKeys = new List<string>();
            mergedFragInChIKeys = new List<string>();
            productIonInChIKeys = new List<string>();
            neutralLossInChIKeys = new List<string>();
        }

        public string ParentShortInChIKey { get => parentShortInChIKey; set => parentShortInChIKey = value; }
        public string ParentSmiles { get => parentSmiles; set => parentSmiles = value; }
        public string ParentAdduct { get => parentAdduct; set => parentAdduct = value; }
        public List<string> FrequentFragInChIKeys { get => frequentFragInChIKeys; set => frequentFragInChIKeys = value; }
        public List<string> MergedFragInChIKeys { get => mergedFragInChIKeys; set => mergedFragInChIKeys = value; }
        public List<string> ProductIonInChIKeys { get => productIonInChIKeys; set => productIonInChIKeys = value; }
        public List<string> NeutralLossInChIKeys { get => neutralLossInChIKeys; set => neutralLossInChIKeys = value; }
    }

    public sealed class MergeFragmentInfoManager {

        public static void Convert(string inputPI, string inputNL, string inputFreq, string output) {

            var arrayList = new List<string[]>();
            var frequentFrags = new List<string>();

            using (var sr = new StreamReader(inputFreq, Encoding.UTF8)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        break;
                    frequentFrags.Add(line);
                }
            }

            using (var sr = new StreamReader(inputPI, Encoding.UTF8)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        break;

                    var lineArray = line.Split('\t');
                    arrayList.Add(new string[] { lineArray[0], lineArray[1], lineArray[2], lineArray[5], "Product ion" });
                }
            }

            using (var sr = new StreamReader(inputNL, Encoding.UTF8)) {
                sr.ReadLine();

                while (sr.Peek() > -1) {
                    var line = sr.ReadLine();
                    if (line == string.Empty)
                        break;

                    var lineArray = line.Split('\t');
                    arrayList.Add(new string[] { lineArray[0], lineArray[1], lineArray[2], lineArray[5], "Neutral loss" });
                }
            }

            arrayList = arrayList.OrderBy(n => n[0]).ToList();

            var firstParentFragmentPairs = new ParentFragmentPairs() {
                ParentShortInChIKey = arrayList[0][0],
                ParentSmiles = arrayList[0][1],
                ParentAdduct = arrayList[0][2],
            };

            if (arrayList[0][4] == "Product ion")
                firstParentFragmentPairs.ProductIonInChIKeys = new List<string>() { arrayList[0][3] };
            else
                firstParentFragmentPairs.NeutralLossInChIKeys = new List<string>() { arrayList[0][3] };

            var parentFragmentPairsList = new List<ParentFragmentPairs>() {
                firstParentFragmentPairs
            };

            var pairLast = new ParentFragmentPairs();
            var mergeInChIKeys = new List<string>();
            for (int i = 1; i < arrayList.Count; i++) {

                var array = arrayList[i];
                pairLast = parentFragmentPairsList[parentFragmentPairsList.Count - 1];

                if (array[0] == pairLast.ParentShortInChIKey) {
                    #region
                    if (array[4] == "Product ion" && !pairLast.ProductIonInChIKeys.Contains(array[3])) {
                        pairLast.ProductIonInChIKeys.Add(array[3]);
                    }
                    else if (array[4] == "Neutral loss" && !pairLast.NeutralLossInChIKeys.Contains(array[3])) {
                        pairLast.NeutralLossInChIKeys.Add(array[3]);
                    }
                    #endregion
                }
                else {
                    #region
                    mergeInChIKeys = new List<string>();
                    foreach (var key in pairLast.ProductIonInChIKeys) {
                        if (!mergeInChIKeys.Contains(key))
                            mergeInChIKeys.Add(key);
                    }

                    foreach (var key in pairLast.NeutralLossInChIKeys) {
                        if (!mergeInChIKeys.Contains(key))
                            mergeInChIKeys.Add(key);
                    }

                    pairLast.MergedFragInChIKeys = mergeInChIKeys;
                    foreach (var key in mergeInChIKeys) {
                        if (frequentFrags.Contains(key))
                            pairLast.FrequentFragInChIKeys.Add(key);
                    }

                    var newPairs = new ParentFragmentPairs() {
                        ParentShortInChIKey = array[0],
                        ParentSmiles = array[1],
                        ParentAdduct = array[2],
                    };

                    if (array[4] == "Product ion")
                        newPairs.ProductIonInChIKeys = new List<string>() { array[3] };
                    else
                        newPairs.NeutralLossInChIKeys = new List<string>() { array[3] };

                    parentFragmentPairsList.Add(newPairs);
                    #endregion
                }
            }
            //reminder
            #region
            pairLast = parentFragmentPairsList[parentFragmentPairsList.Count - 1];
            mergeInChIKeys = new List<string>();
            foreach (var key in pairLast.ProductIonInChIKeys) {
                if (!mergeInChIKeys.Contains(key))
                    mergeInChIKeys.Add(key);
            }

            foreach (var key in pairLast.NeutralLossInChIKeys) {
                if (!mergeInChIKeys.Contains(key))
                    mergeInChIKeys.Add(key);
            }

            pairLast.MergedFragInChIKeys = mergeInChIKeys;

            foreach (var key in mergeInChIKeys) {
                if (frequentFrags.Contains(key))
                    pairLast.FrequentFragInChIKeys.Add(key);
            }
            #endregion

            using (var sw = new StreamWriter(output, false, Encoding.ASCII)) {
                sw.WriteLine("Parent inchikey\tSMILES\tAdduct\tFragment inchikeys (frequent)\tFrequent fragment count\tFragment inchikeys (merged)\tFragment inchikeys (product ion)\tFragment inchikeys (neutral loss)");

                foreach (var pair in parentFragmentPairsList) {

                    sw.Write(pair.ParentShortInChIKey + "\t" + pair.ParentSmiles + "\t" + pair.ParentAdduct + "\t");

                    var keys = string.Empty;
                    for (int i = 0; i < pair.FrequentFragInChIKeys.Count; i++) {
                        var key = pair.FrequentFragInChIKeys[i];
                        if (i == pair.FrequentFragInChIKeys.Count - 1)
                            keys += key;
                        else
                            keys += key + ";";
                    }
                    sw.Write(keys + "\t");
                    sw.Write(pair.FrequentFragInChIKeys.Count + "\t");

                    keys = string.Empty;
                    for (int i = 0; i < pair.MergedFragInChIKeys.Count; i++) {
                        var key = pair.MergedFragInChIKeys[i];
                        if (i == pair.MergedFragInChIKeys.Count - 1)
                            keys += key;
                        else
                            keys += key + ";";
                    }
                    sw.Write(keys + "\t");

                    keys = string.Empty;
                    for (int i = 0; i < pair.ProductIonInChIKeys.Count; i++) {
                        var key = pair.ProductIonInChIKeys[i];
                        if (i == pair.ProductIonInChIKeys.Count - 1)
                            keys += key;
                        else
                            keys += key + ";";
                    }
                    sw.Write(keys + "\t");

                    keys = string.Empty;
                    for (int i = 0; i < pair.NeutralLossInChIKeys.Count; i++) {
                        var key = pair.NeutralLossInChIKeys[i];
                        if (i == pair.NeutralLossInChIKeys.Count - 1)
                            keys += key;
                        else
                            keys += key + ";";
                    }
                    sw.WriteLine(keys);
                }
            }
        }
    }
}
