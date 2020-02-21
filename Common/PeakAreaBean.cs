using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    [DataContract]
    public class PeakAreaBean
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
        private int intensityAtLeftPeakEdge;
        [DataMember]
        private int intensityAtRightPeakEdge;
        [DataMember]
        private int intensityAtPeakTop;
        [DataMember]
        private int areaAboveZero;
        [DataMember]
        private int areaAboveBaseline;
        [DataMember]
        private float normalizedValue;

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
        private float alignedRetentionTime;
        [DataMember]
        private float isotopeSimilarityValue;
        [DataMember]
        private float massSpectraSimilarityValue;
        [DataMember]
        private float reverseSearchSimilarityValue;
        [DataMember]
        private float presenseSimilarityValue;
        [DataMember]
        private int ms1LevelDatapointNumber;
        [DataMember]
        private int ms2LevelDatapointNumber;
        [DataMember]
        private int libraryID;
        [DataMember]
        private int postIdentificationLibraryId;
        [DataMember]
        private int deconvolutionID;

        //Identification (MRM)
        [DataMember]
        private float amplitudeRatioSimilatiryValue;
        [DataMember]
        private float peakTopDifferencialValue;
        [DataMember]
        private float peakShapeSimilarityValue;

        #region
        public float AmplitudeScoreValue
        {
            get { return amplitudeScoreValue; }
            set { amplitudeScoreValue = value; }
        }

        public float AmplitudeOrderValue
        {
            get { return amplitudeOrderValue; }
            set { amplitudeOrderValue = value; }
        }

        public float RtSimilarityValue
        {
            get { return rtSimilarityValue; }
            set { rtSimilarityValue = value; }
        }


        public float NormalizedValue
        {
            get { return normalizedValue; }
            set { normalizedValue = value; }
        }

        public int ScanNumberAtLeftPeakEdge
        {
            get { return scanNumberAtLeftPeakEdge; }
            set { scanNumberAtLeftPeakEdge = value; }
        }

        public float ReverseSearchSimilarityValue
        {
            get { return reverseSearchSimilarityValue; }
            set { reverseSearchSimilarityValue = value; }
        }
        public int IsotopeParentPeakID
        {
            get { return isotopeParentPeakID; }
            set { isotopeParentPeakID = value; }
        }

        public int AdductIonXmer
        {
            get { return adductIonXmer; }
            set { adductIonXmer = value; }
        }

        public int AdductIonChargeNumber
        {
            get { return adductIonChargeNumber; }
            set { adductIonChargeNumber = value; }
        }

        public float PresenseSimilarityValue
        {
            get { return presenseSimilarityValue; }
            set { presenseSimilarityValue = value; }
        }

        public int AdductParent
        {
            get { return adductParent; }
            set { adductParent = value; }
        }

        public int ScanNumberAtRightPeakEdge
        {
            get { return scanNumberAtRightPeakEdge; }
            set { scanNumberAtRightPeakEdge = value; }
        }

        public float AmplitudeRatioSimilatiryValue
        {
            get { return amplitudeRatioSimilatiryValue; }
            set { amplitudeRatioSimilatiryValue = value; }
        }

        public float PeakTopDifferencialValue
        {
            get { return peakTopDifferencialValue; }
            set { peakTopDifferencialValue = value; }
        }

        public float PeakShapeSimilarityValue
        {
            get { return peakShapeSimilarityValue; }
            set { peakShapeSimilarityValue = value; }
        }

        public float ShapenessValue
        {
            get { return shapenessValue; }
            set { shapenessValue = value; }
        }

        public int PeakID
        {
            get { return peakID; }
            set { peakID = value; }
        }

        public string AdductIonName
        {
            get { return adductIonName; }
            set { adductIonName = value; }
        }

        public float RtAtLeftPeakEdge
        {
            get { return rtAtLeftPeakEdge; }
            set { rtAtLeftPeakEdge = value; }
        }

        public int IntensityAtLeftPeakEdge
        {
            get { return intensityAtLeftPeakEdge; }
            set { intensityAtLeftPeakEdge = value; }
        }

        public float RtAtRightPeakEdge
        {
            get { return rtAtRightPeakEdge; }
            set { rtAtRightPeakEdge = value; }
        }

        public int IntensityAtRightPeakEdge
        {
            get { return intensityAtRightPeakEdge; }
            set { intensityAtRightPeakEdge = value; }
        }

        public int IsotopeWeightNumber
        {
            get { return isotopeWeightNumber; }
            set { isotopeWeightNumber = value; }
        }

        public float RtAtPeakTop
        {
            get { return rtAtPeakTop; }
            set { rtAtPeakTop = value; }
        }

        public int IntensityAtPeakTop
        {
            get { return intensityAtPeakTop; }
            set { intensityAtPeakTop = value; }
        }

        public int ScanNumberAtPeakTop
        {
            get { return scanNumberAtPeakTop; }
            set { scanNumberAtPeakTop = value; }
        }

        public int AreaAboveZero
        {
            get { return areaAboveZero; }
            set { areaAboveZero = value; }
        }

        public int AreaAboveBaseline
        {
            get { return areaAboveBaseline; }
            set { areaAboveBaseline = value; }
        }

        public float PeakPureValue
        {
            get { return peakPureValue; }
            set { peakPureValue = value; }
        }

        public float GaussianSimilarityValue
        {
            get { return gaussianSimilarityValue; }
            set { gaussianSimilarityValue = value; }
        }

        public float IdealSlopeValue
        {
            get { return idealSlopeValue; }
            set { idealSlopeValue = value; }
        }

        public float AccurateMassSimilarity
        {
            get { return accurateMassSimilarity; }
            set { accurateMassSimilarity = value; }
        }

        public float BasePeakValue
        {
            get { return basePeakValue; }
            set { basePeakValue = value; }
        }

        public float SymmetryValue
        {
            get { return symmetryValue; }
            set { symmetryValue = value; }
        }

        public float TotalScore
        {
            get { return totalSimilarityValue; }
            set { totalSimilarityValue = value; }
        }

        public float AlignedRetentionTime
        {
            get { return alignedRetentionTime; }
            set { alignedRetentionTime = value; }
        }

        public string MetaboliteName
        {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        public float AccurateMass
        {
            get { return accurateMass; }
            set { accurateMass = value; }
        }

        public float IsotopeSimilarityValue
        {
            get { return isotopeSimilarityValue; }
            set { isotopeSimilarityValue = value; }
        }

        public float MassSpectraSimilarityValue
        {
            get { return massSpectraSimilarityValue; }
            set { massSpectraSimilarityValue = value; }
        }

        public int Ms1LevelDatapointNumber
        {
            get { return ms1LevelDatapointNumber; }
            set { ms1LevelDatapointNumber = value; }
        }

        public int Ms2LevelDatapointNumber
        {
            get { return ms2LevelDatapointNumber; }
            set { ms2LevelDatapointNumber = value; }
        }

        public int LibraryID
        {
            get { return libraryID; }
            set { libraryID = value; }
        }

        public int PostIdentificationLibraryId
        {
            get { return postIdentificationLibraryId; }
            set { postIdentificationLibraryId = value; }
        }

        public int DeconvolutionID
        {
            get { return deconvolutionID; }
            set { deconvolutionID = value; }
        }

        public float AdductIonAccurateMass
        {
            get { return adductIonAccurateMass; }
            set { adductIonAccurateMass = value; }
        }
        #endregion
    }
}
