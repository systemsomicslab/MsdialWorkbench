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
        public string Sequence {

            get {
                if (cacheSequence is null) {
                    cacheSequence = SequenceObj.IsEmptyOrNull() ?
                    string.Empty :
                    String.Join("", SequenceObj.Select(n => n.OneLetter.ToString()));
                }
                return cacheSequence;
            }
        } // original amino acid sequence

        private string cacheSequence = null;

        [IgnoreMember]
        public string ModifiedSequence { get => SequenceObj.IsEmptyOrNull() ? string.Empty : String.Join("", SequenceObj.Select(n => n.Code())); }
        [Key(2)]
        public CompMs.Common.DataObj.Range Position { get; set; }
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

        [Key(6)]
        public bool IsDecoy { get; set; } = false;
        [Key(7)]
        public int MissedCleavages { get; set; } = 0;
        [Key(8)]
        public int SamePeptideNumberInSearchedProteins { get; set; } = 0;
        [Key(9)]
        public Dictionary<int, int> ResidueCodeIndexToModificationIndex { get; set; } = new Dictionary<int, int>();

        public int CountModifiedAminoAcids() {
            if (SequenceObj == null) return 0;
            return SequenceObj.Count(n => n.IsModified()); 
        }

        public void GenerateSequenceObj(string proteinSeq, int start, int end, Dictionary<int, int> ResidueCodeIndexToModificationIndex, Dictionary<int, string> ID2Code, Dictionary<string, AminoAcid> Code2AminoAcidObj) {
            SequenceObj = GetSequenceObj(proteinSeq, start, end, ResidueCodeIndexToModificationIndex, ID2Code, Code2AminoAcidObj);
        }

        private List<AminoAcid> GetSequenceObj(string proteinSeq, int start, int end, 
            Dictionary<int, int> ResidueCodeIndexToModificationIndex, Dictionary<int, string> iD2Code, Dictionary<string, AminoAcid> code2AminoAcidObj) {
            var sequence = new List<AminoAcid>();
            if (Math.Max(start, end) > proteinSeq.Length - 1) return null;
            for (int i = start; i <= end; i++) {
                var oneleter = proteinSeq[i];
                if (ResidueCodeIndexToModificationIndex.ContainsKey(i)) {
                    var residueID = ResidueCodeIndexToModificationIndex[i];
                    var residueCode = iD2Code[residueID];
                    var aa = code2AminoAcidObj[residueCode];
                    sequence.Add(aa);
                }
                else {
                    var residueCode = oneleter;
                    var aa = code2AminoAcidObj[residueCode.ToString()];
                    sequence.Add(aa);
                }
            }
            return sequence;
        }
    }
}
