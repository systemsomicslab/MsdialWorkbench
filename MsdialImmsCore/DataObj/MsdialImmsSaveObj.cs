using CompMs.MsdialCore.DataObj;
using CompMs.MsdialImmsCore.Parameter;
using MessagePack;

namespace CompMs.MsdialImmsCore.DataObj
{
    [MessagePackObject]
    public class MsdialImmsSaveObj : MsdialSaveObj {
        [Key(6)]
        public MsdialImmsParameter MsdialImmsParameter { get; set; }

        public MsdialImmsSaveObj() : base() { }
        public MsdialImmsSaveObj(MsdialDataStorage container) : base(container) {
            MsdialImmsParameter = (MsdialImmsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage() {
            var storage = ConvertToMsdialDataStorageCore();
            storage.ParameterBase = MsdialImmsParameter;
            return storage;
        }
    }
}
