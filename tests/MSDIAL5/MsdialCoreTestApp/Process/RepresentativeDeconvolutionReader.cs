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
/// energy and lists them in DeconvolutionFilePathList, in ascending energy. The spectrum is taken
/// from the energy the representative annotation was made at, and otherwise from the first file
/// of that list.
/// When the list is not empty, an unsuffixed file beside it is not read: a multi-energy run never
/// writes one, so it is left over from an earlier run on the same raw folder (the Console's
/// timestamp has minute resolution and no zero padding, so names can repeat), and its seek
/// pointers do not belong to this run's peaks. The per-file .mdpeak/.mdmsp export follows the same
/// rule through <see cref="OpenPerFileLoader"/>.
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
    /// Opens the deconvolution results the per-file export reads: the first listed collision-energy
    /// file (the lowest energy) when the list is not empty, and DeconvolutionFilePath otherwise.
    /// MSDecLoader's own constructor opens DeconvolutionFilePath whenever it exists, so it would read
    /// the stale unsuffixed file the constructor above ignores.
    /// </summary>
    public static MSDecLoader OpenPerFileLoader(AnalysisFileBean file) {
        var path = HasCollisionEnergyFiles(file) ? file.DeconvolutionFilePathList[0] : file.DeconvolutionFilePath;
        if (!File.Exists(path)) {
            throw new FileNotFoundException($"No deconvolution result for {file.AnalysisFileName}: {path} does not exist.", path);
        }
        return new MSDecLoader(File.Open(path, FileMode.Open, FileAccess.Read, FileShare.Read));
    }

    private static bool HasCollisionEnergyFiles(AnalysisFileBean file) => file.DeconvolutionFilePathList is { Count: > 0 };

    public MSDecResult Read(AlignmentChromPeakFeature peak) {
        var sources = _sources[peak.FileID];
        var energy = peak.MatchResults?.Representative?.CollisionEnergy;
        var source = (energy is double ce ? sources.FirstOrDefault(s => s.CollisionEnergy >= 0 && Math.Abs(s.CollisionEnergy - ce) < 0.005) : null)
            ?? sources[0];
        return MsdecResultsReader.ReadMSDecResult(source.Stream, source.Pointers[peak.MasterPeakID], source.Version, source.IsAnnotationInfo);
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
