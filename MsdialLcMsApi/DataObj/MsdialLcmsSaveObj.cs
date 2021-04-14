using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcmsApi.Parameter;
using MessagePack;

namespace CompMs.MsdialLcMsApi.DataObj
{
    [MessagePackObject]
    public class MsdialLcmsSaveObj : MsdialSaveObj {
        [Key(6)]
        public MsdialLcmsParameter MsdialLcmsParameter { get; set; }

        public MsdialLcmsSaveObj() : base() { }

        public MsdialLcmsSaveObj(MsdialDataStorage container) : base(container) {
            MsdialLcmsParameter = (MsdialLcmsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage() {
            var storage = ConvertToMsdialDataStorageCore();
            storage.ParameterBase = MsdialLcmsParameter;
            return storage;
        }
    }
}
