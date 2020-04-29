using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj {
    public class MsScanMatchResult {
        // basic annotated information
        public string MoleculeName { get; set; }
        public string InChIKey { get; set; }

        public float TotalSimilarity { get; set; }

        // spectral similarity
        public float WeightedDotProduct { get; set; }
        public float SimpleDotProduct { get; set; }
        public float ReverseDotProduct { get; set; }
        public float MatchedPeaksCount { get; set; }
        public float MatchedPeaksPercentage { get; set; }
        public float EssentialFragmentMatchedScore { get; set; }

        // others
        public float RtSimilarity { get; set; }
        public float RiSimilarity { get; set; }
        public float CcsSimilarity { get; set; }
        public float IsotopeSimilarity { get; set; }
        public float AcurateMassSimilarity { get; set; }

        // Link to database
        public int LibraryID { get; set; }

        // Checker
        public bool IsMs1Match { get; set; }
        public bool IsMs2Match { get; set; }
        public bool IsRtMatch { get; set; }
        public bool IsCcsMatch { get; set; }
        public bool IsLipidClassMatch { get; set; }
        public bool IsLipidChainsMatch { get; set; }
        public bool IsLipidPositionMatch { get; set; }
        public bool IsOtherLipidMatch { get; set; }
    }

}
