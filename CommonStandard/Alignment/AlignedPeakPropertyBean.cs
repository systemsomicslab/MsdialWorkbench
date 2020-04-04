using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using MessagePack;

namespace Rfx.Riken.OsakaUniv
{
    /// <summary>
    /// This class and 'AlignedPeakPropertyBean.cs' are used to store the properties of peak alignment result.
    /// 'AlignmentPropertyBean.cs' stores the summary of each alignment spot.
    /// 'AlignedPeakPropertyBean.cs' stores the detail of each peak aligned to a spot described as 'AlignmentPropertyBean.cs'
    /// </summary>
    [DataContract]
    [MessagePackObject]
    public class AlignedPeakPropertyBean
    {
        [DataMember]
        private int fileID;
        [DataMember]
        private int peakID;
        private int masterPeakID;
        private int peakAreaBeanID;
        [DataMember]
        private float retentionTime;
        [DataMember]
        private float retentionIndex;
        private float driftTime;
        private float ccs;
        [DataMember]
        private float accurateMass;
        [DataMember]
        private float quantMass;
        [DataMember]
        private float peakWidth;
        [DataMember]
        private float retentionTimeLeft;
        [DataMember]
        private float retentionTimeRight;
        [DataMember]
        private float retentionIndexLeft;
        [DataMember]
        private float retentionIndexRight;
        private float driftTimeLeft;
        private float driftTimeRight;

        [DataMember]
        private float variable;
        [DataMember]
        private float area;
        [DataMember]
        private float normalizedVariable;
        [DataMember]
        private float estimatedNoise;
        [DataMember]
        private float signalToNoise;
        [DataMember]
        private string fileName;
        [DataMember]
        private int ms2ScanNumber;
        private int ms1ScanNumber;
        [DataMember]
        private long seekPoint;
        [DataMember]
        private string metaboliteName;
        [DataMember]
        private string adductIonName;

        [DataMember]
        private int chargeNumber;
        [DataMember]
        private int isotopeParentID;
        [DataMember]
        private int isotopeNumber;
        [DataMember]
        private int libraryID;
        [DataMember]
        private int postIdentificationLibraryID;

        [DataMember]
        private float totalSimilairty;
        [DataMember]
        private float massSpectraSimilarity;
        [DataMember]
        private float reverseSimilarity;
        [DataMember]
        private float fragmentPresencePercentage;
        [DataMember]
        private float eiSpectrumSimilarity;
        [DataMember]
        private float isotopeSimilarity;
        [DataMember]
        private float retentionTimeSimilarity;
        [DataMember]
        private float retentionIndexSimilarity;
        private float ccsSimilarity;
        [DataMember]
        private float accurateMassSimilarity;

        private bool isMs1Match;
        private bool isMs2Match;
        private bool isRtMatch;
        private bool isCcsMatch;
        private bool isLipidClassMatch;
        private bool isLipidChainsMatch;
        private bool isLipidPositionMatch;
        private bool isOtherLipidMatch;

        private float simpleDotProductSimilarity;
        [DataMember]
        private List<LinkedPeakFeature> peakLinks;
        [DataMember]
        private bool isLinked;
        [DataMember]
        private int peakGroupID;

        [DataMember]
        private bool isManuallyModified;
        [DataMember]
        private int identificationRank;
        [DataMember]
        private List<int> libraryList;


        public AlignedPeakPropertyBean()
        {
            this.fileID = -1;
            this.peakID = -1;
            this.masterPeakID = -1;
            this.peakAreaBeanID = -1;

            this.retentionTime = -1;
            this.retentionTimeLeft = -1;
            this.retentionTimeRight = -1;
            this.accurateMass = -1;
            this.quantMass = -1;
            this.seekPoint = -1;
            this.retentionIndex = -1;
            this.retentionIndexLeft = -1;
            this.retentionIndexRight = -1;
            this.driftTime = -1;
            this.driftTimeLeft = -1;
            this.driftTimeRight = -1;
            this.ccs = -1;
            this.variable = -1;
            this.area = -1;
            this.ms1ScanNumber = -1;
            this.ms2ScanNumber = -1;
            this.fileName = string.Empty;
            this.metaboliteName = string.Empty;
            this.adductIonName = string.Empty;
            this.isotopeNumber = -1;
            this.libraryID = -1;
            this.postIdentificationLibraryID = -1;
            this.massSpectraSimilarity = -1;
            this.reverseSimilarity = -1;
            this.fragmentPresencePercentage = -1;
            this.eiSpectrumSimilarity = -1;
            this.isotopeSimilarity = -1;
            this.totalSimilairty = -1;
            this.retentionTimeSimilarity = -1;
            this.retentionIndexSimilarity = -1;
            this.accurateMassSimilarity = -1;
            this.ccsSimilarity = -1;
            this.peakWidth = -1;
            this.chargeNumber = 1;
            this.isotopeParentID = -1;
            this.peakLinks = new List<LinkedPeakFeature>();
            this.isLinked = false;
            this.peakGroupID = -1;
            this.isManuallyModified = false;
            this.identificationRank = -1;
            this.libraryList = new List<int>();
            this.signalToNoise = 1;
            this.estimatedNoise = 1;
        }

