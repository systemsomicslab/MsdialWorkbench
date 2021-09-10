using MessagePack;
using System;

namespace CompMs.Common.DataObj.Result {
    [Flags]
    public enum SourceType : byte
    {
        None = 0,
        Unknown = 1 << 0,
        MspDB = 1 << 2,
        TextDB = 1 << 4,
        Manual = 1 << 6,
        FastaDB = 1 << 1
    }
    public enum DataBaseSource {
        None, Msp, Lbm, Text, Fasta
    }

    [MessagePackObject]
    public class MsScanMatchResult {
        // basic annotated information
        [Key(0)]
        public string Name { get; set; }
        [Key(1)]
        public string InChIKey { get; set; }

        [Key(2)]
        public float TotalScore { get; set; }

        // spectral similarity
        [Key(3)]
        public float WeightedDotProduct { get; set; }
        [Key(4)]
        public float SimpleDotProduct { get; set; }
        [Key(5)]
        public float ReverseDotProduct { get; set; }
        [Key(6)]
        public float MatchedPeaksCount { get; set; }
        [Key(7)]
        public float MatchedPeaksPercentage { get; set; }
        [Key(8)]
        public float EssentialFragmentMatchedScore { get; set; }
        [Key(29)]
        public float AndromedaScore { get; set; }

        // others
        [Key(9)]
        public float RtSimilarity { get; set; }
        [Key(10)]
        public float RiSimilarity { get; set; }
        [Key(11)]
        public float CcsSimilarity { get; set; }
        [Key(12)]
        public float IsotopeSimilarity { get; set; }
        [Key(13)]
        public float AcurateMassSimilarity { get; set; }

        // Link to database
        [Key(14)]
        public int LibraryID { get; set; } = -1;
        [Key(24)]
        public int LibraryIDWhenOrdered { get; set; } = -1;

        // Checker
        [Key(15)]
        public bool IsPrecursorMzMatch { get; set; }
        [Key(16)]
        public bool IsSpectrumMatch { get; set; }
        [Key(17)]
        public bool IsRtMatch { get; set; }
        [Key(23)]
        public bool IsRiMatch { get; set; }
        [Key(18)]
        public bool IsCcsMatch { get; set; }
        [Key(19)]
        public bool IsLipidClassMatch { get; set; }
        [Key(20)]
        public bool IsLipidChainsMatch { get; set; }
        [Key(21)]
        public bool IsLipidPositionMatch { get; set; }
        [Key(22)]
        public bool IsOtherLipidMatch { get; set; }
        [IgnoreMember]
        public bool IsUnknown => Source.HasFlag(SourceType.Unknown);

        // Support for multiple annotation method
        [IgnoreMember]
        public bool IsManuallyModified => (Source & SourceType.Manual) != 0;
        [Key(26)]
        public SourceType Source { get; set; }
        [Key(27)]
        public string AnnotatorID { get; set; }
        [Key(28)]
        public int SpectrumID { get; set; } = -1;
        [Key(30)]
        public bool IsDecoy { get; set; } = false;
        [Key(31)]
        public int Priority { get; set; } = -1;

        public MsScanMatchResult Clone() {
            return (MsScanMatchResult)MemberwiseClone();
        }
    }
}
