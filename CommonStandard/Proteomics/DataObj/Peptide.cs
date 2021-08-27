using CompMs.Common.DataObj;
using CompMs.Common.DataObj.Property;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.Function;
using MessagePack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace CompMs.Common.Proteomics.DataObj {
    [MessagePackObject]
    public class Peptide {
        [Key(0)]
        public string DatabaseOrigin { get; set; }
        [Key(1)]
        public int DatabaseOriginID { get; set; }
        [IgnoreMember]
        public string Sequence { get => SequenceObj.IsEmptyOrNull() ? string.Empty : String.Join("", SequenceObj.Select(n => n.OneLetter.ToString())); } // original amino acid sequence
        [IgnoreMember]
        public string ModifiedSequence { get => SequenceObj.IsEmptyOrNull() ? string.Empty : String.Join("", SequenceObj.Select(n => n.Code())); }
        [Key(2)]
        public Range Position { get; set; }
        [Key(3)]
        public double ExactMass { get; set; }
        [IgnoreMember]
        public Formula Formula { get => SequenceObj.IsEmptyOrNull() ? null : PeptideCalc.CalculatePeptideFormula(SequenceObj); }

        [Key(4)]
        public bool IsProteinNterminal { get; set; }
        [Key(5)]
        public bool IsProteinCterminal { get; set; }
        [IgnoreMember]
        public List<AminoAcid> SequenceObj { get; set; } // N -> C, including modified amino acid information

        public int CountModifiedAminoAcids() { return SequenceObj.Count(n => n.IsModified()); }
    }
}
