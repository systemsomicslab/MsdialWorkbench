using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Query;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;
using MessagePack;

namespace CompMs.MsdialCore.Parameter {

    [MessagePackObject]
    public class ParameterBase {

        [Key(0)]
        public DateTime ProjectDate { get; set; }
        [Key(1)]
        public DateTime FinalSavedDate { get; set; }
        [Key(2)]
        public string MsdialVersionNumber { get; set; }


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
        public MethodType MethodType { get; set; } = MethodType.ddMSMS;
        [Key(11)]
        public DataType DataType { get; set; } = DataType.Profile;
        [Key(12)]
        public DataType DataTypeMS2 { get; set; } = DataType.Profile;
        [Key(13)]
        public IonMode IonMode { get; set; } = IonMode.Positive;
        [Key(14)]
        public TargetOmics TargetOmics { get; set; } = TargetOmics.Metablomics;
        [Key(15)]
        public Ionization Ionization { get; set; } = Ionization.ESI;
        [Key(16)]
        public SeparationType SeparationType { get; set; } = SeparationType.Chromatography;
        [Key(17)]
        public bool IsAIF { get; set; } = false;



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

        // Other


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
        public LipidQueryBean LipidQueryContainer { get; set; } = new LipidQueryBean() { SolventType = SolventType.CH3COONH4 };
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
        public bool IsKeepIdentifiedMetaboliteFeatures { get; set; } = true;
        [Key(95)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples { get; set; } = false;
        [Key(96)]
        public BlankFiltering BlankFiltering { get; set; } = BlankFiltering.SampleMaxOverBlankAve;
        [Key(97)]
        public bool IsKeepAnnotatedMetaboliteFeatures { get; set; } = false;
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

    }

    public class RiDictionaryInfo {
        
        public string DictionaryFilePath { get; set; } = string.Empty;
        public Dictionary<int, float> RiDictionary { get; set; } = new Dictionary<int, float>(); // int: carbon number, float: retention time
    }


}
