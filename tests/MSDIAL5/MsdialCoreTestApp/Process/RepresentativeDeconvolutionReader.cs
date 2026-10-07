using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process;

/// <summary>
/// Reads each alignment spot's representative MS/MS from the deconvolution file of its
/// representative peak, as the GUI's AlignmentFileBeanModel.LoadMSDecResultsFromEachFiles does.
/// </summary>
/// <remarks>
/// A file deconvoluted once has its results in DeconvolutionFilePath. An AIF file with several
/// collision energies has none there: FileProcess writes one "&lt;name&gt;_&lt;CE x 100&gt;.dcl" per
/// energy and lists them in DeconvolutionFilePathList, in ascending energy.
/// For such a file the spectrum is taken from the energy the representative annotation was made at,
/// as the GUI does. A peak without an annotation at one of the file's energies takes the energy
/// whose deconvoluted spectrum has the most product ions, the lowest of those energies on a tie
/// (the MS-DIAL author's decision of 2026-10-07; the GUI takes the first file of its list).
/// When the list is not empty, an unsuffixed file beside it is not read: a multi-energy run never
/// writes one, so it is left over from an earlier run on the same raw folder (the Console's
/// timestamp has minute resolution and no zero padding, so names can repeat), and its seek
/// pointers do not belong to this run's peaks. The per-file .mdpeak/.mdmsp export follows the same
/// rules through <see cref="LoadPerFileResults"/>.
/// </remarks>
internal sealed class RepresentativeDeconvolutionReader : IDisposable
{
    private sealed class Source
    {
        public double CollisionEnergy;
        public FileStream Stream = null!;
        public int Version;
        public List<long> Pointers = null!;
        public bool IsAnnotationInfo;

        public MSDecResult Read(int id) => MsdecResultsReader.ReadMSDecResult(Stream, Pointers[id], Version, IsAnnotationInfo);
    }

    private readonly Dictionary<int, List<Source>> _sources = new();

    public RepresentativeDeconvolutionReader(IReadOnlyList<AnalysisFileBean> files) {
        try {
            foreach (var file in files) {
                var sources = new List<Source>();
                _sources[file.AnalysisFileId] = sources;
                if (HasCollisionEnergyFiles(file)) {
                    foreach (var path in file.DeconvolutionFilePathList) {
                        sources.Add(Open(path, CollisionEnergyOf(path)));
                    }
                }
                else if (File.Exists(file.DeconvolutionFilePath)) {
                    sources.Add(Open(file.DeconvolutionFilePath, -1d));
                }
                if (sources.Count == 0) {
                    throw new FileNotFoundException(
                        $"No deconvolution result for {file.AnalysisFileName}: neither {file.DeconvolutionFilePath} nor a collision-energy file exists.",
                        file.DeconvolutionFilePath);
                }
            }
        }
        catch {
            Dispose();
            throw;
        }
    }

    /// <summary>
    /// The deconvolution results the per-file export writes, indexed as the file's .dcl files are.
    /// A multi-energy AIF file takes, for each peak, the energy of its representative annotation, and
    /// otherwise the energy whose spectrum has the most product ions, by the same rule as <see cref="Read"/>.
    /// Any other file is read from DeconvolutionFilePath. MSDecLoader's own constructor opens
    /// DeconvolutionFilePath whenever it exists, so it would read the stale unsuffixed file the
    /// constructor above ignores.
    /// </summary>
    public static IReadOnlyList<MSDecResult> LoadPerFileResults(AnalysisFileBean file, IReadOnlyList<ChromatogramPeakFeature> peaks) {
        if (!HasCollisionEnergyFiles(file)) {
            return ReadAll(file, file.DeconvolutionFilePath);
        }

        var energies = file.DeconvolutionFilePathList.Select(CollisionEnergyOf).ToList();
        var perEnergy = file.DeconvolutionFilePathList.Select(path => ReadAll(file, path)).ToList();
        var count = perEnergy[0].Count;
        if (perEnergy.Any(results => results.Count != count)) {
            throw new InvalidDataException(
                $"The collision-energy files of {file.AnalysisFileName} hold different numbers of peaks: " +
                string.Join(", ", file.DeconvolutionFilePathList.Zip(perEnergy, (path, results) => $"{Path.GetFileName(path)} {results.Count}")) + ".");
        }

        var annotatedEnergies = new Dictionary<int, double>();
        foreach (var peak in peaks) {
            if (peak.MatchResults?.Representative?.CollisionEnergy is double ce) {
                annotatedEnergies[peak.GetMSDecResultID()] = ce;
            }
        }

        var chosen = new MSDecResult[count];
        for (int id = 0; id < count; id++) {
            var candidates = perEnergy.Select(results => results[id]).ToList();
            var index = annotatedEnergies.TryGetValue(id, out var ce) ? IndexOfEnergy(energies, ce) : -1;
            chosen[id] = candidates[index >= 0 ? index : IndexOfMostProductIons(candidates)];
        }
        return chosen;
    }

