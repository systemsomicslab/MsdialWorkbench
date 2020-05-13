using CompMs.Common.Components;
using CompMs.Common.Enum;
using CompMs.Common.Parameter;
using CompMs.Common.Query;
using CompMs.MsdialCore.DataObj;
using System;
using System.Collections.Generic;

namespace CompMs.MsdialCore.Parameter {
    public class ParameterBase {

        public DateTime ProjectDate { get; set; }
        public DateTime FinalSavedDate { get; set; }
        public string MsdialVersionNumber { get; set; }


        // Project container
        public string ProjectFolderPath { get; set; }
        public string ProjectFilePath { get; set; }
        public Dictionary<int, string> FileID_ClassName { get; set; }
        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType { get; set; }
        public bool IsBoxPlotForAlignmentResult { get; set; }
        public Dictionary<string, int> ClassnameToOrder { get; set; }
        public Dictionary<string, List<byte>> ClassnameToColorBytes { get; set; }

        // Project type
        public MethodType MethodType { get; set; } = MethodType.ddMSMS;
        public DataType DataType { get; set; } = DataType.Profile;
        public DataType DataTypeMS2 { get; set; } = DataType.Profile;
        public IonMode IonMode { get; set; } = IonMode.Positive;
        public TargetOmics TargetOmics { get; set; } = TargetOmics.Metablomics;
        public Ionization Ionization { get; set; } = Ionization.ESI;
        public SeparationType SeparationType { get; set; } = SeparationType.Chromatography;



        // Project metadata
        public string InstrumentType { get; set; } = string.Empty;
        public string Instrument { get; set; } = string.Empty;
        public string Authors { get; set; } = string.Empty;
        public string License { get; set; } = string.Empty;
        public string Comment { get; set; } = string.Empty;

        // Annotation
        public string MspFilePath { get; set; } = string.Empty;
        public string TextDBFilePath { get; set; } = string.Empty;
        public string IsotopeTextDBFilePath { get; set; } = string.Empty;
        public string CompoundListInTargetModePath { get; set; } = string.Empty;

        // Export
        public ExportSpectraFileFormat ExportSpectraFileFormat { get; set; } = ExportSpectraFileFormat.msp;
        public ExportspectraType ExportSpectraType { get; set; } = ExportspectraType.deconvoluted;
        public string MatExportFolderPath { get; set; } = string.Empty;
        public string ExportFolderPath { get; set; } = string.Empty;


        public bool IsHeightMatrixExport { get; set; } = true;
        public bool IsNormalizedMatrixExport { get; set; }
        public bool IsRepresentativeSpectraExport { get; set; }
        public bool IsPeakIdMatrixExport { get; set; }
        public bool IsRetentionTimeMatrixExport { get; set; }
        public bool IsMassMatrixExport { get; set; }
        public bool IsMsmsIncludedMatrixExport { get; set; }
        
        public bool IsUniqueMsMatrixExport { get; set; }
        public bool IsPeakAreaMatrixExport { get; set; }
        public bool IsParameterExport { get; set; }
        public bool IsGnpsExport { get; set; }
        public bool IsMolecularNetworkingExport { get; set; }
        public bool IsSnMatrixExport { get; set; }
        public bool IsExportedAsMzTabM { get; set; }

        // Other


        // Process parameters
        public ProcessOption ProcessOption { get; set; } = ProcessOption.All;
        public int NumThreads { get; set; } = 2;

        // feature detection base
        public SmoothingMethod SmoothingMethod { get; set; } = SmoothingMethod.LinearWeightedMovingAverage;
        public int SmoothingLevel { get; set; } = 3;
        public double MinimumAmplitude { get; set; } = 1000;
        public double MinimumDatapoints { get; set; } = 5;
        public float MassSliceWidth { get; set; } = 0.1F;
        public float RetentionTimeBegin { get; set; } = 0;
        public float RetentionTimeEnd { get; set; } = 100;
        public float MassRangeBegin { get; set; } = 0;
        public float MassRangeEnd { get; set; } = 2000;
        public float Ms2MassRangeBegin { get; set; } = 0;
        public float Ms2MassRangeEnd { get; set; } = 2000;
        public float CentroidMs1Tolerance { get; set; } = 0.01F;
        public float CentroidMs2Tolerance { get; set; } = 0.025F;
        public int MaxChargeNumber { get; set; } = 2;
        public bool IsBrClConsideredForIsotopes { get; set; } = false;
        public List<MzSearchQuery> ExcludedMassList { get; set; } = new List<MzSearchQuery>();


        // alignment base
        public int AlignmentReferenceFileID { get; set; } = 0;
        public float RetentionTimeAlignmentFactor { get; set; } = 0.5F;
        public float Ms1AlignmentFactor { get; set; } = 0.5F;
        public float SpectrumSimilarityAlignmentFactor { get; set; } = 59F;
        public float Ms1AlignmentTolerance { get; set; } = 0.015F;
        public float RetentionTimeAlignmentTolerance { get; set; } = 0.05F;
        public float SpectrumSimilarityAlignmentTolerance { get; set; } = 0.8F;
        public float AlignmentScoreCutOff { get; set; } = 50;
        public bool TogetherWithAlignment { get; set; } = true;


