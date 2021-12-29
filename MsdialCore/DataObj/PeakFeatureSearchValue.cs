using CompMs.Common.Components;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.MsdialCore.DataObj {
    public enum PeakFeatureSearchType { ProductIon, NeutralLoss  }
    [MessagePackObject]
    public class PeakFeatureSearchValue {
        [Key(0)]
        public string Title { get; set; }
        [Key(1)]
        public double Mass { get; set; }
        [Key(2)]
        public double MassTolerance { get; set; }
        [Key(3)]
        public double Time { get; set; }
        [Key(4)]
        public double TimeTolerance { get; set; }
        [Key(5)]
        public double AbsoluteIntensityCutoff { get; set; }
        [Key(6)]
        public double RelativeIntensityCutoff { get; set; }
        [Key(7)]
        public PeakFeatureSearchType PeakFeatureSearchType { get; set; }

    }
}
