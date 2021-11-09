using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class PeptideScore {
        public float PosteriorErrorProb { get; set; }
        public float AndromedaScore { get; }
        public int FileID { get; }
        public int PeakID { get; }
        public int PeptideLength { get; }
        public int MissedCleavages { get; }
        public int Modifications { get; }
        public bool IsDecoy { get; }

        public PeptideScore(float score, int fileID, int peakID, int pepLength, 
            int missedCleavages, int modifications, bool isDecoy) {
            AndromedaScore = score;
            FileID = fileID;
            PeakID = peakID;
            PeptideLength = pepLength;
            MissedCleavages = missedCleavages;
            Modifications = modifications;
            IsDecoy = isDecoy;
        }
    }
}
