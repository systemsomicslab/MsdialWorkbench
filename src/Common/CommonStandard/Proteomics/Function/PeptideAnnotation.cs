using Accord.Statistics.Distributions.DensityKernels;
using Accord.Statistics.Distributions.Multivariate;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Extension;
using System;
using System.Collections.Generic;
using System.Text;
using System.Linq;

namespace CompMs.Common.Proteomics.Function {

    public class PEPCalcContainer {
        // [0]peptide length, [1]missed cleavage number, [2]modification number
        public Dictionary<Tuple<int, int, int>, MultivariateEmpiricalDistribution> ProbAllDict { get; } = new Dictionary<Tuple<int, int, int>, MultivariateEmpiricalDistribution>();
        public Dictionary<Tuple<int, int, int>, MultivariateEmpiricalDistribution> ProbFalseDict { get; } = new Dictionary<Tuple<int, int, int>, MultivariateEmpiricalDistribution>();
        public Dictionary<Tuple<int, int, int>, bool> ID2Availability { get; } = new Dictionary<Tuple<int, int, int>, bool>();
        public Dictionary<Tuple<int, int, int>, Tuple<int, int, int>> ID2ProbDictUsed { get; } = new Dictionary<Tuple<int, int, int>, Tuple<int, int, int>>();

        public int MiminumPeptideCountForDist { get; } = 10;

        public List<PeptideScore> PeptideScores { get; }
        public bool IsContainerAvailability { get; protected set; } = true;

        public int MaxPepLength { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Max(n => n.PeptideLength); }
        public int MinPepLength { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Min(n => n.PeptideLength); }
        public int MaxMissedCleavages { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Max(n => n.MissedCleavages); }
        public int MinMissedCleavages { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Min(n => n.MissedCleavages); }
        public int MaxModifications { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Max(n => n.Modifications); }
        public int MinModifications { get => PeptideScores.IsEmptyOrNull() ? -1 : PeptideScores.Min(n => n.Modifications); }

        public PEPCalcContainer(List<PeptideScore> peptideScores) {
            PeptideScores = peptideScores;
            if (peptideScores.IsEmptyOrNull()) return;

            var minPepLength = MinPepLength;
            var maxPepLength = MaxPepLength;
            var minMissedCleavages = MinMissedCleavages;
            var maxMissedCleavages = MaxMissedCleavages;
            var minMods = MinModifications;
            var maxMods = MaxModifications;

            var pepArray = GetSeekArray(minPepLength, maxPepLength);
            var cleavageArray = GetSeekArray(minMissedCleavages, maxMissedCleavages);
            var modificationArray = GetSeekArray(minMods, maxMods);
            SetProbDict(pepArray, cleavageArray, modificationArray);
            SetAvailabilityToDictionary();
            SetID2ProbDistUsed(minPepLength, minMissedCleavages, minMods);
        }

        public double GetPosteriorErrorProbability(float score, int peptideLength, int modificationNum, int missedCleavageNum) {
            var key = new Tuple<int, int, int>(peptideLength, missedCleavageNum, modificationNum);
            var keyUsed = ID2ProbDictUsed[key];
            
            //Console.WriteLine("PEPLength\t{0}\tMCleavage\t{1}\tModification\t{2}\tUsedPepLength\t{3}\tUsedMCleavage\t{4}\tUsedModification\t{5}",
            //    peptideLength, missedCleavageNum, modificationNum, keyUsed.Item1, keyUsed.Item2, keyUsed.Item3);

            var probAll = ProbAllDict[keyUsed];
            var probFalse = ProbFalseDict[keyUsed];

            //if (score <= 0.000001) {
            //    score = 0.000001F;
            //}

            var scoreDensity = new double[] { score };
            var prob = 100.0;
            try {
                var probSL = probAll.ProbabilityDensityFunction(scoreDensity);
                var probSLF = probFalse.ProbabilityDensityFunction(scoreDensity);
                prob = 0.5 * probSLF / probSL;
            }
            catch (Accord.NonPositiveDefiniteMatrixException ex) {
                Console.WriteLine(ex.Message);
            }
            
            if (prob == 100.0) {
                var key_all = new Tuple<int, int, int>(-1, -1, -1);
                var keyUsed_all = ID2ProbDictUsed[key_all];

                var probAll_all = ProbAllDict[keyUsed_all];
                var probFalse_all = ProbFalseDict[keyUsed_all];

                try {
                    var probSL = probAll.ProbabilityDensityFunction(scoreDensity);
                    var probSLF = probFalse.ProbabilityDensityFunction(scoreDensity);
                    prob = 0.5 * probSLF / probSL;
                }
                catch (Accord.NonPositiveDefiniteMatrixException ex) {
                    Console.WriteLine(ex.Message);
                }
            }
           
            return prob;
        }

