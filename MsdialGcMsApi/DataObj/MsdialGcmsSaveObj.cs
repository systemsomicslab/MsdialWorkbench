using CompMs.MsdialCore.DataObj;
using CompMs.MsdialGcMsApi.Parameter;
using MessagePack;

namespace CompMs.MsdialGcMsApi.DataObj
{
    [MessagePackObject]
    public class MsdialGcmsSaveObj : MsdialSaveObj {
        [Key(6)]
        public MsdialGcmsParameter MsdialGcmsParameter { get; set; }

        public MsdialGcmsSaveObj() : base() { }

        public MsdialGcmsSaveObj(MsdialDataStorage container) : base(container) {
            MsdialGcmsParameter = (MsdialGcmsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage() {
            var storage = ConvertToMsdialDataStorageCore();
            storage.ParameterBase = MsdialGcmsParameter;
            return storage;
        }
    }
}