        [Key(0)]
        public string FileName
        {
            get { return fileName; }
            set { fileName = value; }
        }

        [Key(1)]
        public int FileID
        {
            get { return fileID; }
            set { fileID = value; }
        }

        [Key(2)]
        public int PeakID
        {
            get { return peakID; }
            set { peakID = value; }
        }

        [Key(3)]
        public float RetentionTime
        {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        [Key(4)]
        public float RetentionIndex
        {
            get { return retentionIndex; }
            set { retentionIndex = value; }
        }

        [Key(5)]
        public float PeakWidth
        {
            get { return peakWidth; }
            set { peakWidth = value; }
        }

        [Key(6)]
        public float AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        [Key(7)]
        public float QuantMass
        {
            get { return quantMass; }
            set { quantMass = value; }
        }

        [Key(8)]
        public float Variable
        {
            get { return variable; }
            set { variable = value; }
        }

        [Key(9)]
        public float Area
        {
            get { return area; }
            set { area = value; }
        }

        [Key(10)]
        public float NormalizedVariable
        {
            get { return normalizedVariable; }
            set { normalizedVariable = value; }
        }

        [Key(11)]
        public int Ms2ScanNumber
        {
            get { return ms2ScanNumber; }
            set { ms2ScanNumber = value; }
        }

        [Key(12)]
        public long SeekPoint
        {
            get { return seekPoint; }
            set { seekPoint = value; }
        }

        [Key(13)]
        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        [Key(14)]
        public int LibraryID
        {
            get { return libraryID; }
            set { libraryID = value; }
        }

        [Key(15)]
        public int PostIdentificationLibraryID
        {
            get { return postIdentificationLibraryID; }
            set { postIdentificationLibraryID = value; }
        }

        [Key(16)]
        public float MassSpectraSimilarity
        {
            get { return massSpectraSimilarity; }
            set { massSpectraSimilarity = value; }
        }

        [Key(17)]
        public float EiSpectrumSimilarity
        {
            get { return eiSpectrumSimilarity; }
            set { eiSpectrumSimilarity = value; }
        }

        [Key(18)]
        public float RetentionTimeSimilarity
        {
            get { return retentionTimeSimilarity; }
            set { retentionTimeSimilarity = value; }
        }

        [Key(19)]
        public float RetentionIndexSimilarity
        {
            get { return retentionIndexSimilarity; }
            set { retentionIndexSimilarity = value; }
        }

        [Key(20)]
        public float AccurateMassSimilarity
        {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        [Key(21)]
        public float ReverseSimilarity
        {
            get { return reverseSimilarity; }
            set { reverseSimilarity = value; }
        }

        [Key(22)]
        public float FragmentPresencePercentage
        {
            get { return fragmentPresencePercentage; }
            set { fragmentPresencePercentage = value; }
        }

        [Key(23)]
        public float IsotopeSimilarity
        {
            get { return isotopeSimilarity; }
            set { isotopeSimilarity = value; }
        }

        [Key(24)]
        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        [Key(25)]
        public int IsotopeNumber
        {
            get { return isotopeNumber; }
            set { isotopeNumber = value; }
        }

        [Key(26)]
        public float TotalSimilairty
        {
            get { return totalSimilairty; }
            set { totalSimilairty = value; }
        }

        [Key(27)]
        public int ChargeNumber {
            get {
                return chargeNumber;
            }

            set {
                chargeNumber = value;
            }
        }

        [Key(28)]
        public int IsotopeParentID {
            get {
                return isotopeParentID;
            }

            set {
                isotopeParentID = value;
            }
        }

        [Key(29)]
        public float RetentionTimeLeft {
            get {
                return retentionTimeLeft;
            }

            set {
                retentionTimeLeft = value;
            }
        }

        [Key(30)]
        public float RetentionTimeRight {
            get {
                return retentionTimeRight;
            }

            set {
                retentionTimeRight = value;
            }
        }

        [Key(31)]
        public float RetentionIndexLeft {
            get {
                return retentionIndexLeft;
            }

            set {
                retentionIndexLeft = value;
            }
        }

        [Key(32)]
        public float RetentionIndexRight {
            get {
                return retentionIndexRight;
            }

            set {
                retentionIndexRight = value;
            }
        }

        [Key(33)]
        public List<LinkedPeakFeature> PeakLinks {
            get {
                return peakLinks;
            }

            set {
                peakLinks = value;
            }
        }

        [Key(34)]
        public bool IsLinked {
            get {
                return isLinked;
            }

            set {
                isLinked = value;
            }
        }

        [Key(35)]
        public int PeakGroupID {
            get {
                return peakGroupID;
            }

            set {
                peakGroupID = value;
            }
        }

        [Key(36)]
        public bool IsManuallyModified {
            get {
                return isManuallyModified;
            }

            set {
                isManuallyModified = value;
            }
        }
        [Key(37)]
        public int IdentificationRank {
            get { return identificationRank; }
            set { identificationRank = value; }
        }

        [Key(38)]
        public List<int> LibraryIdList {
            get { return libraryList; }
            set { libraryList = value; }
        }

        [Key(39)]
        public float EstimatedNoise {
            get {
                return estimatedNoise;
            }

            set {
                estimatedNoise = value;
            }
        }

        [Key(40)]
        public float SignalToNoise {
            get {
                return signalToNoise;
            }

            set {
                signalToNoise = value;
            }
        }

        [Key(41)]
        public float DriftTime {
            get {
                return driftTime;
            }

            set {
                driftTime = value;
            }
        }

        [Key(42)]
        public float DriftTimeLeft {
            get {
                return driftTimeLeft;
            }

            set {
                driftTimeLeft = value;
            }
        }

        [Key(43)]
        public float DriftTimeRight {
            get {
                return driftTimeRight;
            }

            set {
                driftTimeRight = value;
            }
        }

        [Key(44)]
        public int MasterPeakID {
            get {
                return masterPeakID;
            }

            set {
                masterPeakID = value;
            }
        }

        [Key(45)]
        public int PeakAreaBeanID {
            get {
                return peakAreaBeanID;
            }

            set {
                peakAreaBeanID = value;
            }
        }

        [Key(46)]
        public float Ccs {
            get {
                return ccs;
            }

            set {
                ccs = value;
            }
        }

        [Key(47)]
        public float CcsSimilarity {
            get {
                return ccsSimilarity;
            }

            set {
                ccsSimilarity = value;
            }
        }

        [Key(48)]
        public bool IsMs1Match {
            get {
                return isMs1Match;
            }

            set {
                isMs1Match = value;
            }
        }

        [Key(49)]
        public bool IsMs2Match {
            get {
                return isMs2Match;
            }

            set {
                isMs2Match = value;
            }
        }

        [Key(50)]
        public bool IsRtMatch {
            get {
                return isRtMatch;
            }

            set {
                isRtMatch = value;
            }
        }

        [Key(51)]
        public bool IsCcsMatch {
            get {
                return isCcsMatch;
            }

            set {
                isCcsMatch = value;
            }
        }

        [Key(52)]
        public bool IsLipidClassMatch {
            get {
                return isLipidClassMatch;
            }

            set {
                isLipidClassMatch = value;
            }
        }

        [Key(53)]
        public bool IsLipidChainsMatch {
            get {
                return isLipidChainsMatch;
            }

            set {
                isLipidChainsMatch = value;
            }
        }

        [Key(54)]
        public bool IsLipidPositionMatch {
            get {
                return isLipidPositionMatch;
            }

            set {
                isLipidPositionMatch = value;
            }
        }

        [Key(55)]
        public bool IsOtherLipidMatch {
            get {
                return isOtherLipidMatch;
            }

            set {
                isOtherLipidMatch = value;
            }
        }

        [Key(56)]
        public float SimpleDotProductSimilarity {
            get {
                return simpleDotProductSimilarity;
            }
            set {
                simpleDotProductSimilarity = value;
            }
        }

        [Key(57)]
        public int Ms1ScanNumber {
            get {
                return ms1ScanNumber;
            }

            set {
                ms1ScanNumber = value;
            }
        }
    }
}
