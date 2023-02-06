using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using MessagePack;

namespace CompMs.MsdialDimsCore.Parameter {
    [MessagePackObject]
    public class MsdialDimsParameter : ParameterBase {
        public MsdialDimsParameter(bool isLabUseOnly) : base(isLabUseOnly) {
            this.MachineCategory = MachineCategory.IFMS;

            MspSearchParam.WeightedDotProductCutOff = 0.1f;
            MspSearchParam.SimpleDotProductCutOff = 0.1f;
            MspSearchParam.ReverseDotProductCutOff = 0.3f;
            MspSearchParam.MatchedPeaksPercentageCutOff = 0.2f;
            MspSearchParam.MinimumSpectrumMatch = 1;
        }

        [SerializationConstructor]
        public MsdialDimsParameter() : this(isLabUseOnly: false) {

        }

        [Key(20)]
        public IDimsDataProviderFactoryParameter ProviderFactoryParameter { get; set; }
    }
}
