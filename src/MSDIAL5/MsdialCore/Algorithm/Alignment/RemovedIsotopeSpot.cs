using CompMs.Common.Components;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Algorithm.Alignment
{
    /// <summary>
    /// An alignment spot that the refinement removed because its representative peak is labelled as an isotope
    /// (M + n) of another peak in the same file and the spot is not reference-matched.
    /// The values are copied when the spot is removed, so later steps cannot change them.
    /// </summary>
    public sealed class RemovedIsotopeSpot
    {
        public RemovedIsotopeSpot(AlignmentSpotProperty spot, AlignmentChromPeakFeature? representative, AlignmentChromPeakFeature? parent) {
            if (spot is null) {
                throw new ArgumentNullException(nameof(spot));
            }
            Mz = spot.MassCenter;
            Time = spot.TimesCenter?.Value ?? double.NaN;
            TimeType = spot.TimesCenter?.MainType ?? ChromXType.RT;
            // The library search skips isotope peaks, so these spots usually have no name.
            Name = string.IsNullOrEmpty(spot.Name) ? "Unknown" : spot.Name;
            HeightAverage = spot.HeightAverage;
            FillPercentage = spot.FillParcentage;
            IsotopeWeightNumber = spot.PeakCharacter.IsotopeWeightNumber;
            IsotopeParentPeakID = spot.PeakCharacter.IsotopeParentPeakID;
            RepresentativeFileID = spot.RepresentativeFileID;
            RepresentativeFileName = representative?.FileName ?? string.Empty;
            RepresentativePeakID = representative?.PeakID;
            RepresentativeHeight = representative?.PeakHeightTop;
            if (parent is not null) {
                ParentMz = parent.Mass;
                ParentTime = parent.ChromXsTop?.Value;
                ParentHeight = parent.PeakHeightTop;
            }
        }

        public double Mz { get; }
        public double Time { get; }
        public ChromXType TimeType { get; }
        public string Name { get; }
        public float HeightAverage { get; }
        public float FillPercentage { get; }
        public int IsotopeWeightNumber { get; }
        /// <summary>Peak ID, in the representative file, of the peak this spot's representative is an isotope of.</summary>
        public int IsotopeParentPeakID { get; }
        public int RepresentativeFileID { get; }
        public string RepresentativeFileName { get; }
        public int? RepresentativePeakID { get; }
        public double? RepresentativeHeight { get; }
        /// <summary>Null when the parent peak is not in any alignment spot.</summary>
        public double? ParentMz { get; }
        public double? ParentTime { get; }
        public double? ParentHeight { get; }

        /// <summary>
        /// Records the removed spots, finding each parent among <paramref name="peaks"/>, the peaks known to the
        /// alignment, by representative file and peak ID.
        /// </summary>
        public static IReadOnlyList<RemovedIsotopeSpot> Collect(
            IEnumerable<AlignmentSpotProperty> removed,
            Func<AlignmentSpotProperty, AlignmentChromPeakFeature?> representativeOf,
            IEnumerable<AlignmentChromPeakFeature> peaks) {
            var removedList = removed.ToList();
            if (removedList.Count == 0) {
                return Array.Empty<RemovedIsotopeSpot>();
            }
            var wanted = new HashSet<(int, int)>(removedList.Select(spot => (spot.RepresentativeFileID, spot.PeakCharacter.IsotopeParentPeakID)));
            var parents = new Dictionary<(int, int), AlignmentChromPeakFeature>();
            foreach (var peak in peaks) {
                if (peak is null) {
                    continue;
                }
                var key = (peak.FileID, peak.PeakID);
                if (wanted.Contains(key) && !parents.ContainsKey(key)) {
                    parents[key] = peak;
                }
            }
            return removedList
                .Select(spot => {
                    parents.TryGetValue((spot.RepresentativeFileID, spot.PeakCharacter.IsotopeParentPeakID), out var parent);
                    return new RemovedIsotopeSpot(spot, representativeOf(spot), parent);
                })
                .OrderBy(spot => spot.Mz)
                .ToList();
        }

        /// <summary>The representative peak among the spot's aligned peaks, or null when they are not held.</summary>
        public static AlignmentChromPeakFeature? RepresentativeAmongAlignedPeaks(AlignmentSpotProperty spot) {
            return spot.AlignedPeakProperties?.FirstOrDefault(peak => peak.FileID == spot.RepresentativeFileID);
        }
    }

    /// <summary>
    /// Writes the alignment spots removed as isotopes as a tab-separated table, one row per spot,
    /// so that a feature missing from the alignment result can be found with its parent ion.
    /// </summary>
    public static class RemovedIsotopeSpotWriter
    {
        public const string FileSuffix = ".mdremovedisotopes.tsv";

        public static string PathFor(string folder, string alignmentFileName) {
            return Path.Combine(folder, alignmentFileName + FileSuffix);
        }

        public static void Write(string path, IReadOnlyList<RemovedIsotopeSpot> spots) {
            using var stream = File.Open(path, FileMode.Create, FileAccess.Write);
            Write(stream, spots);
        }

        public static void Write(Stream stream, IReadOnlyList<RemovedIsotopeSpot> spots) {
            var timeType = spots.Count > 0 ? spots[0].TimeType : ChromXType.RT;
            var (timeHeader, parentTimeHeader) = timeType switch {
                ChromXType.Drift => ("Average drift time", "Parent drift time"),
                ChromXType.RI => ("Average RI", "Parent RI"),
                _ => ("Average Rt(min)", "Parent Rt(min)"),
            };
            using var writer = new StreamWriter(stream, new UTF8Encoding(false), 4096, leaveOpen: true) { NewLine = "\n" };
            writer.WriteLine(string.Join("\t", new[] {
                timeHeader, "Average Mz", "Metabolite name", "Height average", "Fill %", "Isotope",
                "Representative file", "Representative peak ID", "Representative height",
                "Isotope parent peak ID", "Parent m/z", parentTimeHeader, "Parent height", "Height / parent height",
            }));
            foreach (var spot in spots) {
                var ratio = spot.RepresentativeHeight is double height && spot.ParentHeight is double parentHeight && parentHeight > 0
                    ? height / parentHeight
                    : (double?)null;
                writer.WriteLine(string.Join("\t", new[] {
                    Format(spot.Time, "F3"), Format(spot.Mz, "F5"), spot.Name, Format(spot.HeightAverage, "F0"), Format(spot.FillPercentage, "F2"),
                    "M + " + spot.IsotopeWeightNumber.ToString(CultureInfo.InvariantCulture),
                    spot.RepresentativeFileName, Format(spot.RepresentativePeakID), Format(spot.RepresentativeHeight, "F0"),
                    spot.IsotopeParentPeakID.ToString(CultureInfo.InvariantCulture),
                    Format(spot.ParentMz, "F5"), Format(spot.ParentTime, "F3"), Format(spot.ParentHeight, "F0"), Format(ratio, "F2"),
                }));
            }
        }

        private static string Format(double? value, string format) {
            return value is double v && !double.IsNaN(v) ? v.ToString(format, CultureInfo.InvariantCulture) : string.Empty;
        }

        private static string Format(int? value) {
            return value?.ToString(CultureInfo.InvariantCulture) ?? string.Empty;
        }
    }
}
