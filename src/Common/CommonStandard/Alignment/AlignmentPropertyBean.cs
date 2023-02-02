using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    [MessagePackObject]
    public class AlignmentSpotVariableCorrelation 
    {
        [DataMember]
        private int correlateAlignmentID;
        [DataMember]
        private float correlationScore;

        public AlignmentSpotVariableCorrelation()
        {
            correlateAlignmentID = -1;
            correlationScore = -1;
        }

        [Key(0)]
        public int CorrelateAlignmentID
        {
            get {
                return correlateAlignmentID;
            }

            set {
                correlateAlignmentID = value;
            }
        }

        [Key(1)]
        public float CorrelationScore
        {
            get {
                return correlationScore;
            }

            set {
                correlationScore = value;
            }
        }
    }



    /// <summary>
    /// This class and 'AlignedPeakPropertyBean.cs' are used to store the properties of peak alignment result.
    /// 'AlignmentPropertyBean.cs' stores the summary of each alignment spot.
    /// 'AlignedPeakPropertyBean.cs' stores the detail of each peak aligned to a spot described as 'AlignmentPropertyBean.cs'
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class AlignmentPropertyBean : INotifyPropertyChanged
    {
        private int masterID; //sequential ID containing rt vs m/z axis and dt vs m/z axis 
        [DataMember]
        private int alignmentID;
        [DataMember]
        private int isotopeTrackingParentID;
        [DataMember]
        private int isotopeTrackingWeightNumber;
        [DataMember]
        private int chargeNumber;
        [DataMember]
        private float averageValiable;
        [DataMember]
        private float minValiable;
        [DataMember]
        private float maxValiable;
        [DataMember]
        private int representativeFileID;
        [DataMember]
        private Formula formula;
        [DataMember]
        private AdductIon adduct;

        [DataMember]
        private float estimatedNoiseMax;
        [DataMember]
        private float signalToNoiseMax;
        [DataMember]
        private float estimatedNoiseMin;
        [DataMember]
        private float signalToNoiseMin;
        [DataMember]
        private float estimatedNoiseAve;
        [DataMember]
        private float signalToNoiseAve;

        [DataMember]
        private int postDefinedIsotopeWeightNumber;
        [DataMember]
        private int postDefinedIsotopeParentID;
        [DataMember]
        private int postDefinedAdductParentID;
        [DataMember]
        private List<AlignmentSpotVariableCorrelation> alignmentSpotVariableCorrelations;

        [DataMember]
        private int libraryID;
        [DataMember]
        private int postIdentificationLibraryID;
        [DataMember]
        private int targetFormulaLibraryID;

        [DataMember]
        private float minRt;
        [DataMember]
        private float maxRt;
        [DataMember]
        private float maxRi;
        [DataMember]
        private float minRi;
        [DataMember]
        private float averagePeakWidth;

        [DataMember]
        private float minMz;
        [DataMember]
        private float maxMz;
        [DataMember]
        private float fillParcentage;
        [DataMember]
        private float relativeAmplitudeValue;
        [DataMember]
        private float monoIsotopicPercentage;

        [DataMember]
        private float centralRetentionTime;
        [DataMember]
        private float centralRetentionIndex;
        [DataMember]
        private float centralAccurateMass;
        [DataMember]
        private float quantMass;
        [DataMember]
        private string metaboliteName;
        [DataMember]
        private string adductIonName;
        private string adductIonNameFromAmalgamation;
        [DataMember]
        private bool msmsIncluded;
        [DataMember]
        private float totalSimilairty;
        [DataMember]
        private float eiSpectrumSimilarity;
        [DataMember]
        private bool isBlankFiltered;

        [DataMember]
        private float massSpectraSimilarity;
        [DataMember]
        private float reverseSimilarity;
        [DataMember]
        private float fragmentPresencePercentage;

        [DataMember]
        private float isotopeSimilarity;
        [DataMember]
        private float retentionTimeSimilarity;
        [DataMember]
        private float retentionIndexSimilarity;

        [DataMember]
        private float accurateMassSimilarity;
        [DataMember]
        private int internalStandardAlignmentID;
        [DataMember]
        ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeanCollection;
        ObservableCollection<AlignedDriftSpotPropertyBean> alignedDriftSpots;

        //Identification
        private bool isMs1Match;
        private bool isMs2Match;
        private bool isRtMatch;
        private bool isCcsMatch;
        private float ccsSimilarity;
        private bool isLipidClassMatch;
        private bool isLipidChainsMatch;
        private bool isLipidPositionMatch;
        private bool isOtherLipidMatch;

        //Searcher
        [DataMember]
        private bool isFragmentQueryExist;
        [DataMember]
        private string comment;
        [DataMember]
        private float anovaPval;
        [DataMember]
        private float foldChange;

        //character
        [DataMember]
        private List<LinkedPeakFeature> peakLinks;
        [DataMember]
        private bool isLinked;
        [DataMember]
        private int peakGroupID;
        [DataMember]
        private bool isManuallyModified;
        private IonAbundanceUnit ionAbundanceUnit;
        private bool isManuallyModifiedForAnnotation;

        // for AIF
        [DataMember]
        private List<int> libraryIdList;
        [DataMember]
        private List<int> correlBasedlibraryIdList;
        [DataMember]
        private int identificationRank;
        [DataMember]
        private int correlDecIdentificationRank;
        [DataMember]
        private float simpleDotProductSimilarity;

        // for database issue
        private string masterIdString;
        private string postCurationResult;
        private string spectrumReferenceFileName;
        private string spectrumReferenceFileClass;
        private string ms1SpecString;
        private string ms2SpecString;
        private string smiles;
        private string inchikey;
        private string ontology;

        public AlignmentPropertyBean()
        {
            this.masterID = -1;
            this.alignmentID = -1;
            this.isotopeTrackingParentID = -1;
            this.isotopeTrackingWeightNumber = -1;
            this.postDefinedAdductParentID = -1;
            this.postDefinedIsotopeParentID = -1;
            this.postDefinedIsotopeWeightNumber = -1;
            this.libraryID = -1;
            this.chargeNumber = 1;
            this.averagePeakWidth = 5;
            this.postIdentificationLibraryID = -1;
            this.internalStandardAlignmentID = -1;
            this.representativeFileID = -1;
            this.massSpectraSimilarity = -1;
            this.reverseSimilarity = -1;
            this.isotopeSimilarity = -1;
            this.totalSimilairty = -1;
            this.retentionTimeSimilarity = -1;
            this.retentionIndexSimilarity = -1;
            this.eiSpectrumSimilarity = -1;
            this.fragmentPresencePercentage = -1;
            this.formula = new Formula();
            this.alignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>();
            this.alignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation>();
            this.alignedDriftSpots = new ObservableCollection<AlignedDriftSpotPropertyBean>();
            this.isFragmentQueryExist = false;
            this.comment = string.Empty;
            this.foldChange = -1;
            this.anovaPval = -1;
            this.peakLinks = new List<LinkedPeakFeature>();
            this.isLinked = false;
            this.peakGroupID = -1;
            this.IsBlankFiltered = false;
            this.isManuallyModified = false;
            this.isManuallyModifiedForAnnotation = false;
            this.libraryIdList = new List<int>();
            this.correlBasedlibraryIdList = new List<int>();
            this.identificationRank = -1;
            this.correlDecIdentificationRank = -1;
            this.simpleDotProductSimilarity = -1;
            this.estimatedNoiseAve = 1;
            this.estimatedNoiseMax = 1;
            this.estimatedNoiseMin = 1;
            this.signalToNoiseAve = 1;
            this.signalToNoiseMax = 1;
            this.signalToNoiseMin = 1;
            this.ionAbundanceUnit = IonAbundanceUnit.Height;
            this.ccsSimilarity = -1F;
            this.adductIonNameFromAmalgamation = string.Empty;
        }

        [Key(0)]
        public int InternalStandardAlignmentID
        {
            get { return internalStandardAlignmentID; }
            set { internalStandardAlignmentID = value; }
        }

        [Key(1)]
        public int IsotopeTrackingParentID
        {
            get { return isotopeTrackingParentID; }
            set { isotopeTrackingParentID = value; }
        }

        [Key(2)]
        public int IsotopeTrackingWeightNumber
        {
            get { return isotopeTrackingWeightNumber; }
            set { isotopeTrackingWeightNumber = value; }
        }

        [Key(3)]
        public float AverageValiable
        {
            get { return averageValiable; }
            set { averageValiable = value; }
        }

        [Key(4)]
        public float MinValiable
        {
            get { return minValiable; }
            set { minValiable = value; }
        }

        [Key(5)]
        public float MaxValiable
        {
            get { return maxValiable; }
            set { maxValiable = value; }
        }

        [Key(6)]
        public float AveragePeakWidth
        {
            get { return averagePeakWidth; }
            set { averagePeakWidth = value; }
        }

        [Key(7)]
        public int RepresentativeFileID
        {
            get { return representativeFileID; }
            set { representativeFileID = value; }
        }

        [Key(8)]
        public int LibraryID
        {
            get { return libraryID; }
            set { libraryID = value; }
        }

        [Key(9)]
        public int PostIdentificationLibraryID
        {
            get { return postIdentificationLibraryID; }
            set { postIdentificationLibraryID = value; }
        }

        [Key(10)]
        public float CentralRetentionIndex
        {
            get { return centralRetentionIndex; }
            set { centralRetentionIndex = value; }
        }

        [Key(11)]
        public Formula Formula
        {
            get { return formula; }
            set { formula = value; }
        }

        [Key(12)]
        public AdductIon Adduct
        {
            get { return adduct; }
            set { adduct = value; }
        }

        [Key(13)]
        public float QuantMass
        {
            get { return quantMass; }
            set { quantMass = value; }
        }

        [Key(14)]
        public int TargetFormulaLibraryID
        {
            get { return targetFormulaLibraryID; }
            set { targetFormulaLibraryID = value; }
        }

        [Key(15)]
        public float RetentionTimeSimilarity
        {
            get { return retentionTimeSimilarity; }
            set { retentionTimeSimilarity = value; }
        }

        [Key(16)]
        public float RetentionIndexSimilarity
        {
            get { return retentionIndexSimilarity; }
            set { retentionIndexSimilarity = value; }
        }

        [Key(17)]
        public float EiSpectrumSimilarity
        {
            get { return eiSpectrumSimilarity; }
            set { eiSpectrumSimilarity = value; }
        }

        [Key(18)]
        public float AccurateMassSimilarity
        {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        [Key(19)]
        public float MinRt
        {
            get { return minRt; }
            set { minRt = value; }
        }

        [Key(20)]
        public float MaxRt
        {
            get { return maxRt; }
            set { maxRt = value; }
        }

        [Key(21)]
        public float MaxRi
        {
            get { return maxRi; }
            set { maxRi = value; }
        }

        [Key(22)]
        public float MinRi
        {
            get { return minRi; }
            set { minRi = value; }
        }

        [Key(23)]
        public float MinMz
        {
            get { return minMz; }
            set { minMz = value; }
        }

        [Key(24)]
        public float MaxMz
        {
            get { return maxMz; }
            set { maxMz = value; }
        }

        [Key(25)]
        public int AlignmentID
        {
            get { return alignmentID; }
            set { alignmentID = value; }
        }

        [Key(26)]
        public float MonoIsotopicPercentage
        {
            get { return monoIsotopicPercentage; }
            set { monoIsotopicPercentage = value; }
        }

        [Key(27)]
        public float CentralRetentionTime
        {
            get { return centralRetentionTime; }
            set { centralRetentionTime = value; }
        }

        [Key(28)]
        public float CentralAccurateMass
        {
            get { return centralAccurateMass; }
            set { centralAccurateMass = value; }
        }

        [Key(29)]
        public float FillParcentage
        {
            get { return fillParcentage; }
            set { fillParcentage = value; }
        }

        [Key(30)]
        public ObservableCollection<AlignedPeakPropertyBean> AlignedPeakPropertyBeanCollection
        {
            get { return alignedPeakPropertyBeanCollection; }
            set { alignedPeakPropertyBeanCollection = value; }
        }

        [Key(31)]
        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { if (metaboliteName == value) return;  metaboliteName = value; OnPropertyChanged("MetaboliteName"); }
        }

        [Key(32)]
        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(33)]
        public float RelativeAmplitudeValue
        {
            get { return relativeAmplitudeValue; }
            set { relativeAmplitudeValue = value; }
        }

        [Key(34)]
        public bool MsmsIncluded
        {
            get { return msmsIncluded; }
            set { msmsIncluded = value; }
        }

        [Key(35)]
        public float TotalSimilairty
        {
            get { return totalSimilairty; }
            set { totalSimilairty = value; }
        }

        [Key(36)]
        public float MassSpectraSimilarity
        {
            get { return massSpectraSimilarity; }
            set { massSpectraSimilarity = value; }
        }

        [Key(37)]
        public float ReverseSimilarity
        {
            get { return reverseSimilarity; }
            set { reverseSimilarity = value; }
        }

        [Key(38)]
        public float FragmentPresencePercentage
        {
            get { return fragmentPresencePercentage; }
            set { fragmentPresencePercentage = value; }
        }

        [Key(39)]
        public float IsotopeSimilarity
        {
            get { return isotopeSimilarity; }
            set { isotopeSimilarity = value; }
        }

        [Key(40)]
        public int PostDefinedIsotopeParentID
        {
            get { return postDefinedIsotopeParentID; }
            set { postDefinedIsotopeParentID = value; }
        }

        [Key(41)]
        public int PostDefinedAdductParentID
        {
            get { return postDefinedAdductParentID; }
            set { postDefinedAdductParentID = value; }
        }

        [Key(42)]
        public int PostDefinedIsotopeWeightNumber
        {
            get {
                return postDefinedIsotopeWeightNumber;
            }

            set {
                postDefinedIsotopeWeightNumber = value;
            }
        }

        [Key(43)]
        public List<AlignmentSpotVariableCorrelation> AlignmentSpotVariableCorrelations
        {
            get {
                return alignmentSpotVariableCorrelations;
            }

            set {
                alignmentSpotVariableCorrelations = value;
            }
        }

        [Key(44)]
        public int ChargeNumber {
            get {
                return chargeNumber;
            }

            set {
                chargeNumber = value;
            }
        }

        [Key(45)]
        public bool IsFragmentQueryExist {
            get {
                return isFragmentQueryExist;
            }

            set {
                isFragmentQueryExist = value;
            }
        }
        [Key(46)]
        public string Comment {
            get { return comment; }
            set { comment = value; }
        }

        [Key(47)]
        public float AnovaPval {
            get { return anovaPval; }
            set { anovaPval = value; }
        }

        [Key(48)]
        public float FoldChange {
            get { return foldChange; }
            set { foldChange = value; }
        }

        [Key(49)]
        public List<LinkedPeakFeature> PeakLinks {
            get {
                return peakLinks;
            }

            set {
                peakLinks = value;
            }
        }

        [Key(50)]
        public bool IsLinked {
            get {
                return isLinked;
            }

            set {
                isLinked = value;
            }
        }

        [Key(51)]
        public int PeakGroupID {
            get {
                return peakGroupID;
            }

            set {
                peakGroupID = value;
            }
        }

        [Key(52)]
        public bool IsBlankFiltered {
            get {
                return isBlankFiltered;
            }

            set {
                isBlankFiltered = value;
            }
        }

        [Key(53)]
        public bool IsManuallyModified {
            get {
                return isManuallyModified;
            }

            set {
                isManuallyModified = value;
            }
        }

        [Key(54)]
        public int IdentificationRank {
            get { return identificationRank; }
            set { identificationRank = value; }
        }
        [Key(55)]
        public List<int> LibraryIdList {
            get { return libraryIdList; }
            set { libraryIdList = value; }
        }

        [Key(56)]
        public List<int> CorrelBasedlibraryIdList {
            get { return correlBasedlibraryIdList; }
            set { correlBasedlibraryIdList = value; }
        }

        [Key(57)]
        public float SimpleDotProductSimilarity {
            get { return simpleDotProductSimilarity; }
            set { simpleDotProductSimilarity = value; }
        }

        [Key(58)]
        public int CorrelDecIdentificationRank {
            get { return correlDecIdentificationRank; }
            set { correlDecIdentificationRank = value; }
        }

        [Key(59)]
        public float EstimatedNoiseMax {
            get {
                return estimatedNoiseMax;
            }

            set {
                estimatedNoiseMax = value;
            }
        }

        [Key(60)]
        public float SignalToNoiseMax {
            get {
                return signalToNoiseMax;
            }

            set {
                signalToNoiseMax = value;
            }
        }

        [Key(61)]
        public float EstimatedNoiseMin {
            get {
                return estimatedNoiseMin;
            }

            set {
                estimatedNoiseMin = value;
            }
        }

        [Key(62)]
        public float SignalToNoiseMin {
            get {
                return signalToNoiseMin;
            }

            set {
                signalToNoiseMin = value;
            }
        }

        [Key(63)]
        public float EstimatedNoiseAve {
            get {
                return estimatedNoiseAve;
            }

            set {
                estimatedNoiseAve = value;
            }
        }

        [Key(64)]
        public float SignalToNoiseAve {
            get {
                return signalToNoiseAve;
            }

            set {
                signalToNoiseAve = value;
            }
        }

        [Key(65)]
        public IonAbundanceUnit IonAbundanceUnit {
            get {
                return ionAbundanceUnit;
            }

            set {
                ionAbundanceUnit = value;
            }
        }

        [Key(66)]
        public int MasterID {
            get {
                return masterID;
            }

            set {
                masterID = value;
            }
        }

        [Key(67)]
        public ObservableCollection<AlignedDriftSpotPropertyBean> AlignedDriftSpots {
            get {
                return alignedDriftSpots;
            }

            set {
                alignedDriftSpots = value;
            }
        }

        [Key(68)]
        public string MasterIdString {
            get {
                return masterIdString;
            }

            set {
                masterIdString = value;
            }
        }

        [Key(69)]
        public string PostCurationResult {
            get {
                return postCurationResult;
            }

            set {
                postCurationResult = value;
            }
        }

        [Key(70)]
        public string SpectrumReferenceFileName {
            get {
                return spectrumReferenceFileName;
            }

            set {
                spectrumReferenceFileName = value;
            }
        }

        [Key(71)]
        public string SpectrumReferenceFileClass {
            get {
                return spectrumReferenceFileClass;
            }

            set {
                spectrumReferenceFileClass = value;
            }
        }

        [Key(72)]
        public string Ms1SpecString {
            get {
                return ms1SpecString;
            }

            set {
                ms1SpecString = value;
            }
        }

        [Key(73)]
        public string Ms2SpecString {
            get {
                return ms2SpecString;
            }

            set {
                ms2SpecString = value;
            }
        }

        [Key(74)]
        public string Smiles {
            get {
                return smiles;
            }

            set {
                smiles = value;
            }
        }

        [Key(75)]
        public string Inchikey {
            get {
                return inchikey;
            }

            set {
                inchikey = value;
            }
        }

        [Key(76)]
        public string Ontology {
            get {
                return ontology;
            }

            set {
                ontology = value;
            }
        }

        [Key(77)]
        public string AdductIonNameFromAmalgamation {
            get {
                return adductIonNameFromAmalgamation;
            }

            set {
                adductIonNameFromAmalgamation = value;
            }
        }

        [Key(78)]
        public bool IsMs1Match {
            get {
                return isMs1Match;
            }

            set {
                isMs1Match = value;
            }
        }

        [Key(79)]
        public bool IsMs2Match {
            get {
                return isMs2Match;
            }

            set {
                isMs2Match = value;
            }
        }

        [Key(80)]
        public bool IsRtMatch {
            get {
                return isRtMatch;
            }

            set {
                isRtMatch = value;
            }
        }

        [Key(81)]
        public bool IsCcsMatch {
            get {
                return isCcsMatch;
            }

            set {
                isCcsMatch = value;
            }
        }

        [Key(82)]
        public float CcsSimilarity {
            get {
                return ccsSimilarity;
            }

            set {
                ccsSimilarity = value;
            }
        }

        [Key(83)]
        public bool IsLipidClassMatch {
            get {
                return isLipidClassMatch;
            }

            set {
                isLipidClassMatch = value;
            }
        }

        [Key(84)]
        public bool IsLipidChainsMatch {
            get {
                return isLipidChainsMatch;
            }

            set {
                isLipidChainsMatch = value;
            }
        }

        [Key(85)]
        public bool IsLipidPositionMatch {
            get {
                return isLipidPositionMatch;
            }

            set {
                isLipidPositionMatch = value;
            }
        }

        [Key(86)]
        public bool IsOtherLipidMatch {
            get {
                return isOtherLipidMatch;
            }

            set {
                isOtherLipidMatch = value;
            }
        }

        [Key(87)]
        public bool IsManuallyModifiedForAnnotation {
            get {
                return isManuallyModifiedForAnnotation;
            }

            set {
                isManuallyModifiedForAnnotation = value;
            }
        }

        public void UpdateStatisticalValues() {
            OnPropertyChanged("AnovaPval");
            OnPropertyChanged("FoldChange");
            OnPropertyChanged("CentralRetentionTime");
            OnPropertyChanged("SignalToNoiseAve");

            // for ion mobility data update
            OnPropertyChanged("RT");
            OnPropertyChanged("SN");
            OnPropertyChanged("Anova");
            OnPropertyChanged("FC");
            OnPropertyChanged("Mobility");
            OnPropertyChanged("CCS");
        }
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

    [MessagePackObject]
    public class AlignedDriftSpotPropertyBean {
        private int masterID;
        private int alignmentSpotID;
        private int alignmentID;
        private int chargeNumber;
        private float averageValiable;
        private float minValiable;
        private float maxValiable;
        private int representativeFileID;
        private Formula formula;
        private AdductIon adduct;

        private float estimatedNoiseMax;
        private float signalToNoiseMax;
        private float estimatedNoiseMin;
        private float signalToNoiseMin;
        private float estimatedNoiseAve;
        private float signalToNoiseAve;

        private int postDefinedIsotopeWeightNumber;
        private int postDefinedIsotopeParentID;
        private int postDefinedAdductParentID;

        private List<AlignmentSpotVariableCorrelation> alignmentSpotVariableCorrelations;

        private int libraryID;
        private int postIdentificationLibraryID;

        private float minDt;
        private float maxDt;
        private float minCcs;
        private float maxCcs;
        private float averagePeakWidth;

        private float minMz;
        private float maxMz;
        private float fillParcentage;
        private float relativeAmplitudeValue;
        private float monoIsotopicPercentage;

        private float centralDriftTime;
        private float centralCcs;
        private float centralAccurateMass;
        private string metaboliteName;
        private string adductIonName;
        private bool msmsIncluded;
        private float totalSimilairty;
        private bool isBlankFiltered;

        private float massSpectraSimilarity;
        private float reverseSimilarity;
        private float fragmentPresencePercentage;

        private bool isMs1Match;
        private bool isMs2Match;
        private bool isRtMatch;
        private bool isCcsMatch;

        private bool isLipidClassMatch;
        private bool isLipidChainsMatch;
        private bool isLipidPositionMatch;
        private bool isOtherLipidMatch;

        private float isotopeSimilarity;
        private float retentionTimeSimilarity;
        private float ccsSimilarity;
        private int displayedSpotID;

        private float accurateMassSimilarity;
        private int internalStandardAlignmentID;
        ObservableCollection<AlignedPeakPropertyBean> alignedPeakPropertyBeanCollection;

        //Searcher
        private bool isFragmentQueryExist;
        private string comment;
        private float anovaPval;
        private float foldChange;

        //character
        private List<LinkedPeakFeature> peakLinks;
        private bool isLinked;
        private int peakGroupID;
        private bool isManuallyModified;
        private bool isManuallyModifiedForAnnotation;
        private IonAbundanceUnit ionAbundanceUnit;

        public AlignedDriftSpotPropertyBean() {
            this.masterID = -1;
            this.alignmentSpotID = -1;
            this.alignmentID = -1;
            this.postDefinedAdductParentID = -1;
            this.postDefinedIsotopeParentID = -1;
            this.postDefinedIsotopeWeightNumber = -1;
            this.centralDriftTime = -1;
            this.centralCcs = -1;
            this.libraryID = -1;
            this.chargeNumber = 1;
            this.averagePeakWidth = 5;
            this.postIdentificationLibraryID = -1;
            this.internalStandardAlignmentID = -1;
            this.representativeFileID = -1;
            this.massSpectraSimilarity = -1;
            this.reverseSimilarity = -1;
            this.isotopeSimilarity = -1;
            this.totalSimilairty = -1;
            this.retentionTimeSimilarity = -1;
            this.ccsSimilarity = -1;
            this.fragmentPresencePercentage = -1;
            this.formula = new Formula();
            this.alignedPeakPropertyBeanCollection = new ObservableCollection<AlignedPeakPropertyBean>();
            this.alignmentSpotVariableCorrelations = new List<AlignmentSpotVariableCorrelation>();
            this.isFragmentQueryExist = false;
            this.comment = string.Empty;
            this.foldChange = -1;
            this.anovaPval = -1;
            this.peakLinks = new List<LinkedPeakFeature>();
            this.isLinked = false;
            this.peakGroupID = -1;
            this.IsBlankFiltered = false;
            this.isManuallyModified = false;
            this.isManuallyModifiedForAnnotation = false;
            this.estimatedNoiseAve = 1;
            this.estimatedNoiseMax = 1;
            this.estimatedNoiseMin = 1;
            this.signalToNoiseAve = 1;
            this.signalToNoiseMax = 1;
            this.signalToNoiseMin = 1;
            this.displayedSpotID = 0;
            this.ionAbundanceUnit = IonAbundanceUnit.Height;
        }

        [Key(0)]
        public int InternalStandardAlignmentID {
            get { return internalStandardAlignmentID; }
            set { internalStandardAlignmentID = value; }
        }

        [Key(1)]
        public float AverageValiable {
            get { return averageValiable; }
            set { averageValiable = value; }
        }

        [Key(2)]
        public float MinValiable {
            get { return minValiable; }
            set { minValiable = value; }
        }

        [Key(3)]
        public float MaxValiable {
            get { return maxValiable; }
            set { maxValiable = value; }
        }

        [Key(4)]
        public float AveragePeakWidth {
            get { return averagePeakWidth; }
            set { averagePeakWidth = value; }
        }

        [Key(5)]
        public int RepresentativeFileID {
            get { return representativeFileID; }
            set { representativeFileID = value; }
        }

        [Key(6)]
        public int LibraryID {
            get { return libraryID; }
            set { libraryID = value; }
        }

        [Key(7)]
        public int PostIdentificationLibraryID {
            get { return postIdentificationLibraryID; }
            set { postIdentificationLibraryID = value; }
        }

        [Key(8)]
        public float CentralCcs {
            get { return centralCcs; }
            set { centralCcs = value; }
        }

        [Key(9)]
        public Formula Formula {
            get { return formula; }
            set { formula = value; }
        }

        [Key(10)]
        public AdductIon Adduct {
            get { return adduct; }
            set { adduct = value; }
        }

        [Key(11)]
        public float RetentionTimeSimilarity {
            get { return retentionTimeSimilarity; }
            set { retentionTimeSimilarity = value; }
        }

        [Key(12)]
        public float CcsSimilarity {
            get { return ccsSimilarity; }
            set { ccsSimilarity = value; }
        }

        [Key(13)]
        public float AccurateMassSimilarity {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        [Key(14)]
        public float MinDt {
            get { return minDt; }
            set { minDt = value; }
        }

        [Key(15)]
        public float MaxDt {
            get { return maxDt; }
            set { maxDt = value; }
        }

        [Key(16)]
        public float MinMz {
            get { return minMz; }
            set { minMz = value; }
        }

        [Key(17)]
        public float MaxMz {
            get { return maxMz; }
            set { maxMz = value; }
        }

        [Key(18)]
        public int AlignmentID {
            get { return alignmentID; }
            set { alignmentID = value; }
        }

        [Key(19)]
        public float MonoIsotopicPercentage {
            get { return monoIsotopicPercentage; }
            set { monoIsotopicPercentage = value; }
        }

        [Key(20)]
        public float CentralDriftTime {
            get { return centralDriftTime; }
            set { centralDriftTime = value; }
        }

        [Key(21)]
        public float CentralAccurateMass {
            get { return centralAccurateMass; }
            set { centralAccurateMass = value; }
        }

        [Key(22)]
        public float FillParcentage {
            get { return fillParcentage; }
            set { fillParcentage = value; }
        }

        [Key(23)]
        public ObservableCollection<AlignedPeakPropertyBean> AlignedPeakPropertyBeanCollection {
            get { return alignedPeakPropertyBeanCollection; }
            set { alignedPeakPropertyBeanCollection = value; }
        }

        [Key(24)]
        public string MetaboliteName {
            get { return metaboliteName; }
            set { if (metaboliteName == value) return; metaboliteName = value; OnPropertyChanged("MetaboliteName"); }
        }

        [Key(25)]
        public string AdductIonName {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(26)]
        public float RelativeAmplitudeValue {
            get { return relativeAmplitudeValue; }
            set { relativeAmplitudeValue = value; }
        }

        [Key(27)]
        public bool MsmsIncluded {
            get { return msmsIncluded; }
            set { msmsIncluded = value; }
        }

        [Key(28)]
        public float TotalSimilairty {
            get { return totalSimilairty; }
            set { totalSimilairty = value; }
        }

        [Key(29)]
        public float MassSpectraSimilarity {
            get { return massSpectraSimilarity; }
            set { massSpectraSimilarity = value; }
        }

        [Key(30)]
        public float ReverseSimilarity {
            get { return reverseSimilarity; }
            set { reverseSimilarity = value; }
        }

        [Key(31)]
        public float FragmentPresencePercentage {
            get { return fragmentPresencePercentage; }
            set { fragmentPresencePercentage = value; }
        }

        [Key(32)]
        public float IsotopeSimilarity {
            get { return isotopeSimilarity; }
            set { isotopeSimilarity = value; }
        }

        [Key(33)]
        public int PostDefinedIsotopeParentID {
            get { return postDefinedIsotopeParentID; }
            set { postDefinedIsotopeParentID = value; }
        }

        [Key(34)]
        public int PostDefinedAdductParentID {
            get { return postDefinedAdductParentID; }
            set { postDefinedAdductParentID = value; }
        }

        [Key(35)]
        public int PostDefinedIsotopeWeightNumber {
            get {
                return postDefinedIsotopeWeightNumber;
            }

            set {
                postDefinedIsotopeWeightNumber = value;
            }
        }

        [Key(36)]
        public List<AlignmentSpotVariableCorrelation> AlignmentSpotVariableCorrelations {
            get {
                return alignmentSpotVariableCorrelations;
            }

            set {
                alignmentSpotVariableCorrelations = value;
            }
        }

        [Key(37)]
        public int ChargeNumber {
            get {
                return chargeNumber;
            }

            set {
                chargeNumber = value;
            }
        }

        [Key(38)]
        public bool IsFragmentQueryExist {
            get {
                return isFragmentQueryExist;
            }

            set {
                isFragmentQueryExist = value;
            }
        }

        [Key(39)]
        public string Comment {
            get { return comment; }
            set { comment = value; }
        }

        [Key(40)]
        public float AnovaPval {
            get { return anovaPval; }
            set { anovaPval = value; }
        }

        [Key(41)]
        public float FoldChange {
            get { return foldChange; }
            set { foldChange = value; }
        }

        [Key(42)]
        public List<LinkedPeakFeature> PeakLinks {
            get {
                return peakLinks;
            }

            set {
                peakLinks = value;
            }
        }

        [Key(43)]
        public bool IsLinked {
            get {
                return isLinked;
            }

            set {
                isLinked = value;
            }
        }

        [Key(44)]
        public int PeakGroupID {
            get {
                return peakGroupID;
            }

            set {
                peakGroupID = value;
            }
        }

        [Key(45)]
        public bool IsBlankFiltered {
            get {
                return isBlankFiltered;
            }

            set {
                isBlankFiltered = value;
            }
        }

        [Key(46)]
        public bool IsManuallyModified {
            get {
                return isManuallyModified;
            }

            set {
                isManuallyModified = value;
            }
        }

        [Key(47)]
        public float EstimatedNoiseMax {
            get {
                return estimatedNoiseMax;
            }

            set {
                estimatedNoiseMax = value;
            }
        }

        [Key(48)]
        public float SignalToNoiseMax {
            get {
                return signalToNoiseMax;
            }

            set {
                signalToNoiseMax = value;
            }
        }

        [Key(49)]
        public float EstimatedNoiseMin {
            get {
                return estimatedNoiseMin;
            }

            set {
                estimatedNoiseMin = value;
            }
        }

        [Key(50)]
        public float SignalToNoiseMin {
            get {
                return signalToNoiseMin;
            }

            set {
                signalToNoiseMin = value;
            }
        }

        [Key(51)]
        public float EstimatedNoiseAve {
            get {
                return estimatedNoiseAve;
            }

            set {
                estimatedNoiseAve = value;
            }
        }

        [Key(52)]
        public float SignalToNoiseAve {
            get {
                return signalToNoiseAve;
            }

            set {
                signalToNoiseAve = value;
            }
        }

        [Key(53)]
        public IonAbundanceUnit IonAbundanceUnit {
            get {
                return ionAbundanceUnit;
            }

            set {
                ionAbundanceUnit = value;
            }
        }

        [Key(54)]
        public int MasterID {
            get {
                return masterID;
            }

            set {
                masterID = value;
            }
        }

        [Key(55)]
        public int AlignmentSpotID {
            get {
                return alignmentSpotID;
            }

            set {
                alignmentSpotID = value;
            }
        }

        [Key(56)]
        public int DisplayedSpotID {
            get {
                return displayedSpotID;
            }

            set {
                displayedSpotID = value;
            }
        }

        [Key(57)]
        public float MinCcs {
            get {
                return minCcs;
            }

            set {
                minCcs = value;
            }
        }

        [Key(58)]
        public float MaxCcs {
            get {
                return maxCcs;
            }

            set {
                maxCcs = value;
            }
        }

        [Key(59)]
        public bool IsMs1Match {
            get {
                return isMs1Match;
            }

            set {
                isMs1Match = value;
            }
        }

        [Key(60)]
        public bool IsMs2Match {
            get {
                return isMs2Match;
            }

            set {
                isMs2Match = value;
            }
        }

        [Key(61)]
        public bool IsRtMatch {
            get {
                return isRtMatch;
            }

            set {
                isRtMatch = value;
            }
        }

        [Key(62)]
        public bool IsCcsMatch {
            get {
                return isCcsMatch;
            }

            set {
                isCcsMatch = value;
            }
        }

        [Key(63)]
        public bool IsLipidClassMatch {
            get {
                return isLipidClassMatch;
            }

            set {
                isLipidClassMatch = value;
            }
        }

        [Key(64)]
        public bool IsLipidChainsMatch {
            get {
                return isLipidChainsMatch;
            }

            set {
                isLipidChainsMatch = value;
            }
        }

        [Key(65)]
        public bool IsLipidPositionMatch {
            get {
                return isLipidPositionMatch;
            }

            set {
                isLipidPositionMatch = value;
            }
        }

        [Key(66)]
        public bool IsOtherLipidMatch {
            get {
                return isOtherLipidMatch;
            }

            set {
                isOtherLipidMatch = value;
            }
        }

        [Key(67)]
        public bool IsManuallyModifiedForAnnotation {
            get {
                return isManuallyModifiedForAnnotation;
            }

            set {
                isManuallyModifiedForAnnotation = value;
            }
        }

        public void UpdateStatisticalValues() {
            OnPropertyChanged("AnovaPval");
            OnPropertyChanged("FoldChange");
        }
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
