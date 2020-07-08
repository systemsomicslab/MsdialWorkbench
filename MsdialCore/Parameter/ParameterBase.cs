using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Query;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using MessagePack;
using CompMs.MsdialCore.Properties;
using Accord;
using System.Linq;
using CompMs.Common.Parser;

namespace CompMs.MsdialCore.Parameter {

    [MessagePackObject]
    public class ParameterBase {

        [Key(0)]
        public DateTime ProjectStartDate { get; set; } = DateTime.Now;
        [Key(1)]
        public DateTime FinalSavedDate { get; set; } = DateTime.Now;
        [Key(2)]
        public string MsdialVersionNumber { get; set; } = Resources.VERSION;


        // Project container
        [Key(3)]
        public string ProjectFolderPath { get; set; }
        [Key(4)]
        public string ProjectFilePath { get; set; }
        [Key(5)]
        public Dictionary<int, string> FileID_ClassName { get; set; }
        [Key(6)]
        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType { get; set; }
        [Key(7)]
        public bool IsBoxPlotForAlignmentResult { get; set; }
        [Key(8)]
        public Dictionary<string, int> ClassnameToOrder { get; set; }
        [Key(9)]
        public Dictionary<string, List<byte>> ClassnameToColorBytes { get; set; }

        // Project type
        [Key(10)]
        public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.DDA;
        [Key(11)]
        public MSDataType MSDataType { get; set; } = MSDataType.Profile;
        [Key(12)]
        public MSDataType MS2DataType { get; set; } = MSDataType.Profile;
        [Key(13)]
        public IonMode IonMode { get; set; } = IonMode.Positive;
        [Key(14)]
        public TargetOmics TargetOmics { get; set; } = TargetOmics.Metablomics;
        [Key(15)]
        public Ionization Ionization { get; set; } = Ionization.ESI;
        [Key(16)]
        public MachineCategory MachineCategory { get; set; } = MachineCategory.LCMS;
        //[Key(17)]
        //public bool IsAIF { get; set; } = false;

        // Project metadata
        [Key(18)]
        public string InstrumentType { get; set; } = string.Empty;
        [Key(19)]
        public string Instrument { get; set; } = string.Empty;
        [Key(20)]
        public string Authors { get; set; } = string.Empty;
        [Key(21)]
        public string License { get; set; } = string.Empty;
        [Key(22)]
        public string Comment { get; set; } = string.Empty;

        // Annotation
        [Key(23)]
        public string MspFilePath { get; set; } = string.Empty;
        [Key(24)]
        public string TextDBFilePath { get; set; } = string.Empty;
        [Key(25)]
        public string IsotopeTextDBFilePath { get; set; } = string.Empty;
        [Key(26)]
        public string CompoundListInTargetModePath { get; set; } = string.Empty;
        [Key(147)]
        public string CompoundListForRtCorrectionPath { get; set; } = string.Empty;
        [Key(27)]
        public List<AdductIon> SearchedAdductIons { get; set; } = new List<AdductIon>();

        // Export
        [Key(28)]
        public ExportSpectraFileFormat ExportSpectraFileFormat { get; set; } = ExportSpectraFileFormat.msp;
        [Key(29)]
        public ExportspectraType ExportSpectraType { get; set; } = ExportspectraType.deconvoluted;
        [Key(30)]
        public string MatExportFolderPath { get; set; } = string.Empty;
        [Key(31)]
        public string ExportFolderPath { get; set; } = string.Empty;


        [Key(32)]
        public bool IsHeightMatrixExport { get; set; } = true;
        [Key(33)]
        public bool IsNormalizedMatrixExport { get; set; }
        [Key(34)]
        public bool IsRepresentativeSpectraExport { get; set; }
        [Key(35)]
        public bool IsPeakIdMatrixExport { get; set; }
        [Key(36)]
        public bool IsRetentionTimeMatrixExport { get; set; }
        [Key(37)]
        public bool IsMassMatrixExport { get; set; }
        [Key(38)]
        public bool IsMsmsIncludedMatrixExport { get; set; }

