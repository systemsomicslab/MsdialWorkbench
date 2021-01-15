using CompMs.Common.Components;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialLcmsApi.Parameter {
    [MessagePackObject]
    public class MsdialLcmsParameter : ParameterBase {
        public MsdialLcmsParameter() {
            this.MachineCategory = Common.Enum.MachineCategory.LCMS;

            // MspSearchParam.WeightedDotProductCutOff = 0.15f;
            // MspSearchParam.SimpleDotProductCutOff = 0.15f;
            // MspSearchParam.ReverseDotProductCutOff = 0.5f;
        }

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();
            return pStrings;
        }
    }
}
