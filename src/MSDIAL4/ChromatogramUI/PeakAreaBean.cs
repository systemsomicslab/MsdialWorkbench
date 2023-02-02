using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Rfx.Riken.OsakaUniv
{
    [Serializable()]
    public class PeakAreaBean
    {
        //Basic information
        private int peakID;
        private int scanNumberAtPeakTop;
        private int scanNumberAtLeftPeakEdge;
        private int scanNumberAtRightPeakEdge;
        private float rtAtPeakTop;
        private float rtAtLeftPeakEdge;
        private float rtAtRightPeakEdge;
        private int intensityAtLeftPeakEdge;
        private int intensityAtRightPeakEdge;
        private int intensityAtPeakTop;
        private int areaAboveZero;
        private int areaAboveBaseline;
        private float normalizedValue;

        //Peak feature
        private float peakPureValue;
        private float shapenessValue;
        private float gaussianSimilarityValue;
        private float idealSlopeValue;
        private float basePeakValue;
        private float symmetryValue;
        private float amplitudeScoreValue;
        private float amplitudeOrderValue;
        
        //Identification (MS/MS)
        private string metaboliteName;
        private string adductIonName;
        private int adductParent;
        private int isotopeWeightNumber;
        private int isotopeParentPeakID;
        private float accurateMass;
        private float rtSimilarityValue;
        private float totalScore;
        private float alignedRetentionTime;
        private float isotopeSimilarityValue;
        private float massSpectraSimilarityValue;
        private float reverseSearchSimilarityValue;
        private int ms1LevelDatapointNumber;
        private int ms2LevelDatapointNumber;
        private int libraryID;
        private int deconvolutionID;

        //Identification (MRM)
        private float amplitudeRatioSimilatiryValue;
        private float peakTopDifferencialValue;
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
            get { return totalScore; }
            set { totalScore = value; }
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

        public int DeconvolutionID
        {
            get { return deconvolutionID; }
            set { deconvolutionID = value; }
        }

        #endregion
    }
}
