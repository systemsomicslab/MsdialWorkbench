using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.Collections.ObjectModel;
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
            InnerProjectPaths = Storages.Select(storage => storage.Parameter.ProjectParam.ProjectFileName).ToList();
            ProjectPaths = InnerProjectPaths.AsReadOnly();
        }

        public ProjectDataStorage(ProjectParameter projectParameter) : this(projectParameter, new List<IMsdialDataStorage<ParameterBase>>()) {

        }
        
        // MessagePack for C# use this constructor.
        [SerializationConstructor]
        public ProjectDataStorage(ReadOnlyCollection<string> projectPaths) {
            InnerStorages = new List<IMsdialDataStorage<ParameterBase>>();
            Storages = InnerStorages.AsReadOnly();
            InnerProjectPaths = projectPaths.ToList();
            this.ProjectPaths = InnerProjectPaths.AsReadOnly();
        }

        public ProjectDataStorage(ProjectParameter projectParameter, ReadOnlyCollection<string> projectPaths) {
            this.ProjectParameter = projectParameter;
            InnerStorages = new List<IMsdialDataStorage<ParameterBase>>();
            Storages = InnerStorages.AsReadOnly();
            InnerProjectPaths = projectPaths.ToList();
            this.ProjectPaths = InnerProjectPaths.AsReadOnly();
        }

        [IgnoreMember]
        public ProjectParameter ProjectParameter { get; private set; }

        [Key(nameof(ProjectPaths))]
        public ReadOnlyCollection<string> ProjectPaths { get; }

        [IgnoreMember]
        private List<string> InnerProjectPaths { get; }

        [IgnoreMember]
        public ReadOnlyCollection<IMsdialDataStorage<ParameterBase>> Storages { get; }

        [IgnoreMember]
        private List<IMsdialDataStorage<ParameterBase>> InnerStorages { get; }

        private static readonly string ParameterKey = "Parameter";
        private static readonly string SerializationKey = "Project";

        public void AddStorage(IMsdialDataStorage<ParameterBase> storage) {
            InnerStorages.Add(storage);
            InnerProjectPaths.Add(storage.Parameter.ProjectParam.ProjectFilePath);
        }

        public async Task Save(IStreamManager streamManager, IMsdialSerializer serializer) {
            using (var parameterStream = await streamManager.Create(ParameterKey).ConfigureAwait(false)) {
                ProjectParameter.Save(parameterStream);
            }
            using (var projectStream = await streamManager.Create(SerializationKey).ConfigureAwait(false)) {
                MessagePackDefaultHandler.SaveToStream(this, projectStream);
            }

            var tasks = Storages.Select(storage => serializer.SaveAsync(storage, streamManager, Path.GetFileNameWithoutExtension(storage.Parameter.ProjectFileName), storage.Parameter.ProjectFolderPath));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public static async Task<ProjectDataStorage> Load(IStreamManager streamManager, IMsdialSerializer serializer) {
            ProjectDataStorage storage;
            using (var projectStream = await streamManager.Get(SerializationKey)) {
                storage = MessagePackDefaultHandler.LoadFromStream<ProjectDataStorage>(projectStream);
            }
            using (var parameterStream = await streamManager.Get(ParameterKey)) {
                storage.ProjectParameter = ProjectParameter.Load(parameterStream);
            }

            var tasks = storage.ProjectPaths.Select(projectPath => serializer.LoadAsync(streamManager, projectPath, null, string.Empty));
            var datas = await Task.WhenAll(tasks).ConfigureAwait(false);
            storage.InnerStorages.AddRange(datas);

            return storage;
        }
    }
}