        private void SetID2ProbDistUsed(int minPepLength, int minMissedCleavages, int minMods) {
            foreach (var item in ID2Availability) {
                var key = item.Key;
                if (item.Value == true) {
                    ID2ProbDictUsed[key] = key;
                }
                else {
                    Tuple<int, int, int> newKey = key;
                    while (true) {
                        if (newKey.Item3 != -1 && newKey.Item3 > minMods) {
                            newKey = new Tuple<int, int, int>(newKey.Item1, newKey.Item2, newKey.Item3 - 1);
                            
                        } 
                        else if (newKey.Item3 == minMods) {
                            newKey = new Tuple<int, int, int>(newKey.Item1, newKey.Item2, -1);
                        } 
                        else {
                            if (newKey.Item2 != -1 && newKey.Item2 > minMissedCleavages) {
                                newKey = new Tuple<int, int, int>(newKey.Item1, newKey.Item2 - 1, newKey.Item3);
                            }
                            else if (newKey.Item2 == minMissedCleavages) {
                                newKey = new Tuple<int, int, int>(newKey.Item1, -1, newKey.Item3);
                            }
                            else {
                                if (newKey.Item1 != -1 && newKey.Item1 > minPepLength) {
                                    newKey = new Tuple<int, int, int>(newKey.Item1 - 1, newKey.Item2, newKey.Item3);
                                }
                                else if (newKey.Item1 == minPepLength) {
                                    newKey = new Tuple<int, int, int>(-1, newKey.Item2, newKey.Item3);
                                }
                                else {
                                    break;
                                }
                            }
                        }
                        if (ID2Availability[newKey] == true) {
                            ID2ProbDictUsed[key] = newKey;
                            break;
                        }
                        else {
                            continue;
                        }
                    }
                }
            }
        }

        private int[] GetSeekArray(int min, int max) {
            var array = new List<int>() { -1 };
            for (int i = min; i <= max; i++) {
                array.Add(i);
            }
            return array.ToArray();
        }

        private void SetAvailabilityToDictionary() {
            foreach (var item in ProbAllDict) {
                var key = item.Key;
                if (item.Value != null &&
                    item.Value.Samples.Length > MiminumPeptideCountForDist &&
                    ProbFalseDict[key] != null &&
                    ProbFalseDict[key].Samples.Length > MiminumPeptideCountForDist) {
                    ID2Availability[key] = true;
                }
                else {
                    ID2Availability[key] = false;
                    if (key.Item1 == -1 && key.Item2 == -1 && key.Item3 == -1) {
                        IsContainerAvailability = false;
                        ID2Availability[key] = true; // temp
                    }
                }
            }
        }

        private void SetProbDict(int[] pepArray, int[] cleavageArray, int[] modificationArray) {
            for (int i = 0; i < pepArray.Length; i++) {
                for (int j = 0; j < cleavageArray.Length; j++) {
                    for (int k = 0; k < modificationArray.Length; k++) {
                        var key = new Tuple<int, int, int>(pepArray[i], cleavageArray[j], modificationArray[k]);
                        var pepScores = GetTargetScores(null, key.Item1, key.Item2, key.Item3);
                        var decoyScores = GetTargetScores(true, key.Item1, key.Item2, key.Item3);
                        var pepDist = GetGussianKernelDistribution(pepScores);
                        var decoyDist = GetGussianKernelDistribution(decoyScores);

                        ProbAllDict[key] = pepDist;
                        ProbFalseDict[key] = decoyDist;
                    }
                }
            }
        }

        public MultivariateEmpiricalDistribution GetGussianKernelDistribution(List<PeptideScore> scores) {
            if (scores.IsEmptyOrNull()) return null;
            var array = new double[scores.Count][];
            for (int i = 0; i < scores.Count; i++) {
                array[i] = new double[] { scores[i].AndromedaScore };
            }
            return GetGussianKernelDistribution(array);
        }

