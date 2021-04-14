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

            SaveMsdialDataStorageCore(file, container);

            SaveMspDB(file, mspList);
            container.MspDB = mspList;
        }

        public virtual MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = LoadMsdialDataStorageCore(file);
            container.MspDB = LoadMspDB(file);
            return container;
        }

        protected virtual void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(container, file);
        }

        protected virtual MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialDataStorage>(file);
        } 

        public static string GetNewMspFileName(string path) {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            return Path.Combine(folder, fileName + "_Loaded.msp2");
        }

        public static void SaveMspDB(string path, List<MoleculeMsReference> db) {
            MoleculeMsRefMethods.SaveMspToFile(db, GetNewMspFileName(path));
        }

        public static List<MoleculeMsReference> LoadMspDB(string path) {
            var mspPath = GetNewMspFileName(path);
            if (File.Exists(mspPath)) {
                return MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }
            return new List<MoleculeMsReference>(0);
        }
    }
}
