using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialLcImMsApi.Parser {
    public class MsdialLcImMsSerializer : MsdialSerializer {
        public override void SaveMsdialDataStorage(string file, MsdialDataStorage container) {

            var mspList = container.MspDB;
            container.MspDB = new List<MoleculeMsReference>();

            var saveObj = new MsdialLcImMsSaveObj(container);
            MessagePackHandler.SaveToFile(saveObj, file);

            var mspPath = GetNewMspFileName(file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);

            container.MspDB = mspList;
        }

        public override MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialLcImMsSaveObj>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }

            return new MsdialLcImMsSaveObj().ConvertToMsdialDataStorage(container);
        }
    }
}
