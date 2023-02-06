using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Algorithm.Scoring;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.Parser;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Algorithm.Function {

    public class IsotopeTemp {
        public int WeightNumber { get; set; }
        public double Mz { get; set; }
        public double MzClBr { get; set; }
        public double Intensity { get; set; }
        public int PeakID { get; set; }
    }

    public sealed class SpectrumHandler {

        public static List<SpectrumPeak> GetCombinedSpectrum(
             List<SpectrumPeak> peaks1,
             List<SpectrumPeak> peaks2,
             double bin) {

            var peaks = new List<SpectrumPeak>();
            var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

            foreach (var peak in peaks1) {
                var mass = peak.Mass;
                var massframe = (int)(mass / bin);
                if (range2Peaks.ContainsKey(massframe))
                    range2Peaks[massframe].Add(peak);
                else
                    range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
            }

            foreach (var peak in peaks2) {
                var mass = peak.Mass;
                var massframe = (int)(mass / bin);
                if (range2Peaks.ContainsKey(massframe))
                    range2Peaks[massframe].Add(peak);
                else
                    range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
            }

            foreach (var pair in range2Peaks) {
                var maxMass = pair.Value.Argmax(n => n.Intensity).Mass;
                var sumIntensity = pair.Value.Sum(n => n.Intensity) * 0.5;
                peaks.Add(new SpectrumPeak(maxMass, sumIntensity));
            }

            return peaks;
        }

        public static List<SpectrumPeak> GetNormalizedPeaks(List<SpectrumPeak> spectrum, double powFactor, double maxValue) {
            if (spectrum.Count == 0) return new List<SpectrumPeak>();
            var maxIntensity = Math.Pow(spectrum.Max(n => n.Intensity), powFactor);
            return spectrum.Select(n => new SpectrumPeak { Mass = n.Mass, Intensity = Math.Pow(n.Intensity, powFactor) / maxIntensity * maxValue }).ToList();
        }

        public static List<SpectrumPeak> GetBinnedSpectrum(List<SpectrumPeak> spectrum, double delta = 100, int maxPeaks = 12) {

            var peaks = new List<SpectrumPeak>();
            var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

            foreach (var peak in spectrum) {
                var mass = peak.Mass;
                var massframe = (int)(mass / delta);
                if (range2Peaks.ContainsKey(massframe))
                    range2Peaks[massframe].Add(peak);
                else
                    range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
            }

            foreach (var pair in range2Peaks) {
                var counter = 1;
                foreach (var peak in pair.Value.OrderByDescending(n => n.Intensity)) {
                    if (counter > maxPeaks) break;
                    peaks.Add(peak);
                    counter++;
                }
            }
            return peaks;
        }

        public static List<SpectrumPeak> GetBinnedSpectrum(List<SpectrumPeak> spectrum, double bin) {
            var peaks = new List<SpectrumPeak>();
            var range2Peaks = new Dictionary<int, List<SpectrumPeak>>();

            foreach (var peak in spectrum) {
                var mass = peak.Mass;
                var massframe = (int)(mass / bin);
                if (range2Peaks.ContainsKey(massframe))
                    range2Peaks[massframe].Add(peak);
                else
                    range2Peaks[massframe] = new List<SpectrumPeak>() { peak };
            }

            foreach (var pair in range2Peaks) {
                var maxMass = pair.Value.Argmax(n => n.Intensity).Mass;
                var sumIntensity = pair.Value.Sum(n => n.Intensity);
                peaks.Add(new SpectrumPeak(maxMass, sumIntensity));
            }
            return peaks;
        }

        public static List<SpectrumPeak> GetNormalizedPeak4SpectralEntropyCalc(
            List<SpectrumPeak> peaklist,
            double precursorMz,
            double ms2Tol = 0.05,
            double relativeAbundanceCutOff = 0.1,
            double absoluteAbundanceCutOff = 3,
            double minMz = 0,
            double maxMz = 100000) {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            double maxIntensity = peaklist.Max(n => n.Intensity);
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist) {
                if (peak.Mass < minMz) continue;
                if (peak.Mass > maxMz) continue;
                if (peak.Mass > precursorMz + ms2Tol) continue;
                if (peak.Intensity < absoluteAbundanceCutOff) continue;
                if (peak.Intensity >= maxIntensity * relativeAbundanceCutOff * 0.01) {
                    refinedPeaklist.Add(peak);
                }
            }
            var sumIntensity = refinedPeaklist.Sum(n => n.Intensity);
            return refinedPeaklist.Select(n => new SpectrumPeak() { Mass = n.Mass, Intensity = n.Intensity / sumIntensity }).ToList();
        }

        public static List<SpectrumPeak> GetNormalizedPeak4SpectralEntropySimilarityCalc(
            List<SpectrumPeak> peaklist,
            double precursorMz,
            double ms2Tol = 0.05,
            double relativeAbundanceCutOff = 0.1,
            double absoluteAbundanceCutOff = 3,
            double minMz = 0,
            double maxMz = 100000) {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            double maxIntensity = peaklist.Max(n => n.Intensity);
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist) {
                if (peak.Mass < minMz) continue;
                if (peak.Mass > maxMz) continue;
                if (peak.Mass > precursorMz + ms2Tol) continue;
                if (peak.Intensity < absoluteAbundanceCutOff) continue;
                if (peak.Intensity >= maxIntensity * relativeAbundanceCutOff * 0.01) {
                    refinedPeaklist.Add(peak);
                }
            }

            var entropy = MsScanMatching.GetSpectralEntropy(refinedPeaklist);
            if (entropy < 3) {
                foreach (var peak in refinedPeaklist) {
                    peak.Intensity = Math.Pow(peak.Intensity, 0.25 + entropy * 0.25);
                }
            }
            var sumIntensity = refinedPeaklist.Sum(n => n.Intensity);
            return refinedPeaklist.Select(n => new SpectrumPeak() { Mass = n.Mass, Intensity = n.Intensity / sumIntensity }).ToList();
        }

        public static List<SpectrumPeak> GetRefinedPeaklist(
            List<SpectrumPeak> peaklist,
            double relativeAbundanceCutOff,
            double absoluteAbundanceCutOff,
            double minMz,
            double maxMz,
            double precursorMz,
            double ms2Tol,
            MassToleranceType massTolType,
            int precursorCharge,
            bool isBrClConsideredForIsotopes = false,
            bool isRemoveIsotopes = false,
            bool removeAfterPrecursor = true) {
            if (peaklist == null || peaklist.Count == 0) return new List<SpectrumPeak>();
            double maxIntensity = peaklist.Max(n => n.Intensity);
            var refinedPeaklist = new List<SpectrumPeak>();

            foreach (var peak in peaklist) {
                if (peak.Mass < minMz) continue;
                if (peak.Mass > maxMz) continue;
                if (removeAfterPrecursor && peak.Mass > precursorMz + ms2Tol) continue;
                if (peak.Intensity < absoluteAbundanceCutOff) continue;
                if (peak.Intensity >= maxIntensity * relativeAbundanceCutOff * 0.01) {
                    refinedPeaklist.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity, Comment = string.Empty });
                }
            }
            if (isRemoveIsotopes) {
                EstimateIsotopes(refinedPeaklist, ms2Tol, isBrClConsideredForIsotopes, precursorCharge);
                return refinedPeaklist.Where(n => n.IsotopeWeightNumber == 0).ToList();
            }
            else {
                return refinedPeaklist;
            }
        }

        /// <summary>
        /// peak list must be sorted by m/z (ordering)
        /// peak should be initialized by new Peak() { Mz = spec[0], Intensity = spec[1], Charge = 1, IsotopeFrag = false  }
        /// </summary>
        public static void EstimateIsotopes(List<SpectrumPeak> peaks, double mztolerance, bool isBrClConsideredForIsotopes = false, int maxChargeNumber = 0) {

            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var br81_br79 = MassDiffDictionary.Br81_Br79; //1.9979535; also to be used for S34_S32 (1.9957959), Cl37_Cl35 (1.99704991)
            var tolerance = mztolerance;
            for (int i = 0; i < peaks.Count; i++) {
                var peak = peaks[i];
                peak.PeakID = i;
                if (peak.IsotopeWeightNumber >= 0) continue;

                peak.IsotopeWeightNumber = 0;
                peak.IsotopeParentPeakID = i;

                // charge state checking at M + 1
                var predChargeNumber = 1;
                for (int j = i + 1; j < peaks.Count; j++) {
                    var isotopePeak = peaks[j];
                    if (isotopePeak.Mass > peak.Mass + c13_c12Diff + tolerance) break;
                    if (isotopePeak.IsotopeWeightNumber >= 0) continue;

                    for (int k = maxChargeNumber; k >= 1; k--) {
                        var predIsotopeMass = (double)peak.Mass + (double)c13_c12Diff / (double)k;
                        var diff = Math.Abs(predIsotopeMass - isotopePeak.Mass);
                        if (diff < tolerance) {
                            predChargeNumber = k;
                            if (k <= 3) {
                                break;
                            }
                            else if (k == 4 || k == 5) {
                                var predNextIsotopeMass = (double)peak.Mass + (double)c13_c12Diff / (double)(k - 1);
                                var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mass);
                                if (diff > nextDiff) predChargeNumber = k - 1;
                                break;
                            }
                            else if (k >= 6) {
                                var predNextIsotopeMass = (double)peak.Mass + (double)c13_c12Diff / (double)(k - 1);
                                var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mass);
                                if (diff > nextDiff) {
                                    predChargeNumber = k - 1;
                                    diff = nextDiff;

                                    predNextIsotopeMass = (double)peak.Mass + (double)c13_c12Diff / (double)(k - 2);
                                    nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mass);

                                    if (diff > nextDiff) {
                                        predChargeNumber = k - 2;
                                        diff = nextDiff;
                                    }
                                }
                                break;
                            }
                        }
                    }
                    if (predChargeNumber != 1) break;
                }
                peak.Charge = predChargeNumber;

                // isotope grouping till M + 8
                var maxTraceNumber = 15;
                var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
                isotopeTemps[0] = new IsotopeTemp() {
                    WeightNumber = 0,
                    Mz = peak.Mass, Intensity = peak.Intensity, PeakID = i, MzClBr = peak.Mass
                };

                for (int j = 1; j < isotopeTemps.Length; j++) {
                    isotopeTemps[j] = new IsotopeTemp() {
                        WeightNumber = j, Mz = peak.Mass + (double)j * c13_c12Diff / (double)predChargeNumber,
                        MzClBr = j % 2 == 0 ? peak.Mass + (double)j * c13_c12Diff / (double)predChargeNumber : peak.Mass + (double)j * br81_br79 * 0.5 / (double)predChargeNumber,
                        Intensity = 0, PeakID = -1
                    };
                }

                var reminderIndex = i + 1;
                var isFinished = false;
                var mzFocused = peak.Mass;
                for (int j = 1; j <= maxTraceNumber; j++) {
                    var predIsotopicMass = mzFocused + (double)c13_c12Diff / (double)predChargeNumber;
                    var predClBrIsotopicMass = mzFocused + (double)br81_br79 * 0.5 / (double)predChargeNumber;

                    for (int k = reminderIndex; k < peaks.Count; k++) {
                        var isotopePeak = peaks[k];
                        if (isotopePeak.IsotopeWeightNumber >= 0) continue;

                        var isotopeMz = isotopePeak.Mass;
                        var diffMz = Math.Abs(predIsotopicMass - isotopeMz);
                        var diffMzClBr = Math.Abs(predClBrIsotopicMass - isotopeMz);

                        if (diffMz < tolerance) {

                            if (isotopeTemps[j].PeakID == -1) {
                                isotopeTemps[j] = new IsotopeTemp() {
                                    WeightNumber = j, Mz = isotopeMz,
                                    Intensity = isotopePeak.Intensity, PeakID = k
                                };
                                mzFocused = isotopeMz;
                            }
                            else {
                                if (Math.Abs(isotopeTemps[j].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                    isotopeTemps[j].Mz = isotopeMz;
                                    isotopeTemps[j].Intensity = isotopePeak.Intensity;
                                    isotopeTemps[j].PeakID = k;

                                    mzFocused = isotopeMz;
                                }
                            }
                        }
                        else if (isBrClConsideredForIsotopes && j % 2 == 0 && diffMzClBr < tolerance) {
                            if (isotopeTemps[j].PeakID == -1) {
                                isotopeTemps[j] = new IsotopeTemp() {
                                    WeightNumber = j, Mz = isotopeMz, MzClBr = isotopeMz,
                                    Intensity = isotopePeak.Intensity, PeakID = k
                                };
                                mzFocused = isotopeMz;
                            }
                            else {
                                if (Math.Abs(isotopeTemps[j].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                    isotopeTemps[j].Mz = isotopeMz;
                                    isotopeTemps[j].MzClBr = isotopeMz;
                                    isotopeTemps[j].Intensity = isotopePeak.Intensity;
                                    isotopeTemps[j].PeakID = k;

                                    mzFocused = isotopeMz;
                                }
                            }
                        }
                        else if (isotopePeak.Mass >= predIsotopicMass + tolerance) {
                            if (k == peaks.Count - 1) break;
                            reminderIndex = k;
                            if (isotopeTemps[j - 1].PeakID == -1 && isotopeTemps[j].PeakID == -1) {
                                isFinished = true;
                            }
                            else if (isotopeTemps[j].PeakID == -1) {
                                mzFocused += (double)c13_c12Diff / (double)predChargeNumber;
                            }
                            break;
                        }

                    }
                    if (isFinished)
                        break;
                }

                // finalize and store
                var monoisotopicMass = (double)peak.Mass * (double)predChargeNumber;
                var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

                //from here, simple decreasing will be expected for <= 800 Da
                //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
                IsotopeProperty simulatedIsotopicPeaks = null;
                var isIsotopeDetected = false;
                var iupac = IupacResourceParser.GetIupacCHData();
                if (monoisotopicMass > 800)
                    simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);

                for (int j = 1; j <= maxTraceNumber; j++) {
                    if (isotopeTemps[j].PeakID == -1) continue;
                    if (isotopeTemps[j - 1].PeakID == -1 && isotopeTemps[j].PeakID == -1) break;

                    if (monoisotopicMass <= 800) {
                        if (isotopeTemps[j - 1].Intensity > isotopeTemps[j].Intensity && isBrClConsideredForIsotopes == false) {
                            peaks[isotopeTemps[j].PeakID].IsotopeParentPeakID = peak.PeakID;
                            peaks[isotopeTemps[j].PeakID].IsotopeWeightNumber = j;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            isIsotopeDetected = true;
                        }
                        else if (isBrClConsideredForIsotopes == true) {
                            peaks[isotopeTemps[j].PeakID].IsotopeParentPeakID = peak.PeakID;
                            peaks[isotopeTemps[j].PeakID].IsotopeWeightNumber = j;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            isIsotopeDetected = true;
                        }
                        else {
                            break;
                        }
                    }
                    else {
                        if (isotopeTemps[j - 1].Intensity <= 0) break;
                        var expRatio = isotopeTemps[j].Intensity / isotopeTemps[j - 1].Intensity;
                        var simRatio = simulatedIsotopicPeaks.IsotopeProfile[j].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[j - 1].RelativeAbundance;

                        if (Math.Abs(expRatio - simRatio) < 5.0) {
                            peaks[isotopeTemps[j].PeakID].IsotopeParentPeakID = peak.PeakID;
                            peaks[isotopeTemps[j].PeakID].IsotopeWeightNumber = j;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            isIsotopeDetected = true;
                        }
                        else {
                            break;
                        }
                    }
                }
                if (!isIsotopeDetected) {
                    peak.Charge = 1;
                }
            }
        }

        private static string getSimulatedFormulaByAlkane(double mass) {

            var ch2Mass = 14.0;
            var carbonCount = (int)(mass / ch2Mass);
            var hCount = (int)(carbonCount * 2);

            if (carbonCount == 0 || carbonCount == 1)
                return "CH2";
            else {
                return "C" + carbonCount.ToString() + "H" + hCount.ToString();
            }

        }
    }
}
