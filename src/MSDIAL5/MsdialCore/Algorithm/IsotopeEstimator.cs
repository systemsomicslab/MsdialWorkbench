using CompMs.Common.Algorithm.IsotopeCalc;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using CompMs.Common.FormulaGenerator.Function;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm
{

    public class IsotopeTemp {
        public int WeightNumber { get; set; }
        public double Mz { get; set; }
        public double MzClBr { get; set; }
        public double Intensity { get; set; }
        public int PeakID { get; set; }
    }

    public sealed class IsotopeEstimator
    {
        /// <summary>
        /// This method tries to decide if the detected peak is the isotopic ion or not.
        /// The peaks less than the abundance of the mono isotopic ion will be assigned to the isotopic ions within the same data point.
        /// </summary>
        /// <param name="peakAreaBeanCollection"></param>
        /// <param name="analysisParametersBean"></param>
        public static void Process(
            IReadOnlyList<ChromatogramPeakFeature> peakFeatures,
            ParameterBase param, 
            IupacDatabase iupac, bool isDriftAxis = false)
        {
            var peaks = peakFeatures.OrderBy(n => n.PrecursorMz).ToList();

            //var spectrumMargin = 2;
            var xMargin = isDriftAxis ? 0.01F : 0.25F;
            var isotopeMax = 8.1;
            foreach (var peak in peaks) {
                var peakCharacter = peak.PeakCharacter;
                //if (peak.MasterPeakID == 10999) {
                //    Console.WriteLine();
                //}
                //if (peak.PeakID == 10999) {
                //    Console.WriteLine();
                //} 
                if (peakCharacter.IsotopeWeightNumber >= 0) continue;

                // var focusedScan = peak.ScanNumberAtPeakTop;
                var focusedMass = peak.PrecursorMz;
                var focusedXValue = isDriftAxis ? peak.ChromXsTop.Drift.Value : peak.ChromXsTop.RT.Value;

                var startScanIndex = SearchCollection.LowerBound(peaks, focusedMass - param.CentroidMs1Tolerance, (a, b) => a.Mass.CompareTo(b));
                //DataAccess.GetScanStartIndexByMz((float)focusedMass - param.CentroidMs1Tolerance, peakFeatures);
                var isotopeCandidates = new List<ChromatogramPeakFeature>() { peak };

                for (int j = startScanIndex; j < peaks.Count; j++) {

                    var xValue = isDriftAxis ? peaks[j].ChromXsTop.Drift.Value : peaks[j].ChromXsTop.RT.Value;
                    if (peaks[j].PrecursorMz <= focusedMass) continue;
                    if (peaks[j].PrecursorMz > focusedMass + isotopeMax) break;
                    if (peaks[j].PeakID == peak.PeakID) continue;
                    if (xValue < focusedXValue - xMargin) continue;
                    if (xValue > focusedXValue + xMargin) continue;
                    if (peaks[j].PeakCharacter.IsotopeWeightNumber >= 0) continue;
                   
                    isotopeCandidates.Add(peaks[j]);
                }
                EstimateIsotopes(isotopeCandidates, param, iupac, isDriftAxis);
            }
        }
     
        public static void EstimateIsotopes(
            List<ChromatogramPeakFeature> peakFeatures,
            ParameterBase param,
            IupacDatabase iupac, bool isDriftAxis = false) {
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var br81_br79 = MassDiffDictionary.Br81_Br79; //1.9979535; also to be used for S34_S32 (1.9957959), Cl37_Cl35 (1.99704991)
            var tolerance = param.CentroidMs1Tolerance;
            var monoIsoPeak = peakFeatures[0];
            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + param.CentroidMs1Tolerance); //based on m/z 200
            var accuracy = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.PrecursorMz, ppm);
            var xMargin = isDriftAxis ? 0.005F : 0.06F;

            tolerance = (float)accuracy;
            if (tolerance < param.CentroidMs1Tolerance) tolerance = param.CentroidMs1Tolerance;
            var isFinished = false;

            monoIsoPeak.PeakCharacter.IsotopeWeightNumber = 0;
            //monoIsoPeak.PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
            monoIsoPeak.PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;

            //if (Math.Abs(monoIsoPeak.AccurateMass - 762.5087) < 0.001) {
            //    Console.WriteLine();
            //}
            var xMonoisotope = isDriftAxis ? monoIsoPeak.ChromXsTop.Drift.Value : monoIsoPeak.ChromXsTop.RT.Value;
            var xFocused = isDriftAxis ? monoIsoPeak.ChromXsTop.Drift.Value : monoIsoPeak.ChromXsTop.RT.Value;
            //charge number check at M + 1
            var predChargeNumber = 1;
            for (int j = 1; j < peakFeatures.Count; j++) {
                var isotopePeak = peakFeatures[j];
                if (isotopePeak.PrecursorMz > monoIsoPeak.PrecursorMz + c13_c12Diff + tolerance) break;
                var isotopeXValue = isDriftAxis ? isotopePeak.ChromXsTop.Drift.Value : isotopePeak.ChromXsTop.RT.Value;

                for (int k = param.MaxChargeNumber; k >= 1; k--) {
                    var predIsotopeMass = (double)monoIsoPeak.PrecursorMz + (double)c13_c12Diff / (double)k;
                    var diff = Math.Abs(predIsotopeMass - isotopePeak.PrecursorMz);
                    var diffX = Math.Abs(xMonoisotope - isotopeXValue);
                    if (diff < tolerance && diffX < xMargin) {
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

            var maxTraceNumber =15;
            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];
            isotopeTemps[0] = new IsotopeTemp() {
                WeightNumber = 0, Mz = monoIsoPeak.PrecursorMz,
                MzClBr = monoIsoPeak.PrecursorMz,
                Intensity = monoIsoPeak.PeakHeightTop, PeakID = monoIsoPeak.PeakID
            };
            //isotopeTemps[0] = new IsotopeTemp() {
            //    WeightNumber = 0, Mz = monoIsoPeak.PrecursorMz,
            //    MzClBr = monoIsoPeak.PrecursorMz,
            //    Intensity = monoIsoPeak.PeakHeightTop, PeakID = monoIsoPeak.MasterPeakID
            //};
            for (int i = 1; i < isotopeTemps.Length; i++) {
                isotopeTemps[i] = new IsotopeTemp() {
                    WeightNumber = i, Mz = monoIsoPeak.PrecursorMz + (double)i * c13_c12Diff / (double)predChargeNumber,
                    MzClBr = i % 2 == 0 ? monoIsoPeak.PrecursorMz + (double)i * c13_c12Diff / (double)predChargeNumber : monoIsoPeak.PrecursorMz + (double)i * br81_br79 * 0.5 / (double)predChargeNumber,
                    Intensity = 0, PeakID = -1
                };
            }

            var reminderIndex = 1;
            var mzFocused = (double)monoIsoPeak.PrecursorMz;
            for (int i = 1; i <= maxTraceNumber; i++) {

                //var predIsotopicMass = (double)monoIsoPeak.PrecursorMz + (double)i * c13_c12Diff / (double)predChargeNumber;
                var predIsotopicMass = mzFocused + (double)c13_c12Diff / (double)predChargeNumber;
                var predClBrIsotopicMass = mzFocused + (double)br81_br79 * 0.5 / (double)predChargeNumber;
                for (int j = reminderIndex; j < peakFeatures.Count; j++) {

                    var isotopePeak = peakFeatures[j];
                    var isotopeXValue = isDriftAxis ? isotopePeak.ChromXsTop.Drift.Value : isotopePeak.ChromXsTop.RT.Value;
                    var isotopeMz = isotopePeak.PrecursorMz;
                    var diffMz = Math.Abs(predIsotopicMass - isotopeMz);
                    var diffMzClBr = Math.Abs(predClBrIsotopicMass - isotopeMz);
                    var diffXValue = Math.Abs(xFocused - isotopeXValue);

                    if (diffMz < tolerance && diffXValue < xMargin) {

                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz,
                                Intensity = isotopePeak.PeakHeightTop, PeakID = j
                            };
                            xFocused = isotopeXValue;
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.PeakHeightTop;
                                isotopeTemps[i].PeakID = j;

                                xFocused = isotopeXValue;
                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (param.IsBrClConsideredForIsotopes && i % 2 == 0 && diffMzClBr < tolerance && diffXValue < xMargin) {
                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz, MzClBr = isotopeMz,
                                Intensity = isotopePeak.PeakHeightTop, PeakID = j
                            };
                            xFocused = isotopeXValue;
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].MzClBr = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.PeakHeightTop;
                                isotopeTemps[i].PeakID = j;

                                xFocused = isotopeXValue;
                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (isotopePeak.PrecursorMz >= predIsotopicMass + tolerance) {
                        if (j == peakFeatures.Count - 1) break;
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

            var monoisotopicMass = (double)monoIsoPeak.PrecursorMz * (double)predChargeNumber;
            var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            IsotopeProperty simulatedIsotopicPeaks = null;
            var isIsotopeDetected = false;
            if (monoisotopicMass > 800)
                simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);
            for (int i = 1; i <= maxTraceNumber; i++) {
                if (isotopeTemps[i].PeakID == -1) continue;
                if (isotopeTemps[i - 1].PeakID == -1 && isotopeTemps[i].PeakID == -1) break;

                if (monoisotopicMass <= 800) {
                    if (isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity && param.IsBrClConsideredForIsotopes == false) {
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
                        isIsotopeDetected = true;
                    }
                    else if (param.IsBrClConsideredForIsotopes == true) {
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
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
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.PeakID;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                        peakFeatures[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
                        isIsotopeDetected = true;
                    }
                    else {
                        break;
                    }
                }
            }
            if (!isIsotopeDetected) {
                monoIsoPeak.PeakCharacter.Charge = 1;
            }
        }

        /// <summary>
        /// peak list must be sorted by m/z (ordering)
        /// peak should be initialized by new Peak() { Mz = spec[0], Intensity = spec[1], Charge = 1, IsotopeFrag = false  }
        /// </summary>
        public static void EstimateIsotopes(List<SpectrumPeak> peaks, ParameterBase param, IupacDatabase iupac, double mztolerance, int maxChargeNumber = 0) {

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
                isotopeTemps[0] = new IsotopeTemp() { WeightNumber = 0, 
                    Mz = peak.Mass, Intensity = peak.Intensity, PeakID = i, MzClBr = peak.Mass };

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
                        else if (param.IsBrClConsideredForIsotopes && j % 2 == 0 && diffMzClBr < tolerance) {
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
                        #region
                        //if (predIsotopicMass - tolerance < isotopePeak.Mass && isotopePeak.Mass < predIsotopicMass + tolerance) {
                        //    if (isotopeTemps[j] == null) {
                        //        isotopeTemps[j] = new IsotopeTemp() {
                        //            WeightNumber = j,
                        //            Mz = isotopePeak.Mass,
                        //            Intensity = isotopePeak.Intensity,
                        //            PeakID = k
                        //        };
                        //    } else {
                        //        if (Math.Abs(isotopeTemps[j].Mz - predIsotopicMass) > Math.Abs(isotopePeak.Mass - predIsotopicMass)) {
                        //            isotopeTemps[j].Mz = isotopePeak.Mass;
                        //            isotopeTemps[j].Intensity = isotopePeak.Intensity;
                        //            isotopeTemps[j].PeakID = k;
                        //        }
                        //    }
                        //}
                        //else if (isotopePeak.Mass >= predIsotopicMass + tolerance) {
                        //    reminderIndex = k;
                        //    if (isotopeTemps[j] == null) isFinished = true;
                        //    break;
                        //}
                        #endregion
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
                if (monoisotopicMass > 800)
                    simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);

                for (int j = 1; j <= maxTraceNumber; j++) {
                    if (isotopeTemps[j].PeakID == -1) continue;
                    if (isotopeTemps[j - 1].PeakID == -1 && isotopeTemps[j].PeakID == -1) break;

                    if (monoisotopicMass <= 800) {
                        if (isotopeTemps[j - 1].Intensity > isotopeTemps[j].Intensity && param.IsBrClConsideredForIsotopes == false) {
                            peaks[isotopeTemps[j].PeakID].IsotopeParentPeakID = peak.PeakID;
                            peaks[isotopeTemps[j].PeakID].IsotopeWeightNumber = j;
                            peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                            isIsotopeDetected = true;
                        }
                        else if (param.IsBrClConsideredForIsotopes == true) {
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


                    //if (monoisotopicMass <= 800) {
                    //    if (isotopeTemps[j - 1].Intensity > isotopeTemps[j].Intensity) {
                    //        peaks[isotopeTemps[j].PeakID].IsotopeFrag = true;
                    //        peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                    //        peaks[isotopeTemps[j].PeakID].Comment = i.ToString();
                    //    }
                    //    else {
                    //        break;
                    //    }
                    //}
                    //else {
                    //    var expRatio = isotopeTemps[j].Intensity / isotopeTemps[j - 1].Intensity;
                    //    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[j].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[j - 1].RelativeAbundance;

                    //    if (Math.Abs(expRatio - simRatio) < 5.0) {
                    //        peaks[isotopeTemps[j].PeakID].IsotopeFrag = true;
                    //        peaks[isotopeTemps[j].PeakID].Charge = peak.Charge;
                    //        peaks[isotopeTemps[j].PeakID].Comment = i.ToString();
                    //    }
                    //    else {
                    //        break;
                    //    }
                    //}
                }
                if (!isIsotopeDetected) {
                    peak.Charge = 1;
                }
            }
        }

        public static void Process(IEnumerable<AlignmentSpotProperty> alignmentSpots, ParameterBase param, IupacDatabase iupac) {
            var rtMargin = 0.06F;
            var isotopeMax = 8.1;
            var spots = alignmentSpots.OrderBy(spot => spot.MassCenter).ToList();
            var dummy = new AlignmentSpotProperty(); // used for binary search

            foreach (var target in spots) {
                if (target.PeakCharacter.IsotopeWeightNumber > 0) continue;

                var spotRt = target.TimesCenter;
                var spotMz = target.MassCenter;

                dummy.MassCenter = spotMz - 0.0001f;
                var idx = SearchCollection.LowerBound(spots, dummy, (x, y) => x.MassCenter.CompareTo(y.MassCenter));

                var isotopeCandidates = new List<AlignmentSpotProperty> { target };

                for (int i = idx; i < spots.Count; i++) {
                    var spot = spots[i];
                    if (spot.MassCenter <= spotMz) continue;
                    if (spot.MassCenter > spotMz + isotopeMax) break;
                    if (spot.MasterAlignmentID == target.MasterAlignmentID) continue;
                    if (!spot.IsUnknown) continue;
                    if (spot.TimesCenter.Value < spotRt.Value - rtMargin) continue;
                    if (spot.TimesCenter.Value > spotRt.Value + rtMargin) continue;
                    if (spot.PeakCharacter.IsotopeWeightNumber >= 0) continue;

                    isotopeCandidates.Add(spot);
                }
                EstimateIsotopes(isotopeCandidates, param, iupac);
            }
        }

        public static void EstimateIsotopes(List<AlignmentSpotProperty> spots, ParameterBase param, IupacDatabase iupac) {
            EstimateIsotopes(spots, iupac, param.CentroidMs1Tolerance, param.IsBrClConsideredForIsotopes);
        }

        public static void EstimateIsotopes(List<AlignmentSpotProperty> spots, IupacDatabase iupac, double ms1Tolerance, bool isBrClConsidreredForIsotopes = false) {

            var maxTraceNumber = 15;
            var c13_c12Diff = MassDiffDictionary.C13_C12;  //1.003355F;
            var br81_br79 = MassDiffDictionary.Br81_Br79;  //1.9979535; also to be used for S34_S32 (1.9957959), Cl37_Cl35 (1.99704991)
            var monoIsoPeak = spots[0];
            monoIsoPeak.PeakCharacter.IsotopeWeightNumber = 0;
            monoIsoPeak.PeakCharacter.IsotopeParentPeakID = monoIsoPeak.AlignmentID;

            var ppm = MolecularFormulaUtility.PpmCalculator(200.0, 200.0 + ms1Tolerance); //based on m/z 400
            var tolerance = MolecularFormulaUtility.ConvertPpmToMassAccuracy(monoIsoPeak.MassCenter, ppm);
            if (tolerance < ms1Tolerance) tolerance = ms1Tolerance;
            var predChargeNumber = monoIsoPeak.PeakCharacter.Charge;

            var isotopeTemps = GetIsotopeCandidates(spots, monoIsoPeak, c13_c12Diff / predChargeNumber, br81_br79 / predChargeNumber, tolerance, maxTraceNumber, isBrClConsidreredForIsotopes);
            SetParentToIsotopes(spots, monoIsoPeak, isotopeTemps, maxTraceNumber, iupac, isBrClConsidreredForIsotopes);
        }

        private static IsotopeTemp[] GetIsotopeCandidates(IReadOnlyList<AlignmentSpotProperty> spots,
            AlignmentSpotProperty monoIsoPeak, double isotopeUnit, double isotopeUnitBr, double tolerance, int maxTraceNumber, bool isBrClConsidreredForIsotopes) {

            var isotopeTemps = new IsotopeTemp[maxTraceNumber + 1];

            var mzFocused = monoIsoPeak.MassCenter;
            isotopeTemps[0] = new IsotopeTemp() {
                WeightNumber = 0, Mz = mzFocused, MzClBr = mzFocused,
                Intensity = monoIsoPeak.HeightAverage, PeakID = monoIsoPeak.AlignmentID
            };
            for (int i = 1; i < isotopeTemps.Length; i++) {
                isotopeTemps[i] = new IsotopeTemp() {
                    WeightNumber = i, Mz = mzFocused + i * isotopeUnit,
                    MzClBr = i % 2 == 0 ? mzFocused + i * isotopeUnit : mzFocused + i * isotopeUnitBr * 0.5,
                    Intensity = 0, PeakID = -1
                };
            }
            
            var j = 1;
            for (int i = 1; i <= maxTraceNumber; i++) {
                var predIsotopicMass = mzFocused + isotopeUnit;
                var predClBrIsotopicMass = mzFocused + isotopeUnitBr * 0.5;
                for (; j < spots.Count; j++) {

                    var isotopePeak = spots[j];
                    var isotopeMz = isotopePeak.MassCenter;

                    if (Math.Abs(predIsotopicMass - isotopeMz) < tolerance) {

                        if (isotopeTemps[i].PeakID == -1 || Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                            isotopeTemps[i].Mz = isotopeMz;
                            isotopeTemps[i].Intensity = isotopePeak.HeightAverage;
                            isotopeTemps[i].PeakID = j;

                            mzFocused = isotopeMz;
                        }
                    }
                    else if (isBrClConsidreredForIsotopes && i % 2 == 0 && Math.Abs(predClBrIsotopicMass - isotopeMz) < tolerance) {
                        if (isotopeTemps[i].PeakID == -1) {
                            isotopeTemps[i] = new IsotopeTemp() {
                                WeightNumber = i, Mz = isotopeMz, MzClBr = isotopeMz,
                                Intensity = isotopePeak.HeightAverage, PeakID = j
                            };
                            mzFocused = isotopeMz;
                        }
                        else {
                            if (Math.Abs(isotopeTemps[i].Mz - predIsotopicMass) > Math.Abs(isotopeMz - predIsotopicMass)) {
                                isotopeTemps[i].Mz = isotopeMz;
                                isotopeTemps[i].MzClBr = isotopeMz;
                                isotopeTemps[i].Intensity = isotopePeak.HeightAverage;
                                isotopeTemps[i].PeakID = j;

                                mzFocused = isotopeMz;
                            }
                        }
                    }
                    else if (isotopeMz >= predIsotopicMass + tolerance) {
                        if (isotopeTemps[i].PeakID == -1) {
                            if (isotopeTemps[i - 1].PeakID == -1)
                                return isotopeTemps;
                            mzFocused += isotopeUnit;
                        }
                        break;
                    }
                }
            }

            return isotopeTemps;
        }

        private static void SetParentToIsotopes(IList<AlignmentSpotProperty> spots, AlignmentSpotProperty monoIsoPeak,
            IReadOnlyList<IsotopeTemp> isotopeTemps, int maxTraceNumber, IupacDatabase iupac, bool isBrClConsidered) {

            Func<int, bool> predicate = i => isBrClConsidered || isotopeTemps[i - 1].Intensity > isotopeTemps[i].Intensity;
            var monoisotopicMass = monoIsoPeak.MassCenter * monoIsoPeak.PeakCharacter.Charge;

            //from here, simple decreasing will be expected for <= 800 Da
            //simulated profiles by alkane formula will be projected to the real abundances for the peaks of more than 800 Da
            if (monoisotopicMass > 800) {
                var simulatedFormulaByAlkane = getSimulatedFormulaByAlkane(monoisotopicMass);
                var simulatedIsotopicPeaks = IsotopeCalculator.GetNominalIsotopeProperty(simulatedFormulaByAlkane, maxTraceNumber + 1, iupac);
                predicate = i => {
                    if (isotopeTemps[i - 1].Intensity <= 0) return false;
                    var expRatio = isotopeTemps[i].Intensity / isotopeTemps[i - 1].Intensity;
                    var simRatio = simulatedIsotopicPeaks.IsotopeProfile[i].RelativeAbundance / simulatedIsotopicPeaks.IsotopeProfile[i - 1].RelativeAbundance;

                    return Math.Abs(expRatio - simRatio) < 5.0;
                };
            }

            for (int i = 1; i <= maxTraceNumber; i++) {
                if (isotopeTemps[i].PeakID == -1) {
                    if (isotopeTemps[i - 1].PeakID == -1) break;
                    continue;
                }
                if (!predicate(i)) break;

                spots[isotopeTemps[i].PeakID].PeakCharacter.IsotopeParentPeakID = monoIsoPeak.AlignmentID;
                spots[isotopeTemps[i].PeakID].PeakCharacter.IsotopeWeightNumber = i;
                spots[isotopeTemps[i].PeakID].PeakCharacter.Charge = monoIsoPeak.PeakCharacter.Charge;
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
