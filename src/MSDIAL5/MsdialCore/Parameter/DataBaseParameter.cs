using CompMs.Common.DataObj.Result;
using MessagePack;

namespace CompMs.MsdialCore.Parameter
{
    [MessagePackObject]
    public class DataBaseParameter {
        public DataBaseParameter(string dataBaseID, DataBaseSource dBSource) {
            DataBaseID = dataBaseID;
            DBSource = dBSource;
        }

        [Key(nameof(DataBaseID))]
        public string DataBaseID { get; }

        [Key(nameof(DBSource))]
        public DataBaseSource DBSource { get; }
    }
}
