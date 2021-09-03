using CompMs.MsdialCore.Parameter;
using CompMs.Common.Enum;
using MessagePack;
using System;

namespace CompMs.MsdialDimsCore.Parameter {
    [MessagePackObject]
    public class MsdialDimsParameter : ParameterBase {
        public MsdialDimsParameter() {
            this.MachineCategory = MachineCategory.IFMS;

            MspSearchParam.WeightedDotProductCutOff = 0.1f;
            MspSearchParam.SimpleDotProductCutOff = 0.1f;
            MspSearchParam.ReverseDotProductCutOff = 0.3f;
            MspSearchParam.MatchedPeaksPercentageCutOff = 0.2f;
            MspSearchParam.MinimumSpectrumMatch = 1;
        }

        [Key(20)]
        public IDimsDataProviderFactoryParameter ProviderFactoryParameter { get; set; }
    }
}
