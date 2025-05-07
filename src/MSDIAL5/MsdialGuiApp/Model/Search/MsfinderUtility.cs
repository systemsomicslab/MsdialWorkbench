using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.App.Msdial.Model.Search {
    internal class MsfinderUtility {
        public MsfinderUtility() { }

        public static List<SpectrumPeak>? GetTheoreticalIsotopicIons(FormulaResult formulaVM, string precursorType, double precursorIntensity) {
            if (formulaVM == null) return null;

            var massSpectraCollection = new List<SpectrumPeak>();

            var isotopicPeaks = GetIsotopicPeaks(formulaVM.Formula);
            var adductIon = AdductIon.GetAdductIon(precursorType);

            foreach (var isotope in isotopicPeaks) {
                if (isotope.RelativeAbundance <= 0) continue;
                massSpectraCollection.Add(new SpectrumPeak() { Mass = isotope.Mass, Intensity = isotope.RelativeAbundance * precursorIntensity, Comment = isotope.Comment });
            }
            return massSpectraCollection;
        }

        public static List<SpectrumPeak>? GetExperimentalIsotopicIons(double precursorMz, List<SpectrumPeak> peaklist, out double precursorIntensity) {
            var massSpectraCollection = new List<SpectrumPeak>();
            var minDiff = double.MaxValue;
            precursorIntensity = double.MinValue;

            foreach (var peak in peaklist) {
                if (peak.Mass < precursorMz - 0.2) continue;
                if (peak.Mass > precursorMz + 2.2) break;
                if (Math.Abs(precursorMz - peak.Mass) < minDiff) { minDiff = Math.Abs(precursorMz - peak.Mass); precursorIntensity = peak.Intensity; }

                massSpectraCollection.Add(new SpectrumPeak() { Mass = peak.Mass, Intensity = peak.Intensity, Comment = Math.Round(peak.Mass, 4).ToString() });
            }
            return massSpectraCollection;
        }

        private static readonly double c13_c12 = 0.010815728;
        private static readonly double h2_h1 = 0.000115013;
        private static readonly double n15_n14 = 0.003653298;
        private static readonly double o17_o16 = 0.000380926;
        private static readonly double o18_o16 = 0.002054994;
        private static readonly double si29_si28 = 0.050800776;
        private static readonly double si30_si28 = 0.033527428;
        private static readonly double s33_s32 = 0.007895568;
        private static readonly double s34_s32 = 0.044741552;
        private static readonly double cl37_cl35 = 0.319957761;
        private static readonly double br81_br79 = 0.972775695;

        private static readonly double c13_c12_MassDiff = 1.003354838;
        private static readonly double h2_h1_MassDiff = 1.006276746;
        private static readonly double n15_n14_MassDiff = 0.997034893;
        private static readonly double o17_o16_MassDiff = 1.00421708;
        private static readonly double o18_o16_MassDiff = 2.00424638;
        private static readonly double si29_si28_MassDiff = 0.999568168;
        private static readonly double si30_si28_MassDiff = 1.996843638;
        private static readonly double s33_s32_MassDiff = 0.99938776;
        private static readonly double s34_s32_MassDiff = 1.9957959;
        private static readonly double cl37_cl35_MassDiff = 1.99704991;
        private static readonly double br81_br79_MassDiff = 1.9979535;

        private static List<IsotopicPeak> GetIsotopicPeaks(Formula formula) {
            var isotopicPeaks = new List<IsotopicPeak> {
                new() { RelativeAbundance = 1, Mass = formula.Mass, Comment = formula.FormulaString },
                new() { RelativeAbundance = formula.Cnum * c13_c12, Mass = formula.Mass + c13_c12_MassDiff, Comment = "13C" },
                new() { RelativeAbundance = formula.Hnum * h2_h1, Mass = formula.Mass + h2_h1_MassDiff, Comment = "2H" },
                new() { RelativeAbundance = formula.Nnum * n15_n14, Mass = formula.Mass + n15_n14_MassDiff, Comment = "15N" },
                new() { RelativeAbundance = formula.Onum * o17_o16, Mass = formula.Mass + o17_o16_MassDiff, Comment = "17O" },
                new() { RelativeAbundance = formula.Snum * s33_s32, Mass = formula.Mass + s33_s32_MassDiff, Comment = "33S" },
                new() { RelativeAbundance = formula.Sinum * si29_si28, Mass = formula.Mass + si29_si28_MassDiff, Comment = "29Si" },
                new() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Hnum * h2_h1, Mass = formula.Mass + c13_c12_MassDiff + h2_h1_MassDiff, Comment = "2H,13C" },
                new() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Nnum * n15_n14, Mass = formula.Mass + c13_c12_MassDiff + n15_n14_MassDiff, Comment = "13C,15N" },
                new() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Onum * o17_o16, Mass = formula.Mass + c13_c12_MassDiff + o17_o16_MassDiff, Comment = "13C,17O" },
                new() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Snum * s33_s32, Mass = formula.Mass + c13_c12_MassDiff + s33_s32_MassDiff, Comment = "13C,33S" },
                new() { RelativeAbundance = formula.Cnum * c13_c12 * formula.Sinum * si29_si28, Mass = formula.Mass + c13_c12_MassDiff + si29_si28_MassDiff, Comment = "13C,29Si" },
                new() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Nnum * n15_n14, Mass = formula.Mass + h2_h1_MassDiff + n15_n14_MassDiff, Comment = "2H,15N" },
                new() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Onum * o17_o16, Mass = formula.Mass + h2_h1_MassDiff + o17_o16_MassDiff, Comment = "2H,17O" },
                new() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Snum * s33_s32, Mass = formula.Mass + h2_h1_MassDiff + s33_s32_MassDiff, Comment = "2H,33S" },
                new() { RelativeAbundance = formula.Hnum * h2_h1 * formula.Sinum * si29_si28, Mass = formula.Mass + h2_h1_MassDiff + si29_si28_MassDiff, Comment = "2H,29Si" },
                new() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Onum * o17_o16, Mass = formula.Mass + n15_n14_MassDiff + o17_o16_MassDiff, Comment = "15N,17O" },
                new() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Snum * s33_s32, Mass = formula.Mass + n15_n14_MassDiff + s33_s32_MassDiff, Comment = "15N,33S" },
                new() { RelativeAbundance = formula.Nnum * n15_n14 * formula.Sinum * si29_si28, Mass = formula.Mass + n15_n14_MassDiff + si29_si28_MassDiff, Comment = "15N,29Si" },
                new() { RelativeAbundance = formula.Onum * o17_o16 * formula.Snum * s33_s32, Mass = formula.Mass + o17_o16_MassDiff + s33_s32_MassDiff, Comment = "17O,33S" },
                new() { RelativeAbundance = formula.Onum * o17_o16 * formula.Sinum * si29_si28, Mass = formula.Mass + o17_o16_MassDiff + si29_si28_MassDiff, Comment = "17O,29Si" },
                new() { RelativeAbundance = formula.Snum * s33_s32 * formula.Sinum * si29_si28, Mass = formula.Mass + s33_s32_MassDiff + si29_si28_MassDiff, Comment = "29Si,33S" },
                new() { RelativeAbundance = formula.Cnum * (formula.Cnum - 1) * 0.5 * Math.Pow(c13_c12, 2), Mass = formula.Mass + c13_c12_MassDiff * 2.0, Comment = "13C,13C" },
                new() { RelativeAbundance = formula.Hnum * (formula.Hnum - 1) * 0.5 * Math.Pow(h2_h1, 2), Mass = formula.Mass + h2_h1_MassDiff * 2.0, Comment = "2H,2H" },
                new() { RelativeAbundance = formula.Nnum * (formula.Nnum - 1) * 0.5 * Math.Pow(n15_n14, 2), Mass = formula.Mass + n15_n14_MassDiff * 2.0, Comment = "15N,15N" },
                new() { RelativeAbundance = formula.Onum * (formula.Onum - 1) * 0.5 * Math.Pow(o17_o16, 2), Mass = formula.Mass + o17_o16_MassDiff * 2.0, Comment = "17O,17O" },
                new() { RelativeAbundance = formula.Snum * (formula.Snum - 1) * 0.5 * Math.Pow(s33_s32, 2), Mass = formula.Mass + s33_s32_MassDiff * 2.0, Comment = "33S,33S" },
                new() { RelativeAbundance = formula.Sinum * (formula.Sinum - 1) * 0.5 * Math.Pow(si29_si28, 2), Mass = formula.Mass + si29_si28_MassDiff * 2.0, Comment = "29Si,29Si" },
                new() { RelativeAbundance = formula.Onum * o18_o16, Mass = formula.Mass + o18_o16_MassDiff, Comment = "18O" },
                new() { RelativeAbundance = formula.Snum * s34_s32, Mass = formula.Mass + s34_s32_MassDiff, Comment = "34S" },
                new() { RelativeAbundance = formula.Sinum * si30_si28, Mass = formula.Mass + si30_si28_MassDiff, Comment = "30Si" },
                new() { RelativeAbundance = formula.Brnum * br81_br79, Mass = formula.Mass + br81_br79_MassDiff, Comment = "81Br" },
                new() { RelativeAbundance = formula.Clnum * cl37_cl35, Mass = formula.Mass + cl37_cl35_MassDiff, Comment = "37Cl" }
            };

            return isotopicPeaks;
        }

        public static string GetLabelForInsilicoSpectrum(string formula, double penalty, IonMode ionMode, string adductString) {
            var hydrogen = (int)Math.Abs(Math.Round(penalty, 0));
            var hydrogenString = hydrogen.ToString(); if (hydrogen == 1) hydrogenString = string.Empty;
            var ionString = string.Empty; if (ionMode == IonMode.Positive) ionString = "+"; else ionString = "-";
            var frgString = "[" + formula;

            if (penalty < 0) {
                frgString += "-" + hydrogenString + "H";
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            } else if (penalty > 0) {
                frgString += "+" + hydrogenString + "H";
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            } else {
                if (adductString != null && adductString != string.Empty) frgString += adductString;
            }

            frgString += "]" + ionString;

            return frgString;
        }
    }
}
