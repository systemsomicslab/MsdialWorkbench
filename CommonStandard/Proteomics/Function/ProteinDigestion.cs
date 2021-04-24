using CompMs.Common.DataObj;
using CompMs.Common.Proteomics.DataObj;
using System;
using System.Collections.Generic;
using System.Text;

namespace CompMs.Common.Proteomics.Function {
    
    public sealed class ProteinDigestion {
        private ProteinDigestion() { }

        public static List<Peptide> GetDigestedPeptideSequences(string proteinSequence, List<string> cleavagesites) {
            var peptides = new List<Peptide>();
            var peptideSequence = string.Empty;
            var twoletterAAs = string.Empty;
            var start = 0;
            var end = 0;
            for (int i = 0; i < proteinSequence.Length - 1; i++) {
                peptideSequence += proteinSequence[i];
                twoletterAAs = proteinSequence[i].ToString() + proteinSequence[i + 1].ToString();

                if (cleavagesites.Contains(twoletterAAs)) {
                    end = i;
                    var peptide = new Peptide() { Sequence = peptideSequence, Position = new Range(start, end) };
                    peptide.IsProteinNterminal = start == 0 ? true : false;
                    peptide.IsProteinCterminal = end == proteinSequence.Length - 1 ? true : false;
                    peptides.Add(peptide);

                    peptideSequence = string.Empty;
                    start = i + 1;
                    end = i + 1;
                }
            }

            return peptides;
        }
    }
}
