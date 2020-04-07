using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;

namespace Rfx.Riken.OsakaUniv
{
    
    [DataContract]
    public class AnalysisParamOfMsdialMzML
    {
        //Data type
        [DataMember]
        private MethodType methodType;

        [DataMember]
        private DataType dataType;

        [DataMember]
        private IonMode ionMode;

        [DataMember]
        private Dictionary<int, AnalystExperimentInformationBean> experimentID_AnalystExperimentInformationBean;

        //Data collection
        [DataMember]
        private float retentionTimeBegin;
        [DataMember]
        private float retentionTimeEnd;
        [DataMember]
        private float massRangeBegin;
        [DataMember]
        private float massRangeEnd;

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
        private float peakCountFilter;
        [DataMember]
        private bool qcAtLeastFilter;
        [DataMember]
        private bool gapFillingOption;
        [DataMember]
        private bool togetherWithAlignment;

        //Tracking of isotope labeles
        [DataMember]
        private bool trackingIsotopeLabels;
        [DataMember]
        private bool fullyLabeledFileAvailability;
        [DataMember]
        private IsotopeTrackingDictionary isotopeTrackingDictionary;
        [DataMember]
        private int nonLabeledReferenceID;
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
        [DataMember]
        private double lowessSpan;

        //Statistics, PCA
        [DataMember]
        private TransformMethod transform;
        [DataMember]
        private ScaleMethod scale;
        [DataMember]
        private int maxComponent;

        #region
        public MethodType MethodType
        {
            get { return methodType; }
            set { methodType = value; }
        }

        public DataType DataType
        {
            get { return dataType; }
            set { dataType = value; }
        }

        public IonMode IonMode
        {
            get { return ionMode; }
            set { ionMode = value; }
        }

        public Dictionary<int, AnalystExperimentInformationBean> ExperimentID_AnalystExperimentInformationBean
        {
            get { return experimentID_AnalystExperimentInformationBean; }
            set { experimentID_AnalystExperimentInformationBean = value; }
        }

        public SmoothingMethod SmoothingMethod
        {
            get { return smoothingMethod; }
            set { smoothingMethod = value; }
        }

        public double MinimumAmplitude
        {
            get { return minimumAmplitude; }
            set { minimumAmplitude = value; }
        }

        public int SmoothingLevel
        {
            get { return smoothingLevel; }
            set { smoothingLevel = value; }
        }

        public double AmplitudeNoiseFactor
        {
            get { return amplitudeNoiseFactor; }
            set { amplitudeNoiseFactor = value; }
        }

        public double PeaktopNoiseFactor
        {
            get { return peaktopNoiseFactor; }
            set { peaktopNoiseFactor = value; }
        }

        public double SlopeNoiseFactor
        {
            get { return slopeNoiseFactor; }
            set { slopeNoiseFactor = value; }
        }

        public double MinimumDatapoints
        {
            get { return minimumDatapoints; }
            set { minimumDatapoints = value; }
        }

        public float MassSliceWidth
        {
            get { return massSliceWidth; }
            set { massSliceWidth = value; }
        }

        public bool BackgroundSubtraction
        {
            get { return backgroundSubtraction; }
            set { backgroundSubtraction = value; }
        }

        public List<ExcludeMassBean> ExcludedMassList
        {
            get { return excludedMassList; }
            set { excludedMassList = value; }
        }

        public List<AdductIonInformationBean> AdductIonInformationBeanList
        {
            get { return adductIonInformationBeanList; }
            set { adductIonInformationBeanList = value; }
        }

        public LipidQueryBean LipidQueryBean
        {
            get { return lipidQueryBean; }
            set { lipidQueryBean = value; }
        }

        public float RetentionTimeBegin
        {
            get { return retentionTimeBegin; }
            set { retentionTimeBegin = value; }
        }

        public float RetentionTimeEnd
        {
            get { return retentionTimeEnd; }
            set { retentionTimeEnd = value; }
        }

        public float MassRangeBegin
        {
            get { return massRangeBegin; }
            set { massRangeBegin = value; }
        }

        public float MassRangeEnd
        {
            get { return massRangeEnd; }
            set { massRangeEnd = value; }
        }

        public float CentroidMs1Tolerance
        {
            get { return centroidMs1Tolerance; }
            set { centroidMs1Tolerance = value; }
        }

        public float CentroidMs2Tolerance
        {
            get { return centroidMs2Tolerance; }
            set { centroidMs2Tolerance = value; }
        }

        public bool PeakDetectionBasedCentroid
        {
            get { return peakDetectionBasedCentroid; }
            set { peakDetectionBasedCentroid = value; }
        }

        public float RetentionTimeAlignmentFactor
        {
            get { return retentionTimeAlignmentFactor; }
            set { retentionTimeAlignmentFactor = value; }
        }

        public float Ms1AlignmentFactor
        {
            get { return ms1AlignmentFactor; }
            set { ms1AlignmentFactor = value; }
        }

        public float Ms2SimilarityAlignmentFactor
        {
            get { return ms2SimilarityAlignmentFactor; }
            set { ms2SimilarityAlignmentFactor = value; }
        }

        public float Ms1AlignmentTolerance
        {
            get { return ms1AlignmentTolerance; }
            set { ms1AlignmentTolerance = value; }
        }

        public float RetentionTimeAlignmentTolerance
        {
            get { return retentionTimeAlignmentTolerance; }
            set { retentionTimeAlignmentTolerance = value; }
        }

