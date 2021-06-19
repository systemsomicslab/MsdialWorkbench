using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialImmsCore.DataObj;
using System.IO;

namespace CompMs.MsdialImmsCore.Parser
{

    public class MsdialImmsSerializer : MsdialSerializer {
        protected override void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(new MsdialImmsSaveObj(container), file);
        }

        protected override MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialImmsSaveObj>(file).ConvertToMsdialDataStorage();
        }

        protected override void LoadDataBaseMapper(string path, MsdialDataStorage storage) {
            using (var stream = File.Open(path, FileMode.Open)) {
                storage.DataBaseMapper?.Restore(new ImmsRestorationVisitor(storage.ParameterBase), stream);
            }
        }
    }
}
