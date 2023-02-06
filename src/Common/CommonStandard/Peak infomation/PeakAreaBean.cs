using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    public enum IonAbundanceUnit {
        Intensity, Height, Area, pmol, fmol, ng, pg,
        nmol_per_microL_plasma, pmol_per_microL_plasma, fmol_per_microL_plasma,
        nmol_per_mg_tissue, pmol_per_mg_tissue, fmol_per_mg_tissue,
        nmol_per_10E6_cells, pmol_per_10E6_cells, fmol_per_10E6_cells, 
        NormalizedByInternalStandardPeakHeight, NormalizedByQcPeakHeight, NormalizedByTIC, NormalizedByMTIC,
        nmol_per_individual, pmol_per_individual, fmol_per_individual,
        nmol_per_microG_protein, pmol_per_microG_protein, fmol_per_microG_protein,
    }

    public enum PeakLinkFeatureEnum {
        SameFeature, Isotope, Adduct, ChromSimilar, FoundInUpperMsMs, CorrelSimilar
    }

    [DataContract]
    [MessagePackObject]
    public class LinkedPeakFeature {
        private int linkedPeakID;
        private PeakLinkFeatureEnum character;

        [Key(0)]
        public int LinkedPeakID {
            get {
                return linkedPeakID;
            }

            set {
                linkedPeakID = value;
            }
        }

        [Key(1)]
        public PeakLinkFeatureEnum Character {
            get {
                return character;
            }

            set {
                character = value;
            }
        }
    }

    /// <summary>
    /// This is the storage of a detected peak.
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class PeakAreaBean : INotifyPropertyChanged
    {
        //Basic information
        [DataMember]
        private int peakID;
        [DataMember]
        private int scanNumberAtPeakTop;
        [DataMember]
        private int scanNumberAtLeftPeakEdge;
        [DataMember]
        private int scanNumberAtRightPeakEdge;
        [DataMember]
        private float rtAtPeakTop;
        [DataMember]
        private float rtAtLeftPeakEdge;
        [DataMember]
        private float rtAtRightPeakEdge;
        [DataMember]
        private float intensityAtLeftPeakEdge;
        [DataMember]
        private float intensityAtRightPeakEdge;
        [DataMember]
        private float intensityAtPeakTop;
        [DataMember]
        private float areaAboveZero;
        [DataMember]
        private float areaAboveBaseline;
        [DataMember]
        private float normalizedValue;
        [DataMember]
        private float ms1IsotopicIonM1PeakHeight;
        [DataMember]
        private float ms1IsotopicIonM2PeakHeight;
        [DataMember]
        private List<LinkedPeakFeature> peakLinks;
        [DataMember]
        private bool isLinked;
        [DataMember]
        private int peakGroupID;
        [DataMember]
        private float estimatedNoise;
        [DataMember]
        private float signalToNoise;

        //Peak feature
        [DataMember]
        private float peakPureValue;
        [DataMember]
        private float shapenessValue;
        [DataMember]
        private float gaussianSimilarityValue;
        [DataMember]
        private float idealSlopeValue;
        [DataMember]
        private float basePeakValue;
        [DataMember]
        private float symmetryValue;
        [DataMember]
        private float amplitudeScoreValue;
        [DataMember]
        private float amplitudeOrderValue;
        [DataMember]
        private int chargeNumber;
        
        //Isotopes 
        [DataMember]
        private int isotopeWeightNumber;
        [DataMember]
        private int isotopeParentPeakID;
        
        //Adduct
        [DataMember]
        private float adductIonAccurateMass;
        [DataMember]
        private int adductIonXmer;
        [DataMember]
        private int adductIonChargeNumber;
        [DataMember]
        private string adductIonName;
        [DataMember]
        private int adductParent;

        //Adduct from amalgamation
        private AdductIon adductFromAmalgamation;

        //Identification (MS/MS)
        [DataMember]
        private string metaboliteName;
        [DataMember]
        private string inchikey;
        [DataMember]
        private float accurateMass;
        [DataMember]
        private float accurateMassSimilarity;
        [DataMember]
        private float rtSimilarityValue;
        private float ccsSimilarity;

        [DataMember]
        private float totalSimilarityValue;
        [DataMember]
        private List<float> totalSimilarityValueList;
        [DataMember]
        private float alignedRetentionTime;
        [DataMember]
        private float isotopeSimilarityValue;
        [DataMember]
        private float massSpectraSimilarityValue;
        [DataMember]
        private float reverseSearchSimilarityValue;
        [DataMember]
        private float presenseSimilarityValue;
        private float simpleDotProductSimilarity;
        [DataMember]
        private int ms1LevelDatapointNumber;
        private int ms1LevelDatapointNumberAtAcculateMs1;
        [DataMember]
        private int ms2LevelDatapointNumber;
        [DataMember]
        private List<int> ms2LevelDatapointNumberList;

        //Identification
        [DataMember]
        private int libraryID;
        [DataMember]
        private List<int> libraryIDList;
        [DataMember]
        private int postIdentificationLibraryId;
        [DataMember]
        private int deconvolutionID;
        [DataMember]
        private string comment;

        private bool isMs1Match;
        private bool isMs2Match;
        private bool isRtMatch;
        private bool isCcsMatch;
        private bool isLipidClassMatch;
        private bool isLipidChainsMatch;
        private bool isLipidPositionMatch;
        private bool isOtherLipidMatch;

        //Identification (MRM)
        [DataMember]
        private float amplitudeRatioSimilatiryValue;
        [DataMember]
        private float peakTopDifferencialValue;
        [DataMember]
        private float peakShapeSimilarityValue;

        //Searcher
        [DataMember]
        private bool isFragmentQueryExist;

        //Ion mobility
        [DataMember]
        private int masterPeakID; // sequential peak ids including normal rt-mz axis peak spots + drift-mz axis peak spots
        [DataMember]
        private List<DriftSpotBean> driftSpots;

        #region accessors
        public PeakAreaBean() {
            chargeNumber = 1;
            comment = "";
            inchikey = string.Empty;
            driftSpots = new List<DriftSpotBean>();
            adductFromAmalgamation = new AdductIon();
        }


        [Key(0)]
        public float AmplitudeScoreValue
        {
            get { return amplitudeScoreValue; }
            set { amplitudeScoreValue = value; }
        }

        [Key(1)]
        public float AmplitudeOrderValue
        {
            get { return amplitudeOrderValue; }
            set { amplitudeOrderValue = value; }
        }

        [Key(2)]
        public float RtSimilarityValue
        {
            get { return rtSimilarityValue; }
            set { rtSimilarityValue = value; }
        }

        [Key(3)]
        public float NormalizedValue
        {
            get { return normalizedValue; }
            set { normalizedValue = value; }
        }

        [Key(4)]
        public int ScanNumberAtLeftPeakEdge
        {
            get { return scanNumberAtLeftPeakEdge; }
            set { scanNumberAtLeftPeakEdge = value; }
        }

        [Key(5)]
        public float ReverseSearchSimilarityValue
        {
            get { return reverseSearchSimilarityValue; }
            set { reverseSearchSimilarityValue = value; }
        }
        [Key(6)]
        public int IsotopeParentPeakID
        {
            get { return isotopeParentPeakID; }
            set { isotopeParentPeakID = value; }
        }

        [Key(7)]
        public float Ms1IsotopicIonM1PeakHeight
        {
            get { return ms1IsotopicIonM1PeakHeight; }
            set { ms1IsotopicIonM1PeakHeight = value; }
        }

        [Key(8)]
        public float Ms1IsotopicIonM2PeakHeight
        {
            get { return ms1IsotopicIonM2PeakHeight; }
            set { ms1IsotopicIonM2PeakHeight = value; }
        }

        [Key(9)]
        public int AdductIonXmer
        {
            get { return adductIonXmer; }
            set { adductIonXmer = value; }
        }

        [Key(10)]
        public int AdductIonChargeNumber
        {
            get { return adductIonChargeNumber; }
            set { adductIonChargeNumber = value; }
        }

        [Key(11)]
        public float PresenseSimilarityValue
        {
            get { return presenseSimilarityValue; }
            set { presenseSimilarityValue = value; }
        }

        [Key(12)]
        public int AdductParent
        {
            get { return adductParent; }
            set { adductParent = value; }
        }

        [Key(13)]
        public int ScanNumberAtRightPeakEdge
        {
            get { return scanNumberAtRightPeakEdge; }
            set { scanNumberAtRightPeakEdge = value; }
        }

        [Key(14)]
        public float AmplitudeRatioSimilatiryValue
        {
            get { return amplitudeRatioSimilatiryValue; }
            set { amplitudeRatioSimilatiryValue = value; }
        }

        [Key(15)]
        public float PeakTopDifferencialValue
        {
            get { return peakTopDifferencialValue; }
            set { peakTopDifferencialValue = value; }
        }

        [Key(16)]
        public float PeakShapeSimilarityValue
        {
            get { return peakShapeSimilarityValue; }
            set { peakShapeSimilarityValue = value; }
        }

        [Key(17)]
        public float ShapenessValue
        {
            get { return shapenessValue; }
            set { shapenessValue = value; }
        }

        [Key(18)]
        public int PeakID
        {
            get { return peakID; }
            set { peakID = value; }
        }

        [Key(19)]
        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(20)]
        public float RtAtLeftPeakEdge
        {
            get { return rtAtLeftPeakEdge; }
            set { rtAtLeftPeakEdge = value; }
        }

        [Key(21)]
        public float IntensityAtLeftPeakEdge
        {
            get { return intensityAtLeftPeakEdge; }
            set { intensityAtLeftPeakEdge = value; }
        }

        [Key(22)]
        public float RtAtRightPeakEdge
        {
            get { return rtAtRightPeakEdge; }
            set { rtAtRightPeakEdge = value; }
        }

        [Key(23)]
        public float IntensityAtRightPeakEdge
        {
            get { return intensityAtRightPeakEdge; }
            set { intensityAtRightPeakEdge = value; }
        }

        [Key(24)]
        public int IsotopeWeightNumber
        {
            get { return isotopeWeightNumber; }
            set { isotopeWeightNumber = value; }
        }

        [Key(25)]
        public float RtAtPeakTop
        {
            get { return rtAtPeakTop; }
            set { rtAtPeakTop = value; }
        }

        [Key(26)]
        public float IntensityAtPeakTop
        {
            get { return intensityAtPeakTop; }
            set { intensityAtPeakTop = value; }
        }

        [Key(27)]
        public int ScanNumberAtPeakTop
        {
            get { return scanNumberAtPeakTop; }
            set { scanNumberAtPeakTop = value; }
        }

        [Key(28)]
        public float AreaAboveZero
        {
            get { return areaAboveZero; }
            set { areaAboveZero = value; }
        }

        [Key(29)]
        public float AreaAboveBaseline
        {
            get { return areaAboveBaseline; }
            set { areaAboveBaseline = value; }
        }

        [Key(30)]
        public float PeakPureValue
        {
            get { return peakPureValue; }
            set { peakPureValue = value; }
        }

        [Key(31)]
        public float GaussianSimilarityValue
        {
            get { return gaussianSimilarityValue; }
            set { gaussianSimilarityValue = value; }
        }

        [Key(32)]
        public float IdealSlopeValue
        {
            get { return idealSlopeValue; }
            set { idealSlopeValue = value; }
        }

        [Key(33)]
        public float AccurateMassSimilarity
        {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        [Key(34)]
        public float BasePeakValue
        {
            get { return basePeakValue; }
            set { basePeakValue = value; }
        }

        [Key(35)]
        public float SymmetryValue
        {
            get { return symmetryValue; }
            set { symmetryValue = value; }
        }

        [Key(36)]
        public float TotalScore
        {
            get { return totalSimilarityValue; }
            set { totalSimilarityValue = value; }
        }

        [Key(37)]
        public List<float> TotalScoreList {
            get { return totalSimilarityValueList; }
            set { totalSimilarityValueList = value; }
        }

        [Key(38)]
        public float AlignedRetentionTime
        {
            get { return alignedRetentionTime; }
            set { alignedRetentionTime = value; }
        }

        [Key(39)]
        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { if (metaboliteName == value) return;
                metaboliteName = value; OnPropertyChanged("MetaboliteName"); }
        }

        [Key(40)]
        public float AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        [Key(41)]
        public float IsotopeSimilarityValue
        {
            get { return isotopeSimilarityValue; }
            set { isotopeSimilarityValue = value; }
        }

        [Key(42)]
        public float MassSpectraSimilarityValue
        {
            get { return massSpectraSimilarityValue; }
            set { massSpectraSimilarityValue = value; }
        }

        [Key(43)]
        public int Ms1LevelDatapointNumber
        {
            get { return ms1LevelDatapointNumber; }
            set { ms1LevelDatapointNumber = value; }
        }

        [Key(44)]
        public int Ms2LevelDatapointNumber
        {
            get { return ms2LevelDatapointNumber; }
            set { ms2LevelDatapointNumber = value; }
        }

        [Key(45)]
        public List<int> Ms2LevelDatapointNumberList 
        {
            get { return ms2LevelDatapointNumberList; }
            set { ms2LevelDatapointNumberList = value; }
        }

        [Key(46)]
        public int LibraryID
        {
            get { return libraryID; }
            set { libraryID = value; }
        }

        [Key(47)]
        public List<int> LibraryIDList 
        {
            get { return libraryIDList; }
            set { libraryIDList = value; }
        }

        [Key(48)]
        public int PostIdentificationLibraryId
        {
            get { return postIdentificationLibraryId; }
            set { postIdentificationLibraryId = value; }
        }

        [Key(49)]
        public int DeconvolutionID
        {
            get { return deconvolutionID; }
            set { deconvolutionID = value; }
        }

        [Key(50)]
        public string Comment {
            get { return comment; }
            set { comment = value; }
        }

        [Key(51)]
        public float AdductIonAccurateMass
        {
            get { return adductIonAccurateMass; }
            set { adductIonAccurateMass = value; }
        }

        [Key(52)]
        public int ChargeNumber {
            get {
                return chargeNumber;
            }

            set {
                chargeNumber = value;
            }
        }

        [Key(53)]
        public bool IsFragmentQueryExist {
            get {
                return isFragmentQueryExist;
            }

            set {
                isFragmentQueryExist = value;
            }
        }

        [Key(54)]
        public List<DriftSpotBean> DriftSpots {
            get {
                return driftSpots;
            }

            set {
                driftSpots = value;
            }
        }

        [Key(55)]
        public List<LinkedPeakFeature> PeakLinks {
            get {
                return peakLinks;
            }

            set {
                peakLinks = value;
            }
        }

        [Key(56)]
        public bool IsLinked {
            get {
                return isLinked;
            }

            set {
                isLinked = value;
            }
        }

        [Key(57)]
        public int PeakGroupID {
            get {
                return peakGroupID;
            }

            set {
                peakGroupID = value;
            }
        }

        [Key(58)]
        public string Inchikey {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        [Key(59)]
        public float EstimatedNoise {
            get {
                return estimatedNoise;
            }

            set {
                estimatedNoise = value;
            }
        }

        [Key(60)]
        public float SignalToNoise {
            get {
                return signalToNoise;
            }

            set {
                signalToNoise = value;
            }
        }

        [Key(61)]
        public int MasterPeakID {
            get {
                return masterPeakID;
            }

            set {
                masterPeakID = value;
            }
        }

        [Key(62)]
        public int Ms1LevelDatapointNumberAtAcculateMs1 {
            get {
                return ms1LevelDatapointNumberAtAcculateMs1;
            }

            set {
                ms1LevelDatapointNumberAtAcculateMs1 = value;
            }
        }

        [Key(63)]
        public AdductIon AdductFromAmalgamation {
            get {
                return adductFromAmalgamation;
            }

            set {
                adductFromAmalgamation = value;
            }
        }

        [Key(64)]
        public bool IsMs1Match {
            get {
                return isMs1Match;
            }

            set {
                isMs1Match = value;
            }
        }

        [Key(65)]
        public bool IsMs2Match {
            get {
                return isMs2Match;
            }

            set {
                isMs2Match = value;
            }
        }

        [Key(66)]
        public bool IsRtMatch {
            get {
                return isRtMatch;
            }

            set {
                isRtMatch = value;
            }
        }

        [Key(67)]
        public bool IsCcsMatch {
            get {
                return isCcsMatch;
            }

            set {
                isCcsMatch = value;
            }
        }

        [Key(68)]
        public float CcsSimilarity {
            get {
                return ccsSimilarity;
            }

            set {
                ccsSimilarity = value;
            }
        }

        [Key(69)]
        public bool IsLipidClassMatch {
            get {
                return isLipidClassMatch;
            }

            set {
                isLipidClassMatch = value;
            }
        }

        [Key(70)]
        public bool IsLipidChainsMatch {
            get {
                return isLipidChainsMatch;
            }

            set {
                isLipidChainsMatch = value;
            }
        }

        [Key(71)]
        public bool IsLipidPositionMatch {
            get {
                return isLipidPositionMatch;
            }

            set {
                isLipidPositionMatch = value;
            }
        }

        [Key(72)]
        public bool IsOtherLipidMatch {
            get {
                return isOtherLipidMatch;
            }

            set {
                isOtherLipidMatch = value;
            }
        }

        [Key(73)]
        public float SimpleDotProductSimilarity {
            get {
                return simpleDotProductSimilarity;
            }
            set {
                simpleDotProductSimilarity = value;
            }
        }
        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName) {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged

    }

    [DataContract]
    [MessagePackObject]
    public class DriftSpotBean : INotifyPropertyChanged {
        //Basic information
        [DataMember]
        private int peakAreaBeanID;
        [DataMember]
        private int peakID;

        [DataMember]
        private int displayedSpotID;
        [DataMember]
        private int driftScanAtPeakTop;
        [DataMember]
        private int driftScanAtLeftPeakEdge;
        [DataMember]
        private int driftScanAtRightPeakEdge;

        [DataMember]
        private float driftTimeAtPeakTop;
        [DataMember]
        private float driftTimeAtLeftPeakEdge;
        [DataMember]
        private float driftTimeAtRightPeakEdge;
        [DataMember]
        private float intensityAtLeftPeakEdge;
        [DataMember]
        private float intensityAtRightPeakEdge;
        [DataMember]
        private float intensityAtPeakTop;
        [DataMember]
        private float areaAboveZero;
        [DataMember]
        private float areaAboveBaseline;
        [DataMember]
        private float normalizedValue;
        [DataMember]
        private float ms1IsotopicIonM1PeakHeight;
        [DataMember]
        private float ms1IsotopicIonM2PeakHeight;
        [DataMember]
        private float estimatedNoise;
        [DataMember]
        private float signalToNoise;

        //Peak feature
        [DataMember]
        private float peakPureValue;
        [DataMember]
        private float shapenessValue;
        [DataMember]
        private float gaussianSimilarityValue;
        [DataMember]
        private float idealSlopeValue;
        [DataMember]
        private float basePeakValue;
        [DataMember]
        private float symmetryValue;
        [DataMember]
        private float amplitudeScoreValue;
        [DataMember]
        private float amplitudeOrderValue;
        [DataMember]
        private int chargeNumber;
        [DataMember]
        private float ccs;

        //Identification (MS/MS)
        [DataMember]
        private string metaboliteName;
        [DataMember]
        private string adductIonName;
        [DataMember]
        private int adductParent;
        [DataMember]
        private float adductIonAccurateMass;
        [DataMember]
        private int adductIonXmer;
        [DataMember]
        private int adductIonChargeNumber;
        [DataMember]
        private int isotopeWeightNumber;
        [DataMember]
        private int isotopeParentPeakID;
        [DataMember]
        private float accurateMass;
        [DataMember]
        private float accurateMassSimilarity;
        [DataMember]
        private float rtSimilarityValue;
        [DataMember]
        private float totalSimilarityValue;
        [DataMember]
        private List<float> totalSimilarityValueList;
        [DataMember]
        private float alignedRetentionTime;
        [DataMember]
        private float isotopeSimilarityValue;
        [DataMember]
        private float massSpectraSimilarityValue;
        [DataMember]
        private float reverseSearchSimilarityValue;
        [DataMember]
        private float presenseSimilarityValue;
        private float ccsSimilarity;
        [DataMember]
        private int ms1LevelDatapointNumber; // by drift scan number
        [DataMember]
        private int ms2LevelDatapointNumber; // by drift scan number
        [DataMember]
        private List<int> ms2LevelDatapointNumberList;

        [DataMember]
        private int libraryID;
        [DataMember]
        private List<int> libraryIDList;
        [DataMember]
        private int postIdentificationLibraryId;
        [DataMember]
        private int deconvolutionID;
        [DataMember]
        private string comment;
        [DataMember]
        private string inchikey;

        private bool isMs1Match;
        private bool isMs2Match;
        private bool isRtMatch;
        private bool isCcsMatch;
        private bool isLipidClassMatch;
        private bool isLipidChainsMatch;
        private bool isLipidPositionMatch;
        private bool isOtherLipidMatch;

        //Identification (MRM)
        [DataMember]
        private float amplitudeRatioSimilatiryValue;
        [DataMember]
        private float peakTopDifferencialValue;
        [DataMember]
        private float peakShapeSimilarityValue;

        //Searcher
        [DataMember]
        private bool isFragmentQueryExist;

        //Ion mobility
        [DataMember]
        private int masterPeakID; // sequential peak ids including normal rt-mz axis peak spots + drift-mz axis peak spots

        #region accessors
        public DriftSpotBean() {
            chargeNumber = 1;
            driftTimeAtPeakTop = -1;
            comment = "";
        }

        [Key(0)]
        public float AmplitudeScoreValue {
            get { return amplitudeScoreValue; }
            set { amplitudeScoreValue = value; }
        }

        [Key(1)]
        public float AmplitudeOrderValue {
            get { return amplitudeOrderValue; }
            set { amplitudeOrderValue = value; }
        }

        [Key(2)]
        public float RtSimilarityValue {
            get { return rtSimilarityValue; }
            set { rtSimilarityValue = value; }
        }

        [Key(3)]
        public float NormalizedValue {
            get { return normalizedValue; }
            set { normalizedValue = value; }
        }

        [Key(4)]
        public int DriftScanAtLeftPeakEdge {
            get { return driftScanAtLeftPeakEdge; }
            set { driftScanAtLeftPeakEdge = value; }
        }

        [Key(5)]
        public float ReverseSearchSimilarityValue {
            get { return reverseSearchSimilarityValue; }
            set { reverseSearchSimilarityValue = value; }
        }
        [Key(6)]
        public int IsotopeParentPeakID {
            get { return isotopeParentPeakID; }
            set { isotopeParentPeakID = value; }
        }

        [Key(7)]
        public float Ms1IsotopicIonM1PeakHeight {
            get { return ms1IsotopicIonM1PeakHeight; }
            set { ms1IsotopicIonM1PeakHeight = value; }
        }

        [Key(8)]
        public float Ms1IsotopicIonM2PeakHeight {
            get { return ms1IsotopicIonM2PeakHeight; }
            set { ms1IsotopicIonM2PeakHeight = value; }
        }

        [Key(9)]
        public int AdductIonXmer {
            get { return adductIonXmer; }
            set { adductIonXmer = value; }
        }

        [Key(10)]
        public int AdductIonChargeNumber {
            get { return adductIonChargeNumber; }
            set { adductIonChargeNumber = value; }
        }

        [Key(11)]
        public float PresenseSimilarityValue {
            get { return presenseSimilarityValue; }
            set { presenseSimilarityValue = value; }
        }

        [Key(12)]
        public int AdductParent {
            get { return adductParent; }
            set { adductParent = value; }
        }

        [Key(13)]
        public int DriftScanAtRightPeakEdge {
            get { return driftScanAtRightPeakEdge; }
            set { driftScanAtRightPeakEdge = value; }
        }

        [Key(14)]
        public float AmplitudeRatioSimilatiryValue {
            get { return amplitudeRatioSimilatiryValue; }
            set { amplitudeRatioSimilatiryValue = value; }
        }

        [Key(15)]
        public float PeakTopDifferencialValue {
            get { return peakTopDifferencialValue; }
            set { peakTopDifferencialValue = value; }
        }

        [Key(16)]
        public float PeakShapeSimilarityValue {
            get { return peakShapeSimilarityValue; }
            set { peakShapeSimilarityValue = value; }
        }

        [Key(17)]
        public float ShapenessValue {
            get { return shapenessValue; }
            set { shapenessValue = value; }
        }

        [Key(18)]
        public int PeakID {
            get { return peakID; }
            set { peakID = value; }
        }

        [Key(19)]
        public string AdductIonName {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(20)]
        public float DriftTimeAtLeftPeakEdge {
            get { return driftTimeAtLeftPeakEdge; }
            set { driftTimeAtLeftPeakEdge = value; }
        }

        [Key(21)]
        public float IntensityAtLeftPeakEdge {
            get { return intensityAtLeftPeakEdge; }
            set { intensityAtLeftPeakEdge = value; }
        }

        [Key(22)]
        public float DriftTimeAtRightPeakEdge {
            get { return driftTimeAtRightPeakEdge; }
            set { driftTimeAtRightPeakEdge = value; }
        }

        [Key(23)]
        public float IntensityAtRightPeakEdge {
            get { return intensityAtRightPeakEdge; }
            set { intensityAtRightPeakEdge = value; }
        }

        [Key(24)]
        public int IsotopeWeightNumber {
            get { return isotopeWeightNumber; }
            set { isotopeWeightNumber = value; }
        }

        [Key(25)]
        public float DriftTimeAtPeakTop {
            get { return driftTimeAtPeakTop; }
            set { driftTimeAtPeakTop = value; }
        }

        [Key(26)]
        public float IntensityAtPeakTop {
            get { return intensityAtPeakTop; }
            set { intensityAtPeakTop = value; }
        }

        [Key(27)]
        public int DriftScanAtPeakTop {
            get { return driftScanAtPeakTop; }
            set { driftScanAtPeakTop = value; }
        }

        [Key(28)]
        public float AreaAboveZero {
            get { return areaAboveZero; }
            set { areaAboveZero = value; }
        }

        [Key(29)]
        public float AreaAboveBaseline {
            get { return areaAboveBaseline; }
            set { areaAboveBaseline = value; }
        }

        [Key(30)]
        public float PeakPureValue {
            get { return peakPureValue; }
            set { peakPureValue = value; }
        }

        [Key(31)]
        public float GaussianSimilarityValue {
            get { return gaussianSimilarityValue; }
            set { gaussianSimilarityValue = value; }
        }

        [Key(32)]
        public float IdealSlopeValue {
            get { return idealSlopeValue; }
            set { idealSlopeValue = value; }
        }

        [Key(33)]
        public float AccurateMassSimilarity {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        [Key(34)]
        public float BasePeakValue {
            get { return basePeakValue; }
            set { basePeakValue = value; }
        }

        [Key(35)]
        public float SymmetryValue {
            get { return symmetryValue; }
            set { symmetryValue = value; }
        }

        [Key(36)]
        public float TotalScore {
            get { return totalSimilarityValue; }
            set { totalSimilarityValue = value; }
        }

        [Key(37)]
        public List<float> TotalScoreList {
            get { return totalSimilarityValueList; }
            set { totalSimilarityValueList = value; }
        }

        [Key(38)]
        public float AlignedRetentionTime {
            get { return alignedRetentionTime; }
            set { alignedRetentionTime = value; }
        }

        [Key(39)]
        public string MetaboliteName {
            get { return metaboliteName; }
            set {
                if (metaboliteName == value) return;
                metaboliteName = value; OnPropertyChanged("MetaboliteName");
            }
        }

        [Key(40)]
        public float AccurateMass {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        [Key(41)]
        public float IsotopeSimilarityValue {
            get { return isotopeSimilarityValue; }
            set { isotopeSimilarityValue = value; }
        }

        [Key(42)]
        public float MassSpectraSimilarityValue {
            get { return massSpectraSimilarityValue; }
            set { massSpectraSimilarityValue = value; }
        }

        [Key(43)]
        public int Ms1LevelDatapointNumber {
            get { return ms1LevelDatapointNumber; }
            set { ms1LevelDatapointNumber = value; }
        }

        [Key(44)]
        public int Ms2LevelDatapointNumber {
            get { return ms2LevelDatapointNumber; }
            set { ms2LevelDatapointNumber = value; }
        }

        [Key(45)]
        public List<int> Ms2LevelDatapointNumberList {
            get { return ms2LevelDatapointNumberList; }
            set { ms2LevelDatapointNumberList = value; }
        }

        [Key(46)]
        public int LibraryID {
            get { return libraryID; }
            set { libraryID = value; }
        }

        [Key(47)]
        public List<int> LibraryIDList {
            get { return libraryIDList; }
            set { libraryIDList = value; }
        }

        [Key(48)]
        public int PostIdentificationLibraryId {
            get { return postIdentificationLibraryId; }
            set { postIdentificationLibraryId = value; }
        }

        [Key(49)]
        public int DeconvolutionID {
            get { return deconvolutionID; }
            set { deconvolutionID = value; }
        }

        [Key(50)]
        public string Comment {
            get { return comment; }
            set { comment = value; }
        }

        [Key(51)]
        public float AdductIonAccurateMass {
            get { return adductIonAccurateMass; }
            set { adductIonAccurateMass = value; }
        }

        [Key(52)]
        public int ChargeNumber {
            get {
                return chargeNumber;
            }

            set {
                chargeNumber = value;
            }
        }

        [Key(53)]
        public bool IsFragmentQueryExist {
            get {
                return isFragmentQueryExist;
            }

            set {
                isFragmentQueryExist = value;
            }
        }

        [Key(54)]
        public int PeakAreaBeanID {
            get {
                return peakAreaBeanID;
            }

            set {
                peakAreaBeanID = value;
            }
        }

        [Key(55)]
        public int DisplayedSpotID {
            get {
                return displayedSpotID;
            }

            set {
                displayedSpotID = value;
            }
        }

        [Key(56)]
        public float EstimatedNoise {
            get {
                return estimatedNoise;
            }

            set {
                estimatedNoise = value;
            }
        }

        [Key(57)]
        public float SignalToNoise {
            get {
                return signalToNoise;
            }

            set {
                signalToNoise = value;
            }
        }

        [Key(58)]
        public int MasterPeakID {
            get {
                return masterPeakID;
            }

            set {
                masterPeakID = value;
            }
        }

        [Key(59)]
        public float Ccs {
            get {
                return ccs;
            }

            set {
                ccs = value;
            }
        }

        [Key(60)]
        public string Inchikey {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        [Key(61)]
        public float CcsSimilarity {
            get {
                return ccsSimilarity;
            }

            set {
                ccsSimilarity = value;
            }
        }

        [Key(62)]
        public bool IsMs1Match {
            get {
                return isMs1Match;
            }

            set {
                isMs1Match = value;
            }
        }

        [Key(63)]
        public bool IsMs2Match {
            get {
                return isMs2Match;
            }

            set {
                isMs2Match = value;
            }
        }

        [Key(64)]
        public bool IsRtMatch {
            get {
                return isRtMatch;
            }

            set {
                isRtMatch = value;
            }
        }

        [Key(65)]
        public bool IsCcsMatch {
            get {
                return isCcsMatch;
            }

            set {
                isCcsMatch = value;
            }
        }

        [Key(66)]
        public bool IsLipidClassMatch {
            get {
                return isLipidClassMatch;
            }

            set {
                isLipidClassMatch = value;
            }
        }

        [Key(67)]
        public bool IsLipidChainsMatch {
            get {
                return isLipidChainsMatch;
            }

            set {
                isLipidChainsMatch = value;
            }
        }

        [Key(68)]
        public bool IsLipidPositionMatch {
            get {
                return isLipidPositionMatch;
            }

            set {
                isLipidPositionMatch = value;
            }
        }

        [Key(69)]
        public bool IsOtherLipidMatch {
            get {
                return isOtherLipidMatch;
            }

            set {
                isOtherLipidMatch = value;
            }
        }
    
        #endregion

        #region // Required Methods for INotifyPropertyChanged
        /// <summary>
        /// Occurs when a property value changes.
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="propertyName">Name of the property that changed.</param>
        protected void OnPropertyChanged(string propertyName) {
            this.OnPropertyChanged(new PropertyChangedEventArgs(propertyName));
        }

        /// <summary>
        /// Raises the <see cref="PropertyChanged"/> event.
        /// </summary>
        /// <param name="e">The <see cref="PropertyChangedEventArgs"/> instance containing the event data.</param>
        protected virtual void OnPropertyChanged(PropertyChangedEventArgs e) {
            PropertyChangedEventHandler eventHandlers = this.PropertyChanged;
            if (null != eventHandlers)
                eventHandlers(this, e);
        }
        #endregion // Required Methods for INotifyPropertyChanged
    }
}
