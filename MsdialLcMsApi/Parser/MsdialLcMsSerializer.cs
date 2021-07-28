using CompMs.Common.MessagePack;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Parser;
using CompMs.MsdialLcMsApi.DataObj;
using System.IO;

namespace CompMs.MsdialLcMsApi.Parser
{
    public class MsdialLcmsSerializer : MsdialSerializer {
        protected override void SaveMsdialDataStorageCore(string file, MsdialDataStorage container) {
            MessagePackHandler.SaveToFile(new MsdialLcmsSaveObj(container), file);
        }

        protected override MsdialDataStorage LoadMsdialDataStorageCore(string file) {
            return MessagePackHandler.LoadFromFile<MsdialLcmsSaveObj>(file).ConvertToMsdialDataStorage();
        }

        protected override void LoadDataBaseMapper(string path, MsdialDataStorage storage) {
            using (var stream = File.Open(path, FileMode.Open)) {
                storage.DataBaseMapper?.Restore(new LcmsLoadAnnotatorVisitor(storage.ParameterBase), stream);
            }
        }
    }
}
