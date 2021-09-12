using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialCore.Parser
{
    public class MsdialSerializer {
        public static void SaveChromatogramPeakFeatures(string file, List<ChromatogramPeakFeature> chromPeakFeatures) {
            MessagePackHandler.SaveToFile<List<ChromatogramPeakFeature>>(chromPeakFeatures, file);
        }

        public static List<ChromatogramPeakFeature> LoadChromatogramPeakFeatures(string file) {
            return MessagePackHandler.LoadFromFile<List<ChromatogramPeakFeature>>(file);
        }

        public virtual void SaveMsdialDataStorage(string file, MsdialDataStorage container) {
            var mspList = container.MspDB;
            container.MspDB = new List<MoleculeMsReference>();

            SaveMspDB(GetNewMspFileName(file), mspList);
            SaveDataBaseMapper(GetNewZippedDatabaseFileName(file), container);
            SaveDataBases(GetDataBasesFileName(file), container);
            SaveMsdialDataStorageCore(file, container);

            container.MspDB = mspList;
        }

        public virtual MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var storage = LoadMsdialDataStorageCore(file);
            LoadDataBases(GetDataBasesFileName(file), storage);
            LoadDataBaseMapper(GetNewZippedDatabaseFileName(file), storage);
            storage.MspDB = LoadMspDB(GetNewMspFileName(file));
            return storage;
        }

        protected virtual void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(container, file);
        }

        protected virtual MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialDataStorage>(file);
        } 

        public static void SaveMspDB(string path, List<MoleculeMsReference> db) {
            MoleculeMsRefMethods.SaveMspToFile(db, path);
        }

        public static List<MoleculeMsReference> LoadMspDB(string path) {
            if (File.Exists(path)) {
                return MoleculeMsRefMethods.LoadMspFromFile(path);
            }
            return new List<MoleculeMsReference>(0);
        }

        protected virtual void SaveDataBaseMapper(string path, MsdialDataStorage storage) {
            using (var stream = File.Open(path, FileMode.Create)) {
                storage.DataBaseMapper.Save(stream);
            }
        }

        protected virtual void LoadDataBaseMapper(string path, MsdialDataStorage storage) {
            using (var stream = File.Open(path, FileMode.Open)) {
                storage.DataBaseMapper?.Restore(new StandardLoadAnnotatorVisitor(storage.ParameterBase), stream);
            }
        }

        protected virtual void SaveDataBases(string path, MsdialDataStorage storage) {
            using (var stream = File.Open(path, FileMode.Create)) {
                storage.DataBases?.Save(stream);
            }
        }

        protected virtual void LoadDataBases(string path, MsdialDataStorage storage) {
            if (File.Exists(path)) {
                using (var stream = File.Open(path, FileMode.Open)) {
                    storage.DataBases = DataBaseStorage.Load(stream, new StandardLoadAnnotatorVisitor(storage.ParameterBase));
                }
            }
        }

        public static string GetNewMspFileName(string path) {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            return Path.Combine(folder, fileName + "_Loaded.msp2");
        }

        private static string GetNewZippedDatabaseFileName(string path) {
            return GetNewMspFileName(path) + ".zip";
        }

        private static string GetDataBasesFileName(string path) {
            return GetNewMspFileName(path) + ".dbs";
        }
    }
}