        [Key(39)]
        public bool IsUniqueMsMatrixExport { get; set; }
        [Key(40)]
        public bool IsPeakAreaMatrixExport { get; set; }
        [Key(41)]
        public bool IsParameterExport { get; set; }
        [Key(42)]
        public bool IsGnpsExport { get; set; }
        [Key(43)]
        public bool IsMolecularNetworkingExport { get; set; }
        [Key(44)]
        public bool IsSnMatrixExport { get; set; }
        [Key(45)]
        public bool IsExportedAsMzTabM { get; set; }


        // Process parameters
        [Key(46)]
        public ProcessOption ProcessOption { get; set; } = ProcessOption.All;
        [Key(47)]
        public int NumThreads { get; set; } = 2;

        // feature detection base
        [Key(48)]
        public SmoothingMethod SmoothingMethod { get; set; } = SmoothingMethod.LinearWeightedMovingAverage;
        [Key(49)]
        public int SmoothingLevel { get; set; } = 3;
        [Key(50)]
        public double MinimumAmplitude { get; set; } = 1000;
        [Key(51)]
        public double MinimumDatapoints { get; set; } = 5;
        [Key(52)]
        public float MassSliceWidth { get; set; } = 0.1F;
        [Key(53)]
        public float RetentionTimeBegin { get; set; } = 0;
        [Key(54)]
        public float RetentionTimeEnd { get; set; } = 100;
        [Key(55)]
        public float MassRangeBegin { get; set; } = 0;
        [Key(56)]
        public float MassRangeEnd { get; set; } = 2000;
        [Key(57)]
        public float Ms2MassRangeBegin { get; set; } = 0;
        [Key(58)]
        public float Ms2MassRangeEnd { get; set; } = 2000;
        [Key(59)]
        public float CentroidMs1Tolerance { get; set; } = 0.01F;
        [Key(60)]
        public float CentroidMs2Tolerance { get; set; } = 0.025F;
        [Key(61)]
        public int MaxChargeNumber { get; set; } = 2;
        [Key(62)]
        public bool IsBrClConsideredForIsotopes { get; set; } = false;
        [Key(63)]
        public List<MzSearchQuery> ExcludedMassList { get; set; } = new List<MzSearchQuery>();


        // alignment base
        [Key(64)]
        public int AlignmentReferenceFileID { get; set; } = 0;
        [Key(65)]
        public float RetentionTimeAlignmentFactor { get; set; } = 0.5F;
        [Key(66)]
        public float Ms1AlignmentFactor { get; set; } = 0.5F;
        [Key(67)]
        public float SpectrumSimilarityAlignmentFactor { get; set; } = 59F;
        [Key(68)]
        public float Ms1AlignmentTolerance { get; set; } = 0.015F;
        [Key(69)]
        public float RetentionTimeAlignmentTolerance { get; set; } = 0.05F;
        [Key(70)]
        public float SpectrumSimilarityAlignmentTolerance { get; set; } = 0.8F;
        [Key(71)]
        public float AlignmentScoreCutOff { get; set; } = 50;
        [Key(72)]
        public bool TogetherWithAlignment { get; set; } = true;


        // spectral library search
        [Key(73)]
        public LipidQueryBean LipidQueryContainer { get; set; } = new LipidQueryBean() { 
            SolventType = SolventType.CH3COONH4,
            LbmQueries = LbmQueryParcer.GetLbmQueries(false)
        };
        [Key(74)]
        public MsRefSearchParameterBase MspSearchParam { get; set; } = new MsRefSearchParameterBase();


        [Key(75)]
        public bool OnlyReportTopHitInMspSearch { get; set; } = false;
        [Key(76)]
        public MsRefSearchParameterBase TextDbSearchParam { get; set; } = new MsRefSearchParameterBase() {
            RtTolerance = 0.1F, Ms1Tolerance = 0.01F, TotalScoreCutoff = 85
        };
        [Key(77)]
        public bool OnlyReportTopHitInTextDBSearch { get; set; } = false;
        [Key(78)]
        public bool IsIdentificationOnlyPerformedForAlignmentFile { get; set; } = false;

        [Key(79)]
        public Dictionary<int, RiDictionaryInfo> FileIdRiInfoDictionary { get; set; } = new Dictionary<int, RiDictionaryInfo>();

