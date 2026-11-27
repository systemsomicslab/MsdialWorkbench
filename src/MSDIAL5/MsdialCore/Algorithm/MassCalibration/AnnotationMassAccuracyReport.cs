using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// Measures, and writes down, how far a file's MS1 and MS2 m/z lie from the theoretical m/z of what they were
/// annotated as: the calibrants of each file, and their error overall and by m/z, retention time, intensity, library
/// and, for the product ions, whether the ion is the unfragmented precursor.
/// </summary>
/// <remarks>
/// It reports and changes nothing: no m/z is corrected, and no annotation is kept.
/// </remarks>
public static class AnnotationMassAccuracyReport
{
    public const string FolderName = "annotation_mass_accuracy";
    public const string SummaryFileName = "annotation_mass_accuracy_summary.tsv";
    public const string CalibrantsFileSuffix = ".annotation_calibrants.tsv";
    public const string Ms1Pairs = "MS1 precursor";
    public const string Ms2Pairs = "MS2 product ion";

    private static readonly string[] CalibrantsHeader =
    {
        "Peak ID", "RT (min)", "MS level", "Observed m/z", "Reference m/z", "Reference m/z from", "Error (ppm)", "Observed intensity",
        "Precursor m/z", "Precursor ion", "Reference", "Library", "Library kind", "Score", "Scan index",
    };

    public static string CalibrantsFileName(string analysisFileName) => analysisFileName + CalibrantsFileSuffix;

    public static void WriteCalibrants(TextWriter writer, IEnumerable<AnnotationCalibrant> calibrants) {
        writer.WriteLine(string.Join("\t", CalibrantsHeader));
        foreach (var calibrant in calibrants) {
            writer.WriteLine(string.Join("\t", new[]
            {
                calibrant.PeakId.ToString(CultureInfo.InvariantCulture),
                Format(calibrant.RetentionTime, "0.0000"),
                calibrant.MsLevel.ToString(CultureInfo.InvariantCulture),
                Format(calibrant.ObservedMz, "0.00000"),
                Format(calibrant.ReferenceMz, "0.00000"),
                calibrant.ReferenceMzSource,
                Format(calibrant.ErrorPpm, "0.00"),
                Format(calibrant.ObservedIntensity, "0"),
                Format(calibrant.PrecursorMz, "0.00000"),
                calibrant.IsPrecursorIon ? "True" : "False",
                Cell(calibrant.ReferenceName),
                Cell(calibrant.AnnotatorId),
                calibrant.DataBaseSource.ToString(),
                Format(calibrant.Score, "0.000"),
                calibrant.ScanIndex.ToString(CultureInfo.InvariantCulture),
            }));
        }
    }

    /// <summary>
    /// The error of one file's calibrants: overall, then by m/z, RT, intensity and library, first for the precursors in
    /// the MS1 survey and then for the product ions, which are also split into the precursor left unfragmented and the
    /// fragments. A group without calibrants is left out.
    /// </summary>
    public static List<MassAccuracyRow> Summarize(string fileName, IReadOnlyList<AnnotationCalibrant> calibrants, double searchPpm = AnnotationCalibrantCollector.DefaultSearchPpm) {
        if (calibrants is null) {
            throw new ArgumentNullException(nameof(calibrants));
        }
        var rows = new List<MassAccuracyRow>();

        var ms1 = new MassAccuracyGroups<AnnotationCalibrant>(rows, fileName, Ms1Pairs, calibrants.Where(c => c.MsLevel == 1).ToList(), c => c.ErrorPpm, searchPpm);
        ms1.Overall();
        ms1.ByMz("m/z", c => c.ReferenceMz);
        ms1.ByRetentionTime(c => c.RetentionTime);
        ms1.ByDecade("MS1 intensity", c => c.ObservedIntensity);
        ms1.By("Library", c => c.AnnotatorId, id => id);

        var ms2 = new MassAccuracyGroups<AnnotationCalibrant>(rows, fileName, Ms2Pairs, calibrants.Where(c => c.MsLevel == 2).ToList(), c => c.ErrorPpm, searchPpm);
        ms2.Overall();
        ms2.ByMz("Product ion m/z", c => c.ReferenceMz);
        ms2.ByRetentionTime(c => c.RetentionTime);
        ms2.ByDecade("MS2 intensity", c => c.ObservedIntensity);
        ms2.By("Library", c => c.AnnotatorId, id => id);
        ms2.By("Ion", c => c.IsPrecursorIon, isPrecursor => isPrecursor ? "Precursor" : "Fragment");
        return rows;
    }

    /// <summary>One line for the console: the overall error of the MS1 precursors and of the MS2 product ions.</summary>
    public static string Describe(IReadOnlyList<MassAccuracyRow> rows) {
        return $"MS1 {Describe(rows, Ms1Pairs, "annotated precursors")}; MS2 {Describe(rows, Ms2Pairs, "product ions of libraries with computed fragment m/z")}.";
    }

    private static string Describe(IReadOnlyList<MassAccuracyRow> rows, string pairs, string what) {
        var overall = rows.FirstOrDefault(row => row.Pairs == pairs && row.Group == "Overall");
        if (overall is null || !overall.Estimate.IsEstimated) {
            return $"no {what}";
        }
        var count = overall.Estimate.PairCount.ToString(CultureInfo.InvariantCulture);
        if (!overall.Estimate.IsReliable) {
            return $"{count} {what}, fewer than {MassErrorEstimate.MinimumSignalPairs.ToString(CultureInfo.InvariantCulture)} of them above the background: no offset";
        }
        return $"{MassAccuracySummary.Describe(overall.Estimate)} from {count} {what}";
    }

    // A library name or a compound name with a tab or a line break in it would shift every later column.
    private static string Cell(string text) {
        return text is null ? string.Empty : text.Replace('\t', ' ').Replace('\r', ' ').Replace('\n', ' ');
    }

    private static string Format(double value, string format) => MassAccuracySummary.Format(value, format);
}
