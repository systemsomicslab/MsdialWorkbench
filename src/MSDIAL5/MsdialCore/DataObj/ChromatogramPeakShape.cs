using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    [MessagePackObject]
    public class ChromatogramPeakShape {
        [SerializationConstructor]
        public ChromatogramPeakShape() {
            
        }

        public ChromatogramPeakShape(ChromatogramPeakShape peakShape) {
            EstimatedNoise = EstimatedNoise;
            SignalToNoise = SignalToNoise;
            PeakPureValue = PeakPureValue;
            ShapenessValue = ShapenessValue;
            GaussianSimilarityValue = GaussianSimilarityValue;
            IdealSlopeValue = IdealSlopeValue;
            BasePeakValue = BasePeakValue;
            SymmetryValue = SymmetryValue;
            AmplitudeOrderValue = AmplitudeOrderValue;
            AmplitudeScoreValue = AmplitudeScoreValue;
        }

        [Key(0)]
        public float EstimatedNoise { get; set; }
        [Key(1)]
        public float SignalToNoise { get; set; }
        [Key(2)]
        public float PeakPureValue { get; set; }
        [Key(3)]
        public float ShapenessValue { get; set; }
        [Key(4)]
        public float GaussianSimilarityValue { get; set; }
        [Key(5)]
        public float IdealSlopeValue { get; set; }
        [Key(6)]
        public float BasePeakValue { get; set; }
        [Key(7)]
        public float SymmetryValue { get; set; }
        [Key(8)]
        public float AmplitudeOrderValue { get; set; }
        [Key(9)]
        public float AmplitudeScoreValue { get; set; }
    }
}
