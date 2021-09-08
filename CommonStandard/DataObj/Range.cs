using MessagePack;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj {
    [MessagePackObject]
    public class Range {
        [Key(0)]
        public int Start { get; }
        [Key(1)]
        public int End { get; }
        [IgnoreMember]
        public int Length { get => End - Start + 1; }
        public override string ToString() {
            return Start.ToString() + "_" + End.ToString();
        }
        public Range(int start, int end) {
            this.Start = start;
            this.End = end;
        }
    }
}
