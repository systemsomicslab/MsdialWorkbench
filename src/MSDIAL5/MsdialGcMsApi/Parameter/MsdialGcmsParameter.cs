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
        /// The GC-MS alignment's threshold on the integrated EI similarity of two features (see GcmsPeakJoiner;
        /// retention is judged separately by its own tolerance). Two spectra of one compound in two files commonly
        /// score 0.6-0.75 on it, so the 0.8 that <see cref="AlignmentBaseParameter"/> starts with, and 0.7, split
        /// most compounds of a GC-MS run.
        /// </summary>
        public const float DefaultSpectrumSimilarityAlignmentTolerance = .5f;

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
        /// then gives the file the GC-MS default threshold, <see cref="DefaultSpectrumSimilarityAlignmentTolerance"/>,
        /// whatever Ms1AlignmentTolerance held: 0.7 from the dialog fragments the alignment, and 0.015 or 0.5 from
        /// a method file admits any pair within the retention window. The EI weight the file aligned with,
        /// Ms1AlignmentFactor, is carried into the spectrum similarity factor, so the dialog shows it and the next
        /// alignment uses it.
        /// </remarks>
        [Key(157)]
        public bool AlignmentEiSimilarityIsSpectrumSimilarity { get; set; }

        void IMessagePackSerializationCallbackReceiver.OnBeforeSerialize() {
            AlignmentEiSimilarityIsSpectrumSimilarity = true;
        }

        void IMessagePackSerializationCallbackReceiver.OnAfterDeserialize() {
            if (!AlignmentEiSimilarityIsSpectrumSimilarity) {
                AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = DefaultSpectrumSimilarityAlignmentTolerance;
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
