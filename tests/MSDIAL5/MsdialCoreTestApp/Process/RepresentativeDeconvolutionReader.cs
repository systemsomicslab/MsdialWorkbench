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
/// energy and lists them in DeconvolutionFilePathList. The spectrum is taken from the energy the
/// representative annotation was made at, and otherwise from the first file: the unsuffixed one,
/// then the energies in ascending order.
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
                if (File.Exists(file.DeconvolutionFilePath)) {
                    sources.Add(Open(file.DeconvolutionFilePath, -1d));
                }
                foreach (var path in file.DeconvolutionFilePathList) {
                    sources.Add(Open(path, CollisionEnergyOf(path)));
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
