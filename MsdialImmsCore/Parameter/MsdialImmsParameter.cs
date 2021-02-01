using CompMs.MsdialCore.Parameter;
using CompMs.Common.Enum;

namespace CompMs.MsdialImmsCore.Parameter
{
    [MessagePack.MessagePackObject]
    public class MsdialImmsParameter : ParameterBase {
        public MsdialImmsParameter() {
            MachineCategory = MachineCategory.IMMS;
        }
    }
}
