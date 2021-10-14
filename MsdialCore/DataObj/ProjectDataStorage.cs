using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    [MessagePackObject]
    public class ProjectDataStorage
    {
        public ProjectDataStorage(ParameterBase projectParameter) : this(projectParameter, new List<IMsdialDataStorage<ParameterBase>>()) {

        }

        public ProjectDataStorage(ParameterBase projectParameter, List<IMsdialDataStorage<ParameterBase>> storages) {
            ProjectParameter = projectParameter;
            Storages = storages;
        }

        [Key(nameof(ProjectParameter))]
        public ParameterBase ProjectParameter { get; }

        [IgnoreMember]
        public List<IMsdialDataStorage<ParameterBase>> Storages { get; private set; }

        private static readonly string SerializationKey = "Project";

        public async Task Save(IStreamManager streamManager) {
            using (var projectStream = await streamManager.Create(SerializationKey)) {
                MessagePackDefaultHandler.SaveToStream(this, projectStream);
            }

            // TODO: save each measurement here.
        }

        public static async Task<ProjectDataStorage> Load(IStreamManager streamManager) {
            ProjectDataStorage storage;
            using (var projectStream = await streamManager.Get(SerializationKey)) {
                storage = MessagePackDefaultHandler.LoadFromStream<ProjectDataStorage>(projectStream);
            }

            // TODO: load each measurement here.
            return storage;
        }
    }
}
