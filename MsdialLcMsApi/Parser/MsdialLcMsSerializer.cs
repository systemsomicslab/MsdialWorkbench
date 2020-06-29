using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcmsApi.Parameter;
using CompMs.MsdialLcMsApi.DataObj;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialLcMsApi.Parser {
    public class MsdialLcmsSerializer : MsdialSerializer {
        public override void SaveMsdialDataStorage(string file, MsdialDataStorage container) {

            var saveObj = new MsdialLcmsSaveObj(container);
            var mspList = container.MspDB;
            container.MspDB = new List<MoleculeMsReference>();

            var mspPath = GetNewMspFileName(file);
            MessagePackDefaultHandler.SaveToFile(saveObj, file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);

            container.MspDB = mspList;
        }

        public override MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialLcmsSaveObj>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }

            return new MsdialLcmsSaveObj().ConvertToMsdialDataStorage(container);
        }
    }
}
