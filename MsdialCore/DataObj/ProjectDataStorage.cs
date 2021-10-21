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
        public ProjectDataStorage(ParameterBase projectParameter, List<IMsdialDataStorage<ParameterBase>> storages) {
            ProjectParameter = projectParameter;
            InnerStorages = storages;
            Storages = InnerStorages.AsReadOnly();
            InnerProjectPaths = Storages.Select(storage => storage.Parameter.ProjectParam.ProjectFilePath).ToList();
            ProjectPaths = InnerProjectPaths.AsReadOnly();
        }

        public ProjectDataStorage(ParameterBase projectParameter) : this(projectParameter, new List<IMsdialDataStorage<ParameterBase>>()) {

        }
        
        // MessagePack for C# use this constructor.
        [SerializationConstructor]
        public ProjectDataStorage(ParameterBase projectParameter, ReadOnlyCollection<string> projectPaths) {
            this.ProjectParameter = projectParameter;
            InnerStorages = new List<IMsdialDataStorage<ParameterBase>>();
            Storages = InnerStorages.AsReadOnly();
            InnerProjectPaths = projectPaths.ToList();
            this.ProjectPaths = InnerProjectPaths.AsReadOnly();
        }

        [Key(nameof(ProjectParameter))]
        public ParameterBase ProjectParameter { get; }

        [Key(nameof(ProjectPaths))]
        public ReadOnlyCollection<string> ProjectPaths { get; }

        [IgnoreMember]
        private List<string> InnerProjectPaths { get; }

        [IgnoreMember]
        public ReadOnlyCollection<IMsdialDataStorage<ParameterBase>> Storages { get; }

        [IgnoreMember]
        private List<IMsdialDataStorage<ParameterBase>> InnerStorages { get; }

        private static readonly string SerializationKey = "Project";

        public void AddStorage(IMsdialDataStorage<ParameterBase> storage) {
            InnerStorages.Add(storage);
            InnerProjectPaths.Add(storage.Parameter.ProjectFilePath);
        }

        public async Task Save(IStreamManager streamManager, IMsdialSerializer serializer) {
            using (var projectStream = await streamManager.Create(SerializationKey).ConfigureAwait(false)) {
                MessagePackDefaultHandler.SaveToStream(this, projectStream);
            }

            var tasks = Storages.Select(storage => serializer.SaveAsync(storage, streamManager, Path.GetFileNameWithoutExtension(storage.Parameter.ProjectFilePath), string.Empty));
            await Task.WhenAll(tasks).ConfigureAwait(false);
        }

        public static async Task<ProjectDataStorage> Load(IStreamManager streamManager, IMsdialSerializer serializer) {
            ProjectDataStorage storage;
            using (var projectStream = await streamManager.Get(SerializationKey)) {
                storage = MessagePackDefaultHandler.LoadFromStream<ProjectDataStorage>(projectStream);
            }

            var tasks = storage.ProjectPaths.Select(projectPath => serializer.LoadAsync(streamManager, projectPath, string.Empty));
            var datas = await Task.WhenAll(tasks).ConfigureAwait(false);
            storage.InnerStorages.AddRange(datas);

            return storage;
        }
    }
}
