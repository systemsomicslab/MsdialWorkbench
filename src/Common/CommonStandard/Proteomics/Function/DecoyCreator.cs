using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.Extension;
using CompMs.Common.Proteomics.DataObj;

namespace CompMs.Common.Proteomics.Function {
    public sealed class DecoyCreator {
        private DecoyCreator() {

        }

        public static List<FastaProperty> Convert2DecoyQueries(List<FastaProperty> fastaQueries) {
            var decoyQueries = new List<FastaProperty>();
            foreach (var query in fastaQueries) {
                var cloneQuery = query.Clone();
                var decoySequence = Convert2ReverseSequence(cloneQuery.Sequence, false);
                cloneQuery.Sequence = decoySequence;
                cloneQuery.IsDecoy = true;
                decoyQueries.Add(cloneQuery);
            }
            return decoyQueries;
        }

        public static string Convert2ReverseSequence(string sequence, bool isSwapKL = true) {
            if (sequence.IsEmptyOrNull()) return string.Empty;
            var revSeq = new char[sequence.Length];
            revSeq[0] = sequence[sequence.Length - 1];
            for (int i = 1; i < sequence.Length; i++) {
                var aaChar = sequence[sequence.Length - i - 1];
                if ((aaChar == 'R' || aaChar == 'K') && isSwapKL) {
                    revSeq[i] = revSeq[i - 1];
                    revSeq[i - 1] = aaChar;
                }
                else {
                    revSeq[i] = aaChar;
                }
            }
            return new string(revSeq);
        }

        public static Peptide Convert2DecoyPeptide(Peptide forwardPep, bool isSwapKL = true) {
            if (forwardPep == null) return null;
            var revPep = new Peptide() {
                IsDecoy = true,
                DatabaseOrigin = forwardPep.DatabaseOrigin,
                DatabaseOriginID = forwardPep.DatabaseOriginID,
                Position = new Common.DataObj.Range(forwardPep.Position.Start, forwardPep.Position.End),
                ExactMass = forwardPep.ExactMass,
                IsProteinCterminal = forwardPep.IsProteinCterminal,
                IsProteinNterminal = forwardPep.IsProteinNterminal,
                MissedCleavages = forwardPep.MissedCleavages,
                SamePeptideNumberInSearchedProteins = forwardPep.SamePeptideNumberInSearchedProteins
            };
            var sequence = forwardPep.SequenceObj;
            if (sequence.IsEmptyOrNull()) return revPep;

            var revAAs = new List<AminoAcid>();
            if (isSwapKL) {
                for (int i = 2; i < sequence.Count; i++) {
                    var aaObj = sequence[sequence.Count - i - 1];
                    revAAs.Add(aaObj);
                }
                revAAs.Add(sequence[sequence.Count - 1]);
                revAAs.Add(sequence[sequence.Count - 2]);
                //for (int i = 1; i < sequence.Count; i++) {
                //    var aaObj = sequence[sequence.Count - i - 1];
                //    revAAs.Add(aaObj);
                //}
                //revAAs.Add(sequence[sequence.Count - 1]);
            }
            else {
                for (int i = 0; i < sequence.Count; i++) {
                    var aaObj = sequence[sequence.Count - i - 1];
                    revAAs.Add(aaObj);
                }
            }
            revPep.SequenceObj = revAAs;
            return revPep;
            //var revSeq = new AminoAcid[sequence.Count];
            //revSeq[0] = sequence[sequence.Count - 1];
            //for (int i = 1; i < sequence.Count; i++) {
            //    var aaObj = sequence[sequence.Count - i - 1];
            //    if ((aaObj.OneLetter == 'R' || aaObj.OneLetter == 'K') && isSwapKL) {
            //        revSeq[i] = revSeq[i - 1];
            //        revSeq[i - 1] = aaObj;
            //    }
            //    else {
            //        revSeq[i] = aaObj;
            //    }
            //}
            //revPep.SequenceObj = new List<AminoAcid>(revSeq);
            //return revPep;
        }
    }
}
