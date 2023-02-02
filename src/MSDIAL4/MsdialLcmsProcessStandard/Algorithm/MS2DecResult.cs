using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Msdial.Lcms.Dataprocess.Algorithm {
    public class MS2DecResult {

        private float peakTopRetentionTime;
        private float ms1AccurateMass;
        private int peakTopScan;
        private float uniqueMs;
        private float ms1PeakHeight;
        private float ms1IsotopicIonM1PeakHeight;
        private float ms1IsotopicIonM2PeakHeight;

        private float ms2DecPeakHeight;
        private float ms2DecPeakArea;

        private List<List<double[]>> peaklistList;
        private List<double[]> massSpectra;
        private List<double[]> baseChromatogram;
        private List<float> modelMasses;

        public float PeakTopRetentionTime {
            get { return peakTopRetentionTime; }
            set { peakTopRetentionTime = value; }
        }

        public float Ms1AccurateMass {
            get { return ms1AccurateMass; }
            set { ms1AccurateMass = value; }
        }

        public int PeakTopScan {
            get { return peakTopScan; }
            set { peakTopScan = value; }
        }

        public float UniqueMs {
            get { return uniqueMs; }
            set { uniqueMs = value; }
        }


        public List<double[]> MassSpectra {
            get { return massSpectra; }
            set { massSpectra = value; }
        }

        public List<List<double[]>> PeaklistList {
            get { return peaklistList; }
            set { peaklistList = value; }
        }

        public List<double[]> BaseChromatogram {
            get { return baseChromatogram; }
            set { baseChromatogram = value; }
        }

        public List<float> ModelMasses {
            get { return modelMasses; }
            set { modelMasses = value; }
        }

        public float Ms1PeakHeight {
            get { return ms1PeakHeight; }
            set { ms1PeakHeight = value; }
        }

        public float Ms2DecPeakHeight {
            get { return ms2DecPeakHeight; }
            set { ms2DecPeakHeight = value; }
        }

        public float Ms1IsotopicIonM1PeakHeight {
            get { return ms1IsotopicIonM1PeakHeight; }
            set { ms1IsotopicIonM1PeakHeight = value; }
        }

        public float Ms1IsotopicIonM2PeakHeight {
            get { return ms1IsotopicIonM2PeakHeight; }
            set { ms1IsotopicIonM2PeakHeight = value; }
        }

        public float Ms2DecPeakArea {
            get { return ms2DecPeakArea; }
            set { ms2DecPeakArea = value; }
        }

        public MS2DecResult() {
            peaklistList = new List<List<double[]>>();
            massSpectra = new List<double[]>();
            baseChromatogram = new List<double[]>();
            modelMasses = new List<float>();
        }

    }
}
