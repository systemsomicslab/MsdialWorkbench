using MessagePack;
using System;

namespace CompMs.Common.DataObj.Result {
    [Flags]
    public enum SourceType : byte
    {
        None = 0,
        Unknown = 1 << 0,
        FastaDB = 1 << 1,
        MspDB = 1 << 2,
        TextDB = 1 << 4,
        GeneratedLipid = 1 << 5,
        Manual = 1 << 6,
        DataBases = FastaDB | MspDB | TextDB | GeneratedLipid,
    }
    public enum DataBaseSource {
        None, Msp, Lbm, Text, Fasta, EieioLipid, OadLipid, EidLipid, MsFinder,
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
        public float SquaredWeightedDotProduct { get; set; }
        [IgnoreMember]
        public float WeightedDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredWeightedDotProduct, 0f));
            set => SquaredWeightedDotProduct = value * value;
        }

        [Key(4)]
        public float SquaredSimpleDotProduct { get; set; }
        [IgnoreMember]
        public float SimpleDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredSimpleDotProduct, 0f));
            set => SquaredSimpleDotProduct = value * value;
        }
        [Key(5)]
        public float SquaredReverseDotProduct { get; set; }
        [IgnoreMember]
        public float ReverseDotProduct {
            get => (float)Math.Sqrt(Math.Max(SquaredReverseDotProduct, 0f));
            set => SquaredReverseDotProduct = value * value;
        }
        [Key(6)]
        public float MatchedPeaksCount { get; set; }
        [Key(7)]
        public float MatchedPeaksPercentage { get; set; }
        [Key(8)]
        public float EssentialFragmentMatchedScore { get; set; }
        [Key(29)]
        public float AndromedaScore { get; set; }
        [Key(32)]
        public float PEPScore { get; set; }

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
        [Key(35)]
        public bool IsLipidDoubleBondPositionMatch { get; set; }
        [Key(22)]
        public bool IsOtherLipidMatch { get; set; }
        [IgnoreMember]
        public bool IsUnknown => Source.HasFlag(SourceType.Unknown);
        [IgnoreMember]
        public bool AnyMatched => (Source & SourceType.DataBases) != SourceType.None;

        /// <summary>
        /// True when a product-ion spectrum was actually compared against a reference spectrum, so the
        /// spectral score fields below hold measurements rather than unset defaults.
        /// </summary>
        /// <remarks>
        /// The scoring functions in MsScanMatching return -1 when there is nothing to compare, which is
        /// the same condition that gives a suggested annotation the "no MS2: " prefix. That -1 survives
        /// in <see cref="SquaredSimpleDotProduct"/> and its siblings, but the <see cref="SimpleDotProduct"/>,
        /// <see cref="WeightedDotProduct"/> and <see cref="ReverseDotProduct"/> getters clamp it to 0, so a
        /// score that was never computed is otherwise indistinguishable from one that was computed as 0.
        /// A text database holds no reference spectrum, so no comparison is attempted for a TextDB result
        /// either and its annotator leaves every spectral field at the default 0.
        /// </remarks>
        [IgnoreMember]
        public bool IsSpectrumComparisonPerformed {
            get {
                if (IsUnknown) {
                    return false;
                }
                if ((Source & SourceType.DataBases) == SourceType.TextDB) {
                    return false;
                }
                return SquaredSimpleDotProduct >= 0f
                    && SquaredWeightedDotProduct >= 0f
                    && SquaredReverseDotProduct >= 0f
                    && MatchedPeaksCount >= 0f
                    && MatchedPeaksPercentage >= 0f;
            }
        }

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
        [Key(33)]
        public bool IsReferenceMatched { get; set; } = false;
        [Key(34)]
        public bool IsAnnotationSuggested { get; set; } = false;
        [Key(36)]
        public double CollisionEnergy { get; set; }
        [Key(37)]
        public float EnhancedDotProduct { get; set; }
        [Key(38)]
        public float SpectralEntropy { get; set; }

        public MsScanMatchResult Clone() {
            return (MsScanMatchResult)MemberwiseClone();
        }
    }
}
