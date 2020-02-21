using Msdial.Lcms.Dataprocess.Utility;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm {

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
        public static void SetIsotopeInformation(int fileID,
            ObservableCollection<PeakAreaBean> peakAreaBeans,
            AnalysisParametersBean param, 
            IupacReferenceBean iupac)
        {
            var peakAreaBeanList = new List<PeakAreaBean>(peakAreaBeans);
            peakAreaBeanList = peakAreaBeanList.OrderBy(n => n.AccurateMass).ToList();

            //var spectrumMargin = 2;
            var rtMargin = 0.0275F;

            foreach (var peak in peakAreaBeanList) {

                if (peak.IsotopeWeightNumber >= 0) continue;

               // var focusedScan = peak.ScanNumberAtPeakTop;
                var focusedMass = peak.AccurateMass;
                var focusedRt = peak.RtAtPeakTop;

                var startScanIndex = DataAccessLcUtility.GetScanStartIndexByMz(focusedMass - 0.0001F, peakAreaBeanList);
                var isotopeCandidates = new List<PeakAreaBean>() { peak };

                //if (Math.Abs(peak.AccurateMass - 762.5087) < 0.001) {
                //    Console.WriteLine();
                //}

                for (int j = startScanIndex; j < peakAreaBeanList.Count; j++) {

                    if (peakAreaBeanList[j].PeakID == peak.PeakID) continue;
                    if (peakAreaBeanList[j].RtAtPeakTop < focusedRt - rtMargin) continue;
                    if (peakAreaBeanList[j].RtAtPeakTop > focusedRt + rtMargin) continue;
                    if (peakAreaBeanList[j].IsotopeWeightNumber >= 0) continue;
                    if (peakAreaBeanList[j].AccurateMass <= focusedMass) continue;
                    if (peakAreaBeanList[j].AccurateMass > focusedMass + 8.1) break;

                    isotopeCandidates.Add(peakAreaBeanList[j]);
                }
                //isotopeCalculation(isotopeCandidates, param);
                isotopeCalculationImproved(isotopeCandidates, param, iupac);
            }
            if (param.IsIonMobility) {
                var calinfo = param.FileidToCcsCalibrantData[fileID];
                foreach (var peak in peakAreaBeanList) {
                    if (peak.DriftSpots == null || peak.DriftSpots.Count == 0) continue;
                    var mass = peak.AccurateMass;
                    var charge = peak.ChargeNumber;
                    foreach (var dSpot in peak.DriftSpots) {
                        dSpot.Ccs = (float)IonMobilityUtility.MobilityToCrossSection(param.IonMobilityType, dSpot.DriftTimeAtPeakTop, Math.Abs(charge), mass, calinfo, param.IsAllCalibrantDataImported);
                    }
                }
            }
        }

        private static void isotopeCalculation(List<PeakAreaBean> peakAreaBeanList, AnalysisParametersBean param) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = peakAreaBeanList[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.AccurateMass, ppm);

            tolerance = (float)accuracy;
            var isFinished = false;

            monoIsoPeak.IsotopeWeightNumber = 0;
            monoIsoPeak.IsotopeParentPeakID = monoIsoPeak.PeakID;

            var reminderIntensity = monoIsoPeak.IntensityAtPeakTop;
            var reminderIndex = 1;

            //charge number check at M + 1
            var isDoubleCharged = false;
            var isotopicMassDoubleCharged = (double)monoIsoPeak.AccurateMass + (float)c13_c12Diff * 0.50;

            for (int j = 1; j < peakAreaBeanList.Count; j++) {
                var isotopePeak = peakAreaBeanList[j];
                if (isotopicMassDoubleCharged - tolerance <= isotopePeak.AccurateMass &&
                    isotopePeak.AccurateMass <= isotopicMassDoubleCharged + tolerance) {

                    if (monoIsoPeak.AccurateMass > 900 || monoIsoPeak.IntensityAtPeakTop > isotopePeak.IntensityAtPeakTop) {
                        isDoubleCharged = true;
                    }

                    if (isotopePeak.AccurateMass >= isotopicMassDoubleCharged + tolerance) {
                        break;
                    }
                }
            }

            var chargeCoff = isDoubleCharged == true ? 0.50 : 1.0;
            monoIsoPeak.ChargeNumber = isDoubleCharged == true ? 2 : 1;

            for (int i = 1; i <= 8; i++) {

                var isotopicMass = (double)monoIsoPeak.AccurateMass + (double)i * c13_c12Diff * chargeCoff;
                for (int j = reminderIndex; j < peakAreaBeanList.Count; j++) {

                    var isotopePeak = peakAreaBeanList[j];

                    if (isotopicMass - tolerance <= isotopePeak.AccurateMass &&
                        isotopePeak.AccurateMass <= isotopicMass + tolerance) {
                        #region 
                        if (monoIsoPeak.AccurateMass < 900) {

                            if (reminderIntensity > isotopePeak.IntensityAtPeakTop) {

                                isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
                                isotopePeak.IsotopeWeightNumber = i;
                                isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                reminderIntensity = isotopePeak.IntensityAtPeakTop;
                                reminderIndex = j + 1;
                                break;
                            }
                            else {
                                isFinished = true;
                                break;
                            }
                        }
                        else {
                            if (i <= 3) {

                                isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
                                isotopePeak.IsotopeWeightNumber = i;
                                isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                reminderIntensity = isotopePeak.IntensityAtPeakTop;
                                reminderIndex = j + 1;

                                break;
                            }
                            else {
                                if (reminderIntensity > isotopePeak.IntensityAtPeakTop) {

                                    isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
                                    isotopePeak.IsotopeWeightNumber = i;
                                    isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

                                    reminderIntensity = isotopePeak.IntensityAtPeakTop;
                                    reminderIndex = j + 1;

                                    break;
                                }
                                else {
                                    isFinished = true;
                                    break;
                                }
                            }
                        }
                        #endregion
                    }
                }
                if (isFinished)
                    break;
            }
        }

        public static void isotopeCalculationImproved(
            List<PeakAreaBean> peakAreaBeanList, 
            AnalysisParametersBean param, 
            IupacReferenceBean iupac) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = peakAreaBeanList[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.AccurateMass, ppm);

            tolerance = (float)accuracy;
            var isFinished = false;

            monoIsoPeak.IsotopeWeightNumber = 0;
            monoIsoPeak.IsotopeParentPeakID = monoIsoPeak.PeakID;

            //if (Math.Abs(monoIsoPeak.AccurateMass - 762.5087) < 0.001) {
            //    Console.WriteLine();
            //}

            //charge number check at M + 1
            var predChargeNumber = 1;
            for (int j = 1; j < peakAreaBeanList.Count; j++) {
                var isotopePeak = peakAreaBeanList[j];
                if (isotopePeak.AccurateMass > monoIsoPeak.AccurateMass + c13_c12Diff + tolerance) break;

                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)monoIsoPeak.AccurateMass + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.AccurateMass);
                    if (diff < tolerance) {
                        predChargeNumber = k;
                        if (k <= 3) {
                            break;
                        } else if (k == 4 || k == 5) {
                            var predNextIsotopeMass = (double)monoIsoPeak.AccurateMass + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.AccurateMass);
                            if (diff > nextDiff) predChargeNumber = k - 1;
                            break;
                        } else if (k >= 6) {
                            var predNextIsotopeMass = (double)monoIsoPeak.AccurateMass + (double)c13_c12Diff / (double)(k - 1);
                            var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.AccurateMass);
                            if (diff > nextDiff) {
                                predChargeNumber = k - 1;
                                diff = nextDiff;

                                predNextIsotopeMass = (double)monoIsoPeak.AccurateMass + (double)c13_c12Diff / (double)(k - 2);
                                nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.AccurateMass);

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

            monoIsoPeak.ChargeNumber = predChargeNumber;

            var maxTraceNumber = 8;
            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
            isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, Mz = monoIsoPeak.AccurateMass,
                Intensity = monoIsoPeak.IntensityAtPeakTop, PeakID = monoIsoPeak.PeakID };

            var reminderIndex = 1;
            for (int i = 1; i <= maxTraceNumber; i++) {

                var predIsotopicMass = (double)monoIsoPeak.AccurateMass + (double)i * c13_c12Diff / (double)predChargeNumber;
                for (int j = reminderIndex; j < peakAreaBeanList.Count; j++) {

                    var isotopePeak = peakAreaBeanList[j];

                    if (predIsotopicMass - tolerance < isotopePeak.AccurateMass &&
                        isotopePeak.AccurateMass < predIsotopicMass + tolerance) {

                        if (isotopeTemps[i] == null) {
                            isotopeTemps[i] = new IsotopeTemp() { WeightNumber = i, Mz = isotopePeak.AccurateMass,
                                Intensity = isotopePeak.IntensityAtPeakTop, PeakID = j };
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopePeak.AccurateMass - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopePeak.AccurateMass;
                                isotopeTemps[i].Intensity = isotopePeak.IntensityAtPeakTop;
                                isotopeTemps[i].PeakID = j;
                            }
                        }
                    }
                    else if (isotopePeak.AccurateMass >= predIsotopicMass + tolerance) {
                        reminderIndex = j;
                        if (isotopeTemps[i] == null) isFinished = true;
                        break;
                    }
                }
                if (isFinished)
                    break;
            }

            var reminderIntensity = monoIsoPeak.IntensityAtPeakTop;
            var monoisotopicMass = (double)monoIsoPeak.AccurateMass * (double)predChargeNumber;
            var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            CompoundPropertyBean simulatedIsotopicPeaks = null;
            if (monoisotopicMass > 800)
                simulatedIsotopicPeaks = IsotopeRatioCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, 9, iupac);
            for (int i = 1; i <= maxTraceNumber; i++) {
                if (isotopeTemps[i] == null) break;
                if (isotopeTemps[i].Intensity <= 0) break;

                if (monoisotopicMass <= 800) {
                    if (isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity && param.IsBrClConsideredForIsotopes == false) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                    }
                    else if (param.IsBrClConsideredForIsotopes == true) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                    }
                    else {
                        break;
                    }
                }
                else {
                    var expRatio = isotopeTemps[i].Intensity / isotopeTemps[i - 1].Intensity;
                    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[i].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[i - 1].RelativeAbundance;

                    if (Math.Abs(expRatio - simRatio) < 5.0) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
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
        public static void MsmsIsotopeRecognition(List<Peak> peaks, 
            int maxTraceNumber, int maxChargeNumber, double tolerance,
            IupacReferenceBean iupac) {
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
                    if (isotopePeak.Mz > peak.Mz + c13_c12Diff + tolerance) break;
                    if (isotopePeak.Comment != "NA") continue;

                    for (int k = maxChargeNumber; k >= 1; k--) {
                        var predIsotopeMass = (double)peak.Mz + (double)c13_c12Diff / (double)k;
                        var diff = Math.Abs(predIsotopeMass - isotopePeak.Mz);
                        if (diff < tolerance) {
                            predChargeNumber = k;
                            if (k <= 3) {
                                break;
                            }
                            else if (k == 4 || k == 5) {
                                var predNextIsotopeMass = (double)peak.Mz + (double)c13_c12Diff / (double)(k - 1);
                                var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mz);
                                if (diff > nextDiff) predChargeNumber = k - 1;
                                break;
                            }
                            else if (k >= 6) {
                                var predNextIsotopeMass = (double)peak.Mz + (double)c13_c12Diff / (double)(k - 1);
                                var nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mz);
                                if (diff > nextDiff) {
                                    predChargeNumber = k - 1;
                                    diff = nextDiff;

                                    predNextIsotopeMass = (double)peak.Mz + (double)c13_c12Diff / (double)(k - 2);
                                    nextDiff = Math.Abs(predNextIsotopeMass - isotopePeak.Mz);

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
                isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, Mz = peak.Mz, Intensity = peak.Intensity, PeakID = i };

                var reminderIndex = i + 1;
                var isFinished = false;
                for (int j = 1; j <= maxTraceNumber; j++) {
                    var predIsotopicMass = (double)peak.Mz + (double)j * c13_c12Diff / (double)predChargeNumber;

                    for (int k = reminderIndex; k < peaks.Count; k++) {
                        var isotopePeak = peaks[k];
                        if (isotopePeak.Comment != "NA") continue;

                        if (predIsotopicMass - tolerance < isotopePeak.Mz && isotopePeak.Mz < predIsotopicMass + tolerance) {
                            if (isotopeTemps[j] == null) {
                                isotopeTemps[j] = new IsotopeTemp() {
                                    WeightNumber = j,
                                    Mz = isotopePeak.Mz,
                                    Intensity = isotopePeak.Intensity,
                                    PeakID = k
                                };
                            } else {
                                if (Math.Abs(isotopeTemps[j].Mz - predIsotopicMass) > Math.Abs(isotopePeak.Mz - predIsotopicMass)) {
                                    isotopeTemps[j].Mz = isotopePeak.Mz;
                                    isotopeTemps[j].Intensity = isotopePeak.Intensity;
                                    isotopeTemps[j].PeakID = k;
                                }
                            }
                        }
                        else if (isotopePeak.Mz >= predIsotopicMass + tolerance) {
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
                var monoisotopicMass = (double)peak.Mz * (double)predChargeNumber;
                var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

                //from here, simple decreasing will be expected for <= 800 Da
                //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
                CompoundPropertyBean simulatedIsotopicPeaks = null;
                if (monoisotopicMass > 800)
                    simulatedIsotopicPeaks = IsotopeRatioCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, 9, iupac);

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
