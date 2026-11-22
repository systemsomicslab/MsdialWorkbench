using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// One record of a multi-record MSP as the diagrules reader keeps it (design section 8 PR 5): PRECURSORMZ,
    /// PRECURSORTYPE, IONMODE, INSTRUMENTTYPE, COLLISIONENERGY, SPECTRUMTYPE, the MS2 peaks and the record index.
    /// Nothing else of the record is read.
    /// </summary>
    public sealed class DiagrulesRecord
    {
        internal DiagrulesRecord(string fileName, int recordIndex) {
            FileName = fileName;
            RecordIndex = recordIndex;
        }

        /// <summary>The MSP file name (no directory).</summary>
        public string FileName { get; }

        /// <summary>The 0-based index of the record in its file.</summary>
        public int RecordIndex { get; }

        /// <summary>PRECURSORMZ (invariant culture), or 0 when absent, unparseable, not finite or not positive.</summary>
        public double PrecursorMz { get; internal set; }

        /// <summary>PRECURSORTYPE as written (trimmed), or null. Never passed to the base evaluation.</summary>
        public string PrecursorType { get; internal set; }

        /// <summary>IONMODE as written (trimmed), or null.</summary>
        public string IonModeText { get; internal set; }

        /// <summary>INSTRUMENTTYPE as written (trimmed), or null.</summary>
        public string InstrumentType { get; internal set; }

        /// <summary>COLLISIONENERGY as written (trimmed), or null. Report only.</summary>
        public string CollisionEnergy { get; internal set; }

        /// <summary>SPECTRUMTYPE as written (trimmed), or null.</summary>
        public string SpectrumTypeText { get; internal set; }

        /// <summary>The count of the MS2 block's <c>Num Peaks</c> line (0 without a block).</summary>
        public int DeclaredPeakCount { get; internal set; }

        /// <summary>The MS2 peaks as <see cref="Peak"/> objects (rounded by <see cref="Peak"/> itself), in the reader's
        /// stable m/z order.</summary>
        public List<Peak> Peaks { get; internal set; } = new List<Peak>();

        /// <summary>The reason the record's peak lines are outside the strict grammar
        /// (<see cref="DiagnosticInputInfo.PeakBlockDisagrees"/> or <see cref="DiagnosticInputInfo.PeakCountMismatch"/>),
        /// or null.</summary>
        public string PeaksSuspect { get; internal set; }

        /// <summary><see cref="IonModeText"/> was absent (the record is read as positive, as MS-FINDER reads it).</summary>
        public bool IonModeAbsent => IonModeText == null;

        /// <summary>The ion mode as MS-FINDER's reader maps it: a value containing P or p is positive, any other
        /// value negative, an absent one positive (the RawData default).</summary>
        public IonMode IonMode => IonModeText == null || IonModeText.IndexOf('P') >= 0 || IonModeText.IndexOf('p') >= 0
            ? IonMode.Positive : IonMode.Negative;

        /// <summary>Profile as MS-FINDER's reader decides it: a SPECTRUMTYPE without an upper-case C. Absent means
        /// centroid (the RawData default).</summary>
        public bool IsProfile => SpectrumTypeText != null && SpectrumTypeText.IndexOf('C') < 0;

        /// <summary>The report-only stratum of <see cref="CollisionEnergy"/>.</summary>
        public string CollisionEnergyBin => DiagnosticRules.CollisionEnergyBin.Of(CollisionEnergy);

        /// <summary>
        /// The record as an in-memory MS-FINDER query, holding only what the PR 4 adapter reads. It has no file path,
        /// so the adapter applies the count and sub-1-Da guards and not its own re-read of a file.
        /// </summary>
        public RawData ToRawData() {
            var raw = new RawData {
                PrecursorMz = PrecursorMz,
                IonMode = IonMode,
                SpectrumType = IsProfile ? DataType.Profile : DataType.Centroid,
                Ms2PeakNumber = DeclaredPeakCount,
            };
            if (InstrumentType != null) {
                raw.InstrumentType = InstrumentType;
            }
            raw.Ms2Spectrum.PeakList = new List<Peak>(Peaks);
            return raw;
        }
    }

    /// <summary>
    /// The allowlisted multi-record MSP reader of <c>diagrules</c> (design section 8 PR 5). Records are maximal runs of
    /// non-blank lines. A header line is <c>key: value</c>; only the six allowlisted keys are kept (ASCII
    /// case-insensitive, exact, the last occurrence wins). Peak blocks are found as the MSDIAL4 reader finds them
    /// (<c>Num Peaks:</c>, and <c>MSTYPE: MS1|MS2</c> before one) and each line must be in the strict grammar of the
    /// <c>peak_block_disagrees</c> guard (<see cref="PeakBlockCheck"/>). Numbers are read with the invariant culture.
    /// </summary>
    public static class DiagrulesMspReader
    {
        private const string PrecursorMzKey = "precursormz";
        private const string PrecursorTypeKey = "precursortype";
        private const string IonModeKey = "ionmode";
        private const string InstrumentTypeKey = "instrumenttype";
        private const string CollisionEnergyKey = "collisionenergy";
        private const string SpectrumTypeKey = "spectrumtype";

        /// <summary>The records of <paramref name="path"/>, in file order, read lazily.</summary>
        public static IEnumerable<DiagrulesRecord> Read(string path) {
            var fileName = Path.GetFileName(path);
            var lines = new List<string>();
            var index = 0;
            using (var reader = new StreamReader(path, Encoding.UTF8, true)) {
                string line;
                while ((line = reader.ReadLine()) != null) {
                    if (IsBlank(line)) {
                        if (lines.Count > 0) {
                            yield return Parse(fileName, index++, lines);
                            lines = new List<string>();
                        }
                        continue;
                    }
                    lines.Add(line);
                }
            }
            if (lines.Count > 0) {
                yield return Parse(fileName, index, lines);
            }
        }

        /// <summary>Parses one record (its non-blank lines).</summary>
        public static DiagrulesRecord Parse(string fileName, int recordIndex, IReadOnlyList<string> lines) {
            var record = new DiagrulesRecord(fileName, recordIndex);
            List<Peak> ms2 = null;
            var declared = 0;
            var i = 0;
            while (i < lines.Count) {
                var line = lines[i];
                if (PeakBlockCheck.IsMarker(line)) {
                    int count;
                    var level = 2;
                    int start;
                    if (PeakBlockCheck.TryParseNumPeaks(line, out count)) {
                        start = i + 1;
                    }
                    else if (PeakBlockCheck.TryParseMsType(line, out level) && i + 1 < lines.Count && PeakBlockCheck.TryParseNumPeaks(lines[i + 1], out count)) {
                        start = i + 2;
                    }
                    else {
                        record.PeaksSuspect = DiagnosticInputInfo.PeakBlockDisagrees;
                        break;
                    }
                    var block = Block(lines, start, count, out var suspect);
                    if (block == null) {
                        record.PeaksSuspect = suspect;
                        break;
                    }
                    if (level == 2) {
                        ms2 = block;
                        declared = count;
                    }
                    i = start + count;
                    continue;
                }
                if (PeakBlockCheck.TryParsePair(line, out _, out _)) {
                    // A peak line outside any block: the declared count does not cover the record's peaks.
                    record.PeaksSuspect = DiagnosticInputInfo.PeakCountMismatch;
                    break;
                }
                Header(record, line);
                i++;
            }
            // Header lines after a refused block are still read, so the strata of a skipped record are reported.
            for (i++; record.PeaksSuspect != null && i < lines.Count; i++) {
                if (!PeakBlockCheck.IsMarker(lines[i])) {
                    Header(record, lines[i]);
                }
            }
            if (record.PeaksSuspect == null && ms2 != null) {
                record.DeclaredPeakCount = declared;
                // The reader's stable m/z order (OrderBy), on the rounded values.
                record.Peaks = ms2.OrderBy(p => p.Mz).ToList();
            }
            return record;
        }

        private static List<Peak> Block(IReadOnlyList<string> lines, int start, int count, out string suspect) {
            suspect = null;
            var block = new List<Peak>(Math.Min(count, 4096));
            for (var k = 0; k < count; k++) {
                if (start + k >= lines.Count) {
                    // The record ends before the declared count.
                    suspect = DiagnosticInputInfo.PeakCountMismatch;
                    return null;
                }
                if (!PeakBlockCheck.TryParsePair(lines[start + k], out var mz, out var intensity)) {
                    suspect = DiagnosticInputInfo.PeakBlockDisagrees;
                    return null;
                }
                block.Add(new Peak { Mz = mz, Intensity = intensity });
            }
            return block;
        }

        private static void Header(DiagrulesRecord record, string line) {
            var colon = line.IndexOf(':');
            if (colon < 0) {
                return;
            }
            var key = AsciiLower(TrimSpaceTab(line.Substring(0, colon)));
            var value = TrimSpaceTab(line.Substring(colon + 1));
            switch (key) {
                case PrecursorMzKey:
                    record.PrecursorMz = PrecursorMzOf(value);
                    break;
                case PrecursorTypeKey:
                    record.PrecursorType = value;
                    break;
                case IonModeKey:
                    record.IonModeText = value;
                    break;
                case InstrumentTypeKey:
                    record.InstrumentType = value;
                    break;
                case CollisionEnergyKey:
                    record.CollisionEnergy = value;
                    break;
                case SpectrumTypeKey:
                    record.SpectrumTypeText = value;
                    break;
            }
        }

        /// <summary>Invariant culture; 0 when unparseable, not finite or not positive (MS-FINDER's reader also sets a
        /// non-positive precursor to 0), so the engine reports no_precursor.</summary>
        public static double PrecursorMzOf(string value) {
            if (double.TryParse(value, NumberStyles.Float, CultureInfo.InvariantCulture, out var mz) && !double.IsNaN(mz) && !double.IsInfinity(mz) && mz > 0.0) {
                return mz;
            }
            return 0.0;
        }

        private static bool IsBlank(string line) {
            foreach (var c in line) {
                if (c != ' ' && c != '\t') {
                    return false;
                }
            }
            return true;
        }

        internal static string TrimSpaceTab(string s) {
            var start = 0;
            var end = s.Length;
            while (start < end && (s[start] == ' ' || s[start] == '\t')) {
                start++;
            }
            while (end > start && (s[end - 1] == ' ' || s[end - 1] == '\t')) {
                end--;
            }
            return s.Substring(start, end - start);
        }

        internal static string AsciiLower(string s) {
            var sb = new StringBuilder(s.Length);
            foreach (var c in s) {
                sb.Append(c >= 'A' && c <= 'Z' ? (char)(c + ('a' - 'A')) : c);
            }
            return sb.ToString();
        }
    }
}
