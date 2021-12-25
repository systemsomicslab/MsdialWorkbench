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

        protected override void SaveDataBaseMapper(Stream stream) {

        }

        public static IMsdialSerializer Serializer { get; } = new MsdialDimsSerializer();

        class MsdialDimsSerializer : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialDimsDataStorage>(stream);
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
                    foreach (var db in storage.DataBases.ProteomicsDataBases) {
                        foreach (var pair in db.Pairs) {
                            mapper.Add(pair.SerializableAnnotator, db.DataBase);
                        }
                    }
                }
                storage.DataBaseMapper = mapper;
                return Task.CompletedTask;
            }

            protected override async Task LoadDataBasesAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage, string projectFolderPath) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new DimsLoadAnnotatorVisitor(storage.Parameter), projectFolderPath);
                }
            }
        }
    }
}
