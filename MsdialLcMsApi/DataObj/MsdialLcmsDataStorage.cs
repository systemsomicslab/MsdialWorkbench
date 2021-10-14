using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.Parser;
using MessagePack;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialLcMsApi.DataObj
{
    [MessagePackObject]
    public class MsdialLcmsDataStorage : MsdialDataStorageBase, IMsdialDataStorage<MsdialLcmsParameter> {
        [Key(6)]
        public MsdialLcmsParameter MsdialLcmsParameter { get; set; }

        MsdialLcmsParameter IMsdialDataStorage<MsdialLcmsParameter>.Parameter => MsdialLcmsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        protected override void SaveDataBaseMapper(Stream stream) {

        }

        public static IMsdialSerializer Serializer { get; } = new MsdialLcmsSerializer();

        class MsdialLcmsSerializer : MsdialSerializerInner, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialLcmsDataStorage>(stream);
                }
            }

            protected override async Task LoadDataBasesAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new LcmsLoadAnnotatorVisitor(storage.Parameter));
                }
            }

            protected override Task LoadDataBaseMapperAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                var mapper = new DataBaseMapper();
                if (!(storage.DataBases is null)) {
                    foreach (var db in storage.DataBases.MetabolomicsDataBases) {
                        foreach (var pair in db.Pairs) {
                            mapper.Add(pair.SerializableAnnotator, db.DataBase);
                        }
                    }
                }
                storage.DataBaseMapper = mapper;

                return Task.CompletedTask;
            }
        }
    }
}
