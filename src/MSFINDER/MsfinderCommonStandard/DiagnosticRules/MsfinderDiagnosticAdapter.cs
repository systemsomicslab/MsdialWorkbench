using CompMs.Common.StructureFinder.DiagnosticRules;
using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// Turns an MS-FINDER query into the evaluator's four inputs (design 5.4): MS2 peaks, precursor m/z, ion mode and
    /// instrument type. Nothing else of the query reaches the evaluator.
    /// </summary>
    /// <remarks>
    /// The adapter reads only <see cref="RawData.PrecursorMz"/>, <see cref="RawData.IonMode"/>,
    /// <see cref="RawData.InstrumentType"/>, <see cref="RawData.SpectrumType"/>, <see cref="RawData.Ms2Spectrum"/>
    /// (its <see cref="Spectrum.PeakList"/>, and of each peak <see cref="Peak.Mz"/> and <see cref="Peak.Intensity"/>),
    /// <see cref="RawData.Ms2PeakNumber"/> and <see cref="RawData.RawdataFilePath"/>, the last two for the guards
    /// only, which can skip a query but never change what is evaluated. A test pins this list by scanning the IL.
    /// The peaks are those of <see cref="FragmentAssigner.GetCentroidMsMsSpectrum"/>, the list MS-FINDER's own
    /// structure finder uses (centroided when the query is a profile spectrum).
    /// </remarks>
    public static class MsfinderDiagnosticAdapter
    {
        /// <summary>
        /// The evaluator input of <paramref name="raw"/>, or null when a guard refuses its peaks
        /// (<see cref="DiagnosticInputInfo.PeaksSuspect"/> then says which).
        /// </summary>
        /// <param name="raw">The query as MS-FINDER read it.</param>
        /// <param name="fallbackInstrumentType">The DiagnosticRuleInstrumentType setting, used when the query has no
        /// instrument type; empty for none.</param>
        /// <param name="info">How the input was made.</param>
        public static DiagnosticSpectrum ToSpectrum(RawData raw, string fallbackInstrumentType, out DiagnosticInputInfo info) {
            if (raw == null) {
                throw new ArgumentNullException(nameof(raw));
            }
            var peaksSource = raw.SpectrumType == DataType.Centroid ? DiagnosticInputInfo.PeaksAsRead : DiagnosticInputInfo.PeaksMsfinderCentroid;

            string instrumentType;
            string instrumentSource;
            var queryType = raw.InstrumentType;
            if (!string.IsNullOrWhiteSpace(queryType)) {
                instrumentType = queryType.Trim();
                instrumentSource = DiagnosticInputInfo.InstrumentTypeFromQuery;
            }
            else if (!string.IsNullOrWhiteSpace(fallbackInstrumentType)) {
                instrumentType = fallbackInstrumentType.Trim();
                instrumentSource = DiagnosticInputInfo.InstrumentTypeFromOption;
            }
            else {
                instrumentType = null;
                instrumentSource = DiagnosticInputInfo.InstrumentTypeNone;
            }

            // The peaks MS-FINDER uses; only m/z and intensity are copied, non-positive intensities are dropped
            // (MassBank PK$ANNOTATION rows absent from PK$PEAK are read as zero-intensity peaks).
            var centroid = FragmentAssigner.GetCentroidMsMsSpectrum(raw);
            var peaks = new List<DiagnosticPeak>();
            var dropped = 0;
            var sub1Da = false;
            foreach (var peak in centroid) {
                var intensity = peak.Intensity;
                if (!(intensity > 0.0)) {
                    dropped++;
                    continue;
                }
                var mz = peak.Mz;
                if (mz < 1.0) {
                    sub1Da = true;
                }
                peaks.Add(new DiagnosticPeak(mz, intensity));
            }

            var suspect = Suspect(raw, sub1Da);
            info = new DiagnosticInputInfo(peaksSource, instrumentSource, dropped, suspect);
            if (suspect != null) {
                return null;
            }
            return new DiagnosticSpectrum(peaks, raw.PrecursorMz, IonModeOf(raw.IonMode), instrumentType);
        }

        /// <summary>Positive and Negative map directly; Both (or any other value) has no ion mode.</summary>
        public static DiagnosticIonMode? IonModeOf(IonMode mode) {
            switch (mode) {
                case IonMode.Positive:
                    return DiagnosticIonMode.Positive;
                case IonMode.Negative:
                    return DiagnosticIonMode.Negative;
                default:
                    return null;
            }
        }

        /// <summary>Whether MS-FINDER reads the file as a MassBank record (its reader tests the same suffix).</summary>
        public static bool IsMassBankRecord(string path) => path != null && path.EndsWith(".txt", StringComparison.Ordinal);

        private static string Suspect(RawData raw, bool sub1Da) {
            var path = raw.RawdataFilePath;
            var massBank = IsMassBankRecord(path);
            var readerPeaks = raw.Ms2Spectrum == null ? null : raw.Ms2Spectrum.PeakList;
            var declared = raw.Ms2PeakNumber;

            // The count the reader produced: as read for MSP/MAT; after the non-positive drop for MassBank, whose
            // annotation rows can add zero-intensity peaks.
            var count = 0;
            if (readerPeaks != null) {
                foreach (var peak in readerPeaks) {
                    if (!massBank || peak.Intensity > 0.0) {
                        count++;
                    }
                }
            }
            if (count != declared) {
                return DiagnosticInputInfo.PeakCountMismatch;
            }
            if (sub1Da) {
                return DiagnosticInputInfo.Sub1DaPeak;
            }
            if (!massBank && !string.IsNullOrEmpty(path) && !PeakBlockCheck.Agrees(path, readerPeaks)) {
                return DiagnosticInputInfo.PeakBlockDisagrees;
            }
            return null;
        }
    }
}
