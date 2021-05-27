using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcImMsApi.DataObj;

namespace CompMs.MsdialLcImMsApi.Parser
{
    public class MsdialLcImMsSerializer : MsdialSerializer {
        protected override void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(new MsdialLcImMsSaveObj(container), file);
        }

        protected override MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialLcImMsSaveObj>(file).ConvertToMsdialDataStorage();
        }
    }
}
