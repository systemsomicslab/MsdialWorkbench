using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.Parameter;
using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialGcMsApi.DataObj
{
    [MessagePackObject]
    public class MsdialGcmsDataStorage : MsdialDataStorageBase, IMsdialDataStorage<MsdialGcmsParameter> {
        [Key(6)]
        public MsdialGcmsParameter MsdialGcmsParameter { get; set; }

        MsdialGcmsParameter IMsdialDataStorage<MsdialGcmsParameter>.Parameter => MsdialGcmsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public Task SaveParameterAsync(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(MsdialGcmsParameter, stream);
            return Task.CompletedTask;
        }

        public MsdialGcmsParameter LoadParameter(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<MsdialGcmsParameter>(stream);
        }

        public AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage() {
            return DataBases.CreateQueryFactories();
        }

        public static IMsdialSerializer Serializer { get; } = new MsdialGcmsSerializer();

        class MsdialGcmsSerializer : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialGcmsDataStorage>(stream);
                }
            }
        }
    }
}
