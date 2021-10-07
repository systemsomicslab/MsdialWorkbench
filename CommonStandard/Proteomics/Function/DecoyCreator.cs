using System;
using System.Collections.Generic;
using System.Text;
using CompMs.Common.Components;
using CompMs.Common.Extension;

namespace CompMs.Common.Proteomics.Function {
    public sealed class DecoyCreator {
        private DecoyCreator() {

        }

        public static List<FastaProperty> Convert2DecoyQueries(List<FastaProperty> fastaQueries) {
            var decoyQueries = new List<FastaProperty>();
            foreach (var query in fastaQueries) {
                var cloneQuery = query.Clone();
                var decoySequence = Convert2ReverseSequence(cloneQuery.Sequence);
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
    }
}