        // spectral library search
        public LipidQueryBean LipidQueryContainer { get; set; } = new LipidQueryBean() { SolventType = SolventType.CH3COONH4 };
        public MsRefSearchParameterBase MspSearchParam { get; set; } = new MsRefSearchParameterBase();

        public bool IsUseTimeForAnnotationFiltering { get; set; } = false;
        public bool IsUseTimeForAnnotationScoring { get; set; } = false;
        public bool IsUseCcsForAnnotationFiltering { get; set; } = false;
        public bool IsUseCcsForAnnotationScoring { get; set; } = false;

        public bool OnlyReportTopHitInMspSearch { get; set; } = false;
        public MsRefSearchParameterBase TextDbSearchParam { get; set; } = new MsRefSearchParameterBase() {
            RtTolerance = 0.1F, Ms1Tolerance = 0.01F, TotalScoreCutoff = 85
        };
        public bool OnlyReportTopHitInTextDBSearch { get; set; } = false;
        public float RelativeAbundanceCutOff { get; set; } = 0.0F;
        public bool IsIdentificationOnlyPerformedForAlignmentFile { get; set; } = false;

        // deconvolution
        public float SigmaWindowValue { get; set; } = 0.5F;
        public float AmplitudeCutoff { get; set; } = 0;
        public float AveragePeakWidth { get; set; } = 30;
        public bool RemoveAfterPrecursor { get; set; } = true;
        public bool KeepOriginalPrecursorIsotopes { get; set; } = false;
        public AccuracyType AccuracyType { get; set; } = AccuracyType.IsAccurate;


        // Post-alignment and filtering
        public float PeakCountFilter { get; set; } = 0;
        public bool IsForceInsertForGapFilling { get; set; } = true;
        public float NPercentDetectedInOneGroup { get; set; } = 0;
        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange { get; set; } = false;
        public float SampleMaxOverBlankAverage { get; set; } = 5;
        public float SampleAverageOverBlankAverage { get; set; } = 5;
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking { get; set; } = true;
        public bool IsKeepIdentifiedMetaboliteFeatures { get; set; } = true;
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples { get; set; } = false;
        public BlankFiltering BlankFiltering { get; set; } = BlankFiltering.SampleMaxOverBlankAve;
        public bool IsKeepAnnotatedMetaboliteFeatures { get; set; } = false;
        public float FoldChangeForBlankFiltering { get; set; } = 5;


        // Normalization option
        public bool IsNormalizeNone { get; set; } = true;
        public bool IsNormalizeIS { get; set; }
        public bool IsNormalizeLowess { get; set; }
        public bool IsNormalizeIsLowess { get; set; }
        public bool IsNormalizeTic { get; set; }
        public bool IsNormalizeMTic { get; set; }
        public bool IsNormalizeSplash { get; set; }

        public double LowessSpan { get; set; }
        public bool IsBlankSubtract { get; set; }

        // Statistics
        public TransformMethod Transform { get; set; } = TransformMethod.None;
        public ScaleMethod Scale { get; set; } = ScaleMethod.AutoScale;
        public int MaxComponent { get; set; } = 2;

        public TransformMethod TransformPls { get; set; } = TransformMethod.None;
        public ScaleMethod ScalePls { get; set; } = ScaleMethod.AutoScale;
        public bool IsAutoFitPls { get; set; } = true;
        public int ComponentPls { get; set; } = 2;

        public MultivariateAnalysisOption MultivariateAnalysisOption { get; set; } = MultivariateAnalysisOption.Pca;
        public bool IsIdentifiedImportedInStatistics { get; set; } = true;
        public bool IsAnnotatedImportedInStatistics { get; set; }
        public bool IsUnknownImportedInStatistics { get; set; }

        // Mrmprobs export
        public float MpMs1Tolerance { get; set; } = 0.005F;
        public float MpMs2Tolerance { get; set; } = 0.01F;
        public float MpRtTolerance { get; set; } = 0.5F;
        public int MpTopN { get; set; } = 5;
        public bool MpIsIncludeMsLevel1 { get; set; } = true;
        public bool MpIsUseMs1LevelForQuant { get; set; } = false;
        public bool MpIsFocusedSpotOutput { get; set; } = false;
        public bool MpIsReferenceBaseOutput { get; set; } = true;
        public bool MpIsExportOtherCandidates { get; set; } = false;
        public float MpIdentificationScoreCutOff { get; set; } = 80F;


        // molecular networking
        public double MnRtTolerance { get; set; } = 100;
        public double MnIonCorrelationSimilarityCutOff { get; set; } = 95;
        public double MnSpectrumSimilarityCutOff { get; set; } = 75;
        public double MnRelativeAbundanceCutOff { get; set; } = 1;
        public double MnMassTolerance { get; set; } = 0.025;
        public bool MnIsExportIonCorrelation { get; set; } = false;


        // others
        public RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon { get; set; } = new RetentionTimeCorrectionCommon();
        public List<MoleculeMsReference> CompoundListInTargetMode { get; set; } = null;
        public List<StandardCompound> StandardCompounds { get; set; } = null;

    }
}
