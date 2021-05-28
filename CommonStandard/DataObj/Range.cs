using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.DataObj {
    public class Range {
        public int Start { get; }
        public int End { get; }
        public int Length() { return End - Start + 1; }
        public Range(int start, int end) {
            this.Start = start;
            this.End = end;
        }
    }
}
