using CompMs.Common.Components;
using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.FormulaGenerator.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Utility {
    public sealed class ChromMsConverter {
        private ChromMsConverter() { }

        public static List<SpectrumPeak> ConvertToSpectrumPeaks(RawPeakElement[] peakElements) {
            var peaks = new List<SpectrumPeak>();
            foreach (var peak in peakElements) {
                peaks.Add(new SpectrumPeak(peak.Mz, peak.Intensity));
            }
            return peaks;
        }

        /// <summary>
        /// from the list of m/z and intensity
        /// the list of scan (auto), times (by m/z), m/z, and intensity is created
        /// </summary>
        /// <param name="spectrum"></param>
        /// <returns></returns>
        public static List<ChromatogramPeak> ConvertRawPeakElementToChromatogramPeakList(RawPeakElement[] spectrum) {
            var chromPeaks = new List<ChromatogramPeak>();
            for (int i = 0; i < spectrum.Length; i++) {
                var mz = spectrum[i].Mz;
                var intensity = spectrum[i].Intensity;
                chromPeaks.Add(new ChromatogramPeak(i, mz, intensity, new MzValue(mz)));
            }
            return chromPeaks;
        }

        public static string GetSpectrumString(RawPeakElement[] spectrum, char mz_abs_separator = ':', char peaks_separater = ' ') {
            if (spectrum == null || spectrum.Length == 0)
                return string.Empty;

            var specString = string.Empty;
            for (int i = 0; i < spectrum.Length; i++) {
                var spec = spectrum[i];
                var mz = Math.Round(spec.Mz, 5);
                var intensity = Math.Round(spec.Intensity, 0);
                var sString = mz + mz_abs_separator + intensity;

                if (i == spectrum.Length - 1)
                    specString += sString;
                else
                    specString += sString + peaks_separater;
            }

            return specString;
        }

        public static List<IsotopicPeak> GetIsotopicPeaks(List<RawSpectrum> rawSpectrumList, int scanID, float targetedMz, float massTolerance, int maxIsotopes = 5) {
            if (scanID < 0 || rawSpectrumList == null || scanID > rawSpectrumList.Count - 1) return null;
            var spectrum = rawSpectrumList[scanID].Spectrum;
            var startID = GetMs1StartIndex(targetedMz, massTolerance, spectrum);
            var massDiffBase = MassDiffDictionary.C13_C12;
            var maxIsotopeRange = (double)maxIsotopes;
            var isotopes = new List<IsotopicPeak>();
            for (int i = 0; i < maxIsotopes; i++) {
                isotopes.Add(new IsotopicPeak() { 
                    Mass = targetedMz + (double)i * massDiffBase, 
                    MassDifferenceFromMonoisotopicIon = (double)i * massDiffBase 
                });
            }

            for (int i = startID; i < spectrum.Length; i++) {
                var peak = spectrum[i];
                if (peak.Mz < targetedMz - massTolerance) continue;
                if (peak.Mz > targetedMz + massDiffBase * maxIsotopeRange + massTolerance) break;

                foreach (var isotope in isotopes) {
                    if (Math.Abs(isotope.Mass - peak.Mz) < massTolerance) 
                        isotope.RelativeAbundance += peak.Intensity;
                }
            }

            if (isotopes[0].RelativeAbundance <= 0) return null;
            var baseIntensity = isotopes[0].RelativeAbundance;

            foreach (var isotope in isotopes) 
                isotope.RelativeAbundance = isotope.RelativeAbundance / baseIntensity * 100;

            return isotopes;
        }
        public static int GetMs1StartIndex(float targetedMass, float ms1Tolerance, RawPeakElement[] ms1peaks) {
            if (ms1peaks.Length == 0) return 0;
            float targetMass = targetedMass - ms1Tolerance;
            int startIndex = 0, endIndex = ms1peaks.Length - 1;
            int counter = 0;

            if (targetMass > ms1peaks[endIndex].Mz) return endIndex;

            while (counter < 10) {
                if (ms1peaks[startIndex].Mz <= targetMass && targetMass < ms1peaks[(startIndex + endIndex) / 2].Mz) {
                    endIndex = (startIndex + endIndex) / 2;
                }
                else if (ms1peaks[(startIndex + endIndex) / 2].Mz <= targetMass && targetMass < ms1peaks[endIndex].Mz) {
                    startIndex = (startIndex + endIndex) / 2;
                }
                counter++;
            }
            return startIndex;
        }

        
        public static ChromXs GetChromXsObj(double value, ChromXType type, ChromXUnit unit) {
            switch (type) {
                case ChromXType.RT: return new ChromXs() { RT = new RetentionTime(value, unit) };
                case ChromXType.RI: return new ChromXs() { RI = new RetentionIndex(value, unit) };
                case ChromXType.Drift: return new ChromXs() { Drift = new DriftTime(value, unit) };
                case ChromXType.Mz: return new ChromXs() { Mz = new MzValue(value, unit) };
                default: return new ChromXs() { RT = new RetentionTime(value) };
            }
        }
    }
}
