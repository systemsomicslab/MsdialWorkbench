using CompMs.MsdialCore.Parameter;
using MessagePack;
using System.Collections.Generic;

namespace CompMs.MsdialLcmsApi.Parameter {
    [MessagePackObject]
    public class MsdialLcmsParameter : ParameterBase {
        public MsdialLcmsParameter(bool isLabUseOnly) : base(isLabUseOnly) {
            this.MachineCategory = Common.Enum.MachineCategory.LCMS;

            // MspSearchParam.WeightedDotProductCutOff = 0.15f;
            // MspSearchParam.SimpleDotProductCutOff = 0.15f;
            // MspSearchParam.ReverseDotProductCutOff = 0.5f;
        }

        [SerializationConstructor]
        public MsdialLcmsParameter() : this(isLabUseOnly: false) {

        }

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();
            return pStrings;
        }
    }
}
