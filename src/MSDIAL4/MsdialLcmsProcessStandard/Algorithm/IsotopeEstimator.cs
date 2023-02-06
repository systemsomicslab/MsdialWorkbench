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
        public double MzClBr { get; set; }
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
            var rtMargin = 0.25F;

            foreach (var peak in peakAreaBeanList) {

                if (peak.IsotopeWeightNumber >= 0) continue;

               // var focusedScan = peak.ScanNumberAtPeakTop;
                var focusedMass = peak.AccurateMass;
                var focusedRt = peak.RtAtPeakTop;
                var focusedScan = peak.ScanNumberAtPeakTop;

                var startScanIndex = DataAccessLcUtility.GetScanStartIndexByMz(focusedMass - 0.0001F, peakAreaBeanList);
                var isotopeCandidates = new List<PeakAreaBean>() { peak };

                //if (Math.Abs(peak.AccurateMass - 614.61926) < 0.001 && Math.Abs(peak.RtAtPeakTop - 68.579) < 0.1) {
                //   Console.WriteLine();
                //}

                for (int j = startScanIndex; j < peakAreaBeanList.Count; j++) {

                    var peakRt = peakAreaBeanList[j].RtAtPeakTop;
                    var peakMz = peakAreaBeanList[j].AccurateMass;
                    var peakScan = peakAreaBeanList[j].ScanNumberAtPeakTop;

                    if (peakAreaBeanList[j].PeakID == peak.PeakID) continue;
                    if (peakAreaBeanList[j].IsotopeWeightNumber >= 0) continue;
                    //if (Math.Abs(peakRt - focusedRt) > rtMargin && Math.Abs(peakScan - focusedScan) > spectrumMargin) continue;
                    if (Math.Abs(peakRt - focusedRt) > rtMargin) continue;
                    if (peakMz <= focusedMass) continue;
                    if (peakMz > focusedMass + 8.1) break;

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

        //private static void isotopeCalculation(List<PeakAreaBean> peakAreaBeanList, AnalysisParametersBean param) {
        //    var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
        //    var tolerance = param.CentroidMs1Tolerance;
        //    var monoIsoPeak = peakAreaBeanList[0];
        //    var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
        //    var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.AccurateMass, ppm);

        //    tolerance = (float)accuracy;
        //    var isFinished = false;

        //    monoIsoPeak.IsotopeWeightNumber = 0;
        //    monoIsoPeak.IsotopeParentPeakID = monoIsoPeak.PeakID;

        //    var reminderIntensity = monoIsoPeak.IntensityAtPeakTop;
        //    var reminderIndex = 1;

        //    //charge number check at M + 1
        //    var isDoubleCharged = false;
        //    var isotopicMassDoubleCharged = (double)monoIsoPeak.AccurateMass + (float)c13_c12Diff * 0.50;

        //    for (int j = 1; j < peakAreaBeanList.Count; j++) {
        //        var isotopePeak = peakAreaBeanList[j];
        //        if (isotopicMassDoubleCharged - tolerance <= isotopePeak.AccurateMass &&
        //            isotopePeak.AccurateMass <= isotopicMassDoubleCharged + tolerance) {

        //            if (monoIsoPeak.AccurateMass > 900 || monoIsoPeak.IntensityAtPeakTop > isotopePeak.IntensityAtPeakTop) {
        //                isDoubleCharged = true;
        //            }

        //            if (isotopePeak.AccurateMass >= isotopicMassDoubleCharged + tolerance) {
        //                break;
        //            }
        //        }
        //    }

        //    var chargeCoff = isDoubleCharged == true ? 0.50 : 1.0;
        //    monoIsoPeak.ChargeNumber = isDoubleCharged == true ? 2 : 1;

        //    for (int i = 1; i <= 8; i++) {

        //        var isotopicMass = (double)monoIsoPeak.AccurateMass + (double)i * c13_c12Diff * chargeCoff;
        //        for (int j = reminderIndex; j < peakAreaBeanList.Count; j++) {

        //            var isotopePeak = peakAreaBeanList[j];

        //            if (isotopicMass - tolerance <= isotopePeak.AccurateMass &&
        //                isotopePeak.AccurateMass <= isotopicMass + tolerance) {
        //                #region 
        //                if (monoIsoPeak.AccurateMass < 900) {

        //                    if (reminderIntensity > isotopePeak.IntensityAtPeakTop) {

        //                        isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
        //                        isotopePeak.IsotopeWeightNumber = i;
        //                        isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

        //                        reminderIntensity = isotopePeak.IntensityAtPeakTop;
        //                        reminderIndex = j + 1;
        //                        break;
        //                    }
        //                    else {
        //                        isFinished = true;
        //                        break;
        //                    }
        //                }
        //                else {
        //                    if (i <= 3) {

        //                        isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
        //                        isotopePeak.IsotopeWeightNumber = i;
        //                        isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

        //                        reminderIntensity = isotopePeak.IntensityAtPeakTop;
        //                        reminderIndex = j + 1;

        //                        break;
        //                    }
        //                    else {
        //                        if (reminderIntensity > isotopePeak.IntensityAtPeakTop) {

        //                            isotopePeak.IsotopeParentPeakID = monoIsoPeak.PeakID;
        //                            isotopePeak.IsotopeWeightNumber = i;
        //                            isotopePeak.ChargeNumber = monoIsoPeak.ChargeNumber;

        //                            reminderIntensity = isotopePeak.IntensityAtPeakTop;
        //                            reminderIndex = j + 1;

        //                            break;
        //                        }
        //                        else {
        //                            isFinished = true;
        //                            break;
        //                        }
        //                    }
        //                }
        //                #endregion
        //            }
        //        }
        //        if (isFinished)
        //            break;
        //    }
        //}

        public static void isotopeCalculationImproved(
            List<PeakAreaBean> peakAreaBeanList, 
            AnalysisParametersBean param, 
            IupacReferenceBean iupac) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var br81_br79 = MassDiffDictionary.Br81_Br79; //1.9979535; also to be used for S34_S32 (1.9957959), Cl37_Cl35 (1.99704991)
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = peakAreaBeanList[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.AccurateMass, ppm);
            var rtMargin = 0.06F;

            tolerance = (float)accuracy;
            if (tolerance < param.CentroidMs1Tolerance) tolerance = param.CentroidMs1Tolerance;
            var isFinished = false;

            monoIsoPeak.IsotopeWeightNumber = 0;
            monoIsoPeak.IsotopeParentPeakID = monoIsoPeak.PeakID;

            var rtMonoisotope = monoIsoPeak.RtAtPeakTop;
            var rtFocused = monoIsoPeak.RtAtPeakTop;
            //if (Math.Abs(monoIsoPeak.AccurateMass - 308.84033) < 0.001 && Math.Abs(monoIsoPeak.RtAtPeakTop - 10.663) < 0.1) {
            //    Console.WriteLine();
            //}

            //charge number check at M + 1
            var predChargeNumber = 1;
            for (int j = 1; j < peakAreaBeanList.Count; j++) {
                var isotopePeak = peakAreaBeanList[j];
                if (isotopePeak.AccurateMass > monoIsoPeak.AccurateMass + c13_c12Diff + tolerance) break;
                var isotopeRt = isotopePeak.RtAtPeakTop;
                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)monoIsoPeak.AccurateMass + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.AccurateMass);
                    var diffRt = Math.Abs(rtMonoisotope - isotopeRt);
                    if (diff < tolerance && diffRt < rtMargin) {
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

            var maxTraceNumber = 15;
            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
            isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, Mz = monoIsoPeak.AccurateMass, MzClBr = monoIsoPeak.AccurateMass,
                Intensity = monoIsoPeak.IntensityAtPeakTop, PeakID = monoIsoPeak.PeakID };
            for (int i = 1; i < isotopeTemps.Length; i++) {
                isotopeTemps[i] = new IsotopeTemp() {
                    WeightNumber = i, Mz = monoIsoPeak.AccurateMass + (double)i * c13_c12Diff / (double)predChargeNumber,
                    MzClBr = i % 2 == 0 ? monoIsoPeak.AccurateMass + (double)i * c13_c12Diff / (double)predChargeNumber : monoIsoPeak.AccurateMass + (double)i * br81_br79 * 0.5 / (double)predChargeNumber,
                    Intensity = 0, PeakID = -1
                };
            }

            var mzFocused = (double)monoIsoPeak.AccurateMass;
            var reminderIndex = 1;
            for (int i = 1; i <= maxTraceNumber; i++) {

                //var predIsotopicMass = mzFocused + (double)i * c13_c12Diff / (double)predChargeNumber;
                var predIsotopicMass = mzFocused + (double)c13_c12Diff / (double)predChargeNumber;
                var predClBrIsotopicMass = mzFocused + (double)br81_br79 * 0.5 / (double)predChargeNumber;

                for (int j = reminderIndex; j < peakAreaBeanList.Count; j++) {

                    var isotopePeak = peakAreaBeanList[j];
                    var isotopeRt = isotopePeak.RtAtPeakTop;
                    var isotopeMz = isotopePeak.AccurateMass;
                    var diffMz = Math.Abs(predIsotopicMass - isotopeMz);
                    var diffMzClBr = Math.Abs(predClBrIsotopicMass - isotopeMz);
                    var diffRt = Math.Abs(rtFocused - isotopeRt);

                    if (diffMz < tolerance && diffRt < rtMargin) {

                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() { WeightNumber = i, Mz = isotopeMz,
                                Intensity = isotopePeak.IntensityAtPeakTop, PeakID = j };
                            rtFocused = isotopeRt;
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.IntensityAtPeakTop;
                                isotopeTemps[i].PeakID = j;

                                rtFocused = isotopeRt;
                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (param.IsBrClConsideredForIsotopes && i % 2 == 0 && diffMzClBr < tolerance && diffRt < rtMargin) {
                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz, MzClBr = isotopeMz,
                                Intensity = isotopePeak.IntensityAtPeakTop, PeakID = j
                            };
                            rtFocused = isotopeRt;
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].MzClBr = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.IntensityAtPeakTop;
                                isotopeTemps[i].PeakID = j;

                                rtFocused = isotopeRt;
                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (isotopeMz >= predIsotopicMass + tolerance) {
                        if (j == peakAreaBeanList.Count - 1) break;
                        reminderIndex = j;
                        if (isotopeTemps[i - 1].PeakID == -1 && isotopeTemps[i].PeakID == -1) {
                            isFinished = true;
                        } 
                        else if (isotopeTemps[i].PeakID == -1) {
                            mzFocused += (double)c13_c12Diff / (double)predChargeNumber;
                        }
                        break;
                    }

                }
                if (isFinished)
                    break;
            }

            var monoisotopicMass = (double)monoIsoPeak.AccurateMass * (double)predChargeNumber;
            var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            CompoundPropertyBean simulatedIsotopicPeaks = null;
            var isIsotopeDetected = false;
            if (monoisotopicMass > 800)
                simulatedIsotopicPeaks = IsotopeRatioCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);
            for (int i = 1; i <= maxTraceNumber; i++) {
                //if (isotopeTemps[i] == null) continue;
                if (isotopeTemps[i].PeakID == -1) continue;
                //if (isotopeTemps[i - 1] == null && isotopeTemps[i] == null) break;
                if (isotopeTemps[i - 1].PeakID == -1 && isotopeTemps[i].PeakID == -1) break;

                if (monoisotopicMass <= 800) {
                    if (isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity && param.IsBrClConsideredForIsotopes == false) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                        isIsotopeDetected = true;
                    }
                    else if (param.IsBrClConsideredForIsotopes == true) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                        isIsotopeDetected = true;
                    }
                    else {
                        break;
                    }
                }
                else {
                    if (isotopeTemps[i - 1].Intensity <= 0) break;
                    var expRatio = isotopeTemps[i].Intensity / isotopeTemps[i - 1].Intensity;
                    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[i].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[i - 1].RelativeAbundance;

                    if (Math.Abs(expRatio - simRatio) < 5.0) {
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakAreaBeanList[isotopeTemps[i].PeakID].IsotopeWeightNumber = i;
                        peakAreaBeanList[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                        isIsotopeDetected = true;
                    }
                    else {
                        break;
                    }
                }
            }
            if (!isIsotopeDetected) {
                monoIsoPeak.ChargeNumber = 1;
            }
        }

        public static void PostIsotopeCurator(List<AlignmentPropertyBean> alignSpots,
            AnalysisParametersBean param, ProjectPropertyBean projectProperty, IupacReferenceBean iupacRef) {
            var rtMargin = 0.06F;
            alignSpots = alignSpots.OrderBy(n => n.CentralAccurateMass).ToList();
            foreach (var spot in alignSpots) {
                #region
                if (spot.PostDefinedIsotopeWeightNumber > 0) continue;

                var spotRt = spot.CentralRetentionTime;
                var spotMz = spot.CentralAccurateMass;

                var startScanIndex = DataAccessLcUtility.GetScanStartIndexByMz(spotMz - 0.0001F, alignSpots);
                var isotopeCandidates = new List<AlignmentPropertyBean>() { spot };

                for (int j = startScanIndex; j < alignSpots.Count; j++) {

                    if (alignSpots[j].AlignmentID == spot.AlignmentID) continue;
                    if (alignSpots[j].LibraryID >= 0 || alignSpots[j].PostIdentificationLibraryID >= 0) continue;
                    if (alignSpots[j].CentralRetentionTime < spotRt - rtMargin) continue;
                    if (alignSpots[j].CentralRetentionTime > spotRt + rtMargin) continue;
                    if (alignSpots[j].PostDefinedIsotopeWeightNumber >= 0) continue;
                    if (alignSpots[j].CentralAccurateMass <= spotMz) continue;
                    if (alignSpots[j].CentralAccurateMass > spotMz + 8.1) break;

                    isotopeCandidates.Add(alignSpots[j]);
                }
                isotopeCalculationImproved(isotopeCandidates, param, iupacRef);
                #endregion
            }
        }

        public static void isotopeCalculationImproved(List<AlignmentPropertyBean> alignSpots, AnalysisParametersBean param, IupacReferenceBean iupac) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var br81_br79 = MassDiffDictionary.Br81_Br79;  //1.9979535; also to be used for S34_S32 (1.9957959), Cl37_Cl35 (1.99704991)
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = alignSpots[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 400
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.CentralAccurateMass, ppm);
            tolerance = (float)accuracy;
            if (tolerance < param.CentroidMs1Tolerance) tolerance = param.CentroidMs1Tolerance;

            var isFinished = false;

            monoIsoPeak.PostDefinedIsotopeWeightNumber = 0;
            monoIsoPeak.PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;

            //if (Math.Abs(monoIsoPeak.CentralAccurateMass - 266.9979) < 0.005 && Math.Abs(monoIsoPeak.CentralRetentionTime - 12.985) < 0.05) {
            //    Console.WriteLine();
            //}

            var reminderIndex = 1;
            var maxTraceNumber = 15;
            var mzFocused = (double)monoIsoPeak.CentralAccurateMass;
            var predChargeNumber = monoIsoPeak.ChargeNumber;
            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
            isotopeTemps[0] = new IsotopeTemp() {
                WeightNumber = 0, Mz = mzFocused, MzClBr = mzFocused,
                Intensity = monoIsoPeak.AverageValiable, PeakID = monoIsoPeak.AlignmentID
            };
            for (int i = 1; i < isotopeTemps.Length; i++) {
                isotopeTemps[i] = new IsotopeTemp() {
                    WeightNumber = i, Mz = mzFocused + (double)i * c13_c12Diff / (double)predChargeNumber,
                    MzClBr = i % 2 == 0 ? mzFocused + (double)i * c13_c12Diff / (double)predChargeNumber : mzFocused + (double)i * br81_br79 * 0.5 / (double)predChargeNumber,
                    Intensity = 0, PeakID = -1
                };
            }
            
            for (int i = 1; i <= maxTraceNumber; i++) {
                var predIsotopicMass = mzFocused + (double)c13_c12Diff / (double)predChargeNumber;
                var predClBrIsotopicMass = mzFocused + (double)br81_br79 * 0.5 / (double)predChargeNumber;
                for (int j = reminderIndex; j < alignSpots.Count; j++) {

                    var isotopePeak = alignSpots[j];
                    var isotopeMz = isotopePeak.CentralAccurateMass;
                    var diffMz = Math.Abs(predIsotopicMass - isotopeMz);
                    var diffMzClBr = Math.Abs(predClBrIsotopicMass - isotopeMz);

                    if (diffMz < tolerance) {

                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz,
                                Intensity = isotopePeak.AverageValiable, PeakID = j
                            };
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.AverageValiable;
                                isotopeTemps[i].PeakID = j;

                                mzFocused = isotopeMz;
                            }
                        }
                        
                    }
                    else if (param.IsBrClConsideredForIsotopes && i % 2 == 0 && diffMzClBr < tolerance) {
                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz, MzClBr = isotopeMz,
                                Intensity = isotopePeak.AverageValiable, PeakID = j
                            };
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].MzClBr = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.AverageValiable;
                                isotopeTemps[i].PeakID = j;

                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (isotopeMz >= predIsotopicMass + tolerance) {
                        if (j == alignSpots.Count - 1) break;
                        reminderIndex = j;
                        if (isotopeTemps[i - 1].PeakID == -1 && isotopeTemps[i].PeakID == -1) {
                            isFinished = true;
                        }
                        else if (isotopeTemps[i].PeakID == -1) {
                            mzFocused += (double)c13_c12Diff / (double)predChargeNumber;
                        }
                        break;
                    }

                }
                if (isFinished)
                    break;
            }

            var monoisotopicMass = (double)monoIsoPeak.CentralAccurateMass * (double)predChargeNumber;
            var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            CompoundPropertyBean simulatedIsotopicPeaks = null;
            if (monoisotopicMass > 800)
                simulatedIsotopicPeaks = IsotopeRatioCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);
            for (int i = 1; i <= maxTraceNumber; i++) {
                if (isotopeTemps[i].PeakID == -1) continue;
                if (isotopeTemps[i - 1].PeakID == -1 && isotopeTemps[i].PeakID == -1) break;

                if (monoisotopicMass <= 800) {
                    if (isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity && param.IsBrClConsideredForIsotopes == false) {
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeWeightNumber = i;
                        alignSpots[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                    }
                    else if (param.IsBrClConsideredForIsotopes == true) {
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeWeightNumber = i;
                        alignSpots[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
                    }
                    else {
                        break;
                    }
                }
                else {
                    if (isotopeTemps[i - 1].Intensity <= 0) break;
                    var expRatio = isotopeTemps[i].Intensity / isotopeTemps[i - 1].Intensity;
                    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[i].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[i - 1].RelativeAbundance;

                    if (Math.Abs(expRatio - simRatio) < 5.0) {
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeParentID = monoIsoPeak.AlignmentID;
                        alignSpots[isotopeTemps[i].PeakID].PostDefinedIsotopeWeightNumber = i;
                        alignSpots[isotopeTemps[i].PeakID].ChargeNumber = monoIsoPeak.ChargeNumber;
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