    private static bool HasCollisionEnergyFiles(AnalysisFileBean file) => file.DeconvolutionFilePathList is { Count: > 0 };

    private static List<MSDecResult> ReadAll(AnalysisFileBean file, string path) {
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"No deconvolution result for {file.AnalysisFileName}: {path} does not exist.", path);
        }
        return MsdecResultsReader.ReadMSDecResults(path, out _, out _);
    }

    public MSDecResult Read(AlignmentChromPeakFeature peak) {
        var sources = _sources[peak.FileID];
        var id = peak.MasterPeakID;
        if (sources.Count == 1) {
            return sources[0].Read(id);
        }
        if (peak.MatchResults?.Representative?.CollisionEnergy is double ce) {
            var index = IndexOfEnergy(sources.Select(s => s.CollisionEnergy).ToList(), ce);
            if (index >= 0) {
                return sources[index].Read(id);
            }
        }
        var candidates = sources.Select(s => s.Read(id)).ToList();
        return candidates[IndexOfMostProductIons(candidates)];
    }

    /// <summary>
    /// The file at the representative annotation's energy, or -1 when no file has it. An unannotated
    /// peak's representative is the unknown result, whose energy no AIF deconvolution is made at.
    /// </summary>
    private static int IndexOfEnergy(IReadOnlyList<double> energies, double ce) {
        for (int i = 0; i < energies.Count; i++) {
            if (energies[i] >= 0 && Math.Abs(energies[i] - ce) < 0.005) {
                return i;
            }
        }
        return -1;
    }

    /// <summary>
    /// The candidate whose deconvoluted spectrum has the most peaks; the first, that is the lowest energy, on a tie.
    /// </summary>
    internal static int IndexOfMostProductIons(IReadOnlyList<MSDecResult> candidates) {
        var best = 0;
        var bestCount = -1;
        for (int i = 0; i < candidates.Count; i++) {
            var count = candidates[i]?.Spectrum?.Count ?? 0;
            if (count > bestCount) {
                best = i;
                bestCount = count;
            }
        }
        return best;
    }

    public void Dispose() {
        foreach (var source in _sources.Values.SelectMany(s => s)) {
            source.Stream?.Dispose();
        }
        _sources.Clear();
    }

    private static Source Open(string path, double collisionEnergy) {
        MsdecResultsReader.GetSeekPointers(path, out var version, out var pointers, out var isAnnotationInfo);
        return new Source {
            CollisionEnergy = collisionEnergy,
            Stream = File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read),
            Version = version,
            Pointers = pointers,
            IsAnnotationInfo = isAnnotationInfo,
        };
    }

    // "<base>_<CE x 100>.dcl", as MSDecResultCollection.GetDeconvolutionFilePathWithCE names it.
    private static double CollisionEnergyOf(string path) {
        var suffix = Path.GetFileNameWithoutExtension(path).Split('_').Last();
        return double.Parse(suffix, CultureInfo.InvariantCulture) / 100d;
    }
}
