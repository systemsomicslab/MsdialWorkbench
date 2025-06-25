using CompMs.Common.Enum;
using CompMs.MsdialCore.Parameter;
using MessagePack;

namespace CompMs.MsdialDimsCore.Parameter {
    [MessagePackObject]
    public class MsdialDimsParameter : ParameterBase {
        public MsdialDimsParameter(bool isLabUseOnly) : base(isLabUseOnly) {
            this.MachineCategory = MachineCategory.IFMS;

            MspSearchParam.SquaredWeightedDotProductCutOff = .32f * .32f;
            MspSearchParam.SquaredSimpleDotProductCutOff = .32f * .32f;
            MspSearchParam.SquaredReverseDotProductCutOff = .55f * .55f;
            MspSearchParam.MatchedPeaksPercentageCutOff = .2f;
            MspSearchParam.MinimumSpectrumMatch = 1;
        }

        [SerializationConstructor]
        public MsdialDimsParameter() : this(isLabUseOnly: false) {

        }

        [Key(20)]
        public IDimsDataProviderFactoryParameter ProviderFactoryParameter { get; set; }
    }
}
