using CompMs.Common.Algorithm.Function;
using CompMs.Common.Components;
using CompMs.Common.DataObj.Result;
using CompMs.Common.Enum;
using CompMs.Common.Interfaces;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialIntegrate.Parser;
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
            var peaks = MsdialPeakSerializer.LoadChromatogramPeakFeatures(ResolveStoredFile(peakPath)).ToList();
            return new MolecularNetworkingInput<ChromatogramPeakFeature>(peaks, LoadScans(dclPath, peaks.Select(p => p.GetMSDecResultID())), peakPath);
        }

        public static MolecularNetworkingInput<AlignmentSpotProperty> LoadAlignment(string alignmentPath, string dclPath) {
            var container = AlignmentResultContainer.Load(new AlignmentFileBean { FilePath = ResolveStoredFile(alignmentPath) });
            if (container is null) {
                throw new InvalidDataException($"Could not load alignment result: {alignmentPath}");
            }
            var spots = container.AlignmentSpotProperties.ToList();
            // Alignment dcl records are indexed by the global master alignment ID.
            // AlignmentID is only sequential within a dimension and can point past
            // the dcl record range for a filtered alignment result.
            return new MolecularNetworkingInput<AlignmentSpotProperty>(spots, LoadScans(dclPath, spots.Select(p => p.MasterAlignmentID)), alignmentPath);
        }

        // Loaded the way the application (and the normalize command) does: the serializer
        // picks the mode-specific storage type that a raw MessagePack load does not know.
        public static IMsdialDataStorage<ParameterBase> LoadProject(string projectPath) {
            if (!File.Exists(projectPath)) throw new FileNotFoundException("Project file was not found.", projectPath);
            var projectFolder = Path.GetDirectoryName(Path.GetFullPath(projectPath)) ?? ".";
            using (IStreamManager streamManager = new DirectoryTreeStreamManager(projectFolder)) {
                var storage = new MsdialIntegrateSerializer()
                    .LoadAsync(streamManager, Path.GetFileName(projectPath), projectFolder, string.Empty)
                    .GetAwaiter().GetResult();
                streamManager.Complete();
                storage.FixDatasetFolder(projectFolder);
                return storage;
            }
        }

        /// <summary>
        /// The one analysis file to network: the included file named <paramref name="analysisFileName"/>
        /// (by AnalysisFileName or peak-list file name), or the only included file when no name is given.
        /// </summary>
        public static AnalysisFileBean SelectAnalysisFile(IEnumerable<AnalysisFileBean> files, string analysisFileName, string projectPath) {
            var candidates = files
                .Where(file => file.AnalysisFileIncluded && StoredFileExists(file.PeakAreaBeanInformationFilePath))
                .ToList();
            if (!string.IsNullOrWhiteSpace(analysisFileName)) {
                candidates = candidates.Where(file =>
                    string.Equals(file.AnalysisFileName, analysisFileName, StringComparison.OrdinalIgnoreCase)
                    || string.Equals(Path.GetFileNameWithoutExtension(file.PeakAreaBeanInformationFilePath), analysisFileName, StringComparison.OrdinalIgnoreCase))
                    .ToList();
            }
            if (candidates.Count == 0) {
                throw new FileNotFoundException(string.IsNullOrWhiteSpace(analysisFileName)
                    ? "No included analysis peak list was found in the project."
                    : $"No included analysis peak list matched '{analysisFileName}'.", projectPath);
            }
            if (candidates.Count > 1) {
                var names = string.Join(", ", candidates.Select(file => file.AnalysisFileName));
                throw new InvalidOperationException($"Multiple analysis peak lists are available. Specify --analysis-file with one of: {names}");
            }
            return candidates[0];
        }

        // A project names its peak lists and alignment results .pai and .arf, but
        // MessagePackHandler writes them to disk with its version appended (.pai2, .arf2)
        // and reads them back from the unversioned name. Either spelling is accepted here.
        public static string ToProjectName(string path) {
            if (path.EndsWith(".pai2", StringComparison.OrdinalIgnoreCase) || path.EndsWith(".arf2", StringComparison.OrdinalIgnoreCase)) {
                return path.Substring(0, path.Length - 1);
            }
            return path;
        }

        public static bool StoredFileExists(string path) {
            return !string.IsNullOrEmpty(path) && File.Exists(ToProjectName(path) + "2");
        }

        private static string ResolveStoredFile(string path) {
            if (!StoredFileExists(path)) throw new FileNotFoundException("Peak or alignment result file was not found.", ToProjectName(path) + "2");
            return ToProjectName(path);
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
