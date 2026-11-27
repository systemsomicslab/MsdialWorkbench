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

        /// <summary>
        /// Whether the alignment joins two spots of one compound that its retention drift split apart (see
        /// GcmsPeakJoiner), when their centres are within <see cref="SplitSpotJoinWindow"/>.
        /// </summary>
        /// <remarks>
        /// Keys follow on from 157 without a gap. MessagePack writes nil into a gap of an int-keyed array, and a
        /// value-type member (bool, float, double) added at that key later could not read such a file.
        /// </remarks>
        [Key(158)]
        public bool JoinSpotsSplitByRetentionDrift { get; set; } = true;

        /// <summary>
        /// How far apart the centres of two spots (the mean retention of their detected peaks) may be for
        /// <see cref="JoinSpotsSplitByRetentionDrift"/> to join them, in multiples of the alignment's retention
        /// tolerance: <see cref="AlignmentBaseParameter.RetentionTimeAlignmentTolerance"/> when aligning on
        /// retention time, <see cref="RetentionIndexAlignmentTolerance"/> on retention index.
        /// </summary>
        /// <remarks>
        /// Every detected peak of a joined spot must also lie within one retention tolerance of the joined spot's
        /// centre, the window in which the gap filler re-extracts each file's peak. Two spots' centres are then at
        /// most twice the tolerance apart, so a window above the default of 2 joins no more; a smaller one joins less.
        /// </remarks>
        [Key(159)]
        public float SplitSpotJoinWindow { get; set; } = DefaultSplitSpotJoinWindow;

        public const float DefaultSplitSpotJoinWindow = 2f;

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
            pStrings.Add(String.Join(": ", new string[] { "Retention index alignment tolerance", RetentionIndexAlignmentTolerance.ToString(CultureInfo.InvariantCulture) }));
            pStrings.Add(String.Join(": ", new string[] { "Replace quant mass by user defined value", IsReplaceQuantmassByUserDefinedValue.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Is quant mass based on base peak mz", IsRepresentativeQuantMassBasedOnBasePeakMz.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Join spots split by retention drift", JoinSpotsSplitByRetentionDrift.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Split spot join window", SplitSpotJoinWindow.ToString(CultureInfo.InvariantCulture) }));

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
            // The Console reads this key in percent of the base peak, as the GUI shows it; the model holds a fraction.
            pStrings.Add(String.Join(": ", new string[] { "Relative amplitude cutoff for MSP-based annotation", F((float)Math.Round(msp.RelativeAmpCutoff * 100d, 6)) }));
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
