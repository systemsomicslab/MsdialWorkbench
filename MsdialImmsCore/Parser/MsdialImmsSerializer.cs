using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.DataObj;

namespace CompMs.MsdialImmsCore.Parser
{

    public class MsdialImmsSerializer : MsdialSerializer {
        protected override void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(new MsdialImmsSaveObj(container), file);
        }

        protected override MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialImmsSaveObj>(file).ConvertToMsdialDataStorage();
        }
    }
}
