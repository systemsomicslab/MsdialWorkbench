using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Parameter {
    [MessagePackObject]
    public class MsdialGcmsParameter : ParameterBase, IMessagePackSerializationCallbackReceiver {

        public MsdialGcmsParameter(bool isLabUseOnly) : base(isLabUseOnly) {
            this.MachineCategory = MachineCategory.GCMS;
            AlignmentBaseParam.Ms1AlignmentTolerance = .5f;
            AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = DefaultSpectrumSimilarityAlignmentTolerance;
        } 

        /// <summary>
        /// The GC-MS alignment's threshold on the total score of two features (EI similarity and retention similarity,
        /// weighed by their factors; see GcmsPeakJoiner): MS-DIAL 4's cutoff of 70 and the GC-MS dialog's 70 %. The 0.8
        /// that <see cref="AlignmentBaseParameter"/> starts with splits most compounds of a GC-MS run.
        /// </summary>
        public const float DefaultSpectrumSimilarityAlignmentTolerance = .7f;

        [SerializationConstructor]
        public MsdialGcmsParameter() : this(isLabUseOnly: false) { }
        
        [Key(150)]
        public string RiDictionaryFilePath { get; set; } = string.Empty;
        [Key(151)]
        public RiCompoundType RiCompoundType { get; set; } = RiCompoundType.Alkanes;
        [Key(152)]
        public RetentionType RetentionType { get; set; } = RetentionType.RT;
        [Key(153)]
        public AlignmentIndexType AlignmentIndexType { get; set; } = AlignmentIndexType.RT;
        [Key(154)]
        public float RetentionIndexAlignmentTolerance { get; set; } = 20;
        [Key(155)]
        public bool IsReplaceQuantmassByUserDefinedValue { get; set; } = false;
        [Key(156)]
        public bool IsRepresentativeQuantMassBasedOnBasePeakMz { get; set; } = false;

        /// <summary>
        /// True when this parameter was read from a project or parameter file in which the alignment's
        /// similarity threshold and EI weight are <see cref="AlignmentBaseParameter.SpectrumSimilarityAlignmentTolerance"/>
        /// and <see cref="AlignmentBaseParameter.SpectrumSimilarityAlignmentFactor"/>. Every save writes true.
        /// </summary>
        /// <remarks>
        /// Until this key existed GcmsPeakJoiner took the threshold and weight from Ms1AlignmentTolerance and
        /// Ms1AlignmentFactor, and the GC-MS alignment dialog wrote its "EI similarity tolerance / factor" there,
        /// while the spectrum similarity settings kept a default nobody saw. A file written then has no element
        /// for this key; the member has no initializer and no constructor sets it, so it reads false whether
        /// MessagePack leaves an absent member alone or sets it to default(T). <see cref="OnAfterDeserialize"/>
        /// then moves the settings that file aligned with into the spectrum similarity settings, so that the
        /// dialog shows them and the next alignment uses them. A project the Console wrote from a method file
        /// with "MS1 tolerance for alignment: 0.015" therefore keeps 0.015, which admits any pair, as it did.
        /// </remarks>
        [Key(157)]
        public bool AlignmentEiSimilarityIsSpectrumSimilarity { get; set; }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize() {
            AlignmentEiSimilarityIsSpectrumSimilarity = true;
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize() {
            if (!AlignmentEiSimilarityIsSpectrumSimilarity) {
                AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = AlignmentBaseParam.Ms1AlignmentTolerance;
                AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = AlignmentBaseParam.Ms1AlignmentFactor;
                AlignmentEiSimilarityIsSpectrumSimilarity = true;
            }
        }

        public Dictionary<int, RetentionIndexHandler> GetRIHandlers() {
            return RefSpecMatchBaseParam.FileIdRiInfoDictionary.ToDictionary(kvp => kvp.Key, kvp => new RetentionIndexHandler(RiCompoundType, kvp.Value.RiDictionary));
        }

        public override List<string> ParametersAsText() {
            var pStrings = base.ParametersAsText();

            pStrings.Add("\r\n");
            pStrings.Add("# GCMS specific parameters");
            pStrings.Add(String.Join(": ", new string[] { "RI dictionary file path", RiDictionaryFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RI compound type", RiCompoundType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention type", RetentionType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Alignment index type", AlignmentIndexType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention index alignment tolerance", RetentionIndexAlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Replace quant mass by user defined value", IsReplaceQuantmassByUserDefinedValue.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Is quant mass based on base peak mz", IsRepresentativeQuantMassBasedOnBasePeakMz.ToString() }));

            return pStrings;
        }
    }
}
