using CompMs.MsdialCore.Parameter;
using MessagePack;
using System.Collections.Generic;
using System.Globalization;

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

        /// <summary>The method-file and parameter-text names of the MS/MS entropy cleanup settings.</summary>
        public const string MsmsEntropyCleanupKey = "MS/MS entropy cleanup";
        public const string MsmsMaxSpectralEntropyKey = "MS/MS maximum spectral entropy";
        public const string MsmsEntropyCleanupProtectedPercentKey = "MS/MS entropy cleanup protected percent of base peak";

        /// <summary>The MS/MS entropy cleanup settings as parameter-text lines, which the console method file reads back.</summary>
        public List<string> MsmsEntropyCleanupAsText() {
            var chromDec = ChromDecBaseParam;
            return new List<string> {
                MsmsEntropyCleanupKey + ": " + chromDec.IsDoMsmsEntropyCleanup.ToString(),
                MsmsMaxSpectralEntropyKey + ": " + chromDec.MsmsMaxSpectralEntropy.ToString(CultureInfo.InvariantCulture),
                MsmsEntropyCleanupProtectedPercentKey + ": " + chromDec.MsmsEntropyCleanupProtectedPercentOfBasePeak.ToString(CultureInfo.InvariantCulture),
            };
        }

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();
            // The MS/MS entropy cleanup and the lipid RT check run in LC-MS processing only; MsdialLcImMsParameter derives
            // from this class and runs neither, so these sections are written for LC-MS projects only.
            if (MachineCategory == Common.Enum.MachineCategory.LCMS) {
                pStrings.Add("\r\n");
                pStrings.Add("# MS/MS entropy cleanup");
                pStrings.AddRange(MsmsEntropyCleanupAsText());

                pStrings.Add("\r\n");
                pStrings.Add("# Lipid RT check");
                pStrings.AddRange(LipidRtCheckParam.ParametersAsText(TargetOmics));
            }
            return pStrings;
        }
    }
}
