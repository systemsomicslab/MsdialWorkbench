using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class Peptide {
        public string DatabaseOrigin { get; set; }
        public int DatabaseOriginID { get; set; }
        public string Sequence { get; set; } // original amino acid sequence
        public string ModifiedSequence { get; set; }
        public Range Position { get; set; }
        public double ExactMass() { return Formula.Mass; }
        public Formula Formula { get; set; }

        public bool IsProteinNterminal { get; set; }
        public bool IsProteinCterminal { get; set; }
        public List<AminoAcid> SequenceObj { get; set; } // N -> C, including modified amino acid information
    }
}