        // deconvolution
        [Key(80)]
        public float SigmaWindowValue { get; set; } = 0.5F;
        [Key(81)]
        public float AmplitudeCutoff { get; set; } = 0;
        [Key(82)]
        public float AveragePeakWidth { get; set; } = 30;
        [Key(83)]
        public float KeptIsotopeRange { get; set; } = 5;
        [Key(84)]
        public bool RemoveAfterPrecursor { get; set; } = true;
        [Key(85)]
        public bool KeepOriginalPrecursorIsotopes { get; set; } = false;
        [Key(86)]
        public AccuracyType AccuracyType { get; set; } = AccuracyType.IsAccurate;


        // Post-alignment and filtering
        [Key(87)]
        public float PeakCountFilter { get; set; } = 0;
        [Key(88)]
        public bool IsForceInsertForGapFilling { get; set; } = true;
        [Key(89)]
        public float NPercentDetectedInOneGroup { get; set; } = 0;
        [Key(90)]
        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange { get; set; } = false;
        [Key(91)]
        public float SampleMaxOverBlankAverage { get; set; } = 5;
        [Key(92)]
        public float SampleAverageOverBlankAverage { get; set; } = 5;
        [Key(93)]
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking { get; set; } = true;
        [Key(94)]
        public bool IsKeepRefMatchedMetaboliteFeatures { get; set; } = true;
        [Key(95)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples { get; set; } = false;
        [Key(96)]
        public BlankFiltering BlankFiltering { get; set; } = BlankFiltering.SampleMaxOverBlankAve;
        [Key(97)]
        public bool IsKeepSuggestedMetaboliteFeatures { get; set; } = false;
        [Key(98)]
        public float FoldChangeForBlankFiltering { get; set; } = 5;


        // Normalization option
        [Key(99)]
        public bool IsNormalizeNone { get; set; } = true;
        [Key(100)]
        public bool IsNormalizeIS { get; set; }
        [Key(101)]
        public bool IsNormalizeLowess { get; set; }
        [Key(102)]
        public bool IsNormalizeIsLowess { get; set; }
        [Key(103)]
        public bool IsNormalizeTic { get; set; }
        [Key(104)]
        public bool IsNormalizeMTic { get; set; }
        [Key(105)]
        public bool IsNormalizeSplash { get; set; }

        [Key(106)]
        public double LowessSpan { get; set; }
        [Key(107)]
        public bool IsBlankSubtract { get; set; }

        // Statistics
        [Key(108)]
        public TransformMethod Transform { get; set; } = TransformMethod.None;
        [Key(109)]
        public ScaleMethod Scale { get; set; } = ScaleMethod.AutoScale;
        [Key(110)]
        public int MaxComponent { get; set; } = 2;

        [Key(111)]
        public TransformMethod TransformPls { get; set; } = TransformMethod.None;
        [Key(112)]
        public ScaleMethod ScalePls { get; set; } = ScaleMethod.AutoScale;
        [Key(113)]
        public bool IsAutoFitPls { get; set; } = true;
        [Key(114)]
        public int ComponentPls { get; set; } = 2;

        [Key(115)]
        public MultivariateAnalysisOption MultivariateAnalysisOption { get; set; } = MultivariateAnalysisOption.Pca;
        [Key(116)]
        public bool IsIdentifiedImportedInStatistics { get; set; } = true;
        [Key(117)]
        public bool IsAnnotatedImportedInStatistics { get; set; }
        [Key(118)]
        public bool IsUnknownImportedInStatistics { get; set; }

        // Mrmprobs export
        [Key(119)]
        public float MpMs1Tolerance { get; set; } = 0.005F;
        [Key(120)]
        public float MpMs2Tolerance { get; set; } = 0.01F;
        [Key(121)]
        public float MpRtTolerance { get; set; } = 0.5F;
        [Key(122)]
        public int MpTopN { get; set; } = 5;
        [Key(123)]
        public bool MpIsIncludeMsLevel1 { get; set; } = true;
        [Key(124)]
        public bool MpIsUseMs1LevelForQuant { get; set; } = false;
        [Key(125)]
        public bool MpIsFocusedSpotOutput { get; set; } = false;
        [Key(126)]
        public bool MpIsReferenceBaseOutput { get; set; } = true;
        [Key(127)]
        public bool MpIsExportOtherCandidates { get; set; } = false;
        [Key(128)]
        public float MpIdentificationScoreCutOff { get; set; } = 80F;


        // molecular networking
        [Key(129)]
        public double MnRtTolerance { get; set; } = 100;
        [Key(130)]
        public double MnIonCorrelationSimilarityCutOff { get; set; } = 95;
        [Key(131)]
        public double MnSpectrumSimilarityCutOff { get; set; } = 75;
        [Key(132)]
        public double MnRelativeAbundanceCutOff { get; set; } = 1;
        [Key(133)]
        public double MnMassTolerance { get; set; } = 0.025;
        [Key(134)]
        public bool MnIsExportIonCorrelation { get; set; } = false;


        // others
        [Key(135)]
        public RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon { get; set; } = new RetentionTimeCorrectionCommon();
        [Key(136)]
        public List<MoleculeMsReference> CompoundListInTargetMode { get; set; } = null;
        [Key(137)]
        public List<StandardCompound> StandardCompounds { get; set; } = null;

        [Key(138)]
        public bool IsLabPrivate { get; set; } = false;
        [Key(139)]
        public bool IsLabPrivateVersionTada { get; set; } = false;

        //Tracking of isotope labeles
        [Key(140)]
        public bool TrackingIsotopeLabels { get; set; } = false;
        [Key(141)]
        public bool UseTargetFormulaLibrary { get; set; } = false;
        [Key(142)]
        public IsotopeTrackingDictionary IsotopeTrackingDictionary { get; set; } = new IsotopeTrackingDictionary();
        [Key(143)]
        public int NonLabeledReferenceID { get; set; } = 0;
        [Key(144)]
        public bool SetFullyLabeledReferenceFile { get; set; } = false;
        [Key(145)]
        public int FullyLabeledReferenceID { get; set; } = 0;

        // corrdec
        [Key(146)]
        public CorrDecParam CorrDecParam { get; set; } = new CorrDecParam();

        public virtual List<string> ParametersAsText() {
            var pStrings = new List<string>();
            pStrings.Add("# Project information");
            pStrings.Add(String.Join(": ", new string[] { "Project start date", ProjectStartDate.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Final saved date", FinalSavedDate.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS-DIAL version number", MsdialVersionNumber.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Project folder path", ProjectFolderPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Project file path", ProjectFilePath.ToString() }));

            pStrings.Add(String.Join(": ", new string[] { "Acquisition type", AcquisitionType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 data type", MSDataType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS2 data type", MS2DataType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Ion mode", IonMode.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Target omics", TargetOmics.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Ionization", Ionization.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Machine category", MachineCategory.ToString() }));

            pStrings.Add(String.Join(": ", new string[] { "Instrument type", InstrumentType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Instrument", Instrument.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Authors", Authors.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "License", License.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Comment", Comment.ToString() }));

            pStrings.Add(String.Join(": ", new string[] { "Msp file path", MspFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Text DB file path", TextDBFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Isotope text DB file path", IsotopeTextDBFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Compounds library file path for target detection", CompoundListInTargetModePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Compounds library file path for RT correction", CompoundListForRtCorrectionPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Searched adduct ions", String.Join(",", SearchedAdductIons.Select(n => n.AdductIonName).ToArray()) }));

            pStrings.Add("\r\n");
            pStrings.Add("# FileID ClassName");
            foreach (var item in FileID_ClassName) pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, item.Value }));
           
            pStrings.Add("\r\n");
            pStrings.Add("# FileID AnalysisFileType");
            foreach (var item in FileID_AnalysisFileType) pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, item.Value.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Classname order");
            foreach (var item in ClassnameToOrder) pStrings.Add(String.Join(": ", new string[] { "Class name=" + item.Key, item.Value.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Classname ColorBytes");
            foreach (var item in ClassnameToColorBytes) pStrings.Add(String.Join(": ", new string[] { "Class name=" + item.Key, String.Join(",", item.Value.ToArray()) }));

            pStrings.Add("\r\n");
            pStrings.Add("# Export");
            pStrings.Add(String.Join(": ", new string[] { "Export spectra file format", ExportSpectraFileFormat.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Export spectra type", ExportSpectraType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Mat file export folder path", MatExportFolderPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Export folder path", ExportFolderPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Height matrix export", IsHeightMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Normalized height matrix export", IsNormalizedMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Representative spectra export", IsRepresentativeSpectraExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Peak ID matrix export", IsPeakIdMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention time matrix export", IsRetentionTimeMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Mass matrix export", IsMassMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MSMS included matrix export", IsMsmsIncludedMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Unique mass matrix export", IsUniqueMsMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Peak area matrix export", IsPeakAreaMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Parameter export", IsParameterExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "GNPS export", IsGnpsExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Molecular networking export", IsMolecularNetworkingExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "SN matrix export", IsSnMatrixExport.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Export as mztabM format", IsExportedAsMzTabM.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Process parameters");
            pStrings.Add(String.Join(": ", new string[] { "Process option", ProcessOption.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Number of threads", NumThreads.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Feature detection parameters");
            pStrings.Add(String.Join(": ", new string[] { "Smoothing method", SmoothingMethod.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Smoothing level", SmoothingLevel.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Minimum peak height", MinimumAmplitude.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Minimum peak width", MinimumDatapoints.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Average peak width", AveragePeakWidth.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Mass slice width", MassSliceWidth.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention time begin", RetentionTimeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention time end", RetentionTimeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 mass range begin", MassRangeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 mass range end", MassRangeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS2 mass range begin", Ms2MassRangeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS2 mass range end", Ms2MassRangeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 tolerance for centroid", CentroidMs1Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS2 tolerance for centroid", CentroidMs2Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accuracy type", AccuracyType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Max charge number", MaxChargeNumber.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Considering Br and Cl for isotopes", IsBrClConsideredForIsotopes.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Exclude mass list", 
                String.Join(";", ExcludedMassList.Select(n => String.Join(" ", new string[] { n.Mass.ToString(), n.MassTolerance.ToString() })))}));

            pStrings.Add("\r\n");
            pStrings.Add("# Deconvolution");
            pStrings.Add(String.Join(": ", new string[] { "Sigma window value", SigmaWindowValue.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Amplitude cut off", AmplitudeCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep isotope range", KeptIsotopeRange.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Exclude after precursor", RemoveAfterPrecursor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep original precursor isotopes", KeepOriginalPrecursorIsotopes.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# MSP-based annotation");
            pStrings.Add(String.Join(": ", new string[] { "RT tolerance for MSP-based annotation", MspSearchParam.RtTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RI tolerance for MSP-based annotation", MspSearchParam.RiTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CCS tolerance for MSP-based annotation", MspSearchParam.CcsTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Mass range begin for MSP-based annotation", MspSearchParam.MassRangeBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Mass range end for MSP-based annotation", MspSearchParam.MassRangeEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Relative amplitude cutoff for MSP-based annotation", MspSearchParam.RelativeAmpCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Absolute amplitude cutoff for MSP-based annotation", MspSearchParam.AbsoluteAmpCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Weighted dot product cutoff", MspSearchParam.WeightedDotProductCutOff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Simple dot product cutoff", MspSearchParam.SimpleDotProductCutOff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Reverse dot product cutoff", MspSearchParam.ReverseDotProductCutOff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Matched peaks percentage cutoff", MspSearchParam.MatchedPeaksPercentageCutOff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Minimum spectrum match", MspSearchParam.MinimumSpectrumMatch.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Total score cutoff for MSP-based annotation", MspSearchParam.TotalScoreCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 tolerance for MSP-based annotation", MspSearchParam.Ms1Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS2 tolerance for MSP-based annotation", MspSearchParam.Ms2Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation scoring", MspSearchParam.IsUseTimeForAnnotationScoring.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation filtering", MspSearchParam.IsUseTimeForAnnotationFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use CCS for MSP-based annotation scoring", MspSearchParam.IsUseCcsForAnnotationScoring.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use CCS for MSP-based annotation filtering", MspSearchParam.IsUseCcsForAnnotationFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Only report top hit for MSP-based annotation", OnlyReportTopHitInMspSearch.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Execute annotation process only for alignment file", IsIdentificationOnlyPerformedForAlignmentFile.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Solvent type", LipidQueryContainer.SolventType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Searched lipid class",  
                String.Join(";", LipidQueryContainer.LbmQueries.Where(n => n.IsSelected).Select(n => String.Join(" ", new string[] { n.LbmClass.ToString(), n.AdductType.AdductIonName }).ToArray())) }));

            pStrings.Add("\r\n");
            pStrings.Add("# Text-based annotation");
            pStrings.Add(String.Join(": ", new string[] { "RT tolerance for Text-based annotation", TextDbSearchParam.RtTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RI tolerance for Text-based annotation", TextDbSearchParam.RiTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CCS tolerance for Text-based annotation", TextDbSearchParam.CcsTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Total score cutoff for Text-based annotation", TextDbSearchParam.TotalScoreCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Accurate ms1 tolerance for Text-based annotation", TextDbSearchParam.Ms1Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for Text-based annotation scoring", TextDbSearchParam.IsUseTimeForAnnotationScoring.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use retention information for Text-based annotation filtering", TextDbSearchParam.IsUseTimeForAnnotationFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use CCS for Text-based annotation scoring", TextDbSearchParam.IsUseCcsForAnnotationScoring.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Use CCS for Text-based annotation filtering", TextDbSearchParam.IsUseCcsForAnnotationFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Only report top hit for Text-based annotation", OnlyReportTopHitInTextDBSearch.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Retention index dictionary information");
            foreach (var item in FileIdRiInfoDictionary) pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, item.Value.DictionaryFilePath }));

            pStrings.Add("\r\n");
            pStrings.Add("# Alignment parameters");
            pStrings.Add(String.Join(": ", new string[] { "Alignment reference file ID", AlignmentReferenceFileID.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention time tolerance for alignment", RetentionTimeAlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Retention time factor for alignment", RetentionTimeAlignmentFactor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Spectrum similarity tolerance for alignment", SpectrumSimilarityAlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Spectrum similarity factor for alignment", SpectrumSimilarityAlignmentFactor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 tolerance for alignment", Ms1AlignmentTolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS1 factor for alignment", Ms1AlignmentFactor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Force insert peaks in gap filling", IsForceInsertForGapFilling.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Filtering");
            pStrings.Add(String.Join(": ", new string[] { "Peak count filter", PeakCountFilter.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "N percent detected in one group", NPercentDetectedInOneGroup.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Remove feature based on peak height fold-change", IsRemoveFeatureBasedOnBlankPeakHeightFoldChange.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Blank filtering", BlankFiltering.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Sample max / blank average", SampleMaxOverBlankAverage.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Sample average / blank average", SampleAverageOverBlankAverage.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep reference matched metabolites", IsKeepRefMatchedMetaboliteFeatures.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep suggested metabolites", IsKeepSuggestedMetaboliteFeatures.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep removable features and assigned tag for checking", IsKeepRemovableFeaturesAndAssignedTagForChecking.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Replace true zero values with 1/2 of minimum peak height over all samples", IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Retention time correction");
            pStrings.Add(String.Join(": ", new string[] { "Execute RT correction", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExcuteRtCorrection.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RT correction with smoothing for RT diff", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.doSmoothing.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "User setting intercept", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.UserSettingIntercept.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "RT diff calc method", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.RtDiffCalcMethod.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Interpolation method", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.InterpolationMethod.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Extrapolation method (begin)", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodBegin.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Extrapolation method (end)", RetentionTimeCorrectionCommon.RetentionTimeCorrectionParam.ExtrapolationMethodEnd.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Internal standards for RT alignment", String.Join(",", RetentionTimeCorrectionCommon.StandardLibrary.Where(n => n.IsTargetMolecule).Select(n => n.Name).ToArray()) }));

            pStrings.Add("\r\n");
            pStrings.Add("# Isotope tracking setting");
            pStrings.Add(String.Join(": ", new string[] { "Tracking isotope label", TrackingIsotopeLabels.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Set fully labeled reference file", SetFullyLabeledReferenceFile.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Non labeled reference ID", NonLabeledReferenceID.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Fully labeled reference ID", FullyLabeledReferenceID.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Isotope tracking dictionary ID", IsotopeTrackingDictionary.SelectedID.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# CorrDec settings");
            pStrings.Add(String.Join(": ", new string[] { "CorrDec execute", CorrDecParam.CanExcute.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec MS2 tolerance", CorrDecParam.MS2Tolerance.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec minimum MS2 peak height", CorrDecParam.MinMS2Intensity.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec minimum number of detected samples", CorrDecParam.MinNumberOfSample.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec exclude highly correlated spots", CorrDecParam.MinCorr_MS1.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec minimum correlation coefficient (MS2)", CorrDecParam.MinCorr_MS2.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec margin 1 (target precursor)", CorrDecParam.CorrDiff_MS1.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec margin 2 (coeluted precursor)", CorrDecParam.CorrDiff_MS2.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec minimum detected rate", CorrDecParam.MinDetectedPercentToVisualize.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec minimum MS2 relative intensity", CorrDecParam.MinMS2RelativeIntensity.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "CorrDec remove peaks larger than precursor", CorrDecParam.RemoveAfterPrecursor.ToString() }));

            return pStrings;
        }
    }



    [MessagePackObject]
    public class RiDictionaryInfo {

        [Key(0)]
        public string DictionaryFilePath { get; set; } = string.Empty;
        [Key(1)]
        public Dictionary<int, float> RiDictionary { get; set; } = new Dictionary<int, float>(); // int: carbon number, float: retention time
    }

    [MessagePackObject]
    public class IsotopeTrackingDictionary {
       
        public IsotopeTrackingDictionary() {
            IsotopeElements = new List<IsotopeElement>() {
                new IsotopeElement() { ElementName = "13C", MassDifference = 1.003354838F },
                new IsotopeElement() { ElementName = "15N", MassDifference = 0.997034893F },
                new IsotopeElement() { ElementName = "34S", MassDifference = 1.9957959F },
                new IsotopeElement() { ElementName = "18O", MassDifference = 1.9979535F },
                new IsotopeElement() { ElementName = "13C+15N", MassDifference = 2.000389731F },
                new IsotopeElement() { ElementName = "13C+34S", MassDifference = 2.999150738F },
                new IsotopeElement() { ElementName = "15N+34S", MassDifference = 2.992830793F },
                new IsotopeElement() { ElementName = "13C+15S+34S", MassDifference = 3.996185631F },
                new IsotopeElement() { ElementName = "Deuterium", MassDifference = 1.006276745F }
            };
        }

        [Key(0)]
        public List<IsotopeElement> IsotopeElements { get; set; } = new List<IsotopeElement>();
        [Key(1)]
        public int SelectedID { get; set; } = 0;
    }

    [MessagePackObject]
    public class IsotopeElement {
        [Key(0)]
        public string ElementName { get; set; } = string.Empty;
        [Key(1)]
        public float MassDifference { get; set; } = 0.0F;
    }

    [MessagePackObject]
    public class CorrDecParam {
        [Key(0)]
        public int MinMS2Intensity { get; set; } = 1000;
        [Key(1)]
        public float MS2Tolerance { get; set; } = 0.01f;
        [Key(2)]
        public float MinCorr_MS1 { get; set; } = 0.9f;
        [Key(3)]
        public float MinCorr_MS2 { get; set; } = 0.7f;
        [Key(4)]
        public float CorrDiff_MS1 { get; set; } = 0.2f;
        [Key(5)]
        public float CorrDiff_MS2 { get; set; } = 0.1f;
        [Key(6)]
        public float MinDetectedPercentToVisualize { get; set; } = 0.5f;
        [Key(7)]
        public bool RemoveAfterPrecursor { get; set; } = true;
        [Key(8)]
        public int MinNumberOfSample { get; set; } = 3;
        [Key(9)]
        public float MinMS2RelativeIntensity { get; set; } = 2;
        [Key(10)]
        public bool CanExcute { get; set; } = true;
        public CorrDecParam() { }
    }
}
