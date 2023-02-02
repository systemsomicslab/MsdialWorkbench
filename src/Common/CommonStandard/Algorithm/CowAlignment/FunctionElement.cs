using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CompMs.Common.Algorithm.CowAlignment {
    /// <summary>
    /// This class is used in dynamic programming algorithm of CowAlignment.cs.
    /// </summary>
    internal class FunctionElement
    {
        private double score;
        private TraceDirection trace;
        private int warp;

        public int Warp
        {
            get { return warp; }
            set { warp = value; }
        }

        public double Score
        {
            get { return score; }
            set { score = value; }
        }

        public TraceDirection Trace
        {
            get { return trace; }
            set { trace = value; }
        }

        public FunctionElement(double score, TraceDirection trace)
        {
            this.score = score;
            this.trace = trace;
        }

        public FunctionElement(double score, int warp)
        {
            this.score = score;
            this.warp = warp;
        }
    }
}
