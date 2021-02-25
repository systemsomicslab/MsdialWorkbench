using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialDimsCore.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialDimsCore.Parser {
    public class MsdialDimsSerializer : MsdialSerializer {
        public override void SaveMsdialDataStorage(string file, MsdialDataStorage container) {

            var mspList = container.MspDB;

            var saveObj = new MsdialDimsSaveObj(container);
            saveObj.MspDB = new List<MoleculeMsReference>();
            MessagePackHandler.SaveToFile(saveObj, file);

            var mspPath = GetNewMspFileName(file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);
        }

        public override MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialDimsSaveObj>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }
            return new MsdialDimsSaveObj().ConvertToMsdialDataStorage(container);
        }
    }
}
