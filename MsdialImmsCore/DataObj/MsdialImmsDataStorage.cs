using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.Parameter;
using CompMs.MsdialImmsCore.Parser;
using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialImmsCore.DataObj
{
    [MessagePackObject]
    public class MsdialImmsDataStorage : MsdialDataStorageBase, IMsdialDataStorage<MsdialImmsParameter> {
        [Key(6)]
        public MsdialImmsParameter MsdialImmsParameter { get; set; }

        MsdialImmsParameter IMsdialDataStorage<MsdialImmsParameter>.Parameter => MsdialImmsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public Task SaveParameterAsync(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(MsdialImmsParameter, stream);
            return Task.CompletedTask;
        }

        public MsdialImmsParameter LoadParameter(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<MsdialImmsParameter>(stream);
        }

        public AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage() {
            return DataBases.CreateQueryFactories();
        }

        public static IMsdialSerializer Serializer { get; } = new MsdialImmsSerializer();

        class MsdialImmsSerializer : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialImmsDataStorage>(stream);
                }
            }

            protected override async Task LoadDataBasesAsync(IStreamManager streamManager, string path, DataBaseMapper mapper, IMsdialDataStorage<ParameterBase> storage, string projectFolderPath) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new ImmsLoadAnnotatorVisitor(storage.Parameter), new ImmsAnnotationQueryFactoryGenerationVisitor(storage.Parameter.PeakPickBaseParam, storage.Parameter.RefSpecMatchBaseParam, storage.Parameter.ProteomicsParam, mapper), projectFolderPath);
                }
            }
        }
    }
}
