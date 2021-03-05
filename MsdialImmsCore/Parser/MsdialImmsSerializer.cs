using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.DataObj;
using System.Collections.Generic;
using System.IO;

namespace CompMs.MsdialImmsCore.Parser {

    public class MsdialImmsSerializer : MsdialSerializer {
        public override void SaveMsdialDataStorage(string file, MsdialDataStorage container) {

            var mspList = container.MspDB;

            var saveObj = new MsdialImmsSaveObj(container);
            saveObj.MspDB = new List<MoleculeMsReference>();
            MessagePackHandler.SaveToFile(saveObj, file);

            var mspPath = GetNewMspFileName(file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);
        }

        public override MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialImmsSaveObj>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }
            return new MsdialImmsSaveObj().ConvertToMsdialDataStorage(container);
        }
    }
}
