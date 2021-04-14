using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialGcMsApi.DataObj;

namespace CompMs.MsdialGcMsApi.Parser
{
    public class MsdialGcmsSerializer : MsdialSerializer {
        protected override void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(new MsdialGcmsSaveObj(container), file);
        }

        protected override MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialGcmsSaveObj>(file).ConvertToMsdialDataStorage();
        }
    }
}
