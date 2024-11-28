using CompMs.Common.Components;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Enum;
using CompMs.Common.Extension;
using CompMs.Common.Parameter;
using CompMs.Common.Parser;
using CompMs.Common.Proteomics.DataObj;
using CompMs.Common.Proteomics.Parser;
using CompMs.Common.Query;
using CompMs.Common.Utility;
using CompMs.MsdialCore.DataObj;
using CompMs.MsdialCore.Properties;
using MessagePack;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace CompMs.MsdialCore.Parameter
{

    [MessagePackObject]
    public class ParameterBase {

        [SerializationConstructor]
        public ParameterBase() {
            RefSpecMatchBaseParam = new RefSpecMatchBaseParameter(isLabUseOnly: false);
        }

        public ParameterBase(bool isLabUseOnly) {
            RefSpecMatchBaseParam = new RefSpecMatchBaseParameter(isLabUseOnly);
        }

        [Key(0)]
        public ProjectBaseParameter ProjectParam { get; set; } = new ProjectBaseParameter();
        [IgnoreMember]
        public DateTime ProjectStartDate { get => ProjectParam.ProjectStartDate; set => ProjectParam.ProjectStartDate = value; }
        [IgnoreMember]
        public DateTime FinalSavedDate { get => ProjectParam.FinalSavedDate; set => ProjectParam.FinalSavedDate = value; }
        [IgnoreMember]
        public string MsdialVersionNumber { get => ProjectParam.MsdialVersionNumber; set => ProjectParam.MsdialVersionNumber = value; }


        // Project container
        [IgnoreMember]
        public string ProjectFolderPath { get => ProjectParam.ProjectFolderPath; set => ProjectParam.ProjectFolderPath = value; }
        [IgnoreMember]
        public string ProjectFileName { get => ProjectParam.ProjectFileName; set => ProjectParam.ProjectFileName = value; }
        [IgnoreMember]
        public Dictionary<int, string> FileID_ClassName { get => ProjectParam.FileID_ClassName; set => ProjectParam.FileID_ClassName = value; }
        [IgnoreMember]
        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType { get => ProjectParam.FileID_AnalysisFileType; set => ProjectParam.FileID_AnalysisFileType = value; }
        [IgnoreMember]
        public bool IsBoxPlotForAlignmentResult { get => ProjectParam.IsBoxPlotForAlignmentResult; set => ProjectParam.IsBoxPlotForAlignmentResult = value; }
        [IgnoreMember]
        public Dictionary<string, int> ClassnameToOrder { get => ProjectParam.ClassnameToOrder; set => ProjectParam.ClassnameToOrder = value; }
        [IgnoreMember]
        public Dictionary<string, List<byte>> ClassnameToColorBytes { get => ProjectParam.ClassnameToColorBytes; set => ProjectParam.ClassnameToColorBytes = value; }
        [IgnoreMember]
        public Dictionary<string, List<byte>> SpectrumCommentToColorBytes { get => ProjectParam.SpectrumCommentToColorBytes; set => ProjectParam.SpectrumCommentToColorBytes = value; }


        // Project type
        [IgnoreMember]
        public MSDataType MSDataType { get => ProjectParam.MSDataType; set => ProjectParam.MSDataType = value; }
        [IgnoreMember]
        public MSDataType MS2DataType { get => ProjectParam.MS2DataType; set => ProjectParam.MS2DataType = value; }
        [IgnoreMember]
        public IonMode IonMode { get => ProjectParam.IonMode; set => ProjectParam.IonMode = value; }
        [IgnoreMember]
        public TargetOmics TargetOmics { get => ProjectParam.TargetOmics; set => ProjectParam.TargetOmics = value; }
        [IgnoreMember]
        public Ionization Ionization { get => ProjectParam.Ionization; set => ProjectParam.Ionization = value; }
        [IgnoreMember]
        public MachineCategory MachineCategory { get => ProjectParam.MachineCategory; set => ProjectParam.MachineCategory = value; }

        // Project metadata
        [IgnoreMember]
        public string CollisionEnergy { get => ProjectParam.CollisionEnergy; set => ProjectParam.CollisionEnergy = value; }
        [IgnoreMember]
        public string InstrumentType { get => ProjectParam.InstrumentType; set => ProjectParam.InstrumentType = value; }
        [IgnoreMember]
        public string Instrument { get => ProjectParam.Instrument; set => ProjectParam.Instrument = value; }
        [IgnoreMember]
        public string Authors { get => ProjectParam.Authors; set => ProjectParam.Authors = value; }
        [IgnoreMember]
        public string License { get => ProjectParam.License; set => ProjectParam.License = value; }
        [IgnoreMember]
        public string Comment { get => ProjectParam.Comment; set => ProjectParam.Comment = value; }


        // Annotation
        [Key(1)]
        public ReferenceBaseParameter ReferenceFileParam { get; set; } = new ReferenceBaseParameter();
        [IgnoreMember]
        public string MspFilePath { get => ReferenceFileParam.MspFilePath; set => ReferenceFileParam.MspFilePath = value; }
        [IgnoreMember]
        public string LbmFilePath { get => ReferenceFileParam.LbmFilePath; set => ReferenceFileParam.LbmFilePath = value; }
        [IgnoreMember]
        public string TextDBFilePath { get => ReferenceFileParam.TextDBFilePath; set => ReferenceFileParam.TextDBFilePath = value; }
        [IgnoreMember]
        public string IsotopeTextDBFilePath { get => ReferenceFileParam.IsotopeTextDBFilePath; set => ReferenceFileParam.IsotopeTextDBFilePath = value; }
        [IgnoreMember]
        public string CompoundListInTargetModePath { get => ReferenceFileParam.CompoundListInTargetModePath; set => ReferenceFileParam.CompoundListInTargetModePath = value; }
        [IgnoreMember]
        public string CompoundListForRtCorrectionPath { get => ReferenceFileParam.CompoundListForRtCorrectionPath; set => ReferenceFileParam.CompoundListForRtCorrectionPath = value; }
        [IgnoreMember]
        public List<AdductIon> SearchedAdductIons { get => ReferenceFileParam.SearchedAdductIons; set => ReferenceFileParam.SearchedAdductIons = value; }

        // Export
        [Key(2)]
        public DataExportBaseParameter DataExportParam { get; set; } = new DataExportBaseParameter();
        [IgnoreMember]
        public ExportSpectraFileFormat ExportSpectraFileFormat { get => DataExportParam.ExportSpectraFileFormat; set => DataExportParam.ExportSpectraFileFormat = value; }
        [IgnoreMember]
        public ExportspectraType ExportSpectraType { get => DataExportParam.ExportSpectraType; set => DataExportParam.ExportSpectraType = value; }
        [IgnoreMember]
        public string MatExportFolderPath { get => DataExportParam.MatExportFolderPath; set => DataExportParam.MatExportFolderPath = value; }
        [IgnoreMember]
        public string ExportFolderPath { get => DataExportParam.ExportFolderPath; set => DataExportParam.ExportFolderPath = value; }
        [IgnoreMember]
        public bool IsHeightMatrixExport { get => DataExportParam.IsHeightMatrixExport; set => DataExportParam.IsHeightMatrixExport = value; }
        [IgnoreMember]
        public bool IsNormalizedMatrixExport { get => DataExportParam.IsNormalizedMatrixExport; set => DataExportParam.IsNormalizedMatrixExport = value; }
        [IgnoreMember]
        public bool IsRepresentativeSpectraExport { get => DataExportParam.IsRepresentativeSpectraExport; set => DataExportParam.IsRepresentativeSpectraExport = value; }
        [IgnoreMember]
        public bool IsPeakIdMatrixExport { get => DataExportParam.IsPeakIdMatrixExport; set => DataExportParam.IsPeakIdMatrixExport = value; }
        [IgnoreMember]
        public bool IsRetentionTimeMatrixExport { get => DataExportParam.IsRetentionTimeMatrixExport; set => DataExportParam.IsRetentionTimeMatrixExport = value; }
        [IgnoreMember]
        public bool IsMassMatrixExport { get => DataExportParam.IsMassMatrixExport; set => DataExportParam.IsMassMatrixExport = value; }
        [IgnoreMember]
        public bool IsMsmsIncludedMatrixExport { get => DataExportParam.IsMsmsIncludedMatrixExport; set => DataExportParam.IsMsmsIncludedMatrixExport = value; }

        [IgnoreMember]
        public bool IsUniqueMsMatrixExport { get => DataExportParam.IsUniqueMsMatrixExport; set => DataExportParam.IsUniqueMsMatrixExport = value; }
        [IgnoreMember]
        public bool IsPeakAreaMatrixExport { get => DataExportParam.IsPeakAreaMatrixExport; set => DataExportParam.IsPeakAreaMatrixExport = value; }
        [IgnoreMember]
        public bool IsParameterExport { get => DataExportParam.IsParameterExport; set => DataExportParam.IsParameterExport = value; }
        [IgnoreMember]
        public bool IsGnpsExport { get => DataExportParam.IsGnpsExport; set => DataExportParam.IsGnpsExport = value; }
        [IgnoreMember]
        public bool IsMolecularNetworkingExport { get => DataExportParam.IsMolecularNetworkingExport; set => DataExportParam.IsMolecularNetworkingExport = value; }
        [IgnoreMember]
        public bool IsSnMatrixExport { get => DataExportParam.IsSnMatrixExport; set => DataExportParam.IsSnMatrixExport = value; }
        [IgnoreMember]
        public bool IsExportedAsMzTabM { get => DataExportParam.IsExportedAsMzTabM; set => DataExportParam.IsExportedAsMzTabM = value; }



        // Process parameters
        [Key(3)]
        public ProcessBaseParameter ProcessBaseParam { get; set; } = new ProcessBaseParameter();
        [IgnoreMember]
        public ProcessOption ProcessOption { get => ProcessBaseParam.ProcessOption; set => ProcessBaseParam.ProcessOption = value; }

        [IgnoreMember]
        // for advanced settings; ignore max ProcessorCount
        public int NumThreads { get => ProcessBaseParam.NumThreads; set => ProcessBaseParam.NumThreads = value; }


        // feature detection base
        [Key(4)]
        public PeakPickBaseParameter PeakPickBaseParam { get; set; } = new PeakPickBaseParameter();

        [IgnoreMember]
        public SmoothingMethod SmoothingMethod { get => PeakPickBaseParam.SmoothingMethod; set => PeakPickBaseParam.SmoothingMethod = value; }
        [IgnoreMember]
        public int SmoothingLevel { get => PeakPickBaseParam.SmoothingLevel; set => PeakPickBaseParam.SmoothingLevel = value; }
        [IgnoreMember]
        public double MinimumAmplitude { get => PeakPickBaseParam.MinimumAmplitude; set => PeakPickBaseParam.MinimumAmplitude = value; }
        [IgnoreMember]
        public double MinimumDatapoints { get => PeakPickBaseParam.MinimumDatapoints; set => PeakPickBaseParam.MinimumDatapoints = value; }
        [IgnoreMember]
        public float MassSliceWidth { get => PeakPickBaseParam.MassSliceWidth; set => PeakPickBaseParam.MassSliceWidth = value; }
        [IgnoreMember]
        public float RetentionTimeBegin { get => PeakPickBaseParam.RetentionTimeBegin; set => PeakPickBaseParam.RetentionTimeBegin = value; }
        [IgnoreMember]
        public float RetentionTimeEnd { get => PeakPickBaseParam.RetentionTimeEnd; set => PeakPickBaseParam.RetentionTimeEnd = value; }
        [IgnoreMember]
        public float MassRangeBegin { get => PeakPickBaseParam.MassRangeBegin; set => PeakPickBaseParam.MassRangeBegin = value; }
        [IgnoreMember]
        public float MassRangeEnd { get => PeakPickBaseParam.MassRangeEnd; set => PeakPickBaseParam.MassRangeEnd = value; }
        [IgnoreMember]
        public float Ms2MassRangeBegin { get => PeakPickBaseParam.Ms2MassRangeBegin; set => PeakPickBaseParam.Ms2MassRangeBegin = value; }
        [IgnoreMember]
        public float Ms2MassRangeEnd { get => PeakPickBaseParam.Ms2MassRangeEnd; set => PeakPickBaseParam.Ms2MassRangeEnd = value; }
        [IgnoreMember]
        public float CentroidMs1Tolerance { get => PeakPickBaseParam.CentroidMs1Tolerance; set => PeakPickBaseParam.CentroidMs1Tolerance = value; }
        [IgnoreMember]
        public float CentroidMs2Tolerance { get => PeakPickBaseParam.CentroidMs2Tolerance; set => PeakPickBaseParam.CentroidMs2Tolerance = value; }
        [IgnoreMember]
        public int MaxChargeNumber { get => PeakPickBaseParam.MaxChargeNumber; set => PeakPickBaseParam.MaxChargeNumber = value; }
        [IgnoreMember]
        public bool IsBrClConsideredForIsotopes { get => PeakPickBaseParam.IsBrClConsideredForIsotopes; set => PeakPickBaseParam.IsBrClConsideredForIsotopes = value; }
        [IgnoreMember]
        public List<MzSearchQuery> ExcludedMassList { get => PeakPickBaseParam.ExcludedMassList; set => PeakPickBaseParam.ExcludedMassList = value; }



        // alignment base
        [Key(5)]
        public AlignmentBaseParameter AlignmentBaseParam { get; set; } = new AlignmentBaseParameter();
        [IgnoreMember]
        public int AlignmentReferenceFileID { get => AlignmentBaseParam.AlignmentReferenceFileID; set => AlignmentBaseParam.AlignmentReferenceFileID = value; }
        [IgnoreMember]
        public float RetentionTimeAlignmentFactor { get => AlignmentBaseParam.RetentionTimeAlignmentFactor; set => AlignmentBaseParam.RetentionTimeAlignmentFactor = value; }
        [IgnoreMember]
        public float Ms1AlignmentFactor { get => AlignmentBaseParam.Ms1AlignmentFactor; set => AlignmentBaseParam.Ms1AlignmentFactor = value; }
        [IgnoreMember]
        public float SpectrumSimilarityAlignmentFactor { get => AlignmentBaseParam.SpectrumSimilarityAlignmentFactor; set => AlignmentBaseParam.SpectrumSimilarityAlignmentFactor = value; }
        [IgnoreMember]
        public float Ms1AlignmentTolerance { get => AlignmentBaseParam.Ms1AlignmentTolerance; set => AlignmentBaseParam.Ms1AlignmentTolerance = value; }
        [IgnoreMember]
        public float RetentionTimeAlignmentTolerance { get => AlignmentBaseParam.RetentionTimeAlignmentTolerance; set => AlignmentBaseParam.RetentionTimeAlignmentTolerance = value; }
        [IgnoreMember]
        public float SpectrumSimilarityAlignmentTolerance { get => AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance; set => AlignmentBaseParam.SpectrumSimilarityAlignmentTolerance = value; }
        [IgnoreMember]
        public float AlignmentScoreCutOff { get => AlignmentBaseParam.AlignmentScoreCutOff; set => AlignmentBaseParam.AlignmentScoreCutOff = value; }
        [IgnoreMember]
        public bool TogetherWithAlignment { get => AlignmentBaseParam.TogetherWithAlignment; set => AlignmentBaseParam.TogetherWithAlignment = value; }

        // spectral library search
        [Key(6)]
        public RefSpecMatchBaseParameter RefSpecMatchBaseParam { get; set; } = new RefSpecMatchBaseParameter();

        [IgnoreMember]
        public LipidQueryBean LipidQueryContainer { get => RefSpecMatchBaseParam.LipidQueryContainer; set => RefSpecMatchBaseParam.LipidQueryContainer = value; }
        [IgnoreMember]
        public MsRefSearchParameterBase MspSearchParam { get => RefSpecMatchBaseParam.MspSearchParam; set => RefSpecMatchBaseParam.MspSearchParam = value; }
        [IgnoreMember]
        public bool OnlyReportTopHitInMspSearch { get => RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch; set => RefSpecMatchBaseParam.OnlyReportTopHitInMspSearch = value; }
        [IgnoreMember]
        public MsRefSearchParameterBase TextDbSearchParam { get => RefSpecMatchBaseParam.TextDbSearchParam; set => RefSpecMatchBaseParam.TextDbSearchParam = value; }
        [IgnoreMember]
        public bool OnlyReportTopHitInTextDBSearch { get => RefSpecMatchBaseParam.OnlyReportTopHitInTextDBSearch; set => RefSpecMatchBaseParam.OnlyReportTopHitInTextDBSearch = value; }
        [IgnoreMember]
        public bool IsIdentificationOnlyPerformedForAlignmentFile { get => RefSpecMatchBaseParam.IsIdentificationOnlyPerformedForAlignmentFile; set => RefSpecMatchBaseParam.IsIdentificationOnlyPerformedForAlignmentFile = value; }
        [IgnoreMember]
        public Dictionary<int, RiDictionaryInfo> FileIdRiInfoDictionary { get => RefSpecMatchBaseParam.FileIdRiInfoDictionary; set => RefSpecMatchBaseParam.FileIdRiInfoDictionary = value; }
        [IgnoreMember]
        public MsRefSearchParameterBase LbmSearchParam { get => RefSpecMatchBaseParam.LbmSearchParam; set => RefSpecMatchBaseParam.LbmSearchParam = value; }

        // deconvolution
        [Key(7)]
        public ChromDecBaseParameter ChromDecBaseParam { get; set; } = new ChromDecBaseParameter();
        [IgnoreMember]
        [Obsolete("Use AnalysisFileBean.IsDoMs2ChromDeconvolution")]
        public bool IsDoMs2ChromDeconvolution { get => ChromDecBaseParam.IsDoMs2ChromDeconvolution; set => ChromDecBaseParam.IsDoMs2ChromDeconvolution = value; }
        [IgnoreMember]
        public float SigmaWindowValue { get => ChromDecBaseParam.SigmaWindowValue; set => ChromDecBaseParam.SigmaWindowValue = value; }
        [IgnoreMember]
        public float AmplitudeCutoff { get => ChromDecBaseParam.AmplitudeCutoff; set => ChromDecBaseParam.AmplitudeCutoff = value; }
        [IgnoreMember]
        public float AveragePeakWidth { get => ChromDecBaseParam.AveragePeakWidth; set => ChromDecBaseParam.AveragePeakWidth = value; }
        [IgnoreMember]
        public float KeptIsotopeRange { get => ChromDecBaseParam.KeptIsotopeRange; set => ChromDecBaseParam.KeptIsotopeRange = value; }
        [IgnoreMember]
        public bool RemoveAfterPrecursor { get => ChromDecBaseParam.RemoveAfterPrecursor; set => ChromDecBaseParam.RemoveAfterPrecursor = value; }
        [IgnoreMember]
        public bool KeepOriginalPrecursorIsotopes { get => ChromDecBaseParam.KeepOriginalPrecursorIsotopes; set => ChromDecBaseParam.KeepOriginalPrecursorIsotopes = value; }
        [IgnoreMember]
        public AccuracyType AccuracyType { get => ChromDecBaseParam.AccuracyType; set => ChromDecBaseParam.AccuracyType = value; }
        [IgnoreMember]
        public double TargetCE { get => ChromDecBaseParam.TargetCE; set => ChromDecBaseParam.TargetCE = value; }

        [Key(8)]
        public ProteomicsParameter ProteomicsParam { get; set; } = new ProteomicsParameter();

        [IgnoreMember]
        public bool IsDoAndromedaMs2Deconvolution { get => ProteomicsParam.IsDoAndromedaMs2Deconvolution; set => ProteomicsParam.IsDoAndromedaMs2Deconvolution = value; }
        [IgnoreMember]
        public float AndromedaDelta { get => ProteomicsParam.AndromedaDelta; set => ProteomicsParam.AndromedaDelta = value; }
        [IgnoreMember]
        public int AndromedaMaxPeaks { get => ProteomicsParam.AndromedaMaxPeaks; set => ProteomicsParam.AndromedaMaxPeaks = value; }

        [IgnoreMember]
        public string FastaFilePath { get => ProteomicsParam.FastaFilePath; set => ProteomicsParam.FastaFilePath = value; }
        [IgnoreMember]
        public List<Modification> VariableModifications { get => ProteomicsParam.VariableModifications; set => ProteomicsParam.VariableModifications = value; }
        [IgnoreMember]
        public List<Modification> FixedModifications { get => ProteomicsParam.FixedModifications; set => ProteomicsParam.FixedModifications = value; }
        [IgnoreMember]
        public List<Enzyme> EnzymesForDigestion { get => ProteomicsParam.EnzymesForDigestion; set => ProteomicsParam.EnzymesForDigestion = value; }
        [IgnoreMember]
        public CollisionType CollistionType { get => ProteomicsParam.CollisionType; set => ProteomicsParam.CollisionType = value; }
        [IgnoreMember]
        public float FalseDiscoveryRateForPeptide { get => ProteomicsParam.FalseDiscoveryRateForPeptide; set => ProteomicsParam.FalseDiscoveryRateForPeptide = value; }
        [IgnoreMember]
        public float FalseDiscoveryRateForProtein { get => ProteomicsParam.FalseDiscoveryRateForProtein; set => ProteomicsParam.FalseDiscoveryRateForProtein = value; }
        [IgnoreMember]
        public int MaxNumberOfModificationsPerPeptide { get => ProteomicsParam.MaxNumberOfModificationsPerPeptide; set => ProteomicsParam.MaxNumberOfModificationsPerPeptide = value; }
        [IgnoreMember]
        public int MaxMissedCleavage { get => ProteomicsParam.MaxMissedCleavage; set => ProteomicsParam.MaxMissedCleavage = value; }
        [IgnoreMember]
        public int MinimumPeptideLength { get => ProteomicsParam.MinimumPeptideLength; set => ProteomicsParam.MinimumPeptideLength = value; }
        [IgnoreMember]
        public float MaxPeptideMass { get => ProteomicsParam.MaxPeptideMass; set => ProteomicsParam.MaxPeptideMass = value; }
        [IgnoreMember]
        public float MinPeptideMass { get => ProteomicsParam.MinPeptideMass; set => ProteomicsParam.MinPeptideMass = value; }

        //[IgnoreMember]
        //public MsRefSearchParameterBase ProteoMs2RefSearchParam { get => ProteomicsParam.MsRefSearchParam; set => ProteomicsParam.MsRefSearchParam = value; }

        // Post-alignment and filtering
        [Key(9)]
        public PostProcessBaseParameter PostProcessBaseParam { get; set; } = new PostProcessBaseParameter();

        [IgnoreMember]
        public float PeakCountFilter { get => PostProcessBaseParam.PeakCountFilter; set => PostProcessBaseParam.PeakCountFilter = value; }
        [IgnoreMember]
        public bool IsForceInsertForGapFilling { get => PostProcessBaseParam.IsForceInsertForGapFilling; set => PostProcessBaseParam.IsForceInsertForGapFilling = value; }
        [IgnoreMember]
        public float NPercentDetectedInOneGroup { get => PostProcessBaseParam.NPercentDetectedInOneGroup; set => PostProcessBaseParam.NPercentDetectedInOneGroup = value; }
        [IgnoreMember]
        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange { get => PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange; set => PostProcessBaseParam.IsRemoveFeatureBasedOnBlankPeakHeightFoldChange = value; }
        [IgnoreMember]
        public float SampleMaxOverBlankAverage { get => PostProcessBaseParam.SampleMaxOverBlankAverage; set => PostProcessBaseParam.SampleMaxOverBlankAverage = value; }
        [IgnoreMember]
        public float SampleAverageOverBlankAverage { get => PostProcessBaseParam.SampleAverageOverBlankAverage; set => PostProcessBaseParam.SampleAverageOverBlankAverage = value; }
        [IgnoreMember]
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking { get => PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking; set => PostProcessBaseParam.IsKeepRemovableFeaturesAndAssignedTagForChecking = value; }
        [IgnoreMember]
        public bool IsKeepRefMatchedMetaboliteFeatures { get => PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures; set => PostProcessBaseParam.IsKeepRefMatchedMetaboliteFeatures = value; }
        [IgnoreMember]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples { get => PostProcessBaseParam.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples; set => PostProcessBaseParam.IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value; }
        [IgnoreMember]
        public BlankFiltering BlankFiltering { get => PostProcessBaseParam.BlankFiltering; set => PostProcessBaseParam.BlankFiltering = value; }
        [IgnoreMember]
        public bool IsKeepSuggestedMetaboliteFeatures { get => PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures; set => PostProcessBaseParam.IsKeepSuggestedMetaboliteFeatures = value; }
        [IgnoreMember]
        public float FoldChangeForBlankFiltering { get => PostProcessBaseParam.FoldChangeForBlankFiltering; set => PostProcessBaseParam.FoldChangeForBlankFiltering = value; }


        // Normalization option
        [Key(10)]
        public DataNormalizationBaseParameter DataNormalizationBaseParam { get; set; } = new DataNormalizationBaseParameter();

        [IgnoreMember]
        public bool IsNormalizeNone { get => DataNormalizationBaseParam.IsNormalizeNone; set => DataNormalizationBaseParam.IsNormalizeNone = value; }
        [IgnoreMember]
        public bool IsNormalizeIS { get => DataNormalizationBaseParam.IsNormalizeIS; set => DataNormalizationBaseParam.IsNormalizeIS = value; }
        [IgnoreMember]
        public bool IsNormalizeLowess { get => DataNormalizationBaseParam.IsNormalizeLowess; set => DataNormalizationBaseParam.IsNormalizeLowess = value; }
        [IgnoreMember]
        public bool IsNormalizeIsLowess { get => DataNormalizationBaseParam.IsNormalizeIsLowess; set => DataNormalizationBaseParam.IsNormalizeIsLowess = value; }
        [IgnoreMember]
        public bool IsNormalizeTic { get => DataNormalizationBaseParam.IsNormalizeTic; set => DataNormalizationBaseParam.IsNormalizeTic = value; }
        [IgnoreMember]
        public bool IsNormalizeMTic { get => DataNormalizationBaseParam.IsNormalizeMTic; set => DataNormalizationBaseParam.IsNormalizeMTic = value; }
        [IgnoreMember]
        public bool IsNormalizeSplash { get => DataNormalizationBaseParam.IsNormalizeSplash; set => DataNormalizationBaseParam.IsNormalizeSplash = value; }
        [IgnoreMember]
        public double LowessSpan { get => DataNormalizationBaseParam.LowessSpan; set => DataNormalizationBaseParam.LowessSpan = value; }
        [IgnoreMember]
        public bool IsBlankSubtract { get => DataNormalizationBaseParam.IsBlankSubtract; set => DataNormalizationBaseParam.IsBlankSubtract = value; }

        // Statistics
        [Key(11)]
        public StatisticsBaseParameter StatisticsBaseParam { get; set; } = new StatisticsBaseParameter();
        [IgnoreMember]
        public TransformMethod Transform { get => StatisticsBaseParam.Transform; set => StatisticsBaseParam.Transform = value; }
        [IgnoreMember]
        public ScaleMethod Scale { get => StatisticsBaseParam.Scale; set => StatisticsBaseParam.Scale = value; }
        [IgnoreMember]
        public int MaxComponent { get => StatisticsBaseParam.MaxComponent; set => StatisticsBaseParam.MaxComponent = value; }
        [IgnoreMember]
        public TransformMethod TransformPls { get => StatisticsBaseParam.TransformPls; set => StatisticsBaseParam.TransformPls = value; }
        [IgnoreMember]
        public ScaleMethod ScalePls { get => StatisticsBaseParam.ScalePls; set => StatisticsBaseParam.ScalePls = value; }
        [IgnoreMember]
        public bool IsAutoFitPls { get => StatisticsBaseParam.IsAutoFitPls; set => StatisticsBaseParam.IsAutoFitPls = value; }
        [IgnoreMember]
        public int ComponentPls { get => StatisticsBaseParam.ComponentPls; set => StatisticsBaseParam.ComponentPls = value; }
        [IgnoreMember]
        public MultivariateAnalysisOption MultivariateAnalysisOption { get => StatisticsBaseParam.MultivariateAnalysisOption; set => StatisticsBaseParam.MultivariateAnalysisOption = value; }
        [IgnoreMember]
        public bool IsIdentifiedImportedInStatistics { get => StatisticsBaseParam.IsIdentifiedImportedInStatistics; set => StatisticsBaseParam.IsIdentifiedImportedInStatistics = value; }
        [IgnoreMember]
        public bool IsAnnotatedImportedInStatistics { get => StatisticsBaseParam.IsAnnotatedImportedInStatistics; set => StatisticsBaseParam.IsAnnotatedImportedInStatistics = value; }
        [IgnoreMember]
        public bool IsUnknownImportedInStatistics { get => StatisticsBaseParam.IsUnknownImportedInStatistics; set => StatisticsBaseParam.IsUnknownImportedInStatistics = value; }

        // Mrmprobs export
        [Key(12)]
        public MrmprobsExportBaseParameter MrmprobsExportBaseParam { get; set; } = new MrmprobsExportBaseParameter();
        [IgnoreMember]
        public float MpMs1Tolerance { get => MrmprobsExportBaseParam.MpMs1Tolerance; set => MrmprobsExportBaseParam.MpMs1Tolerance = value; }
        [IgnoreMember]
        public float MpMs2Tolerance { get => MrmprobsExportBaseParam.MpMs2Tolerance; set => MrmprobsExportBaseParam.MpMs2Tolerance = value; }
        [IgnoreMember]
        public float MpRtTolerance { get => MrmprobsExportBaseParam.MpRtTolerance; set => MrmprobsExportBaseParam.MpRtTolerance = value; }
        [IgnoreMember]
        public int MpTopN { get => MrmprobsExportBaseParam.MpTopN; set => MrmprobsExportBaseParam.MpTopN = value; }
        [IgnoreMember]
        public bool MpIsIncludeMsLevel1 { get => MrmprobsExportBaseParam.MpIsIncludeMsLevel1; set => MrmprobsExportBaseParam.MpIsIncludeMsLevel1 = value; }
        [IgnoreMember]
        public bool MpIsUseMs1LevelForQuant { get => MrmprobsExportBaseParam.MpIsUseMs1LevelForQuant; set => MrmprobsExportBaseParam.MpIsUseMs1LevelForQuant = value; }
        [IgnoreMember]
        public bool MpIsFocusedSpotOutput { get => MrmprobsExportBaseParam.MpIsFocusedSpotOutput; set => MrmprobsExportBaseParam.MpIsFocusedSpotOutput = value; }
        [IgnoreMember]
        public bool MpIsReferenceBaseOutput { get => MrmprobsExportBaseParam.MpIsReferenceBaseOutput; set => MrmprobsExportBaseParam.MpIsReferenceBaseOutput = value; }
        [IgnoreMember]
        public bool MpIsExportOtherCandidates { get => MrmprobsExportBaseParam.MpIsExportOtherCandidates; set => MrmprobsExportBaseParam.MpIsExportOtherCandidates = value; }
        [IgnoreMember]
        public float MpIdentificationScoreCutOff { get => MrmprobsExportBaseParam.MpIdentificationScoreCutOff; set => MrmprobsExportBaseParam.MpIdentificationScoreCutOff = value; }


        // molecular networking
        [Key(13)]
        public MolecularSpectrumNetworkingBaseParameter MolecularSpectrumNetworkingBaseParam { get; set; } = new MolecularSpectrumNetworkingBaseParameter();
        [IgnoreMember] 
        public double MnRtTolerance { get => MolecularSpectrumNetworkingBaseParam.MnRtTolerance; set => MolecularSpectrumNetworkingBaseParam.MnRtTolerance = value; }
        [IgnoreMember]
        public double MnIonCorrelationSimilarityCutOff { get => MolecularSpectrumNetworkingBaseParam.MnIonCorrelationSimilarityCutOff; set => MolecularSpectrumNetworkingBaseParam.MnIonCorrelationSimilarityCutOff = value; }
        [IgnoreMember]
        public double MnSpectrumSimilarityCutOff { get => MolecularSpectrumNetworkingBaseParam.MnSpectrumSimilarityCutOff; set => MolecularSpectrumNetworkingBaseParam.MnSpectrumSimilarityCutOff = value; }
        [IgnoreMember]
        public double MnRelativeAbundanceCutOff { get => MolecularSpectrumNetworkingBaseParam.MnRelativeAbundanceCutOff; set => MolecularSpectrumNetworkingBaseParam.MnRelativeAbundanceCutOff = value; }
        [IgnoreMember]
        public double MnAbsoluteAbundanceCutOff { get => MolecularSpectrumNetworkingBaseParam.MnAbsoluteAbundanceCutOff; set => MolecularSpectrumNetworkingBaseParam.MnAbsoluteAbundanceCutOff = value; }
        [IgnoreMember]
        public double MnMassTolerance { get => MolecularSpectrumNetworkingBaseParam.MnMassTolerance; set => MolecularSpectrumNetworkingBaseParam.MnMassTolerance = value; }
        [IgnoreMember]
        public bool MnIsExportIonCorrelation { get => MolecularSpectrumNetworkingBaseParam.MnIsExportIonCorrelation; set => MolecularSpectrumNetworkingBaseParam.MnIsExportIonCorrelation = value; }
        [IgnoreMember]
        public double MinimumPeakMatch { get => MolecularSpectrumNetworkingBaseParam.MinimumPeakMatch; set => MolecularSpectrumNetworkingBaseParam.MinimumPeakMatch = value; }
        [IgnoreMember]
        public double MaxEdgeNumberPerNode { get => MolecularSpectrumNetworkingBaseParam.MaxEdgeNumberPerNode; set => MolecularSpectrumNetworkingBaseParam.MaxEdgeNumberPerNode = value; }
        [IgnoreMember]
        public double MaxPrecursorDifference { get => MolecularSpectrumNetworkingBaseParam.MaxPrecursorDifference; set => MolecularSpectrumNetworkingBaseParam.MaxPrecursorDifference = value; }
        [IgnoreMember]
        public double MaxPrecursorDifferenceAsPercent { get => MolecularSpectrumNetworkingBaseParam.MaxPrecursorDifferenceAsPercent; set => MolecularSpectrumNetworkingBaseParam.MaxPrecursorDifferenceAsPercent = value; }
        [IgnoreMember]
        public MsmsSimilarityCalc MsmsSimilarityCalc { get => MolecularSpectrumNetworkingBaseParam.MsmsSimilarityCalc; set => MolecularSpectrumNetworkingBaseParam.MsmsSimilarityCalc = value; }
        [IgnoreMember]
        public string MnExportFolderPath { get => MolecularSpectrumNetworkingBaseParam.ExportFolderPath; set => MolecularSpectrumNetworkingBaseParam.ExportFolderPath = value; }

        //Tracking of isotope labeles
        [Key(14)]
        public IsotopeTrackingBaseParameter IsotopeTrackingBaseParam { get; set; } = new IsotopeTrackingBaseParameter();
        [IgnoreMember]
        public bool TrackingIsotopeLabels { get => IsotopeTrackingBaseParam.TrackingIsotopeLabels; set => IsotopeTrackingBaseParam.TrackingIsotopeLabels = value; }
        [IgnoreMember]
        public bool UseTargetFormulaLibrary { get => IsotopeTrackingBaseParam.UseTargetFormulaLibrary; set => IsotopeTrackingBaseParam.UseTargetFormulaLibrary = value; }
        [IgnoreMember]
        public IsotopeTrackingDictionary IsotopeTrackingDictionary { get => IsotopeTrackingBaseParam.IsotopeTrackingDictionary; set => IsotopeTrackingBaseParam.IsotopeTrackingDictionary = value; }
        [IgnoreMember]
        public int NonLabeledReferenceID { get => IsotopeTrackingBaseParam.NonLabeledReferenceID; set => IsotopeTrackingBaseParam.NonLabeledReferenceID = value; }
        [IgnoreMember]
        public bool SetFullyLabeledReferenceFile { get => IsotopeTrackingBaseParam.SetFullyLabeledReferenceFile; set => IsotopeTrackingBaseParam.SetFullyLabeledReferenceFile = value; }
        [IgnoreMember]
        public int FullyLabeledReferenceID { get => IsotopeTrackingBaseParam.FullyLabeledReferenceID; set => IsotopeTrackingBaseParam.FullyLabeledReferenceID = value; }


        // others
        [Key(15)]
        public AdvancedProcessOptionBaseParameter AdvancedProcessOptionBaseParam { get; set; } = new AdvancedProcessOptionBaseParameter();
        [IgnoreMember]
        public RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon { get => AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon; set => AdvancedProcessOptionBaseParam.RetentionTimeCorrectionCommon = value; }
        [IgnoreMember]
        public List<MoleculeMsReference> CompoundListInTargetMode { get => AdvancedProcessOptionBaseParam.CompoundListInTargetMode; set => AdvancedProcessOptionBaseParam.CompoundListInTargetMode = value; }
        [IgnoreMember]
        public List<StandardCompound> StandardCompounds { get => AdvancedProcessOptionBaseParam.StandardCompounds; set => AdvancedProcessOptionBaseParam.StandardCompounds = value; }
        [IgnoreMember]
        public bool IsLabPrivate { get => AdvancedProcessOptionBaseParam.IsLabPrivate; set => AdvancedProcessOptionBaseParam.IsLabPrivate = value; }
        [IgnoreMember]
        public bool IsLabPrivateVersionTada { get => AdvancedProcessOptionBaseParam.IsLabPrivateVersionTada; set => AdvancedProcessOptionBaseParam.IsLabPrivateVersionTada = value; }
        [IgnoreMember]
        public bool QcAtLeastFilter { get => AdvancedProcessOptionBaseParam.QcAtLeastFilter; set => AdvancedProcessOptionBaseParam.QcAtLeastFilter = value; }
        [IgnoreMember]
        public List<PeakFeatureSearchValue> DiplayEicSettingValues { get => AdvancedProcessOptionBaseParam.DiplayEicSettingValues; set => AdvancedProcessOptionBaseParam.DiplayEicSettingValues = value; }
        [IgnoreMember]
        public List<PeakFeatureSearchValue> FragmentSearchSettingValues { get => AdvancedProcessOptionBaseParam.FragmentSearchSettingValues; set => AdvancedProcessOptionBaseParam.FragmentSearchSettingValues = value; }
        [IgnoreMember]
        public AndOr AndOrAtFragmentSearch { get => AdvancedProcessOptionBaseParam.AndOrAtFragmentSearch; set => AdvancedProcessOptionBaseParam.AndOrAtFragmentSearch = value; }


        // corrdec
        [Key(16)]
        public CorrDecParam CorrDecParam { get; set; } = new CorrDecParam();
        [IgnoreMember]
        public int MinMS2Intensity { get => CorrDecParam.MinMS2Intensity; set => CorrDecParam.MinMS2Intensity = value; }
        [IgnoreMember]
        public float MS2Tolerance { get => CorrDecParam.MS2Tolerance; set => CorrDecParam.MS2Tolerance = value; }
        [IgnoreMember]
        public float MinCorr_MS1 { get => CorrDecParam.MinCorr_MS1; set => CorrDecParam.MinCorr_MS1 = value; }
        [IgnoreMember]
        public float MinCorr_MS2 { get => CorrDecParam.MinCorr_MS2; set => CorrDecParam.MinCorr_MS2 = value; }
        [IgnoreMember]
        public float CorrDiff_MS1 { get => CorrDecParam.CorrDiff_MS1; set => CorrDecParam.CorrDiff_MS1 = value; }
        [IgnoreMember]
        public float CorrDiff_MS2 { get => CorrDecParam.CorrDiff_MS2; set => CorrDecParam.CorrDiff_MS2 = value; }
        [IgnoreMember]
        public float MinDetectedPercentToVisualize { get => CorrDecParam.MinDetectedPercentToVisualize; set => CorrDecParam.MinDetectedPercentToVisualize = value; }
        [IgnoreMember]
        public bool CorrDecRemoveAfterPrecursor { get => CorrDecParam.CorrDecRemoveAfterPrecursor; set => CorrDecParam.CorrDecRemoveAfterPrecursor = value; }
        [IgnoreMember]
        public int MinNumberOfSample { get => CorrDecParam.MinNumberOfSample; set => CorrDecParam.MinNumberOfSample = value; }
        [IgnoreMember]
        public float MinMS2RelativeIntensity { get => CorrDecParam.MinMS2RelativeIntensity; set => CorrDecParam.MinMS2RelativeIntensity = value; }
        [IgnoreMember]
        public bool CanExcute { get => CorrDecParam.CanExcute; set => CorrDecParam.CanExcute = value; }
        [Key(17)]
        public PostCuratorParameter PostCurationParameter { get; set; } = new PostCuratorParameter();

        public virtual List<string> ParametersAsText() {
            var pStrings = new List<string>();
            pStrings.Add("# Project information");
            pStrings.Add(String.Join(": ", new string[] { "Project start date", ProjectStartDate.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Final saved date", FinalSavedDate.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "MS-DIAL version number", MsdialVersionNumber.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Project folder path", ProjectFolderPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Project file path", Path.Combine(ProjectFolderPath, ProjectFileName).ToString() }));

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
            pStrings.Add(String.Join(": ", new string[] { "Lbm file path", LbmFilePath?.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Text DB file path", TextDBFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Isotope text DB file path", IsotopeTextDBFilePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Compounds library file path for target detection", CompoundListInTargetModePath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Compounds library file path for RT correction", CompoundListForRtCorrectionPath.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Searched adduct ions", String.Join(",", SearchedAdductIons.Select(n => n.AdductIonName).ToArray()) }));

            pStrings.Add("\r\n");
            pStrings.Add("# FileID ClassName");
            foreach (var item in FileID_ClassName.OrEmptyIfNull()) pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, item.Value }));
           
            pStrings.Add("\r\n");
            pStrings.Add("# FileID AnalysisFileType");
            foreach (var item in FileID_AnalysisFileType.OrEmptyIfNull()) pStrings.Add(String.Join(": ", new string[] { "File ID=" + item.Key, item.Value.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Classname order");
            foreach (var item in ClassnameToOrder.OrEmptyIfNull()) pStrings.Add(String.Join(": ", new string[] { "Class name=" + item.Key, item.Value.ToString() }));

            pStrings.Add("\r\n");
            pStrings.Add("# Classname ColorBytes");
            foreach (var item in ClassnameToColorBytes.OrEmptyIfNull()) pStrings.Add(String.Join(": ", new string[] { "Class name=" + item.Key, String.Join(",", item.Value.ToArray()) }));

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
            pStrings.Add($"Max isotopes detected in ms1 spectrum: {PeakPickBaseParam.MaxIsotopesDetectedInMs1Spectrum}");

            pStrings.Add("\r\n");
            pStrings.Add("# Deconvolution");
            pStrings.Add(String.Join(": ", new string[] { "Sigma window value", SigmaWindowValue.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Amplitude cut off", AmplitudeCutoff.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep isotope range", KeptIsotopeRange.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Exclude after precursor", RemoveAfterPrecursor.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Keep original precursor isotopes", KeepOriginalPrecursorIsotopes.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Is do andromeda ms2 deconvolution", IsDoAndromedaMs2Deconvolution.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Andromeda delta", AndromedaDelta.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Andromeda max peaks", AndromedaMaxPeaks.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Target CE", TargetCE.ToString() }));

            //pStrings.Add("\r\n");
            //pStrings.Add("# MSP-based annotation");
            //pStrings.Add(String.Join(": ", new string[] { "RT tolerance for MSP-based annotation", MspSearchParam.RtTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "RI tolerance for MSP-based annotation", MspSearchParam.RiTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "CCS tolerance for MSP-based annotation", MspSearchParam.CcsTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Mass range begin for MSP-based annotation", MspSearchParam.MassRangeBegin.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Mass range end for MSP-based annotation", MspSearchParam.MassRangeEnd.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Relative amplitude cutoff for MSP-based annotation", MspSearchParam.RelativeAmpCutoff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Absolute amplitude cutoff for MSP-based annotation", MspSearchParam.AbsoluteAmpCutoff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Weighted dot product cutoff", MspSearchParam.WeightedDotProductCutOff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Simple dot product cutoff", MspSearchParam.SimpleDotProductCutOff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Reverse dot product cutoff", MspSearchParam.ReverseDotProductCutOff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Matched peaks percentage cutoff", MspSearchParam.MatchedPeaksPercentageCutOff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Minimum spectrum match", MspSearchParam.MinimumSpectrumMatch.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Total score cutoff for MSP-based annotation", MspSearchParam.TotalScoreCutoff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "MS1 tolerance for MSP-based annotation", MspSearchParam.Ms1Tolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "MS2 tolerance for MSP-based annotation", MspSearchParam.Ms2Tolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation scoring", MspSearchParam.IsUseTimeForAnnotationScoring.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use retention information for MSP-based annotation filtering", MspSearchParam.IsUseTimeForAnnotationFiltering.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use CCS for MSP-based annotation scoring", MspSearchParam.IsUseCcsForAnnotationScoring.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use CCS for MSP-based annotation filtering", MspSearchParam.IsUseCcsForAnnotationFiltering.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Only report top hit for MSP-based annotation", OnlyReportTopHitInMspSearch.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Execute annotation process only for alignment file", IsIdentificationOnlyPerformedForAlignmentFile.ToString() }));
            pStrings.Add("\r\n");
            pStrings.Add("# Annotation parameter");
            pStrings.Add(String.Join(": ", new string[] { "Solvent type", LipidQueryContainer.SolventType.ToString() }));
            pStrings.Add(String.Join(": ", new string[] { "Searched lipid class",  
                String.Join(";", LipidQueryContainer.LbmQueries.Where(n => n.IsSelected).Select(n => String.Join(" ", new string[] { n.LbmClass.ToString(), n.AdductType.AdductIonName })).ToArray()) }));

            //pStrings.Add("\r\n");
            //pStrings.Add("# Text-based annotation");
            //pStrings.Add(String.Join(": ", new string[] { "RT tolerance for Text-based annotation", TextDbSearchParam.RtTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "RI tolerance for Text-based annotation", TextDbSearchParam.RiTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "CCS tolerance for Text-based annotation", TextDbSearchParam.CcsTolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Total score cutoff for Text-based annotation", TextDbSearchParam.TotalScoreCutoff.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Accurate ms1 tolerance for Text-based annotation", TextDbSearchParam.Ms1Tolerance.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use retention information for Text-based annotation scoring", TextDbSearchParam.IsUseTimeForAnnotationScoring.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use retention information for Text-based annotation filtering", TextDbSearchParam.IsUseTimeForAnnotationFiltering.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use CCS for Text-based annotation scoring", TextDbSearchParam.IsUseCcsForAnnotationScoring.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Use CCS for Text-based annotation filtering", TextDbSearchParam.IsUseCcsForAnnotationFiltering.ToString() }));
            //pStrings.Add(String.Join(": ", new string[] { "Only report top hit for Text-based annotation", OnlyReportTopHitInTextDBSearch.ToString() }));

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
            pStrings.Add(String.Join(": ", new string[] { "CorrDec remove peaks larger than precursor", CorrDecParam.CorrDecRemoveAfterPrecursor.ToString() }));

            return pStrings;
        }

        public void SaveParameter(string filepath, List<string> paramTexts) {
            using (var sw = new StreamWriter(filepath, false, Encoding.ASCII)) {
                foreach (var text in paramTexts) sw.WriteLine(text);
            }
        }

        public void SaveParameter(string filepath) {
            var paramTexts = ParametersAsText();
            SaveParameter(filepath, paramTexts);
        }
    }



    [MessagePackObject]
    public class RiDictionaryInfo {

        [Key(0)]
        public string DictionaryFilePath { get; set; } = string.Empty;
        [Key(1)]
        public Dictionary<int, float>? RiDictionary { get; set; } = new Dictionary<int, float>(); // int: carbon number, float: retention time

        [IgnoreMember]
        public bool IsIncorrectFormat => RiDictionary is null || RiDictionary.Count == 0;

        [IgnoreMember]
        public bool IsFamesContents {
            get {
                var fiehnFamesDictionary = RetentionIndexHandler.GetFiehnFamesDictionary();
                if (RiDictionary is null || fiehnFamesDictionary.Count != RiDictionary.Count) {
                    return false;
                }
                return fiehnFamesDictionary.Keys.All(RiDictionary.ContainsKey);
            }
        }

        [IgnoreMember]
        public bool IsSequentialCarbonRtOrdering {
            get {
                return RiDictionary?.OrderBy(kvp => kvp.Key).Select(kvp => kvp.Value)
                    .Aggregate((acc: true, rt: float.MinValue), (prev, rt) => (prev.acc && prev.rt < rt, rt)).acc
                    ?? false;
            }
        }

        public static RiDictionaryInfo FromDictionaryFile(string filePath) {
            return new RiDictionaryInfo
            {
                DictionaryFilePath = filePath,
                RiDictionary = File.Exists(filePath)
                    ? RetentionIndexHandler.GetRiDictionary(filePath)
                    : new Dictionary<int, float>(0),
            };
        }
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
    public class ProjectBaseParameter {

        [Key(0)]
        public DateTime ProjectStartDate { get; set; } = DateTime.Now;
        [Key(1)]
        public DateTime FinalSavedDate { get; set; } = DateTime.Now;
        [Key(2)]
        public string MsdialVersionNumber { get; set; } = Resources.VERSION;


        // Project container
        [Key(3)]
        public string ProjectFolderPath { get; set; } = string.Empty;
        [Key(4)]
        public string ProjectFileName { get; set; } = string.Empty;
        [IgnoreMember]
        public string ProjectFilePath => Path.Combine(ProjectFolderPath, ProjectFileName);
        [Key(5)]
        public Dictionary<int, string> FileID_ClassName { get; set; } = new Dictionary<int, string>();
        [Key(6)]
        public Dictionary<int, AnalysisFileType> FileID_AnalysisFileType { get; set; } = new Dictionary<int, AnalysisFileType>();
        [Key(7)]
        public bool IsBoxPlotForAlignmentResult { get; set; }
        [Key(8)]
        public Dictionary<string, int> ClassnameToOrder { get; set; } = new Dictionary<string, int>();
        [Key(9)]
        public Dictionary<string, List<byte>> ClassnameToColorBytes { get; set; } = new Dictionary<string, List<byte>>();
        [Key(23)]
        public Dictionary<string, List<byte>> SpectrumCommentToColorBytes { get; set; } = new Dictionary<string, List<byte>>();

        public ProjectBaseParameter() {
            SpectrumCommentToColorBytes = new Dictionary<string, List<byte>>() {
                // { SpectrumComment.none.ToString(), new List<byte>(){ 0, 0, 0, 255 } }, //black
                { SpectrumComment.experiment.ToString(), new List<byte>(){ 0, 90, 160, 255 } }, // blue
                { SpectrumComment.reference.ToString(), new List<byte>(){ 230, 0, 18, 255 } }, // red
                { SpectrumComment.precursor.ToString(), new List<byte>(){ 0, 153, 68, 255 } }, // green
                { SpectrumComment.b.ToString(), new List<byte>(){ 230, 0, 18, 255 } },  // red
                { SpectrumComment.y.ToString(), new List<byte>(){ 0, 138, 131, 255 } }, // lime
                { SpectrumComment.b2.ToString(), new List<byte>(){ 230, 0, 18, 255 } }, // red
                { SpectrumComment.y2.ToString(), new List<byte>(){ 0, 138, 131, 255 } },// lime
                { SpectrumComment.b_h2o.ToString(), new List<byte>(){ 230, 0, 18, 255 } }, // red
                { SpectrumComment.y_h2o.ToString(), new List<byte>(){ 0, 138, 131, 255 } },// lime
                { SpectrumComment.b_nh3.ToString(), new List<byte>(){ 230, 0, 18, 255 } }, // red
                { SpectrumComment.y_nh3.ToString(), new List<byte>(){ 0, 138, 131, 255 } },// lime
                { SpectrumComment.b_h3po4.ToString(), new List<byte>(){ 230, 0, 18, 255 } }, // red
                { SpectrumComment.y_h3po4.ToString(), new List<byte>(){ 0, 138, 131, 255 } },// lime
                { SpectrumComment.tyrosinep.ToString(), new List<byte>(){ 214, 0, 119, 255 } }, // pink
                { SpectrumComment.metaboliteclass.ToString(), new List<byte>(){ 202, 0, 0, 255 } },  // 
                { SpectrumComment.acylchain.ToString(), new List<byte>(){ 62, 106, 69, 255 } }, // #3e6a45
                { SpectrumComment.snposition.ToString(), new List<byte>(){ 243, 124, 61, 255 } }, // #F37C3D
                { SpectrumComment.doublebond.ToString(), new List<byte>(){ 78, 48, 45, 255 } }, // #4e302d
                { SpectrumComment.doublebond_high.ToString(), new List<byte>(){ 78, 88, 45, 255 } }, // #4e302d
                { SpectrumComment.doublebond_low.ToString(), new List<byte>(){ 78, 8, 45, 255 } }, // #4e302d
                { SpectrumComment.c.ToString(), new List<byte>(){ 78, 48, 45, 255 } }, // #4e302d
                { SpectrumComment.c2.ToString(), new List<byte>(){ 78, 48, 45, 255 } }, // #4e302d
                { SpectrumComment.z.ToString(), new List<byte>(){ 20, 20, 80, 255 } }, // #4e302d
                { SpectrumComment.z2.ToString(), new List<byte>(){ 20, 20, 80, 255 } }, // #4e302d
            };
        } 
        // Project type
        [Key(10)]
        [Obsolete("Use AnalysisFileBean.AcquisitionType property.")]
        public AcquisitionType AcquisitionType { get; set; } = AcquisitionType.None;
        [Key(11)]
        public MSDataType MSDataType { get; set; } = MSDataType.Centroid;
        [Key(12)]
        public MSDataType MS2DataType { get; set; } = MSDataType.Centroid;
        [Key(13)]
        public IonMode IonMode { get; set; } = IonMode.Positive;
        [Key(14)]
        public TargetOmics TargetOmics { get; set; } = TargetOmics.Metabolomics;
        [Key(15)]
        public Ionization Ionization { get; set; } = Ionization.ESI;
        [Key(16)]
        public MachineCategory MachineCategory { get; set; } = MachineCategory.LCMS;

        // Project metadata
        [Key(17)]
        public string CollisionEnergy { get; set; } = string.Empty;
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
    }
    [MessagePackObject]
    public class ProteomicsParameter {

        // processing
        [Key(0)]
        public bool IsDoAndromedaMs2Deconvolution { get; set; } = false;
        [Key(1)]
        public float AndromedaDelta { get; set; } = 100;
        [Key(2)]
        public int AndromedaMaxPeaks { get; set; } = 12;

        // spectral annotation
        [Key(3)]
        public string FastaFilePath { get; set; } = string.Empty;
        [Key(4)]
        public List<Modification> VariableModifications { get; set; } = new List<Modification>();
        [Key(5)]
        public List<Modification> FixedModifications { get; set; } = new List<Modification>();
        [Key(6)]
        public List<Enzyme> EnzymesForDigestion { get; set; } = new List<Enzyme>();
        [Key(7)]
        public CollisionType CollisionType { get; set; } = CollisionType.HCD;
        [Key(8)]
        public float FalseDiscoveryRateForPeptide { get; set; } = 1.0F; //%
        [Key(9)]
        public float FalseDiscoveryRateForProtein { get; set; } = 1.0F; //%
        [Key(10)]
        public int MaxNumberOfModificationsPerPeptide { get; set; } = 2;
        [Key(11)]
        public int MaxMissedCleavage { get; set; } = 2;
        [Key(12)]
        public int MinimumPeptideLength { get; set; } = 7;
        [Key(13)]
        public float MaxPeptideMass { get; set; } = 4600;
        [Key(14)]
        public float MinPeptideMass { get; set; } = 300;
        [Key(15)]
        public float MaxMs2Mz { get; set; } = 1500;
        [Key(16)]
        public float MinMs2Mz { get; set; } = 100;

        //// other basic parameter
        //[Key(10)]
        //public MsRefSearchParameterBase MsRefSearchParam { get; set; } = new MsRefSearchParameterBase();
        public ProteomicsParameter() {
            EnzymesForDigestion = GetDefaultEnzymeSetting();
            VariableModifications = GetDefaultVariableModifications();
            FixedModifications = GetDefaultFixedModifications();
        }

        public List<Enzyme> GetDefaultEnzymeSetting() {
            var eParser = new EnzymesXmlRefParser();
            eParser.Read();
            var enzymes = eParser.Enzymes;

            var defaultEnzyme = "Trypsin/P";
            foreach (var enzyme in enzymes) {
                if (defaultEnzyme == enzyme.Title) {
                    enzyme.IsSelected = true;
                }
            }
            return enzymes;
        }

        public List<Modification> GetDefaultVariableModifications() {
            var mParser = new ModificationsXmlRefParser();
            mParser.Read();
            var modifications = mParser.Modifications;
            var defaultVMods = new List<string> { "Acetyl (Protein N-term)", "Oxidation (M)" };

            foreach (var modification in modifications) {
                foreach (var dVMod in defaultVMods) {
                    if (dVMod == modification.Title) {
                        modification.IsSelected = true;
                        break;
                    }
                }
            }
            return modifications;
        }

        public List<Modification> GetDefaultFixedModifications() {
            var mParser = new ModificationsXmlRefParser();
            mParser.Read();
            var modifications = mParser.Modifications;
            var defaultFMods = new List<string> { "Carbamidomethyl (C)" };
            foreach (var modification in modifications) {
                foreach (var dFMod in defaultFMods) {
                    if (dFMod == modification.Title) {
                        modification.IsSelected = true;
                        break;
                    }
                }
            }
            return modifications;
        }

       
    }

    [MessagePackObject]
    public class ReferenceBaseParameter {
        [Key(0)]
        public string? MspFilePath { get; set; } = string.Empty;
        [Key(1)]
        public string TextDBFilePath { get; set; } = string.Empty;
        [Key(2)]
        public string IsotopeTextDBFilePath { get; set; } = string.Empty;
        [Key(3)]
        public string CompoundListInTargetModePath { get; set; } = string.Empty;
        [Key(4)]
        public string CompoundListForRtCorrectionPath { get; set; } = string.Empty;
        [Key(5)]
        public List<AdductIon> SearchedAdductIons { get; set; } = new List<AdductIon>();
        [Key(6)]
        public string LbmFilePath { get; set; } = string.Empty;
    }

    [MessagePackObject]
    public class DataExportBaseParameter {
        [Key(0)]
        public ExportSpectraFileFormat ExportSpectraFileFormat { get; set; } = ExportSpectraFileFormat.msp;
        [Key(1)]
        public ExportspectraType ExportSpectraType { get; set; } = ExportspectraType.deconvoluted;
        [Key(2)]
        public string MatExportFolderPath { get; set; } = string.Empty;
        [Key(3)]
        public string ExportFolderPath { get; set; } = string.Empty;


        [Key(4)]
        public bool IsHeightMatrixExport { get; set; } = true;
        [Key(5)]
        public bool IsNormalizedMatrixExport { get; set; }
        [Key(6)]
        public bool IsRepresentativeSpectraExport { get; set; }
        [Key(7)]
        public bool IsPeakIdMatrixExport { get; set; }
        [Key(8)]
        public bool IsRetentionTimeMatrixExport { get; set; }
        [Key(9)]
        public bool IsMassMatrixExport { get; set; }
        [Key(10)]
        public bool IsMsmsIncludedMatrixExport { get; set; }

        [Key(11)]
        public bool IsUniqueMsMatrixExport { get; set; }
        [Key(12)]
        public bool IsPeakAreaMatrixExport { get; set; }
        [Key(13)]
        public bool IsParameterExport { get; set; }
        [Key(14)]
        public bool IsGnpsExport { get; set; }
        [Key(15)]
        public bool IsMolecularNetworkingExport { get; set; }
        [Key(16)]
        public bool IsSnMatrixExport { get; set; }
        [Key(17)]
        public bool IsExportedAsMzTabM { get; set; }
    }

    [MessagePackObject]
    public class ProcessBaseParameter {
        // Process parameters
        [Key(0)]
        public ProcessOption ProcessOption { get; set; } = ProcessOption.All;
        [Key(1)]
        public int NumThreads { get; set; } = 2;

        [IgnoreMember]
        public int UsableNumThreads => Math.Max(1, Math.Min(Environment.ProcessorCount, NumThreads));
    }

    // MS-CleanR (post curator) parameters

    [MessagePackObject]
    public class PostCuratorParameter {
        [Key(0)]
        public double FilterBlankThreshold { get; set; }
        [Key(1)]
        public bool IsBlankFilter { get; set; } = false;
        [Key(2)]
        public bool IsMzFilter { get; set; } = false;
        [Key(3)]
        public bool IsBlankGhostFilter { get; set; } = false;
        [Key(4)]
        public bool IsRsdFilter { get; set; } = false;
        [Key(5)]
        public double FilterRsdThreshold { get; set; }
        [Key(6)]
        public bool IsRmdFilter { get; set; } = false;
        [Key(7)]
        public double FilterMinRmdThreshold { get; set; }
        [Key(8)]
        public double FilterMaxRmdThreshold { get; set; }
    }

    [MessagePackObject]
    public class PeakPickBaseParameter {
        [Key(0)]
        public SmoothingMethod SmoothingMethod { get; set; } = SmoothingMethod.LinearWeightedMovingAverage;
        [Key(1)]
        public int SmoothingLevel { get; set; } = 3;
        [Key(2)]
        public double MinimumAmplitude { get; set; } = 1000;
        [Key(3)]
        public double MinimumDatapoints { get; set; } = 5;
        [Key(4)]
        public float MassSliceWidth { get; set; } = 0.1F;
        [Key(5)]
        public float RetentionTimeBegin { get; set; } = 0;
        [Key(6)]
        public float RetentionTimeEnd { get; set; } = 100;
        [Key(7)]
        public float MassRangeBegin { get; set; } = 0;
        [Key(8)]
        public float MassRangeEnd { get; set; } = 2000;
        [Key(9)]
        public float Ms2MassRangeBegin { get; set; } = 0;
        [Key(10)]
        public float Ms2MassRangeEnd { get; set; } = 2000;
        [Key(11)]
        public float CentroidMs1Tolerance { get; set; } = 0.01F;
        [Key(12)]
        public float CentroidMs2Tolerance { get; set; } = 0.025F;
        [Key(13)]
        public int MaxChargeNumber { get; set; } = 2;
        [Key(14)]
        public bool IsBrClConsideredForIsotopes { get; set; } = false;
        [Key(15)]
        public List<MzSearchQuery> ExcludedMassList { get; set; } = new List<MzSearchQuery>();
        [Key(16)]
        public int MaxIsotopesDetectedInMs1Spectrum { get; set; } = 2;

        public bool ShouldExclude(double mass) {
            foreach (var query in ExcludedMassList) {
                if (Math.Abs(query.Mass - mass) < query.MassTolerance) {
                    return true;
                }
            }
            return false;
        }

        public bool IsInMassRange(double mz) => Math.Abs(mz - MassRangeBegin) > 1e-6 && Math.Abs(MassRangeEnd - mz) > 1e-6;
    }

    [MessagePackObject]
    public class AlignmentBaseParameter {
        [Key(0)]
        public int AlignmentReferenceFileID { get; set; } = 0;
        [Key(1)]
        public float RetentionTimeAlignmentFactor { get; set; } = 0.5F;
        [Key(2)]
        public float Ms1AlignmentFactor { get; set; } = 0.5F;
        [Key(3)]
        public float SpectrumSimilarityAlignmentFactor { get; set; } = 0.5F;
        [Key(4)]
        public float Ms1AlignmentTolerance { get; set; } = 0.015F;
        [Key(5)]
        public float RetentionTimeAlignmentTolerance { get; set; } = 0.10F;
        [Key(6)]
        public float SpectrumSimilarityAlignmentTolerance { get; set; } = 0.8F;
        [Key(7)]
        public float AlignmentScoreCutOff { get; set; } = 50;
        [Key(8)]
        public bool TogetherWithAlignment { get; set; } = true;
    }

    [MessagePackObject]
    public sealed class RefSpecMatchBaseParameter {
        [SerializationConstructor]
        public RefSpecMatchBaseParameter() {
            LipidQueryContainer = new LipidQueryBean() {
                SolventType = SolventType.CH3COONH4,
                LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: false)
            };
        }

        public RefSpecMatchBaseParameter(bool isLabUseOnly) {
            LipidQueryContainer = new LipidQueryBean() {
                SolventType = SolventType.CH3COONH4,
                LbmQueries = LbmQueryParcer.GetLbmQueries(isLabUseOnly: isLabUseOnly)
            };
        }

        [Key(0)]
        public LipidQueryBean LipidQueryContainer { get; set; }

        [Key(1)]
        public MsRefSearchParameterBase MspSearchParam { get; set; } = new MsRefSearchParameterBase();


        [Key(2)]
        public bool OnlyReportTopHitInMspSearch { get; set; } = false;
        [Key(3)]
        public MsRefSearchParameterBase TextDbSearchParam { get; set; } = new MsRefSearchParameterBase() {
            RtTolerance = 0.1F, Ms1Tolerance = 0.01F, TotalScoreCutoff = 0.85F
        };
        [Key(4)]
        public bool OnlyReportTopHitInTextDBSearch { get; set; } = false;
        [Key(5)]
        public bool IsIdentificationOnlyPerformedForAlignmentFile { get; set; } = false;
        [Key(6)]
        public Dictionary<int, RiDictionaryInfo> FileIdRiInfoDictionary { get; set; } = new Dictionary<int, RiDictionaryInfo>();
        [Key(7)]
        public MsRefSearchParameterBase LbmSearchParam { get; set; } = new MsRefSearchParameterBase();
        [IgnoreMember]
        public bool MayRiDictionaryImported => FileIdRiInfoDictionary.Count > 0;
    }

    [MessagePackObject]
    public class ChromDecBaseParameter {
        [Key(0)]
        [Obsolete("Use AnalysisFileBean.IsDoMs2ChromDeconvolution")]
        public bool IsDoMs2ChromDeconvolution { get; set; } = false;
        [Key(1)]
        public float SigmaWindowValue { get; set; } = 0.5F;
        [Key(2)]
        public float AmplitudeCutoff { get; set; } = 0;
        [Key(3)]
        public float AveragePeakWidth { get; set; } = 30;
        [Key(4)]
        public float KeptIsotopeRange { get; set; } = 5;
        [Key(5)]
        public bool RemoveAfterPrecursor { get; set; } = true;
        [Key(6)]
        public bool KeepOriginalPrecursorIsotopes { get; set; } = false;
        [Key(7)]
        public AccuracyType AccuracyType { get; set; } = AccuracyType.IsAccurate;
        [Key(8)]
        public double TargetCE { get; set; } = 0; // used for AIF deconvolution. Zero means that min CE is used for MS1 
    }

    [MessagePackObject]
    public class PostProcessBaseParameter {
        // Post-alignment and filtering
        [Key(0)]
        public float PeakCountFilter { get; set; } = 0;
        [Key(1)]
        public bool IsForceInsertForGapFilling { get; set; } = true;
        [Key(2)]
        public float NPercentDetectedInOneGroup { get; set; } = 0;
        [Key(3)]
        public bool IsRemoveFeatureBasedOnBlankPeakHeightFoldChange { get; set; } = false;
        [Key(4)]
        public float SampleMaxOverBlankAverage { get; set; } = 5;
        [Key(5)]
        public float SampleAverageOverBlankAverage { get; set; } = 5;
        [Key(6)]
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking { get; set; } = true;
        [Key(7)]
        public bool IsKeepRefMatchedMetaboliteFeatures { get; set; } = true;
        [Key(8)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples { get; set; } = false;
        [Key(9)]
        public BlankFiltering BlankFiltering { get; set; } = BlankFiltering.SampleMaxOverBlankAve;
        [Key(10)]
        public bool IsKeepSuggestedMetaboliteFeatures { get; set; } = false;
        [Key(11)]
        public float FoldChangeForBlankFiltering { get; set; } = 5;

    }

    [MessagePackObject]
    public class DataNormalizationBaseParameter {
        [Key(0)]
        public bool IsNormalizeNone { get; set; } = true;
        [Key(1)]
        public bool IsNormalizeIS { get; set; }
        [Key(2)]
        public bool IsNormalizeLowess { get; set; }
        [Key(3)]
        public bool IsNormalizeIsLowess { get; set; }
        [Key(4)]
        public bool IsNormalizeTic { get; set; }
        [Key(5)]
        public bool IsNormalizeMTic { get; set; }
        [Key(6)]
        public bool IsNormalizeSplash { get; set; }
        [Key(7)]
        public double LowessSpan { get; set; }
        [Key(8)]
        public bool IsBlankSubtract { get; set; }
    }

    [MessagePackObject]
    public class StatisticsBaseParameter {
        [Key(0)]
        public TransformMethod Transform { get; set; } = TransformMethod.None;
        [Key(1)]
        public ScaleMethod Scale { get; set; } = ScaleMethod.AutoScale;
        [Key(2)]
        public int MaxComponent { get; set; } = 2;
        [Key(3)]
        public TransformMethod TransformPls { get; set; } = TransformMethod.None;
        [Key(4)]
        public ScaleMethod ScalePls { get; set; } = ScaleMethod.AutoScale;
        [Key(5)]
        public bool IsAutoFitPls { get; set; } = true;
        [Key(6)]
        public int ComponentPls { get; set; } = 2;
        [Key(7)]
        public MultivariateAnalysisOption MultivariateAnalysisOption { get; set; } = MultivariateAnalysisOption.Pca;
        [Key(8)]
        public bool IsIdentifiedImportedInStatistics { get; set; } = true;
        [Key(9)]
        public bool IsAnnotatedImportedInStatistics { get; set; }
        [Key(10)]
        public bool IsUnknownImportedInStatistics { get; set; }
    }

    [MessagePackObject]
    public class MrmprobsExportBaseParameter {
        [Key(0)]
        public float MpMs1Tolerance { get; set; } = 0.005F;
        [Key(1)]
        public float MpMs2Tolerance { get; set; } = 0.01F;
        [Key(2)]
        public float MpRtTolerance { get; set; } = 0.5F;
        [Key(3)]
        public int MpTopN { get; set; } = 5;
        [Key(4)]
        public bool MpIsIncludeMsLevel1 { get; set; } = true;
        [Key(5)]
        public bool MpIsUseMs1LevelForQuant { get; set; } = false;
        [Key(6)]
        public bool MpIsFocusedSpotOutput { get; set; } = false;
        [Key(7)]
        public bool MpIsReferenceBaseOutput { get; set; } = true;
        [Key(8)]
        public bool MpIsExportOtherCandidates { get; set; } = false;
        [Key(9)]
        public float MpIdentificationScoreCutOff { get; set; } = 80F;
    }

    [MessagePackObject]
    public class MolecularSpectrumNetworkingBaseParameter {
        [Key(0)]
        public double MnRtTolerance { get; set; } = 100;
        [Key(1)]
        public double MnIonCorrelationSimilarityCutOff { get; set; } = 95;
        [Key(2)]
        public double MnSpectrumSimilarityCutOff { get; set; } = 50;
        [Key(3)]
        public double MnRelativeAbundanceCutOff { get; set; } = 1;
        [Key(4)]
        public double MnMassTolerance { get; set; } = 0.025;
        [Key(5)]
        public bool MnIsExportIonCorrelation { get; set; } = false;
        [Key(6)]
        public double MinimumPeakMatch { get; set; } = 2;
        [Key(7)]
        public double MaxEdgeNumberPerNode { get; set; } = 6;
        [Key(8)]
        public double MaxPrecursorDifference { get; set; } = 400;
        [Key(9)]
        public double MaxPrecursorDifferenceAsPercent { get; set; } = 50;
        [Key(10)]
        public MsmsSimilarityCalc MsmsSimilarityCalc { get; set; } = MsmsSimilarityCalc.Bonanza;
        [Key(11)]
        public string ExportFolderPath { get; set; } = string.Empty;
        [Key(12)]
        public double MnAbsoluteAbundanceCutOff { get; set; } = 0.0;
    }

    [MessagePackObject]
    public class IsotopeTrackingBaseParameter {
        [Key(0)]
        public bool TrackingIsotopeLabels { get; set; } = false;
        [Key(1)]
        public bool UseTargetFormulaLibrary { get; set; } = false;
        [Key(2)]
        public IsotopeTrackingDictionary IsotopeTrackingDictionary { get; set; } = new IsotopeTrackingDictionary();
        [Key(3)]
        public int NonLabeledReferenceID { get; set; } = 0;
        [Key(4)]
        public bool SetFullyLabeledReferenceFile { get; set; } = false;
        [Key(5)]
        public int FullyLabeledReferenceID { get; set; } = 0;
    }

    [MessagePackObject]
    public class AdvancedProcessOptionBaseParameter {
        [Key(0)]
        public RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon { get; set; } = new RetentionTimeCorrectionCommon();
        [Key(1)]
        public List<MoleculeMsReference> CompoundListInTargetMode { get; set; } = new List<MoleculeMsReference>();
        [Key(2)]
        public List<StandardCompound> StandardCompounds { get; set; } = new List<StandardCompound>();
        [Key(3)]
        public bool IsLabPrivate { get; set; } = false;
        [Key(4)]
        public bool IsLabPrivateVersionTada { get; set; } = false;
        [Key(5)]
        public bool QcAtLeastFilter { get; set; } = false;
        [Key(6)]
        public List<PeakFeatureSearchValue> DiplayEicSettingValues { get; set; } = new List<PeakFeatureSearchValue>();
        [Key(7)]
        public List<PeakFeatureSearchValue> FragmentSearchSettingValues { get; set; } = new List<PeakFeatureSearchValue>();
        [Key(8)]
        public AndOr AndOrAtFragmentSearch { get; set; } = AndOr.AND;

        [IgnoreMember]
        public bool IsTargetMode => !CompoundListInTargetMode.IsEmptyOrNull();
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
        public bool CorrDecRemoveAfterPrecursor { get; set; } = true;
        [Key(8)]
        public int MinNumberOfSample { get; set; } = 3;
        [Key(9)]
        public float MinMS2RelativeIntensity { get; set; } = 2;
        [Key(10)]
        public bool CanExcute { get; set; } = true;
        public CorrDecParam() { }
    }
}
