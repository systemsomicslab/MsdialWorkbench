using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.Parameter;
using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialLcImMsApi.DataObj
{
    [MessagePackObject]
    public class MsdialLcImMsDataStorage : MsdialDataStorageBase, IMsdialDataStorage<MsdialLcImMsParameter>
    {
        [Key(6)]
        public MsdialLcImMsParameter MsdialLcImMsParameter { get; set; }

        MsdialLcImMsParameter IMsdialDataStorage<MsdialLcImMsParameter>.Parameter => MsdialLcImMsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static IMsdialSerializer Serializer { get; } = new MsdialLcImMsSerializerInner();

        class MsdialLcImMsSerializerInner : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialLcImMsDataStorage>(stream);
                }
            }
        }
    }
}
