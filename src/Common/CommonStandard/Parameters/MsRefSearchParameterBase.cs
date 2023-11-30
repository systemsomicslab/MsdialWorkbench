using MessagePack;

namespace CompMs.Common.Parameter {
    [MessagePackObject]
    public class MsRefSearchParameterBase {
        [Key(0)]
        public float MassRangeBegin { get; set; } = 0;
        [Key(1)]
        public float MassRangeEnd { get; set; } = 2000;
        [Key(2)]
        public float RtTolerance { get; set; } = 100.0F;
        [Key(3)]
        public float RiTolerance { get; set; } = 100.0F;
        [Key(4)]
        public float CcsTolerance { get; set; } = 20.0F;
        [Key(5)]
        public float Ms1Tolerance { get; set; } = 0.01F;
        [Key(6)]
        public float Ms2Tolerance { get; set; } = 0.025F;
        [Key(7)]
        public float RelativeAmpCutoff { get; set; } = 0F;
        [Key(8)]
        public float AbsoluteAmpCutoff { get; set; } = 0;
        
        // by [0-1]
        [Key(9)]
        public float WeightedDotProductCutOff { get; set; } = 0.6F;
        [Key(10)]
        public float SimpleDotProductCutOff { get; set; } = 0.6F;
        [Key(11)]
        public float ReverseDotProductCutOff { get; set; } = 0.8F;
        [Key(12)]
        public float MatchedPeaksPercentageCutOff { get; set; } = 0.25F;
        [Key(19)]
        public float AndromedaScoreCutOff { get; set; } = 0.1F;
        [Key(13)]
        public float TotalScoreCutoff { get; set; } = 0.8F;

        // by absolute value
        [Key(14)]
        public float MinimumSpectrumMatch { get; set; } = 3;

        // option
        [Key(15)]
        public bool IsUseTimeForAnnotationFiltering { get; set; } = false;
        [Key(16)]
        public bool IsUseTimeForAnnotationScoring { get; set; } = false;
        [Key(17)]
        public bool IsUseCcsForAnnotationFiltering { get; set; } = false;
        [Key(18)]
        public bool IsUseCcsForAnnotationScoring { get; set; } = false;


        public MsRefSearchParameterBase() { }
        public MsRefSearchParameterBase(MsRefSearchParameterBase parameter) {
            MassRangeBegin = parameter.MassRangeBegin;
            MassRangeEnd = parameter.MassRangeEnd;
            RtTolerance = parameter.RtTolerance;
            RiTolerance = parameter.RiTolerance;
            CcsTolerance = parameter.CcsTolerance;
            Ms1Tolerance = parameter.Ms1Tolerance;
            Ms2Tolerance = parameter.Ms2Tolerance;
            RelativeAmpCutoff = parameter.RelativeAmpCutoff;
            AbsoluteAmpCutoff = parameter.AbsoluteAmpCutoff;
            WeightedDotProductCutOff = parameter.WeightedDotProductCutOff;
            SimpleDotProductCutOff = parameter.SimpleDotProductCutOff;
            ReverseDotProductCutOff = parameter.ReverseDotProductCutOff;
            MatchedPeaksPercentageCutOff = parameter.MatchedPeaksPercentageCutOff;
            TotalScoreCutoff = parameter.TotalScoreCutoff;
            MinimumSpectrumMatch = parameter.MinimumSpectrumMatch;
            IsUseTimeForAnnotationFiltering = parameter.IsUseTimeForAnnotationFiltering;
            IsUseTimeForAnnotationScoring = parameter.IsUseTimeForAnnotationScoring;
            IsUseCcsForAnnotationFiltering = parameter.IsUseCcsForAnnotationFiltering;
            IsUseCcsForAnnotationScoring = parameter.IsUseCcsForAnnotationScoring;
        }

        public string ParameterAsString() {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine($"{nameof(MassRangeBegin)}: {MassRangeBegin:f5}");
            sb.AppendLine($"{nameof(MassRangeEnd)}: {MassRangeEnd:f5}");
            sb.AppendLine($"{nameof(RtTolerance)}: {RtTolerance:f2}");
            sb.AppendLine($"{nameof(RiTolerance)}: {RiTolerance:f3}");
            sb.AppendLine($"{nameof(CcsTolerance)}: {CcsTolerance:f2}");
            sb.AppendLine($"{nameof(Ms1Tolerance)}: {Ms1Tolerance:f5}");
            sb.AppendLine($"{nameof(Ms2Tolerance)}: {Ms2Tolerance:f5}");
            sb.AppendLine($"{nameof(RelativeAmpCutoff)}: {RelativeAmpCutoff:f3}");
            sb.AppendLine($"{nameof(AbsoluteAmpCutoff)}: {AbsoluteAmpCutoff}");
            sb.AppendLine($"{nameof(WeightedDotProductCutOff)}: {WeightedDotProductCutOff:f3}");
            sb.AppendLine($"{nameof(SimpleDotProductCutOff)}: {SimpleDotProductCutOff:f3}");
            sb.AppendLine($"{nameof(ReverseDotProductCutOff)}: {ReverseDotProductCutOff:f3}");
            sb.AppendLine($"{nameof(MatchedPeaksPercentageCutOff)}: {MatchedPeaksPercentageCutOff:f3}");
            sb.AppendLine($"{nameof(AndromedaScoreCutOff)}: {AndromedaScoreCutOff:f3}");
            sb.AppendLine($"{nameof(TotalScoreCutoff)}: {TotalScoreCutoff:f3}");
            sb.AppendLine($"{nameof(MinimumSpectrumMatch)}: {MinimumSpectrumMatch:f3}");
            sb.AppendLine($"{nameof(IsUseTimeForAnnotationFiltering)}: {IsUseTimeForAnnotationFiltering:f3}");
            sb.AppendLine($"{nameof(IsUseTimeForAnnotationScoring)}: {IsUseTimeForAnnotationScoring:f3}");
            sb.AppendLine($"{nameof(IsUseCcsForAnnotationFiltering)}: {IsUseCcsForAnnotationFiltering:f3}");
            sb.AppendLine($"{nameof(IsUseCcsForAnnotationScoring)}: {IsUseCcsForAnnotationScoring:f3}");
            return sb.ToString();
        }
    }
}