        public float AlignmentScoreCutOff
        {
            get { return alignmentScoreCutOff; }
            set { alignmentScoreCutOff = value; }
        }

        public float RetentionTimeLibrarySearchTolerance
        {
            get { return retentionTimeLibrarySearchTolerance; }
            set { retentionTimeLibrarySearchTolerance = value; }
        }

        public float Ms1LibrarySearchTolerance
        {
            get { return ms1LibrarySearchTolerance; }
            set { ms1LibrarySearchTolerance = value; }
        }

        public float Ms2LibrarySearchTolerance
        {
            get { return ms2LibrarySearchTolerance; }
            set { ms2LibrarySearchTolerance = value; }
        }

        public float IdentificationScoreCutOff
        {
            get { return identificationScoreCutOff; }
            set { identificationScoreCutOff = value; }
        }

        public float RetentionTimeToleranceOfPostIdentification
        {
            get { return retentionTimeToleranceOfPostIdentification; }
            set { retentionTimeToleranceOfPostIdentification = value; }
        }

        public float AccurateMassToleranceOfPostIdentification
        {
            get { return accurateMassToleranceOfPostIdentification; }
            set { accurateMassToleranceOfPostIdentification = value; }
        }

        public float PostIdentificationScoreCutOff
        {
            get { return postIdentificationScoreCutOff; }
            set { postIdentificationScoreCutOff = value; }
        }

        public bool OnlyReportTopHitForPostAnnotation
        {
            get { return onlyReportTopHitForPostAnnotation; }
            set { onlyReportTopHitForPostAnnotation = value; }
        }

        public float RelativeAbundanceCutOff
        {
            get { return relativeAbundanceCutOff; }
            set { relativeAbundanceCutOff = value; }
        }

        public int AlignmentReferenceFileID
        {
            get { return alignmentReferenceFileID; }
            set { alignmentReferenceFileID = value; }
        }

        public int BandWidth
        {
            get { return bandWidth; }
            set { bandWidth = value; }
        }

        public float SigmaWindowValue
        {
            get { return sigmaWindowValue; }
            set { sigmaWindowValue = value; }
        }

        public int SegmentNumber
        {
            get { return segmentNumber; }
            set { segmentNumber = value; }
        }

        public DeconvolutionType DeconvolutionType
        {
            get { return deconvolutionType; }
            set { deconvolutionType = value; }
        }

        public float AmplitudeCutoff
        {
            get { return amplitudeCutoff; }
            set { amplitudeCutoff = value; }
        }

        public bool RemoveAfterPrecursor
        {
            get { return removeAfterPrecursor; }
            set { removeAfterPrecursor = value; }
        }

        public float AdductAndIsotopeMassTolerance
        {
            get { return adductAndIsotopeMassTolerance; }
            set { adductAndIsotopeMassTolerance = value; }
        }

        public float PeakCountFilter
        {
            get { return peakCountFilter; }
            set { peakCountFilter = value; }
        }

        public bool QcAtLeastFilter
        {
            get { return qcAtLeastFilter; }
            set { qcAtLeastFilter = value; }
        }

        public bool GapFillingOption
        {
            get { return gapFillingOption; }
            set { gapFillingOption = value; }
        }

        public bool TrackingIsotopeLabels
        {
            get { return trackingIsotopeLabels; }
            set { trackingIsotopeLabels = value; }
        }

        public IsotopeTrackingDictionary IsotopeTrackingDictionary
        {
            get { return isotopeTrackingDictionary; }
            set { isotopeTrackingDictionary = value; }
        }

        public int NonLabeledReferenceID
        {
            get { return nonLabeledReferenceID; }
            set { nonLabeledReferenceID = value; }
        }

        public int FullyLabeledReferenceID
        {
            get { return fullyLabeledReferenceID; }
            set { fullyLabeledReferenceID = value; }
        }

        public bool FullyLabeledFileAvailability
        {
            get { return fullyLabeledFileAvailability; }
            set { fullyLabeledFileAvailability = value; }
        }

        public bool TogetherWithAlignment
        {
            get { return togetherWithAlignment; }
            set { togetherWithAlignment = value; }
        }

        public bool IsNormalizeNone
        {
            get { return isNormalizeNone; }
            set { isNormalizeNone = value; }
        }

        public bool IsNormalizeIS
        {
            get { return isNormalizeIS; }
            set { isNormalizeIS = value; }
        }

        public bool IsNormalizeLowess
        {
            get { return isNormalizeLowess; }
            set { isNormalizeLowess = value; }
        }

        public bool IsNormalizeIsLowess
        {
            get { return isNormalizeIsLowess; }
            set { isNormalizeIsLowess = value; }
        }

        public bool IsNormalizeTic
        {
            get { return isNormalizeTic; }
            set { isNormalizeTic = value; }
        }

        public bool IsNormalizeMTic
        {
            get { return isNormalizeMTic; }
            set { isNormalizeMTic = value; }
        }

        public double LowessSpan
        {
            get { return lowessSpan; }
            set { lowessSpan = value; }
        }

        public bool IsBlankSubtract
        {
            get { return isBlankSubtract; }
            set { isBlankSubtract = value; }
        }

        public TransformMethod Transform
        {
            get { return transform; }
            set { transform = value; }
        }

        public ScaleMethod Scale
        {
            get { return scale; }
            set { scale = value; }
        }

        public int MaxComponent
        {
            get { return maxComponent; }
            set { maxComponent = value; }
        }
        #endregion
    }
}
