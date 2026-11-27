using CompMs.Common.Enum;
using CompMs.Common.Utility;
using CompMs.MsdialCore.Parameter;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace CompMs.MsdialGcMsApi.Parameter {
    [MessagePackObject]
    public class MsdialGcmsParameter : ParameterBase {

        public MsdialGcmsParameter(bool isLabUseOnly) : base(isLabUseOnly) {
            this.MachineCategory = MachineCategory.GCMS;
            AlignmentBaseParam.Ms1AlignmentTolerance = .5f;
        } 

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
            pStrings.Add(String.Join(": ", new string[] { "Retention index alignment tolerance", RetentionIndexAlignmentTolerance.ToString(CultureInfo.InvariantCulture) }));
            pStrings.Add(String.Join(": ", new string[] { "Replace quant mass by user defined value", IsReplaceQuantmassByUserDefinedValue.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Is quant mass based on base peak mz", IsRepresentativeQuantMassBasedOnBasePeakMz.ToString() }));

            // The EI library search. These lines use the Console's method-file keys, so that a parameter file exported
            // from a GC-MS project runs in the Console with the same identification settings. The dot product cutoffs
            // are written as plain cosines with the "Square root of" keys, as the Console's method template does; the
            // keys without that prefix are read as squared values.
            var msp = MspSearchParam;
            string F(float value) => value.ToString(CultureInfo.InvariantCulture);
            pStrings.Add("\r\n");
            pStrings.Add("# MSP-based annotation");
            pStrings.Add(String.Join(": ", new string[] { "RT tolerance for MSP-based annotation", F(msp.RtTolerance) }));
            pStrings.Add(String.Join(": ", new string[] { "RI tolerance for MSP-based annotation", F(msp.RiTolerance) }));
            pStrings.Add(String.Join(": ", new string[] { "Mass range begin for MSP-based annotation", F(msp.MassRangeBegin) }));
            pStrings.Add(String.Join(": ", new string[] { "Mass range end for MSP-based annotation", F(msp.MassRangeEnd) }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 tolerance for MSP-based annotation", F(msp.Ms1Tolerance) }));
            pStrings.Add(String.Join(": ", new string[] { "Relative amplitude cutoff for MSP-based annotation", F(msp.RelativeAmpCutoff) }));
            pStrings.Add(String.Join(": ", new string[] { "Absolute amplitude cutoff for MSP-based annotation", F(msp.AbsoluteAmpCutoff) }));
            pStrings.Add(String.Join(": ", new string[] { "Square root of weighted dot product cutoff for MSP-based annotation", F(msp.WeightedDotProductCutOff) }));
            pStrings.Add(String.Join(": ", new string[] { "Square root of simple dot product cutoff for MSP-based annotation", F(msp.SimpleDotProductCutOff) }));
            pStrings.Add(String.Join(": ", new string[] { "Square root of reverse dot product cutoff for MSP-based annotation", F(msp.ReverseDotProductCutOff) }));
            pStrings.Add(String.Join(": ", new string[] { "Matched peaks percentage cutoff for MSP-based annotation", F(msp.MatchedPeaksPercentageCutOff) }));
            pStrings.Add(String.Join(": ", new string[] { "Minimum spectrum match for MSP-based annotation", F(msp.MinimumSpectrumMatch) }));
            pStrings.Add(String.Join(": ", new string[] { "Total score cutoff for MSP-based annotation", F(msp.TotalScoreCutoff) }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation scoring", msp.IsUseTimeForAnnotationScoring.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation filtering", msp.IsUseTimeForAnnotationFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Only report top hit for MSP-based annotation", OnlyReportTopHitInMspSearch.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Execute annotation process only for alignment file", IsIdentificationOnlyPerformedForAlignmentFile.ToString() }));

            return pStrings;
        }
    }
}
