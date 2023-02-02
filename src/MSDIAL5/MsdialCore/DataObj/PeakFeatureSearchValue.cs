using CompMs.Common.Components;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    public enum PeakFeatureSearchType { ProductIon, NeutralLoss  }
    public enum PeakFeatureQueryLevel { MS1, MS2 }
    [MessagePackObject]
    public class PeakFeatureSearchValue {
        [Key(0)]
        public string Title { get; set; }
        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double MassTolerance { get; set; }
        [Key(3)]
        public double TimeMin { get; set; }
        [Key(4)]
        public double TimeMax { get; set; }
        [Key(5)]
        public double AbsoluteIntensityCutoff { get; set; }
        [Key(6)]
        public double RelativeIntensityCutoff { get; set; }
        [Key(7)]
        public PeakFeatureSearchType PeakFeatureSearchType { get; set; }
        [Key(8)]
        public PeakFeatureQueryLevel PeakFeatureQueryLevel { get; set; }
        [Key(9)]
        public double MobilityMin { get; set; }
        [Key(10)]
        public double MobilityMax { get; set; }
    }
}
