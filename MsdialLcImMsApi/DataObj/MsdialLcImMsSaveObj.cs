using CompMs.MsdialCore.DataObj;
using CompMs.MsdialLcImMsApi.Parameter;
using MessagePack;

namespace CompMs.MsdialLcImMsApi.DataObj
{
    [MessagePack.MessagePackObject]
    public class MsdialLcImMsSaveObj : MsdialSaveObj {
        [Key(6)]
        public MsdialLcImMsParameter MsdialLcImMsParameter { get; set; }

        public MsdialLcImMsSaveObj() : base() { }

        public MsdialLcImMsSaveObj(MsdialDataStorage container) : base(container) {
            MsdialLcImMsParameter = (MsdialLcImMsParameter)container.ParameterBase;
        }

        public MsdialDataStorage ConvertToMsdialDataStorage() {
            var storage = ConvertToMsdialDataStorageCore();
            storage.ParameterBase = MsdialLcImMsParameter;
            return storage;
        }
    }
}
