using CompMs.MsdialCore.DataObj;
using CompMs.MsdialDimsCore.Parameter;
using MessagePack;

namespace CompMs.MsdialDimsCore.DataObj
{
    [MessagePackObject]
    public class MsdialDimsSaveObj : MsdialSaveObj {
        [Key(6)]
        public MsdialDimsParameter MsdialDimsParameter { get; set; }

        public MsdialDimsSaveObj() : base() { }

        public MsdialDimsSaveObj(MsdialDataStorage container) : base(container) {
            MsdialDimsParameter = (MsdialDimsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage() {
            var storage = ConvertToMsdialDataStorageCore();
            storage.ParameterBase = MsdialDimsParameter;
            return storage;
        }
    }
}
