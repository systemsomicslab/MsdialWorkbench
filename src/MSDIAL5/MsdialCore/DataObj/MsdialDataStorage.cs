using CompMs.Common.Components;
using CompMs.Common.DataObj.Database;
using CompMs.Common.Extension;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.Enum;
using CompMs.MsdialCore.Parameter;
using CompMs.MsdialCore.Parser;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;

namespace CompMs.MsdialCore.DataObj
{
    public interface IMsdialDataStorage<out T> where T : ParameterBase {
        List<AnalysisFileBean> AnalysisFiles { get; set; }
        List<AlignmentFileBean> AlignmentFiles { get; set; }
        List<MoleculeMsReference> MspDB { get; set; }
        List<MoleculeMsReference> TextDB { get; set; }
        List<MoleculeMsReference> IsotopeTextDB { get; set; }
        IupacDatabase IupacDatabase { get; set; }
        T Parameter { get; }
        DataBaseMapper DataBaseMapper { get; set; }
        DataBaseStorage DataBases { get; set; }

        Task SaveAsync(IStreamManager streamManager, string projectTitle, string prefix);
        Task SaveParameterAsync(Stream stream);
        T LoadParameter(Stream stream);

        void FixDatasetFolder(string projectFolder);
        AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage();
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

        public async Task SaveAsync(IStreamManager streamManager, string projectTitle, string prefix = "") {
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, MsdialSerializer.GetNewMspFileName(projectTitle))).ConfigureAwait(false)) {
                SaveMspDB(stream);
            }
            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, MsdialSerializer.GetDataBasesFileName(projectTitle))).ConfigureAwait(false)) {
                await Task.Run(() => SaveDataBases(stream)).ConfigureAwait(false);
            }

            using (var stream = await streamManager.Create(MsdialSerializer.Combine(prefix, projectTitle)).ConfigureAwait(false)) {
                var mspList = MspDB;
                MspDB = new List<MoleculeMsReference>();
                foreach (var file in AnalysisFiles) file.RetentionTimeCorrectionBean.ClearCache();
                SaveMsdialDataStorageCore(stream);
                MspDB = mspList;
            }
        }

        protected virtual void SaveMspDB(Stream stream) {
            MoleculeMsRefMethods.SaveMspToStream(MspDB, stream);
        }

        protected virtual void SaveDataBases(Stream stream) {
            DataBases?.Save(stream);
        }

        protected virtual void SaveMsdialDataStorageCore(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(this, stream);
        }

        public void FixDatasetFolder(string projectFolder) {
            var storage = this as IMsdialDataStorage<ParameterBase>;

            if (storage is null) return;
            foreach (var file in storage.AnalysisFiles) {
                if (file.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath.IsEmptyOrNull()) {
                    var filedir = Path.GetDirectoryName(file.DeconvolutionFilePath);
                    var filename = Path.GetFileNameWithoutExtension(file.DeconvolutionFilePath);
                    var filepath = Path.Combine(filedir, filename + "." + MsdialDataStorageFormat.rtc);
                    file.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath = filepath;
                }
            }

            if (storage.Parameter.ProjectFolderPath == projectFolder) {
                return;
            }

            var previousFolder = storage.Parameter.ProjectFolderPath;
            storage.Parameter.ProjectFileName = Path.GetFileName(storage.Parameter.ProjectFileName);
            storage.Parameter.ProjectFolderPath = projectFolder;
            storage.Parameter.TextDBFilePath = ReplaceFolderPath(storage.Parameter.TextDBFilePath, previousFolder, projectFolder);
            storage.Parameter.IsotopeTextDBFilePath = ReplaceFolderPath(storage.Parameter.IsotopeTextDBFilePath, previousFolder, projectFolder);

            foreach (var info in storage.Parameter.RefSpecMatchBaseParam.FileIdRiInfoDictionary.Values) {
                try {
                    var newpath = ReplaceFolderPath(info.DictionaryFilePath, previousFolder, projectFolder);
                    if (File.Exists(newpath) || !File.Exists(info.DictionaryFilePath)) {
                        info.DictionaryFilePath = newpath;
                    }
                }
                catch (ArgumentException) {
                    // Do nothing
                }
            }

            foreach (var file in storage.AnalysisFiles) {
                file.AnalysisFilePath = ReplaceFolderPath(file.AnalysisFilePath, previousFolder, projectFolder);
                file.DeconvolutionFilePath = ReplaceFolderPath(file.DeconvolutionFilePath, previousFolder, projectFolder);
                file.PeakAreaBeanInformationFilePath = ReplaceFolderPath(file.PeakAreaBeanInformationFilePath, previousFolder, projectFolder);
                file.ProteinAssembledResultFilePath = ReplaceFolderPath(file.ProteinAssembledResultFilePath, previousFolder, projectFolder);
                file.RiDictionaryFilePath = ReplaceFolderPath(file.RiDictionaryFilePath, previousFolder, projectFolder);
                file.DeconvolutionFilePathList = file.DeconvolutionFilePathList.Select(decfile => ReplaceFolderPath(decfile, previousFolder, projectFolder)).ToList();
                file.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath = ReplaceFolderPath(file.RetentionTimeCorrectionBean.RetentionTimeCorrectionResultFilePath, previousFolder, projectFolder); ;
            }

            foreach (var file in storage.AlignmentFiles) {
                file.FilePath = ReplaceFolderPath(file.FilePath, previousFolder, projectFolder);
                file.EicFilePath = ReplaceFolderPath(file.EicFilePath, previousFolder, projectFolder);
                file.SpectraFilePath = ReplaceFolderPath(file.SpectraFilePath, previousFolder, projectFolder);
                file.ProteinAssembledResultFilePath = ReplaceFolderPath(file.ProteinAssembledResultFilePath, previousFolder, projectFolder);
            }
        }

       

        private static string ReplaceFolderPath(string path, string oldFolder, string newFolder) {
            if (string.IsNullOrEmpty(path))
                return path;
            if (path.StartsWith(oldFolder))
                return Path.Combine(newFolder, path.Substring(oldFolder.Length).TrimStart('\\', '/'));
            if (!Path.IsPathRooted(path))
                return Path.Combine(newFolder, path);
            throw new ArgumentException("Invalid path or directory.");
        }

        protected class MsdialSerializer : IMsdialSerializer {
            public virtual Task SaveAsync(IMsdialDataStorage<ParameterBase> dataStorage, IStreamManager streamManager, string projectTitle, string prefix) {
                return dataStorage.SaveAsync(streamManager, projectTitle, prefix);
            }

            public virtual async Task<IMsdialDataStorage<ParameterBase>> LoadAsync(IStreamManager streamManager, string projectTitle, string projectFolderPath, string prefix = "") {
                var storage = await LoadMsdialDataStorageCoreAsync(streamManager, Combine(prefix, projectTitle)).ConfigureAwait(false);
                var mapper =  new DataBaseMapper();
                await LoadDataBasesAsync(streamManager, Combine(prefix, GetDataBasesFileName(projectTitle)), mapper, storage, projectFolderPath).ConfigureAwait(false);
                LoadDataBaseMapper(mapper, storage);
                storage.DataBaseMapper = mapper;
                storage.MspDB = await LoadMspDBAsync(streamManager, Combine(prefix, GetNewMspFileName(projectTitle))).ConfigureAwait(false);
                foreach (var file in storage.AnalysisFiles) {
                    if (file.AcquisitionType == Common.Enum.AcquisitionType.None) {
#pragma warning disable CS0618 // Type or member is obsolete
                        // ProjectBaseParameter.AcquisitionType is obsolete, but is used to load previous projects.
                        file.AcquisitionType = storage.Parameter.ProjectParam.AcquisitionType;
#pragma warning restore CS0618 // Type or member is obsolete
                    }
                }
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

            private void LoadDataBaseMapper(DataBaseMapper mapper, IMsdialDataStorage<ParameterBase> storage) {
                storage.DataBases.SetDataBaseMapper(mapper);
            }

            protected virtual async Task LoadDataBasesAsync(IStreamManager streamManager, string path, DataBaseMapper mapper, IMsdialDataStorage<ParameterBase> storage, string projectFolderPath) {
                using (var stream = await streamManager.Get(path).ConfigureAwait(false)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new StandardLoadAnnotatorVisitor(storage.Parameter), new StandardAnnotationQueryFactoryGenerationVisitor(storage.Parameter.PeakPickBaseParam, storage.Parameter.RefSpecMatchBaseParam), projectFolderPath);
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

        public Task SaveParameterAsync(Stream stream) {
            MessagePackDefaultHandler.SaveToStream(ParameterBase, stream);
            return Task.CompletedTask;
        }

        public ParameterBase LoadParameter(Stream stream) {
            return MessagePackDefaultHandler.LoadFromStream<ParameterBase>(stream);
        }

        public AnnotationQueryFactoryStorage CreateAnnotationQueryFactoryStorage() {
            return DataBases.CreateQueryFactories();
        }
    }
}
