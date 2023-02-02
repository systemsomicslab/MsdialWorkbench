using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Query {
    public enum SearchType { ProductIon, NeutralLoss }
    [MessagePackObject]
    public class MzSearchQuery {
        [Key(0)]
        public double Mass { get; set; }
        [Key(1)]
        public double RelativeIntensity { get; set; }
        [Key(2)]
        public SearchType SearchType { get; set; }
        [Key(3)]
        public double MassTolerance { get; set; }
    }
}
