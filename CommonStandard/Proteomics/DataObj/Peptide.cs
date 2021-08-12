using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.Function;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    public class Peptide {
        public string DatabaseOrigin { get; set; }
        public int DatabaseOriginID { get; set; }
        public string Sequence { get => SequenceObj.IsEmptyOrNull() ? string.Empty : String.Join("", SequenceObj.Select(n => n.OneLetter.ToString())); } // original amino acid sequence
        public string ModifiedSequence { get => SequenceObj.IsEmptyOrNull() ? string.Empty : String.Join("", SequenceObj.Select(n => n.Code())); }
        public Range Position { get; set; }
        public double ExactMass { get; set; }
        public Formula Formula { get => SequenceObj.IsEmptyOrNull() ? null : PeptideCalc.CalculatePeptideFormula(SequenceObj); }

        public bool IsProteinNterminal { get; set; }
        public bool IsProteinCterminal { get; set; }
        public List<AminoAcid> SequenceObj { get; set; } // N -> C, including modified amino acid information

        public int CountModifiedAminoAcids() { return SequenceObj.Count(n => n.IsModified()); }
    }
}
