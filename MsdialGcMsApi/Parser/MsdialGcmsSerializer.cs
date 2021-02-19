using Accord.Statistics.Distributions.Univariate;
using CompMs.Common.Components;
using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace CompMs.MsdialGcMsApi.Parser {
    public class MsdialGcmsSerializer : MsdialSerializer {
        public override void SaveMsdialDataStorage(string file, MsdialDataStorage container) {

            var mspList = container.MspDB;

            var saveObj = new MsdialGcmsSaveObj(container);
            saveObj.MspDB = new List<MoleculeMsReference>();
            MessagePackHandler.SaveToFile<MsdialGcmsSaveObj>(saveObj, file);

            var mspPath = GetNewMspFileName(file);
            MoleculeMsRefMethods.SaveMspToFile(mspList, mspPath);
        }

        public override MsdialDataStorage LoadMsdialDataStorageBase(string file) {
            var container = MessagePackHandler.LoadFromFile<MsdialGcmsSaveObj>(file);
            var mspPath = GetNewMspFileName(file);
            if (File.Exists(mspPath)) {
                container.MspDB = MoleculeMsRefMethods.LoadMspFromFile(mspPath);
            }
            return new MsdialGcmsSaveObj().ConvertToMsdialDataStorage(container);
        }
    }
}
