using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.MSDec;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialCore.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Process.MoleculerNetworking {
    internal sealed class MolecularNetworkingInput<T> where T : IMoleculeProperty, IChromatogramPeak {
        public MolecularNetworkingInput(IReadOnlyList<T> spots, IReadOnlyList<IMSScanProperty> scans, string sourcePath) {
            if (spots.Count != scans.Count) {
                throw new ArgumentException("The number of molecular peaks and MS/MS spectra must match.");
            }
            Spots = spots;
            Scans = scans;
            SourcePath = sourcePath;
        }

        public IReadOnlyList<T> Spots { get; }
        public IReadOnlyList<IMSScanProperty> Scans { get; }
        public string SourcePath { get; }
    }

    internal static class MolecularNetworkingInputLoader {
        public static MolecularNetworkingInput<ChromatogramPeakFeature> LoadAnalysis(string peakPath, string dclPath) {
            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(NormalizeV2Path(peakPath)).ToList();
            return new MolecularNetworkingInput<ChromatogramPeakFeature>(peaks, LoadScans(dclPath, peaks.Select(p => p.GetMSDecResultID())), peakPath);
        }

        public static MolecularNetworkingInput<AlignmentSpotProperty> LoadAlignment(string alignmentPath, string dclPath) {
            var container = AlignmentResultContainer.Load(new AlignmentFileBean { FilePath = NormalizeV2Path(alignmentPath) });
            if (container is null) {
                throw new InvalidDataException($"Could not load alignment result: {alignmentPath}");
            }
            var spots = container.AlignmentSpotProperties.ToList();
            return new MolecularNetworkingInput<AlignmentSpotProperty>(spots, LoadScans(dclPath, spots.Select(p => p.GetMSDecResultID())), alignmentPath);
        }

        public static MsdialDataStorage LoadProject(string projectPath) {
            if (!File.Exists(projectPath)) throw new FileNotFoundException("Project file was not found.", projectPath);
            var storage = MessagePackDefaultHandler.LoadFromFile<MsdialDataStorage>(projectPath);
            storage.FixDatasetFolder(Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? ".");
            return storage;
        }

        public static string NormalizeV2Path(string path) {
            if (path.EndsWith(".pai2", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".arf2", StringComparison.OrdinalIgnoreCase)) {
                var legacyPath = path.Substring(0, path.Length - 1);
                if (File.Exists(legacyPath)) return legacyPath;
            }
            if (!File.Exists(path)) throw new FileNotFoundException("Peak or alignment result file was not found.", path);
            return path;
        }

        private static IReadOnlyList<MSDecResult> LoadScans(string dclPath, IEnumerable<int> ids) {
            if (!File.Exists(dclPath)) throw new FileNotFoundException("Deconvolution spectrum file was not found.", dclPath);
            MsdecResultsReader.GetSeekPointers(dclPath, out _, out var seekPoints, out _);
            using var loader = new MSDecLoader(dclPath, new List<string>());
            var scans = new List<MSDecResult>();
            foreach (var id in ids) {
                if (id < 0 || id >= seekPoints.Count) {
                    throw new InvalidDataException($"MSDec result ID {id} is invalid for dcl file: {dclPath}");
                }
                var scan = loader.LoadMSDecResult(id);
                if (scan is null) {
                    throw new InvalidDataException($"MSDec result ID {id} could not be loaded from dcl file: {dclPath}");
                }
                scans.Add(scan);
            }
            return scans;
        }
    }
}
