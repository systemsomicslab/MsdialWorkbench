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

        public AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage() {
            return DataBases.CreateQueryFactories();
        }

        [IgnoreMember]
        public IAnnotationQueryFactoryGenerationVisitor CreateAnnotationQueryFactoryVisitor
            => new DimsAnnotationQueryFactoryGenerationVisitor(MsdialDimsParameter.PeakPickBaseParam, MsdialDimsParameter.RefSpecMatchBaseParam, MsdialDimsParameter.ProteomicsParam, DataBaseMapper);

        MsdialDimsParameter IMsdialDataStorage<MsdialDimsParameter>.Parameter => MsdialDimsParameter;

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public Task SaveParameterAsync(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(MsdialDimsParameter, stream);
            return Task.CompletedTask;
        }

        public MsdialDimsParameter LoadParameter(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<MsdialDimsParameter>(stream);
        }

        public static IMsdialSerializer Serializer { get; } = new MsdialDimsSerializer();

        class MsdialDimsSerializer : MsdialSerializer, IMsdialSerializer
        {
            protected override async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialDimsDataStorage>(stream);
                }
            }

            protected override async Task LoadDataBasesAsync(IStreamManager streamManager, string path, DataBaseMapper mapper, IMsdialDataStorage<ParameterBase> storage, string projectFolderPath) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new DimsLoadAnnotatorVisitor(storage.Parameter), new DimsAnnotationQueryFactoryGenerationVisitor(storage.Parameter.PeakPickBaseParam, storage.Parameter.RefSpecMatchBaseParam, storage.Parameter.ProteomicsParam, mapper), projectFolderPath);
                }
            }
        }
    }
}
