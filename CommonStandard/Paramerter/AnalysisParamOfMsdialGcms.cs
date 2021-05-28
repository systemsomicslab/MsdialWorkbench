using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public enum RetentionType { RI, RT }
    public enum RiCompoundType { Alkanes, Fames }
    public enum AlignmentIndexType { RT, RI }

    [DataContract]
    [MessagePackObject]
    public class RiDictionaryInfo
    {
        [DataMember]
        private string dictionaryFilePath;
        [DataMember]
        private Dictionary<int, float> riDictionary;

        public RiDictionaryInfo()
        {
            dictionaryFilePath = string.Empty;
            riDictionary = new Dictionary<int, float>();
        }

        [Key(0)]
        public string DictionaryFilePath
        {
            get { return dictionaryFilePath; }
            set { dictionaryFilePath = value; }
        }

        [Key(1)]
        public Dictionary<int, float> RiDictionary
        {
            get { return riDictionary; }
            set { riDictionary = value; }
        }
    }

    [DataContract]
    [MessagePackObject]
    public class AnalysisParamOfMsdialGcms
    {
        [Key(102)]
        private string msdialVersionNumber;
        [Key(103)]
        private ProcessOption processOption;
        [Key(104)]
        [DataMember]
        private DataType dataType;
        [Key(105)]
        [DataMember]
        private IonMode ionMode;
        [Key(106)]
        [DataMember]
        private AccuracyType accuracyType;
        [Key(107)]
        [DataMember]
        private float retentionTimeBegin;
        [Key(108)]
        [DataMember]
        private float retentionTimeEnd;
        [Key(109)]
        [DataMember]
        private float massRangeBegin;
        [Key(110)]
        [DataMember]
        private float massRangeEnd;
        [Key(111)]
        [DataMember]
        private int numThreads;
        [Key(112)]
        [DataMember]
        private float massAccuracy;
        [Key(113)]
        [DataMember]
        private SmoothingMethod smoothingMethod;
        [Key(114)]
        [DataMember]
        private int smoothingLevel;
        [Key(115)]
        [DataMember]
        private double amplitudeNoiseFactor;
        [Key(116)]
        [DataMember]
        private double slopeNoiseFactor;
        [Key(117)]
        [DataMember]
        private double peaktopNoiseFactor;
        [Key(118)]
        [DataMember]
        private double minimumDatapoints;
        [Key(119)]
        [DataMember]
        private double minimumAmplitude;
        [Key(120)]
        [DataMember]
        private int averagePeakWidth;
        [Key(121)]
        [DataMember]
        private float massSliceWidth;
        [Key(122)]
        [DataMember]
        private bool backgroundSubtraction;
        [Key(123)]
        private List<ExcludeMassBean> excludedMassList;
        [Key(124)]
        [DataMember]
        private float amplitudeCutoff;
        [Key(125)]
        [DataMember]
        private float sigmaWindowValue;
        [Key(126)]
        [DataMember]
        private bool isReplaceQuantmassByUserDefinedValue;
        [Key(127)]
        [DataMember]
        private string mspFilePath;
        [Key(128)]
        [DataMember]
        private string riDictionaryFilePath;
        [Key(129)]
        [DataMember]
        private RetentionType retentionType;
        [Key(130)]
        [DataMember]
        private RiCompoundType riCompoundType;
        [Key(131)]
        [DataMember]
        private Dictionary<int, float> riDictionary;
        [Key(132)]
        [DataMember]
        private Dictionary<int, RiDictionaryInfo> fileIdRiInfoDictionary;
        [Key(133)]
        [DataMember]
        private float retentionTimeLibrarySearchTolerance;
        [Key(134)]
        [DataMember]
        private float retentionIndexLibrarySearchTolerance;
        [Key(135)]
        [DataMember]
        private float eiSimilarityLibrarySearchCutOff;
        [Key(136)]
        [DataMember]
        private float identificationScoreCutOff;
        [Key(137)]
        [DataMember]
        private float mzLibrarySearchTolerance;
        [Key(138)]
        [DataMember]
        private int maxMspDisplayNumber;
        [Key(139)]
        [DataMember]
        private bool isIdentificationOnlyPerformedForAlignmentFile;
        [Key(140)]
        [DataMember]
        private bool isUseRetentionInfoForIdentificationScoring;
        [Key(141)]
        private bool isUseRetentionInfoForIdentificationFiltering;
        [Key(142)]
        private bool isOnlyTopHitReport;
        [Key(143)]
        [DataMember]
        private AlignmentIndexType alignmentIndexType;
        [Key(144)]
        [DataMember]
        private int alignmentReferenceFileID;
        [Key(145)]
        [DataMember]
        private float retentionTimeAlignmentFactor;
        [Key(146)]
        [DataMember]
        private float retentionTimeAlignmentTolerance;
        [Key(147)]
        [DataMember]
        private float eiSimilarityAlignmentCutOff;
        [Key(148)]
        [DataMember]
        private float eiSimilarityAlignmentFactor;
        [Key(149)]
        [DataMember]
        private float retentionIndexAlignmentTolerance;
        [Key(150)]
        [DataMember]
        private float alignmentScoreCutOff;
        [Key(151)]
        [DataMember]
        private bool togetherWithAlignment;
        [Key(152)]
        [DataMember]
        private bool isForceInsertForGapFilling;
        [Key(153)]
        private bool isRepresentativeQuantMassBasedOnBasePeakMz;
        [Key(154)]
        [DataMember]
        private float peakCountFilter;
        [Key(155)]
        [DataMember]
        private bool qcAtLeastFilter;
        [Key(156)]
        [DataMember]
        private bool gapFillingOption;
        [Key(157)]
        [DataMember]
        private bool isRemoveFeatureBasedOnPeakHeightFoldChange;
        [Key(158)]
        [DataMember]
        private float sampleMaxOverBlankAverage;
        [Key(159)]
        [DataMember]
        private float sampleAverageOverBlankAverage;
        [Key(160)]
        [DataMember]
        private float foldChangeForBlankFiltering;
        [Key(161)]
        [DataMember]
        private bool isKeepRemovableFeaturesAndAssignedTagForChecking;
        [Key(162)]
        [DataMember]
        private bool isKeepIdentifiedMetaboliteFeatures;
        [Key(163)]
        [DataMember]
        private bool isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
        [Key(164)]
        [DataMember]
        private bool isKeepAnnotatedMetaboliteFeatures;
        [Key(165)]
        [DataMember]
        private BlankFiltering blankFiltering;
        [Key(166)]
        private float nPercentDetectedInOneGroup;
        [Key(167)]
        [DataMember]
        private bool isNormalizeNone;
        [Key(168)]
        [DataMember]
        private bool isNormalizeIS;
        [Key(169)]
        [DataMember]
        private bool isNormalizeLowess;
        [Key(170)]
        [DataMember]
        private bool isNormalizeIsLowess;
        [Key(171)]
        [DataMember]
        private bool isNormalizeTic;
        [Key(172)]
        [DataMember]
        private bool isNormalizeMTic;
        [Key(173)]
        [DataMember]
        private bool isBlankSubtract;
        [Key(174)]
        private bool isNormalizeSplash;
        [Key(175)]
        [DataMember]
        private double lowessSpan;
        [Key(176)]
        private List<StandardCompound> standardCompounds;
        [Key(177)]
        [DataMember]
        private TransformMethod transform;
        [Key(178)]
        [DataMember]
        private ScaleMethod scale;
        [Key(179)]
        [DataMember]
        private int maxComponent;
        [Key(180)]
        [DataMember]
        private TransformMethod transformPls;
        [Key(181)]
        [DataMember]
        private ScaleMethod scalePls;
        [Key(182)]
        [DataMember]
        private bool isAutoFitPls;
        [Key(183)]
        [DataMember]
        private int componentPls;
        [Key(184)]
        [DataMember]
        private MultivariateAnalysisOption multivariateAnalysisOption;
        [Key(185)]
        [DataMember]
        private bool isIdentifiedImportedInStatistics;
        [Key(186)]
        [DataMember]
        private bool isAnnotatedImportedInStatistics;
        [Key(187)]
        [DataMember]
        private bool isUnknownImportedInStatistics;
        [Key(188)]
        [DataMember]
        private float mpMs1Tolerance;
        [Key(189)]
        [DataMember]
        private float mpMs2Tolerance;
        [Key(190)]
        [DataMember]
        private float mpRtTolerance;
        [Key(191)]
        [DataMember]
        private int mpTopN;
        [Key(192)]
        [DataMember]
        private bool mpIsIncludeMsLevel1;
        [Key(193)]
        [DataMember]
        private bool mpIsUseMs1LevelForQuant;
        [Key(194)]
        [DataMember]
        private bool mpIsFocusedSpotOutput;
        [Key(195)]
        [DataMember]
        private bool mpIsReferenceBaseOutput;
        [Key(196)]
        [DataMember]
        private bool mpIsExportOtherCandidates;
        [Key(197)]
        [DataMember]
        private float mpIdentificationScoreCutOff;
        [Key(198)]
        [DataMember]
        private List<ExtractedIonChromatogramDisplaySettingBean> eicDisplayQueries;
        [Key(199)]
        [DataMember]
        private bool mnIsExportIonCorrelation;
        [Key(200)]
        [DataMember]
        private double mnMassTolerance;
        [Key(201)]
        [DataMember]
        private double mnRelativeAbundanceCutOff;
        [Key(202)]
        [DataMember]
        private double mnSpectrumSimilarityCutOff;
        [Key(203)]
        [DataMember]
        private double mnIonCorrelationSimilarityCutOff;
        [Key(204)]
        [DataMember]
        private double mnRtTolerance;

        public AnalysisParamOfMsdialGcms()
        {
            //Process option
            ProcessOption = ProcessOption.All;
            AccuracyType = AccuracyType.IsNominal;
            IonMode = IonMode.Positive;

            //data correction
            MassRangeBegin = 0;
            MassRangeEnd = 1000;
            RetentionTimeBegin = 0;
            RetentionTimeEnd = 100;
            numThreads = 1;

            //peak detection
            SmoothingMethod = SmoothingMethod.LinearWeightedMovingAverage;
            SmoothingLevel = 3;
            MassSliceWidth = 0.1F;
            BackgroundSubtraction = true;
            SlopeNoiseFactor = 2;
            AmplitudeNoiseFactor = 4;
            MinimumAmplitude = 1000;
            MinimumDatapoints = 10;
            PeaktopNoiseFactor = 2;
            AveragePeakWidth = 20;
            MassAccuracy = 0.025F;
            ExcludedMassList = new List<ExcludeMassBean>(); 


            //MS1Dec
            AmplitudeCutoff = 10;
            SigmaWindowValue = 0.5F;
            IsReplaceQuantmassByUserDefinedValue = false;

            //Identification
            RiDictionaryFilePath = string.Empty;
            MspFilePath = string.Empty;
            RetentionType = RetentionType.RI;
            RiCompoundType = RiCompoundType.Alkanes;
            RiDictionary = new Dictionary<int, float>();
            FileIdRiInfoDictionary = new Dictionary<int, RiDictionaryInfo>();
            RetentionIndexLibrarySearchTolerance = 20;
            RetentionTimeLibrarySearchTolerance = 0.5F;
            EiSimilarityLibrarySearchCutOff = 70;
            IdentificationScoreCutOff = 70;
            MzLibrarySearchTolerance = 0.5F;
            MaxMspDisplayNumber = 100;
            IsIdentificationOnlyPerformedForAlignmentFile = false;
            IsUseRetentionInfoForIdentificationScoring = true;
            IsOnlyTopHitReport = true;

            //Alignment
            AlignmentIndexType = AlignmentIndexType.RT;
            RetentionTimeAlignmentTolerance = 0.075F;
            RetentionIndexAlignmentTolerance = 20;
            EiSimilarityAlignmentCutOff = 70;
            RetentionTimeAlignmentFactor = 0.5F;
            EiSimilarityAlignmentFactor = 0.5F;
            AlignmentScoreCutOff = 70;
            AlignmentReferenceFileID = 0;
            TogetherWithAlignment = true;

            //Alignment filtering
            QcAtLeastFilter = true;
            PeakCountFilter = 0;
            GapFillingOption = true;
            IsRemoveFeatureBasedOnPeakHeightFoldChange = false;
            SampleMaxOverBlankAverage = 5;
            SampleAverageOverBlankAverage = 5;
            FoldChangeForBlankFiltering = 5;
            IsKeepRemovableFeaturesAndAssignedTagForChecking = true;
            IsKeepIdentifiedMetaboliteFeatures = true;
            IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = false;
            IsKeepAnnotatedMetaboliteFeatures = false;
            BlankFiltering = BlankFiltering.SampleMaxOverBlankAve;
            IsForceInsertForGapFilling = true;
            NPercentDetectedInOneGroup = 0;

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
            IsAnnotatedImportedInStatistics = true;
            IsUnknownImportedInStatistics = true;

            //Mrmprobs
            mpMs1Tolerance = 0.2F;
            mpMs2Tolerance = 0.2F;
            mpRtTolerance = 0.5F;
            mpTopN = 5;
            mpIsIncludeMsLevel1 = true;
            mpIsUseMs1LevelForQuant = false;
            mpIsFocusedSpotOutput = false;
            mpIsReferenceBaseOutput = true;
            mpIsExportOtherCandidates = false;
            mpIdentificationScoreCutOff = 80.0F;

            //Molecular networking
            MnIsExportIonCorrelation = false;
            MnIonCorrelationSimilarityCutOff = 95;
            MnMassTolerance = 0.5;
            MnRtTolerance = 100;
            MnSpectrumSimilarityCutOff = 90;
            MnRelativeAbundanceCutOff = 1;

            //Viewer
            eicDisplayQueries = new List<ExtractedIonChromatogramDisplaySettingBean>();
        }

        #region

        [Key(0)]
        public ProcessOption ProcessOption
        {
            get { return processOption; }
            set { processOption = value; }
        }

        [Key(1)]
        public string MspFilePath
        {
            get { return mspFilePath; }
            set { mspFilePath = value; }
        }

        [Key(2)]
        public string RiDictionaryFilePath
        {
            get { return riDictionaryFilePath; }
            set { riDictionaryFilePath = value; }
        }

        [Key(3)]
        public DataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        [Key(4)]
        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        [Key(5)]
        public AccuracyType AccuracyType
        {
            get { return accuracyType; }
            set { accuracyType = value; }
        }

        [Key(6)]
        public SmoothingMethod SmoothingMethod
        {
            get { return smoothingMethod; }
            set { smoothingMethod = value; }
        }

        [Key(7)]
        public double MinimumAmplitude
        {
            get { return minimumAmplitude; }
            set { minimumAmplitude = value; }
        }

        [Key(8)]
        public float MzLibrarySearchTolerance
        {
            get { return mzLibrarySearchTolerance; }
            set { mzLibrarySearchTolerance = value; }
        }

        [Key(9)]
        public int SmoothingLevel
        {
            get { return smoothingLevel; }
            set { smoothingLevel = value; }
        }

        [Key(10)]
        public double AmplitudeNoiseFactor
        {
            get { return amplitudeNoiseFactor; }
            set { amplitudeNoiseFactor = value; }
        }

        [Key(11)]
        public double PeaktopNoiseFactor
        {
            get { return peaktopNoiseFactor; }
            set { peaktopNoiseFactor = value; }
        }

        [Key(12)]
        public double SlopeNoiseFactor
        {
            get { return slopeNoiseFactor; }
            set { slopeNoiseFactor = value; }
        }

        [Key(13)]
        public double MinimumDatapoints
        {
            get { return minimumDatapoints; }
            set { minimumDatapoints = value; }
        }

        [Key(14)]
        public float MassSliceWidth
        {
            get { return massSliceWidth; }
            set { massSliceWidth = value; }
        }

        [Key(15)]
        public int AveragePeakWidth
        {
            get { return averagePeakWidth; }
            set { averagePeakWidth = value; }
        }

        [Key(16)]
        public bool BackgroundSubtraction
        {
            get { return backgroundSubtraction; }
            set { backgroundSubtraction = value; }
        }

        [Key(17)]
        public float RetentionTimeBegin
        {
            get { return retentionTimeBegin; }
            set { retentionTimeBegin = value; }
        }

        [Key(18)]
        public float RetentionTimeEnd
        {
            get { return retentionTimeEnd; }
            set { retentionTimeEnd = value; }
        }

        [Key(19)]
        public float MassRangeBegin
        {
            get { return massRangeBegin; }
            set { massRangeBegin = value; }
        }

        [Key(20)]
        public float MassRangeEnd
        {
            get { return massRangeEnd; }
            set { massRangeEnd = value; }
        }

        [Key(21)]
        public float MassAccuracy
        {
            get { return massAccuracy; }
            set { massAccuracy = value; }
        }

        [Key(22)]
        public RetentionType RetentionType
        {
            get { return retentionType; }
            set { retentionType = value; }
        }

        [Key(23)]
        public RiCompoundType RiCompoundType
        {
            get { return riCompoundType; }
            set { riCompoundType = value; }
        }

        [Key(24)]
        public Dictionary<int, float> RiDictionary
        {
            get { return riDictionary; }
            set { riDictionary = value; }
        }

        [Key(25)]
        public Dictionary<int, RiDictionaryInfo> FileIdRiInfoDictionary
        {
            get { return fileIdRiInfoDictionary; }
            set { fileIdRiInfoDictionary = value; }
        }

        [Key(26)]
        public int MaxMspDisplayNumber
        {
            get { return maxMspDisplayNumber; }
            set { maxMspDisplayNumber = value; }
        }

        [Key(27)]
        public float RetentionTimeAlignmentFactor
        {
            get { return retentionTimeAlignmentFactor; }
            set { retentionTimeAlignmentFactor = value; }
        }

        [Key(28)]
        public float EiSimilarityAlignmentCutOff
        {
            get { return eiSimilarityAlignmentCutOff; }
            set { eiSimilarityAlignmentCutOff = value; }
        }

        [Key(29)]
        public float RetentionIndexLibrarySearchTolerance
        {
            get { return retentionIndexLibrarySearchTolerance; }
            set { retentionIndexLibrarySearchTolerance = value; }
        }

        [Key(30)]
        public AlignmentIndexType AlignmentIndexType
        {
            get { return alignmentIndexType; }
            set { alignmentIndexType = value; }
        }

        [Key(31)]
        public float RetentionTimeAlignmentTolerance
        {
            get { return retentionTimeAlignmentTolerance; }
            set { retentionTimeAlignmentTolerance = value; }
        }

        [Key(32)]
        public float AlignmentScoreCutOff
        {
            get { return alignmentScoreCutOff; }
            set { alignmentScoreCutOff = value; }
        }

        [Key(33)]
        public float EiSimilarityAlignmentFactor
        {
            get { return eiSimilarityAlignmentFactor; }
            set { eiSimilarityAlignmentFactor = value; }
        }

        [Key(34)]
        public float RetentionTimeLibrarySearchTolerance
        {
            get { return retentionTimeLibrarySearchTolerance; }
            set { retentionTimeLibrarySearchTolerance = value; }
        }

        [Key(35)]
        public float RetentionIndexAlignmentTolerance
        {
            get { return retentionIndexAlignmentTolerance; }
            set { retentionIndexAlignmentTolerance = value; }
        }

        [Key(36)]
        public float EiSimilarityLibrarySearchCutOff
        {
            get { return eiSimilarityLibrarySearchCutOff; }
            set { eiSimilarityLibrarySearchCutOff = value; }
        }

        [Key(37)]
        public float IdentificationScoreCutOff
        {
            get { return identificationScoreCutOff; }
            set { identificationScoreCutOff = value; }
        }

        [Key(38)]
        public int AlignmentReferenceFileID
        {
            get { return alignmentReferenceFileID; }
            set { alignmentReferenceFileID = value; }
        }

        [Key(39)]
        public float SigmaWindowValue
        {
            get { return sigmaWindowValue; }
            set { sigmaWindowValue = value; }
        }

        [Key(40)]
        public float AmplitudeCutoff
        {
            get { return amplitudeCutoff; }
            set { amplitudeCutoff = value; }
        }

        [Key(41)]
        public float PeakCountFilter
        {
            get { return peakCountFilter; }
            set { peakCountFilter = value; }
        }

        [Key(42)]
        public bool QcAtLeastFilter
        {
            get { return qcAtLeastFilter; }
            set { qcAtLeastFilter = value; }
        }

        [Key(43)]
        public bool GapFillingOption
        {
            get { return gapFillingOption; }
            set { gapFillingOption = value; }
        }

        [Key(44)]
        public bool TogetherWithAlignment
        {
            get { return togetherWithAlignment; }
            set { togetherWithAlignment = value; }
        }

        [Key(45)]
        public bool IsNormalizeNone
        {
            get { return isNormalizeNone; }
            set { isNormalizeNone = value; }
        }

        [Key(46)]
        public bool IsNormalizeIS
        {
            get { return isNormalizeIS; }
            set { isNormalizeIS = value; }
        }

        [Key(47)]
        public bool IsNormalizeLowess
        {
            get { return isNormalizeLowess; }
            set { isNormalizeLowess = value; }
        }

        [Key(48)]
        public bool IsNormalizeIsLowess
        {
            get { return isNormalizeIsLowess; }
            set { isNormalizeIsLowess = value; }
        }

        [Key(49)]
        public bool IsNormalizeTic
        {
            get { return isNormalizeTic; }
            set { isNormalizeTic = value; }
        }

        [Key(50)]
        public bool IsNormalizeMTic
        {
            get { return isNormalizeMTic; }
            set { isNormalizeMTic = value; }
        }

        [Key(51)]
        public double LowessSpan
        {
            get { return lowessSpan; }
            set { lowessSpan = value; }
        }

        [Key(52)]
        public bool IsBlankSubtract
        {
            get { return isBlankSubtract; }
            set { isBlankSubtract = value; }
        }

        [Key(53)]
        public TransformMethod Transform
        {
            get { return transform; }
            set { transform = value; }
        }

        [Key(54)]
        public ScaleMethod Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        [Key(55)]
        public int MaxComponent
        {
            get { return maxComponent; }
            set { maxComponent = value; }
        }

        [Key(56)]
        public float MpMs1Tolerance
        {
            get { return mpMs1Tolerance; }
            set { mpMs1Tolerance = value; }
        }

        [Key(57)]
        public float MpMs2Tolerance
        {
            get { return mpMs2Tolerance; }
            set { mpMs2Tolerance = value; }
        }

        [Key(58)]
        public float MpRtTolerance
        {
            get { return mpRtTolerance; }
            set { mpRtTolerance = value; }
        }

        [Key(59)]
        public int MpTopN
        {
            get { return mpTopN; }
            set { mpTopN = value; }
        }

        [Key(60)]
        public bool MpIsIncludeMsLevel1
        {
            get { return mpIsIncludeMsLevel1; }
            set { mpIsIncludeMsLevel1 = value; }
        }

        [Key(61)]
        public bool MpIsUseMs1LevelForQuant
        {
            get { return mpIsUseMs1LevelForQuant; }
            set { mpIsUseMs1LevelForQuant = value; }
        }

        [Key(62)]
        public bool MpIsFocusedSpotOutput
        {
            get { return mpIsFocusedSpotOutput; }
            set { mpIsFocusedSpotOutput = value; }
        }

        [Key(63)]
        public bool MpIsReferenceBaseOutput
        {
            get { return mpIsReferenceBaseOutput; }
            set { mpIsReferenceBaseOutput = value; }
        }

        [Key(64)]
        public bool IsIdentificationOnlyPerformedForAlignmentFile {
            get { return isIdentificationOnlyPerformedForAlignmentFile; }
            set { isIdentificationOnlyPerformedForAlignmentFile = value; }
        }

        [Key(65)]
        public bool IsUseRetentionInfoForIdentificationScoring {
            get { return isUseRetentionInfoForIdentificationScoring; }
            set { isUseRetentionInfoForIdentificationScoring = value; }
        }

        [Key(66)]
        public int NumThreads {
            get { return numThreads; }
            set { numThreads = value; }
        }

        [Key(67)]
        public List<ExtractedIonChromatogramDisplaySettingBean> EicDisplayQueries {
            get {
                return eicDisplayQueries;
            }

            set {
                eicDisplayQueries = value;
            }
        }

        [Key(68)]
        public double MnRtTolerance {
            get {
                return mnRtTolerance;
            }

            set {
                mnRtTolerance = value;
            }
        }

        [Key(69)]
        public double MnIonCorrelationSimilarityCutOff {
            get {
                return mnIonCorrelationSimilarityCutOff;
            }

            set {
                mnIonCorrelationSimilarityCutOff = value;
            }
        }

        [Key(70)]
        public double MnSpectrumSimilarityCutOff {
            get {
                return mnSpectrumSimilarityCutOff;
            }

            set {
                mnSpectrumSimilarityCutOff = value;
            }
        }

        [Key(71)]
        public double MnRelativeAbundanceCutOff {
            get {
                return mnRelativeAbundanceCutOff;
            }

            set {
                mnRelativeAbundanceCutOff = value;
            }
        }

        [Key(72)]
        public double MnMassTolerance {
            get {
                return mnMassTolerance;
            }

            set {
                mnMassTolerance = value;
            }
        }

        [Key(73)]
        public bool MnIsExportIonCorrelation {
            get {
                return mnIsExportIonCorrelation;
            }

            set {
                mnIsExportIonCorrelation = value;
            }
        }

        [Key(74)]
        public bool IsRemoveFeatureBasedOnPeakHeightFoldChange {
            get {
                return isRemoveFeatureBasedOnPeakHeightFoldChange;
            }

            set {
                isRemoveFeatureBasedOnPeakHeightFoldChange = value;
            }
        }

        [Key(75)]
        public float SampleMaxOverBlankAverage {
            get {
                return sampleMaxOverBlankAverage;
            }

            set {
                sampleMaxOverBlankAverage = value;
            }
        }

        [Key(76)]
        public float SampleAverageOverBlankAverage {
            get {
                return sampleAverageOverBlankAverage;
            }

            set {
                sampleAverageOverBlankAverage = value;
            }
        }

        [Key(77)]
        public bool IsKeepRemovableFeaturesAndAssignedTagForChecking {
            get {
                return isKeepRemovableFeaturesAndAssignedTagForChecking;
            }

            set {
                isKeepRemovableFeaturesAndAssignedTagForChecking = value;
            }
        }

        [Key(78)]
        public bool IsKeepIdentifiedMetaboliteFeatures {
            get {
                return isKeepIdentifiedMetaboliteFeatures;
            }

            set {
                isKeepIdentifiedMetaboliteFeatures = value;
            }
        }

        [Key(79)]
        public bool IsReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples {
            get {
                return isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples;
            }

            set {
                isReplaceTrueZeroValuesWithHalfOfMinimumPeakHeightOverAllSamples = value;
            }
        }

        [Key(80)]
        public BlankFiltering BlankFiltering {
            get {
                return blankFiltering;
            }

            set {
                blankFiltering = value;
            }
        }

        [Key(81)]
        public bool IsKeepAnnotatedMetaboliteFeatures {
            get {
                return isKeepAnnotatedMetaboliteFeatures;
            }

            set {
                isKeepAnnotatedMetaboliteFeatures = value;
            }
        }

        [Key(82)]
        public float FoldChangeForBlankFiltering {
            get {
                return foldChangeForBlankFiltering;
            }

            set {
                foldChangeForBlankFiltering = value;
            }
        }

        [Key(83)]
        public bool MpIsExportOtherCandidates {
            get {
                return mpIsExportOtherCandidates;
            }

            set {
                mpIsExportOtherCandidates = value;
            }
        }

        [Key(84)]
        public float MpIdentificationScoreCutOff {
            get {
                return mpIdentificationScoreCutOff;
            }

            set {
                mpIdentificationScoreCutOff = value;
            }
        }

        [Key(85)]
        public bool IsReplaceQuantmassByUserDefinedValue {
            get {
                return isReplaceQuantmassByUserDefinedValue;
            }

            set {
                isReplaceQuantmassByUserDefinedValue = value;
            }
        }


        [Key(86)]
        public TransformMethod TransformPls {
            get {
                return transformPls;
            }

            set {
                transformPls = value;
            }
        }

        [Key(87)]
        public ScaleMethod ScalePls {
            get {
                return scalePls;
            }

            set {
                scalePls = value;
            }
        }

        [Key(88)]
        public bool IsAutoFitPls {
            get {
                return isAutoFitPls;
            }

            set {
                isAutoFitPls = value;
            }
        }

        [Key(89)]
        public int ComponentPls {
            get {
                return componentPls;
            }

            set {
                componentPls = value;
            }
        }

        [Key(90)]
        public MultivariateAnalysisOption MultivariateAnalysisOption {
            get {
                return multivariateAnalysisOption;
            }

            set {
                multivariateAnalysisOption = value;
            }
        }

        [Key(91)]
        public bool IsForceInsertForGapFilling {
            get {
                return isForceInsertForGapFilling;
            }

            set {
                isForceInsertForGapFilling = value;
            }
        }

        [Key(92)]
        public bool IsIdentifiedImportedInStatistics {
            get {
                return isIdentifiedImportedInStatistics;
            }

            set {
                isIdentifiedImportedInStatistics = value;
            }
        }

        [Key(93)]
        public bool IsAnnotatedImportedInStatistics {
            get {
                return isAnnotatedImportedInStatistics;
            }

            set {
                isAnnotatedImportedInStatistics = value;
            }
        }

        [Key(94)]
        public bool IsUnknownImportedInStatistics {
            get {
                return isUnknownImportedInStatistics;
            }

            set {
                isUnknownImportedInStatistics = value;
            }
        }

        [Key(95)]
        public List<StandardCompound> StandardCompounds {
            get {
                return standardCompounds;
            }

            set {
                standardCompounds = value;
            }
        }

        [Key(96)]
        public bool IsNormalizeSplash {
            get {
                return isNormalizeSplash;
            }

            set {
                isNormalizeSplash = value;
            }
        }

        [Key(97)]
        public string MsdialVersionNumber {
            get {
                return msdialVersionNumber;
            }

            set {
                msdialVersionNumber = value;
            }
        }

        [Key(98)]
        public bool IsUseRetentionInfoForIdentificationFiltering {
            get {
                return isUseRetentionInfoForIdentificationFiltering;
            }

            set {
                isUseRetentionInfoForIdentificationFiltering = value;
            }
        }

        [Key(99)]
        public bool IsOnlyTopHitReport {
            get {
                return isOnlyTopHitReport;
            }

            set {
                isOnlyTopHitReport = value;
            }
        }

        [Key(100)]
        public bool IsRepresentativeQuantMassBasedOnBasePeakMz {
            get {
                return isRepresentativeQuantMassBasedOnBasePeakMz;
            }

            set {
                isRepresentativeQuantMassBasedOnBasePeakMz = value;
            }
        }

        [Key(101)]
        public List<ExcludeMassBean> ExcludedMassList {
            get {
                return excludedMassList;
            }

            set {
                excludedMassList = value;
            }
        }

        [Key(102)]
        public float NPercentDetectedInOneGroup {
            get {
                return nPercentDetectedInOneGroup;
            }

            set {
                nPercentDetectedInOneGroup = value;
            }
        }

        #endregion
    }
}
