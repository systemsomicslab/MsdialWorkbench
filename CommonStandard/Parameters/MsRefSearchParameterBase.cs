using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Parameter {
    public class MsRefSearchParameterBase {
        public float MassRangeBegin { get; set; } = 0;
        public float MassRangeEnd { get; set; } = 2000;
        public float RtTolerance { get; set; } = 100.0F;
        public float RiTolerance { get; set; } = 100.0F;
        public float CcsTolerance { get; set; } = 10.0F;
        public float Ms1Tolerance { get; set; } = 0.01F;
        public float Ms2Tolerance { get; set; } = 0.05F;

        // by [0-1]
        public float WeightedDotProductCutOff { get; set; } = 0.5F;
        public float SimpleDotProductCutOff { get; set; } = 0.5F;
        public float ReverseDotProductCutOff { get; set; } = 0.5F;
        public float MatchedPeaksPercentageCutOff { get; set; } = 0.5F;
        public float TotalScoreCutoff { get; set; } = 0.8F;

        // by absolute value
        public float MinimumSpectrumMatch { get; set; } = 3;
    }
}
