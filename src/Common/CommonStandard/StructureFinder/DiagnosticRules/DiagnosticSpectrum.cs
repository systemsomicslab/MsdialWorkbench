using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace CompMs.Common.StructureFinder.DiagnosticRules
{
    /// <summary>
    /// The four inputs of an evaluation (design 4.10, 6.1 item 1): peaks, precursor m/z, ion mode and instrument
    /// type. It has exactly these four members, and a reflection test (<c>InputSurfaceIsExactlyFourMembers</c>) fails
    /// on any widening: no formula, name, structure or other field of a query can reach a rule.
    /// </summary>
    public sealed class DiagnosticSpectrum
    {
        /// <param name="peaks">The MS/MS peaks in any order; copied, so later changes to the list are not seen.
        /// Null is read as no peaks.</param>
        /// <param name="precursorMz">The observed precursor m/z; a non-finite or non-positive value gives
        /// <c>no_precursor</c>.</param>
        /// <param name="ionMode">Null gives <c>unsupported_ion_mode</c>.</param>
        /// <param name="instrumentType">Free text, classified only by <c>InstrumentClassifier</c>; may be null.</param>
        public DiagnosticSpectrum(IReadOnlyList<DiagnosticPeak> peaks, double precursorMz, DiagnosticIonMode? ionMode, string instrumentType) {
            var copy = new DiagnosticPeak[peaks?.Count ?? 0];
            for (var i = 0; i < copy.Length; i++) {
                copy[i] = peaks![i];
            }
            Peaks = new ReadOnlyCollection<DiagnosticPeak>(copy);
            PrecursorMz = precursorMz;
            IonMode = ionMode;
            InstrumentType = instrumentType;
        }

        /// <summary>The peaks in input order; <c>peak_index</c> in results indexes this list.</summary>
        public IReadOnlyList<DiagnosticPeak> Peaks { get; }

        public double PrecursorMz { get; }

        public DiagnosticIonMode? IonMode { get; }

        public string InstrumentType { get; }
    }
}
