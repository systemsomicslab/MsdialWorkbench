using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class Peptide {
        public string Sequence { get; set; }
        public Range Position { get; set; }
        public double ExactMass { get; set; }
        public Formula Formula { get; set; }

        public bool IsProteinNterminal { get; set; }
        public bool IsProteinCterminal { get; set; }
        public List<AminoAcid> SequenceObj { get; set; } // N -> C, 

    }
}
