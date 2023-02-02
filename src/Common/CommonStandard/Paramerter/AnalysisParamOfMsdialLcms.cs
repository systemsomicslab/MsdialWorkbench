using Riken.Metabolomics.Common.Query;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public enum ProcessOption { All, IdentificationPlusAlignment, Alignment };
    public enum BlankFiltering { SampleMaxOverBlankAve, SampleAveOverBlankAve }
    public enum MultivariateAnalysisOption { Pca, Plsda, Plsr, Oplsda, Oplsr, Hca}
    public enum IonMobilityType { Tims, Dtims, Twims, CCS }
   
    [MessagePackObject]
    public class CoefficientsForCcsCalculation {
        [Key(0)]
        public bool IsAgilentIM { get; set; } = false;
        [Key(1)]
        public bool IsBrukerIM { get; set; } = false;
        [Key(2)]
        public bool IsWatersIM { get; set; } = false;

        [Key(3)]
        public double AgilentBeta { get; set; } = -1.0;
        [Key(4)]
        public double AgilentTFix { get; set; } = -1.0;
        [Key(5)]
        public double WatersCoefficient { get; set; } = -1.0;
        [Key(6)]
        public double WatersT0 { get; set; } = -1.0;
        [Key(7)]
        public double WatersExponent { get; set; } = -1.0;
    }
    
    [DataContract]
    [MessagePackObject]
    public class AnalysisParametersBean
    {
        //version
        private string msdialVersionNumber;

        //Process options
        private ProcessOption processOption;

        //Data collection
        [DataMember]
        private float retentionTimeBegin;
        [DataMember]
        private float retentionTimeEnd;
        [DataMember]
        private float massRangeBegin;
        [DataMember]
        private float massRangeEnd;
        private float ms2MassRangeBegin;
        private float ms2MassRangeEnd;

        //Centroid parameters
        [DataMember]
        private float centroidMs1Tolerance;
        [DataMember]
        private float centroidMs2Tolerance;
        [DataMember]
        private bool peakDetectionBasedCentroid;

        //Peak detection
        [DataMember]
        private SmoothingMethod smoothingMethod;
        [DataMember]
        private int smoothingLevel;
        [DataMember]
        private double amplitudeNoiseFactor;
        [DataMember]
        private double slopeNoiseFactor;
        [DataMember]
        private double peaktopNoiseFactor;
        [DataMember]
        private double minimumDatapoints;
        [DataMember]
        private double minimumAmplitude;
        [DataMember]
        private float massSliceWidth;
        [DataMember]
        private List<ExcludeMassBean> excludedMassList;
        [DataMember]
        private List<AdductIonInformationBean> adductIonInformationBeanList;
        [DataMember]
        private bool backgroundSubtraction;
        [DataMember]
        private int maxChargeNumber;
        [DataMember]
        private bool isBrClConsideredForIsotopes;


        //Deconvolution
        [DataMember]
        private int bandWidth;
        [DataMember]
        private int segmentNumber;
        [DataMember]
        private DeconvolutionType deconvolutionType;
        [DataMember]
        private float amplitudeCutoff;
        [DataMember]
        private float sigmaWindowValue;
        [DataMember]
        private float keptIsotopeRange;
        [DataMember]
        private bool keepOriginalPrecursorIsotopes;
        [DataMember]
        private bool removeAfterPrecursor;

        //Peak identification
        [DataMember]
        private float retentionTimeLibrarySearchTolerance;
        [DataMember]
        private float ms1LibrarySearchTolerance;
        [DataMember]
        private float ms2LibrarySearchTolerance;
        [DataMember]
        private float identificationScoreCutOff;
        [DataMember]
        private float adductAndIsotopeMassTolerance;
        [DataMember]
        private float retentionTimeToleranceOfPostIdentification;
        [DataMember]
        private float accurateMassToleranceOfPostIdentification;
        [DataMember]
        private float postIdentificationScoreCutOff;
        [DataMember]
        private bool onlyReportTopHitForPostAnnotation;
        [DataMember]
        private float relativeAbundanceCutOff;
        [DataMember]
        private LipidQueryBean lipidQueryBean;
        [DataMember]
        private bool isIdentificationOnlyPerformedForAlignmentFile;
        [DataMember]
        private bool isUseRetentionInfoForIdentificationScoring;
        private bool isUseRetentionInfoForIdentificationFiltering;
       

        //Alignment parameters
        [DataMember]
        private int alignmentReferenceFileID;
        [DataMember]
        private float retentionTimeAlignmentFactor;
        [DataMember]
        private float ms1AlignmentFactor;
        [DataMember]
        private float ms2SimilarityAlignmentFactor;
        [DataMember]
        private float retentionTimeAlignmentTolerance;
        [DataMember]
        private float ms1AlignmentTolerance;
        [DataMember]
        private float alignmentScoreCutOff;
        [DataMember]
        private bool isForceInsertForGapFilling;

        // Ion mobility
        private float driftTimeAlignmentTolerance;
        private float driftTimeAlignmentFactor;
        private IonMobilityType ionMobilityType;
        private float accumulatedRtRagne;
        private bool isUseCcsForIdentificationFiltering;
        private bool isUseCcsForIdentificationScoring;
        private float ccsSearchTolerance;
        private Dictionary<int, CoefficientsForCcsCalculation> fileidToCcsCalibrantData;
        private bool isAllCalibrantDataImported;

        //filtering for alignment results
        [DataMember]
        private float peakCountFilter;
        [DataMember]
        private bool qcAtLeastFilter;
        [DataMember]
        private bool gapFillingOption;
        [DataMember]
        private bool togetherWithAlignment;
        [DataMember]
        private float nPercentDetectedInOneGroup;
        [DataMember]
        private bool isRemoveFeatureBasedOnPeakHeightFoldChange;
        [DataMember]
        private float sampleMaxOverBlankAverage;
        [DataMember]
        private float sampleAverageOverBlankAverage;
        [DataMember]
        private bool isKeepRemovableFeaturesAndAssignedTagForChecking;
        [DataMember]
        private bool isKeepIdentifiedMetaboliteFeatures;
        [DataMember]
        private bool isKeepAnnotatedMetaboliteFeatures;
        [DataMember]
        private bool isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
        [DataMember]
        private BlankFiltering blankFiltering;
        [DataMember]
        private float foldChangeForBlankFiltering;


        //Tracking of isotope labeles
        [DataMember]
        private bool trackingIsotopeLabels;
        [DataMember]
        private bool useTargetFormulaLibrary;
        [DataMember]
        private IsotopeTrackingDictionary isotopeTrackingDictionary;
        [DataMember]
        private int nonLabeledReferenceID;
        [DataMember]
        private bool setFullyLabeledReferenceFile;
        [DataMember]
        private int fullyLabeledReferenceID;

        //Normalization
        [DataMember]
        private bool isNormalizeNone;
        [DataMember]
        private bool isNormalizeIS;
        [DataMember]
        private bool isNormalizeLowess;
        [DataMember]
        private bool isNormalizeIsLowess;
        [DataMember]
        private bool isNormalizeTic;
        [DataMember]
        private bool isNormalizeMTic;
        [DataMember]
        private bool isBlankSubtract;
        private bool isNormalizeSplash;
        [DataMember]
        private double lowessSpan;
        private List<StandardCompound> standardCompounds;

        //Statistics, PCA
        [DataMember]
        private TransformMethod transform;
        [DataMember]
        private ScaleMethod scale;
        [DataMember]
        private int maxComponent;

        //Statistics, PLS
        [DataMember]
        private TransformMethod transformPls;
        [DataMember]
        private ScaleMethod scalePls;
        [DataMember]
        private bool isAutoFitPls;
        [DataMember]
        private int componentPls;
        [DataMember]
        private MultivariateAnalysisOption multivariateAnalysisOption;

        [DataMember]
        private bool isIdentifiedImportedInStatistics;
        [DataMember]
        private bool isAnnotatedImportedInStatistics;
        [DataMember]
        private bool isUnknownImportedInStatistics;

    

        //Output for MRMPROBS
        [DataMember]
        private float mpMs1Tolerance;
        [DataMember]
        private float mpMs2Tolerance;
        [DataMember]
        private float mpRtTolerance;
        [DataMember]
        private int mpTopN;
        [DataMember]
        private bool mpIsIncludeMsLevel1;
        [DataMember]
        private bool mpIsUseMs1LevelForQuant;
        [DataMember]
        private bool mpIsFocusedSpotOutput;
        [DataMember]
        private bool mpIsReferenceBaseOutput;
        [DataMember]
        private bool mpIsExportOtherCandidates;
        [DataMember]
        private float mpIdentificationScoreCutOff;

        //Output for spectra
        [DataMember]
        private double relativeAbundanceCutOffForSpectrumExport;

        //searcher
        [DataMember]
        private List<FragmentSearcherQuery> fragmentSearcherQueries;
        [DataMember]
        private List<AdductDiff> addictDiffQueries;

        //viewer
        [DataMember]
        private List<ExtractedIonChromatogramDisplaySettingBean> eicDisplayQueries;

        [DataMember]
        private int numThreads;
        [DataMember]
        private bool isAndAsFragmentSearcherOption;

        [DataMember]
        private bool isIonMobility;

        // molecular networking
        [DataMember]
        private bool mnIsExportIonCorrelation;
        [DataMember]
        private double mnMassTolerance;
        [DataMember]
        private double mnRelativeAbundanceCutOff;
        [DataMember]
        private double mnSpectrumSimilarityCutOff;
        [DataMember]
        private double mnIonCorrelationSimilarityCutOff;
        [DataMember]
        private double mnRtTolerance;
        [DataMember]
        private AnalysisParamOfMsdialCorrDec analysisParamOfMsdialCorrDec;
        [DataMember]
        private bool isUseSimpleDotScore;
        [DataMember]
        private List<TextFormatCompoundInformationBean> compoundListInTargetMode;

        #region // constructor

        public AnalysisParametersBean() {
            //Process option
            ProcessOption = ProcessOption.All;

            //data correction
            MassRangeBegin = 0;
            MassRangeEnd = 2000;
            Ms2MassRangeBegin = 0;
            Ms2MassRangeEnd = 2000;
            RetentionTimeBegin = 0;
            RetentionTimeEnd = 100;
            CentroidMs1Tolerance = 0.01F;
            CentroidMs2Tolerance = 0.025F;
            PeakDetectionBasedCentroid = true;
            NumThreads = 1;

            //peak detection
            SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage;
            SmoothingLevel = 3;
            MassSliceWidth = 0.1F;
            BackgroundSubtraction = true;
            SlopeNoiseFactor = 2;
            AmplitudeNoiseFactor = 4;
            MinimumAmplitude = 1000;
            MinimumDatapoints = 5;
            PeaktopNoiseFactor = 2;
            MaxChargeNumber = 2;
            IsBrClConsideredForIsotopes = false;

            //MS2Dec
            BandWidth = 5;
            SegmentNumber = 1;
            DeconvolutionType = DeconvolutionType.Both;
            AmplitudeCutoff = 0;
            SigmaWindowValue = 0.5F;
            RemoveAfterPrecursor = true;
            KeptIsotopeRange = 0.5F;
            RelativeAbundanceCutOff = 0.0F;
            KeepOriginalPrecursorIsotopes = false;

            //Identification
            RetentionTimeLibrarySearchTolerance = 100.0F;
            Ms1LibrarySearchTolerance = 0.01F;
            Ms2LibrarySearchTolerance = 0.05F;
            IdentificationScoreCutOff = 80;
            RetentionTimeToleranceOfPostIdentification = 0.1F;
            AccurateMassToleranceOfPostIdentification = 0.01F;
            PostIdentificationScoreCutOff = 85;
            OnlyReportTopHitForPostAnnotation = false;
            LipidQueryBean = null;
            IsIdentificationOnlyPerformedForAlignmentFile = false;
            IsUseRetentionInfoForIdentificationScoring = false;
            IsUseRetentionInfoForIdentificationFiltering = false;
            IsUseSimpleDotScore = false;

            //Adduct
            AdductAndIsotopeMassTolerance = 0.025F;
            AdductIonInformationBeanList = new List<AdductIonInformationBean>();

            //Alignment
            RetentionTimeAlignmentTolerance = 0.05F;
            Ms1AlignmentTolerance = 0.015F;
            RetentionTimeAlignmentFactor = 0.5F;
            Ms1AlignmentFactor = 0.5F;
            Ms2SimilarityAlignmentFactor = 0.5F;
            AlignmentScoreCutOff = 50;
            AlignmentReferenceFileID = 0;
            TogetherWithAlignment = true;
            IsForceInsertForGapFilling = true;

            //ion mobility
            DriftTimeAlignmentTolerance = 0.02F; // msec
            DriftTimeAlignmentFactor = 0.5F;
            AccumulatedRtRagne = 0.2F;
            IsUseCcsForIdentificationFiltering = true;
            CcsSearchTolerance = 10.0F;
            FileidToCcsCalibrantData = new Dictionary<int, CoefficientsForCcsCalculation>();
            IsIonMobility = false;
            IonMobilityType = IonMobilityType.Tims;
            IsAllCalibrantDataImported = false;

            //Alignment filtering
            QcAtLeastFilter = true;
            PeakCountFilter = 0;
            NPercentDetectedInOneGroup = 0;
            GapFillingOption = true;
            IsRemoveFeatureBasedOnPeakHeightFoldChange = false;
            SampleMaxOverBlankAverage = 5;
            SampleAverageOverBlankAverage = 5;
            FoldChangeForBlankFiltering = 5;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = true;
            IsKeepIdentifiedMetaboliteFeatures = true;
            IsKeepAnnotatedMetaboliteFeatures = false;
            IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = false;
            BlankFiltering = BlankFiltering.SampleMaxOverBlankAve;

            //Isotope tracking
            TrackingIsotopeLabels = false;
            UseTargetFormulaLibrary = false;
            IsotopeTrackingDictionary = new IsotopeTrackingDictionary();
            NonLabeledReferenceID = 0;
            SetFullyLabeledReferenceFile = false;
            FullyLabeledReferenceID = 0;

            //Normalization and Statistics
            IsNormalizeNone = true;
            LowessSpan = 0.7;
            StandardCompounds = new List<StandardCompound>();

            //PCA
            Scale = ScaleMethod.AutoScale;
            Transform = TransformMethod.None;
            MaxComponent = 2;

            //PLS
            ScalePls = ScaleMethod.AutoScale;
            TransformPls = TransformMethod.None;
            IsAutoFitPls = true;
            ComponentPls = 2;
            MultivariateAnalysisOption = MultivariateAnalysisOption.Plsda;

            IsIdentifiedImportedInStatistics = true;
            IsAnnotatedImportedInStatistics = false;
            IsUnknownImportedInStatistics = false;

            //Mrmprobs
            mpMs1Tolerance = 0.005F;
            mpMs2Tolerance = 0.01F;
            mpRtTolerance = 0.5F;
            mpTopN = 5;
            mpIsIncludeMsLevel1 = true;
            mpIsUseMs1LevelForQuant = false;
            mpIsFocusedSpotOutput = false;
            mpIsReferenceBaseOutput = true;
            mpIsExportOtherCandidates = false;
            mpIdentificationScoreCutOff = 80.0F;

            //output 
            relativeAbundanceCutOffForSpectrumExport = 0.001;

            //Searcher
            fragmentSearcherQueries = new List<FragmentSearcherQuery>();
            isAndAsFragmentSearcherOption = false;
            addictDiffQueries = new List<AdductDiff>();

            //Viewer
            EicDisplayQueries = new List<ExtractedIonChromatogramDisplaySettingBean>();

            //Molecular networking
            MnIsExportIonCorrelation = false;
            MnIonCorrelationSimilarityCutOff = 95;
            MnMassTolerance = 0.025;
            MnRtTolerance = 100;
            MnSpectrumSimilarityCutOff = 75;
            MnRelativeAbundanceCutOff = 1;

            //CorrDec settings
            analysisParamOfMsdialCorrDec = new AnalysisParamOfMsdialCorrDec();            
        }

        [Key(0)]
        public ProcessOption ProcessOption {
            get { return processOption; }
            set { processOption = value; }
        }

        [Key(1)]
        public SmoothingMethod SmoothingMethod {
            get { return smoothingMethod; }
            set { smoothingMethod = value; }
        }

        [Key(2)]
        public double MinimumAmplitude {
            get { return minimumAmplitude; }
            set { minimumAmplitude = value; }
        }

        [Key(3)]
        public int SmoothingLevel {
            get { return smoothingLevel; }
            set { smoothingLevel = value; }
        }

        [Key(4)]
        public double AmplitudeNoiseFactor {
            get { return amplitudeNoiseFactor; }
            set { amplitudeNoiseFactor = value; }
        }

        [Key(5)]
        public double PeaktopNoiseFactor {
            get { return peaktopNoiseFactor; }
            set { peaktopNoiseFactor = value; }
        }

        [Key(6)]
        public double SlopeNoiseFactor {
            get { return slopeNoiseFactor; }
            set { slopeNoiseFactor = value; }
        }

        [Key(7)]
        public double MinimumDatapoints {
            get { return minimumDatapoints; }
            set { minimumDatapoints = value; }
        }

        [Key(8)]
        public float MassSliceWidth {
            get { return massSliceWidth; }
            set { massSliceWidth = value; }
        }

        [Key(9)]
        public bool BackgroundSubtraction {
            get { return backgroundSubtraction; }
            set { backgroundSubtraction = value; }
        }

        [Key(10)]
        public List<ExcludeMassBean> ExcludedMassList {
            get { return excludedMassList; }
            set { excludedMassList = value; }
        }

        [Key(11)]
        public List<AdductIonInformationBean> AdductIonInformationBeanList {
            get { return adductIonInformationBeanList; }
            set { adductIonInformationBeanList = value; }
        }

        [Key(12)]
        public LipidQueryBean LipidQueryBean {
            get { return lipidQueryBean; }
            set { lipidQueryBean = value; }
        }

        [Key(13)]
        public float RetentionTimeBegin {
            get { return retentionTimeBegin; }
            set { retentionTimeBegin = value; }
        }

        [Key(14)]
        public float RetentionTimeEnd {
            get { return retentionTimeEnd; }
            set { retentionTimeEnd = value; }
        }

        [Key(15)]
        public float MassRangeBegin {
            get { return massRangeBegin; }
            set { massRangeBegin = value; }
        }

        [Key(16)]
        public float MassRangeEnd {
            get { return massRangeEnd; }
            set { massRangeEnd = value; }
        }

        [Key(17)]
        public float CentroidMs1Tolerance {
            get { return centroidMs1Tolerance; }
            set { centroidMs1Tolerance = value; }
        }

        [Key(18)]
        public float CentroidMs2Tolerance {
            get { return centroidMs2Tolerance; }
            set { centroidMs2Tolerance = value; }
        }

        [Key(19)]
        public bool PeakDetectionBasedCentroid {
            get { return peakDetectionBasedCentroid; }
            set { peakDetectionBasedCentroid = value; }
        }

        [Key(20)]
        public float RetentionTimeAlignmentFactor {
            get { return retentionTimeAlignmentFactor; }
            set { retentionTimeAlignmentFactor = value; }
        }

        [Key(21)]
        public float Ms1AlignmentFactor {
            get { return ms1AlignmentFactor; }
            set { ms1AlignmentFactor = value; }
        }

        [Key(22)]
        public float Ms2SimilarityAlignmentFactor {
            get { return ms2SimilarityAlignmentFactor; }
            set { ms2SimilarityAlignmentFactor = value; }
        }

        [Key(23)]
        public float Ms1AlignmentTolerance {
            get { return ms1AlignmentTolerance; }
            set { ms1AlignmentTolerance = value; }
        }

        [Key(24)]
        public float RetentionTimeAlignmentTolerance {
            get { return retentionTimeAlignmentTolerance; }
            set { retentionTimeAlignmentTolerance = value; }
        }

        [Key(25)]
        public float AlignmentScoreCutOff {
            get { return alignmentScoreCutOff; }
            set { alignmentScoreCutOff = value; }
        }

        [Key(26)]
        public float RetentionTimeLibrarySearchTolerance {
            get { return retentionTimeLibrarySearchTolerance; }
            set { retentionTimeLibrarySearchTolerance = value; }
        }

        [Key(27)]
        public float Ms1LibrarySearchTolerance {
            get { return ms1LibrarySearchTolerance; }
            set { ms1LibrarySearchTolerance = value; }
        }

        [Key(28)]
        public float Ms2LibrarySearchTolerance {
            get { return ms2LibrarySearchTolerance; }
            set { ms2LibrarySearchTolerance = value; }
        }

        [Key(29)]
        public float IdentificationScoreCutOff {
            get { return identificationScoreCutOff; }
            set { identificationScoreCutOff = value; }
        }

        [Key(30)]
        public float RetentionTimeToleranceOfPostIdentification {
            get { return retentionTimeToleranceOfPostIdentification; }
            set { retentionTimeToleranceOfPostIdentification = value; }
        }

        [Key(31)]
        public float AccurateMassToleranceOfPostIdentification {
            get { return accurateMassToleranceOfPostIdentification; }
            set { accurateMassToleranceOfPostIdentification = value; }
        }

        [Key(32)]
        public float PostIdentificationScoreCutOff {
            get { return postIdentificationScoreCutOff; }
            set { postIdentificationScoreCutOff = value; }
        }

        [Key(33)]
        public bool OnlyReportTopHitForPostAnnotation {
            get { return onlyReportTopHitForPostAnnotation; }
            set { onlyReportTopHitForPostAnnotation = value; }
        }

        [Key(34)]
        public float RelativeAbundanceCutOff {
            get { return relativeAbundanceCutOff; }
            set { relativeAbundanceCutOff = value; }
        }

        [Key(35)]
        public int AlignmentReferenceFileID {
            get { return alignmentReferenceFileID; }
            set { alignmentReferenceFileID = value; }
        }

        [Key(36)]
        public int BandWidth {
            get { return bandWidth; }
            set { bandWidth = value; }
        }

        [Key(37)]
        public float SigmaWindowValue {
            get { return sigmaWindowValue; }
            set { sigmaWindowValue = value; }
        }

        [Key(38)]
        public int SegmentNumber {
            get { return segmentNumber; }
            set { segmentNumber = value; }
        }

        [Key(39)]
        public DeconvolutionType DeconvolutionType {
            get { return deconvolutionType; }
            set { deconvolutionType = value; }
        }

        [Key(40)]
        public float AmplitudeCutoff {
            get { return amplitudeCutoff; }
            set { amplitudeCutoff = value; }
        }

        [Key(41)]
        public bool RemoveAfterPrecursor {
            get { return removeAfterPrecursor; }
            set { removeAfterPrecursor = value; }
        }

        [Key(42)]
        public float AdductAndIsotopeMassTolerance {
            get { return adductAndIsotopeMassTolerance; }
            set { adductAndIsotopeMassTolerance = value; }
        }

        [Key(43)]
        public float PeakCountFilter {
            get { return peakCountFilter; }
            set { peakCountFilter = value; }
        }

        [Key(44)]
        public bool QcAtLeastFilter {
            get { return qcAtLeastFilter; }
            set { qcAtLeastFilter = value; }
        }

        [Key(45)]
        public bool GapFillingOption {
            get { return gapFillingOption; }
            set { gapFillingOption = value; }
        }

        [Key(46)]
        public bool TrackingIsotopeLabels {
            get { return trackingIsotopeLabels; }
            set { trackingIsotopeLabels = value; }
        }

        [Key(47)]
        public IsotopeTrackingDictionary IsotopeTrackingDictionary {
            get { return isotopeTrackingDictionary; }
            set { isotopeTrackingDictionary = value; }
        }

        [Key(48)]
        public int NonLabeledReferenceID {
            get { return nonLabeledReferenceID; }
            set { nonLabeledReferenceID = value; }
        }

        [Key(49)]
        public bool UseTargetFormulaLibrary {
            get { return useTargetFormulaLibrary; }
            set { useTargetFormulaLibrary = value; }
        }

        [Key(50)]
        public bool TogetherWithAlignment {
            get { return togetherWithAlignment; }
            set { togetherWithAlignment = value; }
        }

        [Key(51)]
        public bool IsNormalizeNone {
            get { return isNormalizeNone; }
            set { isNormalizeNone = value; }
        }

        [Key(52)]
        public bool IsNormalizeIS {
            get { return isNormalizeIS; }
            set { isNormalizeIS = value; }
        }

        [Key(53)]
        public bool IsNormalizeLowess {
            get { return isNormalizeLowess; }
            set { isNormalizeLowess = value; }
        }

        [Key(54)]
        public bool IsNormalizeIsLowess {
            get { return isNormalizeIsLowess; }
            set { isNormalizeIsLowess = value; }
        }

        [Key(55)]
        public bool IsNormalizeTic {
            get { return isNormalizeTic; }
            set { isNormalizeTic = value; }
        }

        [Key(56)]
        public bool IsNormalizeMTic {
            get { return isNormalizeMTic; }
            set { isNormalizeMTic = value; }
        }

        [Key(57)]
        public double LowessSpan {
            get { return lowessSpan; }
            set { lowessSpan = value; }
        }

        [Key(58)]
        public bool IsBlankSubtract {
            get { return isBlankSubtract; }
            set { isBlankSubtract = value; }
        }

        [Key(59)]
        public TransformMethod Transform {
            get { return transform; }
            set { transform = value; }
        }

        [Key(60)]
        public ScaleMethod Scale {
            get { return scale; }
            set { scale = value; }
        }

        [Key(61)]
        public int MaxComponent {
            get { return maxComponent; }
            set { maxComponent = value; }
        }


        [Key(62)]
        public float MpMs1Tolerance {
            get { return mpMs1Tolerance; }
            set { mpMs1Tolerance = value; }
        }

        [Key(63)]
        public float MpMs2Tolerance {
            get { return mpMs2Tolerance; }
            set { mpMs2Tolerance = value; }
        }

        [Key(64)]
        public float MpRtTolerance {
            get { return mpRtTolerance; }
            set { mpRtTolerance = value; }
        }

        [Key(65)]
        public int MpTopN {
            get { return mpTopN; }
            set { mpTopN = value; }
        }

        [Key(66)]
        public bool MpIsIncludeMsLevel1 {
            get { return mpIsIncludeMsLevel1; }
            set { mpIsIncludeMsLevel1 = value; }
        }

        [Key(67)]
        public bool MpIsUseMs1LevelForQuant {
            get { return mpIsUseMs1LevelForQuant; }
            set { mpIsUseMs1LevelForQuant = value; }
        }

        [Key(68)]
        public bool MpIsFocusedSpotOutput {
            get { return mpIsFocusedSpotOutput; }
            set { mpIsFocusedSpotOutput = value; }
        }

        [Key(69)]
        public bool MpIsReferenceBaseOutput {
            get { return mpIsReferenceBaseOutput; }
            set { mpIsReferenceBaseOutput = value; }
        }

        [Key(70)]
        public bool SetFullyLabeledReferenceFile {
            get {
                return setFullyLabeledReferenceFile;
            }

            set {
                setFullyLabeledReferenceFile = value;
            }
        }

        [Key(71)]
        public int FullyLabeledReferenceID {
            get {
                return fullyLabeledReferenceID;
            }

            set {
                fullyLabeledReferenceID = value;
            }
        }

        [Key(72)]
        public double RelativeAbundanceCutOffForSpectrumExport {
            get { return relativeAbundanceCutOffForSpectrumExport; }
            set { relativeAbundanceCutOffForSpectrumExport = value; }
        }

        [Key(73)]
        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get { return isIdentificationOnlyPerformedForAlignmentFile; }
            set { isIdentificationOnlyPerformedForAlignmentFile = value; }
        }

        [Key(74)]
        public bool IsUseRetentionInfoForIdentificationScoring {
            get { return isUseRetentionInfoForIdentificationScoring; }
            set { isUseRetentionInfoForIdentificationScoring = value; }
        }

        [Key(75)]
        public float KeptIsotopeRange {
            get {
                return keptIsotopeRange;
            }

            set {
                keptIsotopeRange = value;
            }
        }

        [Key(76)]
        public float NPercentDetectedInOneGroup {
            get {
                return nPercentDetectedInOneGroup;
            }

            set {
                nPercentDetectedInOneGroup = value;
            }
        }

        [Key(77)]
        public bool KeepOriginalPrecursorIsotopes {
            get {
                return keepOriginalPrecursorIsotopes;
            }

            set {
                keepOriginalPrecursorIsotopes = value;
            }
        }

        [Key(78)]
        public int MaxChargeNumber {
            get {
                return maxChargeNumber;
            }

            set {
                maxChargeNumber = value;
            }
        }

        [Key(79)]
        public List<FragmentSearcherQuery> FragmentSearcherQueries {
            get {
                return fragmentSearcherQueries;
            }

            set {
                fragmentSearcherQueries = value;
            }
        }

        [Key(80)]
        public bool IsAndAsFragmentSearcherOption {
            get {
                return isAndAsFragmentSearcherOption;
            }

            set {
                isAndAsFragmentSearcherOption = value;
            }
        }

        [Key(81)]
        public int NumThreads {
            get { return numThreads; }
            set { numThreads = value; }
        }

        [Key(82)]
        public bool IsIonMobility {
            get {
                return isIonMobility;
            }

            set {
                isIonMobility = value;
            }
        }

        [Key(83)]
        public List<ExtractedIonChromatogramDisplaySettingBean> EicDisplayQueries {
            get {
                return eicDisplayQueries;
            }

            set {
                eicDisplayQueries = value;
            }
        }

        [Key(84)]
        public List<AdductDiff> AddictDiffQueries {
            get {
                return addictDiffQueries;
            }

            set {
                addictDiffQueries = value;
            }
        }

        [Key(85)]
        public double MnRtTolerance {
            get {
                return mnRtTolerance;
            }

            set {
                mnRtTolerance = value;
            }
        }

        [Key(86)]
        public double MnIonCorrelationSimilarityCutOff {
            get {
                return mnIonCorrelationSimilarityCutOff;
            }

            set {
                mnIonCorrelationSimilarityCutOff = value;
            }
        }

        [Key(87)]
        public double MnSpectrumSimilarityCutOff {
            get {
                return mnSpectrumSimilarityCutOff;
            }

            set {
                mnSpectrumSimilarityCutOff = value;
            }
        }

        [Key(88)]
        public double MnRelativeAbundanceCutOff {
            get {
                return mnRelativeAbundanceCutOff;
            }

            set {
                mnRelativeAbundanceCutOff = value;
            }
        }

        [Key(89)]
        public double MnMassTolerance {
            get {
                return mnMassTolerance;
            }

            set {
                mnMassTolerance = value;
            }
        }

        [Key(90)]
        public bool MnIsExportIonCorrelation {
            get {
                return mnIsExportIonCorrelation;
            }

            set {
                mnIsExportIonCorrelation = value;
            }
        }

        [Key(91)]
        public bool IsRemoveFeatureBasedOnPeakHeightFoldChange {
            get {
                return isRemoveFeatureBasedOnPeakHeightFoldChange;
            }

            set {
                isRemoveFeatureBasedOnPeakHeightFoldChange = value;
            }
        }

        [Key(92)]
        public float SampleMaxOverBlankAverage {
            get {
                return sampleMaxOverBlankAverage;
            }

            set {
                sampleMaxOverBlankAverage = value;
            }
        }

        [Key(93)]
        public float SampleAverageOverBlankAverage {
            get {
                return sampleAverageOverBlankAverage;
            }

            set {
                sampleAverageOverBlankAverage = value;
            }
        }

        [Key(94)]
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get {
                return isKeepRemovableFeaturesAndAssignedTagForChecking;
            }

            set {
                isKeepRemovableFeaturesAndAssignedTagForChecking = value;
            }
        }

        [Key(95)]
        public bool IsKeepIdentifiedMetaboliteFeatures {
            get {
                return isKeepIdentifiedMetaboliteFeatures;
            }

            set {
                isKeepIdentifiedMetaboliteFeatures = value;
            }
        }

        [Key(96)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get {
                return isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            }

            set {
                isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value;
            }
        }

        [Key(97)]
        public BlankFiltering BlankFiltering {
            get {
                return blankFiltering;
            }

            set {
                blankFiltering = value;
            }
        }

        [Key(98)]
        public bool IsKeepAnnotatedMetaboliteFeatures {
            get {
                return isKeepAnnotatedMetaboliteFeatures;
            }

            set {
                isKeepAnnotatedMetaboliteFeatures = value;
            }
        }

        [Key(99)]
        public float FoldChangeForBlankFiltering {
            get {
                return foldChangeForBlankFiltering;
            }

            set {
                foldChangeForBlankFiltering = value;
            }
        }

        [Key(100)]
        public RetentionTimeCorrection.RetentionTimeCorrectionCommon RetentionTimeCorrectionCommon { get; set; }
            = new RetentionTimeCorrection.RetentionTimeCorrectionCommon();

        [Key(101)]
        public bool MpIsExportOtherCandidates {
            get {
                return mpIsExportOtherCandidates;
            }

            set {
                mpIsExportOtherCandidates = value;
            }
        }

        [Key(102)]
        public float MpIdentificationScoreCutOff {
            get {
                return mpIdentificationScoreCutOff;
            }

            set {
                mpIdentificationScoreCutOff = value;
            }
        }

        [Key(103)]
        public bool IsBrClConsideredForIsotopes {
            get {
                return isBrClConsideredForIsotopes;
            }

            set {
                isBrClConsideredForIsotopes = value;
            }
        }

        [Key(104)]
        public TransformMethod TransformPls {
            get {
                return transformPls;
            }

            set {
                transformPls = value;
            }
        }

        [Key(105)]
        public ScaleMethod ScalePls {
            get {
                return scalePls;
            }

            set {
                scalePls = value;
            }
        }

        [Key(106)]
        public bool IsAutoFitPls {
            get {
                return isAutoFitPls;
            }

            set {
                isAutoFitPls = value;
            }
        }

        [Key(107)]
        public int ComponentPls {
            get {
                return componentPls;
            }

            set {
                componentPls = value;
            }
        }

        [Key(108)]
        public MultivariateAnalysisOption MultivariateAnalysisOption {
            get {
                return multivariateAnalysisOption;
            }

            set {
                multivariateAnalysisOption = value;
            }
        }

        [Key(109)]
        public bool IsForceInsertForGapFilling {
            get {
                return isForceInsertForGapFilling;
            }

            set {
                isForceInsertForGapFilling = value;
            }
        }

        [Key(110)]
        public bool IsIdentifiedImportedInStatistics {
            get {
                return isIdentifiedImportedInStatistics;
            }

            set {
                isIdentifiedImportedInStatistics = value;
            }
        }

        [Key(111)]
        public bool IsAnnotatedImportedInStatistics {
            get {
                return isAnnotatedImportedInStatistics;
            }

            set {
                isAnnotatedImportedInStatistics = value;
            }
        }

        [Key(112)]
        public bool IsUnknownImportedInStatistics {
            get {
                return isUnknownImportedInStatistics;
            }

            set {
                isUnknownImportedInStatistics = value;
            }
        }

        [Key(113)]
        public AnalysisParamOfMsdialCorrDec AnalysisParamOfMsdialCorrDec {
            get {
                return analysisParamOfMsdialCorrDec;
            }

            set {
                analysisParamOfMsdialCorrDec = value;
			}
		}
		
		
        [Key(114)]
        public List<TextFormatCompoundInformationBean> CompoundListInTargetMode {
            get {
                return compoundListInTargetMode;
            }
            set {
                compoundListInTargetMode = value;
            }
        }
		
		[Key(115)]
        public List<StandardCompound> StandardCompounds {
            get {
                return standardCompounds;
            }

            set {
                standardCompounds = value;
            }
        }

        [Key(116)]
        public bool IsNormalizeSplash {
            get {
                return isNormalizeSplash;
            }

            set {
                isNormalizeSplash = value;
            }
        }

        [Key(117)]
        public float DriftTimeAlignmentTolerance {
            get {
                return driftTimeAlignmentTolerance;
            }

            set {
                driftTimeAlignmentTolerance = value;
            }
        }

        [Key(118)]
        public IonMobilityType IonMobilityType {
            get {
                return ionMobilityType;
            }

            set {
                ionMobilityType = value;
            }
        }

        [Key(119)]
        public float DriftTimeAlignmentFactor {
            get {
                return driftTimeAlignmentFactor;
            }

            set {
                driftTimeAlignmentFactor = value;
            }
        }

        [Key(120)]
        public float AccumulatedRtRagne {
            get {
                return accumulatedRtRagne;
            }

            set {
                accumulatedRtRagne = value;
            }
        }

        [Key(121)]
        public float CcsSearchTolerance {
            get {
                return ccsSearchTolerance;
            }

            set {
                ccsSearchTolerance = value;
            }
        }

        [Key(122)]
        public bool IsUseRetentionInfoForIdentificationFiltering {
            get {
                return isUseRetentionInfoForIdentificationFiltering;
            }

            set {
                isUseRetentionInfoForIdentificationFiltering = value;
            }
        }

        [Key(123)]
        public bool IsUseCcsForIdentificationFiltering {
            get {
                return isUseCcsForIdentificationFiltering;
            }

            set {
                isUseCcsForIdentificationFiltering = value;
            }
        }

        [Key(124)]
        public bool IsUseCcsForIdentificationScoring {
            get {
                return isUseCcsForIdentificationScoring;
            }

            set {
                isUseCcsForIdentificationScoring = value;
            }
        }

        [Key(125)]
        public string MsdialVersionNumber {
            get {
                return msdialVersionNumber;
            }

            set {
                msdialVersionNumber = value;
            }
        }

        [Key(126)]
        public bool IsUseSimpleDotScore {
            get {
                return isUseSimpleDotScore;
            }
            set {
                isUseSimpleDotScore = value;
            }
        }

        [Key(127)]
        public Dictionary<int, CoefficientsForCcsCalculation> FileidToCcsCalibrantData {
            get {
                return fileidToCcsCalibrantData;
            }

            set {
                fileidToCcsCalibrantData = value;
            }
        }

        [Key(128)]
        public bool IsAllCalibrantDataImported {
            get {
                return isAllCalibrantDataImported;
            }

            set {
                isAllCalibrantDataImported = value;
            }
        }

        [Key(129)]
        public float Ms2MassRangeBegin {
            get {
                return ms2MassRangeBegin;
            }

            set {
                ms2MassRangeBegin = value;
            }
        }

        [Key(130)]
        public float Ms2MassRangeEnd {
            get {
                return ms2MassRangeEnd;
            }

            set {
                ms2MassRangeEnd = value;
            }
        }
        #endregion
    }
}
