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

        public static async Task<ProjectDataStorage> LoadAsync(IStreamManager streamManager, IMsdialSerializer serializer, Func<string, IStreamManager> datasetStreamManagerFactory, string newProjectDir, Func<ProjectBaseParameter, Task<string?>> setNewPlacement, Action<ProjectBaseParameter> faultedHandle) {
            ProjectDataStorage storage;
            using (var projectStream = await streamManager.Get(SerializationKey).ConfigureAwait(false)) {
                storage = MessagePackDefaultHandler.LoadFromStream<ProjectDataStorage>(projectStream);
            }
            using (var parameterStream = await streamManager.Get(ParameterKey).ConfigureAwait(false)) {
                storage.ProjectParameter = ProjectParameter.Load(parameterStream);
            }

            var tasks = storage.ProjectParameters.Select(projectParameter => LoadDataStorage(datasetStreamManagerFactory, projectParameter, serializer, newProjectDir, setNewPlacement, faultedHandle)).ToArray();
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
            Action<ProjectBaseParameter> faultedHandle) {

            var projectDir = projectParameter.ProjectFolderPath;
            var projectFile = projectParameter.ProjectFileName;

            if (projectDir != newProjectDir) {
                try {
                    using (var streamManager = datasetStreamManagerFactory?.Invoke(projectDir)) {
                        var storage = await LoadDataStorageCore(streamManager, serializer, projectDir, projectFile);
                        streamManager?.Complete();
                        return storage;
                    }
                }
                catch {

                }
            }

            try {
                using (var streamManager = datasetStreamManagerFactory?.Invoke(newProjectDir)) {
                    var storage = await LoadDataStorageCore(streamManager, serializer, newProjectDir, projectFile);
                    streamManager?.Complete();
                    storage.FixDatasetFolder(newProjectDir);
                    KeepPreviousRtCorrectionResult(storage);
                    return storage;
                }
            }
            catch (FileNotFoundException) {
                faultedHandle?.Invoke(projectParameter);
                if (setNewPlacement is null) {
                    throw;
                }
                var path = await setNewPlacement.Invoke(projectParameter);
                if (path is not null) {
                    projectDir = Path.GetDirectoryName(path);
                    projectFile = Path.GetFileName(path);
                }
                else {
                    faultedHandle?.Invoke(projectParameter);
                    throw;
                }
            }

            try {
                using (var streamManager = datasetStreamManagerFactory(projectDir)) {
                    var storage = await LoadDataStorageCore(streamManager, serializer, projectDir, projectFile);
                    streamManager?.Complete();
                    storage.FixDatasetFolder(projectDir);
                    KeepPreviousRtCorrectionResult(storage);
                    return storage;
                }
            }
            catch (FileNotFoundException) {
                faultedHandle?.Invoke(projectParameter);
                throw;
            }
        }

        public static void KeepPreviousRtCorrectionResult(IMsdialDataStorage<ParameterBase> storage) {
            if (storage.AnalysisFiles is null) {
                return;
            }
            foreach (var file in storage.AnalysisFiles) {
                var rtbean = file.RetentionTimeCorrectionBean;
                if (!rtbean.OriginalRt.IsEmptyOrNull()) {
                    RetentionTimeCorrectionMethod.SaveRetentionCorrectionResult(rtbean.RetentionTimeCorrectionResultFilePath, rtbean.OriginalRt, rtbean.RtDiff, rtbean.PredictedRt);
                }
            }
        }
        private static Task<IMsdialDataStorage<ParameterBase>> LoadDataStorageCore(IStreamManager manager, IMsdialSerializer serializer, string projectFolderPath, string projectFileName) {
            return serializer.LoadAsync(manager, projectFileName, projectFolderPath, string.Empty);
        }
    }
}
