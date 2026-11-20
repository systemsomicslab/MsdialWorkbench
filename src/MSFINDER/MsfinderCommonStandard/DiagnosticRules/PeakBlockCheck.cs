using Rfx.Riken.OsakaUniv;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text.RegularExpressions;

namespace Riken.Metabolomics.MsfinderCommon.DiagnosticRules
{
    /// <summary>
    /// The strict re-read of an MSP/MAT query's MS2 peak block (design 5.4, guard <c>peak_block_disagrees</c>).
    /// </summary>
    /// <remarks>
    /// The MS-FINDER reader tokenises peak lines character by character, so an unquoted annotation such as
    /// <c>100 50 C7H7O2</c> becomes the peak (7, 7) in place of (100, 50): the count stays right and no peak falls
    /// below 1 Da, and only a second, strict reading shows it. This reader finds the MS2 block as the MS-FINDER
    /// reader does (the lines after a <c>Num Peaks:</c> line, except the block an <c>MSTYPE: MS1</c> line consumes;
    /// the last block wins) and accepts only lines of one pair <c>&lt;num&gt;[SP|HT]+&lt;num&gt;</c>, with
    /// <c>&lt;num&gt;</c> = <c>[0-9]*\.?[0-9]+([eE]\+[0-9]+)?</c>, optionally followed by one double-quoted comment.
    /// Values are parsed with the invariant culture and rounded as <see cref="Peak"/> rounds them
    /// (<c>Math.Round(x, 6)</c>). Header lines are only scanned for the two markers; a marker line not in its plain
    /// form (<c>Num Peaks: &lt;n&gt;</c>, <c>MSTYPE: MS1</c>, <c>MSTYPE: MS2</c>) is a disagreement, because the
    /// block would then be ambiguous. No header value is parsed or kept.
    /// </remarks>
    public static class PeakBlockCheck
    {
        private const string Number = @"[0-9]*\.?[0-9]+(?:[eE]\+[0-9]+)?";

        private static readonly Regex PairLine = new Regex(
            @"^[ \t]*(" + Number + @")[ \t]+(" + Number + @")(?:[ \t]+""[^""]*"")?[ \t]*$", RegexOptions.CultureInvariant);

        private static readonly Regex NumPeaksLine = new Regex(@"^[ \t]*Num Peaks:[ \t]*([0-9]{1,9})[ \t]*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex MsTypeLine = new Regex(@"^[ \t]*MSTYPE: MS([12])[ \t]*$",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        private static readonly Regex AnyMarker = new Regex(@"Num Peaks:|MSTYPE:",
            RegexOptions.IgnoreCase | RegexOptions.CultureInvariant);

        /// <summary>
        /// Whether the strict reading of the file's MS2 peak block equals <paramref name="readerPeaks"/> (the
        /// MS-FINDER reader's list, in its m/z order). An unreadable file, a line outside the grammar or an ambiguous
        /// marker is a disagreement.
        /// </summary>
        public static bool Agrees(string path, List<Peak> readerPeaks) {
            List<double[]> block;
            try {
                block = ReadMs2Block(path);
            }
            catch (Exception) {
                return false;
            }
            if (block == null) {
                return false;
            }
            if (readerPeaks == null) {
                return block.Count == 0;
            }
            if (block.Count != readerPeaks.Count) {
                return false;
            }
            var i = 0;
            foreach (var peak in readerPeaks) {
                if (block[i][0] != peak.Mz || block[i][1] != peak.Intensity) {
                    return false;
                }
                i++;
            }
            return true;
        }

        /// <summary>
        /// The MS2 block as (m/z, intensity) pairs sorted stably by m/z, an empty list when the file has no block, or
        /// null when the file is outside the grammar.
        /// </summary>
        public static List<double[]> ReadMs2Block(string path) {
            var lines = new List<string>(File.ReadLines(path));
            var ms2 = new List<double[]>();
            var i = 0;
            while (i < lines.Count) {
                var line = lines[i];
                if (!AnyMarker.IsMatch(line)) {
                    i++;
                    continue;
                }
                var numPeaks = NumPeaksLine.Match(line);
                if (numPeaks.Success) {
                    var block = Block(lines, i + 1, Count(numPeaks));
                    if (block == null) {
                        return null;
                    }
                    ms2 = block;
                    i += 1 + block.Count;
                    continue;
                }
                var msType = MsTypeLine.Match(line);
                if (!msType.Success || i + 1 >= lines.Count) {
                    return null;
                }
                var next = NumPeaksLine.Match(lines[i + 1]);
                if (!next.Success) {
                    return null;
                }
                var typed = Block(lines, i + 2, Count(next));
                if (typed == null) {
                    return null;
                }
                if (msType.Groups[1].Value == "2") {
                    ms2 = typed;
                }
                i += 2 + typed.Count;
            }
            return SortedByMz(ms2);
        }

        private static int Count(Match m) => int.Parse(m.Groups[1].Value, NumberStyles.None, CultureInfo.InvariantCulture);

        private static List<double[]> Block(List<string> lines, int start, int count) {
            if (start + count > lines.Count) {
                return null;
            }
            var block = new List<double[]>(count);
            for (var k = 0; k < count; k++) {
                var m = PairLine.Match(lines[start + k]);
                if (!m.Success) {
                    return null;
                }
                block.Add(new[] { R6(m.Groups[1].Value), R6(m.Groups[2].Value) });
            }
            return block;
        }

        private static double R6(string text) => Math.Round(double.Parse(text, NumberStyles.Float, CultureInfo.InvariantCulture), 6);

        private static List<double[]> SortedByMz(List<double[]> pairs) {
            // A stable insertion sort, as the reader's OrderBy is stable.
            var sorted = new List<double[]>(pairs.Count);
            foreach (var p in pairs) {
                var at = sorted.Count;
                while (at > 0 && sorted[at - 1][0] > p[0]) {
                    at--;
                }
                sorted.Insert(at, p);
            }
            return sorted;
        }
    }
}
