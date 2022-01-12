using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Export;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialIntegrate.Parser;
using CompMs.MsdialLcMsApi.Algorithm.PostCuration;
using CompMs.MsdialLcMsApi.Export;
using System;
using System.IO;
using System.Linq;

namespace CompMs.App.MsdialConsole.Export
{
    public class ExporterTest
    {
        public void Export(string projectfile, Stream output, PostCurator curator) {
            var storage = LoadProjectFromPath(projectfile);
            var alignmentFile = storage.AlignmentFiles.Last();
            var container = MessagePackHandler.LoadFromFile<AlignmentResultContainer>(alignmentFile.FilePath);
            var decResults = MsdecResultsReader.ReadMSDecResults(alignmentFile.SpectraFilePath, out _, out _);

            var curatedSpots = curator?.MsCleanRCurator(container.AlignmentSpotProperties, decResults, storage.AnalysisFiles, storage.Parameter) ?? container.AlignmentSpotProperties;

            var metadataAccessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter);
            var quantAccessor = new LegacyQuantValueAccessor("Normalized height", storage.Parameter);
            var exporter = new AlignmentCSVExporter();

            exporter.Export(output, curatedSpots, decResults, storage.AnalysisFiles, metadataAccessor, quantAccessor, new[] { StatsValue.Average, StatsValue.Stdev });
        }

        private static IMsdialDataStorage<ParameterBase> LoadProjectFromPath(string projectfile) {
            var projectFolder = Path.GetDirectoryName(projectfile);

            var serializer = new MsdialIntegrateSerializer();
            var streamManager = new DirectoryTreeStreamManager(Path.GetDirectoryName(projectfile));
            var storage = serializer.LoadAsync(streamManager, Path.GetFileName(projectfile), Path.GetDirectoryName(projectfile), string.Empty).Result;
            storage.Parameter.ProjectFileName = Path.GetFileName(storage.Parameter.ProjectFileName);

            var previousFolder = storage.Parameter.ProjectFolderPath;
            if (projectFolder == previousFolder)
                return storage;

            storage.Parameter.ProjectFolderPath = projectFolder;

            storage.Parameter.TextDBFilePath = ReplaceFolderPath(storage.Parameter.TextDBFilePath, previousFolder, projectFolder);
            storage.Parameter.IsotopeTextDBFilePath = ReplaceFolderPath(storage.Parameter.IsotopeTextDBFilePath, previousFolder, projectFolder);

            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFilePath = ReplaceFolderPath(file.AnalysisFilePath, previousFolder, projectFolder);
                file.DeconvolutionFilePath = ReplaceFolderPath(file.DeconvolutionFilePath, previousFolder, projectFolder);
                file.PeakAreaBeanInformationFilePath = ReplaceFolderPath(file.PeakAreaBeanInformationFilePath, previousFolder, projectFolder);
                file.RiDictionaryFilePath = ReplaceFolderPath(file.RiDictionaryFilePath, previousFolder, projectFolder);

                file.DeconvolutionFilePathList = file.DeconvolutionFilePathList.Select(decfile => ReplaceFolderPath(decfile, previousFolder, projectFolder)).ToList();
            }

            foreach (var file in storage.AlignmentFiles) {
                file.FilePath = ReplaceFolderPath(file.FilePath, previousFolder, projectFolder);
                file.EicFilePath = ReplaceFolderPath(file.EicFilePath, previousFolder, projectFolder);
                file.SpectraFilePath = ReplaceFolderPath(file.SpectraFilePath, previousFolder, projectFolder);
            }

            return storage;
        }

        private static string ReplaceFolderPath(string path, string previous, string current) {
            if (string.IsNullOrEmpty(path))
                return path;
            if (path.StartsWith(previous))
                return Path.Combine(current, path.Substring(previous.Length).TrimStart('\\', '/'));
            if (!Path.IsPathRooted(path))
                return Path.Combine(current, path);
            throw new ArgumentException("Invalid path or directory.");
        }
    }
}
