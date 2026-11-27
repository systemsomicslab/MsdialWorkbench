using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.MsdialCore.Algorithm.MassCalibration;

/// <summary>
/// The record of a run's mass recalibration that a person reads: for each file, the calibrants of its first annotation
/// and its MS1 and MS2 models with their decisions, in <see cref="FolderName"/> under the Console's output folder or the
/// GUI's project folder.
/// </summary>
public static class MassRecalibrationAudit
{
    public const string FolderName = "mass_recalibration";
    public const string ModelsFileName = "mass_recalibration_models.tsv";

    /// <summary>
    /// Removes an earlier run's audit files from <paramref name="parentFolder"/>, so that they cannot be read as this
    /// run's, and returns how many were removed.
    /// </summary>
    /// <exception cref="IOException">One of them is open elsewhere; the ones before it are removed.</exception>
    /// <exception cref="UnauthorizedAccessException">One of them cannot be removed.</exception>
    public static int RemoveEarlier(string parentFolder) {
        var folder = Path.Combine(parentFolder, FolderName);
        if (!Directory.Exists(folder)) {
            return 0;
        }
        var earlier = Directory.GetFiles(folder, "*" + AnnotationMassAccuracyReport.CalibrantsFileSuffix)
            .Concat(Directory.GetFiles(folder, ModelsFileName))
            .ToList();
        foreach (var path in earlier) {
            File.Delete(path);
        }
        return earlier.Count;
    }

    /// <summary>Writes the audit of each file <paramref name="recalibration"/> decided on, and returns the models file's path.</summary>
    public static string Write(string parentFolder, MassRecalibrationProcess recalibration) {
        var folder = Path.Combine(parentFolder, FolderName);
        Directory.CreateDirectory(folder);
        var outcomes = recalibration.Outcomes;
        foreach (var outcome in outcomes) {
            using var writer = new StreamWriter(Path.Combine(folder, AnnotationMassAccuracyReport.CalibrantsFileName(outcome.FileName)));
            AnnotationMassAccuracyReport.WriteCalibrants(writer, outcome.Calibrants);
        }
        var modelsPath = Path.Combine(folder, ModelsFileName);
        using (var writer = new StreamWriter(modelsPath)) {
            MassCalibrationReport.WriteHeader(writer);
            foreach (var outcome in outcomes) {
                MassCalibrationReport.WriteRow(writer, outcome.FileName, AnnotationMassAccuracyReport.Ms1Pairs, outcome.Ms1);
                MassCalibrationReport.WriteRow(writer, outcome.FileName, AnnotationMassAccuracyReport.Ms2Pairs, outcome.Ms2);
            }
        }
        return modelsPath;
    }

    /// <summary>"X of N file(s) recalibrated (MS1 in a, MS2 in b)", of the files processed.</summary>
    public static string Summarize(IReadOnlyCollection<AnalysisFileBean> files, MassRecalibrationProcess recalibration) {
        var outcomes = recalibration.Outcomes;
        var ms1 = outcomes.Count(outcome => outcome.Ms1.IsApplied);
        var ms2 = outcomes.Count(outcome => outcome.Ms2.IsApplied);
        var recalibrated = files.Count(file => file.MassRecalibration is not null);
        return $"{recalibrated} of {files.Count} file(s) recalibrated (MS1 in {ms1}, MS2 in {ms2})";
    }
}
