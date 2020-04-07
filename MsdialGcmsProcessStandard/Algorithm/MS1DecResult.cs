using Rfx.Riken.OsakaUniv;
using System.Collections.Generic;

namespace Msdial.Gcms.Dataprocess.Algorithm {
	public class MS1DecResult {
        private int scanNumber;
        private int ms1DecID;

        // mz of the model ion
        private float basepeakMz;
        // height of the biggest peak
        private float basepeakHeight;
        // area of the biggest peak
        private float basepeakArea;
        // doh!!!
        private float retentionTime;
        // sum of heights of the all contributing peaks
        private float integratedHeight;
        // sum of areas of the all contributing peaks
        private float integratedArea;

        // used in peak spot display (slider cutoff)
        private float amplitudeScore;
        // used to find data in file
        private long seekPoint;


        // identification properties
        private int mspDbID;
        private string metaboliteName;

        private float retentionIndex;
        private float retentionTimeSimilarity;
        private float retentionIndexSimilarity;

        // avg of (dotProd + reverseDotProd + presence% )
        private float eiSpectrumSimilarity;

        //next 3 come from scoring
        private float dotProduct;
        private float reverseDotProduct;
        private float presencePersentage;

        // avg of eiSpecSimilarity and (retention time || retention index) similarity
        private float totalSimilarity;

        // end identification properties

        private List<Peak> basepeakChromatogram;
        private List<Peak> spectrum;
		private string splash;
        private List<float> modelMasses;
		private float modelPeakPurity;
		private float modelPeakQuality;
		private float signalNoiseRatio;
        private float estimatedNoise;

        public MS1DecResult() {
            mspDbID = -1;
            retentionIndex = -1;
            retentionTime = -1;
            basepeakArea = -1;
            basepeakHeight = -1;
            basepeakMz = -1;
            metaboliteName = string.Empty;
            basepeakChromatogram = new List<Peak>();
            spectrum = new List<Peak>();
			splash = string.Empty;
            modelMasses = new List<float>();
			modelPeakPurity = 0.0F;
			modelPeakQuality = 0.0F;
			signalNoiseRatio = 0.0F;
            estimatedNoise = 1.0F;
            totalSimilarity = -1;
            dotProduct = -1;
            reverseDotProduct = -1;
            presencePersentage = -1;
		}

        public int ScanNumber {
            get { return scanNumber; }
            set { scanNumber = value; }
        }

        public int Ms1DecID {
            get { return ms1DecID; }
            set { ms1DecID = value; }
        }

        public float BasepeakMz {
            get { return basepeakMz; }
            set { basepeakMz = value; }
        }

        public float BasepeakHeight {
            get { return basepeakHeight; }
            set { basepeakHeight = value; }
        }

        public float BasepeakArea {
            get { return basepeakArea; }
            set { basepeakArea = value; }
        }

        public float RetentionTime {
            get { return retentionTime; }
            set { retentionTime = value; }
        }

        public float IntegratedHeight {
            get { return integratedHeight; }
            set { integratedHeight = value; }
        }

        public float IntegratedArea {
            get { return integratedArea; }
            set { integratedArea = value; }
        }

        public float AmplitudeScore {
            get { return amplitudeScore; }
            set { amplitudeScore = value; }
        }

        public List<Peak> BasepeakChromatogram {
            get { return basepeakChromatogram; }
            set { basepeakChromatogram = value; }
        }

		public List<Peak> Spectrum {
			get { return spectrum; }
			set { spectrum = value; }
		}

		public string Splash {
			get { return splash; }
			set { splash = value; }
		}

		public long SeekPoint {
            get { return seekPoint; }
            set { seekPoint = value; }
        }

        public int MspDbID {
            get { return mspDbID; }
            set { mspDbID = value; }
        }

        public string MetaboliteName {
            get { return metaboliteName; }
            set { metaboliteName = value; }
        }

        public float RetentionIndex {
            get { return retentionIndex; }
            set { retentionIndex = value; }
        }

        public float RetentionTimeSimilarity {
            get { return retentionTimeSimilarity; }
            set { retentionTimeSimilarity = value; }
        }

        public float RetentionIndexSimilarity {
            get { return retentionIndexSimilarity; }
            set { retentionIndexSimilarity = value; }
        }

        public float EiSpectrumSimilarity {
            get { return eiSpectrumSimilarity; }
            set { eiSpectrumSimilarity = value; }
        }

        public float DotProduct {
            get { return dotProduct; }
            set { dotProduct = value; }
        }

        public float ReverseDotProduct {
            get { return reverseDotProduct; }
            set { reverseDotProduct = value; }
        }

        public float PresencePersentage {
            get { return presencePersentage; }
            set { presencePersentage = value; }
        }

        public float TotalSimilarity {
            get { return totalSimilarity; }
            set { totalSimilarity = value; }
        }

        public List<float> ModelMasses {
            get { return modelMasses; }
            set { modelMasses = value; }
        }

		public float ModelPeakPurity {
			get { return modelPeakPurity; }
			set { modelPeakPurity = value; }
		}

		public float ModelPeakQuality {
			get { return modelPeakQuality; }
			set { modelPeakQuality = value; }
		}

		public float SignalNoiseRatio {
			get { return signalNoiseRatio; }
			set { signalNoiseRatio = value; }
		}

        public float EstimatedNoise {
            get {
                return estimatedNoise;
            }

            set {
                estimatedNoise = value;
            }
        }
    }
}
