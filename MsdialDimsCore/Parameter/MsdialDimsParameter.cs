using CompMs.MsdialCore.Parameter;
using CompMs.Common.Components;
using CompMs.Common.Enum;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialDimsCore.Parameter {
    [MessagePack.MessagePackObject]
    public class MsdialDimsParameter : ParameterBase {
        public MsdialDimsParameter() {
            this.MachineCategory = MachineCategory.IFMS;

            MspSearchParam.WeightedDotProductCutOff = 0.1f;
            MspSearchParam.SimpleDotProductCutOff = 0.1f;
            MspSearchParam.ReverseDotProductCutOff = 0.3f;
            MspSearchParam.MatchedPeaksPercentageCutOff = 0.2f;
            MspSearchParam.MinimumSpectrumMatch = 1;
        }
    }
}
