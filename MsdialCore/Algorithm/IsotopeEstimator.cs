using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Utility;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm {

    public class IsotopeTemp {
        public int WeightNumber { get; set; }
        public double Mz { get; set; }
        public double Intensity { get; set; }
        public int PeakID { get; set; }
    }

    public sealed class IsotopeEstimator
    {
        private IsotopeEstimator() { }

        /// <summary>
        /// This method tries to decide if the detected peak is the isotopic ion or not.
        /// The peaks less than the abundance of the mono isotopic ion will be assigned to the isotopic ions within the same data point.
        /// </summary>
        /// <param name="peakAreaBeanCollection"></param>
        /// <param name="analysisParametersBean"></param>
        public static void Process(
            List<ChromatogramPeakFeature> peakFeatures,
            ParameterBase param, 
            IupacDatabase iupac)
        {
            peakFeatures = peakFeatures.OrderBy(n => n.PrecursorMz).ToList();

            //var spectrumMargin = 2;
            var rtMargin = 0.0275F;
            var isotopeMax = 8.1;

            foreach (var peak in peakFeatures) {
                var peakCharacter = peak.PeakCharacter;
                if (peakCharacter.IsotopeWeightNumber >= 0) continue;

                // var focusedScan = peak.ScanNumberAtPeakTop;
                var focusedMass = peak.PrecursorMz;
                var focusedRt = peak.ChromXsTop.RT.Value;

                var startScanIndex = SearchCollection.LowerBound(peakFeatures, new ChromatogramPeakFeature() { Mass = focusedMass - param.CentroidMs1Tolerance }, (a, b) => a.Mass.CompareTo(b.Mass));
                //DataAccess.GetScanStartIndexByMz((float)focusedMass - param.CentroidMs1Tolerance, peakFeatures);
                var isotopeCandidates = new List<ChromatogramPeakFeature>() { peak };

                for (int j = startScanIndex; j < peakFeatures.Count; j++) {

                    if (peakFeatures[j].PeakID == peak.PeakID) continue;
                    if (peakFeatures[j].ChromXsTop.RT.Value < focusedRt - rtMargin) continue;
                    if (peakFeatures[j].ChromXsTop.RT.Value > focusedRt + rtMargin) continue;
                    if (peakFeatures[j].PeakCharacter.IsotopeWeightNumber >= 0) continue;
                    if (peakFeatures[j].PrecursorMz <= focusedMass) continue;
                    if (peakFeatures[j].PrecursorMz > focusedMass + isotopeMax) break;

                    isotopeCandidates.Add(peakFeatures[j]);
                }
                EstimateIsotopes(isotopeCandidates, param, iupac);
            }
        }
     
        public static void EstimateIsotopes(
            List<ChromatogramPeakFeature> peakFeatures,
            ParameterBase param,
            IupacDatabase iupac) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = peakFeatures[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.PrecursorMz, ppm);

            tolerance = (float)accuracy;
            var isFinished = false;

            monoIsoPeak.PeakCharacter.IsotopeWeightNumber = 0;
            monoIsoPeak.PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;

            //if (Math.Abs(monoIsoPeak.AccurateMass - 762.5087) < 0.001) {
            //    Console.WriteLine();
            //}

            //charge number check at M + 1
            var predChargeNumber = 1;
            for (int j = 1; j < peakFeatures.Count; j++) {
                var isotopePeak = peakFeatures[j];
                if (isotopePeak.PrecursorMz > monoIsoPeak.PrecursorMz + c13_c12Diff + tolerance) break;

                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)monoIsoPeak.PrecursorMz + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.PrecursorMz);
                    if (diff < tolerance) {
                        predChargeNumber = k;
                        if (k <= 3) {
                            break;
                        } else if (k == 4 || k == 5) {
                            var predNextIsotopeMass = (double)monoIsoPeak.PrecursorMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.PrecursorMz);
                            if (diff > nextDiff) predChargeNumber = k - 1;
                            break;
                        } else if (k >= 6) {
                            var predNextIsotopeMass = (double)monoIsoPeak.PrecursorMz + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.PrecursorMz);
                            if (diff > nextDiff) {
                                predChargeNumber = k - 1;
                                diff = nextDiff;

                                predNextIsotopeMass = (double)monoIsoPeak.PrecursorMz + (double)c13_c12Diff / (double)(k - 2);
                                nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.PrecursorMz);

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

            monoIsoPeak.PeakCharacter.Charge = predChargeNumber;

            var maxTraceNumber = 8;
            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
            isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, Mz = monoIsoPeak.PrecursorMz,
                Intensity = monoIsoPeak.PeakHeightTop, PeakID = monoIsoPeak.PeakID };

            var reminderIndex = 1;
            for (int i = 1; i <= maxTraceNumber; i++) {

                var predIsotopicMass = (double)monoIsoPeak.PrecursorMz + (double)i * c13_c12Diff / (double)predChargeNumber;
                for (int j = reminderIndex; j < peakFeatures.Count; j++) {

                    var isotopePeak = peakFeatures[j];

                    if (predIsotopicMass - tolerance < isotopePeak.PrecursorMz &&
                        isotopePeak.PrecursorMz < predIsotopicMass + tolerance) {

                        if (isotopeTemps[i] == null) {
                            isotopeTemps[i] = new IsotopeTemp() { WeightNumber = i, Mz = isotopePeak.PrecursorMz,
                                Intensity = isotopePeak.PeakHeightTop, PeakID = j };
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopePeak.PrecursorMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopePeak.PrecursorMz;
                                isotopeTemps[i].Intensity = isotopePeak.PeakHeightTop;
                                isotopeTemps[i].PeakID = j;
                            }
                        }
                    }
                    else if (isotopePeak.PrecursorMz >= predIsotopicMass + tolerance) {
                        reminderIndex = j;
                        if (isotopeTemps[i] == null) isFinished = true;
                        break;
                    }
                }
                if (isFinished)
                    break;
            }

            var reminderIntensity = monoIsoPeak.PeakHeightTop;
            var monoisotopicMass = (double)monoIsoPeak.PrecursorMz * (double)predChargeNumber;
            var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            IsotopeProperty simulatedIsotopicPeaks = null;
            if (monoisotopicMass > 800)
                simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, 9, iupac);
            for (int i = 1; i <= maxTraceNumber; i++) {
                if (isotopeTemps[i] == null) break;
                if (isotopeTemps[i].Intensity <= 0) break;

                if (monoisotopicMass <= 800) {
                    if (isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity && param.IsBrClConsideredForIsotopes == false) {
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
                    }
                    else if (param.IsBrClConsideredForIsotopes == true) {
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
                    }
                    else {
                        break;
                    }
                }
                else {
                    var expRatio = isotopeTemps[i].Intensity / isotopeTemps[i - 1].Intensity;
                    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[i].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[i - 1].RelativeAbundance;

                    if (Math.Abs(expRatio - simRatio) < 5.0) {
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
                    }
                    else {
                        break;
                    }
                }
            }
        }

        /// <summary>
        /// peak list must be sorted by m/z (ordering)
        /// peak should be initialized by new Peak() { Mz = spec[0], Intensity = spec[1], Charge = 1, IsotopeFrag = false, Comment = "NA" }
        /// </summary>
        public static void MsmsIsotopeRecognition(List<SpectrumPeak> peaks, 
            int maxTraceNumber, int maxChargeNumber, double tolerance,
            IupacDatabase iupac) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            for (int i = 0; i < peaks.Count; i++) {
                var peak = peaks[i];
                if (peak.Comment != "NA") continue;
                peak.IsotopeFrag = false;
                peak.Comment = i.ToString();

                // charge state checking at M + 1
                var predChargeNumber = 1;
                for (int j = i + 1; j < peaks.Count; j++) {
                    var isotopePeak = peaks[j];
                    if (isotopePeak.Mass > peak.Mass + c13_c12Diff + tolerance) break;
                    if (isotopePeak.Comment != "NA") continue;

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
                var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
                isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, Mz = peak.Mass, Intensity = peak.Intensity, PeakID = i };

                var reminderIndex = i + 1;
                var isFinished = false;
                for (int j = 1; j <= maxTraceNumber; j++) {
                    var predIsotopicMass = (double)peak.Mass + (double)j * c13_c12Diff / (double)predChargeNumber;

                    for (int k = reminderIndex; k < peaks.Count; k++) {
                        var isotopePeak = peaks[k];
                        if (isotopePeak.Comment != "NA") continue;

                        if (predIsotopicMass - tolerance < isotopePeak.Mass && isotopePeak.Mass < predIsotopicMass + tolerance) {
                            if (isotopeTemps[j] == null) {
                                isotopeTemps[j] = new IsotopeTemp() {
                                    WeightNumber = j,
                                    Mz = isotopePeak.Mass,
                                    Intensity = isotopePeak.Intensity,
                                    PeakID = k
                                };
                            } else {
                                if (Math.Abs(isotopeTemps[j].Mz - predIsotopicMass) > Math.Abs(isotopePeak.Mass - predIsotopicMass)) {
                                    isotopeTemps[j].Mz = isotopePeak.Mass;
                                    isotopeTemps[j].Intensity = isotopePeak.Intensity;
                                    isotopeTemps[j].PeakID = k;
                                }
                            }
                        }
                        else if (isotopePeak.Mass >= predIsotopicMass + tolerance) {
                            reminderIndex = k;
                            if (isotopeTemps[j] == null) isFinished = true;
                            break;
                        }
                    }
                    if (isFinished)
                        break;
                }

                // finalize and store
                var reminderIntensity = peak.Intensity;
                var monoisotopicMass = (double)peak.Mass * (double)predChargeNumber;
                var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

                //from here, simple decreasing will be expected for <= 800 Da
                //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
                IsotopeProperty simulatedIsotopicPeaks = null;
                if (monoisotopicMass > 800)
                    simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, 9, iupac);

                for (int j = 1; j <= maxTraceNumber; j++) {
                    if (isotopeTemps[j] == null) break;
                    if (isotopeTemps[j].Intensity <= 0) break;

                    if (monoisotopicMass <= 800) {
                        if (isotopeTemps[j - 1].Intensity > isotopeTemps[j].Intensity) {
                            peaks[isotopeTemps[j].PeakID].IsotopeFrag = true;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            peaks[isotopeTemps[j].PeakID].Comment = i.ToString();
                        }
                        else {
                            break;
                        }
                    }
                    else {
                        var expRatio = isotopeTemps[j].Intensity / isotopeTemps[j - 1].Intensity;
                        var simRatio = simulatedIsotopicPeaks.IsotopeProfile[j].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[j - 1].RelativeAbundance;

                        if (Math.Abs(expRatio - simRatio) < 5.0) {
                            peaks[isotopeTemps[j].PeakID].IsotopeFrag = true;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            peaks[isotopeTemps[j].PeakID].Comment = i.ToString();
                        }
                        else {
                            break;
                        }
                    }
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
