using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public interface IMsdialDataStorage<out T> where T: ParameterBase
    {
        List<AnalysisFileBean> AnalysisFiles { get; set; }
        List<AlignmentFileBean> AlignmentFiles { get; set; }
        List<MoleculeMsReference> MspDB { get; set; }
        List<MoleculeMsReference> TextDB { get; set; }
        List<MoleculeMsReference> IsotopeTextDB { get; set; }
        IupacDatabase IupacDatabase { get; set; }
        T Parameter { get; }
        DataBaseMapper DataBaseMapper { get; set; }
        DataBaseStorage DataBases { get; set; }

        Task Save(IStreamManager streamManager, string projectTitle, string prefix);
    }

    [MessagePackObject]
    public abstract class MsdialDataStorageBase
    {
        [Key(0)]
        public List<AnalysisFileBean> AnalysisFiles { get; set; } = new List<AnalysisFileBean>();
        [Key(1)]
        public List<AlignmentFileBean> AlignmentFiles { get; set; } = new List<AlignmentFileBean>();
        [Key(2)]
        public List<MoleculeMsReference> MspDB { get; set; } = new List<MoleculeMsReference>();
        [Key(3)]
        public List<MoleculeMsReference> TextDB { get; set; } = new List<MoleculeMsReference>();
        [Key(4)]
        public List<MoleculeMsReference> IsotopeTextDB { get; set; } = new List<MoleculeMsReference>();
        [Key(5)]
        public IupacDatabase IupacDatabase { get; set; }
        [Key(7)]
        public DataBaseMapper DataBaseMapper { get; set; }
        [Key(8)]
        public DataBaseStorage DataBases { get; set; }

        public async Task Save(IStreamManager streamManager, string projectTitle, string prefix = "") {
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, MsdialSerializer.GetNewMspFileName(projectTitle))).ConfigureAwait(false)) {
                SaveMspDB(stream);
            }
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, MsdialSerializer.GetNewZippedDatabaseFileName(projectTitle))).ConfigureAwait(false)) {
                SaveDataBaseMapper(stream);
            }
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, MsdialSerializer.GetDataBasesFileName(projectTitle))).ConfigureAwait(false)) {
                SaveDataBases(stream);
            }
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, projectTitle)).ConfigureAwait(false)) {
                var mspList = MspDB;
                MspDB = new List<MoleculeMsReference>();
                SaveMsdialDataStorageCore(stream);
                MspDB = mspList;
            }

        }

        protected virtual void SaveMspDB(Stream stream) {
            MoleculeMsRefMethods.SaveMspToStream(MspDB, stream);
        }

        protected virtual void SaveDataBaseMapper(Stream stream) {
            DataBaseMapper.Save(stream);
        }

        protected virtual void SaveDataBases(Stream stream) {
            DataBases?.Save(stream);
        }

        protected virtual void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        protected class MsdialSerializer : IMsdialSerializer {
            public virtual Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
                return dataStorage.Save(streamManager, projectTitle, prefix);
            }

            public virtual async Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string prefix = "") {
                var storage = await LoadMsdialDataStorageCoreAsync(streamManager, Combine(prefix, projectTitle)).ConfigureAwait(false);
                await LoadDataBasesAsync(streamManager, Combine(prefix, GetDataBasesFileName(projectTitle)), storage).ConfigureAwait(false);
                await LoadDataBaseMapperAsync(streamManager, Combine(prefix, GetNewZippedDatabaseFileName(projectTitle)), storage).ConfigureAwait(false);
                storage.MspDB = await LoadMspDBAsync(streamManager, Combine(prefix, GetNewMspFileName(projectTitle))).ConfigureAwait(false);
                return storage;
            }

            protected virtual async Task<IMsdialDataStorage<ParameterBase>> LoadMsdialDataStorageCoreAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MessagePackDefaultHandler.LoadFromStream<MsdialDataStorage>(stream);
                }
            }

            protected static async Task<List<MoleculeMsReference>> LoadMspDBAsync(IStreamManager streamManager, string path) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    return MoleculeMsRefMethods.LoadMspFromStream(stream);
                }
            }

            protected virtual async Task LoadDataBaseMapperAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBaseMapper?.Restore(new StandardLoadAnnotatorVisitor(storage.Parameter), stream);
                }
            }

            protected virtual async Task LoadDataBasesAsync(IStreamManager streamManager, string path, IMsdialDataStorage<ParameterBase> storage) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new StandardLoadAnnotatorVisitor(storage.Parameter));
                }
            }

            internal static string GetNewMspFileName(string path) {
                var fileName = Path.GetFileNameWithoutExtension(path);
                var folder = Path.GetDirectoryName(path);
                return Path.Combine(folder, fileName + "_Loaded.msp2");
            }

            internal static string GetNewZippedDatabaseFileName(string path) {
                return GetNewMspFileName(path) + ".zip";
            }

            internal static string GetDataBasesFileName(string path) {
                return GetNewMspFileName(path) + ".dbs";
            }

            internal static string Combine(string path1, string path2) {
                if (string.IsNullOrEmpty(path1)) {
                    return path2;
                }
                if (string.IsNullOrEmpty(path2)) {
                    return path1;
                }
                return Path.Combine(path1, path2);
            }
        }
    }

    [MessagePackObject]
    public class MsdialDataStorage : MsdialDataStorageBase, IMsdialDataStorage<ParameterBase> {
        [Key(6)]
        public ParameterBase ParameterBase { get; set; }

        ParameterBase IMsdialDataStorage<ParameterBase>.Parameter => ParameterBase;

        public static IMsdialSerializer Serializer { get; } = new MsdialSerializer();

        protected override void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }
    }
}
