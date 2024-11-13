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
using System.Threading.Tasks;

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

            var metadataAccessor = new LcmsMetadataAccessor(storage.DataBaseMapper, storage.Parameter, false);
            var quantAccessor = new LegacyQuantValueAccessor("Normalized height", storage.Parameter);
            var exporter = new AlignmentCSVExporter();

            exporter.Export(output, curatedSpots, decResults, storage.AnalysisFiles, new MulticlassFileMetaAccessor(0), metadataAccessor, quantAccessor, new[] { StatsValue.Average, StatsValue.Stdev });
        }

        public async Task<IMsdialDataStorage<ParameterBase>> LoadProjectFromPathAsync(string projectfile) {

            var projectDir = Path.GetDirectoryName(projectfile) ?? Path.Combine(projectfile, "../");
            using (var fs = File.Open(projectfile, FileMode.Open, FileAccess.Read, FileShare.Read))
            using (IStreamManager streamManager = ZipStreamManager.OpenGet(fs)) {
                var deserializer = new MsdialIntegrateSerializer();
                var projectDataStorage = await ProjectDataStorage.LoadAsync(
                    streamManager,
                    deserializer,
                    path => new DirectoryTreeStreamManager(path),
                    projectDir!,
                    parameter =>
                    {
                        string? result = null;
                        //await Application.Current.Dispatcher.InvokeAsync(() => {
                        //    var newofd = new OpenFileDialog {
                        //        Filter = "MTD3 file(.mtd3)|*.mtd3|All(*)|*",
                        //        Title = "Import a project file",
                        //        RestoreDirectory = true
                        //    };
                        //    if (newofd.ShowDialog() == true) {
                        //        result = newofd.FileName;
                        //    }
                        //});
                        return Task.FromResult(result);
                    },
                    _ => { });
                streamManager.Complete();
                
                projectDataStorage.FixProjectFolder(projectDir);
                return projectDataStorage.Storages.FirstOrDefault();
            }





            //var projectFolder = Path.GetDirectoryName(projectfile);
            //var projectFileName = Path.GetFileName(projectfile);
            //var serializer = new MsdialIntegrateSerializer();
            //var streamManager = new DirectoryTreeStreamManager(projectFolder);
            //var storage = await serializer.LoadAsync(streamManager, projectFileName, projectFolder, string.Empty);
            //storage.FixDatasetFolder(projectFolder);
            //return storage;
        }

        public IMsdialDataStorage<ParameterBase> LoadProjectFromPath(string projectfile) {
            var projectFolder = Path.GetDirectoryName(projectfile) ?? Path.GetFullPath(Path.Combine(projectfile, "../"));

            var serializer = new MsdialIntegrateSerializer();
            IMsdialDataStorage<ParameterBase> storage;
            using (var streamManager = new DirectoryTreeStreamManager(projectFolder)) {
                storage = serializer.LoadAsync(streamManager, Path.GetFileName(projectfile), Path.GetDirectoryName(projectfile), string.Empty).Result;
                storage.Parameter.ProjectFileName = Path.GetFileName(storage.Parameter.ProjectFileName);
                ((IStreamManager)streamManager).Complete();
            }
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

        private string ReplaceFolderPath(string path, string previous, string current) {
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
