using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialCore.Parser {
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

            var mspPath = GetNewMspFileName(file);
            MessagePackHandler.SaveToFile(container, file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);

            container.MspDB = mspList;
        }

        public virtual MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialDataStorage>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }
            return container;
        }

        public static string GetNewMspFileName(string path) {
            var fileName = Path.GetFileNameWithoutExtension(path);
            var folder = Path.GetDirectoryName(path);
            return Path.Combine(folder, fileName + "_Loaded.msp2");
        }
    }
}
