using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class ProjectDataStorage
    {
        public ProjectDataStorage(ProjectParameter projectParameter, List<IMsdialDataStorage<ParameterBase>> storages) {
            ProjectParameter = projectParameter;
            InnerStorages = storages;
            Storages = InnerStorages.AsReadOnly();
            InnerProjectParameters = Storages.Select(storage => storage.Parameter.ProjectParam).ToList();
            ProjectParameters = InnerProjectParameters.AsReadOnly();
        }

        public ProjectDataStorage(ProjectParameter projectParameter) : this(projectParameter, new List<IMsdialDataStorage<ParameterBase>>()) {

        }
        
        // MessagePack for C# use this constructor.
        [SerializationConstructor]
        public ProjectDataStorage(ReadOnlyCollection<ProjectBaseParameter> projectParameters) {
            InnerStorages = new List<IMsdialDataStorage<ParameterBase>>();
            Storages = InnerStorages.AsReadOnly();
            InnerProjectParameters = projectParameters.ToList();
            this.ProjectParameters = InnerProjectParameters.AsReadOnly();
        }

        [IgnoreMember]
        public ProjectParameter ProjectParameter { get; private set; }

        [Key(nameof(ProjectParameters))]
        public ReadOnlyCollection<ProjectBaseParameter> ProjectParameters { get; }

        [IgnoreMember]
        private List<ProjectBaseParameter> InnerProjectParameters { get; }

        [IgnoreMember]
        public ReadOnlyCollection<IMsdialDataStorage<ParameterBase>> Storages { get; }

        [IgnoreMember]
        private List<IMsdialDataStorage<ParameterBase>> InnerStorages { get; }

        private static readonly string ParameterKey = "Parameter";
        private static readonly string SerializationKey = "Project";

        public void AddStorage(IMsdialDataStorage<ParameterBase> storage) {
            InnerStorages.Add(storage);
            InnerProjectParameters.Add(storage.Parameter.ProjectParam);
        }

        public void FixProjectFolder(string projectDir) {
            ProjectParameter.FixProjectFolder(projectDir);
        }

        public async Task Save(IStreamManager streamManager, IMsdialSerializer serializer, Func<string, IStreamManager?>? datasetStreamManagerFactory, Action<ProjectBaseParameter> faultedHandle) {
            using (var parameterStream = await streamManager.Create(ParameterKey).ConfigureAwait(false)) {
                ProjectParameter.Save(parameterStream);
            }
            using (var projectStream = await streamManager.Create(SerializationKey).ConfigureAwait(false)) {
                MessagePackDefaultHandler.SaveToStream(this, projectStream);
            }

            var tasks = Storages.Select(storage => SaveDataStorage(datasetStreamManagerFactory, storage, serializer, faultedHandle));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        /// <param name="faultedHandle">Called for a dataset that cannot be loaded because a file it reads was not found.</param>
        public static Task<ProjectDataStorage> LoadAsync(IStreamManager streamManager, IMsdialSerializer serializer, Func<string, IStreamManager> datasetStreamManagerFactory, string newProjectDir, Func<ProjectBaseParameter, Task<string?>> setNewPlacement, Action<ProjectBaseParameter> faultedHandle) {
            return LoadReportingFailuresAsync(streamManager, serializer, datasetStreamManagerFactory, newProjectDir, setNewPlacement,
                faultedHandle is null ? null : new Action<ProjectBaseParameter, Exception>((parameter, error) =>
                {
                    if (error is FileNotFoundException) {
                        faultedHandle(parameter);
                    }
                }));
        }

        /// <summary>
        /// Loads the project as <see cref="LoadAsync"/> does, and says why each dataset that cannot be loaded failed.
        /// A dataset that fails is left out of the result, so a caller that needs every dataset can tell from these reports.
        /// </summary>
        /// <param name="datasetLoadFailed">
        /// Called for each dataset that cannot be loaded, with the exception that stopped it.
        /// When a file it reads was not found, this is the <see cref="FileNotFoundException"/> whose <see cref="FileNotFoundException.FileName"/> names that file;
        /// when the project was moved and the dataset fails in both folders, the file named is the one missing in the new folder.
        /// It is called again if a dataset file picked through <paramref name="setNewPlacement"/> fails too.
        /// </param>
        /// <remarks>
        /// <paramref name="setNewPlacement"/> is asked only when the missing file is the dataset file itself:
        /// a dataset file picked elsewhere cannot supply a file that is missing beside it.
        /// </remarks>
        public static async Task<ProjectDataStorage> LoadReportingFailuresAsync(IStreamManager streamManager, IMsdialSerializer serializer, Func<string, IStreamManager> datasetStreamManagerFactory, string newProjectDir, Func<ProjectBaseParameter, Task<string?>> setNewPlacement, Action<ProjectBaseParameter, Exception>? datasetLoadFailed) {
            ProjectDataStorage storage;
            using (var projectStream = await streamManager.Get(SerializationKey).ConfigureAwait(false)) {
                storage = MessagePackDefaultHandler.LoadFromStream<ProjectDataStorage>(projectStream);
            }
            using (var parameterStream = await streamManager.Get(ParameterKey).ConfigureAwait(false)) {
                storage.ProjectParameter = ProjectParameter.Load(parameterStream);
            }

            var tasks = storage.ProjectParameters.Select(projectParameter => LoadDataStorage(datasetStreamManagerFactory, projectParameter, serializer, newProjectDir, setNewPlacement, datasetLoadFailed)).ToArray();
            try {
                var datas = await Task.WhenAll(tasks).ConfigureAwait(false);
                var distinctDatas = datas.GroupBy(data => data.Parameter.ProjectParam.ProjectFilePath).Select(ds => ds.First());
                storage.InnerStorages.AddRange(distinctDatas);
            }
            catch (Exception ex) {
                Debug.WriteLine(ex.ToString());
                storage.InnerStorages.AddRange(tasks.Where(t => t.Status == TaskStatus.RanToCompletion).Select(t => t.Result).Where(s => !(s is null)));
                storage.InnerProjectParameters.Clear();
                storage.InnerProjectParameters.AddRange(storage.Storages.Select(s => s.Parameter.ProjectParam));
            }

            return storage;
        }

        private async static Task SaveDataStorage(
            Func<string, IStreamManager?>? datasetStreamManagerFactory,
            IMsdialDataStorage<ParameterBase> storage,
            IMsdialSerializer serializer,
            Action<ProjectBaseParameter> faultedHandle) {

            var dir = storage.Parameter.ProjectFolderPath;
            var file = storage.Parameter.ProjectFileName;
            try {
                using var streamManager = datasetStreamManagerFactory?.Invoke(dir);
                await serializer.SaveAsync(storage, streamManager, file, dir);
                streamManager?.Complete();
            }
            catch (Exception ex) {
                Debug.WriteLine(ex);
                faultedHandle?.Invoke(storage.Parameter.ProjectParam);
            }
        }

        private static async Task<IMsdialDataStorage<ParameterBase>> LoadDataStorage(
            Func<string, IStreamManager> datasetStreamManagerFactory,
            ProjectBaseParameter projectParameter,
            IMsdialSerializer serializer,
            string newProjectDir,
            Func<ProjectBaseParameter, Task<string?>> setNewPlacement,
            Action<ProjectBaseParameter, Exception>? datasetLoadFailed) {

            var projectDir = projectParameter.ProjectFolderPath;
            var projectFile = projectParameter.ProjectFileName;

            FileNotFoundException? missingInNewFolder = null;
            if (projectDir != newProjectDir) {
                try {
                    using var streamManager = datasetStreamManagerFactory?.Invoke(newProjectDir);
                    var storage = await LoadDataStorageCore(streamManager, serializer, newProjectDir, projectFile);
                    streamManager?.Complete();
                    storage.FixDatasetFolder(newProjectDir);
                    KeepPreviousRtCorrectionResult(storage);
                    return storage;
                }
                catch (FileNotFoundException ex) {
                    // The project file is in this folder, so this is the file to name if the stored folder fails too.
                    missingInNewFolder = ex;
                }
                catch {

                }
            }

            try {
                using var streamManager = datasetStreamManagerFactory?.Invoke(projectDir);
                var storage = await LoadDataStorageCore(streamManager, serializer, projectDir, projectFile);
                streamManager?.Complete();
                return storage;
            }
            catch (FileNotFoundException ex) {
                var reported = missingInNewFolder ?? ex;
                var reportedDir = missingInNewFolder is null ? projectDir : newProjectDir;
                datasetLoadFailed?.Invoke(projectParameter, reported);
                if (setNewPlacement is null || !IsDatasetFile(reported, reportedDir, projectFile)) {
                    // A sibling of the dataset file is missing (its loaded library, for example): picking another dataset file cannot supply it.
                    throw;
                }
                var path = await setNewPlacement.Invoke(projectParameter);
                if (path is null) {
                    throw;
                }
                projectDir = Path.GetDirectoryName(path);
                projectFile = Path.GetFileName(path);
            }
            catch (Exception ex) {
                // A corrupt or unreadable dataset is dropped from the project as well; it is reported so that this is not silent.
                datasetLoadFailed?.Invoke(projectParameter, ex);
                throw;
            }

            try {
                using var streamManager = datasetStreamManagerFactory(projectDir);
                var storage = await LoadDataStorageCore(streamManager, serializer, projectDir, projectFile);
                streamManager?.Complete();
                storage.FixDatasetFolder(projectDir);
                KeepPreviousRtCorrectionResult(storage);
                return storage;
            }
            catch (Exception ex) {
                datasetLoadFailed?.Invoke(projectParameter, ex);
                throw;
            }
        }

        private static bool IsDatasetFile(FileNotFoundException error, string folder, string file) {
            try {
                return string.Equals(
                    Path.GetFullPath(error.FileName ?? error.Message),
                    Path.GetFullPath(Path.Combine(folder, file)),
                    StringComparison.OrdinalIgnoreCase);
            }
            catch (Exception ex) when (ex is ArgumentException || ex is NotSupportedException || ex is PathTooLongException) {
                return false;
            }
        }

        public static void KeepPreviousRtCorrectionResult(IMsdialDataStorage<ParameterBase> storage) {
            if (storage.AnalysisFiles is null) {
                return;
            }
            foreach (var file in storage.AnalysisFiles) {
                var rtbean = file.RetentionTimeCorrectionBean;
                if (rtbean.IsLoaded) {
                    RetentionTimeCorrectionMethod.SaveRetentionCorrectionResult(rtbean.RetentionTimeCorrectionResultFilePath, rtbean.OriginalRt, rtbean.RtDiff, rtbean.PredictedRt);
                }
            }
        }
        private static Task<IMsdialDataStorage<ParameterBase>> LoadDataStorageCore(IStreamManager manager, IMsdialSerializer serializer, string projectFolderPath, string projectFileName) {
            return serializer.LoadAsync(manager, projectFileName, projectFolderPath, string.Empty);
        }
    }
}