        public  MultivariateEmpiricalDistribution GetGussianKernelDistribution(double[][] data, int kernelDimension = 1) {
            IDensityKernel kernel = new GaussianKernel(dimension: kernelDimension);
            var dist = new MultivariateEmpiricalDistribution(kernel, data);
            return dist;
        }



        /// <summary>
        /// -1 to take all queries of the target property
        /// </summary>
        /// <param name="isDecoy"></param>
        /// <param name="pepLength"></param>
        /// <param name="missedCleavages"></param>
        /// <param name="modifications"></param>
        /// <returns></returns>
        public List<PeptideScore> GetTargetScores(bool? isDecoy, int pepLength = -1, int missedCleavages = -1, int modifications = -1) {
            if (PeptideScores.IsEmptyOrNull()) return null;

            if (isDecoy == null) {
                if (pepLength < 0) {
                    if (missedCleavages < 0) {
                        if (modifications < 0) {
                            return PeptideScores;
                        }
                        else {
                            return PeptideScores.Where(n => n.Modifications == modifications).ToList();
                        }
                    }
                    else {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.MissedCleavages == missedCleavages).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.MissedCleavages == missedCleavages && n.Modifications == modifications).ToList();
                        }
                    }
                }
                else {
                    if (missedCleavages < 0) {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.PeptideLength == pepLength).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.PeptideLength == pepLength && n.Modifications == modifications).ToList();
                        }
                    }
                    else {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.PeptideLength == pepLength && n.MissedCleavages == missedCleavages).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.PeptideLength == pepLength && n.MissedCleavages == missedCleavages && n.Modifications == modifications).ToList();
                        }
                    }
                }
            }
            else {
                if (pepLength < 0) {
                    if (missedCleavages < 0) {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.Modifications == modifications).ToList();
                        }
                    }
                    else {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.MissedCleavages == missedCleavages).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.MissedCleavages == missedCleavages && n.Modifications == modifications).ToList();
                        }
                    }
                }
                else {
                    if (missedCleavages < 0) {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.PeptideLength == pepLength).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.PeptideLength == pepLength && n.Modifications == modifications).ToList();
                        }
                    }
                    else {
                        if (modifications < 0) {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.PeptideLength == pepLength && n.MissedCleavages == missedCleavages).ToList();
                        }
                        else {
                            return PeptideScores.Where(n => n.IsDecoy == isDecoy && n.PeptideLength == pepLength && n.MissedCleavages == missedCleavages && n.Modifications == modifications).ToList();
                        }
                    }
                }
            }
        }
    }

    //public class PeptideScoreHistogram {
    //    double BinSize { get; } = 1;
    //    double XMin { get; } = 0; // andromeda score minimum
    //    double XMax { get; } = 1000; // andromeda score max

    //    public double[][] Histogram { get; }
    //    public PeptideScoreHistogram(List<PeptideScore> PeptideScores) {
    //        if (PeptideScores.IsEmptyOrNull()) return;
    //        var bins = new double[(int)((XMax - XMin + 1) / BinSize) + 1];
    //        foreach (var score in PeptideScores) {
    //            var bin = (int)(score.AndromedaScore / BinSize);
    //            bins[bin]++;
    //        }

    //        var counter = 0.0;
    //        var binlist = new List<double[]>();
    //        foreach (var bin in bins) {
    //            var dist = new double[] { XMin + BinSize * counter, bin };
    //            binlist.Add(dist);
    //            counter++;
    //        }
    //        Histogram = binlist.ToArray();
    //    }
    //}

    public sealed class PeptideAnnotation {
        private PeptideAnnotation() { }
        public static MultivariateEmpiricalDistribution GetGussianKernelDistribution(List<PeptideScore> scores) {
            if (scores.IsEmptyOrNull()) return null;
            var array = new double[scores.Count][];
            for (int i = 0; i < scores.Count; i++) {
                array[i] = new double[] { scores[i].AndromedaScore };
            }
            return GetGussianKernelDistribution(array);
        }

        public static MultivariateEmpiricalDistribution GetGussianKernelDistribution(double[][] data, int kernelDimension = 1) {
            IDensityKernel kernel = new GaussianKernel(dimension: kernelDimension);
            var dist = new MultivariateEmpiricalDistribution(kernel, data);
            return dist;
        }

       

    }
}
