using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.Parameter;
using CompMs.MsdialDimsCore.Parser;
using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialDimsCore.DataObj
{
    [MessagePackObject]
    public class MsdialDimsDataStorage : MsdialDataStorageBase, IMsdialDataStorage<MsdialDimsParameter> {
        [Key(6)]
        public MsdialDimsParameter MsdialDimsParameter { get; set; }

        MsdialDimsParameter IMsdialDataStorage<MsdialDimsParameter>.Parameter => MsdialDimsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public static IMsdialSerializer Serializer { get; } = new MsdialDimsSerializer();

        class MsdialDimsSerializer : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialDimsDataStorage>(stream);
                }
            }

            protected async override Task LoadDataBaseMapperAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBaseMapper?.Restore(new DimsLoadAnnotatorVisitor(storage.Parameter), stream);
                }
            }

            protected override async Task LoadDataBasesAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new DimsLoadAnnotatorVisitor(storage.Parameter));
                }
            }
        }
    }
}
